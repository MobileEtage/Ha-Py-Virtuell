using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ZoomObjectHandler;

public class ViewObjectHelper : MonoBehaviour
{
    public Camera mainCamera;

    [Space(10)]

    public bool shouldMove = true;
    public bool shouldMoveOnScreen = false;
    public GameObject moveRoot;
    public GameObject moveObject;
    public GameObject moveHitbox;
    private bool selected = false;
    private bool moveEnabled = false;
    private Vector3 hitOffset = Vector3.zero;
    private Vector3 startPosition = Vector3.zero;
    private Vector3 startPositionObj = Vector3.zero;

    [Space(10)]

    public bool shouldRotate = true;
    public GameObject rotateRoot;
    public bool disableHorizontalAxis = false;
    public bool disableVerticalAxis = true;
    private Vector3 oldPosition = Vector3.zero;
    private Vector3 startRotation = Vector3.zero;
    
    [Space(10)]

    public bool shouldZoom = true;
    public GameObject zoomRoot;
    [Range(1, 50)]
    public float zoomFaktor = 10f;
    public float maxScale = 5;
    public float minScale = 0.3f;
    private float startScale = 1f;
    private float oldDist = -1;

    void Start()
    {
        if(mainCamera == null) {

            if (ARController.instance != null) { mainCamera = ARController.instance.mainCamera.GetComponent<Camera>(); }
            else { mainCamera = Camera.main; }
        }

        if (moveRoot != null) { startPosition = moveRoot.transform.localPosition; }
        if (moveObject != null) { startPositionObj = moveObject.transform.localPosition; }
        if (moveHitbox == null) { moveHitbox = moveRoot; }
        if (rotateRoot != null) { startRotation = rotateRoot.transform.localEulerAngles; }
        if (zoomRoot != null) { startScale = zoomRoot.transform.localScale.x; }
    }

    void Update()
    {
        if (moveRoot != null) {

            if (shouldMoveOnScreen)
            {
                MoveARObjectOnScreen(moveRoot, moveObject);
            }
            else if (shouldMove)
            {
                MoveARObject(moveRoot);
            }
        }

        if (shouldRotate && rotateRoot != null) { RotateModelWithOneFinger(rotateRoot); }
        if (shouldZoom && zoomRoot != null) { UpdateZoom(zoomRoot); }
    }

    public void MoveARObject(GameObject obj)
    {
        if (Input.touchCount >= 2) { moveEnabled = false; selected = false; return; }

        if (Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject())
        {
            RaycastHit[] hits;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject == moveHitbox)
                {
                    hitOffset = hits[i].point - obj.transform.position;

                    //moveEnabled = true;
                    selected = true;

                    // Start drag object delayed, because maybe we also want to scale it with two fingers
                    StopCoroutine("EnableMoveARObjectCoroutine");
                    StartCoroutine("EnableMoveARObjectCoroutine");

                    break;
                }
            }  
        }
        else if (moveEnabled && Input.GetMouseButton(0))
        {
            Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
            Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;

            bool hitGround = false;
            if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, out hitPosition))
            {
                hitGround = true;
            }
            else
            {

#if UNITY_EDITOR

                Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
                RaycastHit[] hits;
                hits = Physics.RaycastAll(ray, 100);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.CompareTag("ARPlane"))
                    {
                        hitPosition = hits[i].point;
                        hitGround = true;
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

            //arObject.transform.position = hitPosition - hitOffset;
            obj.transform.position = hitPosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopCoroutine("EnableMoveARObjectCoroutine");
            moveEnabled = false;
            selected = false;
        }
    }

    public void MoveARObjectOnScreen(GameObject root, GameObject obj)
    {
        if (Input.touchCount >= 2) { moveEnabled = false; return; }
        if (PhotoCaptureController.instance.photoPreviewImage.gameObject.activeInHierarchy) { return; }

        root.transform.position = mainCamera.transform.position;
        root.transform.eulerAngles = mainCamera.transform.eulerAngles;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit[] hits;
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 100);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.gameObject == moveHitbox)
                {
                    hitOffset = hits[i].point - obj.transform.position;

                    //moveEnabled = true;
                    selected = true;

                    // Start drag object delayed, because maybe we also want to scale it with two fingers
                    StopCoroutine("EnableMoveARObjectCoroutine");
                    StartCoroutine("EnableMoveARObjectCoroutine");

                    break;
                }
            }
        }
        else if (moveEnabled && Input.GetMouseButton(0))
        {
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 2.0f);
            Vector3 pos = mainCamera.GetComponent<Camera>().ScreenToWorldPoint(mousePosition);
            obj.transform.position = pos - hitOffset;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopCoroutine("EnableMoveARObjectCoroutine");
            moveEnabled = false;
            selected = false;
        }
    }

    public IEnumerator EnableMoveARObjectCoroutine()
    {
        yield return new WaitForSeconds(0.15f);
        if (Input.touchCount >= 2) { moveEnabled = false; selected = false; }
        else { moveEnabled = true; }
    }

    public void MoveObject(GameObject obj)
    {
        if (mainCamera == null) return;

        if (Input.touchCount == 1 || Application.isEditor)
        {
            if (Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject())
            {
                Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
                Ray ray = mainCamera.ScreenPointToRay(touchPosition);
                RaycastHit[] hits;
                hits = Physics.RaycastAll(ray, 100);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.gameObject == obj)
                    {
                        selected = false;
                        moveEnabled = true;
                        break;
                    }
                }
            }
            else if (Input.GetMouseButton(0) && moveEnabled)
            {
                Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
                Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;

                bool hitGround = false;
                if (ARController.instance != null && ARController.instance.RaycastHit(touchPosition, out hitPosition))
                {
                    hitGround = true;

                    /*
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
                        if (hits[i].transform.gameObject == ARController.instance.singlePlane)
                        {

                            obj.transform.position = hits[i].point;
                            //Vector3 rot = obj.transform.eulerAngles;
                            //obj.transform.LookAt(mainCamera.transform);
                            //obj.transform.eulerAngles = new Vector3( rot.x, obj.transform.eulerAngles.y, rot.z );
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

                float dist = Vector3.Distance(hitPosition, mainCamera.transform.position);
                if (dist < 1.0f)
                {
                    Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                    hitPosition = rayTemp.origin + rayTemp.direction * 2.0f;
                    obj.transform.position = hitPosition;
                }
                else
                {
                    //obj.transform.position = hitPosition + hitOffset;
                    obj.transform.position = hitPosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                moveEnabled = false;
            }
        }
        else
        {
            moveEnabled = false;
        }
    }

    private void RotateModelWithTwoFingers(GameObject obj)
    {
        if (moveEnabled) return;

        float angle = 0;
        bool shouldRotate = false;

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.R))
        {
            angle = Time.deltaTime * 10f;
            shouldRotate = true;
        }
#endif

        if (Input.touchCount == 2)
        {
            shouldRotate = true;
            var touch1 = Input.GetTouch(0);
            var touch2 = Input.GetTouch(1);

            Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
            Vector2 prevPos2 = touch2.position - touch2.deltaPosition;
            Vector2 prevDir = prevPos2 - prevPos1;
            Vector2 currDir = touch2.position - touch1.position;

            if (prevPos1.y < prevPos2.y)
            {
                if (prevDir.x < currDir.x)
                {
                    angle = Vector2.Angle(prevDir, currDir);
                }
                else
                {
                    angle = -Vector2.Angle(prevDir, currDir);
                }
            }
            else
            {
                if (prevDir.x < currDir.x)
                {
                    angle = -Vector2.Angle(prevDir, currDir);
                }
                else
                {
                    angle = Vector2.Angle(prevDir, currDir);
                }
            }
        }

        if (shouldRotate)
        {
            Vector3 center = obj.transform.position;
            obj.transform.RotateAround(center, Vector3.up, angle);
        }
    }

    public void RotateModelWithOneFinger(GameObject obj)
    {
        if (moveEnabled || selected) return;
        if (Input.touchCount >= 2) { return; }

        if (Input.GetMouseButton(0) && !ToolsController.instance.IsPointerOverUIObject() )
        {
            if (oldPosition.x < 0) { oldPosition = Input.mousePosition; }
            Vector2 currentPosition = Input.mousePosition;

            Vector2 differnceVectorLastFrame = new Vector2((currentPosition.x / Screen.width), (currentPosition.y / Screen.height)) -
                new Vector2((oldPosition.x / Screen.width), (oldPosition.y / Screen.height));
            oldPosition = currentPosition;
          
            bool useCustomAxis = false;
            Vector3 customXRotationAxis = Vector3.zero;
            Vector3 customYRotationAxis = Vector3.zero;
            float rotateSpeed = 100f;

            if (!disableHorizontalAxis && !disableVerticalAxis)
            {
                if (useCustomAxis)
                {
                    obj.transform.Rotate(customYRotationAxis, -differnceVectorLastFrame.x * rotateSpeed, Space.World);
                    obj.transform.Rotate(customXRotationAxis, differnceVectorLastFrame.y * rotateSpeed, Space.World);
                }
                else
                {
                    if (shouldMoveOnScreen)
                    {
                        obj.transform.Rotate(mainCamera.transform.up, -differnceVectorLastFrame.x * rotateSpeed, Space.World);
                        obj.transform.Rotate(mainCamera.transform.right, differnceVectorLastFrame.y * rotateSpeed, Space.World);                   
                    }
                    else
                    {
                        obj.transform.Rotate(Vector3.up, -differnceVectorLastFrame.x * rotateSpeed, Space.World);
                        obj.transform.Rotate(Vector3.right, differnceVectorLastFrame.y * rotateSpeed, Space.World);
                    }
                }

            }
            else if (disableVerticalAxis)
            {

                if (useCustomAxis)
                {
                    obj.transform.Rotate(customYRotationAxis, -differnceVectorLastFrame.x * rotateSpeed, Space.World);
                }
                else
                {
                    obj.transform.Rotate(Vector3.up, -differnceVectorLastFrame.x * rotateSpeed, Space.Self);
                }

            }
            else if (disableHorizontalAxis)
            {
                if (useCustomAxis)
                {
                    obj.transform.Rotate(customXRotationAxis, differnceVectorLastFrame.y * rotateSpeed, Space.World);
                }
                else
                {
                    obj.transform.Rotate(Vector3.right, differnceVectorLastFrame.y * rotateSpeed, Space.Self);
                }
            }

        }
        else if (Input.GetMouseButtonUp(0))
        {
            oldPosition = new Vector2(-1, -1);
        }
    }

    public void UpdateZoom(GameObject obj)
    {
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                // Umrechnung für unabhängige Screengrößen und Auflösungen
                Vector2 t1 = new Vector2((touch1.position.x / Screen.width), (touch1.position.y / Screen.height));
                Vector2 t2 = new Vector2((touch2.position.x / Screen.width), (touch2.position.y / Screen.height));

                if (oldDist < 0){ oldDist = Vector2.Distance(t1, t2); }
                float curDist = Vector2.Distance(t1, t2);
                float scaleFaktor = Mathf.Abs(oldDist - curDist) * zoomFaktor;

                if (curDist > oldDist)
                {
                    obj.transform.localScale += new Vector3(scaleFaktor, scaleFaktor, scaleFaktor);
                    if (obj.transform.localScale.x > maxScale) { obj.transform.localScale = new Vector3(maxScale, maxScale, maxScale); }
                }
                else if (curDist < oldDist)
                {
                    obj.transform.localScale += new Vector3(-scaleFaktor, -scaleFaktor, -scaleFaktor);
                    if (obj.transform.localScale.x < minScale){ obj.transform.localScale = new Vector3(minScale, minScale, minScale); }
                }
                oldDist = curDist;
            }
        }
        else
        {
            oldDist = -1;
        }
    }

    public void Reset()
    {
        selected = false;
        moveEnabled = false;

        if (moveRoot != null) { moveRoot.transform.localPosition = startPosition; }
        if (moveObject != null) { moveObject.transform.localPosition = startPositionObj; }
        if (rotateRoot != null) { rotateRoot.transform.localEulerAngles = startRotation; }
        if (zoomRoot != null) { zoomRoot.transform.localScale = new Vector3(startScale, startScale, startScale); }
    }
}
