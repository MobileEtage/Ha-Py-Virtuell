Shader "Unlit/Transparent Colored Front" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	}

		SubShader{
			Tags{ "Queue" = "Transparent+1000" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

			ZTest Off
			ZWrite Off
			Lighting Off
			Fog{ Mode Off }

			Blend SrcAlpha OneMinusSrcAlpha

			Pass{
			Color[_Color]
			SetTexture[_MainTex]{ combine texture * primary }
		}
	}
}