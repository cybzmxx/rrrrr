using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using Kinect;

public class KinectSensor : MonoBehaviour, KinectInterface {
	//make KinectSensor a singleton (sort of)
	private static KinectInterface instance;
    public static KinectInterface Instance
    {
        get
        {
            if (instance == null)
                throw new Exception("There needs to be an active instance of the KinectSensor component.");
            return instance;
        }
        private set
        { instance = value; }
    }
	
	/// <summary>
	/// how high (in meters) off the ground is the sensor
	/// </summary>
	public float sensorHeight;
	/// <summary>
	/// where (relative to the ground directly under the sensor) should the kinect register as 0,0,0
	/// </summary>
	public Vector3 kinectCenter;
	/// <summary>
	/// what point (relative to kinectCenter) should the sensor look at
	/// </summary>
	public Vector4 lookAt;
	
	/// <summary>
	/// Variables used to pass to smoothing function. Values are set to default based on Action in Motion's Research
	/// </summary>
	public float smoothing =0.5f;	
	public float correction=0.5f;
	public float prediction=0.5f;
	public float jitterRadius=0.05f;
	public float maxDeviationRadius=0.04f;
	//public bool enableNearMode = false;
	
	/// <summary>
	///variables used for updating and accessing depth data 
	/// </summary>
	private bool updatedSkeleton = false;
	private bool newSkeleton = false;
	[HideInInspector]
	private NuiSkeletonFrame skeletonFrame = new NuiSkeletonFrame() { SkeletonData = new NuiSkeletonData[6] };
	private Vector4[] m_skeletonPosition = null;
	private float[] m_skldata = null;
	private float[,] m_skldata_mul = null;
    private int[] m_userID = null;


    private byte[] handEvent = null;
    private bool m_isLeftHandGrip = false;
    private bool m_isRightHandGrip = false;

    private byte[,] handEvent_Multi = null;
    private bool[] m_isLeftHandGrip_Multi = null;
    private bool[] m_isRightHandGrip_Multi = null;

    /// <summary>
	///variables used for updating and accessing video data 
	/// </summary>
	private bool updatedBackgroundRemoval = false;
	private bool newBackgroundRemoval = false;
	[HideInInspector]
	private Color32[] BackgroundRemovalImage;
	private Color32[] BackgroundRemovalImageTexture;
	byte[] m_BackgroundRemoval_data = null;

	/// <summary>
	///variables used for updating and accessing video data 
	/// </summary>
	private bool updatedColor = false;
	private bool newColor = false;
	[HideInInspector]
	private Color32[] colorImage;
	private Color32[] colorImageTexture;
	private byte[] m_color_data = null;

	/// <summary>
	///variables used for updating and accessing depth data 
	/// </summary>
	private bool updatedDepth = false;
	private bool newDepth = false;
	[HideInInspector]
	private short[] depthPlayerData;
	//private Color32[] depthImage;
	private Color32[] depthImageTexture;
	private byte[] m_depth_data = null;

	//image stream handles for the kinect
	private IntPtr colorStreamHandle;
	private IntPtr depthStreamHandle;

	private int depth_width = 640;
	private int depth_height = 480;
	private int video_width = 640;
	private int video_height = 480;

    private System.Threading.Thread m_th = null;

	float KinectInterface.getSensorHeight() {
		return sensorHeight;
	}
	Vector3 KinectInterface.getKinectCenter() {
		return kinectCenter;
	}
	Vector4 KinectInterface.getLookAt() {
		return lookAt;
	}
	
	
	void Awake()
	{
		if (KinectSensor.instance != null)
		{
			Debug.Log("There should be only one active instance of the KinectSensor component at at time.");
            throw new Exception("There should be only one active instance of the KinectSensor component at a time.");
		}
		try
        {
            // The MSR Kinect DLL (native code) is going to load into the Unity process and stay resident even between debug runs of the game.  
            // So our component must be resilient to starting up on a second run when the Kinect DLL is already loaded and
            // perhaps even left in a running state.  Kinect does not appear to like having NuiInitialize called when it is already initialized as
            // it messes up the internal state and stops functioning.  It is resilient to having Shutdown called right before initializing even if it
            // hasn't been initialized yet.  So calling this first puts us in a good state on a first or second run.
            // However, calling NuiShutdown before starting prevents the image streams from being read, so if you want to use image data
            // (either depth or RGB), comment this line out.
            //NuiShutdown();

            int hr = NativeMethods.qfKinectInit();
            if (hr != 0)
            {
                throw new Exception("NuiInitialize Failed.");
            }

            NativeMethods.qfKinectSetEnableMultiUser(true);
            NativeMethods.qfKinectSetEnableBackgroundRemoval(true);

            ////////////////////////////////////////////////////////
            depth_width = NativeMethods.qfKinectGetDepthWidth();
            depth_height = NativeMethods.qfKinectGetDepthHeight();

            video_width = NativeMethods.qfKinectGetVideoWidth();
            video_height = NativeMethods.qfKinectGetVideoHeight();

            ////////////////////////////////////////////////////////
            //init
            BackgroundRemovalImage = new Color32[video_width * video_height];
            BackgroundRemovalImageTexture = new Color32[video_width * video_height];
            m_BackgroundRemoval_data = new byte[video_width * video_height * 4];

            colorImage = new Color32[video_width * video_height];
            colorImageTexture = new Color32[video_width * video_height];
            m_color_data = new byte[video_width * video_height * 4];

            //depthImage = new Color32[depth_width * depth_height];
            m_depth_data = new byte[depth_width * depth_height * 4];
            depthPlayerData = new short[depth_width * depth_height];

            m_skldata = new float[20 * 4];
            m_skldata_mul = new float[6, 20 * 4];
            m_userID = new int[6];

            handEvent = new byte[2];
            handEvent_Multi = new byte[6, 2];
            m_isLeftHandGrip_Multi = new bool[6];
            m_isRightHandGrip_Multi = new bool[6];

            for (int i = 0; i < skeletonFrame.SkeletonData.Length; ++i)
            {
                skeletonFrame.SkeletonData[i].eTrackingState = Kinect.NuiSkeletonTrackingState.NotTracked;

                skeletonFrame.SkeletonData[i].eSkeletonPositionTrackingState = new NuiSkeletonPositionTrackingState[20];
                skeletonFrame.SkeletonData[i].SkeletonPositions = new Vector4[20];

                for (int i_skl = 0; i_skl < skeletonFrame.SkeletonData[i].eSkeletonPositionTrackingState.Length; ++i_skl)
                {
                    skeletonFrame.SkeletonData[i].eSkeletonPositionTrackingState[i_skl] = Kinect.NuiSkeletonPositionTrackingState.NotTracked;
                }
            }
            ////////////////////////////////////////////////////////

            DontDestroyOnLoad(gameObject);
            KinectSensor.Instance = this;

            if (null == m_th)
            {
                m_th = new System.Threading.Thread(new System.Threading.ThreadStart(pollThread));
                m_th.Start();
            }
        }
		catch (Exception e)
		{
			Debug.Log(e.Message);
		}
	}
	
	void LateUpdate()
	{
		updatedSkeleton = false;
		newSkeleton = false;

		updatedBackgroundRemoval = false;
		newBackgroundRemoval = false;
		
		updatedColor = false;
		//newColor = false;
		
		updatedDepth = false;
		newDepth = false;
		
		m_isLeftHandGrip = false;
		m_isRightHandGrip = false;
		
		m_skeletonPosition = null;
	}
	/// <summary>
	///The first time in each frame that it is called, poll the kinect for updated skeleton data and return
	///true if there is new data. Subsequent calls do nothing and return the same value.
	/// </summary>
	/// <returns>
	/// A <see cref="System.Boolean"/> : is there new data this frame
	/// </returns>
	bool pollSkeleton_1user()
	{
		if (!updatedSkeleton)
		{
			updatedSkeleton = true;
			
			float[] skldata = m_skldata;
			if (null != skldata)
			{
				int hr = NativeMethods.qfKinectCopySkeletonData(skldata);
				if(hr == 0)
				{
					newSkeleton = true;
				}
				
				int i_skldata = 0;
				
				for (int i=0; i<1; ++i)
				{
					skeletonFrame.liTimeStamp = (long)(Time.time * 1000);
					
					skeletonFrame.SkeletonData[i].eTrackingState =
						newSkeleton ? Kinect.NuiSkeletonTrackingState.SkeletonTracked
									: Kinect.NuiSkeletonTrackingState.NotTracked;
					
					for (int i_skl=0
							; i_skl<skeletonFrame.SkeletonData[i].eSkeletonPositionTrackingState.Length
							; ++i_skl, i_skldata+=4)
					{
						skeletonFrame.SkeletonData[i].eSkeletonPositionTrackingState[i_skl] =
								newSkeleton ? Kinect.NuiSkeletonPositionTrackingState.Tracked
											: Kinect.NuiSkeletonPositionTrackingState.NotTracked;
						
						skeletonFrame.SkeletonData[i].SkeletonPositions[i_skl] =
								new Vector4(skldata[i_skldata+0], skldata[i_skldata+1], skldata[i_skldata+2], skldata[i_skldata+3]);
					}
				}
				
				//default user
				m_skeletonPosition = skeletonFrame.SkeletonData[0].SkeletonPositions;
			}

			if (null != handEvent)
			{
				//handEvent
				for (int i=0; i<handEvent.Length; ++i)
				{
					handEvent[i] = 0;
				}
				
				try {
					NativeMethods.qfKinectCopyHandEventReslut(handEvent);
				} catch (Exception ex) {
					Debug.Log(ex);
				}
				
				m_isLeftHandGrip = handEvent[0] == 1;
				m_isRightHandGrip = handEvent[1] == 1;
			}

		}
		
		return newSkeleton;
	}

    bool KinectInterface.pollSkeleton()
    {
        if (!updatedSkeleton)
        {
            updatedSkeleton = true;

            float[,] skldata = m_skldata_mul;
            int[] userID = m_userID;

            if (null != skldata)
            {
                int hr = NativeMethods.qfKinectCopyMultiSkeletonData(skldata, userID);
                if (hr == 0)
                {
                    newSkeleton = true;
                }


                for (int i = 0; i < 6; ++i)
                {
                    skeletonFrame.liTimeStamp = (long)(Time.time * 1000);

                    skeletonFrame.SkeletonData[i].eTrackingState =
                        (newSkeleton && (userID[i]!=0)) ? Kinect.NuiSkeletonTrackingState.SkeletonTracked
                                    : Kinect.NuiSkeletonTrackingState.NotTracked;

                    int i_skldata = 0;
                    for (int i_skl = 0
                            ; i_skl < skeletonFrame.SkeletonData[i].eSkeletonPositionTrackingState.Length
                            ; ++i_skl, i_skldata += 4)
                    {
                        skeletonFrame.SkeletonData[i].eSkeletonPositionTrackingState[i_skl] =
                                (newSkeleton && (userID[i] != 0)) ? Kinect.NuiSkeletonPositionTrackingState.Tracked
                                            : Kinect.NuiSkeletonPositionTrackingState.NotTracked;

                        skeletonFrame.SkeletonData[i].SkeletonPositions[i_skl] =
                                new Vector4(skldata[i, i_skldata + 0]
                                                , skldata[i, i_skldata + 1]
                                                , skldata[i, i_skldata + 2]
                                                , skldata[i, i_skldata + 3]
                                            );
                    }
                }

                //default user
                m_skeletonPosition = skeletonFrame.SkeletonData[0].SkeletonPositions;
            }

            if (null != handEvent_Multi)
            {
                //handEvent
                for (int i = 0; i < 6; ++i)
                {
                    for (int j=0; j<2; ++j)
                    {
                        handEvent_Multi[i,j] = 0;
                    }
                }

                try
                {
                    NativeMethods.qfKinectCopyMultiHandEventReslut(handEvent_Multi);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }

                for (int i = 0; i < 6; ++i)
                {
                    m_isLeftHandGrip_Multi[i] = handEvent_Multi[i,0] == 1;
                    m_isRightHandGrip_Multi[i] = handEvent_Multi[i,1] == 1;
                }
            }

        }

        return newSkeleton;
    }
    
    NuiSkeletonFrame KinectInterface.getSkeleton()
    {
		return skeletonFrame;
	}
	
	bool KinectInterface.getIsLeftHandGrip(int i)
	{
        if (i >= 0 && i < 6)
        {
            return m_isLeftHandGrip_Multi[i];
        }

        return false;
	}
	
	bool KinectInterface.getIsRightHandGrip(int i)
	{
        if (i >= 0 && i < 6)
        {
            return m_isRightHandGrip_Multi[i];
        }

        return false;
    }

	Vector4[] KinectInterface.getSkeleton_defaultUser()
	{
		return m_skeletonPosition;
	}
	
	bool KinectInterface.getIsLeftHandGrip_defaultUser()
	{
		return m_isLeftHandGrip;
	}
	
	bool KinectInterface.getIsRightHandGrip_defaultUser()
	{
		return m_isRightHandGrip;
	}

	/// <summary>
	///The first time in each frame that it is called, poll the kinect for updated color data and return
	///true if there is new data. Subsequent calls do nothing and return the same value.
	/// </summary>
	/// <returns>
	/// A <see cref="System.Boolean"/> : is there new data this frame
	/// </returns>
	bool KinectInterface.pollColor()
	{
		if (!updatedColor)
		{
			updatedColor = true;

            m_need_pollColor = true;
		}

		return true;
	}
	
	Color32[] KinectInterface.getColor(){
		return colorImage;
	}
	
	Color32[] KinectInterface.getColorTexture() {
        if (newColor)
        {
            newColor = false;
            return colorImageTexture;
        }

        return null;
	}
	
	/// <summary>
	///The first time in each frame that it is called, poll the kinect for updated depth (and player) data and return
	///true if there is new data. Subsequent calls do nothing and return the same value.
	/// </summary>
	/// <returns>
	/// A <see cref="System.Boolean"/> : is there new data this frame
	/// </returns>
	bool KinectInterface.pollDepth()
	{
		if (!updatedDepth)
		{
			updatedDepth = true;

            m_need_pollDepth = true;
		}
		
		return true;
	}
	
	short[] KinectInterface.getDepth(){
        if (newDepth)
        {
            newDepth = false;
            return depthPlayerData;
        }

        return null;
	}
	
	private Color32[] extractColorImage(byte[] buf)
	{
		int totalPixels = video_width * video_height;
		int i_byte = 0;
		
		Color32[] colorBuf = colorImage;

		for (int pix = 0; pix < totalPixels; ++pix, i_byte+=4)
		{
			colorBuf[pix].r = buf[i_byte+0];
			colorBuf[pix].g = buf[i_byte+1];
			colorBuf[pix].b = buf[i_byte+2];
			colorBuf[pix].a = buf[i_byte+3];
		}
		return colorBuf;
	}
	
	private short[] extractDepthImage(byte[] buf)
	{
		short[] newbuf = depthPlayerData;
		for (int i=0, i_byte=0; i<newbuf.Length; ++i, i_byte+=4)
		{
			newbuf[i] = (short)((buf[i_byte+1] << 8) | buf[i_byte+0]);
		}
		
		return newbuf;
	}
	
	bool KinectInterface.pollBackgroundRemoval()
	{
		if (!updatedBackgroundRemoval)
		{
			updatedBackgroundRemoval = true;

            m_need_pollBackgroundRemoval = true;
		}

		return true;
	}
	
	Color32[] KinectInterface.getBackgroundRemoval(){
		return BackgroundRemovalImage;
	}
	
	Color32[] KinectInterface.getBackgroundRemovalTexture() {
        if (newBackgroundRemoval)
        {
            newBackgroundRemoval = false;
            return BackgroundRemovalImageTexture;
        }

        return null;
	}
	
	void OnApplicationQuit()
	{
        m_pollThread_run = false;

		NativeMethods.qfKinectUnInit();
	}

    private bool m_pollThread_run = true;

    private bool m_need_pollColor = false;
    private bool m_need_pollDepth = false;
    private bool m_need_pollBackgroundRemoval = false;
    public void pollThread()
    {
        while (m_pollThread_run)
        {
            if (m_need_pollColor)
            {
                byte[] data = m_color_data;
                if (null != data)
                {
                    int hr = NativeMethods.qfKinectCopyVideoData(data);
                    if (hr == 0)
                    {
                        qfOpenCV.bgraToColor32(m_color_data, video_width, video_height, 4, colorImageTexture);

                        m_need_pollColor = false;
                        newColor = true;
                    }
                }

            }

            if (m_need_pollDepth)
            {
                byte[] depth_data = m_depth_data;
                if (null != depth_data)
                {
                    int hr = NativeMethods.qfKinectCopyDepthData(depth_data);
                    if (hr == 0)
                    {
                        depthPlayerData = extractDepthImage(depth_data);

                        m_need_pollDepth = false;
                        newDepth = true;
                    }
                }
            }

            if (m_need_pollBackgroundRemoval)
            {
                byte[] data = m_BackgroundRemoval_data;
                if (null != data)
                {
                    int hr = NativeMethods.qfKinectCopyBackgroundRemovalData(data);
                    if (hr == 0)
                    {
                        qfOpenCV.bgraToColor32(data, video_width, video_height, 4, BackgroundRemovalImageTexture);

                        m_need_pollBackgroundRemoval = false;
                        newBackgroundRemoval = true;
                    }
                }
            }
        }

        Debug.Log("pollThread EXIT SUCC");
    }
}

