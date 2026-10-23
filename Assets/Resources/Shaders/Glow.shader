// Brilho aditivo sem luz: clarão do tiro, rastro da bala, brilho dos itens.
Shader "Ravenbrook/Glow"
{
    Properties { _Color ("Cor", Color) = (1,0.8,0.4,1) _MainTex ("Textura", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Color;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 col : COLOR; };
            v2f vert (appdata_full v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; o.col = v.color; return o; }
            fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.uv) * _Color * i.col; }
            ENDCG
        }
    }
}
