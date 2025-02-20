using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using Unity.VisualScripting;

public class AudiothekController : MonoBehaviour
{
    public GameObject audioElementsRoot;
    public GameObject tutorialContent;
    public GameObject arContent;
    public GameObject background;

    [Space(10)]

    public AudioSource audioSource;
    public int spectrumDataCount = 256;
    public FFTWindow spectrumType = FFTWindow.BlackmanHarris;

    public float sphereDefaultScale = 0.3f;
    public float sphereSelectedScale = 0.5f;
    public float speechScaleFactor = 10f;
    public float speechScaleLerpFactor = 5f;

    [Space(10)]

    public GameObject mainCamera;
    public GameObject eventSystem;
    public GameObject dummyPlane;

    [Space(10)]

    public GameObject currentAudioSphere;
    public GameObject audioNavigation;
    public GameObject playButton;
    public GameObject pauseButton;
    public UnityEngine.UI.Slider timeSlider;
    public TextMeshProUGUI currentTimeLabel;
    public TextMeshProUGUI leftTimeLabel;
    public TextMeshProUGUI audioTitle;
    public float audioNavigationAlpha = 0;
    public bool audioSliderSelected = false;

    [Space(10)]

    public float spawnIntervall = 0.2f;
    public Color sphereDefaultColor;
    public Color sphereSelectedColor;
    public List<GameObject> audioSpheres = new List<GameObject>();

    private bool isActive = false;
    private bool isLoading = false;

    public static AudiothekController instance;
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
            ToolsController.instance.InstantiateObject("AnimationController", this.transform);

            StartCoroutine(InitCoroutine());
        }
    }

    void Update()
    {
        if (SiteController.instance != null && SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "AudiothekSite") return;
        DetectHit();
        UpdateAudioTimer();
        UpdateAudioTimerVisiblity();
        AnimateSpeech();
    }

    public void AnimateSpeech()
    {
        if (!audioSource.isPlaying || audioSource.clip == null || currentAudioSphere == null)
        {
            if (currentAudioSphere != null) { currentAudioSphere.GetComponent<AudioSphere>().targetScale = sphereSelectedScale; }
            return;
        }

        float[] spectrumData = new float[spectrumDataCount];
        audioSource.GetSpectrumData(spectrumData, 0, spectrumType);

        float s = 0;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            s += spectrumData[i];
        }
        s = s / spectrumDataCount;

        float targetScale = Mathf.Clamp(s * speechScaleFactor, sphereDefaultScale, sphereSelectedScale);
        currentAudioSphere.GetComponent<AudioSphere>().targetScale = targetScale;
    }

    public void DetectHit()
    {
        if (!isActive) return;

        if (Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject())
        {
            RaycastHit[] hits;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.GetComponentInParent<AudioSphere>())
                {
                    OnHitSphere(hits[i].transform.GetComponentInParent<AudioSphere>().gameObject);
                    break;
                }
            }
        }
    }

    public void OnHitSphere(GameObject audioSphere)
    {
        if (currentAudioSphere != audioSphere)
        {
            if (currentAudioSphere != null)
            {
                currentAudioSphere.GetComponentInChildren<Renderer>().material.SetColor("_RimColor", sphereDefaultColor);
                currentAudioSphere.GetComponent<AudioSphere>().targetScale = sphereDefaultScale;
            }

            //if (audioSource.isPlaying)
            {
                audioSource.time = 0;
                audioSource.Stop();
            }

            AudioClip audioClip = Resources.Load<AudioClip>(audioSphere.GetComponent<AudioSphere>().audioFile);
            if (audioClip != null) { audioSource.clip = audioClip; }
            currentAudioSphere = audioSphere;
            currentAudioSphere.GetComponentInChildren<Renderer>().material.SetColor("_RimColor", sphereSelectedColor);
            currentAudioSphere.GetComponent<AudioSphere>().targetScale = sphereSelectedScale;

            audioTitle.text = LanguageController.GetTranslation(audioSphere.GetComponent<AudioSphere>().audioTitle);
            PlayAudio();
        }
        else if (currentAudioSphere == audioSphere)
        {
            OnReachedLoopPoint();
        }
        else
        {
            if (currentAudioSphere != null) { PlayAudio(); }
        }
    }

    public void PlayAudio()
    {
        if (audioSource.clip != null)
        {
            if (!audioSource.isPlaying) { audioSource.Play(); }
            playButton.SetActive(false);
            pauseButton.SetActive(true);

            audioNavigation.SetActive(true);
            audioNavigationAlpha = 1.0f;
        }
    }

    public void PauseAudio()
    {
        if (audioSource.clip != null)
        {
            audioSource.Pause();
            playButton.SetActive(true);
            pauseButton.SetActive(false);
        }
    }

    public void PlayPauseAudio()
    {
        if (audioSource.isPlaying)
        {
            PauseAudio();
        }
        else
        {
            PlayAudio();
        }
    }

    public void OnReachedLoopPoint()
    {
        if (audioSource.clip != null)
        {
            audioSource.time = 0;
            audioSource.Stop();
            audioNavigationAlpha = 0;
            //audioNavigation.SetActive(false);

            if (currentAudioSphere != null)
            {
                currentAudioSphere.GetComponentInChildren<Renderer>().material.SetColor("_RimColor", sphereDefaultColor);
                currentAudioSphere.GetComponent<AudioSphere>().targetScale = sphereDefaultScale;
            }
            currentAudioSphere = null;
        }
    }

    private void UpdateAudioTimer()
    {
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            float audioDuration = audioSource.clip.length;
            float audioTime = audioSource.time;
            if (audioDuration != 0)
            {
                if (!audioSliderSelected)
                {
                    timeSlider.value = (float)(audioTime / audioDuration);
                    currentTimeLabel.text = GetMinutes(audioTime) + ":" + GetSeconds(audioTime);
                    leftTimeLabel.text = "-" + GetMinutes(audioDuration - audioTime) + ":" + GetSeconds(audioDuration - audioTime);
                }

            }

            if (audioTime >= (audioDuration - 0.1f))
            {
                OnReachedLoopPoint();
            }
        }
    }

    public string GetMinutes(double time)
    {
        TimeSpan VideoUrlLength = TimeSpan.FromSeconds(time);
        if (VideoUrlLength.Minutes < 10) return VideoUrlLength.Minutes.ToString("0");
        return VideoUrlLength.Minutes.ToString("00");
    }
    public string GetSeconds(double time)
    {
        TimeSpan VideoUrlLength = TimeSpan.FromSeconds(time);
        return VideoUrlLength.Seconds.ToString("00");
    }

    public void SetAudioTimeBySlider()
    {
        if (audioSource.clip == null) return;

        float val = timeSlider.value;
        float audioDuration = audioSource.clip.length;

        if (audioDuration != 0)
        {
            audioSource.time = Mathf.Clamp(val * audioDuration, 0, audioDuration - 2f);
        }

        if (audioSource.isPlaying)
        {
            playButton.SetActive(false);
            pauseButton.SetActive(true);
        }


        StopCoroutine("WaitForSeekCompletedCoroutine");
        StartCoroutine("WaitForSeekCompletedCoroutine");
    }

    IEnumerator WaitForSeekCompletedCoroutine()
    {
        yield return new WaitForEndOfFrame();
        audioSliderSelected = false;
    }

    public void StepForward()
    {
        if (audioSource.clip == null) return;

        audioSliderSelected = true;
        float audioDuration = audioSource.clip.length;
        float step = (float)(15 / audioDuration);

        float percentage = 2 / audioDuration;
        timeSlider.value = Mathf.Clamp(timeSlider.value + step, 0, 1 - percentage);

        SetAudioTimeBySlider();
    }

    public void StepBack()
    {
        if (audioSource.clip == null) return;

        audioSliderSelected = true;
        float audioDuration = audioSource.clip.length;
        float step = (float)(15 / audioDuration);

        float percentage = 2 / audioDuration;
        timeSlider.value = Mathf.Clamp(timeSlider.value - step, 0, 1 - percentage);

        SetAudioTimeBySlider();
    }

    private void UpdateAudioTimerVisiblity()
    {
        audioNavigation.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(audioNavigation.GetComponent<CanvasGroup>().alpha, audioNavigationAlpha, Time.deltaTime * 6);
    }

    public IEnumerator InitCoroutine()
    {
        yield return null;

        if (ARController.instance != null) { mainCamera = ARController.instance.mainCamera; }
        tutorialContent.SetActive(true);
        arContent.SetActive(false);

        for (int i = 0; i < audioSpheres.Count; i++)
        {
            audioSpheres[i].transform.localScale = Vector3.zero;
            audioSpheres[i].GetComponentInChildren<Renderer>(true).material.SetColor("_RimColor", sphereDefaultColor);
            audioSpheres[i].GetComponent<AudioSphere>().targetScale = sphereDefaultScale;
            audioSpheres[i].SetActive(false);
        }
    }

    public void StartAudiothek()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine("StartAudiothekCoroutine");
    }

    public IEnumerator StartAudiothekCoroutine()
    {
        if (ARController.instance != null && !ARController.instance.arSession.enabled)
        {
            InfoController.instance.loadingCircle.SetActive(true);
            ARController.instance.arPlaneManager.enabled = false;
            ARController.instance.InitARFoundation();
            yield return new WaitForSeconds(0.5f);
            InfoController.instance.loadingCircle.SetActive(false);
        }

        tutorialContent.SetActive(false);
        background.SetActive(false);

        bool shouldScan = true;
        if (shouldScan){ yield return StartCoroutine( ScanController.instance.EnableScanCoroutine()); }

        if (InfoController.instance != null)
        {
            if (shouldScan) { yield return new WaitForSeconds(2.0f); }
            InfoController.instance.ShowMessage("Sprechblasen", "Schau Dich um und finde die schwebenden Sprechblasen. Tippe sie an, um Dir ihren Inhalt anzuhören.", CommitStartAudiothek);
        }
        else
        {
            isLoading = false;
            CommitStartAudiothek();
        }

        arContent.SetActive(true);
        isLoading = false;
    }

    public void CancelScan()
    {
        StopCoroutine("StartAudiothekCoroutine");
        ScanController.instance.DisableScanCoroutine();

        ARController.instance.ShowHidePlanes(false);
        ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.Invisible);

        ScanController.instance.defaultScanDescription.SetActive(false);
        ScanController.instance.defaultScanDialog.SetActive(false);

        ARController.instance.isScanning = false;
        ARController.instance.scanAnimator.HideScanAnimation();
    }

    public void CommitStartAudiothek()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(CommitStartAudiothekCoroutine());
    }

    public IEnumerator CommitStartAudiothekCoroutine()
    {
        yield return new WaitForSeconds(1.0f);

        audioElementsRoot.SetActive(true);

        //Vector3 targetPosition = mainCamera.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized * 4.0f;
        //targetPosition.y = mainCamera.transform.position.y - 1.4f;
        //audioElementsRoot.transform.position = targetPosition;

        audioElementsRoot.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y - 1.2f, mainCamera.transform.position.z);

        for (int i = 0; i < audioSpheres.Count; i++)
        {
            audioSpheres[i].SetActive(true);
            StartCoroutine(AnimationController.instance.AnimateScaleCoroutine(audioSpheres[i].transform, Vector3.zero, Vector3.one, 0.65f, "smooth"));
            yield return new WaitForSeconds(spawnIntervall);
        }

        isActive = true;
        isLoading = false;
    }

    public void Back()
    {
        if (tutorialContent.activeInHierarchy) { InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation); }
        else { CommitBack(); }
    }

    public void CommitClose()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(CommitCloseCoroutine());
    }

    public IEnumerator CommitCloseCoroutine()
    {
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ImprintSite"));
        ARController.instance.StopARSession();
        Reset();

        isLoading = false;
    }

    public void CommitBack()
    {
        if (ARController.instance.isScanning) { isLoading = false; }

        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        yield return null;

        CancelScan();
        Reset();
        yield return StartCoroutine(InitCoroutine());

        isLoading = false;
    }

    public void Reset()
    {
        for (int i = 0; i < audioSpheres.Count; i++)
        {
            audioSpheres[i].transform.localScale = Vector3.zero;
            audioSpheres[i].GetComponentInChildren<Renderer>(true).material.SetColor("_RimColor", sphereDefaultColor);
            audioSpheres[i].GetComponent<AudioSphere>().targetScale = sphereDefaultScale;
            audioSpheres[i].SetActive(false);
        }

        isActive = false;
        currentAudioSphere = null;
        audioNavigationAlpha = 0;
        audioNavigation.GetComponent<CanvasGroup>().alpha = 0;

        audioElementsRoot.SetActive(false);
        arContent.SetActive(false);
        tutorialContent.SetActive(false);
        background.SetActive(true);

        audioSource.Stop();
    }
}
