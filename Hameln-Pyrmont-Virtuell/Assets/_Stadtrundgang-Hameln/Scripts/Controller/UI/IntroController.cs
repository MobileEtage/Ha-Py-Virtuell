using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour
{	
	public CanvasGroup canvasGroup;
	
    void Start()
    {
	    StartCoroutine( InitCoroutine() );
    }

	public IEnumerator InitCoroutine(){
				
		yield return new WaitForSeconds(1.0f);
		
		/*
		float timer = 0.5f;
		while( timer > 0 ){
			canvasGroup.alpha = timer/0.5f;
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		canvasGroup.alpha = 0;
		*/
		
		SceneManager.LoadScene("Main");
		
		/*
		float waitTime = 1.5f;
		float startTime = Time.time;
		
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive );
		while (!asyncLoad.isDone){
			yield return null;
		}
		
		float diffTime = Time.deltaTime-startTime;
		if( diffTime < waitTime ){
			
			float timeToWait = Mathf.Clamp(waitTime-diffTime, 0, 1);
			yield return new WaitForSeconds(timeToWait);			
		}
		
		SceneManager.UnloadScene("Intro");
		*/
	}
}
