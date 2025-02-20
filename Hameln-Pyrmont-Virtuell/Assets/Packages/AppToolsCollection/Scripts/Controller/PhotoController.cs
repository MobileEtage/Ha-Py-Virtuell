using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using TMPro;

public class PhotoController : MonoBehaviour
{
	public Camera mainCamera;
	public PhotoHelper photoHelper;
	public AudioSource cameraShotSound;
	public GameObject cameraShotAnim;

	public Texture2D photoTex;
	private Texture2D slicedPhotoTex;
	public string savePath = "";

	private bool isSaving = false;
	private bool errorOnSave = false;

	public static PhotoController Instance;
	void Awake()
	{
		Instance = this;
	}

	public IEnumerator PlayTakePhotoAnimationCoroutine()
	{
		cameraShotSound.Play();
		cameraShotAnim.SetActive(true);
		yield return new WaitForSeconds(0.5f);
		cameraShotAnim.SetActive(false);
	}

	public IEnumerator CapturePhotoCoroutine(Camera myCamera, int marginBottom, int photoHeight, bool captureWithAlpha, Action<Texture2D> Callback)
	{
		yield return new WaitForEndOfFrame();

		int cullingMask = myCamera.cullingMask;
		myCamera.cullingMask =
		(1 << LayerMask.NameToLayer("TransparentFX")) |
		(1 << LayerMask.NameToLayer( "Webcam" )) |
		(1 << LayerMask.NameToLayer( "Avatar" )) |
		(1 << LayerMask.NameToLayer("Default"));

		// Capture fullscreen photo
		RenderTexture rtTmp = new RenderTexture(Screen.width, Screen.height, 24);
		myCamera.targetTexture = rtTmp;
		if (captureWithAlpha) { photoTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false); }
		else { photoTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false); }
		myCamera.Render();
		RenderTexture.active = rtTmp;

		photoTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		photoTex.Apply();
		myCamera.targetTexture = null;
		RenderTexture.active = null;
		Destroy(rtTmp);

		// Cut photo
		if (marginBottom == 0 && photoHeight == Screen.height)
		{
			SavePhoto(photoTex);
			Callback(photoTex);
		}
		else
		{

			//print(marginBottom);
			//print(photoHeight);
			//print(Screen.height);
			Color[] c = photoTex.GetPixels(0, marginBottom, Screen.width, photoHeight);
			slicedPhotoTex = new Texture2D(Screen.width, photoHeight);
			slicedPhotoTex.SetPixels(c);
			slicedPhotoTex.Apply();
			photoTex = slicedPhotoTex;

			SavePhoto(slicedPhotoTex);
			Callback(slicedPhotoTex);
		}

		myCamera.cullingMask = cullingMask;

		yield return StartCoroutine(ToolsController.instance.CleanMemoryCoroutine());
	}

	public IEnumerator CapturePhotoCoroutine(int marginBottom, int photoHeight, Action<Texture2D> Callback)
	{
		yield return new WaitForEndOfFrame();

		int cullingMask = mainCamera.cullingMask;
		mainCamera.cullingMask =
		(1 << LayerMask.NameToLayer("TransparentFX")) |
		(1 << LayerMask.NameToLayer( "Webcam" )) |
		(1 << LayerMask.NameToLayer( "Avatar" )) |
		(1 << LayerMask.NameToLayer("Default"));

		// Capture fullscreen photo
		RenderTexture rtTmp = new RenderTexture(Screen.width, Screen.height, 24);
		mainCamera.targetTexture = rtTmp;
		photoTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
		mainCamera.Render();
		RenderTexture.active = rtTmp;

		photoTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		photoTex.Apply();
		mainCamera.targetTexture = null;
		RenderTexture.active = null;
		Destroy(rtTmp);

		// Cut photo
		if (marginBottom == 0 && photoHeight == Screen.height)
		{

			SavePhoto(photoTex);
			Callback(photoTex);
		}
		else
		{

			//print(marginBottom);
			//print(photoHeight);
			//print(Screen.height);
			Color[] c = photoTex.GetPixels(0, marginBottom, Screen.width, photoHeight);
			slicedPhotoTex = new Texture2D(Screen.width, photoHeight);
			slicedPhotoTex.SetPixels(c);
			slicedPhotoTex.Apply();
			photoTex = slicedPhotoTex;

			SavePhoto(slicedPhotoTex);
			Callback(slicedPhotoTex);
		}

		mainCamera.cullingMask = cullingMask;

		yield return StartCoroutine(ToolsController.instance.CleanMemoryCoroutine());
	}

	public void SavePhoto(Texture2D photo)
	{

		savePath = Application.persistentDataPath + "/" + "Photo_" +
			DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss-fff") + ".png";
		byte[] bytes = photo.EncodeToPNG();
		System.IO.File.WriteAllBytes(savePath, bytes);
	}

	public IEnumerator SavePhotoCoroutine(Action<bool> Callback)
	{
#if !UNITY_EDITOR
		
		if( !PermissionController.instance.HasPermissionSavePhotos() ){
		
			bool isSuccess = true;
			yield return StartCoroutine(
				PermissionController.instance.RequestPhotoLibraryPermissionCoroutine((bool success) => {                			
					isSuccess = success;
				})
			);		
			
			if( !isSuccess ){
				
				InfoController.instance.ShowMessage(
					"Der Zugriff auf die Galerie ist erforderlich, um das Foto zu speichern");
					
				isSaving = false;
				yield break;
			}
		}
#endif

		yield return new WaitForEndOfFrame();

		errorOnSave = false;

		NativeGallery.SaveImageToGallery(
			photoTex, Params.galleryAlbum, System.IO.Path.GetFileName(savePath), MediaSaveCallback);

		float timer = 10;
		while (isSaving && timer > 0)
		{
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		if (timer < 0 || errorOnSave)
		{
			Callback(false);
		}
		else
		{

			Callback(true);
		}
		isSaving = false;
	}

	private void MediaSaveCallback(bool success, string path)
	{

		if (success)
		{
			errorOnSave = false;
		}
		else
		{
			errorOnSave = true;
		}

		isSaving = false;
	}


}
