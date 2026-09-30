Shader "RaptorRally/Brand Artwork"
{
    Properties { _MainTex ("Original artwork", 2D) = "black" {} _Opacity ("Opacity", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Opacity;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Varying { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            Varying vert(Input v) { Varying o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(Varying i):SV_Target { fixed4 c=tex2D(_MainTex,i.uv); c.a*=_Opacity; return c; }
            ENDCG
        }
    }
}
