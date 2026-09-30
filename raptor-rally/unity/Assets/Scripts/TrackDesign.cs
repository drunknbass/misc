using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaptorRally
{
    [Serializable]
    public sealed class TrackDesign
    {
        public int version=1;
        public string name="My dirt circuit";
        public int[] cells;
        public int[] pieces;
        public const int Columns=7, Rows=5;
        public const float CellSize=16;
        public static bool Adjacent(int a,int b) => Mathf.Abs(a%Columns-b%Columns)+Mathf.Abs(a/Columns-b/Columns)==1;
        public bool Straight(int i) => cells[(i+cells.Length-1)%cells.Length]+cells[(i+1)%cells.Length]==2*cells[i];
        public const float CrossoverHeight=3.8f;
        public bool HasCrossings => cells!=null && new HashSet<int>(cells).Count!=cells.Length;
        public int[] CrossoverRoles()
        {
            var roles=new int[cells.Length];
            for(int i=0;i<cells.Length;i++) {
                if(System.Array.IndexOf(cells,cells[i])==System.Array.LastIndexOf(cells,cells[i])) continue;
                if(Mathf.Abs(cells[(i+1)%cells.Length]-cells[i])!=1) continue;
                roles[(i+cells.Length-1)%cells.Length]=1;roles[i]=2;roles[(i+1)%cells.Length]=3;
            }
            return roles;
        }
        public string Validate()
        {
            if(version!=1 && version!=2) return "This course uses an unsupported file version.";
            int limit=version==1?34:48;
            if(cells==null || cells.Length<8 || cells.Length>limit) return "Draw a loop of 8 to "+limit+" connected tiles.";
            if(pieces==null || pieces.Length!=cells.Length) return "Each track tile needs a piece.";
            var visits=new Dictionary<int,List<int>>();
            for(int i=0;i<cells.Length;i++) {
                if(cells[i]<0 || cells[i]>=Columns*Rows) return "Keep the circuit on the grid.";
                if(!visits.TryGetValue(cells[i],out var indices)) visits[cells[i]]=indices=new List<int>();
                indices.Add(i);
                if(!Adjacent(cells[i],cells[(i+1)%cells.Length])) return "Connect every tile, including the last tile back to the start.";
                if(pieces[i]<0 || pieces[i]>5) return "Unknown track piece.";
                if(pieces[i]!=0 && !Straight(i)) return "Place jumps and surface pieces on straight tiles.";
            }
            var reserved=new HashSet<int>();
            foreach(var pair in visits) {
                if(pair.Value.Count==1) continue;
                if(version==1 || pair.Value.Count!=2) return "A crossover can carry two perpendicular straights only.";
                int a=pair.Value[0],b=pair.Value[1];
                if(!Straight(a)||!Straight(b)||Mathf.Abs(cells[(a+1)%cells.Length]-cells[a])==Mathf.Abs(cells[(b+1)%cells.Length]-cells[b]))
                    return "Cross straight through a junction at right angles; do not turn or retrace there.";
                foreach(int center in pair.Value) for(int offset=-1;offset<=1;offset++) {
                    int i=(center+offset+cells.Length)%cells.Length;
                    if(!Straight(i)||pieces[i]!=0) return "Keep each crossover and its approaches straight and flat. The jump is added automatically.";
                    if(!reserved.Add(i) || offset!=0 && visits[cells[i]].Count!=1) return "Space crossover junctions farther apart.";
                }
            }
            if(!Straight(0) || !Straight(cells.Length-1) || pieces[0]!=0 || pieces[pieces.Length-1]!=0 || reserved.Contains(0)||reserved.Contains(cells.Length-1))
                return "The start needs two flat straight tiles away from crossover ramps.";
            return null;
        }
        public static TrackDesign Parse(string json)
        {
            if(string.IsNullOrEmpty(json) || json.Length>8192) throw new ArgumentException("Course file is empty or too large.");
            TrackDesign design;
            try { design=JsonUtility.FromJson<TrackDesign>(json); } catch { throw new ArgumentException("Could not read this course file."); }
            string error=design==null?"Missing course.":design.Validate();
            if(error!=null) throw new ArgumentException(error);
            design.name=System.Text.RegularExpressions.Regex.Replace(design.name??"",@"[^\p{L}\p{N} _-]","").Trim();
            if(design.name.Length>28) design.name=design.name.Substring(0,28);
            if(design.name.Length==0) design.name="My dirt circuit";
            return design;
        }
        public static Vector3 Center(int cell) => new Vector3((cell%Columns-3)*CellSize,0,(2-cell/Columns)*CellSize+5);
        public static float Elevation(int piece,float t)
        {
            if(piece==1) return Mathf.Pow(Mathf.Sin(Mathf.PI*t),2)*1.8f;
            if(piece==2) return 1.65f*(t<.3f?Mathf.SmoothStep(0,1,t/.3f):t>.65f?Mathf.SmoothStep(1,0,(t-.65f)/.35f):1);
            if(piece==3) return Mathf.Pow(Mathf.Sin(3*Mathf.PI*t),2)*.35f;
            return 0;
        }
        public void Sample(Vector3[] result,int[] surfaces,bool[] elevated=null,bool[] gaps=null)
        {
            var path=new List<Vector3>(); var kinds=new List<int>(); var heights=new List<bool>(); var lengths=new List<float>();
            var roles=CrossoverRoles();
            float length=0;
            for(int i=0;i<cells.Length;i++) {
                Vector3 c=Center(cells[i]),incoming=(c-Center(cells[(i+cells.Length-1)%cells.Length])).normalized,outgoing=(Center(cells[(i+1)%cells.Length])-c).normalized;
                Vector3 start=c-incoming*8,end=c+outgoing*8,arcCenter=start+outgoing*8;
                for(int j=0;j<16;j++) {
                    float t=j/16f;
                    Vector3 p=Straight(i)?Vector3.Lerp(start,end,t):arcCenter+Vector3.Slerp(start-arcCenter,end-arcCenter,t);
                    if(Straight(i)) p.y=Elevation(pieces[i],t);
                    if(roles[i]==1) p.y=3.2f*Mathf.Pow(t,1.5f);
                    if(roles[i]==2) p.y=t<.125f?Mathf.Lerp(3.2f,CrossoverHeight,t/.125f):t>.875f?Mathf.Lerp(1.2f,1,(t-.875f)/.125f):Mathf.Lerp(CrossoverHeight,1.2f,(t-.125f)/.75f)+Mathf.Sin(Mathf.PI*(t-.125f)/.75f)*1.2f;
                    if(roles[i]==3) p.y=Mathf.SmoothStep(1,0,t);
                    if(path.Count>0) length+=Vector3.Distance(new Vector3(p.x,0,p.z),new Vector3(path[path.Count-1].x,0,path[path.Count-1].z));
                    path.Add(p); kinds.Add(roles[i]>0?6:pieces[i]); heights.Add(roles[i]>0); lengths.Add(length);
                }
            }
            length+=Vector3.Distance(new Vector3(path[0].x,0,path[0].z),new Vector3(path[path.Count-1].x,0,path[path.Count-1].z));
            path.Add(path[0]); kinds.Add(kinds[0]); heights.Add(heights[0]); lengths.Add(length);
            int segment=1;
            for(int i=0;i<result.Length;i++) {
                float d=length*i/result.Length;
                while(segment<path.Count-1 && lengths[segment]<d) segment++;
                result[i]=Vector3.Lerp(path[segment-1],path[segment],(d-lengths[segment-1])/(lengths[segment]-lengths[segment-1]));
                surfaces[i]=kinds[segment-1];
                if(elevated!=null) elevated[i]=heights[segment-1]||heights[segment];
                if(gaps!=null) {
                    gaps[i]=false;
                    for(int tile=0;tile<roles.Length;tile++) if(roles[tile]==2 && kinds[segment-1]==6) {
                        Vector3 delta=result[i]-Center(cells[tile]);
                        if(Mathf.Abs(delta.x)<5.8f && Mathf.Abs(delta.z)<.1f) gaps[i]=true;
                    }
                }
            }
        }
    }
}
