using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

// Helper class to send a mail with image attachment using sendMail.php on a server

public class SendEmailController : MonoBehaviour
{
	public TMP_Dropdown genderDropDown;
	public TMP_InputField firstNameInputfield;
	public TMP_InputField surNameInputfield;
	public TMP_InputField emailInputfield;
	public TMP_InputField phoneNumberInputfield;
	public Image sendButtonImage;
	//public GameObject emailSendInfo;
	
	private bool isSending = false;
	private bool formCompleted = false;
	private float sendMailMinWaitTime = 1f;
	
	public static SendEmailController instance;
	void Awake(){
		instance = this;
	}

	public void SendEmail(){
		
		if(isSending) return;
		
		if(!formCompleted) {
			InfoController.instance.ShowMessage( "Füllen Sie erst alle Felder aus." );
			return;
		}

		if ( !ToolsController.instance.isValidEmail (emailInputfield.text)) {
			InfoController.instance.ShowMessage( "Diese E-Mail-Adresse ist ungültig." );
			return;
		}		
		
		isSending = true;
		StartCoroutine( "SendEmailCoroutine" );
	}
	
	private IEnumerator SendEmailCoroutine(){
		
		InfoController.instance.loadingCircle.SetActive(true);
		
		string url = "https://app-etagen.die-etagen.de/.../SendMail/sendMail.php";
		WWWForm form = new WWWForm();
		
		form.AddField("email", emailInputfield.text);
		
		string formContent = "Informationen";
		form.AddField("info", formContent);
		//form.AddField ("imgName", System.IO.Path.GetFileName(PhotoController.Instance.filePath));
		//form.AddBinaryData("imgData", PhotoController.Instance.photoBytes);

		float time = Time.time;
		
		WWW www = new WWW(url,form);
		yield return www;
		
		float waitTime = Mathf.Clamp( sendMailMinWaitTime - (Time.time - time), 0, sendMailMinWaitTime );
		yield return new WaitForSeconds(waitTime);
		
		InfoController.instance.loadingCircle.SetActive(false);

		if( string.IsNullOrEmpty(www.error) ){		
			//emailSendInfo.SetActive(true);
		}else{
			
			InfoController.instance.ShowMessage( "Angebot konnte nicht angefordert werden. Stellen Sie sicher, dass eine Internetverbindung besteht." );
		}
		
		isSending = false;
	}

}
