using System.Collections.Generic;
using UnityEngine;

namespace RaptorRally
{
    // Original model meshes. Dimensions are deliberately tuned for the arcade scale.
    // Render geometry is independent of the existing Rigidbody/collision setup.
    public sealed class RaptorModel
    {
        public readonly Transform Body;
        public readonly Transform[] Wheels=new Transform[4], Pivots=new Transform[4];
        readonly Dictionary<Material, Geometry> parts=new Dictionary<Material, Geometry>();
        readonly Material paint, trim, glass, metal, alloy, rubber, light, amber, red, surface;
        readonly float length, width, axle, tire;
        readonly int type;
        readonly RaptorModelResources resources;

        public RaptorModel(Transform parent,int type,Color color,bool player)
        {
            this.type=type;
            resources=parent.gameObject.AddComponent<RaptorModelResources>();
            var spec=TruckSpec.Lineup[type]; length=spec.Length*(type==1?1.05f:1.10f); width=spec.Width*.395f;
            axle=length*(type==1?.325f:.32f); tire=type==1?.61f:type==0?.57f:.55f;
            Body=new GameObject(spec.Name+" • shaped bodywork").transform; Body.SetParent(parent,false);
            paint=Material(color*.94f,.42f,.76f); paint.shader=Resources.Load<Shader>("RallyPaint"); trim=Material(new Color(.032f,.039f,.045f),.05f,.24f);
            glass=Material(new Color(.025f,.048f,.065f),.72f,.94f);
            metal=Material(new Color(.32f,.36f,.39f),.72f,.58f);
            alloy=Material(new Color(.20f,.23f,.26f),.5f,.4f);
            rubber=Material(new Color(.018f,.023f,.027f),0,.1f);
            light=Material(new Color(.90f,.97f,1),.05f,.7f);
            amber=Material(new Color(1,.32f,.025f),.05f,.5f); red=Material(new Color(.68f,.025f,.015f),.1f,.55f);
            surface=new Material(Resources.Load<Shader>("RaptorVertexSurface")) { name="Raptor shared vertex surface" }; resources.Items.Add(surface);
            BuildBody(); BuildFace(); BuildDetails(player); BuildFinishDetails();
            Flush(Body);
            for(int side=-1;side<=1;side+=2) for(int front=-1;front<=1;front+=2)
            {
                int index=(side==1?2:0)+(front==1?1:0);
                var pivot=new GameObject("Steering / suspension pivot").transform; pivot.SetParent(parent,false);
                pivot.localPosition=new Vector3(side*width*1.04f,-.18f,front*axle); Pivots[index]=pivot;
                var wheel=new GameObject("Treaded tire and six-spoke wheel").transform; wheel.SetParent(pivot,false); Wheels[index]=wheel;
                BuildWheel(side,tire); Flush(wheel);
            }
            if(type==1)
            {
                var spare=new GameObject("Bronco full-size tailgate spare").transform; spare.SetParent(Body,false);
                spare.localPosition=new Vector3(0,.48f,-length*.5f-.24f);
                spare.localRotation=Quaternion.Euler(0,90,0); BuildWheel(1,tire); Flush(spare);
            }
        }

        Material Material(Color color,float metallic,float smooth)
        {
            var m=new Material(Shader.Find("Standard")); m.color=color;
            m.SetFloat("_Metallic",metallic); m.SetFloat("_Glossiness",smooth); resources.Items.Add(m); return m;
        }
        Geometry G(Material material)
        {
            if(!parts.TryGetValue(material,out var g)) parts[material]=g=new Geometry(); return g;
        }
        void Box(Material material,Vector3 p,Vector3 size,Quaternion? rotation=null)
        { G(material).Box(p,size,rotation??Quaternion.identity); }
        void Beam(Material material,Vector3 a,Vector3 b,float width,float depth)
        { Box(material,(a+b)*.5f,new Vector3(width,depth,Vector3.Distance(a,b)),Quaternion.LookRotation(b-a)); }
        void Flush(Transform parent)
        {
            var vertices=new List<Vector3>(); var normals=new List<Vector3>();
            var triangles=new List<int>(); var colors=new List<Color>(); var properties=new List<Vector4>();
            foreach(var pair in parts)
            {
                int offset=vertices.Count;
                vertices.AddRange(pair.Value.vertices); normals.AddRange(pair.Value.normals);
                foreach(int index in pair.Value.triangles) triangles.Add(index+offset);
                Color color=QualitySettings.activeColorSpace==ColorSpace.Linear?pair.Key.color.linear:pair.Key.color;
                var values=new Vector4(pair.Key.GetFloat("_Metallic"),pair.Key.GetFloat("_Glossiness"),pair.Key==paint?1:0,0);
                for(int i=0;i<pair.Value.vertices.Count;i++) { colors.Add(color); properties.Add(values); }
            }
            var mesh=new Mesh { name="Raptor articulated single-surface mesh" };
            if(vertices.Count>65535) mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetUVs(0,properties); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
            if(Application.isPlaying) mesh.UploadMeshData(true);
            resources.Items.Add(mesh);
            var go=new GameObject("Batched bodywork / wheel",typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=surface;
            parts.Clear();
        }

        void BuildBody()
        {
            float half=length*.5f, belt=type==1?.65f:.57f;
            Box(trim,new Vector3(0,-.27f,0),new Vector3(width*1.55f,.22f,length*.91f));
            // Raptor-specific wide fenders cover the off-road tires on every model.
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<100;i++)
                {
                    float z0=Mathf.Lerp(-half,half,i/100f),z1=Mathf.Lerp(-half,half,(i+1)/100f);
                    float b0=Bottom(z0),b1=Bottom(z1);
                    float w0=width*(.95f+.05f*Mathf.Sin(Mathf.PI*i/100f));
                    float w1=width*(.95f+.05f*Mathf.Sin(Mathf.PI*(i+1)/100f));
                    G(paint).Quad(new Vector3(side*w0,b0,z0),new Vector3(side*w1,b1,z1),new Vector3(side*w1,belt,z1),new Vector3(side*w0,belt,z0),Vector3.right*side);
                    G(paint).Quad(new Vector3(side*w0,belt,z0),new Vector3(side*w1,belt,z1),new Vector3(side*(w1-.12f),belt+.10f,z1),new Vector3(side*(w0-.12f),belt+.10f,z0),new Vector3(side,1,0));
                }
                foreach(int front in new[]{-1,1})
                {
                    float flare=type==1?1.28f:type==0?1.20f:1.22f;
                    Arch(paint,side*width*flare,front*axle,tire+.06f,tire+(type==1?.27f:.22f),width*(flare-.97f),type==1?10:22);
                    Arch(trim,side*width*(flare+.015f),front*axle,tire+.025f,tire+(type==1?.13f:.095f),.08f,type==1?10:22);
                    if(type==1)
                        Box(front==1?amber:red,new Vector3(side*width*(flare+.025f),.24f,front*axle+front*.64f),new Vector3(.025f,.075f,.12f));
                }
            }
            float cabFront=type==1?.82f:1.12f, cabRear=type==1?-1.73f:-.90f;
            float roofFront=type==1?.61f:.63f, roofRear=type==1?-1.65f:-.76f, roof=type==1?1.43f:1.20f;
            // Sloped windscreen, tapered greenhouse and chamfered roof define the silhouette.
            Vector3 fl=new Vector3(-width*.88f,belt+.1f,cabFront),fr=new Vector3(width*.88f,belt+.1f,cabFront);
            Vector3 tl=new Vector3(-width*.78f,roof-.08f,roofFront),tr=new Vector3(width*.78f,roof-.08f,roofFront);
            G(glass).Quad(fl,fr,tr,tl,Vector3.forward);
            G(glass).Quad(new Vector3(width*.88f,belt+.1f,cabRear),new Vector3(-width*.88f,belt+.1f,cabRear),new Vector3(-width*.78f,roof-.08f,roofRear),new Vector3(width*.78f,roof-.08f,roofRear),Vector3.back);
            foreach(int side in new[]{-1,1})
            {
                Vector3 a=new Vector3(side*width*.89f,belt+.12f,cabFront),b=new Vector3(side*width*.89f,belt+.12f,cabRear);
                Vector3 c=new Vector3(side*width*.79f,roof-.1f,roofRear),d=new Vector3(side*width*.79f,roof-.1f,roofFront);
                G(glass).Quad(a,b,c,d,Vector3.right*side);
                Beam(paint,a,d,.105f,.12f); Beam(paint,b,c,.12f,.13f); Beam(paint,c,d,.09f,.12f);
                float split=type==1?-.15f:.04f;
                Beam(trim,new Vector3(side*width*.9f,belt+.11f,split),new Vector3(side*width*.80f,roof-.09f,split),.1f,.1f);
                if(type==1) Beam(paint,new Vector3(side*width*.9f,belt+.11f,-.95f),new Vector3(side*width*.80f,roof-.09f,-.95f),.12f,.13f);
                // Door seams, handles, mirror housings and rock rails.
                foreach(float z in new[]{cabFront-.18f,split-.10f})
                {
                    Box(type==1?trim:paint,new Vector3(side*(width+.018f),.48f,z),new Vector3(.045f,.045f,.22f));
                    Box(trim,new Vector3(side*(width+.012f),.16f,z-.13f),new Vector3(.014f,.45f,.016f));
                }
                Box(type==1?trim:paint,new Vector3(side*width*1.17f,.77f,cabFront-.06f),new Vector3(.28f,.22f,.30f));
                Box(metal,new Vector3(side*width*1.025f,-.25f,0),new Vector3(.16f,.10f,axle*1.2f));
            }
            Loft(type==1?trim:paint,roofRear-.08f,roofFront+.08f,width*.82f,roof-.1f,roof+.045f,.08f);
            Loft(paint,cabFront,half,width*.9f,belt-.05f,belt+.11f,.09f);
            // Separate extractor layouts keep the three Raptor hoods distinct.
            if(type!=1)
            {
                Loft(paint,cabFront+.1f,half-.24f,width*.52f,belt+.08f,belt+.18f,.06f);
                if(type==0)
                {
                    Box(trim,new Vector3(0,belt+.188f,1.56f),new Vector3(.91f,.015f,.32f));
                    for(int i=0;i<5;i++) Box(metal,new Vector3(-.34f+i*.17f,belt+.20f,1.55f),new Vector3(.045f,.014f,.25f));
                }
                else foreach(int side in new[]{-1,1})
                {
                    Box(trim,new Vector3(side*.44f,belt+.188f,1.5f),new Vector3(.23f,.02f,.48f));
                    for(int i=0;i<3;i++) Box(metal,new Vector3(side*.44f,belt+.20f,1.35f+i*.15f),new Vector3(.20f,.012f,.035f));
                }
            }
            else foreach(int side in new[]{-1,1})
            {
                Box(trim,new Vector3(side*.65f,belt+.117f,1.22f),new Vector3(.26f,.015f,.52f));
                for(int i=0;i<4;i++) Box(metal,new Vector3(side*.65f,belt+.13f,1.04f+i*.12f),new Vector3(.22f,.012f,.027f));
            }
            if(type!=1)
            {
                // Deep open bed, raised side rails and a separate tailgate.
                Box(trim,new Vector3(0,.12f,(-half+cabRear)*.5f),new Vector3(width*1.68f,.10f,half+cabRear));
                for(int side=-1;side<=1;side+=2)
                {
                    Loft(paint,-half,cabRear,width,.40f,.64f,.05f,side*(width-.10f),.12f);
                    Box(trim,new Vector3(side*(width-.05f),.66f,(-half+cabRear)*.5f),new Vector3(.14f,.035f,half+cabRear));
                }
                Box(paint,new Vector3(0,.31f,-half+.055f),new Vector3(width*1.92f,.61f,.13f));
                Box(trim,new Vector3(0,.46f,-half-.015f),new Vector3(.24f,.055f,.025f));
            }
            else
            {
                Box(paint,new Vector3(0,.15f,-half+.06f),new Vector3(width*1.9f,.9f,.15f));
                foreach(float z in new[]{-.95f,-.20f}) Box(metal,new Vector3(0,roof+.05f,z),new Vector3(width*1.5f,.012f,.018f));
            }
        }

        float Bottom(float z)
        {
            float d=Mathf.Min(Mathf.Abs(z-axle),Mathf.Abs(z+axle)), r=tire+.055f;
            return d<r?-.18f+Mathf.Sqrt(r*r-d*d):-.23f;
        }
        void Arch(Material m,float x,float z,float inner,float outer,float depth,int segments=22)
        {
            for(int i=0;i<segments;i++)
            {
                float a=Mathf.Lerp(-.14f,Mathf.PI+.14f,(float)i/segments),b=Mathf.Lerp(-.14f,Mathf.PI+.14f,(float)(i+1)/segments);
                Vector3 p=new Vector3(x,-.18f+inner*Mathf.Sin(a),z+inner*Mathf.Cos(a));
                Vector3 q=new Vector3(x,-.18f+inner*Mathf.Sin(b),z+inner*Mathf.Cos(b));
                Vector3 r=new Vector3(x,-.18f+outer*Mathf.Sin(b),z+outer*Mathf.Cos(b));
                Vector3 s=new Vector3(x,-.18f+outer*Mathf.Sin(a),z+outer*Mathf.Cos(a));
                G(m).Quad(p,q,r,s,Vector3.right*Mathf.Sign(x));
                Vector3 inset=Vector3.right*(-Mathf.Sign(x)*depth);
                G(m).Quad(s,r,r+inset,s+inset,new Vector3(0,Mathf.Sin((a+b)/2),Mathf.Cos((a+b)/2)));
            }
        }
        void Loft(Material m,float rear,float front,float halfWidth,float bottom,float top,float bevel,float offsetX=0,float overrideWidth=0)
        {
            float w=overrideWidth>0?overrideWidth:halfWidth;
            bevel=Mathf.Min(bevel,Mathf.Min((top-bottom)*.45f,w*.45f));
            var ring=new[]{new Vector2(-w+bevel,bottom),new Vector2(-w,bottom+bevel),new Vector2(-w,top-bevel),new Vector2(-w+bevel,top),new Vector2(w-bevel,top),new Vector2(w,top-bevel),new Vector2(w,bottom+bevel),new Vector2(w-bevel,bottom)};
            for(int i=0;i<8;i++)
            {
                Vector2 a=ring[i],b=ring[(i+1)%8]; Vector3 normal=new Vector3((a.x+b.x)*.5f,(a.y+b.y-bottom-top)*.5f,0);
                G(m).Quad(new Vector3(a.x+offsetX,a.y,rear),new Vector3(b.x+offsetX,b.y,rear),new Vector3(b.x+offsetX,b.y,front),new Vector3(a.x+offsetX,a.y,front),normal);
                G(m).Triangle(new Vector3(offsetX,(bottom+top)*.5f,front),new Vector3(a.x+offsetX,a.y,front),new Vector3(b.x+offsetX,b.y,front),Vector3.forward);
                G(m).Triangle(new Vector3(offsetX,(bottom+top)*.5f,rear),new Vector3(a.x+offsetX,a.y,rear),new Vector3(b.x+offsetX,b.y,rear),Vector3.back);
            }
        }

        void BuildFace()
        {
            float z=length*.5f+.025f, y=type==1?.31f:.24f;
            Loft(trim,z-.08f,z+.025f,width*.95f,-.04f,.57f,.07f);
            // All three Raptor trims use FORD lettering, including Bronco Raptor.
            Lettering("FORD",new Vector3(.60f,y-.12f,z+.045f),.050f,metal);
            if(type!=2) foreach(float x in new[]{-.30f,0,.30f}) Box(amber,new Vector3(x,.565f,z+.04f),new Vector3(.08f,.045f,.035f));
            foreach(int side in new[]{-1,1})
            {
                float x=side*width*.79f;
                if(type==1)
                {
                    Ring(amber,new Vector3(x,y+.04f,z+.045f),.18f,.135f,Vector3.forward,32);
                    Box(amber,new Vector3(x-side*.1f,y+.04f,z+.055f),new Vector3(.34f,.047f,.025f));
                    Ring(light,new Vector3(x,y+.04f,z+.05f),.065f,0,Vector3.forward,20);
                }
                else
                {
                    Box(trim,new Vector3(x,y+.05f,z+.06f),new Vector3(.36f,.49f,.04f));
                    Box(light,new Vector3(x+side*.13f,y+.06f,z+.09f),new Vector3(.048f,.40f,.025f));
                    foreach(int edge in new[]{-1,1}) Box(light,new Vector3(x,y+.06f+edge*.19f,z+.09f),new Vector3(.29f,.048f,.025f));
                    Box(type==0?amber:light,new Vector3(x,.49f,z+.09f),new Vector3(.28f,.035f,.025f));
                }
                Box(red,new Vector3(side*width*.82f,.26f,-length*.5f-.045f),new Vector3(.17f,.41f,.07f));
                Box(light,new Vector3(side*width*.82f,.21f,-length*.5f-.084f),new Vector3(.13f,.04f,.012f));
            }
            Loft(type==2?metal:trim,z-.20f,z+.08f,width,-.36f,-.06f,.07f);
            Loft(type==1?trim:metal,z-.13f,z+.10f,width*.60f,-.49f,-.29f,.05f);
            foreach(int side in new[]{-1,1})
            {
                Box(metal,new Vector3(side*.58f,-.25f,z+.13f),new Vector3(.10f,.09f,.14f));
                Box(trim,new Vector3(side*width*.81f,-.14f,z+.09f),new Vector3(.24f,.13f,.045f));
                for(int lamp=0;lamp<2;lamp++)
                    Box(light,new Vector3(side*width*.81f+(lamp-.5f)*.09f,-.14f,z+.12f),new Vector3(.06f,.065f,.015f));
            }
            Box(type==2?metal:trim,new Vector3(0,-.26f,-length*.5f-.09f),new Vector3(width*2,.20f,.25f));
            if(type==0)
            {
                Box(trim,new Vector3(0,.28f,-length*.5f-.03f),new Vector3(width*1.55f,.36f,.035f));
                Lettering("FORD",new Vector3(-.44f,.16f,-length*.5f-.055f),.040f,metal,Quaternion.Euler(0,180,0));
            }
            if(type!=1) foreach(int side in new[]{-1,1})
            {
                Vector3 exhaust=new Vector3(side*width*.73f,-.42f,-length*.5f-.23f);
                Ring(metal,exhaust,.09f,.065f,Vector3.back,20);
                Ring(trim,exhaust+Vector3.forward*.005f,.065f,0,Vector3.back,20);
            }
        }
        void BuildDetails(bool player)
        {
            foreach(int side in new[]{-1,1})
            {
                Box(trim,new Vector3(side*(width+.02f),.43f,.91f),new Vector3(.025f,.18f,.33f));
                for(int i=0;i<3;i++) Box(metal,new Vector3(side*(width+.035f),.39f+i*.045f,.91f),new Vector3(.02f,.012f,.25f));
            }

        }
        void BuildFinishDetails()
        {
            // Grille mesh sits behind raised FORD lettering, with a dark surround.
            float front=length*.5f+.07f;
            for(int row=0;row<4;row++) for(int col=0;col<15;col++)
                Box(alloy,new Vector3((col-7)*.10f,.06f+row*.11f,front-.016f),new Vector3(.06f,.016f,.012f),Quaternion.Euler(0,0,row%2==0?18:-18));
            if(type!=1) {
                float rear=-length*.5f, cab=-.90f;
                for(int rib=-6;rib<=6;rib++) Box(trim,new Vector3(rib*.12f,.183f,(rear+cab)*.5f),new Vector3(.026f,.028f,cab-rear-.2f));
                foreach(int side in new[]{-1,1}) {
                    Box(trim,new Vector3(side*width*.88f,.25f,rear+.8f),new Vector3(.24f,.22f,.64f));
                    Box(metal,new Vector3(side*width*.83f,.50f,rear+.20f),new Vector3(.045f,.045f,.08f));
                }
            }
            // Chamfered rock rails, visible tow loops and machined bumper edges.
            foreach(int side in new[]{-1,1}) {
                Loft(trim,-axle*.66f,axle*.60f,.13f,-.29f,-.17f,.04f,side*width*1.07f);
                Ring(red,new Vector3(side*.58f,-.26f,front+.09f),.082f,.048f,Vector3.forward,20);
                Box(trim,new Vector3(side*width*.99f,.535f,-.27f),new Vector3(.018f,.016f,.86f));
            }
            // Split wipers follow the windscreen plane.
            float z=type==1?.82f:1.12f, belt=type==1?.65f:.57f;
            foreach(int side in new[]{-1,1}) Beam(trim,new Vector3(side*.12f,belt+.13f,z+.012f),new Vector3(side*.69f,belt+.21f,z-.025f),.022f,.020f);
        }
        void Lettering(string word,Vector3 origin,float unit,Material material,Quaternion? rotation=null)
        {
            string[] glyphs={"11111100001000011110100001000010000","01110100011000110001100011000101110","11110100011000111110101001001010001","11110100011000110001100011000111110"};
            for(int n=0;n<word.Length;n++) for(int row=0;row<7;row++) for(int col=0;col<5;col++)
                if(glyphs[n][row*5+col]=='1')
                {
                    Quaternion facing=rotation??Quaternion.identity;
                    Box(material,origin+facing*new Vector3(-(n*6+col)*unit,(6-row)*unit,0),new Vector3(unit*1.01f,unit*1.01f,.025f),facing);
                }
        }
        void BuildWheel(int side,float radius)
        {
            float[] xs={-.25f,-.22f,-.16f,.16f,.22f,.25f};
            float[] rs={radius*.67f,radius*.90f,radius,radius,radius*.90f,radius*.67f};
            for(int i=0;i<40;i++) for(int band=0;band<xs.Length-1;band++)
            {
                float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;
                G(rubber).TireQuad(new Vector3(xs[band],Mathf.Cos(a)*rs[band],Mathf.Sin(a)*rs[band]),new Vector3(xs[band],Mathf.Cos(b)*rs[band],Mathf.Sin(b)*rs[band]),new Vector3(xs[band+1],Mathf.Cos(b)*rs[band+1],Mathf.Sin(b)*rs[band+1]),new Vector3(xs[band+1],Mathf.Cos(a)*rs[band+1],Mathf.Sin(a)*rs[band+1]),new Vector3(0,Mathf.Cos((a+b)/2),Mathf.Sin((a+b)/2)));
            }
            for(int i=0;i<28;i++)
            {
                float a=i*360f/28,rad=a*Mathf.Deg2Rad;
                for(int row=-1;row<=1;row++) Box(trim,new Vector3(row*.15f,Mathf.Cos(rad)*(radius-.008f),Mathf.Sin(rad)*(radius-.008f)),new Vector3(.13f,.05f,.105f),Quaternion.Euler(a,0,row*13));
            }
            // 17-inch wheel proportions: 37-inch Bronco, 35-inch F-150, 33-inch Ranger tires.
            float rim=radius*(type==1?17f/37:type==0?17f/35:17f/33);
            Vector3 center=new Vector3(side*.253f,0,0);
            Ring(rubber,center,radius*.70f,rim,Vector3.right*side,40);
            Ring(alloy,center,rim,rim*.87f,Vector3.right*side,32);
            Ring(trim,center-Vector3.right*side*.01f,rim*.87f,0,Vector3.right*side,32);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3; Vector3 end=center+new Vector3(0,Mathf.Cos(a),Mathf.Sin(a))*rim*.90f;
                Beam(alloy,center,end,.065f,.085f);
                Vector3 bolt=center+new Vector3(0,Mathf.Cos(a),Mathf.Sin(a))*.10f;
                Box(light,bolt+Vector3.right*side*.03f,Vector3.one*.035f);
            }
            Ring(metal,center+Vector3.right*side*.02f,.12f,0,Vector3.right*side,16);
        }
        void Ring(Material material,Vector3 p,float outer,float inner,Vector3 normal,int segments)
        {
            Quaternion rotation=Quaternion.FromToRotation(Vector3.forward,normal);
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Vector3 va=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),vb=new Vector3(Mathf.Cos(b),Mathf.Sin(b),0);
                G(material).Quad(p+rotation*(va*outer),p+rotation*(vb*outer),p+rotation*(vb*inner),p+rotation*(va*inner),normal);
            }
        }

        internal sealed class Geometry
        {
            public readonly List<Vector3> vertices=new List<Vector3>(); public readonly List<int> triangles=new List<int>();
            public readonly List<Vector3> normals=new List<Vector3>();
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3 normal)
            {
                int start=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                Vector3 n=Vector3.Cross(b-a,c-a).normalized; if(Vector3.Dot(n,normal)<0) n=-n; normals.AddRange(new[]{n,n,n});
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)>=0) triangles.AddRange(new[]{start,start+1,start+2});
                else triangles.AddRange(new[]{start,start+2,start+1});
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
            { Triangle(a,b,c,normal); Triangle(a,c,d,normal); }
            public void TireQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 direction)
            {
                int start=vertices.Count; Quad(a,b,c,d,direction);
                for(int i=start;i<vertices.Count;i++) {
                    Vector3 p=vertices[i];
                    float shoulder=Mathf.Clamp01((Mathf.Abs(p.x)-.15f)/.10f);
                    normals[i]=new Vector3(Mathf.Sign(p.x)*shoulder*.9f,p.y,p.z).normalized;
                }
            }
            public void Box(Vector3 p,Vector3 size,Quaternion rotation)
            {
                var c=new Vector3[8];
                for(int i=0;i<8;i++) c[i]=p+rotation*Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),size);
                Quad(c[0],c[2],c[3],c[1],rotation*Vector3.back); Quad(c[4],c[5],c[7],c[6],rotation*Vector3.forward);
                Quad(c[0],c[4],c[6],c[2],rotation*Vector3.left); Quad(c[1],c[3],c[7],c[5],rotation*Vector3.right);
                Quad(c[2],c[6],c[7],c[3],rotation*Vector3.up); Quad(c[0],c[1],c[5],c[4],rotation*Vector3.down);
            }
        }
    }

    public sealed class RaptorModelResources : MonoBehaviour
    {
        public readonly List<Object> Items=new List<Object>();
        void OnDestroy()
        {
            foreach(var item in Items)
                if(Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
