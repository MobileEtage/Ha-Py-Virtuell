using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;
using System;

public class TourController : MonoBehaviour
{
    public Transform listHolder;
    public ScrollRect listScrollRect;
    public GameObject mainImageContent;
    public Image mainImage;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI subTitleLabel;
    public TextMeshProUGUI descriptionLabel;
    public TextMeshProUGUI distanceLabel;
    public TextMeshProUGUI durationLabel;
    public GameObject tourInfo;
    public TextMeshProUGUI tourInfoLabel;

    public string currentTourId = "";

    private bool isLoading = false;

    public static TourController instance;
    void Awake()
    {

        instance = this;
    }

    public void InitTours()
    {
        foreach (Transform child in listHolder) { if ( child.name == "Feedback" ) { continue; } Destroy(child.gameObject); }

        JSONNode toursJson = ServerBackendController.instance.GetJson("_tours");
        print("InitTours " + toursJson.ToString());

        for (int i = 0; i < toursJson["tours"].Count; i++)
        {
            int index = i;
            JSONNode tourData = toursJson["tours"][index];
            GameObject listElement = ToolsController.instance.InstantiateObject("UI/TourListPrefab", listHolder);

			// Image
			//if (toursJson["tours"].Count <= 2) { listElement.GetComponent<LayoutElement>().minHeight = 900; }
			//else { listElement.GetComponent<LayoutElement>().minHeight = 600; }

			if ( tourData["imageURL"] != null && tourData["imageURL"].Value != "")
            {
                ToolsController.instance.ApplyOnlineImage(listElement.GetComponent<TourListElement>().previewImage, tourData["imageURL"].Value, true);
				listElement.GetComponent<TourListElement>().updateHeight = true;
			}
            else if (tourData["image"] != null && tourData["image"].Value != "")
            {
                Sprite sprite = Resources.Load<Sprite>(tourData["image"].Value);

				if (sprite != null)
                {
					listElement.GetComponent<TourListElement>().previewImage.sprite = sprite;
                    listElement.GetComponent<TourListElement>().previewImage.GetComponent<AspectRatioFitter>().enabled = true;
                    listElement.GetComponent<TourListElement>().previewImage.GetComponent<AspectRatioFitter>().aspectRatio = sprite.bounds.size.x / sprite.bounds.size.y;
					listElement.GetComponent<TourListElement>().updateHeight = true;
					//listElement.GetComponent<TourListElement>().UpdateHeight();
				}
			}

            // Texts
            listElement.GetComponent<TourListElement>().titleLabel.text = LanguageController.GetTranslationFromNode(tourData["title"]);
			if ( tourData["title2"] != null ) { listElement.GetComponent<TourListElement>().titleLabel.text += "\n<size=70%><color=#BBD4E8>" + LanguageController.GetTranslationFromNode( tourData["title2"] ); }
			listElement.GetComponent<TourListElement>().stationsLabel.text = tourData["stations"].Count + " " + LanguageController.GetTranslation("Stationen");
            if (tourData["stations"].Count == 1) listElement.GetComponent<TourListElement>().stationsLabel.text = tourData["stations"].Count + " " + LanguageController.GetTranslation("Station");

            if (listElement.GetComponent<TourListElement>().tourInfo != null) { listElement.GetComponent<TourListElement>().tourInfo.SetActive(true); }
            if (listElement.GetComponent<TourListElement>().distanceLabel != null) { listElement.GetComponent<TourListElement>().distanceLabel.text = LanguageController.GetTranslationFromNode(tourData["distance"]); }
            if (listElement.GetComponent<TourListElement>().durationLabel != null) { listElement.GetComponent<TourListElement>().durationLabel.text = LanguageController.GetTranslationFromNode(tourData["duration"]); }

            // Button event
            listElement.GetComponentInChildren<Button>().onClick.AddListener(() => OpenTour(tourData));
        }

		foreach ( Transform child in listHolder ) { if ( child.name == "Feedback" ) { child.SetAsLastSibling(); break; } }
	}

	public void OpenTour(JSONNode tourData)
    {
        print("OpenTour " + tourData["id"].Value);

        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenTourCoroutine(tourData));
    }

    public IEnumerator OpenTourCoroutine(JSONNode tourData)
    {
		// To update UI sizes, we need to activate the site and wait one frame --> images will be scaled correctly
		SiteController.instance.ShowHideSite( "TourOverviewSite", true, 0 );
		yield return null;

		try
		{

			if (currentTourId == tourData["id"].Value)
            {
                StationController.instance.MarkStationsFinished();
            }
            else
            {

                currentTourId = tourData["id"].Value;

                // Image
                if (tourData["imageURL"] != null && tourData["imageURL"].Value != "")
                {
                    ToolsController.instance.ApplyOnlineImage(mainImage, tourData["imageURL"].Value, true);
                }
                else if (tourData["imageOverview"] != null && tourData["imageOverview"].Value != "")
                {
                    Sprite sprite = Resources.Load<Sprite>(tourData["imageOverview"].Value);
                    mainImage.sprite = sprite;

                    float width = mainImageContent.GetComponent<RectTransform>().rect.width;
                    float aspectRatio = sprite.bounds.size.y / sprite.bounds.size.x;
                    mainImageContent.GetComponent<LayoutElement>().minHeight = aspectRatio * width;

				}
				else if ( tourData["image"] != null && tourData["image"].Value != "" )
				{
					Sprite sprite = Resources.Load<Sprite>( tourData["image"].Value );
					mainImage.sprite = sprite;

					float width = mainImageContent.GetComponent<RectTransform>().rect.width;
					float aspectRatio = sprite.bounds.size.y / sprite.bounds.size.x;
					mainImageContent.GetComponent<LayoutElement>().minHeight = aspectRatio * width;
				}

				// Texts
				titleLabel.text = LanguageController.GetTranslationFromNode(tourData["title"]);
                subTitleLabel.text = LanguageController.GetTranslationFromNode(tourData["subTitle"]);
                descriptionLabel.text = LanguageController.GetTranslationFromNode(tourData["description"]);
                distanceLabel.text = LanguageController.GetTranslationFromNode(tourData["distance"]);
                durationLabel.text = LanguageController.GetTranslationFromNode(tourData["duration"]);

                // TourInfo
                if (tourData["tourInfo"] != null)
                {
                    tourInfo.SetActive(true);
                    tourInfoLabel.text = LanguageController.GetTranslationFromNode(tourData["tourInfo"]);
                }
                else
                {
                    tourInfo.SetActive(false);
                }

                // Stations
                StationController.instance.LoadStations(tourData["id"].Value);
            }

        }
        catch (Exception e) { print("Error " + e.Message); }

        listScrollRect.verticalNormalizedPosition = 1;
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("TourOverviewSite"));
        isLoading = false;
    }

    public JSONNode GetTourData()
    {
        if (currentTourId != "") return GetTourData(currentTourId);

        JSONNode dataJson = ServerBackendController.instance.GetJson("_tours");
        if (dataJson == null) return null;
        if (dataJson["tours"].Count == 0) return null;
        return dataJson["tours"][0];
    }

    public int GetTourIndex()
    {
        JSONNode dataJson = ServerBackendController.instance.GetJson("_tours");
        if (dataJson == null) return -1;
        if (dataJson["tours"].Count == 0) return -1;
        for (int i = 0; i < dataJson["tours"].Count; i++) { if (dataJson["tours"][i]["id"].Value == currentTourId) return i; }
        return -1;
    }

    public JSONNode GetTourData(string id)
    {
        JSONNode dataJson = ServerBackendController.instance.GetJson("_tours");
        if (dataJson == null) return null;
        for (int i = 0; i < dataJson["tours"].Count; i++) { if (dataJson["tours"][i]["id"].Value == id) return dataJson["tours"][i]; }
        return null;
    }

    public string GetCurrentTourId()
    {

        if (currentTourId == "")
        {
            JSONNode dataJson = ServerBackendController.instance.GetJson("_tours");
            if (dataJson == null) return "";
            return dataJson["tours"][0]["id"].Value;
        }
        return currentTourId;
    }

    public bool IsTourStation(string stationId)
    {
        JSONNode tourJson = GetTourData(currentTourId);
        if (tourJson == null) return false;
        for (int i = 0; i < tourJson["stations"].Count; i++) { if (tourJson["stations"][i].Value == stationId) return true; }
        return false;
    }

    public bool IsTourMarker(string markerId)
    {
        JSONNode tourJson = GetTourData(currentTourId);
        if (tourJson == null) return true;

        JSONNode stationsJson = ServerBackendController.instance.GetJson("_stations");
        if (stationsJson == null) return true;

        for (int i = 0; i < stationsJson["stations"].Count; i++)
        {
            if (!IsTourStation(stationsJson["stations"][i]["id"].Value)) continue;

            for (int j = 0; j < stationsJson["stations"][i]["marker"].Count; j++)
            {
                if (stationsJson["stations"][i]["marker"][j].Value == markerId) { return true; }
            }
        }
        return false;
    }

    public int GetStationCount()
    {
        JSONNode tourData = GetTourData();
        if (tourData == null) return 0;
        return tourData["stations"].Count;
    }

    public int GetFinishedStationCount()
    {
        JSONNode tourData = GetTourData();
        if (tourData == null) return 0;
        string tourId = tourData["id"].Value;

        int finishedCount = 0;
        for (int i = 0; i < tourData["stations"].Count; i++)
        {

            string stationId = tourData["stations"][i].Value;
            bool finished = (PlayerPrefs.GetInt(tourId + "_" + stationId + "_finished", 0) == 1);
            if (finished) { finishedCount++; }
        }
        return finishedCount;
    }

    public void StartTour()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(StartTourCoroutine());
    }

    public IEnumerator StartTourCoroutine()
	{
        yield return StartCoroutine(MapController.instance.LoadMapFromMenuCoroutine());
		MapFilterController.instance.UpdateCurrentTourHeaderImage(currentTourId);
		isLoading = false;
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused) { 

            PlayerPrefs.Save();
            print("OnApplicationPause PlayerPrefs.Save");
        }
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
        print("OnApplicationQuit PlayerPrefs.Save");
    }
}
