using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RaptorRally;

// Runs in Play Mode: edit-mode Physics.Simulate does not deliver collision callbacks.
[InitializeOnLoad]
public static class CollisionVerification
{
    const string Pending="RaptorCollisionVerification";
    static CollisionVerification() { EditorApplication.playModeStateChanged+=OnMode; }
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CoyoteBasin.unity");
        SessionState.SetBool(Pending,true); EditorApplication.EnterPlaymode();
    }
    static void OnMode(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending,false)) return;
        SessionState.SetBool(Pending,false); EditorApplication.update+=RunWhenReady;
    }
    static void RunWhenReady()
    {
        if(UnityEngine.Object.FindAnyObjectByType<RallyGame>()==null) return;
        EditorApplication.update-=RunWhenReady; Run();
    }
    static void Run()
    {
        var report=new StringBuilder("# Collision acceptance — Play Mode\n\n");
        var failures=new List<string>();
        int exit=0;
        try {
            Debug.Log("RAPTOR COLLISION VERIFICATION STARTED");
            var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>(); game.Initialize(); game.Verification=true; game.enabled=false;
            Physics.simulationMode=SimulationMode.Script;
            foreach(var truck in game.Trucks) truck.gameObject.SetActive(false);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="Contact test floor"; floor.layer=9;
            floor.transform.position=new Vector3(0,-.5f,200); floor.transform.localScale=new Vector3(120,1,140);
            var walls=new GameObject[2];
            for(int s=0;s<2;s++) {
                var wall=walls[s]=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer=11;
                wall.transform.position=new Vector3(s==0?-8:8,.65f,200); wall.transform.localScale=new Vector3(.55f,1.3f,120);
            }
            var attackers=new RaptorTruck[3]; var targets=new RaptorTruck[3];
            for(int i=0;i<3;i++) {
                attackers[i]=Make(game,i,"Attacker"); targets[i]=Make(game,i,"Target");
            }
            foreach(var a in attackers) foreach(int sign in new[]{-1,1}) foreach(float angle in new[]{15f,35f,55f}) {
                Vector3 heading=new Vector3(sign*Mathf.Sin(angle*Mathf.Deg2Rad),0,Mathf.Cos(angle*Mathf.Deg2Rad));
                Place(a,new Vector3(sign*4.5f,.8f,175),heading,heading*14); a.Throttle=1;
                float start=a.Body.position.z,maxYaw=0;
                for(int i=0;i<100;i++) { Step(a); maxYaw=Mathf.Max(maxYaw,Mathf.Abs(a.Body.angularVelocity.y)); }
                float distance=a.Body.position.z-start;
                bool pass=distance>12 && a.Speed>7 && Mathf.Abs(a.Body.position.x)<7 && Vector3.Dot(a.Body.rotation*Vector3.forward,Vector3.forward)>.7f;
                Result(report,failures,pass,$"{a.Spec.Name} wall {sign:+0;-0} at {angle:0}°: travel {distance:F2} m, exit {a.Speed:F2} m/s, peak yaw {maxYaw:F2} rad/s");
                // Steering away must release contact, including immediately after a long scrape.
                float wallX=Mathf.Abs(a.Body.position.x); a.Steer=-sign;
                for(int i=0;i<25;i++) Step(a);
                Result(report,failures,Mathf.Abs(a.Body.position.x)<wallX-.25f,$"{a.Spec.Name} steer away after {angle:0}° scrape");
                a.gameObject.SetActive(false);
            }
            foreach(var a in attackers) {
                Place(a,new Vector3(3,.8f,180),Vector3.right,Vector3.right*14); a.Throttle=1;
                for(int i=0;i<75;i++) Step(a);
                Result(report,failures,a.Speed<1 && Mathf.Abs(a.Body.position.z-180)<.5f && a.Body.position.x<6,$"{a.Spec.Name} square wall hit stops without invented sideways speed");
                a.gameObject.SetActive(false);
            }
            foreach(var wall in walls) wall.SetActive(false);
            foreach(var a in attackers) foreach(var b in targets) foreach(string kind in new[]{"rear","offset","side","head-on"}) {
                Vector3 origin=new Vector3(0,.8f,200),target=origin+Vector3.forward*10;
                Vector3 heading=Vector3.forward,otherHeading=kind=="head-on"?Vector3.back:Vector3.forward;
                if(kind=="offset") origin.x=.8f;
                if(kind=="side") { origin=target+Vector3.left*7; heading=Vector3.right; }
                Place(a,origin,heading,heading*(kind=="head-on"?10:14));
                Place(b,target,otherHeading,kind=="head-on"?Vector3.back*10:Vector3.zero);
                a.Throttle=kind=="head-on"?0:1;
                float peak=0,spin=0;
                for(int i=0;i<75;i++) { Step(a,b); peak=Mathf.Max(peak,b.Speed); spin=Mathf.Max(spin,Quaternion.Angle(b.Body.rotation,Quaternion.LookRotation(otherHeading))); }
                float push=Vector3.Dot(b.Body.position-target,heading);
                bool pass=kind=="head-on"?a.Speed<3 && b.Speed<3 && spin<35:push>2 && peak>3 && spin<45;
                pass &= a.Speed<25 && b.Speed<25 && a.Body.position.y<1.5f && b.Body.position.y<1.5f;
                Result(report,failures,pass,$"{a.Spec.Name} → {b.Spec.Name} {kind}: push {push:F2} m, target peak {peak:F2} m/s, target turn {spin:F1}°");
                a.gameObject.SetActive(false); b.gameObject.SetActive(false);
            }
            // Exercise actual segmented rails and all four active opponents with callbacks enabled.
            floor.SetActive(false);
            foreach(var a in attackers) foreach(int sign in new[]{-1,1}) {
                Vector3 tangent=game.Track.Tangent(3),side=game.Track.Side(3);
                Vector3 start=game.Track.Points[3]+side*sign*(Stadium.HalfWidth-2.9f)+Vector3.up*.8f;
                Vector3 heading=(tangent*Mathf.Cos(35*Mathf.Deg2Rad)+side*sign*Mathf.Sin(35*Mathf.Deg2Rad)).normalized;
                Place(a,start,heading,heading*14); a.Throttle=1;
                for(int step=0;step<60;step++) Step(a);
                float along=Vector3.Dot(a.Body.position-start,tangent);
                Result(report,failures,along>9 && a.Speed>8 && !a.OffCourse && Vector3.Dot(a.Body.rotation*Vector3.forward,tangent)>.7f,
                    $"{a.Spec.Name} actual segmented rail {sign:+0;-0}: travel {along:F2} m, exit {a.Speed:F2} m/s");
                a.gameObject.SetActive(false);
            }
            for(int type=0;type<3;type++) {
                game.Selected=type; game.StartRace(); game.Countdown=0; game.Player.Autopilot=true;
                foreach(var truck in game.Trucks) truck.Body.interpolation=RigidbodyInterpolation.None;
                for(int step=0;step<12000 && game.Trucks.Exists(t=>!t.Finished);step++) { game.Tick(.02f); Physics.Simulate(.02f); }
                bool pass=true; foreach(var truck in game.Trucks) pass &= truck.Finished && truck.FinishTime<180 && truck.RecoveryCount<3;
                Result(report,failures,pass,$"Four-truck race with callbacks: {TruckSpec.Lineup[type].Name}, player {game.Player.FinishTime:F2}s, recoveries {game.Player.RecoveryCount}");
            }
            if(failures.Count>0) throw new Exception(failures.Count+" collision acceptance cases failed");
            Debug.Log("RAPTOR COLLISION VERIFICATION PASSED\n"+report);
        } catch(Exception e) { report.AppendLine("\nFAIL: "+e); Debug.LogError(report); exit=1; }
        finally {
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../COLLISIONS.md")),report.ToString());
            Physics.simulationMode=SimulationMode.FixedUpdate;
            EditorApplication.Exit(exit);
        }
    }
    static RaptorTruck Make(RallyGame game,int type,string name)
    {
        var go=new GameObject(name+type); var t=go.AddComponent<RaptorTruck>();
        t.Setup(game.Track,type,name,true,TruckSpec.Lineup[type].Color); go.SetActive(false); return t;
    }
    static void Place(RaptorTruck truck,Vector3 position,Vector3 forward,Vector3 velocity)
    {
        truck.ResetForRace(0); truck.Body.isKinematic=false; truck.Body.interpolation=RigidbodyInterpolation.None;
        truck.Body.position=position; truck.Body.rotation=Quaternion.LookRotation(forward); truck.transform.SetPositionAndRotation(position,truck.Body.rotation);
        truck.Body.linearVelocity=velocity; truck.Body.angularVelocity=Vector3.zero; truck.Throttle=truck.Steer=0;
        truck.UpdateBarrierRecovery(true); Physics.SyncTransforms();
    }
    static void Step(RaptorTruck a,RaptorTruck b=null) { a.Tick(.02f,true); if(b!=null) b.Tick(.02f,true); Physics.Simulate(.02f); }
    static void Result(StringBuilder report,List<string> failures,bool pass,string description)
    { report.AppendLine("- "+(pass?"PASS":"FAIL")+": "+description); Debug.Log("CONTACT TEST "+(pass?"PASS: ":"FAIL: ")+description); if(!pass) failures.Add(description); }
}
