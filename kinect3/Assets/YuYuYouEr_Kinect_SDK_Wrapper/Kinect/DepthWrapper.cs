using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Level of indirection for the depth image,
/// provides:
/// -a frames of depth image (no player information),
/// -an array representing which players are detected,
/// -a segmentation image for each player,
/// -bounds for the segmentation of each player.
/// </summary>
public class DepthWrapper: MonoBehaviour {
	
	public DeviceOrEmulator devOrEmu;
	private Kinect.KinectInterface kinect;
	
	private struct frameData
	{
		public short[] depthImg;
		public bool[] players;
		public bool[,] segmentation;
		public int[,] bounds;
	}
	
	public int storedFrames = 1;
	
	private bool updatedSeqmentation = false;
	private bool newSeqmentation = false;
	
	private Queue frameQueue;
	
	/// <summary>
	/// Depth image for the latest frame
	/// </summary>
	[HideInInspector]
	public short[] depthImg;
	/// <summary>
	/// players[i] true iff i has been detected in the frame
	/// </summary>
	[HideInInspector]
	public bool[] players;
	/// <summary>
	/// Array of segmentation images [player, pixel]
	/// </summary>
	[HideInInspector]
	public bool[,] segmentations;
	/// <summary>
	/// Array of bounding boxes for each player (left, right, top, bottom)
	/// </summary>
	[HideInInspector]
	//right,left,up,down : but the image is fliped horizontally.
	//public int[,] bounds;

	private int depth_width = 640;
	private int depth_height = 480;

	// Use this for initialization
	void Start () {
		kinect = devOrEmu.getKinect();

		depth_width = Kinect.NativeMethods.qfKinectGetDepthWidth();
		depth_height = Kinect.NativeMethods.qfKinectGetDepthHeight();

		//allocate space to store the data of storedFrames frames.
		frameQueue = new Queue(storedFrames);
		for(int ii = 0; ii < storedFrames; ii++){	
			frameData frame = new frameData();
			frame.depthImg = new short[depth_width * depth_height];
			frame.players = new bool[Kinect.Constants.NuiSkeletonCount];
			frame.segmentation = new bool[Kinect.Constants.NuiSkeletonCount, depth_width * depth_height];
			frame.bounds = new int[Kinect.Constants.NuiSkeletonCount,4];
			frameQueue.Enqueue(frame);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void LateUpdate()
	{
		updatedSeqmentation = false;
		newSeqmentation = false;
	}
	/// <summary>
	/// First call per frame checks if there is a new depth image and updates,
	/// returns true if there is new data
	/// Subsequent calls do nothing have the same return as the first call.
	/// </summary>
	/// <returns>
	/// A <see cref="System.Boolean"/>
	/// </returns>
	public bool pollDepth()
	{
		//Debug.Log("" + updatedSeqmentation + " " + newSeqmentation);
		if (!updatedSeqmentation)
		{
			updatedSeqmentation = true;
			if (kinect.pollDepth())
			{
				newSeqmentation = true;
				frameData frame = (frameData)frameQueue.Dequeue();
				depthImg = frame.depthImg;
				players = frame.players;
				segmentations = frame.segmentation;
				//bounds = frame.bounds;
				frameQueue.Enqueue(frame);

				processDepth();
			}
		}
		return newSeqmentation;
	}
	
	private void processDepth()
	{
		depthImg = kinect.getDepth();
	}

}
