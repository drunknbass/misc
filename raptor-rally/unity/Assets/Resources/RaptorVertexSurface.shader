Shader "RaptorRally/VertexSurface"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        // One forward color pass and one caster pass. Material properties live on
        // vertices, so paint, glass, rubber and metal share each articulated mesh.
        #pragma surface surf Standard vertex:vert addshadow noforwardadd nolightmap nodynlightmap nometa nofog exclude_path:deferred
        #pragma target 3.0
        struct Input { float4 color : COLOR; float4 properties; float3 localPos; };
        void vert(inout appdata_full v,out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.color=v.color; o.properties=v.texcoord; o.localPos=v.vertex.xyz;
        }
        void surf(Input i,inout SurfaceOutputStandard o)
        {
            // Broad dust transition is stable at subpixel sizes; avoid unfiltered
            // procedural hash speckles on distant vehicles.
            half dirt=(1-smoothstep(-.22,.33,i.localPos.y))*.45*i.properties.z;
            o.Albedo=lerp(i.color.rgb,half3(.17,.11,.058),dirt);
            o.Metallic=i.properties.x*(1-dirt);
            o.Smoothness=lerp(i.properties.y,.16,dirt);
            o.Occlusion=lerp(1,lerp(.82,1,smoothstep(-.25,.55,i.localPos.y)),i.properties.z);
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack Off
}
