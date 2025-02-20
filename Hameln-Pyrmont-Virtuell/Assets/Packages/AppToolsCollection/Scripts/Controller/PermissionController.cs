using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

using TMPro;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

#if UNITY_iOS
using UnityEngine.iOS;
#endif

public class PermissionController : MonoBehaviour
{
	public ARSession arSession;

	[Space(10)]

	public bool editorLocationPermissionGranted = true;
	public bool editorLocationServiceEnabled = true;
	public bool editorMicrophoneEnabled = true;
	public bool editorCameraEnabled = true;
	public bool editorCameraPermissionAsked = true;
	public bool editorARTrackingSupported = true;

	[Header("Custom ARCore params")]

	public TextMeshProUGUI mapScanLabel;

	private bool gpsRequired = false;
	private bool isLoading = false;
	private bool waitingForCallback = false;

	private bool requestingPermission = false;
	private int permissionState = 0;

	public static PermissionController instance;
	void Awake()
	{
		instance = this;
	}

	public void Start()
	{

		PlayerPrefs.SetInt("ARCoreWasInstalled", 0);

		int currentVersion = 1;
		int version = PlayerPrefs.GetInt("PermissionVersion", 0);
		if (version < currentVersion)
		{
			PlayerPrefs.SetInt("PermissionVersion", currentVersion);
			PlayerPrefs.SetInt("iOSPermissionCameraAsked", 0);
			PlayerPrefs.SetInt("iOSPermissionMicrophonAsked", 0);
		}
	}


	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*####################                                                         ######################*/
	/********************* Functions to call before starting camera or loading map ***********************/
	/*####################                                                         ######################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/

	// Checks camera, arcore, gps (optional) and continues*
	public void ValidatePermissions()
	{

		if (isLoading) return;
		isLoading = true;
		StartCoroutine(ValidatePermissionsCoroutine());
	}

	public IEnumerator ValidatePermissionsCoroutine()
	{

		CanvasController.instance.eventSystem.enabled = false;

		bool hasPermissions = false;
		string myMessage = "";
		string myTitle = "";
		yield return StartCoroutine(
			ValidatePermissionsCoroutine((bool success, string message, string title) => {

				hasPermissions = success;
				myMessage = message;
				myTitle = title;
			})
		);

#if UNITY_EDITOR
		hasPermissions = editorCameraEnabled && editorLocationPermissionGranted;
#endif

		if (!hasPermissions)
		{

			// Navigate to android settings to enable gps
			if (myMessage == "Bitte erlaube in den Einstellungen den Zugriff auf Deinen Standort, damit wir Dir Deine Position in der Karte anzeigen können.")
			{

				InfoController.instance.ShowMessage(myTitle, myMessage);
				yield return new WaitForSeconds(2.0f);
				InfoController.instance.messageDialog.SetActive(false);

				try
				{
					AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
					AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
					AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

					AndroidJavaClass jc = new AndroidJavaClass("de.dieetagen.tools.Tools");
					jc.CallStatic("setLocationServicesEnabled", activity);

				}
				catch (UnityException e)
				{

					print(e.Message);
				}
			}
			else if (myMessage == "Du musst ARCore aus dem Play Store installieren.")
			{

				DownloadARCore();
			}
			else
			{

				InfoController.instance.ShowMessage(myTitle, myMessage);
			}

		}
		else
		{

			// if all okay do this*
			//InitARFoundation();
			//SiteController.instance.SwitchToSite("TourOverviewSite");
		}

		isLoading = false;
		CanvasController.instance.eventSystem.enabled = true;
	}

	public IEnumerator ValidatePermissionsCoroutine(Action<bool, string, string> Callback)
	{

		/************************ Permission camera ************************/
		bool hasPermissionCamera = true;
		if (!instance.HasPermissionCamera())
		{

			hasPermissionCamera = false;
			yield return StartCoroutine(
				RequestCameraPermissionCoroutine((bool success) => {
					hasPermissionCamera = success;
				})
			);

			if (!hasPermissionCamera)
			{

				Callback(false, "Der Zugriff auf die Kamera ist erforderlich, damit Du die Augmented-Reality-Funktion nutzen kannst.", "Kamerafreigabe");
				yield break;
			}
		}

		/************************ Permission location ************************/

		if (gpsRequired)
		{

			bool hasPermissionLocation = true;
			if (!HasPermissionLocation())
			{

				hasPermissionLocation = false;
				yield return StartCoroutine(
					RequestLocationPermissionCoroutine((bool success) => {
						hasPermissionLocation = success;
					})
				);

				if (!hasPermissionLocation)
				{

					Callback(false, "Bitte erlaube der App den Zugriff auf Deinen Standort, damit sie Dir Deine Position in der Karte anzeigen kann.", "Standort freigeben");
					yield break;
				}
			}
		}


		if (Application.platform == RuntimePlatform.Android)
		{
			/************************ Location services enabled? ************************/
			if (gpsRequired)
			{

				try
				{

					print("Checking gps enabled...");
					AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
					AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
					AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

					AndroidJavaClass jc = new AndroidJavaClass("de.dieetagen.tools.Tools");
					bool locationServicesEnabled = jc.CallStatic<bool>("isLocationServicesEnabled", context);

					if (!locationServicesEnabled)
					{

						Callback(false, "Bitte erlaube in den Einstellungen den Zugriff auf Deinen Standort, damit wir Dir Deine Position in der Karte anzeigen können.", "Standortfreigabe");
						yield break;
					}
					else
					{

						print("GPS enabled");
					}

				}
				catch (UnityException e)
				{

					print(e.Message);
				}
			}


			/************************ ARCore installed? ************************/
			print("Checking ARCore installed...");
			bool markerlessAvailable = false;
			int arSessionCode = -1;
			yield return StartCoroutine(
				CheckingMarkerlessAvailabilityCoroutine((bool success, int arSessionState) => {
					markerlessAvailable = success;
					arSessionCode = arSessionState;
				})
			);

			print("CheckingMarkerlessAvailabilityCoroutine " + markerlessAvailable + " " + arSessionCode);

			if (!markerlessAvailable)
			{

				if (arSessionCode == 0)
				{

					InfoController.instance.ShowMessage("Dein mobiles Gerät verfügt nicht über die notwendigen Hardwareanforderungen und funktioniert möglicherweise nicht richtig.");
				}
				else
				{

					if (PlayerPrefs.GetInt("ARCoreInstalled", 0) != 1)
					{

						Callback(false, "Du musst ARCore aus dem Play Store installieren.", "ARCore installieren");
						yield break;
					}
				}
			}
		}

		Callback(true, "", "");
	}

	// Checks gps granted and location services enabled
	// Displays messages if not everything okay
	public IEnumerator ValidatePermissionsGPSCoroutine(Action<bool> Callback)
	{

		bool hasPermission = false;
		int errorCode = 0;
		yield return StartCoroutine(
			ValidatePermissionsGPSCoroutine((bool success, int error) => {
				hasPermission = success;
				errorCode = error;
			})
		);

		if (!hasPermission)
		{

			if (errorCode == 1)
			{

				InfoController.instance.ShowMessage("Der Zugriff auf den Standort ist erforderlich, damit wir deine Position anzeigen können.");
			}
			else if (errorCode == 2)
			{

				InfoController.instance.ShowMessage("Bitte aktiviere GPS.");
				yield return new WaitForSeconds(2.0f);
				InfoController.instance.messageDialog.SetActive(false);

				try
				{
					AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
					AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
					AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

					AndroidJavaClass jc = new AndroidJavaClass("de.dieetagen.tools.Tools");
					jc.CallStatic("setLocationServicesEnabled", activity);

				}
				catch (UnityException e)
				{

					print(e.Message);
				}
			}

			Callback(false);
		}
		else
		{
			Callback(true);
		}
	}

	// Use this to check gps and show custom message depending on success and errorCode <bool, int>
	public IEnumerator ValidatePermissionsGPSCoroutine(Action<bool, int> Callback)
	{

		/************************ Permission location granted? ************************/

		bool hasPermissionLocation = true;
		if (!HasPermissionLocation())
		{

			hasPermissionLocation = false;
			yield return StartCoroutine(
				RequestLocationPermissionCoroutine((bool success) => {
					hasPermissionLocation = success;
				})
			);

#if UNITY_EDITOR
			if (!editorLocationPermissionGranted)
			{
				Callback(false, 1);
				yield break;
			}
#else
			if( !hasPermissionLocation ){
				Callback(false, 1);				
				yield break;
			}
#endif
		}



		if (Application.platform == RuntimePlatform.Android)
		{
			/************************ Location services enabled? ************************/
			try
			{

				AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

				AndroidJavaClass jc = new AndroidJavaClass("de.dieetagen.tools.Tools");
				bool locationServicesEnabled = jc.CallStatic<bool>("isLocationServicesEnabled", context);

				if (!locationServicesEnabled)
				{

					Callback(false, 2);
					yield break;
				}
				else
				{

				}

			}
			catch (UnityException e)
			{

				print(e.Message);
			}

		}

		Callback(true, 0);
	}

	public IEnumerator ValidatePermissionsCameraCoroutine(string requestType, Action<bool> Callback)
	{

		bool iOSPermissionCameraAsked = false;

#if UNITY_IOS && !UNITY_EDITOR

		string iOSPermissionStatus = HasCameraPermission();
		if(iOSPermissionStatus != "AVAuthorizationStatusNotDetermined"){ iOSPermissionCameraAsked = true; }
		print("ValidatePermissionsCameraCoroutine iOS " + iOSPermissionStatus);
#endif

		if (!HasPermissionCamera() &&
		(
			PlayerPrefs.GetInt("cameraPermissionPermanentlyDenied") == 1 ||
			//PlayerPrefs.GetInt("iOSPermissionCameraAsked") == 1
			iOSPermissionCameraAsked
		)
		)
		{

			InfoController.instance.ShowCommitAbortDialog("Kamerafreigabe", "Du musst in den Einstellungen den Zugriff auf Deine Kamera erteilen, damit Du die Augmented-Reality-Funktion nutzen kannst.", DirectToAppSettings);
			Callback(false);
			yield break;
		}

		bool hasPermission = false;
		int errorCode = 0;
		yield return StartCoroutine(
			ValidatePermissionsCameraCoroutine((bool success, int error) => {
				hasPermission = success;
				errorCode = error;
			})
		);

#if UNITY_EDITOR
		//hasPermission = false;
		//errorCode = 2;
#endif

		if (!hasPermission)
		{

			if (errorCode == 1)
			{

				if (requestType == "ticket")
				{

					InfoController.instance.ShowMessage("Der Zugriff auf die Kamera ist erforderlich, damit du dein Ticket scannen kannst.");

				}
				else
				{

					InfoController.instance.ShowMessage(
						"Kamerafreigabe", "Bitte erlaube der App den Zugriff auf Deine Kamera, damit Du die Augmented-Reality-Funktion nutzen kannst.", ScanController.instance.StartScan);
				}
			}
			else if (errorCode == -1)
			{
				if (requestType == "ar")
				{
					ScanController.instance.ContinueGuideWithoutAR();
				}
				else if (requestType == "arFeature")
				{
					InfoController.instance.ShowMessage("Augmented Reality", "Dein mobiles Gerät verfügt nicht über die notwendigen Hardwareanforderungen und kann diesen Bereich nicht aufrufen.");
				}
				else
				{
					InfoController.instance.ShowMessage("Augmented Reality", "Dein mobiles Gerät verfügt nicht über die notwendigen Hardwareanforderungen und kann diesen Bereich nicht aufrufen.");
				}
			}
			else if (errorCode == 2)
			{

				if (requestType == "ar")
				{
					InfoController.instance.ShowCommitAbortDialog("Augmented Reality", "\"Augmented Reality\", kurz AR, bedeutet \"erweiterte Realität\" und ergänzt Dein Kamerabild um zusätzliche Elemente. In dieser App werden Stadtführende eingeblendet, die die Stationen anmoderieren. Ohne AR-Technik kannst Du diesen wichtigen Teil der App nicht nutzen.", DownloadARCore, ScanController.instance.ContinueGuideWithoutAR, "Weiter", "Abbrechen");
				}
				else if (requestType == "arFeature")
				{
					DownloadARCore();
				}
				else
				{

				}
			}

			Callback(false);
		}
		else
		{
			Callback(true);
		}
	}

	public IEnumerator ValidatePermissionsCameraCoroutine(Action<bool, int> Callback)
	{

		/************************ Permission camera granted? ************************/
		bool hasPermissionCamera = true;
		if (!HasPermissionCamera())
		{

			hasPermissionCamera = false;
			yield return StartCoroutine(
				RequestCameraPermissionCoroutine((bool success) => {
					hasPermissionCamera = success;
				})
			);

#if UNITY_EDITOR
			if (!editorCameraEnabled)
			{
				Callback(false, 1);
				yield break;
			}
#else
			if( !hasPermissionCamera ){			
				Callback(false, 1);
				yield break;
			}
#endif
		}

		if (Application.platform == RuntimePlatform.Android)
		{

			/************************ ARCore installed? ************************/
			print("Checking ARCore installed...");
			bool markerlessAvailable = false;
			int arSessionCode = -1;
			yield return StartCoroutine(
				CheckingMarkerlessAvailabilityCoroutine((bool success, int arSessionState) => {
					markerlessAvailable = success;
					arSessionCode = arSessionState;
				})
			);

			print("CheckingMarkerlessAvailabilityCoroutine " + markerlessAvailable + " " + arSessionCode);

			if (!markerlessAvailable)
			{

				if (arSessionCode == 0)
				{

					//InfoController.instance.ShowMessage("Dein mobiles Gerät verfügt nicht über die notwendigen Hardwareanforderungen und funktioniert möglicherweise nicht richtig.");
					Callback(false, -1);
					yield break;
				}
				else
				{

					if (PlayerPrefs.GetInt("ARCoreInstalled", 0) != 1 && PlayerPrefs.GetInt("ARCoreWasInstalled", 0) != 1)
					{

						Callback(false, 2);
						yield break;
					}
				}
			}
		}

		Callback(true, 0);
	}

	public IEnumerator ValidatePermissionsMicrophoneCoroutine(Action<bool> Callback)
	{

		bool iOSPermissionMicrophoneAsked = false;

#if UNITY_IOS && !UNITY_EDITOR

		string iOSPermissionStatus = HasMicrophonePermission();
		if(iOSPermissionStatus != "AVAuthorizationStatusNotDetermined"){ iOSPermissionMicrophoneAsked = true; }
		
		print("ValidatePermissionsMicrophoneCoroutine iOS " + iOSPermissionStatus);
#endif

		if (!HasPermissionMicrophone() &&
		(
			PlayerPrefs.GetInt("microphonePermissionPermanentlyDenied") == 1 ||
			//PlayerPrefs.GetInt("iOSPermissionMicrophonAsked") == 1
			iOSPermissionMicrophoneAsked
		)
		)
		{

			InfoController.instance.ShowCommitAbortDialog("Du musst in den Einstellungen den Zugriff auf das Mikrofon erteilen, um alle Funktionen dieser Station nutzen zu können.", DirectToAppSettings);
			Callback(false);
			yield break;
		}

		bool hasPermission = false;
		int errorCode = 0;
		yield return StartCoroutine(
			ValidatePermissionsMicrophoneCoroutine((bool success, int error) => {
				hasPermission = success;
				errorCode = error;
			})
		);

		if (!hasPermission)
		{

			if (errorCode == 1)
			{

				//InfoController.instance.ShowMessage("Um alle Funktionen dieser Station zu nutzen, ist der Zugriff auf das Mikrofon erforderlich.");
				InfoController.instance.ShowInfo("Um alle Funktionen dieser Station zu nutzen, ist der Zugriff auf das Mikrofon erforderlich.", 5.0f);
			}

			Callback(false);
		}
		else
		{
			Callback(true);
		}
	}

	public IEnumerator ValidatePermissionsMicrophoneCoroutine(Action<bool, int> Callback)
	{

		if (!HasPermissionMicrophone())
		{

			bool hasPermission = false;
			yield return StartCoroutine(
				RequestMicrophonPermissionCoroutine((bool success) => {
					hasPermission = success;
				})
			);

#if UNITY_EDITOR
			if (!editorMicrophoneEnabled)
			{
				Callback(false, 1);
				yield break;
			}
#else
			if( !hasPermission ){			
				Callback(false, 1);
				yield break;
			}
#endif
		}

		Callback(true, 0);
	}

	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*####################                                                         ######################*/
	/*********************          Functions to check misc permissions            ***********************/
	/*####################                                                         ######################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/

	public bool HasAndroidPermission(string permissionID)
	{

#if UNITY_ANDROID && !UNITY_EDITOR
		return Permission.HasUserAuthorizedPermission(permissionID);
#endif

		return false;
	}

	public void RequestAndroidPermission(string permissionID)
	{
#if UNITY_ANDROID
		Permission.RequestUserPermission(permissionID);
#endif
	}

	public IEnumerator RequestLocationPermissionCoroutine(Action<bool> Callback)
	{

#if UNITY_ANDROID

		//RequestAndroidPermission( "android.permission.ACCESS_COARSE_LOCATION" );
		RequestAndroidPermission("android.permission.ACCESS_FINE_LOCATION");

#if !UNITY_EDITOR
		yield return new WaitForSeconds( 1f );
#else
		yield return null;
#endif

		if (HasPermissionLocation())
		{
			Callback(true);
		}
		else
		{
			Callback(false);
		}

#elif UNITY_IOS && !UNITY_EDITOR
		
		if( PlayerPrefs.GetString("PermissionLocationAsked") == "true" ){
			
			waitingForCallback = true;
			RequestPermissionSettings( "PermissionController",
				LanguageController.GetTranslation("Die App möchte auf deinen Standort zugreifen"),
				LanguageController.GetTranslation("Der Zugriff auf den Standort ist erforderlich, damit wir deine Position anzeigen können."),
				LanguageController.GetTranslation("Ok"),
				LanguageController.GetTranslation("Abbrechen"),
				"PermissionCallback"
			);
			
			float timer = 60;
			while( waitingForCallback && timer > 0 ){				
				timer -= Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}	
		}
		else{
			
			// Own native location permission request (see iOSToolsController.mm)
			// Callback: OnLocationPermissionRequestCompleted (see iOSToolsController.mm)
			
			if( LocationServicesEnabled() != "true" ){
			
				if( PlayerPrefs.GetString("PermissionLocationServicesAsked") == "true" ){
				
					waitingForCallback = true;
					RequestPermissionSettings( "PermissionController",
						LanguageController.GetTranslation("Die App möchte auf deinen Standort zugreifen"),
		LanguageController.GetTranslation("Du musst die Ortungsdienste aktivieren. Der Zugriff auf den Standort ist erforderlich, damit wir deine Position anzeigen können."),
						LanguageController.GetTranslation("Ok"),
						LanguageController.GetTranslation("Abbrechen"),
						"PermissionCallback"
					);
			
					float timer = 60;
					while( waitingForCallback && timer > 0 ){				
						timer -= Time.deltaTime;
						yield return new WaitForEndOfFrame();
					}	
				}
				else{
				
					waitingForCallback = true;
					RequestLocationPermission();
			
					float timer = 60;
					while( waitingForCallback && timer > 0 ){				
						timer -= Time.deltaTime;
						yield return new WaitForEndOfFrame();
					}

					yield return new WaitForSeconds(0.2f);
					PlayerPrefs.SetString("PermissionLocationServicesAsked", "true");
				}
			}
			else{

				waitingForCallback = true;
				RequestLocationPermission();
				print("waitingForCallback " + waitingForCallback);
				
				float timer = 60;
				while( waitingForCallback && timer > 0 ){				
					timer -= Time.deltaTime;
					yield return new WaitForEndOfFrame();
				}
	
				yield return new WaitForSeconds(0.2f);
				
				PlayerPrefs.SetString("PermissionLocationAsked", "true");					
			}
			
		}
		
		if ( HasLocationPermission() == "true" )
		{
			print("Location permission granted");
			Callback(true);
		}
		else
		{
			print("Location permission not granted");
			Callback(false);
		}
		
#else
		
		yield return new WaitForEndOfFrame();
		Callback( true );
		
#endif
	}

	public IEnumerator RequestLocationPermissionCoroutine(Action<bool, int> Callback)
	{

#if UNITY_ANDROID && !UNITY_EDITOR
		
		if( PlayerPrefs.GetInt("locationPermissionPermanentlyDenied") == 1 ){
				
			Callback(false, 3);
			yield  break;
		}
		
		requestingPermission = true;
		
		var callbacks = new PermissionCallbacks();
		callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
		callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
		callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
		Permission.RequestUserPermission(Permission.FineLocation, callbacks);
		
		float timer = 3;
		while( timer > 0 && requestingPermission ){
			
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		
		if( permissionState == 0 ){
			
			PlayerPrefs.SetInt("locationPermissionPermanentlyDenied", 0);
			Callback(true, 0);
		}
		else if( permissionState == 1 ){
			PlayerPrefs.SetInt("locationPermissionPermanentlyDenied", 0);
			Callback(false, 1);
		}
		else{
			PlayerPrefs.SetInt("locationPermissionPermanentlyDenied", 1);
			Callback(false, 2);
		}
		
		print("CheckLocationPermissionCoroutine done " + permissionState);
		
#elif UNITY_IOS && !UNITY_EDITOR
		
		print( "iOSPermissionLocationAsked 1 " + PlayerPrefs.GetString("iOSPermissionLocationAsked") );
		
		if( PlayerPrefs.GetString("iOSPermissionLocationAsked") == "true" ){
			
			Callback(false, 3);
			yield break;
		}
		else{
			
			PlayerPrefs.SetString("iOSPermissionLocationAsked", "true");
			PlayerPrefs.Save();
			print( "iOSPermissionLocationAsked 1 " + PlayerPrefs.GetString("iOSPermissionLocationAsked") );

			waitingForCallback = true;
			RequestLocationPermission();
			
			float timer = 60;
			while( waitingForCallback && timer > 0 ){				
				timer -= Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			yield return new WaitForSeconds(0.2f);			

		}
		
		if ( HasLocationPermission() == "true" )
		{
			print("Location permission granted");
			Callback(true, 0);
		}
		else
		{
			print("Location permission not granted");
			Callback(false, 1);
		}
		
#else

		yield return new WaitForEndOfFrame();
		Callback(editorLocationPermissionGranted, 0);

#endif
	}

	internal void PermissionCallbacks_PermissionGranted(string permissionName)
	{
		Debug.Log("PermissionCallbacks_PermissionGranted " + permissionName);
		requestingPermission = false;
		permissionState = 0;
	}

	internal void PermissionCallbacks_PermissionDenied(string permissionName)
	{
		Debug.Log("PermissionCallbacks_PermissionDenied " + permissionName);
		requestingPermission = false;
		permissionState = 1;
	}

	internal void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
	{
		Debug.Log("PermissionCallbacks_PermissionDeniedAndDontAskAgain " + permissionName);
		requestingPermission = false;
		permissionState = 2;
	}

	public void DirectToAppSettings()
	{

		try
		{
			print("DirectToAppSettings");

#if UNITY_ANDROID && !UNITY_EDITOR
			
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			//AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
				
			AndroidJavaClass jc = new AndroidJavaClass("de.dieetagen.tools.Tools");	
			jc.CallStatic("openAppDetailSettings", activity, Application.identifier);
			
#elif UNITY_IOS && !UNITY_EDITOR
			ForwardToAppSettings();
#endif
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	public void DirectToLocationServiceSettings()
	{

		try
		{

			print("DirectToLocationServiceSettings");

#if UNITY_ANDROID && !UNITY_EDITOR
			
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			//AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
				
			AndroidJavaClass jc = new AndroidJavaClass("de.dieetagen.tools.Tools");	
			jc.CallStatic("setLocationServicesEnabled", activity);
			
#elif UNITY_IOS && !UNITY_EDITOR
			
			ForwardToLocationSettings();
			
#endif


		}
		catch (UnityException e)
		{

			print(e.Message);
		}
	}

	public IEnumerator RequestCameraPermissionCoroutine(Action<bool> Callback)
	{

#if UNITY_ANDROID

		requestingPermission = true;

		var callbacks = new PermissionCallbacks();
		callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
		callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
		callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
		Permission.RequestUserPermission(Permission.Camera, callbacks);

		float timer = 3;
		while (timer > 0 && requestingPermission)
		{

			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		if (permissionState == 0)
		{

			PlayerPrefs.SetInt("cameraPermissionPermanentlyDenied", 0);
			Callback(true);
		}
		else if (permissionState == 1)
		{
			PlayerPrefs.SetInt("cameraPermissionPermanentlyDenied", 0);
			Callback(false);
		}
		else
		{
			PlayerPrefs.SetInt("cameraPermissionPermanentlyDenied", 1);
			Callback(false);
		}

		print("RequestCameraPermissionCoroutine done " + permissionState);

		/*
		RequestAndroidPermission( "android.permission.CAMERA" );

		#if !UNITY_EDITOR
		yield return new WaitForSeconds( 1f );
		#else
		yield return null;
		#endif

		if( HasPermissionCamera() ){
			Callback(true);
		}else{
			Callback(false);
		}
		*/

#elif UNITY_IOS && !UNITY_EDITOR
		
		if( PlayerPrefs.GetInt("iOSPermissionCameraAsked") == 1 ){
			
			waitingForCallback = true;
			RequestPermissionSettings( "PermissionController",
			LanguageController.GetTranslation("Die App möchte auf deine Kamera zugreifen"),
			LanguageController.GetTranslation("Der Zugriff auf die Kamera ist erforderlich, damit Sie die Augmented Reality Funktion nutzen können."),
			LanguageController.GetTranslation("Ok"),
			LanguageController.GetTranslation("Abbrechen"),
			"PermissionCallback"
			);
				
			while( waitingForCallback ){
				yield return new WaitForEndOfFrame();
			}	
		}
		else{
			
			// For some reason unitys solution is not working:
			//yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
				
			// Own native camera permission request (see iOSToolsController.mm)
			// Callback: OnCameraPermissionRequestCompleted (see iOSToolsController.mm)			
			waitingForCallback = true;
			RequestCameraPermission();
				
			while( waitingForCallback ){ yield return new WaitForEndOfFrame(); }	
	
			yield return new WaitForSeconds(0.2f);
			//PlayerPrefs.SetInt("iOSPermissionCameraAsked", 1);
		}
		
		
		if ( HasCameraPermission() == "true" )
		{
			print("Camera permission granted");
			Callback(true);
		}
		else
		{
			print("Camera permission not granted");
			Callback(false);
		}
		
#else
		
		yield return new WaitForEndOfFrame();
		Callback( true );
		
#endif

	}

	public void OnCameraPermissionRequestCompleted(string granted)
	{

		print("OnCameraPermissionRequestCompleted");
		waitingForCallback = false;
	}

	public IEnumerator RequestMicrophonPermissionCoroutine(Action<bool> Callback)
	{

#if UNITY_ANDROID && !UNITY_EDITOR
		
		requestingPermission = true;

		var callbacks = new PermissionCallbacks();
		callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
		callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
		callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
		Permission.RequestUserPermission(Permission.Microphone, callbacks);

		float timer = 3;
		while( timer > 0 && requestingPermission ){
	
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		if( permissionState == 0 ){
	
			PlayerPrefs.SetInt("microphonePermissionPermanentlyDenied", 0);
			Callback(true);
		}
		else if( permissionState == 1 ){
			PlayerPrefs.SetInt("microphonePermissionPermanentlyDenied", 0);
			Callback(false);
		}
		else{
			PlayerPrefs.SetInt("microphonePermissionPermanentlyDenied", 1);
			Callback(false);
		}

		print("RequestCameraPermissionCoroutine done " + permissionState);
		
		/*
		RequestAndroidPermission( "android.permission.RECORD_AUDIO" );
		
#if !UNITY_EDITOR
		yield return new WaitForSeconds( 1f );
#else
		yield return null;
#endif

		if( HasPermissionMicrophone() ){
			Callback(true);
		}else{
			Callback(false);
		}
		*/
		
#elif UNITY_IOS && !UNITY_EDITOR
		
		if( PlayerPrefs.GetInt("iOSPermissionMicrophonAsked") == 1 ){

			yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

			/*
			waitingForCallback = true;
			RequestPermissionSettings( "PermissionController",
				LanguageController.GetTranslation("Die App möchte auf deine Mikrofon zugreifen"),
				LanguageController.GetTranslation("Der Zugriff auf das Mikrofon ist erforderlich, damit du diese Station durchführen kannst."),
				LanguageController.GetTranslation("Ok"),
				LanguageController.GetTranslation("Abbrechen"),
				"PermissionCallback"
			);
			
			while( waitingForCallback ){
				yield return new WaitForEndOfFrame();
			}	
			*/
			
		}
		else{
			
			yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
			
			/*
			waitingForCallback = true;
			RequestMicrophonPermission();
			
			while( waitingForCallback ){
				yield return new WaitForEndOfFrame();
			}	

			yield return new WaitForSeconds(0.2f);
			*/
			
			//PlayerPrefs.SetInt("iOSPermissionMicrophonAsked", 1);
		}
		
		if ( HasPermissionMicrophone() )
		{
			Callback(true);
		}
		else
		{
			Callback(false);
		}
		
#else

		yield return new WaitForEndOfFrame();
		Callback(editorMicrophoneEnabled);

#endif

	}

	public void OnLocationPermissionRequestCompleted(string granted)
	{

		print("OnLocationPermissionRequestCompleted");
		waitingForCallback = false;
	}

	public IEnumerator RequestPhotoLibraryPermissionCoroutine(Action<bool> Callback)
	{

#if UNITY_ANDROID

		RequestAndroidPermission("android.permission.WRITE_EXTERNAL_STORAGE");
		yield return new WaitForSeconds(0.2f);

		if (HasPermissionSavePhotos())
		{
			Callback(true);
		}
		else
		{
			Callback(false);
		}

#elif UNITY_IOS && !UNITY_EDITOR
		
		waitingForCallback = true;
		if( PermissionStatusSavePhotosIOS() == "PHAuthorizationStatusNotDetermined" ){
		RequestPermissionPhotoLibrary("PermissionController", "PermissionCallback");
		}
		else{
		RequestPermissionSettings( "PermissionController",
		LanguageController.GetTranslation("Die App möchte auf deine Fotogalerie zugreifen"),
		LanguageController.GetTranslation("Um diese Funktion nutzen zu können, ist der Zugriff auf die Fotogalerie erforderlich."),
		LanguageController.GetTranslation("Ok"),
		LanguageController.GetTranslation("Abbrechen"),
		"PermissionCallback"
		);
		}
		
		while( waitingForCallback ){
		yield return new WaitForEndOfFrame();
		}
		
		if ( HasPermissionSavePhotos() )
		{
		Callback(true);
		}
		else
		{
		Callback(false);
		}
		
#else
		yield return new WaitForEndOfFrame();
		Callback( true );
#endif

	}

	public void PermissionCallback(string permissionWasGranted)
	{
		print("PermissionCallback");

		waitingForCallback = false;

		if (permissionWasGranted == "true")
		{

		}
		else
		{

		}
	}

	void OnApplicationPause(bool paused)
	{
		if (!paused)
		{
			//print("OnApplicationPause false");
			waitingForCallback = false;
		}
	}

	// Permission to use location
	public bool HasPermissionLocation()
	{

#if UNITY_ANDROID && !UNITY_EDITOR
		//return Permission.HasUserAuthorizedPermission( "android.permission.ACCESS_COARSE_LOCATION" );
		return Permission.HasUserAuthorizedPermission( "android.permission.ACCESS_FINE_LOCATION" );
#elif UNITY_IOS && !UNITY_EDITOR
		return HasLocationPermission() == "true";
#endif

#if UNITY_EDITOR
		return editorLocationPermissionGranted;
#endif

		return false;
	}

	// Permission to use location
	public string GetPermissionLocationStatus()
	{

#if UNITY_IOS && !UNITY_EDITOR
		return HasLocationPermission();
#endif

		return "";
	}

	public bool LocationServiceEnabled()
	{

		//return Input.location.isEnabledByUser;

#if UNITY_ANDROID && !UNITY_EDITOR
		
		try{
				
			print("ANDROID, Checking gps enabled...");			
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
				
			AndroidJavaClass jc = new AndroidJavaClass("de.dieetagen.tools.Tools");	
			bool locationServicesEnabled = jc.CallStatic<bool>("isLocationServicesEnabled", context);		
			return locationServicesEnabled;
				
		}catch( UnityException e ){
				
			print(e.Message);
		}
		
#elif UNITY_IOS && !UNITY_EDITOR
		
		return (LocationServicesEnabled() == "true");
			
#endif

#if UNITY_EDITOR
		return editorLocationServiceEnabled;
#endif

		return false;
	}

	// Permission to use camera
	public bool HasPermissionCamera()
	{

#if UNITY_ANDROID && !UNITY_EDITOR
		return Permission.HasUserAuthorizedPermission( "android.permission.CAMERA" );
#elif UNITY_IOS && !UNITY_EDITOR
		//return Application.HasUserAuthorization(UserAuthorization.WebCam);
		return HasCameraPermission() == "true";
#endif

#if UNITY_EDITOR
		return editorCameraEnabled;
#endif

		return false;
	}

	// Permission to use microphone
	public bool HasPermissionMicrophone()
	{

#if UNITY_ANDROID && !UNITY_EDITOR
		return Permission.HasUserAuthorizedPermission( "android.permission.RECORD_AUDIO" );
#elif UNITY_IOS && !UNITY_EDITOR
		return Application.HasUserAuthorization(UserAuthorization.Microphone);
		//return HasCameraPermission() == "true";
#endif

#if UNITY_EDITOR
		return editorMicrophoneEnabled;
#endif

		return false;
	}

	// Permission to write data like images to device ( e.g. save images to gallery )
	public bool HasPermissionSavePhotos()
	{

		// On Android 13+ we do not need to ask to save photos 
		try
		{
			using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
			{
				int apiLevel = version.GetStatic<int>("SDK_INT");
				print("apiLevel " + apiLevel);

				if (apiLevel >= 33) { return true; }
			}
		}
		catch (Exception e)
		{
			print("Error HasPermissionSavePhotos " + e.Message);
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		
		return Permission.HasUserAuthorizedPermission( "android.permission.WRITE_EXTERNAL_STORAGE" );
		
#elif UNITY_IOS && !UNITY_EDITOR
		
		string status = GetPhotoLibraryAuthorizationStatus();
		if( status == "PHAuthorizationStatusAuthorized" ){
		return true;
		}else if( status == "PHAuthorizationStatusDenied" ){
		return false;
		}else if( status == "PHAuthorizationStatusNotDetermined" ){
		return false;
		}else if( status == "PHAuthorizationStatusRestricted" ){
		return false;
		}
		
#endif

		return false;
	}

	public string PermissionStatusSavePhotosIOS()
	{

#if UNITY_IOS && !UNITY_EDITOR
		return GetPhotoLibraryAuthorizationStatus();
#endif

		return "";
	}

	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*####################                                                         ######################*/
	/********************* Functions to check markerless tracking and load ARCore  ***********************/
	/*####################                                                         ######################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/
	/*###################################################################################################*/

	public IEnumerator CheckingMarkerlessAvailabilityCoroutine(Action<bool, int> Callback)
	{

		print("CheckingMarkerlessAvailabilityCoroutine");

		int state = 0;
		bool checkingDone = false;
		StartCoroutine(
			CheckMarkerlessSupportedByMobile((int arSessionState, string data) => {
				print("ARSessionState " + arSessionState);
				print("data " + data);

				state = arSessionState;
				checkingDone = true;
			})
		);

		float timer = 10;
		while (timer > 0 && !checkingDone)
		{
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

#if UNITY_EDITOR
		state = 0;
#endif

		if (state == 0)
		{                           // markerless tracking available			
			Callback(true, -1);
		}
		else if (state == 1)
		{                       // need to install ARCore

			PlayerPrefs.SetInt("ARCoreInstalled", 0);
			Callback(false, 1);

		}
		else
		{                                       // markerless tracking not available

			Callback(false, 0);
		}
	}

	public void DownloadARCore()
	{

		if (!isLoading)
		{
			isLoading = true;
			StartCoroutine("DownloadARCoreCoroutine");
		}
	}

	public IEnumerator DownloadARCoreCoroutine()
	{

		yield return null;

		int state = 0;
		bool checkingDone = false;
		StartCoroutine(
			InstallARCoreCoroutine((int installStatus, string data) => {
				print("installStatus " + installStatus);
				print("data " + data);

				state = installStatus;
				checkingDone = true;
			})
		);

		float timer = 600;
		while (timer > 0 && !checkingDone)
		{
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		if (state == 0)
		{

			// Success installed
			print("Success installed ARCore");

			PlayerPrefs.SetInt("ARCoreInstalled", 1);
			PlayerPrefs.SetInt("ARCoreWasInstalled", 1);
			//MenuController.instance.ContinueToTutorial();		

		}
		else
		{

			// Failed install
			InfoController.instance.ShowMessage("Augmented Reality", "Google Play-Dienste für AR konnten nicht installiert werden.");
			//InfoController.instance.ShowMessage("AR-Funktion", "Für diese Funktion ist die neueste Version der Google Play-Dienste für AR erforderlich.");
			//InfoController.instance.ShowMessage("Du musst ARCore aus dem Play Store installieren, um die App nutzen zu können.");
		}

		isLoading = false;
	}


	public IEnumerator CheckMarkerlessSupportedByMobile(Action<int, string> Callback)
	{
		print("CheckMarkerlessSupportedByMobile " + ARSession.state.ToString());

		/*
		public enum ARSessionState
		{
			None,
			Unsupported,
			CheckingAvailability,
			NeedsInstall,
			Installing,
			Ready,
			SessionInitializing,
			SessionTracking
		}
		*/

		if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability || PlayerPrefs.GetInt("ARCoreWasInstalled", 0) == 1)
		{
			Debug.Log("Checking AR Availability on mobile device");
			yield return ARSession.CheckAvailability();
		}

		switch (ARSession.state)
		{
			case ARSessionState.None:
				Callback(2, "AR Session - None");
				break;
			case ARSessionState.CheckingAvailability:
				Callback(2, "AR Session - Looking for availability");
				break;
			case ARSessionState.Unsupported:
				Callback(2, "AR Session - not available to this mobile device");
				break;
			case ARSessionState.NeedsInstall:
				Callback(1, "AR Session - needs to be installed in mobile device");
				break;
			case ARSessionState.Installing:
				Callback(2, "AR Session - Installing AR onto mobile device");
				break;
			case ARSessionState.Ready:
				Callback(0, "AR Session - supported by mobile device and ready to fire up");
				break;
			case ARSessionState.SessionInitializing:
				Callback(0, "AR session is initializing");
				break;
			case ARSessionState.SessionTracking:
				Callback(0, "AR Session is tracking");
				break;
			default:
				Callback(2, "AR Session - no switch worked");
				break;
		}
	}

	public XRSessionSubsystem GetSubsystem()
	{
		if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
		{
			var loader = XRGeneralSettings.Instance.Manager.activeLoader;
			if (loader != null)
			{
				return loader.GetLoadedSubsystem<XRSessionSubsystem>();
			}
		}

		return null;
	}

	public IEnumerator InstallARCoreCoroutine(Action<int, string> Callback)
	{
		print("InstallARCoreCoroutine arSession.enabled " + arSession.enabled);

		var subsystem = GetSubsystem();
		if (subsystem == null)
		{

			print("No subsystem, possibly device is not supporting AR");
			Callback(2, "");
			yield break;
		}

		print("subsystem.InstallAsync()");
		var installPromise = subsystem.InstallAsync();
		yield return installPromise;
		var installStatus = installPromise.result;

		print("installStatus " + installStatus.ToString());

		switch (installStatus)
		{
			case SessionInstallationStatus.Success:
				Callback(0, "Success");
				break;
			case SessionInstallationStatus.ErrorUserDeclined:
				Callback(1, "ErrorUserDeclined");
				break;
			default:
				Callback(2, "");
				break;
		}
	}

	public IEnumerator CheckARFoundationSupportedCoroutine()
	{
		yield return null;

		bool ARFoundationSupportChecked = true;
		if (!PlayerPrefs.HasKey("ARFoundationSupported")) { PlayerPrefs.SetInt("ARFoundationSupported", 1); PlayerPrefs.Save(); }
		if (!PlayerPrefs.HasKey("ARFoundationSupportChecked")) { ARFoundationSupportChecked = false; }
		bool supported = PlayerPrefs.GetInt("ARFoundationSupported", 1) == 1;

#if UNITY_EDITOR

		supported = editorARTrackingSupported;
		PlayerPrefs.SetInt("ARFoundationSupported", editorARTrackingSupported ? 1 : 0);
		PlayerPrefs.Save();
		yield return null;

#elif UNITY_ANDROID

		if (Params.supportAllAndroidDevices && !ARFoundationSupportChecked)
		{
			yield return StartCoroutine(
				CheckARFoundationSupportedCoroutine((bool isSupported, ARSessionState state) =>
				{
					print("CheckARFoundationSupportedCoroutine, supported " + isSupported + " state: " + state.ToString());
					PlayerPrefs.SetInt("ARFoundationSupportChecked", 1);
					PlayerPrefs.SetInt("ARFoundationSupported", isSupported?1:0);
					PlayerPrefs.Save();
					supported = isSupported;
				})
			);
		}

#endif

		if (!supported)
		{
			mapScanLabel.text = "";
			//mapScanButtonLabel.text = "";
		}
	}

	public IEnumerator CheckARFoundationSupportedCoroutine(Action<bool, ARSessionState> Callback)
	{
		yield return ARSession.CheckAvailability();
		switch (ARSession.state)
		{
			case ARSessionState.None:
				Callback(false, ARSession.state);
				break;
			case ARSessionState.CheckingAvailability:
				Callback(false, ARSession.state);
				break;
			case ARSessionState.Unsupported:
				Callback(false, ARSession.state);
				break;
			case ARSessionState.NeedsInstall:
				Callback(true, ARSession.state);
				break;
			case ARSessionState.Installing:
				Callback(true, ARSession.state);
				break;
			case ARSessionState.Ready:
				Callback(true, ARSession.state);
				break;
			case ARSessionState.SessionInitializing:
				Callback(true, ARSession.state);
				break;
			case ARSessionState.SessionTracking:
				Callback(true, ARSession.state);
				break;
			default:
				Callback(false, ARSession.state);
				break;
		}
	}

	public bool IsARFoundationSupported()
	{
		return PlayerPrefs.GetInt("ARFoundationSupported", 1) == 1;
	}


#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private string HasCameraPermission();
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private string HasMicrophonePermission();
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private string HasLocationPermission();
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")] 
	extern static private string LocationServicesEnabled();
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private void RequestCameraPermission();
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private void RequestLocationPermission();
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private void RequestPermissionSettings(string gameObject, string title, string message, string okButton, string abortButton, string callback);
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private string GetPhotoLibraryAuthorizationStatus();
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private void RequestPermissionPhotoLibrary(string gameObject, string callback);
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private void ForwardToAppSettings();
#endif

#if UNITY_IOS && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static private void ForwardToLocationSettings();
#endif

}
