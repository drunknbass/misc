using UnityEngine;
using UnityEngine.Rendering;
namespace RaptorRally
{
    // Fixed-capacity dust and contact-shadow mesh shared by the entire field.
    public sealed class RallyDust : MonoBehaviour
    {
        const int Capacity=160;
        struct Puff { public Vector3 position,velocity; public float age,life,size; }
        readonly Puff[] puffs=new Puff[Capacity];
        readonly Vector3[] vertices=new Vector3[Capacity*4];
        readonly Color[] colors=new Color[Capacity*4];
        RallyGame game; Mesh mesh; Material material; int next=4; float emission;
        public void Initialize(RallyGame owner)
        {
            game=owner; mesh=new Mesh { name="Pooled tire dust" }; mesh.MarkDynamic();
            var uv=new Vector2[Capacity*4]; var indices=new int[Capacity*6];
            for(int i=0;i<Capacity;i++) {
                int v=i*4,t=i*6; uv[v]=Vector2.zero; uv[v+1]=Vector2.right; uv[v+2]=Vector2.one; uv[v+3]=Vector2.up;
                indices[t]=v; indices[t+1]=v+1; indices[t+2]=v+2; indices[t+3]=v; indices[t+4]=v+2; indices[t+5]=v+3;
            }
            mesh.vertices=vertices; mesh.uv=uv; mesh.colors=colors; mesh.triangles=indices;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            material=new Material(Resources.Load<Shader>("RallyDust"));
            var renderer=gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        }
        void LateUpdate()
        {
            if(game==null || game.Paused) return;
            float dt=Time.deltaTime; emission+=dt;
            if(game.State==RallyGame.Phase.Garage) { for(int i=0;i<Capacity;i++) puffs[i].life=0; }
            else if(emission>=.075f) {
                emission=0;
                foreach(var truck in game.Trucks) if(truck.Grounded && truck.Speed>4) for(int side=-1;side<=1;side+=2) {
                    float speed=Mathf.Clamp01(truck.Speed/22);
                    puffs[next]=new Puff { position=truck.transform.TransformPoint(new Vector3(side*truck.Spec.Width*.42f,-.5f,-truck.Spec.Length*.36f)), velocity=-truck.transform.forward*(.4f+speed)+new Vector3(.2f,.4f,0), age=0,life=.75f+speed*.6f,size=.23f+speed*.45f };
                    next++; if(next>=Capacity) next=4;
                }
            }
            Vector3 right=game.View.transform.right,up=game.View.transform.up;
            for(int i=0;i<Capacity;i++) {
                if(i<4) {
                    int shadowVertex=i*4; bool visible=false;
                    if(game.State!=RallyGame.Phase.Garage && i<game.Trucks.Count) {
                        var truck=game.Trucks[i];
                        if(Physics.Raycast(truck.transform.position+Vector3.up,Vector3.down,out var hit,6,1<<9)) {
                            Vector3 f=Vector3.ProjectOnPlane(truck.transform.forward,hit.normal).normalized*truck.Spec.Length*.58f;
                            Vector3 shadowRight=Vector3.Cross(hit.normal,f.normalized)*truck.Spec.Width*.65f;
                            Vector3 center=hit.point+hit.normal*.035f;
                            vertices[shadowVertex]=center-shadowRight-f; vertices[shadowVertex+1]=center+shadowRight-f; vertices[shadowVertex+2]=center+shadowRight+f; vertices[shadowVertex+3]=center-shadowRight+f;
                            float alpha=Mathf.Clamp01(1-(hit.distance-1.8f)/3)*.52f;
                            for(int k=0;k<4;k++) colors[shadowVertex+k]=new Color(.018f,.012f,.008f,alpha);
                            visible=true;
                        }
                    }
                    if(!visible) for(int k=0;k<4;k++) colors[shadowVertex+k]=Color.clear;
                    continue;
                }
                var p=puffs[i]; p.age+=dt; p.position+=p.velocity*dt; puffs[i]=p;
                float life=p.life>0?Mathf.Clamp01(p.age/p.life):1;
                float size=p.size*(1+life*2.4f); Vector3 r=right*size,u=up*size; int v=i*4;
                vertices[v]=p.position-r-u; vertices[v+1]=p.position+r-u; vertices[v+2]=p.position+r+u; vertices[v+3]=p.position-r+u;
                var c=new Color(.42f,.30f,.18f,(1-life)*.24f);
                for(int k=0;k<4;k++) colors[v+k]=c;
            }
            mesh.vertices=vertices; mesh.colors=colors; mesh.RecalculateBounds();
        }
        void OnDestroy() { if(mesh!=null) Destroy(mesh); if(material!=null) Destroy(material); }
    }
}
