// Manchas de sangue e marcas de tiro (transparente, recebe luz).
Shader "Ravenbrook/Decal"
{
    Properties { _Color ("Cor", Color) = (1,1,1,1) _MainTex ("Textura", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Geometry+50" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        ZWrite Off
        Offset -1, -1
        CGPROGRAM
        #pragma surface surf Lambert decal:blend
        sampler2D _MainTex; fixed4 _Color;
        struct Input { float2 uv_MainTex; };
        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb; o.Alpha = c.a;
        }
        ENDCG
    }
}
