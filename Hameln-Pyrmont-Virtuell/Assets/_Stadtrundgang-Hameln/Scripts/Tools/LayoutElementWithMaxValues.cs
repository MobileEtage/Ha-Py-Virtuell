using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[ExecuteInEditMode]
//[ExecuteAlways]
[RequireComponent(typeof(LayoutElement))]

public class LayoutElementWithMaxValues : MonoBehaviour
{

    private LayoutElement layoutElement;
    public RectTransform content;

    public bool controllWidth;
    public bool controllHeight;

    public float maxWidth;
    public float maxHeight;

    public float oldWidth = 0;
    public float oldHeight = 0;

    void Start()
    {
        layoutElement = GetComponent<LayoutElement>();
        if (content == null) content = GetComponent<RectTransform>();
    }

    public void Update()
    {
        if (SizeChanged())
        {
            if (controllHeight)
            {
                if (content.rect.height >= maxHeight)
                {
                    layoutElement.preferredHeight = maxHeight;
                }
                else if (content.rect.height < maxHeight)
                {
                    layoutElement.preferredHeight = content.rect.height;
                }
                else
                {
                    layoutElement.preferredHeight = -1;
                }
            }

            if (controllWidth)
            {
                if (content.rect.width >= maxWidth)
                {
                    layoutElement.preferredWidth = maxWidth;
                }
                else if (content.rect.width < maxWidth)
                {
                    layoutElement.preferredWidth = content.rect.width;
                }
                else
                {
                    layoutElement.preferredWidth = -1;
                }
            }
        }
    }

    public bool SizeChanged()
    {
        if (content.rect.height != oldHeight) { oldHeight = content.rect.height; return true; }
        if (content.rect.width != oldWidth) { oldWidth = content.rect.width; return true; }
        return false;
    }
}