Shader "Custom/AlphaVideo" {

	Properties{
		_MainColor("Main Color", Color) = (0.0, 0.0, 0.0, 1.0)
		_MainTex("Color (RGB)", 2D) = "white" {}
		_AlphaSide("AlphaSide (0-3)", Range(0, 3)) = 1
	}
	SubShader{

		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		CGPROGRAM

		//#pragma surface surf NoLighting alpha 
		#pragma surface surf Lambert alpha 

		float3 _MainColor;

		struct Input {
			 float2 uv_MainTex;
		};
		sampler2D _MainTex;
		int _AlphaSide;

		void surf(Input IN, inout SurfaceOutput o) {

			//o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _MainColor;
			o.Emission = tex2D(_MainTex, IN.uv_MainTex).rgb * _MainColor;

			if (_AlphaSide == 0) {

				if (IN.uv_MainTex.y <= 0.5) {
					o.Alpha = 0;
				}
				else {
					o.Alpha = tex2D(_MainTex, float2(IN.uv_MainTex.x, IN.uv_MainTex.y - 0.5)).rgb;
				}
			}
			else if (_AlphaSide == 1) {

				if (IN.uv_MainTex.y >= 0.5) {
					o.Alpha = 0;
				}
				else {
					o.Alpha = tex2D(_MainTex, float2(IN.uv_MainTex.x, IN.uv_MainTex.y + 0.5)).rgb;
				}
			}
			else if (_AlphaSide == 2) {

				if (IN.uv_MainTex.x <= 0.5) {
					o.Alpha = 0;
				}
				else {
					o.Alpha = tex2D(_MainTex, float2(IN.uv_MainTex.x - 0.5, IN.uv_MainTex.y)).rgb;
				}
			}
			else if (_AlphaSide == 3) {

				if (IN.uv_MainTex.x >= 0.5) {
					o.Alpha = 0;
				}
				else {
					o.Alpha = tex2D(_MainTex, float2(IN.uv_MainTex.x + 0.5, IN.uv_MainTex.y)).rgb;
				}
			}

		}
		ENDCG
	}
}