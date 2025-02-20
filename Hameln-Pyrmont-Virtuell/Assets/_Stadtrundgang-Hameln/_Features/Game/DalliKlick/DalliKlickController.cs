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
using Unity.VisualScripting;
using JetBrains.Annotations;

public class DalliKlickController : MonoBehaviour
{		
	public string gameId = "dalliKlick0";
	public GameObject eventSystem;

    [Space(10)]

    public Image tutorialImage;
    public TextMeshProUGUI tutorialTitle;
    public TextMeshProUGUI tutorialDescription;

    [Space(10)]

    public GameObject gameTutorialContent;
    public GameObject gameTutorial1;
    public GameObject gameTutorial2;
    public GameObject stepPoint;

    [Space(10)]

    public GameObject tutorialContent;
    public GameObject gameContent;
    public GameObject resultContent;
    public GameObject finalResultContent;
    public TextMeshProUGUI resultLabel;
    public TextMeshProUGUI infoLabel;
    public TextMeshProUGUI timerLabel;

	public GameObject answersScrollView;
	public List<MPImage> lines = new List<MPImage>();
	public List<TextMeshProUGUI> answerLabels = new List<TextMeshProUGUI>();
	public GameObject maskImage;
	public TextMeshProUGUI currentPointsLabel;
	public TextMeshProUGUI roundLabel;
	public TextMeshProUGUI turnsLabel;
	public TextMeshProUGUI questionLabel;
	public Image checkButtonImage;
	public Image quizImage;
	
	[Space(10)]

    public TextMeshProUGUI resultRoundLabel;
	public TextMeshProUGUI resultTitleLabel;
	public TextMeshProUGUI resultPointsLabel;
	public TextMeshProUGUI resultSubTitleLabel;
	public TextMeshProUGUI resultDescriptionLabel;
	public TextMeshProUGUI resultContinueButtonLabel;
	public Image resultImage;
	public GameObject openInfo;

    [Space(10)]

    public TextMeshProUGUI tutorialDescriptionLabel1;
    public TextMeshProUGUI tutorialTitleLabel2;
    public TextMeshProUGUI tutorialDescriptionLabel2;
    public List<GameObject> puzzleButtons = new List<GameObject>();

    private JSONNode dataJson;
	private JSONNode dataNode;
	Dictionary<string, string[]> gamesDictionary = new Dictionary<string, string[]>();
	private bool isLoading = false;

	private List<int> openPuzzles = new List<int>();
	private int turns = 0;
	private int points = 0;
	private int pointsCurrentTurn = 9;
	private int currentRound = 0;
	private int selectedAnswer = -1;
    private float openInfoTimer = 5.0f;
    private float gameTimer = 0.0f;
    private bool gameTimerActive = false;
    private bool openInfoShowed = false;

    public static DalliKlickController instance;
	void Awake(){
		instance = this;
	}

	void Start(){
		
		gamesDictionary.Add("dalliKlick0", new string[]{"Buchfink", "Eichelhaeher"});

        if ( TestController.instance == null ){
			
			eventSystem.SetActive(true);
			Init("dalliKlick0");
			//StartGame();
		}
	}
	
	void Update()
	{
        if(SiteController.instance != null && SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "DalliKlickSite") { return; }

		if( openInfo.activeInHierarchy ){
			
			if( Input.GetMouseButtonDown(0) || openInfoTimer <= 0 ){
				openInfo.SetActive(false);
			}else{
				openInfoTimer -= Time.deltaTime;
			}
		}

        UpdateGameTutorialStepPosition();

        if (gameTimerActive)
        {
            if (InfoController.instance != null && InfoController.instance.commitAbortDialog.activeInHierarchy) return;
            gameTimer += Time.deltaTime;
            timerLabel.text = LanguageController.GetTranslation("Zeit") + " " + gameTimer.ToString("F0");
        }
    }

    public void Init(JSONNode featureData, JSONNode gameIdsNode)
	{
		if(featureData == null || gameIdsNode == null)
		{
            InfoController.instance.ShowMessage("Es konnten keine Informationen zu dieser Station abgerufen werden. Versuche es später erneut.");
			return;
        }

        LoadIntroSite(featureData);

        string[] gameIds = new string[gameIdsNode.Count];
		for (int i = 0; i < gameIdsNode.Count; i++){ gameIds[i] = gameIdsNode[i].Value; }
		gamesDictionary["dalliKlick0"] = gameIds;
		Init("dalliKlick0");

        openInfoShowed = false;
        int dalliKlickTimeIndex = PlayerPrefs.GetInt("DalliKlickTimeId", 0);
        if (dalliKlickTimeIndex != 3)
        {
            for (int i = 0; i < puzzleButtons.Count; i++) { puzzleButtons[i].GetComponent<Button>().enabled = false; }
            timerLabel.transform.parent.gameObject.SetActive(true);
            tutorialDescriptionLabel1.text = LanguageController.GetTranslation("Errate, welches Motiv sich hinter\nden Puzzleteilen versteckt.\nSie werden automatisch\nnach und nach aufgedeckt.");
            tutorialTitleLabel2.text = LanguageController.GetTranslation("Sei schnell!");
            tutorialDescriptionLabel2.text = LanguageController.GetTranslation("Entscheide Dich zügig!\nBrauchst Du alle acht Puzzleteile,\num das Bild zu erkennen,\noder benötigst Du weniger?");
        }
        else
        {
            for (int i = 0; i < puzzleButtons.Count; i++) { puzzleButtons[i].GetComponent<Button>().enabled = true; }
            timerLabel.transform.parent.gameObject.SetActive(false);
            tutorialDescriptionLabel1.text = LanguageController.GetTranslation("Errate, welches Motiv sich hinter\nden Puzzleteilen versteckt.\nDu kannst ein Puzzleteil durch Klicken aufdecken.");
            tutorialTitleLabel2.text = LanguageController.GetTranslation("Überlege gut!");
            tutorialDescriptionLabel2.text = LanguageController.GetTranslation("Brauchst Du alle acht Puzzleteile,\num das Bild zu erkennen,\noder benötigst Du weniger?");
        }
    }

    public void LoadIntroSite(JSONNode featureData)
    {
        if (featureData["infoTitle"] != null) { tutorialTitle.text = LanguageController.GetTranslationFromNode(featureData["infoTitle"]); }
        else { tutorialTitle.text = LanguageController.GetTranslation("Bilderrätsel"); }

        if (featureData["infoDescription"] != null) { tutorialDescription.text = LanguageController.GetTranslationFromNode(featureData["infoDescription"]); }
        else {

            //tutorialDescription.text = LanguageController.GetTranslation("Errate, welches Motiv sich hinter\nden Puzzleteilen versteckt.\nSie werden automatisch\nnach und nach aufgedeckt.");
            tutorialDescription.text = "";
        }
        tutorialDescription.text = LanguageController.GetTranslation("Errate, welches Motiv sich hinter\nden Puzzleteilen versteckt.");

        // Image
        tutorialImage.transform.parent.gameObject.SetActive(true);
        if (featureData["imageURL"] != null && featureData["imageURL"].Value != "")
        {
            ToolsController.instance.ApplyOnlineImage(tutorialImage, featureData["imageURL"].Value, true);
        }
        else if (featureData["image"] != null && featureData["image"].Value != "")
        {
            Sprite sprite = Resources.Load<Sprite>(featureData["image"].Value);
            tutorialImage.sprite = sprite;
            tutorialImage.preserveAspect = true;
        }
        else
        {
            //tutorialImage.transform.parent.gameObject.SetActive(false);

            Sprite sprite = Resources.Load<Sprite>("UI/Sprites/puzzle");
            tutorialImage.sprite = sprite;
            tutorialImage.preserveAspect = true;
        }
    }

    public void Init(string gameId){

		if (ServerBackendController.instance != null) { dataJson = ServerBackendController.instance.GetJson("_dalliKlick"); }
		else { dataJson = JSONNode.Parse(Resources.Load<TextAsset>("_dalliKlick").text); }
        print(dataJson.ToString());

        this.gameId = gameId;
        tutorialContent.SetActive(true);
        gameTutorialContent.SetActive(true);
        gameTutorial1.SetActive(true);
        gameTutorial2.SetActive(false);
        stepPoint.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, 0);
        timerLabel.text = "";
        infoLabel.text = LanguageController.GetTranslation("Schaue\nhinter die Puzzleteile");
    }

    public void GameTutorialBack()
    {
        if (gameTutorial1.activeInHierarchy)
        {
            tutorialContent.SetActive(true);
            gameContent.SetActive(false);
        }
        else if (gameTutorial2.activeInHierarchy)
        {
            gameTutorial1.SetActive(true);
            gameTutorial2.SetActive(false);
        }
    }

    public void UpdateGameTutorialStepPosition()
    {
        int currentStep = 0;
        if (gameTutorial2.activeInHierarchy) currentStep = 1;
        float targetPosition = Mathf.Lerp(stepPoint.GetComponent<RectTransform>().anchoredPosition.x, 30+(currentStep*60), Time.deltaTime*10.0f);
        stepPoint.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPosition, 0);
    }

    public void OnGameTutorialFinished()
    {
        gameTutorialContent.SetActive(false);
        StartTimer();
    }
	
    public void StartTimer()
    {
        gameTimer = 0;
        gameTimerActive = true;

        int dalliKlickTimeIndex = PlayerPrefs.GetInt("DalliKlickTimeId", 0);
        if (dalliKlickTimeIndex == 3)
        {
            if (!openInfoShowed)
            {
                openInfoShowed = true;
                openInfoTimer = 5.0f;
                openInfo.SetActive(true);
            }
        }
        else
        {
            StopCoroutine("UpdateGameCoroutine");
            StartCoroutine("UpdateGameCoroutine");
        }
    }

    public IEnumerator UpdateGameCoroutine()
    {
        float timer = 3.0f;
        int dalliKlickTimeIndex = PlayerPrefs.GetInt("DalliKlickTimeId", 0);
        if (dalliKlickTimeIndex == 1) { timer = 5; }
        if (dalliKlickTimeIndex == 2) { timer = 10; }

        if (openPuzzles.Count <= 0) { timer = 0.5f; }
        while (timer > 0)
        {
            if (InfoController.instance != null) { if (!InfoController.instance.commitAbortDialog.activeInHierarchy) { timer -= Time.deltaTime; } }
            else { timer -= Time.deltaTime; }        
            yield return null;
        }

        List<int> availableIds = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };
        for (int i = 0; i < openPuzzles.Count; i++) { if (availableIds.Contains(openPuzzles[i])) { availableIds.Remove(openPuzzles[i]); } }
        OpenPuzzle( availableIds[Random.Range(0, availableIds.Count)]);

        if (availableIds.Count > 1) { StartCoroutine("UpdateGameCoroutine"); }
    }

    public void StartGame()
    {
        currentPointsLabel.text = LanguageController.GetTranslation("Punkte") + " <b>0</b>";
        ShuffleQuizImages();
        LoadRound();

        openInfoTimer = 5.0f;
        //openInfo.SetActive(true);
        tutorialContent.SetActive(false);
        gameContent.SetActive(true);
    }

    public void ShuffleQuizImages(){
		
		foreach( KeyValuePair<string, string[]> pair in gamesDictionary ){
			for( int i = 0; i<pair.Value.Length; i++ ){
				string val = pair.Value[i];
				int index = Random.Range(0, pair.Value.Length);
				pair.Value[i] = pair.Value[index];
				pair.Value[index] = val;
			}
		}
	}
	
	public void NextRound(){
		
		if( currentRound+1 == gamesDictionary[gameId].Length ){

            string text = "Du hast <score> von <points> Punkten erspielt!";
            text = text.Replace("<score>", points.ToString());
            text = text.Replace("<points>", (9*gamesDictionary[gameId].Length).ToString());
            resultLabel.text = LanguageController.GetTranslation(text);
            gameContent.SetActive(false);
            finalResultContent.SetActive(true);

            int savedPoints = PlayerPrefs.GetInt("DalliKlick_" + TourController.instance.GetCurrentTourId() + "_" + gameId + "_Points", 0);
            if (points > savedPoints) { PlayerPrefs.SetInt("DalliKlick_" + TourController.instance.GetCurrentTourId() + "_" + gameId + "_Points", points); }
        }
		else{
			
			currentRound++;
			LoadRound();
            StartTimer();
        }

        resultContent.SetActive(false);
    }

    public void LoadRound(){
				
		// Get infos from json
		dataNode = GetDataNode( gamesDictionary[gameId][currentRound] );
		if(dataNode == null) {

			print("Could not find id " + gamesDictionary[gameId][currentRound]);
			return;
		}

        // Set labels
        roundLabel.text = "<b>" + LanguageController.GetTranslation("Runde") + " " + (currentRound+1) + "</b>/" + gamesDictionary[gameId].Length;
		questionLabel.text = LanguageController.GetTranslationFromNode(dataNode["question"]);

        for (int i = 0; i < answerLabels.Count; i++) { answerLabels[i].GetComponentInParent<Button>(true).gameObject.SetActive(false); }
        for ( int i = 0; i < answerLabels.Count && i < dataNode["answers"].Count; i++ ){

            answerLabels[i].GetComponentInParent<Button>(true).gameObject.SetActive(true);
            answerLabels[i].color = Params.GetColorFromHexString("#4892A9");
			answerLabels[i].text = LanguageController.GetTranslationFromNode(dataNode["answers"][i]["name"]);
			answerLabels[i].transform.parent.GetComponent<MPImage>().StrokeWidth = 6;
			
			Button rootButton = answerLabels[i].GetComponentInParent<Button>();
			if(rootButton) rootButton.transform.SetSiblingIndex(Random.Range(0,4));
		}

		selectedAnswer = -1;
		checkButtonImage.color = Params.GetColorFromHexString("#DDDDDD");

        if (dataNode["imageURL"] != null && dataNode["imageURL"].Value != "")
        {
            Sprite sprite = Resources.Load<Sprite>("dummy");
            resultImage.sprite = sprite;
            ToolsController.instance.ApplyOnlineImage(quizImage, dataNode["imageURL"].Value, false);
        }
        else if (dataNode["image"] != null && dataNode["image"].Value != "")
        {
            Sprite sprite = Resources.Load<Sprite>(dataNode["image"].Value);
            quizImage.sprite = sprite;
        }
		else
		{
            if (dataNode["imagesPath"] != null && dataNode["imagesPath"].Count > 0)
            {
                Sprite sprite = Resources.Load<Sprite>(dataNode["imagesPath"][0].Value);
                quizImage.sprite = sprite;
                if (sprite == null){ print("Could not find image " + dataNode["imagesPath"][0].Value); }
            }
        }

		pointsCurrentTurn = 9;
		turns = 0;
		turnsLabel.text = LanguageController.GetTranslation("Züge") + " <b>0</b>";

		openPuzzles.Clear();
		HideImage();

		float width = answersScrollView.GetComponent<RectTransform>().rect.width - 140;	// Padding 70	
		if ( width > 0 )
		{
			Vector2 cellSize = answerLabels[0].GetComponentInParent<Beardy.GridLayoutGroup>( true ).cellSize;
			cellSize.x = (width - 30) / 2.0f;
			cellSize.x = Mathf.Clamp(cellSize.x, 450, 800);
			answerLabels[0].GetComponentInParent<Beardy.GridLayoutGroup>( true ).cellSize = cellSize;
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

        gameTimerActive = false;
        StopCoroutine("UpdateGameCoroutine");
        StartCoroutine(CheckAnswerCoroutine());
	}
	
    public IEnumerator CheckAnswerCoroutine()
    {
        if(InfoController.instance != null) { InfoController.instance.blocker.SetActive(true); }
        yield return new WaitForSeconds(0.5f);
        CheckAnswer();
        if (InfoController.instance != null) { InfoController.instance.blocker.SetActive(false); }
    }

	public void CheckAnswer(){
		
		if( selectedAnswer < 0 ) return;
		
		if( selectedAnswer == 0 ){
			
			// Correct
			resultTitleLabel.text = LanguageController.GetTranslation("Ja, korrekt!");
			resultPointsLabel.text = "<color=#6EC561><b>+" + pointsCurrentTurn + "</b> " + LanguageController.GetTranslation("Punkte") + "</color>";
			points += pointsCurrentTurn;
		}
		else{
			
			// Wrong
			resultTitleLabel.text = LanguageController.GetTranslation("Leider Falsch");
			resultPointsLabel.text = "<color=#D3D3D3><b>0</b> " + LanguageController.GetTranslation("Punkte") + "</color>";
            points -= 0;
		}
		
        // Round
		resultRoundLabel.text = "<b>" + LanguageController.GetTranslation("Runde") + " " + (currentRound+1) + "</b>/" + gamesDictionary[gameId].Length;

        // Title
		//resultSubTitleLabel.text = LanguageController.GetTranslation("Hinter den Puzzleteilen ist") + " " + LanguageController.GetTranslation(dataNode["article"].Value) + " " + LanguageController.GetTranslationFromNode( dataNode["title"] );

        if (dataNode["title"] != null){ resultSubTitleLabel.text = LanguageController.GetTranslationFromNode(dataNode["resultTitle"]); }
        else{ resultSubTitleLabel.text = ""; }

        // Description
        resultDescriptionLabel.text = LanguageController.GetTranslationFromNode(dataNode["description"]);

        
        if (dataNode["imageSolutionURL"] != null && dataNode["imageSolutionURL"].Value != "")
        {
            Sprite sprite = Resources.Load<Sprite>("dummy");
            resultImage.sprite = sprite;
            ToolsController.instance.ApplyOnlineImage(resultImage, dataNode["imageSolutionURL"].Value, true);
        }
        else if (dataNode["image"] != null && dataNode["image"].Value != "")
        {
            Sprite sprite = Resources.Load<Sprite>(dataNode["image"].Value);
            resultImage.sprite = sprite;
        }
        else
        {
            if (dataNode["imageSolutionPath"] != null)
            {
                Sprite sprite = Resources.Load<Sprite>( dataNode["imageSolutionPath"].Value);
                resultImage.sprite = sprite;
                if (sprite == null){ print("Could not find image " + dataNode["imageSolutionPath"].Value); }
            }
        }


        if ( currentRound+1 == gamesDictionary[gameId].Length ){
			resultContinueButtonLabel.text = LanguageController.GetTranslation("Weiter");
		}
		else{
			resultContinueButtonLabel.text = LanguageController.GetTranslation("Zur nächsten Runde");
		}
		
		currentPointsLabel.text = LanguageController.GetTranslation("Punkte") + " <b>" + points + "</b>";

        resultContent.SetActive(true);
	}
	
	public JSONNode GetDataNode( string id ){

        if (dataJson["images"] != null)
        {
            for (int i = 0; i < dataJson["images"].Count; i++)
            {
                if (dataJson["images"][i]["id"].Value == id) return dataJson["images"][i];
            }
        }
        else
        {
            for (int i = 0; i < dataJson.Count; i++)
            {
                if (dataJson[i]["id"].Value == id) return dataJson[i];
            }
        }
		return null;
	}
	
	public void HideImage(){
		
		for( int i = 0; i < lines.Count; i++ ){ lines[i].color = new Color(1,1,1,1); }
		for( int i = 1; i < 9; i++ ){ maskImage.GetComponent<Image>().material.SetFloat( "_MaskTex" + i + "Factor", 1 ); }
	}
	
	public void OpenPuzzle( int id ){
		
		if( isLoading ) return;
        infoLabel.text = "";

        if ( !openPuzzles.Contains(id) ){
			
			isLoading = true;
			StartCoroutine( OpenPuzzleCoroutine(id) );
		}
	}
	
	public IEnumerator OpenPuzzleCoroutine( int id ){
			
		turns++;
		turnsLabel.text = LanguageController.GetTranslation("Züge") + " <b>" + turns + "</b>";
		pointsCurrentTurn--;
		openPuzzles.Add(id);
		
		float maskFactor = 1;
		float time = 0.5f;
		float timer = 0.5f;
		while(time > 0){
			
			maskFactor = time/timer;
			maskImage.GetComponent<Image>().material.SetFloat( "_MaskTex" + id + "Factor", maskFactor );
			FadeOutLine(id, maskFactor);
			
			time -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

        FadeOutLine(id, 0);
        maskImage.GetComponent<Image>().material.SetFloat( "_MaskTex" + id + "Factor", 0 );
		
		isLoading = false;
	}
	
	public void FadeOutLine( int id, float maskFactor ){
		
		int prevID = (id-1)<=0 ? 8:(id-1);
		int nextID = (id+1)>=9 ? 1:(id+1);
		
		int prevLineID = id-1;
		int nextLineID = id>7 ? 0:id;
		
		if( openPuzzles.Contains(prevID) ){
			lines[prevLineID].color = new Color(1,1,1,maskFactor);
		}
		if( openPuzzles.Contains(nextID) ){
			lines[nextLineID].color = new Color(1,1,1,maskFactor);
		}
	}

    public void Back()
    {
        if (!tutorialContent.activeInHierarchy)
        {
            InfoController.instance.ShowCommitAbortDialog("Möchtest Du das Spiel beenden?", CommitBack);
        }
        else
        {
            //CommitBack();
	        InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);
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

    public void Repeat()
    {
        Reset();
        Init(this.gameId);
        StartGame();

        gameTutorialContent.SetActive(false);
        StartTimer();
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
        StopCoroutine("UpdateGameCoroutine");

        isLoading = false;
        gameTimerActive = false;
        gameTimer = 0;
        currentRound = 0;
        points = 0;
        turns = 0;
        pointsCurrentTurn = 9;
        selectedAnswer = -1;
        openPuzzles.Clear();
        tutorialContent.SetActive(true);
        resultContent.SetActive(false);
        finalResultContent.SetActive(false);
        gameContent.SetActive(false);
    }
}
