using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightScaler : MonoBehaviour
{
	public Transform referenceObject;
	public Vector3 scaleReference = new Vector3( 0.16f, 1.0f );
	public Vector3 lightIntensity = new Vector3( 0.3f, 6.24f );

	private Light myLight;

	void Start()
    {
		myLight = GetComponent<Light>();
	}

    void Update()
    {
		float intensityPercentage = Mathf.InverseLerp( scaleReference.x, scaleReference.y, referenceObject.localScale.x );
		myLight.intensity = Mathf.Lerp( lightIntensity.x, lightIntensity.y, intensityPercentage );
	}
}
