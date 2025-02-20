using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using RenderHeads.Media.AVProMovieCapture;
using System.Text.RegularExpressions;

public class VideoCaptureController : MonoBehaviour
{
    public CaptureBase captureBase;
    public CameraSelector cameraSelector;
    public VideoTarget videoTarget;
    public float maxRecordingDuration = 60;
    public float recordingDuration = 0;

    public bool useCustomHeightKeepAspectRatio = false;
    public bool useCustomResolution = false;

    public float aspectRatio = 0.75f;
    public int width = 720;
    public int height = 1280;

    [Space(10)]

    public float activeVideobuttonScale = 1.5f;
    public GameObject videoButtonRoot;
    public GameObject videoButtonCircle;
    public GameObject videoButtonFill;
    public GameObject previewUI;
    public GameObject savedInfo;

    [Space(10)]

    private bool isLoading = false;
    private bool isSaving = false;
    private bool errorOnSave = false;
    private bool isProcessingVideo = false;
    private string videoPath = "";
	private Device audioInputDevice;

    public static VideoCaptureController instance;
    void Awake()
    {
        instance = this; 
    }

    void Start()
    {
        Init();
    }

    private void Update()
    {
        if (captureBase.IsCapturing())
        {
            recordingDuration += Time.deltaTime;
            videoButtonFill.GetComponent<Image>().fillAmount = recordingDuration / maxRecordingDuration;

            if (recordingDuration >= maxRecordingDuration)
            {
                StartStopCapture();
                videoButtonFill.GetComponent<Image>().fillAmount = 1;
            }
        }
    }

    public void Init()
    {
        cameraSelector.Camera = ARController.instance.mainCamera.GetComponent<Camera>();
        captureBase.CompletedFileWritingAction += OnCompleteFinalFileWriting;
        captureBase.FrameRate = 60;
        captureBase.CameraRenderResolution = CaptureBase.Resolution.Custom;

        if (useCustomHeightKeepAspectRatio)
        {
            aspectRatio = (float)Screen.width / (float)Screen.height;
            float h = height;
            float w = aspectRatio * h;
            captureBase.CameraRenderCustomResolution = new Vector2(w, h);
        }
        else if (useCustomResolution)
        {
            aspectRatio = (float)width / (float)height;
            captureBase.CameraRenderCustomResolution = new Vector2(width, height);
        }
        else
        {
            aspectRatio = (float)Screen.width / (float)Screen.height;
            captureBase.CameraRenderCustomResolution = new Vector2(Screen.width, Screen.height);
        }
    }

    public void StartStopCapture()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(StartStopCaptureCoroutine());
    }

    public IEnumerator StartStopCaptureCoroutine()
    {
        yield return null;

        if (captureBase.IsCapturing())
        {
            InfoController.instance.loadingCircle.SetActive(true);
            yield return new WaitForSeconds(0.25f);

            isProcessingVideo = true;
            captureBase.StopCapture();

            float timer = 120;
            while(isProcessingVideo && timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
            InfoController.instance.loadingCircle.SetActive(false);

            yield return new WaitForSeconds(0.5f);

            videoPath = captureBase.LastFilePath;
            videoTarget.videoURL = captureBase.LastFilePath;
            previewUI.SetActive(true);

            VideoController.instance.AdjustVideoRatio(aspectRatio);
            VideoController.instance.currentVideoTarget = videoTarget;
            VideoController.instance.currentVideoTarget.isLoaded = false;
            VideoController.instance.PlayVideo();
            ResetButtons();
        }
        else
        {
            bool useMic = false;

            if (!useMic)
            {
                captureBase.AudioCaptureSource = AudioCaptureSource.Unity;
                captureBase.UnityAudioCapture = cameraSelector.Camera.GetComponent<CaptureAudioFromAudioListener>();
            }
            else {

                bool isSuccess = false;
                yield return StartCoroutine(
                    PermissionController.instance.ValidatePermissionsMicrophoneCoroutine((bool success) =>
                    {
                        isSuccess = success;
                    })
                );


                if (isSuccess)
                {
                    print("Capturing with mic");
                    captureBase.AudioCaptureSource = AudioCaptureSource.Microphone;
                    if (audioInputDevice == null) { audioInputDevice = captureBase.SelectAudioInputDevice(); }

                    if (audioInputDevice == null)
                    {
                        print("No Mic found");
                    }
                    else
                    {
                        print("Mic: " + audioInputDevice.Name);
                    }
                }
                else
                {
                    captureBase.AudioCaptureSource = AudioCaptureSource.None;
                }
            }

            recordingDuration = 0;
            videoButtonFill.GetComponent<Image>().fillAmount = 0;
	        yield return StartCoroutine(AnimateButtonsCoroutine());
            
	        captureBase.StartCapture();

            
            /*
            if ( SiteController.instance.currentSite.siteID == "GuideVideoSite"){

                //yield return null;
                //ARGuideController.instance.videoPlayer.Pause();
                //ARGuideController.instance.videoPlayer.Play();

                if ( SpeechController.instance.speakWasStarted ){
		        	
		        	SpeechController.instance.speakWasStarted = false;
			        ARGuideController.instance.videoPlayer.Pause();
			        ARGuideController.instance.videoPlayer.Play();
		        }            
	        }
	        */

	        yield return new WaitForSeconds(1.0f);

        }

        isLoading = false;
    }

    public IEnumerator AnimateButtonsCoroutine()
    {
        MediaCaptureController.instance.switchCameraButton.SetActive(false);

        float targetScalePhotoButton = captureBase.IsCapturing() ? 1.0f : 0.75f;
        StartCoroutine(AnimationController.instance.AnimateScaleCoroutine(MediaCaptureController.instance.photoButton.transform, MediaCaptureController.instance.photoButton.transform.localScale, Vector3.one * targetScalePhotoButton, 0.3f, "smooth"));

        float targetScaleVideoButton = captureBase.IsCapturing() ? 1.0f : activeVideobuttonScale;
        float targetScaleVideoButtonCircle = captureBase.IsCapturing() ? 1.0f : activeVideobuttonScale;
        float targetScaleVideoButtonFill = captureBase.IsCapturing() ? 1.0f : 1.25f;

        StartCoroutine(AnimationController.instance.AnimateScaleCoroutine(videoButtonRoot.transform, videoButtonRoot.transform.localScale, Vector3.one * targetScaleVideoButton, 0.3f, "smooth"));
        StartCoroutine(AnimationController.instance.AnimateScaleCoroutine(videoButtonCircle.transform, videoButtonCircle.transform.localScale, Vector3.one * targetScaleVideoButtonCircle, 0.3f, "smooth"));
        yield return StartCoroutine(AnimationController.instance.AnimateScaleCoroutine(videoButtonFill.transform, videoButtonFill.transform.localScale, Vector3.one * targetScaleVideoButtonFill, 0.3f, "smooth"));
    }

    public void ResetButtons()
    {
        MediaCaptureController.instance.switchCameraButton.SetActive(true);
        MediaCaptureController.instance.photoButton.transform.localScale = Vector3.one;

        float targetScalePhotoButton = captureBase.IsCapturing() ? 1.0f : 0.75f;
        videoButtonRoot.transform.localScale = Vector3.one;
        videoButtonCircle.transform.localScale = Vector3.one;
        videoButtonFill.transform.localScale = Vector3.one;
        videoButtonFill.GetComponent<Image>().fillAmount = 0;
    }

    private void OnCompleteFinalFileWriting(FileWritingHandler handler)
    {
        Debug.Log("Completed capture '" + handler.Path + "' with status: " + handler.Status.ToString());
        isProcessingVideo = false;
    }

    public void SaveVideo()
    {
        if (isSaving) return;
        isSaving = true;
        StartCoroutine(SaveVideoCoroutine());
    }

    public IEnumerator SaveVideoCoroutine()
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
					"Der Zugriff auf die Galerie ist erforderlich, um das Video zu speichern");
					
				isSaving = false;
				yield break;
			}
		}
#endif

        yield return new WaitForEndOfFrame();

        errorOnSave = false;

        NativeGallery.SaveVideoToGallery(videoPath, "Osnabr�ck", System.IO.Path.GetFileName(videoPath), MediaSaveCallback);

        float timer = 10;
        while (isSaving && timer > 0)
        {
            timer -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        if (timer < 0 || errorOnSave)
        {
            InfoController.instance.ShowMessage("Das Video konnte nicht gespeichert werden.");
        }
        else
        {
            savedInfo.SetActive(true);
            yield return new WaitForSeconds(3.0f);
            savedInfo.SetActive(false);
        }

        isSaving = false;
    }

    private void MediaSaveCallback(bool success, string path)
    {
        if (success){ errorOnSave = false; }
        else{ errorOnSave = true; }
        isSaving = false;
    }

    public void ShareVideo()
    {
        string subject = "";
        new NativeShare()
            .SetSubject(subject)
            .AddFile(videoPath)
            .SetCallback((result, shareTarget) => OnShareResult(result, shareTarget))
            .Share();
    }

    public void OnShareResult(NativeShare.ShareResult result, string shareTarget)
    {
        Debug.Log("Share result: " + result + ", selected app: " + shareTarget);
        if (result == NativeShare.ShareResult.Shared){ }
    }

    public void AbortSaveVideo()
    {
        Reset();
    }

    public void Reset()
    {
        ResetButtons();
        VideoController.instance.StopVideo();
		VideoController.instance.currentVideoTarget = VideoController.instance.main3DVideoTarget;
        if (captureBase.IsCapturing()) { 
            captureBase.StopCapture(); 
        }
    }
}
