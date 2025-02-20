using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;
using TMPro;
using SimpleJSON;
using UnityEngine.XR.ARSubsystems;

public class ScanController : MonoBehaviour
{
    public bool editorShouldShowScanAnimation = true;
	public bool isScanningMarker = false;
	public string currentStationId = "";

	[Space(10)]

    public ARSession arSession;
    public GameObject mainCamera;
    public GameObject scanCamera;
    public GameObject defaultScanDialog;
    public GameObject defaultScanDescription;
	public GameObject skipScanButton;

	[Space(10)]

    public GameObject scanFrameRoot;
    public GameObject scanWithoutMarkerContent;
    public GameObject scanDialog;
    public GameObject scanDescription;
    public RawImage scanAnimationRawImage;
    public GameObject closeVideoTutorialButton;

    [Space(10)]

    public string currentTrackedMarker = "";
    public bool useGuideInAR = true;
    public Vector3 currentMarkerPosition = Vector3.zero;

    public bool isLoading = false;
    private bool isScanning = false;
    private bool finishedScanning = false;
    private RenderTexture scanCameraRenderTexture;

    public static ScanController instance;
    void Awake()
    {

        instance = this;
    }

    private void Update()
    {
		if (!isScanningMarker) return;
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "ScanSite") return;

        
        #if UNITY_EDITOR
        for (int i = 0; i <= 9; i++)
        {
			if ( Input.GetKeyDown(i.ToString()) && i < ImageTargetController.Instance.imageTargets.Count) {

				if ( ImageTargetController.Instance.imageTargets[i].trackingState != TrackingState.Tracking ){
					ImageTargetController.Instance.imageTargets[i].trackingState = TrackingState.Tracking;
				}
				else{ ImageTargetController.Instance.imageTargets[i].trackingState = TrackingState.None; }

				//OnMarkerTracked(ImageTargetController.Instance.imageTargets[i].id);
			}
        }
        #endif

        for (int i = 0; i < ImageTargetController.Instance.imageTargets.Count; i++)
        {
            if(ImageTargetController.Instance.imageTargets[i].isTracking)
            {
                OnMarkerTracked(ImageTargetController.Instance.imageTargets[i].id);
            }         
        }
	}

	public void StartScan()
    {
        if (isLoading) return;
        isLoading = true;

        StartCoroutine(StartScanCoroutine());
    }

    public IEnumerator StartScanCoroutine()
    {
        bool hasPermissionCamera = false;

        yield return StartCoroutine(
            PermissionController.instance.ValidatePermissionsCameraCoroutine("ar", (bool success) => {
                hasPermissionCamera = success;
            })
        );

        if (hasPermissionCamera)
        {
            InfoController.instance.loadingCircle.SetActive(true);
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
            InfoController.instance.loadingCircle.SetActive(false);

            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ScanSite"));
			scanFrameRoot.SetActive(true);
			SetIsScanningActive();
		}

        isLoading = false;
    }

	public void SetIsScanningActive()
	{
		isScanningMarker = true;
		if(GlasObjectController.instance != null )
		{
			GlasObjectController.instance.isScanningMarker = true;
			GlasObjectController.instance.hasScannedMarker = false;
			ARMenuController.instance.currentFeature = "";
		}
	}

	public void StartScanWithoutMarker()
    {
        if (isLoading) return;
        isLoading = true;

        StartCoroutine(StartScanWithoutMarkerCoroutine());
    }

    public IEnumerator StartScanWithoutMarkerCoroutine()
    {
        bool hasPermissionCamera = false;

        yield return StartCoroutine(
            PermissionController.instance.ValidatePermissionsCameraCoroutine("ar", (bool success) => {
                hasPermissionCamera = success;
            })
        );

        if (hasPermissionCamera)
        {
            this.useGuideInAR = true;
            InfoController.instance.loadingCircle.SetActive(true);
            InfoController.instance.blocker.SetActive(true);
            ARController.instance.arPlaneManager.enabled = false;
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
            InfoController.instance.loadingCircle.SetActive(false);

            scanDialog.SetActive(false);
            scanCamera.SetActive(true);
            scanWithoutMarkerContent.SetActive(true);
			scanFrameRoot.SetActive( false );
			if ( PlayerPrefs.GetInt("scanDescriptionShowed") != 1) { scanDescription.SetActive(false); }
            else { scanDescription.SetActive(true); }

            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ScanSite"));
            yield return new WaitForSeconds(0.25f);

            if (PlayerPrefs.GetInt("scanDescriptionShowed") != 1) { scanDialog.SetActive(true); }
            else { CommitStartScanWithoutMarker(); }

            InfoController.instance.blocker.SetActive(false);

            isScanningMarker = false;
        }

        isLoading = false;
    }

    public void ContinueGuideWithoutAR()
    {
        this.useGuideInAR = false;
        string markerId = StationController.instance.GetStationMarkerId(MapController.instance.selectedStationId);
        OnMarkerTracked(markerId);
    }

    public void CommitStartScanWithoutMarker()
    {
        if (isScanning)
        {
            scanDialog.SetActive(false);
            return;
        }
        
        isScanning = true;

        StartCoroutine("CommitStartScanWithoutMarkerCoroutine");
    }

    public IEnumerator CommitStartScanWithoutMarkerCoroutine()
    {
        yield return new WaitForEndOfFrame();
        PlayerPrefs.SetInt("scanDescriptionShowed", 1);

        bool shouldShowScanAnimation = true;
#if UNITY_EDITOR
        shouldShowScanAnimation = editorShouldShowScanAnimation;
#endif
        if(currentStationId == MapController.instance.selectedStationId)
        {
            shouldShowScanAnimation = false;
        }

        currentStationId = MapController.instance.selectedStationId;
        scanCamera.SetActive(false);
        scanDialog.SetActive(false);
        if(shouldShowScanAnimation) scanDescription.SetActive(true);

        ARController.instance.arPlaneManager.enabled = true;

        if (shouldShowScanAnimation)
        {
            ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.Wireframe);
            ARController.instance.ShowScanAnimation();
            ARController.instance.scanAnimationAlwaysVisible = false;
            ScanAnimator.instance.ShowHideDepthMask(true);
            ScanAnimator.instance.ShowHideMobileScreen(false);

            finishedScanning = false;
            while (
                ARController.instance.isScanning ||
                scanDialog.activeInHierarchy ||
                VideoController.instance.videoSite.activeInHierarchy
            )
            {
                yield return new WaitForEndOfFrame();
            }

            finishedScanning = true;
            yield return new WaitForSeconds(1.0f);
            ARController.instance.ShowHidePlanes(false);
            ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.Invisible);
        }
        else
        {
            //InfoController.instance.loadingCircle.SetActive(true);
            yield return new WaitForSeconds(1.0f);
            //InfoController.instance.loadingCircle.SetActive(false);
        }

        string markerId = StationController.instance.GetStationMarkerId(MapController.instance.selectedStationId);
        if (markerId != "")
        {
            scanWithoutMarkerContent.SetActive(false);
            OnMarkerTracked(markerId);
            currentMarkerPosition = ARController.instance.GetGroundPositionInfrontUser();
			Debug.Log( "<color=#87CEFA>GetGroundPositionInfrontUser</color>" );
        }
        else
        {
            InfoController.instance.ShowMessage("Inhalte dieser Station konnten nicht abgerufen werden.");
        }

        isScanning = false;
    }

    public void AbortScanWithoutMarker()
    {
        if (isLoading) return;
        isLoading = true;

        StartCoroutine(AbortScanWithoutMarkerCoroutine());
    }

    public IEnumerator AbortScanWithoutMarkerCoroutine()
    {
        print("AbortScanWithoutMarkerCoroutine");

        StopCoroutine("CommitStartScanWithoutMarkerCoroutine");

		MapController.instance.EnableMap();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));
        yield return new WaitForSeconds(0.25f);

        ARController.instance.StopAndResetARSession();
        ARController.instance.HideScanAnimation();
        ResetMarkerlessScan();

        isScanning = false;
        isLoading = false;
    }


	public void ResetMarkerlessScan()
    {
        ARController.instance.ShowHidePlanes(false);
        ARController.instance.scanAnimationAlwaysVisible = false;
	    ScanAnimator.instance.ShowHideDepthMask(true);
	    ScanAnimator.instance.ShowHideMobileScreen(false);

        scanCamera.SetActive(false);
        scanDialog.SetActive(false);
        scanWithoutMarkerContent.SetActive(false);
    }

    public void ShowAdditionalScanInfo()
    {
        if (!finishedScanning) { scanDialog.SetActive(true); }
        
        /*
        if (isLoading || finishedScanning) return;
        isLoading = true;

        StartCoroutine(ShowAdditionalScanInfoCoroutine());
        */
    }

    public IEnumerator ShowAdditionalScanInfoCoroutine()
    {
        yield return null;

        string savePath = Application.persistentDataPath + "/" + "HowToBodenmarker.mp4";
        if (File.Exists(savePath))
        {
            // Set URL
            VideoController.instance.currentVideoTarget.videoType = VideoTarget.VideoType.URL;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoType = VideoTarget.VideoType.URL;
            VideoController.instance.currentVideoTarget.videoURL = savePath;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoURL = savePath;

            // Start play video
            VideoController.instance.AdjustVideoRatio(1.777777f);
            VideoController.instance.currentVideoTarget.isLoaded = false;
            VideoController.instance.PlayVideo();

            // Wait until loaded
            InfoController.instance.loadingCircle.SetActive(true);
            float timer = 10;
            while (!VideoController.instance.currentVideoTarget.isLoaded && timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
            InfoController.instance.loadingCircle.SetActive(false);
            VideoController.instance.EnableFullscreen();
            closeVideoTutorialButton.SetActive(true);

            SiteController.instance.currentSite.gameObject.SetActive(false);
        }
       
        isLoading = false;
    }

    public void CloseVideoScanTutorial()
    {
        closeVideoTutorialButton.SetActive(false);
        VideoController.instance.DisableFullscreen();
        SiteController.instance.currentSite.gameObject.SetActive(true);
    }

    public void AbortScan()
    {
        //if (isLoading) return;
        //isLoading = true;
        //StartCoroutine(AbortScanCoroutine());
    }

    public IEnumerator AbortScanCoroutine()
    {
        print("AbortScanCoroutine");

        isScanningMarker = false;
        ARController.instance.StopARSession();

		MapController.instance.EnableMap();
		yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));
	    ARMenuController.instance.StopFeatures();
	    
        isLoading = false;
    }

	public void CloseARSite()
	{
		if (isLoading) return;
		isLoading = true;

		StartCoroutine(CloseARSiteCoroutine());
	}

	public IEnumerator CloseARSiteCoroutine()
	{
        print("CloseARSiteCoroutine");

        if ( PhotoController.Instance.photoHelper.capturedPhotoOptions.activeInHierarchy ){
			
			PhotoController.Instance.photoHelper.Reset();
		}
        else if (ARMenuController.instance.currentFeature == "video")
        {
        	yield return StartCoroutine(StationController.instance.BackToStationSiteCoroutine());

            if (VideoFeatureController.instance != null) { VideoFeatureController.instance.Reset(); }
            ARMenuController.instance.currentFeature = "";
            ARMenuController.instance.MarkMenuButton("");
        }
        else
        {		
			isScanningMarker = false;
			ARController.instance.StopARSession();

            MapController.instance.EnableMap();
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));
	        ARMenuController.instance.StopFeatures();
        }
		isLoading = false;
	}

    public void CloseStation()
    {
	    //InfoController.instance.ShowCommitAbortDialog("Möchtest Du weiter zur nächsten Station?", CommitCloseStation);
        
	    InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, CommitCloseStation);
    }

    public void CommitCloseStation()
    {
        if (isLoading) return;
        isLoading = true;

        StartCoroutine(CommitCloseStationCoroutine());
    }

    public IEnumerator CommitCloseStationCoroutine()
    {
        isScanningMarker = false;
        ARController.instance.StopARSession();

        MapFilterController.instance.didClickedOnFilterStation = false;
		MapController.instance.EnableMap();
		yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));

	    ARMenuController.instance.StopFeatures();
        yield return StartCoroutine(ToolsController.instance.CleanMemoryCoroutine());

        isLoading = false;
    }

    public void OnMarkerTracked(string markerId)
    {
		print( "<color=#87CEFA>OnMarkerTracked " + markerId + "</color>" );

		// Optional: we can force to only scan the marker which belongs to the current selected station
		//JSONNode stationNode = StationController.instance.GetStationDataFromMarkerId( markerId, MapController.instance.selectedStationId, true );
		JSONNode stationNode = StationController.instance.GetStationDataFromMarkerId( markerId );

		if ( stationNode == null) { Debug.LogError("Marker does not available for this station " + markerId); return; }

        MapFilterController.instance.didClickedOnFilterStation = false;

		// New: Disabled, because otherwise we would set the wrong station data if scanning a marker which does not belongs to the current selected station
		//StationController.instance.SetCurrentStationDataFromMarkerId( markerId );

		currentMarkerPosition = ImageTargetController.Instance.GetImageTargetPosition(markerId);
        currentTrackedMarker = markerId;
        isScanningMarker = false;

		if (StationController.instance.currentStationData != null)
        {
            MapController.instance.MarkStationFinished(StationController.instance.currentStationData["id"].Value);
        }

        StartCoroutine(OnMarkerTrackedCoroutine(markerId));
    }

	public IEnumerator UpdateMarkerPositionCoroutine(string markerId)
	{
		float timer = 0.25f;
		while ( timer > 0 && ImageTargetController.Instance.IsTrackingMarker( markerId ) )
		{
			currentMarkerPosition = ImageTargetController.Instance.GetImageTargetPosition( markerId );

			Debug.Log( "UpdateMarkerPositionCoroutine " + currentMarkerPosition );

			timer -= Time.deltaTime;
			yield return null;
		}
	}

	public IEnumerator OnMarkerTrackedCoroutine(string markerId)
    {
		StopCoroutine( "UpdateMarkerPositionCoroutine" );
		StartCoroutine( "UpdateMarkerPositionCoroutine", markerId );

		ARMenuController.instance.InitMenu(markerId);
        GuideController.instance.guideOptions.SetActive(false);

        bool hasGuideFeature = StationController.instance.HasFeature("guide");
        bool useGuideInAR = StationController.instance.UseGuideInAR() && PermissionController.instance.IsARFoundationSupported();

		if ( hasGuideFeature && useGuideInAR)
        {
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ARSite"));
            yield return new WaitForSeconds(0.25f);
        }

		bool isImplemented = true;
        if (hasGuideFeature)
        {
            yield return StartCoroutine(ARMenuController.instance.OpenCloseMenuCoroutine());
            ARMenuController.instance.OpenMenu("guide");
        }
        else
        {
			if (!ARMenuController.instance.mainMenuButton.gameObject.activeSelf)
            {
                InfoController.instance.ShowMessage("Funktionen für diese Station noch nicht verfügbar.");
				isImplemented = false;
			}
			else if ( MapController.instance.selectedStationId.Contains( "glashuette" ) ) { ARMenuController.instance.OpenMenu( MapController.instance.selectedStationId ); }
			else if ( MapController.instance.selectedStationId == "glasmacher" ) { ARMenuController.instance.OpenMenu( "avatarGuide" ); }
			else if ( StationController.instance.HasFeature( "avatarGuide" ) ) { ARMenuController.instance.OpenMenu( "avatarGuide" ); }
			else if (StationController.instance.HasFeature("info")) { ARMenuController.instance.OpenMenu("info"); }
            else { ARMenuController.instance.OpenMenu(); }
        }

		if ( isImplemented ) { MapController.instance.DisableMap(); }
    }

    public void ShowInfo()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(ShowInfoCoroutine());
    }

    public IEnumerator ShowInfoCoroutine()
    {
        //isScanningMarker = false;

        yield return null;
        InfoController.instance.ShowMessage("QR-Code-Erkennung", "Damit der QR-Code erkannt wird, achte darauf, dass er den Scanrahmen vollständig ausfüllt.");
        isLoading = false;
    }

    public void ToggleFlashLight()
    {

    }

    public IEnumerator EnableScanCoroutine()
    {
        yield return StartCoroutine("ScanCoroutine");
    }

    public void DisableScanCoroutine()
    {
        StopCoroutine("ScanCoroutine");
    }

    public IEnumerator ScanCoroutine()
    {
        ARController.instance.arPlaneManager.enabled = true;
        ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.Wireframe);
        ARController.instance.ShowScanAnimation();
        ARController.instance.scanAnimationAlwaysVisible = false;
        ScanAnimator.instance.ShowHideDepthMask(true);
        ScanAnimator.instance.ShowHideMobileScreen(false);
        ScanAnimator.instance.ShowHideMobileScreen(false);
        ScanAnimator.instance.scanInfo.SetActive(false);

        yield return null;
        ScanAnimator.instance.scanInfo.SetActive(false);
        defaultScanDescription.SetActive(true);

        while (
            ARController.instance.isScanning ||
            defaultScanDialog.activeInHierarchy
        )
        {
            yield return new WaitForEndOfFrame();
        }

        ARController.instance.ShowHidePlanes(false);
        ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.Invisible);
        defaultScanDescription.SetActive(false);
        defaultScanDialog.SetActive(false);
    }

	public void SkipMarkerlessScan()
	{
		if ( isLoading ) return;
		StartCoroutine( SkipMarkerlessScanCoroutine() );
	}

	public IEnumerator SkipMarkerlessScanCoroutine()
	{
		InfoController.instance.blocker.SetActive( true );

		string markerId = StationController.instance.GetStationMarkerId( MapController.instance.selectedStationId );
		if ( markerId != "" )
		{
			StopCoroutine( "CommitStartScanWithoutMarkerCoroutine" );
			scanWithoutMarkerContent.SetActive( false );
			currentMarkerPosition = ARController.instance.GetGroundPositionInfrontUser();
			isScanning = false;

			if ( StationController.instance.currentStationData != null ) { MapController.instance.MarkStationFinished( StationController.instance.currentStationData["id"].Value ); }

			//PoiInfoController.instance.shouldOpenMenu = true;
			ARMenuController.instance.InitMenu( markerId );
			ARMenuController.instance.OpenMenu( "info" );

			yield return new WaitForSeconds( 0.25f );

			ARController.instance.StopAndResetARSession();
			ARController.instance.HideScanAnimation();
			ResetMarkerlessScan();
		}
		else
		{
			InfoController.instance.ShowMessage( "Inhalte dieser Station konnten nicht abgerufen werden." );
		}

		InfoController.instance.blocker.SetActive( false );
	}
}
