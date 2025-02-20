using UnityEngine;
using System.Collections;

/// <summary>
/// Touch.deltaPosition unterscheidet sich stark von der DeltaPosition, die man über Input.mousePosition berechnet, daher nutzen wir nur Input.mousePosition, 
/// da es universell auf allen Geräten nutzbar ist
/// </summary>
public class RotateObjectHandler : MonoBehaviour {

    Transform MyTransform;
    bool shouldRotate = false;
    Vector2 oldPosition = new Vector2(-1, -1);

    [Space(10)]

    public ZoomObjectHandler ZoomObjectHandler;

    [Space(10)]

	public bool RotationActive = true;

	[Range(10, 1000)]
	public float rotateSpeed = 100f;

    [Space(10)]

	public bool disableHorizontalAxis;
	public bool disableVerticalAxis;

	public enum RotationType
	{
		OnScreenHit,
		OnColliderHit
	}

    [Space(10)]

	public RotationType rotationtype;
	public Camera CameraForColliderHitCheck;

    [Space(10)]

	public bool useCustomAxis = false;
	public Vector3 customXRotationAxis;
	public Vector3 customYRotationAxis;

	void Start () {
		MyTransform = this.transform;
        if( CameraForColliderHitCheck == null )
            CameraForColliderHitCheck = Camera.main;
	}

	void Update () {

		if( RotationActive )
			rotate (); 
	}


	void rotate(){
		switch (rotationtype) {
		case RotationType.OnScreenHit:
			rotate_OnMouse ( false );
			break;
		case RotationType.OnColliderHit:
			rotate_OnMouse ( true );
			break;
		default:
			break;
		}
	}

		
	private bool VerifyColliderHit( Vector2 position ){
        Ray ray = CameraForColliderHitCheck.ScreenPointToRay( position );
		RaycastHit hit;
		
		if(GetComponent<Collider>() == null)
			gameObject.AddComponent(typeof(BoxCollider));
		
		if(Physics.Raycast(ray, out hit)){
			if(hit.collider.gameObject == this.gameObject){
				return true;
			}
		}
		return false;
	}


	void rotate_OnMouse( bool onColliderHit )
	{
		if (Input.GetMouseButtonDown (0)) {
			if (onColliderHit) {
				if (VerifyColliderHit ( Input.mousePosition ))
					shouldRotate = true;
			} else {
				shouldRotate = true;
			}
		}
		else if (Input.GetMouseButton (0) && shouldRotate) {

			if (oldPosition.x < 0) {
				oldPosition = Input.mousePosition;
			}
			Vector2 currentPosition = Input.mousePosition;

			Vector2 differnceVectorLastFrame = new Vector2 ((currentPosition.x / Screen.width), (currentPosition.y / Screen.height)) -
			                                   new Vector2 ((oldPosition.x / Screen.width), (oldPosition.y / Screen.height));
			oldPosition = currentPosition;

            if (!disableHorizontalAxis && !disableVerticalAxis) {
                
                if (useCustomAxis)
                {
                    MyTransform.Rotate(customYRotationAxis, -differnceVectorLastFrame.x * rotateSpeed, Space.World);
                    MyTransform.Rotate(customXRotationAxis, differnceVectorLastFrame.y * rotateSpeed, Space.World);
                }
                else
                {
                	if( CameraForColliderHitCheck == null ){
                    	MyTransform.Rotate(Vector3.up, -differnceVectorLastFrame.x * rotateSpeed, Space.World);
	                	MyTransform.Rotate(Vector3.right, differnceVectorLastFrame.y * rotateSpeed, Space.World);
	                }else{
	                	MyTransform.Rotate(CameraForColliderHitCheck.transform.up, -differnceVectorLastFrame.x * rotateSpeed, Space.World);
		                MyTransform.Rotate(CameraForColliderHitCheck.transform.right, differnceVectorLastFrame.y * rotateSpeed, Space.World);
	                }
                }

            } else if (disableVerticalAxis) {

                if (useCustomAxis)
                {
                    MyTransform.Rotate(customYRotationAxis, -differnceVectorLastFrame.x * rotateSpeed, Space.World);
                }
                else
                {
	                MyTransform.Rotate(Vector3.up, -differnceVectorLastFrame.x * rotateSpeed, Space.Self);
                }

            } else if (disableHorizontalAxis) {
                if (useCustomAxis)
                {
                    MyTransform.Rotate(customXRotationAxis, differnceVectorLastFrame.y * rotateSpeed, Space.World);
                }
                else
                {
                    MyTransform.Rotate(Vector3.right, differnceVectorLastFrame.y * rotateSpeed, Space.Self);
                }
			}
			
		}else if (Input.GetMouseButtonUp (0)) {
			shouldRotate = false;
			oldPosition = new Vector2(-1, -1);
		}
	}

	public void reset(){
		MyTransform.localEulerAngles = Vector3.zero;

        if(ZoomObjectHandler != null)
            ZoomObjectHandler.reset ();
	}

	
}
