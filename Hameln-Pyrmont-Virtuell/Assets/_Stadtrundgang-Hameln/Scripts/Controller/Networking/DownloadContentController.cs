using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;
using System.Net.NetworkInformation;

public class DownloadContentController : MonoBehaviour
{
	private bool canSkipDownload = true;

	public List<DataElement> dataElements = new List<DataElement>();

	private UnityEngine.Object[] jsonFiles;
	private UnityWebRequest uwr;
	private bool isLoading = false;
	private Texture2D tex;

	public static DownloadContentController instance;
	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		StartCoroutine(InitCoroutine());    
	}

	public IEnumerator InitCoroutine()
	{
        InfoController.instance.loadingCircle.SetActive(true);
        yield return new WaitForSeconds(0.25f);

		// Determine if ARTracking is supported by Android device
#if UNITY_ANDROID
		yield return StartCoroutine(
			PermissionController.instance.CheckARFoundationSupportedCoroutine()
		);
#endif

		// Optional: Move files from Resources to persistentDataPath (works only with uncompressed images)
		yield return StartCoroutine(LoadLocalImagesCoroutine());

		// Optional: Move raw files from streamingAssetsPath to persistentDataPath
		yield return StartCoroutine(MoveFilesController.instance.SaveFilesToPersitDataPathCoroutine());

        // Optional: load local destination.one jsons from Resources add collect images to download
        dataElements.Clear();
		jsonFiles = Resources.LoadAll("_destinationOne", typeof(TextAsset));
		for(int i = 0; i < jsonFiles.Length; i++){

			string savePath = Application.persistentDataPath + "/" + jsonFiles[i].name + ".json";
			TextAsset jsonTextAsset = (TextAsset)jsonFiles[i];
			JSONNode jsonData = JSONNode.Parse(jsonTextAsset.text);

			yield return StartCoroutine(CollectFilesFromDestinationOne(jsonData));

			if (File.Exists(savePath)) continue;
			File.WriteAllText(savePath, jsonTextAsset.text);

			print("Saved local file to PersistentDataPath " + jsonFiles[i].name);
		}

        // Download jsons from Backend and init tour list
        yield return StartCoroutine(ServerBackendController.instance.SyncBackendCoroutine());

		try { TourController.instance.InitTours(); }
		catch (Exception e) { print("Error " + e.Message); }

		// Collect image, video files etc. from json and show option to download them
        yield return StartCoroutine(CheckUpdateDataCoroutine());

        InfoController.instance.loadingCircle.SetActive(false);
    }

    public IEnumerator LoadLocalImagesCoroutine()
	{
		yield return null;

		// Important: This only works with uncompressed Textures

        UnityEngine.Object[] images = Resources.LoadAll("_Images", typeof(Texture2D));
        for (int i = 0; i < images.Length; i++)
		{
			Texture2D tex = (Texture2D)images[i];

            string savePath = Application.persistentDataPath + "/" + images[i].name + ".jpg";
            TextureFormat texFormat = tex.format;

            bool isPNG = false;

#if UNITY_EDITOR
            isPNG = tex.alphaIsTransparency;	// this only works in Editor
#endif

			if (isPNG){ savePath = Application.persistentDataPath + "/" + images[i].name + ".png"; }
            else{ savePath = Application.persistentDataPath + "/" + images[i].name + ".jpg"; }

            if (File.Exists(savePath)) { print("Exists " + savePath); continue; }
            if (!tex.isReadable) { print("Not readable " + savePath); continue; }

            if (isPNG) { File.WriteAllBytes(savePath, tex.EncodeToPNG()); }
            else { File.WriteAllBytes(savePath, tex.EncodeToJPG()); }
        }
    }

	public IEnumerator CollectFilesFromDestinationOne(JSONNode jsonData)
	{
		if (jsonData == null) yield break;
		if (jsonData["items"] == null) yield break;
		if (jsonData["items"].Count <= 0) yield break;
		JSONNode dataJson = jsonData["items"][0];

		if (dataJson["media_objects"] != null)
		{
			for (int i = 0; i < dataJson["media_objects"].Count; i++)
			{
				JSONNode nodeData = dataJson["media_objects"][i];
				if (nodeData["rel"] != null && nodeData["url"] != null )
				{
					if(nodeData["rel"].Value == "default" || nodeData["rel"].Value == "imagegallery") { AddURL(nodeData["url"]); }
				}
			}
		}
	}

	public IEnumerator CheckUpdateDataCoroutine()
	{
		JSONNode toursJson = ServerBackendController.instance.GetJson("_tours");
		for (int i = 0; i < toursJson["tours"].Count; i++)
		{
			AddURL(toursJson["tours"][i]["imageURL"], "tour");
		}

		JSONNode stationsJson = ServerBackendController.instance.GetJson("_stations");
		for (int i = 0; i < stationsJson["stations"].Count; i++)
		{
			JSONNode stationData = stationsJson["stations"][i];
            AddURL(stationData["imageURL"], "station");
            AddURL(stationData["imageMapURL"], "stationMap");

            JSONNode featureData = stationData["features"];
			if (featureData == null) continue;

			for (int j = 0; j < featureData.Count; j++)
			{
				JSONNode featureNode = featureData[j];
				switch (featureNode["id"].Value)
				{
				case "guide": 

                    if (featureNode["video"] != null && featureNode["video"].Value != "") { AddURL(featureNode["video"]); }
                    if (featureNode["videoBackground"] != null && featureNode["videoBackground"].Value != "") { AddURL(featureNode["videoBackground"]); }
                    if (featureNode["audioBackground"] != null && featureNode["audioBackground"].Value != "") { AddURL(featureNode["audioBackground"]); }
                    break;
					
				case "info": 
				
					if (featureNode["imageURL"] != null && featureNode["imageURL"].Value != "") { AddURL(featureNode["imageURL"]); }
					if (featureNode["images"] != null)
					{
						for (int k = 0; k < featureNode["images"].Count; k++) { AddURL(featureNode["images"][k]["url"]); }
					}
					break;
					
				case "gallery":

					for (int k = 0; k < featureNode["images"].Count; k++){ AddURL(featureNode["images"][k]["url"], "gallery");}
					break;

				case "video": 

					AddURL(featureNode["url"]); 
					if(featureNode["infoImage"] != null) { AddURL(featureNode["infoImage"]); }
					break;

                case "audio":

                    if (featureNode["audios"] != null){ for (int k = 0; k < featureNode["audios"].Count; k++) { AddURL(featureNode["audios"][k]["url"], "audio"); } }
                    if (featureNode["url"] != null) { AddURL(featureNode["url"], "audio"); }
                    if (featureNode["infoImage"] != null) { AddURL(featureNode["infoImage"]); }
                    break;

                case "panorama":

					if (featureNode["panoramas"] != null)
					{
						for (int k = 0; k < featureNode["panoramas"].Count; k++)
						{
							AddURL(featureNode["panoramas"][k]["url"], "panorama");

							if (featureNode["panoramas"][k]["infoPoints"] != null)
							{
								for (int u = 0; u < featureNode["panoramas"][k]["infoPoints"].Count; u++)
								{
									AddURL(featureNode["panoramas"][k]["infoPoints"][u]["image"]);
								}
							}
						}
					}

					if (featureNode["url"] != null) { AddURL(featureNode["url"], "panorama"); }
					else if (featureNode["images"] != null) {
						for (int k = 0; k < featureNode["images"].Count; k++) { AddURL(featureNode["images"][k]["url"], "panorama"); }
					}

                    if (featureNode["infoImage"] != null) { AddURL(featureNode["infoImage"]); }
                    break;

				case "ar": 
				
					if (featureNode["videoURL"] != null && featureNode["videoURL"].Value != "") { AddURL(featureNode["videoURL"]); }
                    if (featureNode["videoUrl"] != null && featureNode["videoUrl"].Value != "") { AddURL(featureNode["videoUrl"]); }
                    break;
					
				case "game": break;
				}
			}
		}

        JSONNode dalliKlickJson = ServerBackendController.instance.GetJson("_dalliKlick");
		if (dalliKlickJson != null)
		{
			if (dalliKlickJson["images"] != null)
			{
				for (int i = 0; i < dalliKlickJson["images"].Count; i++)
				{
					if (dalliKlickJson["images"][i]["imageURL"] != null) { AddURL(dalliKlickJson["images"][i]["imageURL"]); }
					if (dalliKlickJson["images"][i]["imageSolutionURL"] != null) { AddURL(dalliKlickJson["images"][i]["imageSolutionURL"]); }
				}
			}
			else
			{
				for (int i = 0; i < dalliKlickJson.Count; i++)
				{
					if (dalliKlickJson[i]["imageURL"] != null) { AddURL(dalliKlickJson[i]["imageURL"]); }
					if (dalliKlickJson[i]["imageSolutionURL"] != null) { AddURL(dalliKlickJson[i]["imageSolutionURL"]); }
				}
			}
		}

        if (dataElements.Count > 0)
		{
			if (DownloadContentUI.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("DownloadContentSite")); }

            if (PlayerPrefs.GetInt("DownloadComplete", 0) == 1) { for (int i = 0; i < DownloadContentUI.instance.skipButtons.Count; i++) { DownloadContentUI.instance.skipButtons[i].SetActive(true); } }
            else { for (int i = 0; i < DownloadContentUI.instance.skipButtons.Count; i++) { DownloadContentUI.instance.skipButtons[i].SetActive(canSkipDownload); } }
            DownloadContentUI.instance.newDataAvailableContent.SetActive(true);
		}
		else
		{
			yield return StartCoroutine(SkipDownloadCoroutine());
		}
	}

	public void AddURL(JSONNode urlNode, string mediaType = "default")
	{
		if (urlNode == null) return;
		for(int i = 0; i < dataElements.Count; i++){if (dataElements[i].url.Contains(urlNode.Value)) return;}
		if (urlNode.Value == "") return;
		if (!urlNode.Value.StartsWith("http")) return;
		if (FileExits(urlNode.Value, mediaType)) return;
		if (urlNode.Value.EndsWith(".gif")) return;

		DataElement dataElement = new DataElement(urlNode.Value, mediaType, 0);
		dataElements.Add(dataElement);
	}

	public bool FileExits(string url, string mediaType)
	{
        List<int> maxSizes = new List<int>() { 8196, 4096, 2048, 1024, 512, 256, 128, 64 };

        if (mediaType == "panorama") { maxSizes = new List<int>() { 4096 }; }
        else if (mediaType == "tour") { maxSizes = new List<int>() { 1024 }; }
        else if (mediaType == "station") { maxSizes = new List<int>() { 256 }; }
        else if (mediaType == "stationMap") { maxSizes = new List<int>() { 1024 }; }
        else if (mediaType == "gallery") { maxSizes = new List<int>() { 2048 }; }
        else { maxSizes = new List<int>() { 1024 }; }

        string extensionTmp = Path.GetExtension(url).ToLower();
        if (extensionTmp != ".jpg" && extensionTmp != ".jpeg" && extensionTmp != ".png") {
            maxSizes = new List<int>() { -1 };
        }

        for (int i = 0; i < maxSizes.Count; i++) {

            string savePathTmp = ToolsController.instance.GetSavePathFromURL(url, maxSizes[i]);
			if (!File.Exists(savePathTmp)) { return false; }
        }
		return true;

		//string savePath = ToolsController.instance.GetSavePathFromURL(url, -1);
		//return File.Exists(savePath);
	}

	public string GetVideoFile(JSONNode urlNode)
	{
		if (urlNode == null) return "";

        string savePath = ToolsController.instance.GetSavePathFromURL(urlNode.Value, -1);
		if (File.Exists(savePath)) return savePath;
		return urlNode.Value;
	}

    public string GetAudioFile(JSONNode urlNode)
    {
        if (urlNode == null) return "";

        string savePath = ToolsController.instance.GetSavePathFromURL(urlNode.Value, -1);
	    if (File.Exists(savePath)) return ("file://" + savePath);
        return urlNode.Value;
    }

    public void Download()
	{
		if (isLoading) return;
		isLoading = true;
		StartCoroutine(DownloadCoroutine());
	}

	public IEnumerator DownloadCoroutine()
	{
        DownloadContentUI.instance.newDataAvailableContent.SetActive(false);
        DownloadContentUI.instance.downloadingContent.SetActive(true);
        DownloadContentUI.instance.progressImage.fillAmount = 0;
        DownloadContentUI.instance.downloadInfoLabel.text = "";

		yield return StartCoroutine(DownloadFilesCoroutine());
		PlayerPrefs.SetInt("DownloadComplete", 1);

        yield return new WaitForSeconds(0.25f);
		yield return StartCoroutine(SkipDownloadCoroutine());

		isLoading = false;
	}


	public IEnumerator DownloadFilesCoroutine()
	{
		for (int i = 0; i < dataElements.Count; i++)
		{
			float progress = (float)i / (float)dataElements.Count;
            DownloadContentUI.instance.progressImage.fillAmount = progress;
            DownloadContentUI.instance.downloadInfoLabel.text = (progress * 100).ToString("F0") + "%";
					
			if( IsImage(dataElements[i].url) ){
				
				List<int> maxSizes = new List<int>();
				if (dataElements[i].mediaType == "panorama") { maxSizes = new List<int>() { 4096 }; }
                else if (dataElements[i].mediaType == "tour") { maxSizes = new List<int>() { 1024 }; }
                else if (dataElements[i].mediaType == "station") { maxSizes = new List<int>() { 256 }; }
                else if (dataElements[i].mediaType == "stationMap") { maxSizes = new List<int>() { 1024 }; }
                else if (dataElements[i].mediaType == "gallery") { maxSizes = new List<int>() { 2048 }; }
                else { maxSizes = new List<int>() { 1024 }; }

                bool resize = false;
				if( dataElements[i].mediaType == "gallery" ){ 
					
					//resize = true; 
					resize = false; 
				}
				
				yield return StartCoroutine(
					ImageDownloadController.instance.DownloadTextureCoroutine(
					dataElements[i].url, maxSizes, resize, (bool success, Texture2D tex) => {

						if(success && tex != null){ Destroy(tex); }
					})
				);
				
			}else{
				
				yield return StartCoroutine(
					GetFileCoroutine(dataElements[i], (bool success) => {

                   
					})
				);     
			}
		}

        DownloadContentUI.instance.progressImage.fillAmount = 1;
        DownloadContentUI.instance.downloadInfoLabel.text = "100%";
		
		yield return StartCoroutine(ToolsController.instance.CleanMemoryCoroutine());
	}

	public bool IsImage(string url){
		
		string extension = Path.GetExtension(url).ToLower();
		if( extension == ".jpg" || 
			extension == ".jpeg" || 
			extension == ".png" || 
			extension == ".gif" || 
			extension == ".bmp"
		){ return true; }
		
		return false;
	}

	public IEnumerator GetFileCoroutine(DataElement dataElement, Action<bool> Callback)
	{
		string savePathTemp = ToolsController.instance.GetTmpSavePathFromURL(dataElement.url);
		string savePath = ToolsController.instance.GetSavePathFromURL(dataElement.url, -1);

		uwr = UnityWebRequest.Get(dataElement.url);
		uwr.downloadHandler = new DownloadHandlerFile(savePathTemp);
		uwr.timeout = 3600;

		//yield return uwr.SendWebRequest();
		uwr.SendWebRequest();
		while (!uwr.isDone)
		{
			//currentFileDownloadProgress = uwr.downloadProgress;
			yield return new WaitForEndOfFrame();
		}

		if (uwr.isNetworkError || uwr.isHttpError)
		{
			print("Error downloading data: " + uwr.error);
			if (File.Exists(savePathTemp)) File.Delete(savePathTemp);
			Callback(false);
		}
		else
		{

			//print("Succes downloading data");
			//string savePath = Application.persistentDataPath + "/" + SaveLoadController.instance.GetSaveFolder() + "/" + Path.GetFileName(url);
			//File.WriteAllBytes( savePath, uwr.downloadHandler.data );

			if (File.Exists(savePath))
			{
				File.Delete(savePath);
			}

			File.Copy(savePathTemp, savePath);

			if (File.Exists(savePathTemp))
			{
				File.Delete(savePathTemp);
			}

			Callback(true);
		}
	}

	public void AbortDownload()
	{
		InfoController.instance.ShowCommitAbortDialog("Willst Du das Herunterladen wirklich abbrechen?", CommitAbort);
	}

	public void CommitAbort()
	{
		if(SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "DownloadContentSite") { return; }

		if (uwr != null) uwr.Abort();
		StopAllCoroutines();

		StartCoroutine(SkipDownloadCoroutine());
	}

	public void SkipDownload()
	{
		if (isLoading) return;
		isLoading = true;
		StartCoroutine(SkipDownloadCoroutine());
	}

	public IEnumerator SkipDownloadCoroutine()
	{
        if (PlayerPrefs.GetInt("TutorialShowed", 0) == 1 || !Params.showTutorial)
		{
			yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));
        }
		else
		{
			if (PlayerPrefs.GetInt("introVideoShowed", 0) == 0 && Params.showIntroVideo)
			{
                yield return StartCoroutine(LoadIntroVideoCoroutine());
                PlayerPrefs.SetInt("introVideoShowed", 1);
            }
            else
            {
				if (TutorialController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("TutorialSite")); }
				yield return StartCoroutine(TutorialController.instance.InitTutorialCoroutine());
				yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("TutorialSite"));
			}		
		}

		isLoading = false;
	}

	public IEnumerator LoadIntroVideoCoroutine()
    {
        if (IntroVideoController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("IntroVideoSite")); }
        yield return StartCoroutine(IntroVideoController.instance.InitCoroutine());
		IntroVideoController.instance.continueTutorial = true;
    }
}

[Serializable]
public class DataElement{
	
	public string url = "";
	public string mediaType = "";
	public float size = 0;
	
	public DataElement(string url, string mediaType, float size){
		
		this.url = url;
		this.mediaType = mediaType;
		this.size = size;
	}
}






