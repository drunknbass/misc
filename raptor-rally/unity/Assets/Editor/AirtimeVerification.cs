using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor.SceneManagement;
using RaptorRally;
public static class AirtimeVerification
{
    public static void Measure() => Run(false);
    public static void Verify() => Run(true);
    public static void VerifyReleaseAndBuild() { PrototypeBuilder.Verify(); CrossoverVerification.Verify(); CrossoverVerification.VerifyNitro(); TrackBuilderVerification.VerifyAndBuild(); }
    static void Run(bool verify)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CoyoteBasin.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>();game.Initialize();game.Verification=true;
        var report=new StringBuilder();Physics.simulationMode=SimulationMode.Script;
        try { for(int type=0;type<3;type++) foreach(int crest in Stadium.JumpCrests) {
            game.Selected=type;game.StartRace();game.Countdown=0;game.Tick(.02f);
            var t=game.Player;foreach(var other in game.Trucks)if(other!=t)other.gameObject.SetActive(false);
            t.Autopilot=false;t.Body.interpolation=RigidbodyInterpolation.None;
            int approach=crest-14;var heading=game.Track.Tangent(approach);
            t.Body.position=game.Track.Points[approach]+Vector3.up*.92f;t.Body.rotation=Quaternion.LookRotation(heading);
            t.transform.SetPositionAndRotation(t.Body.position,t.Body.rotation);t.Body.linearVelocity=heading*t.Spec.TopSpeed*1.3f;t.Body.angularVelocity=Vector3.zero;
            t.UpdateBarrierRecovery(true);Physics.SyncTransforms();
            float air=0,longest=0,height=0,clearance=0,launch=0;bool landed=false,hadAir=false;
            for(int step=0;step<95;step++) {
                t.Throttle=1;t.Steer=0;t.Boost=true;t.Tick(.02f,true);Physics.Simulate(.02f);
                if(!t.Grounded){air+=.02f;hadAir=true;}else {if(air>.2f)landed=true;air=0;}
                longest=Mathf.Max(longest,air);height=Mathf.Max(height,t.Body.position.y);launch=Mathf.Max(launch,t.Body.linearVelocity.y);
                if(Physics.Raycast(t.Body.position,Vector3.down,out var hit,30,1<<9))clearance=Mathf.Max(clearance,hit.distance-.8f);
                if(verify&&game.Track.DistanceFromCourse(t.Body.position)>Stadium.HalfWidth-.4f)throw new Exception("AIR: left lane "+type+" crest="+crest);
            }
            string line=$"{t.Spec.Name} crest={crest} air={longest:F2}s height={height:F2}m clearance={clearance:F2}m launch={launch:F2}m/s landed={landed}";
            report.AppendLine(line);Debug.Log("AIR MEASURE "+line);
            if(verify&&(!hadAir||longest<.5f||clearance<.65f||height>7||!landed))throw new Exception("AIR: jump acceptance failed "+line);
        }} finally {Physics.simulationMode=SimulationMode.FixedUpdate;}
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,verify?"../../AIRTIME-CHECKS.md":"../../AIRTIME-BASELINE.md")),report.ToString());
    }
}
