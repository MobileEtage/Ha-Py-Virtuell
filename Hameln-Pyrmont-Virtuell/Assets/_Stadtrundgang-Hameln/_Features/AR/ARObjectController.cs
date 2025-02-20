using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ARObjectController : MonoBehaviour
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
    private bool placementEnabled = false;
    private bool isMovingARObject = false;
    private bool isLoading = false;
    private bool placementInfoShowed = false;

    public static ARObjectController instance;
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

    public IEnumerator InitCoroutine()
    {
        yield return null;

        if (ARController.instance != null)
        {
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
        screenObjectRoot.SetActive(false);
        arObjectRoot.SetActive(false);

        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
        yield return new WaitForSeconds(0.25f);

        if (cameraARType == CameraARType.BackFacingAR) { yield return StartCoroutine(InitBackFacingARCoroutine()); }
        else if (cameraARType == CameraARType.BackFacingScreen) { yield return StartCoroutine(InitBackFacingScreenCoroutine()); }
        else if (cameraARType == CameraARType.FrontFacing) { yield return StartCoroutine(InitFrontFacingCoroutine()); }

        if (InfoController.instance != null && !placementInfoShowed)
        {
            yield return new WaitForSeconds(0.25f);
            InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_rotate_scale_desc, EnablePlacement);
            placementInfoShowed = true;
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
        Vector3 targetPosition = mainCamera.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized * 2.5f;
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
        PlaceScreenObject(2.0f, 0f);
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
        PlaceScreenObject(2.0f, 0f);
        switchCameraUI.SetActive(false);
        tutorialUI.SetActive(false);
        arUI.SetActive(true);
        if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
    }

    public void PlaceScreenObject(float distInfront, float offsetY)
    {
        if (screenObjectRoot.GetComponentInChildren<LookAt>(true) != null) { screenObjectRoot.GetComponentInChildren<LookAt>(true).enabled = false; }
        if (screenObjectRoot.GetComponentInChildren<LookAt>(true) != null) { screenObjectRoot.GetComponentInChildren<LookAt>(true).transform.localEulerAngles = Vector3.zero; }

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

        if (cameraARType == CameraARType.FrontFacing)
        {

            WebcamController.instance.DisablePhotoCamera();
            yield return new WaitForSeconds(0.5f);
            cameraARType = cameraARTypeBackFacing;
        }
        else
        {

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
        if (tutorialUI.activeInHierarchy) { InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation); }
        else { CommitBack(); }
    }

    public void Back_v2()
    {
        if (!tutorialUI.activeInHierarchy)
        {
            if (InfoController.instance == null) { CommitBack(); }
            else { InfoController.instance.ShowCommitAbortDialog("Möchtest Du das Spiel beenden?", CommitBack); }
        }
        else
        {
            if (InfoController.instance == null) { }
            else
            {
                Close();
            }
        }
    }

    public void Close()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(CloseCoroutine());
    }

    public IEnumerator CloseCoroutine()
    {
        Reset();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("GamesSite"));
        isLoading = false;
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

        placementEnabled = false;
        placementInfoShowed = false;

        switchCameraUI.SetActive(false);
        arUI.SetActive(false);
        tutorialUI.SetActive(true);

        arObjectRoot.GetComponentInChildren<ViewObjectHelper>(true).Reset();
        screenObjectRoot.GetComponentInChildren<ViewObjectHelper>(true).Reset();
        screenObjectRoot.SetActive(false);
        arObjectRoot.SetActive(false);
    }
}
