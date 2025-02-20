Shader "Custom/GridShader"
{
    Properties
    {
        _CellSize("Cell Size", Float) = 1.0
        _GridColor("Grid Color", Color) = (1, 1, 1, 1)
        _LineThickness("Line Thickness", Float) = 0.01
    }
        SubShader
    {
        Tags { "Queue" = "Overlay" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _CellSize;
            float4 _GridColor;
            float _LineThickness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * _CellSize;

                float lineX = abs(frac(uv.x) - 0.5);
                float lineY = abs(frac(uv.y) - 0.5);

                float gridLine = (lineX < _LineThickness || lineY < _LineThickness) ? 1.0 : 0.0;

                float4 color = lerp(fixed4(0, 0, 0, 0), _GridColor, gridLine);

                return color;
            }
            ENDCG
        }
    }
        FallBack "Diffuse"
}