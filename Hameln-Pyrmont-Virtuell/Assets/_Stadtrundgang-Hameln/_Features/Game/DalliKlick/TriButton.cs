using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TriButton : MonoBehaviour
{
    public float alphaHitTestMinimumThreshold = 0.5f;

    void Start()
    {
	    GetComponent<Image>().alphaHitTestMinimumThreshold = alphaHitTestMinimumThreshold;
    }

}
