using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

// This script handles and shows info messages to the user

public class InfoController : MonoBehaviour
{

    public bool showMessageAfterTimeout = true;

    [Space(10)]
    public GameObject messageDialog;
    public Button messageDialogCommitButton;
    public Button messageDialogCloseButton;
    public TextMeshProUGUI messageDialogTitle;
    public TextMeshProUGUI messageDialogDescription;

    [Space(10)]
    public GameObject commitAbortDialog;
    public Button commitAbortDialogCommitButton;
    public Button commitAbortDialogAbortButton;
    public Button commitAbortDialogCloseButton;
    public TextMeshProUGUI commitAbortDialogTitle;
    public TextMeshProUGUI commitAbortDialogDescription;

    [Space(10)]
    public GameObject info;
    public TextMeshProUGUI infoLabel;
    public GameObject infoWithIcon;
    public TextMeshProUGUI infoWithIconLabel;

    [Space(10)]
    public GameObject messageLoading;
    public GameObject messageLoadingButton;
    public GameObject messageLoadingCircle;
    public TextMeshProUGUI messageLoadingLabel;

    [Space(10)]
    public GameObject loadingCircle;
    public GameObject loadingBackground;
    public TextMeshProUGUI loadingCircleLabel;

    [Space(10)]
    public GameObject loadingCircleWithBackground;

    [Space(10)]
    public GameObject loadingScreenshot;
    public RawImage screenShotRawImage;
    public Texture2D screenshot;

    [Space(10)]
    public GameObject loadingProgressContainer;
    public GameObject loadingProgressCircle;
    public Image loadingProgressImage;
    public TextMeshProUGUI loadingProgressContainerLabel;

    [Space(10)]
    public GameObject blocker;

    private IEnumerator messageCoroutine;
    private float infoTimer = -1;

    public static InfoController instance;
    void Awake()
    {
        instance = this;
    }

    void Update()
    {

        if (info.activeInHierarchy || infoWithIcon.activeInHierarchy)
        {

            if (infoTimer <= 0 || Input.GetMouseButtonDown(0))
            {
                info.SetActive(false);
                infoWithIcon.SetActive(false);
            }
            else
            {
                infoTimer -= Time.deltaTime;
            }
        }
    }

    public void ShowMessage(string title, string text = "", Action commitAction = null)
    {
        messageDialog.SetActive(true);

        if (title == "") { messageDialogTitle.transform.parent.gameObject.SetActive(false); }
        else { messageDialogTitle.transform.parent.gameObject.SetActive(true); messageDialogTitle.text = LanguageController.GetTranslation(title); }

        if (text == "") { messageDialogDescription.transform.parent.gameObject.SetActive(false); }
        else { messageDialogDescription.transform.parent.gameObject.SetActive(true); messageDialogDescription.text = LanguageController.GetTranslation(text); }

        messageDialogCommitButton.onClick.RemoveAllListeners();
        messageDialogCommitButton.onClick.AddListener(() => messageDialog.SetActive(false));
        if (commitAction != null) { messageDialogCommitButton.onClick.AddListener(() => commitAction.Invoke()); }

        messageDialogCloseButton.onClick.RemoveAllListeners();
        messageDialogCloseButton.onClick.AddListener(() => messageDialog.SetActive(false));
        if (commitAction != null) { messageDialogCloseButton.onClick.AddListener(() => commitAction.Invoke()); }
    }

    public void ShowCommitAbortDialog(string title, string text, Action commitAction, Action abortAction = null, string commitButtonText = "", string abortButtonText = "")
    {
        commitAbortDialog.SetActive(true);

        if (title == "") { commitAbortDialogTitle.transform.parent.gameObject.SetActive(false); }
        else { commitAbortDialogTitle.transform.parent.gameObject.SetActive(true); commitAbortDialogTitle.text = LanguageController.GetTranslation(title); }

        if (text == "") { commitAbortDialogDescription.transform.parent.gameObject.SetActive(false); }
        else { commitAbortDialogDescription.transform.parent.gameObject.SetActive(true); commitAbortDialogDescription.text = LanguageController.GetTranslation(text); }

        commitAbortDialogCommitButton.onClick.RemoveAllListeners();
        commitAbortDialogCommitButton.onClick.AddListener(() => commitAbortDialog.SetActive(false));
        commitAbortDialogCommitButton.onClick.AddListener(() => commitAction.Invoke());

        commitAbortDialogAbortButton.onClick.RemoveAllListeners();
        commitAbortDialogAbortButton.onClick.AddListener(() => commitAbortDialog.SetActive(false));
        if (abortAction != null) { commitAbortDialogAbortButton.onClick.AddListener(() => abortAction.Invoke()); }

        commitAbortDialogCloseButton.onClick.RemoveAllListeners();
        commitAbortDialogCloseButton.onClick.AddListener(() => commitAbortDialog.SetActive(false));
        if (abortAction != null) { commitAbortDialogCloseButton.onClick.AddListener(() => abortAction.Invoke()); }

        if (commitButtonText != "") { commitAbortDialogCommitButton.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(commitButtonText); }
        else { commitAbortDialogCommitButton.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Ja"); }

        if (abortButtonText != "") { commitAbortDialogAbortButton.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(abortButtonText); }
        else { commitAbortDialogAbortButton.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Nein"); }
    }

    public void ShowCommitAbortDialog(string title, Action commitAction, Action abortAction = null)
    {
        ShowCommitAbortDialog(title, "", commitAction, abortAction);
    }

    public void HideCommitAbortDialog()
    {
        commitAbortDialog.SetActive(false);
    }

    public bool InfoVisible()
    {
        return info.activeInHierarchy || infoWithIcon.activeInHierarchy;
    }

    public void ShowInfo(string text, float timer, bool darken = false, bool withIcon = false)
    {

        if (darken)
        {
            info.GetComponent<Image>().enabled = true;
            infoWithIcon.GetComponent<Image>().enabled = true;
        }
        else
        {
            info.GetComponent<Image>().enabled = false;
            infoWithIcon.GetComponent<Image>().enabled = false;
        }

        if (withIcon)
        {
            infoWithIcon.SetActive(true);
            infoWithIconLabel.text = LanguageController.GetTranslation(text);
        }
        else
        {
            info.SetActive(true);
            infoLabel.text = LanguageController.GetTranslation(text);
        }
        infoTimer = timer;
    }

    public void SetLoadingProgress(float val)
    {
        loadingProgressImage.fillAmount = val;
    }

    public void ShowLoadingMessage(string text)
    {

        messageLoadingButton.SetActive(false);
        messageLoadingCircle.SetActive(true);
        messageLoading.SetActive(true);
        messageLoadingLabel.text = LanguageController.GetTranslation(text);
    }

    public void ShowLoadingMessageWithTimeout(string text, float timeout, string timeoutMessage, float timeoutMessageTime = 0, Action timeoutAction = null)
    {

        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = ShowLoadingMessageWithTimeoutCoroutine(text, timeout, timeoutMessage, timeoutMessageTime, timeoutAction);
        StartCoroutine(messageCoroutine);
    }

    public void DisableLoadingMessage()
    {

        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageLoading.SetActive(false);
    }

    private IEnumerator ShowLoadingMessageWithTimeoutCoroutine(string text, float timeout, string timeoutMessage, float timeoutMessageTime = 0, Action timeoutAction = null)
    {

        ShowLoadingMessage(text);
        yield return new WaitForSeconds(timeout);
        ShowLoadingMessage(timeoutMessage);
        messageLoadingCircle.SetActive(false);

        if (timeoutMessageTime > 0)
        {
            yield return new WaitForSeconds(timeoutMessageTime);
            messageLoading.SetActive(false);
        }
        else
        {
            messageLoadingButton.SetActive(true);
        }

        if (timeoutAction != null)
        {
            timeoutAction.Invoke();
        }

        messageCoroutine = null;
    }

    public bool InfoBoxActive()
    {
        return messageDialog.activeInHierarchy ||
            commitAbortDialog.activeInHierarchy ||
            loadingCircle.activeInHierarchy ||
            loadingProgressContainer.activeInHierarchy;
    }

    public IEnumerator ShowLoadingScreenShotCoroutine_v1()
    {
        string filename = "screenshot.jpg";
        string savePath = Application.persistentDataPath + "/" + filename;

        yield return null; yield return new WaitForEndOfFrame();

#if UNITY_EDITOR
        ScreenCapture.CaptureScreenshot(savePath);
#else
        ScreenCapture.CaptureScreenshot(filename);
#endif

        // Todo: We need to wait until the texture is actually saved, because on Android/iOS it takes some time
        
        yield return null; yield return new WaitForEndOfFrame();

        if (File.Exists(savePath))
        {
            byte[] fileData;

            if (screenshot == null)
            {
                screenshot = new Texture2D(2, 2, TextureFormat.RGB24, true);
                screenshot.wrapMode = TextureWrapMode.Clamp;
            }

            fileData = File.ReadAllBytes(savePath);
            screenshot.LoadImage(fileData);
            screenShotRawImage.texture = screenshot;
        }

        loadingScreenshot.SetActive(true);
    }


    public IEnumerator ShowLoadingScreenShotCoroutine()
    {
        yield return null; yield return new WaitForEndOfFrame();

        screenshot = ScreenCapture.CaptureScreenshotAsTexture(); 
        screenShotRawImage.texture = screenshot;

        loadingScreenshot.SetActive(true);
    }

    public void HideLoadingScreenShot()
    {
        loadingScreenshot.SetActive(false);
        if (screenshot != null) { Destroy(screenshot); }
        screenShotRawImage.texture = null;
    }
}
