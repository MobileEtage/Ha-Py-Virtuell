using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;

public class FeedbackController : MonoBehaviour
{
    public TMP_InputField inputField;
    public GameObject thumbsUpToggle;
    public GameObject thumbsDownToggle;

    private bool isLoading = false;

    public static FeedbackController instance;
    void Awake()
    {
        instance = this;
    }
    public void Back()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        if( SiteController.instance.previousSite != null)
        {
            if (SiteController.instance.previousSite.siteID == "MapSite")
            {
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));
            }
            else
            {
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));
            }
        }
        else
        {
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));
        }

        Reset();
        isLoading = false;
    }

    public void ToggleThumbs(bool thumbsUp)
    {
        thumbsDownToggle.SetActive(!thumbsUp);
        thumbsUpToggle.SetActive(thumbsUp);
    }

    public void SendFeedback()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(SendFeedbackCoroutine());
    }

    public IEnumerator SendFeedbackCoroutine()
    {
        string feedbackText = inputField.text;
        if( feedbackText == "" && !thumbsDownToggle.activeInHierarchy && !thumbsUpToggle.activeInHierarchy) { 

            InfoController.instance.ShowMessage("Bitte trage Dein Feedback ein.");
            isLoading = false;
            yield break;
        }

        InfoController.instance.loadingCircle.SetActive(true);
        yield return new WaitForSeconds(0.25f);

        bool isSuccess = true;
        yield return StartCoroutine(
            SendFeedbackoroutine((bool success, string data) => {

                isSuccess = success;                
            })
        );

        if (isSuccess)
        {
	        InfoController.instance.ShowMessage("Vielen Dank für Dein Feedback!");
            inputField.text = "";
            thumbsDownToggle.SetActive(false);
            thumbsUpToggle.SetActive(false);
        }
        else
        {
            //InfoController.instance.ShowMessage("Funktion noch nicht implementiert.");
	        InfoController.instance.ShowMessage("Es ist ein unerwarteter Fehler aufgetreten. Bitte versuche es später nochmal.");
        }

        InfoController.instance.loadingCircle.SetActive(false);

        isLoading = false;
    }

    public IEnumerator SendFeedbackoroutine(Action<bool, string> Callback)
    {
        string message = inputField.text;

        //bool isThumbsUp = thumbsUpToggle.activeInHierarchy;
        //bool isThumbsDown = thumbsDownToggle.activeInHierarchy;

        WWWForm form = new WWWForm();

		//string thumbsText = LanguageController.GetTranslation("Der User hat keinen Daumen aktiviert.");
		//if (isThumbsDown) { thumbsText = LanguageController.GetTranslation("Der User hat Daumen nach unten aktiviert."); }
		//else if (isThumbsUp) { thumbsText = LanguageController.GetTranslation("Der User hat Daumen nach oben aktiviert."); }

		form.AddField( "info", "Nachricht:\n" + message );
		//form.AddField( "info", thumbsText + "\n\nNachricht:\n" + message );
		//form.AddField("img", PhotoController.Instance.GetPhotoName());
		//form.AddField("imgName", PhotoController.Instance.GetPhotoName());
		//form.AddBinaryData("imgData", PhotoController.Instance.GetPhotoBytes());

		Debug.Log("Send feedback " + Params.feedbackURL );

		using ( UnityWebRequest www = UnityWebRequest.Post(Params.feedbackURL, form))
        {
            //Authorization
            //www.SetRequestHeader("Authorization", "123456");


            //Use an UploadHandler
            // Drupal Post solution
            //UploadHandler uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));
            //uploadHandler.contentType = "application/x-www-form-urlencoded";
            //www.uploadHandler = uploadHandler;


            // User-Agent
            //www.SetRequestHeader("User-Agent", "DefaultBrowser");

            //Content-Type

            // default
            //www.SetRequestHeader("Content-Type", "application/octet-stream");

            // json header
            //www.SetRequestHeader("Content-Type", "application/json");

            // if using base64 string
            //www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");		

            // if using bytes --> form.AddBinaryData 
            //www.SetRequestHeader("Content-Type", "multipart/form-data");			

            // ???
            //www.SetRequestHeader("Content-Type", "text/plain;charset=UTF-8");

            //Additonal settings
            //www.timeout = 3600;
            //www.chunkedTransfer = false;
            //www.SetRequestHeader("Accept", "application/json");

            www.SendWebRequest();
            float timer = 15;
            while (!www.isDone && timer > 0)
            {
                timer -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            if (timer <= 0)
            {
                Debug.LogError("Error SendFeedbackoroutine, Timeout");
                Callback(false, "");
            }
            else
            {
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError("Error SendFeedbackoroutine: " + www.error);
                    Callback(false, "");
                }
                else
                {
                    print("Sending formular was successful");
                    Callback(true, "");
                }
            }
        }
        
        
        /*
        WWW www = new WWW(url, form);
        yield return www;

        if (string.IsNullOrEmpty(www.error))
        {
            print("Sending formular was successful");
            Callback(true, "");
        }
        else
        {
            Callback(false, "");
        }
        */
    }

    public void Reset()
    {
        inputField.text = "";
        thumbsDownToggle.SetActive(false);
        thumbsUpToggle.SetActive(false);
    }
}
