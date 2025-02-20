using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARController : MonoBehaviour
{
    public bool increaseScanTime = false;
    public bool scanAnimationAlwaysVisible = false;
    public bool skipScanEditor = false;
    public bool skipScanMarkerEditor = false;
    public float onMarkerTrackedUpdatePositionInterval = 2.0f;
    public enum ScanPlanesType { DefaultEdit, UserAnimation, Wireframe, Invisible }
    public ScanPlanesType scanPlanesType;

    private float minScanTime = 2.0f;       // Old: 5
    private float currentScanTime = 0.0f;

    [Space(10)]

    public ARSession arSession;
    public ScanAnimator scanAnimator;
    public GameObject singlePlane;
    public Material defaultPlaneMaterial;
    public Material scanPlaneMaterial;
    public Material wireframe;

    [Space(10)]

    public ARRaycastManager m_RaycastManager;
    public ARPointCloudManager arPointCloudManager;
    public ARPlaneManager arPlaneManager;
	public GameObject mainCamera;
	public NavArrow navArrow;

	[Space(10)]

	public LineRenderer arcRenderer;
	public GameObject positionMarker;
	public float angle = 10;
	public float strength = 5;
	public int vertexToRemove = 5;
	public LayerMask rayCastLayerMask;
	private List<Vector3> vertexList = new List<Vector3>();
	private bool groundDetected = false;
	private bool isEnabling = false;

	private bool isActive = false;
    private bool scanInitialized = false;
    private bool scanningCoroutineActive = false;
    private bool floorTracked = false;
    [HideInInspector] public bool isScanning = false;

    private TrackingState trackingState;
    private static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

	private LayerMask withARPlanesLayermask;
	private LayerMask withoutARPlanesLayermask;

	public static ARController instance;
    void Awake()
    {
        instance = this;

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_EDITOR

        if (increaseScanTime) { minScanTime = 20; }
        singlePlane.SetActive(true);
        mainCamera.transform.position = new Vector3(0, 1.7f, -4);
        mainCamera.transform.eulerAngles = new Vector3(10f, 0, 0);
#else
		singlePlane.SetActive(false);
#endif

#if !UNITY_EDITOR
		mainCamera.GetComponent<Camera>().backgroundColor = Color.black;
#endif


    }

	void Start()
	{
		withARPlanesLayermask = mainCamera.GetComponent<Camera>().cullingMask;
		ToolsController.instance.AddLayerToLayerMask( ref withARPlanesLayermask, "ARPlane" );
		withoutARPlanesLayermask = mainCamera.GetComponent<Camera>().cullingMask;
		ToolsController.instance.RemoveLayerFromLayerMask( ref withoutARPlanesLayermask, "ARPlane" );
	}

	void Update()
    {
#if UNITY_EDITOR
		MoveCamera();
#endif
		if (isScanning && !scanAnimationAlwaysVisible)
        {
            HandleScan();
        }

        if (!isActive) return;
        UpdateTrackingStatus();
    }

	public void MoveCamera()
	{
		float walkSpeed = 3.0f;
		float rotSpeed = 10.0f;
		if ( Input.GetKey( KeyCode.W ) )
		{
			//mainCamera.transform.localPosition += mainCamera.transform.forward * walkSpeed * Time.deltaTime;
			mainCamera.transform.localPosition += new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z) * walkSpeed * Time.deltaTime;
		}
		if ( Input.GetKey( KeyCode.S ) )
		{
			//mainCamera.transform.localPosition -= mainCamera.transform.forward * walkSpeed * Time.deltaTime;
			mainCamera.transform.localPosition -= new Vector3( mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z ) * walkSpeed * Time.deltaTime;
		}
		if ( Input.GetKey( KeyCode.D ) )
		{
			//mainCamera.transform.localPosition += mainCamera.transform.right * walkSpeed * Time.deltaTime;
			mainCamera.transform.localPosition += new Vector3( mainCamera.transform.right.x, 0, mainCamera.transform.right.z ) * walkSpeed * Time.deltaTime;
		}
		if ( Input.GetKey( KeyCode.A ) )
		{
			//mainCamera.transform.localPosition -= mainCamera.transform.right * walkSpeed * Time.deltaTime;
			mainCamera.transform.localPosition -= new Vector3( mainCamera.transform.right.x, 0, mainCamera.transform.right.z ) * walkSpeed * Time.deltaTime;
		}
		if ( Input.GetKey( KeyCode.E ) )
		{
			mainCamera.transform.localEulerAngles += new Vector3( 0f, rotSpeed * Time.deltaTime, 0f );
		}
		if ( Input.GetKey( KeyCode.Q ) )
		{
			mainCamera.transform.localEulerAngles -= new Vector3( 0f, rotSpeed * Time.deltaTime, 0f );
		}
	}

	public void StartScan()
    {
        isActive = true;
        StartScan(2, 5);
    }

    public void StartScan(float startDelay, float endDelay)
    {
        if (scanningCoroutineActive) return;
        scanningCoroutineActive = true;
        StartCoroutine(StartScanCoroutine(startDelay, endDelay));
    }

    public void HideScan()
    {
        if (scanningCoroutineActive) return;
        scanningCoroutineActive = true;
        StartCoroutine(HideScanCoroutine());
    }

    private IEnumerator StartScanCoroutine(float startDelay, float endDelay)
    {

        yield return new WaitForSeconds(startDelay);
        scanAnimator.ShowScanAnimation();
        arPointCloudManager.enabled = true;
        //arPlaneManager.enabled = true;
        yield return new WaitForSeconds(endDelay);

        scanInitialized = true;
        scanningCoroutineActive = false;
    }

    private IEnumerator HideScanCoroutine()
    {

        scanAnimator.HideScanAnimation();

        yield return new WaitForSeconds(1);

        scanInitialized = true;
        scanningCoroutineActive = false;

        if (!floorTracked)
        {
            floorTracked = true;
        }
    }

    private void UpdateTrackingStatus()
    {

        if (scanInitialized && !floorTracked)
        {

            if (IsTracking())
            {
                HideScan();
            }
            else
            {
                StartScan(0, 2);
            }
        }
    }

    public void UpdateScanPlanesType(ScanPlanesType scanPlanesType)
    {

        this.scanPlanesType = scanPlanesType;
        ShowHidePlanes(!(scanPlanesType == ScanPlanesType.Invisible));
    }

    public void ShowScanAnimation()
    {

#if UNITY_EDITOR
        //if( skipScanEditor ){ EditStationController.instance.ContinueScanStep();return; }
#endif

        scanAnimator.ShowScanAnimation();
        currentScanTime = 0;
        isScanning = true;
    }

    public void ShowHidePlanes(bool shouldShow)
    {
        //LayerMask withARPlanesLayermask = LayerMask.GetMask("Default", "TransparentFX", "Ignore Raycast", "Water", "UI", "Plane", "ARPlane", "Pigeon", "Avatar", "Coal");
        //LayerMask withoutARPlanesLayermask = LayerMask.GetMask("Default", "TransparentFX", "Ignore Raycast", "Water", "UI", "Plane", "Pigeon", "Avatar", "Coal");

        mainCamera.GetComponent<Camera>().cullingMask = shouldShow ? withARPlanesLayermask : withoutARPlanesLayermask;
	}

	public void HideScanAnimation()
    {

        scanAnimator.HideScanAnimation();
        currentScanTime = 0;
        isScanning = false;
    }

    public void HandleScan()
    {

        float minScanArea = 3;  // Old: 6
#if UNITY_ANDROID
        minScanArea = 10;       // Old 10
#endif

        UpdateTrackedAreas();
        float maxScanTime = 6.0f;   // Old: 10

#if UNITY_EDITOR
        if (increaseScanTime) { maxScanTime = 30; }
#endif

        if (
            (currentScanTime < minScanTime || GetTrackedArea() < minScanArea) &&
            currentScanTime < maxScanTime
        )
        {
            if (
                !VideoController.instance.videoSite.activeInHierarchy &&
                !ScanController.instance.scanDialog.activeInHierarchy &&
                !ScanController.instance.defaultScanDialog.activeInHierarchy
            )
            {
                currentScanTime += Time.deltaTime;
            }

        }
        else
        {

            isScanning = false;
            scanAnimator.HideScanAnimation();
            EditStationController.instance.ContinueScanStep();
        }
    }

    public float GetTrackedArea()
    {

#if UNITY_EDITOR
        return 100.0f;
#endif

        TrackableCollection<ARPlane> planes = arPlaneManager.trackables;

        float areaFromExtents = 0;
        float areaFromSize = 0;
        float scannedArea = 0;

        foreach (ARPlane plane in planes)
        {

            areaFromExtents += (plane.extents.x * plane.extents.y);
            areaFromSize += (plane.size.x * plane.size.y);

            List<Vector2> boundaryPoints = new List<Vector2>();
            for (int i = 0; i < plane.boundary.Length; i++)
            {
                boundaryPoints.Add(plane.boundary[i]);
            }

            float area = ToolsController.instance.GetArea(boundaryPoints);
            scannedArea += area;
        }

        return scannedArea;
    }

    public void UpdateTrackedAreas()
    {

        if (scanPlanesType == ScanPlanesType.Invisible) return;

        TrackableCollection<ARPlane> planes = arPlaneManager.trackables;
        foreach (ARPlane plane in planes)
        {

            if (scanPlanesType == ScanPlanesType.UserAnimation)
            {

                plane.GetComponent<Renderer>().material = scanPlaneMaterial;
                plane.GetComponent<LineRenderer>().SetWidth(0, 0);

                Vector4 pos = new Vector4(
                    mainCamera.transform.position.x,
                    plane.transform.position.y,
                    mainCamera.transform.position.z, 0);
                plane.GetComponent<Renderer>().material.SetVector("_Center", pos);
            }
            else if (scanPlanesType == ScanPlanesType.Wireframe)
            {

                plane.GetComponent<Renderer>().material = wireframe;
                //plane.GetComponent<LineRenderer>().SetWidth(0.01f, 0.01f);
                plane.GetComponent<LineRenderer>().SetWidth(0.0f, 0.0f);
            }
            else if (scanPlanesType == ScanPlanesType.DefaultEdit)
            {

                plane.GetComponent<Renderer>().material = defaultPlaneMaterial;
                //plane.GetComponent<LineRenderer>().SetWidth(0.01f, 0.01f);
                plane.GetComponent<LineRenderer>().SetWidth(0.0f, 0.0f);
            }
        }
    }

    public bool IsTracking()
    {

        return arPlaneManager.trackables.count > 0;
        //return arSession.subsystem.trackingState == 
        //	TrackingState.Tracking || arSession.subsystem.trackingState == TrackingState.Limited ;
    }

    public void CheckTrackingQuality()
    {

        string trackingFeedback = "";

        if (!IsTracking())
        {

            switch (ARSession.notTrackingReason)
            {
                case NotTrackingReason.None:
                    break;
                case NotTrackingReason.Initializing:
                    break;
                case NotTrackingReason.Relocalizing:
                    break;
                case NotTrackingReason.InsufficientLight:
                    trackingFeedback = "Schlechte Lichtverhältnisse!";
                    break;
                case NotTrackingReason.InsufficientFeatures:
                    trackingFeedback = "Raum unzureichend gesacnnt!";
                    break;
                case NotTrackingReason.ExcessiveMotion:
                    trackingFeedback = "Zu schnelle Bewegung!";
                    break;
                case NotTrackingReason.Unsupported:
                    break;
            }
        }

        if (trackingFeedback != "")
        {
            print(trackingFeedback);
        }
    }

    public void InitARFoundation()
    {

        if (arSession.enabled) return;

        //mainCamera.GetComponent<ARCameraManager>().requestedFacingDirection = worldDirection ? CameraFacingDirection.World:CameraFacingDirection.User;
        if (ARSession.state != ARSessionState.Ready) { arSession.attemptUpdate = true; }
        arSession.enabled = true;
        arPlaneManager.enabled = true;
        ImageTargetController.Instance.SetTrackingStateNone();
    }

    public void StopAndResetARSession()
    {
        print("StopAndResetARSession");

        arPlaneManager.enabled = false;
        if (arSession.enabled)
        {
            arSession.Reset();
            arSession.enabled = false;
        }

        ImageTargetController.Instance.SetTrackingStateNone();
    }

    public void StopARSession()
    {

        print("StopARSession");

        if (Params.alwaysResetARSession) { StopAndResetARSession(); }
        else
        {
            if (arSession.enabled) { arSession.enabled = false; }
            arPlaneManager.enabled = false;
            ImageTargetController.Instance.SetTrackingStateNone();
        }
    }

    public void DisableARSession()
    {
        if (arSession.enabled) { arSession.enabled = false; }
        arPlaneManager.enabled = false;
        ImageTargetController.Instance.SetTrackingStateNone();
    }

    /**********************************************************************/
    /*************************** Raycast helper ***************************/
    /**********************************************************************/

    public bool TryGetHitPositionWithScreenPosition(Vector2 screenPoint, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        if (RaycastHit(screenPoint, out worldPos))
        {
            return true;
        }
        return false;
    }

    public bool TryGetHitPosition(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        if (!TryGetTouchPosition(out Vector2 touchPos))
            return false;

        if (RaycastHit(touchPos, out worldPos))
        {
            return true;
        }
        return false;
    }

	public bool CurvedRaycastHit(Vector2 touchPosition, out Vector3 worldPos)
	{
		worldPos = Vector3.zero;
		return CurvedRaycastHit( touchPosition, Vector3.zero, out worldPos );
	}

	public bool CurvedRaycastHit(Vector2 touchPosition, Vector3 lastPosition, out Vector3 worldPos)
	{
		worldPos = lastPosition;

		Vector3 groundPos = Vector3.zero;
		Vector3 lastNormal = Vector3.zero;
		float vertexDelta = 0.02f;
		int maxVertexcount = 100;
		
		
		Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay( touchPosition );
		RaycastHit hit;

		groundDetected = false;
		vertexList.Clear();
		Vector3 velocity = Quaternion.AngleAxis( -angle, mainCamera.transform.right ) * ray.direction * strength;
		Vector3 pos = ray.origin;
		vertexList.Add( pos );

		while ( !groundDetected && vertexList.Count < maxVertexcount )
		{
			Vector3 newPos = pos + velocity * vertexDelta + 0.5f * Physics.gravity * vertexDelta * vertexDelta;
			velocity += Physics.gravity * vertexDelta;
			vertexList.Add( newPos );

			if ( Physics.Linecast( pos, newPos, out hit, rayCastLayerMask ) )
			{
				vertexList.RemoveAt( vertexList.Count - 1 );
				groundDetected = true;
				groundPos = hit.point;
				lastNormal = hit.normal;
				worldPos = groundPos;
				vertexList.Add( groundPos );
			}
			pos = newPos;
		}

		if ( groundDetected && !arcRenderer.gameObject.activeInHierarchy && !isEnabling )
		{
			isEnabling = true;
			StartCoroutine( EnableArcCoroutine() );
		}
		else if ( !groundDetected && arcRenderer.gameObject.activeInHierarchy )
		{
			arcRenderer.gameObject.SetActive( false );
		}
		positionMarker.SetActive( groundDetected );

		if ( groundDetected )
		{
			positionMarker.transform.position = groundPos + lastNormal * 0.03f;
			positionMarker.transform.LookAt( groundPos );
		}

		int vertexToRemoveTmp = vertexToRemove;
		while ( vertexList.Count > vertexToRemoveTmp && vertexToRemoveTmp > 0)
		{
			vertexList.RemoveAt(0);
			vertexToRemoveTmp--;
		}

		arcRenderer.positionCount = vertexList.Count;
		arcRenderer.SetPositions( vertexList.ToArray() );

		return groundDetected;
	}

	public void DisableArcRenderer()
	{
		arcRenderer.gameObject.SetActive( false );
		positionMarker.gameObject.SetActive( false );
	}

	public IEnumerator EnableArcCoroutine()
	{
		yield return new WaitForEndOfFrame();
		if ( groundDetected ){ arcRenderer.gameObject.SetActive( true ); }
		isEnabling = false;
	}

	public bool RaycastHit(Vector2 touchPosition, out Vector3 worldPos)
	{
		worldPos = Vector3.zero;
		return RaycastHit(touchPosition, Vector3.zero, out worldPos);
	}

	public bool RaycastHit(Vector2 touchPosition, Vector3 lastPosition, out Vector3 worldPos)
    {
        worldPos = lastPosition;

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            worldPos = s_Hits[0].pose.position;
            return true;
        }
        else if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinBounds))
        {
            worldPos = s_Hits[0].pose.position;
            return true;
        }
        else if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneEstimated))
        {
            worldPos = s_Hits[0].pose.position;
            return true;
        }
        else if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinInfinity))
        {
            worldPos = s_Hits[0].pose.position;
            return true;
        }

#if UNITY_EDITOR
        //if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
            RaycastHit[] hits;
            hits = Physics.RaycastAll(ray, 100);
            //Debug.DrawRay(ray.origin, ray.direction*10, Color.red);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject == singlePlane)
                {
                    worldPos = hits[i].point;
                    return true;
                }
            }
        }
#endif

        return false;
    }

    public bool RaycastHit(Vector2 touchPosition, bool horizontalPlanesOnly, out Vector3 worldPos)
    {

        worldPos = Vector3.zero;

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            worldPos = s_Hits[0].pose.position;

            if (horizontalPlanesOnly)
            {

                ARPlane arPlane = arPlaneManager.GetPlane(s_Hits[0].trackableId);

                print("1ARPlane " + arPlane);
                if (arPlane != null) { print(arPlane.alignment.ToString()); }

                if (arPlane != null && arPlane.alignment == PlaneAlignment.Vertical) { return false; }
                else if (arPlane != null && arPlane.alignment == PlaneAlignment.HorizontalUp) { return true; }
                else if (arPlane != null && arPlane.alignment == PlaneAlignment.HorizontalDown) { return true; }
                else { return false; }
            }
            //return true;
        }
        else if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinBounds))
        {
            worldPos = s_Hits[0].pose.position;

            if (horizontalPlanesOnly)
            {

                ARPlane arPlane = arPlaneManager.GetPlane(s_Hits[0].trackableId);

                print("2ARPlane " + arPlane);
                if (arPlane != null) { print(arPlane.alignment.ToString()); }

                if (arPlane != null && arPlane.alignment == PlaneAlignment.Vertical) { return false; }
                else if (arPlane != null && arPlane.alignment == PlaneAlignment.HorizontalUp) { return true; }
                else if (arPlane != null && arPlane.alignment == PlaneAlignment.HorizontalDown) { return true; }
                else { return false; }
            }
            //return true;
        }
        else if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneEstimated))
        {
            worldPos = s_Hits[0].pose.position;

            if (horizontalPlanesOnly)
            {

                ARPlane arPlane = arPlaneManager.GetPlane(s_Hits[0].trackableId);

                print("3ARPlane " + arPlane);
                if (arPlane != null) { print(arPlane.alignment.ToString()); }

                if (arPlane != null && arPlane.alignment == PlaneAlignment.Vertical) { return false; }
                else if (arPlane != null && arPlane.alignment == PlaneAlignment.HorizontalUp) { return true; }
                else if (arPlane != null && arPlane.alignment == PlaneAlignment.HorizontalDown) { return true; }
                else { return false; }
            }
            //return true;
        }
        else if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinInfinity))
        {
            worldPos = s_Hits[0].pose.position;

            if (horizontalPlanesOnly)
            {

                ARPlane arPlane = arPlaneManager.GetPlane(s_Hits[0].trackableId);

                print("4ARPlane " + arPlane);
                if (arPlane != null) { print(arPlane.alignment.ToString()); }

                if (arPlane != null && arPlane.alignment == PlaneAlignment.Vertical) { return false; }
                else if (arPlane != null && arPlane.alignment == PlaneAlignment.HorizontalUp) { return true; }
                else if (arPlane != null && arPlane.alignment == PlaneAlignment.HorizontalDown) { return true; }
                else { return false; }
            }
            //return true;
        }

#if UNITY_EDITOR
        //if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
            RaycastHit[] hits;
            hits = Physics.RaycastAll(ray, 100);
            //Debug.DrawRay(ray.origin, ray.direction*10, Color.red);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject == singlePlane)
                {
                    worldPos = hits[i].point;
                    return true;
                }
            }
        }
#endif

        return false;
    }

    private bool RaycastHit(Vector2 touchPosition)
    {

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            return true;
        }

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinBounds))
        {
            return true;
        }

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneEstimated))
        {
            return true;
        }

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinInfinity))
        {
            return true;
        }

        return false;
    }

    public bool TryGetTouchPosition(out Vector2 touchPosition)
    {
#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            var mousePosition = Input.mousePosition;
            touchPosition = new Vector2(mousePosition.x, mousePosition.y);
            return true;
        }
#else
		if (Input.touchCount > 0)
		{
		touchPosition = Input.GetTouch(0).position;
		return true;
		}
#endif

        touchPosition = default;
        return false;
    }

    public Vector3 GetGroundPositionInfrontUser(bool useRaycastHit = true)
    {
        float minDist = 1.0f;
        Vector2 touchPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 1;
        float yPosition = mainCamera.transform.position.y - 1.4f;

        if (useRaycastHit && RaycastHit(touchPosition, out hitPosition))
        {
            float dist = Vector3.Distance(mainCamera.transform.position, hitPosition);

			if ( dist > 5 || dist < 0.5f)
            {
                //Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
                //hitPosition = rayTemp.origin + rayTemp.direction * minDist - Vector3.up * 1.0f;
            }
            else
            {
                yPosition = hitPosition.y;
            }
        }

        Vector3 dir = mainCamera.transform.forward;
        dir.y = 0;
		Vector3 pos = new Vector3( mainCamera.transform.position.x, yPosition, mainCamera.transform.position.z ) + (dir.normalized * 0.5f);
		return pos;
    }
}
