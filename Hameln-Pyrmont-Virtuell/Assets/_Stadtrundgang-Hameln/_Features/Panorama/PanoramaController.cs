using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;

public class PanoramaController : MonoBehaviour
{
    public JSONNode featureJSON;
    public int panoramaButtonIndex = 0;
    public List<GameObject> panoramaButtons = new List<GameObject>();

    [Space(10)]

    public Image infoImage;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoDescription;

    [Space(10)]

    public GameObject tutorialUI;
    public GameObject panoramaUI;

    [Space(10)]

    public GameObject gyroCamera;
    public GameObject panoramSphere;
    public GameObject navigationDataHolder;

    [Space(10)]

    public GameObject infoDialog;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI descriptionLabel;
    public Image previewImage;
    public Sprite dummySprite;

    [Space(10)]

    public GameObject panoramaInfoDialog;
    public GameObject panoramaInfoButton;
    public TextMeshProUGUI panoramaTitleLabel;
    public TextMeshProUGUI panoramaDescriptionLabel;

    private bool isLoading = false;
    private bool isLandscape = false;
    private int currentIndex = 0;
    private JSONNode dataJson;
    private GameObject clickedObject;

    public static PanoramaController instance;
    void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "PanoramaSite") return;

        MoveGyroCamera();
        UpdateHit();
        Zoom();

        if(OrientationUIController.instance == null)
        {
            isLandscape = false;
            gyroCamera.GetComponent<Camera>().fieldOfView = 90;
        }
        else
        {
            if (!isLandscape && OrientationUIController.instance.isLandscape && gyroCamera.GetComponent<Camera>().fieldOfView != 60)
            {
                isLandscape = true;
                gyroCamera.GetComponent<Camera>().fieldOfView = 60;
            }
            else if (isLandscape && !OrientationUIController.instance.isLandscape && gyroCamera.GetComponent<Camera>().fieldOfView != 90)
            {

                isLandscape = false;
                gyroCamera.GetComponent<Camera>().fieldOfView = 90;
            }
        }

        /*
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            NextPreviousPanorama(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextPreviousPanorama(1);
        }
        #endif
        */
    }

    public IEnumerator InitCoroutine()
    {
        if (dataJson == null) { dataJson = JSONNode.Parse(Resources.Load<TextAsset>("panorama").text); }

        // Optional: Multiple Buttons
        panoramaButtonIndex = 0;
        /*
        if (MapController.instance.selectedStationId == "geschichtspfad2"){

            panoramaButtons[0].GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Kirche St. Martinus");
            panoramaButtons[1].GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Töpfereimuseum");
            panoramaButtons[1].SetActive(true);
        }
        else {

            panoramaButtons[0].GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Okay, Verstanden");
            panoramaButtons[0].GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Okay, Verstanden");
            panoramaButtons[1].SetActive(false);
        }
        */

        GameObject stationsRoot = ToolsController.instance.FindGameObjectByName(this.gameObject, "StationsRoot");
        foreach (Transform child in stationsRoot.transform) { child.gameObject.SetActive(false); }
        GameObject station = ToolsController.instance.FindGameObjectByName(this.gameObject, MapController.instance.selectedStationId);
        //if (station != null) { station.SetActive(true); }

        ARMenuController.instance.DisableMenu(true);
        tutorialUI.SetActive(true);
        panoramaUI.SetActive(false);
        LoadIntroSite();

        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("PanoramaSite"));

        yield return new WaitForSeconds(0.5f);
        ARController.instance.StopARSession();
    }

    public void LoadIntroSite()
    {
        JSONNode featureData = StationController.instance.GetStationFeature("panorama");
        if (featureData == null) return;

        if (featureData["infoTitle"] != null) { infoTitle.text = LanguageController.GetTranslationFromNode(featureData["infoTitle"]); }
        else { infoTitle.text = LanguageController.GetTranslation("360° Panorama"); }

        if (featureData["infoDescription"] != null) { infoDescription.text = LanguageController.GetTranslationFromNode(featureData["infoDescription"]); }
        else { infoDescription.text = LanguageController.GetTranslation("Schau Dir ein Panoramabild zu dieser Station an."); }

        // Image
        if (featureData["infoImage"] != null && featureData["infoImage"].Value != "")
        {
            ToolsController.instance.ApplyOnlineImage(infoImage, featureData["infoImage"].Value, true);
        }
        else
        {
            Sprite sprite = Resources.Load<Sprite>("UI/Sprites/360");
            infoImage.sprite = sprite;
            infoImage.preserveAspect = true;
        }
    }

    public void LoadPanorama(int buttonIndex)
    {
        if (isLoading) return;
        isLoading = true;
        panoramaButtonIndex = buttonIndex;

        /*
        if (MapController.instance.selectedStationId == "geschichtspfad2")
        {
            if (panoramaButtonIndex == 0)
            {
                GameObject stationsRoot = ToolsController.instance.FindGameObjectByName(this.gameObject, "StationsRoot");
                foreach (Transform child in stationsRoot.transform) { child.gameObject.SetActive(false); }
                GameObject station = ToolsController.instance.FindGameObjectByName(this.gameObject, MapController.instance.selectedStationId);
                if (station != null) { station.SetActive(true); }
            }
            else
            {
                GameObject stationsRoot = ToolsController.instance.FindGameObjectByName(this.gameObject, "StationsRoot");
                foreach (Transform child in stationsRoot.transform) { child.gameObject.SetActive(false); }
            }
        }
        */

        StartCoroutine(LoadPanoramaCoroutine());
    }

    public void LoadPanorama()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(LoadPanoramaCoroutine());
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

    public void NextPreviousPanorama(int dir)
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(NextPreviousPanoramaCoroutine(dir));
    }

    public IEnumerator NextPreviousPanoramaCoroutine(int dir)
    {
        JSONNode featureData = StationController.instance.GetStationFeature("panorama");
        if (featureData == null) yield break;
        if (featureData["images"] == null) yield break;

        if (currentIndex+dir >= featureData["images"].Count) { currentIndex = 0; }
        else if (currentIndex+dir < 0) { currentIndex = featureData["images"].Count - 1; }
        else{ currentIndex += dir; }

	    bool isSuccess = false;
	    yield return StartCoroutine(
		    LoadPanoramaCoroutine(currentIndex, (bool success) => {
			    isSuccess = success;
		    })
	    );

	    if(!isSuccess){
	    	
	    	InfoController.instance.ShowMessage("Panorama-Bild konnte nicht geladen werden.");
	    }

        isLoading = false;
    }

    public IEnumerator LoadPanoramaCoroutine()
    {
        if(OrientationUIController.instance == null)
        {
            isLandscape = false;
            gyroCamera.GetComponent<Camera>().fieldOfView = 90;
        }
        else
        {
            if (OrientationUIController.instance.isLandscape)
            {
                isLandscape = true;
                gyroCamera.GetComponent<Camera>().fieldOfView = 60;
            }
            else if (!OrientationUIController.instance.isLandscape)
            {
                isLandscape = false;
                gyroCamera.GetComponent<Camera>().fieldOfView = 90;
            }
        }
        
        bool isSuccess = false;
	    yield return StartCoroutine(
		    LoadPanoramaCoroutine(0, (bool success) => {
			    isSuccess = success;
		    })
	    );
	    
	    if(!isSuccess){

		    InfoController.instance.ShowMessage("Panorama-Bild konnte nicht geladen werden. Versuche es später erneut.");
            isLoading = false;
            yield break;
	    }

        tutorialUI.SetActive(false);
        panoramaUI.SetActive(true);
        panoramSphere.SetActive(true);
        gyroCamera.SetActive(true);
        Input.gyro.enabled = true;

        isLoading = false;
    }

	public IEnumerator LoadPanoramaCoroutine(int panoramaIndex, Action<bool> Callback)
    {
        currentIndex = panoramaIndex;

        JSONNode featureData = StationController.instance.GetStationFeature("panorama");
        if (featureData == null) yield break;

        /*
        if (MapController.instance.selectedStationId == "geschichtspfad2" && panoramaButtonIndex == 1)
        {
            JSONNode featureDataTmp = JSONNode.Parse(Resources.Load<TextAsset>("panoramas-Toepfermuseum").text);
            featureData["panoramas"] = featureDataTmp["panoramas"];
        }
        */

        /*
        if (MapController.instance.selectedStationId == "geschichtspfad11")
        {
            JSONNode featureDataTmp = JSONNode.Parse(Resources.Load<TextAsset>("panoramas-Toepfermuseum").text);
            featureData["panoramas"] = featureDataTmp["panoramas"];
        }
        */
        featureJSON = featureData;

        if (featureData["panoramas"] != null && currentIndex < featureData["panoramas"].Count && currentIndex >= 0)
        {
            print("Loading panorama " + currentIndex);

            bool isSuccess = false;
            yield return StartCoroutine(
                LoadPanoramaCoroutine(featureData["panoramas"][currentIndex], (bool success) => {
                    isSuccess = success;
                })
            );
            Callback(isSuccess);
        }
        else if (featureData["url"] != null)
        {
            print("Loading panorama " + featureData["url"].Value);

            bool isSuccess = false;
            yield return StartCoroutine(
                LoadPanoramaCoroutine(featureData["url"].Value, (bool success) => {
                    isSuccess = success;
                })
            );
            Callback(isSuccess);
        }
        else
        {
            if (featureData["images"] != null && currentIndex < featureData["images"].Count && currentIndex >= 0)
            {
                print("Loading panorama " + currentIndex);

                bool isSuccess = false;
                yield return StartCoroutine(
                    LoadPanoramaCoroutine(featureData["images"][currentIndex]["url"].Value, (bool success) => {
                        isSuccess = success;
                    })
                );
                Callback(isSuccess);
            }
        }
    }

    public IEnumerator LoadPanoramaCoroutine(JSONNode dataNode, Action<bool> Callback)
    {
        InfoController.instance.loadingCircle.SetActive(true);

        bool isSuccess = false;
        string savePath = "";
        yield return StartCoroutine(
            LoadSourceCoroutine(dataNode["url"], (bool success, string path) => {

                isSuccess = success;
                savePath = path;
            })
        );

        if (isSuccess)
        {
            yield return StartCoroutine(ApplySourceCoroutine(savePath));
            yield return StartCoroutine(LoadPanoramaInfoDataCoroutine(dataNode));
            Callback(true);
        }
        else
        {
            Callback(false);
        }

        InfoController.instance.loadingCircle.SetActive(false);
    }

    public IEnumerator LoadPanoramaInfoDataCoroutine(JSONNode dataNode)
    {
        yield return null;

        // Panorama info
        if(dataNode["title"] != null || dataNode["description"] != null)
        {
            panoramaTitleLabel.text = LanguageController.GetTranslationFromNode(dataNode["title"]);
            panoramaDescriptionLabel.text = LanguageController.GetTranslationFromNode(dataNode["description"]);
            panoramaInfoButton.SetActive(true);
        }

        foreach (Transform child in navigationDataHolder.transform){ Destroy(child.gameObject); }

        // InfoPoints
        if (dataNode["infoPoints"] != null)
        {
            for (int i = 0; i < dataNode["infoPoints"].Count; i++)
            {
                int index = i;
                GameObject obj = ToolsController.instance.InstantiateObject("PanoramaInfoPoint", navigationDataHolder.transform);
                obj.GetComponentInChildren<LookAt>().target = gyroCamera.transform;
                obj.GetComponent<NavPoint>().nodeData = dataNode["infoPoints"][index];

                float x = 0;
                float y = 0;
                float z = 0;
                if (dataNode["infoPoints"][index]["coordinates"] != null)
                {
                    if (dataNode["infoPoints"][index]["coordinates"]["x"] != null) { x = ToolsController.instance.GetFloatValueFromJsonNode(dataNode["infoPoints"][index]["coordinates"]["x"]); }
                    if (dataNode["infoPoints"][index]["coordinates"]["y"] != null) { y = ToolsController.instance.GetFloatValueFromJsonNode(dataNode["infoPoints"][index]["coordinates"]["y"]); }
                    if (dataNode["infoPoints"][index]["coordinates"]["z"] != null) { z = ToolsController.instance.GetFloatValueFromJsonNode(dataNode["infoPoints"][index]["coordinates"]["z"]); }
                }

                obj.transform.localPosition = new Vector3(x,y,z);
            }
        }

        // NavPoints
        if (dataNode["navigationPoints"] != null)
        {
            for (int i = 0; i < dataNode["navigationPoints"].Count; i++)
            {
                int index = i;
                GameObject obj = ToolsController.instance.InstantiateObject("PanoramaNavPoint", navigationDataHolder.transform);
                obj.GetComponentInChildren<LookAt>().target = gyroCamera.transform;
                obj.GetComponent<NavPoint>().nodeData = dataNode["navigationPoints"][index];

                float x = 0;
                float y = 0;
                float z = 0;
                if (dataNode["navigationPoints"][index]["coordinates"] != null)
                {
                    if (dataNode["navigationPoints"][index]["coordinates"]["x"] != null) { x = ToolsController.instance.GetFloatValueFromJsonNode(dataNode["navigationPoints"][index]["coordinates"]["x"]); }
                    if (dataNode["navigationPoints"][index]["coordinates"]["y"] != null) { y = ToolsController.instance.GetFloatValueFromJsonNode(dataNode["navigationPoints"][index]["coordinates"]["y"]); }
                    if (dataNode["navigationPoints"][index]["coordinates"]["z"] != null) { z = ToolsController.instance.GetFloatValueFromJsonNode(dataNode["navigationPoints"][index]["coordinates"]["z"]); }
                }

                obj.transform.localPosition = new Vector3(x, y, z);
            }
        }
    }

    public IEnumerator LoadPanoramaCoroutine(string url, Action<bool> Callback)
    {
        InfoController.instance.loadingCircle.SetActive(true);

        bool isSuccess = false;
        string savePath = "";
        yield return StartCoroutine(
            LoadSourceCoroutine(url, (bool success, string path) => {

                isSuccess = success;
                savePath = path;
            })
        );

        if (isSuccess)
        {
            yield return StartCoroutine(ApplySourceCoroutine(savePath));
	        Callback(true);
        }
        else{
	        Callback(false);
        }

        InfoController.instance.loadingCircle.SetActive(false);
    }

    public IEnumerator LoadSourceCoroutine(string url, Action<bool, string> Callback)
    {
        print("LoadSourceCoroutine " + url);

        if (url.ToLower().EndsWith(".png") || url.ToLower().EndsWith(".jpg") || url.ToLower().EndsWith("jpeg"))
        {
            string savePath = ToolsController.instance.GetSavePathFromURL(url, 4096);
            if (File.Exists(savePath)) { Callback(true, savePath); yield break; }

            List<int> maxSizes = new List<int>() { 4096 };
	        yield return StartCoroutine(
		        ImageDownloadController.instance.DownloadTextureCoroutine(url, maxSizes, false, (bool success, Texture2D tex) => {

			        print("Done download");
			        if (success)
			        {
				        Callback(true, savePath);
			        }
			        else
			        {
				        Callback(false, "");
			        }
		        })
	        );
        }
        else if (url.ToLower().EndsWith(".mp4") || url.ToLower().EndsWith(".webm"))
        {
            string savePath = ToolsController.instance.GetSavePathFromURL(url, -1);
            if (File.Exists(savePath)) { Callback(true, savePath); yield break; }

            InfoController.instance.loadingCircle.SetActive(true);
            
	        DataElement dataElement = new DataElement(url, "video", 0);
            yield return StartCoroutine(
                DownloadContentController.instance.GetFileCoroutine(dataElement, (bool success) => {

                    print("Done download");
                    if (success)
                    {
                        Callback(true, savePath);
                    }
                    else
                    {
                        Callback(false, "");
                    }
                })
            );
            InfoController.instance.loadingCircle.SetActive(false);
        }

    }

    public IEnumerator ApplySourceCoroutine(string savePath)
    {
        VideoController.instance.StopVideo();

		if (savePath.ToLower().EndsWith(".png") || savePath.ToLower().EndsWith(".jpg") || savePath.ToLower().EndsWith("jpeg"))
        {
            Texture2D tex = null;
            byte[] fileData;

            fileData = File.ReadAllBytes(savePath);
            tex = new Texture2D(2, 2, TextureFormat.RGB24, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.LoadImage(fileData);

            panoramSphere.GetComponent<Renderer>().material.mainTexture = tex;
        }
        else if (savePath.ToLower().EndsWith(".mp4") || savePath.ToLower().EndsWith(".webm"))
        {
            VideoController.instance.currentVideoTarget.videoType = VideoTarget.VideoType.URL;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoType = VideoTarget.VideoType.URL;
            VideoController.instance.currentVideoTarget.videoURL = savePath;
            VideoController.instance.videoSite.GetComponentInChildren<VideoTarget>(true).videoURL = savePath;

            VideoController.instance.currentVideoTarget.isLoaded = false;
            VideoController.instance.PlayVideo();

            InfoController.instance.loadingCircle.SetActive(true);
            float timer = 10;
            while (!VideoController.instance.currentVideoTarget.isLoaded && timer > 0)
            {
                timer -= Time.deltaTime;
                print("Loading video");
                yield return null;
            }
            InfoController.instance.loadingCircle.SetActive(false);

            if (VideoController.instance.videoPlayer.texture != null)
            {
                panoramSphere.GetComponent<Renderer>().material.mainTexture = VideoController.instance.videoPlayer.texture;
            }
        }
    }

    public void MoveGyroCamera()
    {
#if !UNITY_EDITOR
		gyroCamera.transform.localEulerAngles = GetGyroRotation();
#endif
    }

    public Vector3 GetGyroRotation()
    {
        Quaternion gyroQuaternion = GyroToUnity(Input.gyro.attitude);
        Quaternion calculatedRotation = Quaternion.Euler(90f, 0f, 0f) * gyroQuaternion;

#if UNITY_ANDROID
        //calculatedRotation = Input.gyro.attitude * new Quaternion(0f, 0f, 1f, 0f);
#endif
        return calculatedRotation.eulerAngles;
    }

    private Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    public void ShowInfo(GameObject navPoint)
    {
        if (isLoading) return;
        isLoading = false;
        StartCoroutine("ShowInfoCoroutine", navPoint);
    }

    public IEnumerator ShowInfoCoroutine(GameObject navPoint)
    {
        yield return null;

        for (int i = 0; i < dataJson["panoramas"].Count; i++)
        {
            if (MapController.instance.selectedStationId == dataJson["panoramas"][i]["id"].Value)
            {

                JSONNode nodeData = GetInfoData(dataJson["panoramas"][i], navPoint.GetComponent<NavPoint>().id);
                if (nodeData == null) continue;

                // Texts
                titleLabel.text = LanguageController.GetTranslation(nodeData["title"].Value);
                descriptionLabel.text = LanguageController.GetTranslation(nodeData["description"].Value);

                // Image
                previewImage.transform.parent.parent.gameObject.SetActive(false);
                previewImage.GetComponent<UIImage>().url = "";
                previewImage.sprite = dummySprite;
                if (nodeData["imageURL"] != null && nodeData["imageURL"].Value != "")
                {
                    previewImage.transform.parent.parent.gameObject.SetActive(true);
                    ToolsController.instance.ApplyOnlineImage(previewImage, nodeData["imageURL"].Value, true);
                }
                else if (nodeData["image"] != null && nodeData["image"].Value != "")
                {
                    previewImage.transform.parent.parent.gameObject.SetActive(true);
                    Sprite sprite = Resources.Load<Sprite>(nodeData["image"].Value);
                    previewImage.sprite = sprite;

                    previewImage.GetComponent<AspectRatioFitter>().enabled = true;
                    previewImage.GetComponent<AspectRatioFitter>().aspectRatio = sprite.bounds.size.x / sprite.bounds.size.y;
                }
            }
        }

        infoDialog.SetActive(true);
        isLoading = false;
    }

    public JSONNode GetInfoData(JSONNode node, string id)
    {
        for (int i = 0; i < node["infoPoints"].Count; i++)
        {
            if (node["infoPoints"][i]["id"].Value == id) { return node["infoPoints"][i]; }
        }
        return null;
    }

    public void UpdateHit()
    {
        //if (Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject())
        if (Input.GetMouseButtonDown(0) && !InfoController.instance.commitAbortDialog.activeInHierarchy)
        {
            RaycastHit[] hits;
            Ray ray = gyroCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.GetComponent<NavPoint>() != null)
                {
                    clickedObject = hits[i].transform.gameObject;
                    break;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0) && !InfoController.instance.commitAbortDialog.activeInHierarchy)
        {
            RaycastHit[] hits;
            Ray ray = gyroCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.GetComponent<NavPoint>() != null && hits[i].transform.gameObject == clickedObject)
                {
                    if (hits[i].transform.GetComponent<NavPoint>().navPointType == "showInfo")
                    {
                        ShowInfo(hits[i].transform.gameObject);
                    }
                    else if (hits[i].transform.GetComponent<NavPoint>().navPointType == "ShowPanoramaInfo")
                    {
                        ShowPanoramaInfo(hits[i].transform.gameObject);
                    }
                    else if (hits[i].transform.GetComponent<NavPoint>().navPointType == "SwitchPanoramaPosition")
                    {
                        SwitchPanoramaPosition(hits[i].transform.gameObject);
                    }
                    break;
                }
            }
            clickedObject = null;
        }
    }

    public void ShowPanoramaInfo(GameObject obj)
    {
        if (isLoading) return;
        isLoading = false;
        StartCoroutine("ShowPanoramaInfoCoroutine", obj);
    }

    public IEnumerator ShowPanoramaInfoCoroutine(GameObject obj)
    {
        yield return null;

        JSONNode nodeData = obj.GetComponent<NavPoint>().nodeData;

        // Texts
        titleLabel.text = LanguageController.GetTranslationFromNode(nodeData["title"]);
        descriptionLabel.text = LanguageController.GetTranslationFromNode(nodeData["description"]);

        // Image
        previewImage.transform.parent.parent.gameObject.SetActive(false);
        previewImage.GetComponent<UIImage>().url = "";
        previewImage.sprite = dummySprite;
        if (nodeData["url"] != null && nodeData["url"].Value != "")
        {
            previewImage.transform.parent.parent.gameObject.SetActive(true);
            ToolsController.instance.ApplyOnlineImage(previewImage, nodeData["url"].Value, true);
        }
        else if (nodeData["image"] != null && nodeData["image"].Value != "")
        {
            previewImage.transform.parent.parent.gameObject.SetActive(true);
            ToolsController.instance.ApplyOnlineImage(previewImage, nodeData["image"].Value, true);
        }
        /*
        else if (nodeData["image"] != null && nodeData["image"].Value != "")
        {
            previewImage.transform.parent.parent.gameObject.SetActive(true);
            Sprite sprite = Resources.Load<Sprite>(nodeData["image"].Value);
            previewImage.sprite = sprite;

            previewImage.GetComponent<AspectRatioFitter>().enabled = true;
            previewImage.GetComponent<AspectRatioFitter>().aspectRatio = sprite.bounds.size.x / sprite.bounds.size.y;
        }
        */

        infoDialog.SetActive(true);
        isLoading = false;
    }

    public void SwitchPanoramaPosition(GameObject obj)
    {
        if (isLoading) return;
        isLoading = false;
        StartCoroutine("SwitchPanoramaPositionCoroutine", obj);
    }

    public IEnumerator SwitchPanoramaPositionCoroutine(GameObject obj)
    {
        yield return null;

        string targetPanoramaId = "";
        if (obj.GetComponent<NavPoint>().nodeData["panoramaId"] != null) { targetPanoramaId = obj.GetComponent<NavPoint>().nodeData["panoramaId"].Value; }

        if (featureJSON != null && featureJSON["panoramas"] != null) {

            for (int i = 0; i < featureJSON["panoramas"].Count; i++)
            {
                if(featureJSON["panoramas"][i]["panoramaId"] != null && featureJSON["panoramas"][i]["panoramaId"].Value == targetPanoramaId)
                {
                    bool isSuccess = false;

                    // Todo: Fade black

                    yield return StartCoroutine(
                        LoadPanoramaCoroutine(featureJSON["panoramas"][i], (bool success) => {
                            isSuccess = success;
                        })
                    );

                    // Todo: Remove black

                    break;
                }
            }
        }

        isLoading = false;
    }

    public void OpenDetails()
    {
        panoramaInfoDialog.SetActive(true);
    }

    private float oldDist = -1;
    public void Zoom()
    {
        float minFieldOfView = 10;
        float maxFieldOfView = 120;
        float zoomFaktor = 4f;

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                Vector2 t1 = new Vector2((touch1.position.x / Screen.width), (touch1.position.y / Screen.height));
                Vector2 t2 = new Vector2((touch2.position.x / Screen.width), (touch2.position.y / Screen.height));

                if (oldDist < 0) { oldDist = Vector2.Distance(t1, t2); }
                float curDist = Vector2.Distance(t1, t2);
                float scaleFaktor = Mathf.Abs(oldDist - curDist) * zoomFaktor;

                if (curDist > oldDist)
                {
                    gyroCamera.GetComponent<Camera>().fieldOfView += -scaleFaktor * 100;
                    gyroCamera.GetComponent<Camera>().fieldOfView = Mathf.Clamp(gyroCamera.GetComponent<Camera>().fieldOfView, minFieldOfView, maxFieldOfView);
                }
                else if (curDist < oldDist)
                {
                    gyroCamera.GetComponent<Camera>().fieldOfView += scaleFaktor * 100;
                    gyroCamera.GetComponent<Camera>().fieldOfView = Mathf.Clamp(gyroCamera.GetComponent<Camera>().fieldOfView, minFieldOfView, maxFieldOfView);
                }
                oldDist = curDist;
            }
        }
        else
        {
            oldDist = -1;
        }
    }

    public void Reset()
    {
        //JSONNode featureData = StationController.instance.GetStationFeature("panorama");
        //if (MapController.instance.selectedStationId == "geschichtspfad2" && featureData["panoramas"] != null) { featureData.Remove("panoramas"); }
        //if (MapController.instance.selectedStationId == "geschichtspfad11" && featureData["panoramas"] != null) { featureData.Remove("panoramas"); }
        foreach (Transform child in navigationDataHolder.transform) { Destroy(child.gameObject); }

        featureJSON = null;
        tutorialUI.SetActive(true);
        panoramaUI.SetActive(false);
        clickedObject = null;

        Input.gyro.enabled = false;
        gyroCamera.SetActive(false);
        panoramSphere.SetActive(false);
        panoramSphere.GetComponent<Renderer>().material.mainTexture = null;
        Screen.orientation = ScreenOrientation.Portrait;
    }
}
