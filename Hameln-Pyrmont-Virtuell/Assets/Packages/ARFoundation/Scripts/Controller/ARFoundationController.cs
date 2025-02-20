using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

using TMPro;

using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class ARFoundationController : MonoBehaviour
{
	public ARSession m_Session;
	
	[Space(10)]
	
	public GameObject downloadMenu;
	public GameObject info;
	
	private bool isLoading = false;

	public static ARFoundationController instance;
	void Awake(){
		instance = this;		
		StartCoroutine("CheckingMarkerlessAvailabilityCoroutine");
	}
	
	private IEnumerator CheckingMarkerlessAvailabilityCoroutine(){
		
		int state = 0;
		bool checkingDone = false;
		StartCoroutine(
				CheckMarkerlessSupportedByMobile((int arSessionState, string data) => {                
				print( "ARSessionState " + arSessionState );
				print( "data " + data );
				
				state = arSessionState;
				checkingDone = true;
			})
		);
		
		float timer = 10;
		while( timer > 0 && !checkingDone){
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		
		#if UNITY_EDITOR
		state = 0;
		#endif
		
		if( state == 0 ){							// markerless tracking available
			
			SceneManager.LoadScene(1);
			
		}else if( state == 1 ){						// need to install ARCore
			
			#if UNITY_ANDROID
			downloadMenu.SetActive(true);
			#else

			#endif
		}else {										// markerless tracking not available
			
			// Todo: Show message
			info.SetActive(true);

		}
	}
	
	public void DownloadARCore(){
		
		if (!isLoading) {
			isLoading = true;
			StartCoroutine( "DownloadARCoreCoroutine" );
		}
	}

	public IEnumerator DownloadARCoreCoroutine(){
		
		int state = 0;
		bool checkingDone = false;
		StartCoroutine(
			InstallARCoreCoroutine((int installStatus, string data) => {                
				print( "installStatus " + installStatus );
				print( "data " + data );
				
				state = installStatus;
				checkingDone = true;
			})
		);
		
		float timer = 600;
		while( timer > 0 && !checkingDone){
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}	
		
		if( state == 0 ){
			// Success installed
			downloadMenu.SetActive(false);
			SceneManager.LoadScene(1);

		}else {
			// Failed install
			downloadMenu.SetActive(false);
			info.SetActive(true);
		}
		
		isLoading = false;
	}

		
	public IEnumerator CheckMarkerlessSupportedByMobile( Action<int, string> Callback )
	{ 
		if ((ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability))
		{
			Debug.Log("Checking AR Availability on mobile device");
			yield return ARSession.CheckAvailability();
		}
		
		switch (ARSession.state)
		{
		case ARSessionState.None:
			Callback( 2, "AR Session - None");
			break;
		case ARSessionState.CheckingAvailability:
			Callback( 2, "AR Session - Looking for availability");
			break;
		case ARSessionState.Unsupported:
			Callback( 2, "AR Session - not available to this mobile device");
			break;
		case ARSessionState.NeedsInstall:
			Callback( 1, "AR Session - needs to be installed in mobile device");
			break;
		case ARSessionState.Installing:
			Callback( 2, "AR Session - Installing AR onto mobile device");
			break;
		case ARSessionState.Ready:
			Callback( 0, "AR Session - supported by mobile device and ready to fire up");
			break;
		case ARSessionState.SessionInitializing:
			Callback( 0, "AR session is initializing");
			break;
		case ARSessionState.SessionTracking:
			Callback( 0, "AR Session is tracking");
			break;
		default:
			Callback( 2, "AR Session - no switch worked");
			break;
		}
	}
	
	public IEnumerator InstallARCoreCoroutine( Action<int, string> Callback )
	{
		var installPromise = m_Session.subsystem.InstallAsync();
		yield return installPromise;
		var installStatus = installPromise.result;

		switch (installStatus)
		{
		case SessionInstallationStatus.Success:
			Callback( 0, "Success");
			break;
		case SessionInstallationStatus.ErrorUserDeclined:
			Callback( 1, "ErrorUserDeclined");
			break;
		default:
			Callback( 2, "");
			break;
		}
	}
}
