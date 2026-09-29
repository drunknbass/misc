Shader "RaptorRally/StadiumVertexColor"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard addshadow noforwardadd nolightmap nodynlightmap nometa nofog exclude_path:deferred
        #pragma target 3.0
        struct Input { float4 color : COLOR; float3 worldPos; };
        void surf(Input input,inout SurfaceOutputStandard output)
        {
            float dirt=(1-smoothstep(.12,.8,input.worldPos.y))*.23;
            output.Albedo=lerp(input.color.rgb,float3(.15,.095,.05),dirt)*.97;
            output.Metallic=0;
            output.Smoothness=.12;
            output.Alpha=1;
        }
        ENDCG
    }
    FallBack Off
}
