#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace RaptorRally
{
    // Opt-in editor QA. No code from this helper is included in a player.
    public sealed class SplashCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-raptorSplashPreview")<0) return;
            new GameObject("Splash QA").AddComponent<SplashCapture>();
        }
        static void Check(bool value,string message) { if(!value) throw new Exception("SPLASH QA: "+message); }
        IEnumerator Start()
        {
            var game=UnityEngine.Object.FindAnyObjectByType<RallyGame>();
            game.Verification=true;
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../splash-preview"));
            Directory.CreateDirectory(output);
            Check(game.IntroActive,"startup intro missing");
            Check(game.Branding.Performance.width==720 && game.Branding.Performance.height==1280,"source dimensions changed");
            Check(game.Branding.Badge.width==339 && game.Branding.Badge.height==296,"badge dimensions changed");
            Check(game.Branding.Ford.width>=1024,"sharp Ford artwork missing");
            Check(StartupIntro.RevealAt(1)==0 && StartupIntro.RevealAt(4)==1,"wipe timeline invalid");
            var position=game.Player.Body.position;
            game.StartRace(); game.Garage(); game.SetTouchControls(1); game.TouchAction("start"); game.SetTouchInput(31); game.Tick(5);
            Check(game.IntroActive && game.RaceTime==0 && game.Countdown==0 && game.Player.Throttle==0 && game.Player.Body.isKinematic && game.Player.Body.position==position,"gameplay escaped intro guard");
            game.SetTouchControls(0);
            // A paused simulation must not stop the intro's real-time clock.
            Time.timeScale=0;
            float before=game.Intro.Elapsed;
            yield return new WaitForSecondsRealtime(.2f);
            Check(game.Intro.Elapsed>before,"intro depends on simulation clock");
            Time.timeScale=1;
            float[] times={1f,2.3f,3f,3.7f,4.6f,5.8f,6.4f};
            string[] names={"01-sharp","02-wipe-start","03-wipe-half","04-wipe-end","05-pixel","06-fade","07-black"};
            for(int i=0;i<times.Length;i++)
            {
                while(game.IntroActive && game.Intro.Elapsed<times[i]) yield return null;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output,names[i]+".png"));
            }
            while(game.IntroActive) yield return null;
            Check(game.State==RallyGame.Phase.Garage,"completion did not enter menu");
            yield return new WaitForSecondsRealtime(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"08-menu.png"));
            yield return new WaitForSecondsRealtime(.3f);
            game.StartRace(); Check(game.State==RallyGame.Phase.Countdown,"race failed after intro");
            game.Garage(); game.BeginStartupIntro(); Check(!game.IntroActive,"intro replayed on retry");
            using(var intro=new StartupIntro(game.transform,game.Branding))
            {
                intro.Advance(1); intro.Skip(); intro.Advance(.1f);
                Check(intro.Active && intro.Opacity<1,"skip did not fade");
                intro.Advance(.2f); Check(!intro.Active,"skip did not finish");
            }
            File.WriteAllText(Path.Combine(output,"verification.txt"),"PASS: source dimensions, single Ford card, vertical wipe, race entry and touch input guards, stationary truck, unscaled intro clock, automatic menu transition, no replay on retry, skip fade. Captured seven intro frames and menu.\n");
            Debug.Log("RAPTOR SPLASH QA PASSED");
            UnityEditor.EditorApplication.Exit(0);
        }
    }
}
#endif
