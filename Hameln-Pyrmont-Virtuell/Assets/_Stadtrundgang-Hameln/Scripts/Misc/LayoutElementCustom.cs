using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayoutElementCustom : MonoBehaviour
{
	public float targetHeightPercentage = 0.385f;
	
    void Update()
	{
		if( GetComponent<LayoutElement>() == null ) return;
	    AdjustHeight();
    }
    
	public void AdjustHeight(){
		
		Canvas parentCanvas = GetComponentInParent<Canvas>();
		if( parentCanvas == null ) return;
		
		float height = parentCanvas.GetComponent<RectTransform>().rect.height;
		
		float minAspect = 1170f/2532f;
		float maxAspect = 9f/16f;
		float myAspect = (float)Screen.width/(float)Screen.height;
		
		float percentage = 1;
		if( myAspect > minAspect ){
			
			percentage = Mathf.InverseLerp(minAspect, maxAspect, myAspect);
			percentage = 1-(percentage*0.2f);
			percentage = Mathf.Clamp(percentage, 0.5f, 1.0f);
		}
		
		float factor = targetHeightPercentage*percentage;
		float targetHeight = height*factor;
		
		GetComponent<LayoutElement>().preferredHeight = targetHeight;
	}
}
