using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// This controller can be used to drag a menu
// Add an Event trigger to the area which should be dragged. Add a pointer down event and call OnHitDragArea

 
public class DragMenuController : MonoBehaviour
{
	[Header("The object to move")]
	public RectTransform dragContent;
	
	[Header("The root canvas to calculate height")]
	public RectTransform canvas;
	
	[Header("Optional mask image")]
	public CanvasGroup backgroundImage;
	
	[Header("Open Close events")]
	public UnityEvent OnClose;
	public UnityEvent OnOpen;

    [Space(10)]

    private float onOpenCloseMinDistancePercentage = 0.001f; // 0.01 (origin)

    public float minDragDistancePercentage = 0.01f;						
	public float menuClosedPrecentage = -0.47f;
	public float menuOpenPrecentage = -0.125f;
	public float menuPositionResetSpeed = 10f;
	public float fastOpenCloseMaxTime = 0.3f;

    public bool hideContentOnClose = true;
    public bool hitDragArea = false;
    public bool dragEnabled = false;
	public bool moveEnabled = false;
	public bool menuIsOpen = false;
	public bool enabledDrag = true;
	public bool isMoving = false;
	private float disableMovingTime = 1.0f;
	private float currentDisableMovingTime = 0.0f;

    private Vector3 lastMousePosition = new Vector3(-1, -1, -1);
    private Vector3 mousePositionDown = new Vector3(-1, -1, -1);
	private float dragTime = 0;
	private bool notifyOnClose = true;

    void Start(){
		
		Vector2 pos = dragContent.anchoredPosition;
		if( menuIsOpen ){
			pos.y = GetCanvasHeight()*menuOpenPrecentage;
		}else{
			pos.y = GetCanvasHeight()*menuClosedPrecentage;
		}
		dragContent.anchoredPosition = pos;
	}
		
	void Update(){
		
		UpdateDrag();
	}
	
	public void CloseImmediate(){
		
		menuIsOpen = false;
		Vector2 pos = dragContent.anchoredPosition;
		pos.y = GetCanvasHeight()*menuClosedPrecentage;
		dragContent.anchoredPosition = pos;
		if( backgroundImage ) backgroundImage.gameObject.SetActive(false);
	}
	
	public void OpenImmediate(){
		
		menuIsOpen = true;
		Vector2 pos = dragContent.anchoredPosition;
		pos.y = GetCanvasHeight()*menuOpenPrecentage;
		dragContent.anchoredPosition = pos;
		dragContent.gameObject.SetActive(true);
		if( backgroundImage ) backgroundImage.gameObject.SetActive(true);
	}
	
	public void SetPosition( float percentage ){
		
		Vector2 pos = dragContent.anchoredPosition;
		pos.y = GetCanvasHeight()*percentage;
		dragContent.anchoredPosition = pos;
	}
	
	public void UpdateDrag(){
		
		if( !isMoving ){
			
			if( Input.GetMouseButtonDown(0) ){
				
				mousePositionDown = Input.mousePosition;
				dragTime = 0;
			}
			
			else if( Input.GetMouseButton(0) ){
				
				if( hitDragArea && !dragEnabled ){
	
					float pixelDist = Mathf.Abs( mousePositionDown.y - Input.mousePosition.y );
					float minPixelDist = minDragDistancePercentage * Screen.height;
					if( pixelDist > minPixelDist ){
						
						//print( "Drag started" );
						dragEnabled = true;
						moveEnabled = true;
					}						
				}
				
				if( dragEnabled ){
				
					DragMenu();
					dragTime += Time.deltaTime;
				}

            }
			
			else if( Input.GetMouseButtonUp(0) ){
				
				if( dragEnabled ){
					OnReleaseDragContent();
				}
				
				dragEnabled = false;
				hitDragArea = false;
				lastMousePosition = new Vector3(-1,-1, -1);
				mousePositionDown = new Vector3(-1,-1, -1);			
			}
		}
		else{
			
			currentDisableMovingTime += Time.deltaTime;
			if( currentDisableMovingTime > disableMovingTime ){
				isMoving = false;
			}
		}
		
		UpdateDragContentPosition();
    }
	
	public void UpdateDragContentPosition(){
		
		if( !isMoving ){
			if( dragEnabled || !moveEnabled ) return;
		}
		
		Vector2 pos = dragContent.anchoredPosition;

		if( menuIsOpen ){
			
			pos.y = GetCanvasHeight()*menuOpenPrecentage;
			
		}else{
			pos.y = GetCanvasHeight()*menuClosedPrecentage;
		}
		
		dragContent.anchoredPosition = Vector2.Lerp( dragContent.anchoredPosition, pos, Time.deltaTime * menuPositionResetSpeed );
		if(backgroundImage) backgroundImage.alpha = Mathf.Lerp( backgroundImage.alpha, menuIsOpen ? 1:0, Time.deltaTime * menuPositionResetSpeed );
		
		float distToTarget = Mathf.Abs( dragContent.anchoredPosition.y - pos.y );
		float distToTargetPercentage = distToTarget/GetCanvasHeight();
		
		if( distToTargetPercentage < onOpenCloseMinDistancePercentage ){
			
			if( menuIsOpen ){
			
				OnMenuOpened();
			
			}else{
				
				OnMenuClosed();
			}
			
			if(backgroundImage) backgroundImage.alpha = menuIsOpen ? 1:0;
		}
	}
	
	public void OnReleaseDragContent(){

        // Check if we should close or keep the content open

        if (dragTime < fastOpenCloseMaxTime)
        {
            if (menuIsOpen && Input.mousePosition.y <= lastMousePosition.y)
            {
                menuIsOpen = false;
                return;
            }
            else if (!menuIsOpen && Input.mousePosition.y >= lastMousePosition.y)
            {
                menuIsOpen = true;
                return;
            }
        }

        Vector2 pos = dragContent.anchoredPosition;
        float posPercentage = pos.y / GetCanvasHeight();
        float distToOpen = Mathf.Abs(posPercentage - menuOpenPrecentage);
        float distToClose = Mathf.Abs(posPercentage - menuClosedPrecentage);
        float tolerance = 0.5f;
        if (!menuIsOpen && (distToOpen*tolerance) < distToClose) { menuIsOpen = true; }
        else if (menuIsOpen && (distToClose* tolerance) < distToOpen) { menuIsOpen = false; }


        /*
        Vector2 pos = dragContent.anchoredPosition;
        float posClose = GetCanvasHeight() * menuClosedPrecentage;
        float posOpen = GetCanvasHeight() * menuOpenPrecentage;
        posClose = -(Mathf.Abs(posOpen) + (Mathf.Abs(posClose) - Mathf.Abs(posOpen)) * (0.5f));
        float distToClose = Mathf.Abs( pos.y - posClose );
		float distToOpen = Mathf.Abs( pos.y - posOpen );      

        if ( distToOpen < distToClose ){		
			menuIsOpen = true;
		}
		else{
			menuIsOpen = false;
		}
		
		if( dragTime < fastOpenCloseMaxTime ){		
			menuIsOpen = Input.mousePosition.y >= lastMousePosition.y;
		}
        */
    }

    public void DragMenu(){
		
		if( lastMousePosition.y == -1 ){
			lastMousePosition = Input.mousePosition;
		}
		
		Vector3 mouseDelta = lastMousePosition - Input.mousePosition;
		Vector3 mouseDeltaCanvasUnits = ( GetCanvasHeight() / (float)Screen.height ) * mouseDelta;
		Vector3 currentPos = dragContent.anchoredPosition;
		currentPos -= mouseDeltaCanvasUnits;
		dragContent.anchoredPosition = new Vector2( 0, Mathf.Clamp( currentPos.y, GetCanvasHeight()*menuClosedPrecentage, GetCanvasHeight()*menuOpenPrecentage ) );		
		lastMousePosition = Input.mousePosition;
	}
	
	private void OnMenuClosed(){
		
		if( !dragContent.gameObject.activeInHierarchy ){ return; }

        //print("OnMenuClosed");

        Vector2 pos = dragContent.anchoredPosition;
        pos.y = GetCanvasHeight() * menuClosedPrecentage;
        dragContent.anchoredPosition = pos;

        if (hideContentOnClose) dragContent.gameObject.SetActive(false);
		if( backgroundImage ) backgroundImage.gameObject.SetActive(false);
		
		if( notifyOnClose ){ OnClose.Invoke(); }
		notifyOnClose = true;
		moveEnabled = false;
	}
	
	private void OnMenuOpened(){

        //print("OnMenuOpened");

        Vector2 pos = dragContent.anchoredPosition;
        pos.y = GetCanvasHeight() * menuOpenPrecentage;
        dragContent.anchoredPosition = pos;

        OnOpen.Invoke();
		moveEnabled = false;
	}
	
	public void Open(){
		
		dragContent.gameObject.SetActive(true);
		if( backgroundImage ) backgroundImage.gameObject.SetActive(true);
		
		notifyOnClose = true;
		menuIsOpen = true;
		isMoving = true;
		currentDisableMovingTime = 0;
	}
	
	public void CloseWithoutNotify(){
		
		notifyOnClose = false;
		menuIsOpen = false;
		isMoving = true;
		currentDisableMovingTime = 0;
	}
	
	public void Close(){
		
		menuIsOpen = false;
		isMoving = true;
		currentDisableMovingTime = 0;
	}
	
	public void OnHitDragArea(){
		
		if( !enabledDrag ) return;
		hitDragArea = true;
	}
	
	public float GetCanvasHeight(){
		
		if( canvas == null ){
			canvas = GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();
		}
		return canvas.rect.height;
	}
}
