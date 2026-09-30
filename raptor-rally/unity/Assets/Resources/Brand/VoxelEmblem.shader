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
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; };
            struct Varying { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; fixed4 color:COLOR; };
            Varying vert(Input v) { Varying o; o.vertex=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); o.color=v.color; return o; }
            fixed4 frag(Varying i):SV_Target
            {
                float3 n=normalize(i.normal);
                float key=saturate(dot(n,normalize(float3(-.4,.65,-1))));
                float rim=pow(1-abs(n.z),3);
                return fixed4(i.color.rgb*(.40+key*.80)+float3(.035,.09,.16)*rim,1);
            }
            ENDCG
        }
    }
}
