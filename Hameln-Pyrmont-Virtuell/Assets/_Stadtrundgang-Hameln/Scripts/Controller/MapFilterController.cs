using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using MPUIKIT;
using SimpleJSON;
using Mapbox.Utils;

public class MapFilterController : MonoBehaviour
{
    public bool newSearch = false;
    public bool useLanguageAsFilter = true;
    public float searchRadius = 5000;
    public float resultsLimit = 50;
    public int eventsDayRange = 1;
    private bool useLocalFileAsBackup = false;

    [Space(10)]

    public bool didClickedOnFilterStation = false;
    public Image mainMenuButton;
    public GameObject menuButtonsContent;
    public List<MapFilterItem> mapFilterItems = new List<MapFilterItem>();
    public List<MapFilterItem> mapFilterItemsDragMenu = new List<MapFilterItem>();
    public List<GameObject> tourListItemsDragMenu = new List<GameObject>();
    public List<GameObject> stationsHolderListItemsDragMenu = new List<GameObject>();

    [Space(10)]

    public GameObject toursHolder;
    public GameObject filtersRoot;
    public GameObject filtersRootBackground;

    [Space(10)]

    public GameObject currentFilterContent;
    public TextMeshProUGUI currentFilterContentTitle;
    public Image currentTourImage;
    public DragMenuController filterOptionsDragMenu;
    public GameObject headerCurrentSelection;
    public Image headerCurrentSelectionImage;
    public GameObject headerCurrentSelectionFilters;

    [Space(10)]

    public string currentDestinationOneId = "";

    private bool isLoading = false;
    private bool menuIsOpen = false;
    private bool tourListInitialized = false;

    private float menuOpenSpacing = 5;
    private float menuClosedSpacing = -140;
    private float stationItemHeight = 150;

    public static MapFilterController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        filtersRoot.SetActive(Params.showMapFilters);
        if (!Params.showMapFilters) {
            filtersRoot.GetComponentInParent<DragMenuController>(true).menuOpenPrecentage = -0.55f;
            filtersRoot.GetComponentInParent<ScrollRect>(true).GetComponent<RectTransform>().offsetMin = new Vector3(0, 1290f);
        }
        else { 
            filtersRoot.GetComponentInParent<DragMenuController>(true).menuOpenPrecentage = -0.35f;
            filtersRoot.GetComponentInParent<ScrollRect>(true).GetComponent<RectTransform>().offsetMin = new Vector3(0, 816f);
        }

        toursHolder.GetComponent<Image>().color = Params.mapMenuToursBackgroundColor;
        filtersRootBackground.GetComponent<Image>().color = Params.mapMenuFiltersBackgroundColor;

        string filePath = Application.persistentDataPath + "/data_Event.json";
        if (File.Exists(filePath) && !useLocalFileAsBackup) { File.Delete(filePath); }
        filePath = Application.persistentDataPath + "/data_Gastro.json";
        if (File.Exists(filePath) && !useLocalFileAsBackup) { File.Delete(filePath); }
        filePath = Application.persistentDataPath + "/data_Hotel.json";
        if (File.Exists(filePath) && !useLocalFileAsBackup) { File.Delete(filePath); }
    }

    public void LoadTours()
    {
        if (tourListInitialized) return;
        foreach (Transform child in toursHolder.transform) {

            if (child.name == "StationSeparator") continue;
            Destroy(child.gameObject); 
        }

        tourListItemsDragMenu.Clear();
        stationsHolderListItemsDragMenu.Clear();

        JSONNode toursJson = ServerBackendController.instance.GetJson("_tours");
        for (int i = 0; i < toursJson["tours"].Count; i++)
        {
            int index = i;
            JSONNode tourData = toursJson["tours"][index];
            GameObject listElement = ToolsController.instance.InstantiateObject("UI/MapTourButtonPrefab", toursHolder.transform);
            listElement.GetComponent<TourListElement>().tourId = tourData["id"].Value;
            //if(i == (toursJson["tours"].Count - 1)) { listElement.GetComponent<LayoutElement>().minHeight = 180; }

            // Image
            if (tourData["imageURL"] != null && tourData["imageURL"].Value != "")
            {
                ToolsController.instance.ApplyOnlineImage(listElement.GetComponent<TourListElement>().previewImage, tourData["imageURL"].Value, true);
            }
            else if (tourData["image"] != null && tourData["image"].Value != "")
            {
                Sprite sprite = Resources.Load<Sprite>(tourData["image"].Value);
                if (sprite != null)
                {
                    listElement.GetComponent<TourListElement>().previewImage.sprite = sprite;
                    listElement.GetComponent<TourListElement>().previewImage.GetComponent<AspectRatioFitter>().enabled = true;
                    listElement.GetComponent<TourListElement>().previewImage.GetComponent<AspectRatioFitter>().aspectRatio = sprite.bounds.size.x / sprite.bounds.size.y;
                }
            }

            // Texts
            listElement.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslationFromNode(tourData["title"]);

            // Button event
            listElement.GetComponentInChildren<Button>().onClick.AddListener(() => SelectTour(tourData["id"].Value));

            tourListItemsDragMenu.Add(listElement);

            // Load stations
            LoadStations(tourData, listElement, i == (toursJson["tours"].Count-1));
        }

        tourListInitialized = true;
    }

    public void LoadStations(JSONNode tourData, GameObject mapTourButton, bool lastTour)
    {
        GameObject stationsHolder = ToolsController.instance.InstantiateObject("UI/StationsHolder", toursHolder.transform);
        GameObject stationButton = ToolsController.instance.FindGameObjectByName(mapTourButton, "StationsButton");
        stationButton.GetComponentInChildren<Button>().onClick.AddListener(() => OpenCloseStations(stationsHolder, mapTourButton));
        stationsHolderListItemsDragMenu.Add(stationsHolder);

        float margin = 30;
        GameObject stationSeparatorTop = ToolsController.instance.InstantiateObject("UI/StationSeparator", stationsHolder.transform);
        stationSeparatorTop.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        for (int i = 0; i < tourData["stations"].Count; i++)
        {
            int index = i;
            JSONNode stationData = StationController.instance.GetStationData(tourData["stations"][index].Value);
            if (stationData == null) continue;
            GameObject listElement = ToolsController.instance.InstantiateObject("UI/MapStationButtonPrefab", stationsHolder.transform);
            listElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (-i * stationItemHeight) - margin);

            // Image
            if (stationData["imageURL"] != null && stationData["imageURL"].Value != "")
            {
                ToolsController.instance.ApplyOnlineImage(listElement.GetComponent<TourListElement>().previewImage, stationData["imageURL"].Value, true);
                listElement.GetComponent<TourListElement>().previewImage.gameObject.GetComponent<UIImage>().SetSizes(new List<int>() { 256 }, 256);
            }
            else if (stationData["image"] != null && stationData["image"].Value != "")
            {
                Sprite sprite = Resources.Load<Sprite>(stationData["image"].Value);
                listElement.GetComponent<TourListElement>().previewImage.sprite = sprite;

                listElement.GetComponent<TourListElement>().previewImage.GetComponent<AspectRatioFitter>().enabled = true;
                float aspect = sprite.rect.width / sprite.rect.height;
                listElement.GetComponent<TourListElement>().previewImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
            }

            // Texts
            listElement.GetComponent<TourListElement>().stationId = stationData["id"].Value;
            listElement.GetComponent<TourListElement>().titleLabel.text = LanguageController.GetTranslationFromNode(stationData["titleOverview"]);

            if (stationData["mapNumber"] != null) { listElement.GetComponent<TourListElement>().numberLabel.text = stationData["mapNumber"].Value; }
            else
            {
                string number = "";
                if ((index + 1) < 10) { number = "0"; };
                listElement.GetComponent<TourListElement>().numberLabel.text = number + (index + 1);
            }

            // Button event
            listElement.GetComponent<Button>().onClick.AddListener(() => MapController.instance.FocusStation(tourData["id"].Value, stationData));

            string tourId = tourData["id"].Value;
            string stationId = stationData["id"].Value;
            bool finished = (PlayerPrefs.GetInt(tourId + "_" + stationId + "_finished", 0) == 1);

            
            if (finished)
            {
                if (listElement.GetComponent<TourListElement>().background != null) { listElement.GetComponent<TourListElement>().background.color = ToolsController.instance.GetColorFromHexString("#FFFFFF"); }
                if (listElement.GetComponent<TourListElement>().numberLabel != null)
                {
                    listElement.GetComponent<TourListElement>().numberLabel.color = ToolsController.instance.GetColorFromHexString("#21212133");
                }
                if (listElement.GetComponent<TourListElement>().titleLabel != null)
                {
                    listElement.GetComponent<TourListElement>().titleLabel.color = ToolsController.instance.GetColorFromHexString("#FFFFFFFF");
                }
            }
            else
            {
                if (listElement.GetComponent<TourListElement>().background != null)
                {
                    listElement.GetComponent<TourListElement>().background.color = ToolsController.instance.GetColorFromHexString("#6EC561");
                }
                if (listElement.GetComponent<TourListElement>().numberLabel != null)
                {
                    listElement.GetComponent<TourListElement>().numberLabel.color = ToolsController.instance.GetColorFromHexString("#21212133");
                }
                if (listElement.GetComponent<TourListElement>().titleLabel != null)
                {
                    listElement.GetComponent<TourListElement>().titleLabel.color = ToolsController.instance.GetColorFromHexString("#FFFFFFFF");
                }
            }
            
        }

        string prefab = "UI/StationSeparator";
        if (lastTour) { prefab = "UI/StationSeparatorBottom"; }
        GameObject stationSeparatorBottom = ToolsController.instance.InstantiateObject(prefab, stationsHolder.transform);
        stationSeparatorBottom.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (-tourData["stations"].Count * stationItemHeight) - margin);

    }

    public void OpenCloseStations(GameObject stationsHolder, GameObject mapTourButton)
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenCloseStationsCoroutine(stationsHolder, mapTourButton));
    }

    public IEnumerator OpenCloseStationsCoroutine(GameObject stationsHolder, GameObject mapTourButton)
    {
        yield return null;

        float targetHeight = (stationsHolder.transform.childCount-2) * stationItemHeight + 60;
        float openCloseButtonRotation = 180;
        if (stationsHolder.GetComponent<LayoutElement>().minHeight > 0) { targetHeight = 0; openCloseButtonRotation = 0; }

        GameObject openCloseButton = ToolsController.instance.FindGameObjectByName(mapTourButton, "StationsButton");

        StartCoroutine(AnimationController.instance.AnimateRotateXYZCoroutine(openCloseButton.transform, openCloseButton.transform.localEulerAngles, new Vector3(0, 0, openCloseButtonRotation), 0.5f, "smooth"));

        yield return StartCoroutine(AnimationController.instance.AnimateLayoutElementMinHeightCoroutine(stationsHolder.GetComponent<LayoutElement>(), stationsHolder.GetComponent<LayoutElement>().minHeight, targetHeight, 0.5f, "smooth"));

        isLoading = false;
    }

    public void CloseStations()
    {
        for (int i = 0; i < stationsHolderListItemsDragMenu.Count; i++)
        {
            stationsHolderListItemsDragMenu[i].GetComponent<LayoutElement>().minHeight = 0;
        }

        for (int i = 0; i < tourListItemsDragMenu.Count; i++)
        {
            GameObject openCloseButton = ToolsController.instance.FindGameObjectByName(tourListItemsDragMenu[i], "StationsButton");
            openCloseButton.transform.localEulerAngles = new Vector3(0, 0, 180);
        }
    }

    public void SelectTour(string tourId)
    {
		JSONNode tourNode = TourController.instance.GetTourData(tourId);
        if (tourNode == null) return;

		/*
        filterOptionsDragMenu.CloseImmediate();
        filterOptionsDragMenu.dragContent.gameObject.SetActive(false);
        currentFilterContent.SetActive(true);

        DisableFilterSelectionImages();
        GameObject currentFilterImage = ToolsController.instance.FindGameObjectByName(currentFilterContent, "ImageContent-Tour");
        currentFilterImage.SetActive(true);

        currentFilterContentTitle.text = LanguageController.GetTranslationFromNode(tourNode["title"])
                + "\n" + tourNode["stations"].Count + " " + LanguageController.GetTranslation("Stationen");

        currentTourImage.sprite = GetTourSprite(tourId);
        currentTourImage.GetComponent<AspectRatioFitter>().aspectRatio = currentTourImage.sprite.bounds.size.x / currentTourImage.sprite.bounds.size.y;
        */

		TourController.instance.currentTourId = tourId;
        MapController.instance.LoadData();
        MapController.instance.EnableDisablePath(false);
        DisableFilters();

        // Header
        headerCurrentSelection.SetActive(true);
        headerCurrentSelectionFilters.SetActive(false);
        headerCurrentSelectionImage.sprite = GetTourSprite(tourId);
        //headerCurrentSelectionImage.GetComponent<AspectRatioFitter>().aspectRatio = currentTourImage.sprite.bounds.size.x / currentTourImage.sprite.bounds.size.y;
        headerCurrentSelection.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslationFromNode(tourNode["title"]);
        SetDragMenuFilterSelection("");
        SetDragMenuTourSelection(tourId);
		if ( Params.showMapFilterOptions ) { filterOptionsDragMenu.Close(); }


        if (PlayerPrefs.GetInt("filterOptionsDragMenu_Showed") != 1)
        {
			if ( Params.showMapFilterOptions ) { filterOptionsDragMenu.OpenImmediate(); }
            StopCoroutine("CloseFilterOptionsDragMenuCoroutine");
            StartCoroutine("CloseFilterOptionsDragMenuCoroutine");
            PlayerPrefs.SetInt("filterOptionsDragMenu_Showed", 1);
        }
        else
        {
			if ( Params.showMapFilterOptions ) { filterOptionsDragMenu.Close(); }
        }
    }
    
	public void UpdateCurrentTourHeaderImage(string tourId)
	{
		headerCurrentSelectionImage.sprite = GetTourSprite(tourId);
	}

    public IEnumerator CloseFilterOptionsDragMenuCoroutine()
    {
        print("CloseFilterOptionsDragMenuCoroutine");
        while (!MapController.instance.mapUIRoot.activeInHierarchy) { yield return null; }
        yield return new WaitForSeconds(1.0f);
		if ( Params.showMapFilterOptions ) { filterOptionsDragMenu.Close(); }
    }

    public Sprite GetTourSprite(string tourId)
    {
        foreach (Transform child in toursHolder.transform)
        {
            if (child.GetComponent<TourListElement>() == null) { continue; }
			if (child.GetComponent<TourListElement>().tourId == tourId) { return child.GetComponent<TourListElement>().previewImage.sprite; }
        }
        return null;
    }

    public void SelectFilter(MapFilterItem mapFilterItem)
    {
        ActivateDeactiveFilter(mapFilterItem);
        MapController.instance.EnableDisablePath(false);
    }

    public void DisableCurrentFilter()
    {
		if ( Params.showMapFilterOptions ) { filterOptionsDragMenu.CloseImmediate(); }
        filterOptionsDragMenu.dragContent.gameObject.SetActive( Params.showMapFilterOptions );
        currentFilterContent.SetActive(false);

        MapController.instance.EnableDisableFilteredStations(false);
        MapController.instance.EnableDisableTourStations(false);
	}

    public void CloseMenuImmediate()
    {
        //mainMenuButton.GetChild(0).GetChild(0).GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#FFFFFFCD");
        //mainMenuButton.GetChild(0).GetChild(1).GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#212121FF");

        MPImage image = mainMenuButton.GetComponent<MPImage>();
        Rectangle rectangle = mainMenuButton.GetComponent<MPImage>().Rectangle;
        rectangle.CornerRadius = new Vector4(25, 25, 25, 25);
        image.Rectangle = rectangle;

        menuButtonsContent.GetComponent<GridLayoutGroup>().spacing = new Vector2(0, menuClosedSpacing);
        menuIsOpen = false;

        EnableDisableButtons(false);
    }

    public void OpenMenu()
    {
        if (menuIsOpen) return;
        OpenCloseMenu();
    }

    public void CloseMenu()
    {
        if (!menuIsOpen) return;
        OpenCloseMenu();
    }

    public void OpenCloseMenu()
    {
        if (isLoading) return;
        isLoading = true;

        StartCoroutine(OpenCloseMenuCoroutine());
    }

    public IEnumerator OpenCloseMenuCoroutine()
    {
        AnimationCurve animationCurve = AnimationController.instance.GetAnimationCurveWithID("fastSlow");
        if (animationCurve == null) yield break;

        if (!menuIsOpen)
        {
            MPImage image = mainMenuButton.GetComponent<MPImage>();
            Rectangle rectangle = mainMenuButton.GetComponent<MPImage>().Rectangle;
            rectangle.CornerRadius = new Vector4(0, 0, 25, 25);
            image.Rectangle = rectangle;
        }

        if (menuIsOpen)
        {
            EnableDisableButtons(false);
        }

        //mainMenuButton.GetChild(0).GetChild(0).GetComponent<Image>().color = menuIsOpen ? ToolsController.instance.GetColorFromHexString("#FFFFFFCD") : ToolsController.instance.GetColorFromHexString("#FF2D44CD");
        //mainMenuButton.GetChild(0).GetChild(1).GetComponent<Image>().color = menuIsOpen ? ToolsController.instance.GetColorFromHexString("#212121FF") : ToolsController.instance.GetColorFromHexString("#FFFFFFFF");

        float ySpacing = menuButtonsContent.GetComponent<GridLayoutGroup>().spacing.y;
        float targetSpacing = menuIsOpen ? menuClosedSpacing : menuOpenSpacing;
        float animationDuration = 0.4f;
        float currentTime = 0;
        while (currentTime < animationDuration)
        {
            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            float spacing = Mathf.LerpUnclamped(ySpacing, targetSpacing, lerpValue);
            menuButtonsContent.GetComponent<GridLayoutGroup>().spacing = new Vector2(0, spacing);

            currentTime += Time.deltaTime;
            yield return null;
        }
        menuButtonsContent.GetComponent<GridLayoutGroup>().spacing = new Vector2(0, targetSpacing);

        menuIsOpen = !menuIsOpen;

        if (!menuIsOpen)
        {
            MPImage image = mainMenuButton.GetComponent<MPImage>();
            Rectangle rectangle = mainMenuButton.GetComponent<MPImage>().Rectangle;
            rectangle.CornerRadius = new Vector4(25, 25, 25, 25);
            image.Rectangle = rectangle;
        }

        if (menuIsOpen)
        {
            EnableDisableButtons(true);
        }

        isLoading = false;
    }

    public void EnableDisableButtons(bool activate)
    {
        foreach (Transform child in menuButtonsContent.transform)
        {
            if (child.GetSiblingIndex() == 0) continue;
            child.GetComponent<Button>().enabled = activate;
        }
    }

    public void MarkActiveFilters()
    {
        for (int i = 0; i < mapFilterItems.Count; i++)
        {
            if (mapFilterItems[i].isActive)
            {
                mapFilterItems[i].buttonBackground.color = ToolsController.instance.GetColorFromHexString("#FF2D44CD");
                mapFilterItems[i].buttonIcon.color = ToolsController.instance.GetColorFromHexString("#FFFFFFFF");
            }
            else
            {
                mapFilterItems[i].buttonBackground.color = ToolsController.instance.GetColorFromHexString("#FFFFFFCD");
                mapFilterItems[i].buttonIcon.color = ToolsController.instance.GetColorFromHexString("#212121FF");
            }
        }

    }

    public void ActivateDeactiveFilter(MapFilterItem mapFilterItem)
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(ActivateDeactiveFilterCoroutine(mapFilterItem));
    }

    public IEnumerator ActivateDeactiveFilterCoroutine(MapFilterItem mapFilterItem)
    {
        /*
        if (mapFilterItem != null) { mapFilterItem.isActive = !mapFilterItem.isActive; }
        MarkActiveFilters();

        if (mapFilterItem != null && !mapFilterItem.isActive) {

            OnFilterResultsLoaded(mapFilterItem.filterType, mapFilterItem.filterTitle);

            MapController.instance.EnableDisableFilteredStations(mapFilterItem.filterType, false);
            isLoading = false;
            yield break;
        }
        */

        MapController.instance.EnableDisableFilteredStations(false);

        for (int i = 0; i < mapFilterItems.Count; i++)
        {
            //if (!mapFilterItems[i].isActive) { MapController.instance.EnableDisableFilteredStations(mapFilterItems[i].filterType, false); continue; }
            if (mapFilterItems[i].filterType != mapFilterItem.filterType) continue;

            if (mapFilterItems[i].isLoaded && !newSearch)
            {
                MapController.instance.EnableDisableFilteredStations(mapFilterItems[i].filterType, true);
            }
            else
            {
                InfoController.instance.loadingCircle.SetActive(true);
                //yield return new WaitForSeconds(0.25f);

                bool isSuccess = false;
                string responseData = "";
                yield return StartCoroutine(
                    GetDataCoroutine(mapFilterItems[i].filterType, (bool success, string data) =>
                    {
                        isSuccess = success;
                        responseData = data;
                    })
                );

                if (isSuccess)
                {
                    mapFilterItems[i].isLoaded = true;
                    MapController.instance.LoadFilterStations(mapFilterItems[i].filterType, responseData);
                }
            }
        }
        InfoController.instance.loadingCircle.SetActive(false);

        OnFilterResultsLoaded(mapFilterItem.filterType, mapFilterItem.filterTitle);

        // Header
        headerCurrentSelection.SetActive(true);
        headerCurrentSelectionFilters.SetActive(true);
        headerCurrentSelection.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(mapFilterItem.filterTitle);
        foreach (Transform child in headerCurrentSelectionFilters.transform) { child.gameObject.SetActive(false); if (child.name == mapFilterItem.filterType) { child.gameObject.SetActive(true); } }
        SetDragMenuFilterSelection(mapFilterItem.filterType);
        SetDragMenuTourSelection("");

        isLoading = false;
    }

    public void OnFilterResultsLoaded(string filterType, string filterTitle)
    {
		MapController.instance.EnableDisableTourStations(false);

		/*
        currentFilterContentTitle.text = LanguageController.GetTranslation(filterTitle)
                + "\n" + MapController.instance.GetFilteredStationsCount(filterType) + " " + LanguageController.GetTranslation("Ergebnisse");

        currentFilterContent.SetActive(true);
        filterOptionsDragMenu.CloseImmediate();
        filterOptionsDragMenu.dragContent.gameObject.SetActive(false);

        DisableFilterSelectionImages();
        GameObject currentFilterImage = ToolsController.instance.FindGameObjectByName(currentFilterContent, "ImageContent-" + filterType);
        currentFilterImage.SetActive(true);
        */

		if ( Params.showMapFilterOptions ) { filterOptionsDragMenu.Close(); }
    }

    public void DisableFilterSelectionImages()
    {
        GameObject tour = ToolsController.instance.FindGameObjectByName(currentFilterContent, "ImageContent-Tour");
        GameObject gastro = ToolsController.instance.FindGameObjectByName(currentFilterContent, "ImageContent-Gastro");
        GameObject events = ToolsController.instance.FindGameObjectByName(currentFilterContent, "ImageContent-Event");
        GameObject hotel = ToolsController.instance.FindGameObjectByName(currentFilterContent, "ImageContent-Hotel");
        tour.SetActive(false);
        gastro.SetActive(false);
        events.SetActive(false);
        hotel.SetActive(false);
    }

    public IEnumerator GetDataCoroutine(string filterType, Action<bool, string> Callback)
    {
        print("GetDataCoroutine " + filterType);

        bool isSuccess = false;

        string lang = LanguageController.GetAppLanguageCode();
        //lang = "de";

        yield return StartCoroutine(
            GetDataByTypeCoroutine(filterType, lang, (bool success, string data) => {

                isSuccess = success;
            })
        );

        if (!isSuccess)
        {
            // Fallback get de data if we are not using de
            if (lang != "de")
            {
                yield return StartCoroutine(
                    GetDataByTypeCoroutine(filterType, "de", (bool success, string data) => {

                        isSuccess = success;
                    })
                );
            }
        }

        string filePath = Application.persistentDataPath + "/data_" + filterType + ".json";
        if (File.Exists(filePath))
        {
            Callback(true, File.ReadAllText(filePath));
        }
        else
        {
            Callback(false, "");
        }
    }

    public IEnumerator GetDataByTypeCoroutine(string filterType, string lang, Action<bool, string> Callback)
    {
        string experience = Params.destinationOneExperience;
        if (filterType == "Event") { experience = Params.destinationOneEventExperience; }

        // http://developer.et4.de/explorer/?samples=true
        // Request type within distance to loaction
        // https://meta.et4.de/rest.ashx/search/?experience=osnabruecker-land&type=Event&latitude=52.26704331903741&longitude=8.050923000256894&distance=3000&template=ET2014A.json

        //if (filterType == "Event") lang = "de";

        //Vector2d myPosition = MapController.instance.GetCurrentLocation();
        Vector2d myPosition = Params.mapSearchCenter;

        // Events date range
        // Format: &startdate=01.01.2015&enddate=01.02.2015
        //DateTime now = DateTime.Now;
        //string nowString = now.ToString("dd.MM.yyyy");
        //DateTime end = now.AddDays(eventsDayRange);
        //string endString = now.ToString("dd.MM.yyyy");

        DateTime now = DateTime.Now;
        string nowString = now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH:mm:ss");
        nowString = Uri.EscapeDataString(nowString);

        DateTime endTmp = now.AddDays(eventsDayRange);
        DateTime end = new DateTime(endTmp.Year, endTmp.Month, endTmp.Day, 23, 59, 0);

        string endString = end.ToString("yyyy-MM-dd") + "T" + end.ToString("HH:mm:ss");
        endString = Uri.EscapeDataString(endString);


        string dateInterval = "";
        if (filterType == "Event" && eventsDayRange > 0) { dateInterval = "&startdate=" + nowString + "&enddate=" + endString; }
        else if (filterType == "Event") { dateInterval = "&startdate=" + now.ToString("dd.MM.yyyy") + "&enddate=" + now.ToString("dd.MM.yyyy"); }

        // Request URL
        string url = "https://meta.et4.de/rest.ashx/search/?experience=" + experience + "&type=" + filterType +
            "&latitude=" + myPosition.x.ToString().Replace(",", ".") + "&longitude=" + myPosition.y.ToString().Replace(",", ".") +
            "&distance=" + searchRadius +
            "&template=ET2014A.json" +
            dateInterval +
            "&limit=" + resultsLimit;

        if (filterType == "Event") { url += "&mkt=de"; }
        else if (useLanguageAsFilter) { url += "&mkt=" + lang; }
        else { url += "&mkt=de"; }

        print("GetDataByTypeCoroutine " + url);

        bool isSuccess = false;
        string responseData = "";
        yield return StartCoroutine(
            DestinationOneController.instance.GetCoroutine(url, (bool success, string data) => {

                isSuccess = success;
                responseData = data;
            })
        );

        if (!isSuccess)
        {
            print("Failed get data, error: " + responseData);
        }
        else
        {
            print("Success get data " + responseData);

            if (string.IsNullOrEmpty(responseData)) { Callback(false, ""); yield break; }

            JSONNode dataJson = JSONNode.Parse(responseData);
            if (dataJson == null) { Callback(false, ""); yield break; }

            if (dataJson["items"] == null) { Callback(false, ""); yield break; }
            if (dataJson["items"].Count <= 0) { Callback(false, ""); yield break; }

            string filePath = Application.persistentDataPath + "/data_" + filterType + ".json";
            File.WriteAllText(filePath, responseData);
            Callback(true, responseData);
        }
    }

    public void LoadInfo(string destinationOneId, JSONNode stationData)
    {
        if (isLoading) return;
        isLoading = false;
        StartCoroutine(LoadInfoCoroutine(destinationOneId, stationData));
    }

    public IEnumerator LoadInfoCoroutine(string destinationOneId, JSONNode stationData)
    {
        currentDestinationOneId = destinationOneId;

        /*
        InfoController.instance.loadingCircle.SetActive(true);
        yield return new WaitForSeconds(0.25f);

        bool isSuccess = false;
        string responseData = "";
        yield return StartCoroutine(
            DestinationOneController.instance.GetDataCoroutine(destinationOneId, (bool success, string data) => {
                isSuccess = success;
                responseData = data;
            })
        );

        InfoController.instance.loadingCircle.SetActive(false);

        if (isSuccess)
        {
            JSONNode infoData = JSONNode.Parse(responseData);
            if (infoData == null) { OnFailedRetrieveData(); yield break; }
            if (infoData["items"] == null || infoData["items"].Count <= 0) { OnFailedRetrieveData(); yield break; }
            infoData = infoData["items"][0];

            bool showPopup = ShouldShowPopup(infoData);
            if (!showPopup)
            {             
                PoiInfoController.instance.LoadInfo(infoData);
                SpeechController.instance.Init();
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("InfoSite"));
            }
            else
            {
                StationController.instance.LoadStationInfoOnMap(infoData);
            }

            didClickedOnFilterStation = true;
        }
        */

        bool showPopup = ShouldShowPopup(stationData);
        if (!showPopup)
        {
            PoiInfoController.instance.LoadInfo(stationData);
            SpeechController.instance.Init();
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("InfoSite"));
        }
        else
        {
            StationController.instance.LoadStationInfoOnMap(stationData);
        }

        didClickedOnFilterStation = true;

        isLoading = false;
    }

    public bool ShouldShowPopup(JSONNode infoData)
    {
        //if (infoData["web"] != null) return true;

        if (infoData["type"] != null)
        {
            if (infoData["type"].Value == "Event") { return true; }
            if (infoData["type"].Value == "Gastro") { return true; }
            if (infoData["type"].Value == "Hotel") { return false; }
        }
        return true;
    }

    public void OnFailedRetrieveData()
    {
        InfoController.instance.ShowMessage("Informationen konnten nicht abgerufen werden. Versuche es später nochmal.");
    }

    public void DisableFilters()
    {
        CloseMenuImmediate();
        for (int i = 0; i < mapFilterItems.Count; i++)
        {
            mapFilterItems[i].isActive = false;
            MapController.instance.EnableDisableFilteredStations(mapFilterItems[i].filterType, false);
        }
        MarkActiveFilters();
    }

    public void EnableFilters()
    {
        CloseMenuImmediate();
        for (int i = 0; i < mapFilterItems.Count; i++)
        {
            mapFilterItems[i].isActive = true;
        }
        MarkActiveFilters();
    }

    public void ResetLoadedFilters()
    {
        for (int i = 0; i < mapFilterItems.Count; i++)
        {
            mapFilterItems[i].isLoaded = false;
        }
    }

    public void SetDragMenuFilterSelection(string filterType)
    {
        for (int i = 0; i < mapFilterItemsDragMenu.Count; i++)
        {
            mapFilterItemsDragMenu[i].selection.SetActive(false);
            if (mapFilterItemsDragMenu[i].filterType == filterType) { mapFilterItemsDragMenu[i].selection.SetActive(true); }
        }
    }

    public void SetDragMenuTourSelection(string tourId)
    {
        for (int i = 0; i < tourListItemsDragMenu.Count; i++)
        {
            tourListItemsDragMenu[i].GetComponent<TourListElement>().selection.SetActive(false);
            if (tourListItemsDragMenu[i].GetComponent<TourListElement>().tourId == tourId) { tourListItemsDragMenu[i].GetComponent<TourListElement>().selection.SetActive(true); }
        }
    }
}
