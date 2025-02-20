using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using SimpleJSON;

// This controller handles videoplayback of a videoTarget object
// It handles play, pause, step back, step forward, switch to fullscreen, show/hide video navigation

public class VideoController : MonoBehaviour
{
    public bool autoPlayVideo = false;

    [Space(10)]

    public Material greenscreenMaterial;
    public Material overUnderAlphaMaterial;
    public GameObject backgroundImage;
    public GameObject shadowImage_SideBySide;
    public GameObject shadowImage_Greenscreen;

    [Space(10)]

    public VideoPlayer videoPlayer;
    public GameObject videoRoot;
    public GameObject videoOffset;
    public GameObject videoCanvas;
    public GameObject videoSite;
    public GameObject noConnectionUI;
    public GameObject noConnectionUISite;
    public bool isConnectedEditorTest = true;

    [Space(10)]

    public float switchToFullscreenTime = 0.6f;
    public float switchOrientationTime = 0.6f;
    public float showHideVideoNavigationSpeed = 10f;
    public float navigationSliderHideTime = 4;
    private float currentNavigationSliderHideTime = 0;

    [Space(10)]

    public bool videoSliderSelected = false;
    public VideoTarget currentVideoTarget;
    public VideoTarget mainUIVideoTarget;
    public VideoTarget main3DVideoTarget;
    public ScreenOrientation testScreenOrientation; // for testing in unity editor change this

    [Space(10)]

    public List<AspectRatioFitter> screenAspectRatioFitter = new List<AspectRatioFitter>();
    public List<AspectRatioFitter> screenAspectRatioFitterMainVideo = new List<AspectRatioFitter>();

    [Space(10)]

    public EventSystem eventSystem;
    public Canvas canvas;

    private bool frameIsReady = false;
    private bool videoIsPrepared = false;
    private bool videoStarting = false;
    private bool isSwitchingFullscreen = false;
    private bool isRotating = false;
    private bool seekCompleted = false;
    private bool isLoading = false;
    private float videoNavigationAlpha = 0;
    private ScreenOrientation videoOrientation;
    private ScreenOrientation currentScreenOrientation;
    private bool appWasPaused = false;
    private RenderTexture videoRenderTexture;

    public static VideoController instance;
    void Awake()
    {

        instance = this;
        videoPlayer.sendFrameReadyEvents = true;
    }

    void Start()
    {

        videoOrientation = Screen.orientation;
        currentScreenOrientation = Screen.orientation;
        testScreenOrientation = Screen.orientation;

        videoPlayer.loopPointReached += VideoPlayer_loopPointReached;
    }

    /************** check and handle video state every frame **************/
    void Update()
    {

        if (mainUIVideoTarget != null && mainUIVideoTarget.gameObject.activeInHierarchy)
        {

            if (!mainUIVideoTarget.hideNavigation)
            {
                UpdateNavigationTimer(mainUIVideoTarget);
                UpdateVideoNavigationVisiblity(mainUIVideoTarget);
                //UpdateOrientation(mainUIVideoTarget);     // FullscreenMode (Old)
            }
        }

        if (currentVideoTarget != null && currentVideoTarget.gameObject.activeInHierarchy)
        {

            if (!currentVideoTarget.hideNavigation)
            {
                UpdateNavigationTimer(currentVideoTarget);
                UpdateVideoNavigationVisiblity(currentVideoTarget);
            }
        }
    }

    /************** Videoplayer event callbacks **************/
    private void VideoPlayer_frameReady(UnityEngine.Video.VideoPlayer vp, long frame)
    {
        frameIsReady = true;
    }

    private void VideoPlayer_prepareCompleted(VideoPlayer source)
    {
        videoIsPrepared = true;
    }

    private void VideoPlayer_seekCompleted(VideoPlayer source)
    {
        seekCompleted = true;
    }

    private void VideoPlayer_loopPointReached(VideoPlayer source)
    {
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID == "ARSite")
        {
            if (ARMenuController.instance.currentFeature == "guide")
            {
                if (StationController.instance.HasFeature("video") && MapController.instance.selectedStationId == "automuseum_melle")
                {
                    autoPlayVideo = true;
                    ARMenuController.instance.OpenMenu("video");
                }
                else
                {
                    if (Params.highlightMenuAfterVideo)
                    {
                        main3DVideoTarget.playButton.SetActive(true);
                        ARMenuController.instance.ShowHighlightMenu();
                    }
                    else if (Params.showInfoSiteAfterVideo)
                    {
                        PoiInfoController.instance.shouldOpenMenu = true;
                        ARMenuController.instance.OpenMenu("info");
                    }
                }
            }
        }
    }

    /************** function to play or pause video **************/
    public void PlayVideo(VideoTarget videoTarget)
    {

        if (noConnectionUI.activeInHierarchy)
        {
            PlayPauseVideo(videoTarget);
            return;
        }

        videoPlayer.Play();
        videoTarget.pauseButton.SetActive(true);
        videoTarget.playButton.SetActive(false);

        if (currentVideoTarget != null)
        {
            currentVideoTarget.pauseButton.SetActive(true);
            currentVideoTarget.playButton.SetActive(false);
        }
    }

    public void PauseVideo(VideoTarget videoTarget)
    {

        videoPlayer.Pause();
        videoTarget.pauseButton.SetActive(false);
        videoTarget.playButton.SetActive(true);

        if (currentVideoTarget != null)
        {
            currentVideoTarget.pauseButton.SetActive(false);
            currentVideoTarget.playButton.SetActive(true);
        }
    }

    public void PlayVideo()
    {

        if (!currentVideoTarget.isLoaded)
        {

            StartPlayVideo(currentVideoTarget);

        }
        else
        {

            if (!videoStarting)
            {
                videoPlayer.Play();
                currentVideoTarget.targetImage.gameObject.SetActive(true);
                currentVideoTarget.pauseButton.SetActive(true);
                currentVideoTarget.playButton.SetActive(false);
            }
        }
    }

    public void PlayPauseVideo(VideoTarget videoTarget)
    {

        print("PlayPauseVideo VideoController");

        if (videoPlayer.isPlaying && videoTarget == currentVideoTarget)
        {

            videoPlayer.Pause();
            videoTarget.pauseButton.SetActive(false);
            videoTarget.playButton.SetActive(true);

            if (mainUIVideoTarget != null)
            {
                mainUIVideoTarget.pauseButton.SetActive(false);
                mainUIVideoTarget.playButton.SetActive(true);
            }

        }
        else
        {

            if (videoTarget != currentVideoTarget)
            {

                ResetVideoPlayer();
                StartPlayVideo(videoTarget);

            }
            else
            {

                if (noConnectionUI.activeInHierarchy)
                {
                    StartPlayVideo(videoTarget);
                }
                else
                {

                    if (!videoStarting)
                    {

                        videoPlayer.Play();
                        videoTarget.targetImage.gameObject.SetActive(true);
                        videoTarget.pauseButton.SetActive(true);
                        videoTarget.playButton.SetActive(false);

                        if (mainUIVideoTarget != null)
                        {
                            mainUIVideoTarget.pauseButton.SetActive(true);
                            mainUIVideoTarget.playButton.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    /************** function to reset currentVideoTarget **************/
    public void ResetVideoPlayer()
    {
        //videoPlayer.Stop();
		videoPlayer.Pause();

		if (currentVideoTarget != null)
        {
            currentVideoTarget.Reset();
            currentVideoTarget = null;
        }
    }

    /************** function to initially load and start new video **************/
    public void StartPlayVideo(VideoTarget videoTarget)
    {

        videoTarget.pauseButton.SetActive(false);
        videoTarget.playButton.SetActive(true);

        if (videoTarget.videoType == VideoTarget.VideoType.URL && videoTarget.videoURL == "") return;
        if (isLoading) return;
        if (videoStarting) return;

        isLoading = true;
        StartCoroutine(ValidateConnectionAndStartPlayVideoCoroutine(videoTarget));

        /*
		if(videoStarting) return;
		videoStarting = true;
		StartCoroutine( PlayVideoCoroutine( videoTarget ) );
		*/
    }

    public IEnumerator ValidateConnectionAndStartPlayVideoCoroutine(VideoTarget videoTarget)
    {

        bool isSuccess = false;
        if (videoTarget.videoType == VideoTarget.VideoType.VideoClip || videoTarget.useFilePath) { isSuccess = true; }
        else
        {
            if (ToolsController.instance.IsValidURL(videoTarget.videoURL))
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
        }

#if UNITY_EDITOR
        isSuccess = isConnectedEditorTest;
#endif

        if (isSuccess)
        {
            noConnectionUI.SetActive(false);
            noConnectionUISite.SetActive(false);

            videoStarting = true;
            StartCoroutine(PlayVideoCoroutine(videoTarget));
        }
        else
        {
            noConnectionUI.SetActive(true);
            noConnectionUISite.SetActive(true);
			if (main3DVideoTarget != null && main3DVideoTarget.playButton != null) { main3DVideoTarget.playButton.SetActive(false); main3DVideoTarget.targetImage.gameObject.SetActive(false); }
            if (currentVideoTarget != null ) { currentVideoTarget.isLoaded = true; }
        }

        isLoading = false;
    }

    /************** coroutine to initially load and start new video **************/
    IEnumerator PlayVideoCoroutine(VideoTarget videoTarget)
    {

        if (videoPlayer.isPlaying)
        {
            //videoPlayer.Stop();
			videoPlayer.Pause();
		}

        if (videoTarget.videoType == VideoTarget.VideoType.URL)
        {

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoTarget.videoURL;

            /*
			if( System.IO.File.Exists( LanguageController.GetTranslation( videoTarget.videoURL ) ) ){
				videoPlayer.url = LanguageController.GetTranslation( videoTarget.videoURL );
			}else{
				videoPlayer.url = Application.streamingAssetsPath + "/" + LanguageController.GetTranslation( videoTarget.videoURL );
			}
			*/

        }
        else
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoTarget.videoClip;
        }

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
        //print("frameReady " + timer);

        videoTarget.targetImage.gameObject.SetActive(true);

        if (!videoTarget.hideNavigation)
        {
            videoTarget.videoNavigationHeader.SetActive(true);
            videoTarget.videoNavigationFooter.SetActive(true);
        }

        videoTarget.pauseButton.SetActive(true);
        videoTarget.playButton.SetActive(false);
        videoTarget.SetVideoContainerIsActive(true);
        currentVideoTarget = videoTarget;


        //uint w = videoPlayer.clip.width;
        //uint h = videoPlayer.clip.height;

        /*
        uint w = 1080;
        uint h = 1920;
        videoRenderTexture = new RenderTexture((int)w, (int)h, 16, RenderTextureFormat.ARGB32);
        videoRenderTexture.useMipMap = true;
        videoRenderTexture.Create();
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoTarget.targetImage.texture = videoRenderTexture;
        */

        videoTarget.targetImage.texture = videoPlayer.texture;

        // Options to activate video shadow
        //shadowImage.SetActive(false);
        //videoTarget.hasShadow = (videoTarget.videoType == VideoTarget.VideoType.URL && videoTarget.videoURL.Contains("shadow"));

        videoTarget.hasShadow = true;
        if (videoTarget.hasShadow)
        {
            shadowImage_SideBySide.SetActive(false);
            shadowImage_Greenscreen.SetActive(false);

            if (videoTarget.videoURL.ToLower().Contains("greenscreen"))
            {
                shadowImage_Greenscreen.SetActive(true);
                shadowImage_Greenscreen.GetComponent<RawImage>().texture = videoPlayer.texture;
            }
            else
            {
                shadowImage_SideBySide.SetActive(true);
                shadowImage_SideBySide.GetComponent<RawImage>().uvRect = currentVideoTarget.targetImage.uvRect;
                shadowImage_SideBySide.GetComponent<RawImage>().uvRect = new Rect(-0.25f, 0, 1, 1);
                shadowImage_SideBySide.GetComponent<RawImage>().texture = videoPlayer.texture;
            }
        }
        videoTarget.hasShadow = false;

        if (mainUIVideoTarget != null)
        {
            mainUIVideoTarget.targetImage.texture = videoPlayer.texture;
            mainUIVideoTarget.pauseButton.SetActive(true);
            mainUIVideoTarget.playButton.SetActive(false);
        }

#if UNITY_IOS
		//videoTarget.targetImage.transform.localScale = new Vector3(1, -1, 1);
#endif

        videoTarget.isLoaded = true;
        videoStarting = false;
    }

    public IEnumerator GetVideoDurationCoroutine(VideoTarget videoTarget, Action<float> Callback)
    {

        if (videoTarget.videoType == VideoTarget.VideoType.URL)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = LanguageController.GetTranslation(videoTarget.videoURL);
        }
        else
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoTarget.videoClip;
        }

        videoIsPrepared = false;
        videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
        videoPlayer.Prepare();

        // Wait until video is prepared
        float timer = 2;
        while (!videoIsPrepared && timer > 0)
        {
            yield return null;
            timer -= Time.deltaTime;
        }
        videoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;

        double time = videoPlayer.frameCount / videoPlayer.frameRate;
        Callback((float)time);
    }


    /************** function to switch to fullscreen and back **************/
    public void SwitchFullscreen(VideoTarget videoTarget)
    {
        if (isSwitchingFullscreen || isRotating) return;
        isSwitchingFullscreen = true;

        DisableEventSystemForSeconds(switchToFullscreenTime + 0.02f);
        StartCoroutine(SwitchFullScreenCoroutine(videoTarget));
    }

    /************** coroutine to switch to fullscreen and back **************/
    public IEnumerator SwitchFullScreenCoroutine(VideoTarget videoTarget)
    {

        if (!videoTarget.isFullscreen && videoTarget.fullScreenOverrideSorting)
        {
            videoTarget.videoCanvas.overrideSorting = true;
            videoTarget.videoCanvas.sortingOrder = 100;
            if (videoTarget.videoMask != null) { videoTarget.videoMask.GetComponent<Mask>().enabled = false; }
        }

        // Calculate target position of video image (center of screen)
        float yTargetPosition = videoTarget.GetVideoContainerStartPosition().y;
        if (!videoTarget.isFullscreen) yTargetPosition = GetLocalYPositionFromCanvasCenter(videoTarget.videoContainer.GetComponent<RectTransform>(), CanvasController.instance.content.transform);
        Vector2 targetPosition = new Vector2(0, yTargetPosition);

        // Calculate target size of video image (to fill the screen)
        Vector2 targetSizeVideoImage = videoTarget.GetVideoContainerStartSize();
        Vector2 targetSizeVideoBackground = videoTarget.GetVideoContainerStartSize();
        if (!videoTarget.isFullscreen)
        {
            float ratio = videoTarget.videoContainer.GetComponent<RectTransform>().rect.height / videoTarget.videoContainer.GetComponent<RectTransform>().rect.width;
            float targetWidth = GetCanvasWidth();
            float targetHeight = ratio * targetWidth;

            if (targetHeight > GetCanvasHeight())
            {
                targetHeight = GetCanvasHeight();
                targetWidth = targetHeight / ratio;
            }
            targetSizeVideoImage = new Vector2(targetWidth, targetHeight);

            targetSizeVideoBackground.x = GetCanvasWidth();
            targetSizeVideoBackground.y = GetCanvasHeight();
        }


        // Define the start values
        Vector2 videoStartPosition = videoTarget.videoContainer.GetComponent<RectTransform>().anchoredPosition;
        Vector2 videoStartSize = new Vector2(videoTarget.videoContainer.GetComponent<RectTransform>().rect.width, videoTarget.videoContainer.GetComponent<RectTransform>().rect.height);
        Vector2 videoStartSizeBackground = new Vector2(videoTarget.videoContainerBackground.GetComponent<RectTransform>().rect.width, videoTarget.videoContainerBackground.GetComponent<RectTransform>().rect.height);
        if (videoTarget.isFullscreen)
        {
            videoStartSizeBackground = new Vector2(GetCanvasWidth(), GetCanvasHeight());
        }

        // Define values for rotation, if we have rotate the defice, we need to switch between portrait and landscape orientation
        Vector2 fromToRotationAngles = GetOrientationAngles(videoOrientation, ScreenOrientation.Portrait);
        Vector3 startRotation = new Vector3(0, 0, fromToRotationAngles.x);
        Vector3 targetRotation = new Vector3(0, 0, fromToRotationAngles.y);

        // Set anchors to center. This is necessary to lerp to the target values, because we do not use top, bottom, right, left anchors, but x, y, width and height anchors
        videoTarget.videoContainer.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        videoTarget.videoContainer.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        videoTarget.videoContainerBackground.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        videoTarget.videoContainerBackground.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        // Calc target position of video navigations
        float videoNavigationOffsetToBottom = (targetSizeVideoBackground.y - targetSizeVideoImage.y) * 0.5f;

        Vector2 videoNavigationHeaderAnchorTopStart = new Vector2(0, videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMax.y);
        Vector2 videoNavigationHeaderAnchorBottomStart = new Vector2(0, videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMin.y);
        Vector2 videoNavigationHeaderAnchorTopTarget = new Vector2(0, videoNavigationOffsetToBottom);
        Vector2 videoNavigationHeaderAnchorBottomTarget = new Vector2(0, videoNavigationOffsetToBottom);

        Vector2 videoNavigationFooterAnchorTopStart = new Vector2(0, videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMax.y);
        Vector2 videoNavigationFooterAnchorBottomStart = new Vector2(0, videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMin.y);
        Vector2 videoNavigationFooterAnchorTopTarget = new Vector2(0, -videoNavigationOffsetToBottom);
        Vector2 videoNavigationFooterAnchorBottomTarget = new Vector2(0, -videoNavigationOffsetToBottom);

        // Lerp helper variables
        float currentTime = 0;
        float timePassed = 0;
        float delta = Time.smoothDeltaTime;
        float moveTime = switchToFullscreenTime;
        float speedCurveFaktor = 3;
        timePassed = moveTime;

        // routine to lerp from start to end values
        while (timePassed > 0)
        {
            videoTarget.videoContainer.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(videoStartPosition, targetPosition, currentTime);
            videoTarget.videoContainer.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(videoStartSize, targetSizeVideoImage, currentTime);

            videoTarget.videoContainerBackground.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(videoStartPosition, targetPosition, currentTime);
            videoTarget.videoContainerBackground.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(videoStartSizeBackground, targetSizeVideoBackground, currentTime);

            videoTarget.videoContainer.transform.eulerAngles = Vector3.Lerp(startRotation, targetRotation, currentTime);

            videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMin = Vector2.Lerp(videoNavigationHeaderAnchorBottomStart, videoNavigationHeaderAnchorBottomTarget, currentTime);
            videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMax = Vector2.Lerp(videoNavigationHeaderAnchorTopStart, videoNavigationHeaderAnchorTopTarget, currentTime);

            videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMin = Vector2.Lerp(videoNavigationFooterAnchorBottomStart, videoNavigationFooterAnchorBottomTarget, currentTime);
            videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMax = Vector2.Lerp(videoNavigationFooterAnchorTopStart, videoNavigationFooterAnchorTopTarget, currentTime);

            // Lerp to end value
            if (timePassed > 0)
            {
                float faktor = Mathf.Pow(timePassed / moveTime, speedCurveFaktor);
                currentTime = 1 - faktor;
            }
            delta = Time.smoothDeltaTime;

            if (timePassed - delta < 0)
            {
                timePassed = 0;
            }
            else
            {
                timePassed -= delta;
            }
            yield return new WaitForEndOfFrame();
        }

        // Set end values
        videoTarget.videoContainer.GetComponent<RectTransform>().anchoredPosition = targetPosition;
        videoTarget.videoContainer.GetComponent<RectTransform>().sizeDelta = targetSizeVideoImage;
        videoTarget.videoContainerBackground.GetComponent<RectTransform>().anchoredPosition = targetPosition;
        videoTarget.videoContainerBackground.GetComponent<RectTransform>().sizeDelta = targetSizeVideoBackground;
        videoTarget.videoContainer.transform.eulerAngles = targetRotation;
        videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMin = videoNavigationHeaderAnchorBottomTarget;
        videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMax = videoNavigationHeaderAnchorTopTarget;
        videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMin = videoNavigationFooterAnchorBottomTarget;
        videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMax = videoNavigationFooterAnchorTopTarget;


        // +4 as offset to avoid white spaces at border of the screen because of unexpected scaling to fit the screen
        if (!videoTarget.isFullscreen)
        {
            videoTarget.videoContainerBackground.GetComponent<RectTransform>().sizeDelta = new Vector2(targetSizeVideoBackground.x + 4, targetSizeVideoBackground.y + 4);
        }
        else
        {
            videoTarget.videoContainerBackground.GetComponent<RectTransform>().sizeDelta = targetSizeVideoBackground;
        }

        // Reset anchors
        //videoTarget.videoContainer.GetComponent<RectTransform>().anchorMin = new Vector2( 0f, 0f );
        //videoTarget.videoContainer.GetComponent<RectTransform>().anchorMax = new Vector2( 1f, 1f );
        //videoTarget.videoContainerBackground.GetComponent<RectTransform>().anchorMin = new Vector2( 0f, 0f );
        //videoTarget.videoContainerBackground.GetComponent<RectTransform>().anchorMax = new Vector2( 1f, 1f );

        videoOrientation = ScreenOrientation.Portrait;
        videoTarget.isFullscreen = !videoTarget.isFullscreen;
        isSwitchingFullscreen = false;

        if (!videoTarget.isFullscreen)
        {
            videoTarget.videoCanvas.overrideSorting = false;
            if (videoTarget.videoMask != null) { videoTarget.videoMask.GetComponent<Mask>().enabled = true; }
        }

        if (videoTarget.parentScrollRect != null)
        {
            videoTarget.parentScrollRect.enabled = !videoTarget.isFullscreen;
        }
        else
        {
            videoTarget.parentScrollRect = videoTarget.GetComponentInParent<ScrollRect>();
            if (videoTarget.parentScrollRect != null)
            {
                videoTarget.parentScrollRect.enabled = !videoTarget.isFullscreen;
            }
        }

        if (videoTarget.isFullscreen)
        {
            videoTarget.disableFullscreenButton.SetActive(true);
            videoTarget.switchToFullscreenButton.SetActive(false);
        }
        else
        {
            videoTarget.switchToFullscreenButton.SetActive(true);
            videoTarget.disableFullscreenButton.SetActive(false);
        }
    }

    /************** helper function to get the center position for the videoImage if switching to fullscreen **************/
    public float GetLocalYPositionFromCanvasCenter(RectTransform rect, Transform content)
    {

        Vector3[] corners = new Vector3[4];
        rect.GetComponent<RectTransform>().GetWorldCorners(corners);
        for (int i = 0; i < 4; i++)
        {
            corners[i] = content.InverseTransformPoint(corners[i]);
        }

        float height = Mathf.Abs(corners[0].y - corners[1].y);
        float yCenter = -corners[0].y - (height * 0.5f);

        return rect.anchoredPosition.y + yCenter;

    }

    /************** helper function to get the right rotation for the videoImage if switching the device orientation **************/
    private Vector2 GetOrientationAngles(ScreenOrientation currentOrientation, ScreenOrientation targetOrientation)
    {

        switch (currentOrientation)
        {

            case ScreenOrientation.LandscapeLeft:
                if (targetOrientation == ScreenOrientation.PortraitUpsideDown)
                {
                    return new Vector2(270, 180);
                }
                else if (targetOrientation == ScreenOrientation.LandscapeRight)
                {
                    return new Vector2(270, 90);
                }
                else if (targetOrientation == ScreenOrientation.Portrait)
                {
                    return new Vector2(-90, 0);
                }
                else if (targetOrientation == ScreenOrientation.LandscapeLeft)
                {
                    return new Vector2(-90, -90);
                }
                break;
            case ScreenOrientation.LandscapeRight:
                if (targetOrientation == ScreenOrientation.PortraitUpsideDown)
                {
                    return new Vector2(90, 180);
                }
                else if (targetOrientation == ScreenOrientation.LandscapeRight)
                {
                    return new Vector2(90, 90);
                }
                else if (targetOrientation == ScreenOrientation.Portrait)
                {
                    return new Vector2(90, 0);
                }
                else if (targetOrientation == ScreenOrientation.LandscapeLeft)
                {
                    return new Vector2(90, -90);
                }

                break;
            case ScreenOrientation.Portrait:
                if (targetOrientation == ScreenOrientation.PortraitUpsideDown)
                {
                    return new Vector2(0, 180);
                }
                else if (targetOrientation == ScreenOrientation.LandscapeRight)
                {
                    return new Vector2(0, 90);
                }
                else if (targetOrientation == ScreenOrientation.Portrait)
                {
                    return new Vector2(0, 0);
                }
                else if (targetOrientation == ScreenOrientation.LandscapeLeft)
                {
                    return new Vector2(0, -90);
                }
                break;
            case ScreenOrientation.PortraitUpsideDown:
                if (targetOrientation == ScreenOrientation.PortraitUpsideDown)
                {
                    return new Vector2(180, 180);
                }
                else if (targetOrientation == ScreenOrientation.LandscapeRight)
                {
                    return new Vector2(180, 90);
                }
                else if (targetOrientation == ScreenOrientation.Portrait)
                {
                    return new Vector2(180, 0);
                }
                else if (targetOrientation == ScreenOrientation.LandscapeLeft)
                {
                    return new Vector2(180, 270);
                }
                break;
        }

        return Vector2.zero;
    }

    /************** function to check for device orientation change in fullscreen mode **************/
    private void UpdateOrientation(VideoTarget videoTarget)
    {

        if (isRotating || isSwitchingFullscreen || !videoTarget.isFullscreen) return;

#if UNITY_EDITOR
        currentScreenOrientation = testScreenOrientation;
#else
		
		if( Input.deviceOrientation == DeviceOrientation.Portrait ){
		currentScreenOrientation = ScreenOrientation.Portrait;
		}else if( Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown ){
		currentScreenOrientation = ScreenOrientation.PortraitUpsideDown;
		}else if( Input.deviceOrientation == DeviceOrientation.LandscapeLeft ){
		currentScreenOrientation = ScreenOrientation.LandscapeLeft;
		}else if( Input.deviceOrientation == DeviceOrientation.LandscapeRight ){
		currentScreenOrientation = ScreenOrientation.LandscapeRight;
		}
				
#endif

        if (videoOrientation != currentScreenOrientation)
        {
            isRotating = true;
            RotateVideo(videoTarget);
        }

    }

    /************** function to rotate the video image on device orientation change **************/
    private void RotateVideo(VideoTarget videoTarget)
    {
        DisableEventSystemForSeconds(switchOrientationTime + 0.02f);
        StartCoroutine(RotateVideoCoroutine(videoTarget));
    }

    /************** coroutine to rotate the video image on device orientation change **************/
    private IEnumerator RotateVideoCoroutine(VideoTarget videoTarget)
    {

        Vector2 orientationAngles = GetOrientationAngles(videoOrientation, currentScreenOrientation);
        Vector3 startRotation = new Vector3(0, 0, orientationAngles.x);
        Vector3 targetRotation = new Vector3(0, 0, orientationAngles.y);

        Vector2 startSize = new Vector2(videoTarget.videoContainer.GetComponent<RectTransform>().rect.width, videoTarget.videoContainer.GetComponent<RectTransform>().rect.height);
        Vector2 targetSize = startSize;

        if (currentScreenOrientation == ScreenOrientation.Portrait || currentScreenOrientation == ScreenOrientation.PortraitUpsideDown)
        {
            float ratio = videoTarget.videoContainer.GetComponent<RectTransform>().rect.height / videoTarget.videoContainer.GetComponent<RectTransform>().rect.width;
            float targetWidth = GetCanvasWidth();
            float targetHeight = ratio * targetWidth;

            if (targetHeight > GetCanvasHeight())
            {
                targetHeight = GetCanvasHeight();
                targetWidth = targetHeight / ratio;
            }
            targetSize = new Vector2(targetWidth, targetHeight);
        }
        else if (currentScreenOrientation == ScreenOrientation.LandscapeRight || currentScreenOrientation == ScreenOrientation.LandscapeLeft || currentScreenOrientation == ScreenOrientation.LandscapeLeft)
        {
            float ratio = videoTarget.videoContainer.GetComponent<RectTransform>().rect.height / videoTarget.videoContainer.GetComponent<RectTransform>().rect.width;
            float targetWidth = GetCanvasHeight();
            float targetHeight = ratio * targetWidth;

            if (targetHeight > GetCanvasWidth())
            {
                targetHeight = GetCanvasWidth();
                targetWidth = targetHeight / ratio;
            }

            targetSize = new Vector2(targetWidth, targetHeight);
        }


        // Calc target position of video navigation
        float videoNavigationOffsetToBottom = (GetCanvasHeight() - targetSize.y) * 0.5f;

        Vector2 videoNavigationFooterAnchorTopStart = new Vector2(0, videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMax.y);
        Vector2 videoNavigationFooterAnchorBottomStart = new Vector2(0, videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMin.y);
        if (currentScreenOrientation != ScreenOrientation.Portrait && currentScreenOrientation != ScreenOrientation.PortraitUpsideDown)
        {
            videoNavigationOffsetToBottom = (GetCanvasWidth() - targetSize.y) * 0.5f;
        }
        Vector2 videoNavigationFooterAnchorTopTarget = new Vector2(0, -videoNavigationOffsetToBottom);
        Vector2 videoNavigationFooterAnchorBottomTarget = new Vector2(0, -videoNavigationOffsetToBottom);

        Vector2 videoNavigationHeaderAnchorTopStart = new Vector2(0, videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMax.y);
        Vector2 videoNavigationHeaderAnchorBottomStart = new Vector2(0, videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMin.y);
        Vector2 videoNavigationHeaderAnchorTopTarget = new Vector2(0, videoNavigationOffsetToBottom);
        Vector2 videoNavigationHeaderAnchorBottomTarget = new Vector2(0, videoNavigationOffsetToBottom);


        float currentTime = 0;
        float timePassed = 0;
        float delta = Time.smoothDeltaTime;
        float moveTime = switchOrientationTime;
        float speedCurveFaktor = 3;
        timePassed = moveTime;

        while (timePassed > 0)
        {
            videoTarget.videoContainer.transform.eulerAngles = Vector3.Lerp(startRotation, targetRotation, currentTime);
            videoTarget.videoContainer.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(startSize, targetSize, currentTime);

            videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMin = Vector2.Lerp(videoNavigationHeaderAnchorBottomStart, videoNavigationHeaderAnchorBottomTarget, currentTime);
            videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMax = Vector2.Lerp(videoNavigationHeaderAnchorTopStart, videoNavigationHeaderAnchorTopTarget, currentTime);

            videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMin = Vector2.Lerp(videoNavigationFooterAnchorBottomStart, videoNavigationFooterAnchorBottomTarget, currentTime);
            videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMax = Vector2.Lerp(videoNavigationFooterAnchorTopStart, videoNavigationFooterAnchorTopTarget, currentTime);

            if (timePassed > 0)
            {
                float faktor = Mathf.Pow(timePassed / moveTime, speedCurveFaktor);
                currentTime = 1 - faktor;
            }

            delta = Time.smoothDeltaTime;

            if (timePassed - delta < 0)
            {
                timePassed = 0;
            }
            else
            {
                timePassed -= delta;
            }

            yield return new WaitForEndOfFrame();
        }

        videoTarget.videoContainer.transform.eulerAngles = targetRotation;
        videoTarget.videoContainer.GetComponent<RectTransform>().sizeDelta = targetSize;
        videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMin = videoNavigationHeaderAnchorBottomTarget;
        videoTarget.videoNavigationHeader.GetComponent<RectTransform>().offsetMax = videoNavigationHeaderAnchorTopTarget;
        videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMin = videoNavigationFooterAnchorBottomTarget;
        videoTarget.videoNavigationFooter.GetComponent<RectTransform>().offsetMax = videoNavigationFooterAnchorTopTarget;

        videoOrientation = currentScreenOrientation;
        isRotating = false;
    }


    /************************************************ Navigation options ******************************************************/

    /************** function to update video navigation time slider and time labels **************/
    private void UpdateNavigationTimer(VideoTarget videoTarget)
    {

        if (videoPlayer.isPlaying)
        {

            double videoTime = GetTotalTimeOfVideo();
            if (videoTime != 0 && !Double.IsNaN(videoTime))
            {

                if (!videoSliderSelected && !Double.IsNaN(videoPlayer.time))
                {

                    if (videoTarget.useNewTimerUI)
                    {
                        videoTarget.timerSlider.value = (float)(videoPlayer.time / videoTime);
                        videoTarget.currentTimeLabel.text = GetMinutes(videoPlayer.time) + ":" + GetSeconds(videoPlayer.time) + "<color=#9F9F9F> / " + GetMinutes(videoTime) + ":" + GetSeconds(videoTime);
                    }
                    else
                    {

                        videoTarget.timerSlider.value = (float)(videoPlayer.time / videoTime);
                        videoTarget.currentTimeLabel.text = GetMinutes(videoPlayer.time) + ":" + GetSeconds(videoPlayer.time);
                        videoTarget.leftTimeLabel.text = "-" + GetMinutes(videoTime - videoPlayer.time) + ":" + GetSeconds(videoTime - videoPlayer.time);
                    }
                }
            }
        }
    }

    /************** helper function to get the video time **************/
    public double GetTotalTimeOfVideo()
    {
        if (currentVideoTarget == null || (currentVideoTarget.videoURL == "" && currentVideoTarget.videoClip == null))
        {
            return 10;
        }

        double time = videoPlayer.frameCount / videoPlayer.frameRate;
        return time;
    }

    /************** helper function to get the video time minutes **************/
    public string GetMinutes(double time)
    {
        TimeSpan VideoUrlLength = TimeSpan.FromSeconds(time);
        if (VideoUrlLength.Minutes < 10) return VideoUrlLength.Minutes.ToString("0");
        return VideoUrlLength.Minutes.ToString("00");
    }

    /************** helper function to get the video time seconds **************/
    public string GetSeconds(double time)
    {
        TimeSpan VideoUrlLength = TimeSpan.FromSeconds(time);
        return VideoUrlLength.Seconds.ToString("00");
    }

    /************** function to change video time position by slider event (see in "TimeSliderEvent.cs") **************/
    public void SetVideoTimeBySlider(VideoTarget videoTarget)
    {

        float val = videoTarget.timerSlider.value;

        double videoTime = GetTotalTimeOfVideo();
        if (videoTime != 0 && videoPlayer.canSetTime)
        {
            seekCompleted = false;
            videoPlayer.seekCompleted -= VideoPlayer_seekCompleted;
            videoPlayer.seekCompleted += VideoPlayer_seekCompleted;
            videoPlayer.time = val * videoTime;
        }

        if (videoPlayer.isPlaying)
        {
            videoTarget.playButton.SetActive(false);
            videoTarget.pauseButton.SetActive(true);
        }

        StopCoroutine("WaitForSeekCompletedCoroutine");
        StartCoroutine("WaitForSeekCompletedCoroutine");
    }

    /************** coroutine to enable video time slider changes (videoSliderSelected = false) after seek to new time is completed **************/
    IEnumerator WaitForSeekCompletedCoroutine()
    {
        float timer = 2;
        while (!seekCompleted && timer > 0)
        {
            yield return null;
            timer -= Time.deltaTime;
        }

        yield return new WaitForEndOfFrame();
        videoSliderSelected = false;
        videoPlayer.seekCompleted -= VideoPlayer_seekCompleted;
    }

    /************** function to update the video navigation visiblity **************/
    private void UpdateVideoNavigationVisiblity(VideoTarget videoTarget)
    {

        if (Input.GetMouseButtonDown(0))
        {

            if (IsPointerOverGameObject(videoTarget.videoContainerBackground.gameObject, true))
            {

                videoNavigationAlpha = 1;
                currentNavigationSliderHideTime = 0;
            }
        }

        if (currentNavigationSliderHideTime > navigationSliderHideTime)
        {
            videoNavigationAlpha = 0;
            currentNavigationSliderHideTime = 0;
        }
        else
        {
            currentNavigationSliderHideTime += Time.deltaTime;
        }

        videoTarget.videoNavigationHeader.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(videoTarget.videoNavigationHeader.GetComponent<CanvasGroup>().alpha, videoNavigationAlpha, Time.deltaTime * showHideVideoNavigationSpeed);
        videoTarget.videoNavigationFooter.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(videoTarget.videoNavigationFooter.GetComponent<CanvasGroup>().alpha, videoNavigationAlpha, Time.deltaTime * showHideVideoNavigationSpeed);

    }

    /************** function to step forward 15 seconds **************/
    public void StepForward(VideoTarget videoTarget)
    {

        videoSliderSelected = true;
        double videoTime = GetTotalTimeOfVideo();
        float step = (float)(15 / videoTime);
        videoTarget.timerSlider.value = Mathf.Clamp(videoTarget.timerSlider.value + step, 0, 1);

        SetVideoTimeBySlider(videoTarget);
    }

    /************** function to step back 15 seconds **************/
    public void StepBack(VideoTarget videoTarget)
    {

        videoSliderSelected = true;
        double videoTime = GetTotalTimeOfVideo();
        float step = (float)(15 / videoTime);
        videoTarget.timerSlider.value = Mathf.Clamp(videoTarget.timerSlider.value - step, 0, 1);

        SetVideoTimeBySlider(videoTarget);

    }

    /************** helper function to stop click events for seconds to avoid side effects (for example during an ui animation) **************/
    public void DisableEventSystemForSeconds(float seconds)
    {
        if (!eventSystem.enabled) return;
        eventSystem.enabled = false;
        StartCoroutine(DisableEventSystemForSecondsCoroutine(seconds));
    }

    private IEnumerator DisableEventSystemForSecondsCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        eventSystem.enabled = true;
    }

    public float GetCanvasHeight()
    {
        return CanvasController.instance.GetContentHeight();
        //return canvas.GetComponent<RectTransform>().rect.height;
    }

    public float GetCanvasWidth()
    {
        return CanvasController.instance.GetContentWidth();
        //return canvas.GetComponent<RectTransform>().rect.width;
    }

    /************** helper function to check if a ui element gets hit **************/
    public bool IsPointerOverGameObject(GameObject obj, bool onFound = false)
    {

        if (obj == null) return false;

#if !UNITY_EDITOR
		
		if( Input.touchCount > 0 ){
		if ( EventSystem.current.IsPointerOverGameObject( Input.touches[0].fingerId ) )
		{
		PointerEventData pointer = new PointerEventData(EventSystem.current);
		pointer.position = Input.mousePosition;
	
		List<RaycastResult> raycastResults = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointer, raycastResults);
			
		int maxDepth = 0;
		int myDepth = 0;
		bool hit = false;
		if(raycastResults.Count > 0)
		{
		foreach(var go in raycastResults)
		{  	
		
		if( onFound && go.gameObject == obj ){
		return true;
		}
		
		if( go.depth > maxDepth && !go.gameObject.transform.IsChildOf(obj.transform) ){
		maxDepth = go.depth;
		}
		if( go.gameObject == obj ) {
		myDepth = go.depth;
		hit = true;
		}
		}
		}
			
		if( hit && maxDepth <= myDepth){
		return true;
		}
		}
		}
		
		
#else

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, raycastResults);

            int maxDepth = 0;
            int myDepth = 0;
            bool hit = false;
            if (raycastResults.Count > 0)
            {
                foreach (var go in raycastResults)
                {
                    //print(go.gameObject.name);

                    if (onFound && go.gameObject == obj)
                    {
                        return true;
                    }

                    if (go.depth > maxDepth && !go.gameObject.transform.IsChildOf(obj.transform))
                    {
                        maxDepth = go.depth;
                    }
                    if (go.gameObject == obj)
                    {
                        myDepth = go.depth;
                        hit = true;
                    }
                }
            }

            if (hit && maxDepth <= myDepth)
            {
                return true;
            }
        }

#endif

        return false;
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            appWasPaused = true;
        }
    }

    public void AdjustVideoRatio(JSONNode featureData)
    {
        if (featureData["videoWidth"] != null && featureData["videoHeight"] != null)
        {
            float aspectRatio = featureData["videoWidth"].AsFloat / featureData["videoHeight"].AsFloat;
            for (int i = 0; i < screenAspectRatioFitter.Count; i++) { screenAspectRatioFitter[i].aspectRatio = aspectRatio; }
        }
        else if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("greenscreen"))
        {
            float aspectRatio = 0.5625f;
            for (int i = 0; i < screenAspectRatioFitter.Count; i++) { screenAspectRatioFitter[i].aspectRatio = aspectRatio; }
        }
        else
        {
            float aspectRatio = 1.777777f;
            for (int i = 0; i < screenAspectRatioFitter.Count; i++) { screenAspectRatioFitter[i].aspectRatio = aspectRatio; }
        }

        mainUIVideoTarget.targetImage.transform.localScale = new Vector3(1, 1, 1);
        if (featureData["id"] != null && featureData["id"].Value == "guide")
        {
            bool setToFullscreenRatio = true;
            if (setToFullscreenRatio)
            {
                float videoAspectRatio = 1080f / 1920f;
                float screenRatio = (float)Screen.width / (float)Screen.height;
                if (Screen.width > Screen.height) { screenRatio = (float)Screen.height / (float)Screen.width; }
                for (int i = 0; i < screenAspectRatioFitterMainVideo.Count; i++) { screenAspectRatioFitterMainVideo[i].aspectRatio = screenRatio; }

                float sXZ = 1;
                float sY = screenRatio / videoAspectRatio;
                if (sY > 1) { sXZ = 1f / sY; sY = 1f; }
                mainUIVideoTarget.targetImage.transform.localScale = new Vector3(sXZ, sY, sXZ);
            }
            else
            {
                // We need to use Portrait resolution to fit the Screen
                if (featureData["videoWidthMainVideo"] != null && featureData["videoHeightMainVideo"] != null)
                {
                    float aspectRatio = featureData["videoWidthMainVideo"].AsFloat / featureData["videoHeightMainVideo"].AsFloat;
                    for (int i = 0; i < screenAspectRatioFitterMainVideo.Count; i++) { screenAspectRatioFitterMainVideo[i].aspectRatio = aspectRatio; }
                }
                else
                {
                    float videoAspectRatio = 1080f / 1920f;
                    for (int i = 0; i < screenAspectRatioFitterMainVideo.Count; i++) { screenAspectRatioFitterMainVideo[i].aspectRatio = videoAspectRatio; }
                }
            }
        }
    }

    public void UpdateUVRect(JSONNode featureData)
    {
        currentVideoTarget.targetImage.uvRect = new Rect(-0.25f, 0, 1, 1);
        mainUIVideoTarget.targetImage.uvRect = new Rect(-0.25f, 0, 1, 1);

        if (featureData["id"] != null && featureData["id"].Value == "guide")
        {
            float x = 0;
            float y = 0;
            float w = 1;
            float h = 1;
            if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("greenscreen"))
            {
                currentVideoTarget.targetImage.uvRect = new Rect(0, 0, 1, 1);

                x = featureData["fullScreenUVRectX"] != null ? featureData["fullScreenUVRectX"].AsFloat : 0.03f;
                y = featureData["fullScreenUVRectY"] != null ? featureData["fullScreenUVRectY"].AsFloat : 0.06f;
                w = featureData["fullScreenUVRectWidth"] != null ? featureData["fullScreenUVRectWidth"].AsFloat : 1f;
                h = featureData["fullScreenUVRectHeight"] != null ? featureData["fullScreenUVRectHeight"].AsFloat : 1f;
            }
            else
            {
                x = featureData["fullScreenUVRectX"] != null ? featureData["fullScreenUVRectX"].AsFloat : 0.06f;
                y = featureData["fullScreenUVRectY"] != null ? featureData["fullScreenUVRectY"].AsFloat : -0.08f;
                w = featureData["fullScreenUVRectWidth"] != null ? featureData["fullScreenUVRectWidth"].AsFloat : 0.38f;
                h = featureData["fullScreenUVRectHeight"] != null ? featureData["fullScreenUVRectHeight"].AsFloat : 1.2f;
            }

            mainUIVideoTarget.targetImage.uvRect = new Rect(x, y, w, h);
        }
    }

    public void AdjustVideoRatio(float aspectRatio)
    {
        for (int i = 0; i < screenAspectRatioFitter.Count; i++) { screenAspectRatioFitter[i].aspectRatio = aspectRatio; }
    }

    public float GetVideoRatio()
    {
        if (screenAspectRatioFitter.Count <= 0) { return 16f / 9f; }
        return screenAspectRatioFitter[0].aspectRatio;
    }

    public void AdjustVideoScale(JSONNode featureData)
    {
        if (featureData["videoScale"] != null) { videoOffset.transform.GetChild(0).localScale = Vector3.one * featureData["videoScale"].AsFloat; }
        else if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("greenscreen"))
        {
            videoOffset.transform.GetChild(0).localScale = Vector3.one * Params.greenscreenVideoScale;
        }
        else
        {
            videoOffset.transform.GetChild(0).localScale = Vector3.one * Params.sideBySideVideoScale;
        }
    }

    public void AdjustVideoPosition(JSONNode featureData)
    {
        float x = featureData["videoXOffset"] == null ? 0 : featureData["videoXOffset"].AsFloat;
        float y = featureData["videoYOffset"] == null ? 0 : featureData["videoYOffset"].AsFloat;

        if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("greenscreen"))
        {
            y = Params.greenscreenVideoYOffset;
        }
        else
        {
            y = Params.sideBySideVideoYOffset;
        }

        videoOffset.transform.localPosition = new Vector3(0, y, 0);
        videoOffset.transform.GetChild(0).localPosition = new Vector3(x, 0, 0);
    }

    public IEnumerator ShowVideoCoroutine(JSONNode featureData)
    {
        if (featureData["videoAppearance"] != null)
        {
            switch (featureData["videoAppearance"].Value)
            {
                case "fade":

                    StartCoroutine(
                        AnimationController.instance.AnimateCanvasGroupAlphaCoroutine(
                            videoCanvas.GetComponent<CanvasGroup>(), 0, 1, 2.0f, "smooth"
                        )
                    );

                    if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("greenscreen"))
                    {
                        StartCoroutine(
                        AnimationController.instance.AnimateMaterialColorPropertyCoroutine(mainUIVideoTarget.targetImage, "_BaseColor", new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), 1.0f, "smooth"));

                        yield return StartCoroutine(
                        AnimationController.instance.AnimateMaterialColorPropertyCoroutine(shadowImage_Greenscreen.GetComponent<RawImage>(), "_BaseColor", new Color(0, 0, 0, 0), new Color(0, 0, 0, 90f / 255f), 1.0f, "smooth"));
                    }
                    else
                    {
                        StartCoroutine(AnimationController.instance.AnimateMaterialPropertyCoroutine(mainUIVideoTarget.targetImage, "_Alpha", 0, 1, 1.0f, "smooth"));

                        yield return StartCoroutine(
                        AnimationController.instance.AnimateMaterialPropertyCoroutine(shadowImage_SideBySide.GetComponent<RawImage>(), "_Alpha", 0, 1, 1.0f, "smooth"));
                    }

                    break;

                case "scale":

                    yield return StartCoroutine(
                        AnimationController.instance.AnimateScaleCoroutine(
                            videoRoot.transform, Vector3.zero, Vector3.one, 2.0f, "smooth"
                        )
                    );

                    break;

                default: break;
            }
        }
    }

    public IEnumerator ShowVideoCoroutine()
    {
        StartCoroutine(
            AnimationController.instance.AnimateCanvasGroupAlphaCoroutine(
                videoCanvas.GetComponent<CanvasGroup>(), 0, 1, 2.0f, "smooth"
            )
        );

        if (currentVideoTarget.videoURL.ToLower().Contains("greenscreen"))
        {
            StartCoroutine(
            AnimationController.instance.AnimateMaterialColorPropertyCoroutine(mainUIVideoTarget.targetImage, "_BaseColor", new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), 1.0f, "smooth"));

            yield return StartCoroutine(
            AnimationController.instance.AnimateMaterialColorPropertyCoroutine(shadowImage_Greenscreen.GetComponent<RawImage>(), "_BaseColor", new Color(0, 0, 0, 0), new Color(0, 0, 0, 90f / 255f), 1.0f, "smooth"));
        }
        else
        {
            StartCoroutine(
            AnimationController.instance.AnimateMaterialPropertyCoroutine(mainUIVideoTarget.targetImage, "_Alpha", 0, 1, 1.0f, "smooth"));

            yield return StartCoroutine(
            AnimationController.instance.AnimateMaterialPropertyCoroutine(shadowImage_SideBySide.GetComponent<RawImage>(), "_Alpha", 0, 1, 1.0f, "smooth"));
        }
    }

    public void EnableFullscreen()
    {
        videoSite.SetActive(true);
        mainUIVideoTarget.targetImage.gameObject.SetActive(true);
        mainUIVideoTarget.videoNavigationFooter.SetActive(true);

        if (!mainUIVideoTarget.isFullscreen)
        {
            //SwitchFullscreen(mainUIVideoTarget);  // FullscreenMode (Old)
        }
    }

    public void DisableFullscreen()
    {
        videoSite.SetActive(false);
    }

	public void StopVideo()
	{
		Debug.Log( "StopVideo" );

		videoPlayer.Pause();
		//videoPlayer.Stop();
	}
}

