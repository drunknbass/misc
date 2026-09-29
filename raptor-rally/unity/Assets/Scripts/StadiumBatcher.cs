using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaptorRally
{
    // Bake color into static vertices so hundreds of differently colored props can
    // share a draw call. Spatial chunks retain useful culling in the follow camera.
    public static class StadiumBatcher
    {
        public static void Combine(Transform root)
        {
            var groups=new Dictionary<Vector2Int,List<CombineInstance>>();
            var staged=new List<Mesh>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null || filter.sharedMesh==null || renderer.sharedMaterials.Length!=1) continue;
                var material=renderer.sharedMaterial;
                if(material==null || material.mainTexture!=null || material.shader.name!="Standard") continue;
                Vector3 center=root.InverseTransformPoint(renderer.bounds.center);
                var cell=new Vector2Int(Mathf.FloorToInt(center.x/24),Mathf.FloorToInt(center.z/24));
                if(!groups.TryGetValue(cell,out var group)) groups[cell]=group=new List<CombineInstance>();
                var mesh=Object.Instantiate(filter.sharedMesh);
                Color color=QualitySettings.activeColorSpace==ColorSpace.Linear?material.color.linear:material.color;
                var colors=new Color[mesh.vertexCount];
                for(int i=0;i<colors.Length;i++) colors[i]=color;
                mesh.colors=colors; staged.Add(mesh);
                group.Add(new CombineInstance { mesh=mesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix });
                // The collider and its identity remain intact for one-way recovery.
                Object.DestroyImmediate(renderer); Object.DestroyImmediate(filter);
            }
            var shared=new Material(Resources.Load<Shader>("StadiumVertexColor")) { name="Batched stadium colors" };
            foreach(var pair in groups)
            {
                var mesh=new Mesh { name="Stadium chunk "+pair.Key,indexFormat=IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(),true,true); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(root,false); go.GetComponent<MeshFilter>().sharedMesh=mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial=shared;
            }
            foreach(var mesh in staged) Object.DestroyImmediate(mesh);
            Debug.Log("RAPTOR STADIUM BATCHING: "+staged.Count+" static renderers -> "+groups.Count+" colored chunks; colliders preserved.");
        }
    }
}
