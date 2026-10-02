using System;
using System.IO;
using UnityEngine;
using RaptorRally;
public static class IntroWipeVerification
{
    static void Check(bool ok,string why){if(!ok)throw new Exception("INTRO WIPE: "+why);}
    public static void VerifyAndBuild()
    {
        float[] samples=StartupIntro.CoinSamples();
        float energy=0,peak=0,jump=0;
        for(int i=0;i<samples.Length;i++) {
            energy+=samples[i]*samples[i]; peak=Mathf.Max(peak,Mathf.Abs(samples[i]));
            if(i>0) jump=Mathf.Max(jump,Mathf.Abs(samples[i]-samples[i-1]));
        }
        Check(samples.Length==21168 && energy/samples.Length>.0001f && peak<.4f && jump<.25f,"chime PCM duration, level and click protection");
        Check(Mathf.Abs(samples[0])<.001f && Mathf.Abs(samples[samples.Length-1])<.001f,"chime endpoints silent");
        using(var intro=new StartupIntro(null,null)) {
            intro.Advance(2); Check(!intro.CoinTriggered,"chime too early");
            intro.Advance(.12f); Check(intro.CoinTriggered,"chime missing at pixel transition");
            intro.Skip(); intro.Advance(.3f); Check(!intro.Active,"sound prevents skip");
        }
        using(var intro=new StartupIntro(null,null)) {
            intro.Advance(1); intro.Skip(); intro.Advance(4); Check(!intro.CoinTriggered,"skipped intro triggers chime");
        }
        using(var art=new BrandArtwork()) {
            Check(art.Ford!=null&&art.Ford.width==2048&&art.Ford.height==800,"sharp artwork dimensions");
            Check(StartupIntro.OpacityAt(0)==0&&StartupIntro.OpacityAt(1)==1,"fade-in");
            Check(StartupIntro.RevealAt(1)==0&&StartupIntro.RevealAt(2.1f)==0&&Mathf.Abs(StartupIntro.RevealAt(3)-.5f)<.001f&&StartupIntro.RevealAt(4)==1,"wipe progression");
            Check(StartupIntro.OpacityAt(5)==1&&StartupIntro.OpacityAt(5.8f)>.4f&&StartupIntro.OpacityAt(6.4f)==0,"fade-out");
            using(var intro=new StartupIntro(null,art)) {
                for(int i=0;i<66;i++) {Check(intro.SkipVisible,"skip disappeared during intro at "+intro.Elapsed);Check(intro.CurrentCard==StartupIntro.Card.Ford||intro.CurrentCard==StartupIntro.Card.Black,"extra logo card");intro.Advance(.1f);}
                intro.Advance(.01f);Check(!intro.Active&&!intro.SkipVisible,"completion");
            }
            foreach(float t in new[]{0f,1f,3f,5.8f,6.4f})using(var intro=new StartupIntro(null,art)) {
                intro.Advance(t);float reveal=intro.PixelReveal;intro.Skip();intro.Advance(.1f);Check(intro.Active&&intro.SkipVisible,"skip button disappeared during skip fade");Check(intro.PixelReveal==reveal,"wipe moved during skip fade");intro.Advance(.2f);Check(!intro.Active,"skip failed");
            }
        }
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../INTRO-WIPE-CHECKS.md")),"# Ford intro wipe checks\n\n- PASS: 2048×800 sharp Ford artwork, fade-in, top-to-bottom reveal timeline, pixel hold, fade-out and completion.\n- PASS: original 480 ms two-note coin chime has bounded PCM amplitude and silent endpoints; fires at pixel reveal and stays silent when skipped early.\n- PASS: only Ford/black states; Skip stays present during the entire intro including fades, wipe and black tail. Skip works before, during and after the wipe and freezes the reveal during its exit fade.\n");
        Debug.Log("INTRO WIPE VERIFICATION PASSED");PrototypeBuilder.BuildWeb();
    }
}
