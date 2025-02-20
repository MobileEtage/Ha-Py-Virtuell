using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyMe : MonoBehaviour
{
	public bool shouldDestroy = false;
	public float time = 0;
	private float timer = 5;
	
    void Start()
    {
        
    }

    void Update()
    {
	    if( shouldDestroy ){
	    	
	    	time += Time.deltaTime;
	    	if( time > timer ) Destroy(this.gameObject);
	    }
    }
}
