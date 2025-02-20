using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HearGameController : MonoBehaviour
{
    public bool withCamera = false;

    public GameObject tutorialContent;
    public GameObject gameContent;
    public GameObject resultContent;

    private bool isLoading = false;

    public static HearGameController instance;
    void Awake()
    {
        instance = this;
    }

    public void Init()
    {
        tutorialContent.SetActive(true);
        gameContent.SetActive(false);
        resultContent.SetActive(false);
    }

    public void StartGame()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine("StartGameCoroutine");
    }

    public IEnumerator StartGameCoroutine()
    {
        yield return null;

        if (withCamera)
        {
            if (!ARController.instance.arSession.enabled)
            {
                InfoController.instance.loadingCircle.SetActive(true);
                ARController.instance.arPlaneManager.enabled = false;
                ARController.instance.InitARFoundation();
                yield return new WaitForSeconds(0.5f);
                InfoController.instance.loadingCircle.SetActive(false);
            }

            /*
            if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(true); }
            yield return new WaitForSeconds(0.25f);
            yield return StartCoroutine(WebcamController.instance.StartWebcamTextureCoroutine());
            yield return new WaitForSeconds(0.25f);
            if (InfoController.instance != null) { InfoController.instance.loadingCircle.SetActive(false); }
            */

        }

        tutorialContent.SetActive(false);
        gameContent.SetActive(true);

        isLoading = false;
    }

    public void EndGame()
    {
        tutorialContent.SetActive(false);
        gameContent.SetActive(false);
        resultContent.SetActive(true);
    }

    public void Back()
    {
        if (tutorialContent.activeInHierarchy)
        {
            InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);
        }
        else
        {
            tutorialContent.SetActive(true);
            gameContent.SetActive(false);
            resultContent.SetActive(false);
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
        Init();

        isLoading = false;
    }

    public void Repeat()
    {
        //Reset();
        //Init();
        //StartGame();

        tutorialContent.SetActive(false);
        gameContent.SetActive(true);
        resultContent.SetActive(false);
    }

    public void Reset()
    {
        isLoading = false;
        tutorialContent.SetActive(true);
        gameContent.SetActive(false);
        resultContent.SetActive(false);
    }
}
