using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioAnimator : MonoBehaviour
{
	public GameObject obj;
	public AudioSource audioSource;

	public float intensityMultiplier = 300.0f;
	public float smoothFactor = 0.1f;
	public float[] spectrumData = new float[256];
	public float smoothedIntensity = 0f;

	void Start()
	{
		audioSource = GetComponent<AudioSource>();
	}

	void Update()
	{
		if ( obj == null || !obj.activeInHierarchy ) return;

		if ( audioSource != null && audioSource.clip != null )
		{
			// Spektrumdaten abrufen
			audioSource.GetSpectrumData( spectrumData, 0, FFTWindow.Rectangular );

			// Bereich der relevanten Frequenzen einschränken und Durchschnittsintensität berechnen
			float averageIntensity = 0f;
			int numberOfSamples = Mathf.Min( spectrumData.Length, 128 ); // Optional: Nur die unteren 128 Frequenzen verwenden
			for ( int i = 0; i < numberOfSamples; i++ )
			{
				averageIntensity += spectrumData[i];
			}
			averageIntensity /= numberOfSamples;

			// Anwendung der Exponential Moving Average (EMA) als Glättung
			smoothedIntensity = Mathf.Lerp( smoothedIntensity, averageIntensity, smoothFactor );

			// Die Emission Farbe basierend auf der geglätteten Intensität setzen
			Color emissionColor = new Color( smoothedIntensity, smoothedIntensity, smoothedIntensity ) * intensityMultiplier;

			obj.GetComponent<Renderer>().material.SetColor( "_EmissionColor", emissionColor );
		}
	}
}
