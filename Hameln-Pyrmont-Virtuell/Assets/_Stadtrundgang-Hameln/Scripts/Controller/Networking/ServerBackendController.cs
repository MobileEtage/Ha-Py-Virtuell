using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;


public class ServerBackendController : MonoBehaviour
{
    public enum FileSource { Local, Stage, Production, MobileServer, LocalAndProduction, StageAndLocalFeatures }
    public FileSource filesSource;

    public JSONNode translationJson;
    public JSONNode toursJson;
    public JSONNode stationsJson;
    public JSONNode quizJson;
    public JSONNode dalliKlickJson;

	// Mobile Server
	private string translationURL_MobileServer = "https://osnabrueck.stadtrundgang.dev.hob-by-horse.de/api/translations";
	private string toursURL_MobileServer = "https://app-etagen.die-etagen.de/Stadtrundgang/Osnabrueck/Data/tours.json";
	private string stationsURL_MobileServer = "https://app-etagen.die-etagen.de/Stadtrundgang/Osnabrueck/Data/stations.json";
	private string quizURL_MobileServer = "https://app-etagen.die-etagen.de/Stadtrundgang/Osnabrueck/Data/quiz.json";
	private string dalliKlickURL_MobileServer = "https://app-etagen.die-etagen.de/Stadtrundgang/Osnabrueck/Data/dalliKlick.json";

	// Stage (Testing)
	private string translationURL_Stage = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/translations";
	private string toursURL_Stage = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/tours";
	private string stationsURL_Stage = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/stations";
	private string quizURL_Stage = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/quiz";
	private string dalliKlickURL_Stage = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/dalliKlick";

	// Production (Store)
	private string translationURL_Production = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/translations";
	private string toursURL_Production = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/tours";
	private string stationsURL_Production = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/stations";
	private string quizURL_Production = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/quiz";
	private string dalliKlickURL_Production = "https://hameln.stadtrundgang.dev.hob-by-horse.de/api/dalliKlick";

	// Links
	private string translationURL = "";
    private string toursURL = "";
    private string stationsURL = "";
    private string quizURL = "";
	private string dalliKlickURL = "";
	private string markerPositionsURL = "https://app-etagen.die-etagen.de/Stadtrundgang/Hameln/MarkerPosition/markerPositions.json";

	// Production
    private string usr = "";
	private string pw = "";

	private bool isDownloading = false;
	private bool isUploading = false;

	private JSONNode stationsLocal;
	private JSONNode configJson;

	public static ServerBackendController instance;
	void Awake()
	{
		instance = this;

		TextAsset asset = Resources.Load<TextAsset>( "config" );

		if( asset != null )
		{
			configJson = JSONNode.Parse( Resources.Load<TextAsset>( "config" ).text );
			usr = configJson["usr"].Value;
			pw = configJson["pw"].Value;
		}
		else
		{
			Debug.LogError("No config file");
		}

		if ( filesSource == FileSource.Stage || filesSource == FileSource.StageAndLocalFeatures )
		{
			translationURL = translationURL_Stage;
			toursURL = toursURL_Stage;
			stationsURL = stationsURL_Stage;
			quizURL = quizURL_Stage;
			dalliKlickURL = dalliKlickURL_Stage;

			if ( configJson != null )
			{
				usr = configJson["usrStage"].Value;
				pw = configJson["pwStage"].Value;
			}
		}
		else if ( filesSource == FileSource.Production || filesSource == FileSource.LocalAndProduction )
		{
			translationURL = translationURL_Production;
			toursURL = toursURL_Production;
			stationsURL = stationsURL_Production;
			quizURL = quizURL_Production;
			dalliKlickURL = dalliKlickURL_Production;
		}
		else if ( filesSource == FileSource.MobileServer )
		{
			translationURL = translationURL_MobileServer;
			toursURL = toursURL_MobileServer;
			stationsURL = stationsURL_MobileServer;
			quizURL = quizURL_MobileServer;
			dalliKlickURL = dalliKlickURL_MobileServer;
		}

		translationJson = GetJson( "_translations" );
	}

	void Start()
	{
		TextAsset textAsset = Resources.Load<TextAsset>( "_stations" );
		if ( textAsset != null ) { stationsLocal = JSONNode.Parse( textAsset.text ); }
	}

	
	public JSONNode GetJson( string id, bool useLocalFileAsBackup = true ){

        if (id == "_translations") { if (translationJson != null) { return translationJson; } }
        if (id == "_tours") { if (toursJson != null) { return toursJson; } }
        if (id == "_stations") { if (stationsJson != null) { return stationsJson; } }
        if (id == "_quiz") { if (quizJson != null) { return quizJson; } }
        if (id == "_dalliKlick") { if (dalliKlickJson != null) { return dalliKlickJson; } }

        bool loadFileFromPersistentDataPath = true;
		if ( filesSource == FileSource.Local ) { loadFileFromPersistentDataPath = false; }

		JSONNode json = JSONNode.Parse( "{}");			
		string path = Application.persistentDataPath + "/" + id + ".json";
		bool shouldUseLocalFile = false;
		if( File.Exists(path) && loadFileFromPersistentDataPath )
        {
			json = JSONNode.Parse( File.ReadAllText(path) );
			if(json == null){

				print("<color=#FF0000>GetJson " + id + " could not parse:</color> " + File.ReadAllText(path) );
				shouldUseLocalFile = true; 
			}
		}
		else
		{
			shouldUseLocalFile = true;
        }

		if(shouldUseLocalFile && useLocalFileAsBackup)
        {		
			TextAsset textAsset = Resources.Load<TextAsset>(id);
			if( textAsset != null ){
				
				print("Using local json file " + id);
				json = JSONNode.Parse(textAsset.text);
			}
		}
		else if( filesSource == FileSource.LocalAndProduction && (id == "_tours" || id == "_stations") )
		{
			TextAsset textAsset = Resources.Load<TextAsset>(id);
			if ( textAsset != null )
			{
				print( "Using local json file additionally " + id );
				JSONNode jsonTmp = JSONNode.Parse( textAsset.text );

				// Merge local and online json
				// It could be tricky if we have stations and tours with the same id, local and online, because then we need to merge all information and prioritize which to take if local and online json have the same attributes
				if ( jsonTmp != null )
				{
					JSONNode mergedJson = JSONNode.Parse( "[]" );
					for ( int i = 0; i < jsonTmp.Count; i++ ) { mergedJson.Add( jsonTmp[i] ); }
					for ( int i = 0; i < json.Count; i++ ) { mergedJson.Add( json[i] ); }
					json = mergedJson;
				}
			}
		}
		else if ( filesSource == FileSource.StageAndLocalFeatures && (id == "_stations") && stationsLocal != null )
		{
			AppendFeature( json, "glasmacher", "avatarGuide" );
			AppendFeature( json, "glashuette1", "glashuette1" );
			AppendFeature( json, "glashuette2", "glashuette2" );
			AppendFeature( json, "glashuette3", "glashuette3" );
			AppendFeature( json, "glashuette4", "glashuette4" );

			//Debug.Log(json.ToString());
		}

		if (id == "_tours") 
		{
			if (json["tours"] == null)
			{
				toursJson = JSONNode.Parse("{\"tours\":[]}");
				toursJson["tours"] = json;
			}
			else
			{
                toursJson = json;
            }
            return toursJson;
        }
        else if (id == "_stations")
        {
            if (json["stations"] == null)
            {
                stationsJson = JSONNode.Parse("{\"stations\":[]}");
                stationsJson["stations"] = json;
            }
            else
            {
                stationsJson = json;
            }

            return stationsJson;
        }
        else if(id == "_translations") { translationJson = json; }
        else if(id == "_quiz") { quizJson = json; }
        else if(id == "_dalliKlick") { dalliKlickJson = json; }

        return json;
	}

	public void AppendFeature(JSONNode json, string stationId, string featureId)
	{
		for ( int i = 0; i < json.Count; i++ )
		{
			if ( json[i]["id"].Value == stationId )
			{
				bool hasFeature = false;
				for ( int j = 0; j < json[i]["features"].Count; j++ )
				{
					if ( json[i]["features"][j]["id"].Value == featureId ) { hasFeature = true; }
				}

				if ( !hasFeature )
				{
					if ( featureId == "avatarGuide" ) { json[i]["features"].Add( JSONNode.Parse( "{\"id\": \"avatarGuide\", \"audio\":\"audios/speech_ritter\"}" ) ); }
					else
					{
						json[i]["features"].Add( JSONNode.Parse( "{\"id\": \"" + featureId + "\"}" ) );
						json[i]["marker"] = JSONNode.Parse( "[\"" + featureId + "\"]" );
					}
				}
			}
		}
	}
	
	public JSONNode GetLocalJson( string id ){
		
		JSONNode json = JSONNode.Parse( "{}");
		TextAsset textAsset = Resources.Load<TextAsset>(id);
		if( textAsset != null ){
			json = JSONNode.Parse(textAsset.text);
		}

		if( id == "translation" ){ translationJson = json; }
		
		return json;
	}
	
	public IEnumerator SyncBackendCoroutine(){

        List<string> fileURLs = new List<string>();

        if (translationURL != "") { fileURLs.Add(translationURL); }
        if (toursURL != "") { fileURLs.Add(toursURL); }
        if (stationsURL != "") { fileURLs.Add(stationsURL); }
        if (quizURL != "") { fileURLs.Add(quizURL); }
		if ( dalliKlickURL != "" ) { fileURLs.Add( dalliKlickURL ); }
		if ( markerPositionsURL != "" ) { fileURLs.Add( markerPositionsURL ); }

		for ( int i = 0; i < fileURLs.Count; i++ ){
			
			bool isSuccess = false;
			string url = fileURLs[i];
			
			yield return StartCoroutine(
				GetCoroutine(url, 8, (bool success, string data) => {  
				
					isSuccess = success;
					if( !isSuccess ){	
						print("Failed download json, error: " + data );
					}else{
						
						print("Success DownloadData " + data);

                        string filePath = Application.persistentDataPath + "/_" + Path.GetFileNameWithoutExtension(url) + ".json";
                        //if (i == 0) { filePath = Application.persistentDataPath + "/_translations.json"; }

						File.WriteAllText( filePath, data );

                        print("Saved to " + filePath);
                        print("Data " + data);

                        // Optional: after we download the new translation file, we can update LanguageController 
                        // to use the new file and also update all labels with TextOptions script

                        if ( i == 0 ){
							
							translationJson = null;
							LanguageController.UpdateBackendJson();
							LanguageController.TranslateAllTextOptionLabels();
						}

						if( url == markerPositionsURL )
						{
							GlasObjectController.instance.UpdatePositionsData( filePath );
						}
					}
				})
			);
		}
		
		translationJson = GetJson("_translations");
	}
	
	public IEnumerator DownloadDataCoroutine( string url ){
				
		bool isSuccess = false;
		yield return StartCoroutine(
			GetCoroutine(url, 3600, (bool success, string data) => {  
				
				isSuccess = success;
				if( !isSuccess ){	
					print("Failed download json, error: " + data );
				}else{
					
					print("Success DownloadData " + data);
					
					string fileName = Application.persistentDataPath + "/" + 
						Path.GetFileNameWithoutExtension(url) + ".json";
					File.WriteAllText( fileName, data );
				}
			})
		);
		isDownloading = false;
	}
	
	public IEnumerator GetCoroutine( string backendURL, int timeout, Action<bool, string> Callback ){

        print("GetCoroutine " + backendURL);

		using (UnityWebRequest www = UnityWebRequest.Get(backendURL))
		{	
			www.timeout = timeout;

            /********** Authorization **********/
            www.SetRequestHeader("Authorization", authenticate(usr, pw));

            /********** Use an UploadHandler **********/
            // Drupal Post solution
            //UploadHandler uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));
            //uploadHandler.contentType = "application/x-www-form-urlencoded";
            //www.uploadHandler = uploadHandler;



            /********** User-Agent **********/
            www.SetRequestHeader("User-Agent", "DefaultBrowser");


			/********** Content-Type **********/
			
			// default
			//www.SetRequestHeader("Content-Type", "application/octet-stream");
			
			// json header
			www.SetRequestHeader("Content-Type", "application/json");
			
			// if using base64 string
			//www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");		
			
			// if using bytes --> form.AddBinaryData 
			//www.SetRequestHeader("Content-Type", "multipart/form-data");			
			
			// ???
			//www.SetRequestHeader("Content-Type", "text/plain;charset=UTF-8");
			
			/********** Additonal settings **********/
			//www.timeout = 3600;
			//www.chunkedTransfer = false;
			//www.SetRequestHeader("Accept", "application/json");
			
			www.SendWebRequest();
			float timer = 180;
			while( !www.isDone && timer > 0 ){
			
				timer -= Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			if( timer <= 0 ){
				
				Debug.LogError ("Error GetCoroutine, Timeout");
				Callback(false, "");
			}
			else{
				if (www.isNetworkError || www.isHttpError)
				{
					Debug.LogError ("Error GetCoroutine: " + www.error);
					Callback(false, www.error);
				}
				else
				{
					print ("Response: " + www.downloadHandler.text);
					Callback(true, www.downloadHandler.text);
				}
			}
		}	
		
		isDownloading = false;
	}
	
	public IEnumerator ValidateConnectionCoroutine( Action<bool, string> Callback ){

        //string url = "https://app-etagen.die-etagen.de/";
        string url = "https://www.google.de/";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
		{	
			www.SetRequestHeader("User-Agent", "DefaultBrowser");
			www.SetRequestHeader("Content-Type", "application/json");
			www.SendWebRequest();
			float timer = 180;
			while( !www.isDone && timer > 0 ){
			
				timer -= Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			if( timer <= 0 ){
				
				Debug.LogError ("Error GetCoroutine, Timeout");
				Callback(false, "");
			}
			else{
				if (www.isNetworkError || www.isHttpError)
				{
					Debug.LogError ("Error GetCoroutine: " + www.error);
					Callback(false, www.error);
				}
				else
				{
					//print ("Response: " + www.downloadHandler.text);
					Callback(true, www.downloadHandler.text);
				}
			}
		}	
	}
	
	public IEnumerator PostCoroutine( string backendURL, string formString, Action<bool, string> Callback ){
				
		WWWForm form = new WWWForm ();
		
		print("Uploading formString " + formString);
		
		using (UnityWebRequest www = UnityWebRequest.Post(backendURL, form))
		{	
			UploadHandler uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(formString));
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
				
				Debug.LogError ("Error PostCoroutine, Timeout");
				Callback(false, "");
			}
			else{
				if (www.isNetworkError || www.isHttpError)
				{
					Debug.LogError ("Error PostCoroutine: " + www.error);
					Callback(false, www.error);
				}
				else
				{
					print ("Response: " + www.downloadHandler.text);
					Callback(true, www.downloadHandler.text);
				}
			}
		}	
		
		isUploading = false;
	}

    string authenticate(string username, string password)
    {
        string auth = username + ":" + password;
        auth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
        auth = "Basic " + auth;
        return auth;
    }
}
