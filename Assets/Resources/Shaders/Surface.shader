// Material padrão do jogo (personagens e objetos). Emissão sem palavra-chave: funciona no .exe.
Shader "Ravenbrook/Surface"
{
    Properties
    {
        _Color ("Cor", Color) = (1,1,1,1)
        _MainTex ("Textura", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _Glossiness ("Brilho", Range(0,1)) = 0.2
        _Metallic ("Metal", Range(0,1)) = 0
        _EmissionColor ("Emissao", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex; sampler2D _BumpMap;
        fixed4 _Color; fixed4 _EmissionColor; half _Glossiness; half _Metallic;
        struct Input { float2 uv_MainTex; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Smoothness = _Glossiness;
            o.Metallic = _Metallic;
            o.Emission = _EmissionColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
