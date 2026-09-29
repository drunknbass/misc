using UnityEngine;

namespace RaptorRally
{
    // The garage renders the same 3D mesh used by the racing trucks.
    public sealed class GaragePreview
    {
        readonly Transform stage;
        readonly Camera camera;
        public readonly RenderTexture Texture;
        readonly RenderTexture sceneTexture;
        readonly Material guiColor,plinthMaterial;
        Transform model;
        int selected=-1;

        public GaragePreview(Transform parent)
        {
            stage=new GameObject("Garage studio").transform; stage.SetParent(parent,false); stage.localPosition=new Vector3(0,-1000,0);
            var go=new GameObject("Garage detail camera",typeof(Camera)); go.transform.SetParent(stage,false);
            camera=go.GetComponent<Camera>(); camera.enabled=false; camera.cullingMask=1<<10;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.045f,.06f);
            camera.orthographic=true; camera.orthographicSize=2.75f; camera.nearClipPlane=.1f; camera.farClipPlane=30;
            go.transform.localPosition=new Vector3(4.8f,3.25f,6.3f); go.transform.LookAt(stage.position+new Vector3(0,.35f,0));
            sceneTexture=new RenderTexture(1000,700,24,RenderTextureFormat.ARGB32) { name="Garage scene render",antiAliasing=4 };
            Texture=new RenderTexture(1000,700,0,RenderTextureFormat.ARGB32) { name="Live Raptor garage preview" };
            guiColor=new Material(Resources.Load<Shader>("HUD/PreviewToGUI"));
            camera.targetTexture=sceneTexture;
            var plinth=GameObject.CreatePrimitive(PrimitiveType.Cylinder); plinth.name="Garage display plinth";
            plinth.transform.SetParent(stage,false); plinth.transform.localPosition=new Vector3(0,-.83f,0); plinth.transform.localScale=new Vector3(6,.06f,6);
            plinth.layer=10; Object.Destroy(plinth.GetComponent<Collider>());
            plinthMaterial=new Material(Shader.Find("Standard")) { color=new Color(.055f,.075f,.085f) };
            plinthMaterial.SetFloat("_Glossiness",.35f); plinth.GetComponent<Renderer>().sharedMaterial=plinthMaterial;
            foreach(Vector3 p in new[]{new Vector3(2,5,4),new Vector3(-4,3,-2)})
            {
                var lamp=new GameObject("Studio soft light",typeof(Light)); lamp.transform.SetParent(stage,false); lamp.transform.localPosition=p;
                var light=lamp.GetComponent<Light>(); light.type=LightType.Point; light.range=15; light.intensity=.8f; light.cullingMask=1<<10;
            }
        }
        public void Render(int vehicle)
        {
            if(selected==vehicle && Texture.IsCreated()) return;
            if(selected!=vehicle)
            {
                if(model!=null) { model.gameObject.SetActive(false); Object.Destroy(model.gameObject); }
                model=new GameObject("Selected Raptor display model").transform; model.SetParent(stage,false);
                new RaptorModel(model,vehicle,TruckSpec.Lineup[vehicle].Color,false);
                foreach(var t in model.GetComponentsInChildren<Transform>()) t.gameObject.layer=10;
                selected=vehicle;
            }
            model.localRotation=Quaternion.Euler(0,-12,0);
            camera.Render();
            bool srgb=GL.sRGBWrite; GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
            Graphics.Blit(sceneTexture,Texture,guiColor); GL.sRGBWrite=srgb;
        }
        public void Dispose()
        {
            camera.targetTexture=null; Texture.Release(); sceneTexture.Release();
            foreach(Object item in new Object[]{Texture,sceneTexture,guiColor,plinthMaterial})
                if(Application.isPlaying) Object.Destroy(item); else Object.DestroyImmediate(item);
        }
    }
}
