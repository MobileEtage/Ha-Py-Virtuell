Shader "Custom/AlphaVideoDown" {

  Properties{ 
  	   _MainColor ("Main Color", Color)  = (1.0, 0.0, 0.0, 1.0)
       _MainTex("Color (RGB)", 2D) = "white" 
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
       
       void surf(Input IN, inout SurfaceOutput o) { 

       	   //o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _MainColor;
           o.Emission = tex2D(_MainTex, IN.uv_MainTex).rgb * _MainColor; 

           if(IN.uv_MainTex.y >= 0.5){ 
              o.Alpha=0; 
           }
           
           else{ 
                o.Alpha = tex2D(_MainTex, float2(IN.uv_MainTex.x, IN.uv_MainTex.y+0.5)).rgb;
                //o.Alpha = 1;
           }
           
       } 
       ENDCG 
  } 
}