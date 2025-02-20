using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoContainerBackground;
    public GameObject videoNavigationFooter;
    public RawImage videoImage;
    public GameObject playButton;
    public GameObject pauseButton;
    public Slider timerSlider;
    public TextMeshProUGUI currentTimeLabel;
    public List<AspectRatioFitter> screenAspectRatioFitter = new List<AspectRatioFitter>();
    public bool videoSliderSelected = false;
    public bool continueTutorial = true;

    private float showHideVideoNavigationSpeed = 10f;
    private float navigationSliderHideTime = 4;
    private float currentNavigationSliderHideTime = 0;
    private float videoNavigationAlpha = 0;
    private bool frameIsReady = false;
    private bool videoIsPrepared = false;
    private bool seekCompleted = false;
    private bool isLoading = false;

    public static IntroVideoController instance;
    void Awake()
    {
        instance = this;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.loopPointReached += VideoPlayer_loopPointReached;
    }

    void Update()
    {
        if (SiteController.instance != null && SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "IntroVideoSite") return;
        UpdateNavigationTimer();
        UpdateVideoNavigationVisiblity();
    }

    private void UpdateNavigationTimer()
    {
        if (videoPlayer.isPlaying)
        {
            double videoTime = GetTotalTimeOfVideo();
            if (videoTime != 0 && !Double.IsNaN(videoTime))
            {
                if (!videoSliderSelected && !Double.IsNaN(videoPlayer.time))
                {
                    timerSlider.value = (float)(videoPlayer.time / videoTime);
                    currentTimeLabel.text = GetMinutes(videoPlayer.time) + ":" + GetSeconds(videoPlayer.time) + "<color=#9F9F9F> / " + GetMinutes(videoTime) + ":" + GetSeconds(videoTime);
                }
            }
        }
    }

    public double GetTotalTimeOfVideo()
    {
        double time = videoPlayer.frameCount / videoPlayer.frameRate;
        return time;
    }

    public string GetMinutes(double time)
    {
        TimeSpan VideoUrlLength = TimeSpan.FromSeconds(time);
        if (VideoUrlLength.Minutes < 10) return VideoUrlLength.Minutes.ToString("0");
        return VideoUrlLength.Minutes.ToString("00");
    }

    public string GetSeconds(double time)
    {
        TimeSpan VideoUrlLength = TimeSpan.FromSeconds(time);
        return VideoUrlLength.Seconds.ToString("00");
    }

    private void UpdateVideoNavigationVisiblity()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (ToolsController.instance.IsPointerOverGameObject(videoContainerBackground, true))
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

        videoNavigationFooter.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(videoNavigationFooter.GetComponent<CanvasGroup>().alpha, videoNavigationAlpha, Time.deltaTime * showHideVideoNavigationSpeed);
    }

    public void SetVideoTimeBySlider()
    {
        float val = timerSlider.value;

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
            playButton.SetActive(false);
            pauseButton.SetActive(true);
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

    private void VideoPlayer_loopPointReached(VideoPlayer source)
    {
        Close();
    }

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

    public IEnumerator InitCoroutine()
    {
        //videoImage.material = VideoController.instance.overUnderAlphaMaterial;
        //videoImage.material.SetFloat("_Alpha", 1);

        //AdjustVideoRatio();
        //UpdateUVRect();

        yield return StartCoroutine("PlayVideoCoroutine");
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("IntroVideoSite"));
    }

    public void AdjustVideoRatio()
    {
        videoImage.transform.localScale = new Vector3(1, 1, 1);
        bool setToFullscreenRatio = true;
        if (setToFullscreenRatio)
        {
            float videoAspectRatio = 1080f / 1920f;
            float screenRatio = (float)Screen.width / (float)Screen.height;
            if (Screen.width > Screen.height) { screenRatio = (float)Screen.height / (float)Screen.width; }
            for (int i = 0; i < screenAspectRatioFitter.Count; i++) { screenAspectRatioFitter[i].aspectRatio = screenRatio; }

            float sXZ = 1;
            float sY = screenRatio / videoAspectRatio;
            if (sY > 1) { sXZ = 1f / sY; sY = 1f; }
            videoImage.transform.localScale = new Vector3(sXZ, sY, sXZ);
        }
        else
        {
            float videoAspectRatio = 1080f / 1920f;
            for (int i = 0; i < screenAspectRatioFitter.Count; i++) { screenAspectRatioFitter[i].aspectRatio = videoAspectRatio; }
        }
    }

    public void UpdateUVRect()
    {
        float x = 0.06f;
        float y = -0.06f;
        float w = 0.38f;
        float h = 1.2f;
        videoImage.uvRect = new Rect(x, y, w, h);
    }

    IEnumerator PlayVideoCoroutine()
    {
        if (videoPlayer.isPlaying)
		{
			//videoPlayer.Stop();
			videoPlayer.Pause();
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

        pauseButton.SetActive(true);
        playButton.SetActive(false);

        videoImage.texture = videoPlayer.texture;
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

    public void PlayPauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            pauseButton.SetActive(false);
            playButton.SetActive(true);
        }
        else
        {
            videoPlayer.Play();
            pauseButton.SetActive(true);
            playButton.SetActive(false);
        }
    }

    public void Close()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(CloseCoroutine());
    }

    public IEnumerator CloseCoroutine()
    {
        Reset();

        if (continueTutorial)
        {
            if (TutorialController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("TutorialSite")); }
            yield return StartCoroutine(TutorialController.instance.InitTutorialCoroutine());
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("TutorialSite"));
        }
        else
        {
            if (SiteController.instance.previousSite != null)
            {
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine(SiteController.instance.previousSite.siteID));
            }
            else
            {
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));
            }
        }

        isLoading = false;
    }

    public void Reset()
    {
        StopCoroutine("PlayVideoCoroutine");

        //videoPlayer.Stop();
		videoPlayer.Pause();
	}
}
