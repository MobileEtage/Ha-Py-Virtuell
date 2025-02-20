using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using MPUIKIT;
using SimpleJSON;
using UnityEngine.Video;

public class GuideController : MonoBehaviour
{
    public Transform mainCamera;
    public AudioSource beamAudio;

    [Space(10)]

    public GameObject arImage;
    public TextMeshProUGUI arLabel;
    public GameObject videoImage;
    public TextMeshProUGUI videoLabel;
    public GameObject audioImage;
    public TextMeshProUGUI audioLabel;
    public GameObject textImage;
    public TextMeshProUGUI textLabel;

    [Space(10)]

    public GameObject guideOptions;
    public GameObject textContent;
    public TextMeshProUGUI title;
    public TextMeshProUGUI subTitle;
    public TextMeshProUGUI description;
    public GameObject textInfoCircle;
    public Image videoBackgroundImage;
    public GameObject guideAudioImage;

    private bool isLoading = false;
    private bool langInfoShowed = false;
    private string currentViewType = "ar";

    public static GuideController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Init();
    }

    public void Init()
    {
        arImage.GetComponent<Image>().color = Params.guideMenuButtonActiveColor;
        videoImage.GetComponent<Image>().color = Params.guideMenuButtonActiveColor;
        audioImage.GetComponent<Image>().color = Params.guideMenuButtonActiveColor;
        textImage.GetComponent<Image>().color = Params.guideMenuButtonActiveColor;
    }

    public IEnumerator LoadGuideCoroutine()
    {
        JSONNode featureData = StationController.instance.GetStationFeature("guide");
        if (featureData == null) yield break;
        print("Loading guide video " + featureData["video"].Value);

        InfoController.instance.loadingCircle.SetActive(true);
        if (!ARController.instance.arSession.enabled)
        {
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
        }

        InitOptions();
        MarkButtonViewType("ar");
        currentViewType = "ar";

        // Set URL
        if (featureData["localVideo"] != null)
        {
            VideoClip videoClip = Resources.Load<VideoClip>(featureData["localVideo"].Value);
            VideoController.instance.currentVideoTarget.videoType = VideoTarget.VideoType.VideoClip;
            VideoController.instance.currentVideoTarget.videoClip = videoClip;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoType = VideoTarget.VideoType.VideoClip;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoClip = videoClip;

            //VideoController.instance.videoPlayer.source = UnityEngine.Video.VideoSource.VideoClip;
            //VideoController.instance.videoPlayer.clip = videoClip;
        }
        else
        {
            VideoController.instance.currentVideoTarget.videoType = VideoTarget.VideoType.URL;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoType = VideoTarget.VideoType.URL;
            VideoController.instance.currentVideoTarget.videoURL = DownloadContentController.instance.GetVideoFile(featureData["video"]);
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoURL = DownloadContentController.instance.GetVideoFile(featureData["video"]);
        }

        // Update video params
        //VideoController.instance.videoRoot.transform.position = ScanController.instance.currentMarkerPosition;
        UpdateVideoPosition();
        VideoController.instance.AdjustVideoRatio(featureData);
        VideoController.instance.AdjustVideoScale(featureData);
        VideoController.instance.AdjustVideoPosition(featureData);

        // Start play video
        VideoController.instance.currentVideoTarget.isLoaded = false;
        VideoController.instance.PlayVideo();

        // Wait until loaded
        float timer = 10;
        while (!VideoController.instance.currentVideoTarget.isLoaded && timer > 0)
        {
            timer -= Time.deltaTime;
            //print("Loading video");
            yield return null;
        }

        // Enable options and show video
        guideOptions.SetActive(true);
        VideoController.instance.videoCanvas.SetActive(true);
        VideoController.instance.currentVideoTarget.targetImage.uvRect = new Rect(0, 0, 1, 1);
        VideoController.instance.mainUIVideoTarget.targetImage.uvRect = new Rect(0, 0, 1, 1);
        VideoController.instance.backgroundImage.SetActive(false);

        // Video background image
        if (featureData["videoBackground"] != null && featureData["videoBackground"].Value != "")
        {
            VideoController.instance.backgroundImage.SetActive(true);
            ToolsController.instance.ApplyOnlineImage(videoBackgroundImage, featureData["videoBackground"].Value, true);
        }
        else
        {
            VideoController.instance.backgroundImage.SetActive(false);
        }

        // Audio background image
        if (featureData["audioBackground"] != null && featureData["audioBackground"].Value != "")
        {
            guideAudioImage.SetActive(true);
            ToolsController.instance.ApplyOnlineImage(guideAudioImage.GetComponent<Image>(), featureData["audioBackground"].Value, true);
        }
        else
        {
            guideAudioImage.SetActive(false);
        }


        if (featureData["useGreenscreenShader"] != null && featureData["useGreenscreenShader"].AsBool)
        {
            VideoController.instance.currentVideoTarget.targetImage.material = VideoController.instance.greenscreenMaterial;
            VideoController.instance.mainUIVideoTarget.targetImage.material = VideoController.instance.greenscreenMaterial;
            VideoController.instance.UpdateUVRect(featureData);
        }
        else if (featureData["useAlphaShader"] != null && featureData["useAlphaShader"].AsBool)
        {
            VideoController.instance.currentVideoTarget.targetImage.material = VideoController.instance.overUnderAlphaMaterial;
            VideoController.instance.mainUIVideoTarget.targetImage.material = VideoController.instance.overUnderAlphaMaterial;
            VideoController.instance.UpdateUVRect(featureData);
        }
        else if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("greenscreen"))
        {
            VideoController.instance.currentVideoTarget.targetImage.material = VideoController.instance.greenscreenMaterial;
            VideoController.instance.mainUIVideoTarget.targetImage.material = VideoController.instance.greenscreenMaterial;
            VideoController.instance.UpdateUVRect(featureData);
        }
        else if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("no_alpha_keying"))
        {
            VideoController.instance.currentVideoTarget.targetImage.material = null;
            VideoController.instance.mainUIVideoTarget.targetImage.material = null;
        }
        else
        {
            VideoController.instance.currentVideoTarget.targetImage.material = VideoController.instance.overUnderAlphaMaterial;
            VideoController.instance.mainUIVideoTarget.targetImage.material = VideoController.instance.overUnderAlphaMaterial;
            VideoController.instance.UpdateUVRect(featureData);
        }

        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "ARSite")
        {
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ARSite"));
            ARMenuController.instance.DisableMenu(true);
        }
        InfoController.instance.loadingCircle.SetActive(false);

        StartCoroutine(VideoController.instance.ShowVideoCoroutine());
        beamAudio.Play();
    }

    public IEnumerator LoadGuideVideoCoroutine()
    {
        JSONNode featureData = StationController.instance.GetStationFeature("guide");
        if (featureData == null) yield break;
        print("Loading guide video " + featureData["video"].Value);

        InfoController.instance.loadingCircle.SetActive(true);

        InitOptions();
        MarkButtonViewType("video");
        currentViewType = "video";

        // Set URL
        if (featureData["localVideo"] != null)
        {
            VideoClip videoClip = Resources.Load<VideoClip>(featureData["localVideo"].Value);
            VideoController.instance.currentVideoTarget.videoType = VideoTarget.VideoType.VideoClip;
            VideoController.instance.currentVideoTarget.videoClip = videoClip;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoType = VideoTarget.VideoType.VideoClip;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoClip = videoClip;
        }
        else
        {
            VideoController.instance.currentVideoTarget.videoType = VideoTarget.VideoType.URL;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoType = VideoTarget.VideoType.URL;
            VideoController.instance.currentVideoTarget.videoURL = DownloadContentController.instance.GetVideoFile(featureData["video"]);
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoURL = DownloadContentController.instance.GetVideoFile(featureData["video"]);
        }

        // Update video params
        VideoController.instance.AdjustVideoRatio(featureData);
        VideoController.instance.AdjustVideoScale(featureData);
        VideoController.instance.AdjustVideoPosition(featureData);

        // Start play video
        VideoController.instance.currentVideoTarget.isLoaded = false;
        VideoController.instance.PlayVideo();

        // Wait until loaded
        float timer = 10;
        while (!VideoController.instance.currentVideoTarget.isLoaded && timer > 0)
        {
            timer -= Time.deltaTime;
            //print("Loading video");
            yield return null;
        }

        // Enable options and show video
        guideOptions.SetActive(true);
        VideoController.instance.currentVideoTarget.targetImage.uvRect = new Rect(0, 0, 1, 1);
        VideoController.instance.mainUIVideoTarget.targetImage.uvRect = new Rect(0, 0, 1, 1);
        VideoController.instance.mainUIVideoTarget.targetImage.enabled = true;
        VideoController.instance.EnableFullscreen();

        // Video background image
        if (featureData["videoBackgroundLocal"] != null && featureData["videoBackgroundLocal"].Value != "")
        {
            Sprite sprite = Resources.Load<Sprite>(featureData["videoBackgroundLocal"].Value);
            videoBackgroundImage.sprite = sprite;
            videoBackgroundImage.GetComponent<AspectRatioFitter>().enabled = true;
            videoBackgroundImage.GetComponent<AspectRatioFitter>().aspectRatio = sprite.bounds.size.x / sprite.bounds.size.y;
            VideoController.instance.backgroundImage.SetActive(true);
        }
        else if (featureData["videoBackground"] != null && featureData["videoBackground"].Value != "")
        {
            VideoController.instance.backgroundImage.SetActive(true);
            ToolsController.instance.ApplyOnlineImage(videoBackgroundImage, featureData["videoBackground"].Value, true);
        }
        else { VideoController.instance.backgroundImage.SetActive(false); }

        // Audio background image
        if (featureData["audioBackgroundLocal"] != null && featureData["audioBackgroundLocal"].Value != "")
        {
            Sprite sprite = Resources.Load<Sprite>(featureData["audioBackgroundLocal"].Value);
            guideAudioImage.GetComponent<Image>().sprite = sprite;
            guideAudioImage.GetComponent<AspectRatioFitter>().enabled = true;
            guideAudioImage.GetComponent<AspectRatioFitter>().aspectRatio = sprite.bounds.size.x / sprite.bounds.size.y;
            guideAudioImage.SetActive(true);
        }
        else if (featureData["audioBackground"] != null && featureData["audioBackground"].Value != "")
        {
            guideAudioImage.SetActive(true);
            ToolsController.instance.ApplyOnlineImage(guideAudioImage.GetComponent<Image>(), featureData["audioBackground"].Value, true);
        }
        else { guideAudioImage.SetActive(false); }

        if (featureData["useBackgroundImage"] != null && featureData["useBackgroundImage"].AsBool)
        {
            VideoController.instance.backgroundImage.SetActive(true);
            // Optional: Load image
        }

        if (featureData["useGreenscreenShader"] != null && featureData["useGreenscreenShader"].AsBool)
        {

            VideoController.instance.currentVideoTarget.targetImage.material = VideoController.instance.greenscreenMaterial;
            VideoController.instance.mainUIVideoTarget.targetImage.material = VideoController.instance.greenscreenMaterial;
            VideoController.instance.UpdateUVRect(featureData);
        }
        else if (featureData["useAlphaShader"] != null && featureData["useAlphaShader"].AsBool)
        {
            VideoController.instance.currentVideoTarget.targetImage.material = VideoController.instance.overUnderAlphaMaterial;
            VideoController.instance.mainUIVideoTarget.targetImage.material = VideoController.instance.overUnderAlphaMaterial;
            VideoController.instance.UpdateUVRect(featureData);
        }
        else if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("greenscreen"))
        {
            VideoController.instance.currentVideoTarget.targetImage.material = VideoController.instance.greenscreenMaterial;
            VideoController.instance.mainUIVideoTarget.targetImage.material = VideoController.instance.greenscreenMaterial;
            VideoController.instance.UpdateUVRect(featureData);
        }
        else if (featureData["video"] != null && featureData["video"].Value.ToLower().Contains("no_alpha_keying"))
        {
            VideoController.instance.currentVideoTarget.targetImage.material = null;
            VideoController.instance.mainUIVideoTarget.targetImage.material = null;
        }
        else
        {
            VideoController.instance.currentVideoTarget.targetImage.material = VideoController.instance.overUnderAlphaMaterial;
            VideoController.instance.mainUIVideoTarget.targetImage.material = VideoController.instance.overUnderAlphaMaterial;
            VideoController.instance.UpdateUVRect(featureData);
        }

        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "ARSite")
        {
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ARSite"));
            //ARMenuController.instance.DisableMenu(true);
        }
        InfoController.instance.loadingCircle.SetActive(false);

        StartCoroutine(VideoController.instance.ShowVideoCoroutine());
        beamAudio.Play();
    }

    public void InitOptions()
    {
        arImage.transform.parent.gameObject.SetActive(true);
        videoImage.transform.parent.gameObject.SetActive(true);
        audioImage.transform.parent.gameObject.SetActive(true);
        textImage.transform.parent.gameObject.SetActive(true);

        JSONNode featureData = StationController.instance.GetStationFeature("guide");
        if (featureData == null) return;

        bool hasAR = (featureData["useGuideInAR"] != null) ? featureData["useGuideInAR"].AsBool : true;
        bool hasVideo = (featureData["hasVideo"] != null) ? featureData["hasVideo"].AsBool : true;
        bool hasAudio = (featureData["hasAudio"] != null) ? featureData["hasAudio"].AsBool : true;
        bool hasText = (featureData["hasText"] != null) ? featureData["hasText"].AsBool : false;
        if (!ScanController.instance.useGuideInAR) hasAR = false;

        List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        if (featureData["title"] != null) { title.text = LanguageController.GetTranslationFromNode(featureData["title"]); labels.Add(title); hasText = true; }
        else { title.text = ""; }
        if (featureData["subTitle"] != null) { subTitle.text = LanguageController.GetTranslationFromNode(featureData["subTitle"]); labels.Add(subTitle); }
        else { subTitle.text = ""; }
        if (featureData["description"] != null) { description.text = LanguageController.GetTranslationFromNode(featureData["description"]); labels.Add(description); }
        else { description.text = ""; }
        PoiInfoController.instance.labels = labels;

        arImage.transform.parent.gameObject.SetActive(hasAR);
        videoImage.transform.parent.gameObject.SetActive(hasVideo);
        audioImage.transform.parent.gameObject.SetActive(hasAudio);
        textImage.transform.parent.gameObject.SetActive(hasText);

        if (!langInfoShowed && LanguageController.GetLanguageCode() != "de")
        {
            if (hasText)
            {
                langInfoShowed = true;
                StartCoroutine(ShowTranslationInfoCoroutine());
            }
        }
    }

    public IEnumerator ShowTranslationInfoCoroutine()
    {
        yield return new WaitForSeconds(1.0f);
        InfoController.instance.ShowMessage("TRANSLATION AVAILABLE", "The spoken text\nis available in your language\nunder \"Text\"", CommitTextInfo);
        textInfoCircle.SetActive(true);
    }

    public void CommitTextInfo() { textInfoCircle.SetActive(false); }

    public void SelectViewType(string viewType)
    {
        if (viewType == currentViewType) return;

        if (isLoading) return;
        isLoading = true;
        StartCoroutine(SelectViewTypeCoroutine(viewType));
    }

    public IEnumerator SelectViewTypeCoroutine(string viewType)
    {
        currentViewType = viewType;
        MarkButtonViewType(viewType);
        SpeechController.instance.Reset();

        if (viewType == "ar")
        {
            InfoController.instance.loadingCircle.SetActive(true);
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
            InfoController.instance.loadingCircle.SetActive(false);

            VideoController.instance.DisableFullscreen();

            // Todo: Maybe we need to scan ground first
            //VideoController.instance.videoRoot.transform.position = ScanController.instance.currentMarkerPosition;
            UpdateVideoPosition();
            VideoController.instance.videoCanvas.SetActive(true);
            AudioVisualizer.instance.DisableAudioVisualizer();

            textContent.SetActive(false);
            if (!VideoController.instance.videoPlayer.isPlaying)
            {

                VideoController.instance.videoPlayer.Play();
                VideoController.instance.main3DVideoTarget.playButton.SetActive(false);
            }
        }
        else if (viewType == "video")
        {
            AudioVisualizer.instance.DisableAudioVisualizer();
            VideoController.instance.mainUIVideoTarget.targetImage.enabled = true;
            VideoController.instance.videoCanvas.SetActive(false);
            VideoController.instance.EnableFullscreen();

            //ARController.instance.StopARSession();
            ARController.instance.DisableARSession();

            textContent.SetActive(false);
            if (!VideoController.instance.videoPlayer.isPlaying)
            {

                VideoController.instance.videoPlayer.Play();
                VideoController.instance.main3DVideoTarget.playButton.SetActive(false);
            }
        }
        else if (viewType == "audio")
        {
            //AudioVisualizer.instance.EnableAudioVisualizer(VideoController.instance.GetComponentInChildren<AudioSource>(true));
            AudioVisualizer.instance.EnableAudioVisualizer(VideoController.instance.videoPlayer.GetTargetAudioSource(0));
            //AudioVisualizer.instance.SetCameraPositionY(-300);
            AudioVisualizer.instance.SetCameraPositionY(0);

            VideoController.instance.mainUIVideoTarget.targetImage.enabled = false;
            VideoController.instance.videoCanvas.SetActive(false);
            VideoController.instance.EnableFullscreen();

            //ARController.instance.StopARSession();
            ARController.instance.DisableARSession();

            textContent.SetActive(false);
            if (!VideoController.instance.videoPlayer.isPlaying)
            {

                VideoController.instance.videoPlayer.Play();
                VideoController.instance.main3DVideoTarget.playButton.SetActive(false);
            }
        }
        else if (viewType == "text")
        {
            textContent.SetActive(true);
            VideoController.instance.videoPlayer.Pause();
            ARController.instance.StopARSession();
        }

        isLoading = false;
    }

    public void UpdateVideoPosition()
    {
        // Optional: Place depending on raycastHit with certain distance
        /*
        float minDist = 3.0f;
        Vector2 touchPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector3 hitPosition = mainCamera.position + mainCamera.forward * 2;
        if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, out hitPosition))
        {
            float dist = Vector3.Distance(mainCamera.position, hitPosition);
            if(dist > 5 || dist < 1)
            {
                Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
                hitPosition = rayTemp.origin + rayTemp.direction * minDist - Vector3.up * 1.0f;
            }
        }
        else
        {
            Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
            hitPosition = rayTemp.origin + rayTemp.direction * minDist - Vector3.up*1.0f;
        }

        VideoController.instance.videoRoot.transform.position = hitPosition;
        */

        Vector3 dir = mainCamera.forward;
        dir.y = 0;

        VideoController.instance.videoRoot.transform.position = ScanController.instance.currentMarkerPosition + dir.normalized * 1;
    }

    public void MarkButtonViewType(string viewType)
    {
        arImage.GetComponent<Image>().enabled = (viewType == "ar");
        videoImage.GetComponent<Image>().enabled = (viewType == "video");
        audioImage.GetComponent<Image>().enabled = (viewType == "audio");
        textImage.GetComponent<Image>().enabled = (viewType == "text");

        Color activeColor = ToolsController.instance.GetColorFromHexString("#FFFFFF"); ;
        Color inActiveColor = ToolsController.instance.GetColorFromHexString("#414141"); ;
        arLabel.color = viewType == "ar" ? activeColor : inActiveColor;
        videoLabel.color = viewType == "video" ? activeColor : inActiveColor;
        audioLabel.color = viewType == "audio" ? activeColor : inActiveColor;
        textLabel.color = viewType == "text" ? activeColor : inActiveColor;
    }

    public void Skip()
    {
        PoiInfoController.instance.shouldOpenMenu = true;
        ARMenuController.instance.OpenMenu("info");
    }

    public void Reset()
    {
        textContent.SetActive(false);
        guideOptions.SetActive(false);
        VideoController.instance.DisableFullscreen();
        VideoController.instance.videoCanvas.SetActive(false);
        VideoController.instance.mainUIVideoTarget.targetImage.enabled = true;
        //VideoController.instance.videoPlayer.Stop();

        VideoController.instance.mainUIVideoTarget.targetImage.transform.localScale = new Vector3(1, 1, 1);
        VideoController.instance.currentVideoTarget.targetImage.uvRect = new Rect(0, 0, 1, 1);
        VideoController.instance.mainUIVideoTarget.targetImage.uvRect = new Rect(0, 0, 1, 1);
        VideoController.instance.currentVideoTarget.targetImage.material = null;
        VideoController.instance.mainUIVideoTarget.targetImage.material = null;
        VideoController.instance.backgroundImage.SetActive(false);

        AudioVisualizer.instance.DisableAudioVisualizer();
        SpeechController.instance.Reset();
    }
}
