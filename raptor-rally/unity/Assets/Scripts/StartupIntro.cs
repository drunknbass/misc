using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaptorRally
{
    // Original JPEGs stay intact. Crops below are GPU UV windows in source pixels.
    public sealed class BrandArtwork : IDisposable
    {
        public readonly Texture2D Performance=Resources.Load<Texture2D>("Brand/FordPerformance");
        public readonly Texture2D Ford=Resources.Load<Texture2D>("Brand/FordOval");
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
        public void DrawBadge(Rect box,float opacity=1) => Draw(Badge,Fit(box,BadgePixels.width/BadgePixels.height),UV(Badge,BadgePixels),opacity);
        public void DrawPerformance(Rect box,float opacity=1) => Draw(Performance,Fit(box,PerformancePixels.width/PerformancePixels.height),UV(Performance,PerformancePixels),opacity);
        public void DrawFord(Rect box,float opacity,float wipe) => Draw(Ford,Fit(box,(float)Ford.width/Ford.height),new Rect(0,0,1,1),opacity,new Vector2(88,34),wipe);
        public void Draw(Texture texture,Rect rect,Rect uv,float opacity=1,Vector2 pixelGrid=default,float wipe=1)
        {
            if(Event.current.type!=EventType.Repaint || opacity<=0) return;
            // Draw in screen coordinates with an identity GUI matrix so letterboxing and scale apply once.
            Vector3 a=GUI.matrix.MultiplyPoint3x4(new Vector3(rect.x,rect.y,0));
            Vector3 b=GUI.matrix.MultiplyPoint3x4(new Vector3(rect.xMax,rect.yMax,0));
            material.SetFloat("_Opacity",Mathf.Clamp01(opacity));
            material.SetFloat("_Wipe",Mathf.Clamp01(wipe));
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
        public enum Card { Ford, Black, Complete }
        public const float Duration=6.6f, CoinTime=2.1f;
        readonly AudioSource coinSource;
        readonly AudioClip coinClip;
        bool coinTriggered;
        public bool CoinTriggered => coinTriggered;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int RaptorIntroAudio(int active);
#endif
        static bool SoundReady(bool active)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return RaptorIntroAudio(active?1:0)!=0;
#else
            return true;
#endif
        }
        // Original two-note pulse chime. Band-limit harmonics and ramp the envelope
        // to retain the console timbre without harsh aliasing or click transients.
        public static float[] CoinSamples(int rate=44100)
        {
            var samples=new float[Mathf.CeilToInt(rate*.48f)];
            for(int i=0;i<samples.Length;i++) {
                float t=(float)i/rate,local=t<.085f?t:t-.085f;
                float length=t<.085f?.085f:.395f,frequency=t<.085f?1046.5f:1568f;
                float envelope=Mathf.Min(1,local/.003f)*Mathf.Clamp01((length-local)/.018f)*Mathf.Exp(-local*7);
                float pulse=0;
                for(int harmonic=1;harmonic<=9;harmonic+=2)
                    pulse+=Mathf.Sin(2*Mathf.PI*frequency*harmonic*local)/harmonic;
                samples[i]=pulse*.23f*envelope;
            }
            return samples;
        }
        readonly BrandArtwork art;
        float elapsed,skipRemaining=-1,skipOpacity;
        public bool Active { get; private set; }=true;
        public float Elapsed => elapsed;
        public bool SkipVisible => Active;
        public Card CurrentCard => !Active?Card.Complete:CardAt(elapsed);
        public float PixelReveal => RevealAt(elapsed);
        public float Opacity => !Active?0:skipRemaining>=0?skipOpacity*Mathf.Clamp01(skipRemaining/.28f):OpacityAt(elapsed);
        public StartupIntro(Transform parent,BrandArtwork artwork)
        {
            art=artwork;
            if(parent!=null && Application.isPlaying) {
                var sound=new GameObject("Intro coin chime"); sound.transform.SetParent(parent,false);
                coinSource=sound.AddComponent<AudioSource>(); coinSource.playOnAwake=false;
                coinSource.spatialBlend=0; coinSource.volume=.65f; coinSource.ignoreListenerPause=true;
                float[] samples=CoinSamples();
                coinClip=AudioClip.Create("Original retro coin",samples.Length,1,44100,false);
                coinClip.SetData(samples,0); coinSource.clip=coinClip;
            }
            SoundReady(true);
        }
        public static Card CardAt(float t) => t>=Duration?Card.Complete:t>=6.3f?Card.Black:Card.Ford;
        public static float RevealAt(float t) => Mathf.SmoothStep(0,1,(t-2.1f)/1.8f);
        public static float OpacityAt(float t) => t>=6.3f?0:Mathf.Min(Mathf.SmoothStep(0,1,t/.8f),1-Mathf.SmoothStep(0,1,(t-5.3f)/1f));
        public void Advance(float dt)
        {
            if(!Active) return;
            dt=Mathf.Max(0,dt);
            if(skipRemaining>=0) { skipRemaining-=dt; if(skipRemaining<=0) Active=false; }
            else {
                float before=elapsed; elapsed+=dt;
                bool ready=SoundReady(true);
                if(!coinTriggered && before<CoinTime && elapsed>=CoinTime) {
                    coinTriggered=true;
                    // Never queue a blocked chime for a later, unrelated gesture.
                    if(ready && elapsed<CoinTime+.25f && coinSource!=null) coinSource.Play();
                }
                if(elapsed>=Duration) Active=false;
            }
        }
        public void Skip()
        {
            if(!Active || skipRemaining>=0) return;
            skipOpacity=OpacityAt(elapsed); skipRemaining=.28f;
            if(coinSource!=null) coinSource.Stop(); SoundReady(false);
        }
        public void Draw(bool touch)
        {
            var saved=GUI.matrix; var color=GUI.color; int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity; GUI.depth=-100; GUI.color=Color.black;
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color=Color.white;
            var box=new Rect(Screen.width*.17f,Screen.height*.25f,Screen.width*.66f,Screen.height*.44f);
            if(CurrentCard==Card.Ford) art.DrawFord(box,Opacity,PixelReveal);
            // Skip belongs to the entire intro, independent of artwork opacity and wipe progress.
            if(!touch && SkipVisible)
            {
                float h=Mathf.Clamp(Screen.height*.06f,42,58),w=180;
                var rect=new Rect(Screen.width-w-24,Screen.height-h-18,w,h);
                var style=new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter,fontSize=Mathf.Clamp(Screen.height/55,12,18) };
                style.normal.textColor=new Color(.64f,.67f,.70f);
                GUI.Label(rect,"Skip intro  /  Enter",style);
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
            SoundReady(false);
            if(coinSource!=null) { coinSource.Stop(); Release(coinSource.gameObject); }
            if(coinClip!=null) Release(coinClip);
        }
        internal static void Release(UnityEngine.Object item) { if(Application.isPlaying) UnityEngine.Object.Destroy(item); else UnityEngine.Object.DestroyImmediate(item); }
    }
}
