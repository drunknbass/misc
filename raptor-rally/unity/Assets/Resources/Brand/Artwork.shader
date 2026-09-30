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
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Opacity; float _Wipe; float4 _Crop; float4 _PixelGrid;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Varying { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            Varying vert(Input v) { Varying o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(Varying i):SV_Target
            {
                float2 uv=i.uv;
                float2 local=saturate((uv-_Crop.xy)/_Crop.zw);
                if(_PixelGrid.x>0 && _Wipe>0 && local.y>=1-_Wipe)
                {
                    float2 cell=min(floor(local*_PixelGrid.xy),_PixelGrid.xy-1);
                    uv=_Crop.xy+(cell+.5)/_PixelGrid.xy*_Crop.zw;
                }
                // Sample source colors at cell centers, then hold that color across
                // the entire cell. Explicit LOD avoids derivatives at pixel edges.
                fixed4 c=tex2Dlod(_MainTex,float4(uv,0,0)); c.a*=_Opacity; return c;
            }
            ENDCG
        }
    }
}
