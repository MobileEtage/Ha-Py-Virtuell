using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;

public class LoadingAnimation : MonoBehaviour {

	public Color startColor = new Color( 255f/255f, 255f/255f, 255f/255f, 80f/255f);
	public Color endColor = new Color( 79f/255f, 79f/255f, 79f/255f, 223f/255f);
	public float colorLerpTime = 1.0f;
	public bool clockwise = true;
	public bool updateStartAndEndColor = false;
	
	//private List<Image> images = new List<Image>();
	private List<MPImage> images = new List<MPImage>();
	private Color tmpColor;
	private float currentLerpTime = 0;
	
	void Awake(){
		
		foreach( Transform child in transform ){
			if( child != transform && child.GetComponent<MPImage>() != null ){
				images.Add( child.GetComponent<MPImage>());
			}
		}
		
		for(int i = 0; i < images.Count; i++){
			float percentage = (float)i / (float)images.Count;
			images[i].color = Color.Lerp( startColor, endColor, percentage );
		}	
	}
	
	void Update(){	
		LerpColors();
	}

	private void LerpColors(){
		
		if( updateStartAndEndColor ){
			for(int i = 0; i < images.Count; i++){
				float percentage = (float)i / (float)images.Count;
				images[i].color = Color.Lerp( startColor, endColor, percentage );
			}	
		}
		
		if( currentLerpTime >= colorLerpTime ){
			
			if( clockwise ){
				tmpColor = images[ images.Count - 1 ].color;
	
				for(int i = (images.Count-1); i >= 0; i--){
	
					if( (i-1) >= 0 ){
						images[i].color = images[i-1].color;
					}else{
						images[i].color = tmpColor;
					}
				}
			}
			else{
				tmpColor = images[0].color;
	
				for(int i = 0; i < images.Count; i++){
	
					if( (i+1) < images.Count ){
						images[i].color = images[i+1].color;
					}else{
						images[i].color = tmpColor;
					}
				}
			}
			
			currentLerpTime = 0;
			
		}else{
			currentLerpTime += Time.deltaTime;
		}
	}
}
