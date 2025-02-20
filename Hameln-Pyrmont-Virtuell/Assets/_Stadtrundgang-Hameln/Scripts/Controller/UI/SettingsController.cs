using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsController : MonoBehaviour
{
    [Header("Text settings")]
    public Slider textScaleSlider;
    public TextMeshProUGUI textScaleLabel;

    [Space(10)]
    [Header("Speech settings")]
    public Toggle speechSupportToggle;
    public GameObject speechSupportSizeButtonsRoot;
    public List<GameObject> speechSupportSizeButtons = new List<GameObject>();

    [Space(10)]
    [Header("Dalli Klicks settings")]
    public List<GameObject> dalliKlickTimerButtons = new List<GameObject>();

    private bool isLoading = false;

    public static SettingsController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        //int textScale = PlayerPrefs.GetInt("TextScale", 0);
        //SetTextScale(textScale);

        // Text scale
        float textScalePercentage = PlayerPrefs.GetFloat("TextScalePercentage", 1);
        textScaleSlider.SetValueWithoutNotify(textScalePercentage);
        textScaleLabel.text = "<mspace=0.5em>" + (textScalePercentage * 100).ToString("F0") + "<space=0.3em>%";
        if (TextController.instance != null) { TextController.instance.UpdateTextScale(textScalePercentage); }

        // Speech support
        int speechSupported = PlayerPrefs.GetInt("SpeechSupportEnabled", 0);
        speechSupportToggle.SetIsOnWithoutNotify(speechSupported == 1);
        ToggleSpeechSupport();

        int speechSupportButtonSize = PlayerPrefs.GetInt("SpeechSupportButtonsSize", 1);
        SelectSpeechSupportButtonSize(speechSupportButtonSize);

        // Dalli Klick
        int dalliKlickTimeIndex = PlayerPrefs.GetInt("DalliKlickTimeId", 0);
        SelectDalliKlickTime(dalliKlickTimeIndex);
    }

    public void SetTextScale(int id)
    {
        PlayerPrefs.SetInt("TextScale", id);
        if (id == 0) { TextController.instance.UpdateTextScale(1); }
        else if (id == 1) { TextController.instance.UpdateTextScale(1.5f); }
        else if (id == 2) { TextController.instance.UpdateTextScale(2.0f); }
    }

    public void UpdateTextScale()
    {
        float val = textScaleSlider.value;
        if (TextController.instance != null) { TextController.instance.UpdateTextScale(val); }
        textScaleLabel.text = "<mspace=0.5em>" + (val * 100).ToString("F0") + "<space=0.3em>%";
        PlayerPrefs.SetFloat("TextScalePercentage", val);
    }

    public void Back()
    {
        if (isLoading) return;
        isLoading = false;
        StartCoroutine(OpenSettingsCoroutine());
    }

    public IEnumerator OpenSettingsCoroutine()
    {
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));

        isLoading = false;
    }

    public void ToggleSpeechSupport()
    {
        if(SpeechController.instance != null) { SpeechController.instance.ToggleSpeechSupport(speechSupportToggle.isOn); }
        PlayerPrefs.SetInt("SpeechSupportEnabled", speechSupportToggle.isOn ? 1:0);
        speechSupportSizeButtonsRoot.SetActive(speechSupportToggle.isOn);
    }

    public void SelectSpeechSupportButtonSize(int index)
    {
        if (SpeechController.instance != null) { SpeechController.instance.SetSpeechSupportButtonSize(index); }
        PlayerPrefs.SetInt("SpeechSupportButtonsSize", index);

        for (int i = 0; i < speechSupportSizeButtons.Count; i++) {
            speechSupportSizeButtons[i].transform.GetChild(0).GetComponent<Image>().color = Color.white;
            speechSupportSizeButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = GetColorFromHexString("#323232");
        }

        speechSupportSizeButtons[index].transform.GetChild(0).GetComponent<Image>().color = GetColorFromHexString("#6CB931");
        speechSupportSizeButtons[index].GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
    }

    public void SelectDalliKlickTime(int index)
    {
        PlayerPrefs.SetInt("DalliKlickTimeId", index);

        for (int i = 0; i < dalliKlickTimerButtons.Count; i++)
        {
            dalliKlickTimerButtons[i].transform.GetChild(0).GetComponent<Image>().color = Color.white;
            dalliKlickTimerButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = GetColorFromHexString("#323232");
        }

        dalliKlickTimerButtons[index].transform.GetChild(0).GetComponent<Image>().color = GetColorFromHexString("#6CB931");
        dalliKlickTimerButtons[index].GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
    }

    public Color GetColorFromHexString(string hexCode)
    {
        if (hexCode.Length == 6){ hexCode += "FF"; }
        if (!hexCode.StartsWith("#")){ hexCode = "#" + hexCode; }
        Color color;
        if (ColorUtility.TryParseHtmlString(hexCode, out color)) return color;
        return Color.white;
    }
}
