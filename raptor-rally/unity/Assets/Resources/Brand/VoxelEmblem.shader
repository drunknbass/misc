Shader "RaptorRally/Voxel Emblem"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; float2 cell:TEXCOORD0; };
            struct Varying { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; fixed4 color:COLOR; };
            Varying vert(Input v)
            {
                Varying o; o.vertex=UnityObjectToClipPos(v.vertex);
                // Small, fixed microfacet tilts catch light as the solid rotates.
                // They alter shading only; every geometric seam remains sealed.
                float2 grain=frac(sin(float2(dot(v.cell,float2(12.9898,78.233)),dot(v.cell,float2(39.346,11.135))))*43758.5453)-.5;
                float3 n=normalize(v.normal+float3(grain*.055*abs(v.normal.z),0));
                o.normal=UnityObjectToWorldNormal(n); o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.color=v.color; return o;
            }
            fixed4 frag(Varying i):SV_Target
            {
                float3 n=normalize(i.normal);
                float3 light=normalize(float3(-.4,.65,-1));
                float key=saturate(dot(n,light));
                float rim=pow(1-abs(n.z),3);
                float3 view=normalize(_WorldSpaceCameraPos.xyz-i.world);
                float glint=pow(saturate(dot(n,normalize(light+view))),90);
                float chrome=saturate(dot(i.color.rgb,float3(.2126,.7152,.0722))*2);
                float3 shine=lerp(float3(.12,.24,.42),float3(.8,.9,1),chrome)*glint*.42;
                return fixed4(i.color.rgb*(.40+key*.80)+float3(.035,.09,.16)*rim+shine,1);
            }
            ENDCG
        }
    }
}
