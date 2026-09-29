using UnityEngine;
using UnityEngine.Rendering;

namespace RaptorRally
{
    // Small original environment map: reflections without a realtime probe pass.
    public sealed class RallyLighting : MonoBehaviour
    {
        Cubemap environment;
        public void Configure()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.30f,.39f,.49f);
            RenderSettings.ambientEquatorColor=new Color(.19f,.21f,.23f);
            RenderSettings.ambientGroundColor=new Color(.12f,.095f,.07f);
            QualitySettings.shadows=ShadowQuality.All;
            QualitySettings.shadowResolution=ShadowResolution.High;
            // The orthographic stadium needs one atlas; the near cascade was largely empty.
            QualitySettings.shadowCascades=0;
            QualitySettings.shadowDistance=185;
            QualitySettings.shadowCascade2Split=.32f;
            QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
            QualitySettings.antiAliasing=2;
            environment=new Cubemap(64,TextureFormat.RGBAHalf,true) { name="Coyote Basin reflection environment",filterMode=FilterMode.Trilinear };
            for(int face=0;face<6;face++)
            {
                var pixels=new Color[64*64];
                for(int y=0;y<64;y++) for(int x=0;x<64;x++)
                {
                    float u=(x+.5f)/32-1,v=(y+.5f)/32-1;
                    Vector3 d=face==0?new Vector3(1,-v,-u):face==1?new Vector3(-1,-v,u):face==2?new Vector3(u,1,v):face==3?new Vector3(u,-1,-v):face==4?new Vector3(u,-v,1):new Vector3(-u,-v,-1);
                    d.Normalize();
                    Color sky=Color.Lerp(new Color(.65f,.69f,.72f),new Color(.12f,.24f,.39f),Mathf.Pow(Mathf.Max(0,d.y),.55f));
                    Color ground=Color.Lerp(new Color(.32f,.25f,.17f),new Color(.10f,.09f,.075f),Mathf.Max(0,-d.y));
                    Color c=Color.Lerp(ground,sky,Mathf.SmoothStep(0,1,(d.y+.06f)/.16f));
                    float strip=Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,new Vector3(-.3f,.8f,.5f).normalized)),45);
                    c+=new Color(1,.92f,.79f)*strip*1.7f;
                    pixels[y*64+x]=c.linear;
                }
                environment.SetPixels(pixels,(CubemapFace)face);
            }
            environment.Apply(true,false);
            RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture=environment;
            RenderSettings.reflectionIntensity=.85f;
        }
        void OnDestroy() { if(environment!=null) { if(Application.isPlaying) Destroy(environment); else DestroyImmediate(environment); } }
    }
}
