using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using MPUIKIT;
using TMPro;

public class HighscoreController : MonoBehaviour
{
	private bool resetHighscoreOnUnistall = true;
	private bool editorTestEnabled = false;
	private int minSearchPrefixLength = 1;
	
	[Space(10)]

	public int userToShow = 10;
	public string highscoreId = "AppNameHighscore";
	public string highscoreName = "AppName Bestenliste";
	public int maxEntryCount = 0;
	
	[Space(10)]
	
	public string userID = "";
	public string userName = "";
	public int score = 0;

	[Space(10)]

	public Transform userHolder;
	public TMP_InputField searchInputField;
	
	private List<GameObject> listElements = new List<GameObject>();
	
	//private const string GET_HIGHSCORE_URL = "https://highscore-stage.die-etagen.de/api/highscore/list";
	//private const string SAVE_HIGHSCORE_URL = "https://highscore-stage.die-etagen.de/api/highscore/entry";
	private const string GET_HIGHSCORE_URL = "https://drupal-highscore.badiburg-ar.de/api/highscore/list";
	private const string SAVE_HIGHSCORE_URL = "https://drupal-highscore.badiburg-ar.de/api/highscore/entry";
	
	List<string> stationIds = new List<string>()
	{
		
	};
	
	private JSONNode dataJson;
	private bool isLoading = false;
	
	public static HighscoreController instance;
	void Awake()
    {
	    instance = this;
	    
	    #if UNITY_EDITOR
	    
	    userID = "097ed588-3b02-48df-832b-05ef40401d1c";
	    //userID = "0CEF88BD-08CB-43ED-9E0A-EDE03ECB99A6"; // iPad mini 4
	    
	    PlayerPrefs.SetString("userID", userID);
	    #endif
    }

	void Start(){
		
		//Init();
	}
	
	/*
    void Update()
	{
		if( !editorTestEnabled ) return;
		
        #if UNITY_EDITOR
	    if( Input.GetKeyDown(KeyCode.H) ){
	    	CreateHighscoreList(highscoreId);
	    }
	    if( Input.GetKeyDown(KeyCode.S) ){
	    	SaveHighscore();
	    }
	    if( Input.GetKeyDown(KeyCode.G) ){
	    	StartCoroutine( GetHighscoreCoroutine() );
	    }
        #endif
	}
	*/
    
	public void Init(){
		
		dataJson = JSONNode.Parse( Resources.Load<TextAsset>("_highscores").text );		

		RetrieveUserID();
		
		if( PlayerPrefs.HasKey("userName") ){			
			userName = PlayerPrefs.GetString("userName");
		}
		
		print( "userID: " + userID );
		print( "userName: " + userName );
		
		TextAsset textAsset = Resources.Load<TextAsset>("stations");
		if( textAsset != null ){
				
			stationIds.Clear();
			JSONNode stationsJson = JSONNode.Parse(textAsset.text);
			stationsJson = stationsJson["data"];
			for( int i = 0; i < stationsJson["stations"].Count; i++ ){
				stationIds.Add(stationsJson["stations"][i]["stationId"].Value);
			}
		}
	}
	
	public void RetrieveUserID(){
				
		bool useUUID = resetHighscoreOnUnistall;
		bool useDeviceUniqueIdentifier = true;
		bool useKeychain = false;
	
		print("Device credentials");
		#if UNITY_IOS
		print("iOS advertisingTrackingEnabled " + UnityEngine.iOS.Device.advertisingTrackingEnabled);
		print("iOS advertisingIdentifier " + UnityEngine.iOS.Device.advertisingIdentifier);
		print("iOS vendorIdentifier " + UnityEngine.iOS.Device.vendorIdentifier);
		#endif
		
		print("Android/iOS deviceUniqueIdentifier " + SystemInfo.deviceUniqueIdentifier);


		if( PlayerPrefs.HasKey("userID") ){			
			userID = PlayerPrefs.GetString("userID");
		}
		else{
			
			if( useUUID ){
				
				// Not persistent if uninstalling app
				userID = Guid.NewGuid().ToString();
				PlayerPrefs.SetString("userID", userID);
			}
			else if( useDeviceUniqueIdentifier ){
				
				// Not persistent if uninstalling app on iOS
				userID = SystemInfo.deviceUniqueIdentifier;
				PlayerPrefs.SetString("userID", userID);
			}
			else if( useKeychain ){

				#if UNITY_IOS
				
				// Save id into keychain which also can be retrieved after uninstalling app ?! 
				// https://github.com/phamtanlong/unity-ios-keychain-plugin
				
				try{
					
					// Get possible existing id from KeyChain
					//userID = KeyChain.BindGetKeyChainUser ();
	
					// Create and save new id if not already existing
					if( string.IsNullOrEmpty(userID) ){
	
						print("No id found in KeyChain, creating new one ...");
							
						userID = Guid.NewGuid().ToString();
						//KeyChain.BindSetKeyChainUser ("0", userID);
					}
					
					PlayerPrefs.SetString("userID", userID);
					
				}catch( Exception e ){
					
					print("Failed saving or retrieving id from KeyChain " + e.Message);
					
					userID = SystemInfo.deviceUniqueIdentifier;
					PlayerPrefs.SetString("userID", userID);
				}
				
				#endif
				
				#if UNITY_ANDROID
				
				userID = SystemInfo.deviceUniqueIdentifier;
				PlayerPrefs.SetString("userID", userID);
				
				#endif
			}
			else{
				
				// deviceUniqueIdentifier should be unique for Android
				#if UNITY_ANDROID
				
				userID = SystemInfo.deviceUniqueIdentifier;
				PlayerPrefs.SetString("userID", userID);
				
				#endif
				
				// vendorIdentifier should be unique for iOS ???
				#if UNITY_IOS
				
				// Maybe always unique ?!
				userID = UnityEngine.iOS.Device.vendorIdentifier;
				
				// Can be 00000000-0000-0000-0000-000000000000 if "Limit Ad Tracking" enabled ?!
				//if( UnityEngine.iOS.Device.advertisingTrackingEnabled ){	// Check this to avoid zero id ?!
				//	userID = UnityEngine.iOS.Device.advertisingIdentifier;
				//}
				
				PlayerPrefs.SetString("userID", userID);
				
				#endif
			}
		}
	}
	
	public void CreateHighscoreList( string highscoreId ){
		
		if( isLoading ) return;
		isLoading = true;
		
		StartCoroutine( CreateHighscoreListCoroutine(highscoreId) );
	}
	
	public IEnumerator CreateHighscoreListCoroutine( string highscoreId ){

		print("CreateHighscoreListCoroutine " + highscoreId);
		
		foreach( Transform child in userHolder ){		
			if( child.GetSiblingIndex() == 0 ) continue;			
			if( child.GetSiblingIndex() == 1 ) continue;			
			if( child.GetSiblingIndex() == 2 ) continue;			
			Destroy(child.gameObject);
		}
		listElements.Clear();
		
		JSONNode highscoreData = JSONNode.Parse("{}");
		bool isSuccess = false;
		yield return StartCoroutine(
			GetHighscoreCoroutine(highscoreId, (bool success, JSONNode json) => {                
			
				isSuccess = success;
				if( success ){
					highscoreData = json;
				}
			})
		);
		
		if( !isSuccess ){
			
			print("GetHighscoreCoroutine failed");
			
		}else{
			
			print("Success GetHighscoreCoroutine" + highscoreData);
			
			bool foundMyUser = false;
			int rank = 0;
			int currentPoints = int.MaxValue;
			JSONNode userData = GetVisibleUserList(highscoreData);
			
			if( userData["users"].Count > 10 ){
				
			}
			
			int userToLoad = userToShow;
			bool foundMe = false;
			bool playerInBetweenExists = false;
			//for( int i = 0; i < userData["users"].Count; i++ ){
			for( int i = 0; i < userData["users"].Count; i++ ){
				
				if( userData["users"][i]["userName"] == null ){					
					continue;
				}else if( userData["users"][i]["userName"].Value == "" ){
					continue;
				}else if( userData["users"][i]["userName"].Value == "null" ){
					continue;
				}
				
				if( userToLoad <= 0 && foundMe ){
					break;
				}
				else if( userToLoad <= 0 && !foundMe ){
					
					if( userData["users"][i]["userID"].Value != userID ){
						
						if( !playerInBetweenExists ){
						
							playerInBetweenExists = true;

							GameObject listElementDummy = 
								ToolsController.instance.InstantiateObject("UI/HighscorePrefab", userHolder);
							
							listElementDummy.GetComponent<HighscoreListElement>().SetUser( 
								"", 
								"...",
								""
							);
							listElements.Add(listElementDummy);
						}				
						continue;
					}
				}
								
				int points = userData["users"][i]["score"].AsInt;
				if( points < currentPoints ){ rank=i+1; currentPoints = points; }
				
				GameObject listElement = 
					ToolsController.instance.InstantiateObject("UI/HighscorePrefab", userHolder);

				listElement.GetComponent<HighscoreListElement>().SetUser( 
					rank.ToString(), 
					userData["users"][i]["userName"].Value,
					points.ToString()
				);
				
				if( userData["users"][i]["userID"].Value == userID ){
					
					foundMe = true;
					
					listElement.transform.GetChild(0).GetComponent<MPImage>().enabled = true;
					listElement.GetComponent<HighscoreListElement>().rankLabel.color = Color.white;
					listElement.GetComponent<HighscoreListElement>().userNameLabel.color = Color.white;
					listElement.GetComponent<HighscoreListElement>().scoreLabel.color = Color.white;
					
					// Hide lines
					listElement.transform.GetChild(1).gameObject.SetActive(false);
					if( listElements.Count > 0 ){
						listElements[listElements.Count-1].transform.GetChild(1).gameObject.SetActive(false);
						foundMyUser = true;
					}
				}
				
				listElements.Add(listElement);
				userToLoad--;
			}
		}
		
		isLoading = false;
	}
	
	public JSONNode GetVisibleUserList( JSONNode highscoreData ){
		
		JSONNode userData = JSONNode.Parse("{\"users\":[]}");
		for( int i = 0; i < highscoreData["data"]["entries"].Count; i++ ){
			userData["users"].Add(highscoreData["data"]["entries"][i]);
		}
		return userData;
	}
	
	public void SaveHighscore(string id, int points){
		
		if( !IsRegisteredForHighscore() ) return;
		
		highscoreId = id + "_Highscore";
		score = points;

		if( isLoading ) return;
		isLoading = true;
		
		StartCoroutine( SaveHighscoreCoroutine() );
	}
	
	public void SaveHighscore(){
		
		if( isLoading ) return;
		isLoading = true;
		
		StartCoroutine( SaveHighscoreCoroutine() );
	}
	
	public IEnumerator SaveHighscoreCoroutine(){

		bool isSuccess = false;
		yield return StartCoroutine(
			SaveHighscoreCoroutine(highscoreId, score, (bool success) => {                
			
				isSuccess = success;
				
				if( !success ){
					print( "Failed save highscore");
				} 	
				else{
					
					print( "Success save highscore");
					
					int oldScore = PlayerPrefs.GetInt(highscoreId);
					if( score > oldScore ){
						
						PlayerPrefs.SetInt( highscoreId, score );
						PlayerPrefs.Save();
						
						print("Score saved for " + highscoreId + ", " + score);
					}
				}
			})
		);
		
		if( isSuccess ){
			
			yield return StartCoroutine(
				SaveTourHighscoreCoroutine((bool success, string data) => {                
							
					if( !success ){
						print( "Failed save tour highscore");
					} 	
					else{
					
						print( "Success save tour highscore");
					}
				})
			);
		}
		
		isLoading = false;
	}
	
	public IEnumerator SaveHighscoreCoroutine( string highscoreId, int score, Action<bool> Callback ){
			
		JSONNode json = JSONNode.Parse("{}");
		json["highscoreId"] = highscoreId;
		json["highscoreName"] = GetHighscoreName(highscoreId);
		json["maxEntryCount"] = GetMaxEntryCount(highscoreId).ToString();
		json["userID"] = userID;
		json["userName"] = userName;
		json["score"] = score.ToString();
		json["timestamp"] = ((long)(DateTime.Now.ToUniversalTime() - new System.DateTime (1970, 1, 1)).TotalSeconds).ToString();
		
		print("SaveHighscoreCoroutine " + json.ToString() );
		
		WWWForm form = new WWWForm();
		
		using (UnityWebRequest www = UnityWebRequest.Post(SAVE_HIGHSCORE_URL, form))
		{	
			UploadHandler uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json.ToString()));
			//uploadHandler.contentType = "application/x-www-form-urlencoded";
			uploadHandler.contentType = "application/json";
			www.uploadHandler = uploadHandler;
			
			www.SetRequestHeader("User-Agent", "DefaultBrowser");
			www.SetRequestHeader("Content-Type", "application/json");

			www.SendWebRequest();
			float timer = 180;
			while( !www.isDone && timer > 0 ){
			
				timer -= Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			if( timer <= 0 ){
				
				Debug.LogError ("Error SaveHighscoreCoroutine, Timeout");
				Callback(false);
			}
			else{
				if (www.isNetworkError || www.isHttpError)
				{
					Debug.LogError ("Error SaveHighscoreCoroutine: " + www.error);
					Callback(false);
				}
				else
				{
					//print ("Success SaveHighscoreCoroutine, response: " + www.downloadHandler.text);
					Callback(true);
				}
			}
		}	
		
		isLoading = false;
	}
	
	public IEnumerator GetHighscoreCoroutine(){

		yield return StartCoroutine(
			GetHighscoreCoroutine(highscoreId, (bool success, JSONNode json) => {                
			
				if( !success ){
					print( "Failed get highscore");
				} 	
				else{		
					print( "Success get highscore");
					print(json.ToString());
				}
			})
		);
		isLoading = false;
	}
	
	public IEnumerator GetHighscoreCoroutine( string highscoreId, Action<bool, JSONNode> Callback ){
			
		JSONNode json = JSONNode.Parse("{}");
		json["highscoreId"] = highscoreId;
		json["highscoreName"] = GetHighscoreName(highscoreId);
		json["maxEntryCount"] = GetMaxEntryCount(highscoreId).ToString();
		
		//print( "GetHighscoreCoroutine " + json.ToString() );
		
		WWWForm form = new WWWForm();
		
		using (UnityWebRequest www = UnityWebRequest.Post(GET_HIGHSCORE_URL, form))
		{	
			UploadHandler uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json.ToString()));
			//uploadHandler.contentType = "application/x-www-form-urlencoded";
			uploadHandler.contentType = "application/json";
			www.uploadHandler = uploadHandler;
			
			www.SetRequestHeader("User-Agent", "DefaultBrowser");
			www.SetRequestHeader("Content-Type", "application/json");

			www.SendWebRequest();
			float timer = 180;
			while( !www.isDone && timer > 0 ){
			
				timer -= Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			if( timer <= 0 ){
				
				Debug.LogError ("Error GetHighscoreCoroutine, Timeout");
				Callback(false, "");
			}
			else{
				if (www.isNetworkError || www.isHttpError)
				{
					Debug.LogError ("Error GetHighscoreCoroutine: " + www.error);
					Callback(false, "");
				}
				else
				{
					//print ("Success GetHighscoreCoroutine, response: " + www.downloadHandler.text);
					
					JSONNode responseJson = JSONNode.Parse(www.downloadHandler.text);
					if( responseJson == null ){
						Callback(false, "");
					}else{
						Callback(true, responseJson);
					}
				}
			}
		}	
		
		isLoading = false;
	}
	
	public void OnSearch(){
		
		for( int i = 0; i < listElements.Count; i++ ){
			listElements[i].SetActive(true);
		}
		
		string searchPrefix = searchInputField.text;
		if( searchPrefix.Length < minSearchPrefixLength ) return;
		
		for( int i = 0; i < listElements.Count; i++ ){
			
			string userName = listElements[i].GetComponent<HighscoreListElement>().userNameLabel.text;
			if( userName.StartsWith(searchPrefix, StringComparison.InvariantCultureIgnoreCase) ){
				listElements[i].SetActive(true);
			}else{
				listElements[i].SetActive(false);
			}
		}
	}
	
	public void ClearSearch(){
		
		searchInputField.SetTextWithoutNotify("");
		OnSearch();
	}
	
	public string GetHighscoreName( string highscoreId ){
		
		/*
		for( int i = 0; i < dataJson["highscores"].Count; i++ ){
			if( dataJson["highscores"][i]["highscoreId"].Value == highscoreId ) return dataJson["highscores"][i]["highscoreName"].Value;
		}
		
		Debug.LogError("highscoreName undefined");
		*/
		
		return highscoreId;
	}
	
	public int GetMaxEntryCount( string highscoreId ){
		
		/*
		for( int i = 0; i < dataJson["highscores"].Count; i++ ){
			if( dataJson["highscores"][i]["highscoreId"].Value == highscoreId ) return dataJson["highscores"][i]["maxEntryCount"].AsInt;
		}
		
		Debug.LogError("maxEntryCount undefined");
		*/
		return 0;
	}
	
	public bool IsMyUserId( string userID ){
		return userID == this.userID;
	}
	
	public bool IamInList( JSONNode highscoreData ){
		
		for( int i = 0; i < highscoreData["data"]["entries"].Count; i++ ){
			
			if( highscoreData["data"]["entries"][i]["userID"].Value == userID )
			{
				return true;
			}
		}
		return false;
	}
	
	public IEnumerator SaveTourHighscoreCoroutine( Action<bool, string> Callback ){
		
		int tourScore = GetMainScore();;

		string myHighscoreId = TourController.instance.GetCurrentTourId();
		if( myHighscoreId == "" ){ myHighscoreId = "introTour"; }		
		myHighscoreId += "_Highscore";

		yield return StartCoroutine(
			SaveHighscoreCoroutine(myHighscoreId, tourScore, (bool success) => {                
			
				if( !success ){
					print( "Failed save highscore");
					Callback(false, "");
				} 	
				else{		
					print( "Success save tour highscore " + myHighscoreId + " " + tourScore);
					Callback(true, "");
				}
			})
		);
		
		isLoading = false;
	}
	
	
	public IEnumerator UpdateMainHighscoreCoroutine(){

		float startTime = Time.time;

		//Comparing local saved points with server saved points
		int localSavedPoints = GetMainScore();
		int mainServerPoints = 0;
		
		string myHighscoreId = TourController.instance.GetCurrentTourId();
		if( myHighscoreId == "" ){ myHighscoreId = "introTour"; }		
		myHighscoreId += "_Highscore";
		
		print("UpdateMainHighscoreCoroutine " + myHighscoreId);
		
		JSONNode highscoreDataTmp = JSONNode.Parse("{}");
		bool isSuccessTmp = false;
		yield return StartCoroutine(
			GetHighscoreCoroutine(myHighscoreId, (bool success, JSONNode json) => {                
				
				isSuccessTmp = success;
				if( success ){
					highscoreDataTmp = json;
				}
			})
		);
		
		if( isSuccessTmp ){
			
			JSONNode userDataTmp = GetVisibleUserList(highscoreDataTmp);
			for( int i = 0; i < userDataTmp["users"].Count; i++ ){
								
				int userPoints = userDataTmp["users"][i]["score"].AsInt;
				if( userDataTmp["users"][i]["userID"].Value == userID ){
					mainServerPoints = userPoints;
					break;
				}
			}
			
			if( mainServerPoints == localSavedPoints ){
				
				print( "Server points are equal as local points, nothing to do " + mainServerPoints + " " + localSavedPoints);
				PlayerPrefs.SetInt("ScoreSync", 0);
				yield break;
				
			}else if( mainServerPoints > localSavedPoints ){
				
				// This can happen if we uninstall the app, all local points gets lost and have to be retrieved from server again
				print( "Server points are higher than local points " + mainServerPoints + " " + localSavedPoints);
				print( "We need to get points from server");
				
				// Avoid syncing on every app start, usually one sync should fix the score,
				// but if somethings wrong saved on the server, local and server score will never get in sync
				int syncedCount = PlayerPrefs.GetInt("ScoreSync", 0);
				if( syncedCount > 0 ){ 
					
					syncedCount--;
					PlayerPrefs.SetInt("ScoreSync", syncedCount);
					yield break;
				}
			}
			else if( mainServerPoints < localSavedPoints ){
				
				// This can happen if score did not get saved to server
				print( "Server points are less than local points " + mainServerPoints + " " + localSavedPoints);
				yield return StartCoroutine( SaveHighscoreCoroutine() );
				PlayerPrefs.SetInt("ScoreSync", 0);
				yield break;
			}
		}

		//If local points are not equal to server points we might need to retrieve all station scores and save locally
		for( int i = 0; i<stationIds.Count; i++){
						
			string highscoreIdTmp = stationIds[i] + "_Highscore";
			//print("Get score from server " + stationIds[i]);

			JSONNode highscoreData = JSONNode.Parse("{}");
			bool isSuccess = false;
			yield return StartCoroutine(
				GetHighscoreCoroutine(highscoreIdTmp, (bool success, JSONNode json) => {                
				
					isSuccess = success;
					if( success ){
						highscoreData = json;
					}
				})
			);
			
			if( isSuccess ){
				
				JSONNode userData = GetVisibleUserList(highscoreData);
				for( int j = 0; j < userData["users"].Count; j++ ){
								
					int serverPoints = userData["users"][j]["score"].AsInt;
					if( userData["users"][j]["userID"].Value == userID ){
						
						//print("user found");
						
						if( !PlayerPrefs.HasKey(highscoreIdTmp) ){
							
							print("Settings severpoints for " + highscoreIdTmp + serverPoints);
							PlayerPrefs.SetInt(highscoreIdTmp, serverPoints);
							
						}else{
							
							int p = PlayerPrefs.GetInt(highscoreIdTmp);
							if( p < serverPoints ){
								
								print("Settings severpoints for " + highscoreIdTmp + serverPoints);
								PlayerPrefs.SetInt(highscoreIdTmp, serverPoints);
							}
						}
					}
				}
			}else{
				//print("Failed get score");
			}
		}
		
		PlayerPrefs.SetInt("ScoreSync", 10);
		print("UpdateMainHighscore duration 2: " + (Time.time-startTime));
	}
	
	public int GetMainScore(){
		
		int tourScore = 0;
		
		string tourId = TourController.instance.GetCurrentTourId();
		if( tourId == "" ){ tourId = "introTour"; }		
		
		
		for( int i = 0; i<stationIds.Count; i++){
			
			//if( !TourController.instance.IsStationInTour(stationIds[i], tourId) ) continue;
			
			string highscoreIdTmp = stationIds[i] + "_Highscore";
			if( PlayerPrefs.HasKey(highscoreIdTmp) ){
				
				int p = GetPointsWithWeight(stationIds[i], PlayerPrefs.GetInt(highscoreIdTmp) );
				tourScore += p;
				
				//print("Found points for station highscore " + highscoreIdTmp + " " + p);
			}
		}
		
		return tourScore;
	}
	
	public int GetPointsWithWeight( string stationId, int points ){
		
		int weightedPoints = points;
		
		/*
		// Optional;
		switch( stationId ){
			
		case "dalliKlick1":
		
			weightedPoints *= 1;
			break;
		}
		*/
		
		return weightedPoints;
	}
	
	public void OpenTourHighscore(){
		
		string myHighscoreId = TourController.instance.GetCurrentTourId();
		if( myHighscoreId == "" ){ myHighscoreId = "introTour"; }
		myHighscoreId += "_Highscore";
		
		CreateHighscoreList(myHighscoreId);
		MenuController.instance.OpenDragMenu("Highscore");
	}
	
	public void OpenHighscore(){
		
		CreateHighscoreList(highscoreId);
		MenuController.instance.OpenDragMenu("Highscore");
	}
	
	public void OpenHighscore( string id ){
		
		highscoreId = id + "_Highscore";

		CreateHighscoreList(highscoreId);
		MenuController.instance.OpenDragMenu("Highscore");
	}
	
	public bool IsRegisteredForHighscore(){
		
		//return PlayerPrefs.HasKey("userName");
		return userName != "";

	}
}
