using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectViewer : MonoBehaviour
{
    public enum MoveType { None, Move2D, Move3D };
    public enum RotateType { None, RotateAroundPivot, RotateAroundBottom, RotateAroundCenter };
    public enum RotateAxis { Camera, Local, Global };
    public enum ScaleType { None, ScaleAroundPivot, ScaleAroundBottom, ScaleAroundCenter };

    public Transform mainCamera;

    [Space(20)]

    public MoveType moveType;
    public float move2DDistance = 2.0f;
    public bool useFixedMove2DDistance = false;

    [Space(20)]

    public RotateType rotateType;
    public RotateAxis rotateAxis;
    public float rotateFactor = 200.0f;
    public bool rotateHorizontal = true;
    public bool rotateVertical = true;
    public bool rotateClockwiseWithTwoFingers = false;
    public bool rotateOnlyOnHit = false;
    [HideInInspector] public bool rotationActive = false;
    public List<ObjectViewer> otherRotatingObjectViewer = new List<ObjectViewer>();

    [Space(20)]

    public ScaleType scaleType;
    public float scaleTouchFactor = 1.0f;
    public float scaleScrollFactor = 0.2f;
    public float minScale = 1f;
    public float maxScale = 5f;

    // Move params
    private Vector3 moveHitOffset = Vector3.zero;
    private bool moveEnabled = false;
    private bool selected = false;

    // Rotate params
    private Vector3 rotateCenter = Vector3.zero;
    private Vector3 oldRotatePosition = Vector3.zero;
    private GameObject rotateCenterObj;
    private Vector2 rotateClockwiseVector = new Vector2(-9999, -9999);

    // Scale params
    private Vector3 scaleCenter = Vector3.zero;
    private float oldScaleDist = -1;
    private GameObject scaleCenterObj;

    private bool initialized = false;
    private Vector3 startPosition = new Vector3(-9999, -9999, -9999);
    private Vector3 startRotation = new Vector3(-9999, -9999, -9999);
    private Vector3 startScale = new Vector3(-9999, -9999, -9999);

    void Start()
    {
        Init();
    }

    void Update()
    {
        if (moveType != MoveType.None && mainCamera != null) { Move(); }
        if (rotateType != RotateType.None) { Rotate(); }
        if (scaleType != ScaleType.None) { Scale(); }
    }

    public void Init()
    {
        if (initialized) return;
        initialized = true;

        startPosition = this.transform.localPosition;
        startRotation = this.transform.localEulerAngles;
        startScale = this.transform.localScale;
        if (mainCamera != null && !useFixedMove2DDistance) { move2DDistance = Vector3.Distance(mainCamera.position, this.transform.position); }

        UpdateTransformCenters();
        GenerateBoxCollider(this.gameObject);
    }

    public void CreateLocalAxis(Transform target)
    {
        float length = 0.5f;
        float thickness = 0.02f;

        GameObject axisX = GameObject.CreatePrimitive(PrimitiveType.Cube);
        axisX.transform.SetParent(target);
        axisX.transform.localScale = new Vector3(thickness, length, thickness);
        axisX.transform.localPosition = new Vector3(length * 0.5f, 0, 0);
        axisX.GetComponent<Renderer>().material.color = Color.red;
        axisX.transform.up = target.right;

        GameObject axisY = GameObject.CreatePrimitive(PrimitiveType.Cube);
        axisY.transform.SetParent(target);
        axisY.transform.localScale = new Vector3(thickness, length, thickness);
        axisY.transform.localPosition = new Vector3(0, length * 0.5f, 0);
        axisY.GetComponent<Renderer>().material.color = Color.green;
        axisY.transform.up = target.up;

        GameObject axisZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
        axisZ.transform.SetParent(target);
        axisZ.transform.localScale = new Vector3(thickness, length, thickness);
        axisZ.transform.localPosition = new Vector3(0, 0, length * 0.5f);
        axisZ.GetComponent<Renderer>().material.color = Color.blue;
        axisZ.transform.up = target.forward;
    }

    // Call this after you replace the object
    public void UpdateTransformCenters()
    {
        Vector3 maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        Vector3 minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        SetBoundingValues(this.gameObject, ref minPos, ref maxPos);
        float distX = Mathf.Abs(maxPos.x - minPos.x);
        float distY = Mathf.Abs(maxPos.y - minPos.y);
        float distZ = Mathf.Abs(maxPos.z - minPos.z);

        rotateCenter = this.transform.localPosition;
        if (rotateType == RotateType.RotateAroundBottom) { rotateCenter = new Vector3(minPos.x + distX * 0.5f, minPos.y, minPos.z + distZ * 0.5f); }
        else if (rotateType == RotateType.RotateAroundCenter) { rotateCenter = new Vector3(minPos.x + distX * 0.5f, minPos.y + distY * 0.5f, minPos.z + distZ * 0.5f); }

        scaleCenter = this.transform.localPosition;
        if (scaleType == ScaleType.ScaleAroundBottom) { scaleCenter = new Vector3(minPos.x + distX * 0.5f, minPos.y, minPos.z + distZ * 0.5f); }
        else if (scaleType == ScaleType.ScaleAroundCenter) { scaleCenter = new Vector3(minPos.x + distX * 0.5f, minPos.y + distY * 0.5f, minPos.z + distZ * 0.5f); }

        // Helper
        /*
        if (rotateCenterObj == null) { rotateCenterObj = GameObject.CreatePrimitive(PrimitiveType.Sphere); }
        rotateCenterObj.transform.position = rotateCenter;
        rotateCenterObj.transform.localScale = Vector3.one * 0.05f;

        if (scaleCenterObj == null) { scaleCenterObj = GameObject.CreatePrimitive(PrimitiveType.Sphere); }
        scaleCenterObj.transform.position = scaleCenter;
        scaleCenterObj.transform.localScale = Vector3.one * 0.05f;
        */
    }

    public void Move()
    {
        if (moveType == MoveType.Move2D) { Move2D(this.transform); }
        if (moveType == MoveType.Move3D) { Move3D(this.transform); }
    }

    public void Move2D(Transform target)
    {
        if (Input.touchCount >= 2) { moveEnabled = false; return; }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit[] hits;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject == target.gameObject)
                {
                    moveHitOffset = hits[i].point - target.position;

                    //moveEnabled = true;
                    selected = true;

                    // Start drag object delayed, because maybe we also want to scale it with two fingers
                    StopCoroutine("EnableMoveCoroutine");
                    StartCoroutine("EnableMoveCoroutine");

                    break;
                }
            }
        }
        else if (moveEnabled && Input.GetMouseButton(0))
        {
            //Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 2.0f);
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, move2DDistance);
            Vector3 pos = mainCamera.GetComponent<Camera>().ScreenToWorldPoint(mousePosition);
            target.position = pos - moveHitOffset;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (moveEnabled) { UpdateTransformCenters(); }

            StopCoroutine("EnableMoveCoroutine");
            moveEnabled = false;
            selected = false;
        }
    }

    public void TriggerMove()
    {
        RaycastHit[] hits;
        Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        hits = Physics.RaycastAll(ray, 100);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.gameObject == this.gameObject)
            {
                moveHitOffset = hits[i].point - this.transform.position;

                //moveEnabled = true;
                selected = true;

                // Start drag object delayed, because maybe we also want to scale it with two fingers
                StopCoroutine("EnableMoveCoroutine");
                StartCoroutine("EnableMoveCoroutine");

                break;
            }
        }
    }

    public IEnumerator EnableMoveCoroutine()
    {
        yield return new WaitForSeconds(0.15f);
        if (Input.touchCount >= 2) { moveEnabled = false; selected = false; }
        else { moveEnabled = true; }
    }

    public void Move3D(Transform target)
    {
        if (Input.touchCount == 1 || Application.isEditor)
        {
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject())
            {
                Vector2 touchPosition = GetTouchPosition();
                Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
                RaycastHit[] hits;
                hits = Physics.RaycastAll(ray, 100);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.gameObject == target.gameObject)
                    {
                        selected = false;
                        moveEnabled = true;
                        break;
                    }
                }
            }
            else if (Input.GetMouseButton(0) && moveEnabled)
            {
                Vector2 touchPosition = GetTouchPosition();
                Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;

                bool hitGround = false;
                if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, out hitPosition))
                {
                    hitGround = true;

                    /*
                     * 
                    if (!selected)
                    {
                        selected = true;
                        hitOffset = obj.transform.position - hitPosition;
                    }
                    obj.transform.position = hitPosition + hitOffset;

                    */
                }
                else
                {

#if UNITY_EDITOR

                    Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
                    RaycastHit[] hits;
                    hits = Physics.RaycastAll(ray, 100);

                    for (int i = 0; i < hits.Length; i++)
                    {
                        if (ARController.instance != null && hits[i].transform.gameObject == ARController.instance.singlePlane)
                        {
                            hitGround = true;
                            target.position = hits[i].point;
                            break;
                        }
                    }
#endif
                    if (!hitGround)
                    {
                        Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                        hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;
                    }
                }

                // Optional avoid very near distance
                float minDist = 0;
                float dist = Vector3.Distance(hitPosition, mainCamera.transform.position);
                if (dist < minDist)
                {
                    Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                    hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;
                    target.position = hitPosition;
                }
                else
                {
                    //obj.transform.position = hitPosition + hitOffset;
                    target.position = hitPosition;
                }
                
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (moveEnabled) { UpdateTransformCenters(); }

                moveEnabled = false;
            }
        }
        else
        {
            moveEnabled = false;
        }
    }

    public void Rotate()
    {
        if (moveEnabled || selected) return;
        if (Input.touchCount >= 2 && scaleType != ScaleType.None) { return; }

        // Do not rotate if other object is rotated
        for (int i = 0; i < otherRotatingObjectViewer.Count; i++)
        {
            if (otherRotatingObjectViewer[i].rotateOnlyOnHit && otherRotatingObjectViewer[i].rotationActive) return;
        }

        if (rotateOnlyOnHit && mainCamera != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit[] hits;
                Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                hits = Physics.RaycastAll(ray, 100);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.gameObject == this.gameObject)
                    {
                        rotationActive = true;
                        break;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                rotationActive = false;
            }
        }
        else
        {
            rotationActive = true;
        }

        if (!rotationActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            oldRotatePosition = new Vector2(-1, -1);
        }
        else if (Input.GetMouseButton(0) && !IsPointerOverUIObject())
        {
            if (rotateClockwiseWithTwoFingers && Input.touchCount == 2)
            {
                float angle = 0;
                Vector2 touch1 = Input.GetTouch(0).position;
                Vector2 touch2 = Input.GetTouch(1).position;
                Vector2 line = touch1 - touch2;
                if(rotateClockwiseVector.x <= -9999) { angle = 0; }
                else
                {
                    angle = Vector3.SignedAngle(line, rotateClockwiseVector, mainCamera.transform.forward);
                    this.transform.Rotate(mainCamera.transform.forward, -angle, Space.World);
                }

                rotateClockwiseVector = line;

                return;
            }

            if (oldRotatePosition.x <= 0) { oldRotatePosition = Input.mousePosition; }
            Vector2 currentPosition = Input.mousePosition;
            Vector2 differnceVectorLastFrame = 
                new Vector2((currentPosition.x / Screen.width), (currentPosition.y / Screen.height)) - new Vector2((oldRotatePosition.x / Screen.width), (oldRotatePosition.y / Screen.height));
            oldRotatePosition = currentPosition;
            differnceVectorLastFrame *= rotateFactor;

            if (mainCamera != null && rotateAxis == RotateAxis.Camera)
            {
                if (rotateHorizontal) { this.transform.RotateAround(rotateCenter, mainCamera.up, -differnceVectorLastFrame.x); }
                if (rotateVertical) { this.transform.RotateAround(rotateCenter, mainCamera.right, differnceVectorLastFrame.y); }
            }
            else if(rotateAxis == RotateAxis.Local)
            {
                if (rotateHorizontal) { this.transform.RotateAround(rotateCenter, this.transform.up, -differnceVectorLastFrame.x); }
                if (rotateVertical) { this.transform.RotateAround(rotateCenter, this.transform.right, differnceVectorLastFrame.y); }
            }
            else if (rotateAxis == RotateAxis.Global)
            {
                if (rotateHorizontal) { this.transform.RotateAround(rotateCenter, Vector3.up, -differnceVectorLastFrame.x); }
                if (rotateVertical) { this.transform.RotateAround(rotateCenter, Vector3.right, differnceVectorLastFrame.y); }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            oldRotatePosition = new Vector2(-1, -1);
            rotateClockwiseVector = new Vector2(-9999, -9999);
        }
    }

    public void Scale()
    {
        Vector3 newScale = GetScale(this.transform);
        if (newScale.x != this.transform.localScale.x) { ScaleAround(this.transform, scaleCenter, newScale); }
    }

    public Vector3 GetScale(Transform target)
    {
        Vector3 targetScale = target.localScale;

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                // Umrechnung für unabhängige Screengrößen und Auflösungen
                Vector2 t1 = new Vector2((touch1.position.x / Screen.width), (touch1.position.y / Screen.height));
                Vector2 t2 = new Vector2((touch2.position.x / Screen.width), (touch2.position.y / Screen.height));

                if (oldScaleDist < 0) { oldScaleDist = Vector2.Distance(t1, t2); }
                float curDist = Vector2.Distance(t1, t2);
                float scaleDelta = Mathf.Abs(oldScaleDist - curDist) * scaleTouchFactor;

                if (curDist > oldScaleDist)
                {
                    targetScale = target.transform.localScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);
                    if (targetScale.x > maxScale) { targetScale = new Vector3(maxScale, maxScale, maxScale); }
                }
                else if (curDist < oldScaleDist)
                {
                    targetScale = target.transform.localScale + new Vector3(-scaleDelta, -scaleDelta, -scaleDelta);
                    if (targetScale.x < minScale) { targetScale = new Vector3(minScale, minScale, minScale); }
                }
                oldScaleDist = curDist;
            }
        }
        else
        {
            oldScaleDist = -1;

            if (Input.mouseScrollDelta.magnitude > 0)
            {
                //int dir = Input.GetAxisRaw("Mouse ScrollWheel") > 0 ? 1 : -1;
                float dir = Input.mouseScrollDelta.y;
                float scaleDelta = dir * scaleScrollFactor;
                targetScale = target.transform.localScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);
                if (targetScale.x > maxScale) { targetScale = new Vector3(maxScale, maxScale, maxScale); }
                if (targetScale.x < minScale) { targetScale = new Vector3(minScale, minScale, minScale); }
            }
        }

        return targetScale;
    }

    public void ScaleAround(Transform target, Vector3 pivot, Vector3 newScale)
    {
        Vector3 A = target.localPosition;
        Vector3 B = pivot;

        Vector3 C = A - B; // diff from object pivot to desired pivot/origin

        float RS = newScale.x / target.localScale.x; // relative scale factor

        // calc final position post-scale
        Vector3 FP = B + C * RS;

        // finally, actually perform the scale/translation
        target.localScale = newScale;
        target.localPosition = FP;
    }

    public void SetBoundingValues(GameObject obj, ref Vector3 minPos, ref Vector3 maxPos)
    {
        Renderer[] rend = obj.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rend.Length; i++)
        {
            if (maxPos.x < rend[i].bounds.max.x){ maxPos.x = rend[i].bounds.max.x; }
            if (maxPos.y < rend[i].bounds.max.y){ maxPos.y = rend[i].bounds.max.y;}
            if (maxPos.z < rend[i].bounds.max.z){ maxPos.z = rend[i].bounds.max.z; }
            if (minPos.x > rend[i].bounds.min.x){ minPos.x = rend[i].bounds.min.x; }
            if (minPos.y > rend[i].bounds.min.y){ minPos.y = rend[i].bounds.min.y; }
            if (minPos.z > rend[i].bounds.min.z){ minPos.z = rend[i].bounds.min.z; }
        }
    }

    public void GenerateBoxCollider(GameObject obj)
    {
        if (obj.GetComponent<BoxCollider>() == null)
        {
            Vector3 startPosition = obj.transform.position;
            Vector3 startRotation = obj.transform.eulerAngles;
            obj.transform.position = Vector3.zero;
            obj.transform.eulerAngles = Vector3.zero;

            //Find bounds
            Vector3 maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            SetBoundingValues(obj, ref minPos, ref maxPos);

            float distX = Mathf.Abs(maxPos.x - minPos.x);
            float distY = Mathf.Abs(maxPos.y - minPos.y);
            float distZ = Mathf.Abs(maxPos.z - minPos.z);

            GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.transform.SetParent(obj.transform);
            o.transform.position = new Vector3(minPos.x + distX * 0.5f, minPos.y + distY * 0.5f, minPos.z + distZ * 0.5f);
            o.transform.localEulerAngles = Vector3.zero;

            o.transform.SetParent(null);
            o.transform.localScale = new Vector3(distX * 1, distY, distZ * 1);
            o.transform.SetParent(obj.transform);

            o.GetComponent<Renderer>().enabled = false;
            //o.tag = "Model";
            //o.layer = LayerMask.NameToLayer("Model");

            obj.transform.position = startPosition;
            obj.transform.eulerAngles = startRotation;
            obj.AddComponent<BoxCollider>();
            obj.GetComponent<BoxCollider>().center = o.transform.localPosition;
            obj.GetComponent<BoxCollider>().size = o.transform.localScale;

            Destroy(o);
        }
    }

    public bool IsPointerOverUIObject(bool limitToUILayer = false)
    {
        try
        {
            if (EventSystem.current == null) return false;
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            bool hitUI = results.Count > 0;
            if (hitUI)
            {
                if (limitToUILayer)
                {
                    // Also check if one of the objects is in the UI-layer
                    for (int i = 0; i < results.Count; i++) { if (LayerMask.LayerToName(results[i].gameObject.layer) == "UI") { return true; } }
                    return false;
                }
                else
                {
                    return true;
                }
            }

#if UNITY_EDITOR
            if (EventSystem.current.IsPointerOverGameObject()) { return true; }
#else
			if (Input.touchCount > 0)
			{
			    var touch = Input.GetTouch(0);
			    if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return true;
			}
#endif

        }
        catch (Exception e)
        {
            print("IsPointerOverUIObject error " + e.Message);
        }

        return false;
    }

    public Vector2 GetTouchPosition()
    {

#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            var mousePosition = Input.mousePosition;
            return new Vector2(mousePosition.x, mousePosition.y);
        }
#else
		if (Input.touchCount > 0)
		{
		    return Input.GetTouch(0).position;
		}else{

		    if (Input.GetMouseButton(0))
		    {
		        var mousePosition = Input.mousePosition;
		        return new Vector2(mousePosition.x, mousePosition.y);
		    }
		}
#endif

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    public void Reset()
    {
        if (startPosition.x > -9999) { this.transform.localPosition = startPosition; }
        if (startRotation.x > -9999) { this.transform.localEulerAngles = startRotation; }
        if (startScale.x > -9999) { this.transform.localScale = startScale; }
    }
}
