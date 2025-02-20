Shader "Custom/RoundedSquareCutoutWithOutlineAndMargin"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _OutlineColor("Outline Color", Color) = (1, 0, 0, 1)
        _SquareSize("Square Size", float) = 0.5
        _CornerRadius("Corner Radius", float) = 0.1
        _YOffset("Y Offset", float) = 0.0
        _OutlineThickness("Outline Thickness", float) = 0.05
        _SmoothFactor("Smooth Factor", float) = 0.01
        _UseSmooth("Use Smoothing", int) = 1
    }
        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
            }
            LOD 200

            Pass
            {
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Off
                ZWrite Off
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float2 texcoord : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float4 worldPos : TEXCOORD1;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float4 _Color;
                float4 _OutlineColor;
                float _SquareSize;
                float _CornerRadius;
                float _YOffset;
                float _OutlineThickness;
                float _SmoothFactor;
                float _UseSmooth;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = v.texcoord;
                    o.worldPos = UnityObjectToClipPos(v.vertex);
                    return o;
                }

                float sdRoundRect(float2 p, float2 b, float r)
                {
                    float2 q = abs(p) - b + r;
                    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
                }

                half4 frag(v2f i) : SV_Target
                {
                    float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                    float2 uv = i.texcoord * 2.0 - 1.0;
                    uv.y += _YOffset;
                    uv.x *= aspectRatio;

                    // Adjust margin based on aspect ratio
                    float margin = aspectRatio >= 0.5625 ? 0.0 : (0.5625-aspectRatio) * 1;

                    float squareSize = _SquareSize - 2.0 * margin;
                    float cornerRadius = _CornerRadius;
                    float outlineThickness = _OutlineThickness;
                    float smoothFactor = _SmoothFactor;
                    float useSmooth = _UseSmooth;

                    float2 rectSize = float2(squareSize, squareSize);

                    float dist = sdRoundRect(uv, rectSize * 0.5, cornerRadius);

                    half4 baseColor = tex2D(_MainTex, i.texcoord) * _Color;

                    if (useSmooth > 0.5)
                    {
                        if (dist > outlineThickness + smoothFactor) {
                            return baseColor;
                        }
                        else if (dist > outlineThickness - smoothFactor) {
                            return lerp(_OutlineColor, baseColor, (dist - outlineThickness) / smoothFactor);
                        }
                        else if (dist > -smoothFactor) {
                            return lerp(half4(_OutlineColor.rgb, 0.0), _OutlineColor, dist / -smoothFactor);
                        }
                        else {
                            baseColor.a = 0.0;
                            return baseColor;
                        }
                    }
                    else
                    {
                        if (dist > outlineThickness) {
                            return baseColor;
                        }
                        else if (dist > 0.0) {
                            return _OutlineColor;
                        }
                        else {
                            baseColor.a = 0.0;
                            return baseColor;
                        }
                    }
                }
                ENDCG
            }
        }
            FallBack "Diffuse"
}