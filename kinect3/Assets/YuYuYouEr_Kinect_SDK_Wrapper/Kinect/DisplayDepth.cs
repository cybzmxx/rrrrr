using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class DisplayDepth : MonoBehaviour {
	
	public DepthWrapper dw;
	
	private Texture2D tex = null;
	private Color32[] m_img = null;

	private int depth_width = 320;
	private int depth_height = 240;

	// Use this for initialization
	void Start () {
		depth_width = Kinect.NativeMethods.qfKinectGetDepthWidth();
		depth_height = Kinect.NativeMethods.qfKinectGetDepthHeight();

		tex = new Texture2D(depth_width,  depth_height, TextureFormat.ARGB32, false);
		GetComponent<Renderer>().material.mainTexture = tex;
	}
	
	// Update is called once per frame
	void Update () {
		if (dw.pollDepth())
		{
            if (null != dw.depthImg)
            {
                tex.SetPixels32(convertDepthToColor(dw.depthImg));
                tex.Apply(false);
            }
		}
	}
	
	private Color32[] convertDepthToColor(short[] depthBuf)
	{
		if (null == m_img)
		{
			m_img = new Color32[depthBuf.Length];
		}

		for (int pix = 0; pix < depthBuf.Length; pix++)
		{
			m_img[pix].r = (byte)( (depthBuf[pix] >> 3) / 32);
			m_img[pix].g = m_img[pix].r;
			m_img[pix].b = m_img[pix].r;

			if((depthBuf[pix] & 0x07) != 0)
			{
                m_img[pix].b = (byte)((depthBuf[pix] >> 3) / 32);
                m_img[pix].g = 128;
                m_img[pix].r = (byte)((depthBuf[pix]) / 32);
			} else {
                m_img[pix].r = (byte)((depthBuf[pix] >> 3) / 32);
                m_img[pix].g = m_img[pix].r;
                m_img[pix].b = m_img[pix].r;
            }
		}
		return m_img;
	}
	
	private Color32[] convertPlayersToCutout(bool[,] players)
	{
		int depth_pixel_count = depth_width*depth_height;

		Color32[] img = new Color32[depth_pixel_count];
		for (int pix = 0; pix < depth_pixel_count; pix++)
		{
			if(players[0,pix]|players[1,pix]|players[2,pix]|players[3,pix]|players[4,pix]|players[5,pix])
			{
				img[pix].a = (byte)255;
			} else {
				img[pix].a = (byte)0;
			}
		}
		return img;
	}
}
