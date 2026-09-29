Shader "RaptorRally/StadiumVertexColor"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float4 color : COLOR; float3 worldPos; };
        void surf(Input input,inout SurfaceOutputStandard output)
        {
            float grit=frac(sin(dot(floor(input.worldPos.xz*18+input.worldPos.y*7),float2(12.9898,78.233)))*43758.5453);
            float dirt=(1-smoothstep(.12,.8,input.worldPos.y))*.23;
            output.Albedo=lerp(input.color.rgb,float3(.15,.095,.05),dirt)*( .94+grit*.06);
            output.Metallic=0;
            output.Smoothness=.12;
            output.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
