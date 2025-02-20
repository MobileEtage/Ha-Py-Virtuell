using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MPUIKIT;
using SimpleJSON;
using TagLib.Riff;

public class TutorialController : MonoBehaviour
{
    public List<GameObject> tutorialStepsOnboarding = new List<GameObject>();
    public List<GameObject> tutorialStepsMain = new List<GameObject>();
    public List<GameObject> tutorialSteps = new List<GameObject>();

    [Space(10)]

    public GameObject eventSystem;
    public GameObject scrollView;
    public GameObject tutorialStepsHolder;
    public GameObject stepPoint;
    public TextMeshProUGUI continueButtonLabel;
    public float currentScrollPosition = 0;
    public float lerpSpeed = 4.0f;
    public int currentIndex = 0;

    private bool isLoading = false;
    private bool tutorialEnded = false;
    private bool hasClicked = false;
    private bool isUpdateingScrollPosition = false;

    private Vector3 swipeHitPoint;
    private bool hitSwipe = false;
    private float minSwipeWidthPercentage = 0.2f;
    private List<string> swipeAreaExcludedObjects = new List<string>();

    public static TutorialController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (ARController.instance == null)
        {
            eventSystem.SetActive(true);
            StartCoroutine(InitTutorialCoroutine());
        }
    }

    void LateUpdate()
    {

        if (SiteController.instance == null)
        {
            DetectSwipe();
            UpdateGameTutorialStepPosition();
            UpdateScrollPosition();
        }
        else
        {
            if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID == "TutorialSite")
            {
                DetectSwipe();
                UpdateGameTutorialStepPosition();
                UpdateScrollPosition();
            }
        }
    }

    public void UpdateScrollPosition()
    {
        currentScrollPosition = scrollView.GetComponent<ScrollRect>().horizontalNormalizedPosition;

        if (Input.GetMouseButton(0))
        {
            hasClicked = false;
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isUpdateingScrollPosition = true;
            StartCoroutine(OnEndClickCoroutine());
        }

        float stepPercentage = 1f / (tutorialSteps.Count - 1);
        float targetPercentage = currentIndex * stepPercentage;
        if (!isUpdateingScrollPosition) { scrollView.GetComponent<ScrollRect>().horizontalNormalizedPosition = Mathf.Lerp(scrollView.GetComponent<ScrollRect>().horizontalNormalizedPosition, targetPercentage, Time.deltaTime * lerpSpeed); }
    }

    public IEnumerator OnEndClickCoroutine()
    {
        yield return null;
        if (!hasClicked)
        {
            float currentPercentage = scrollView.GetComponent<ScrollRect>().horizontalNormalizedPosition;
            float stepPercentage = 1f / (tutorialSteps.Count - 1);

            int closest = -1;
            float closestDelta = float.MaxValue;
            for (int i = 0; i < tutorialSteps.Count; i++)
            {
                float p = i * stepPercentage;
                float delta = Mathf.Abs(p - currentPercentage);
                if (delta < closestDelta) { closestDelta = delta; closest = i; }
            }

            if (closest >= 0 && closest < tutorialSteps.Count)
            {
                LoadStep(closest);
            }

        }

        hasClicked = false;
        isUpdateingScrollPosition = false;
    }

    public void UpdateGameTutorialStepPosition()
    {
        float targetPosition = Mathf.Lerp(stepPoint.GetComponent<RectTransform>().anchoredPosition.x, 30 + (currentIndex * 60), Time.deltaTime * 10.0f);
        stepPoint.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPosition, 0);
    }

    public IEnumerator InitTutorialCoroutine(string id = "onboardingTutorial")
    {
        if (id == "mainTutorial" && PermissionController.instance.IsARFoundationSupported())
        {
            tutorialSteps = tutorialStepsMain;
        }
        else if (id == "onboardingTutorial")
        {
            tutorialSteps = tutorialStepsOnboarding;
        }
        else
        {

        }

        foreach (Transform child in tutorialStepsHolder.transform) { child.gameObject.SetActive(false); }
        Transform dotsHolder = stepPoint.GetComponentInParent<GridLayoutGroup>(true).transform;
        foreach (Transform child in dotsHolder) { child.gameObject.SetActive(false); }

        for (int i = 0; i < tutorialSteps.Count; i++)
        {

            tutorialSteps[i].SetActive(true);
            if (i < dotsHolder.childCount) { dotsHolder.GetChild(i).gameObject.SetActive(true); }
        }
        dotsHolder.GetChild(dotsHolder.childCount - 1).gameObject.SetActive(true);

        yield return StartCoroutine(InitCoroutine());
    }

    public IEnumerator InitCoroutine()
    {
        yield return null;

        tutorialEnded = false;
        hasClicked = false;
        isUpdateingScrollPosition = false;

        float width = scrollView.GetComponent<RectTransform>().rect.width;
        float height = scrollView.GetComponent<RectTransform>().rect.height;

        for (int i = 0; i < tutorialSteps.Count; i++)
        {
            tutorialSteps[i].GetComponent<LayoutElement>().minWidth = width;
            tutorialSteps[i].GetComponent<LayoutElement>().preferredWidth = width;
            tutorialSteps[i].GetComponent<LayoutElement>().minHeight = height;
            tutorialSteps[i].GetComponent<LayoutElement>().preferredHeight = height;
        }

        LoadStep(0);
        scrollView.GetComponent<ScrollRect>().horizontalNormalizedPosition = 0;
    }

    public void NextButton()
    {
        if (isLoading) return;

        hasClicked = true;
        if ((currentIndex + 1) >= tutorialSteps.Count)
        {
            EndTutorial();
            return;
        }

        NextPrevious(1);
    }

    public void NextSwipe()
    {
        //print("NextSwipe");
        hasClicked = true;
        NextPrevious(1);
    }

    public void PreviousSwipe()
    {
        hasClicked = true;
        NextPrevious(-1);
    }

    public void NextPrevious(int direction)
    {

        if (isLoading) return;

        if ((currentIndex + direction) < 0)
        {
            return;
        }

        if ((currentIndex + direction) >= tutorialSteps.Count)
        {

            //EndTutorial();
            return;
        }

        if (isLoading) return;
        isLoading = true;
        StartCoroutine(NextPreviousCoroutine(direction));
    }

    public void FirstStepBack(string id)
    {

        switch (id)
        {
            case "dashboard":
                MenuController.instance.OpenTouren();
                break;
        }
    }

    public IEnumerator NextPreviousCoroutine(int direction)
    {

        //print("NextPreviousCoroutine " + direction);

        currentIndex += direction;
        LoadStep(currentIndex);

        StopCoroutine("HideAnimationsCoroutine");
        yield return StartCoroutine("HideAnimationsCoroutine");
        yield return new WaitForEndOfFrame();

        isLoading = false;
    }

    public IEnumerator HideAnimationsCoroutine()
    {
        yield return new WaitForSeconds(0.2f);

        if (currentIndex < tutorialSteps.Count && currentIndex >= 0)
        {
            for (int i = 0; i < tutorialSteps.Count; i++)
            {
                if (i == currentIndex) continue;
                if (tutorialSteps[i].GetComponentInChildren<Animator>(true) != null)
                {
                    if (i == 0)
                    {
                        tutorialSteps[i].GetComponentInChildren<Animator>(true).Play("tutorial1-stations", 0, 0);
                        yield return new WaitForEndOfFrame();
                    }
                    tutorialSteps[i].GetComponentInChildren<Animator>(true).gameObject.SetActive(false);
                }
            }
        }
    }

    public void Skip()
    {
        EndTutorial();
    }

    public void LoadStep(int index)
    {
        //print("LoadStep " + index);
        currentIndex = index;

        if (index < tutorialSteps.Count && index >= 0)
        {
            /*
            for (int i = 0; i < tutorialSteps.Count; i++)
            {
                if (i == currentIndex) continue;
                if (tutorialSteps[i].GetComponentInChildren<Animator>(true) != null)
                {
                    tutorialSteps[i].GetComponentInChildren<Animator>(true).gameObject.SetActive(false);
                }
            }
            */

            if (tutorialSteps[currentIndex].GetComponentInChildren<Animator>(true) != null)
            {
                tutorialSteps[currentIndex].GetComponentInChildren<Animator>(true).gameObject.SetActive(true);
            }
        }

        // Last step
        if (index == tutorialSteps.Count - 1)
        {
            continueButtonLabel.text = LanguageController.GetTranslation("Los geht´s!");
        }
        else
        {
            continueButtonLabel.text = LanguageController.GetTranslation("Weiter");
        }
    }

    public void Reset()
    {

        currentIndex = 0;
        //commitButton.SetActive(false);
    }

    public void EndTutorial()
    {

        tutorialEnded = true;
        PlayerPrefs.SetInt("TutorialShowed", 1);
        if (MenuController.instance != null) { MenuController.instance.OpenTouren(); }
    }

    private void DetectSwipe()
    {
        if (tutorialEnded) return;

        if (Input.GetMouseButtonDown(0) && ValidSwipeArea())
        {
            hitSwipe = true;
            swipeHitPoint = Input.mousePosition;
        }

        bool swiped = false;
        if (Input.GetMouseButtonUp(0) && hitSwipe)
        {

            hitSwipe = false;

            if (ValidSwipeArea())
            {

                Vector3 dist = swipeHitPoint - Input.mousePosition;

                if (dist.x > Screen.width * minSwipeWidthPercentage)
                {
                    //NextPrevious(1);
                    NextSwipe();
                    swiped = true;
                }
                else if (dist.x < -Screen.width * minSwipeWidthPercentage)
                {
                    //NextPrevious(-1);
                    PreviousSwipe();
                    swiped = true;
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && !swiped)
        {
            //NextPrevious(1);
        }
    }

    private bool ValidSwipeArea()
    {

#if !UNITY_EDITOR
		if( Input.touchCount > 0 && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject( Input.touches[0].fingerId ) )				
#else
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
#endif

        {
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, raycastResults);

            if (raycastResults.Count > 0)
            {
                foreach (var go in raycastResults)
                {
                    if (swipeAreaExcludedObjects.Contains(go.gameObject.name))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
