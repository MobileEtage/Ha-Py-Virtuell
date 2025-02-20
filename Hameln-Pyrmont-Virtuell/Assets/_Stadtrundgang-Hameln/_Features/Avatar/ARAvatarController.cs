using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using SimpleJSON;
using UnityEngine.Networking;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ARAvatarController : MonoBehaviour
{
	public bool canMoveAvatar = false;
	public bool shouldAddSalsa = false;

	[Space( 20 )]

	public GameObject avatar;
	public SkinnedMeshRenderer avatarHeadRenderer;
	public GameObject arObjectRoot;
	public AudioSource audioSource;
	public SpeechAnimator speechAnimator;
	public EyesBlink eyesBlink;
	public GameObject mainCamera;

	[Space( 20 )]

	public List<CrazyMinnow.SALSA.Salsa> salsaSettings = new List<CrazyMinnow.SALSA.Salsa>();

	private Vector3 hitOffset = Vector3.zero;
	private bool isMovingARObject = false;
	private bool isLoading = false;
	private bool audioWasPlaying = false;

	private bool placementInfoARShowed = false;
	private bool placementInfoSelfieShowed = false;
	private bool showInfoOnlyOnce = true;
	private Vector3 lightRotation = new Vector3( 45, -10, 0 );

	private CrazyMinnow.SALSA.Salsa salsa;

	public static ARAvatarController instance;
	void Awake()
	{
		instance = this;
	}

	void LateUpdate()
	{
		if ( SiteController.instance != null && SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "ARAvatarSite" ) return;

		if ( VideoCaptureController.instance != null && PhotoCaptureController.instance != null )
		{
			if ( VideoCaptureController.instance.previewUI.activeInHierarchy || PhotoCaptureController.instance.previewUI.activeInHierarchy )
			{
				if ( audioSource.isPlaying ) { audioWasPlaying = true; audioSource.Pause(); }
			}
			else
			{
				if ( !audioSource.isPlaying && audioWasPlaying ) { audioSource.Play(); audioWasPlaying = false; }
			}
		}

		if ( canMoveAvatar ) { MoveARObject(); }
	}

	public void MoveARObject()
	{
		if ( Input.touchCount >= 2 ) { isMovingARObject = false; return; }

		if ( Input.GetMouseButtonDown( 0 ) && !ToolsController.instance.IsPointerOverUIObject() )
		{
			RaycastHit[] hits;
			Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay( Input.mousePosition );
			hits = Physics.RaycastAll( ray, 100 );

			for ( int i = 0; i < hits.Length; i++ )
			{
				if ( hits[i].transform == arObjectRoot.transform.GetChild( 0 ) )
				{
					hitOffset = hits[i].point - arObjectRoot.transform.position;
					isMovingARObject = true;
					break;
				}
			}

			// Start drag object delayed, because maybe we also want to scale it with two fingers
			//StopCoroutine("EnableMoveARObjectCoroutine");
			//StartCoroutine("EnableMoveARObjectCoroutine");
		}
		else if ( isMovingARObject && Input.GetMouseButton( 0 ) )
		{
			Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
			Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;

			bool hitGround = false;
			if ( ARController.instance != null && ARController.instance.RaycastHit( touchPosition, out hitPosition ) )
			{
				hitGround = true;
			}
			else
			{

#if UNITY_EDITOR

				Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay( touchPosition );
				RaycastHit[] hits;
				hits = Physics.RaycastAll( ray, 100 );

				for ( int i = 0; i < hits.Length; i++ )
				{
					if ( hits[i].transform.CompareTag( "ARPlane" ) )
					{
						hitPosition = hits[i].point;
						hitGround = true;
						break;
					}
				}
#endif

				if ( !hitGround )
				{
					Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay( Input.mousePosition );
					hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;
				}
			}

			//arObject.transform.position = hitPosition - hitOffset;
			arObjectRoot.transform.position = hitPosition;
		}
		else if ( Input.GetMouseButtonUp( 0 ) )
		{
			StopCoroutine( "EnableMoveARObjectCoroutine" );
			isMovingARObject = false;
		}
	}

	public IEnumerator EnableMoveARObjectCoroutine()
	{
		yield return new WaitForSeconds( 0.15f );
		isMovingARObject = true;
	}

	public IEnumerator InitCoroutine()
	{
		if(ARController.instance != null ) { mainCamera = ARController.instance.mainCamera; }

		JSONNode featureData = StationController.instance.GetStationFeature( "avatarGuide" );
		if ( featureData == null ) yield break;
		yield return StartCoroutine(LoadAudioCoroutine( featureData ) );

		InfoController.instance.loadingCircle.SetActive( true );
		if ( PermissionController.instance.IsARFoundationSupported() && !ARController.instance.arSession.enabled )
		{
			ARController.instance.InitARFoundation();
			yield return new WaitForSeconds( 0.5f );
		}

		arObjectRoot.SetActive( true );
		UpdatePosition();
		arObjectRoot.transform.LookAt( mainCamera.transform );
		arObjectRoot.transform.eulerAngles = new Vector3( 0, arObjectRoot.transform.eulerAngles.y + 180, 0 );
		if ( shouldAddSalsa ) { UpdateLipSync(); }

		if ( LightController.instance != null )
		{
			lightRotation = LightController.instance.directionalLight.transform.eulerAngles;
			LightController.instance.directionalLight.transform.position = Camera.main.transform.position;
			LightController.instance.directionalLight.transform.LookAt( arObjectRoot.transform );
			LightController.instance.directionalLight.transform.eulerAngles = new Vector3( 45, LightController.instance.directionalLight.transform.eulerAngles.y+30, 0 );
			DynamicGI.UpdateEnvironment();
		}

		if ( SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "ARAvatarSite" )
		{
			yield return StartCoroutine( SiteController.instance.SwitchToSiteCoroutine( "ARAvatarSite" ) );
			ARMenuController.instance.DisableMenu( true );
		}

		InfoController.instance.loadingCircle.SetActive( false );

		yield return new WaitForSeconds(1.0f);
		if ( audioSource.clip != null ) { audioSource.Play(); }
	}

	public void UpdatePosition()
	{
		Vector3 dir = mainCamera.transform.forward;
		dir.y = 0;
		Vector3 pos = new Vector3( mainCamera.transform.position.x, ScanController.instance.currentMarkerPosition.y, mainCamera.transform.position.z );


		arObjectRoot.transform.position = pos + dir.normalized * 1;

		if ( !PermissionController.instance.IsARFoundationSupported() )
		{
			Vector3 targetPosition = mainCamera.transform.position + new Vector3( mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z ).normalized * 2.0f;
			targetPosition.y = mainCamera.transform.position.y - 1.4f;
			arObjectRoot.transform.position = targetPosition;
		}
		else
		{
			ARController.instance.navArrow.UpdateFocusObject( arObjectRoot.transform );
		}
	}

	public IEnumerator LoadAudioCoroutine(JSONNode featureData)
	{
		yield return null;
		if ( speechAnimator != null ) { speechAnimator.shouldPlay = false; }

		if(featureData["audioURL"] != null && featureData["audioURL"].Value != "" )
		{
			print( "Downloading audio file " + featureData["audioURL"].Value );

			string path = DownloadContentController.instance.GetAudioFile( featureData["audioURL"] );
			AudioType audioType = AudioType.MPEG;
			if ( path.EndsWith( "wav" ) ) { audioType = AudioType.WAV; }

			print( "UnityWebRequestMultimedia " + path + " " + audioType );

			UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip( path, audioType );
			yield return req.SendWebRequest();
			if ( req.isNetworkError || req.isHttpError ) Debug.Log( "LoadAudioCoroutine error " + req.error );

			print( "UnityWebRequestMultimedia1" );

			if ( !File.Exists( path ) && req.downloadHandler != null && req.downloadHandler.data != null )
			{
				string savePath = ToolsController.instance.GetSavePathFromURL( path, -1 );
				File.WriteAllBytes( savePath, req.downloadHandler.data );
			}

			if ( req.downloadHandler != null && req.downloadHandler.data != null )
			{
				AudioClip clip = DownloadHandlerAudioClip.GetContent( req );
				if ( clip != null ) { audioSource.clip = clip; }
			}
		}
		else if ( featureData["audio"] != null && featureData["audio"].Value != "")
		{
			string audioFile = featureData["audio"].Value;
			AudioClip clip = Resources.Load<AudioClip>( audioFile );
			if ( clip != null ) { audioSource.clip = clip; }
		}
	}

	public void UpdateLipSync()
	{
		float blendShapeFactor = 1.0f;

		GameObject head = ToolsController.instance.FindGameObjectByName( avatar, "Head" );
		SkinnedMeshRenderer smr = GetHeadSkinnedMeshRenderer( avatar );
		if ( smr == null || head == null ) return;
		if ( !smr.gameObject.activeInHierarchy || !head.activeInHierarchy ) return;

		// SALSA
		// API: https://crazyminnowstudio.com/docs/salsa-lip-sync/modules/salsa/api-example/

		/*
		if ( smr.gameObject.GetComponent<CrazyMinnow.SALSA.Salsa>() != null )
		{
			smr.gameObject.GetComponent<CrazyMinnow.SALSA.Salsa>().enabled = true;
			salsa = smr.gameObject.GetComponent<CrazyMinnow.SALSA.Salsa>();
			salsa.audioSrc = audioSource;
		}
		*/


		// Runtime generation seems not to work on iOS
		if ( smr.gameObject.GetComponent<CrazyMinnow.SALSA.Salsa>() == null ) { smr.gameObject.AddComponent<CrazyMinnow.SALSA.Salsa>(); }
		if ( smr.gameObject.GetComponent<CrazyMinnow.SALSA.QueueProcessor>() == null ) { smr.gameObject.AddComponent<CrazyMinnow.SALSA.QueueProcessor>(); }
		salsa = smr.gameObject.GetComponent<CrazyMinnow.SALSA.Salsa>();
		salsa.enabled = true;

		salsa.queueProcessor = smr.gameObject.GetComponent<CrazyMinnow.SALSA.QueueProcessor>();
		salsa.audioSrc = audioSource;
		salsa.visemes.Clear();

		// 1

		salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "saySmall", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
		var saySmallViseme = salsa.visemes[0].expData;
		saySmallViseme.components[0].name = "saySmall component";
		saySmallViseme.controllerVars[0].smr = smr;
		saySmallViseme.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthOpen" );
		saySmallViseme.controllerVars[0].minShape = 0f;
		saySmallViseme.controllerVars[0].maxShape = 0.33f * blendShapeFactor;
		
		// 2
		salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "sayMedium", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
		var sayMediumViseme = salsa.visemes[1].expData;
		sayMediumViseme.components[0].name = "sayMedium component";
		sayMediumViseme.controllerVars[0].smr = smr;
		sayMediumViseme.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthOpen" );
		sayMediumViseme.controllerVars[0].minShape = 0f;
		sayMediumViseme.controllerVars[0].maxShape = 0.66f * blendShapeFactor;

		// 3
		salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "sayBig", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
		var sayBigViseme = salsa.visemes[2].expData;
		sayBigViseme.components[0].name = "sayMedium component";
		sayBigViseme.controllerVars[0].smr = smr;
		sayBigViseme.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthOpen" );
		sayBigViseme.controllerVars[0].minShape = 0f;
		sayBigViseme.controllerVars[0].maxShape = 1f * blendShapeFactor;

		// 1  
		salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "saySmall2", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
		var saySmallViseme2 = salsa.visemes[3].expData;
		saySmallViseme2.components[0].name = "saySmall2 component";
		saySmallViseme2.controllerVars[0].smr = smr;
		saySmallViseme2.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthFunnel" );
		saySmallViseme2.controllerVars[0].minShape = 0f;
		saySmallViseme2.controllerVars[0].maxShape = 0.1f * blendShapeFactor;

		// 2
		salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "sayMedium2", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
		var sayMediumViseme2 = salsa.visemes[4].expData;
		sayMediumViseme2.components[0].name = "sayMedium2 component";
		sayMediumViseme2.controllerVars[0].smr = smr;
		sayMediumViseme2.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthFunnel" );
		sayMediumViseme2.controllerVars[0].minShape = 0f;
		sayMediumViseme2.controllerVars[0].maxShape = 0.2f * blendShapeFactor;

		// 3
		salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "sayBig2", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
		var sayBigViseme2 = salsa.visemes[5].expData;
		sayBigViseme2.components[0].name = "sayMedium2 component";
		sayBigViseme2.controllerVars[0].smr = smr;
		sayBigViseme2.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthFunnel" );
		sayBigViseme2.controllerVars[0].minShape = 0f;
		sayBigViseme2.controllerVars[0].maxShape = 0.3f * blendShapeFactor;

		bool loadTeethVisemes = true;
		smr = GetTeethSkinnedMeshRenderer( avatar );
		if ( smr != null && loadTeethVisemes )
		{
			int index = ToolsController.instance.GetBlendShapeIndex( smr, "mouthOpen" );
			if ( index >= 0 ) { smr.SetBlendShapeWeight( index, 0.65f ); }

			// 1
			/*
			salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "saySmall", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
			var saySmallViseme3 = salsa.visemes[6].expData;
			saySmallViseme3.components[0].name = "saySmall component";
			saySmallViseme3.controllerVars[0].smr = smr;
			saySmallViseme3.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthOpen" );
			saySmallViseme3.controllerVars[0].minShape = 0f;
			saySmallViseme3.controllerVars[0].maxShape = 0.33f * blendShapeFactor;

			// 2
			salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "sayMedium", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
			var sayMediumViseme3 = salsa.visemes[7].expData;
			sayMediumViseme3.components[0].name = "sayMedium component";
			sayMediumViseme3.controllerVars[0].smr = smr;
			sayMediumViseme3.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthOpen" );
			sayMediumViseme3.controllerVars[0].minShape = 0f;
			sayMediumViseme3.controllerVars[0].maxShape = 0.66f * blendShapeFactor;

			// 3
			salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "sayBig", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
			var sayBigViseme3 = salsa.visemes[8].expData;
			sayBigViseme3.components[0].name = "sayMedium component";
			sayBigViseme3.controllerVars[0].smr = smr;
			sayBigViseme3.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthOpen" );
			sayBigViseme3.controllerVars[0].minShape = 0f;
			sayBigViseme3.controllerVars[0].maxShape = 1f * blendShapeFactor;

			// 1  
			salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "saySmall2", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
			var saySmallViseme4 = salsa.visemes[9].expData;
			saySmallViseme4.components[0].name = "saySmall2 component";
			saySmallViseme4.controllerVars[0].smr = smr;
			saySmallViseme4.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthFunnel" );
			saySmallViseme4.controllerVars[0].minShape = 0f;
			saySmallViseme4.controllerVars[0].maxShape = 0.1f * blendShapeFactor;

			// 2
			salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "sayMedium2", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
			var sayMediumViseme4 = salsa.visemes[10].expData;
			sayMediumViseme4.components[0].name = "sayMedium2 component";
			sayMediumViseme4.controllerVars[0].smr = smr;
			sayMediumViseme4.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthFunnel" );
			sayMediumViseme4.controllerVars[0].minShape = 0f;
			sayMediumViseme4.controllerVars[0].maxShape = 0.2f * blendShapeFactor;

			// 3
			salsa.visemes.Add( new CrazyMinnow.SALSA.LipsyncExpression( "sayBig2", new CrazyMinnow.SALSA.InspectorControllerHelperData(), 0f ) );
			var sayBigViseme4 = salsa.visemes[11].expData;
			sayBigViseme4.components[0].name = "sayMedium2 component";
			sayBigViseme4.controllerVars[0].smr = smr;
			sayBigViseme4.controllerVars[0].blendIndex = smr.sharedMesh.GetBlendShapeIndex( "mouthFunnel" );
			sayBigViseme4.controllerVars[0].minShape = 0f;
			sayBigViseme4.controllerVars[0].maxShape = 0.3f * blendShapeFactor;
			*/
		}

		// apply api trigger distribution...
		salsa.DistributeTriggers( CrazyMinnow.SALSA.LerpEasings.EasingType.SquaredIn );
		// at runtime: apply controller baking...
		salsa.UpdateExpressionControllers();

		Debug.Log( "UpdateLipSync SALSA done" );
	}

	public SkinnedMeshRenderer GetHeadSkinnedMeshRenderer(GameObject avatar)
	{
		List<string> headNames = new List<string>() { "Wolf3D_Head", "Renderer_Head", "Renderer_Avatar", "Wolf3D_Avatar" };
		for ( int i = 0; i < headNames.Count; i++ )
		{
			GameObject obj = ToolsController.instance.FindGameObjectByName( avatar, headNames[i] );
			if ( obj != null && obj.GetComponent<SkinnedMeshRenderer>() != null )
			{
				for ( int j = 0; j < obj.GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount; j++ )
				{
					if (
						obj.GetComponent<SkinnedMeshRenderer>().sharedMesh.GetBlendShapeName( j ) == "viseme_sil"
						|| obj.GetComponent<SkinnedMeshRenderer>().sharedMesh.GetBlendShapeName( j ) == "mouthOpen"
					)
					{
						print( "<color=#00ff00>Found mouthOpen blendShape</color>" );
						return obj.GetComponentInChildren<SkinnedMeshRenderer>();
					}
				}
			}
			else { print( "<color=#ff0000>No " + headNames[i] + " found</color>" ); }
		}

		SkinnedMeshRenderer[] skinnedMeshRenderer = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();
		for ( int i = 0; i < skinnedMeshRenderer.Length; i++ )
		{
			int index = ToolsController.instance.GetBlendShapeIndex( skinnedMeshRenderer[i], "viseme_sil" );
			if ( index >= 0 )
			{
				print( "Found viseme_sil blendShape " + skinnedMeshRenderer[i].name );
				return skinnedMeshRenderer[i];
			}

			index = ToolsController.instance.GetBlendShapeIndex( skinnedMeshRenderer[i], "mouthOpen" );
			if ( index >= 0 )
			{
				print( "Found mouthOpen blendShape " + skinnedMeshRenderer[i].name );
				return skinnedMeshRenderer[i];
			}
		}

		return null;
	}

	public SkinnedMeshRenderer GetTeethSkinnedMeshRenderer(GameObject avatar)
	{
		List<string> names = new List<string>() { "Renderer_Teeth", "Teeth", "Wolf3D_Teeth" };
		for ( int i = 0; i < names.Count; i++ )
		{
			GameObject obj = ToolsController.instance.FindGameObjectByName( avatar, names[i] );
			if ( obj != null && obj.GetComponent<SkinnedMeshRenderer>() != null )
			{
				for ( int j = 0; j < obj.GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount; j++ )
				{
					if (
						obj.GetComponent<SkinnedMeshRenderer>().sharedMesh.GetBlendShapeName( j ) == "viseme_sil"
						|| obj.GetComponent<SkinnedMeshRenderer>().sharedMesh.GetBlendShapeName( j ) == "mouthOpen"
					)
					{
						print( "<color=#00ff00>Found mouthOpen blendShape</color>" );
						return obj.GetComponentInChildren<SkinnedMeshRenderer>();
					}
				}
			}
			else { print( "<color=#ff0000>No " + names[i] + " found</color>" ); }
		}

		return null;
	}

	public void Back()
	{
		InfoController.instance.ShowCommitAbortDialog( "STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation );
	}

	public void Reset()
	{
		ARController.instance.navArrow.enableNavArrow = false;
		MediaCaptureController.instance.Reset();
		audioSource.Stop();
		arObjectRoot.SetActive( false );
		if ( LightController.instance != null ){ LightController.instance.directionalLight.transform.eulerAngles = lightRotation; }
	}

	public void SetAvatarRenderer()
	{
		for (int i = 0; i < salsaSettings.Count; i++)
		{
			for (int j = 0; j < salsaSettings[i].visemes.Count; j++)
			{
				salsaSettings[i].visemes[j].expData.controllerVars[0].smr = avatarHeadRenderer;
			}
		}
	}
}

#if UNITY_EDITOR
[CustomEditor( typeof( ARAvatarController ) )]
public class ARAvatarControllerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		ARAvatarController myComponent = (ARAvatarController)target;

		if ( GUILayout.Button( "Update" ) )
		{
			myComponent.SetAvatarRenderer();
		}
	}
}
#endif
