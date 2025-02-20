Shader "Custom/TriMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _MaskTex1("MaskTexture1",2D) = "white"{}
        _MaskTex1Factor ("_MaskTex1Factor", Range(0.0, 1.0)) = 1.0
        _MaskTex2("MaskTexture2",2D) = "white"{}
        _MaskTex2Factor ("_MaskTex2Factor", Range(0.0, 1.0)) = 1.0
        _MaskTex3("MaskTexture3",2D) = "white"{}
        _MaskTex3Factor ("_MaskTex3Factor", Range(0.0, 1.0)) = 1.0
        _MaskTex4("MaskTexture4",2D) = "white"{}
        _MaskTex4Factor ("_MaskTex42Factor", Range(0.0, 1.0)) = 1.0
        _MaskTex5("MaskTexture5",2D) = "white"{}
        _MaskTex5Factor ("_MaskTex5Factor", Range(0.0, 1.0)) = 1.0
        _MaskTex6("MaskTexture6",2D) = "white"{}
        _MaskTex6Factor ("_MaskTex6Factor", Range(0.0, 1.0)) = 1.0
        _MaskTex7("MaskTexture7",2D) = "white"{}
        _MaskTex7Factor ("_MaskTex7Factor", Range(0.0, 1.0)) = 1.0
        _MaskTex8("MaskTexture8",2D) = "white"{}
        _MaskTex8Factor ("_MaskTex8Factor", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = IN.texcoord;
                
                #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1) * OUT.vertex.w;
                #endif
                
                OUT.color = IN.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _MaskTex1;
            sampler2D _MaskTex2;
            sampler2D _MaskTex3;
            sampler2D _MaskTex4;
            sampler2D _MaskTex5;
            sampler2D _MaskTex6;
            sampler2D _MaskTex7;
            sampler2D _MaskTex8;
			float _MaskTex1Factor;
			float _MaskTex2Factor;
			float _MaskTex3Factor;
			float _MaskTex4Factor;
			float _MaskTex5Factor;
			float _MaskTex6Factor;
			float _MaskTex7Factor;
			float _MaskTex8Factor;
			
            fixed4 frag(v2f IN) : SV_Target
            {
                //half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                half4 maskCol1 = tex2D(_MaskTex1, IN.texcoord);
                half4 maskCol2 = tex2D(_MaskTex2, IN.texcoord);
                half4 maskCol3 = tex2D(_MaskTex3, IN.texcoord);
                half4 maskCol4 = tex2D(_MaskTex4, IN.texcoord);
                half4 maskCol5 = tex2D(_MaskTex5, IN.texcoord);
                half4 maskCol6 = tex2D(_MaskTex6, IN.texcoord);
                half4 maskCol7 = tex2D(_MaskTex7, IN.texcoord);
                half4 maskCol8 = tex2D(_MaskTex8, IN.texcoord);
                //color.a = maskCol.a;
				
				if( color.a != 0 ){
				
	                color.a = 
	                (maskCol1.a*_MaskTex1Factor) + 
	                (maskCol2.a*_MaskTex2Factor) +
	                (maskCol3.a*_MaskTex3Factor) +
	                (maskCol4.a*_MaskTex4Factor) +
	                (maskCol5.a*_MaskTex5Factor) +
	                (maskCol6.a*_MaskTex6Factor) +
	                (maskCol7.a*_MaskTex7Factor) +
	                (maskCol8.a*_MaskTex8Factor);
	                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				}
                
                
                //color.a = maskCol2.a;
                //color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}