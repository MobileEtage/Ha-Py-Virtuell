using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavArrow : MonoBehaviour
{
	public Transform navArrow;
	public Transform mainCamera;
	public Transform targetFocusObject;
	public float navArrowVisibilityToleranceX = 0.6f;
	public float navArrowVisibilityToleranceY = 0.6f;
	public float navArrowFadeSpeed = 5;
	public float navArrowAngleSmoothTime = 0.2f;

	[Space( 10 )]

	public float navArrowMaxDist = 200;
	public float navArrowMinDist = 50;
	public float navArrowMoveToleranceX = 0.7f;
	public float navArrowMoveToleranceY = 0.7f;
	public float navArrowMoveSpeed = 5;

	private float screenWidth = 1920;
	private float screenHeight = 1080;
	private CanvasGroup navArrowCanvasGroup;
	private Transform navArrowTransform;
	private Vector3 targetPointScreenPos;
	private Vector3 screenMiddle;
	private Vector3 screenTargetVectorDir;
	private Vector3 screenUpVectorDir;
	private float navArrowTargetAlpha = 1;
	private float navArrowTargetYPos = 50;
	private float navArrowAngleVelocity = 0;

	public bool enableNavArrow = true;
	public bool focusComplete = false;
	private float focusCompleteTimer = 0;

	void Awake()
	{
		
	}

	void Start()
	{
		navArrowCanvasGroup = navArrow.GetComponent<CanvasGroup>();
		navArrowTransform = navArrow.GetChild( 0 );
		screenWidth = (float)Screen.width;
		screenHeight = (float)Screen.height;
	}

	void Update()
	{
		if ( enableNavArrow && !focusComplete && targetFocusObject != null )
		{
			navArrow.gameObject.SetActive( true );
			UpdateNavArrow();
		}
		else
		{
			navArrow.gameObject.SetActive( false );
		}
	}

	public void UpdateFocusObject(Transform parent, bool calcCenter = true)
	{
		if( targetFocusObject == null ) { targetFocusObject = new GameObject("NavArrowFocusObject").transform; }

		if ( calcCenter )
		{
			Vector3 pos = ToolsController.instance.GetCenter( parent.gameObject );
			targetFocusObject.position = pos;
		}
		else
		{
			targetFocusObject.position = parent.position;
		}

		targetFocusObject.SetParent(parent);

		enableNavArrow = true;
		focusComplete = false;
		focusCompleteTimer = 0;
	}

	public void UpdateNavArrow()
	{
		screenMiddle = new Vector3( screenWidth / 2f, screenHeight / 2f, 0 );
		screenUpVectorDir = new Vector3( screenWidth / 2f, screenHeight, 0 ) - screenMiddle;

		//Get the targets position on screen
		targetPointScreenPos = mainCamera.GetComponent<Camera>().WorldToScreenPoint(targetFocusObject.transform.position);

		//Ignore y axis while not focusing the correct x-area
		Vector2 viewPortTargetPos = new Vector2( targetPointScreenPos.x / screenWidth, targetPointScreenPos.y / screenHeight );
		if (
			viewPortTargetPos.x < 0 ||
			viewPortTargetPos.x > 1 ||
			targetPointScreenPos.z < 0
		)
		{
			targetPointScreenPos.y = screenHeight * 0.5f;
		}


		//Direction vector to target position on screen
		screenTargetVectorDir = targetPointScreenPos - screenMiddle;

		float angle = Vector2.SignedAngle( new Vector2( screenUpVectorDir.x, screenUpVectorDir.y ), new Vector2( screenTargetVectorDir.x, screenTargetVectorDir.y ) );
		if ( targetPointScreenPos.z < 0 )
		{
			angle += 180;
		}
		float lerpedAngle = Mathf.SmoothDampAngle( navArrow.transform.localEulerAngles.z, angle, ref navArrowAngleVelocity, navArrowAngleSmoothTime );
		navArrow.transform.localEulerAngles = new Vector3( 0, 0, lerpedAngle );

		UpdateNavArrowVisibility();
	}

	public void UpdateNavArrowVisibility()
	{
		// Update alpha visibility
		Vector2 viewPortTargetPos = new Vector2( targetPointScreenPos.x / screenWidth, targetPointScreenPos.y / screenHeight );
		if (
			viewPortTargetPos.x > navArrowVisibilityToleranceX ||
			viewPortTargetPos.x < (1 - navArrowVisibilityToleranceX) ||
			viewPortTargetPos.y > navArrowVisibilityToleranceY ||
			viewPortTargetPos.y < (1 - navArrowVisibilityToleranceY) ||
			targetPointScreenPos.z < 0
		)
		{
			navArrowTargetAlpha = 1;
		}
		else
		{
			navArrowTargetAlpha = 0;

			focusCompleteTimer += Time.deltaTime;
			if( focusCompleteTimer >= 1.0f ) { focusComplete = true; }
		}
		navArrowCanvasGroup.alpha = Mathf.Lerp( navArrowCanvasGroup.alpha, navArrowTargetAlpha, Time.deltaTime * navArrowFadeSpeed );


		// Move arrow to center if target point is almost in visible area

		if ( targetPointScreenPos.z < 0 )
		{
			navArrowTargetYPos = navArrowMaxDist;
		}
		else if ( viewPortTargetPos.x > 1 || viewPortTargetPos.x < 0 || viewPortTargetPos.y > 1 || viewPortTargetPos.y < 0 )
		{
			navArrowTargetYPos = navArrowMaxDist;
		}
		else if
	   (
		   ((viewPortTargetPos.x > navArrowMoveToleranceX && viewPortTargetPos.x < 1) ||
		   (viewPortTargetPos.x < (1 - navArrowMoveToleranceX) && viewPortTargetPos.x > 0)) &&
		   ((viewPortTargetPos.y > navArrowMoveToleranceY && viewPortTargetPos.y < 1) ||
		   (viewPortTargetPos.y < (1 - navArrowMoveToleranceY) && viewPortTargetPos.y > 0))
	   )
		{
			navArrowTargetYPos = navArrowMaxDist;
		}
		else
		{
			navArrowTargetYPos = navArrowMinDist;
		}

		float targetY = Mathf.Lerp( navArrowTransform.localPosition.y, navArrowTargetYPos, Time.deltaTime * navArrowMoveSpeed );
		navArrowTransform.localPosition = new Vector3( 0, targetY, 0 );

	}
}
