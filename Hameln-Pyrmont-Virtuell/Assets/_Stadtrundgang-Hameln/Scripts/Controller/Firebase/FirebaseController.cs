using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

#if HAS_FIREBASE
using Firebase;
using Firebase.Analytics;
#endif

public class FirebaseController : MonoBehaviour
{
	public bool editorAnalyticsEnabled = true;
	public bool iOSAnalyticsEnabled = true;
	public bool androidAnalyticsEnabled = true;
	public bool iOSCrashlyticsEnabled = true;
	public bool androidCrashlyticsEnabled = true;

	[Space(10)]
	
	//public event Action sentTrackingAuthorizationRequest;
	public GameObject privacySite;
	public GameObject privacySettingsBanner;
	public Toggle privacySettingsToggle;
	
	public bool firebaseInitialized = false;
	public bool firebaseInitializeFailed = false;
	private bool isLoading = false;

	public static FirebaseController instance;
	
	void Awake () {
		instance = this;
    }


    void Start(){
		
#if UNITY_EDITOR
	    if(editorAnalyticsEnabled){ Init(); }
#elif UNITY_IOS

	    if(iOSAnalyticsEnabled || iOSCrashlyticsEnabled)
		{
			var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
			print( "ATTrackingStatusBinding.GetAuthorizationTrackingStatus() " + status.ToString() );
			if ( status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED ) { PlayerPrefs.SetInt( "UserHasUpdatedPrivacySettings", 0 ); }

			Init();
		}

#elif UNITY_ANDROID
	    if(androidAnalyticsEnabled || androidCrashlyticsEnabled){ Init(); }
#endif
	}
	
	private void Init(){

		print("FirebaseController Init");
        
		if (PlayerPrefs.GetInt("UserHasUpdatedPrivacySettings", 0) == 1 && PlayerPrefs.GetInt("firebaseInitializeFailed", 0) == 1)
		{
			print("Do not init Firebase");
			firebaseInitializeFailed = true;
			return;
		} 
		
		StartCoroutine(InitCoroutine());
		
		try{

			#if HAS_FIREBASE
			FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
        	
	            var dependencyStatus = task.Result;
	            if (dependencyStatus == DependencyStatus.Available)
	            {
		            print("Firebase successfully initialized");

		            var app = FirebaseApp.DefaultInstance;
		            firebaseInitialized = true;
	            }
	            else
	            {
	            	firebaseInitializeFailed = true;
		            Debug.LogWarning(System.String.Format("Could not initialize Firebase, DependencyStatus: " + dependencyStatus));
	            }
	        });
			#endif

		}catch(Exception e){
			
			firebaseInitializeFailed = true;
			print("FirebaseController Init error " + e.Message);
		}
		
	}
	
	public IEnumerator InitCoroutine(){
		
		float timer = 10;
		while(!firebaseInitialized && !firebaseInitializeFailed && timer > 0){ timer -= Time.deltaTime; yield return null; }
		if(firebaseInitialized){ OnFirebaseInitialized(); }
		else if(firebaseInitializeFailed){ OnFirebaseInitializeFailed(); }
	}
    
	public void OnFirebaseInitialized(){
		
		print("OnFirebaseInitialized");
		
		// SetAnalyticsCollectionEnabled and IsCrashlyticsCollectionEnabled
		if (PlayerPrefs.GetInt("FirebaseTrackingEnabled", 0) == 1)
		{
			print("Firebase Tracking Enabled");
			
			try{

				#if HAS_FIREBASE
				Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
				Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = true;
				#endif
				
			}catch(Exception e){
				print("OnFirebaseInitialized error " + e.Message);
			}
		}
		else
		{
			print("Firebase Tracking Disabled");
			
			try{

				#if HAS_FIREBASE
				Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(false);
				Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = false;
				#endif
			}
			catch(Exception e){
				print("OnFirebaseInitialized error " + e.Message);
			}
		}
		

		// Log Start events
		LogEvent("AppStart");
		
		/*
		if (!PlayerPrefs.HasKey("FirstAppStart"))
		{
			PlayerPrefs.SetInt("FirstAppStart", 1);
			LogEvent("FirstAppStart");
		}
		*/
		
		
		// Update UI
		if (PlayerPrefs.GetInt("FirebaseTrackingEnabled", 0) == 1){ privacySettingsToggle.SetIsOnWithoutNotify(true); }
		else{ privacySettingsToggle.SetIsOnWithoutNotify(false); }
	   
		if (PlayerPrefs.GetInt("UserHasUpdatedPrivacySettings", 0) != 1)
		{
			privacySettingsToggle.SetIsOnWithoutNotify(true);
			StartCoroutine(ShowPrivacySettingsCoroutine());
		}     		
	}
	
	public void OnFirebaseInitializeFailed(){
		
		print("OnFirebaseInitializeFailed");
		
		PlayerPrefs.SetInt("firebaseInitializeFailed", 1);
		
		if (PlayerPrefs.GetInt("UserHasUpdatedPrivacySettings", 0) != 1)
		{
			privacySettingsToggle.SetIsOnWithoutNotify(true);
			StartCoroutine(ShowPrivacySettingsCoroutine());
		}     		
	}
	

	public IEnumerator ShowPrivacySettingsCoroutine()
	{
		yield return new WaitForSeconds(1.0f);
		privacySettingsBanner.SetActive(true);
	}

	public void ShowPrivacySite()
	{
		if (Params.usePrivacyWeblink)
		{
			ToolsController.instance.OpenWebView(Params.privacyURL);
		}
		else
		{
			if (isLoading) return;
			isLoading = false;
			StartCoroutine(ShowPrivacySiteCoroutine());
		}
	}

	public IEnumerator ShowPrivacySiteCoroutine()
	{
		//yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("PrivacySite"));
		privacySite.SetActive(true);
		yield return null;

		isLoading = false;
	}

	public void BackFromPrivacy()
	{
		if (isLoading) return;
		isLoading = false;
		StartCoroutine(BackFromPrivacyCoroutine());
	}

	public IEnumerator BackFromPrivacyCoroutine()
	{
		yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("IntroSite"));
		if (PlayerPrefs.GetInt("UserHasUpdatedPrivacySettings", 0) != 1){ privacySettingsBanner.SetActive(true); }

		isLoading = false;
	}


	public void CommitPrivacySettings(bool shouldTrackData)
	{
		privacySettingsToggle.SetIsOnWithoutNotify(shouldTrackData);
		if (isLoading) return;
		isLoading = false;
		StartCoroutine(CommitPrivacySettingsCoroutine());
	}

	public void CommitPrivacySettings()
	{
		if (isLoading) return;
		isLoading = false;
		StartCoroutine(CommitPrivacySettingsCoroutine());
	}

	public IEnumerator CommitPrivacySettingsCoroutine()
	{
		yield return null;
		privacySettingsBanner.SetActive(false);
		PlayerPrefs.SetInt("UserHasUpdatedPrivacySettings", 1);
		PlayerPrefs.Save();

		if(!firebaseInitializeFailed){
			
			if (privacySettingsToggle.isOn)
			{
				print("Firebase Tracking Enabled");
				PlayerPrefs.SetInt("FirebaseTrackingEnabled", 1);
				
				try{

					#if HAS_FIREBASE
					Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
					Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = true;
					#endif

					if (!PlayerPrefs.HasKey("FirstAppStart"))
					{
						PlayerPrefs.SetInt("FirstAppStart", 1);
						LogEvent("FirstAppStart");
					}
					
				}catch(Exception e){
					print("Firebase CommitPrivacySettingsCoroutine error " + e.Message);
				}
					
			}
			else
			{
				print("Firebase Tracking Disabled");
				PlayerPrefs.SetInt("FirebaseTrackingEnabled", 0);
				
				try{

					#if HAS_FIREBASE
					Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(false);
					Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = false;
					#endif

				}catch(Exception e){
					print("Firebase CommitPrivacySettingsCoroutine error " + e.Message);
				}
			}
		}

#if UNITY_IOS

		var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
		print("ATTrackingStatusBinding.GetAuthorizationTrackingStatus() " + status.ToString());

		if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
		{
			ATTrackingStatusBinding.RequestAuthorizationTracking();
			//sentTrackingAuthorizationRequest?.Invoke();
		}

#endif

		isLoading = false;
	}
	
	/************************************************************** Log Events helper **************************************************************/
    
	public void LogEvent(string eventName, string parameterName = null, string parameterValue = null ){

		if( !firebaseInitialized ) return;
		
		bool validEventName = ValidateString(ref eventName);
		bool validParameterName = false;
		bool validParameterValue = false;
		
		if( !string.IsNullOrEmpty(parameterName) ) validParameterName = ValidateString(ref parameterName);
		if( !string.IsNullOrEmpty(parameterValue) ) validParameterValue = ValidateString(ref parameterValue);
		
		if( validEventName && validParameterName && validParameterValue ){

#if HAS_FIREBASE
			FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
			print( "FirebaseAnalytics LogEvent: " + eventName + " " + parameterName + " " + parameterValue );
#endif
		}
		else if( validEventName ){

#if HAS_FIREBASE
			FirebaseAnalytics.LogEvent( eventName );
			print( "FirebaseAnalytics LogEvent: " + eventName );
#endif
		}
	}

#if HAS_FIREBASE
	public void LogEvent(string eventName, Parameter[] parameter ){

		if( !firebaseInitialized ) return;
		
		bool validEventName = ValidateString(ref eventName);
				
		if( validEventName ){
			print( "FirebaseAnalytics LogEvent: " + eventName );
			FirebaseAnalytics.LogEvent( eventName, parameter );
		}
	}
#endif
	
	public void LogButtonEvent( string buttonID ){
		
		print("LogButtonEvent " + buttonID);
		
		if( !firebaseInitialized ) return;
		
		bool validEventName = ValidateString(ref buttonID);

		if( validEventName ){

#if HAS_FIREBASE
			print( "FirebaseAnalytics LogEvent: " + buttonID );
			FirebaseAnalytics.LogEvent( buttonID );
#endif
		}
	}
	
	private bool ValidateString( ref string input ){

		if (string.IsNullOrEmpty(input)) return false;

		// Check first character
		if ( !char.IsLetter( input[0] )){
			return false;
		}

		input = input.Replace(" ", "_");

		// Check valide characters
		foreach (char c in input)
		{
			bool isLetterOrDigit = char.IsLetterOrDigit(c);
			if( !isLetterOrDigit && c != '_'){
				return false;
			}
		}

		//Replace invalid characters
		string result = input.Replace(" ", "_");
		result = result.Replace("Ä", "Ae");
		result = result.Replace("ä", "ae");
		result = result.Replace("Ü", "Ue");
		result = result.Replace("ü", "ue");
		result = result.Replace("Ö", "Oe");
		result = result.Replace("ö", "oe");
		result = result.Replace("ß", "ss");

		if (result.Length > 40) return false;

		input = result;
		return true;
	}
	
	public static string ReplaceAt(string input, int index, char newChar)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		StringBuilder builder = new StringBuilder(input);
		builder[index] = newChar;
		return builder.ToString();
	}
	
	public string ConvertToValidString( string input ){

		if (string.IsNullOrEmpty(input)) return "Empty";

		// Check first character
		if ( !char.IsLetter( input[0] )){
			input = ReplaceAt(input, 2, 'A');
		}

		input = input.Replace(" ", "_");

		// Check valide characters
		int index = 0;
		foreach (char c in input)
		{
			bool isLetterOrDigit = char.IsLetterOrDigit(c);
			if( !isLetterOrDigit && c != '_'){
				input = ReplaceAt(input, index, '_');
			}
			index++;
		}

		//Replace invalid characters
		string result = input.Replace(" ", "_");
		result = result.Replace("Ä", "Ae");
		result = result.Replace("ä", "ae");
		result = result.Replace("Ü", "Ue");
		result = result.Replace("ü", "ue");
		result = result.Replace("Ö", "Oe");
		result = result.Replace("ö", "oe");
		result = result.Replace("ß", "ss");

		if (result.Length > 40) return result.Substring(0,40);
		return result;
	}
	
	public void LogParameter( string id, Dictionary<string, string> parameterDictionary ){
		
		if( !firebaseInitialized ) return;

		#if HAS_FIREBASE

		Firebase.Analytics.Parameter[] parameter = new Firebase.Analytics.Parameter[parameterDictionary.Count];

		int index = 0;
		foreach( KeyValuePair<string, string> pair in parameterDictionary ){
			
			parameter[index++] = new Firebase.Analytics.Parameter( pair.Key, pair.Value );
		}
		
		LogEvent( id, parameter );

		#endif	
	}
}
