using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;
public class VideoFeatureController : MonoBehaviour
{
    public Image infoImage;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoDescription;

    public GameObject tutorialUI;
    public GameObject videoUI;

    private bool isLoading = false;

    public static VideoFeatureController instance;
    void Awake()
    {
        instance = this;
    }

    public IEnumerator InitCoroutine()
    {
        ARMenuController.instance.DisableMenu(true);
        tutorialUI.SetActive(true);
        videoUI.SetActive(false);
        LoadIntroSite();

        if (VideoController.instance.autoPlayVideo)
        {
            VideoController.instance.autoPlayVideo = false;
            tutorialUI.SetActive(false);
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("VideoFeatureSite"));
            LoadVideo();
        }
        else
        {
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("VideoFeatureSite"));
        }

        yield return new WaitForSeconds(0.5f);
        ARController.instance.StopARSession();
    }

    public void LoadIntroSite()
    {
        JSONNode featureData = StationController.instance.GetStationFeature("video");
        if (featureData == null) return;

        if (featureData["infoTitle"] != null) { infoTitle.text = LanguageController.GetTranslationFromNode(featureData["infoTitle"]); }
        else { infoTitle.text = LanguageController.GetTranslation("Stationsvideo"); }

        if (featureData["infoDescription"] != null) { infoDescription.text = LanguageController.GetTranslationFromNode(featureData["infoDescription"]); }
        else { infoDescription.text = LanguageController.GetTranslation("Schau Dir hier ein Video zu dieser Station an."); }

        // Image
        if (featureData["infoImage"] != null && featureData["infoImage"].Value != "")
        {
            ToolsController.instance.ApplyOnlineImage(infoImage, featureData["infoImage"].Value, true);
        }
        else
        {
            Sprite sprite = Resources.Load<Sprite>("UI/Sprites/video");
            infoImage.sprite = sprite;
            infoImage.preserveAspect = true;
        }
    }

    public void LoadVideo()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(LoadVideoCoroutine());
    }

    public IEnumerator LoadVideoCoroutine()
    {
        JSONNode featureData = StationController.instance.GetStationFeature("video");
        if (featureData == null) yield break;

        // Set URL
        VideoController.instance.currentVideoTarget.videoType = VideoTarget.VideoType.URL;
        VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoType = VideoTarget.VideoType.URL;
        VideoController.instance.currentVideoTarget.videoURL = DownloadContentController.instance.GetVideoFile(featureData["url"]);
        VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoURL = DownloadContentController.instance.GetVideoFile(featureData["url"]);

        // Start play video
        VideoController.instance.AdjustVideoRatio(featureData);

        /*
        if (MapController.instance.selectedStationId == "automuseum_melle")
        {
            JSONNode data = JSONNode.Parse("{}");
            data["videoWidth"] = "1080";
            data["videoHeight"] = "1920";
            VideoController.instance.AdjustVideoRatio(data);
        }
        */

        VideoController.instance.currentVideoTarget.isLoaded = false;
        VideoController.instance.PlayVideo();

        // Wait until loaded
        InfoController.instance.loadingCircle.SetActive(true);
        float timer = 10;
        while (!VideoController.instance.currentVideoTarget.isLoaded && timer > 0)
        {
            timer -= Time.deltaTime;
            //print("Loading video");
            yield return null;
        }
        InfoController.instance.loadingCircle.SetActive(false);

        // Update aspect ratio
        if (featureData["videoWidth"] == null && VideoController.instance.videoPlayer.texture != null)
        {
            JSONNode data = JSONNode.Parse("{}");
            data["videoWidth"] = VideoController.instance.videoPlayer.texture.width.ToString();
            data["videoHeight"] = VideoController.instance.videoPlayer.texture.height.ToString();
            VideoController.instance.AdjustVideoRatio(data);
        }

        tutorialUI.SetActive(false);
        videoUI.SetActive(true);
        VideoController.instance.EnableFullscreen();

        isLoading = false;
    }

    public void Back()
    {
        if (tutorialUI.activeInHierarchy) { InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation); }
        else { CommitBack(); }
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

        isLoading = false;
    }

    public void Reset()
    {
        tutorialUI.SetActive(true);
        videoUI.SetActive(false);

		VideoController.instance.StopVideo();
		VideoController.instance.DisableFullscreen();
		VideoController.instance.videoCanvas.SetActive(false);
	}
}
