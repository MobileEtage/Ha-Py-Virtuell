using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.AI.Navigation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using SimpleJSON;
using TMPro;
using MPUIKIT;
using LeTai.TrueShadow;

public class QuizController : MonoBehaviour
{		
	public string gameId = "quiz0";
	public GameObject eventSystem;
    public GameObject tutorialContent;
    public GameObject quizContent;
    public GameObject resultContent;
    public GameObject finishedContent;
    public Image tutorialImage;

    [Space(10)]

	public List<GameObject> answerButtons = new List<GameObject>();
	public List<TextMeshProUGUI> answerLabels = new List<TextMeshProUGUI>();
	public Image checkButtonImage;
	
	[Space(10)]

    public TextMeshProUGUI tutorialTitle;
    public TextMeshProUGUI tutorialDescription;
    public TextMeshProUGUI roundLabel;
    public TextMeshProUGUI questionLabel;
    public TextMeshProUGUI resultContinueButtonLabel;
    public TextMeshProUGUI resultTitleLabel;
    public TextMeshProUGUI resultDescriptionLabel;
    public TextMeshProUGUI finishedDescriptionLabel;
    public GameObject resultImageSuccess;
	public GameObject resultImageFailed;
	
	private JSONNode dataJson;
	private JSONNode quizData;
	private bool isLoading = false;

	private int points = 0;
	private int currentRound = 0;
	private int selectedAnswer = -1;
		
	public static QuizController instance;
	void Awake(){
		instance = this;
	}

	void Start(){

		if( TestController.instance == null ){
			
			eventSystem.SetActive(true);
            Init("quiz1");
		}
	}
	
	void Update()
	{

	}
    
	public void Init(string gameId)
    {
        print("Init Quiz " + gameId);

        dataJson = ServerBackendController.instance.GetJson("_quiz");
        print(dataJson.ToString());

        this.gameId = gameId;
        quizData = GetQuizData(dataJson, gameId);
        print(quizData.ToString());

        roundLabel.text = "";
        LoadIntroSite();
        tutorialContent.SetActive(true);
    }

    public JSONNode GetQuizData(JSONNode dataJson, string gameId)
    {
        if (dataJson[gameId] != null) { return dataJson[gameId]; }

        for (int i = 0; i < dataJson.Count; i++)
        {
            if (dataJson[i]["id"].Value == gameId) { return dataJson[i]; }
        }
        return null;
    }

    public void LoadIntroSite()
    {
        if (quizData["tutorialTitle"] != null) { tutorialTitle.text = LanguageController.GetTranslationFromNode(quizData["tutorialTitle"]); }
        else { tutorialTitle.text = LanguageController.GetTranslation("Wie viel weisst Du?"); }

        if (quizData["tutorialDescription"] != null) { tutorialDescription.text = LanguageController.GetTranslationFromNode(quizData["tutorialDescription"]); }
        else { tutorialDescription.text = LanguageController.GetTranslation("Teste Dein Wissen und beantworte verschiedene Fragen."); }

        // Image
        tutorialImage.transform.parent.gameObject.SetActive(true);
        if (quizData["imageURL"] != null && quizData["imageURL"].Value != "")
        {
            ToolsController.instance.ApplyOnlineImage(tutorialImage, quizData["imageURL"].Value, true);
        }
        else if (quizData["image"] != null && quizData["image"].Value != "")
        {
            Sprite sprite = Resources.Load<Sprite>(quizData["image"].Value);
            tutorialImage.sprite = sprite;
            tutorialImage.preserveAspect = true;
        }
        else
        {
            //tutorialImage.transform.parent.gameObject.SetActive(false);

            Sprite sprite = Resources.Load<Sprite>("UI/Sprites/quiz");
            tutorialImage.sprite = sprite;
            tutorialImage.preserveAspect = true;
        }
    }

    public void StartGame()
    {
        LoadRound();
        tutorialContent.SetActive(false);
        quizContent.SetActive(true);
    }

    public void Back()
    {
        if (tutorialContent.activeInHierarchy)
        {
            InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);
        }
        else
        {
            InfoController.instance.ShowCommitAbortDialog("Willst Du das Quiz wirklich beenden?", CommitBack);
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
        Init(this.gameId);

        isLoading = false;
    }

    public void LoadRound()
    {
        JSONNode dataNode = quizData["questions"][currentRound];

        // Set labels
        roundLabel.text = "<b>" + LanguageController.GetTranslation("Runde") + " " + (currentRound + 1) + "</b> / " + quizData["questions"].Count;
        questionLabel.text = LanguageController.GetTranslationFromNode(dataNode["question"]);

        for (int i = 0; i < answerButtons.Count; i++)
        {
            answerButtons[i].SetActive(false);
        }

        for (int i = 0; i < answerLabels.Count && i < dataNode["answers"].Count; i++)
        {
            answerButtons[i].SetActive(true);

            answerLabels[i].color = Params.GetColorFromHexString("#4892A9");
            answerLabels[i].text = LanguageController.GetTranslationFromNode(dataNode["answers"][i]);
            answerLabels[i].transform.parent.GetComponent<MPImage>().StrokeWidth = 6;

            Button rootButton = answerLabels[i].GetComponentInParent<Button>(true);
            if (rootButton) rootButton.transform.SetSiblingIndex(Random.Range(0, 4));
        }

        selectedAnswer = -1;
        checkButtonImage.color = Params.GetColorFromHexString("#D3D3D3");
    }

    public void NextRound(){
		
		if( currentRound+1 == quizData["questions"].Count ){

            //CommitBack();

            string text = "Du hast <score> von <points> Punkten erspielt!";
            text = text.Replace("<score>", points.ToString());
            text = text.Replace("<points>", (currentRound+1).ToString());
            finishedDescriptionLabel.text = LanguageController.GetTranslation(text);

            finishedContent.SetActive(true);
            resultContent.SetActive(false);

            int savedPoints = PlayerPrefs.GetInt("Quiz_" + TourController.instance.GetCurrentTourId() + "_" + gameId + "_Points", 0);
            if (points > savedPoints) { PlayerPrefs.SetInt("Quiz_" + TourController.instance.GetCurrentTourId() + "_" + gameId + "_Points", points); }
        }
		else{
			
			currentRound++;
			LoadRound();
            quizContent.SetActive(true);
            resultContent.SetActive(false);
        }
    }
	
	public void SelectAnswer( int index ){
		
		selectedAnswer = index;
		
		for( int i = 0; i < answerLabels.Count; i++ ){
			answerLabels[i].color = Params.GetColorFromHexString("#4892A9");
			answerLabels[i].transform.parent.GetComponent<MPImage>().StrokeWidth = 6;
		}
		answerLabels[index].color = Params.GetColorFromHexString("#FFFFFF");
		answerLabels[index].transform.parent.GetComponent<MPImage>().StrokeWidth = 0;

		checkButtonImage.color = Params.GetColorFromHexString("#6CB931FF");
	}
	
	public void CheckAnswer(){
		
		if( selectedAnswer < 0 ) return;

        JSONNode dataNode = quizData["questions"][currentRound];

        if ( selectedAnswer == 0 ){
			
			// Correct
			resultImageSuccess.SetActive(true);
			resultImageFailed.SetActive(false);
			resultTitleLabel.text = LanguageController.GetTranslation("Ja, korrekt!");
			points += 1;
		}
		else{
			
			// Wrong
			resultImageSuccess.SetActive(false);
			resultImageFailed.SetActive(true);
			resultTitleLabel.text = LanguageController.GetTranslation("Nein, leider nicht korrekt.");
		}
		
		resultDescriptionLabel.text = LanguageController.GetTranslationFromNode(dataNode["description"]);
			
		if( currentRound+1 == quizData["questions"].Count ){
            resultContinueButtonLabel.text = LanguageController.GetTranslation("Weiter");
		}
		else{
			resultContinueButtonLabel.text = LanguageController.GetTranslation("Zur nächsten Runde");
		}

        resultContent.SetActive(true);
        quizContent.SetActive(false);
    }

    public void Repeat()
    {
        Reset();
        Init(this.gameId);
        StartGame();
    }

    public void OpenChallenge()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenChallengeCoroutine());
    }

    public IEnumerator OpenChallengeCoroutine()
    {
        if (ChallengeController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("ChallengeSite")); }
        ChallengeController.instance.Init();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ChallengeSite"));

        isLoading = false;
    }

    public void Reset()
    {
        isLoading = false;
        currentRound = 0;
        points = 0;
        selectedAnswer = -1;
        tutorialContent.SetActive(false);
        quizContent.SetActive(false);
        resultContent.SetActive(false);
        finishedContent.SetActive(false);
    }
}
