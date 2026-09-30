mergeInto(LibraryManager.library, {
  RaptorTouchState: function(phase,paused,selected,rank,lap,time,speed,nitro,countdown,offCourse,cameraMode,reducedMotion,introSkipVisible) {
    if(window.raptorTouch) window.raptorTouch.update({phase:phase,paused:!!paused,selected:selected,rank:rank,lap:lap,time:time,speed:speed,nitro:nitro,countdown:countdown,offCourse:!!offCourse,cameraMode:cameraMode,reducedMotion:!!reducedMotion,introSkipVisible:!!introSkipVisible});
  }
});
