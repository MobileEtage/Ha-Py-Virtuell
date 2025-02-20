using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;
using UnityEngine.Networking;

public class GlasObjectController : MonoBehaviour
{
	public bool productionBuild = true;
	public bool canChangeHeightMarkerless = true;
	public bool scanMarkerSelected = false;
	public float targetScale = 1.0f;
	private int markerVersion = 1;
	
	[Space( 10 )]

	public GameObject marker;
	public Transform mainCamera;
	public GameObject coneModel;
	public GameObject placementHelper;
	public GameObject commitPlacementButton;
	public GameObject skipPlacementButton;

	[Space( 10 )]

	public GameObject coneModel_Markerless;
	public GameObject coneModel_MarkerlessWithPassages;
	public GameObject coneModel_Marker;
	public GameObject coneModel_MarkerWithPassages;
	public GameObject coneModel_Scheme;
	public GameObject passagesModel;
	public GameObject groundModel;
	public GameObject markerLines;

	[Space( 10 )]

	public AudioSource scaleSound;
	public GameObject menuRoot;
	public GameObject headerOptions;
	public GameObject optionButtons;
	public GameObject scaleButton;
	public GameObject moveObjectToUsButton;
	public GameObject settingsButton;
	public GameObject tunnelButton;
	public GameObject tunnelSelectionImage;
	public Image scaleImage;
	public Image moveObjectToUsImage;
	public Sprite scaleUpIcon;
	public Sprite scaleDownIcon;
	public GameObject infoButton;
	public GameObject updatePlacementButton;
	public DragMenuController settingsMenu;
	public Slider heightSlider;
	public Slider sizeSlider;
	public Slider passagesAlphaSlider;
	public Slider groundAlphaSlider;
	public Toggle mapCanvasToggle;
	public Toggle shadowPlaneToggle;
	public Toggle showMarkerLineToggle;
	public GameObject mapCanvas;
	public GameObject shadowPlane;
	public Image menuButton;
	public Sprite arSprite;
	public Sprite objectSprite;
	public TextMeshProUGUI positionLabel;
	public GameObject markerSettings;
	public GameObject markerSaveButtonImage;

	[Space( 10 )]

	public bool canUpdateMarkerPosition = false;
	public bool isScanningMarker = false;
	public bool hasScannedMarker = false;
	public string placementType = "markCenter"; // default, markCenter
	public float moveFactor = 10.0f;
	public float minMoveFactor = 0.1f;

	private bool isLoading = false;
	private bool isUploading = false;
	private bool isMovingARObject = false;
	private bool moveEnabled = false;
	private string currentViewType = "ar";

	private Vector3 lastHitPosition = Vector3.zero;
	private Vector3 hitOffset = new Vector3( -1000, -1000, -1000 );
	private float smallScale = 0.16f;
	private float defaultSize = 19.0f;
	private float defaultDistance = 5.7f;
	private Vector3 targetPosition = Vector3.zero;

	private Vector3 rotateCenter = Vector3.zero;
	private Vector3 oldRotatePosition = Vector3.zero;
	private Vector2 rotateClockwiseVector = new Vector2( -9999, -9999 );

	private float trackingTimer = 0;
	private float trackingTime = 1.0f;
	private bool shouldShowTrackedAnimation = false;
	private bool hasTracked = true;
	private bool showPlacementInfo = true;
	private bool scanMarkerActive = true;
	private Vector3 lightRotation = new Vector3( 45, -10, 0 );

	private JSONNode dataJson;
	private Vector3 lastTouchPosition;
	private bool canReplace = false;
	private Vector3 lastObjectPosition = Vector3.zero;

	private bool tunnelInfoShowed = false;

	public static GlasObjectController instance;
	void Awake()
    {
		instance = this;    
    }

	void Start()
	{
		coneModel.SetActive( false );
		coneModel_Markerless.SetActive( false );
		coneModel_MarkerlessWithPassages.SetActive( false );
		coneModel_Marker.SetActive( false );
		coneModel_MarkerWithPassages.SetActive( false );

		Init();
		Reset();
	}

	void Update()
    {
		if ( SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "GlassObjectSite" ) return;

		Move();
		Rotate();
		Scale();
		UpdateTracking();
		ReplaceObject();
		optionButtons.GetComponent<CanvasGroup>().alpha = settingsMenu.menuIsOpen ? 0 : 1;

		if ( hasScannedMarker && canUpdateMarkerPosition ) { UpdatePosition(); }

		if(marker != null && canUpdateMarkerPosition && hasScannedMarker)
		{
			if ( marker.GetComponent<ImageTarget>().trackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking )
			{
				markerSaveButtonImage.GetComponent<Image>().color = new Color( 0.5f, 0.5f, 0.5f );
			}
			else
			{
				markerSaveButtonImage.GetComponent<Image>().color = new Color( 0, 0.1804f, 0.3412f );
			}
		}
	}

	public void Init()
	{		
		string savePath = Path.Combine( Application.persistentDataPath, "_marker.json" );
		
		if( File.Exists(savePath) && PlayerPrefs.GetInt("markerVersion", 0) < markerVersion)
		{
			File.Delete( savePath );
		}

		if ( File.Exists( savePath ) ) { dataJson = JSONNode.Parse( File.ReadAllText( savePath ) ); }
		else
		{
			dataJson = JSONNode.Parse( Resources.Load<TextAsset>( "_marker" ).text );
			File.WriteAllText( savePath, dataJson.ToString() );
		}

		PlayerPrefs.SetInt("markerVersion", markerVersion);
		PlayerPrefs.Save();

		foreach ( Transform child in menuRoot.transform ) { child.GetChild( 0 ).GetComponent<Image>().color = Params.guideMenuButtonActiveColor; }
	}

	public void UpdatePositionsData(string filePath)
	{
		if( File.Exists( filePath ) )
		{
			dataJson = JSONNode.Parse(File.ReadAllText(filePath));

			string savePath = Path.Combine( Application.persistentDataPath, "_marker.json" );
			File.WriteAllText( savePath, dataJson.ToString() );

			Debug.Log("marker positions data downloaded and applied!");
		}
	}

	public void UpdateTracking()
	{
		if ( isScanningMarker && marker != null && scanMarkerActive )
		{
			for ( int i = 0; i < ImageTargetController.Instance.imageTargets.Count; i++ )
			{
				if ( ImageTargetController.Instance.imageTargets[i].isTracking )
				{
					ScanController.instance.currentTrackedMarker = ImageTargetController.Instance.imageTargets[i].id;

					if ( marker.GetComponent<ImageTarget>().id != ImageTargetController.Instance.imageTargets[i].id )
					{
						OnMarkerSwitched();
					}

					break;
				}
			}
	
			if( hasTracked && marker.GetComponent<ImageTarget>().trackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
			{
				hasTracked = false;
			}
			else if(!hasTracked && marker.GetComponent<ImageTarget>().trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking )
			{
				hasTracked = true;
				ResetSliderHeight();
				ResetSliderSize();
			}
		}
	}

	public void OnMarkerSwitched()
	{
		ARController.instance.navArrow.UpdateFocusObject( coneModel.transform, !isScanningMarker );
		marker = ImageTargetController.Instance.GetImageTarget( ScanController.instance.currentTrackedMarker ).gameObject;
		UpdateMarkerPosition();

		foreach ( Transform child in markerLines.transform )
		{
			child.gameObject.SetActive( false );
			if ( child.name == marker.GetComponent<ImageTarget>().id ) { child.gameObject.SetActive( true ); }
		}

		ResetSliderHeight();
		ResetSliderSize();
	}

	// Update position every interval if marker is tracked
	public void UpdatePosition()
	{
		if ( marker.GetComponent<ImageTarget>().trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking )
		{
			trackingTimer += Time.deltaTime;
			if( trackingTimer > trackingTime )
			{
				if ( shouldShowTrackedAnimation )
				{
					shouldShowTrackedAnimation = false;
					//marker.GetComponentInChildren<Animator>( true ).Play( "Marker-Tracked", 0, 0 );
				}

				UpdateMarkerPosition();
				trackingTimer = 0;
			}
		}
		else
		{
			shouldShowTrackedAnimation = true;
			trackingTimer = 0;
		}
	}

	public void UpdateMarkerPosition()
	{
		if ( marker == null ) return;
		if ( marker.GetComponent<ImageTarget>().trackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking ) return;
		UpdateObjectPosition();
	}

	public void UpdateObjectPosition()
	{
		if ( marker == null ) return;

		string markerId = ScanController.instance.currentTrackedMarker;
		Vector3 markerOffset = GetObjectOffset( markerId );

		Vector3 right = new Vector3( marker.transform.right.x, 0, marker.transform.right.z );
		Vector3 up = new Vector3( 0, marker.transform.up.y, 0 );
		Vector3 forward = new Vector3( marker.transform.forward.x, 0, marker.transform.forward.z );

		coneModel.transform.position =
			marker.transform.position +
			right.normalized * markerOffset.x +
			up.normalized * markerOffset.y +
			forward.normalized * markerOffset.z;
		coneModel.transform.eulerAngles = new Vector3( 0, marker.transform.eulerAngles.y, 0 );
		lastHitPosition = coneModel.transform.position;

		float objectRotation = GetObjectRotation( markerId );
		coneModel.transform.GetChild( 0 ).localEulerAngles = new Vector3( 0, objectRotation, 0 );

		/*
		coneModel.transform.position =
			marker.transform.position +
			marker.transform.right * markerOffset.x +
			marker.transform.up * markerOffset.y +
			marker.transform.forward * markerOffset.z;
		coneModel.transform.eulerAngles = marker.transform.eulerAngles;
		*/

		//Debug.Log( "Marker p: " + marker.transform.position );
		//Debug.Log( "Marker r: " + marker.transform.eulerAngles );
		//Debug.Log( "Marker s: " + marker.transform.localScale );
		//Debug.Log( "Marker r: " + marker.transform.right );
		//Debug.Log( "Marker u: " + marker.transform.up );
		//Debug.Log( "Marker f: " + marker.transform.forward );
		//Debug.Log( "markerOffset: " + markerOffset );
		//Debug.Log( "Camera p: " + mainCamera.position );
		//Debug.Log( "Cone p: " + coneModel.transform.position );
	}

	public Vector3 GetObjectOffset(string markerId)
	{
		Vector3 markerOffset = new Vector3( 0, 0, 0 );
		for ( int i = 0; i < dataJson["marker"].Count; i++ )
		{
			if ( dataJson["marker"][i]["id"].Value == markerId )
			{
				float x = ToolsController.instance.GetFloatFromString( dataJson["marker"][i]["objectOffset"][0].Value );
				float y = ToolsController.instance.GetFloatFromString( dataJson["marker"][i]["objectOffset"][1].Value );
				float z = ToolsController.instance.GetFloatFromString( dataJson["marker"][i]["objectOffset"][2].Value );

				x = Mathf.Clamp( x, -80, 80 );
				y = Mathf.Clamp( y, -5, 5 );
				z = Mathf.Clamp( z, -80, 80 );

				markerOffset.x = x;
				markerOffset.y = y;
				markerOffset.z = z;

				break;
			}
		}

		return markerOffset;
	}

	public float GetObjectRotation(string markerId)
	{
		for ( int i = 0; i < dataJson["marker"].Count; i++ )
		{
			if ( dataJson["marker"][i]["id"].Value == markerId )
			{
				return ToolsController.instance.GetFloatFromString( dataJson["marker"][i]["objectRotation"].Value );
			}
		}
		return 0;
	}

	public float GetObjectHeight(string markerId)
	{
		for ( int i = 0; i < dataJson["marker"].Count; i++ )
		{
			if ( dataJson["marker"][i]["id"].Value == markerId )
			{
				return ToolsController.instance.GetFloatFromString( dataJson["marker"][i]["height"].Value );
			}
		}
		return 0;
	}


	public void Move()
	{
		if ( hasScannedMarker )
		{
			if(canUpdateMarkerPosition && placementHelper.activeInHierarchy) { }
			else { return; }
		}

		if ( targetScale >= 1.0f && !placementHelper.activeInHierarchy ) { return; }
		if ( Input.touchCount >= 2 ) { isMovingARObject = false; return; }
		if( productionBuild && isScanningMarker ) { return; }

		/*
		if ( Input.GetMouseButton( 0 ) )
		{
			Vector2 touchPositionTmp = ToolsController.instance.GetTouchPosition();
			Vector3 hitPositionTmp = mainCamera.position + mainCamera.forward * 2;
			ARController.instance.CurvedRaycastHit( touchPositionTmp, out hitPositionTmp );
		}
		*/

		//if ( Input.GetMouseButtonDown( 0 ) && !ToolsController.instance.IsPointerOverUIObject() )
		//if ( Input.GetMouseButtonDown( 0 ) )
		if ( Input.GetMouseButtonDown( 0 ) && (mapCanvas.activeInHierarchy || !ToolsController.instance.IsPointerOverUIObject()) )
		{
			RaycastHit[] hits;
			Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay( Input.mousePosition );
			hits = Physics.RaycastAll( ray, 100 );

			for ( int i = 0; i < hits.Length; i++ )
			{
				if ( hits[i].transform == coneModel.transform || hits[i].transform == coneModel.transform.GetChild(0) )
				{
					//hitOffset = hits[i].point - coneModel.transform.position;
					//hitOffset.y = 0;
					isMovingARObject = true;

					// Start drag object delayed, because maybe we also want to scale it with two fingers
					StopCoroutine( "EnableMoveARObjectCoroutine" );
					StartCoroutine( "EnableMoveARObjectCoroutine" );

					break;
				}
				else if (hits[i].transform == placementHelper.transform)
				{
					//hitOffset = hits[i].point - placementHelper.transform.position;
					//hitOffset.y = 0;
					isMovingARObject = true;

					// Start drag object delayed, because maybe we also want to scale it with two fingers
					StopCoroutine( "EnableMoveARObjectCoroutine" );
					StartCoroutine( "EnableMoveARObjectCoroutine" );
				}
			}

			
		}
		else if ( moveEnabled && isMovingARObject && Input.GetMouseButton( 0 ) )
		{
			Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
			//Vector3 hitPosition = mainCamera.position + mainCamera.forward * 2;
			Vector3 hitPosition = lastHitPosition;

			bool hitGround = false;
			if ( placementType == "markCenter" && ARController.instance != null && ARController.instance.CurvedRaycastHit( touchPosition, lastHitPosition, out hitPosition ) )
			{
				hitGround = true;
			}
			else if ( ARController.instance != null && ARController.instance.RaycastHit( touchPosition, lastHitPosition, out hitPosition ) )
			{
				hitGround = true;
			}
			else
			{

				/*
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
				*/

				if ( !hitGround )
				{
					//Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay( Input.mousePosition );
					//hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;

					Plane m_Plane = new Plane( Vector3.up, coneModel.transform.position );
					Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay( Input.mousePosition );
					//hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;

					float enter = 0.0f;
					if ( m_Plane.Raycast( rayTemp, out enter ) )
					{
						hitPosition = rayTemp.GetPoint( enter );
						hitGround = true;
					}
				}
			}

			if ( !hitGround ) return;

			if( hitOffset.x == -1000 )
			{
				hitOffset = hitPosition - coneModel.transform.position;
				if ( placementHelper.activeInHierarchy ){ hitOffset = hitPosition - placementHelper.transform.position; }
				hitOffset.y = 0;
			}

			Vector3 pos = hitPosition - hitOffset;
			if(pos.y > (mainCamera.position.y - 0.25f) )
			{
				if ( lastHitPosition.y < (mainCamera.position.y - 0.25f) ) { pos.y = lastHitPosition.y; }
				else { pos.y = mainCamera.position.y - 0.5f; }
			}

			float dist = Vector3.Distance(mainCamera.position, pos);
			if(dist > 15) { pos = lastHitPosition; }

			coneModel.transform.position = pos;
			placementHelper.transform.position = pos;

			lastHitPosition = pos;
		}
		else if ( Input.GetMouseButtonUp( 0 ) )
		{
			if(ARController.instance != null ) { ARController.instance.DisableArcRenderer(); }
			StopCoroutine( "EnableMoveARObjectCoroutine" );
			isMovingARObject = false;
			hitOffset.x = -1000;
		}
	}

	public IEnumerator EnableMoveARObjectCoroutine()
	{
		yield return new WaitForSeconds( 0.15f );
		if ( Input.touchCount >= 2 ) { moveEnabled = false; }
		else { moveEnabled = true; }
	}

	public void Rotate()
	{
		if ( productionBuild && isScanningMarker ) { return; }

		if ( Input.touchCount != 2 )
		{
			oldRotatePosition = new Vector2( -1, -1 );
			rotateClockwiseVector = new Vector2( -9999, -9999 );
		}

		if ( isMovingARObject ) return;

		if ( Application.isEditor )
		{
			if ( Input.GetKey( KeyCode.R ) )
			{
				float angle = Time.deltaTime*100;
				coneModel.transform.GetChild( 0 ).Rotate( Vector3.up, angle, Space.World );
				placementHelper.transform.eulerAngles = coneModel.transform.GetChild( 0 ).eulerAngles;
				SaveObjectPosition();
			}
		}
		else if ( Input.touchCount == 2 )
		{
			float angle = 0;
			Vector2 touch1 = Input.GetTouch( 0 ).position;
			Vector2 touch2 = Input.GetTouch( 1 ).position;
			Vector2 line = touch1 - touch2;
			if ( rotateClockwiseVector.x <= -9999 ) { angle = 0; }
			else
			{
				//angle = Vector3.SignedAngle( line, rotateClockwiseVector, mainCamera.forward );
				angle = Vector3.SignedAngle( line, rotateClockwiseVector, new Vector3(0,0,1) );
				coneModel.transform.GetChild(0).Rotate( Vector3.up, angle, Space.World );
				placementHelper.transform.eulerAngles = coneModel.transform.GetChild( 0 ).eulerAngles;
			}
			rotateClockwiseVector = line;
			SaveObjectPosition();

			/*
			if ( oldRotatePosition.x <= 0 ) { oldRotatePosition = Input.mousePosition; }
			Vector2 currentPosition = Input.mousePosition;
			Vector2 differnceVectorLastFrame = new Vector2( (currentPosition.x / Screen.width), (currentPosition.y / Screen.height) ) - new Vector2( (oldRotatePosition.x / Screen.width), (oldRotatePosition.y / Screen.height) );
			oldRotatePosition = currentPosition;
			differnceVectorLastFrame *= 200.0f;
			this.transform.RotateAround( coneModel.transform.position, mainCamera.up, -differnceVectorLastFrame.x );
			*/
		}
		else if ( Input.GetMouseButtonUp( 0 ) || Input.GetMouseButtonUp( 1 ) )
		{
			oldRotatePosition = new Vector2( -1, -1 );
			rotateClockwiseVector = new Vector2( -9999, -9999 );
		}
	}

	public void Scale()
	{
		if ( targetScale < 1.0f ) { if ( scaleImage.sprite != scaleUpIcon ) { scaleImage.sprite = scaleUpIcon; } }
		else { if ( scaleImage.sprite != scaleDownIcon ) { scaleImage.sprite = scaleDownIcon; } }

		//coneModel.transform.localScale = Vector3.Slerp( coneModel.transform.localScale, Vector3.one * targetScale, Time.deltaTime * 5 );
		coneModel.transform.GetChild(0).localScale = Vector3.Slerp( coneModel.transform.GetChild( 0 ).localScale, Vector3.one * targetScale, Time.deltaTime * 5 );
	}

	public void SelectViewType(string viewType)
	{
		if ( viewType == currentViewType )
		{
			if ( currentViewType == "AR+" ) { viewType = "AR"; }
			else { return; }
		}

		if ( isLoading ) return;
		isLoading = true;
		StartCoroutine( SelectViewTypeCoroutine( viewType ) );
	}

	public IEnumerator SelectViewTypeCoroutine(string viewType)
	{
		currentViewType = viewType;
		MarkMenuButton( viewType );
		passagesAlphaSlider.transform.parent.gameObject.SetActive( viewType == "AR+" );
		//groundAlphaSlider.transform.parent.gameObject.SetActive( viewType == "AR+" );
		optionButtons.SetActive( viewType != "Scheme" );

		if( viewType == "AR+" && !tunnelInfoShowed ) { tunnelInfoShowed = true; InfoController.instance.ShowMessage("Schürgänge", "Die Schürgänge sind nur sichtbar, wenn der gesamte Rauchgaskegel von außen im Blickfeld ist.\n\nDu kannst mit dem Zoom-Icon die Ansicht ändern."); }

		if ( viewType == "AR" || viewType == "AR+" )
		{
			//InfoController.instance.loadingCircle.SetActive( true );
			//ARController.instance.InitARFoundation();
			//yield return new WaitForSeconds( 0.5f );
			//InfoController.instance.loadingCircle.SetActive( false );

			yield return StartCoroutine( UpdateModelCoroutine( viewType, false ) );
		}
		else if ( viewType == "Scheme" )
		{
			coneModel_Scheme.GetComponentInChildren<FocusAreaController>( true ).Reset();
			settingsMenu.Close();
			coneModel_Scheme.SetActive( true );
			coneModel.SetActive( false );

			//ARController.instance.DisableARSession();
		}

		isLoading = false;
	}

	public void MarkMenuButton(string viewType)
	{
		if ( viewType == "AR+" ) { tunnelSelectionImage.SetActive( true ); return; }
		else { tunnelSelectionImage.SetActive( false ); }

		Color activeColor = ToolsController.instance.GetColorFromHexString( "#ffffff" ); ;
		Color inActiveColor = ToolsController.instance.GetColorFromHexString( "#005CA9" ); ;
		foreach ( Transform child in menuRoot.transform ) {

			if ( child.childCount <= 0 ) continue;
			child.GetChild( 0 ).GetComponent<Image>().enabled = (child.name == viewType);
			child.GetComponentInChildren<TextMeshProUGUI>(true).color = (child.name == viewType) ? activeColor : inActiveColor;
		}
	}

	public void LoadObject()
	{
		if ( isLoading ) return;
		isLoading = true;
		StartCoroutine(LoadObjectCoroutine());
	}

	public IEnumerator LoadObjectCoroutine()
	{
		//skipPlacementButton.SetActive( !isScanningMarker );
		skipPlacementButton.SetActive( !scanMarkerSelected );	

		canUpdateMarkerPosition = true;
		moveObjectToUsImage.sprite = scaleUpIcon;

		showMarkerLineToggle.transform.parent.gameObject.SetActive(isScanningMarker);
		ResetSliderHeight();
		ResetSliderSize();
		settingsMenu.CloseImmediate();

		foreach ( Transform child in markerLines.transform ){ child.gameObject.SetActive( false ); }
		showMarkerLineToggle.GetComponentInChildren<LeTai.TrueShadow.Demo.ToggleSwitchAnimation>().Toggle( false );
		showMarkerLineToggle.SetIsOnWithoutNotify( false );
		markerLines.SetActive( false );

		mapCanvasToggle.GetComponentInChildren<LeTai.TrueShadow.Demo.ToggleSwitchAnimation>().Toggle(false);
		mapCanvasToggle.SetIsOnWithoutNotify( false );
		mapCanvas.SetActive(false);

		shadowPlaneToggle.GetComponentInChildren<LeTai.TrueShadow.Demo.ToggleSwitchAnimation>().Toggle( false );
		shadowPlaneToggle.SetIsOnWithoutNotify( false );
		shadowPlane.SetActive( false );

		if ( !PermissionController.instance.IsARFoundationSupported() )
		{
			//infoButton.SetActive( false );
			currentViewType = "Scheme";
			MarkMenuButton( "Scheme" );
			coneModel_Scheme.SetActive( true );
			coneModel.SetActive( false );
			menuRoot.SetActive(false);
			optionButtons.SetActive( false );
			menuButton.sprite = objectSprite;

			MapController.instance.DisableMap();
			yield return StartCoroutine( SiteController.instance.SwitchToSiteCoroutine( "GlassObjectSite" ) );
		}
		else
		{
			//infoButton.SetActive( true );
			menuRoot.SetActive( true );
			menuButton.sprite = arSprite;

			ARController.instance.navArrow.UpdateFocusObject(coneModel.transform, !isScanningMarker);

			if ( hasScannedMarker )
			{
				MapController.instance.ScanMarker();
				hasScannedMarker = false;
			}
			else
			{
				if ( !ARController.instance.arSession.enabled )
				{
					InfoController.instance.loadingCircle.SetActive( true );
					ARController.instance.InitARFoundation();
					yield return new WaitForSeconds( 0.5f );
					InfoController.instance.loadingCircle.SetActive( false );
				}

				yield return StartCoroutine( UpdateModelCoroutine( "AR", true ) );
				yield return StartCoroutine( SiteController.instance.SwitchToSiteCoroutine( "GlassObjectSite" ) );
			}
		}

		isLoading = false;
	}

	public IEnumerator UpdateModelCoroutine(string viewType, bool shouldUpdate)
	{
		updatePlacementButton.SetActive( !isScanningMarker );
		if( canUpdateMarkerPosition ) { updatePlacementButton.SetActive( true ); }

		tunnelButton.SetActive( MapController.instance.selectedStationId == "glashuette4" );
		moveObjectToUsButton.SetActive( isScanningMarker && productionBuild );
		scaleButton.SetActive( !isScanningMarker );
		optionButtons.SetActive( viewType != "Scheme" );
		settingsButton.SetActive( true );
		headerOptions.SetActive( true );
		markerSettings.SetActive( isScanningMarker );

		if ( productionBuild )
		{
			if ( isScanningMarker )
			{
				settingsButton.SetActive( false );
				updatePlacementButton.SetActive( false );
			}
			else
			{
				if ( !canChangeHeightMarkerless ) { settingsButton.SetActive( false ); }
			}
		}

		if ( isScanningMarker )
		{
			if ( shouldUpdate )
			{
				marker = ImageTargetController.Instance.GetImageTarget( ScanController.instance.currentTrackedMarker ).gameObject;
				//marker.GetComponentInChildren<Animator>( true ).Play( "Marker-Tracked", 0, 0 );
				shouldShowTrackedAnimation = false;
				UpdateMarkerPosition();
				hasScannedMarker = true;
				hasTracked = true;

				LightController.instance.directionalLight.gameObject.SetActive( false );
				lightRotation = LightController.instance.directionalLight.transform.eulerAngles;
				LightController.instance.directionalLight.transform.eulerAngles = new Vector3(45, marker.transform.eulerAngles.y-45, 0);
				DynamicGI.UpdateEnvironment();
				Debug.Log("Marker Rotation: " + marker.transform.eulerAngles);

				foreach (Transform child in markerLines.transform)
				{
					child.gameObject.SetActive( false );
					if (child.name == marker.GetComponent<ImageTarget>().id ) { child.gameObject.SetActive(true); }
				}
			}
		}
		else
		{
			hasScannedMarker = false;

			if ( shouldUpdate )
			{
				coneModel.transform.SetParent( null );
				UpdateMarkerlessPosition();

				LightController.instance.directionalLight.gameObject.SetActive(false);
				lightRotation = LightController.instance.directionalLight.transform.eulerAngles;
				LightController.instance.directionalLight.transform.eulerAngles = new Vector3( 45, mainCamera.eulerAngles.y-45, 0 );
				DynamicGI.UpdateEnvironment();
				Debug.Log( "MainCamera Rotation: " + mainCamera.eulerAngles );
			}
		}

		if ( shouldUpdate ) { UpdateHeight(); }
		UpdateSize();
		currentViewType = viewType;
		MarkMenuButton( viewType );
		passagesAlphaSlider.transform.parent.gameObject.SetActive( viewType == "AR+" );
		//groundAlphaSlider.transform.parent.gameObject.SetActive( viewType == "AR+" );

		coneModel_Scheme.SetActive( false );
		coneModel.SetActive( true );
		if ( viewType == "AR+" ) EnableModelType( "AR+" );
		else if ( isScanningMarker ) EnableModelType( "Marker" );
		else EnableModelType( "Markerless" );

		ToolsController.instance.ShowHideRenderer( coneModel, true );

		if ( !shouldUpdate ) yield break;

		UpdatePlacementHelper( shouldUpdate );
	}

	public void UpdatePlacementHelper(bool shouldUpdate)
	{
		if( placementType == "default" || isScanningMarker )
		{
			placementHelper.SetActive( false );
		}
		else
		{
			if ( placementType == "markCenter" )
			{
				ARController.instance.navArrow.UpdateFocusObject( placementHelper.transform, false );
				optionButtons.SetActive( false );
				headerOptions.SetActive( false );
				placementHelper.SetActive( true );
				coneModel.SetActive( false );
				commitPlacementButton.SetActive(true);
				targetScale = 0;

				if ( showPlacementInfo )
				{
					showPlacementInfo = false;
					InfoController.instance.ShowMessage( "Platzierung", "Tippe mit dem Finger auf das Positionierungs-Objekt und ziehe es an die gewünschte Stelle in deiner Umgebung. Klicke auf \"Platzieren\", um die Position zu bestätigen und den Rauchgaskegel anzuzeigen." );
				}
			}
		}
	}

	public void CommitPlacement()
	{
		skipPlacementButton.SetActive( false );

		coneModel.transform.position = placementHelper.transform.position;
		coneModel.transform.GetChild( 0 ).localPosition = Vector3.zero;
		heightSlider.SetValueWithoutNotify(0);

		headerOptions.SetActive( true );
		placementHelper.SetActive( false );
		coneModel.SetActive( true );
		commitPlacementButton.SetActive( false );
		optionButtons.SetActive( true );

		//targetScale = smallScale;
		targetScale = 1.0f;

		sizeSlider.SetValueWithoutNotify( defaultSize * targetScale );
		placementType = "default";
		scaleSound.Play();

		if ( marker != null && canUpdateMarkerPosition && isScanningMarker )
		{
			targetScale = 1.0f;
			sizeSlider.SetValueWithoutNotify( defaultSize );

			SaveData();
		}

		//FadeHelper[] fadeHelpers = coneModel.GetComponentsInChildren<FadeHelper>();
		//for ( int i = 0; i < fadeHelpers.Length; i++ ) { fadeHelpers[i].SetAlpha( 0.0f ); fadeHelpers[i].targetAlpha = 1.0f; }
	}

	public void UpdatePlacement()
	{
		//placementHelper.transform.position = new Vector3( coneModel.transform.position.x, coneModel.transform.position.y + coneModel.transform.GetChild( 0 ).localPosition.y, coneModel.transform.position.z );
		placementHelper.transform.position = new Vector3( coneModel.transform.position.x, coneModel.transform.position.y, coneModel.transform.position.z );

		headerOptions.SetActive( false );
		placementHelper.SetActive( true );
		coneModel.SetActive( false );
		commitPlacementButton.SetActive( true );
		optionButtons.SetActive( false );
		placementType = "markCenter";
	}

	public void EnableModelType(string id)
	{
		//coneModel_Marker.SetActive( id == "Marker" );
		//coneModel_MarkerWithPassages.SetActive( id == "AR+" );
		//coneModel_Markerless.SetActive( id == "Markerless" );

		coneModel_Markerless.SetActive( false );
		coneModel_MarkerlessWithPassages.SetActive( false );
		coneModel_Marker.SetActive( false );
		coneModel_MarkerWithPassages.SetActive( false );

		if ( isScanningMarker )
		{
			if ( id == "AR+" ) { coneModel_MarkerWithPassages.SetActive( true ); }
			else { coneModel_Marker.SetActive( true ); }
		}
		else
		{
			if ( id == "AR+" ) { coneModel_MarkerlessWithPassages.SetActive( true ); }
			else { coneModel_Markerless.SetActive( true ); }
		}
	}

	public void UpdateMarkerlessPosition()
	{
		Debug.Log( "<color=#87CEFA>UpdateMarkerlessPosition</color>" );
		Vector3 dir = mainCamera.forward;
		dir.y = 0;

		Vector3 pos = new Vector3(mainCamera.position.x, ScanController.instance.currentMarkerPosition.y, mainCamera.position.z);
		coneModel.transform.position = pos + dir.normalized * defaultDistance;
		coneModel.transform.GetChild( 0 ).localEulerAngles = new Vector3( 0, 0, 0 );

		float offset = 23.5f;
		coneModel.transform.eulerAngles = new Vector3( 0, mainCamera.eulerAngles.y + offset, 0 );

		//placementHelper.transform.position = pos + dir.normalized * 1.0f;
		placementHelper.transform.position = pos + dir.normalized * 4f;
		
		placementHelper.transform.eulerAngles = coneModel.transform.eulerAngles - new Vector3(0, offset, 0);
	}

	public void ToggleMoveObjectToUs()
	{
		canUpdateMarkerPosition = !canUpdateMarkerPosition;
		moveObjectToUsImage.sprite = canUpdateMarkerPosition ? scaleUpIcon:scaleDownIcon;

		if ( !canUpdateMarkerPosition )
		{
			lastObjectPosition = coneModel.transform.position;

			float dist = Vector3.Distance( new Vector3( mainCamera.position.x, 0, mainCamera.position.z ), new Vector3( coneModel.transform.position.x, 0, coneModel.transform.position.z ) );
			float minDist = 3.5f;
			Debug.Log( "Distance: " + dist );
			Debug.Log( "Moving to us");

			Vector3 dir = coneModel.transform.position - mainCamera.position;
			dir.y = 0;
			float posY = ScanController.instance.currentMarkerPosition.y - coneModel.transform.GetChild( 0 ).localPosition.y;
			targetPosition = new Vector3( mainCamera.position.x, posY, mainCamera.position.z ) + dir.normalized * minDist;
			StopCoroutine( "AnimateMoveCoroutine" );
			StartCoroutine( "AnimateMoveCoroutine" );
		}
		else
		{
			targetPosition = lastObjectPosition;
			StopCoroutine( "AnimateMoveCoroutine" );
			StartCoroutine( "AnimateMoveCoroutine" );
		}
	}

	public void ToggleScaleObject()
	{
		//if( productionBuild && isScanningMarker ) { ToggleMoveObjectToUs(); return; }

		if ( targetScale < 1.0f )
		{
			targetScale = 1.0f;

			float dist = Vector3.Distance(new Vector3(mainCamera.position.x, 0, mainCamera.position.z), new Vector3(coneModel.transform.position.x, 0, coneModel.transform.position.z));
			float minDist = defaultDistance;
			float maxDist1 = 8f;
			float maxDist2 = 12.0f;

			Debug.Log("Distance: " + dist);
			
			if(dist < minDist)
			{
				Debug.Log("Too close, moving away");

				Vector3 dir = coneModel.transform.position - mainCamera.position;
				dir.y = 0;
				targetPosition = new Vector3(mainCamera.position.x, coneModel.transform.position.y, mainCamera.position.z) + dir.normalized * defaultDistance;
				StopCoroutine( "AnimateMoveCoroutine" );
				StartCoroutine( "AnimateMoveCoroutine" );
			}
			else if ( dist > maxDist1 && dist < maxDist2 )
			{
				Debug.Log( "Probably in wall, moving away" );

				Vector3 dir = coneModel.transform.position - mainCamera.position;
				dir.y = 0;
				targetPosition = new Vector3( mainCamera.position.x, coneModel.transform.position.y, mainCamera.position.z ) + dir.normalized * maxDist2;
				StopCoroutine( "AnimateMoveCoroutine" );
				StartCoroutine( "AnimateMoveCoroutine" );
			}

			sizeSlider.SetValueWithoutNotify( defaultSize );
		}
		else
		{
			targetScale = smallScale;
			sizeSlider.SetValueWithoutNotify( defaultSize * smallScale );
		}
	}

	public void ToggleMarkerLines()
	{
		markerLines.SetActive(showMarkerLineToggle.isOn);
	}

	public IEnumerator AnimateMoveCoroutine()
	{
		AnimationCurve animationCurve = AnimationController.instance.GetAnimationCurveWithID( "smooth" );
		if ( animationCurve == null ) yield break;

		Vector3 startPosition = coneModel.transform.position;
		float animationDuration = 0.5f;
		float currentTime = 0;

		// First update the height

		Debug.Log( "AnimateMoveCoroutine markerPosition: " + ScanController.instance.currentMarkerPosition );
		Debug.Log( "AnimateMoveCoroutine targetPosition: " + targetPosition );
		Debug.Log( "AnimateMoveCoroutine startPosition: " + startPosition );
		Debug.Log( "AnimateMoveCoroutine Mathf.Abs( targetPosition.y - startPosition.y ): " + Mathf.Abs( targetPosition.y - startPosition.y ) + " " + (Mathf.Abs( targetPosition.y - startPosition.y ) > 1.0f) );

		if ( !canUpdateMarkerPosition && productionBuild && isScanningMarker && Mathf.Abs( targetPosition.y - startPosition.y ) > 1.0f )
		{
			Debug.Log( "AnimateMoveCoroutine updating height ..." );

			Vector3 targetPosHeight = startPosition;
			targetPosHeight.y = targetPosition.y;
			while ( currentTime < animationDuration )
			{
				float lerpValue = animationCurve.Evaluate( currentTime / animationDuration );
				coneModel.transform.position = Vector3.LerpUnclamped( startPosition, targetPosHeight, lerpValue );

				currentTime += Time.deltaTime;
				yield return null;
			}
			coneModel.transform.position = targetPosHeight;
		}

		startPosition = coneModel.transform.position;
		animationDuration = 1.0f;
		currentTime = 0;
		bool soundPlayed = false;

		// Fix target position height if ScanController.instance.currentMarkerPosition was not optimal set in ScanController OnMarkerTracked
		// perhaps we can not set the marker position just for one frame if it was recognized 
		Vector3 myTargetPosition = targetPosition;
		float cameraTargetPositionHeightDif = Mathf.Abs(mainCamera.transform.position.y - targetPosition.y);

		Debug.Log( "cameraTargetPositionHeightDif " + cameraTargetPositionHeightDif );

		float expectedHeight = 1.3f;
		float dif = Mathf.Abs( expectedHeight - cameraTargetPositionHeightDif );
		dif = Mathf.Clamp( dif, 0.0f, 0.5f );
		if ( cameraTargetPositionHeightDif < 1.0f )
		{
			Debug.Log( "fixing height not low enough ..." );
			myTargetPosition.y = targetPosition.y - dif;
		}

		while ( currentTime < animationDuration )
		{
			float lerpValue = animationCurve.Evaluate( currentTime / animationDuration );
			coneModel.transform.position = Vector3.LerpUnclamped( startPosition, myTargetPosition, lerpValue );

			currentTime += Time.deltaTime;
			yield return null;

			if( !soundPlayed && lerpValue > 0.25f ) { soundPlayed = true; scaleSound.Play(); }
		}
		coneModel.transform.position = myTargetPosition;
	}

	public void OpenSettings()
	{
		settingsMenu.Open();
	}

	public void UpdateHeight()
	{
		float height = heightSlider.value;
		coneModel.transform.GetChild( 0 ).localPosition = new Vector3(0, height, 0);
		SaveObjectPosition();
	}

	public void UpdateSize()
	{
		float size = sizeSlider.value;
		targetScale = (size / defaultSize);
	}

	public void ResetSliderHeight()
	{
		float height = 0;

		if ( ImageTargetController.Instance.GetImageTarget( ScanController.instance.currentTrackedMarker ) != null )
		{
			string markerId = ImageTargetController.Instance.GetImageTarget( ScanController.instance.currentTrackedMarker ).id;
			height = GetObjectHeight( markerId );
			//if ( markerId == "glashuette3" || markerId == "glashuette4" ) { height = 1.0f; }
		}

		heightSlider.SetValueWithoutNotify( height );
		coneModel.transform.GetChild( 0 ).localPosition = new Vector3( 0, height, 0 );
	}

	public void ResetSliderSize()
	{
		sizeSlider.SetValueWithoutNotify( defaultSize );
		targetScale = 1.0f;
	}

	public void UpdatePassagesAlpha()
	{
		if ( passagesModel == null ) return;

		Color c = passagesModel.GetComponent<Renderer>().material.color;
		c.a = passagesAlphaSlider.value;
		passagesModel.GetComponent<Renderer>().material.color = c;

		if(c.a >= 1){ ToolsController.instance.ChangeRenderMode(passagesModel, StandardShaderUtils.BlendMode.Opaque); }
		else { ToolsController.instance.ChangeRenderMode( passagesModel, StandardShaderUtils.BlendMode.Fade ); }
	}

	public void UpdateGroundAlpha()
	{
		if ( groundModel == null ) return;

		Color c = groundModel.GetComponent<Renderer>().material.color;
		c.a = groundAlphaSlider.value;
		groundModel.GetComponent<Renderer>().material.color = c;

		if ( c.a >= 1 ) { ToolsController.instance.ChangeRenderMode( groundModel, StandardShaderUtils.BlendMode.Opaque ); }
		else { ToolsController.instance.ChangeRenderMode( groundModel, StandardShaderUtils.BlendMode.Fade ); }
	}

	public void ToggleMapCanvas()
	{
		mapCanvas.SetActive(mapCanvasToggle.isOn);
	}

	public void ToggleShadowPlane()
	{
		shadowPlane.SetActive( shadowPlaneToggle.isOn );
	}

	public void Reset()
	{
		ARController.instance.navArrow.enableNavArrow = false;
		coneModel_Scheme.SetActive( false );
		coneModel.SetActive( false );
		placementHelper.SetActive(false);
		markerLines.SetActive( false );
		commitPlacementButton.SetActive( false );

		LightController.instance.directionalLight.gameObject.SetActive( true );
		LightController.instance.directionalLight.transform.eulerAngles = lightRotation;
		placementType = "markCenter";
	}

	public void SaveData()
	{
		if ( isLoading ) return;
		isLoading = true;
		StartCoroutine(SaveDataCoroutine());
	}

	public IEnumerator SaveDataCoroutine()
	{
		InfoController.instance.loadingCircle.SetActive(true);

		// Set object offset position to current marker
		SaveObjectPosition();

		// Save offset locally, so it is restored after app restart
		//string savePath = Path.Combine( Application.persistentDataPath, "_marker.json" );
		//File.WriteAllText( savePath, dataJson.ToString() );

		// Upload position data
		// yield return StartCoroutine(UploadFileCoroutine());
		yield return null;

		InfoController.instance.loadingCircle.SetActive( false );
		isLoading = false;
	}

	public void SaveObjectPosition()
	{
		if ( productionBuild ) return;
		if ( !canUpdateMarkerPosition || !hasScannedMarker ) { return; }

		string markerId = marker.GetComponent<ImageTarget>().id;

		//Vector3 markerOffset = coneModel.transform.position - marker.transform.position;
		//Vector3 markerOffset = marker.transform.InverseTransformPoint( coneModel.transform.position );
		//Matrix4x4 parentWorldToLocalMatrix = Matrix4x4.TRS( marker.transform.position, marker.transform.rotation, Vector3.one ).inverse;

		Quaternion yawOnlyRotation = Quaternion.Euler( 0, marker.transform.eulerAngles.y, 0 );
		Matrix4x4 parentWorldToLocalMatrix = Matrix4x4.TRS( marker.transform.position, yawOnlyRotation, Vector3.one ).inverse;
		Vector3 markerOffset = parentWorldToLocalMatrix.MultiplyPoint3x4( coneModel.transform.position );

		markerOffset.x = Mathf.Clamp( markerOffset.x, -80, 80 );
		markerOffset.y = Mathf.Clamp( markerOffset.y, -5, 5 );
		markerOffset.z = Mathf.Clamp( markerOffset.z, -80, 80 );

		for ( int i = 0; i < dataJson["marker"].Count; i++ )
		{
			if ( dataJson["marker"][i]["id"].Value == markerId )
			{
				dataJson["marker"][i]["objectOffset"][0] = markerOffset.x.ToString();
				dataJson["marker"][i]["objectOffset"][1] = markerOffset.y.ToString();
				dataJson["marker"][i]["objectOffset"][2] = markerOffset.z.ToString();
				dataJson["marker"][i]["objectRotation"] = coneModel.transform.GetChild( 0 ).localEulerAngles.y.ToString();
				dataJson["marker"][i]["height"] = coneModel.transform.GetChild( 0 ).localPosition.y.ToString();

				//positionLabel.text = markerId + " offset:\nPosition: " + markerOffset.ToString( "F2" ) + "\nRotation: " + ToolsController.instance.GetFloatFromString( dataJson["marker"][i]["objectRotation"].Value ).ToString( "F0" ) + "°" + "\nAbstand: " + Vector2.Distance(new Vector2(marker.transform.position.x, marker.transform.position.z), new Vector2(coneModel.transform.position.x, coneModel.transform.position.z)).ToString("F2") + " Meter";

				positionLabel.text =
					"<b>Marker \"" + markerId + "\"</b>" + "\n" +
					"Abstand: " + Vector2.Distance( new Vector2( marker.transform.position.x, marker.transform.position.z ), new Vector2( coneModel.transform.position.x, coneModel.transform.position.z ) ).ToString( "F2" ) + " Meter" + "\n" +
					"Position: " + markerOffset.ToString( "F2" ) + " / " +
					//"Position: (" + markerOffset.x.ToString( "F2" ).Replace(",", ".") + ", " + markerOffset.z.ToString( "F2" ).Replace( ",", "." ) + ") / " +
					"Rotation: " + ToolsController.instance.GetFloatFromString( dataJson["marker"][i]["objectRotation"].Value ).ToString( "F0" ) + "°";

				break;
			}
		}
	}

	public void ReplaceObject()
	{
		if ( productionBuild && isScanningMarker ) { return; }
		if ( !canUpdateMarkerPosition || !hasScannedMarker || placementHelper.activeInHierarchy || Input.touchCount > 1 ){ canReplace = false; return; }

		if ( Input.GetMouseButtonDown( 0 ) && !ToolsController.instance.IsPointerOverUIObject() )
		{
			canReplace = true;
			lastTouchPosition = Input.mousePosition;
		}
		else if ( Input.GetMouseButton( 0 ) && canReplace )
		{
			Vector3 touchPosition = Input.mousePosition;
			Vector3 offset = lastTouchPosition - touchPosition;

			Vector3 right = new Vector3( mainCamera.right.x, 0, mainCamera.right.z ).normalized;
			Vector3 forward = new Vector3( mainCamera.forward.x, 0, mainCamera.forward.z ).normalized;
			float x = (offset.x / Screen.width) * -moveFactor;
			float y = (offset.y / Screen.width) * -moveFactor;

			//Debug.Log("X: " + x + ", Y: " + y);
			//if ( Mathf.Abs( x ) < minMoveFactor ) { x = 0; }
			//if ( Mathf.Abs( y ) < minMoveFactor ) { y = 0; }

			if(Mathf.Abs(x) > Mathf.Abs( y ) ) { y = 0; }
			else { x = 0; }

			coneModel.transform.position += right * x;
			coneModel.transform.position += forward * y;
			placementHelper.transform.position = coneModel.transform.position;

			lastTouchPosition = touchPosition;
			SaveObjectPosition();
		}
		else if ( Input.GetMouseButtonUp( 0 ) )
		{
			canReplace = false;
		}
	}

	public void Upload()
	{
		if ( marker == null ) return;
		if ( productionBuild ) return;

		if ( marker.GetComponent<ImageTarget>().trackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking )
		{
			InfoController.instance.ShowCommitAbortDialog( "Der Marker sollte von der Kamera erfasst sein, um das korrekte Speichern der Position zu gewährleisten.\n\nTrotzdem fortfahren?", CommitUpload );
		}
		else
		{
			CommitUpload();
		}
	}

	public void CommitUpload()
	{
		if ( productionBuild ) return;
		if ( isUploading ) return;
		isUploading = true;
		StartCoroutine( UploadFileCoroutine() );
	}

	public IEnumerator UploadFileCoroutine()
	{
		InfoController.instance.loadingCircle.SetActive( true );

		string filename = "marker-" + DateTime.Now.ToString( "dd-MM-yyyy-hh-mm-ss" ) + ".json";
		string savePath = Path.Combine( Application.persistentDataPath, "_marker.json" );
		File.WriteAllText( savePath, dataJson.ToString() );

		bool isSuccess = false;
		yield return StartCoroutine(
			UploadFileCoroutine( savePath, "https://app-etagen.die-etagen.de/Stadtrundgang/Hameln/Upload/upload.php", filename, ( bool success, string info) =>
			{
				isSuccess = success;
				if ( isSuccess )
				{
					InfoController.instance.ShowMessage( "Erfolgreich gespeichert, Dateiname: " + filename );
				}
				else
				{
					InfoController.instance.ShowMessage( info );
				}

			} )
		);



		InfoController.instance.loadingCircle.SetActive( false );
		isUploading = false;
	}

	public IEnumerator UploadFileCoroutine(string filePath, string phpScriptURL, string filename, Action<bool, string> Callback)
	{
		WWWForm form = new WWWForm();

		//form.AddField( "dataName", Path.GetFileName( filePath ) );

		//string filename = "_marker-" + DateTime.Now.ToString( "dd-MM-yyyy-hh-mm-ss" ) + ".json";
		form.AddField( "dataName", filename );

		byte[] bytes = File.ReadAllBytes( filePath );
		form.AddBinaryData( "data", bytes );

		UnityWebRequest www = UnityWebRequest.Post( phpScriptURL, form );
		www.timeout = 10;
		www.chunkedTransfer = false;

		yield return www.SendWebRequest();

		if ( !string.IsNullOrEmpty( www.error ) )
		{
			Debug.LogError( "Error uploading file: " + www.error );
			Callback( false, www.error );
		}
		else
		{
			print( "Response: " + www.downloadHandler.text );
			Callback( true, "Upload erfolgreich" );
		}

		isUploading = false;
	}

	public void ResetObjectPosition()
	{
		InfoController.instance.ShowCommitAbortDialog("Willst du wirklich die Position auf die Anfangswerte zurücksetzen?", CommitResetObjectPosition);
	}

	public void CommitResetObjectPosition()
	{
		if ( productionBuild ) return;
		if ( !canUpdateMarkerPosition || !hasScannedMarker ) { return; }

		JSONNode localDataJson = JSONNode.Parse( Resources.Load<TextAsset>( "_marker" ).text );

		string markerId = marker.GetComponent<ImageTarget>().id;
		for ( int i = 0; i < dataJson["marker"].Count; i++ )
		{
			if ( dataJson["marker"][i]["id"].Value == markerId )
			{
				dataJson["marker"][i]["objectOffset"][0] = localDataJson["marker"][i]["objectOffset"][0].Value;
				dataJson["marker"][i]["objectOffset"][1] = localDataJson["marker"][i]["objectOffset"][1].Value;
				dataJson["marker"][i]["objectOffset"][2] = localDataJson["marker"][i]["objectOffset"][2].Value;
				dataJson["marker"][i]["objectRotation"] = localDataJson["marker"][i]["objectRotation"].Value;
				dataJson["marker"][i]["height"] = localDataJson["marker"][i]["height"].Value;

				UpdateObjectPosition();
				SaveObjectPosition();

				break;
			}
		}
	}

}
