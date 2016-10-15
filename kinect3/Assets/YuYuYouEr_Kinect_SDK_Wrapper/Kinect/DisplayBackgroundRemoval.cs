using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class DisplayBackgroundRemoval : MonoBehaviour {
	
	public DeviceOrEmulator devOrEmu;
	private Kinect.KinectInterface kinect;
	
	private Texture2D tex;

	private int video_width = 640;
	private int video_height = 480;

	// Use this for initialization
	void Start () {
		kinect = devOrEmu.getKinect();
        video_width = Kinect.NativeMethods.qfKinectGetVideoWidth();
        video_height = Kinect.NativeMethods.qfKinectGetVideoHeight();

		tex = new Texture2D(video_width, video_height, TextureFormat.ARGB32, false);
		GetComponent<Renderer>().material.mainTexture = tex;
	}
	
	// Update is called once per frame
	void Update () {
		if (kinect.pollBackgroundRemoval())
		{
            Color32[] c = kinect.getBackgroundRemovalTexture();
            if (null != c)
            {
                tex.SetPixels32(c);
                tex.Apply(false);
            }
		}
	}
	
	private Color32[] mipmapImg(Color32[] src, int width, int height)
	{
		int newWidth = width / 2;
		int newHeight = height / 2;
		Color32[] dst = new Color32[newWidth * newHeight];
		for(int yy = 0; yy < newHeight; yy++)
		{
			for(int xx = 0; xx < newWidth; xx++)
			{
				int TLidx = (xx * 2) + yy * 2 * width;
				int TRidx = (xx * 2 + 1) + yy * width * 2;
				int BLidx = (xx * 2) + (yy * 2 + 1) * width;
				int BRidx = (xx * 2 + 1) + (yy * 2 + 1) * width;
				dst[xx + yy * newWidth] = Color32.Lerp(Color32.Lerp(src[BLidx],src[BRidx],.5F),
				                                       Color32.Lerp(src[TLidx],src[TRidx],.5F),.5F);
			}
		}
		return dst;
	}
}
