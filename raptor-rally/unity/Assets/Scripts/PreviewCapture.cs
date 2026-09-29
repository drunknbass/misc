#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace RaptorRally
{
    // Opt-in visual QA only; excluded entirely from player builds.
    public sealed class PreviewCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-raptorPreview") >= 0 && !UnityEditor.SessionState.GetBool("RaptorPreviewConsumed",false))
            {
                UnityEditor.SessionState.SetBool("RaptorPreviewConsumed",true);
                new GameObject("Visual QA capture").AddComponent<PreviewCapture>();
            }
        }
        IEnumerator Start()
        {
            var game=Object.FindAnyObjectByType<RallyGame>(); game.Verification=true;
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            yield return new WaitForSeconds(3);
            if(game.MotionHud==null || !game.MotionHud.Ready) throw new System.Exception("Rive HUD runtime initialization failed");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-garage.png"));
            yield return new WaitForSeconds(1);
            for(int selected=0;selected<3;selected++)
            {
                game.Selected=selected; game.PrepareGrid();
                yield return new WaitForSeconds(.6f);
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"garage-raptor-"+selected+".png"));
                yield return new WaitForSeconds(.2f);
            }
            game.Selected=0;
            game.StartRace(); game.Player.Autopilot=true;
            yield return new WaitForSeconds(13);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-race.png"));
            float timeout=Time.time+18;
            while(Mathf.Abs(game.WheelAngle)<25 && Time.time<timeout) yield return null;
            yield return new WaitForSeconds(.15f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-steering.png"));
            yield return new WaitForSeconds(.2f);
            // Freeze only simulation time so each pivot comparison has identical scenery.
            float savedSteer=game.Player.Steer;
            Time.timeScale=0;
            foreach(float steering in new[]{-1f,0f,1f})
            {
                game.Player.Steer=steering; game.AdvanceWheel(4);
                yield return new WaitForSecondsRealtime(.15f);
                string pose=steering<0?"left":steering>0?"right":"center";
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"wheel-"+pose+".png"));
                yield return new WaitForSecondsRealtime(.15f);
            }
            game.Player.Steer=savedSteer; game.AdvanceWheel(4); Time.timeScale=1;
            game.FollowPlayer=true;
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-follow.png"));
            yield return new WaitForSeconds(.3f);
            game.FollowPlayer=false;
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-raptorQuickPreview")>=0) Time.timeScale=4;
            while(game.State!=RallyGame.Phase.Results) yield return null;
            Time.timeScale=1;
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-results.png"));
            yield return new WaitForSeconds(1);
            game.State=RallyGame.Phase.Racing; game.FollowPlayer=true; Time.timeScale=0;
            foreach(var other in game.Trucks) if(other!=game.Player) other.gameObject.SetActive(false);
            var p=game.Player; p.Autopilot=false; p.FinishTime=-1; p.CompletedLaps=0; game.RaceTime=42.15f;
            p.Body.position=game.Track.Points[8]+game.Track.Side(8)*(Stadium.HalfWidth+4)+Vector3.up*.92f;
            p.Body.rotation=Quaternion.LookRotation(-game.Track.Side(8)); p.Body.linearVelocity=Vector3.zero;
            p.transform.SetPositionAndRotation(p.Body.position,p.Body.rotation); p.UpdateBarrierRecovery(true);
            game.UpdateCamera(0,true); game.MotionHud.Update(game,.25f);
            if(game.MotionHud.BoundRecovery!=1) throw new System.Exception("Rive did not receive off-course state");
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-recovery.png"));
            yield return new WaitForSecondsRealtime(.2f);
            p.Body.position=game.Track.Points[8]+Vector3.up*.92f; p.Body.rotation=Quaternion.LookRotation(game.Track.Tangent(8));
            p.transform.SetPositionAndRotation(p.Body.position,p.Body.rotation); p.Body.linearVelocity=game.Track.Tangent(8)*20;
            p.UpdateBarrierRecovery(true); p.Nitro=.65f; p.Boosting=true; game.MotionHud.Update(game,.25f);
            if(game.MotionHud.BoundRecovery!=0 || !game.MotionHud.BoundBoost || game.MotionHud.BoundSpeed<40)
                throw new System.Exception("Rive speed/boost/re-entry binding failed");
            game.UpdateCamera(0,true);
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-rive-boost.png"));
            yield return new WaitForSecondsRealtime(.2f);
            game.Paused=true; string clockBefore=game.MotionHud.BoundClock;
            game.RaceTime+=100; game.MotionHud.Update(game,1);
            if(game.MotionHud.BoundClock!=clockBefore) throw new System.Exception("Paused Rive HUD advanced");
            game.RaceTime-=100;
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"prototype-rive-paused.png"));
            yield return new WaitForSecondsRealtime(.2f);
            game.Paused=false; game.ReducedMotion=true; game.MotionHud.Update(game,.02f);
            if(game.MotionHud.BoundBoost) throw new System.Exception("Reduced-motion HUD still animates boost");
            game.ReducedMotion=false; Time.timeScale=1;
            Debug.Log("RAPTOR RIVE RUNTIME QA PASSED: speed, nitro, recovery, pause, reduced motion.");
            game.Verification=false; game.Garage();
            Debug.Log("RAPTOR VISUAL QA CAPTURE COMPLETE");
            Destroy(gameObject);
        }
    }
}
#endif
