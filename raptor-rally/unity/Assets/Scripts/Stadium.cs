using System.Collections.Generic;
using UnityEngine;

namespace RaptorRally
{
    public sealed class Stadium
    {
        public const int Samples = 384;
        public const int GateCount = 48;
        public const float HalfWidth = 5.2f;
        public readonly Vector3[] Points = new Vector3[Samples];
        public readonly List<Barrier> Barriers = new List<Barrier>();
        public readonly struct Barrier
        {
            public readonly Collider Collider;
            public readonly Vector3 Center, Outward;
            public Barrier(Collider collider,Vector3 center,Vector3 outward)
            { Collider=collider; Center=center; Outward=outward; }
        }
        public static readonly int[] JumpCrests={25,182,269};
        public readonly Transform Root;
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        public readonly Color Sand = new Color(.64f, .38f, .18f);
        public readonly Color Dark = new Color(.055f, .095f, .12f);
        TextMesh raceBoard;
        public string BoardText => raceBoard.text;

        public Stadium(Transform parent)
        {
            Root = new GameObject("Coyote Basin • original course").transform;
            Root.SetParent(parent);
            BuildCenterline();
            BuildTrack();
            BuildScenery();
        }

        void BuildCenterline()
        {
            // Original four-lane stadium layout, sampled uniformly by distance.
            var path=new List<Vector3> { new Vector3(-26,0,-32),new Vector3(36,0,-32) };
            AddArc(path,36,-21,11,-90,90);
            path.Add(new Vector3(-26,0,-10)); AddArc(path,-26,1,11,-90,-270);
            path.Add(new Vector3(30,0,12)); AddArc(path,30,23,11,-90,90);
            path.Add(new Vector3(-43,0,34)); AddArc(path,-43,23,11,90,180);
            path.Add(new Vector3(-54,0,-21)); AddArc(path,-43,-21,11,180,270);
            path.Add(path[0]);
            float length=0; var distances=new List<float>{0};
            for(int i=1;i<path.Count;i++) { length+=Vector3.Distance(path[i-1],path[i]); distances.Add(length); }
            int segment=1;
            for(int i=0;i<Samples;i++)
            {
                float distance=length*i/Samples;
                while(segment<path.Count-1 && distances[segment]<distance) segment++;
                Points[i]=Vector3.Lerp(path[segment-1],path[segment],(distance-distances[segment-1])/(distances[segment]-distances[segment-1]));
                Points[i].y=Height(i);
            }
        }
        static void AddArc(List<Vector3> path,float x,float z,float radius,float from,float to)
        {
            int steps=Mathf.CeilToInt(Mathf.Abs(to-from)/3);
            for(int i=1;i<=steps;i++)
            {
                float angle=Mathf.Lerp(from,to,(float)i/steps)*Mathf.Deg2Rad;
                path.Add(new Vector3(x+Mathf.Cos(angle)*radius,0,z+Mathf.Sin(angle)*radius));
            }
        }

        public Vector3 Tangent(int index)
        {
            Vector3 d = Points[Wrap(index + 1)] - Points[Wrap(index - 1)];
            d.y = 0; return d.normalized;
        }
        public Vector3 Side(int index) => Vector3.Cross(Vector3.up, Tangent(index));
        public static int Wrap(int i) => (i % Samples + Samples) % Samples;
        public Vector3 Gate(int gate) => Points[Wrap(gate * Samples / GateCount)];
        public int Nearest(Vector3 p)
        {
            int best = 0; float distance = float.MaxValue;
            for (int i = 0; i < Samples; i++)
            {
                Vector3 d = p - Points[i]; d.y = 0;
                if (d.sqrMagnitude < distance) { best = i; distance = d.sqrMagnitude; }
            }
            return best;
        }
        public float DistanceFromCourse(Vector3 position)
        {
            int near=Nearest(position); float best=float.MaxValue; position.y=0;
            for(int offset=-1;offset<=0;offset++)
            {
                Vector3 a=Points[Wrap(near+offset)],b=Points[Wrap(near+offset+1)]; a.y=b.y=0;
                Vector3 d=b-a;
                Vector3 point=a+d*Mathf.Clamp01(Vector3.Dot(position-a,d)/d.sqrMagnitude);
                best=Mathf.Min(best,Vector3.Distance(position,point));
            }
            return best;
        }
        static float Height(int index)
        {
            float t = (float)index / Samples;
            // Crests leave a straight landing zone even at nitro speed.
            return Jump(t,25f/Samples,.024f,.026f,1.1f)+Jump(t,182f/Samples,.026f,.027f,1.25f)
                +Jump(t,269f/Samples,.025f,.027f,1.05f)+Jump(t,.28f,.018f,.018f,.55f);
        }
        static float Jump(float t, float peak, float rise, float fall, float height)
        {
            if (t < peak - rise || t > peak + fall) return 0;
            return height * (t < peak ? Mathf.SmoothStep(0,1,(t-peak+rise)/rise) : Mathf.SmoothStep(1,0,(t-peak)/fall));
        }
        public Material Mat(Color color)
        {
            if (!materials.TryGetValue(color, out Material mat))
            {
                mat = new Material(Shader.Find("Standard")) { color = color };
                mat.SetFloat("_Glossiness", .12f); materials[color] = mat;
            }
            return mat;
        }
        public GameObject Box(string name, Vector3 position, Vector3 scale, Color color, bool collider = false, Transform parent = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent == null ? Root : parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            go.layer = 9;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        void BuildTrack()
        {
            Box("Stadium plinth", new Vector3(0, -2.35f, 5), new Vector3(132, 4, 102), new Color(.27f,.16f,.085f), true);
            var ground=Box("Infield dirt", new Vector3(0, -.15f, 5), new Vector3(130, .3f, 100), Sand, true);
            var groundDirt=DirtMaterial(); groundDirt.color=new Color(.84f,.77f,.66f);
            groundDirt.mainTextureScale=new Vector2(9,9); ground.GetComponent<Renderer>().sharedMaterial=groundDirt;
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv=new List<Vector2>();
            for (int i = 0; i <= Samples; i++)
            {
                Vector3 p = Points[i % Samples], side = Side(i % Samples);
                vertices.Add(p - side * HalfWidth); vertices.Add(p + side * HalfWidth);
                uv.Add(new Vector2(0,i/32f)); uv.Add(new Vector2(1,i/32f));
                if (i == Samples) continue;
                int v = i * 2;
                triangles.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
            }
            Mesh mesh = new Mesh { name = "Closed dirt ribbon", vertices = vertices.ToArray(), triangles = triangles.ToArray(), uv=uv.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var road = new GameObject("Driveable dirt / jumps", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            road.transform.SetParent(Root); road.layer = 9;
            road.GetComponent<MeshCollider>().sharedMesh = mesh;
            var visibleRoad=Object.Instantiate(mesh); visibleRoad.name="Dirt ribbon visible surface";
            for(int i=0;i<vertices.Count;i++) vertices[i]+=Vector3.up*.02f;
            visibleRoad.SetVertices(vertices); road.GetComponent<MeshFilter>().sharedMesh=visibleRoad;
            road.GetComponent<MeshRenderer>().sharedMaterial = DirtMaterial();
            BuildRecoveryShoulders();
            BuildRacingLine();
            for (int i = 0; i < Samples; i++)
            {
                foreach (int sign in new[] { -1, 1 })
                {
                    Vector3 p = Points[i] + Side(i) * HalfWidth * sign;
                    Vector3 q = Points[Wrap(i + 1)] + Side(Wrap(i + 1)) * HalfWidth * sign;
                    Color c = (i / 3) % 2 == 0 ? new Color(.84f, .88f, .82f) : new Color(.66f, .095f, .055f);
                    var wall = Box("Safety barrier", (p + q) / 2 + Vector3.up * .65f, new Vector3(.55f, 1.3f, Vector3.Distance(p, q) + .1f), c, true);
                    wall.transform.rotation = Quaternion.LookRotation(q - p);
                    wall.layer=11; // Walls must never count as drivable ground in suspension raycasts.
                    Barriers.Add(new Barrier(wall.GetComponent<Collider>(),(p+q)*.5f,((Side(i)+Side(i+1))*.5f*sign).normalized));
                }
            }
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 10; col++)
                    Box("Start / finish check", Gate(0) + Vector3.up * .03f + Tangent(0) * (row * .6f) + Side(0) * (col - 4.5f), new Vector3(.6f, .04f, 1), (row + col) % 2 == 0 ? Color.white : Dark);
            // Visual chevrons at the first approach make the race direction clear.
            for (int i = 4; i < 10; i += 2)
            {
                var mark = Box("Direction marker", Points[i] + Vector3.up * .035f, new Vector3(.45f, .04f, 2.5f), new Color(.95f, .76f, .31f));
                mark.transform.rotation = Quaternion.LookRotation(Tangent(i)) * Quaternion.Euler(0, 35, 0);
            }
        }
        void BuildRecoveryShoulders()
        {
            // A drivable taper lets a truck return beside a raised jump as well as on flat dirt.
            // The infield and flat track share y=0: even a tiny vertical mesh lip traps a box chassis.
            foreach(int sign in new[]{-1,1})
            {
                var vertices=new List<Vector3>(); var triangles=new List<int>(); var uv=new List<Vector2>();
                for(int i=0;i<=Samples;i++)
                {
                    int sample=i%Samples; Vector3 edge=Points[sample]+Side(sample)*HalfWidth*sign;
                    Vector3 outside=Points[sample]+Side(sample)*(HalfWidth+3.4f)*sign; outside.y=0;
                    vertices.Add(sign==1?edge:outside); vertices.Add(sign==1?outside:edge);
                    uv.Add(new Vector2(0,i/32f)); uv.Add(new Vector2(1,i/32f));
                    if(i<Samples) { int v=i*2; triangles.AddRange(new[]{v,v+2,v+1,v+1,v+2,v+3}); }
                }
                var mesh=new Mesh { name="Sloped recovery verge",vertices=vertices.ToArray(),triangles=triangles.ToArray(),uv=uv.ToArray() };
                mesh.RecalculateNormals();
                var go=new GameObject("Drivable recovery shoulder",typeof(MeshFilter),typeof(MeshCollider));
                go.transform.SetParent(Root,false); go.layer=9;
                go.GetComponent<MeshCollider>().sharedMesh=mesh;
                var visible=Object.Instantiate(mesh); visible.name="Recovery verge surface";
                for(int i=0;i<vertices.Count;i++) vertices[i]+=Vector3.up*.012f;
                visible.SetVertices(vertices); go.GetComponent<MeshFilter>().sharedMesh=visible;
                // Render with the same dirt as the circuit; outside the walls it reads as a graded bank.
                go.AddComponent<MeshRenderer>().sharedMaterial=DirtMaterial();
            }
        }
        void BuildRacingLine()
        {
            foreach(int peak in JumpCrests)
            {
                // Crest markers sit on the sampled road and make both jumps readable.
                for(int offset=-1;offset<=1;offset++)
                {
                    int i=peak+offset;
                    var crest=Box("Jump crest",Points[i]+Vector3.up*.035f,new Vector3(8.8f,.025f,.22f),new Color(.97f,.73f,.38f));
                    crest.transform.rotation=Quaternion.LookRotation(Points[i+1]-Points[i-1]);
                }
            }
            // Wooden facing follows the existing first ramp. Collision remains smooth.
            for(int i=18;i<=23;i++)
            {
                var log=GameObject.CreatePrimitive(PrimitiveType.Cylinder); log.name="Timber ramp facing";
                log.transform.SetParent(Root,false); log.transform.position=Points[i]+Vector3.up*.025f;
                log.transform.rotation=Quaternion.FromToRotation(Vector3.up,Side(i));
                log.transform.localScale=new Vector3(.2f,HalfWidth-.55f,.2f);
                log.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.35f,.22f,.105f));
                Object.DestroyImmediate(log.GetComponent<Collider>());
            }
        }

        Material DirtMaterial()
        {
            var texture=new Texture2D(256,512,TextureFormat.RGB24,false) { name="Original procedural stadium dirt",wrapMode=TextureWrapMode.Repeat };
            var pixels=new Color[256*512]; var random=new System.Random(931);
            for(int y=0;y<512;y++) for(int x=0;x<256;x++)
            {
                float u=x/255f,v=y/512f;
                float broad=Mathf.PerlinNoise(u*7,v*14);
                float grain=(float)random.NextDouble()-.5f;
                float center=.5f+.07f*Mathf.Sin(v*Mathf.PI*6);
                float packed=Mathf.Exp(-Mathf.Pow((u-center)/.32f,4));
                float rut=Mathf.Pow(Mathf.Sin((u+.013f*Mathf.Sin(v*40))*95),14)*packed;
                Color color=Color.Lerp(new Color(.77f,.56f,.29f),new Color(.61f,.40f,.19f),packed*.55f);
                pixels[y*256+x]=color*(.88f+broad*.24f+grain*.13f-rut*.055f);
            }
            texture.SetPixels(pixels); texture.Apply(false,true);
            var material=new Material(Mat(Color.white)); material.name="Warm textured dirt";
            material.mainTexture=texture; return material;
        }

        TextMesh Sign(string name, string text, Vector3 position, float size, Color color)
        {
            var go=new GameObject(name,typeof(TextMesh)); go.transform.SetParent(Root,false);
            go.transform.localPosition=position;
            var label=go.GetComponent<TextMesh>(); label.text=text; label.fontSize=64;
            label.characterSize=size; label.anchor=TextAnchor.MiddleCenter;
            label.alignment=TextAlignment.Center; label.color=color;
            return label;
        }

        public void UpdateBoard(RallyGame.Phase state, bool paused, int lap, string leader)
        {
            string status=paused?"PAUSED":state==RallyGame.Phase.Garage?"READY TO RACE":
                state==RallyGame.Phase.Countdown?"GET READY":state==RallyGame.Phase.Results?"FINISH":"LAP "+lap+" / 3";
            string value="COYOTE BASIN\n"+status+"\n"+(state==RallyGame.Phase.Garage?"RAPTOR RALLY":"P1  "+leader);
            if(raceBoard.text!=value) raceBoard.text=value;
        }
        void BuildScenery()
        {
            var random = new System.Random(47);
            for (int block = -1; block <= 1; block++)
            {
                Box("Grandstand foundation",new Vector3(block*31,-.15f,50),new Vector3(28,.3f,9),Dark);
                Box("Teal concourse fascia",new Vector3(block*31,3.4f,54),new Vector3(28,.7f,.4f),new Color(.10f,.44f,.48f));
                for (int row = 0; row < 6; row++)
                {
                    Box("Grandstand tier", new Vector3(block * 31, row * .35f, 45 + row * 1.45f), new Vector3(27, row*.7f+.5f, 1.5f),new Color(.24f,.31f,.32f));
                    for (int seat = 0; seat < 22; seat++)
                    {
                        if(seat==10 || seat==11) continue; // central access aisle
                        Color c = Color.Lerp(new Color(.13f, .43f, .47f), new Color(.76f, .57f, .34f), (float)random.NextDouble());
                        Box("Crowd", new Vector3(block * 31 - 12.5f + seat * 1.16f, row * .7f + .55f, 45 + row * 1.45f), new Vector3(.6f, .65f, .55f), c);
                    }
                }
                Box("Grandstand front fascia",new Vector3(block*31,.3f,44.2f),new Vector3(27,1,.3f),new Color(.08f,.32f,.36f));
                Sign("Stand banner",block==0?"COYOTE BASIN":"ALL DIRT / ALL DAY",new Vector3(block*31,.5f,44),.12f,new Color(.82f,.85f,.68f));
            }
            foreach (int x in new[] { -63, 63 })
                foreach (int z in new[] { -40, 43 })
                {
                    Box("Floodlight mast", new Vector3(x, 6, z), new Vector3(.35f, 12, .35f), Dark);
                    Box("Floodlight bank", new Vector3(x, 12, z), new Vector3(3.8f, 1.3f, .6f), Dark);
                    for(int lamp=0;lamp<4;lamp++)
                        Box("Lamp",new Vector3(x-1.4f+lamp*.92f,12,z-.33f),new Vector3(.65f,.8f,.12f),new Color(1,.89f,.65f));
                    Box("Mast base",new Vector3(x,.5f,z),new Vector3(1.2f,1,1.2f),new Color(.31f,.36f,.35f));
                }
            // Compact service compound outside the switchback lanes.
            Box("Pit apron",new Vector3(56,-.025f,7),new Vector3(12,.03f,23),new Color(.43f,.40f,.32f));
            Box("Pit canopy",new Vector3(56,2.8f,3),new Vector3(8,.4f,6),new Color(.10f,.36f,.40f));
            foreach(int x in new[]{53,59}) foreach(int z in new[]{1,5})
                Box("Canopy post",new Vector3(x,1.3f,z),new Vector3(.16f,2.6f,.16f),Dark);
            Box("Service trailer",new Vector3(57,1.4f,14),new Vector3(3,2.6f,6),new Color(.73f,.73f,.60f));
            Box("Trailer stripe",new Vector3(55.48f,1.5f,14),new Vector3(.06f,.5f,6),new Color(.12f,.44f,.48f));
            for(int i=0;i<3;i++)
                Box("Pit tire stack",new Vector3(52,.55f,8+i*1.4f),new Vector3(.95f,1.1f,.95f),Dark);
            foreach(Vector3 p in new[]{new Vector3(35,0,-21),new Vector3(-25,0,1),new Vector3(29,0,23)})
            {
                Box("Hairpin flagpole",p+Vector3.up*1.65f,new Vector3(.12f,3.3f,.12f),Dark);
                Box("Blue course flag",p+new Vector3(.7f,2.8f,0),new Vector3(1.4f,.7f,.07f),new Color(.08f,.30f,.85f));
            }
            for(int i=0;i<10;i++)
            {
                float x=-43+(float)random.NextDouble()*3,z=-7+(float)random.NextDouble()*26;
                var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere); rock.name="Infield rock";
                rock.transform.SetParent(Root,false); rock.transform.position=new Vector3(x,.15f,z);
                rock.transform.localScale=new Vector3(1.4f,.6f,1); rock.GetComponent<Renderer>().sharedMaterial=Mat(Sand*.8f);
                Object.DestroyImmediate(rock.GetComponent<Collider>());
            }
            // A rear scoreboard gives the venue a landmark without covering the track.
            foreach(int x in new[]{-7,7}) Box("Scoreboard support",new Vector3(x,5.3f,53),new Vector3(.65f,10.6f,.65f),Dark);
            Box("Scoreboard housing",new Vector3(0,9.5f,52),new Vector3(21,7,.8f),Dark);
            Box("Scoreboard trim",new Vector3(0,13,51.9f),new Vector3(21,.3f,1),new Color(.60f,.80f,.31f));
            raceBoard=Sign("Live race board","COYOTE BASIN\nREADY TO RACE\nRAPTOR RALLY",new Vector3(0,9.5f,51.5f),.22f,new Color(.83f,.94f,.63f));
            foreach(int side in new[]{-1,1})
            {
                Vector3 p=Gate(0)+Side(0)*(HalfWidth+1.2f)*side;
                Box("Finish pylon",p+Vector3.up*2,new Vector3(.5f,4,.5f),Dark);
                for(int row=0;row<4;row++)
                    Box("Finish flag",p+new Vector3(.6f,2.4f+row*.4f,0),new Vector3(.8f,.4f,.15f),row%2==0?Color.white:Dark);
            }
            var sign = new GameObject("Coyote Basin sign", typeof(TextMesh)); sign.transform.SetParent(Root);
            sign.transform.position = new Vector3(-10, .04f, 1); sign.transform.rotation = Quaternion.Euler(90, 0, 0);
            var label = sign.GetComponent<TextMesh>(); label.text = "COYOTE\nBASIN"; label.fontSize = 70; label.characterSize = .17f;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = new Color(.85f, .71f, .48f);
        }
    }
}
