using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;
using SimpleJSON;

public class ARGuideController : MonoBehaviour
{
	public enum CameraARType { BackFacingAR, BackFacingScreen, FrontFacing }
	public CameraARType cameraARType = CameraARType.BackFacingAR;
	public CameraARType cameraARTypeBackFacing = CameraARType.BackFacingAR;
	public bool shouldScan = false;
	private bool fixFrontFacingCameraFullscreen = true;

	[Space(10)]

	public VideoPlayer videoPlayer;
	public RawImage videoInAR_SideBySide;
	public RawImage videoInAR_Greenscreen;
	public RawImage videoInARShadow_SideBySide;
	public RawImage videoInARShadow_Greenscreen;
	public RawImage videoOnScreen;
	public GameObject noConnectionUI;
	private bool frameIsReady = false;
	private bool videoIsPrepared = false;
	public bool isConnectedEditorTest = true;

	[Space(10)]

	public Image infoImage;
	public TextMeshProUGUI infoTitle;
	public TextMeshProUGUI infoDescription;
	public GameObject tutorialUI;
	public GameObject arUI;
	public GameObject switchCameraUI;

	[Space(10)]

	public GameObject screenObjectRoot;
	public GameObject screenObject;
	public GameObject arObjectRoot;

	[Space(10)]

	public GameObject mainCamera;
	public GameObject testHelperObjects;

	private Vector3 hitOffset = Vector3.zero;
	private bool placementEnabled = false;
	private bool isMovingARObject = false;
	private bool isLoading = false;
	private bool videoStarted = false;

	private bool placementInfoARShowed = false;
	private bool placementInfoSelfieShowed = false;
	private bool showInfoOnlyOnce = true;

	public static ARGuideController instance;
	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		videoInAR_Greenscreen.gameObject.SetActive(false);
		videoInARShadow_Greenscreen.gameObject.SetActive(false);
		videoInAR_SideBySide.gameObject.SetActive(false);
		videoInARShadow_SideBySide.gameObject.SetActive(false);
		
		videoPlayer.sendFrameReadyEvents = true;
		videoPlayer.errorReceived += OnErrorReceived;

		if (ARController.instance == null)
		{
			testHelperObjects.SetActive(true);
			GameObject toolsController = new GameObject("ToolsController");
			toolsController.AddComponent<ToolsController>();
			placementEnabled = true;

			StartCoroutine(InitCoroutine());
		}
	}

	void LateUpdate()
	{
		if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "GuideVideoSite") return;

		if (videoStarted && arUI.activeInHierarchy)
		{
			if (VideoCaptureController.instance.previewUI.activeInHierarchy || PhotoCaptureController.instance.previewUI.activeInHierarchy)
			{
				if (videoPlayer.isPlaying) { videoPlayer.Pause(); }
			}
			else
			{
				if (!videoPlayer.isPlaying) { videoPlayer.Play(); }
			}
		}

		HandlePlacement();
		
		if(screenObjectRoot.activeInHierarchy){ 
			
			//ARController.instance.mainCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.Depth; 
		}
	}

	public void OnErrorReceived(VideoPlayer source, string message)
	{

		print("VideoPlayer OnErrorReceived " + message);
	}

	IEnumerator PlayVideoCoroutine()
	{
		videoIsPrepared = false;
		videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
		videoPlayer.Prepare();

		// Wait until video is prepared
		float timer = 4;
		while (!videoIsPrepared && timer > 0)
		{
			yield return null;
			timer -= Time.deltaTime;
		}
		videoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;
		//print("prepareCompleted " + timer);

		frameIsReady = false;
		videoPlayer.frameReady += VideoPlayer_frameReady;
		videoPlayer.Play();

		// Wait for the first video frame, then show video image
		timer = 4;
		while (!frameIsReady && timer > 0)
		{
			yield return null;
			timer -= Time.deltaTime;
		}
		videoPlayer.frameReady -= VideoPlayer_frameReady;

		videoOnScreen.texture = videoPlayer.texture;
		videoInAR_SideBySide.texture = videoPlayer.texture;
		videoInAR_Greenscreen.texture = videoPlayer.texture;
		videoInARShadow_SideBySide.texture = videoPlayer.texture;
		videoInARShadow_Greenscreen.texture = videoPlayer.texture;
	}

	private void VideoPlayer_frameReady(UnityEngine.Video.VideoPlayer vp, long frame) { frameIsReady = true; }
	private void VideoPlayer_prepareCompleted(VideoPlayer source) { videoIsPrepared = true; }

	public void HandlePlacement()
	{
		//if (!placementEnabled) return;
		if (cameraARType != CameraARType.BackFacingAR) { MoveObjectOnScreen(); }
		else { MoveARObject(); }
	}

	public void MoveObjectOnScreen()
	{
		if (Input.touchCount >= 2) { isMovingARObject = false; return; }
		if (PhotoCaptureController.instance.photoPreviewImage.gameObject.activeInHierarchy) { return; }

		screenObjectRoot.transform.position = mainCamera.transform.position;
		screenObjectRoot.transform.eulerAngles = mainCamera.transform.eulerAngles;

		if (Input.GetMouseButtonDown(0))
		{
			RaycastHit[] hits;
			Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
			hits = Physics.RaycastAll(ray, 100);

			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].transform.gameObject == screenObject)
				{
					hitOffset = hits[i].point - screenObject.transform.position;
					isMovingARObject = true;
					break;
				}
			}
		}
		else if (isMovingARObject && Input.GetMouseButton(0))
		{
			Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 2.0f);
			Vector3 pos = mainCamera.GetComponent<Camera>().ScreenToWorldPoint(mousePosition);
			screenObject.transform.position = pos - hitOffset;
		}
		else if (Input.GetMouseButtonUp(0))
		{
			isMovingARObject = false;
		}
	}

	public void MoveARObject()
	{
		if (Input.touchCount >= 2) { isMovingARObject = false; return; }

		if (Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject())
		{
			RaycastHit[] hits;
			Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
			hits = Physics.RaycastAll(ray, 100);

			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].transform == arObjectRoot.transform.GetChild(0))
				{
					hitOffset = hits[i].point - arObjectRoot.transform.position;
					isMovingARObject = true;
					break;
				}
			}

			// Start drag object delayed, because maybe we also want to scale it with two fingers
			//StopCoroutine("EnableMoveARObjectCoroutine");
			//StartCoroutine("EnableMoveARObjectCoroutine");
		}
		else if (isMovingARObject && Input.GetMouseButton(0))
		{
			Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
			Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;

			bool hitGround = false;
			if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, out hitPosition))
			{
				hitGround = true;
			}
			else
			{

#if UNITY_EDITOR

				Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
				RaycastHit[] hits;
				hits = Physics.RaycastAll(ray, 100);

				for (int i = 0; i < hits.Length; i++)
				{
					if (hits[i].transform.CompareTag("ARPlane"))
					{
						hitPosition = hits[i].point;
						hitGround = true;
						break;
					}
				}
#endif

				if (!hitGround)
				{
					Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
					hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;
				}
			}

			//arObject.transform.position = hitPosition - hitOffset;
			arObjectRoot.transform.position = hitPosition;
		}
		else if (Input.GetMouseButtonUp(0))
		{
			StopCoroutine("EnableMoveARObjectCoroutine");
			isMovingARObject = false;
		}
	}

	public IEnumerator EnableMoveARObjectCoroutine()
	{
		yield return new WaitForSeconds(0.15f);
		isMovingARObject = true;
	}

	public IEnumerator InitCoroutine()
	{
		yield return null;

		// Start site params
		JSONNode featureData = StationController.instance.GetStationFeature("ar");
		if (featureData != null && featureData["infoTitle"] != null) { infoTitle.text = LanguageController.GetTranslationFromNode(featureData["infoTitle"]); }
		else { infoTitle.text = LanguageController.GetTranslation("Mache ein Foto mit unserem Stadtführer"); }
		if (featureData != null && featureData["infoDescription"] != null) { infoDescription.text = LanguageController.GetTranslationFromNode(featureData["infoDescription"]); }
		else { infoDescription.text = LanguageController.GetTranslation("Hier kannst Du ein Foto mit unserem Stadtführer machen."); }

		// Image
		if (featureData != null && featureData["infoImage"] != null && featureData["infoImage"].Value != "")
		{
			ToolsController.instance.ApplyOnlineImage(infoImage, featureData["infoImage"].Value, true);
		}
		else
		{
			Sprite sprite = Resources.Load<Sprite>("UI/Sprites/selfie");
			infoImage.sprite = sprite;
			infoImage.preserveAspect = true;
		}

		if (ARController.instance != null)
		{
			mainCamera = ARController.instance.mainCamera;
			WebcamController.instance.webcamContent.GetComponentInChildren<Canvas>(true).worldCamera = mainCamera.GetComponent<Camera>();
		}

		videoStarted = false;
		tutorialUI.SetActive(true);
		arUI.SetActive(false);
	}

	public void StartAR()
	{
		if (isLoading) return;
		isLoading = true;
		StartCoroutine("StartARCoroutine");
	}

	public IEnumerator StartARCoroutine()
	{
		if (cameraARType == CameraARType.BackFacingAR)
		{
			bool hasPermission = true;
			yield return StartCoroutine(PermissionController.instance.ValidatePermissionsCameraCoroutine("arFeature", (bool success) => { hasPermission = success; }));
			if (!hasPermission) { isLoading = false; yield break; }
		}

		screenObjectRoot.SetActive(false);
		arObjectRoot.SetActive(false);

		if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
		yield return new WaitForSeconds(0.25f);

		if (cameraARType == CameraARType.BackFacingAR) { yield return StartCoroutine(InitBackFacingARCoroutine()); }
		else if (cameraARType == CameraARType.BackFacingScreen) { yield return StartCoroutine(InitBackFacingScreenCoroutine()); }
		else if (cameraARType == CameraARType.FrontFacing) { yield return StartCoroutine(InitFrontFacingCoroutine()); }

		if (!videoStarted)
		{
			bool isSuccess = false;
            string videoURL = GetVideoURL();     
            if (ToolsController.instance.IsValidURL(videoURL))
            {
                print("ValidateConnectionAndStartPlayVideoCoroutine IsValidURL true");

                yield return StartCoroutine(
                    ServerBackendController.instance.ValidateConnectionCoroutine((bool success, string data) =>
                    {
                        isSuccess = success;
                    })
                );
            }
            else
            {
                print("ValidateConnectionAndStartPlayVideoCoroutine IsValidURL false");
                isSuccess = true;
            }

#if UNITY_EDITOR
            isSuccess = isConnectedEditorTest;
#endif

            if (isSuccess)
            {
                noConnectionUI.SetActive(false);
                yield return StartCoroutine(StartVideoCoroutine());
            }
            else
            {
                noConnectionUI.SetActive(true);
            }

            videoStarted = true;
		}

		if (InfoController.instance != null)
		{
			if (cameraARType == CameraARType.BackFacingAR && !placementInfoARShowed && PlayerPrefs.GetInt("guideInfoARShowed", 0) != 1)
			{
				yield return new WaitForSeconds(0.25f);
				InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_guide_desc, EnablePlacement);
				placementInfoARShowed = true;
				if (showInfoOnlyOnce) { PlayerPrefs.SetInt("guideInfoARShowed", 1); }
			}
			else if (cameraARType == CameraARType.FrontFacing && !placementInfoSelfieShowed && PlayerPrefs.GetInt("guideInfoSelfieShowed", 0) != 1)
			{
				yield return new WaitForSeconds(0.25f);
				InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_scale_guide_desc, EnablePlacement);
				placementInfoSelfieShowed = true;
				if (showInfoOnlyOnce) { PlayerPrefs.SetInt("guideInfoSelfieShowed", 1); }
			}
		}

		isLoading = false;
	}

    public string GetVideoURL()
    {
        JSONNode featureData = StationController.instance.GetStationFeature("ar");
        if (featureData == null) return "";

        if (featureData["videoURL"] != null)
        {
            return DownloadContentController.instance.GetVideoFile(featureData["videoURL"]);
        }
        else if (featureData["videoUrl"] != null)
        {
            return DownloadContentController.instance.GetVideoFile(featureData["videoUrl"]);
        }

        return "";
    }
	
	public IEnumerator StartVideoCoroutine()
	{
		JSONNode featureData = StationController.instance.GetStationFeature("ar");
		if (featureData == null) yield break;

		print("StartVideoCoroutine " + featureData.ToString());

		// Set URL
		if (featureData["localVideo"] != null)
		{
			videoPlayer.source = VideoSource.VideoClip;
			VideoClip videoClip = Resources.Load<VideoClip>(featureData["localVideo"].Value);
			videoPlayer.clip = videoClip;

			videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
			videoPlayer.SetTargetAudioSource(0, GetComponentInChildren<AudioSource>());
		}
		else if (featureData["videoURL"] != null)
		{
			videoPlayer.source = VideoSource.Url;
			videoPlayer.url = DownloadContentController.instance.GetVideoFile(featureData["videoURL"]);

			videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
			videoPlayer.SetTargetAudioSource(0, GetComponentInChildren<AudioSource>());
		}
		else if (featureData["videoUrl"] != null)
		{
			videoPlayer.source = VideoSource.Url;
			videoPlayer.url = DownloadContentController.instance.GetVideoFile(featureData["videoUrl"]);

			videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
			videoPlayer.SetTargetAudioSource(0, GetComponentInChildren<AudioSource>());
		}

		// Enable options and show video
		videoInAR_SideBySide.uvRect = new Rect(0, 0, 1, 1);
		videoInARShadow_SideBySide.uvRect = new Rect(0, 0, 1, 1);
		videoOnScreen.uvRect = new Rect(0, 0, 1, 1);
		videoOnScreen.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
		videoOnScreen.transform.localScale = Vector3.one * 1.8f;

		bool useAlphaShader = true;
		videoInARShadow_SideBySide.gameObject.SetActive(false);
		videoInARShadow_Greenscreen.gameObject.SetActive(false);
		videoInAR_Greenscreen.gameObject.SetActive(false);
		videoInAR_SideBySide.gameObject.SetActive(false);

		if (featureData["useGreenscreenShader"] != null && featureData["useGreenscreenShader"].AsBool)
		{
			videoInAR_Greenscreen.material = VideoController.instance.greenscreenMaterial;
			videoOnScreen.material = VideoController.instance.greenscreenMaterial;

			videoOnScreen.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
			videoOnScreen.transform.localScale = Vector3.one * 1.2f;
		}
		else if (featureData["videoUrl"] != null && featureData["videoUrl"].Value.ToLower().Contains("greenscreen"))
		{
			//videoInAR_Greenscreen.gameObject.SetActive(true);
			//videoInARShadow_Greenscreen.gameObject.SetActive(true);

			videoInAR_Greenscreen.material = VideoController.instance.greenscreenMaterial;
			videoOnScreen.material = VideoController.instance.greenscreenMaterial;

			videoOnScreen.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
			videoOnScreen.transform.localScale = Vector3.one * 1.2f;
		}
		else if (useAlphaShader)
		{
			//videoInAR_SideBySide.gameObject.SetActive(true);
			//videoInARShadow_SideBySide.gameObject.SetActive(true);

			videoInAR_SideBySide.material = VideoController.instance.overUnderAlphaMaterial;
			videoOnScreen.material = VideoController.instance.overUnderAlphaMaterial;
			videoInAR_SideBySide.uvRect = new Rect(-0.25f, 0, 1, 1);
			videoInARShadow_SideBySide.uvRect = new Rect(-0.25f, 0, 1, 1);
			videoOnScreen.uvRect = new Rect(-0.25f, 0, 1, 1);
		}
		else
		{
			videoInAR_SideBySide.material = null;
			videoOnScreen.material = null;
		}

		yield return StartCoroutine(PlayVideoCoroutine());
		yield return new WaitForSeconds(0.1f);

		if (featureData["videoUrl"] != null && featureData["videoUrl"].Value.ToLower().Contains("greenscreen"))
		{
			videoInAR_Greenscreen.gameObject.SetActive(true);
			videoInARShadow_Greenscreen.gameObject.SetActive(true);

			StartCoroutine(
				AnimationController.instance.AnimateMaterialColorPropertyCoroutine(videoInAR_Greenscreen, "_BaseColor", new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), 1.0f, "smooth"));
			yield return StartCoroutine(
				AnimationController.instance.AnimateMaterialColorPropertyCoroutine(videoInARShadow_Greenscreen, "_BaseColor", new Color(0, 0, 0, 0), new Color(0, 0, 0, 90f / 255f), 1.0f, "smooth"));
		}
		else
		{
			videoInAR_SideBySide.gameObject.SetActive(true);
			videoInARShadow_SideBySide.gameObject.SetActive(true);

			StartCoroutine(AnimationController.instance.AnimateMaterialPropertyCoroutine(videoInAR_SideBySide, "_Alpha", 0, 1, 1.0f, "smooth"));
			yield return StartCoroutine(AnimationController.instance.AnimateMaterialPropertyCoroutine(videoInARShadow_SideBySide, "_Alpha", 0, 1, 1.0f, "smooth"));
		}
	}

	public IEnumerator InitBackFacingARCoroutine()
	{
		//ARController.instance.mainCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
		//ARController.instance.mainCamera.GetComponent<ARCameraBackground>().enabled = true;
		
		if (ARController.instance != null && !ARController.instance.arSession.enabled)
		{
			ARController.instance.InitARFoundation();
			yield return new WaitForSeconds(0.5f);
		}

		tutorialUI.SetActive(false);
		switchCameraUI.SetActive(false);

		if (shouldScan)
		{
			if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
			if (ScanController.instance != null) { yield return StartCoroutine(ScanController.instance.EnableScanCoroutine()); }
			yield return new WaitForSeconds(2.0f);
		}
		else { yield return new WaitForSeconds(1.5f); }

		if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
		arObjectRoot.SetActive(true);
		Vector3 targetPosition = mainCamera.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized * 1.5f;
		targetPosition.y = mainCamera.transform.position.y - 1.4f;
		arObjectRoot.transform.position = targetPosition;

		arUI.SetActive(true);
	}

	public IEnumerator InitBackFacingScreenCoroutine()
	{
		yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine(false));
		yield return new WaitForSeconds(0.5f);

		screenObjectRoot.SetActive(true);
		PlaceScreenObject(2.0f, -0.6f);
		switchCameraUI.SetActive(false);
		tutorialUI.SetActive(false);
		arUI.SetActive(true);
		if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
	}

	public IEnumerator InitFrontFacingCoroutine()
	{
		//WebcamController.instance.webcamCamera.depth = -1;
		//WebcamController.instance.webcamContent.GetComponentInChildren<Canvas>(true).worldCamera = WebcamController.instance.webcamCamera;
		//WebcamController.instance.webcamContent.GetComponentInChildren<Canvas>(true).gameObject.layer = LayerMask.NameToLayer("Webcam");
		//WebcamController.instance.cameraImage.gameObject.layer = LayerMask.NameToLayer("Webcam");
		//ARController.instance.mainCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.Depth;
		//ARController.instance.mainCamera.GetComponent<ARCameraBackground>().enabled = false;

		yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine(true));
		yield return new WaitForSeconds(0.5f);
		
		// ARCamera on ScreenSpace Canvas does not fit an image to Fullscreen. This is an ugly fix to scale the image a bit to fit the screen. 
		// A better solution would to use an other camera for the Webcam, but then we have problems to capture photos and videos with Webcam image
		if(fixFrontFacingCameraFullscreen && WebcamController.instance.cameraImage.transform.localScale.x != 1){ WebcamController.instance.cameraImage.transform.localScale *= 1.01f;}

		screenObjectRoot.SetActive(true);
		PlaceScreenObject(2.0f, -0.6f);
		switchCameraUI.SetActive(false);
		tutorialUI.SetActive(false);
		arUI.SetActive(true);
		if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
		
		yield return new WaitForSeconds(0.5f);
	}

	public void PlaceScreenObject(float distInfront, float offsetY)
	{
		screenObjectRoot.GetComponentInChildren<LookAt>(true).enabled = false;
		screenObjectRoot.GetComponentInChildren<LookAt>(true).transform.localEulerAngles = Vector3.zero;

		screenObjectRoot.transform.position = mainCamera.transform.position;
		screenObjectRoot.transform.eulerAngles = mainCamera.transform.eulerAngles;

		Vector3 forward = new Vector3(mainCamera.transform.forward.x, mainCamera.transform.forward.y, mainCamera.transform.forward.z);
		Vector3 up = new Vector3(mainCamera.transform.up.x, mainCamera.transform.up.y, mainCamera.transform.up.z);
		Vector3 pos = mainCamera.transform.position + forward.normalized * distInfront + up * offsetY;
		screenObject.transform.position = pos;
	}

	public void SwitchFrontBackFacing()
	{
		if (isLoading) return;
		isLoading = true;
		StartCoroutine(SwitchFrontBackFacingCoroutine());
	}

	public IEnumerator SwitchFrontBackFacingCoroutine()
	{
		switchCameraUI.SetActive(true);
		arUI.SetActive(false);

		if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
		yield return new WaitForSeconds(0.25f);

		if (cameraARType == CameraARType.FrontFacing)
		{
			WebcamController.instance.DisablePhotoCamera();
			yield return new WaitForSeconds(0.5f);
			cameraARType = cameraARTypeBackFacing;
		}
		else
		{
			WebcamController.instance.DisablePhotoCamera();
			if (ARController.instance != null && ARController.instance.arSession.enabled) { ARController.instance.StopARSession(); }
			yield return new WaitForSeconds(0.5f);
			cameraARType = CameraARType.FrontFacing;
		}

		yield return StartCoroutine(StartARCoroutine());

		isLoading = false;
	}

	public void EnablePlacement()
	{
		placementEnabled = true;
	}

	public void Back()
	{
		if (tutorialUI.activeInHierarchy) { InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation); }
		else { CommitBack(); }
	}

	public void CommitBack()
	{
		if (isLoading) return;
		isLoading = true;
		StartCoroutine(BackCoroutine());
	}

	public IEnumerator BackCoroutine()
	{
		if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
		yield return new WaitForSeconds(0.25f);

		Reset();

		if (cameraARType == CameraARType.FrontFacing)
		{
			WebcamController.instance.DisablePhotoCamera();
			yield return new WaitForSeconds(0.5f);
		}
		else
		{
			WebcamController.instance.DisablePhotoCamera();
			if (ARController.instance != null && ARController.instance.arSession.enabled) { ARController.instance.StopARSession(); }
			yield return new WaitForSeconds(0.5f);
		}

		if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
		isLoading = false;
	}

	public void Reset()
	{
		//ARController.instance.mainCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
		//ARController.instance.mainCamera.GetComponent<ARCameraBackground>().enabled = true;
		
		if (ScanController.instance != null) { ScanController.instance.DisableScanCoroutine(); }
		StopCoroutine("StartARCoroutine");
		MediaCaptureController.instance.Reset();

		//videoPlayer.Stop();
		videoPlayer.Pause();

		videoStarted = false;
		//placementEnabled = false;
		//placementInfoARShowed = false;
		//placementInfoSelfieShowed = false;

		switchCameraUI.SetActive(false);
		arUI.SetActive(false);
		tutorialUI.SetActive(true);

		videoInAR_Greenscreen.gameObject.SetActive(false);
		videoInARShadow_Greenscreen.gameObject.SetActive(false);
		videoInAR_SideBySide.gameObject.SetActive(false);
		videoInARShadow_SideBySide.gameObject.SetActive(false);

		screenObjectRoot.GetComponentInChildren<ZoomObjectHandler>(true).reset();
		screenObjectRoot.SetActive(false);
		arObjectRoot.SetActive(false);
	}
}
