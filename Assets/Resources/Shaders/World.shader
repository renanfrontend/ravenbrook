// Paredes, piso e teto: textura projetada pelo mundo (sem esticar em blocos grandes),
// rodape de outra cor abaixo de _Split e sujeira junto ao chao.
Shader "Ravenbrook/World"
{
    Properties
    {
        _Color ("Cor", Color) = (1,1,1,1)
        _Color2 ("Cor do rodape", Color) = (1,1,1,1)
        _Split ("Altura do rodape", Float) = -100
        _MainTex ("Textura", 2D) = "white" {}
        _Scale ("Repeticoes por metro", Float) = 0.5
        _Glossiness ("Brilho", Range(0,1)) = 0.15
        _Dirt ("Sujeira", Range(0,1)) = 0.5
        _EmissionColor ("Emissao", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color, _Color2, _EmissionColor;
        float _Split, _Scale, _Dirt; half _Glossiness;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = abs(IN.worldNormal);
            n = n / (n.x + n.y + n.z + 1e-4);
            float3 p = IN.worldPos * _Scale;
            fixed4 c = tex2D(_MainTex, p.zy) * n.x + tex2D(_MainTex, p.xz) * n.y + tex2D(_MainTex, p.xy) * n.z;
            float low = step(IN.worldPos.y, _Split);
            fixed4 tint = lerp(_Color, _Color2, low);
            float trim = (1 - step(0.06, abs(IN.worldPos.y - _Split))) * (1 - n.y);
            float dirt = saturate(1 - IN.worldPos.y / 1.2) * _Dirt * (1 - n.y);
            o.Albedo = c.rgb * tint.rgb * (1 - dirt * 0.6) * (1 - trim * 0.6);
            o.Smoothness = _Glossiness * c.a;
            o.Emission = _EmissionColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
