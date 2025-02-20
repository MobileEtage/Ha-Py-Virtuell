using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineChain : MonoBehaviour
{
    public List<Line> lines = new List<Line>();

    public bool ExistingPointHit(Vector3 currentTargetPoint, ref Vector3 pointObjectPosition)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].startPoint != null)
            {
                float dist = Vector3.Distance(currentTargetPoint, lines[i].startPoint.transform.position);
                if (dist < 0.05f)
                {
                    pointObjectPosition = lines[i].startPoint.transform.position;
                    return true;
                }
            }
            if (lines[i].endPoint != null)
            {
                float dist = Vector3.Distance(currentTargetPoint, lines[i].endPoint.transform.position);
                if (dist < 0.05f)
                {
                    pointObjectPosition = lines[i].endPoint.transform.position;
                    return true;
                }
            }
        }
        return false;
    }

    public Vector3 nearestPointObject()
    {
        float shortestDist = Mathf.Infinity;
        int index = -1;
        bool isStartPoint = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].startPoint != null)
            {
                float dist = Vector3.Distance(Camera.main.transform.position, lines[i].startPoint.transform.position);
                if (dist < 0.05f)
                {
                    shortestDist = dist;
                    index = i;
                    isStartPoint = true;
                }
            }
            if (lines[i].endPoint != null)
            {
                float dist = Vector3.Distance(Camera.main.transform.position, lines[i].endPoint.transform.position);
                if (dist < shortestDist)
                {
                    shortestDist = dist;
                    index = i;
                    isStartPoint = false;
                }
            }
        }

        if (index < 0) {
            return Vector3.zero;
        }
        else
        {
            if(isStartPoint)
                return lines[index].startPoint.transform.position;

            return lines[index].endPoint.transform.position;
        }
    }


}

public class Line
{
    public GameObject startPoint;
    public GameObject endPoint;
    public GameObject line;
    public GameObject measureInfo;
}