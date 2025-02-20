using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MeasureController : MonoBehaviour {

	public GameObject measureOptions;
	public Image stepBackButton;

    [Space(10)]

    public float lineScaleFaktor = 1f;
    public bool measureEnabled = false;

    [Space(10)]

    public LineChain lineChain;
    public LineRenderer previewLine;
    public GameObject pointPrefab;
    public GameObject linePrefab;
    public GameObject measureInfoPrefab;

    [Space(10)]

    public GameObject groundIndicator;
    public GameObject groundIndicatorCircle;
	public Transform myCamera;
    
	private Quaternion groundIndicatorRotation;
	private float scaleLinesFaktor = 1f;
	private float scalePointsFaktor = 1f;
	private float scaleInfoboxFaktor = 1f;
	
	private bool useARKitHitTest = true;
    private bool previewLineEnabled = false;
    private Vector3 lineStartPosition = Vector3.zero;
    private Vector3 vel = Vector3.zero;
    private float smoothTime = 0.5f;
    private int currentPlacingStep = 1;
    private GameObject currentMeasureInfo;
    private Line currentLine;

	public List<Line> undoLines = new List<Line>();

    public static MeasureController Instance;
    private void Awake()
    {
        Instance = this;
    }

    void Start () {
        previewLine.positionCount = 2;
    }

    void Update () {

	    if( !groundIndicator.activeInHierarchy ) return;
	    
        // Get the ground plane hit point
	    Vector3 point = Vector3.zero;
	    groundIndicatorRotation = Quaternion.identity;
	    bool hitGround = RayCastGround(ref point);

        // Show ground indicator
        ShowGroundIndicator(hitGround, point);

        // Animate move groundCircle to existing pointObject if near enough 
        UpdateGroundCircle(point);

        // Update size of line objects depending on the camera distance
        UpdateLineChainSize();

        // show preview line
        if (previewLineEnabled)
        {
            if (hitGround){
                previewLine.SetPosition(1, point);
            }

            // rescale previewLine depending on the camera distance 
            float w = Vector3.Distance(Camera.main.transform.position, Vector3.Lerp(previewLine.GetPosition(0), previewLine.GetPosition(1), 0.5f)) * 0.01f;
            w = Mathf.Clamp(w, 0.001f, 0.1f);
            previewLine.widthMultiplier = w;
            

            // show dotted line
            previewLine.materials[0].mainTextureScale = new Vector3(Vector3.Distance(previewLine.GetPosition(0), previewLine.GetPosition(1))*10, 1);

            // show distance info
            UpdateDistanceInfo(point);
        }

    }

	public void EnableDisableMeasurement( bool doEnable ){
		
		measureEnabled = doEnable;
		groundIndicator.SetActive(doEnable);
	}
	
    void ShowGroundIndicator(bool hitGround, Vector3 point)
    {
        if (measureEnabled)
        {
            if (hitGround)
            {
	            groundIndicator.transform.position = point;
	            groundIndicator.transform.rotation = groundIndicatorRotation;
            }
        }
    }

    void UpdateGroundCircle(Vector3 point)
    {
        Vector3 pointObjectPosition = Vector3.zero;
        if (lineChain.ExistingPointHit(point, ref pointObjectPosition))
        {
            groundIndicatorCircle.transform.position = Vector3.SmoothDamp(groundIndicatorCircle.transform.position, pointObjectPosition, ref vel, smoothTime);
            if (Vector3.Distance(groundIndicatorCircle.transform.position, pointObjectPosition) < 0.01f)
            {
                groundIndicatorCircle.SetActive(false);
            }
        }
        else
        {
            groundIndicatorCircle.SetActive(true);
            groundIndicatorCircle.transform.localPosition = Vector3.SmoothDamp(groundIndicatorCircle.transform.localPosition, Vector3.zero, ref vel, smoothTime);
        }
    }

    void ShowDistanceInfo(Vector3 point)
    {
        if (currentMeasureInfo != null)
        {
            float dist = Vector3.Distance(lineStartPosition, point);
            currentMeasureInfo.SetActive(true);
            currentMeasureInfo.transform.position = lineStartPosition + (point - lineStartPosition) * 0.5f;

            if (dist < 1.0f)
            {
                currentMeasureInfo.GetComponentInChildren<TextMeshPro>().text = "" + (dist * 100).ToString("F0") + " cm";
            }
            else
            {
                currentMeasureInfo.GetComponentInChildren<TextMeshPro>().text = "" + dist.ToString("F2") + " m";
            }
        }
    }

    void UpdateLineChainSize()
    {
        //float w = Vector3.Distance(Camera.main.transform.position, lineChain.nearestPointObject()) * 0.01f;
        //w = Mathf.Clamp(w, 0.001f, 0.1f);

        float w = 0;
        for (int i = 0; i < lineChain.lines.Count; i++)
        {
            if (lineChain.lines[i].line != null)
            {
                w = Vector3.Distance(Camera.main.transform.position, Vector3.Lerp(lineChain.lines[i].line.GetComponent<LineRenderer>().GetPosition(0), lineChain.lines[i].line.GetComponent<LineRenderer>().GetPosition(1), 0.5f)) * 0.01f * scaleLinesFaktor;
                w = Mathf.Clamp(w, 0.001f, 0.1f);
                lineChain.lines[i].line.GetComponent<LineRenderer>().widthMultiplier = w;
            }
        }

        for (int i = 0; i < lineChain.lines.Count; i++)
        {
            if (lineChain.lines[i].startPoint != null)
            {
                w = Vector3.Distance(Camera.main.transform.position, lineChain.lines[i].startPoint.transform.position) * 0.01f * scalePointsFaktor;
                w = Mathf.Clamp(w, 0.001f, 0.1f);
                lineChain.lines[i].startPoint.transform.localScale = Vector3.one * 0.03f * 100 * w * lineScaleFaktor;
            }

            if (lineChain.lines[i].endPoint != null)
            {
                w = Vector3.Distance(Camera.main.transform.position, lineChain.lines[i].endPoint.transform.position) * 0.01f * scalePointsFaktor;
                w = Mathf.Clamp(w, 0.001f, 0.1f);
                lineChain.lines[i].endPoint.transform.localScale = Vector3.one * 0.03f * 100 * w * lineScaleFaktor;
            }
        }

        for (int i = 0; i < lineChain.lines.Count; i++)
        {
            if (lineChain.lines[i].measureInfo != null)
            {
                w = Vector3.Distance(Camera.main.transform.position, lineChain.lines[i].measureInfo.transform.position) * 0.01f * scaleInfoboxFaktor;
                w = Mathf.Clamp(w, 0.001f, 0.1f);
                lineChain.lines[i].measureInfo.transform.localScale = Vector3.one * 0.15f * 100 * w * lineScaleFaktor;
            }
        }

        //if(currentMeasureInfo != null)
        //{
        //    w = Vector3.Distance(Camera.main.transform.position, currentMeasureInfo.transform.position) * 0.01f * scaleInfoboxFaktor;
        //    w = Mathf.Clamp(w, 0.001f, 0.1f);
        //    currentMeasureInfo.transform.localScale = Vector3.one * 0.15f * 100 * w * lineScaleFaktor;
        //}
    }

    void UpdateDistanceInfo(Vector3 point)
    {
        // show distance info
        if (currentMeasureInfo != null)
        {
            float dist = Vector3.Distance(lineStartPosition, point);
            if (dist > 0.05f)
            {
                currentMeasureInfo.SetActive(true);
                currentMeasureInfo.transform.position = lineStartPosition + (point - lineStartPosition) * 0.5f;

                float w = Vector3.Distance(Camera.main.transform.position, currentMeasureInfo.transform.position) * 0.01f * scaleInfoboxFaktor;
                w = Mathf.Clamp(w, 0.001f, 0.1f);
                currentMeasureInfo.transform.localScale = Vector3.one * 0.15f * 100 * w * lineScaleFaktor;
             
                /*
                currentMeasureInfo.transform.LookAt(point);
                currentMeasureInfo.transform.Rotate(Vector3.up, -90);
                Vector3 r = currentMeasureInfo.transform.localEulerAngles;
                currentMeasureInfo.transform.LookAt(Camera.main.transform.position);
                currentMeasureInfo.transform.localEulerAngles = new Vector3(-currentMeasureInfo.transform.localEulerAngles.x, r.y, 0);
                */

                if (dist < 1.0f)
                {
                    currentMeasureInfo.GetComponentInChildren<TextMeshPro>().text = "" + (dist * 100).ToString("F0") + " cm";
                }
                else
                {
                    currentMeasureInfo.GetComponentInChildren<TextMeshPro>().text = "" + dist.ToString("F2") + " m";
                }
            }
            else
            {
                currentMeasureInfo.SetActive(false);
            }
        }
    }

    public bool RayCastGround(ref Vector3 point)
    {
        if (useARKitHitTest)
        {
	        Vector2 screenPosition = new Vector2( Screen.width * 0.5f, Screen.height * 0.5f );
	        if( ARController.instance.TryGetHitPositionWithScreenPosition( screenPosition, out point ) ){
		        return true;
	        }
	        else{
	        	return false;
	        }
        }
        else
        {
            RaycastHit[] hits;
            hits = Physics.RaycastAll(myCamera.position, myCamera.forward, 100.0F);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                if (hit.transform.CompareTag("Ground"))
                {
                    point = hit.point;
                    return true;
                }
            }

            return false;
        }
    }

    public void AddPointToLine()
    {
        if (measureEnabled)
        {
            Vector3 point = Vector3.zero;
            bool hitGround = RayCastGround(ref point);

            if (hitGround)
            {
                Vector3 pointObjectPosition = Vector3.zero;
                bool hitPoint = lineChain.ExistingPointHit(point, ref pointObjectPosition);

                if (currentPlacingStep == 1)
                {
                    currentLine = new Line();

                    if (hitPoint)
                    {
                        previewLine.SetPosition(0, pointObjectPosition);
                        lineStartPosition = pointObjectPosition;
                    }
                    else
                    {
                        previewLine.SetPosition(0, point);
                        lineStartPosition = point;

                        GameObject linePoint = GameObject.Instantiate(pointPrefab);
                        linePoint.SetActive(true);
                        linePoint.transform.position = point;
                        linePoint.transform.SetParent(lineChain.transform);
                        //lineChain.pointObjects.Add(linePoint);

                        currentLine.startPoint = linePoint;
                    }

                    previewLine.gameObject.SetActive(true);
                    previewLineEnabled = true;

                    GameObject measureInfo = GameObject.Instantiate(measureInfoPrefab);
                    measureInfo.SetActive(false);
                    float w = Vector3.Distance(Camera.main.transform.position, measureInfo.transform.position) * 0.01f;
                    w = Mathf.Clamp(w, 0.001f, 0.1f);
                    measureInfo.transform.localScale = Vector3.one * 0.15f * 100 * w * lineScaleFaktor;
                    measureInfo.GetComponent<Renderer>().material.color = new Color(255f / 255f, 173f / 255f, 0f / 255f);
                    measureInfo.GetComponentInChildren<TextMeshPro>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f);
                    currentMeasureInfo = measureInfo;
                    measureInfo.transform.SetParent(lineChain.transform);

                    //lineChain.measureInfoboxes.Add(measureInfo);

                    currentLine.measureInfo = measureInfo;
                    lineChain.lines.Add(currentLine);
                    stepBackButton.color = new Color(1f, 1f, 1f, 0.93f);
                    UndoReset();

                    currentPlacingStep = 2;

                }
                else if (currentPlacingStep == 2 && Vector3.Distance(lineStartPosition, point) > 0.02f)
                {
                    // Create line
                    //if (!hitPoint)    // We could avoid near points
                    {
                        GameObject linePoint = GameObject.Instantiate(pointPrefab);
                        linePoint.SetActive(true);
                        linePoint.transform.position = point;
                        linePoint.transform.SetParent(lineChain.transform);
                        //lineChain.pointObjects.Add(linePoint);

                        GameObject line = GameObject.Instantiate(linePrefab);
                        line.SetActive(true);
                        line.GetComponent<LineRenderer>().SetPosition(0, lineStartPosition);
                        line.GetComponent<LineRenderer>().SetPosition(1, point);
                        line.transform.SetParent(lineChain.transform);
                        //lineChain.lines.Add(line);

                        currentLine.endPoint = linePoint;
                        currentLine.line = line;
                    }

                    previewLine.gameObject.SetActive(false);
                    previewLineEnabled = false;

                    if(currentMeasureInfo != null) {
                        currentMeasureInfo.GetComponent<Renderer>().material.color = new Color(255f / 255f, 255f / 255f, 255f / 255f);
                        currentMeasureInfo.GetComponentInChildren<TextMeshPro>().color = new Color(0f / 255f, 0f / 255f, 0f / 255f);
                    }

                    ShowDistanceInfo(point);
                    currentPlacingStep = 1;
                    UndoReset();
                }
            }
            else
            {
              
            }
        }
    }

    public void UndoReset()
    {
        if (undoLines.Count > 0)
        {
            if (undoLines[undoLines.Count - 1].startPoint != null)
            {
                Destroy(undoLines[undoLines.Count - 1].startPoint);
            }
            if (undoLines[undoLines.Count - 1].endPoint != null)
            {
                Destroy(undoLines[undoLines.Count - 1].endPoint);
            }
            if (undoLines[undoLines.Count - 1].line != null)
            {
                Destroy(undoLines[undoLines.Count - 1].line);
            }
            if (undoLines[undoLines.Count - 1].measureInfo != null)
            {
                Destroy(undoLines[undoLines.Count - 1].measureInfo);
            }
            undoLines.Clear();
        }
    }

    public void UndoDelete()
    {
        if (lineChain.lines.Count > 0)
        {
            currentPlacingStep = 1;
            previewLine.gameObject.SetActive(false);
            previewLineEnabled = false;
            currentMeasureInfo = null;

            if (lineChain.lines[lineChain.lines.Count - 1].startPoint != null)
            {
                Destroy(lineChain.lines[lineChain.lines.Count - 1].startPoint);
            }
            if (lineChain.lines[lineChain.lines.Count - 1].endPoint != null)
            {
                Destroy(lineChain.lines[lineChain.lines.Count - 1].endPoint);
            }
            if (lineChain.lines[lineChain.lines.Count - 1].line != null)
            {
                Destroy(lineChain.lines[lineChain.lines.Count - 1].line);
            }
            if (lineChain.lines[lineChain.lines.Count - 1].measureInfo != null)
            {
                Destroy(lineChain.lines[lineChain.lines.Count - 1].measureInfo);
            }
            lineChain.lines.RemoveAt(lineChain.lines.Count - 1);
        }
    }

    public void Undo()
    {
        if (lineChain.lines.Count > 0) {
            currentPlacingStep = 1;
            previewLine.gameObject.SetActive(false);
            previewLineEnabled = false;
            currentMeasureInfo = null;

            if (lineChain.lines[lineChain.lines.Count - 1].endPoint == null)
            {
                if (lineChain.lines[lineChain.lines.Count - 1].startPoint != null)
                {
                    Destroy(lineChain.lines[lineChain.lines.Count - 1].startPoint);
                }
                if (lineChain.lines[lineChain.lines.Count - 1].measureInfo != null)
                {
                    Destroy(lineChain.lines[lineChain.lines.Count - 1].measureInfo);
                }
                lineChain.lines.RemoveAt(lineChain.lines.Count - 1);
            }
            else
            {
                if (lineChain.lines[lineChain.lines.Count - 1].startPoint != null)
                {
                    lineChain.lines[lineChain.lines.Count - 1].startPoint.SetActive(false);
                }
                if (lineChain.lines[lineChain.lines.Count - 1].endPoint != null)
                {
                    lineChain.lines[lineChain.lines.Count - 1].endPoint.SetActive(false);
                }
                if (lineChain.lines[lineChain.lines.Count - 1].line != null)
                {
                    lineChain.lines[lineChain.lines.Count - 1].line.SetActive(false);
                }
                if (lineChain.lines[lineChain.lines.Count - 1].measureInfo != null)
                {
                    lineChain.lines[lineChain.lines.Count - 1].measureInfo.SetActive(false);
                }
                undoLines.Add(lineChain.lines[lineChain.lines.Count - 1]);
                lineChain.lines.RemoveAt(lineChain.lines.Count - 1);
            }
        }

        if(lineChain.lines.Count == 0){
            stepBackButton.color = new Color(1f,1f,1f, 0.25f);
        }
    }

    public void Redo()
    {
        if (undoLines.Count > 0)
        {
            currentPlacingStep = 1;
            previewLine.gameObject.SetActive(false);
            previewLineEnabled = false;
            currentMeasureInfo = null;

            if (undoLines[undoLines.Count - 1].startPoint != null)
            {
                undoLines[undoLines.Count - 1].startPoint.SetActive(true);
            }
            if (undoLines[undoLines.Count - 1].endPoint != null)
            {
                undoLines[undoLines.Count - 1].endPoint.SetActive(true);
            }
            if (undoLines[undoLines.Count - 1].line != null)
            {
                undoLines[undoLines.Count - 1].line.SetActive(true);
            }
            if (undoLines[undoLines.Count - 1].measureInfo != null)
            {
                undoLines[undoLines.Count - 1].measureInfo.SetActive(true);
            }
            lineChain.lines.Add(undoLines[undoLines.Count - 1]);
            stepBackButton.color = new Color(1f, 1f, 1f, 0.93f);
            undoLines.RemoveAt(undoLines.Count - 1);
        }
    }

    public void DeleteAll()
    {
        if (lineChain.lines.Count > 0)
        {
            currentPlacingStep = 1;
            previewLine.gameObject.SetActive(false);
            previewLineEnabled = false;
            currentMeasureInfo = null;

            for (int i = 0; i < lineChain.lines.Count; i++)
            {
                if (lineChain.lines[i].startPoint != null)
                {
                    Destroy(lineChain.lines[i].startPoint);
                }
                if (lineChain.lines[i].endPoint != null)
                {
                    Destroy(lineChain.lines[i].endPoint);
                }
                if (lineChain.lines[i].line != null)
                {
                    Destroy(lineChain.lines[i].line);
                }
                if (lineChain.lines[i].measureInfo != null)
                {
                    Destroy(lineChain.lines[i].measureInfo);
                }
            }
            lineChain.lines.Clear();
            stepBackButton.color = new Color(1f, 1f, 1f, 0.25f);
        }
    }
}



