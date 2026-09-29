using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RaptorRally;

public static class PrototypeBuilder
{
    const string ScenePath="Assets/Scenes/CoyoteBasin.unity";
    static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
    public static void Preview()
    {
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        if(EditorWindow.focusedWindow != null) EditorWindow.focusedWindow.maximized=true;
        EditorApplication.EnterPlaymode();
    }
    [MenuItem("Raptor Rally/Create or reset prototype scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Raptor Rally").AddComponent<RallyGame>();
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        PlayerSettings.companyName="RaptorRallyPrototype";
        PlayerSettings.productName="Raptor Rally";
        PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Raptor Rally/Verify race simulation")]
    public static void Verify()
    {
        ConfigureWheelTexture();
        CreateScene();
        var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>(); game.Initialize(); game.Verification=true;
        Physics.simulationMode=SimulationMode.Script;
        var report=new System.Text.StringBuilder("# Prototype validation\n\nUnity "+Application.unityVersion+"\n\n");
        try
        {
            for(int vehicle=0;vehicle<3;vehicle++)
            {
                game.Selected=vehicle; game.StartRace(); game.Player.Autopilot=true;
                foreach(var truck in game.Trucks) truck.Body.interpolation=RigidbodyInterpolation.None;
                Physics.SyncTransforms();
                for(int step=0;step<12000 && game.Trucks.Any(t=>!t.Finished);step++)
                { game.Tick(.02f); Physics.Simulate(.02f); }
                if(game.Trucks.Any(t=>!t.Finished))
                    throw new Exception("Incomplete race: "+string.Join(", ",game.Trucks.Select(t=>t.Driver+" laps="+t.CompletedLaps+" gate="+t.NextGate+" recoveries="+t.RecoveryCount+" at "+t.Body.position)));
                if(game.Trucks.Any(t=>t.FinishTime<20 || t.CompletedLaps!=3)) throw new Exception("Invalid finish accounting");
                // The switchback circuit is 452 m, roughly twice the original oval.
                if(game.Player.FinishTime > 180 || game.Player.MaxAirHeight > 8 || game.Player.RecoveryCount > 2) throw new Exception("Unstable handling: time="+game.Player.FinishTime+", peak="+game.Player.MaxAirHeight+", recoveries="+game.Player.RecoveryCount);
                if(game.Trucks.Any(t=>t.FinishTime>180 || t.RecoveryCount>2 || t.MaxAirHeight>8))
                    throw new Exception("Unstable opponent: "+string.Join(", ",game.Trucks.Select(t=>t.Driver+" time="+t.FinishTime+" recoveries="+t.RecoveryCount)));
                game.RefreshBoard();
                if(!game.Track.BoardText.Contains("FINISH") || !game.Track.BoardText.Contains("P1  "+game.Standings()[0].Driver))
                    throw new Exception("Scoreboard finish/leader differs from race standings");
                report.AppendLine("- PASS: "+TruckSpec.Lineup[vehicle].Name+" / all four trucks finished three laps. Player AI time: "+RallyGame.Clock(game.Player.FinishTime)+". Peak height: "+game.Player.MaxAirHeight.ToString("F2")+" m. Recoveries: "+game.Player.RecoveryCount+".");
            }
            game.StartRace(); game.Countdown=0; game.Tick(.02f); Physics.SyncTransforms();
            var p=game.Player; p.CrossedStart=true; p.NextGate=1;
            p.Body.position=game.Track.Gate(0); p.Body.linearVelocity=game.Track.Tangent(0)*8;
            p.CheckGate(10);
            if(p.CompletedLaps!=0 || p.NextGate!=1) throw new Exception("Out of order gate advanced lap");
            p.Body.position=game.Track.Gate(1); p.Body.linearVelocity=-game.Track.Tangent(Stadium.Samples/Stadium.GateCount)*8;
            p.CheckGate(11); if(p.NextGate!=1) throw new Exception("Reverse gate counted");
            int gate=p.NextGate; p.Recover(); if(p.NextGate!=gate || p.CompletedLaps!=0) throw new Exception("Recovery awarded progress");
            game.Paused=true; float before=game.RaceTime; game.Tick(3); if(game.RaceTime!=before) throw new Exception("Paused race advanced");
            report.AppendLine("- PASS: out-of-order gate rejection, reverse gate rejection, reset preserving progress, pause clock.");
            float[] speeds=new float[2];
            for(int trial=0;trial<2;trial++)
            {
                game.StartRace(); game.Countdown=0; game.Tick(.02f);
                p=game.Player; p.Body.interpolation=RigidbodyInterpolation.None;
                p.Body.position=game.Track.Gate(0)+Vector3.up*.92f;
                p.Throttle=1; p.Boost=trial==1;
                foreach(var other in game.Trucks) if(other!=p) other.gameObject.SetActive(false);
                Physics.SyncTransforms();
                for(int i=0;i<50;i++) { p.Tick(.02f,true); Physics.Simulate(.02f); }
                speeds[trial]=p.Speed;
                if(trial==1 && p.Nitro>=.99f) throw new Exception("Nitro did not consume charge");
            }
            if(speeds[0]<5 || speeds[1]<speeds[0]+2) throw new Exception("Throttle or boost did not accelerate correctly");
            p.Boost=false; p.Recover(); p.Throttle=-1; Physics.SyncTransforms();
            for(int i=0;i<50;i++) { p.Tick(.02f,true); Physics.Simulate(.02f); }
            if(Vector3.Dot(p.Body.linearVelocity,p.Body.rotation*Vector3.forward)>-1) throw new Exception("Reverse did not engage");
            report.AppendLine("- PASS: throttle acceleration, nitro increasing acceleration/consuming charge, reverse driving.");
            game.Garage(); game.RefreshBoard();
            if(!game.Track.BoardText.Contains("READY TO RACE")) throw new Exception("Scoreboard did not reset in garage");
            game.StartRace(); game.RefreshBoard();
            if(!game.Track.BoardText.Contains("GET READY")) throw new Exception("Scoreboard countdown mismatch");
            game.Countdown=0; game.Tick(.02f); game.Player.CompletedLaps=1; game.RefreshBoard();
            if(!game.Track.BoardText.Contains("LAP 2 / 3")) throw new Exception("Scoreboard lap mismatch");
            game.Paused=true; game.RefreshBoard();
            if(!game.Track.BoardText.Contains("PAUSED")) throw new Exception("Scoreboard pause mismatch");
            report.AppendLine("- PASS: scoreboard garage, countdown, lap, pause, finish and leader agree with race state.");
            game.Paused=false; game.Player.Steer=1; game.AdvanceWheel(1);
            if(game.WheelAngle!=135) throw new Exception("Wheel right-turn direction or limit incorrect");
            game.Paused=true; game.Player.Steer=-1; game.AdvanceWheel(1);
            if(game.WheelAngle!=135) throw new Exception("Paused wheel moved");
            game.Paused=false; game.AdvanceWheel(1);
            if(game.WheelAngle!=-135) throw new Exception("Wheel left-turn direction incorrect");
            game.Player.Steer=0; game.AdvanceWheel(1);
            if(game.WheelAngle!=0) throw new Exception("Wheel failed to center");
            report.AppendLine("- PASS: steering wheel turns left/right, respects its limit and pause, and returns to center.");
            foreach(float scale in new[]{.75f,1f,2.093333f}) foreach(float angle in new[]{-135f,-90f,0f,90f,135f})
            {
                var canvas=Matrix4x4.TRS(new Vector3(125,38,0),Quaternion.identity,Vector3.one*scale);
                var center=new Vector3(1470,747,0);
                var rotated=SteeringWheelHud.RotationMatrix(canvas,center,angle);
                if(Vector3.Distance(canvas.MultiplyPoint3x4(center),rotated.MultiplyPoint3x4(center))>.002f)
                    throw new Exception("Steering wheel pivot drifts under HUD scaling");
                float radius=Vector3.Distance(rotated.MultiplyPoint3x4(center+Vector3.up*80),canvas.MultiplyPoint3x4(center));
                if(Mathf.Abs(radius-80*scale)>.002f) throw new Exception("Steering wheel radius changed during rotation");
            }
            report.AppendLine("- PASS: steering wheel center and radius stay fixed at five angles across three HUD scales with letterboxing.");
            game.FollowPlayer=true; game.UpdateCamera(.02f,true);
            if(game.View.orthographicSize>25) throw new Exception("Follow camera did not zoom in");
            Vector3 framed=game.View.WorldToViewportPoint(game.Player.transform.position);
            if(framed.z<=0 || framed.x<.1f || framed.x>.9f || framed.y<.1f || framed.y>.9f) throw new Exception("Follow camera lost the player");
            Vector3 cameraBefore=game.View.transform.position, movement=new Vector3(12,0,-9);
            game.Player.transform.position+=movement; game.UpdateCamera(.02f,true);
            if(Vector3.Distance(game.View.transform.position-cameraBefore,movement)>.01f) throw new Exception("Follow camera did not track player movement");
            game.FollowPlayer=false; game.UpdateCamera(.02f,true);
            if(Vector3.Distance(game.View.transform.position,new Vector3(19,84,-100))>.01f || game.View.orthographicSize<50) throw new Exception("Overview camera failed to restore");
            report.AppendLine("- PASS: follow camera zooms, frames and tracks the player; switching back restores the complete stadium view.");
            VerifyRecoveryAndJumps(game,report);
            VerifyBarrierIndex(game,report);
            var existing=game.Player;
            game.Player.Nitro=0; game.Player.FinishTime=42; game.Player.CompletedLaps=3;
            game.Garage(); game.StartRace();
            if(game.Player!=existing || game.Player.Nitro!=1 || game.Player.Finished || game.Player.CompletedLaps!=0 || game.Player.Speed>.001f)
                throw new Exception("Reusable grid retained race state or rebuilt its truck models");
            report.AppendLine("- PASS: retry reuses the four truck models and resets charge, velocity, laps, finish state and pose.");
            bool mac=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneOSX);
            bool web=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL);
            report.AppendLine("\nBuild modules: Mac="+mac+", Web="+web+".");
            report.AppendLine("\nThese are automated physics/AI checks. Human driving feel, gamepad, wheel input and browser behavior are separate validation tasks.");
            File.WriteAllText(Path.Combine(Output,"VALIDATION.md"),report.ToString());
            Debug.Log("RAPTOR VERIFICATION PASSED\n"+report);
        }
        catch(Exception e)
        {
            report.AppendLine("\nFAIL: "+e.Message); File.WriteAllText(Path.Combine(Output,"VALIDATION.md"),report.ToString()); throw;
        }
        finally
        {
            Physics.simulationMode=SimulationMode.FixedUpdate;
            // Restore the saved bootstrap-only scene; generated geometry is runtime-owned.
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
    static void VerifyRecoveryAndJumps(RallyGame game,System.Text.StringBuilder report)
    {
        for(int vehicle=0;vehicle<3;vehicle++)
        {
            game.Selected=vehicle; game.StartRace(); game.Countdown=0; game.Tick(.02f);
            foreach(var other in game.Trucks) if(other!=game.Player) other.gameObject.SetActive(false);
            var truck=game.Player; truck.Autopilot=false; truck.Body.interpolation=RigidbodyInterpolation.None;
            foreach(int recoveryIndex in new[]{8,25,182,269})
            {
            Vector3 origin=game.Track.Points[recoveryIndex],side=game.Track.Side(recoveryIndex);
            foreach(int sign in new[]{-1,1})
            {
                var wall=game.Track.Barriers[recoveryIndex*2+(sign==1?1:0)];
                var collider=truck.GetComponent<Collider>();
                truck.Body.position=origin+side*sign*(Stadium.HalfWidth+4); truck.Body.position=new Vector3(truck.Body.position.x,.92f,truck.Body.position.z);
                truck.Body.rotation=Quaternion.LookRotation(-side*sign); truck.Body.linearVelocity=Vector3.zero;
                truck.Throttle=1; truck.Steer=0; truck.Boost=false; truck.transform.SetPositionAndRotation(truck.Body.position,truck.Body.rotation); truck.Body.angularVelocity=Vector3.zero;
                truck.UpdateBarrierRecovery(true); Physics.SyncTransforms();
                if(!Physics.GetIgnoreCollision(collider,wall.Collider)) throw new Exception("Outside barrier did not open for "+truck.Spec.Name);
                if(Physics.GetIgnoreCollision(game.Trucks[1].GetComponent<Collider>(),wall.Collider)) throw new Exception("Re-entry changed another truck's collision pair");
                int gate=truck.NextGate,lap=truck.CompletedLaps;
                for(int step=0;step<150;step++)
                {
                    truck.Tick(.02f,true); Physics.Simulate(.02f);
                    if(Vector3.Dot(truck.Body.position-origin,side)*sign<1) break;
                }
                truck.UpdateBarrierRecovery();
                float across=Vector3.Dot(truck.Body.position-origin,side)*sign;
                if(across>1.2f || across< -2 || truck.OffCourse) throw new Exception("Truck failed physical re-entry: "+truck.Spec.Name+" side="+sign+" across="+across+" position="+truck.Body.position+" velocity="+truck.Body.linearVelocity+" grounded="+truck.Grounded+" throttle="+truck.Throttle+" rotation="+truck.Body.rotation.eulerAngles);
                if(Physics.GetIgnoreCollision(collider,wall.Collider)) throw new Exception("Barrier stayed open after truck fully cleared it");
                if(truck.NextGate!=gate || truck.CompletedLaps!=lap) throw new Exception("Re-entry awarded race progress");
                truck.Body.position=origin+side*sign*(Stadium.HalfWidth-3.5f)+Vector3.up*.92f;
                truck.Body.rotation=Quaternion.LookRotation(side*sign); truck.Body.linearVelocity=side*sign*9;
                truck.transform.SetPositionAndRotation(truck.Body.position,truck.Body.rotation); truck.Body.angularVelocity=Vector3.zero;
                truck.UpdateBarrierRecovery(true); Physics.SyncTransforms();
                for(int step=0;step<100;step++) { truck.Tick(.02f,true); Physics.Simulate(.02f); }
                if(Vector3.Dot(truck.Body.position-origin,side)*sign>Stadium.HalfWidth-.3f) throw new Exception("On-track truck drove out through a barrier");
            }
            }
            foreach(int crest in Stadium.JumpCrests)
            {
                int approach=crest-14;
                truck.Body.position=game.Track.Points[approach]+Vector3.up*.92f;
                truck.Body.rotation=Quaternion.LookRotation(game.Track.Tangent(approach));
                truck.Body.linearVelocity=game.Track.Tangent(approach)*truck.Spec.TopSpeed*1.3f;
                truck.Throttle=1; truck.Steer=0; truck.Boost=true; truck.Nitro=1; truck.MaxAirHeight=0;
                truck.transform.SetPositionAndRotation(truck.Body.position,truck.Body.rotation); truck.Body.angularVelocity=Vector3.zero;
                truck.UpdateBarrierRecovery(true); Physics.SyncTransforms();
                for(int step=0;step<95;step++)
                {
                    truck.Tick(.02f,true); Physics.Simulate(.02f);
                    if(game.Track.DistanceFromCourse(truck.Body.position)>Stadium.HalfWidth-.4f)
                        throw new Exception("Boosted ramp launch left its lane: "+truck.Spec.Name+" crest="+crest);
                }
                if(truck.MaxAirHeight>4) throw new Exception("Ramp launch remains excessive");
            }
            truck.NextGate=0; truck.CrossedStart=false;
            truck.Body.position=game.Track.Gate(0)+game.Track.Side(0)*(Stadium.HalfWidth+.8f)+Vector3.up*.92f;
            truck.Body.linearVelocity=game.Track.Tangent(0)*8; truck.CheckGate(10);
            if(truck.CrossedStart || truck.NextGate!=0) throw new Exception("Off-course gate crossing counted");
            truck.Recover();
            if(truck.OffCourse || truck.NextGate!=0) throw new Exception("Manual recovery failed to reset collision state safely");
        }
        report.AppendLine("- PASS: all three trucks physically re-enter through inner and outer barriers on flat dirt and beside every raised jump; barriers re-enable after clearance and still block outward driving. Collision changes stay per truck.");
        report.AppendLine("- PASS: all three trucks cross every jump at full nitro speed within the lane; off-course gate crossing and manual recovery preserve progress.");
    }
    static void VerifyBarrierIndex(RallyGame game,System.Text.StringBuilder report)
    {
        var truck=game.Player; var collider=truck.GetComponent<BoxCollider>();
        var expected=new bool[game.Track.Barriers.Count];
        int maxCandidates=0;
        for(int step=0;step<240;step++)
        {
            int index=Stadium.Wrap(step*7);
            Vector3 position=game.Track.Points[index]+game.Track.Side(index)*(Mathf.Sin(step*.41f)*10)+Vector3.up*.92f;
            Quaternion rotation=Quaternion.LookRotation(game.Track.Tangent(index));
            truck.Body.position=position; truck.Body.rotation=rotation;
            truck.transform.SetPositionAndRotation(position,rotation);
            bool reset=step==0 || step%41==0;
            if(step==82) { truck.gameObject.SetActive(false); truck.gameObject.SetActive(true); }
            truck.UpdateBarrierRecovery(reset); maxCandidates=Mathf.Max(maxCandidates,truck.LastBarrierChecks);
            for(int i=0;i<expected.Length;i++)
            {
                var wall=game.Track.Barriers[i]; Vector3 delta=position-wall.Center; delta.y=0;
                float signed=Vector3.Dot(delta,wall.Outward);
                float clearance=Mathf.Abs(Vector3.Dot(rotation*Vector3.right,wall.Outward))*truck.Spec.Width*.5f
                    +Mathf.Abs(Vector3.Dot(rotation*Vector3.forward,wall.Outward))*truck.Spec.Length*.5f+.45f;
                expected[i]=delta.sqrMagnitude<100 && (signed>0 || (!reset && expected[i] && signed>-clearance));
                if(Physics.GetIgnoreCollision(collider,wall.Collider)!=expected[i]) throw new Exception("Spatial recovery diverged at step "+step+" wall "+i);
            }
        }
        if(maxCandidates>=game.Track.Barriers.Count/2) throw new Exception("Barrier index did not sufficiently bound collision work");
        int renderers=game.Track.Root.GetComponentsInChildren<MeshRenderer>().Length;
        if(renderers>80 || game.Track.Barriers.Count!=Stadium.Samples*2) throw new Exception("Static batching changed collider count or failed to reduce renderers");
        report.AppendLine("- PASS: spatial recovery matches exhaustive collision rules across 240 positions, cell boundaries and teleports; at most "+maxCandidates+" candidates instead of 768 per truck. Stadium has "+renderers+" mesh renderers with all 768 barriers retained.");
    }
    static void ConfigureWheelTexture()
    {
        const string path="Assets/Resources/HUD/RaptorSteeringWheel.png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null) throw new Exception("Missing wheel image importer");
        importer.textureType=TextureImporterType.Default;
        importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
        importer.isReadable=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=1024; importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;
        importer.SaveAndReimport();
        if(Resources.Load<Texture2D>("HUD/RaptorSteeringWheel")==null) throw new Exception("Wheel asset is not available to player builds");
    }
    [MenuItem("Raptor Rally/Build Mac")]
    public static void BuildMac() => Build(BuildTarget.StandaloneOSX,"Mac/Raptor Rally.app");
    [MenuItem("Raptor Rally/Build Browser")]
    public static void BuildWeb()
    {
        // GitHub Pages cannot provide custom compression/isolation headers.
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.threadsSupport=false;
        PlayerSettings.WebGL.wasm2023=false;
        PlayerSettings.WebGL.nameFilesAsHashes=true;
        PlayerSettings.WebGL.template="PROJECT:RaptorPages";
        PlayerSettings.defaultWebScreenWidth=1600;
        PlayerSettings.defaultWebScreenHeight=900;
        AssetDatabase.SaveAssets();
        Build(BuildTarget.WebGL,"Web");
    }
    static void Build(BuildTarget target,string relative)
    {
        if(!File.Exists(ScenePath)) CreateScene();
        // Procedural materials have no scene references. Keep the opaque Standard
        // shader in players through a material asset so Shader.Find works there.
        const string surfacePath="Assets/Resources/RacingSurface.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(surfacePath)==null)
        {
            var shader=Shader.Find("Standard");
            if(shader==null) throw new Exception("Built-in Standard shader is unavailable");
            AssetDatabase.CreateAsset(new Material(shader) { name="Racing Surface" },surfacePath);
        }
        AssetDatabase.SaveAssets();
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath}, locationPathName=Path.Combine(Output,relative), target=target, options=BuildOptions.None });
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed: "+result.summary.result);
    }
}
