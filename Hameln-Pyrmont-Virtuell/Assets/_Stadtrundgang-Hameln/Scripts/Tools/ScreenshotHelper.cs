using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScreenshotHelper : MonoBehaviour
{
	[Header("Press \"S\" to take a screenshot")]
	[Header("For transparent screenshots you need to assign\na camera with ClearFlags \"Solid Color\".\nCapture UI in transparent mode does not\nwork out of the box.\n")]

	public Camera myCamera;
	public enum CaptureType { Screen, Camera, CameraWithAlpha }
	public CaptureType captureType;

	[Header("Optional set the Resolution")]
	public int width = -1;
	public int height = -1;

	private void Update()
	{
#if UNITY_EDITOR
		if (Input.GetKeyDown("s"))
		{
			TakeScreenShot();
		}
#endif
	}

	public void TakeScreenShot()
	{

		if (myCamera == null) { if (GetComponent<Camera>() != null) { myCamera = GetComponent<Camera>(); } }
		if (myCamera == null) { myCamera = Camera.main; }
		if (myCamera == null && captureType != CaptureType.Screen) { return; }

		string assetsFolder = Application.dataPath;
		string parentAssetsFolder = assetsFolder.Replace("Assets", "");
		string screenshotsFolder = parentAssetsFolder + "Screenshots";
		if (!Directory.Exists(screenshotsFolder)) { Directory.CreateDirectory(screenshotsFolder); }

		string filename = "Screenshot_" + DateTime.Now.ToString("dd-MM-yyyy-hh-mm-ss") + ".png";
		string savePath = screenshotsFolder + "/" + filename;

		bool isActive = true;
		if (myCamera != null)
		{
			isActive = myCamera.gameObject.activeInHierarchy;
			myCamera.gameObject.SetActive(true);
		}

		StartCoroutine(SaveScreenShotFromCameraCoroutine(myCamera, savePath, 1));

		/*
		if (Application.isPlaying)
        {
			StartCoroutine(SaveScreenShotFromCameraCoroutine(myCamera, savePath, useTransparentBackground, 1));
        }
        else
        {
			SaveScreenShotFromCamera(myCamera, savePath, useTransparentBackground, 1);
		}
		*/

		if (myCamera != null && !isActive) { myCamera.gameObject.SetActive(false); }
	}

	public IEnumerator SaveScreenShotFromCameraCoroutine(Camera cam, string savePath, float scaleFaktor = 1)
	{
		yield return new WaitForEndOfFrame();

		SaveScreenShotFromCamera(myCamera, savePath, 1);

		Debug.Log("Screenshot saved to: " + savePath);
	}


	public void SaveScreenShotFromCamera(Camera cam, string savePath, float scaleFaktor = 1)
	{
		float previewScaleFaktor = scaleFaktor;
		int resWidth = (int)(Screen.width * previewScaleFaktor);
		int resHeight = (int)(Screen.height * previewScaleFaktor);

		if (width > 0) resWidth = width;
		if (height > 0) resHeight = height;

		print("Resolution: " + Screen.width + " " + Screen.height);

		if (captureType == CaptureType.Screen)
		{
			Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
			screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
			screenShot.Apply();

			byte[] bytes = screenShot.EncodeToPNG();
			File.WriteAllBytes(savePath, bytes);
		}
		else if (captureType == CaptureType.Camera)
		{
			RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
			cam.targetTexture = rt;
			Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
			cam.Render();
			RenderTexture.active = rt;

			screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
			cam.targetTexture = null;
			RenderTexture.active = null;
			DestroyImmediate(rt);

			byte[] bytes = screenShot.EncodeToPNG();
			File.WriteAllBytes(savePath, bytes);
		}
		else if (captureType == CaptureType.CameraWithAlpha)
		{
			// Create two cameras
			cam.gameObject.SetActive(true);
			GameObject blackCam = GameObject.Instantiate(cam.gameObject);
			blackCam.GetComponent<Camera>().backgroundColor = Color.black;
			GameObject whiteCam = GameObject.Instantiate(cam.gameObject);
			whiteCam.GetComponent<Camera>().backgroundColor = Color.white;
			//cam.gameObject.SetActive(false);

			// Enable black background camera and render screenshot to a texture2d
			whiteCam.GetComponent<Camera>().enabled = false;
			blackCam.GetComponent<Camera>().enabled = true;

			RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
			blackCam.GetComponent<Camera>().targetTexture = rt;
			Texture2D screenshotWithBlackBackground = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
			blackCam.GetComponent<Camera>().Render();
			RenderTexture.active = rt;

			screenshotWithBlackBackground.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
			blackCam.GetComponent<Camera>().targetTexture = null;
			RenderTexture.active = null; // JC: added to avoid errors

			// Enable white background camera and render screenshot to a texture2d
			blackCam.GetComponent<Camera>().enabled = false;
			whiteCam.GetComponent<Camera>().enabled = true;

			rt = new RenderTexture(resWidth, resHeight, 24);
			whiteCam.GetComponent<Camera>().targetTexture = rt;
			Texture2D screenshotWithWhiteBackground = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
			whiteCam.GetComponent<Camera>().Render();
			RenderTexture.active = rt;

			screenshotWithWhiteBackground.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
			whiteCam.GetComponent<Camera>().targetTexture = null;
			RenderTexture.active = null; // JC: added to avoid errors
			DestroyImmediate(rt);

			// Create final texture, we have two screenshots, one with black background and one with white background
			// the pixels which have a different value will be transparent
			Texture2D textureTransparentBackground = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);

			Color color;
			for (int y = 0; y < resHeight; ++y)
			{
				// each row
				for (int x = 0; x < resWidth; ++x)
				{
					// each column
					float alpha = screenshotWithWhiteBackground.GetPixel(x, y).r - screenshotWithBlackBackground.GetPixel(x, y).r;
					alpha = 1.0f - alpha;
					if (alpha == 0)
					{
						color = Color.clear;
					}
					else
					{
						color = screenshotWithBlackBackground.GetPixel(x, y) / alpha;
					}
					color.a = alpha;
					textureTransparentBackground.SetPixel(x, y, color);
				}
			}

			byte[] bytes = textureTransparentBackground.EncodeToPNG();
			File.WriteAllBytes(savePath, bytes);

			DestroyImmediate(blackCam);
			DestroyImmediate(whiteCam);
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(ScreenshotHelper))]
public class ScreenshotHelperEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		ScreenshotHelper myScript = (ScreenshotHelper)target;
		if (GUILayout.Button("TakeScreenShot"))
		{
			myScript.TakeScreenShot();
		}
	}

}
#endif