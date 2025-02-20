using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : UIBehaviour, IDragHandler
{
    public Canvas canvas;
    public RectTransform _rectTransform;
    public float margin = 100;
    
    protected override void Awake()
    {
        base.Awake();
        _rectTransform = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData pointerData)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            pointerData.position,
            //canvas.worldCamera,
            null,
            out position);

        //_rectTransform.anchoredPosition = position;

        Vector3 targetPosition = canvas.transform.TransformPoint(position);

        Vector3[] v = new Vector3[4];
        canvas.GetComponent<RectTransform>().GetWorldCorners(v);

        float maxX = float.MinValue;
        float minX = float.MaxValue;
        float maxY = float.MinValue;
        float minY = float.MaxValue;

        for (var i = 0; i < 4; i++)
        {
            if (v[i].x > maxX) { maxX = v[i].x; }
            if (v[i].x < minX) { minX = v[i].x; }
            if (v[i].y > maxY) { maxY = v[i].y; }
            if (v[i].y < minY) { minY = v[i].y; }
        }

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX + margin, maxX - margin);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY + margin, maxY - margin);

        _rectTransform.position= targetPosition;
    }
}
