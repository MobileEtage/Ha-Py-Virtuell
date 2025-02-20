using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SimpleJSON;

public class PostcardController : MonoBehaviour
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
    public Canvas webcamCanvas;

    [Space(10)]

    public GameObject postCardUIContent;
    public GameObject postCardUIContentPhoto;
    public Image postCardImage;
    public Image postCardPhotoImage;
    public TMP_InputField postCardMessageInputfield;
    public TextMeshProUGUI postCardMessageInputfieldMark;
    public TextMeshProUGUI postCardMessageInputfieldUnderline;
    public TMP_InputField postCardMessageInputfieldPhoto;
    public TextMeshProUGUI postCardMessageInputfieldPhotoMark;
    public TextMeshProUGUI postCardMessageInputfieldPhotoUnderline;
    private Texture2D postCardTex;

    public GameObject messageContent;
    public TMP_InputField messageInputfield;
    public Image commitMessageButtonImage;

    public GameObject resultContent;
    public Image resultPostCardImage;
    public Image resultPostCardImagePhoto;

    [Space(10)]

    public List<GameObject> postcardImages = new List<GameObject>();

    private JSONNode dataJson;
    private bool isLoading = false;
    private bool isTakingPhoto = false;
    private int currentPostcardImage = -1;

    public static PostcardController instance;
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

        string stationId = "";
        if(MapController.instance != null) { stationId = MapController.instance.selectedStationId; }

        dataJson = JSONNode.Parse(Resources.Load<TextAsset>("Postcard/postcards").text);
        for (int i = 0; i < dataJson["postcards"].Count; i++)
        {
            if (dataJson["postcards"][i]["stationId"].Value == stationId)
            {
                GameObject image1 = ToolsController.instance.FindGameObjectByName(postcardImages[0], "SpriteImage");
                image1.GetComponent<Image>().sprite = Resources.Load<Sprite>(dataJson["postcards"][i]["imagePaths"][0].Value);
                float aspect = image1.GetComponent<Image>().sprite.rect.width / image1.GetComponent<Image>().sprite.rect.height;
                image1.GetComponent<AspectRatioFitter>().aspectRatio = aspect;

                GameObject image2 = ToolsController.instance.FindGameObjectByName(postcardImages[1], "SpriteImage");
                image2.GetComponent<Image>().sprite = Resources.Load<Sprite>(dataJson["postcards"][i]["imagePaths"][1].Value);
                aspect = image2.GetComponent<Image>().sprite.rect.width / image2.GetComponent<Image>().sprite.rect.height;
                image2.GetComponent<AspectRatioFitter>().aspectRatio = aspect;

                break;
            }
        }

        tutorialContent.SetActive(true);
        arContent.SetActive(false);
        commitMessageButtonImage.color = ToolsController.instance.GetColorFromHexString("#9C9C9C");
        messageInputfield.text = "";

        if (ARController.instance != null) {

            webcamCanvas.worldCamera = ARController.instance.mainCamera.GetComponent<Camera>();
            ARController.instance.StopARSession(); 
        }
    }

    public void ContinueCreatePostcard()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine("ContinueCreatePostcardCoroutine");
    }

    public IEnumerator ContinueCreatePostcardCoroutine()
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

    public void SelectPostcardImage(int index)
    {
        for (int i = 0; i < postcardImages.Count; i++)
        {
            if (i == index)
            {
                postcardImages[index].transform.GetChild(0).gameObject.SetActive(!postcardImages[index].transform.GetChild(0).gameObject.activeInHierarchy);
                if (!postcardImages[index].transform.GetChild(0).gameObject.activeInHierarchy) { currentPostcardImage = -1; }
                else { currentPostcardImage = index; }
            }
            else
            {
                postcardImages[i].transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }


    public void CommitSelectPostcardImage()
    {
        if (currentPostcardImage >= 0 && currentPostcardImage < postcardImages.Count)
        {
            isTakingPhoto = false;

            GameObject image = ToolsController.instance.FindGameObjectByName(postcardImages[currentPostcardImage], "SpriteImage");
            float aspect = image.GetComponent<Image>().sprite.rect.width / image.GetComponent<Image>().sprite.rect.height;
            postCardImage.sprite = image.GetComponent<Image>().sprite;
            postCardImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;

            CommitPhoto();
            //dragMenu.Close();
        }
        else
        {
            InfoController.instance.ShowMessage("Wähle erst ein Bildmotiv!");   
        }
    }

    public void ContinueWithPhoto()
    {
        isTakingPhoto = true;
        dragMenu.Close();
    }

    public void OnPhotoTaken()
    {
        isTakingPhoto = true;

        float sizeY = photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().rect.height;
        float aspect = photoHelper.photoPreviewImage.GetComponent<Image>().sprite.rect.width / photoHelper.photoPreviewImage.GetComponent<Image>().sprite.rect.height;
        float sizeX = aspect * sizeY;

        photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeX, photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().sizeDelta.y);

        postCardPhotoImage.sprite = photoHelper.photoPreviewImage.sprite;
        postCardPhotoImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
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
            commitMessageButtonImage.color = ToolsController.instance.GetColorFromHexString("#6CB931");
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

    public void BackToWriteMessage()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackToWriteMessageCoroutine());
    }

    public IEnumerator BackToWriteMessageCoroutine()
    {
        yield return null;
        messageContent.SetActive(true);
        resultContent.SetActive(false);
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

        Camera postCardCamera = postCardUIContent.GetComponentInChildren<Camera>(true);
        if (isTakingPhoto) 
        {
            postCardCamera = postCardUIContentPhoto.GetComponentInChildren<Camera>(true);
            postCardUIContent.SetActive(false);
            postCardUIContentPhoto.SetActive(true);
        }
        else 
        {
            postCardUIContent.SetActive(true);
            postCardUIContentPhoto.SetActive(false);        
        }

        // Texts
        string monospace = "<mspace=0.8em>";
        monospace = "";
        postCardMessageInputfieldPhoto.text = monospace + messageInputfield.text;
        postCardMessageInputfieldPhotoMark.text = monospace + "<mark=#ffffff><color=#ffffff00>" + messageInputfield.text;
        postCardMessageInputfield.text = monospace + messageInputfield.text;
        postCardMessageInputfieldMark.text = monospace + "<mark=#ffffff><color=#ffffff00>" + messageInputfield.text;

        string text = messageInputfield.text;
        List<string> characters = new List<string>();
        for (int i = 0; i < text.Length; i++) { characters.Add(text[i].ToString()); }
        text = "";
        for (int i = 0; i < characters.Count; i++)
        {
            //text += ".";
            if (characters[i] == "\n") { text += "\n"; }
            else if (characters[i] == " ") { text += " "; }
            else { text += "."; }
        }
        postCardMessageInputfieldUnderline.text = monospace + "<indent=-0.5%>" + text;
        postCardMessageInputfieldPhotoUnderline.text = monospace + "<indent=-0.5%>" + text;

        yield return null;

        int w = 1480;
        int h = 1050;
        if (isTakingPhoto) { w = 1050; h = 1480; }
        RenderTexture rtTmp = new RenderTexture(w, h, 24);
        postCardCamera.targetTexture = rtTmp;
        postCardTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        postCardCamera.Render();
        RenderTexture.active = rtTmp;

        postCardTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
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
        if (isTakingPhoto) {

            resultPostCardImage.gameObject.SetActive(false);
            resultPostCardImagePhoto.gameObject.SetActive(true);
            resultPostCardImagePhoto.sprite = newSprite; 
        }
        else {

            resultPostCardImage.gameObject.SetActive(true);
            resultPostCardImagePhoto.gameObject.SetActive(false);
            resultPostCardImage.sprite = newSprite; 
        }

        yield return null;


        postCardUIContent.SetActive(false);
        postCardUIContentPhoto.SetActive(false);
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

        //dragMenu.OpenImmediate();
        messageContent.SetActive(false);
        resultContent.SetActive(false);
        postCardUIContent.SetActive(false);
        postCardUIContentPhoto.SetActive(false);
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
        StopCoroutine("ContinueCreatePostcardCoroutine");
        if (MediaCaptureController.instance != null) { MediaCaptureController.instance.Reset(); }

        arContent.SetActive(false);
        background.SetActive(true);
        tutorialContent.SetActive(false);
        messageContent.SetActive(false);
        resultContent.SetActive(false);
        postCardUIContent.SetActive(false);
        postCardUIContentPhoto.SetActive(false);
        photoPreview.SetActive(false);

        photoHelper.Reset();
        dragMenu.CloseImmediate();

        for (int i = 0; i < postcardImages.Count; i++) { postcardImages[i].transform.GetChild(0).gameObject.SetActive(false); }
        currentPostcardImage = -1;

        isTakingPhoto = false;
        WebcamController.instance.DisablePhotoCamera();
    }
}
