using System;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using RaptorRally;

public static class CockpitVerification
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception("COCKPIT: "+message); }
    public static void VerifyAndBuild()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CoyoteBasin.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>(); game.Initialize(); game.Verification=true;
        var report=new StringBuilder("# Cockpit automated verification\n\n");
        for(int type=0;type<3;type++)
        {
            game.Garage(); game.Selected=type; game.PrepareGrid(); game.StartRace();
            game.CameraMode=RallyGame.RaceCamera.WholeTrack;
            game.CycleCamera(); Check(game.CameraMode==RallyGame.RaceCamera.Follow,"follow cycle");
            game.CycleCamera(); Check(game.InCockpit,"driver cycle");
            game.UpdateCamera(.02f,true);
            Check(!game.View.orthographic && game.View.nearClipPlane==.04f,"perspective projection");
            Check((game.View.cullingMask & (1<<DriverCockpit.ExteriorLayer))==0,"exterior occludes cockpit");
            Check((game.View.cullingMask & (1<<DriverCockpit.InteriorLayer))!=0,"interior is missing");
            var eye=game.Player.transform.InverseTransformPoint(game.View.transform.position);
            Check(Vector3.Distance(eye,game.Player.Cockpit.Eye)<.001f && eye.x<0,"left-hand eye alignment");
            foreach(float x in new[]{.4f,.5f,.6f}) foreach(float y in new[]{.5f,.6f})
                Check(!HitsInterior(game.View.ViewportPointToRay(new Vector3(x,y,0)),game.Player.Cockpit.Root),"central windscreen obstructed for "+type+" at "+x+","+y);
            game.Player.transform.position+=new Vector3(2,.8f,3);
            game.Player.transform.rotation*=Quaternion.Euler(0,35,0);
            game.UpdateCamera(.016f);
            eye=game.Player.transform.InverseTransformPoint(game.View.transform.position);
            Check(Mathf.Abs(eye.y-game.Player.Cockpit.Eye.y)<=.056f,"eye escaped cabin vertically");
            Check(Mathf.Abs(Mathf.DeltaAngle(game.Player.transform.eulerAngles.y,game.View.transform.eulerAngles.y))<=3.01f,"excessive yaw lag");
            game.ReducedMotion=true; game.UpdateCamera(.016f);
            Check(Mathf.Abs(Mathf.DeltaAngle(game.Player.transform.eulerAngles.y,game.View.transform.eulerAngles.y))<.01f,"reduced-motion yaw lag");
            game.ReducedMotion=false;
            game.StartRace(); game.UpdateCamera(.02f,true); Check(game.InCockpit,"retry lost camera mode");
            game.Garage(); game.UpdateCamera(.02f,true);
            Check(game.View.orthographic && !game.Player.Cockpit.Root.gameObject.activeSelf,"garage did not restore preview");
            game.StartRace(); game.UpdateCamera(.02f,true); Check(game.InCockpit,"garage return lost driver preference");
            game.CycleCamera(); game.UpdateCamera(.02f,true);
            Check(game.CameraMode==RallyGame.RaceCamera.WholeTrack && game.View.orthographic && !game.Player.Cockpit.Root.gameObject.activeSelf,"overview cycle failed");
            report.AppendLine("- PASS: "+TruckSpec.Lineup[type].Name+" — 3-mode cycle, perspective/layers, left eye, six clear windshield rays, bounded vertical/yaw damping, reduced motion, retry and garage transitions.");
        }
        game.SetTouchControls(1); game.CameraMode=RallyGame.RaceCamera.Follow; game.TouchAction("camera");
        Check(game.InCockpit,"mobile camera action failed"); game.TouchAction("camera");
        Check(game.CameraMode==RallyGame.RaceCamera.WholeTrack,"mobile wrap failed");
        report.AppendLine("- PASS: mobile camera action cycles follow → driver seat → whole track.");
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../COCKPIT-CHECKS.md")),report.ToString());
        Debug.Log("COCKPIT VERIFICATION PASSED");
        PrototypeBuilder.BuildWeb();
    }
    static bool HitsInterior(Ray ray,Transform root)
    {
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
            var mesh=filter.sharedMesh; var vertices=mesh.vertices; var indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3) {
                Vector3 a=filter.transform.TransformPoint(vertices[indices[i]]),b=filter.transform.TransformPoint(vertices[indices[i+1]]),c=filter.transform.TransformPoint(vertices[indices[i+2]]);
                Vector3 e1=b-a,e2=c-a,p=Vector3.Cross(ray.direction,e2); float det=Vector3.Dot(e1,p);
                if(Mathf.Abs(det)<1e-7f) continue;
                float inverse=1/det; Vector3 t=ray.origin-a; float u=Vector3.Dot(t,p)*inverse;
                if(u<0 || u>1) continue;
                Vector3 q=Vector3.Cross(t,e1); float v=Vector3.Dot(ray.direction,q)*inverse;
                if(v<0 || u+v>1) continue;
                float distance=Vector3.Dot(e2,q)*inverse;
                if(distance>0 && distance<8) return true;
            }
        }
        return false;
    }
}
