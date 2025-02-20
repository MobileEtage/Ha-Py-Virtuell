using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragZoom : MonoBehaviour
{
    public bool isActive = true;
    public Canvas canvas;

    public Transform zoomTarget;
    public GameObject mainImageZoomRoot;
    public GameObject mainImageDefaultRoot;

    public float zoomFaktor = 10f;
    public float maxScale = 10;
    public float minScale = 0.1f;
    private float oldDist = -1;
    private Vector3 mainImageZoomRootPos;

    [Space(10)]

    public bool canDrag = true;
    public bool isDragging = false;
    public Vector3 offset = Vector3.zero;
    private Vector3 targetPosition = Vector3.zero;
    private int childIndex = 0;

    void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        mainImageZoomRootPos = mainImageZoomRoot.transform.position;
        childIndex = zoomTarget.GetSiblingIndex();
    }

    void Update()
    {
        if (!isActive) return;

        Zoom(zoomTarget);

        if (isDragging)
        {
            //offset = Vector3.Lerp(offset, Vector3.zero, Time.deltaTime * 5);
            transform.position = targetPosition + offset;
        }

        if (Input.touchCount >= 2)
        {
            canDrag = false;
        }
        else if (Input.touchCount == 0)
        {
            canDrag = true;
        }
    }

    public void Drag(BaseEventData data)
    {
        if (!isActive) return;
        if (!canDrag) return;
        if (Input.touchCount >= 2) return;
        if (canvas == null) canvas = GetComponentInParent<Canvas>().rootCanvas;
        if (canvas == null) return;

        PointerEventData pointerData = (PointerEventData)data;

        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            pointerData.position,
            //canvas.worldCamera,
            null,
            out position);

        targetPosition = canvas.transform.TransformPoint(position);
        if (!isDragging) { offset = transform.position - targetPosition; offset.z = 0; }
        transform.position = targetPosition + offset;

        isDragging = true;
    }

    public void OnEndDrag()
    {
        if (!isActive) return;
        isDragging = false;
    }

    public void Zoom(Transform myTransform)
    {

#if UNITY_EDITOR
        Scale(myTransform, Input.mouseScrollDelta.y * 2.0f * Time.deltaTime);
#else

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                Vector2 t1 = new Vector2((touch1.position.x / Screen.width), (touch1.position.y / Screen.height));
                Vector2 t2 = new Vector2((touch2.position.x / Screen.width), (touch2.position.y / Screen.height));

                if (myTransform.parent != mainImageZoomRoot.transform)
                {
                    if (GetComponent<AspectRatioFitter>()){ GetComponent<AspectRatioFitter>().enabled = false; }

                    Vector2 center = (touch1.position+touch2.position)*0.5f;
                    mainImageZoomRoot.transform.position = new Vector3(center.x, center.y, 0);

                    myTransform.SetParent(mainImageZoomRoot.transform);
                }         
                

                if (oldDist < 0)
                {
                    oldDist = Vector2.Distance(t1, t2);
                }
                float curDist = Vector2.Distance(t1, t2);
                float scaleFactor = Mathf.Abs(oldDist - curDist) * zoomFaktor;

                int sign = (curDist > oldDist) ? 1 : -1;
                scaleFactor = sign * scaleFactor;

                //Scale(myTransform, scaleFactor);
                Scale(mainImageZoomRoot.transform, scaleFactor);

                oldDist = curDist;
            }
        }
        else
        {
            if (myTransform.transform.parent != mainImageDefaultRoot.transform)
            {
                Vector3 pos = myTransform.position;
                if (GetComponent<AspectRatioFitter>()){ GetComponent<AspectRatioFitter>().enabled = true; }
                myTransform.SetParent(mainImageDefaultRoot.transform);
                myTransform.SetSiblingIndex(childIndex);
                myTransform.position = pos;
            }

            oldDist = -1;
        }
#endif

    }

    public void Scale(Transform myTransform, float scaleFactor)
    {
        myTransform.localScale += Vector3.one * scaleFactor;
        if (myTransform.localScale.x > maxScale)
        {
            myTransform.localScale = new Vector3(maxScale, maxScale, maxScale);
        }
        else if (myTransform.localScale.x < minScale)
        {
            myTransform.localScale = new Vector3(minScale, minScale, minScale);
        }
    }

    public void OnEnable()
    {
        if (!isActive) return;
        Reset();
    }

    public void Reset()
    {
        if (GetComponent<AspectRatioFitter>()) { GetComponent<AspectRatioFitter>().enabled = true; }
        zoomTarget.transform.localPosition = Vector3.zero;
        zoomTarget.transform.localScale = Vector3.one;
        mainImageDefaultRoot.transform.localScale = Vector3.one;
        mainImageZoomRoot.transform.localScale = Vector3.one;
    }
}
