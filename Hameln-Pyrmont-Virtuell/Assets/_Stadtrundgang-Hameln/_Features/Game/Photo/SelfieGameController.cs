using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SelfieGameController : MonoBehaviour
{
    [Header("Guide fixed on Screen params")]
    private bool useFixedGuidePositionOnScreen = false;
    private bool frameIsReady = false;
    private bool videoIsPrepared = false;
    public RawImage guideOnScreen;
    public VideoPlayer videoPlayer;
    public VideoClip borisClip;
    public VideoClip ricardaClip;

    [Space(10)]

    public TextMeshProUGUI titleLabel;
    public PhotoHelper photoHelper;
    public GameObject guideRoot;
    public Renderer guideInAR;
    public GameObject tutorialContent;
    public GameObject arContent;
    public GameObject background;

    [Space(10)]

    public GameObject mainCamera;
    public GameObject eventSystem;
    public GameObject dummyPlane;

    private Vector3 hitOffset = Vector3.zero;
    private bool placementEnabled = true;
    private bool isMovingGuide = false;
    private bool isLoading = false;
    public int gameType = 0;

    public static SelfieGameController instance;
    void Awake()
    {
        instance = this;
        videoPlayer.sendFrameReadyEvents = true;
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
    }

    void Update()
    {
        if (SiteController.instance != null && SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "SelfieGameSite") return;

        if (placementEnabled) { MoveGuide(); }
    }

    public IEnumerator InitCoroutine(int gameType = 0)
    {
        yield return null;
        this.gameType = gameType;

        mainCamera = ARController.instance.mainCamera;
        tutorialContent.SetActive(true);
        arContent.SetActive(false);

        if (gameType == 1)
        {
            useFixedGuidePositionOnScreen = true;
            ARController.instance.StopARSession();
            titleLabel.text = LanguageController.GetTranslation("Mache ein Selfi mit unserem Stadtführer");
            videoPlayer.clip = ricardaClip;
        }
        else
        {
            useFixedGuidePositionOnScreen = false;
            titleLabel.text = LanguageController.GetTranslation("Mache ein Foto mit unserem Stadtführer");
            videoPlayer.clip = borisClip;
        }
    }

    public void StartSelfieGame()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine("StartSelfieGameCoroutine");       
    }

    public IEnumerator StartSelfieGameCoroutine()
    {
        if (useFixedGuidePositionOnScreen)
        {
            InfoController.instance.loadingCircle.SetActive(true);
            yield return new WaitForSeconds(0.25f);
            yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine());
            yield return StartCoroutine(PlayVideoCoroutine());
            yield return new WaitForSeconds(0.25f);
            InfoController.instance.loadingCircle.SetActive(false);
        }
        else
        {
            if (!ARController.instance.arSession.enabled)
            {
                InfoController.instance.loadingCircle.SetActive(true);
                ARController.instance.arPlaneManager.enabled = false;
                ARController.instance.InitARFoundation();
                yield return new WaitForSeconds(0.5f);
                InfoController.instance.loadingCircle.SetActive(false);
            }

            bool shouldScan = true;
            if (shouldScan) {

                tutorialContent.SetActive(false);
                background.SetActive(false);
                yield return StartCoroutine( ScanController.instance.EnableScanCoroutine() );           
            }
            if (shouldScan) { yield return new WaitForSeconds(2.0f); }

            guideRoot.SetActive(true);
            Vector3 targetPosition = mainCamera.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized * 1.5f;
            targetPosition.y = mainCamera.transform.position.y - 1.4f;
            guideRoot.transform.position = targetPosition;

            InfoController.instance.loadingCircle.SetActive(true);
            yield return new WaitForSeconds(0.25f);
            yield return StartCoroutine(PlayVideoCoroutine());
            yield return new WaitForSeconds(0.25f);
            InfoController.instance.loadingCircle.SetActive(false);
        }

        tutorialContent.SetActive(false);
        arContent.SetActive(true);
        background.SetActive(false);

        if (!useFixedGuidePositionOnScreen)
        {
            if (InfoController.instance != null)
            {
                yield return new WaitForSeconds(0.25f);
                //InfoController.instance.ShowMessage("Scanne den Boden vor dir und platziere den Stadtführer, indem Du mit dem Finger den Boden antippst.", "", EnablePlaceGuide);
                InfoController.instance.ShowMessage("Stadtführer platzieren", "Platziere den Stadtführer, indem Du mit dem Finger den Boden antippst.", EnablePlaceGuide);
            }
            else { EnablePlaceGuide(); }
        }
        
        isLoading = false;
    }

    public void EnablePlaceGuide()
    {
        placementEnabled = true;
    }

    public void MoveGuide()
    {
        if (!guideRoot.activeInHierarchy) { isMovingGuide = true; }

        if (Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject())
        {
            RaycastHit[] hits;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject == guideRoot)
                {
                    hitOffset = hits[i].point - guideRoot.transform.position;
                    isMovingGuide = true;
                    break;
                }
            }

            isMovingGuide = true;
        }
        else if (isMovingGuide && Input.GetMouseButton(0))
        {
            Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
            Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;

            bool hitGround = false;
            if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, out hitPosition)){
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
                    if (hits[i].transform.gameObject == dummyPlane)
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

            guideRoot.SetActive(true);

            //guideRoot.transform.position = hitPosition - hitOffset;
            guideRoot.transform.position = hitPosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMovingGuide = false;
        }
    }

    public void Back()
    {
        if (tutorialContent.activeInHierarchy)
        {
	        InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);
        }
        else
        {
            //InfoController.instance.ShowCommitAbortDialog("Möchtest Du die Station beenden?", CommitBack);
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
        yield return null;

        Reset();
        yield return StartCoroutine(InitCoroutine(this.gameType));

        isLoading = false;
    }

    public bool IsUsingWebcam(){ return useFixedGuidePositionOnScreen; }

    IEnumerator PlayVideoCoroutine()
    {
        videoIsPrepared = false;
        videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
        videoPlayer.Prepare();

        // Wait until video is prepared
        float timer = 4;
        while (!videoIsPrepared && timer > 0)
        {
            yield return null;
            timer -= Time.deltaTime;
        }
        videoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;
        //print("prepareCompleted " + timer);

        frameIsReady = false;
        videoPlayer.frameReady += VideoPlayer_frameReady;
        videoPlayer.Play();

        // Wait for the first video frame, then show video image
        timer = 4;
        while (!frameIsReady && timer > 0)
        {
            yield return null;
            timer -= Time.deltaTime;
        }
        videoPlayer.frameReady -= VideoPlayer_frameReady;

        guideOnScreen.texture = videoPlayer.texture;
        guideInAR.material.mainTexture = videoPlayer.texture;
    }

    private void VideoPlayer_frameReady(UnityEngine.Video.VideoPlayer vp, long frame){ frameIsReady = true; }
    private void VideoPlayer_prepareCompleted(VideoPlayer source){ videoIsPrepared = true; }



    public void Reset()
    {
        StopCoroutine("StartSelfieGameCoroutine");
        ScanController.instance.DisableScanCoroutine();

        //placementEnabled = false;
        arContent.SetActive(false);
        background.SetActive(true);
        tutorialContent.SetActive(false);
        guideRoot.SetActive(false);
        photoHelper.Reset();

        //videoPlayer.Stop();
		videoPlayer.Pause();

		if (useFixedGuidePositionOnScreen) {
            WebcamController.instance.DisablePhotoCamera(); 
        }
    }
}
