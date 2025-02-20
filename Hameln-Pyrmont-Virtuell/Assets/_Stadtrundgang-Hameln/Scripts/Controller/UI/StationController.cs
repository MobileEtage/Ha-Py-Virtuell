using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;
using System;
using MPUIKIT;

public class StationController : MonoBehaviour
{
    [Header("Misc")]
    public string currentWebsitePOI = "";
	public bool skipScanIfNoGuide = false;
    public bool clickedOnStationInList = false;
    public bool shouldFocusStation = false;

    [Header("Tour overview params")]
    public Transform stationsListHolder;

    [Header("Map params")]
    public Sprite dummySprite;
    public GameObject stationMapDialog;
    public GameObject stationNearByInfo;
    public GameObject stationNumber;
    public GameObject stationTitle;
    public GameObject stationLocation;
    public GameObject stationDescription;
    public GameObject stationCategory;
    public GameObject stationDates;
    public GameObject stationImageRoot;
    public Image stationImage;
    public GameObject moveToLocationInfo;
	public GameObject startStationWithMarkerButton;
	public GameObject startStationButton;
	public GameObject openWebsiteButton;

    //public GameObject scanQRCodeButton;
    //public GameObject qrCodeInfoButton;
    //public GameObject withoutQRCodeButton;
    //public GameObject qrCodeInfoLabel;
    //public GameObject qrCodeInfoImageRoot;
    //public Image qrCodeInfoImage;
    //public DragMenuController qrCodeDragMenu;

    public JSONNode currentStationData;
    private bool isLoading = false;
    private JSONNode selectedStationData;
    public JSONNode currentListStationData;

    public static StationController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {

    }

    public void LoadStations(string tourId)
    {
        foreach (Transform child in stationsListHolder) { Destroy(child.gameObject); }

        JSONNode tourData = TourController.instance.GetTourData(tourId);
        if (tourData == null) return;

        for (int i = 0; i < tourData["stations"].Count; i++)
        {
            int index = i;
            JSONNode stationData = GetStationData(tourData["stations"][index].Value);
            if (stationData == null) continue;
            GameObject listElement = ToolsController.instance.InstantiateObject("UI/StationListPrefab", stationsListHolder);

            // Image
            if (stationData["imageURL"] != null && stationData["imageURL"].Value != "")
            {
                ToolsController.instance.ApplyOnlineImage(listElement.GetComponent<StationListElement>().previewImage, stationData["imageURL"].Value, true);
                listElement.GetComponent<StationListElement>().previewImage.gameObject.GetComponent<UIImage>().SetSizes(new List<int>() { 256 }, 256);
            }
            else if (stationData["image"] != null && stationData["image"].Value != "")
            {
                Sprite sprite = Resources.Load<Sprite>(stationData["image"].Value);
                listElement.GetComponent<StationListElement>().previewImage.sprite = sprite;

                listElement.GetComponent<StationListElement>().previewImage.GetComponent<AspectRatioFitter>().enabled = true;
                float aspect = sprite.rect.width / sprite.rect.height;
                listElement.GetComponent<StationListElement>().previewImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
            }

            // Texts
            listElement.GetComponent<StationListElement>().stationId = stationData["id"].Value;
            listElement.GetComponent<StationListElement>().titleLabel.text = LanguageController.GetTranslationFromNode(stationData["titleOverview"]);


            if (stationData["mapNumber"] != null) { listElement.GetComponent<StationListElement>().numberLabel.text = stationData["mapNumber"].Value; }
            else
            {

                string number = "";
                //if ((index + 1) < 10) { number = "0"; };
                listElement.GetComponent<StationListElement>().numberLabel.text = number + (index + 1);
            }


            // Button event
            listElement.GetComponent<Button>().onClick.AddListener(() => OpenStation(tourData["id"].Value, stationData));

            // Finished
            string stationId = stationData["id"].Value;
            bool finished = (PlayerPrefs.GetInt(tourId + "_" + stationId + "_finished", 0) == 1);

            if (finished)
            {
                listElement.GetComponent<StationListElement>().background.color = Params.stationListElementBackgroundColorFinished;
                listElement.GetComponent<StationListElement>().numberLabel.color = Params.stationListElementNumberLabelColorFinished;
                listElement.GetComponent<StationListElement>().titleLabel.color = Params.stationListElementTitleLabelColorFinished;
            }
            else
            {
                listElement.GetComponent<StationListElement>().background.color = Params.stationListElementBackgroundColorNotFinished;
                listElement.GetComponent<StationListElement>().numberLabel.color = Params.stationListElementNumberLabelColorNotFinished;
                listElement.GetComponent<StationListElement>().titleLabel.color = Params.stationListElementTitleLabelColorNotFinished;
            }
        }

    }

    public void LoadStationInfoOnMap(MapStation mapStation, JSONNode stationData)
    {
		selectedStationData = stationData;

        // Number
        stationNumber.gameObject.SetActive(true);
        string text = LanguageController.GetTranslation("Station") + " " + mapStation.numberText;
        stationNumber.GetComponentInChildren<TextMeshProUGUI>(true).text = text;

        // Title
        stationTitle.gameObject.SetActive(stationData["title"] != null);
        if (stationData["title"] != null) { stationTitle.GetComponentInChildren<TextMeshProUGUI>(true).text = LanguageController.GetTranslationFromNode(stationData["title"]); }

        // Text
        stationDescription.gameObject.SetActive(false);

        // Image
        stationImage.GetComponentInParent<GalleryImage>(true).SetCopyRight("");
		if ( stationData["imageMapURL"] != null && ToolsController.instance.IsValidURL( stationData["imageMapURL"].Value ) )
		{
			stationImageRoot.SetActive( true );
			if ( stationData["imageMapURL"].Value != stationImage.GetComponent<UIImage>().url || stationImage.sprite == null )
			{
				print( "Loading image" );
				stationImage.sprite = dummySprite;
				stationImage.GetComponent<UIImage>().url = "";
				stationImage.GetComponent<AspectRatioFitter>().aspectRatio = 1;
				ToolsController.instance.ApplyOnlineImage( stationImage, stationData["imageMapURL"].Value, true );
				stationImage.gameObject.GetComponent<UIImage>().SetSizes( new List<int>() { 1024 }, 1024 );
			}
		}
		else if ( stationData["imageMap"] != null && stationData["imageMap"].Value != "" )
		{
			if ( PermissionController.instance.IsARFoundationSupported() ) { LoadSpriteIntoImage( stationData["imageMap"].Value, stationImage ); }
			else if( stationData["image"] != null && stationData["image"].Value != "" ){ LoadSpriteIntoImage( stationData["image"].Value, stationImage ); }
			else{ stationImageRoot.SetActive( false ); }
		}
		else if ( stationData["image"] != null && stationData["image"].Value != "" )
		{
			LoadSpriteIntoImage( stationData["image"].Value, stationImage );
		}
		else
		{
			stationImageRoot.SetActive( false );
		}

        stationNearByInfo.SetActive(false);
        openWebsiteButton.SetActive(false);
        stationCategory.SetActive(false);
        stationDates.SetActive(false);
        stationLocation.SetActive(false);
        moveToLocationInfo.SetActive(true);
        startStationButton.SetActive(true);

		bool useGuideInAR = UseGuideInAR( stationData );
		if ( !useGuideInAR || !stationImageRoot.activeSelf ) { moveToLocationInfo.SetActive( false ); }
		if ( !StationHasFeature(stationData["id"].Value, "guide") || (stationData["imageURL"] == null && stationData["image"] == null)) { moveToLocationInfo.SetActive(false); }

		UpdateCustomStations();
		StartCoroutine( DelayedActivateCoroutine());
    }

	public void UpdateCustomStations()
	{
		startStationWithMarkerButton.SetActive(false);
		startStationButton.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation( "Station starten" );
		startStationButton.transform.GetChild( 0 ).GetComponent<MPImage>().StrokeWidth = 0;

		if ( MapController.instance.selectedStationId.Contains( "glashuette" ) )
		{
			if ( PermissionController.instance.IsARFoundationSupported() )
			{
				startStationWithMarkerButton.SetActive( true );
				startStationButton.GetComponentInChildren<TextMeshProUGUI>().text = "<color=#002E57>" + LanguageController.GetTranslation( "Weiter ohne QR-Code" );
				startStationButton.transform.GetChild( 0 ).GetComponent<MPImage>().StrokeWidth = 6;
				moveToLocationInfo.SetActive( true );
			}
		}
	}

	public void LoadSpriteIntoImage(string path, Image image)
	{
		stationImageRoot.SetActive( true );

		Sprite sprite = Resources.Load<Sprite>( path );
		image.sprite = sprite;
		image.GetComponent<AspectRatioFitter>().enabled = true;
		image.GetComponent<AspectRatioFitter>().aspectRatio = sprite.bounds.size.x / sprite.bounds.size.y;
		image.GetComponent<UIImage>().url = "";
	}

    public void LoadStationInfoOnMap(JSONNode stationData)
    {
        // Pre title
        stationNumber.gameObject.SetActive(true);
        string preTitle = "";
        string filterType = stationData["type"] != null ? stationData["type"].Value : "";


        if (filterType != "")
        {

            //if (stationData["type"].Value == "Event") { preTitle = LanguageController.GetTranslation("Veranstaltung"); }
            if (stationData["type"].Value == "Event") { preTitle = LanguageController.GetTranslation(GetGategories(stationData)); }

            //if (stationData["type"].Value == "Gastro") { preTitle = LanguageController.GetTranslation("Restaurant"); }
            if (stationData["type"].Value == "Gastro") { preTitle = LanguageController.GetTranslation(GetGategories(stationData)); }

            if (stationData["type"].Value == "Hotel") { preTitle = LanguageController.GetTranslation("Hotel"); }
        }
        else { stationNumber.gameObject.SetActive(false); }

        if (preTitle != "") { stationNumber.GetComponentInChildren<TextMeshProUGUI>().text = "<color=#" + ColorUtility.ToHtmlStringRGB(Params.mapMenuFiltersBackgroundColor) + ">" + preTitle; }
        else { stationNumber.gameObject.SetActive(false); }

        // Title
        stationTitle.gameObject.SetActive(true);
        if (stationData["title"] != null) { stationTitle.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslationFromNode(stationData["title"], true); }
        else { stationTitle.gameObject.SetActive(false); }

        // Teaser
        stationDescription.gameObject.SetActive(false);
        if (filterType == "Gastro")
        {
            string teaser = DestinationOneController.instance.GetGastroTeaserText(stationData);
            stationDescription.gameObject.SetActive(teaser != "");
            if (teaser != "") { stationDescription.GetComponentInChildren<TextMeshProUGUI>(true).text = LanguageController.GetTranslation(teaser, true); }
        }

        // Categories and timings for Events
        stationCategory.SetActive(false);
        stationDates.SetActive(false);
        stationLocation.SetActive(false);
        if (filterType == "Event")
        {

            LoadEventInfos(stationData);
            string loc = GetLocationName(stationData);
            if (loc != "")
            {
                stationLocation.SetActive(true);
                stationLocation.GetComponentInChildren<TextMeshProUGUI>(true).text = LanguageController.GetTranslation(loc, true);
            }
        }


        // Image
        bool imageFound = false;
        string imageURL = "";
		stationImage.GetComponentInParent<GalleryImage>(true).SetCopyRight("");
        if (stationData["media_objects"] != null)
        {
            for (int i = 0; i < stationData["media_objects"].Count; i++)
            {
                JSONNode nodeData = stationData["media_objects"][i];
                if (nodeData["rel"] != null && nodeData["url"] != null && nodeData["rel"].Value == "default")
                {
                    imageFound = true;
                    imageURL = nodeData["url"].Value;

                    // Copyright
                    if (nodeData["source"] != null) { stationImage.GetComponentInParent<GalleryImage>(true).SetCopyRight(nodeData["source"].Value); }
                    else { print("No copyright"); }

                    break;
                }
            }
        }

        if (imageFound && ToolsController.instance.IsValidURL(imageURL))
        {
            stationImageRoot.SetActive(true);
            if (imageURL != stationImage.GetComponent<UIImage>().url || stationImage.sprite == null)
            {
                print("Loading image 2");

                stationImage.sprite = dummySprite;
                stationImage.GetComponent<UIImage>().url = "";
                stationImage.GetComponent<AspectRatioFitter>().aspectRatio = 1;
                ToolsController.instance.ApplyOnlineImage(stationImage, imageURL, true);
            }
            //stationImage.GetComponent<UIImage>().shouldEnvelopeParent = true;
            //stationImage.GetComponent<UIImage>().envelopeParentDone = false;
        }
        else
        {
            stationImageRoot.SetActive(false);
        }

        // Website
        if (filterType == "Event")
        {
            currentWebsitePOI = GetEventWebLink(stationData);
            openWebsiteButton.SetActive(currentWebsitePOI != "");
        }
        else if (stationData["web"] != null)
        {
            openWebsiteButton.SetActive(true);
            currentWebsitePOI = stationData["web"].Value;
        }
        else
        {
            openWebsiteButton.SetActive(false);
        }

        stationNearByInfo.SetActive(false);
        moveToLocationInfo.SetActive(false);
        startStationButton.SetActive(false);

        //scanQRCodeButton.SetActive(false);
        //qrCodeInfoButton.SetActive(false);
        //withoutQRCodeButton.SetActive(false);

        StartCoroutine(DelayedActivateCoroutine());
    }

    public void LoadEventInfos(JSONNode stationData)
    {
        // Categories
        /*
        string ca = GetGategories(stationData);
        if (ca != "")
        {
            stationCategory.SetActive(true);
            stationCategory.GetComponentInChildren<TextMeshProUGUI>(true).text = LanguageController.GetTranslation(ca, true);
        }
        */

        // Timings
        string timingsText = GetTimings(stationData);
        if (timingsText != "")
        {
            stationDates.SetActive(true);
            stationDates.GetComponentInChildren<TextMeshProUGUI>(true).text = LanguageController.GetTranslation(timingsText, true);
        }
    }

    public string GetGategories(JSONNode stationData)
    {
        string ca = "";
        if (stationData["categories"] != null && stationData["categories"].Count > 0)
        {
            //string ca = "(";
            for (int i = 0; i < stationData["categories"].Count; i++)
            {
                ca += stationData["categories"][i].Value;
                if (i + 1 < stationData["categories"].Count) { ca += ", "; }
            }
            //ca += ")";
        }
        return ca;
    }

    public string GetTimings(JSONNode stationData)
    {
        string timingsText = "";
        if (stationData["timeIntervals"] != null && stationData["timeIntervals"].Count > 0)
        {
            for (int i = 0; i < stationData["timeIntervals"].Count; i++)
            {
                if (stationData["timeIntervals"][i]["start"] != null)
                {
                    DateTime startDate;
                    if (DateTime.TryParse(stationData["timeIntervals"][i]["start"].Value, out startDate))
                    {
                        DateTime now = DateTime.Now;
                        DateTime nextDay = now.AddDays(1);

                        if (startDate < now)
                        {
                            timingsText += LanguageController.GetTranslation("Seit") + startDate.ToString(" dd.MM.yyyy") + ", " + startDate.ToString("HH:mm") + " " + LanguageController.GetTranslation("Uhr");
                        }
                        else if (now.Day == startDate.Day)
                        {
                            timingsText += LanguageController.GetTranslation("Heute") + startDate.ToString(", dd.MM.yyyy") + ", " + startDate.ToString("HH:mm") + " " + LanguageController.GetTranslation("Uhr");
                        }
                        else if (nextDay.Day == startDate.Day)
                        {
                            timingsText += LanguageController.GetTranslation("Morgen") + startDate.ToString(", dd.MM.yyyy") + ", " + startDate.ToString("HH:mm") + " " + LanguageController.GetTranslation("Uhr");
                        }
                        else
                        {
                            timingsText += startDate.ToString("dddd, dd.MM.yyyy") + ": " + startDate.ToString("HH:mm") + " " + LanguageController.GetTranslation("Uhr");
                        }
                    }

                    /*
                    DateTime endDate;
                    if (stationData["timeIntervals"][i]["end"] != null)
                    {
                        if (DateTime.TryParse(stationData["timeIntervals"][i]["end"].Value, out endDate))
                        {
                            if(startDate.Day == endDate.Day && startDate.Month == endDate.Month && startDate.Year == endDate.Year)
                            {
                                timingsText += " bis " + endDate.ToString("HH:mm") + " " + LanguageController.GetTranslation("Uhr");
                            }
                            else
                            {
                                timingsText += " bis\n\n" + endDate.ToString("dddd, dd.MM.yyyy") + ":\n" + endDate.ToString("HH:mm") + " " + LanguageController.GetTranslation("Uhr");
                            }
                        }
                    }
                    */
                }

                // Limit results
                if (i == 0) break;

                if (i + 1 < stationData["timeIntervals"].Count)
                {
                    timingsText += "\n";
                }
            }
        }

        return timingsText;
    }

    public string GetEventWebLink(JSONNode stationData)
    {
        //if (stationData["web"] != null) { return stationData["web"].Value; }
        return Params.eventsWebLink;
    }

    public string GetLocationName(JSONNode stationData)
    {
        if (stationData["name"] != null) { return stationData["name"].Value; }
        return "";
    }

    public IEnumerator DelayedActivateCoroutine()
    {
        yield return null;
        stationMapDialog.SetActive(true);
    }

    public void OpenPOIWebsite()
    {
		if(currentWebsitePOI != ""){
			ToolsController.instance.OpenWebView(currentWebsitePOI);
		}
    }

    public void OpenQRCodeInfo()
    {
        if (selectedStationData == null) return;

        /*
        if (selectedStationData["qrCodeInfoImage"] != null && ToolsController.instance.IsValidURL(selectedStationData["qrCodeInfoImage"].Value))
        {
            qrCodeDragMenu.menuOpenPrecentage = -0.4f;
        }
        else
        {
            qrCodeDragMenu.menuOpenPrecentage = -0.8f;
        }

        qrCodeDragMenu.Open();
        */
    }

    public void MarkStationsFinished()
    {
        foreach (Transform child in stationsListHolder)
        {
            string stationId = child.GetComponent<StationListElement>().stationId;
            string tourId = TourController.instance.GetCurrentTourId();
            bool finished = (PlayerPrefs.GetInt(tourId + "_" + stationId + "_finished", 0) == 1);

            if (finished)
            {
                child.GetComponent<StationListElement>().background.color = Params.stationListElementBackgroundColorFinished;
                child.GetComponent<StationListElement>().numberLabel.color = Params.stationListElementNumberLabelColorFinished;
                child.GetComponent<StationListElement>().titleLabel.color = Params.stationListElementTitleLabelColorFinished;
            }
            else
            {
                child.GetComponent<StationListElement>().background.color = Params.stationListElementBackgroundColorNotFinished;
                child.GetComponent<StationListElement>().numberLabel.color = Params.stationListElementNumberLabelColorNotFinished;
                child.GetComponent<StationListElement>().titleLabel.color = Params.stationListElementTitleLabelColorNotFinished;
            }
        }
    }

    public void OpenStation(string tourId, JSONNode stationData)
    {
        print("OpenStation " + stationData["id"].Value);

        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenStationCoroutine(tourId, stationData));
    }

    public IEnumerator OpenStationCoroutine(string tourId, JSONNode stationData)
    {
        clickedOnStationInList = true;
        yield return StartCoroutine(MapController.instance.LoadMapFromMenuCoroutine());
        clickedOnStationInList = false;

        //MapController.instance.OnClickOnStation(stationData["id"].Value);

        shouldFocusStation = true;
        currentListStationData = stationData;
        if (MapController.instance.mapUIRoot.activeInHierarchy)
        {

            shouldFocusStation = false;
            MapController.instance.FocusStation(TourController.instance.currentTourId, stationData);
        }

        isLoading = false;
    }

    public IEnumerator BackToStationSiteCoroutine()
    {
        if (!ARController.instance.arSession.enabled)
        {
            InfoController.instance.loadingCircle.SetActive(true);
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
            InfoController.instance.loadingCircle.SetActive(false);
        }

        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ARSite"));
    }



    /********************** Helper **********************/

    public JSONNode GetStationData() { return currentStationData; }

    public JSONNode GetStationData(string stationId)
    {
        JSONNode dataJson = ServerBackendController.instance.GetJson("_stations");
        if (dataJson == null) return null;
        for (int i = 0; i < dataJson["stations"].Count; i++) { if (dataJson["stations"][i]["id"].Value == stationId) return dataJson["stations"][i]; }
        return null;
    }

    public bool StationHasFeature(string stationId, string featureId)
    {
        JSONNode stationData = GetStationData(stationId);
        if (stationData == null) return false;
        JSONNode featuresJson = stationData["features"];
        if (featuresJson == null) return false;
        for (int i = 0; i < featuresJson.Count; i++) { if (featuresJson[i]["id"].Value == featureId) return true; }
        return false;
    }

    public bool HasFeature(string featureId)
    {
        if (currentStationData == null) return false;

        return StationHasFeature(currentStationData["id"].Value, featureId);
        //return GetStationFeature(featureId) != null;
    }

    public bool UseGuideInAR()
    {
        if (!ScanController.instance.useGuideInAR) return false;

        if (currentStationData == null) return true;
        JSONNode stationData = GetStationData(currentStationData["id"].Value);
        if (stationData == null) return true;
        JSONNode featuresJson = stationData["features"];
        if (featuresJson == null) return true;
        for (int i = 0; i < featuresJson.Count; i++)
        {
            if (featuresJson[i]["id"].Value == "guide")
            {
                if (featuresJson[i]["useGuideInAR"] != null)
                {
                    return featuresJson[i]["useGuideInAR"].AsBool;
                }
            }
        }
        return true;
    }

    public bool UseGuideInAR(JSONNode stationData)
    {
        if (stationData == null) return true;
        JSONNode featuresJson = stationData["features"];
        if (featuresJson == null) return true;
        for (int i = 0; i < featuresJson.Count; i++) { if (featuresJson[i]["id"].Value == "guide") { if (featuresJson[i]["useGuideInAR"] != null) return featuresJson[i]["useGuideInAR"].AsBool; } }
        return true;
    }

    public JSONNode GetStationFeature(string featureId)
    {
		//if( featureId == "avatarGuide" ){ return JSONNode.Parse( "{\"id\": \"avatarGuide\", \"audio\":\"audios/speech_ritter\"}" ); }

		if ( currentStationData == null ) return null;
        JSONNode featuresJson = currentStationData["features"];
		if ( featuresJson == null) return null;

        for (int i = 0; i < featuresJson.Count; i++) { if (featuresJson[i]["id"].Value == featureId) return featuresJson[i]; }
        return null;
    }

    public JSONNode GetStationFeature(string stationId, string featureId)
    {
        JSONNode stationData = GetStationData(stationId);
        if (stationData == null) return null;
        JSONNode featuresJson = stationData["features"];
        if (featuresJson == null) return null;

        for (int i = 0; i < featuresJson.Count; i++) { if (featuresJson[i]["id"].Value == featureId) return featuresJson[i]; }
        return null;
    }

    public bool StationMarkerHasFeature(string markerId, string featureId)
    {
        JSONNode dataJson = ServerBackendController.instance.GetJson("_stations");
        if (dataJson == null) return false;
        for (int i = 0; i < dataJson["stations"].Count; i++)
        {

            string stationId = dataJson["stations"][i]["id"].Value;
            string stationMarkerId = GetStationMarkerId(stationId);
            if (stationMarkerId == markerId)
            {
                JSONNode featuresJson = dataJson["stations"][i]["features"];
                if (featuresJson == null) return false;
                for (int j = 0; j < featuresJson.Count; j++) { if (featuresJson[j]["id"].Value == featureId) return true; }
                break;
            }
        }

        return false;
    }

    // If we not use markers we just return the stationId
    public string GetStationMarkerId(string stationId)
    {
        JSONNode stationsJson = ServerBackendController.instance.GetJson("_stations");
        if (stationsJson == null) return "";

        for (int i = 0; i < stationsJson["stations"].Count; i++)
        {
            if (stationsJson["stations"][i]["id"].Value == stationId)
            {
                if (stationsJson["stations"][i]["marker"].Count > 0)
                {
                    return stationsJson["stations"][i]["marker"][0].Value;
                }
                else
                {
                    return stationId;
                }
            }
        }
        return "";
    }

    public JSONNode GetStationFeatures(string stationId)
    {
        JSONNode stationData = GetStationData(stationId);
        if (stationData == null) return null;
        JSONNode featuresJson = stationData["features"];
        if (featuresJson == null) return null;
        return featuresJson;
    }

    public void SetCurrentStationDataFromStationId(string stationId) { currentStationData = GetStationData(stationId); }

    public void SetCurrentStationDataFromMarkerId(string markerId) { currentStationData = GetStationDataFromMarkerId(markerId); }

    public JSONNode GetStationDataFromMarkerId(string markerId)
    {
        JSONNode stationsJson = ServerBackendController.instance.GetJson("_stations");
        if (stationsJson == null) return null;

        for (int i = 0; i < stationsJson["stations"].Count; i++)
        {
            for (int j = 0; j < stationsJson["stations"][i]["marker"].Count; j++)
            {
                if (stationsJson["stations"][i]["marker"][j].Value == markerId) { return stationsJson["stations"][i]; }
            }
        }

        for (int i = 0; i < stationsJson["stations"].Count; i++)
        {
            if (stationsJson["stations"][i]["id"].Value == markerId) { return stationsJson["stations"][i]; }
        }

        return null;
    }

	public JSONNode GetStationDataFromMarkerId(string markerId, string stationId, bool useOnlyStationWithMarkerId)
	{
		if( !useOnlyStationWithMarkerId ) { return GetStationDataFromMarkerId( markerId ); }

		JSONNode stationsJson = ServerBackendController.instance.GetJson( "_stations" );
		if ( stationsJson == null ) return null;

		for ( int i = 0; i < stationsJson["stations"].Count; i++ )
		{
			if ( stationsJson["stations"][i]["id"].Value == stationId )
			{
				for ( int j = 0; j < stationsJson["stations"][i]["marker"].Count; j++ )
				{
					if ( stationsJson["stations"][i]["marker"][j].Value == markerId ) { return stationsJson["stations"][i]; }
				}
				break;
			}
		}

		return null;
	}

	public bool HasQRCodeInfo()
    {
        JSONNode stationData = GetStationData(MapController.instance.selectedStationId);
        if (stationData == null) return false;
        return stationData["qrCodeInfo"] != null;
    }
}
