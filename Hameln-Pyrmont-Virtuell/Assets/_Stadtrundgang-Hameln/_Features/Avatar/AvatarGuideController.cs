using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using SimpleJSON;

public class AvatarGuideController : MonoBehaviour
{
	public string testId = "ritter";

	[Space(10)]

    public CameraARType cameraARType = CameraARType.BackFacingAR;
    public CameraARType cameraARTypeBackFacing = CameraARType.BackFacingAR;
	public enum CameraARType { BackFacingAR, BackFacingScreen, FrontFacing }
	public bool shouldScan = false;

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
    public AudioSource audioSource;
	public SpeechAnimator speechAnimator;
	public EyesBlink eyesBlink;
	public GameObject avatarHolder;
	public GameObject animationButtons;

	[Space(10)]

    public GameObject mainCamera;
    public GameObject testHelperObjects;

    private Vector3 hitOffset = Vector3.zero;
    private bool placementEnabled = false;
    private bool isMovingARObject = false;
    private bool isLoading = false;

    private bool placementInfoARShowed = false;
    private bool placementInfoSelfieShowed = false;
    private bool showInfoOnlyOnce = true;
	private Vector3 lightRotation = new Vector3( 45, -10, 0 );

	public static AvatarGuideController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
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
        if (SiteController.instance != null && SiteController.instance.currentSite != null &&
			SiteController.instance.currentSite.siteID != "AvatarGuideSite" &&
			SiteController.instance.currentSite.siteID != "RitterSite"
			) return;

        if (arUI.activeInHierarchy)
        {
            if (VideoCaptureController.instance != null && PhotoCaptureController.instance != null)
            {
                if (VideoCaptureController.instance.previewUI.activeInHierarchy || PhotoCaptureController.instance.previewUI.activeInHierarchy)
                {
                    if (audioSource.isPlaying) { audioSource.Pause(); }
                }
                else
                {
                    if (!audioSource.isPlaying) { audioSource.Play(); }
                }
            }
        }

        HandlePlacement();
    }

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

		bool foundAvatar = false;
		string id = MapController.instance == null ? testId : MapController.instance.selectedStationId;
		if(id == "Bieber" || id == "Luenni" ) { animationButtons.SetActive(true); } else { animationButtons.SetActive( false ); }

		foreach ( Transform child in avatarHolder.transform)
		{
			child.gameObject.SetActive(false);

			if ( id != "" && child.name.EndsWith( id ) )
			{
				foundAvatar = true;
				child.gameObject.SetActive(true);
				if ( speechAnimator != null ) { speechAnimator.Init( child.gameObject, id ); }
				if ( eyesBlink != null ) { eyesBlink.mySkinnedMeshRenderer = child.GetComponentInChildren<SkinnedMeshRenderer>(); }
			}
		}

		if ( !foundAvatar ){ LoadDefaultAvatar(); }

        if (StationController.instance != null)
        {
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
        }

        if (ARController.instance != null)
        {
            mainCamera = ARController.instance.mainCamera;
            if (WebcamController.instance != null) { WebcamController.instance.webcamContent.GetComponentInChildren<Canvas>(true).worldCamera = mainCamera.GetComponent<Camera>(); }
        }

        tutorialUI.SetActive(true);
        arUI.SetActive(false);
    }

	public void LoadDefaultAvatar()
	{
		if ( speechAnimator != null ) { speechAnimator.shouldPlay = false; }

		foreach ( Transform child in avatarHolder.transform )
		{
			if ( child.name.EndsWith( "default" ) )
			{
				child.gameObject.SetActive( true );
				if ( eyesBlink != null ) { eyesBlink.mySkinnedMeshRenderer = child.GetComponentInChildren<SkinnedMeshRenderer>(); }
			}
		}

		string audioFile = "LipSyncAudioTestFile";
		AudioClip clip = Resources.Load<AudioClip>( audioFile );
		if ( clip != null ) { audioSource.clip = clip; }
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
            if (PermissionController.instance != null) { yield return StartCoroutine(PermissionController.instance.ValidatePermissionsCameraCoroutine("arFeature", (bool success) => { hasPermission = success; })); }
            if (!hasPermission) { isLoading = false; yield break; }
        }

        screenObjectRoot.SetActive(false);
        arObjectRoot.SetActive(false);

        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
        yield return new WaitForSeconds(0.25f);

        if (cameraARType == CameraARType.BackFacingAR) { yield return StartCoroutine(InitBackFacingARCoroutine()); }
        else if (cameraARType == CameraARType.BackFacingScreen) { yield return StartCoroutine(InitBackFacingScreenCoroutine()); }
        else if (cameraARType == CameraARType.FrontFacing) { yield return StartCoroutine(InitFrontFacingCoroutine()); }

        audioSource.Play();

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

    public IEnumerator InitBackFacingARCoroutine()
    {
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

		if ( Camera.main != null )
		{
			arObjectRoot.transform.LookAt( Camera.main.transform );
			arObjectRoot.transform.eulerAngles = new Vector3(0, arObjectRoot.transform.eulerAngles.y+180, 0);

			if ( LightController.instance != null )
			{
				lightRotation = LightController.instance.directionalLight.transform.eulerAngles;
				LightController.instance.directionalLight.transform.position = Camera.main.transform.position;
				LightController.instance.directionalLight.transform.LookAt(arObjectRoot.transform);
				LightController.instance.directionalLight.transform.eulerAngles = new Vector3(45, LightController.instance.directionalLight.transform.eulerAngles.y, 0);
				if(MapController.instance != null && MapController.instance.selectedStationId == "arguide" ) { LightController.instance.directionalLight.transform.eulerAngles = new Vector3( 90, 0, 0 ); }
				DynamicGI.UpdateEnvironment();
			}
		}

		arUI.SetActive(true);
    }

    public IEnumerator InitBackFacingScreenCoroutine()
    {
        yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine(false));
        yield return new WaitForSeconds(0.5f);

        /*
        if (ARController.instance != null && !ARController.instance.arSession.enabled)
        {
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
        }
        */

        screenObjectRoot.SetActive(true);
        PlaceScreenObject(2.0f, -0.6f);
        switchCameraUI.SetActive(false);
        tutorialUI.SetActive(false);
        arUI.SetActive(true);
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
    }

    public IEnumerator InitFrontFacingCoroutine()
    {
        yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine(true));
        yield return new WaitForSeconds(0.5f);

        screenObjectRoot.SetActive(true);
        PlaceScreenObject(2.0f, -0.6f);
        switchCameraUI.SetActive(false);
        tutorialUI.SetActive(false);
        arUI.SetActive(true);
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
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

    public void CommitClose()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(CommitCloseCoroutine());
    }

    public IEnumerator CommitCloseCoroutine()
    {
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("TestFeaturesSite"));
        ARController.instance.StopARSession();
        Reset();

        isLoading = false;
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

	public void PlayAnimation(int animationId)
	{
		string id = MapController.instance == null ? testId : MapController.instance.selectedStationId;
		foreach ( Transform child in avatarHolder.transform )
		{
			if ( id != "" && child.name.EndsWith( id ) )
			{
				if ( id == "Bieber" && animationId == 0 ) { child.GetComponentInChildren<Animator>().CrossFade( "Beaver Talk", 0.1f ); }
				else if ( id == "Bieber" && animationId == 1 ) { child.GetComponentInChildren<Animator>().CrossFade( "Beaver Success", 0.1f ); }
				else if ( id == "Bieber" && animationId == 2 ) { child.GetComponentInChildren<Animator>().CrossFade( "Beaver Failure", 0.1f ); }
				else if ( id == "Bieber" && animationId == 3 ) { child.GetComponentInChildren<Animator>().CrossFade( "Beaver Idle", 0.1f ); }
				else if ( id == "Bieber" && animationId == 4 ) { child.GetComponentInChildren<Animator>().CrossFade( "Beaver Idle 2", 0.1f ); }

				if ( id == "Luenni" && animationId == 0 ) { child.GetComponentInChildren<Animator>().CrossFade( "Rabbit Talk", 0.1f ); }
				else if ( id == "Luenni" && animationId == 1 ) { child.GetComponentInChildren<Animator>().CrossFade( "Rabbit Success", 0.1f ); }
				else if ( id == "Luenni" && animationId == 2 ) { child.GetComponentInChildren<Animator>().CrossFade( "Rabbit Failure", 0.1f ); }
				else if ( id == "Luenni" && animationId == 3 ) { child.GetComponentInChildren<Animator>().CrossFade( "Rabbit Idle", 0.1f ); }
				else if ( id == "Luenni" && animationId == 4 ) { child.GetComponentInChildren<Animator>().CrossFade( "Rabbit Idle 2", 0.1f ); }
			}
		}
	}

    public void Reset()
    {
        if (ScanController.instance != null) { ScanController.instance.DisableScanCoroutine(); }
        StopCoroutine("StartARCoroutine");
        MediaCaptureController.instance.Reset();

        audioSource.Stop();
        //placementEnabled = false;
        //placementInfoARShowed = false;
        //placementInfoSelfieShowed = false;

        switchCameraUI.SetActive(false);
        arUI.SetActive(false);
        tutorialUI.SetActive(true);

        screenObjectRoot.GetComponentInChildren<ZoomObjectHandler>(true).reset();
        screenObjectRoot.SetActive(false);
        arObjectRoot.SetActive(false);

		if ( LightController.instance != null )
		{
			LightController.instance.directionalLight.transform.eulerAngles = lightRotation;
		}
	}
}
