using System;
using Rive;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaptorRally
{
    // The game owns race state. Rive owns its editable vector presentation and transitions.
    public sealed class RiveRaceHud : IDisposable
    {
        Rive.File file;
        Artboard artboard;
        StateMachine machine;
        ViewModelInstance data;
        Rive.RenderQueue queue;
        Rive.Renderer renderer;
        CommandBuffer commands;
        RenderTexture raw, display;
        Material conversion;
        ViewModelInstanceNumberProperty speed,nitro,recovery;
        ViewModelInstanceBooleanProperty boosting;
        ViewModelInstanceStringProperty speedText,nitroText,position,lap,raceTime,truck,surface;
        float displayedSpeed,displayedNitro=100;
        bool rendered;
        public bool Ready { get; private set; }
        public float BoundSpeed => speed?.Value??0;
        public float BoundRecovery => recovery?.Value??0;
        public string BoundClock => raceTime?.Value;
        public bool BoundBoost => boosting?.Value??false;

        public RiveRaceHud()
        {
            try
            {
                file=Rive.File.Load(Resources.Load<TextAsset>("HUD/RacingHud"));
                artboard=file.Artboard("RaceHUD"); machine=artboard.StateMachine("RaceHUD");
                data=artboard.DefaultViewModel.CreateDefaultInstance(); machine.BindViewModelInstance(data);
                speed=data.GetNumberProperty("speed"); nitro=data.GetNumberProperty("nitro"); recovery=data.GetNumberProperty("recovery");
                boosting=data.GetBooleanProperty("boosting");
                speedText=data.GetStringProperty("speedText"); nitroText=data.GetStringProperty("nitroText");
                position=data.GetStringProperty("position"); lap=data.GetStringProperty("lap"); raceTime=data.GetStringProperty("raceTime");
                truck=data.GetStringProperty("truck"); surface=data.GetStringProperty("surface");
                if(speed==null || nitro==null || recovery==null || boosting==null || speedText==null || nitroText==null || position==null || lap==null || raceTime==null || truck==null || surface==null)
                    throw new InvalidOperationException("RaceHUD view-model contract is incomplete.");
                int pixelScale=Screen.width>=2400 && Screen.height>=1350?2:1;
                raw=new RenderTexture(TextureHelper.Descriptor(1600*pixelScale,900*pixelScale)) { name="Rive HUD native surface" };
                raw.Create();
                display=new RenderTexture(raw.width,raw.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear) { name="Rive HUD IMGUI surface" }; display.Create();
                conversion=new Material(Resources.Load<Shader>("HUD/RiveToGUI"));
                conversion.SetFloat("_FlipY",TextureHelper.ShouldFlipTexture()?1:0);
                queue=new Rive.RenderQueue(raw); renderer=queue.Renderer();
                renderer.Align(Fit.Contain,Alignment.Center,artboard); renderer.Draw(artboard);
                commands=new CommandBuffer { name="Raptor Rally / Rive HUD" };
                commands.SetRenderTarget(raw); renderer.AddToCommandBuffer(commands);
                Ready=true;
                Debug.Log("RAPTOR RIVE HUD READY: RaceHUD artboard, state machine and live view model.");
            }
            catch(Exception e)
            {
                Debug.LogWarning("Rive HUD unavailable; using readable fallback HUD. "+e.Message);
                Dispose();
            }
        }
        public void Update(RallyGame game,float dt)
        {
            if(!Ready || game.State==RallyGame.Phase.Garage || (game.Paused && rendered)) return;
            var player=game.Player;
            float mph=player.Speed*2.23694f;
            float blend=game.ReducedMotion?1:1-Mathf.Exp(-Mathf.Max(dt,0)*14);
            displayedSpeed=Mathf.Lerp(displayedSpeed,mph,blend); displayedNitro=Mathf.Lerp(displayedNitro,player.Nitro*100,blend);
            speed.Value=displayedSpeed; nitro.Value=displayedNitro; recovery.Value=player.OffCourse?1:0;
            boosting.Value=player.Boosting && !game.ReducedMotion;
            speedText.Value=Mathf.RoundToInt(mph).ToString("00"); nitroText.Value=Mathf.RoundToInt(player.Nitro*100)+"%";
            position.Value=(game.Standings().IndexOf(player)+1).ToString("00"); lap.Value=Mathf.Min(3,player.CompletedLaps+1).ToString("00");
            raceTime.Value=RallyGame.Clock(player.Finished?player.FinishTime:game.RaceTime); truck.Value=player.Spec.Name;
            surface.Value=player.OffCourse?"RETURN TO TRACK":player.Boosting?"NITRO ENGAGED":!player.Grounded?"AIRBORNE":"PACKED DIRT";
            machine.Advance(game.Paused?0:dt);
            Graphics.ExecuteCommandBuffer(commands);
            bool srgb=GL.sRGBWrite; GL.sRGBWrite=false;
            Graphics.Blit(raw,display,conversion); GL.sRGBWrite=srgb;
            rendered=true;
        }
        public void Draw() { if(Ready && rendered) GUI.DrawTexture(new Rect(0,0,1600,900),display,ScaleMode.StretchToFill,true); }
        public void Reset() { displayedSpeed=0; displayedNitro=100; rendered=false; }
        public void Dispose()
        {
            Ready=false; commands?.Dispose(); commands=null; queue?.Dispose(); queue=null;
            machine?.Dispose(); machine=null; data?.Dispose(); data=null; artboard?.Dispose(); artboard=null; file?.Dispose(); file=null;
            foreach(var texture in new[]{raw,display}) if(texture!=null) { texture.Release(); UnityEngine.Object.Destroy(texture); }
            raw=display=null; if(conversion!=null) UnityEngine.Object.Destroy(conversion); conversion=null;
        }
    }
}
