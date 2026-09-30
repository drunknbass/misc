using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaptorRally
{
    // A closed driving shell matches the exterior cabin dimensions. Only the
    // player's cockpit camera sees this layer; the exterior remains intact.
    public sealed class DriverCockpit
    {
        public const int ExteriorLayer=14, InteriorLayer=15;
        public readonly Transform Root;
        public readonly Vector3 Eye;
        readonly Transform wheel;
        readonly RaptorModelResources resources;
        readonly Material material;
        readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        readonly List<int> triangles=new List<int>();
        readonly List<Color> colors=new List<Color>();
        readonly List<Vector4> properties=new List<Vector4>();
        static readonly Color trim=new Color(.09f,.105f,.12f),rubber=new Color(.028f,.034f,.04f),metal=new Color(.32f,.36f,.4f);

        public DriverCockpit(Transform parent,int type,Color paint)
        {
            var spec=TruckSpec.Lineup[type];
            float width=spec.Width*.395f,length=spec.Length*(type==1?1.05f:1.10f);
            float belt=type==1?.65f:.57f,roof=type==1?1.43f:1.20f;
            float front=type==1?.82f:1.12f,roofFront=type==1?.61f:.63f;
            Eye=new Vector3(-width*.38f,roof-.24f,type==1?.05f:.18f);
            Root=new GameObject(spec.Name+" • driver cabin").transform; Root.SetParent(parent,false);
            resources=Root.gameObject.AddComponent<RaptorModelResources>();
            material=new Material(Resources.Load<Shader>("RaptorVertexSurface")); resources.Items.Add(material);
            // Hood and cowl follow the external model, with closed sides underneath.
            Box(paint,new Vector3(0,belt+.045f,(front+length*.5f)*.5f),new Vector3(width*1.8f,.13f,length*.5f-front));
            if(type!=1) {
                Box(paint*.85f,new Vector3(0,belt+.13f,(front+length*.5f)*.5f),new Vector3(width*1.05f,.08f,length*.5f-front-.23f));
                foreach(int side in new[]{-1,1}) Box(rubber,new Vector3(side*(type==0?.26f:.44f),belt+.178f,1.55f),new Vector3(type==0?.34f:.23f,.012f,.28f));
            }
            else foreach(int side in new[]{-1,1}) Box(rubber,new Vector3(side*.65f,belt+.116f,1.22f),new Vector3(.26f,.012f,.5f));
            // Dashboard extends below the viewpoint and seals the chassis view.
            Box(trim,new Vector3(0,belt-.13f,front-.17f),new Vector3(width*1.85f,.44f,.54f));
            Box(rubber,new Vector3(0,belt+.087f,front-.17f),new Vector3(width*1.83f,.06f,.58f));
            Box(metal,new Vector3(0,belt-.045f,front-.45f),new Vector3(width*1.67f,.018f,.025f));
            foreach(int side in new[]{-1,1}) {
                var bottom=new Vector3(side*width*.89f,belt+.12f,front);
                var top=new Vector3(side*width*.79f,roof+.015f,roofFront);
                Beam(trim,bottom,top,.067f,.075f);
                Box(trim,new Vector3(side*width*.92f,.28f,-.17f),new Vector3(.13f,.78f,2.5f));
                Box(rubber,new Vector3(side*width*.85f,belt-.02f,-.15f),new Vector3(.16f,.12f,1.8f));
                for(int i=0;i<4;i++) Box(metal,new Vector3(side*width*.71f,belt-.07f+i*.023f,front-.45f),new Vector3(.18f,.009f,.025f));
            }
            Box(trim,new Vector3(0,roof,roofFront-.06f),new Vector3(width*1.65f,.10f,.19f));
            Box(rubber,new Vector3(0,roof+.03f,-.43f),new Vector3(width*1.67f,.08f,2.1f));
            Box(rubber,new Vector3(0,-.10f,0),new Vector3(width*1.8f,.16f,2.2f));
            // Low binnacle and center display: road stays visible above the cowl.
            float wheelY=belt+(type==1?.20f:.07f),wheelZ=front-(type==1?.42f:.58f);
            Box(rubber,new Vector3(Eye.x,belt+.025f,front-.45f),new Vector3(.52f,.14f,.19f));
            for(int side=-1;side<=1;side+=2) {
                var center=new Vector3(Eye.x+side*.13f,belt+.025f,front-.551f);
                Ring(metal,center,.085f,.007f,32);
                Beam(new Color(.3f,.82f,.95f),center,center+new Vector3(side*.035f,.043f,0),.012f,.008f);
            }
            Box(rubber,new Vector3(width*.3f,belt-.055f,front-.455f),new Vector3(.37f,.22f,.045f));
            Box(new Color(.045f,.16f,.20f),new Vector3(width*.3f,belt-.055f,front-.481f),new Vector3(.31f,.16f,.008f));
            for(int i=0;i<3;i++) Box(new Color(.32f,.66f,.70f),new Vector3(width*.3f,belt-.02f-i*.038f,front-.487f),new Vector3(.21f-i*.035f,.01f,.004f));
            Flush(Root,"Cabin, A-pillars, dashboard and hood");
            wheel=new GameObject("Driver steering wheel").transform; wheel.SetParent(Root,false);
            wheel.localPosition=new Vector3(Eye.x,wheelY,wheelZ);
            Ring(rubber,Vector3.zero,.20f,.023f,40);
            for(int i=0;i<3;i++) {
                float angle=(i*120+30)*Mathf.Deg2Rad;
                Beam(metal,Vector3.zero,new Vector3(Mathf.Cos(angle)*.18f,Mathf.Sin(angle)*.18f,0),.038f,.022f);
            }
            Box(trim,new Vector3(0,0,-.006f),new Vector3(.12f,.085f,.045f));
            Box(new Color(.8f,.20f,.09f),new Vector3(0,.20f,-.012f),new Vector3(.025f,.037f,.036f));
            Flush(wheel,"Wheel rim and spokes");
            Root.gameObject.SetActive(false);
        }
        void Box(Color color,Vector3 center,Vector3 size,Quaternion? rotation=null)
        {
            var shape=new RaptorModel.Geometry(); shape.Box(center,size,rotation??Quaternion.identity); Append(shape,color);
        }
        void Beam(Color color,Vector3 a,Vector3 b,float width,float depth) => Box(color,(a+b)*.5f,new Vector3(width,depth,Vector3.Distance(a,b)),Quaternion.LookRotation(b-a));
        void Ring(Color color,Vector3 center,float radius,float thickness,int segments)
        {
            for(int i=0;i<segments;i++) {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Beam(color,center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),center+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0),thickness,thickness);
            }
        }
        void Append(RaptorModel.Geometry shape,Color color)
        {
            int offset=vertices.Count; vertices.AddRange(shape.vertices); normals.AddRange(shape.normals);
            foreach(int index in shape.triangles) triangles.Add(index+offset);
            for(int i=0;i<shape.vertices.Count;i++) { colors.Add(QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color); properties.Add(new Vector4(.1f,.3f,0,0)); }
        }
        void Flush(Transform parent,string name)
        {
            var mesh=new Mesh { name=name }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetUVs(0,properties); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds(); resources.Items.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.layer=InteriorLayer;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material; renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            vertices.Clear(); normals.Clear(); triangles.Clear(); colors.Clear(); properties.Clear();
        }
        public void SetVisible(bool visible,float steering)
        {
            Root.gameObject.SetActive(visible);
            if(visible) wheel.localRotation=Quaternion.Euler(12,0,0)*Quaternion.AngleAxis(-steering,Vector3.forward);
        }
    }
}
