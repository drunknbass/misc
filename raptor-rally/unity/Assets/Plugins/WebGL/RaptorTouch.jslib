mergeInto(LibraryManager.library, {
  RaptorTouchState: function(phase,paused,selected,rank,lap,time,speed,nitro,countdown,offCourse,following,reducedMotion,introSkipVisible) {
    if(window.raptorTouch) window.raptorTouch.update({phase:phase,paused:!!paused,selected:selected,rank:rank,lap:lap,time:time,speed:speed,nitro:nitro,countdown:countdown,offCourse:!!offCourse,following:!!following,reducedMotion:!!reducedMotion,introSkipVisible:!!introSkipVisible});
  }
});
