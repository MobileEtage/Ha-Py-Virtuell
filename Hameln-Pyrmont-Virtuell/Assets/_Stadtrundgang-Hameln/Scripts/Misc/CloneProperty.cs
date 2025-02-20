using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloneProperty : MonoBehaviour
{
	public CanvasGroup otherCanvasGroup;

    void Update()
    {
	    this.GetComponent<CanvasGroup>().alpha = otherCanvasGroup.alpha;
    }
}
