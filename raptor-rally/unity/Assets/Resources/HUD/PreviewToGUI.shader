Shader "Hidden/RaptorRally/PreviewToGUI"
{
    Properties { _MainTex ("Preview", 2D) = "white" {} }
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
            float4 frag(v2f_img input) : SV_Target
            {
                float4 color=tex2D(_MainTex,input.uv);
                #ifndef UNITY_COLORSPACE_GAMMA
                color.rgb=GammaToLinearSpace(color.rgb);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
