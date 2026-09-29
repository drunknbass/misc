Shader "RaptorRally/Coachwork"
{
 Properties { _Color("Paint",Color)=(.1,.3,.6,1) _Metallic("Metallic",Range(0,1))=.5 _Glossiness("Smoothness",Range(0,1))=.7 }
 SubShader {
 Tags { "RenderType"="Opaque" }
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 fixed4 _Color; half _Metallic,_Glossiness;
 struct Input { float3 localPos; };
 void vert(inout appdata_full v,out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.localPos=v.vertex.xyz; }
 void surf(Input i,inout SurfaceOutputStandard o) {
   // Fine lower-body dust and splatter interrupt otherwise clean paint.
   float fleck=frac(sin(dot(floor(i.localPos.xz*90),float2(12.9898,78.233)))*43758.5453);
   float dirt=(1-smoothstep(-.22,.28+fleck*.1,i.localPos.y))*.45;
   o.Albedo=lerp(_Color.rgb,float3(.17,.11,.058),dirt);
   o.Metallic=_Metallic*(1-dirt); o.Smoothness=lerp(_Glossiness,.16,dirt);
   o.Occlusion=lerp(.82,1,smoothstep(-.25,.55,i.localPos.y)); o.Alpha=1;
 }
 ENDCG
 }
 FallBack "Standard"
}
