using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryTester : MonoBehaviour
{
    void Start()
    {
	    StartCoroutine(UnloadAndSwitchSceneCoroutine());
    }

	public IEnumerator UnloadAndSwitchSceneCoroutine(){
		
		yield return new WaitForSeconds(5);
		
		//print("Memory1 " + System.GC.GetTotalMemory(false)/1000000);
		//yield return StartCoroutine(ToolsController.instance.CleanMemoryCoroutine());
		//print("Resources unloaded");
		//print("Memory2 " + System.GC.GetTotalMemory(false)/1000000);

		//yield return new WaitForSeconds(5);
		
		UnityEngine.SceneManagement.SceneManager.LoadScene("Main");

	}
}
