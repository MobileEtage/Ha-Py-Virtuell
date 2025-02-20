using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyesBlink : MonoBehaviour
{
	public SkinnedMeshRenderer mySkinnedMeshRenderer;
	public bool initOnStart = false;
	public int eyesClosedIndex = -1;
	public float blendShapeFactor = 1.0f;
	private bool initialized = false;

	void Start()
	{
		if (initOnStart) { Init(); }
	}

    public void Init()
    {
        if(mySkinnedMeshRenderer != null) {

            initialized = true;
            StartCoroutine(BlinkCoroutine());
        }
    }
	
	public IEnumerator BlinkCoroutine(){
		
		while( true ){

			float waitDelay = Random.Range(10, 15);		
			yield return new WaitForSeconds( waitDelay );

			eyesClosedIndex = GetBlendShapeIndex( mySkinnedMeshRenderer, "eyesClosed" );
			if ( eyesClosedIndex < 0 ) { continue; }

			float blinkTransitionTime = 0.2f;
			float currentTime = 0.0f;
			
			while( currentTime < blinkTransitionTime ){
				
				float lerpedValue = currentTime/blinkTransitionTime;
				mySkinnedMeshRenderer.SetBlendShapeWeight(eyesClosedIndex, lerpedValue * blendShapeFactor );
				yield return new WaitForEndOfFrame();
				currentTime += Time.deltaTime;
			}
			mySkinnedMeshRenderer.SetBlendShapeWeight(eyesClosedIndex, blendShapeFactor);
			yield return new WaitForEndOfFrame();

			currentTime = 0.0f;
			
			while( currentTime > 0 ){
				
				float lerpedValue = currentTime/blinkTransitionTime;
				mySkinnedMeshRenderer.SetBlendShapeWeight(eyesClosedIndex, lerpedValue * blendShapeFactor );
				yield return new WaitForEndOfFrame();
				currentTime -= Time.deltaTime;
			}
			
			mySkinnedMeshRenderer.SetBlendShapeWeight(eyesClosedIndex, 0);
			yield return new WaitForEndOfFrame();
		}
	}

	public int GetBlendShapeIndex(SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName)
	{
		if ( skinnedMeshRenderer == null ) { return -1; }
		if ( skinnedMeshRenderer.sharedMesh == null ) { return -1; }

		for ( int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ )
		{
			if ( skinnedMeshRenderer.sharedMesh.GetBlendShapeName( i ) == blendShapeName ) { return i; }
		}

		for ( int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ )
		{
			if ( skinnedMeshRenderer.sharedMesh.GetBlendShapeName( i ).Contains( blendShapeName ) ) { return i; }
		}
		return -1;
	}
}
