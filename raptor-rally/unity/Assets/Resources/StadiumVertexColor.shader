Shader "RaptorRally/StadiumVertexColor"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float4 color : COLOR; };
        void surf(Input input,inout SurfaceOutputStandard output)
        {
            output.Albedo=input.color.rgb;
            output.Metallic=0;
            output.Smoothness=.12;
            output.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
