using UnityEngine;

public class ObjectFader : MonoBehaviour
{
	public float minDistance = 1.0f;
	public Transform mainCamera;
	public Renderer rend;
	private Material[] materials;
	private bool isTransparent = false;
	private Color[] originalEmissionColors;
	private bool[] wasInitiallyOpaque;
	private float lerpSpeed = 3.0f;

	void Start()
	{
		mainCamera = Camera.main.transform;
		rend = GetComponent<Renderer>();
		materials = rend.materials;
		originalEmissionColors = new Color[materials.Length];
		wasInitiallyOpaque = new bool[materials.Length];

		// Speichern der ursprünglichen Einstellungen der Materialien
		for ( int i = 0; i < materials.Length; i++ )
		{
			originalEmissionColors[i] = materials[i].GetColor( "_EmissionColor" );
			wasInitiallyOpaque[i] = (materials[i].GetFloat( "_Surface" ) == 0.0f);  // Annahme: 0.0f ist Opaque in URP/Lit
		}
	}

	void Update()
	{
		FadeObject();
	}

	public void FadeObject()
	{
		float currentDistance = Vector2.Distance( new Vector2( mainCamera.position.x, mainCamera.position.z ),
												 new Vector2( transform.position.x, transform.position.z ) );

		if ( currentDistance < minDistance && !isTransparent )
		{
			isTransparent = true;
			foreach ( var mat in materials )
			{
				SetMaterialTransparent( mat );
			}
		}
		else if ( currentDistance >= minDistance && isTransparent )
		{
			isTransparent = false;
			for ( int i = 0; i < materials.Length; i++ )
			{
				if ( wasInitiallyOpaque[i] )
				{
					SetMaterialOpaque( materials[i] );
				}
			}
		}

		UpdateMaterialProperties();
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

	private void UpdateMaterialProperties()
	{
		float lerpFactor = Time.deltaTime * lerpSpeed;

		for ( int i = 0; i < materials.Length; i++ )
		{
			Material mat = materials[i];
			Color baseColor = mat.color;

			if ( isTransparent )
			{
				baseColor.a = Mathf.MoveTowards( baseColor.a, 0f, lerpFactor );
				if ( baseColor.a <= 0.01f )
				{
					rend.enabled = false;
				}
			}
			else
			{
				rend.enabled = true;
				baseColor.a = Mathf.MoveTowards( baseColor.a, 1f, lerpFactor );
			}
			mat.color = baseColor;

			Color targetEmissionColor = isTransparent ? Color.black : originalEmissionColors[i];
			Color currentEmissionColor = mat.GetColor( "_EmissionColor" );
			currentEmissionColor = Color.Lerp( currentEmissionColor, targetEmissionColor, lerpFactor );
			mat.SetColor( "_EmissionColor", currentEmissionColor );
		}
	}
}
