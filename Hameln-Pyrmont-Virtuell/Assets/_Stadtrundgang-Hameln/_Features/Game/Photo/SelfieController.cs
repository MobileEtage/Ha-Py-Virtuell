using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class SelfieController : MonoBehaviour
{
    public DragMenuController dragMenu;
    public GameObject eventSystem;

    [Space(10)]

	private bool fixFrontFacingCameraFullscreen = true;
	public bool useSelfieCamera = true;
    public PhotoHelper photoHelper;
    public GameObject photoPreview;
    public GameObject switchCameraUI;
    public GameObject tutorialContent;
    public GameObject arContent;
    public GameObject background;
    public GameObject selfieFrame;
    public GameObject banderole;
    public Image selfieFrameImage;
    public Image banderoleImage;

    [Space(10)]

    public GameObject postCardUIContent;
    public Camera postCardCamera;
    public Image postCardImage;
    public TMP_InputField postCardMessageInputfield;
    private Texture2D postCardTex;

    public GameObject messageContent;
    public TMP_InputField messageInputfield;
    public Image commitMessageButtonImage;

    public GameObject resultContent;
    public Image resultPostCardImage;

    [Space(10)]

    public List<GameObject> selfieFrames = new List<GameObject>();
    public List<GameObject> banderolen = new List<GameObject>();

    private bool isLoading = false;
    private int currentSelfieFrame = -1;
    private int currentBanderole = -1;

    public static SelfieController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (ARController.instance == null)
        {
            Reset();
            eventSystem.SetActive(true);
            GameObject toolsController = new GameObject("ToolsController");
            toolsController.AddComponent<ToolsController>();

            StartCoroutine(InitCoroutine());
        }
    }

    public IEnumerator InitCoroutine()
    {
        yield return null;

        tutorialContent.SetActive(true);
        arContent.SetActive(false);
        commitMessageButtonImage.color = ToolsController.instance.GetColorFromHexString("#9C9C9C");
        messageInputfield.text = "";

        if (ARController.instance != null) {

            selfieFrame.GetComponentInParent<Canvas>(true).worldCamera = ARController.instance.mainCamera.GetComponent<Camera>();
            ARController.instance.StopARSession(); 
        }
    }

    public void StartSelfieGame()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine("StartSelfieGameCoroutine");
    }

    public IEnumerator StartSelfieGameCoroutine()
    {
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine(useSelfieCamera));
        yield return new WaitForSeconds(0.25f);
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }

	    if(fixFrontFacingCameraFullscreen && WebcamController.instance.cameraImage.transform.localScale.x != 1){ WebcamController.instance.cameraImage.transform.localScale *= 1.01f;}
	    
        tutorialContent.SetActive(false);
        arContent.SetActive(true);
        background.SetActive(false);

        yield return new WaitForSeconds(0.5f);
        dragMenu.Open();

        isLoading = false;
    }

    public void SelectSelfieFrame(int index)
    {
        for (int i = 0; i < selfieFrames.Count; i++) { 

            if (i == index)
            {
                selfieFrames[index].transform.GetChild(0).gameObject.SetActive(!selfieFrames[index].transform.GetChild(0).gameObject.activeInHierarchy);
                if (!selfieFrames[index].transform.GetChild(0).gameObject.activeInHierarchy) { currentSelfieFrame = -1; }
                else { currentSelfieFrame = index; }
            }
            else
            {
                selfieFrames[i].transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }

    public void CommitSelectSelfieFrame()
    {
        if(currentSelfieFrame >= 0 && currentSelfieFrame < selfieFrames.Count)
        {
            selfieFrameImage.sprite = selfieFrames[currentSelfieFrame].transform.GetChild(1).GetComponent<Image>().sprite;
            selfieFrame.SetActive(true);
        }
        else
        {
            selfieFrame.SetActive(false);
        }

        dragMenu.Close();
    }

    public void SelectBanderole(int index)
    {
        for (int i = 0; i < banderolen.Count; i++)
        {
            if (i == index)
            {
                banderolen[index].transform.GetChild(0).gameObject.SetActive(!banderolen[index].transform.GetChild(0).gameObject.activeInHierarchy);
                if (!banderolen[index].transform.GetChild(0).gameObject.activeInHierarchy) { currentBanderole = -1; }
                else { currentBanderole = index; }
            }
            else
            {
                banderolen[i].transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }

    public void WithoutBanderole()
    {
        selfieFrame.SetActive(true);
        banderole.SetActive(false);
        dragMenu.Close();
    }

    public void CommitSelectBanderole()
    {
        if (currentBanderole >= 0 && currentBanderole < banderolen.Count)
        {
            banderoleImage.sprite = banderolen[currentBanderole].transform.GetChild(1).GetComponent<Image>().sprite;
            banderole.SetActive(true);
        }
        else
        {
            banderole.SetActive(false);
        }

        selfieFrame.SetActive(true);
        dragMenu.Close();
    }

    public void OnPhotoTaken()
    {
        float sizeY = photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().rect.height;
        float aspect = photoHelper.photoPreviewImage.GetComponent<Image>().sprite.rect.width / photoHelper.photoPreviewImage.GetComponent<Image>().sprite.rect.height;
        float sizeX = aspect * sizeY;

        photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeX, photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().sizeDelta.y);

        postCardImage.sprite = photoHelper.photoPreviewImage.sprite;
        postCardImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
    }

    public void CommitPhoto()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(CommitPhotoCoroutine());
    }

    public IEnumerator CommitPhotoCoroutine()
    {
        yield return null;

        //messageInputfield.text = "";
        messageContent.SetActive(true);
        messageInputfield.Select();

        isLoading = false;
    }

    public void OnInputMessageChanged()
    {
        if(messageInputfield.text != "")
        {
            commitMessageButtonImage.color = ToolsController.instance.GetColorFromHexString("#812B3B");
        }
        else
        {
            commitMessageButtonImage.color = ToolsController.instance.GetColorFromHexString("#9C9C9C");
        }
    }

    public void BackMessage()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackMessageCoroutine());
    }

    public IEnumerator BackMessageCoroutine()
    {
        yield return null;
        messageContent.SetActive(false);
        isLoading = false;
    }

    public void CommitMessage()
    {
        /*
        if(messageInputfield.text == "")
        {
            InfoController.instance.ShowMessage("Bitte trage eine Nachricht ein");
            return;
        }
        */

        if (isLoading) return;
        isLoading = true;
        StartCoroutine(CommitMessageCoroutine());
    }

    public IEnumerator CommitMessageCoroutine()
    {
        yield return null;

        postCardUIContent.SetActive(true);
        postCardMessageInputfield.text = messageInputfield.text;

        yield return null;

        RenderTexture rtTmp = new RenderTexture(1480, 1050, 24);
        postCardCamera.targetTexture = rtTmp;
        postCardTex = new Texture2D(1480, 1050, TextureFormat.RGBA32, false);
        postCardCamera.Render();
        RenderTexture.active = rtTmp;

        postCardTex.ReadPixels(new Rect(0, 0, 1480, 1050), 0, 0);
        postCardTex.Apply();
        postCardCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rtTmp);

#if UNITY_EDITOR
        string savePath = Application.persistentDataPath + "/" + "Photo_" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss-fff") + ".png";
        byte[] bytes = postCardTex.EncodeToPNG();
        System.IO.File.WriteAllBytes(savePath, bytes);
#endif

        yield return null;

        Sprite newSprite = Sprite.Create(postCardTex, new Rect(0, 0, postCardTex.width, postCardTex.height), new Vector2(0.5f, 0.5f));
        resultPostCardImage.sprite = newSprite;

        yield return null;


        postCardUIContent.SetActive(false);
        messageContent.SetActive(false);
        resultContent.SetActive(true);

        isLoading = false;
    }

    public void SharePostcard()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(SharePostcardCoroutine());
    }

    public IEnumerator SharePostcardCoroutine()
    {
        yield return null;

        string subject = "";
        new NativeShare()
            .SetSubject(subject)
            .AddFile(postCardTex)
            .SetCallback((result, shareTarget) => OnShareResult(result, shareTarget))
            .Share();

        isLoading = false;
    }

    public void OnShareResult(NativeShare.ShareResult result, string shareTarget)
    {
        Debug.Log("Share result: " + result + ", selected app: " + shareTarget);
    }


    public void SavePostcard()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(SavePostcardCoroutine());
    }

    public IEnumerator SavePostcardCoroutine()
    {
        yield return null;

        PhotoController.Instance.photoTex = postCardTex;
        photoHelper.SavePhoto();

        isLoading = false;
    }

    public void Repeat()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(RepeatCoroutine());
    }

    public IEnumerator RepeatCoroutine()
    {
        yield return null;

        messageContent.SetActive(false);
        resultContent.SetActive(false);
        postCardUIContent.SetActive(false);
        photoPreview.SetActive(false);

        isLoading = false;
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
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
        yield return new WaitForSeconds(0.25f);

        WebcamController.instance.DisablePhotoCamera();
        yield return new WaitForSeconds(0.5f);
        useSelfieCamera = !useSelfieCamera;
        yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine(useSelfieCamera));
		
	    if(fixFrontFacingCameraFullscreen && WebcamController.instance.cameraImage.transform.localScale.x != 1){ WebcamController.instance.cameraImage.transform.localScale *= 1.01f;}
	    
        switchCameraUI.SetActive(false);
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }

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
            CommitBack();
        }
    }

    public void CommitBack()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        yield return null;

        Reset();
        yield return StartCoroutine(InitCoroutine());

        isLoading = false;
    }
  
    public void Reset()
    {
        if (ScanController.instance != null) { ScanController.instance.DisableScanCoroutine(); }
        StopCoroutine("StartSelfieGameCoroutine");
        MediaCaptureController.instance.Reset();

        arContent.SetActive(false);
        background.SetActive(true);
        tutorialContent.SetActive(false);
        messageContent.SetActive(false);
        resultContent.SetActive(false);
        postCardUIContent.SetActive(false);
        photoPreview.SetActive(false);

        photoHelper.Reset();
        dragMenu.CloseImmediate();

        selfieFrame.gameObject.SetActive(false);
        banderole.gameObject.SetActive(false);
        for (int i = 0; i < selfieFrames.Count; i++) { selfieFrames[i].transform.GetChild(0).gameObject.SetActive(false); }
        for (int i = 0; i < banderolen.Count; i++) { banderolen[i].transform.GetChild(0).gameObject.SetActive(false); }
        currentSelfieFrame = 0;
        currentBanderole = -1;

        WebcamController.instance.DisablePhotoCamera();
    }
}
