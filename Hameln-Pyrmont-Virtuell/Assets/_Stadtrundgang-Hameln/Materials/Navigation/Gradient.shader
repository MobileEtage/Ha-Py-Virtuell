Shader "Custom/GradientShaderCylinder"
{
    Properties
    {
        _Color1("Base Color", Color) = (1,1,1,1)
        _Color2("Top Color", Color) = (0,0,0,1)
        _FadeHeight("Fade Height", Float) = 1.0
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            float4 _Color1;
            float4 _Color2;
            float _FadeHeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // Transform the vertex to world space
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Compute gradient color based on UV y-coordinate
                fixed4 color = lerp(_Color1, _Color2, i.uv.y);

            // Apply fade-out effect based on world height and _FadeHeight parameter
            float fadeFactor = smoothstep(_FadeHeight - _FadeHeight * 0.1, _FadeHeight, i.worldPos.y);
            color.a *= (1.0 - fadeFactor);

            return color;
        }
        ENDCG
    }
    }
        FallBack "Diffuse"
}