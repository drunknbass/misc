using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaptorRally
{
    public sealed class RallyGame : MonoBehaviour
    {
        public enum Phase { Garage, Countdown, Racing, Results }
        public Phase State = Phase.Garage;
        [NonSerialized] public Stadium Track;
        public readonly List<RaptorTruck> Trucks = new List<RaptorTruck>();
        readonly List<RaptorTruck> rankedTrucks=new List<RaptorTruck>(4);
        static readonly Comparison<RaptorTruck> CompareTrucks=(a,b)=>a.Finished && b.Finished ? a.FinishTime.CompareTo(b.FinishTime) : a.Finished ? -1 : b.Finished ? 1 : b.Progress.CompareTo(a.Progress);
        public RaptorTruck Player => Trucks[0];
        public int Selected;
        public float RaceTime, Countdown;
        public bool Paused;
        public bool Verification;
        public Camera View;
        Camera garageBackground;
        RenderTexture circuitPreview;
        bool circuitDirty=true;
        float finishFlash;
        float[] best = new float[3];
        readonly Color ink = new Color(.025f,.055f,.078f,.97f);
        readonly Color muted = new Color(.57f,.68f,.71f);
        readonly Color accent = new Color(.64f,.90f,.35f);
        GUIStyle label, heading, small, number, button;
        bool initialized;
        int preparedLineup=-1;
        SteeringWheelHud wheel;
        GaragePreview garagePreview;
        public RiveRaceHud MotionHud { get; private set; }
        public bool ReducedMotion;
        public bool FollowPlayer;
        public const float WheelLockDegrees = 2.5f * 360f;
        public float WheelAngle { get; private set; }
        public bool TouchControls { get; private set; }
        int touchInput;
        float nextTouchState;
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void RaptorTouchState(int phase,int paused,int selected,int rank,int lap,float time,int speed,float nitro,float countdown,int offCourse,int following,int reducedMotion);
#endif
        public void SetTouchControls(float enabled)
        {
            TouchControls=enabled!=0; ClearTouchInput(); nextTouchState=0;
            if(TouchControls) FollowPlayer=true;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput=!TouchControls;
#endif
        }
        public void SetTouchInput(float value) { touchInput=TouchControls?Mathf.RoundToInt(value)&31:0; if(touchInput==0) ClearTouchInput(); }
        void ClearTouchInput()
        {
            touchInput=0;
            if(Trucks.Count>0 && !Player.Autopilot) { Player.Throttle=Player.Steer=0; Player.Boost=false; }
        }
        public void TouchAction(string action)
        {
            if(!TouchControls) return;
            if(action=="camera" && State!=Phase.Garage) FollowPlayer=!FollowPlayer;
            else if(action=="recover" && State==Phase.Racing && !Paused) Player.Recover();
            else if(action=="pause" && State!=Phase.Garage) { ClearTouchInput(); Paused=!Paused; Time.timeScale=Paused?0:1; }
            else if(action=="resume") { ClearTouchInput(); Paused=false; Time.timeScale=1; }
            else if(action=="garage") Garage();
            else if(action=="start" && (State==Phase.Garage || State==Phase.Results)) StartRace();
            else if(action=="motion") ReducedMotion=!ReducedMotion;
            else if(State==Phase.Garage && action.StartsWith("truck") && int.TryParse(action.Substring(5),out int index) && index>=0 && index<3) { Selected=index; PrepareGrid(); }
            nextTouchState=0;
        }
        void PublishTouchState()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if(!TouchControls || Time.unscaledTime<nextTouchState) return;
            nextTouchState=Time.unscaledTime+.1f;
            RaptorTouchState((int)State,Paused?1:0,Selected,Standings().IndexOf(Player)+1,Mathf.Min(3,Player.CompletedLaps+1),Player.Finished?Player.FinishTime:RaceTime,Mathf.RoundToInt(Player.Speed*2.23694f),Player.Nitro,Countdown,Player.OffCourse?1:0,FollowPlayer?1:0,ReducedMotion?1:0);
#endif
        }

        void Awake() { Initialize(); }
        public void Initialize()
        {
            if (initialized) return; initialized = true;
            Application.targetFrameRate = 60;
            gameObject.AddComponent<RallyLighting>().Configure();
            Track = new Stadium(transform);
            wheel=new SteeringWheelHud();
            if(Application.isPlaying) { garagePreview=new GaragePreview(transform); MotionHud=new RiveRaceHud(); }
            var cameraObject = new GameObject("Stadium camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform); View = cameraObject.GetComponent<Camera>();
            View.allowHDR=false;
            View.orthographic = true; View.orthographicSize = 49;
            View.backgroundColor = new Color(.035f,.065f,.09f); View.clearFlags = CameraClearFlags.SolidColor;
            View.transform.position = new Vector3(19, 84, -100); View.transform.LookAt(new Vector3(0, 0, 1));
            View.nearClipPlane = .3f; View.farClipPlane = 300;
            var background=new GameObject("Garage backdrop",typeof(Camera)); background.transform.SetParent(transform,false);
            garageBackground=background.GetComponent<Camera>(); garageBackground.depth=-10; garageBackground.cullingMask=0;
            garageBackground.clearFlags=CameraClearFlags.SolidColor; garageBackground.backgroundColor=new Color(.025f,.04f,.05f);
            var light = new GameObject("Late afternoon sun", typeof(Light)); light.transform.SetParent(transform);
            light.transform.rotation = Quaternion.Euler(48, -35, 0);
            var sun = light.GetComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.08f; sun.color=new Color(1,.91f,.78f); sun.cullingMask=~(1<<10);
            sun.shadows = LightShadows.Soft; sun.shadowCustomResolution=2048; sun.shadowStrength = .88f; sun.shadowBias=.025f; sun.shadowNormalBias=.16f;
            if(Application.isPlaying) { var dust=new GameObject("Shared tire dust"); dust.transform.SetParent(transform,false); dust.layer=12; dust.AddComponent<RallyDust>().Initialize(this); }
            for (int i = 0; i < best.Length; i++) best[i] = PlayerPrefs.GetFloat("coyote-basin-v3-" + i, 0);
            PrepareGrid();
        }
        public void PrepareGrid()
        {
            circuitDirty=true;
            WheelAngle=0;
            MotionHud?.Reset();
            if(preparedLineup==Selected && Trucks.Count==4)
            {
                for(int i=0;i<Trucks.Count;i++) Trucks[i].ResetForRace(i);
                return;
            }
            preparedLineup=Selected;
            foreach (var truck in Trucks) { truck.gameObject.SetActive(false); if (Application.isPlaying) Destroy(truck.gameObject); else DestroyImmediate(truck.gameObject); }
            Trucks.Clear();
            string[] names = { "YOU", "DUST DEVIL", "ROCKHOPPER", "SUNDOWN" };
            Color[] colors = { TruckSpec.Lineup[Selected].Color, new Color(.9f,.36f,.18f), new Color(.76f,.8f,.7f), new Color(.59f,.40f,.76f) };
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject(names[i]); go.transform.SetParent(transform);
                var truck = go.AddComponent<RaptorTruck>(); truck.Setup(Track, i == 0 ? Selected : (Selected + i) % 3, names[i], i == 0, colors[i]);
                truck.Spawn(i); truck.Body.isKinematic = true; Trucks.Add(truck);
            }
        }
        public void StartRace()
        {
            ClearTouchInput(); nextTouchState=0;
            Time.timeScale = 1; Paused = false; PrepareGrid(); RaceTime = 0; Countdown = 3; finishFlash = 0; State = Phase.Countdown;
        }
        public void Garage()
        {
            ClearTouchInput(); nextTouchState=0;
            Paused = false; Time.timeScale = 1; State = Phase.Garage; PrepareGrid();
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.C) && State!=Phase.Garage) FollowPlayer=!FollowPlayer;
            if(Input.GetKeyDown(KeyCode.M)) ReducedMotion=!ReducedMotion;
            if (Input.GetKeyDown(KeyCode.Escape) && State != Phase.Garage) { Paused = !Paused; Time.timeScale = Paused ? 0 : 1; }
            if (Paused) return;
            if (State == Phase.Garage)
            {
                if (Input.GetKeyDown(KeyCode.Return)) StartRace();
                for (int i = 0; i < 3; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { Selected = i; PrepareGrid(); }
            }
            else if (State == Phase.Racing && !Player.Autopilot)
            {
                Player.Throttle = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || (touchInput&4)!=0 ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || (touchInput&8)!=0 ? 1 : 0);
                Player.Steer = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || (touchInput&2)!=0 ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || (touchInput&1)!=0 ? 1 : 0);
                Player.Boost = Input.GetKey(KeyCode.Space) || (touchInput&16)!=0;
                if (Input.GetKeyDown(KeyCode.R)) Player.Recover();
            }
        }
        void FixedUpdate() { Tick(Time.fixedDeltaTime); }
        public void Tick(float dt)
        {
            if (Paused || State == Phase.Garage) return;
            if (State == Phase.Countdown)
            {
                Countdown -= dt;
                if (Countdown <= 0) { State = Phase.Racing; foreach (var t in Trucks) t.Body.isKinematic = false; }
                return;
            }
            RaceTime += dt; finishFlash = Mathf.Max(0, finishFlash - dt);
            foreach (var t in Trucks) { t.Tick(dt, true); t.CheckGate(RaceTime); }
            if (State == Phase.Racing && Player.Finished)
            {
                State = Phase.Results; finishFlash = 1.8f;
                if (!Verification && (best[Selected] <= 0 || Player.FinishTime < best[Selected]))
                { best[Selected] = Player.FinishTime; PlayerPrefs.SetFloat("coyote-basin-v3-" + Selected, best[Selected]); PlayerPrefs.Save(); }
            }
        }
        public List<RaptorTruck> Standings()
        {
            rankedTrucks.Clear(); rankedTrucks.AddRange(Trucks); rankedTrucks.Sort(CompareTrucks);
            return rankedTrucks;
        }
        public static string Clock(float t) => string.Format("{0:00}:{1:00.00}", (int)t / 60, t % 60);
        void LateUpdate()
        {
            RefreshBoard();
            AdvanceWheel(Time.deltaTime);
            UpdateCamera(Time.unscaledDeltaTime);
            if(!TouchControls) MotionHud?.Update(this,Time.deltaTime);
            PublishTouchState();
            if(State==Phase.Garage) {
                garagePreview?.Render(Selected);
                if(!TouchControls && (circuitDirty || circuitPreview==null || !circuitPreview.IsCreated())) RenderCircuitPreview();
            }
        }
        public void UpdateCamera(float dt,bool snap=false)
        {
            float aspect = Mathf.Max(.5f, (float)Screen.width / Screen.height);
            bool tracking=FollowPlayer && State!=Phase.Garage && Trucks.Count>0;
            garageBackground.enabled=State==Phase.Garage;
            View.enabled=State!=Phase.Garage;
            if(State==Phase.Garage) {
                float s=Mathf.Min(Screen.width/1600f,Screen.height/900f),x=(Screen.width-1600*s)*.5f,y=(Screen.height-900*s)*.5f;
                View.rect=new Rect((x+1110*s)/Screen.width,(Screen.height-y-735*s)/Screen.height,465*s/Screen.width,485*s/Screen.height);
            } else View.rect=new Rect(0,0,1,1);
            Vector3 target=tracking?Player.transform.position+Player.transform.forward*2.5f+Vector3.up*.5f:new Vector3(0,0,1);
            Vector3 position=tracking?target+new Vector3(10,20,-25):new Vector3(19,84,-100);
            float size=tracking?(TouchControls?Mathf.Max(8.5f,8/aspect):Mathf.Max(10.5f,12/aspect)):State==Phase.Garage?73:Mathf.Max(58,76/aspect);
            float blend=snap?1:1-Mathf.Exp(-Mathf.Max(0,dt)*7);
            View.transform.position=Vector3.Lerp(View.transform.position,position,blend);
            View.transform.rotation=Quaternion.Slerp(View.transform.rotation,Quaternion.LookRotation(target-position),blend);
            View.orthographicSize=Mathf.Lerp(View.orthographicSize,size,blend);
        }
        void RenderCircuitPreview()
        {
            if(circuitPreview==null) circuitPreview=new RenderTexture(512,534,24,RenderTextureFormat.ARGB32) { name="Cached circuit preview",antiAliasing=2 };
            UpdateCamera(0,true);
            Rect rect=View.rect; int mask=View.cullingMask; View.cullingMask=mask & ~(1<<12);
            View.rect=new Rect(0,0,1,1); View.targetTexture=circuitPreview;
            View.Render(); View.targetTexture=null; View.rect=rect; View.cullingMask=mask;
            circuitDirty=false;
        }
        public void AdvanceWheel(float dt)
        {
            if(Paused || Trucks.Count==0) return;
            float target=State==Phase.Racing?Mathf.Clamp(Player.Steer,-1,1)*WheelLockDegrees:0;
            // Keep the angle unwrapped so reversals travel through all five turns.
            WheelAngle=Mathf.MoveTowards(WheelAngle,target,540*Mathf.Max(0,dt));
        }
        public void RefreshBoard()
        {
            if(Trucks.Count==0) return;
            var leader=Standings()[0];
            Track.UpdateBoard(State,Paused,Mathf.Min(3,Player.CompletedLaps+1),leader.Driver);
        }
        void Style()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            label.normal.textColor = new Color(.93f,.95f,.89f);
            heading = new GUIStyle(label) { fontSize = 36 };
            number = new GUIStyle(label) { fontSize = 52 };
            small = new GUIStyle(label) { fontSize = 14, fontStyle = FontStyle.Normal }; small.normal.textColor = muted;
            button = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        }
        void Panel(Rect r, Color c) { Color old = GUI.color; GUI.color = c.linear; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void Text(float x, float y, float w, float h, string s, GUIStyle style, Color? color = null)
        {
            Color old = style.normal.textColor; if (color.HasValue) style.normal.textColor = color.Value;
            GUI.Label(new Rect(x,y,w,h), s, style); style.normal.textColor = old;
        }
        bool Button(Rect r, string text, bool primary = false)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Panel(r, primary ? (hover ? Color.white : accent) : (hover ? new Color(.19f,.31f,.35f) : new Color(.10f,.20f,.24f)));
            Text(r.x, r.y, r.width, r.height, text, button, primary ? ink : Color.white);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }
        void OnGUI()
        {
            if (!initialized || Trucks.Count == 0) return;
            if(TouchControls) {
                GUI.matrix=Matrix4x4.identity;
                if(State==Phase.Garage && garagePreview!=null) {
                    bool portrait=Screen.height>Screen.width;
                    var rect=portrait?new Rect(0,88,Screen.width,Mathf.Max(80,Screen.height-330)):new Rect(0,0,Screen.width*.56f,Screen.height);
                    GUI.DrawTexture(rect,garagePreview.Texture,ScaleMode.ScaleToFit,false);
                }
                return;
            }
            Style(); float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1600 * scale) / 2, (Screen.height - 900 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            bool riveHud=State!=Phase.Garage && MotionHud!=null && MotionHud.Ready;
            if(riveHud) MotionHud.Draw();
            else {
            Panel(new Rect(0,0,1600,89), ink);
            Panel(new Rect(28,24,5,40), accent);
            Text(48,13,420,41,"RAPTOR / RALLY",heading);
            Text(50,54,420,22,"COYOTE BASIN     /     STADIUM CIRCUIT",small);
            }
            if (State == Phase.Garage)
            {
                Text(1170,22,395,42,"01 COURSE    /    03 RAPTORS",label,muted);
                Panel(new Rect(28,119,363,613),ink);
                Text(50,140,320,25,"CHOOSE YOUR RAPTOR",label);
                Text(50,173,320,22,"FORD PERFORMANCE  /  RAPTOR LINEUP",small);
                for (int i = 0; i < 3; i++)
                {
                    float y = 216 + i * 110;
                    Panel(new Rect(48,y,324,92), Selected == i ? new Color(.14f,.31f,.35f) : new Color(.07f,.13f,.17f));
                    Panel(new Rect(48,y,5,92),TruckSpec.Lineup[i].Color);
                    Text(66,y+9,295,30,TruckSpec.Lineup[i].Name,label);
                    Text(66,y+43,295,34,TruckSpec.Lineup[i].Description,small);
                    if (GUI.Button(new Rect(48,y,324,92), GUIContent.none, GUIStyle.none)) { Selected = i; PrepareGrid(); }
                }
                Text(50,555,310,30,"4 TRUCKS   /   3 LAPS   /   NITRO",small,accent);
                Text(50,587,310,28,best[Selected] > 0 ? "LOCAL BEST   " + Clock(best[Selected]) : "LOCAL BEST   —",label);
                if (Button(new Rect(48,641,324,61),"START RACE  →",true)) StartRace();
                Panel(new Rect(420,132,675,604),ink);
                Text(444,151,620,28,"YOUR RAPTOR",small,accent);
                if(garagePreview!=null) GUI.DrawTexture(new Rect(428,190,660,462),garagePreview.Texture,ScaleMode.ScaleToFit,false);
                Text(447,649,620,43,TruckSpec.Lineup[Selected].Name,heading);
                Text(447,698,620,24,Selected==1?"RAPTOR FLARES  /  AMBER DRLs  /  37-INCH TIRES":Selected==0?"RAPTOR HOOD  /  FORD TAILGATE  /  35-INCH TIRES":"RAPTOR FENDERS  /  VENTED HOOD  /  33-INCH TIRES",small);
                if(circuitPreview!=null) GUI.DrawTexture(new Rect(1110,250,465,485),circuitPreview,ScaleMode.StretchToFill,false);
                Text(1120,156,450,34,"COYOTE BASIN",label);
                Text(1120,195,450,24,"4 TRUCKS  /  3 LAPS  /  SWITCHBACKS",small);
                Text(425,777,1100,30,"THREE RAPTORS. ALL DIRT.",heading);
                Text(427,817,1100,26,"C switches between the full stadium and a close view of your truck.",small);
            }
            else
            {
                int rank = Standings().IndexOf(Player) + 1;
                if(!riveHud) {
                Text(585,15,180,24,"POSITION",small); Text(585,37,175,40,rank+" / 4",heading,accent);
                Text(785,15,160,24,"LAP",small); Text(785,37,160,40,Mathf.Min(3,Player.CompletedLaps+1)+" / 3",heading);
                Text(955,15,235,24,"RACE TIME",small); Text(955,37,235,40,Clock(Player.Finished?Player.FinishTime:RaceTime),heading);
                Text(1220,15,220,24,"NITRO",small);
                for (int i=0;i<10;i++) Panel(new Rect(1220+i*24,47,19,20),Player.Nitro >= (i+.5f)/10 ? accent : new Color(.15f,.23f,.23f));
                Panel(new Rect(28,738,252,102),ink);
                Text(46,749,220,22,Player.Grounded ? (Player.Boosting ? "NITRO ENGAGED" : "PACKED DIRT") : "AIRBORNE",small,accent);
                Text(45,773,150,61,Mathf.RoundToInt(Player.Speed*2.23694f).ToString("00"),number);
                Text(146,802,100,28,"MPH",small);
                Panel(new Rect(296,790,285,43),ink);
                Text(310,794,260,36,Player.Spec.Name,label);
                if(Player.OffCourse) Text(560,135,510,42,"OFF COURSE — drive through a barrier to rejoin",small,accent);
                }
                if (Button(new Rect(1490,24,78,46),Paused?"▶":"II")) { Paused=!Paused; Time.timeScale=Paused?0:1; }
                wheel.Draw(WheelAngle,State==Phase.Results);
                if (State == Phase.Countdown || (State==Phase.Racing && RaceTime<.8f))
                {
                    Panel(new Rect(679,350,242,157),ink);
                    var big = new GUIStyle(number) { fontSize=90, alignment=TextAnchor.MiddleCenter };
                    Text(679,350,242,157,State==Phase.Countdown?Mathf.CeilToInt(Countdown).ToString():"GO!",big,accent);
                }
                if (State==Phase.Results)
                {
                    Panel(new Rect(1118,125,454,542),ink);
                    Text(1143,144,400,29,"RACE COMPLETE",small,accent);
                    Text(1143,179,400,51,rank+Ordinal(rank)+" PLACE",number);
                    Text(1143,239,400,37,Clock(Player.FinishTime),heading);
                    var standings=Standings();
                    for(int i=0;i<standings.Count;i++)
                    {
                        var t=standings[i]; float y=307+i*52;
                        Text(1143,y,230,34,(i+1).ToString("00")+"   "+t.Driver,label,t.IsPlayer?accent:Color.white);
                        Text(1402,y,146,34,t.Finished?Clock(t.FinishTime):"RACING "+Mathf.Min(3,t.CompletedLaps+1)+"/3",small);
                    }
                    if(Button(new Rect(1143,533,403,51),"RACE AGAIN",true)) StartRace();
                    if(Button(new Rect(1143,596,403,43),"CHANGE RAPTOR")) Garage();
                    if(finishFlash>0) Text(480,190,640,95,"FINISH!",new GUIStyle(number){fontSize=90},accent);
                }
            }
            Panel(new Rect(0,860,1600,40),ink);
            Text(30,864,1110,30,"WASD / ARROWS  Drive      SPACE  Nitro      R  Recover      ESC  Pause",small);
            if(Button(new Rect(1110,864,215,29),ReducedMotion?"M  MOTION OFF":"M  MOTION ON")) ReducedMotion=!ReducedMotion;
            if(State!=Phase.Garage && Button(new Rect(1340,864,228,29),FollowPlayer?"C  WHOLE TRACK":"C  FOLLOW TRUCK")) FollowPlayer=!FollowPlayer;
            if(Paused)
            {
                Panel(new Rect(0,89,1600,771),new Color(0,0,0,.62f)); Panel(new Rect(575,260,450,310),ink);
                Text(617,290,370,65,"PAUSED",number);
                if(Button(new Rect(617,381,366,61),"RESUME",true)) { Paused=false; Time.timeScale=1; }
                if(Button(new Rect(617,463,366,61),"BACK TO GARAGE")) Garage();
            }
            GUI.matrix=Matrix4x4.identity;
        }
        static string Ordinal(int n) => n==1?"ST":n==2?"ND":n==3?"RD":"TH";
        void OnDestroy() { Time.timeScale=1; wheel?.Dispose(); garagePreview?.Dispose(); MotionHud?.Dispose(); if(circuitPreview!=null) { circuitPreview.Release(); Destroy(circuitPreview); } }
    }
}
