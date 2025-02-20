using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class PhotoCaptureController : MonoBehaviour
{
    public GameObject previewUI;
    public Image photoPreviewImage;
    public GameObject savedInfo;

    [Space(10)]

    public UnityEvent OnPhotoTaken;
    public UnityEvent OnShareSuccess;

    private bool isLoading = false;
    private bool isTakingPicture = false;

    public static PhotoCaptureController instance;
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
        
    }

    public void TakePhoto()
    {
        if (VideoCaptureController.instance.captureBase.IsCapturing()) return;
        if (isTakingPicture) { return; }
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(TakePhotoCoroutine());
    }

    public IEnumerator TakePhotoCoroutine()
    {
        Camera myCamera = PhotoController.Instance.mainCamera;
        //if (SelfieController.instance != null) { myCamera = WebcamController.instance.webcamCamera; }
        //if (SelfieGameController.instance != null && SelfieGameController.instance.IsUsingWebcam()) { myCamera = WebcamController.instance.webcamCamera; }

        yield return StartCoroutine(
            PhotoController.Instance.CapturePhotoCoroutine(myCamera, 0, Screen.height, false, (Texture2D photo) => {

                Sprite newSprite = Sprite.Create(photo, new Rect(0, 0, photo.width, photo.height), new Vector2(0.5f, 0.5f));
                photoPreviewImage.sprite = newSprite;
                photoPreviewImage.preserveAspect = true;
            })
        );

        yield return StartCoroutine(PhotoController.Instance.PlayTakePhotoAnimationCoroutine());

        float sizeY = photoPreviewImage.transform.parent.GetComponent<RectTransform>().rect.height;
        float aspect = photoPreviewImage.GetComponent<Image>().sprite.rect.width / photoPreviewImage.GetComponent<Image>().sprite.rect.height;
        float sizeX = aspect * sizeY;
        photoPreviewImage.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeX, photoPreviewImage.transform.parent.GetComponent<RectTransform>().sizeDelta.y);
        previewUI.SetActive(true);

        OnPhotoTaken.Invoke();

        isLoading = false;
    }

    public void SavePhoto()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(SavePhotoCoroutine());
    }

    public IEnumerator SavePhotoCoroutine()
    {
        bool isSuccess = false;
        yield return StartCoroutine(
            PhotoController.Instance.SavePhotoCoroutine((bool success) => {

                isSuccess = success;
            })
        );

        if (isSuccess)
        {

            savedInfo.SetActive(true);
            yield return new WaitForSeconds(3.0f);
            savedInfo.SetActive(false);
        }
        else
        {
            InfoController.instance.ShowMessage("Das Foto konnte nicht gespeichert werden.");
        }

        isLoading = false;
    }

    public void SharePhoto()
    {
        string subject = "";

        new NativeShare()
            .SetSubject(subject)
            .AddFile(PhotoController.Instance.savePath)
            .SetCallback((result, shareTarget) => OnShareResult(result, shareTarget))
            .Share();
    }

    public void OnShareResult(NativeShare.ShareResult result, string shareTarget)
    {
        Debug.Log("Share result: " + result + ", selected app: " + shareTarget);
        if (result == NativeShare.ShareResult.Shared){ OnShareSuccess.Invoke(); }
    }

    public int GetPixelHeight(RectTransform rectTransform)
    {
        float height = rectTransform.GetComponent<RectTransform>().rect.height;
        Canvas rootCanvas = rectTransform.GetComponentInParent<Canvas>().rootCanvas;
        float canvasHeight = rootCanvas.GetComponent<RectTransform>().rect.height;
        float percentage = height / canvasHeight;
        float pixelHeight = percentage * Screen.height;

        return (int)pixelHeight;
    }

    public void AbortSavePhoto()
    {
        Reset();
    }

    public void Reset()
    {
        StopAllCoroutines();
        previewUI.SetActive(false);

        isLoading = false;
        isTakingPicture = false;
    }
}
