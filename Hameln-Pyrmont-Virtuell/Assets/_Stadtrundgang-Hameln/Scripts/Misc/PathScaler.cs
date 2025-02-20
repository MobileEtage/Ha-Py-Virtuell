using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;

public class PathScaler : MonoBehaviour
{
    public Transform referenceScaleTranform;
    public List<Polyline> polyLines = new List<Polyline>();

    public float minThickness = 1;
    public float maxThickness = 10;
    public float minScale = 0.2f;
    public float maxScale = 2;

    void Awake()
    {
        UpdateTickness();
    }

    void Update()
    {
        UpdateTickness();
    }

    public void UpdateTickness()
    {
        float percentage = Mathf.InverseLerp(maxScale, minScale, referenceScaleTranform.localScale.x);
        float thickness = Mathf.Lerp(minThickness, maxThickness, percentage);
        for (int i = 0; i < polyLines.Count; i++) { polyLines[i].Thickness = thickness; }
    }
}
