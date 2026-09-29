Shader "Hidden/RaptorRally/RiveToGUI"
{
    Properties { _MainTex ("Rive", 2D) = "white" {} _FlipY ("Flip native surface", Float) = 0 }
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
            float _FlipY;
            float4 frag(v2f_img input) : SV_Target
            {
                float2 uv=input.uv; if(_FlipY>.5) uv.y=1-uv.y;
                float4 color=tex2D(_MainTex,uv);
                // Rive writes gamma-space premultiplied pixels; IMGUI expects straight alpha.
                color.rgb=color.a>0.0001?color.rgb/color.a:0;
                #ifndef UNITY_COLORSPACE_GAMMA
                color.rgb=GammaToLinearSpace(color.rgb);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
