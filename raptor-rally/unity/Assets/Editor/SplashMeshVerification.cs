using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RaptorRally;

public static class SplashMeshVerification
{
    public static void VerifyAndBuild()
    {
        var source=Resources.Load<Texture2D>("Brand/FordPerformance");
        var uv=BrandArtwork.UV(source,BrandArtwork.OvalPixels);
        // Reproduce the previous occupancy rule against the original imported JPEG.
        var old=new bool[104,40]; var outside=new bool[104,40]; var queue=new Queue<Vector2Int>();
        for(int x=0;x<104;x++) for(int y=0;y<40;y++) {
            Color c=source.GetPixelBilinear(uv.x+(x+.5f)/104*uv.width,uv.y+(y+.5f)/40*uv.height);
            old[x,y]=Mathf.Max(c.r,c.g,c.b)>.13f;
            if(!old[x,y] && (x==0 || x==103 || y==0 || y==39)) { outside[x,y]=true; queue.Enqueue(new Vector2Int(x,y)); }
        }
        while(queue.Count>0) {
            var p=queue.Dequeue();
            foreach(var d in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down}) {
                var n=p+d;
                if(n.x<0 || n.x>=104 || n.y<0 || n.y>=40 || old[n.x,n.y] || outside[n.x,n.y]) continue;
                outside[n.x,n.y]=true; queue.Enqueue(n);
            }
        }
        var holes=new List<string>();
        for(int x=0;x<104;x++) for(int y=0;y<40;y++) if(!old[x,y] && !outside[x,y]) holes.Add(x+","+y);
        var mesh=StartupIntro.BuildVoxelMesh(source,out int cells);
        var edges=new Dictionary<string,int>(); var vertices=mesh.vertices; var indices=mesh.triangles;
        Func<Vector3,string> key=v=>Mathf.RoundToInt(v.x*100000)+","+Mathf.RoundToInt(v.y*100000)+","+Mathf.RoundToInt(v.z*100000);
        for(int i=0;i<indices.Length;i+=3) for(int j=0;j<3;j++) {
            string a=key(vertices[indices[i+j]]),b=key(vertices[indices[i+(j+1)%3]]);
            string edge=string.CompareOrdinal(a,b)<0?a+"/"+b:b+"/"+a;
            edges[edge]=edges.TryGetValue(edge,out int count)?count+1:1;
        }
        int badEdges=0; foreach(int count in edges.Values) if(count!=2) badEdges++;
        if(badEdges!=0) throw new Exception("Emblem has "+badEdges+" unsealed or non-manifold edges");
        if(cells<1000 || mesh.bounds.size.z<.4f) throw new Exception("Invalid solid emblem");
        foreach(float time in new[]{4.9f,8f,11.5f}) if(StartupIntro.CardAt(time)!=StartupIntro.Card.Black || StartupIntro.OpacityAt(time)!=0) throw new Exception("Black transition changed");
        string report="Original 104x40 brightness mask: "+holes.Count+" enclosed missing cells: "+string.Join("; ",holes)+"\nNew mesh: "+cells+" occupied cells; "+badEdges+" unsealed/non-manifold edges; depth "+mesh.bounds.size.z+". Black transition timing checks passed.\n";
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../splash-refinement-mesh.txt")),report);
        UnityEngine.Object.DestroyImmediate(mesh);
        Debug.Log("SPLASH MESH VERIFICATION PASSED: "+report);
        PrototypeBuilder.BuildWeb();
    }
}
