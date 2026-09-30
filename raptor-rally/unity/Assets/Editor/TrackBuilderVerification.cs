using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using RaptorRally;

public static class TrackBuilderVerification
{
    const string Preset="{\"version\":1,\"name\":\"Raptor rhythm\",\"cells\":[24,25,26,19,12,11,10,9,8,15,22,23],\"pieces\":[0,1,0,4,0,2,0,5,0,3,0,0]}";
    static void Check(bool condition,string message) { if(!condition) throw new Exception("TRACK BUILDER: "+message); }
    public static void VerifyExtraLayouts()
    {
        const string outer="{\"version\":1,\"name\":\"Outer circuit\",\"cells\":[3,4,5,6,13,20,27,34,33,32,31,30,29,28,21,14,7,0,1,2],\"pieces\":[0,1,0,0,2,4,0,0,0,5,0,0,0,0,0,3,0,0,0,0]}";
        EditorSceneManager.OpenScene("Assets/Scenes/CoyoteBasin.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>();game.Initialize();game.Verification=true;
        game.OpenTrackBuilder();game.BuildCustomTrack(outer);game.Countdown=0;game.Tick(.02f);
        Physics.simulationMode=SimulationMode.Script;
        try {
            foreach(var t in game.Trucks) { t.Autopilot=true;t.Body.interpolation=RigidbodyInterpolation.None; }
            Physics.SyncTransforms();
            for(int step=0;step<12000&&!game.Player.Finished;step++) { game.Tick(.02f);Physics.Simulate(.02f); }
            Check(game.Player.Finished&&game.Player.RecoveryCount==0,"outer circuit could not finish without recovery");
            string result="\n- PASS: alternate 20-tile layout reaching every grid edge: F-150 completed three laps without recovery in "+game.Player.FinishTime.ToString("F2")+"s.\n";
            game.StartRace();game.Countdown=0;game.Tick(.02f);
            foreach(var t in game.Trucks) if(t!=game.Player)t.gameObject.SetActive(false);
            var truck=game.Player;truck.Autopilot=false;
            Func<int,int> find=kind=>Enumerable.Range(0,Stadium.Samples).First(i=>game.Track.SurfaceAt(i)==kind && game.Track.SurfaceAt(Stadium.Wrap(i-5))==kind && game.Track.SurfaceAt(Stadium.Wrap(i+5))==kind);
            Action<int> place=index=>{truck.Body.position=game.Track.Points[index]+Vector3.up*.92f;truck.Body.rotation=Quaternion.LookRotation(game.Track.Tangent(index));truck.transform.SetPositionAndRotation(truck.Body.position,truck.Body.rotation);truck.Body.linearVelocity=game.Track.Tangent(index)*8;truck.Body.angularVelocity=Vector3.zero;truck.Throttle=truck.Steer=0;truck.Boost=false;truck.UpdateBarrierRecovery(true);Physics.SyncTransforms();};
            place(0);truck.Tick(.02f,true);Physics.Simulate(.02f);float flatSpeed=truck.Speed;
            place(find(4));truck.Tick(.02f,true);Physics.Simulate(.02f);Check(truck.Speed<flatSpeed-.1f,"mud did not slow the truck");
            place(find(5));truck.Nitro=.2f;truck.Tick(.5f,true);Check(truck.Nitro>.3f,"pad did not refill nitro");
            result+="- PASS: mud produces additional physical deceleration versus flat dirt; nitro pad refills the truck reservoir.\n";
            File.AppendAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../TRACK-BUILDER-CHECKS.md")),result);
            Debug.Log("EXTRA TRACK LAYOUT VERIFICATION PASSED"+result);
        } finally { Physics.simulationMode=SimulationMode.FixedUpdate; }
    }
    public static void VerifyAndBuild()
    {
        var report=new StringBuilder("# Track builder verification\n\n");
        var design=TrackDesign.Parse(Preset);
        Check(design.Validate()==null,"starter invalid");
        foreach(string invalid in new[]{"{}","null","{bad",Preset.Replace("24,25","24,24"),Preset.Replace("24,25","24,20"),Preset.Replace("0,1,0,4","0,1,1,4"),Preset.Replace("\"version\":1","\"version\":2"),Preset.Replace("0,1,0,4","0,9,0,4")}) {
            bool rejected=false; try { TrackDesign.Parse(invalid); } catch(ArgumentException) { rejected=true; } Check(rejected,"accepted invalid course: "+invalid);
        }
        var points=new Vector3[Stadium.Samples];var surfaces=new int[Stadium.Samples];design.Sample(points,surfaces);
        for(int i=0;i<points.Length;i++) { Check(float.IsFinite(points[i].x)&&float.IsFinite(points[i].y)&&float.IsFinite(points[i].z),"nonfinite geometry");Check(Vector3.Distance(points[i],points[Stadium.Wrap(i+1)])>.1f,"degenerate track seam"); }
        for(int kind=0;kind<=5;kind++) Check(surfaces.Contains(kind),"missing piece surface "+kind);
        Check(points.Max(p=>p.y)>1,"missing jump geometry");
        report.AppendLine("- PASS: valid preset and all six pieces; rejects malformed, missing, duplicate, disconnected, corner-obstacle, unknown-piece and version-mismatched courses; finite nondegenerate closed geometry.");
        EditorSceneManager.OpenScene("Assets/Scenes/CoyoteBasin.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>();game.Initialize();game.Verification=true;
        game.OpenTrackBuilder();Check(game.State==RallyGame.Phase.Builder,"open editor");
        var original=game.Track;game.BuildCustomTrack("{}");Check(game.Track==original&&game.State==RallyGame.Phase.Builder&&!string.IsNullOrEmpty(game.BuilderError),"invalid edit changed live track");
        game.BuildCustomTrack(Preset);Check(game.Track.Design!=null&&game.State==RallyGame.Phase.Countdown,"custom race did not start");
        Check(game.Track.Barriers.Count==768,"missing custom rail colliders");
        Physics.simulationMode=SimulationMode.Script;
        try {
            for(int type=0;type<3;type++) {
                game.Selected=type;game.StartRace();game.Countdown=0;game.Tick(.02f);
                foreach(var truck in game.Trucks) { truck.Autopilot=true;truck.Body.interpolation=RigidbodyInterpolation.None; }
                Physics.SyncTransforms();
                for(int step=0;step<12000&&!game.Player.Finished;step++) { game.Tick(.02f);Physics.Simulate(.02f); }
                Check(game.Player.Finished,"truck did not complete course: "+type+" gates="+game.Player.NextGate+" laps="+game.Player.CompletedLaps);
                Check(game.Player.MaxAirHeight<5,"excessive launch height");
                report.AppendLine("- PASS: "+game.Player.Spec.Name+" completed 3 laps with all 4 AI drivers; time="+game.Player.FinishTime.ToString("F2")+"s, recoveries="+game.Player.RecoveryCount+", max air="+game.Player.MaxAirHeight.ToString("F2")+"m.");
            }
            game.OpenTrackBuilder();game.BuildCustomTrack(Preset);Check(game.Trucks.All(t=>t.Track==game.Track),"stale truck track after rebuild");
            game.OpenTrackBuilder();game.CloseTrackBuilder();Check(game.State==RallyGame.Phase.Garage&&game.Track.Design!=null,"close lost custom course");
            game.OpenTrackBuilder();game.RaceOriginalTrack();Check(game.Track.Design==null&&game.State==RallyGame.Phase.Countdown,"stock circuit restoration");
            report.AppendLine("- PASS: invalid edits leave current track intact; rebuild rebinds all trucks; closing keeps custom course; stock circuit restores and races.");
        } finally { Physics.simulationMode=SimulationMode.FixedUpdate; }
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../TRACK-BUILDER-CHECKS.md")),report.ToString());
        Debug.Log("TRACK BUILDER VERIFICATION PASSED\n"+report);
        EditorSceneManager.OpenScene("Assets/Scenes/CoyoteBasin.unity");
        PrototypeBuilder.BuildWeb();
    }
}
