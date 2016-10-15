using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Kinect {
	
	public interface KinectInterface {
		
		float getSensorHeight();
		Vector3 getKinectCenter();
		Vector4 getLookAt();
		
		bool pollSkeleton();
		NuiSkeletonFrame getSkeleton();
		bool getIsLeftHandGrip(int i);
		bool getIsRightHandGrip(int i);
		
		Vector4[] getSkeleton_defaultUser();
		bool getIsLeftHandGrip_defaultUser();
		bool getIsRightHandGrip_defaultUser();

		bool pollBackgroundRemoval();
		Color32[] getBackgroundRemoval();
		Color32[] getBackgroundRemovalTexture();
		
		bool pollColor();
		Color32[] getColor();
		Color32[] getColorTexture();
		
		bool pollDepth();
		short[] getDepth();
		
	}
	
	public static class Constants
	{
		public static int NuiSkeletonCount = 6;
    	public static int NuiSkeletonMaxTracked = 2;
    	public static int NuiSkeletonInvalidTrackingID = 0;
		
		public static float NuiDepthHorizontalFOV = 58.5f;
		public static float NuiDepthVerticalFOV = 45.6f;
	}
	
	/// <summary>
	///Structs and constants for interfacing c# with the c++ kinect dll 
	/// </summary>

    public enum NuiSkeletonPositionIndex : int
    {
        HipCenter = 0,
        Spine,
        ShoulderCenter,
        Head,
        ShoulderLeft,
        ElbowLeft,
        WristLeft,
        HandLeft,
        ShoulderRight,
        ElbowRight,
        WristRight,
        HandRight,
        HipLeft,
        KneeLeft,
        AnkleLeft,
        FootLeft,
        HipRight,
        KneeRight,
        AnkleRight,
        FootRight,
        Count
    }

    public enum NuiSkeletonPositionTrackingState
    {
        NotTracked = 0,
        Inferred,
        Tracked
    }

    public enum NuiSkeletonTrackingState
    {
        NotTracked = 0,
        PositionOnly,
        SkeletonTracked
    }
	
	public enum NuiImageType
	{
		DepthAndPlayerIndex = 0,	// USHORT
		Color,						// RGB32 data
		ColorYUV,					// YUY2 stream from camera h/w, but converted to RGB32 before user getting it.
		ColorRawYUV,				// YUY2 stream from camera h/w.
		Depth						// USHORT
	}
	
	public enum NuiImageResolution
	{
		resolutionInvalid = -1,
		resolution80x60 = 0,
		resolution320x240,
		resolution640x480,
		resolution1280x1024                        // for hires color only
	}

    /*[StructLayoutAttribute(LayoutKind.Sequential)]
    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }*/

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NuiSkeletonData
    {
        public NuiSkeletonTrackingState eTrackingState;
        public uint dwTrackingID;
        public uint dwEnrollmentIndex_NotUsed;
        public uint dwUserIndex;
        public Vector4 Position;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20, ArraySubType = UnmanagedType.Struct)]
        public Vector4[] SkeletonPositions;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20, ArraySubType = UnmanagedType.Struct)]
        public NuiSkeletonPositionTrackingState[] eSkeletonPositionTrackingState;
        public uint dwQualityFlags;
    }
	
    public struct NuiSkeletonFrame
    {
        public Int64 liTimeStamp;
        public uint dwFrameNumber;
        public uint dwFlags;
        public Vector4 vFloorClipPlane;
        public Vector4 vNormalToGravity;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.Struct)]
        public NuiSkeletonData[] SkeletonData;
    }
	
	public struct NuiTransformSmoothParameters
	{
		public float fSmoothing;
		public float fCorrection;
		public float fPrediction;
		public float fJitterRadius;
		public float fMaxDeviationRadius;
	}
	
	[Serializable]
	public struct SerialVec4 {
		float x,y,z,w;
		
		public SerialVec4(Vector4 vec){
			this.x = vec.x;
			this.y = vec.y;
			this.z = vec.z;
			this.w = vec.w;
		}
		
		public Vector4 deserialize() {
			return new Vector4(x,y,z,w);
		}
	}
	
	[Serializable]
	public struct SerialSkeletonData {
		public NuiSkeletonTrackingState eTrackingState;
        public uint dwTrackingID;
        public uint dwEnrollmentIndex_NotUsed;
        public uint dwUserIndex;
        public SerialVec4 Position;
        public SerialVec4[] SkeletonPositions;
        public NuiSkeletonPositionTrackingState[] eSkeletonPositionTrackingState;
        public uint dwQualityFlags;
		
		public SerialSkeletonData (NuiSkeletonData nui) {
			this.eTrackingState = nui.eTrackingState;
	        this.dwTrackingID = nui.dwTrackingID;
	        this.dwEnrollmentIndex_NotUsed = nui.dwEnrollmentIndex_NotUsed;
	        this.dwUserIndex = nui.dwUserIndex;
	        this.Position = new SerialVec4(nui.Position);
	        this.SkeletonPositions = new SerialVec4[20];
			for(int ii = 0; ii < 20; ii++){
				this.SkeletonPositions[ii] = new SerialVec4(nui.SkeletonPositions[ii]);
			}
	        this.eSkeletonPositionTrackingState = nui.eSkeletonPositionTrackingState;
	        this.dwQualityFlags = nui.dwQualityFlags;
		}
		
		public NuiSkeletonData deserialize() {
			NuiSkeletonData nui = new NuiSkeletonData();
			nui.eTrackingState = this.eTrackingState;
	        nui.dwTrackingID = this.dwTrackingID;
	        nui.dwEnrollmentIndex_NotUsed = this.dwEnrollmentIndex_NotUsed;
	        nui.dwUserIndex = this.dwUserIndex;
	        nui.Position = this.Position.deserialize();
	        nui.SkeletonPositions = new Vector4[20];
			for(int ii = 0; ii < 20; ii++){
				nui.SkeletonPositions[ii] = this.SkeletonPositions[ii].deserialize();
			}
	        nui.eSkeletonPositionTrackingState = this.eSkeletonPositionTrackingState;
	        nui.dwQualityFlags = this.dwQualityFlags;
			return nui;
		}
	}
	
	[Serializable]
	public struct SerialSkeletonFrame
	{
		public Int64 liTimeStamp;
        public uint dwFrameNumber;
        public uint dwFlags;
        public SerialVec4 vFloorClipPlane;
        public SerialVec4 vNormalToGravity;
        public SerialSkeletonData[] SkeletonData;
		
		public SerialSkeletonFrame (NuiSkeletonFrame nui) {
			this.liTimeStamp = nui.liTimeStamp;
			this.dwFrameNumber = nui.dwFrameNumber;
			this.dwFlags = nui.dwFlags;
			this.vFloorClipPlane = new SerialVec4(nui.vFloorClipPlane);
			this.vNormalToGravity = new SerialVec4(nui.vNormalToGravity);
			this.SkeletonData = new SerialSkeletonData[6];
			for(int ii = 0; ii < 6; ii++){
				this.SkeletonData[ii] = new SerialSkeletonData(nui.SkeletonData[ii]);
			}
		}
		
		public NuiSkeletonFrame deserialize() {
			NuiSkeletonFrame nui = new NuiSkeletonFrame();
			nui.liTimeStamp = this.liTimeStamp;
			nui.dwFrameNumber = this.dwFrameNumber;
			nui.dwFlags = this.dwFlags;
			nui.vFloorClipPlane = this.vFloorClipPlane.deserialize();
			nui.vNormalToGravity = this.vNormalToGravity.deserialize();
			nui.SkeletonData = new NuiSkeletonData[6];
			for(int ii = 0; ii < 6; ii++){
				nui.SkeletonData[ii] = this.SkeletonData[ii].deserialize();
			}
			return nui;
		}
	}
	
	public struct NuiImageViewArea
	{
	    int eDigitalZoom_NotUsed;
	    long lCenterX_NotUsed;
	    long lCenterY_NotUsed;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public class NuiImageBuffer
	{
		public int m_Width;
		public int m_Height;
		public int m_BytesPerPixel;
		public IntPtr m_pBuffer;
	}
	
	[StructLayoutAttribute(LayoutKind.Sequential)]
	public struct NuiImageFrame
	{
		public Int64 liTimeStamp;
		public uint dwFrameNumber;
		public NuiImageType eImageType;
		public NuiImageResolution eResolution;
		//[MarshalAsAttribute(UnmanagedType.LPStruct)]
		public IntPtr pFrameTexture;
		public uint dwFrameFlags_NotUsed;
		public NuiImageViewArea ViewArea_NotUsed;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct ColorCust
	{
		public byte b;
		public byte g;
		public byte r;
		public byte a;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct ColorBuffer
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 640 * 480, ArraySubType = UnmanagedType.Struct)]
		public ColorCust[] pixels;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct DepthBuffer
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 320 * 240, ArraySubType = UnmanagedType.I2)]
		public short[] pixels;
	}
	
	public class NativeMethods
	{
		/*
		 * kinect switch functions
		 */
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetEnableMultiUser")]
		public static extern bool qfKinectGetEnableMultiUser();

		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectSetEnableMultiUser")]
		public static extern void qfKinectSetEnableMultiUser(bool bEnable);

		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetEnableFaceTracking")]
		public static extern bool qfKinectGetEnableFaceTracking();

		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectSetEnableFaceTracking")]
		public static extern void qfKinectSetEnableFaceTracking(bool bEnable);

		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetEnableSpeechRecognition")]
		public static extern bool qfKinectGetEnableSpeechRecognition();

		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectSetEnableSpeechRecognition")]
		public static extern void qfKinectSetEnableSpeechRecognition(bool bEnable);

		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetEnableKinectInteractive")]
		public static extern bool qfKinectGetEnableKinectInteractive();

		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectSetEnableKinectInteractive")]
		public static extern void qfKinectSetEnableKinectInteractive(bool bEnable);

		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetEnableBackgroundRemoval")]
		public static extern bool qfKinectGetEnableBackgroundRemoval();

		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectSetEnableBackgroundRemoval")]
		public static extern void qfKinectSetEnableBackgroundRemoval(bool bEnable);
		


		/* 
		 * kinect NUI (general) functions
		 */
				
	    [DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectInit")]
	    public static extern int qfKinectInit();
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectUnInit")]
	    public static extern int qfKinectUnInit();
		
		/* 
		 * kinect image functions
		 */
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopyVideoData")]
		public static extern int qfKinectCopyVideoData(byte[/*640*480*4*/] data);
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopyDepthData")]
		public static extern int qfKinectCopyDepthData(byte[/*320*240*4*/] data);
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopyBackgroundRemovalData")]
		public static extern int qfKinectCopyBackgroundRemovalData(byte[/*640*480*4*/] data);
		
		/*
		 * kinect skeleton functions
		 */
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopySkeletonData")]
		public static extern int qfKinectCopySkeletonData(float[/*20*4*/] data);
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopyMultiSkeletonData")]
		public static extern int qfKinectCopyMultiSkeletonData(float[/*6*/, /*20*4*/] data, int[] userID);

		/*
		 * kinect size functions
		 */
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetDepthWidth")]
		public static extern int qfKinectGetDepthWidth();
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetDepthHeight")]
		public static extern int qfKinectGetDepthHeight();
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetVideoWidth")]
		public static extern int qfKinectGetVideoWidth();
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectGetVideoHeight")]
		public static extern int qfKinectGetVideoHeight();

		/*
		 * kinect face tracking functions
		 */
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopyFaceTrackResult")]
		public static extern int qfKinectCopyFaceTrackResult(float[/*1*/] scale
		                                                     , float[/*3*/] rotationXYZ, float[/*3*/]translationXYZ
		                                                     , long[/*2*/] translationColorXY);


		/*
		 * kinect speech functions
		 */
		
	    [DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectInitSpeech")]
	    public static extern int qfKinectInitSpeech(int languageCode);

		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectUnInitSpeech")]
	    public static extern int qfKinectUnInitSpeech();
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopySpeechReslut")]
		public static extern int qfKinectCopySpeechReslut(byte[/*1024*/] strResult);
		
		/*
		 * kinect interaction functions
		 */
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopyHandEventReslut")]
		public static extern int qfKinectCopyHandEventReslut(byte[/*2*/] handEvent);
		
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectCopyMultiHandEventReslut")]
		public static extern int qfKinectCopyMultiHandEventReslut(byte[/*6*/, /*2*/] handEvent);

		/*
		 * kinect transform functions
		 */
		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectTransformDepthImageToSkeleton")]
		public static extern int qfKinectTransformDepthImageToSkeleton(float[/*4*/] positionXYZW
                                                     , int lDepthX, int lDepthY, short usDepthValue);

		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectTransformSkeletonToDepthImage")]
		public static extern int qfKinectTransformSkeletonToDepthImage(float[/*4*/] positionXYZW
                                                     , int[/*1*/] plDepthX, int[/*1*/] plDepthY, short[/*1*/] pusDepthValue);

		[DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "qfKinectTransformDepthImageToVideoImage")]
        public static extern int qfKinectTransformDepthImageToVideoImage(int lDepthX, int lDepthY, short usDepthValue
                                                     , int[/*1*/] plColorX, int[/*1*/] plColorY);

        /*
         * µ÷ÕûÑö¸©½Ç¶È
         */
        [DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "getElevationAngle")]
        public static extern int getElevationAngle();

        [DllImportAttribute(@"YuYuYouEr_Kinect_SDK_Wrapper.DLL", EntryPoint = "setElevationAngle")]
        public static extern bool setElevationAngle(int angle);

	}
	
}
