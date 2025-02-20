Shader "Custom/AlphaVideoTest" {

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
		
		// Shadow Pass : Adding the shadows (from Directional Light)
		// by blending the light attenuation
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha 
			Name "ShadowPass"
			Tags {"LightMode" = "ForwardBase"}
 
			CGPROGRAM 
// Upgrade NOTE: excluded shader from DX11 and Xbox360; has structs without semantics (struct v2f members lightDir)
//#pragma exclude_renderers d3d11 xbox360
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma fragmentoption ARB_fog_exp2
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
 
			struct v2f { 
				float2 uv_MainTex : TEXCOORD1;
				float4 pos : SV_POSITION;
				LIGHTING_COORDS(3,4)
				float3	lightDir: TEXCOORD2;
			};
 
			float4 _MainTex_ST;
 
			sampler2D _MainTex;
			float4 _Color;
			float _ShadowIntensity;
 
			v2f vert (appdata_full v)
			{
				v2f o;
                o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.pos = UnityObjectToClipPos (v.vertex);
				o.lightDir = ObjSpaceLightDir( v.vertex );
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}
 
			float4 frag (v2f i) : COLOR
			{
				float atten = LIGHT_ATTENUATION(i);
 
				half4 c;
				c.rgb =  0;
				c.a = (1-atten) * _ShadowIntensity * (tex2D(_MainTex, i.uv_MainTex).a); 
				return c;
			}
			ENDCG
		}
	}
}