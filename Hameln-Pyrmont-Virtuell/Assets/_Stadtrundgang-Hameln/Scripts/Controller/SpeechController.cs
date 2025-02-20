using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Tool;
using Crosstales.Common.Util;
using TMPro;

public class SpeechController : MonoBehaviour
{
    public List<AudioSpeechPlayer> audioSpeechPlayers = new List<AudioSpeechPlayer>();
    public AudioSource audioSource;
    public GameObject rtVoiceRoot;
    public TextMeshProUGUI currentLabel;

    [Space(10)]

    public GameObject speechSupportButton;
    public List<TextMeshProUGUI> siteLabels;
    private float updateSpeechPlaybackTimer = 0;

    private bool isSpeaking = false;
    private bool speakWasStarted = false;
    private string originText = "";
    private float delayBetweenLabels = 1.0f;

    private bool initialized = false;
    private bool isSpeakingWord = false;

    public static SpeechController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        //Speaker.Instance.OnSpeakStart += OnSpeakStart;
        //Speaker.Instance.OnVoicesReady += OnVoicesReady;
        //Speaker.Instance.OnSpeakAudioGenerationStart += OnSpeakAudioGenerationStart;
        //Speaker.Instance.OnSpeakAudioGenerationComplete += OnSpeakAudioGenerationComplete;

        Speaker.Instance.OnSpeakCurrentWord += OnSpeakCurrentWord;
        Speaker.Instance.OnSpeakCurrentWordString += OnSpeakCurrentWordString;

        InitSpeechSupport();
    }

    void Update()
    {
        //if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "InfoSite") return;
        //print(Speaker.Instance.isSpeaking + " " + Speaker.Instance.isPaused + " " + Speaker.Instance.isBusy);		    
        //print((audioSource.clip == null) + " " + audioSource.isPlaying + " " + audioSource.time);

        UpdateSpeechPlayback();
    }

    public void UpdateSpeechPlayback()
    {
        if (!isSpeaking) return;
        updateSpeechPlaybackTimer += Time.deltaTime;
        if (updateSpeechPlaybackTimer < 1) return;
        updateSpeechPlaybackTimer = 0;

        GameObject icon = ToolsController.instance.FindGameObjectByName(speechSupportButton, "Icon");
        if (icon.GetComponent<Image>().color.r != 1) 
        {
            bool textsChanged = CollectSiteLabels();
            if (textsChanged) { StopSpeak(); }
        }
    }

    public void InitSpeechSupport()
    {
        // Speech support
        int speechSupported = PlayerPrefs.GetInt("SpeechSupportEnabled", 0);
        ToggleSpeechSupport(speechSupported == 1);

        int speechSupportButtonSize = PlayerPrefs.GetInt("SpeechSupportButtonsSize", 1);
        SetSpeechSupportButtonSize(speechSupportButtonSize);
    }

    public void ToggleSpeechSupport(bool isEnabled)
    {
        speechSupportButton.SetActive(isEnabled);
    }

    public void SetSpeechSupportButtonSize(int index)
    {
        float s = 1.0f;
        if (index == 0) { s = 0.75f; }
        if (index == 2) { s = 1.25f; }
        speechSupportButton.transform.localScale = Vector3.one * s;
    }

    public void PlayPauseSpeechFromSpeechSupport()
    {
        print("PlayPauseSpeechFromSpeechSupport " + Speaker.Instance.isSpeaking + " " + Speaker.Instance.isPaused + " " + Speaker.Instance.isBusy);

        GameObject icon = ToolsController.instance.FindGameObjectByName(speechSupportButton, "Icon");
        GameObject background = ToolsController.instance.FindGameObjectByName(speechSupportButton, "Background");
        icon.GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#FFFFFF");
        background.GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#808080");

        bool shouldPause = false;
        bool textsChanged = CollectSiteLabels();
        if (textsChanged && isSpeaking) { shouldPause = true; }
        if (textsChanged) { StopSpeak(); }

        print("PlayPauseSpeechFromSpeechSupport textsChanged " + textsChanged);

        speakWasStarted = true;

        if (!isSpeaking && !shouldPause)
        {
            isSpeaking = true;
            StartCoroutine(SpeakTextsCoroutine(siteLabels));

            icon.GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#6CB931");
            background.GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("FFFFFF");
        }
        else if (Speaker.Instance.isPaused)
        {
            print("UnPause");
            Speaker.Instance.UnPause();
            OnStartSpeak();

            icon.GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#6CB931");
            background.GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("FFFFFF");
        }
        else if (!Speaker.Instance.isPaused)
        {
            print("Pause");
            Speaker.Instance.Pause();
            OnPausedSpeak();
        }
        else
        {
            print("StopSpeak");
            StopSpeak();
        }
    }

    public bool CollectSiteLabels()
    {
        if(SiteController.instance.currentSite == null) { siteLabels.Clear(); return true; }
        TextMeshProUGUI[] siteLabelsTmp = SiteController.instance.currentSite.GetComponentsInChildren<TextMeshProUGUI>();

        bool labelsChanged = false;
        if (siteLabelsTmp.Length != siteLabels.Count) { labelsChanged = true; }
        else
        {
            for (int i = 0; i < siteLabelsTmp.Length; i++) { 
                if(siteLabelsTmp[i] != siteLabels[i]) { labelsChanged = true; break; }
            }
        }

        siteLabels.Clear();
        for (int i = 0; i < siteLabelsTmp.Length; i++){ siteLabels.Add(siteLabelsTmp[i]); }

        return labelsChanged;
    }

    public void CancelSpeechFromSpeechSupport()
    {
        if (speechSupportButton.activeInHierarchy)
        {
            GameObject icon = ToolsController.instance.FindGameObjectByName(speechSupportButton, "Icon");
            GameObject background = ToolsController.instance.FindGameObjectByName(speechSupportButton, "Background");
            icon.GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#FFFFFF");
            background.GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#808080");
        }
    }

    private void OnSpeakStart(Crosstales.RTVoice.Model.Wrapper wrapper)
    {
        Debug.Log("OnSpeakStart: " + wrapper);
    }

    private void OnVoicesReady()
    {
        Debug.Log("OnVoicesReady: ");
    }

    private void OnSpeakCurrentWord(Crosstales.RTVoice.Model.Wrapper wrapper, string[] speechTextArray, int wordIndex)
    {
        //print("OnSpeakCurrentWord " + wordIndex);
        isSpeakingWord = true;
    }

    private void OnSpeakCurrentWordString(Crosstales.RTVoice.Model.Wrapper wrapper, string word)
    {
        //print("OnSpeakCurrentWordString " + word);
        isSpeakingWord = true;
    }

    private void OnSpeakAudioGenerationStart(Crosstales.RTVoice.Model.Wrapper wrapper)
    {
        print("OnSpeakAudioGenerationStart ");
    }

    private void OnSpeakAudioGenerationComplete(Crosstales.RTVoice.Model.Wrapper wrapper)
    {
        print("OnSpeakAudioGenerationComplete ");
    }

    private void OnDestroy()
    {
        if (rtVoiceRoot != null)
        {

            //Speaker.Instance.OnSpeakStart -= OnSpeakStart;
            //Speaker.Instance.OnVoicesReady -= OnVoicesReady;
            //Speaker.Instance.OnSpeakAudioGenerationStart -= OnSpeakAudioGenerationStart;
            //Speaker.Instance.OnSpeakAudioGenerationComplete += OnSpeakAudioGenerationComplete;
            Speaker.Instance.OnSpeakCurrentWord -= OnSpeakCurrentWord;
            Speaker.Instance.OnSpeakCurrentWordString -= OnSpeakCurrentWordString;
        }
    }

    public void Init()
    {
        //print(Speaker.Instance.isPlatformSupported + " " + Speaker.Instance.isSpeakSupported);	    
        bool supported = (Speaker.Instance.isPlatformSupported && Speaker.Instance.isSpeakSupported);

#if UNITY_EDITOR
        supported = true;
#endif
        supported = true;

        if (!supported)
        {
            for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].infoSpeechContent.SetActive(false); }
        }
        else
        {
            AudioVisualizer.instance.EnableAudioVisualizer(audioSource);
            AudioVisualizer.instance.SetCameraPositionY(-430);
            for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].infoSpeechContent.SetActive(true); }
        }
    }

    public void PlayPauseSpeak()
    {
        print("PlayPauseSpeak " + Speaker.Instance.isSpeaking + " " + Speaker.Instance.isPaused + " " + Speaker.Instance.isBusy);

        speakWasStarted = true;
        CancelSpeechFromSpeechSupport();

        if (!isSpeaking)
        {
            isSpeaking = true;
            List<TextMeshProUGUI> labels = PoiInfoController.instance.labels;
            StartCoroutine(SpeakTextsCoroutine(labels));
        }
        else if (Speaker.Instance.isPaused)
        {
            print("UnPause");
            Speaker.Instance.UnPause();
            OnStartSpeak();
        }
        else if (!Speaker.Instance.isPaused)
        {
            print("Pause");
            Speaker.Instance.Pause();
            OnPausedSpeak();
        }
        else
        {
            print("StopSpeak");
            StopSpeak();
        }
    }

    public IEnumerator SpeakTextsCoroutine(List<TextMeshProUGUI> labels)
    {
        print("SpeakTextsCoroutine");

        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].loadingCircle.SetActive(true); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].playPauseLabel.text = LanguageController.GetTranslation("Lädt"); }

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] == null) continue;
            if (labels[i].text == "") continue;
            if (i > 0) { yield return new WaitForSeconds(delayBetweenLabels); }

            if (labels[i] == null) continue;
            string textToSpeech = labels[i].text;
            string[] sentences = Regex.Split(textToSpeech, @"(?<=[\.!\?])\s+");
            for (int k = 0; k < sentences.Length; k++)
            {
                print(k + " " + sentences[k]);
            }

            if (sentences.Length <= 0)
            {
                string text = Regex.Replace(textToSpeech, @"<[^>]+>| ", " ").Trim();
                yield return StartCoroutine(SpeakCoroutine(text));
            }
            else
            {
                for (int j = 0; j < sentences.Length; j++)
                {
                    print("sentences " + sentences[j]);

                    if (sentences[j] == "") continue;
                    if (labels[i] == null) continue;

                    originText = labels[i].text;
                    string text = labels[i].text;

                    if (text.Contains(sentences[j]) && sentences[j].Length > 0)
                    {
                        string markedSentence = "<mark=#33DDFF44>" + sentences[j] + "</mark>";
                        text = text.Replace(sentences[j], markedSentence);
                    }

                    labels[i].text = text;
                    currentLabel = labels[i];

                    string myText = Regex.Replace(sentences[j], @"<[^>]+>| ", " ").Trim();
                    yield return StartCoroutine(SpeakCoroutine(myText));

                    if (labels[i] == null) continue;
                    labels[i].text = originText;
                }
            }
        }

        StopSpeak();
        isSpeaking = false;
    }

    public IEnumerator SpeakCoroutine(string textToSpeech)
    {
        Speak(textToSpeech);
        OnSpeakLoading();
        //yield return new WaitForSeconds(0.25f);

        float timer = 10;
        while (!Speaker.Instance.isSpeaking && timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        print("Speaking started " + timer);
        OnStartSpeak();

#if UNITY_IOS && !UNITY_EDITOR
		
	    isSpeakingWord = false;

        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].loadingCircle.SetActive(true); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].playPauseLabel.text = LanguageController.GetTranslation("Lädt"); }
	    timer = 15.0f;
	    while(!isSpeakingWord && timer > 0){ timer-= Time.deltaTime; yield return null;}

        /*
        if (!initialized)
        {
            loadingCircle.SetActive(true);
            playPauseLabel.text = LanguageController.GetTranslation("Lädt");
            initialized = true;
	     	        
	        timer = 15.0f;
	        while(!isSpeakingWord && timer > 0){ timer-= Time.deltaTime; yield return null;}
	    }
	    */

        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].loadingCircle.SetActive(false); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].playPauseLabel.text = LanguageController.GetTranslation("Text pausieren"); }
        
#endif

        //audioVisualizerImage.gameObject.SetActive(true);

        while (Speaker.Instance.isSpeaking || Speaker.Instance.isPaused)
        {
            //print(Speaker.Instance.isSpeaking + " " + Speaker.Instance.isPaused);
            yield return null;
        }
    }

    public void MarkWord()
    {
        // <mark=#33DDFF66>ipsum</mark>
    }

    public void Speak(string text)
    {
        print("Speak " + text);
        Speaker.Instance.Speak(text, audioSource, Speaker.Instance.VoiceForCulture(LanguageController.GetAppLanguageCode()));
        //print("Speak " + Speaker.Instance.isSpeaking + " " + Speaker.Instance.isPaused);
    }

    public void StopSpeak()
    {
        isSpeaking = false;
        ResetCurrentLabel();

        CancelSpeechFromSpeechSupport();
        StopAllCoroutines();
        if (rtVoiceRoot != null) { Speaker.Instance.Silence(); }

        //if (Speaker.Instance.isSpeaking || Speaker.Instance.isBusy || Speaker.Instance.isPaused) { Speaker.Instance.Silence(); }

        OnPausedSpeak();
        //stopButton.SetActive(false);
        //audioVisualizerImage.gameObject.SetActive(false);

        //print("StopSpeak " + Speaker.Instance.isSpeaking + " " + Speaker.Instance.isBusy + " " + Speaker.Instance.isPaused);
    }

    public void ResetCurrentLabel()
    {
        if (currentLabel != null && originText != "")
        {
            currentLabel.text = originText;
            originText = "";
        }
    }

    public void OnPausedSpeak()
    {
        //print("OnPausedSpeak");

        for (int i = 0; i < audioSpeechPlayers.Count; i++) { if (audioSpeechPlayers[i] != null) { audioSpeechPlayers[i].playButton.SetActive(true); } }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { if (audioSpeechPlayers[i] != null) { audioSpeechPlayers[i].pauseButton.SetActive(false); } }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { if (audioSpeechPlayers[i] != null) { audioSpeechPlayers[i].loadingCircle.SetActive(false); } }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { if (audioSpeechPlayers[i] != null) { audioSpeechPlayers[i].playPauseLabel.text = LanguageController.GetTranslation("Text vorlesen"); } }
    }

    public void OnStartSpeak()
    {
        print("OnStartSpeak");

        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].playButton.SetActive(false); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].pauseButton.SetActive(true); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].loadingCircle.SetActive(false); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].playPauseLabel.text = LanguageController.GetTranslation("Text pausieren"); }
    }

    public void OnSpeakLoading()
    {
        print("OnSpeakLoading");

        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].playButton.SetActive(false); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].pauseButton.SetActive(true); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].loadingCircle.SetActive(true); }
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { audioSpeechPlayers[i].playPauseLabel.text = LanguageController.GetTranslation("Lädt"); }
    }

    public void Reset()
    {
        StopSpeak();
        AudioVisualizer.instance.DisableAudioVisualizer();

        audioSpeechPlayers.RemoveAll(a => a == null);
        for (int i = 0; i < audioSpeechPlayers.Count; i++) { if (audioSpeechPlayers[i] != null) { audioSpeechPlayers[i].loadingCircle.SetActive(false); } }
    }
}
