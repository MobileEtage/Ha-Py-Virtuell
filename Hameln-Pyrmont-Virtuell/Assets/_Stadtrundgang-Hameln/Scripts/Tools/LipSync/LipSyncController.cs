using UnityEngine;
using System.Collections.Generic;

public class LipSyncController : MonoBehaviour
{
	public float factor = 1.0f;
	public AudioSource audioSource;
	public SkinnedMeshRenderer skinnedMeshRenderer;

	private const int sampleRate = 44100;
	private const int sampleDataLength = 1024;
	private float[] clipSampleData;
	private float[] spectrumData;
	private List<float> bufferList = new List<float>();

	private Dictionary<string, int> visemeBlendShapeIndices = new Dictionary<string, int>
	{
		{"sil", 1}, {"PP", 2}, {"FF", 3}, {"TH", 4}, {"DD", 5},
		{"kk", 6}, {"CH", 7}, {"SS", 8}, {"nn", 9}, {"RR", 10},
		{"aa", 11}, {"E", 12}, {"I", 13}, {"O", 14}, {"U", 15}
	};

	private void Start()
	{
		if ( !audioSource )
		{
			Debug.LogError( "No audio source assigned!" );
			return;
		}

		clipSampleData = new float[sampleDataLength];
		spectrumData = new float[sampleDataLength];
		audioSource.Play();
	}

	private void Update()
	{
		AnalyzeAudio();
	}

	private void AnalyzeAudio()
	{
		if ( !audioSource.isPlaying )
		{
			return;
		}

		audioSource.clip.GetData( clipSampleData, audioSource.timeSamples );
		audioSource.GetSpectrumData( spectrumData, 0, FFTWindow.Hamming );

		float averageLoudness = CalculateAverageLoudness( clipSampleData );
		bufferList.Add( averageLoudness );

		if ( bufferList.Count >= sampleDataLength )
		{
			bufferList.RemoveAt( 0 );
		}

		string detectedViseme = DetectVisemeFromSpectrum( spectrumData );
		UpdateVisemes( detectedViseme, averageLoudness );
	}

	private float CalculateAverageLoudness(float[] clipSamples)
	{
		float totalLoudness = 0f;
		foreach ( var sample in clipSamples )
		{
			totalLoudness += Mathf.Abs( sample );
		}
		return totalLoudness / sampleDataLength;
	}

	private string DetectVisemeFromSpectrum(float[] spectrum)
	{
		float[] energyLevels = new float[5];

		for ( int i = 0; i < spectrum.Length; i++ )
		{
			if ( i < spectrum.Length * 0.2f )
			{
				energyLevels[0] += spectrum[i];
			}
			else if ( i < spectrum.Length * 0.4f )
			{
				energyLevels[1] += spectrum[i];
			}
			else if ( i < spectrum.Length * 0.6f )
			{
				energyLevels[2] += spectrum[i];
			}
			else if ( i < spectrum.Length * 0.8f )
			{
				energyLevels[3] += spectrum[i];
			}
			else
			{
				energyLevels[4] += spectrum[i];
			}
		}

		// Beispielhafte Zuordnung von Frequenzbändern zu Visemen basierend auf Energielevels
		if ( energyLevels[0] > energyLevels[1] && energyLevels[0] > energyLevels[2] && energyLevels[0] > energyLevels[3] && energyLevels[0] > energyLevels[4] )
		{
			return "aa";
		}
		else if ( energyLevels[3] > energyLevels[0] && energyLevels[3] > energyLevels[1] && energyLevels[3] > energyLevels[2] && energyLevels[3] > energyLevels[4] )
		{
			return "I";
		}
		else if ( energyLevels[4] > energyLevels[0] && energyLevels[4] > energyLevels[1] && energyLevels[4] > energyLevels[2] && energyLevels[4] > energyLevels[3] )
		{
			return "U";
		}

		return "sil";
	}

	private void UpdateVisemes(string viseme, float loudness)
	{
		float blendValue = loudness * 100f * factor; // Skaliere Lautstärke auf 0-100%

		// Setze alle BlendShapes auf 0, bevor den erkannten Wert anzuwenden
		foreach ( var kvp in visemeBlendShapeIndices )
		{
			skinnedMeshRenderer.SetBlendShapeWeight( kvp.Value, 0f );
		}

		// Setze den BlendShape des erkannten Viseme auf eine entsprechende Lautstärke
		if ( visemeBlendShapeIndices.ContainsKey( viseme ) )
		{
			skinnedMeshRenderer.SetBlendShapeWeight( visemeBlendShapeIndices[viseme], blendValue );
		}
	}
}
