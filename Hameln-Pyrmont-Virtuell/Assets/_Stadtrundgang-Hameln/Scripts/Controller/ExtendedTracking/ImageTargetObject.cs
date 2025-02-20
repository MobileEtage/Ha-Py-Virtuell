using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageTargetObject : MonoBehaviour
{
	public string id = "";
	public string savePath = "";
	[HideInInspector] public bool updateAlpha = false;
	[HideInInspector] public bool isVisible = true;
	[HideInInspector] public bool rendererGhost = false;
	[HideInInspector] public float targetAlpha = 1;
	public float currentAlpha = 1;
	
	private bool isLoading = false;
	private Renderer[] rend;
	
	void Awake(){
		
		rend = this.gameObject.GetComponentsInChildren<Renderer>();
	}
	
	public void Update(){
		
		if( !updateAlpha ) return;
		
		if( isVisible ){
			targetAlpha = 1;
		}else{
			targetAlpha = 0;
		}
		
		if( currentAlpha < 1 ){
				
			if( !rendererGhost ){
				rendererGhost = true;
				ToolsController.instance.ChangeRenderMode(this.gameObject, StandardShaderUtils.BlendMode.Ghost);
			}
		}
		else{
			
			if( rendererGhost ){
				rendererGhost = false;
				ToolsController.instance.ChangeRenderMode(this.gameObject, StandardShaderUtils.BlendMode.Opaque);
			}
		}
		
		for( int i = 0; i < rend.Length; i++ ){
			
			currentAlpha = Mathf.Lerp( currentAlpha, targetAlpha, Time.deltaTime );
			if( targetAlpha == 1 && (targetAlpha-currentAlpha) < 0.01f ) currentAlpha = targetAlpha;
			if( targetAlpha == 0 && currentAlpha < 0.01f ) currentAlpha = targetAlpha;
			ToolsController.instance.SetAlpha( rend, currentAlpha );
		}
	}

}
