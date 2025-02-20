using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using ARLocation;

public class MapController : MonoBehaviour
{
    public string selectedTourId = "";
    public string selectedStationId = "";

    [Space(10)]

    public bool shouldShowMapFilter = false;
    public string editorNearestStationTestId = "";

    private bool gpsPermissionAsked = false;
    private bool showRealPositionOnMap = false;
    private bool dragEnabled = true;
    private float snapToTowerDistance = 8.0f;
    private float checkNearStationTimer = 0;

    [Space(10)]

    private bool keepStationsSize = true;
    public AbstractMap abstractMap;
    public LocationProviderFactory locationProviderFactory;
    private Mapbox.Unity.Location.AbstractLocationProvider _locationProviderMapbox = null;

    [Space(10)]

    public GameObject mapUIRoot;
    public GameObject mapGPSInfoRoot;
    public GameObject mapsRoot;
    public Camera mapCamera;
    public GameObject mapHolder;
    public RawImage mapRawImage;
    public Transform compassNeedle;

    [Space(10)]

    public GameObject sameLocationStationsContent;
    public GameObject sameLocationList;
    public GameObject sameLocationTitle;

    [Space(10)]

    public GameObject exampleMap;
    public Vector2d exampleMapPosition = new Vector2d( 52.16768d, 9.43933d );
    public float exampleMapZoomFactor = 0.062f;

    private float snapToPathMinDistance = 8.0f;
    private float distanceToNearestPathPosition = 0.0f;
    private float mapStationZoomFactor = 8f;
    private float yOffset = 0f;

    [Space(10)]

    private List<GameObject> stations = new List<GameObject>();
    private List<GameObject> filterStations = new List<GameObject>();

    private GameObject personOnMap;
    private GameObject personOnMapDirection;
    private GameObject realPersonOnMap;
    private Vector3 positionOnMap = Vector3.zero;
    private Vector3 realPositionOnMap = Vector3.zero;
    private int mapLayer;
    private bool isEnabled = false;
    private bool initialized = false;
    private bool isLoading = false;
    private bool isValidatingLocationServices = false;
    private bool isAskingForLocationPermission = false;
    private bool isCentering = false;

    private float poiScale = 14f;   //12
    private float lerpSpeed = 5f;

    private List<Vector2d> geoPositions = new List<Vector2d>();
    private JSONNode dataPathJson;
    private JSONNode dataStationsJson;      // Drupal station infos
    private JSONNode dataStationsMapJson;   // Custom infos for map only
    private GameObject stationButton;

    private float compassSmoothTime = 0.1f;
    private float compassVelocity = 0.0f;
    private float currentCompassAngle = 0.0f;

    [Space(10)]

    public List<DragMenuController> dragMenus = new List<DragMenuController>();
    public List<string> nearByStationIds = new List<string>();

    [Space(10)]

    public List<GameObject> tourMapPaths = new List<GameObject>();
    public GameObject pathButton;
    public Image pathButtonImage;

    public static MapController instance;
    void Awake()
    {

        instance = this;
    }

    void Start()
    {

        /*
		if (null == _locationProviderMapbox)
		{
			_locationProviderMapbox = 
				Mapbox.Unity.Location.LocationProviderFactory.Instance.DefaultLocationProvider 
				as Mapbox.Unity.Location.AbstractLocationProvider;
		}
		*/

        mapLayer = LayerMask.NameToLayer("Map");
    }

    void LateUpdate()
    {
        if (!isEnabled) return;
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "MapSite") return;

#if !UNITY_EDITOR
        if (!Input.compass.enabled){ Input.compass.enabled = true; }
#endif

        UpdateLayer();
        UpdateMap();
        CheckNearStation();
        //RotateMapWithTwoFingers();
        //Interact();

        if (!MapFilterController.instance.headerCurrentSelection.activeInHierarchy || MapFilterController.instance.headerCurrentSelectionFilters.activeInHierarchy)
        {
            pathButton.SetActive(false);
            EnableDisablePath(false);
        }
        else
        {
            if (Params.showPathButton) { pathButton.SetActive(true); }
        }
    }

    public void EnableDisablePath(bool shouldEnable)
    {
        bool pathIsEnabled = false;
        for (int i = 0; i < tourMapPaths.Count; i++) { if (tourMapPaths[i].activeInHierarchy) { pathIsEnabled = true; break; } }
        if (!shouldEnable && !pathIsEnabled) return;

        for (int i = 0; i < tourMapPaths.Count; i++) { tourMapPaths[i].SetActive(false); }
        pathButtonImage.color = Params.pathButtonImageColorInActive;

        int currentTourIndex = TourController.instance.GetTourIndex();
        for (int i = 0; i < tourMapPaths.Count; i++) { if (i==currentTourIndex) { tourMapPaths[i].SetActive(shouldEnable);  break; } }
    }

    public void EnableDisablePath()
    {
        bool isActive = (ColorUtility.ToHtmlStringRGB(pathButtonImage.color) == ColorUtility.ToHtmlStringRGB(Params.pathButtonImageColorActive));
        for (int i = 0; i < tourMapPaths.Count; i++) { tourMapPaths[i].SetActive(false); }
        pathButtonImage.color = Params.pathButtonImageColorInActive;

        int currentTourIndex = TourController.instance.GetTourIndex();
        for (int i = 0; i < tourMapPaths.Count; i++) { if (i == currentTourIndex) { tourMapPaths[i].SetActive(!isActive); break; } }
        pathButtonImage.color = isActive ? Params.pathButtonImageColorInActive : Params.pathButtonImageColorActive;
    }

    public void InitGPS()
    {

        locationProviderFactory.gameObject.SetActive(true);
        if (null == _locationProviderMapbox)
        {
            _locationProviderMapbox =
                Mapbox.Unity.Location.LocationProviderFactory.Instance.DefaultLocationProvider
                as Mapbox.Unity.Location.AbstractLocationProvider;
        }
    }

    public void Init(bool enableLocationProviderFactory = true)
    {

        if (enableLocationProviderFactory)
        {

            locationProviderFactory.gameObject.SetActive(true);
            if (null == _locationProviderMapbox)
            {
                _locationProviderMapbox =
                    Mapbox.Unity.Location.LocationProviderFactory.Instance.DefaultLocationProvider
                    as Mapbox.Unity.Location.AbstractLocationProvider;
            }
        }

        if (!initialized)
        {

            initialized = true;
            InitMap();
            StartCoroutine(InitMapCoroutine());
        }

        LoadData();
        MapFilterController.instance.DisableFilters();
        //MapFilterController.instance.ResetLoadedFilters();

        isEnabled = true;
        mapsRoot.SetActive(true);
    }

    public void LoadData()
    {
        JSONNode tourData = TourController.instance.GetTourData();
        if (tourData == null) return;
        if (selectedTourId == tourData["id"].Value) { EnableDisableTourStations(true); return; }

		string tourId = tourData["id"].Value;
        selectedTourId = tourId;
        for (int i = 0; i < stations.Count; i++) { Destroy(stations[i]); }
        stations.Clear();

        for (int i = 0; i < tourData["stations"].Count; i++)
        {
            int index = i;
            JSONNode stationData = StationController.instance.GetStationData(tourData["stations"][index].Value);
            if (stationData == null) continue;
            if (stationData["latitude"] == null && stationData["longitude"] == null) continue;

            GameObject station = ToolsController.instance.InstantiateObject("Map/MapStationPrefab", mapsRoot.transform);
            station.transform.localScale = Vector3.one * poiScale;
            station.GetComponentInChildren<Canvas>(true).worldCamera = mapCamera;
            station.GetComponentInChildren<Canvas>(true).sortingOrder = index;

            double latitude = ToolsController.instance.GetDoubleValueFromJsonNode(stationData["latitude"]);
            double longitude = ToolsController.instance.GetDoubleValueFromJsonNode(stationData["longitude"]);

            station.GetComponent<MapStation>().dataJson = stationData;
            station.GetComponent<MapStation>().geoPosition = new Vector2d(latitude, longitude);

            if (stationData["mapNumber"] != null)
            {
                station.GetComponent<MapStation>().markerLabel.text = LanguageController.GetTranslation(stationData["mapNumber"].Value);
            }
            else
            {
                if ((index + 1) >= 10) { station.GetComponent<MapStation>().markerLabel.text = (index + 1).ToString(); }
                else { station.GetComponent<MapStation>().markerLabel.text = "0" + (index + 1); }
            }

            station.GetComponentInChildren<Button>(true).onClick.AddListener(() => OnClickOnStation(station.GetComponent<MapStation>(), stationData));
            station.GetComponent<MapStation>().stationId = stationData["id"].Value;
            station.GetComponent<MapStation>().numberText = station.GetComponent<MapStation>().markerLabel.text;
            stations.Add(station);
        }

        for (int i = 0; i < stations.Count; i++)
        {
            string stationId = stations[i].GetComponent<MapStation>().dataJson["id"].Value;
            bool finished = (PlayerPrefs.GetInt(tourId + "_" + stationId + "_finished", 0) == 1);

            if (finished)
            {
                stations[i].GetComponent<MapStation>().finishedUI.SetActive(true);
                stations[i].GetComponent<MapStation>().markerLabel.color = Params.mapStationFinishedLabelColor;
            }
            else
            {
                stations[i].GetComponent<MapStation>().finishedUI.SetActive(false);
                stations[i].GetComponent<MapStation>().markerLabel.color = Params.mapStationNotFinishedLabelColor;
            }
        }
    }

    public void EnableDisableTourStations(bool enable)
    {
        for (int i = 0; i < stations.Count; i++)
        {
            stations[i].SetActive(enable);
        }
    }

    public void EnableDisableFilteredStations(bool enable)
    {
        for (int i = 0; i < filterStations.Count; i++)
        {
            filterStations[i].SetActive(enable);
        }
    }

    public void EnableDisableFilteredStations(string filterType, bool enable)
    {
        for (int i = 0; i < filterStations.Count; i++)
        {
            if (filterStations[i].GetComponent<MapStation>().filterType == filterType) { filterStations[i].SetActive(enable); }
        }
    }

    public int GetFilteredStationsCount(string filterType)
    {
        int count = 0;
        for (int i = 0; i < filterStations.Count; i++)
        {
            if (filterStations[i].GetComponent<MapStation>().filterType == filterType) { count++; }
        }
        return count;
    }

    public void LoadFilterStations(string filterType, string data)
    {
        JSONNode dataJson = JSONNode.Parse(data);
        if (dataJson == null) return;
        if (dataJson["items"] == null) return;
        if (dataJson["items"].Count <= 0) return;

        if (MapFilterController.instance.newSearch)
        {
            List<GameObject> filterStationsTmp = new List<GameObject>();
            for (int i = 0; i < filterStations.Count; i++) { if (filterStations[i].GetComponent<MapStation>().filterType == filterType) { filterStationsTmp.Add(filterStations[i]); } }
            for (int i = 0; i < filterStationsTmp.Count; i++) { filterStations.Remove(filterStationsTmp[i]); Destroy(filterStationsTmp[i]); }
        }

        for (int i = 0; i < dataJson["items"].Count; i++)
        {
            JSONNode stationData = dataJson["items"][i];
            GameObject mapStation = GetMapStation(stationData["id"].Value);
            if (mapStation != null) { mapStation.SetActive(true); continue; }

            int index = i;
            if (stationData["geo"] == null) continue;
            if (stationData["geo"]["main"] == null) continue;
            if (stationData["geo"]["main"]["latitude"] == null || stationData["geo"]["main"]["longitude"] == null) continue;

            //GameObject station = ToolsController.instance.InstantiateObject("Map/MapStationFilterPrefab", mapsRoot.transform);
            GameObject station = ToolsController.instance.InstantiateObject("Map/MapStationFilterPrefab_" + filterType, mapsRoot.transform);
            station.transform.localScale = Vector3.one * poiScale;
            station.GetComponentInChildren<Canvas>(true).worldCamera = mapCamera;
            station.GetComponentInChildren<Canvas>(true).sortingOrder = index;

            double latitude = ToolsController.instance.GetDoubleValueFromJsonNode(stationData["geo"]["main"]["latitude"]);
            double longitude = ToolsController.instance.GetDoubleValueFromJsonNode(stationData["geo"]["main"]["longitude"]);

            station.GetComponent<MapStation>().dataJson = stationData;
            station.GetComponent<MapStation>().geoPosition = new Vector2d(latitude, longitude);
            station.GetComponentInChildren<Button>(true).onClick.AddListener(() => OnClickOnFilteredStation(station.GetComponent<MapStation>(), stationData));
            station.GetComponent<MapStation>().stationId = stationData["id"].Value;
            station.GetComponent<MapStation>().filterType = filterType;
            filterStations.Add(station);
        }

        MarkStationsOnSameLocation();
    }

    public void MarkStationsOnSameLocation()
    {
        for (int i = 0; i < filterStations.Count; i++)
        {
            GameObject currentStation = filterStations[i];
            if (currentStation.GetComponent<MapStation>().filterType != "Event") continue;

            double latitude = ToolsController.instance.GetDoubleValueFromJsonNode(currentStation.GetComponent<MapStation>().dataJson["geo"]["main"]["latitude"]);
            double longitude = ToolsController.instance.GetDoubleValueFromJsonNode(currentStation.GetComponent<MapStation>().dataJson["geo"]["main"]["longitude"]);

            int sameLocationCount = 1;
            for (int j = 0; j < filterStations.Count; j++)
            {
                if (currentStation == filterStations[j]) { continue; }

                double latitudeOther = ToolsController.instance.GetDoubleValueFromJsonNode(filterStations[j].GetComponent<MapStation>().dataJson["geo"]["main"]["latitude"]);
                double longitudeOther = ToolsController.instance.GetDoubleValueFromJsonNode(filterStations[j].GetComponent<MapStation>().dataJson["geo"]["main"]["longitude"]);

                float dist = ToolsController.instance.CalculateDistance(
                (float)latitude, (float)latitudeOther, (float)longitude, (float)longitudeOther);

                if (dist < 2)
                {
                    if (currentStation.GetComponent<MapStation>().dataJson["title"] != null && filterStations[j].GetComponent<MapStation>().dataJson["title"] != null)
                    {
                        string title = currentStation.GetComponent<MapStation>().dataJson["title"].Value;
                        if (title == filterStations[j].GetComponent<MapStation>().dataJson["title"].Value) continue;
                    }

                    sameLocationCount++;
                    if (currentStation.GetComponent<MapStation>().multiLocationId < 0) { currentStation.GetComponent<MapStation>().multiLocationId = i; }
                    if (filterStations[j].GetComponent<MapStation>().multiLocationId < 0) { filterStations[j].GetComponent<MapStation>().multiLocationId = i; }
                }
            }

            if (sameLocationCount > 1)
            {
                currentStation.GetComponent<MapStation>().multiLocationIndicator.SetActive(true);
                currentStation.GetComponent<MapStation>().multiLocationIndicator.GetComponentInChildren<TextMeshProUGUI>().text = sameLocationCount.ToString();
            }
            else { currentStation.GetComponent<MapStation>().multiLocationIndicator.SetActive(false); }
        }
    }

    public List<GameObject> GetSameLocations(GameObject station)
    {
        int multiLocationId = station.GetComponent<MapStation>().multiLocationId;
        List<GameObject> sameStations = new List<GameObject>();
        sameStations.Add(station);

        for (int i = 0; i < filterStations.Count; i++)
        {
            if (station == filterStations[i]) { continue; }
            if (multiLocationId >= 0 && multiLocationId == filterStations[i].GetComponent<MapStation>().multiLocationId)
            {
                sameStations.Add(filterStations[i]);
            }
        }
        return sameStations;
    }

    public GameObject GetMapStation(string id)
    {
        for (int i = 0; i < filterStations.Count; i++) { if (filterStations[i].GetComponent<MapStation>().stationId == id) return filterStations[i]; }
        return null;
    }

    public GameObject GetStationButton(string id)
    {
        for (int i = 0; i < stations.Count; i++) { if (stations[i].GetComponent<MapStation>().stationId == id) return stations[i]; }
        return null;
    }

    public void Interact()
    {

        if (StationController.instance.stationMapDialog.activeInHierarchy) return;

        if (Input.GetMouseButtonDown(0))
        {

            LayerMask mapLayer = LayerMask.GetMask("CustomMap");
            Ray ray = mapCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, mapLayer);
            foreach (RaycastHit hit in hits)
            {

                if (hit.transform.GetComponent<Button>() != null)
                {
                    stationButton = hit.transform.gameObject;
                    break;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {

            LayerMask mapLayer = LayerMask.GetMask("CustomMap");
            Ray ray = mapCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, mapLayer);
            foreach (RaycastHit hit in hits)
            {

                if (hit.transform.GetComponent<Button>() != null &&
                    hit.transform.gameObject == stationButton
                )
                {
                    hit.transform.GetComponent<Button>().onClick.Invoke();
                    break;
                }
            }
            stationButton = null;
        }
    }

    public void InitMap()
    {
        personOnMap = ToolsController.instance.InstantiateObject("Map/PersonPrefab", mapsRoot.transform);
        if (personOnMap != null) { personOnMapDirection = ToolsController.instance.FindGameObjectByName(personOnMap, "GeoDirection"); }

        if (showRealPositionOnMap)
        {
            realPersonOnMap = ToolsController.instance.InstantiateObject("Map/RealPersonPrefab", mapsRoot.transform);
        }

        realPositionOnMap = abstractMap.GeoToWorldPosition(GetCurrentLocation(), true);
        positionOnMap = abstractMap.GeoToWorldPosition(GetCurrentLocation(), true);
        SetMapRenderTexture();
    }

    public IEnumerator InitMapCoroutine()
    {

        //yield return new WaitForSeconds(1.0f);
        yield return new WaitForEndOfFrame();

        try
        {

            Vector2d loc = GetCurrentLocation();
            double lat = loc.x;
            double lon = loc.y;

            float dist = ToolsController.instance.CalculateDistance(
                (float)lat, (float)Params.mapCenter.x, (float)lon, (float)Params.mapCenter.y);
            if (dist > 2000)
            {
                lat = Params.mapCenter.x;
                lon = Params.mapCenter.y;
            }

            lat = Params.mapCenter.x;
            lon = Params.mapCenter.y;

            print(new Vector2d(lat, lon));

            abstractMap.SetCenterLatitudeLongitude(new Vector2d(lat, lon));
            abstractMap.SetZoom(Params.mapZoom);
            abstractMap.UpdateMap(new Vector2d(lat, lon), Params.mapZoom);

            print("Center map to our position " + lat + " " + lon);

        }
        catch (UnityException e)
        {

            print("MapController InitCity error " + e.Message);
        }
    }

    public void DisableMap()
    {
        isEnabled = false;
        mapsRoot.SetActive(false);
    }

    public void EnableMap()
    {
        isEnabled = true;
        mapsRoot.SetActive(true);
    }

    public void Reset()
    {

        isEnabled = false;
        mapsRoot.SetActive(false);
        geoPositions.Clear();

        //for( int i = 0; i < stations.Count; i++ ){ Destroy(stations[i]); }
        //stations.Clear();
    }

    public Vector2 GetCurrentLocationVector2()
    {

        Vector2d loc = GetCurrentLocation();
        return new Vector2((float)loc.x, (float)loc.y);
    }

    public Vector2d GetCurrentLocation()
    {

        Vector2d currentLocation = Params.mapPersonTestPosition;

        try
        {

            // AR GPS Plugin			
            if (ARLocationProvider.Instance != null)
            {

                double lat = ARLocationProvider.Instance.CurrentLocation.latitude;
                double lon = ARLocationProvider.Instance.CurrentLocation.longitude;

                currentLocation.x = lat;
                currentLocation.y = lon;
            }
            else
            {

                // Mapbox
                if (_locationProviderMapbox != null)
                {

                    Mapbox.Unity.Location.Location currLoc = _locationProviderMapbox.CurrentLocation;
                    if (currLoc.IsLocationServiceInitializing)
                    {
                        //_statusText.text = "location services are initializing";
                    }
                    else
                    {
                        if (!currLoc.IsLocationServiceEnabled)
                        {
                            //_statusText.text = "location services not enabled";
                        }
                        else
                        {
                            if (currLoc.LatitudeLongitude.Equals(Vector2d.zero))
                            {
                                //_statusText.text = "Waiting for location ....";
                            }
                            else
                            {
                                //_statusText.text = string.Format("{0}", currLoc.LatitudeLongitude);

                                currentLocation = currLoc.LatitudeLongitude;
                            }
                        }
                    }
                }
            }

        }
        catch (UnityException e)
        {

            print("MapController error " + e.Message);
        }

#if UNITY_EDITOR
        return new Vector2d(Params.mapPersonTestPosition.x, Params.mapPersonTestPosition.y);
#endif

        return currentLocation;
    }

    public void UpdateMyPositionImmdiate()
    {
        if (personOnMap != null)
        {
            realPositionOnMap = abstractMap.GeoToWorldPosition(GetCurrentLocation(), true);
            positionOnMap = GetMyPositionOnPath();
            personOnMap.transform.localPosition = positionOnMap;
            personOnMap.transform.localScale = new Vector3(poiScale, poiScale, poiScale);
        }
    }

    public void UpdateMap()
    {
        if (personOnMap != null)
        {

            realPositionOnMap = abstractMap.GeoToWorldPosition(GetCurrentLocation(), true);
            positionOnMap = GetMyPositionOnPath();
            UpdateCompass();

            if (Input.GetMouseButton(0) || isCentering)
            {

                personOnMap.transform.localPosition = positionOnMap;
                if (realPersonOnMap != null)
                {
                    realPersonOnMap.transform.localPosition = realPositionOnMap;
                }

            }
            else
            {

                personOnMap.transform.localPosition = Vector3.Lerp(
                    personOnMap.transform.localPosition, positionOnMap, Time.deltaTime * lerpSpeed);

                if (realPersonOnMap != null)
                {
                    realPersonOnMap.transform.localPosition = Vector3.Lerp(
                        realPersonOnMap.transform.localPosition, realPositionOnMap, Time.deltaTime * lerpSpeed);
                }
            }

            personOnMap.transform.localScale = new Vector3(poiScale, poiScale, poiScale);

            if (realPersonOnMap != null)
            {
                realPersonOnMap.transform.localScale = new Vector3(poiScale, poiScale, poiScale);
            }
        }

        if (exampleMap != null)
        {

            Vector3 pos = abstractMap.GeoToWorldPosition(new Vector2d(exampleMapPosition.x, exampleMapPosition.y), true);

            pos.y += yOffset;
            exampleMap.transform.position = pos;

            float z = abstractMap.Zoom;
            z = abstractMap.transform.localScale.x;
            exampleMap.transform.localScale = Vector3.one * exampleMapZoomFactor * z;
        }

        for (int i = 0; i < stations.Count; i++)
        {

            Vector3 pos = abstractMap.GeoToWorldPosition(
                new Vector2d(
                stations[i].GetComponent<MapStation>().geoPosition.x,
                stations[i].GetComponent<MapStation>().geoPosition.y), true);

            pos.y += yOffset;
            stations[i].transform.position = pos;

            if (!keepStationsSize)
            {
                float z = abstractMap.Zoom;
                z = abstractMap.transform.localScale.x;
                stations[i].transform.localScale = Vector3.one * mapStationZoomFactor * z;
            }
            else
            {

                stations[i].transform.localScale = Vector3.one * poiScale;
            }
        }

        for (int i = 0; i < filterStations.Count; i++)
        {
            Vector3 pos = abstractMap.GeoToWorldPosition(
                new Vector2d(
                filterStations[i].GetComponent<MapStation>().geoPosition.x,
                filterStations[i].GetComponent<MapStation>().geoPosition.y), true);

            pos.y += yOffset;
            filterStations[i].transform.position = pos;

            if (!keepStationsSize)
            {
                float z = abstractMap.Zoom;
                z = abstractMap.transform.localScale.x;
                filterStations[i].transform.localScale = Vector3.one * mapStationZoomFactor * z;
            }
            else
            {

                filterStations[i].transform.localScale = Vector3.one * poiScale;
            }
        }
    }

    public void UpdateCompass()
    {
        if (personOnMapDirection == null) return;

#if !UNITY_EDITOR
        if (!Input.compass.enabled) return;
#endif
        float compassAngle = 0;

#if UNITY_EDITOR
        compassAngle = ARController.instance.mainCamera.transform.eulerAngles.y;
#else
        compassAngle = Input.compass.trueHeading;
        //compassAngle = Input.compass.magneticHeading;
#endif

        float targetAngleCompass = Mathf.SmoothDampAngle(
            currentCompassAngle,
            compassAngle,
            ref compassVelocity,
            compassSmoothTime);

        currentCompassAngle = targetAngleCompass;
        personOnMapDirection.transform.localEulerAngles = new Vector3(0, 0, -targetAngleCompass);
    }

    public Vector3 GetMyPositionOnPath()
    {

        Vector2d currentGeoPosition = GetCurrentLocation();
        float minDist = float.MaxValue;
        int index = 0;
        for (int i = 0; i < geoPositions.Count; i++)
        {

            Vector2d p1 = currentGeoPosition;
            Vector2d p2 = geoPositions[i];
            float dist = ToolsController.instance.CalculateDistance(
                (float)p1.x, (float)p2.x, (float)p1.y, (float)p2.y);

            if (dist < minDist)
            {
                minDist = dist;
                index = i;
            }
        }

        distanceToNearestPathPosition = minDist;
        //infoLabel.text = "Distanz zum Pfad: " + minDist.ToString("F2");

        Vector2d correctedPosition = currentGeoPosition;
        if ((geoPositions.Count > index) && (minDist < snapToPathMinDistance))
        {
            correctedPosition = geoPositions[index];
        }

        //infoLabel.text =
        //    "GPS-Position:\n" + currentGeoPosition.x.ToString("F10") + "\n" + currentGeoPosition.y.ToString("F10") + "\n\n" +
        //    "Korrigierte GPS-Position:\n" + correctedPosition.x.ToString("F10") + "\n" + correctedPosition.y.ToString("F10");

        Vector2d calculatedTargetGeoPosition = currentGeoPosition;
        Vector3 targetPositionOnMap = Vector3.zero;

        if (minDist < snapToPathMinDistance)
        {

            calculatedTargetGeoPosition = geoPositions[index];
            targetPositionOnMap = abstractMap.GeoToWorldPosition(geoPositions[index]);

        }
        else
        {

            calculatedTargetGeoPosition = currentGeoPosition;
            targetPositionOnMap = realPositionOnMap;
        }

        Vector3 onTowerPosition = Vector3.zero;
        bool nearTower = IsInStationRange(calculatedTargetGeoPosition, ref onTowerPosition);
        if (nearTower)
        {
            return onTowerPosition;
        }

        return targetPositionOnMap;
    }

    public bool IsInStationRange(Vector2d calculatedTargetGeoPosition, ref Vector3 onTowerPosition)
    {

        Vector2d towerCenter = new Vector2d(52.1614688237d, 8.0362739881d);

        float dist = ToolsController.instance.CalculateDistance(
            (float)calculatedTargetGeoPosition.x, (float)towerCenter.x,
            (float)calculatedTargetGeoPosition.y, (float)towerCenter.y);

        //print(calculatedTargetGeoPosition);
        //print("Dist to tower " + dist);

        if (dist < snapToTowerDistance)
        {

            Vector2d onTowerPositionGeoPosition = new Vector2d(52.1617599516d, 8.0362518670d);
            onTowerPosition = abstractMap.GeoToWorldPosition(onTowerPositionGeoPosition);
            return true;

        }
        return false;
    }

    public Vector2d GetMyLocationOnPath()
    {

        Vector2d currentGeoPosition = GetCurrentLocation();
        float minDist = float.MaxValue;
        int index = 0;
        for (int i = 0; i < geoPositions.Count; i++)
        {

            Vector2d p1 = currentGeoPosition;
            Vector2d p2 = geoPositions[i];
            float dist = ToolsController.instance.CalculateDistance(
                (float)p1.x, (float)p2.x, (float)p1.y, (float)p2.y);

            if (dist < minDist)
            {
                minDist = dist;
                index = i;
            }
        }

        Vector2d correctedPosition = currentGeoPosition;
        if ((geoPositions.Count > index) && (minDist < snapToPathMinDistance))
        {
            correctedPosition = geoPositions[index];
        }

        if (minDist < snapToPathMinDistance)
        {

            return correctedPosition;

        }
        else
        {

            return currentGeoPosition;
        }
    }

    public void UpdateSnapToPathDistance(TMP_InputField inputfield)
    {

        float val = snapToPathMinDistance;
        bool parsed = float.TryParse(inputfield.text, out val);
        if (parsed)
        {
            snapToPathMinDistance = val;
        }
    }

    public void SetMapRenderTexture()
    {

        int w = CanvasController.instance.GetContentPixelWidth();
        int h = CanvasController.instance.GetContentPixelHeight();

        RenderTexture rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
        //RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);

        rt.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat_S8_UInt;
        rt.Create();
        mapRawImage.texture = rt;

        mapCamera.targetTexture = rt;
        mapCamera.Render();
    }

    public void UpdateLayer()
    {
        foreach (Transform child in mapHolder.transform)
        {
            child.gameObject.layer = mapLayer;
        }
    }

    public void SwitchMapMode(int id)
    {
        if (id == 0)
        {
            abstractMap.ImageLayer.SetLayerSource(ImagerySourceType.MapboxSatelliteStreet);
        }
        else
        {
            abstractMap.ImageLayer.SetLayerSource(ImagerySourceType.MapboxOutdoors);
        }
    }

    private void RotateMapWithTwoFingers()
    {
        compassNeedle.eulerAngles = new Vector3(0, 0, mapCamera.transform.eulerAngles.y);

#if UNITY_EDITOR
        if (Input.GetKey("r"))
        {
            mapCamera.transform.Rotate(Vector3.forward, Time.deltaTime * 5);
        }
#endif

        if (Input.touchCount == 2)
        {
            var touch1 = Input.GetTouch(0);
            var touch2 = Input.GetTouch(1);

            Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
            Vector2 prevPos2 = touch2.position - touch2.deltaPosition;
            Vector2 prevDir = prevPos2 - prevPos1;
            Vector2 currDir = touch2.position - touch1.position;

            float angle = 0;
            if (prevPos1.y < prevPos2.y)
            {
                if (prevDir.x < currDir.x)
                {
                    angle = Vector2.Angle(prevDir, currDir);
                }
                else
                {
                    angle = -Vector2.Angle(prevDir, currDir);
                }
            }
            else
            {
                if (prevDir.x < currDir.x)
                {
                    angle = -Vector2.Angle(prevDir, currDir);
                }
                else
                {
                    angle = Vector2.Angle(prevDir, currDir);
                }
            }

            //mapCamera.transform.Rotate(0, angle, 0);
            mapCamera.transform.Rotate(Vector3.forward, angle);
        }
    }

    public void ScanMarker()
    {
		GlasObjectController.instance.scanMarkerSelected = true;

		//ScanController.instance.skipScanButton.SetActive( false );
		ScanController.instance.skipScanButton.SetActive( true );

		StationController.instance.SetCurrentStationDataFromStationId(selectedStationId);
        string markerId = StationController.instance.GetStationMarkerId(selectedStationId);

        if (!StationController.instance.skipScanIfNoGuide)
        {
            StationController.instance.stationMapDialog.SetActive(false);
            ScanController.instance.StartScan();
        }
        else
        {
            if (StationController.instance.StationHasFeature(selectedStationId, "guide"))
            {
                StationController.instance.stationMapDialog.SetActive(false);
                ScanController.instance.StartScan();
            }
            else if (markerId != "")
            {
                StationController.instance.stationMapDialog.SetActive(false);
                ScanController.instance.OnMarkerTracked(markerId);
            }
            else
            {
                InfoController.instance.ShowMessage("Inhalte dieser Station konnten nicht abgerufen werden.");
            }
        }
    }

    public void ContinueWithoutMarker()
    {
		GlasObjectController.instance.scanMarkerSelected = false;
		ScanController.instance.skipScanButton.SetActive( true );

		StationController.instance.SetCurrentStationDataFromStationId(selectedStationId);

        string markerId = StationController.instance.GetStationMarkerId(selectedStationId);
        ScanController.instance.useGuideInAR = PermissionController.instance.IsARFoundationSupported();
        bool useGuideInAR = StationController.instance.UseGuideInAR() && PermissionController.instance.IsARFoundationSupported();
        if (StationController.instance.StationHasFeature(selectedStationId, "guide") && useGuideInAR)
        {
            StationController.instance.stationMapDialog.SetActive(false);
            ScanController.instance.StartScanWithoutMarker();
        }
		else if ( StationController.instance.StationHasFeature( selectedStationId, "avatarGuide" ) && useGuideInAR )
		{
			StationController.instance.stationMapDialog.SetActive( false );
			ScanController.instance.StartScanWithoutMarker();
		}
		else if ( selectedStationId == "glasmacher" && useGuideInAR )
		{
			StationController.instance.stationMapDialog.SetActive( false );
			ScanController.instance.StartScanWithoutMarker();
		}
		else if ( selectedStationId.Contains("glashuette") )
		{
			GlasObjectController.instance.isScanningMarker = false;
			GlasObjectController.instance.hasScannedMarker = false;
			StationController.instance.stationMapDialog.SetActive( false );

			if ( PermissionController.instance.IsARFoundationSupported() )
			{
				ScanController.instance.StartScanWithoutMarker();
			}
			else
			{
				ARMenuController.instance.InitMenu( markerId );
				ARMenuController.instance.OpenMenu( selectedStationId );
			}
		}
		else if (markerId != "")
        {
            StationController.instance.stationMapDialog.SetActive(false);
            ScanController.instance.OnMarkerTracked(markerId);
        }
        else
        {
            InfoController.instance.ShowMessage("Inhalte dieser Station konnten nicht abgerufen werden.");
        }
    }

	public void Back()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        if (SiteController.instance.previousSite != null)
        {
            if (SiteController.instance.previousSite.siteID == "DashboardSite") { yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite")); }
            else if (SiteController.instance.previousSite.siteID == "TourOverviewSite") { yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("TourOverviewSite")); }
            else { yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite")); }
        }
        else
        {
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));
        }

        ResetStationBlinking();
        isLoading = false;
    }

    public void OnClickOnFilteredStation(MapStation mapStation, JSONNode stationData)
    {
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "MapSite") return;

        if (mapStation.multiLocationIndicator != null && mapStation.multiLocationIndicator.activeInHierarchy)
        {

            List<GameObject> sameStations = GetSameLocations(mapStation.gameObject);
            print("MultiLocation, Count: " + sameStations.Count);

            foreach (Transform child in sameLocationList.transform) { Destroy(child.gameObject); }

            List<string> locationTitles = new List<string>();
            for (int i = 0; i < sameStations.Count; i++)
            {
                JSONNode stationDataTmp = sameStations[i].GetComponent<MapStation>().dataJson;
                //print(stationDataTmp["id"].Value);

                if (stationDataTmp["title"] == null) continue;

                GameObject obj = ToolsController.instance.InstantiateObject("Map/LocationListElement", sameLocationList.transform);

                GameObject title = ToolsController.instance.FindGameObjectByName(obj, "Title");
                GameObject categories = ToolsController.instance.FindGameObjectByName(obj, "Categories");
                GameObject timings = ToolsController.instance.FindGameObjectByName(obj, "Timings");
                GameObject line = ToolsController.instance.FindGameObjectByName(obj, "Line");
                categories.SetActive(false);
                timings.gameObject.SetActive(false);
                line.SetActive(true);
                if (i + 1 >= sameStations.Count) { line.SetActive(false); }

                // Title
                title.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslationFromNode(stationDataTmp["title"]);

                // Categories
                string ca = StationController.instance.GetGategories(stationDataTmp);
                if (ca != "")
                {
                    categories.SetActive(true);
                    categories.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(ca, true);
                }

                // Timings
                string timingsText = StationController.instance.GetTimings(stationDataTmp);
                if (timingsText != "")
                {
                    timings.SetActive(true);
                    timings.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(timingsText, true);
                }

                // Button
                int index = i;
                obj.GetComponentInChildren<Button>(true).onClick.AddListener(() => OpenLocationListElement(sameStations[index].GetComponent<MapStation>().stationId, stationDataTmp));

                // Location
                string loc = StationController.instance.GetLocationName(stationDataTmp);
                if (loc != "" && !locationTitles.Contains(loc)) { locationTitles.Add(loc); }
            }

            sameLocationTitle.SetActive(false);
            if (locationTitles.Count > 0)
            {

                sameLocationTitle.SetActive(true);
                string locTitle = "";
                for (int i = 0; i < locationTitles.Count; i++)
                {

                    locTitle += locationTitles[i];
                    if (i + 1 < locationTitles.Count) { locTitle += ", "; }
                }

                sameLocationTitle.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(locTitle, true);
            }

            sameLocationStationsContent.SetActive(true);
        }
        else
        {
            MapFilterController.instance.LoadInfo(mapStation.stationId, stationData);
        }
    }

    public void OpenLocationListElement(string id, JSONNode stationData)
    {
        sameLocationStationsContent.SetActive(false);

        //ToolsController.instance.OpenWebView(StationController.instance.GetEventWebLink(stationData));
        MapFilterController.instance.LoadInfo(id, stationData);
    }

    public void OnClickOnStation(string stationId)
    {
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "MapSite") return;
        ResetStationBlinking();

        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i].GetComponent<MapStation>().stationId == stationId)
            {
                OnClickOnStation(stations[i].GetComponent<MapStation>(), stations[i].GetComponent<MapStation>().dataJson);
                break;
            }
        }
    }

    public void OnClickOnStation(MapStation mapStation, JSONNode stationData)
    {
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "MapSite") return;
        ResetStationBlinking();

        string stationId = stationData["id"].Value;
        selectedStationId = stationId;
        print("Station stationId: " + stationId);

        string eventName = "MapStationClick_" + stationId;
        FirebaseController.instance.LogEvent(eventName);

        StationController.instance.LoadStationInfoOnMap(mapStation, stationData);
    }

    public int GetScore(string stationId)
    {

        int score = 0;

        string highscoreId = stationId + "_Highscore";
        score += PlayerPrefs.GetInt(highscoreId);

        print(stationId + "_Highscore " + score);
        return score;
    }

    public void CloseStationMessageDialog()
    {

        StationController.instance.stationMapDialog.SetActive(false);
    }

    public void MarkStationFinished(string stationId)
    {

        string tourId = TourController.instance.GetCurrentTourId();
        PlayerPrefs.SetInt(tourId + "_" + stationId + "_finished", 1);

        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i].GetComponent<MapStation>().stationId == stationId)
            {
                stations[i].GetComponent<MapStation>().finishedUI.SetActive(true);
                stations[i].GetComponent<MapStation>().markerLabel.color = Params.mapStationFinishedLabelColor;
                break;
            }
        }
    }

    public void MarkStationUnFinished(string stationId)
    {

        string tourId = TourController.instance.GetCurrentTourId();
        PlayerPrefs.SetInt(tourId + "_" + stationId + "_finished", 0);

        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i].GetComponent<MapStation>().stationId == stationId)
            {
                stations[i].GetComponent<MapStation>().finishedUI.SetActive(false);
                stations[i].GetComponent<MapStation>().markerLabel.color = Params.mapStationNotFinishedLabelColor;
                break;
            }
        }
    }

    public void CenterMap()
    {
        if (isCentering) return;
        isCentering = true;
        StartCoroutine(CenterMapCoroutine());
    }

    public IEnumerator CenterMapCoroutine()
    {
        InfoController.instance.blocker.SetActive(true);
        dragEnabled = false;

        Vector2d myGeoLocation = GetMyLocationOnPath();
        Vector2d currentCenterLocation = abstractMap.CenterLatitudeLongitude;
        float targetZoom = Params.mapZoom;
        float currentZoom = abstractMap.Zoom;
        float currentTime = 0.0f;
        float animationDuration = 1.0f;

        AnimationCurve animationCurve =
            AnimationController.instance.GetAnimationCurveWithID("fastSlow");
        while (currentTime < animationDuration)
        {
            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            double lat = LerpDouble(currentCenterLocation.x, myGeoLocation.x, lerpValue);
            double lon = LerpDouble(currentCenterLocation.y, myGeoLocation.y, lerpValue);
            float zoom = Mathf.Lerp(currentZoom, targetZoom, lerpValue);
            abstractMap.UpdateMap(new Vector2d(lat, lon), zoom);
            currentTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        abstractMap.UpdateMap(myGeoLocation, targetZoom);

        dragEnabled = true;
        InfoController.instance.blocker.SetActive(false);
        isCentering = false;
    }

    public IEnumerator CenterMapCoroutine(Vector2d myGeoLocation)
    {
        InfoController.instance.blocker.SetActive(true);
        dragEnabled = false;

        Vector2d currentCenterLocation = abstractMap.CenterLatitudeLongitude;
        float targetZoom = Params.mapZoom;
        float currentZoom = abstractMap.Zoom;
        float currentTime = 0.0f;
        float animationDuration = 1.0f;

        AnimationCurve animationCurve =
            AnimationController.instance.GetAnimationCurveWithID("fastSlow");
        while (currentTime < animationDuration)
        {
            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            double lat = LerpDouble(currentCenterLocation.x, myGeoLocation.x, lerpValue);
            double lon = LerpDouble(currentCenterLocation.y, myGeoLocation.y, lerpValue);
            float zoom = Mathf.Lerp(currentZoom, targetZoom, lerpValue);

            try { abstractMap.UpdateMap(new Vector2d(lat, lon), zoom); } catch (Exception e) { print("Failed Updated Map " + e.Message); }

            currentTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        try { abstractMap.UpdateMap(myGeoLocation, targetZoom); } catch (Exception e) { print("Failed Updated Map " + e.Message); }

        dragEnabled = true;
        InfoController.instance.blocker.SetActive(false);
        isCentering = false;
    }

    public static double LerpDouble(double a, double b, float t)
    {
        return a + (b - a) * t;
    }

    public void LoadMapFromMenu()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(LoadMapFromMenuCoroutine());
    }

    public IEnumerator LoadMapFromMenuCoroutine()
    {
        yield return StartCoroutine(LoadMapCoroutine());
        isLoading = false;
    }

    public IEnumerator LoadMapCoroutine()
    {
        print("LoadMapCoroutine");

        bool mapInitialized = initialized;
        bool canShowLocation = false;

        if (gpsPermissionAsked)
        {
            if (!PermissionController.instance.HasPermissionLocation() ||
                !PermissionController.instance.LocationServiceEnabled()
            )
            {
                Init(false);
            }
            else
            {
                Init();
            }
        }
        else
        {

            if (PlayerPrefs.GetString("PermissionLocationAsked") != "true")
            {
                mapUIRoot.SetActive(false);
                PlayerPrefs.SetString("PermissionLocationAsked", "true");
                PlayerPrefs.Save();
            }
            else
            {

                if (!PermissionController.instance.HasPermissionLocation())
                {
                    mapUIRoot.SetActive(false);
                }
                else
                {
                    if (!PermissionController.instance.LocationServiceEnabled())
                    {
                        mapUIRoot.SetActive(false);
                    }
                    else
                    {
                        canShowLocation = true;
                        gpsPermissionAsked = true;
                        mapUIRoot.SetActive(true);
                        mapGPSInfoRoot.SetActive(false);
                        Init();
                    }
                }
            }
        }

        if (!mapInitialized)
        {
            InfoController.instance.loadingCircle.SetActive(true);
            yield return new WaitForSeconds(1.0f);
            InfoController.instance.loadingCircle.SetActive(false);
        }

        if (!canShowLocation && !gpsPermissionAsked)
        {
            gpsPermissionAsked = true;
            StartCoroutine(ValidateLocationServicesCoroutine());
        }

        MenuController.instance.CloseMenu();
        MapFilterController.instance.LoadTours();
        UpdateMyPositionImmdiate();

        bool shouldOpenDragMenu = false;
        if (shouldShowMapFilter)
        {
            shouldShowMapFilter = false;
            //EnableDisableTourStations(false);
            //MapFilterController.instance.filterOptionsDragMenu.dragContent.gameObject.SetActive( Params.showMapFilterOptions );
            //MapFilterController.instance.currentFilterContent.SetActive(false);
            //MapFilterController.instance.headerCurrentSelection.SetActive(false);
            //MapFilterController.instance.SetDragMenuFilterSelection("");
            //MapFilterController.instance.SetDragMenuTourSelection("");

            shouldOpenDragMenu = true;
			if ( Params.showMapFilterOptions ) { MapFilterController.instance.filterOptionsDragMenu.CloseImmediate(); }
            //MapFilterController.instance.filterOptionsDragMenu.Open();
            PlayerPrefs.SetInt("filterOptionsDragMenu_Showed", 1);
        }
        else
        {
            MapFilterController.instance.SelectTour(TourController.instance.GetCurrentTourId());
            if (!StationController.instance.clickedOnStationInList) { shouldOpenDragMenu = true; }
        }

        EnableDisablePath(false);
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));

        if ( shouldOpenDragMenu && Params.showMapFilterOptions )
		{
			yield return new WaitForSeconds(0.25f);
			MapFilterController.instance.filterOptionsDragMenu.Open();
		}

        isLoading = false;
    }

    public void LocationPermissionDenied()
    {
        Init();
        mapUIRoot.SetActive(true);
        mapGPSInfoRoot.SetActive(false);
        gpsPermissionAsked = true;
    }

    public void ContinueToMap()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(ContinueToMapCoroutine());
    }

    public IEnumerator ContinueToMapCoroutine()
    {

        if (Application.platform == RuntimePlatform.IPhonePlayer &&
            !PermissionController.instance.LocationServiceEnabled()
        )
        {
            print("Need to forward to settings to enable location services");
            ShowLocationServiceDialog();
        }
        else if (!PermissionController.instance.HasPermissionLocation())
        {

            print("No Location Permission");

            bool hasPermissionLocation = false;
            int permissionState = 0;
            yield return StartCoroutine(
                PermissionController.instance.RequestLocationPermissionCoroutine((bool success, int state) => {
                    hasPermissionLocation = success;
                    permissionState = state;
                })
            );

            yield return new WaitForSeconds(0.5f);

            if (hasPermissionLocation)
            {
                print("Location Permission granted");

                if (!PermissionController.instance.LocationServiceEnabled())
                {
                    print("Need to forward to settings to enable location services");
                    ShowLocationServiceDialog();
                }
                else
                {
                    print("All granted and enabled");

                    Init();
                    mapUIRoot.SetActive(true);
                    mapGPSInfoRoot.SetActive(false);
                }
            }
            else
            {

                print("Location Permission denied 1");

                // Forward to settings
                if (permissionState == 3)
                {
                    print("Need to forward to settings to grant location permission");
                    ShowLocationPermissionDialog();
                }
                else
                {

                    print("Location Permission denied 2");

                    Init(false);
                    mapUIRoot.SetActive(true);
                    mapGPSInfoRoot.SetActive(false);
                }
            }
        }
        else
        {

            print("Location Permission is already granted");

            if (!PermissionController.instance.LocationServiceEnabled())
            {

                print("Need to forward to settings to enable location services 2");
                ShowLocationServiceDialog();
            }
            else
            {

                print("All granted and enabled 2");

                Init();
                mapUIRoot.SetActive(true);
                mapGPSInfoRoot.SetActive(false);
            }
        }

        gpsPermissionAsked = true;
        isLoading = false;
    }

    public void ForwardToAppSettings()
    {
        Init();
        mapUIRoot.SetActive(true);
        mapGPSInfoRoot.SetActive(false);
        PermissionController.instance.DirectToAppSettings();
    }

    public void RejectLocationPermission()
    {
        Init(false);
        mapUIRoot.SetActive(true);
        mapGPSInfoRoot.SetActive(false);
        isAskingForLocationPermission = false;
    }

    public void ForwardToLocationServiceSettings()
    {
        Init();
        mapUIRoot.SetActive(true);
        mapGPSInfoRoot.SetActive(false);
        PermissionController.instance.DirectToLocationServiceSettings();
    }

    public void RejectLocationService()
    {
        Init(false);
        mapUIRoot.SetActive(true);
        mapGPSInfoRoot.SetActive(false);
    }

    public void ShowLocationServiceDialog()
    {
        InfoController.instance.ShowCommitAbortDialog(
            "Standortfreigabe", "Bitte erlaube in den Einstellungen den Zugriff auf Deinen Standort, damit wir Dir Deine Position in der Karte anzeigen können.",
            ForwardToLocationServiceSettings, RejectLocationService);
    }

    public void ShowLocationPermissionDialog()
    {
        print("ShowLocationPermissionDialog");

        InfoController.instance.ShowCommitAbortDialog(
            "Standort freigeben", "Bitte erlaube der App den Zugriff auf Deinen Standort, damit sie Dir Deine Position in der Karte anzeigen kann.",
            ForwardToAppSettings, RejectLocationPermission);
    }
    public void ShowLocationInfoDialog()
    {
        mapGPSInfoRoot.SetActive(true);
    }

    public void ValidateLocationServices()
    {
        if (isValidatingLocationServices) return;
        isValidatingLocationServices = true;
        StartCoroutine(ValidateLocationServicesCoroutine());
    }

    public IEnumerator ValidateLocationServicesCoroutine()
    {
        mapGPSInfoRoot.SetActive(true);
        yield return null;

        yield return StartCoroutine(LocationServicesController.instance.ValidateLocationPermissionCoroutine());
    }

    public void OnLocationServicesGranted()
    {
        Init();
        mapUIRoot.SetActive(true);
        mapGPSInfoRoot.SetActive(false);

        if (StationController.instance.shouldFocusStation)
        {

            StationController.instance.shouldFocusStation = false;
            StartCoroutine(FocusStationCoroutine());
        }
    }

    public void OnLocationServicesDenied()
    {
        Init(false);
        mapUIRoot.SetActive(true);
        mapGPSInfoRoot.SetActive(false);

        if (StationController.instance.shouldFocusStation)
        {

            StationController.instance.shouldFocusStation = false;
            StartCoroutine(FocusStationCoroutine());
        }
    }

    public IEnumerator FocusStationCoroutine()
    {

        yield return new WaitForSeconds(0.5f);
        FocusStation(TourController.instance.currentTourId, StationController.instance.currentListStationData);
    }

    public void RequestLocationPermission()
    {
        StartCoroutine(RequestLocationPermissionCoroutine());
    }

    public IEnumerator RequestLocationPermissionCoroutine()
    {
        bool hasPermissionLocation = false;
        int permissionState = 0;
        yield return StartCoroutine(
            PermissionController.instance.RequestLocationPermissionCoroutine((bool success, int state) => {
                hasPermissionLocation = success;
                permissionState = state;
            })
        );

        yield return new WaitForSeconds(0.5f);
        isAskingForLocationPermission = false;
    }

    public IEnumerator CheckInRangeOfLocationCoroutine(Action<bool> Callback)
    {

        InitGPS();

        InfoController.instance.loadingCircle.SetActive(true);
        yield return new WaitForSeconds(1.0f);
        Vector2d currentGeoPosition = GetCurrentLocation();

        float timer = 10.0f;
        while (
            timer > 0 &&
            currentGeoPosition.x == 0 && currentGeoPosition.y == 0
        )
        {

            yield return new WaitForSeconds(1.0f);
            timer -= 1.0f;

            currentGeoPosition = GetCurrentLocation();
        }

        //currentGeoPosition = new Vector2d( 52.16112d, 8.03647d );
        print("currentGeoPosition " + currentGeoPosition);

        Vector2d targetLocation = new Vector2d(52.16111d, 8.03646d);
        float dist = ToolsController.instance.CalculateDistance(
            (float)currentGeoPosition.x, (float)targetLocation.x,
            (float)currentGeoPosition.y, (float)targetLocation.y);

        print("Dist to target location " + dist);

        if (dist > 50)
        {
            Callback(false);
        }
        else
        {
            Callback(true);
        }

        InfoController.instance.loadingCircle.SetActive(false);
    }

    public bool IsMoveMapEnabled()
    {
        if (!dragEnabled) return false;
        for (int i = 0; i < dragMenus.Count; i++) { if (dragMenus[i].dragEnabled) { return false; } }
        if (sameLocationStationsContent.activeInHierarchy) return false;
        if (StationController.instance.stationMapDialog.activeInHierarchy) return false;
		if ( Params.showMapFilterOptions && MapFilterController.instance.filterOptionsDragMenu.GetComponent<Canvas>().enabled && MapFilterController.instance.filterOptionsDragMenu.menuIsOpen ) return false;

		return true;
    }

    public void CheckNearStation()
    {

#if UNITY_EDITOR
        return;
#endif

        if (StationController.instance.stationMapDialog.activeInHierarchy) return;

        float updatedIntervall = 5.0f;
        float stationRange = 50.0f;

        if (checkNearStationTimer < updatedIntervall) { checkNearStationTimer += Time.deltaTime; return; }
        checkNearStationTimer = 0;

        float minDist = float.MaxValue;
        string nearestStationId = "";
        int nearestIndex = -1;
        Vector2d currentGeoPosition = GetCurrentLocation();

        for (int i = 0; i < stations.Count; i++)
        {
            if (!stations[i].activeInHierarchy) continue;

            string stationIdTmp = stations[i].GetComponent<MapStation>().stationId;
            if (nearByStationIds.Contains(stationIdTmp)) continue;

            Vector2d stationPoisiton = stations[i].GetComponent<MapStation>().geoPosition;
            float dist = ToolsController.instance.CalculateDistance(
                (float)currentGeoPosition.x, (float)stationPoisiton.x, (float)currentGeoPosition.y, (float)stationPoisiton.y);

            //print(stationIdTmp + " " + currentGeoPosition + " " + stationPoisiton + " " + dist);

            if (dist < stationRange && dist < minDist)
            {
                nearestStationId = stationIdTmp;
                nearestIndex = i;
            }
        }

#if UNITY_EDITOR
        if (nearestStationId == "") { nearestStationId = editorNearestStationTestId; }
#endif

        if (nearestStationId != "" && !nearByStationIds.Contains(nearestStationId))
        {
            nearByStationIds.Add(nearestStationId);

            if (nearestIndex >= 0) { OnClickOnStation(stations[nearestIndex].GetComponent<MapStation>(), stations[nearestIndex].GetComponent<MapStation>().dataJson); }
            else { OnClickOnStation(nearestStationId); }

            StationController.instance.stationNearByInfo.SetActive(true);
        }
    }

    public void FocusStation(string tourId, JSONNode stationData)
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(FocusStationCoroutine(tourId, stationData));
    }

    public IEnumerator FocusStationCoroutine(string tourId, JSONNode stationData)
    {
        MapFilterController.instance.SelectTour(tourId);
		if ( Params.showMapFilterOptions ) { MapFilterController.instance.filterOptionsDragMenu.Close(); }

        double latitude = ToolsController.instance.GetDoubleValueFromJsonNode(stationData["latitude"]);
        double longitude = ToolsController.instance.GetDoubleValueFromJsonNode(stationData["longitude"]);

        isCentering = true;
        yield return StartCoroutine(CenterMapCoroutine(new Vector2d(latitude, longitude)));
        isCentering = false;

        GameObject stationButton = GetStationButton(stationData["id"].Value);
        if (stationButton != null)
        {

            stationButton.GetComponent<MapStation>().Blink(10);
            ///yield return new WaitForSeconds(1.0f);
            //stationButton.GetComponent<MapStation>().isBlinking = false;
        }

        isLoading = false;
    }

    public void ResetStationBlinking()
    {
        for (int i = 0; i < stations.Count; i++) { stations[i].GetComponent<MapStation>().isBlinking = false; }
    }
}
