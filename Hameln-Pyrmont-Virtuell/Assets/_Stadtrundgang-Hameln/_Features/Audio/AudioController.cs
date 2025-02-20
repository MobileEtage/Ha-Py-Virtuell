using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using TMPro;
using UnityEngine.Networking;
using static VideoTarget;
using UnityEngine.Video;

public class AudioController : MonoBehaviour
{
	public Image infoImage;
	public TextMeshProUGUI infoTitle;
	public TextMeshProUGUI infoDescription;
    public GameObject buttonsHolder;

	[Space(10)]

	public AudioSpeechPlayer audioSpeechPlayer;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI descriptionLabel;
    public TextMeshProUGUI copyRightLabel;
	public RawImage audioVisualizationImage;

    [Space(10)]

    public AudioSource audioSource;
	public GameObject tutorialContent;
	public GameObject audioContent;

    [Header("Navigation params")]

    public bool audioSliderSelected = false;
    public GameObject audioNavigation;
    public GameObject playButton;
    public GameObject pauseButton;
    public Slider timeSlider;
    public TextMeshProUGUI currentTimeLabel;

    private float audioNavigationAlpha = 1;
    private bool isLoading = false;
    private JSONNode featureData;
    private JSONNode currentAudioData;

    public static AudioController instance;
	void Awake()
	{
		instance = this;
	}

	private void Update()
	{
        if (SiteController.instance != null && SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "AudioSite") return;
        UpdateAudioTimer();
        UpdateAudioTimerVisiblity();
    }

    private void UpdateAudioTimerVisiblity()
    {
        audioNavigation.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(audioNavigation.GetComponent<CanvasGroup>().alpha, audioNavigationAlpha, Time.deltaTime * 6);
    }

    public IEnumerator InitCoroutine()
	{
		featureData = StationController.instance.GetStationFeature("audio");
		if (featureData == null) yield break;
		if (featureData["url"] == null && featureData["audio"] == null && featureData["audios"] == null)
		{
			InfoController.instance.ShowMessage("Inhalte dieser Station konnten nicht abgerufen werden.");
			yield break;
		}

		InfoController.instance.loadingCircle.SetActive(true);
		yield return new WaitForSeconds(0.25f);

		tutorialContent.SetActive(true);
		audioContent.SetActive(false);
		LoadIntroSite();
		if (!SpeechController.instance.audioSpeechPlayers.Contains(audioSpeechPlayer)) { SpeechController.instance.audioSpeechPlayers.Add(audioSpeechPlayer); }

		InfoController.instance.loadingCircle.SetActive(false);

		yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("AudioSite"));

		ARMenuController.instance.DisableMenu(true);
		yield return new WaitForSeconds(0.5f);
		ARController.instance.StopARSession();
	}

	public void LoadIntroSite()
	{
		JSONNode featureData = StationController.instance.GetStationFeature("audio");
		if (featureData == null) return;

		if (featureData["infoTitle"] != null) { infoTitle.text = LanguageController.GetTranslationFromNode(featureData["infoTitle"]); }
		else { infoTitle.text = LanguageController.GetTranslation("Audio"); }

		if (featureData["infoDescription"] != null) { infoDescription.text = LanguageController.GetTranslationFromNode(featureData["infoDescription"]); }
		else { infoDescription.text = LanguageController.GetTranslation("Hör dir hier eine Sounddatei zu dieser Station an."); }

		// Image
		if (featureData["infoImage"] != null && featureData["infoImage"].Value != "")
		{
			ToolsController.instance.ApplyOnlineImage(infoImage, featureData["infoImage"].Value, true);
		}
		else
		{
			Sprite sprite = Resources.Load<Sprite>("UI/Sprites/music");
			infoImage.sprite = sprite;
			infoImage.preserveAspect = true;
		}

		// Buttons		
		foreach (Transform child in buttonsHolder.transform) { Destroy(child.gameObject); }
		if (featureData["audios"] == null)
		{
			GameObject obj = ToolsController.instance.InstantiateObject("UI/_Buttons/StartButton-800x180", buttonsHolder.transform);
			obj.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Abspielen");

			// Button event
			obj.GetComponentInChildren<Button>().onClick.AddListener(() => PlayAudio());
		}
		else
		{
			for (int i = 0; i < featureData["audios"].Count; i++)
			{
				GameObject obj = ToolsController.instance.InstantiateObject("UI/_Buttons/StartButton-800x180", buttonsHolder.transform);
				if (featureData["audios"][i]["play_button"] != null) { obj.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslationFromNode(featureData["audios"][i]["play_button"]); }
				else { obj.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Abspielen"); }

				// Button event
				int index = i;
				obj.GetComponentInChildren<Button>().onClick.AddListener(() => PlayAudio(featureData["audios"][index]));
			}
		}
	}

    public void PlayAudio(JSONNode dataJson)
    {
        if (isLoading) return;
        isLoading = true;
		currentAudioData = dataJson;
        StartCoroutine(LoadAudioCoroutine());
    }

    public void PlayAudio()
	{
		if (isLoading) return;
		isLoading = true;
		currentAudioData = null;
        StartCoroutine(LoadAudioCoroutine());
	}

	public IEnumerator LoadAudioCoroutine()
	{
		InfoController.instance.loadingCircle.SetActive(true);
		yield return new WaitForSeconds(0.25f);

		if(currentAudioData == null)
		{
			currentAudioData = featureData;
        }

        // Texts
        List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        if (currentAudioData["title"] != null) { titleLabel.text = LanguageController.GetTranslationFromNode(currentAudioData["title"]); labels.Add(titleLabel); }
        else { titleLabel.text = ""; }   
        if (currentAudioData["copyright"] != null) { copyRightLabel.text = LanguageController.GetTranslationFromNode(currentAudioData["copyright"]); labels.Add(copyRightLabel); }
        else { copyRightLabel.text = ""; }
        if (currentAudioData["description"] != null) { descriptionLabel.text = LanguageController.GetTranslationFromNode(currentAudioData["description"]); labels.Add(descriptionLabel); }
        else { descriptionLabel.text = ""; }
        PoiInfoController.instance.labels = labels;

        // Sound file
        if (currentAudioData["url"] != null)
		{
			print("Downloading audio file " + currentAudioData["url"].Value);

			string path = DownloadContentController.instance.GetAudioFile(currentAudioData["url"]);
			AudioType audioType = AudioType.MPEG;
			if (path.EndsWith("wav")) { audioType = AudioType.WAV; }

			print("UnityWebRequestMultimedia " + path + " " + audioType);

			UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
			yield return req.SendWebRequest();
			if (req.isNetworkError || req.isHttpError) Debug.Log("LoadAudioCoroutine error " + req.error);
			
			print("UnityWebRequestMultimedia1" );

			if (!File.Exists(path) && req.downloadHandler != null && req.downloadHandler.data != null) {

				string savePath = ToolsController.instance.GetSavePathFromURL(path, -1);
				File.WriteAllBytes(savePath, req.downloadHandler.data); 
			}

			if (req.downloadHandler != null && req.downloadHandler.data != null) {

				AudioClip audioClip = DownloadHandlerAudioClip.GetContent(req);
				if (audioClip != null) { audioSource.clip = audioClip; audioSource.time = 0; audioSource.Play(); }
			}
			

		}
		else if (currentAudioData["audio"] != null)
		{
			AudioClip audioClip = Resources.Load<AudioClip>(currentAudioData["audio"].Value);
			if (audioClip != null) { audioSource.clip = audioClip; audioSource.time = 0; audioSource.Play(); }
		}

		AudioVisualizer.instance.EnableAudioVisualizer(audioSource);
		AudioVisualizer.instance.SetCameraPositionY(0);
        AudioVisualizer.instance.SetSensibility(300, 300, 3f);
        audioVisualizationImage.texture = AudioVisualizer.instance.myImage.texture;
        audioVisualizationImage.material.mainTexture = AudioVisualizer.instance.myImage.material.mainTexture;

        VideoController.instance.mainUIVideoTarget.targetImage.enabled = false;
		VideoController.instance.videoCanvas.SetActive(false);
		VideoController.instance.EnableFullscreen();

		tutorialContent.SetActive(false);
		audioContent.SetActive(true);
		GuideController.instance.guideAudioImage.SetActive(false);

		InfoController.instance.loadingCircle.SetActive(false);

		isLoading = false;
	}

	public void Back()
	{
		if (tutorialContent.activeInHierarchy)
		{
			InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);
		}
		else
		{
			audioSource.Stop();
			tutorialContent.SetActive(true);
			audioContent.SetActive(false);
			VideoController.instance.DisableFullscreen();
			VideoController.instance.videoCanvas.SetActive(false);
			VideoController.instance.mainUIVideoTarget.targetImage.enabled = true;
			VideoController.instance.backgroundImage.SetActive(false);
			AudioVisualizer.instance.DisableAudioVisualizer();
			AudioVisualizer.instance.ResetSensibility();
		}
	}

	public IEnumerator LoadSingleAudioFileCoroutine()
	{
		JSONNode featureData = StationController.instance.GetStationFeature("audio");
		if (featureData == null) yield break;
		if (featureData["audio"] == null) yield break;

		InfoController.instance.loadingCircle.SetActive(true);
		yield return new WaitForSeconds(0.25f);

		AudioClip audioClip = Resources.Load<AudioClip>(featureData["audio"].Value);
		if (audioClip != null) { audioSource.clip = audioClip; audioSource.time = 0; audioSource.Play(); }
		AudioVisualizer.instance.EnableAudioVisualizer(audioSource);
		AudioVisualizer.instance.SetCameraPositionY(0);
        AudioVisualizer.instance.SetSensibility(300, 300, 3f);

        VideoController.instance.mainUIVideoTarget.targetImage.enabled = false;
		VideoController.instance.videoCanvas.SetActive(false);
		VideoController.instance.EnableFullscreen();
		GuideController.instance.guideAudioImage.SetActive(false);

		InfoController.instance.loadingCircle.SetActive(false);

		if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "ARSite")
		{
			yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ARSite"));
		}

		ARMenuController.instance.DisableMenu(true);
		VideoController.instance.EnableFullscreen();
		yield return new WaitForSeconds(0.5f);
		ARController.instance.StopARSession();
	}

    public void PlayAudioFile()
    {
        if (audioSource.clip != null)
        {
            if (!audioSource.isPlaying) { audioSource.Play(); }
            playButton.SetActive(false);
            pauseButton.SetActive(true);

            audioNavigation.SetActive(true);
            audioNavigationAlpha = 1.0f;
        }
    }

    public void PauseAudioFile()
    {
        if (audioSource.clip != null)
        {
            audioSource.Pause();
            playButton.SetActive(true);
            pauseButton.SetActive(false);
        }
    }

    public void PlayPauseAudio()
    {
        if (audioSource.isPlaying){ PauseAudioFile(); }
        else{ PlayAudioFile(); }
    }

    private void UpdateAudioTimer()
    {
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            float audioDuration = audioSource.clip.length;
            float audioTime = audioSource.time;
            if (audioDuration != 0)
            {
                if (!audioSliderSelected)
                {
                    timeSlider.value = (float)(audioTime / audioDuration);
					currentTimeLabel.text = GetMinutes(audioTime) + ":" + GetSeconds(audioTime) + "<color=#9F9F9F> / " + GetMinutes(audioDuration) + ":" + GetSeconds(audioDuration);
					//leftTimeLabel.text = "-" + GetMinutes(audioDuration - audioTime) + ":" + GetSeconds(audioDuration - audioTime);
				}
			}
			if (audioTime >= (audioDuration - 0.1f))
			{
				//OnReachedLoopPoint();
			}
		}
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

    public void SetAudioTimeBySlider()
    {
        if (audioSource.clip == null) return;

        float val = timeSlider.value;
        float audioDuration = audioSource.clip.length;
        if (audioDuration != 0){ audioSource.time = Mathf.Clamp(val * audioDuration, 0, audioDuration - 2f); }

        if (audioSource.isPlaying)
        {
            playButton.SetActive(false);
            pauseButton.SetActive(true);
        }

        StopCoroutine("WaitForSeekCompletedCoroutine");
        StartCoroutine("WaitForSeekCompletedCoroutine");
    }

    IEnumerator WaitForSeekCompletedCoroutine()
    {
        yield return new WaitForEndOfFrame();
        audioSliderSelected = false;
    }

    public void Reset()
	{
		audioSource.Stop();
		tutorialContent.SetActive(true);
		audioContent.SetActive(false);
		GuideController.instance.guideAudioImage.SetActive(true);

		VideoController.instance.DisableFullscreen();
		VideoController.instance.videoCanvas.SetActive(false);
		VideoController.instance.mainUIVideoTarget.targetImage.enabled = true;
		VideoController.instance.backgroundImage.SetActive(false);
		AudioVisualizer.instance.DisableAudioVisualizer();
		AudioVisualizer.instance.ResetSensibility();

        SpeechController.instance.Reset();
        if (SpeechController.instance.audioSpeechPlayers.Contains(audioSpeechPlayer)) { SpeechController.instance.audioSpeechPlayers.Remove(audioSpeechPlayer); }
    }
}
