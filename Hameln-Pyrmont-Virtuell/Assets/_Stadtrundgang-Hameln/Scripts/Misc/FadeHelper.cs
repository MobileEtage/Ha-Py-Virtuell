using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;

public class FadeHelper : MonoBehaviour
{
	public float targetAlpha = 1.0f;
	public float lerpSpeed = 3.0f;

	public GameObject rootObject;
	public List<FadeObject> fadeObjects = new List<FadeObject>();

	void Start()
	{
		if( rootObject == null ) { rootObject = gameObject; }
		Renderer[] rend = rootObject.GetComponentsInChildren<Renderer>();

		for (int i = 0; i < rend.Length; i++)
		{
			if ( rend[i].materials.Length == 0 ) continue;

			FadeObject fadeObject = new FadeObject();
			fadeObject.rend = rend[i];
			fadeObject.materials = rend[i].materials;
			fadeObject.originalEmissionColors = new Color[fadeObject.materials.Length];
			fadeObject.wasInitiallyOpaque = new bool[fadeObject.materials.Length];
			fadeObject.startAlpha = new float[fadeObject.materials.Length];

			for ( int j = 0; j < fadeObject.materials.Length; j++ )
			{
				//fadeObject.originalEmissionColors[j] = fadeObject.materials[j].GetColor( "_EmissionColor" );
				fadeObject.wasInitiallyOpaque[j] = (fadeObject.materials[j].GetFloat( "_Surface" ) == 0.0f);  // 0.0f is Opaque in URP/Lit

				if ( fadeObject.materials[j].color.a < 1 ) { fadeObject.startAlpha[j] = (fadeObject.materials[j].color.a); }
				else { fadeObject.startAlpha[j] = 0; }
			}

			fadeObjects.Add( fadeObject );
		}
	}

	void Update()
	{
		Fade();
	}

	public void SetAlpha(float alpha)
	{
		if( alpha == 1 )
		{
			for ( int i = 0; i < fadeObjects.Count; i++ )
			{
				for ( int j = 0; j < fadeObjects[i].materials.Length; j++ )
				{
					if ( fadeObjects[i].wasInitiallyOpaque[j] ) { SetMaterialOpaque( fadeObjects[i].materials[j] ); }
				}

				UpdateMaterial( fadeObjects[i], alpha );
			}
		}
		else
		{
			for ( int i = 0; i < fadeObjects.Count; i++ )
			{
				for ( int j = 0; j < fadeObjects[i].materials.Length; j++ )
				{
					SetMaterialTransparent( fadeObjects[i].materials[j] );
				}

				UpdateMaterial( fadeObjects[i], alpha );
			}
		}
	}

	public void Fade()
	{
		for (int i = 0; i < fadeObjects.Count; i++)
		{
			Fade( fadeObjects[i] );
		}
	}

	public void Fade(FadeObject fadeObject)
	{
		if ( targetAlpha < 1.0f && !fadeObject.isTransparent )
		{
			fadeObject.isTransparent = true;
			foreach ( var mat in fadeObject.materials ){ SetMaterialTransparent( mat ); }
		}
		else if ( targetAlpha == 1 && fadeObject.isTransparent )
		{
			fadeObject.isTransparent = false;

			/*
			for ( int i = 0; i < fadeObject.materials.Length; i++ )
			{
				if ( fadeObject.wasInitiallyOpaque[i] ){ SetMaterialOpaque( fadeObject.materials[i] ); }
			}
			*/
		}

		UpdateMaterialProperties( fadeObject );
	}

	private void SetMaterialTransparent(Material mat)
	{
		mat.SetFloat( "_Surface", 1.0f );
		mat.SetOverrideTag( "RenderType", "Transparent" );
		mat.SetInt( "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha );
		mat.SetInt( "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha );
		mat.SetInt( "_ZWrite", 0 );
		mat.DisableKeyword( "_ALPHATEST_ON" );
		mat.EnableKeyword( "_ALPHABLEND_ON" );
		mat.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
		mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
	}

	private void SetMaterialOpaque(Material mat)
	{
		mat.SetFloat( "_Surface", 0.0f );
		mat.SetOverrideTag( "RenderType", "Opaque" );
		mat.SetInt( "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One );
		mat.SetInt( "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero );
		mat.SetInt( "_ZWrite", 1 );
		mat.DisableKeyword( "_ALPHATEST_ON" );
		mat.DisableKeyword( "_ALPHABLEND_ON" );
		mat.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
		mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
	}

	private void UpdateMaterialProperties(FadeObject fadeObject)
	{
		float lerpFactor = Time.deltaTime * lerpSpeed;

		for ( int i = 0; i < fadeObject.materials.Length; i++ )
		{
			Material mat = fadeObject.materials[i];
			Color baseColor = mat.color;

			if ( targetAlpha < 1 )
			{
				baseColor.a = Mathf.MoveTowards( baseColor.a, targetAlpha, lerpFactor );
				baseColor.a = Mathf.Clamp( baseColor.a, fadeObject.startAlpha[i], 1 );
				if ( baseColor.a <= 0.01f )
				{
					fadeObject.rend.enabled = false;
				}
			}
			else
			{
				fadeObject.rend.enabled = true;
				baseColor.a = Mathf.MoveTowards( baseColor.a, 1f, lerpFactor );
				baseColor.a = Mathf.Clamp( baseColor.a, fadeObject.startAlpha[i], 1 );

				if(baseColor.a > 0.99f )
				{
					for ( int j = 0; j < fadeObject.materials.Length; j++ )
					{
						if ( fadeObject.wasInitiallyOpaque[j] ) { SetMaterialOpaque( fadeObject.materials[j] ); }
					}
				}
			}
			mat.color = baseColor;

			//Color targetEmissionColor = targetAlpha < 1 ? Color.black : fadeObject.originalEmissionColors[i];
			//Color currentEmissionColor = mat.GetColor( "_EmissionColor" );
			//currentEmissionColor = Color.Lerp( currentEmissionColor, targetEmissionColor, lerpFactor );
			//mat.SetColor( "_EmissionColor", currentEmissionColor );
		}
	}

	private void UpdateMaterial(FadeObject fadeObject, float alpha)
	{
		float lerpFactor = Time.deltaTime * lerpSpeed;

		for ( int i = 0; i < fadeObject.materials.Length; i++ )
		{
			Material mat = fadeObject.materials[i];
			Color baseColor = mat.color;
			baseColor.a = alpha;
			baseColor.a = Mathf.Clamp( baseColor.a, fadeObject.startAlpha[i], 1 );
			mat.color = baseColor;

			if ( alpha <= 0 ) { fadeObject.rend.enabled = false; }
			else { fadeObject.rend.enabled = true; }

			//Color targetEmissionColor = targetAlpha < 1 ? Color.black : fadeObject.originalEmissionColors[i];
			//Color currentEmissionColor = mat.GetColor( "_EmissionColor" );
			//currentEmissionColor = Color.Lerp( currentEmissionColor, targetEmissionColor, lerpFactor );
			//mat.SetColor( "_EmissionColor", currentEmissionColor );
		}
	}
}

[System.Serializable]
public class FadeObject
{
	public Renderer rend;
	public Material[] materials;
	public Color[] originalEmissionColors;
	public bool[] wasInitiallyOpaque;
	public float[] startAlpha;

	public bool isTransparent = false;
}
