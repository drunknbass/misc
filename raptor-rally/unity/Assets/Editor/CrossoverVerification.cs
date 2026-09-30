using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using RaptorRally;
public static class CrossoverVerification
{
    const string Course="{\"version\":2,\"name\":\"Crossover rodeo\",\"cells\":[29,28,21,14,15,16,17,18,19,20,13,6,5,4,3,10,17,24,31,30],\"pieces\":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]}";
    static void Check(bool ok,string message){if(!ok)throw new Exception("CROSSOVER: "+message);}
    public static void VerifyAndBuild(){Verify();VerifyNitro();TrackBuilderVerification.VerifyAndBuild();}
    public static void VerifyNitro()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CoyoteBasin.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>();game.Initialize();game.Verification=true;
        game.OpenTrackBuilder();game.BuildCustomTrack(Course);
        Physics.simulationMode=SimulationMode.Script;
        var report=new StringBuilder();
        try { for(int type=0;type<3;type++) {
            game.Selected=type;game.StartRace();game.Countdown=0;game.Tick(.02f);
            foreach(var other in game.Trucks)if(other!=game.Player)other.gameObject.SetActive(false);
            var t=game.Player;t.Autopilot=false;t.Body.interpolation=RigidbodyInterpolation.None;
            t.Body.position=new Vector3(-29,.92f,5);t.Body.rotation=Quaternion.LookRotation(Vector3.right);
            t.transform.SetPositionAndRotation(t.Body.position,t.Body.rotation);t.Body.linearVelocity=Vector3.right*20;t.Body.angularVelocity=Vector3.zero;
            t.CrossedStart=true;t.NextGate=(game.Track.Nearest(t.Body.position)/(Stadium.Samples/Stadium.GateCount)+1)%Stadium.GateCount;
            t.UpdateBarrierRecovery(true);Physics.SyncTransforms();bool air=false;float max=0;
            for(int step=0;step<300&&t.Body.position.x<29;step++) {t.Throttle=1;t.Steer=0;t.Boost=true;t.Tick(.02f,true);Physics.Simulate(.02f);t.CheckGate(step*.02f);air|=!t.Grounded&&t.Body.position.y>3;max=Mathf.Max(max,t.Body.position.y);}
            Check(t.Body.position.x>=29&&Mathf.Abs(t.Body.position.z-5)<2&&t.Body.position.y<3&&air,"nitro crossing failed "+t.Spec.Name+" at "+t.Body.position);
            report.AppendLine("- PASS: 20 m/s approach with nitro held: "+t.Spec.Name+" cleared jump and landed on exit; max chassis height="+max.ToString("F2")+"m.");
        }}finally{Physics.simulationMode=SimulationMode.FixedUpdate;}
        File.AppendAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../CROSSOVER-CHECKS.md")),report.ToString());Debug.Log("NITRO CROSSOVER PASSED\n"+report);
    }
    public static void Verify()
    {
        var report=new StringBuilder("# Open jump verification\n\n");
        var design=TrackDesign.Parse(Course);Check(design.HasCrossings,"crossing not detected");
        Check(design.CrossoverRoles().Count(x=>x>0)==3,"upper ramp roles");
        for(int i=0;i<design.cells.Length;i++)if(new[]{5,6,7,15,16,17}.Contains(i)) {var bad=TrackDesign.Parse(Course);bad.pieces[i]=1;Check(bad.Validate()!=null,"obstacle on approach accepted");}
        var legacy=TrackDesign.Parse(Course);legacy.version=1;Check(legacy.Validate()!=null,"legacy duplicate accepted");
        EditorSceneManager.OpenScene("Assets/Scenes/CoyoteBasin.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>();game.Initialize();game.Verification=true;
        game.OpenTrackBuilder();game.BuildCustomTrack(Course);Check(game.Track.HasCrossings,"track not replaced");
        var center=TrackDesign.Center(17);var track=game.Track;
        int lower=track.Nearest(center+Vector3.up*.92f),upper=track.Nearest(center+Vector3.up*(TrackDesign.CrossoverHeight+.92f));
        Check(track.Points[lower].y<.1f&&track.Points[upper].y>3,"nearest confused decks");
        Physics.SyncTransforms();
        Check(Physics.Raycast(center+Vector3.up*3.5f,Vector3.down,out var hit,4,1<<9)&&hit.point.y<.1f,"lower road is obstructed");
        Check(Physics.Raycast(center+Vector3.up*8,Vector3.down,out hit,8,1<<9)&&hit.point.y<.1f,"jump gap still has a deck");
        game.Countdown=0;game.Tick(.02f);var truck=game.Player;
        int gate=Enumerable.Range(0,Stadium.GateCount).OrderBy(i=>Vector3.Distance(track.Gate(i),center+Vector3.up*3.8f)).First();
        truck.NextGate=gate;truck.Body.position=track.Gate(gate)-Vector3.up*(track.Gate(gate).y-.92f);truck.Body.linearVelocity=track.Tangent(gate*Stadium.Samples/Stadium.GateCount)*10;
        truck.CheckGate(1);Check(truck.NextGate==gate,"wrong deck advanced gate");
        truck.Body.position=track.Gate(gate)+Vector3.up*.92f;truck.CheckGate(1);Check(truck.NextGate!=gate,"correct deck rejected gate");
        truck.CrossedStart=true;truck.NextGate=(gate+1)%Stadium.GateCount;truck.Recover();
        Check(truck.Body.position.x<center.x-24,"recovery did not restore jump run-up");
        Physics.SyncTransforms();Check(Physics.Raycast(truck.Body.position,Vector3.down,out hit,2,1<<9),"recovered over empty air");
        report.AppendLine("- PASS: jump recovery restores the truck to solid ground before the takeoff run-up.");
        report.AppendLine("- PASS: reserved approaches; legacy validation; separate upper/lower nearest routes; unobstructed lower surface and open jump gap; gates reject the wrong deck.");
        Physics.simulationMode=SimulationMode.Script;
        try {
            for(int type=0;type<3;type++) {
                game.Selected=type;game.StartRace();game.Countdown=0;game.Tick(.02f);
                foreach(var t in game.Trucks){t.Autopilot=true;t.Body.interpolation=RigidbodyInterpolation.None;}
                Physics.SyncTransforms();int airborne=0;
                for(int step=0;step<15000&&!game.Player.Finished;step++){
                    int recoveries=game.Player.RecoveryCount;var before=game.Player.Body.position;int beforeGate=game.Player.NextGate;float speed=game.Player.Speed;
                    game.Tick(.02f);Physics.Simulate(.02f);
                    if(game.Player.RecoveryCount!=recoveries)Debug.Log("JUMP RECOVERY type="+type+" position="+before+" gate="+beforeGate+" speed="+speed);
                    if(!game.Player.Grounded&&game.Player.Body.position.y>3&&Mathf.Abs(game.Player.Body.position.z-center.z)<5)airborne++;
                }
                string details=game.Player.Spec.Name+" time="+game.Player.FinishTime.ToString("F2")+" recoveries="+game.Player.RecoveryCount+" airFrames="+airborne+" maxY="+game.Player.MaxAirHeight.ToString("F2")+" gate="+game.Player.NextGate+" laps="+game.Player.CompletedLaps;
                Debug.Log("CROSSOVER RUN "+details);
                Check(game.Player.Finished,"did not finish "+details);Check(game.Player.RecoveryCount==0,"needed recovery "+details);Check(airborne>5,"did not jump "+details);Check(game.Player.MaxAirHeight<8,"unsafe launch "+details);
                report.AppendLine("- PASS: three laps with four AI trucks: "+details);
            }
        }finally{Physics.simulationMode=SimulationMode.FixedUpdate;}
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../CROSSOVER-CHECKS.md")),report.ToString());Debug.Log("CROSSOVER VERIFICATION PASSED\n"+report);
    }
}
