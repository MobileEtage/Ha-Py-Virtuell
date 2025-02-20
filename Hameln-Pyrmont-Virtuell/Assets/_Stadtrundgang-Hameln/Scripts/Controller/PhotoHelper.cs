using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class PhotoHelper : MonoBehaviour
{
	public GameObject capturedPhotoOptions;
	public GameObject savedInfo;
	public Image photoPreviewImage;
	//public RectTransform photoPreviewImageRectTransform;
	//public Image imageTimerFill;
	//public TextMeshProUGUI countdownLabel;
	//public Image timerImage;

	[Space(10)]
	
	public List<GameObject> hideAfterActivateContent = new List<GameObject>();

	[Space(10)]

	public UnityEvent OnPhotoTaken;
	public UnityEvent OnShareSuccess;
	
	private bool isLoading = false;
	private bool isTakingPicture = false;
    
	/*
	public void TakePhotoDelayed(){
		
		if( isTakingPicture ){
			
			StopCoroutine( "TakePhotoDelayedCoroutine" );
			if( timerImage ) timerImage.color = ToolsController.instance.GetColorFromHexString("#183B3F");
			if( countdownLabel ) countdownLabel.text = "";
			isTakingPicture = false;
			return;
		}
		
		isTakingPicture = true;
		StartCoroutine( "TakePhotoDelayedCoroutine" );
	}
	
	public IEnumerator TakePhotoDelayedCoroutine(){
		
		if( timerImage ) timerImage.color = ToolsController.instance.GetColorFromHexString("#DE007B");
		//imageTimerFill.gameObject.SetActive(true);
		//imageTimerFill.fillAmount = 1;
		
		float timer = 10;
		while( timer > 0 ){
			
			timer -= Time.deltaTime;
			//imageTimerFill.fillAmount = timer/10f;
			
			int time = (int)Mathf.Clamp( timer, 0, 10)+1;
			if( countdownLabel ) countdownLabel.text = time.ToString();
			yield return new WaitForEndOfFrame();
		}
		
		//imageTimerFill.fillAmount = 0;
		//imageTimerFill.gameObject.SetActive(false);
		if( countdownLabel ) countdownLabel.text = "";
		if( timerImage ) timerImage.color = ToolsController.instance.GetColorFromHexString("#183B3F");

		yield return StartCoroutine( TakePhotoCoroutine() );
		
		isTakingPicture = false;
	}
	*/
	
	public void TakePhoto(){

        if (VideoCaptureController.instance != null && VideoCaptureController.instance.captureBase.IsCapturing()) return;
        if ( isTakingPicture ){ return; }
		if( isLoading ) return;
		isLoading = true;
		StartCoroutine( TakePhotoCoroutine() );
	}
	
	public IEnumerator TakePhotoCoroutine(){

		//int marginBottom = GetPixelHeight( capturedPhotoOptions.GetComponent<RectTransform>() );
		//int photoHeight = GetPixelHeight( photoPreviewImageRectTransform.GetComponent<RectTransform>() );

		Camera myCamera = PhotoController.Instance.mainCamera;
        //if (SelfieController.instance != null ) { myCamera = WebcamController.instance.webcamCamera; }
        //if (SelfieGameController.instance != null && SelfieGameController.instance.IsUsingWebcam()) { myCamera = WebcamController.instance.webcamCamera; }

        yield return StartCoroutine(
			//PhotoController.Instance.CapturePhotoCoroutine(marginBottom, photoHeight, (Texture2D photo) => {  
			PhotoController.Instance.CapturePhotoCoroutine(myCamera, 0, Screen.height, true, (Texture2D photo) => {  
				
				Sprite newSprite = Sprite.Create( photo, new Rect(0, 0, photo.width, photo.height), new Vector2(0.5f, 0.5f) );
				photoPreviewImage.sprite = newSprite;
				photoPreviewImage.preserveAspect = true;
			})
		);

		yield return StartCoroutine(PhotoController.Instance.PlayTakePhotoAnimationCoroutine());

		photoPreviewImage.gameObject.SetActive(true);
		if (capturedPhotoOptions){ capturedPhotoOptions.SetActive(true); }
		ARMenuController.instance.CloseMenu();
		
		OnPhotoTaken.Invoke();
		
		isLoading = false;
	}

	
	public void SavePhoto(){

		if( isLoading ) return;
		isLoading = true;
		StartCoroutine( SavePhotoCoroutine() );
	}
	
	public IEnumerator SavePhotoCoroutine(){
		
		bool isSuccess = false;
		yield return StartCoroutine(
			PhotoController.Instance.SavePhotoCoroutine((bool success) => {  
				
				isSuccess = success;
			})
		);
		
		if( isSuccess ){

			if (savedInfo) { savedInfo.SetActive(true); }
			yield return new WaitForSeconds(3.0f);
			if (savedInfo) { savedInfo.SetActive(false); }
		}
		else
		{
			
			InfoController.instance.ShowMessage("Das Foto konnte nicht gespeichert werden.");
		}
		
		isLoading = false;
	}
	
	public void AbortSavePhoto(){
		
		photoPreviewImage.gameObject.SetActive(false);
		if (capturedPhotoOptions) { capturedPhotoOptions.SetActive(false); }
	}
	
	public void SharePhoto(){
		
		string subject = "";
		
		new NativeShare()
			.SetSubject( subject )
			.AddFile(PhotoController.Instance.savePath)
			.SetCallback( ( result, shareTarget ) => OnShareResult(result, shareTarget) )
			.Share();
		
	}
	
	public void OnShareResult( NativeShare.ShareResult result, string shareTarget ){
		
		Debug.Log( "Share result: " + result + ", selected app: " + shareTarget );
		
		if( result == NativeShare.ShareResult.Shared ){
			OnShareSuccess.Invoke();
		}
	}
	
	public int GetPixelHeight( RectTransform rectTransform ){
		
		float height = rectTransform.GetComponent<RectTransform>().rect.height;
		
		Canvas rootCanvas = rectTransform.GetComponentInParent<Canvas>().rootCanvas;
		float canvasHeight = rootCanvas.GetComponent<RectTransform>().rect.height;
		float percentage = height/canvasHeight;
		float pixelHeight = percentage*Screen.height;
		
		return (int)pixelHeight;
	}
	
	public void EnablePhotoContent(){
		
		for( int i = 0; i < hideAfterActivateContent.Count; i++ ){
			hideAfterActivateContent[i].SetActive(false);
		}
	}
	
	public void DisablePhotoContent(){
		
		for( int i = 0; i < hideAfterActivateContent.Count; i++ ){
			hideAfterActivateContent[i].SetActive(true);
		}
	}
	
	public void Reset(){
		
		StopAllCoroutines();
		photoPreviewImage.gameObject.SetActive(false);
		if (capturedPhotoOptions) { capturedPhotoOptions.SetActive(false); }
		//if( imageTimerFill != null ){ imageTimerFill.gameObject.SetActive(false); }
		//if( countdownLabel ) countdownLabel.text = "";
		//if( timerImage ) timerImage.color = ToolsController.instance.GetColorFromHexString("#183B3F");
		
		isLoading = false;
		isTakingPicture = false;
	}
	
	public bool IsTakingPicture(){
		
		if( isLoading ) return true;
		return false;
	}
}
