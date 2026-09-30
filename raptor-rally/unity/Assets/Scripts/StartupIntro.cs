using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaptorRally
{
    // Original JPEGs stay intact. Crops below are GPU UV windows in source pixels.
    public sealed class BrandArtwork : IDisposable
    {
        public readonly Texture2D Performance=Resources.Load<Texture2D>("Brand/FordPerformance");
        public readonly Texture2D Badge=Resources.Load<Texture2D>("Brand/RaptorBadge");
        readonly Material material=new Material(Resources.Load<Shader>("Brand/Artwork"));
        public static readonly Rect PerformancePixels=new Rect(100,480,540,214);
        public static readonly Rect OvalPixels=new Rect(229,499,271,106);
        public static readonly Rect BadgePixels=new Rect(24,10,292,242);
        public static Rect UV(Texture texture,Rect pixels) => new Rect(pixels.x/texture.width,1-(pixels.y+pixels.height)/texture.height,pixels.width/texture.width,pixels.height/texture.height);
        public static Rect Fit(Rect box,float aspect)
        {
            float width=Mathf.Min(box.width,box.height*aspect),height=width/aspect;
            return new Rect(box.center.x-width*.5f,box.center.y-height*.5f,width,height);
        }
        public void DrawBadge(Rect box,float opacity=1) => Draw(Badge,Fit(box,BadgePixels.width/BadgePixels.height),UV(Badge,BadgePixels),opacity,new Vector2(116,96));
        public void DrawPerformance(Rect box,float opacity=1) => Draw(Performance,Fit(box,PerformancePixels.width/PerformancePixels.height),UV(Performance,PerformancePixels),opacity,new Vector2(192,76));
        public void Draw(Texture texture,Rect rect,Rect uv,float opacity=1,Vector2 pixelGrid=default)
        {
            if(Event.current.type!=EventType.Repaint || opacity<=0) return;
            // Draw in screen coordinates with an identity GUI matrix so letterboxing and scale apply once.
            Vector3 a=GUI.matrix.MultiplyPoint3x4(new Vector3(rect.x,rect.y,0));
            Vector3 b=GUI.matrix.MultiplyPoint3x4(new Vector3(rect.xMax,rect.yMax,0));
            material.SetFloat("_Opacity",Mathf.Clamp01(opacity));
            material.SetVector("_Crop",new Vector4(uv.x,uv.y,uv.width,uv.height));
            material.SetVector("_PixelGrid",new Vector4(pixelGrid.x,pixelGrid.y,0,0));
            Matrix4x4 canvas=GUI.matrix;
            GUI.matrix=Matrix4x4.identity;
            try { Graphics.DrawTexture(new Rect(a.x,a.y,b.x-a.x,b.y-a.y),texture,uv,0,0,0,0,Color.white,material); }
            finally { GUI.matrix=canvas; }
        }
        public void Dispose() { StartupIntro.Release(material); }
    }

    public sealed class StartupIntro : IDisposable
    {
        public enum Card { VoxelFord, Black, FordPerformance, RaptorBadge, Complete }
        public const float Duration=11.7f;
        readonly BrandArtwork art;
        readonly GameObject stage;
        readonly Transform emblem;
        readonly Camera camera;
        readonly Mesh mesh;
        readonly Material voxelMaterial;
        readonly RenderTexture texture;
        float elapsed,skipRemaining=-1,skipOpacity;
        Card skipCard;
        bool rendered;
        public bool Active { get; private set; }=true;
        public float Elapsed => elapsed;
        public int VoxelCount { get; private set; }
        public Bounds MeshBounds => mesh.bounds;
        public Card CurrentCard => !Active?Card.Complete:skipRemaining>=0?skipCard:CardAt(elapsed);
        public float Opacity => !Active?0:skipRemaining>=0?skipOpacity*Mathf.Clamp01(skipRemaining/.28f):OpacityAt(elapsed);

        public StartupIntro(Transform parent,BrandArtwork artwork)
        {
            art=artwork;
            stage=new GameObject("Startup • isolated voxel stage"); stage.transform.SetParent(parent,false); stage.transform.localPosition=new Vector3(0,-2000,0);
            var model=new GameObject("Extruded pixel Ford emblem",typeof(MeshFilter),typeof(MeshRenderer)); model.transform.SetParent(stage.transform,false); model.layer=13;
            emblem=model.transform;
            mesh=BuildVoxelMesh(art.Performance,out int cells); VoxelCount=cells;
            voxelMaterial=new Material(Resources.Load<Shader>("Brand/VoxelEmblem"));
            model.GetComponent<MeshFilter>().sharedMesh=mesh; model.GetComponent<MeshRenderer>().sharedMaterial=voxelMaterial;
            model.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var go=new GameObject("Intro camera",typeof(Camera)); go.transform.SetParent(stage.transform,false);
            camera=go.GetComponent<Camera>(); camera.enabled=false; camera.allowHDR=false; camera.cullingMask=1<<13;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            camera.orthographic=true; camera.orthographicSize=2; camera.nearClipPlane=.1f; camera.farClipPlane=30;
            go.transform.localPosition=new Vector3(0,0,-12); go.transform.localRotation=Quaternion.identity;
            texture=new RenderTexture(1024,512,24,RenderTextureFormat.ARGB32) { name="3D Ford intro",antiAliasing=2 };
            camera.targetTexture=texture;
        }
        public static Card CardAt(float t)
        {
            if(t<4.7f) return Card.VoxelFord;
            if(t<5.05f) return Card.Black;
            if(t<7.85f) return Card.FordPerformance;
            if(t<8.2f) return Card.Black;
            if(t<11.4f) return Card.RaptorBadge;
            return t<Duration?Card.Black:Card.Complete;
        }
        static float Envelope(float t,float start,float fadeIn,float holdEnd,float end) => Mathf.Min(Mathf.SmoothStep(0,1,(t-start)/fadeIn),1-Mathf.SmoothStep(0,1,(t-holdEnd)/(end-holdEnd)));
        public static float OpacityAt(float t)
        {
            switch(CardAt(t)) {
                case Card.VoxelFord:return Envelope(t,0,.65f,4.05f,4.7f);
                case Card.FordPerformance:return Envelope(t,5.05f,.55f,7.25f,7.85f);
                case Card.RaptorBadge:return Envelope(t,8.2f,.75f,10.75f,11.4f);
                default:return 0;
            }
        }
        public void Advance(float dt)
        {
            if(!Active) return;
            dt=Mathf.Max(0,dt);
            if(skipRemaining>=0) { skipRemaining-=dt; if(skipRemaining<=0) Active=false; }
            else { elapsed+=dt; if(elapsed>=Duration) Active=false; }
        }
        public void Skip()
        {
            if(!Active || skipRemaining>=0) return;
            skipCard=CardAt(elapsed); skipOpacity=OpacityAt(elapsed); skipRemaining=.28f;
        }
        public void Render(bool reducedMotion)
        {
            if(!Active || CurrentCard!=Card.VoxelFord) return;
            if(reducedMotion && rendered) return;
            float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/4.05f));
            emblem.localRotation=Quaternion.Euler(reducedMotion?8:9*Mathf.Sin(progress*Mathf.PI*2),reducedMotion?-18:-28+388*progress,0);
            camera.Render(); rendered=true;
        }
        public void Draw(bool touch)
        {
            var saved=GUI.matrix; var color=GUI.color; int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity; GUI.depth=-100; GUI.color=Color.black;
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color=Color.white;
            var box=new Rect(Screen.width*.12f,Screen.height*.17f,Screen.width*.76f,Screen.height*.60f);
            if(CurrentCard==Card.VoxelFord) art.Draw(texture,BrandArtwork.Fit(box,2),new Rect(0,0,1,1),Opacity);
            else if(CurrentCard==Card.FordPerformance) art.DrawPerformance(new Rect(Screen.width*.16f,Screen.height*.27f,Screen.width*.68f,Screen.height*.42f),Opacity);
            else if(CurrentCard==Card.RaptorBadge) art.DrawBadge(box,Opacity);
            // No overlay during the explicit black intervals.
            if(!touch && CurrentCard!=Card.Black && skipRemaining<0 && elapsed>.65f)
            {
                float h=Mathf.Clamp(Screen.height*.06f,42,58),w=touch?112:180;
                var rect=new Rect(Screen.width-w-24,Screen.height-h-18,w,h);
                var style=new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter,fontSize=Mathf.Clamp(Screen.height/55,12,18) };
                style.normal.textColor=new Color(.55f,.57f,.60f);
                GUI.Label(rect,touch?"Skip intro ›":"Skip intro  /  Enter",style);
                if(GUI.Button(rect,GUIContent.none,GUIStyle.none)) Skip();
            }
            GUI.matrix=saved; GUI.color=color; GUI.depth=depth;
        }
        public static Mesh BuildVoxelMesh(Texture2D source,out int cells)
        {
            const int columns=88,rows=34; const float pitch=5.928f/columns,depth=.42f;
            Rect uv=BrandArtwork.UV(source,BrandArtwork.OvalPixels);
            var colors=new Color[columns,rows]; var occupied=new bool[columns,rows]; cells=0;
            for(int x=0;x<columns;x++) for(int y=0;y<rows;y++) {
                Color c=source.GetPixelBilinear(uv.x+(x+.5f)/columns*uv.width,uv.y+(y+.5f)/rows*uv.height);
                occupied[x,y]=Mathf.Max(c.r,c.g,c.b)>.13f;

                colors[x,y]=QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c;
            }
            // Brightness defines only the stepped silhouette. Dark blue interior pixels
            // must not become holes through the solid metal emblem.
            for(int y=0;y<rows;y++) {
                int first=columns,last=-1;
                for(int x=0;x<columns;x++) if(occupied[x,y]) { first=Mathf.Min(first,x); last=x; }
                for(int x=first;x<=last;x++) { occupied[x,y]=true; cells++; }
            }
            var grain=new List<Vector2>(); Vector2 cell=Vector2.zero;
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var vertexColors=new List<Color>(); var triangles=new List<int>();
            Action<Vector3,Vector3,Vector3,Vector3,Vector3,Color> face=(a,b,c,d,n,color)=>{
                int start=vertices.Count; vertices.AddRange(new[]{a,b,c,d});
                grain.AddRange(new[]{cell,cell,cell,cell});
                normals.AddRange(new[]{n,n,n,n}); vertexColors.AddRange(new[]{color,color,color,color});
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            };
            for(int x=0;x<columns;x++) for(int y=0;y<rows;y++) if(occupied[x,y]) {
                // Shared boundaries are computed identically on both neighboring cells.
                // Coplanar fronts remove the previously unsealed relief seams.
                float left=(x-columns*.5f)*pitch,right=(x+1-columns*.5f)*pitch,bottom=(y-rows*.5f)*pitch,top=(y+1-rows*.5f)*pitch;
                float front=-depth*.5f,back=depth*.5f; cell=new Vector2(x,y);
                Color c=colors[x,y],edge=Color.Lerp(c,new Color(.025f,.10f,.25f),.4f);
                face(new Vector3(left,bottom,front),new Vector3(left,top,front),new Vector3(right,top,front),new Vector3(right,bottom,front),Vector3.back,c);
                face(new Vector3(right,bottom,back),new Vector3(right,top,back),new Vector3(left,top,back),new Vector3(left,bottom,back),Vector3.forward,new Color(.012f,.045f,.12f));
                if(x==0 || !occupied[x-1,y]) face(new Vector3(left,bottom,back),new Vector3(left,top,back),new Vector3(left,top,front),new Vector3(left,bottom,front),Vector3.left,edge);
                if(x==columns-1 || !occupied[x+1,y]) face(new Vector3(right,bottom,front),new Vector3(right,top,front),new Vector3(right,top,back),new Vector3(right,bottom,back),Vector3.right,edge);
                if(y==0 || !occupied[x,y-1]) face(new Vector3(left,bottom,back),new Vector3(left,bottom,front),new Vector3(right,bottom,front),new Vector3(right,bottom,back),Vector3.down,edge);
                if(y==rows-1 || !occupied[x,y+1]) face(new Vector3(left,top,front),new Vector3(left,top,back),new Vector3(right,top,back),new Vector3(right,top,front),Vector3.up,edge);
            }
            var mesh=new Mesh { name="Ford oval • sampled extruded pixels",indexFormat=IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0,grain); mesh.SetNormals(normals); mesh.SetColors(vertexColors); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
            return mesh;
        }
        public void Dispose()
        {
            camera.targetTexture=null; texture.Release(); stage.SetActive(false);
            foreach(var item in new UnityEngine.Object[]{mesh,voxelMaterial,texture,stage}) Release(item);
        }
        internal static void Release(UnityEngine.Object item) { if(Application.isPlaying) UnityEngine.Object.Destroy(item); else UnityEngine.Object.DestroyImmediate(item); }
    }
}
