using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusAreaController : MonoBehaviour
{
	public GameObject cam;
	public GameObject focusAreaViewposition;
	public GameObject focusAreaRotationHelper;
	public GameObject colliderBounds;

	[Space( 10 )]

	public float moveUpDownFactor = 0.01f;
	public Vector2 minMaxHeight = new Vector2( 2, 35 );
	public Vector2 moveUpDownOffset = new Vector2( -10, 20 );

	[Space( 10 )]

	public float rotateLeftRightFactor = 0.05f;
	public Vector2 minMaxRotationAngle = new Vector2( -10, 10 );
	private float targetYRotation = 0;

	[Space( 10 )]

	public float zoomSpeed = 50f;
	private float zoomTouchSpeed = 0.03f;
	private float targetZPosition = 0;
	public Vector2 minMaxZPosition = new Vector2( -2, 10 );
	private Vector2 touch1OldPos;
	private Vector2 touch2OldPos;
	private Vector2 touch1CurrentPos;
	private Vector2 touch2CurrentPos;

	private Vector3 previousMousePosition = new Vector3(-1, -1, -1);
	private Vector3 storedCameraPosition = Vector2.zero;
	private Vector3 storedCameraRotation = Vector2.zero;
	private bool isLoading = false;
	private bool isValidInput = true;

	void Start()
	{
		
	}

	void Update()
	{
		ValidateInput();
		if ( !isValidInput ) return;
		ControlCamera();
	}

	public void ValidateInput()
	{
		if ( Input.GetMouseButtonDown( 0 ) )
		{
			if ( ToolsController.instance.IsPointerOverUIObject2( true ) ) { isValidInput = false; }
		}

		if ( Input.GetMouseButtonUp( 0 ) )
		{
			isValidInput = true;
		}
	}

	public void ControlCamera()
	{
		HandleZoom();
		if ( Input.GetMouseButtonUp( 0 ) ) { previousMousePosition.x = -1; }
		if ( Input.touchCount >= 2 ) { previousMousePosition.x = -1; return; }
		if ( !Input.GetMouseButton( 0 ) ) return;

		Vector3 mousePosition = Input.mousePosition;
		if ( previousMousePosition.x == -1 ) { previousMousePosition = mousePosition; }
		Vector2 deltaMove = mousePosition - previousMousePosition;

		minMaxHeight.x = focusAreaViewposition.transform.position.y + moveUpDownOffset.x;
		minMaxHeight.y = focusAreaViewposition.transform.position.y + moveUpDownOffset.y;

		float moveUpDownDistance = -deltaMove.y * moveUpDownFactor;
		Vector3 targetPosition = cam.transform.position + new Vector3( 0, moveUpDownDistance, 0 );
		targetPosition.y = Mathf.Clamp( targetPosition.y, minMaxHeight.x, minMaxHeight.y );
		cam.transform.position = targetPosition;

		float rotateAngle = -deltaMove.x * rotateLeftRightFactor;
		targetYRotation = targetYRotation + rotateAngle;
		if ( minMaxRotationAngle.x > -360 || minMaxRotationAngle.y < 360 ) { targetYRotation = Mathf.Clamp( targetYRotation, minMaxRotationAngle.x, minMaxRotationAngle.y ); }
		focusAreaRotationHelper.transform.localEulerAngles = new Vector3( 0, targetYRotation, 0 );

		previousMousePosition = mousePosition;
	}

	public void HandleZoom()
	{
		if ( Input.mouseScrollDelta.magnitude > 0 )
		{
			float dir = Input.mouseScrollDelta.y;
			targetZPosition += dir * Time.deltaTime * zoomSpeed;
			targetZPosition = Mathf.Clamp( targetZPosition, minMaxZPosition.x, minMaxZPosition.y );

			if ( colliderBounds != null )
			{
				//focusAreaRotationHelper.transform.localPosition = new Vector3(focusAreaRotationHelper.transform.localPosition.x, focusAreaRotationHelper.transform.localPosition.y, targetZPosition);

				Vector3 currentPosition = focusAreaRotationHelper.transform.position;
				Vector3 forward = focusAreaRotationHelper.transform.forward * (dir * Time.deltaTime * zoomSpeed);
				focusAreaRotationHelper.transform.position += new Vector3( forward.x, 0, forward.z );

				bool isInBounds = false;
				BoxCollider[] colliders = colliderBounds.GetComponentsInChildren<BoxCollider>();
				for ( int i = 0; i < colliders.Length; i++ )
				{
					//if( colliders[i].bounds.Contains(focusAreaRotationHelper.transform.position) )
					if ( ToolsController.instance.IsInsideBounds( focusAreaRotationHelper.transform.position, colliders[i] ) ) { isInBounds = true; break; }
				}

				isInBounds = false;
				if ( !isInBounds )
				{
					focusAreaRotationHelper.transform.position = currentPosition;
				}
			}
			else { focusAreaRotationHelper.transform.localPosition = new Vector3( focusAreaRotationHelper.transform.localPosition.x, focusAreaRotationHelper.transform.localPosition.y, targetZPosition ); }
		}
		else if ( Input.touchCount > 0 )
		{
			if ( Input.touchCount > 0 )
			{
				Touch touch1 = Input.GetTouch( 0 );
				if ( touch1.phase == TouchPhase.Began ) { touch1OldPos = touch1.position; }
			}

			if ( Input.touchCount > 1 )
			{
				Touch touch2 = Input.GetTouch( 1 );
				if ( touch2.phase == TouchPhase.Began ) { touch2OldPos = touch2.position; }
			}

			if ( Input.touchCount == 2 )
			{
				Touch touch1 = Input.GetTouch( 0 );
				Touch touch2 = Input.GetTouch( 1 );
				if ( touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved )
				{
					touch1CurrentPos = touch1.position;
					touch2CurrentPos = touch2.position;
					float deltaDistance = Vector2.Distance( touch1CurrentPos, touch2CurrentPos ) - Vector2.Distance( touch1OldPos, touch2OldPos );

					// Use finger inch move distance
					float curDist = GetInchDistance( touch1CurrentPos, touch2CurrentPos );
					float oldDist = GetInchDistance( touch1OldPos, touch2OldPos );
					deltaDistance = curDist - oldDist;
					deltaDistance *= 400;

					if ( colliderBounds != null )
					{
						Vector3 currentPosition = focusAreaRotationHelper.transform.position;
						Vector3 forward = focusAreaRotationHelper.transform.forward * (deltaDistance * zoomTouchSpeed);
						focusAreaRotationHelper.transform.position += new Vector3( forward.x, 0, forward.z );

						bool isInBounds = false;
						BoxCollider[] colliders = colliderBounds.GetComponentsInChildren<BoxCollider>();
						for ( int i = 0; i < colliders.Length; i++ )
						{
							//if( colliders[i].bounds.Contains(focusAreaRotationHelper.transform.position) )
							if ( ToolsController.instance.IsInsideBounds( focusAreaRotationHelper.transform.position, colliders[i] ) ) { isInBounds = true; break; }
						}

						isInBounds = false;
						if ( !isInBounds )
						{
							focusAreaRotationHelper.transform.position = currentPosition;
						}
					}
					else
					{
						targetZPosition += -deltaDistance * zoomTouchSpeed * -1;
						targetZPosition = Mathf.Clamp( targetZPosition, minMaxZPosition.x, minMaxZPosition.y );
						focusAreaRotationHelper.transform.localPosition = new Vector3( focusAreaRotationHelper.transform.localPosition.x, focusAreaRotationHelper.transform.localPosition.y, targetZPosition );
					}

					touch1OldPos = touch1CurrentPos;
					touch2OldPos = touch2CurrentPos;
				}
			}
		}
	}

	public float GetInchDistance(Vector2 pos1, Vector2 pos2)
	{
		float pixelDistance = Vector2.Distance( pos1, pos2 );
		float dpi = Screen.dpi;

		if ( dpi <= 0 )
		{

#if UNITY_STANDALONE
			dpi = 81;
#else
			dpi = 264;
#endif
		}

		if ( pixelDistance > 0 ) { return pixelDistance / dpi; }
		else { return 0; }
	}

	public void Reset()
	{
		previousMousePosition = new Vector3( -1, -1, -1 );
		touch1OldPos = Vector2.zero;
		touch2OldPos = Vector2.zero;
		touch1CurrentPos = Vector2.zero;
		touch2CurrentPos = Vector2.zero;
		storedCameraPosition = Vector3.zero;
		storedCameraRotation = Vector3.zero;
		targetYRotation = 0;
		targetZPosition = 0;

		cam.transform.localPosition = Vector3.zero;
		cam.transform.localEulerAngles = Vector3.zero;
		focusAreaRotationHelper.transform.localPosition = Vector3.zero;
		focusAreaRotationHelper.transform.localEulerAngles = Vector3.zero;
	}
}
