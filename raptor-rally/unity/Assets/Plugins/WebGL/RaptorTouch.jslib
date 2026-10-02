mergeInto(LibraryManager.library, {
  // Unity 6000.6 Audio.js owns this context; share it instead of opening a second mixer.
  RaptorIntroAudio__deps: ['$WEBAudio'],
  RaptorIntroAudio: function(active) {
    var button=document.getElementById('intro-sound');
    var context=WEBAudio.audioContext;
    var ready=!!context && context.state==='running';
    if(button) {
      button.dataset.introActive=active?'true':'false';
      button.hidden=!active || ready;
      button.onclick=function() {
        if(!WEBAudio.audioContext || button.disabled) return;
        button.disabled=true;
        WEBAudio.audioContext.resume().then(function() {
          if(WEBAudio.audioContext.state==='running' && button.dataset.introActive==='true' && window.raptorGame)
            window.raptorGame.SendMessage('Raptor Rally','RestartIntroWithSound');
        }).catch(function() {
          button.textContent='Try sound again';
        }).finally(function() { button.disabled=false; });
      };
    }
    return ready?1:0;
  },
  RaptorBuilderState: function(phase,custom,courseName,error) {
    if(window.raptorBuilder) window.raptorBuilder.update({phase:phase,custom:!!custom,name:UTF8ToString(courseName),error:UTF8ToString(error)});
  },
  RaptorTouchState: function(phase,paused,selected,rank,lap,time,speed,nitro,countdown,offCourse,cameraMode,reducedMotion,introSkipVisible) {
    if(window.raptorTouch) window.raptorTouch.update({phase:phase,paused:!!paused,selected:selected,rank:rank,lap:lap,time:time,speed:speed,nitro:nitro,countdown:countdown,offCourse:!!offCourse,cameraMode:cameraMode,reducedMotion:!!reducedMotion,introSkipVisible:!!introSkipVisible});
  }
});
