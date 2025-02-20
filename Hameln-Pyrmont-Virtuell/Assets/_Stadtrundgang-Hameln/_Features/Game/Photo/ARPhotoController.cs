using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ARPhotoController : MonoBehaviour
{
    public enum CameraARType { BackFacingAR, BackFacingScreen, FrontFacing }
    public CameraARType cameraARType = CameraARType.BackFacingAR;
    public CameraARType cameraARTypeBackFacing = CameraARType.BackFacingAR;
    public bool shouldScan = false;

    [Space(10)]

    public GameObject tutorialUI;
    public GameObject arUI;
    public GameObject switchCameraUI;

    [Space(10)]

    public GameObject screenObjectRoot;
    public GameObject screenObject;
    public GameObject arObjectRoot;

    [Space(10)]

    public GameObject mainCamera;
    public GameObject testHelperObjects;

    private Vector3 hitOffset = Vector3.zero;
    private bool placementEnabled = true;
    private bool isMovingARObject = false;
    private bool selected = false;
    private bool isLoading = false;
    private bool placementInfoShowed = false;

    public static ARPhotoController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (ARController.instance == null)
        {
            testHelperObjects.SetActive(true);
            GameObject toolsController = new GameObject("ToolsController");
            toolsController.AddComponent<ToolsController>();
            placementEnabled = true;

            StartCoroutine(InitCoroutine());
        }
    }

    void LateUpdate()
    {
        HandlePlacement();
    }

    public void HandlePlacement()
    {
        if (!placementEnabled) return;
        if (cameraARType != CameraARType.BackFacingAR){ MoveObjectOnScreen(); }
        else{ MoveARObject(); }
    }

    public void MoveObjectOnScreen()
    {
        if (Input.touchCount >= 2) { isMovingARObject = false; return; }
        if (PhotoCaptureController.instance.photoPreviewImage.gameObject.activeInHierarchy) { return; }

        screenObjectRoot.transform.position = mainCamera.transform.position;
        screenObjectRoot.transform.eulerAngles = mainCamera.transform.eulerAngles;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit[] hits;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject == screenObject)
                {
                    hitOffset = hits[i].point - screenObject.transform.position;

                    //isMovingARObject = true;
                    selected = true;

                    // Start drag object delayed, because maybe we also want to scale it with two fingers
                    StopCoroutine("EnableMoveARObjectCoroutine");
                    StartCoroutine("EnableMoveARObjectCoroutine");

                    break;
                }
            }
        }
        else if (isMovingARObject && Input.GetMouseButton(0))
        {
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 2.0f);
            Vector3 pos = mainCamera.GetComponent<Camera>().ScreenToWorldPoint(mousePosition);
            screenObject.transform.position = pos - hitOffset;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMovingARObject = false;
        }
    }

    public void MoveARObject()
    {
        if (Input.touchCount >= 2) { isMovingARObject = false; return; }

        if (Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject())
        {
            RaycastHit[] hits;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform == arObjectRoot.transform.GetChild(0))
                {
                    hitOffset = hits[i].point - arObjectRoot.transform.position;

                    //isMovingARObject = true;
                    selected = true;

                    // Start drag object delayed, because maybe we also want to scale it with two fingers
                    StopCoroutine("EnableMoveARObjectCoroutine");
                    StartCoroutine("EnableMoveARObjectCoroutine");

                    break;
                }
            }
        }
        else if (isMovingARObject && Input.GetMouseButton(0))
        {
            Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
            Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;

            bool hitGround = false;
            if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, out hitPosition))
            {
                hitGround = true;
            }
            else
            {

#if UNITY_EDITOR

                Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
                RaycastHit[] hits;
                hits = Physics.RaycastAll(ray, 100);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.CompareTag("ARPlane"))
                    {
                        hitPosition = hits[i].point;
                        hitGround = true;
                        break;
                    }
                }
#endif

                if (!hitGround)
                {
                    Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                    hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;
                }
            }

            //arObject.transform.position = hitPosition - hitOffset;
            arObjectRoot.transform.position = hitPosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopCoroutine("EnableMoveARObjectCoroutine");
            isMovingARObject = false;
        }
    }

    public IEnumerator EnableMoveARObjectCoroutine()
    {
        yield return new WaitForSeconds(0.15f);
        if (Input.touchCount >= 2) { isMovingARObject = false; selected = false; }
        else { isMovingARObject = true; }
    }

    public IEnumerator InitCoroutine()
    {
        yield return null;

        if (ARController.instance != null) { 

            mainCamera = ARController.instance.mainCamera;
            WebcamController.instance.webcamContent.GetComponentInChildren<Canvas>(true).worldCamera = mainCamera.GetComponent<Camera>();
        }

        tutorialUI.SetActive(true);
        arUI.SetActive(false);
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

        screenObjectRoot.SetActive(false);
        arObjectRoot.SetActive(false);

        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
        yield return new WaitForSeconds(0.25f);

        if (cameraARType == CameraARType.BackFacingAR){ yield return StartCoroutine(InitBackFacingARCoroutine()); }
        else if (cameraARType == CameraARType.BackFacingScreen){ yield return StartCoroutine(InitBackFacingScreenCoroutine()); }
        else if(cameraARType == CameraARType.FrontFacing){ yield return StartCoroutine(InitFrontFacingCoroutine()); }

        if (PlayerPrefs.GetInt("ARPhotoInfoShowed") != 1)
        {
            if (InfoController.instance != null && !placementInfoShowed)
            {
                yield return new WaitForSeconds(0.25f);
                InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_scale_desc, EnablePlacement);
                placementInfoShowed = true;
                PlayerPrefs.SetInt("ARPhotoInfoShowed", 1);
            }
        }

        isLoading = false;
    }

    public IEnumerator InitBackFacingARCoroutine()
    {
        if (ARController.instance != null && !ARController.instance.arSession.enabled)
        {
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
        }

        tutorialUI.SetActive(false);
        switchCameraUI.SetActive(false);

        if (shouldScan)
        {
            if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
            if (ScanController.instance != null) { yield return StartCoroutine(ScanController.instance.EnableScanCoroutine()); }
            yield return new WaitForSeconds(2.0f);
        }
        else { yield return new WaitForSeconds(1.5f); }

        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
        arObjectRoot.SetActive(true);
        Vector3 targetPosition = mainCamera.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized * 1.5f;
        targetPosition.y = mainCamera.transform.position.y - 1.4f;
        arObjectRoot.transform.position = targetPosition;

        arUI.SetActive(true);
    }

    public IEnumerator InitBackFacingScreenCoroutine()
    {
        yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine(false));
        yield return new WaitForSeconds(0.5f);

        /*
        if (ARController.instance != null && !ARController.instance.arSession.enabled)
        {
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
        }
        */

        screenObjectRoot.SetActive(true);
        PlaceScreenObject(2.0f, -0.6f);
        switchCameraUI.SetActive(false);
        tutorialUI.SetActive(false);
        arUI.SetActive(true);
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
    }

    public IEnumerator InitFrontFacingCoroutine()
    {
        yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine(true));
        yield return new WaitForSeconds(0.5f);

        screenObjectRoot.SetActive(true);
        PlaceScreenObject(2.0f, -0.6f);
        switchCameraUI.SetActive(false);
        tutorialUI.SetActive(false);
        arUI.SetActive(true);
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
    }

    public void PlaceScreenObject(float distInfront, float offsetY)
    {
        screenObjectRoot.GetComponentInChildren<LookAt>(true).enabled = false;
        screenObjectRoot.GetComponentInChildren<LookAt>(true).transform.localEulerAngles = Vector3.zero;

        screenObjectRoot.transform.position = mainCamera.transform.position;
        screenObjectRoot.transform.eulerAngles = mainCamera.transform.eulerAngles;

        Vector3 forward = new Vector3(mainCamera.transform.forward.x, mainCamera.transform.forward.y, mainCamera.transform.forward.z);
        Vector3 up = new Vector3(mainCamera.transform.up.x, mainCamera.transform.up.y, mainCamera.transform.up.z);
        Vector3 pos = mainCamera.transform.position + forward.normalized * distInfront + up * offsetY;
        screenObject.transform.position = pos;
    }

    public void SwitchFrontBackFacing()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(SwitchFrontBackFacingCoroutine());
    }

    public IEnumerator SwitchFrontBackFacingCoroutine()
    {
        switchCameraUI.SetActive(true);
        arUI.SetActive(false);

        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
        yield return new WaitForSeconds(0.25f);

        if (cameraARType == CameraARType.FrontFacing) {

            WebcamController.instance.DisablePhotoCamera();
            yield return new WaitForSeconds(0.5f);
            cameraARType = cameraARTypeBackFacing;
        }
        else {

            WebcamController.instance.DisablePhotoCamera();
            if (ARController.instance != null && ARController.instance.arSession.enabled) { ARController.instance.StopARSession(); }
            yield return new WaitForSeconds(0.5f);
            cameraARType = CameraARType.FrontFacing; 
        }

        yield return StartCoroutine(StartARCoroutine());

        isLoading = false;
    }

    public void EnablePlacement()
    {
        placementEnabled = true;
    }

    public void Back()
    {
        if (tutorialUI.activeInHierarchy){ InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation); }
        else{ CommitBack(); }
    }

    public void CommitBack()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
        yield return new WaitForSeconds(0.25f);

        Reset();

        if (cameraARType == CameraARType.FrontFacing)
        {
            WebcamController.instance.DisablePhotoCamera();
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            WebcamController.instance.DisablePhotoCamera();
            if (ARController.instance != null && ARController.instance.arSession.enabled) { ARController.instance.StopARSession(); }
            yield return new WaitForSeconds(0.5f);
        }

        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
        isLoading = false;
    }

    public void Reset()
    {
        if (ScanController.instance != null) { ScanController.instance.DisableScanCoroutine(); }
        StopCoroutine("StartARCoroutine");
        MediaCaptureController.instance.Reset();

        //placementEnabled = false;
        //placementInfoShowed = false;
        isMovingARObject = false;
        selected = false;

        switchCameraUI.SetActive(false);
        arUI.SetActive(false);
        tutorialUI.SetActive(true);

        screenObjectRoot.GetComponentInChildren<ZoomObjectHandler>(true).reset();
        screenObjectRoot.SetActive(false);
        arObjectRoot.SetActive(false);
    }
}
