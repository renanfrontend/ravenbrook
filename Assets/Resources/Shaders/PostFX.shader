// Pos-processamento leve: vinheta, granulado, dessaturacao e pulso vermelho em PERIGO.
Shader "Hidden/Ravenbrook/PostFX"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Vignette, _Grain, _Desat, _Red;
            float4 _Tint;
            float rand(float2 co) { return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453); }
            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, l.xxx, _Desat) * _Tint.rgb;
                float2 d = i.uv - 0.5;
                float v = saturate(1 - dot(d, d) * _Vignette * 2.2);
                c.rgb *= v;
                c.rgb = lerp(c.rgb, float3(0.35, 0, 0), saturate((1 - v) * _Red));
                c.rgb += (rand(i.uv + frac(_Time.y)) - 0.5) * _Grain;
                return c;
            }
            ENDCG
        }
    }
}
