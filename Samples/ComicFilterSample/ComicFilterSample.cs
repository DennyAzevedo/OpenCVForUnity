﻿using UnityEngine;
using System.Collections;

using OpenCVForUnity;

/// <summary>
/// ComicFilter sample.
/// referring to the http://dev.classmethod.jp/smartphone/opencv-manga-2/.
/// </summary>
public class ComicFilterSample : MonoBehaviour
{

		/// <summary>
		/// The web cam texture.
		/// </summary>
		WebCamTexture webCamTexture;

		/// <summary>
		/// The web cam device.
		/// </summary>
		WebCamDevice webCamDevice;

		/// <summary>
		/// The colors.
		/// </summary>
		Color32[] colors;

		/// <summary>
		/// Should use front facing.
		/// </summary>
		public bool shouldUseFrontFacing = false;

		/// <summary>
		/// The width.
		/// </summary>
		int width = 640;

		/// <summary>
		/// The height.
		/// </summary>
		int height = 480;

		/// <summary>
		/// The rgba mat.
		/// </summary>
		Mat rgbaMat;

		/// <summary>
		/// The gray mat.
		/// </summary>
		Mat grayMat;

		/// <summary>
		/// The line mat.
		/// </summary>
		Mat lineMat;

		/// <summary>
		/// The mask mat.
		/// </summary>
		Mat maskMat;

		/// <summary>
		/// The background mat.
		/// </summary>
		Mat bgMat;

		/// <summary>
		/// The dst mat.
		/// </summary>
		Mat dstMat;

		/// <summary>
		/// The gray pixels.
		/// </summary>
		byte[] grayPixels;

		/// <summary>
		/// The mask pixels.
		/// </summary>
		byte[] maskPixels;

		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// The init done.
		/// </summary>
		bool initDone = false;

		/// <summary>
		/// The screenOrientation.
		/// </summary>
		ScreenOrientation screenOrientation = ScreenOrientation.Unknown;
	
		// Use this for initialization
		void Start ()
		{

				StartCoroutine (init ());

		}

		private IEnumerator init ()
		{

				if (webCamTexture != null) {
						webCamTexture.Stop ();
						initDone = false;

						rgbaMat.Dispose ();
						grayMat.Dispose ();
						lineMat.Dispose ();
						maskMat.Dispose ();

						bgMat.Dispose ();
				}
		
				// Checks how many and which cameras are available on the device
				for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
			

						if (WebCamTexture.devices [cameraIndex].isFrontFacing == shouldUseFrontFacing) {

				
								Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
								
								webCamDevice = WebCamTexture.devices [cameraIndex];

								webCamTexture = new WebCamTexture (webCamDevice.name, width, height);
	
								break;
						}

			
				}
		
				if (webCamTexture == null) {
						webCamDevice = WebCamTexture.devices [0];
						webCamTexture = new WebCamTexture (webCamDevice.name, width, height);
				}

				Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);

		
				// Starts the camera
				webCamTexture.Play ();
		
		
				

				while (true) {

						//If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
						#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
			if (webCamTexture.width > 16 && webCamTexture.height > 16) {
						#else
						if (webCamTexture.didUpdateThisFrame) {
								#if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2                                    
					while (webCamTexture.width <= 16) {
						webCamTexture.GetPixels32 ();
						yield return new WaitForEndOfFrame ();
					} 
								#endif
								#endif

								Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
								Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);


								colors = new Color32[webCamTexture.width * webCamTexture.height];
				
								rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
								grayMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
								lineMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
								maskMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);

								//create a striped background.
								bgMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1, new Scalar (255));
								for (int i = 0; i < bgMat.rows ()*2.5f; i=i+4) {
										Core.line (bgMat, new Point (0, 0 + i), new Point (bgMat.cols (), -bgMat.cols () + i), new Scalar (0), 1);
								}
				
								dstMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
				
								grayPixels = new byte[grayMat.cols () * grayMat.rows () * grayMat.channels ()];
								maskPixels = new byte[maskMat.cols () * maskMat.rows () * maskMat.channels ()];
				
								texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

								gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

								updateLayout ();

								screenOrientation = Screen.orientation;
								initDone = true;

								break;
						} else {
								yield return 0;
						}
				}
		}

		private void updateLayout ()
		{
				gameObject.transform.localRotation = new Quaternion (0, 0, 0, 0);
				gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);

				if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270) {
						gameObject.transform.eulerAngles = new Vector3 (0, 0, -90);
				}


				float width = 0;
				float height = 0;
				if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270) {
						width = gameObject.transform.localScale.y;
						height = gameObject.transform.localScale.x;
				} else if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 180) {
						width = gameObject.transform.localScale.x;
						height = gameObject.transform.localScale.y;
				}

				float widthScale = (float)Screen.width / width;
				float heightScale = (float)Screen.height / height;
				if (widthScale < heightScale) {
						Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
				} else {
						Camera.main.orthographicSize = height / 2;
				}
		}

		// Update is called once per frame
		void Update ()
		{

				if (!initDone)
						return;

				if (screenOrientation != Screen.orientation) {
						screenOrientation = Screen.orientation;
						updateLayout ();
				}

				#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
			if (webCamTexture.width > 16 && webCamTexture.height > 16) {
				#else
				if (webCamTexture.didUpdateThisFrame) {
						#endif
		
						Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);

						//flip to correct direction.
						if (webCamDevice.isFrontFacing) {
								if (webCamTexture.videoRotationAngle == 0) {
										Core.flip (rgbaMat, rgbaMat, 1);
								} else if (webCamTexture.videoRotationAngle == 90) {
										Core.flip (rgbaMat, rgbaMat, 0);
								}
								if (webCamTexture.videoRotationAngle == 180) {
										Core.flip (rgbaMat, rgbaMat, 0);
								} else if (webCamTexture.videoRotationAngle == 270) {
										Core.flip (rgbaMat, rgbaMat, 1);
								}
						} else {
								if (webCamTexture.videoRotationAngle == 180) {
										Core.flip (rgbaMat, rgbaMat, -1);
								} else if (webCamTexture.videoRotationAngle == 270) {
										Core.flip (rgbaMat, rgbaMat, -1);
								}
						}

						Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

//						Utils.webCamTextureToMat (webCamTexture, grayMat, colors);

				
						bgMat.copyTo (dstMat);


						Imgproc.GaussianBlur (grayMat, lineMat, new Size (3, 3), 0);
				



						grayMat.get (0, 0, grayPixels);

						for (int i = 0; i < grayPixels.Length; i++) {

								maskPixels [i] = 0;
			
								if (grayPixels [i] < 70) {
										grayPixels [i] = 0;

										maskPixels [i] = 1;
								} else if (70 <= grayPixels [i] && grayPixels [i] < 120) {
										grayPixels [i] = 100;

								
								} else {
										grayPixels [i] = 255;

										maskPixels [i] = 1;
								}
						}
		
						grayMat.put (0, 0, grayPixels);
	
						maskMat.put (0, 0, maskPixels);

						grayMat.copyTo (dstMat, maskMat);




				
						Imgproc.Canny (lineMat, lineMat, 20, 120);
		
						lineMat.copyTo (maskMat);
		
						Core.bitwise_not (lineMat, lineMat);

						lineMat.copyTo (dstMat, maskMat);


//		Imgproc.cvtColor(dstMat,rgbaMat,Imgproc.COLOR_GRAY2RGBA);
//				Utils.matToTexture2D (rgbaMat, texture);

						Utils.matToTexture2D (dstMat, texture, colors);
		
				

				}
		}
	
		void OnDisable ()
		{
				webCamTexture.Stop ();
		}
	
		void OnGUI ()
		{
				float screenScale = Screen.height / 240.0f;
				Matrix4x4 scaledMatrix = Matrix4x4.Scale (new Vector3 (screenScale, screenScale, screenScale));
				GUI.matrix = scaledMatrix;
		
		
				GUILayout.BeginVertical ();
				if (GUILayout.Button ("back")) {
						Application.LoadLevel ("OpenCVForUnitySample");
				}
				if (GUILayout.Button ("change camera")) {
						shouldUseFrontFacing = !shouldUseFrontFacing;
						StartCoroutine (init ());
				}
		
		
				GUILayout.EndVertical ();
		}
}
