Shader "RaptorRally/PackedDirt"
{
 Properties { _MainTex("Dirt albedo",2D)="white"{} _NoiseTex("Surface detail",2D)="gray"{} _Color("Tint",Color)=(1,1,1,1) _Course("Course ribbon",Float)=1 }
 SubShader {
 Tags { "RenderType"="Opaque" }
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _MainTex,_NoiseTex; fixed4 _Color; half _Course;
 struct Input { float2 uv_MainTex; float3 worldPos; };
 void surf(Input i,inout SurfaceOutputStandard o) {
   float2 p=i.worldPos.xz;
   float4 grain=tex2D(_NoiseTex,p*.12);
   float broad=tex2D(_NoiseTex,p*.019+float2(.2,.4)).r;
   float fine=tex2D(_NoiseTex,p*.9).g;
   float u=i.uv_MainTex.x;
   float lane=exp(-pow((u-.5)/.32,4))*_Course;
   float rut=(pow(abs(sin(u*88+sin(i.uv_MainTex.y*4)*.7)),18))*.04*lane;
   float edge=(1-smoothstep(.0,.11,min(u,1-u)))*_Course;
   float variation=.78+grain.r*.30+fine*.10+broad*.28-rut;
   float3 albedo=lerp(tex2D(_MainTex,p*.17).rgb,tex2D(_MainTex,float2(-p.y,p.x)*.113+.37).rgb,.35);
   o.Albedo=albedo*(1.25-lane*.15)*variation*_Color.rgb;
   o.Albedo*=1-edge*.30;
   o.Normal=normalize(float3((grain.g-.5)*.24,(grain.b-.5)*.24,1));
   o.Smoothness=.10+lane*.12; o.Metallic=0; o.Occlusion=1-edge*.22; o.Alpha=1;
 }
 ENDCG
 }
 FallBack "Diffuse"
}
