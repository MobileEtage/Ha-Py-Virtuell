using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using TMPro;
using SimpleJSON;
using MPUIKIT;

public class ViewingPigeonsController : MonoBehaviour
{
    public GameObject uiContent;
    public GameObject tutorialContent1;
    public GameObject tutorialContent2;
    public GameObject arContent;
    public GameObject background;

    [Space(10)]

    public GameObject mainCamera;
    public GameObject eventSystem;
    public GameObject dummyPlane;

    [Space(10)]

    public GameObject hashtag3dRoot;
    public GameObject hashtag3dButton;
    public GameObject hashtag2dButton;
    public GameObject hashtagSelection;
    public PhotoHelper photoHelper;

    [Space(10)]

    public List<GameObject> hashtagButtons = new List<GameObject>();

    private GameObject mainPrefab;
    private GameObject shadowPlane;
    public int currentSelectedHashtag = -1;

    private bool placementEnabled = false;
    private bool isLoading = false;
    private bool isMovingHashtag = false;
    private Vector3 camPosition = new Vector3(-1000, -1000, -1000);
    private List<GameObject> arObjects = new List<GameObject>();
    private List<GameObject> shadowPlanes = new List<GameObject>();
    private Vector3 hitOffset = Vector3.zero;
    private float spawnDelay = 0.75f;
    private float currentSpawnTime = 0.0f;

    public static ViewingPigeonsController instance;
    void Awake()
    {

        instance = this;
    }

    void Start()
    {

        if (ARController.instance == null)
        {

            mainCamera.SetActive(true);
            eventSystem.SetActive(true);
            dummyPlane.SetActive(true);
            GameObject toolsController = new GameObject("ToolsController");
            toolsController.AddComponent<ToolsController>();
        }

#if UNITY_EDITOR
        dummyPlane.SetActive(true);
#endif
    }

    void LateUpdate()
    {
        if (placementEnabled)
        {

            hashtag3dRoot.transform.position = ARController.instance.mainCamera.transform.position;
            hashtag3dRoot.transform.eulerAngles = ARController.instance.mainCamera.transform.eulerAngles;

            //Vector3 forward = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z);
            //Vector3 forward = new Vector3(mainCamera.transform.forward.x, mainCamera.transform.forward.y, mainCamera.transform.forward.z);
            //Vector3 pos = mainCamera.transform.position + forward.normalized * 2.0f;
            //hashtag3dButton.transform.position = pos;

            MoveHashtag3D();

            if (currentSpawnTime >= spawnDelay) { PlaceObject(); }
            else { currentSpawnTime += Time.deltaTime; }

            UpdatePigeons();
        }
    }

    public void Init()
    {

        mainCamera = ARController.instance.mainCamera;

#if UNITY_EDITOR
        camPosition = ARController.instance.mainCamera.transform.position;
        ARController.instance.mainCamera.transform.position = new Vector3(0, 1.7f, -4.0f);
#endif

        if (mainPrefab == null)
        {
            GameObject prefab = Resources.Load("Pigeon", typeof(GameObject)) as GameObject;
            mainPrefab = ToolsController.instance.InstantiateObject(prefab, this.transform);
            mainPrefab.SetActive(false);
        }

        SelectHashtag(0);
        arContent.SetActive(false);
        tutorialContent1.SetActive(true);
        tutorialContent2.SetActive(false);
        uiContent.SetActive(true);
    }

    public void StartAR()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine("StartARCoroutine");
    }

    public IEnumerator StartARCoroutine()
    {
        bool hasPermission = true;
        yield return StartCoroutine(PermissionController.instance.ValidatePermissionsCameraCoroutine("arFeature", (bool success) => { hasPermission = success; }));
        if (!hasPermission) { isLoading = false; yield break; }

        tutorialContent1.SetActive(false);
        tutorialContent2.SetActive(true);
        isLoading = false;
    }

    public void SelectHashtag(int index)
    {

        currentSelectedHashtag = index;

        for (int i = 0; i < hashtagButtons.Count; i++)
        {
            hashtagButtons[i].GetComponentInChildren<MPImage>().StrokeWidth = 5;
            hashtagButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = ToolsController.instance.GetColorFromHexString("#313131");
        }

        if (currentSelectedHashtag >= 0 && currentSelectedHashtag < hashtagButtons.Count)
        {
            hashtagButtons[currentSelectedHashtag].GetComponentInChildren<MPImage>().StrokeWidth = 0;
            hashtagButtons[currentSelectedHashtag].GetComponentInChildren<TextMeshProUGUI>().color = ToolsController.instance.GetColorFromHexString("#FFFFFF");
        }
    }

    public void CommitWithoutHashtagSelection()
    {
        if (isLoading) return;
        isLoading = true;

        currentSelectedHashtag = -1;
        StartCoroutine("CommitHashtagSelectionCoroutine");
    }

    public void CommitHashtagSelection()
    {

        if (isLoading) return;
        isLoading = true;
        StartCoroutine("CommitHashtagSelectionCoroutine");
    }

    public IEnumerator CommitHashtagSelectionCoroutine()
    {
        if (!ARController.instance.arSession.enabled)
        {
            InfoController.instance.loadingCircle.SetActive(true);
            ARController.instance.arPlaneManager.enabled = false;
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
            InfoController.instance.loadingCircle.SetActive(false);
        }

        tutorialContent2.SetActive(false);
        background.SetActive(false);

        bool shouldScan = true;
        if (shouldScan) { yield return StartCoroutine(ScanController.instance.EnableScanCoroutine()); }
        if (shouldScan) { yield return new WaitForSeconds(2.0f); }

        placementEnabled = true;
        tutorialContent2.SetActive(false);
        arContent.SetActive(true);
        background.SetActive(false);

        if (InfoController.instance != null)
        {
            if (currentSelectedHashtag == -1 && PlayerPrefs.GetInt("pigeonsInfoShowed", 0) != 1)
            {
                InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.pigeons_desc);
                PlayerPrefs.SetInt("pigeonsInfoShowed", 1);
            }
            else if (PlayerPrefs.GetInt("pigeonsAndHashtagInfoShowed", 0) != 1)
            {
                InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.hashtag_desc);
                PlayerPrefs.SetInt("pigeonsAndHashtagInfoShowed", 1);
            }
        }

        PlaceHashtagObject();

        isLoading = false;
    }

    public void PlaceHashtagObject()
    {

        if (currentSelectedHashtag >= 0 && currentSelectedHashtag < hashtagButtons.Count)
        {

            hashtag3dButton.SetActive(true);
            hashtag3dButton.GetComponentInChildren<TextMeshProUGUI>(true).text = hashtagButtons[currentSelectedHashtag].GetComponentInChildren<TextMeshProUGUI>(true).text;

            hashtag3dRoot.transform.position = ARController.instance.mainCamera.transform.position;
            hashtag3dRoot.transform.eulerAngles = ARController.instance.mainCamera.transform.eulerAngles;

            //Vector3 forward = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z);
            Vector3 forward = new Vector3(mainCamera.transform.forward.x, mainCamera.transform.forward.y, mainCamera.transform.forward.z);
            Vector3 up = new Vector3(mainCamera.transform.up.x, mainCamera.transform.up.y, mainCamera.transform.up.z);
            Vector3 pos = mainCamera.transform.position + forward.normalized * 2.0f + up * 0.6f;
            hashtag3dButton.transform.position = pos;

            /*
            hashtag2dButton.SetActive(true);
            hashtag2dButton.GetComponentInChildren<TextMeshProUGUI>(true).text = hashtagButtons[currentSelectedHashtag].GetComponentInChildren<TextMeshProUGUI>(true).text;
			hashtag2dButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
			*/
        }
        else
        {
            hashtag2dButton.SetActive(false);
            hashtag3dButton.SetActive(false);
        }
    }

    public void MoveHashtag3D()
    {

        hashtagSelection.SetActive(false);
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit[] hits;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {

                if (hits[i].transform.gameObject == hashtag3dButton)
                {

                    hitOffset = hits[i].point - hashtag3dButton.transform.position;
                    isMovingHashtag = true;
                    break;
                }
            }
        }
        else if (isMovingHashtag && Input.GetMouseButton(0))
        {
            hashtagSelection.SetActive(true);
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 2.0f);
            Vector3 pos = mainCamera.GetComponent<Camera>().ScreenToWorldPoint(mousePosition);

            //Vector3 forward = new Vector3( ray.direction.x, 0, ray.direction.z);
            //Vector3 pos = ray.origin + forward.normalized*2.0f + hitOffset;
            //Vector3 pos = ray.origin + forward.normalized*2.0f;
            hashtag3dButton.transform.position = pos;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMovingHashtag = false;
        }
    }

    public void UpdatePigeons()
    {

        GameObject objToRemove = null;
        for (int i = 0; i < arObjects.Count; i++)
        {

            if (arObjects[i].GetComponent<Pigeon>().targetReached)
            {

                objToRemove = arObjects[i];

                /*
				float dist = Vector3.Distance(mainCamera.transform.position, arObjects[i].transform.position);
				print(dist);
				if( dist > 30 ){
					objToRemove = arObjects[i];
				}
				*/
            }
        }

        if (objToRemove != null)
        {
            arObjects.Remove(objToRemove);
            Destroy(objToRemove);
        }
    }

    public bool HitOtherPigeon()
    {

        Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits;
        hits = Physics.RaycastAll(ray, 100);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.GetComponent<Pigeon>() != null)
            {

                float dist = Vector3.Distance(hits[i].point, mainCamera.transform.position);
                if (dist < 4.0f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void PlaceObject()
    {

        if (isMovingHashtag) return;
        if (arObjects.Count > 20) return;

        if (Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject())
        {

            if (HitOtherPigeon()) return;

            Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
            Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;
	        bool isFlying = true;
            
            #if UNITY_ANDROID
	        //if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, out hitPosition))
	        if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, true, out hitPosition))
            #else
	        if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, true, out hitPosition))
            #endif
	        {
                isFlying = false;
            }
            else
            {

#if UNITY_EDITOR

                Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
                RaycastHit[] hits;
                hits = Physics.RaycastAll(ray, 100);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.gameObject == dummyPlane)
                    {
                        hitPosition = hits[i].point;
                        isFlying = false;
                        break;
                    }
                }

#endif

                if (isFlying)
                {
                    Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                    hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;
                }
            }

            float dist = Vector3.Distance(hitPosition, mainCamera.transform.position);
            if (dist > 3.0f)
            {

                print("Hit distance to far");

                isFlying = true;
                Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;

                //return;
            }

            //string[] prefabPaths = new string[]{"Models/Crow_PBR", "Models/Sparrow_PBR", "Models/Pigeon_PBR"};
            //string[] prefabPaths = new string[]{"Pigeon"};
            //var prefab = Resources.Load( prefabPaths[Random.Range(0,prefabPaths.Length)], typeof(GameObject)) as GameObject;

            if (!isFlying)
            {

                if (shadowPlane == null)
                {
                    shadowPlane = ToolsController.instance.InstantiateObject("ShadowPlaneMulti", this.transform);
                    shadowPlane.transform.position = hitPosition;
	                shadowPlane.transform.localScale = Vector3.one * 1;
                    //shadowPlanes.Add(shadowPlane);
                }
            }

            if (mainPrefab == null)
            {
                GameObject prefab = Resources.Load("Pigeon", typeof(GameObject)) as GameObject;
                mainPrefab = ToolsController.instance.InstantiateObject(prefab, this.transform);
                mainPrefab.SetActive(false);
            }

            GameObject obj = ToolsController.instance.InstantiateObject(mainPrefab, this.transform);
            obj.transform.position = hitPosition;
            Vector3 rot = obj.transform.eulerAngles;
            obj.transform.LookAt(mainCamera.transform);
            obj.transform.eulerAngles = new Vector3(rot.x, obj.transform.eulerAngles.y + Random.Range(-90, 90), rot.z);
            obj.GetComponent<Pigeon>().InitMovement(isFlying);
            arObjects.Add(obj);

            // Set custom layer and light to be able to fade in shadow with _ShadowIntensity from shader
            string layerName = "Pigeon";
            LayerMask layerMask = LayerMask.GetMask(layerName);
            ToolsController.instance.ChangeLayer(obj, layerName);
            obj.GetComponent<Pigeon>().myLight.GetComponent<Light>().cullingMask = layerMask;
            obj.GetComponent<Pigeon>().myLight.transform.SetParent(null);
            obj.GetComponent<Pigeon>().myLight.transform.eulerAngles = LightController.instance.directionalLight.transform.eulerAngles;
            obj.GetComponent<Pigeon>().myLight.SetActive(true);

            obj.GetComponent<Pigeon>().shadowPlane.SetActive(true);
            obj.GetComponent<Pigeon>().shadowPlane.transform.SetParent(null);

            if (isFlying && shadowPlane != null)
            {
                obj.GetComponent<Pigeon>().shadowPlane.transform.position = shadowPlane.transform.position;
                obj.GetComponent<Pigeon>().shadowPlane.transform.eulerAngles = shadowPlane.transform.eulerAngles;
            }

            currentSpawnTime = 0;
        }
    }

    public void OnPhotoTaken()
    {
        float sizeY = photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().rect.height;
        float aspect = photoHelper.photoPreviewImage.GetComponent<Image>().sprite.rect.width / photoHelper.photoPreviewImage.GetComponent<Image>().sprite.rect.height;
        float sizeX = aspect * sizeY;

        photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeX, photoHelper.photoPreviewImage.transform.parent.GetComponent<RectTransform>().sizeDelta.y);
    }

    public void Back()
    {
        if (tutorialContent1.activeInHierarchy)
        {
            InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);

        }
        else if (tutorialContent2.activeInHierarchy)
        {
            tutorialContent1.SetActive(true);
            tutorialContent2.SetActive(false);
        }
        else
        {
            CommitBack();
        }
    }

    public void CommitBack()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        //yield return StartCoroutine(StationController.instance.BackToStationSiteCoroutine());
        //ARMenuController.instance.MarkMenuButton("");
        //ARMenuController.instance.currentFeature = "";

        yield return null;

        Reset();
        Init();

        isLoading = false;
    }

    public void Reset()
    {
        if (ScanController.instance != null) { ScanController.instance.DisableScanCoroutine(); }
        StopCoroutine("CommitHashtagSelectionCoroutine");
        MediaCaptureController.instance.Reset();

        background.SetActive(true);
        hashtag2dButton.SetActive(false);
        hashtag3dButton.SetActive(false);
        arContent.SetActive(false);
        tutorialContent1.SetActive(false);
        tutorialContent2.SetActive(false);
        uiContent.SetActive(false);
        hashtagSelection.SetActive(false);
        photoHelper.Reset();
        SelectHashtag(-1);
        currentSelectedHashtag = -1;

        //placementEnabled = false;
        isMovingHashtag = false;

        for (int i = 0; i < arObjects.Count; i++) { Destroy(arObjects[i]); }
        arObjects.Clear();
        for (int i = 0; i < shadowPlanes.Count; i++) { Destroy(shadowPlanes[i]); }
        shadowPlanes.Clear();

        if (mainPrefab != null) { Destroy(mainPrefab); mainPrefab = null; }
        if (shadowPlane != null) { Destroy(shadowPlane); shadowPlane = null; }

#if UNITY_EDITOR
        if (camPosition.x != -1000) { ARController.instance.mainCamera.transform.position = camPosition; }
#endif
    }
}
