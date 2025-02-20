using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrivacySettingsButton : MonoBehaviour
{
    void Start()
    {
	    if(!FirebaseController.instance.firebaseInitialized){
	    	//this.gameObject.SetActive(false);
	    }
    }

}
