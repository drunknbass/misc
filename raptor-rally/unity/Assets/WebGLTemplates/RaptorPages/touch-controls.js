(() => {
  'use strict';
  const root=document.getElementById('touch-ui'),byId=id=>document.getElementById(id);
  // Use the same 116×96 cell grid and source crop as Unity's badge shader.
  // This is a display-only canvas; the supplied JPEG remains unchanged.
  const badge=document.getElementById('menu-badge'),badgeSource=new Image();
  badgeSource.onload=()=>{const context=badge.getContext('2d');context.imageSmoothingEnabled=false;context.drawImage(badgeSource,24,10,292,242,0,0,116,96);};
  badgeSource.src='raptor-badge.jpg';
  const holdButtons=[...root.querySelectorAll('[data-bit]')],pointers=new Map();
  const trucks=['F-150 RAPTOR','BRONCO RAPTOR','RANGER RAPTOR'];
  const descriptions=['Long wheelbase / strong boost','Short wheelbase / quick rotation','Light pickup / balanced grip'];
  let instance=null,enabled=false,lastMask=-1,state={phase:4,paused:false,selected:0};
  const coarse=matchMedia('(pointer:coarse)');
  const mobile=()=>coarse.matches || navigator.maxTouchPoints>0 && innerWidth<=1100;
  const send=(method,value)=>{if(instance)instance.SendMessage('Raptor Rally',method,value);};
  const canDrive=()=>enabled && state.phase===2 && !state.paused;
  function flush(){
    let mask=0;for(const bit of pointers.values())mask|=bit;
    if(!canDrive())mask=0;
    for(const button of holdButtons)button.setAttribute('aria-pressed',String(!!(mask&Number(button.dataset.bit))));
    if(mask!==lastMask){lastMask=mask;send('SetTouchInput',mask);}
  }
  function releaseAll(){pointers.clear();flush();}
  function configure(){
    releaseAll();const next=mobile();
    document.body.classList.toggle('touch-enabled',next);
    byId('input-help').textContent=next?'Touch controls • hold gas + steer • turn your phone for a wider view':'WASD / arrows to drive · Space for nitro · C to cycle camera views';
    if(!instance)return;
    if(next!==enabled){enabled=next;root.hidden=!enabled;send('SetTouchControls',enabled?1:0);lastMask=-1;flush();}
  }
  for(const button of holdButtons){
    const bit=Number(button.dataset.bit);
    button.addEventListener('pointerdown',event=>{
      if(!canDrive() || event.pointerType==='mouse' && event.button!==0)return;
      event.preventDefault();button.setPointerCapture(event.pointerId);pointers.set(event.pointerId,bit);flush();
    });
    const release=event=>{pointers.delete(event.pointerId);flush();};
    button.addEventListener('pointerup',release);button.addEventListener('pointercancel',release);button.addEventListener('lostpointercapture',release);
    button.addEventListener('keydown',event=>{if([' ','Enter'].includes(event.key) && canDrive()){event.preventDefault();pointers.set('key'+bit,bit);flush();}});
    button.addEventListener('keyup',event=>{if([' ','Enter'].includes(event.key)){event.preventDefault();pointers.delete('key'+bit);flush();}});
    button.addEventListener('blur',()=>{pointers.delete('key'+bit);flush();});
    button.addEventListener('contextmenu',event=>event.preventDefault());
  }
  root.querySelectorAll('[data-action]').forEach(button=>button.addEventListener('click',()=>{
    releaseAll();send('TouchAction',button.dataset.action);
  }));
  addEventListener('blur',releaseAll);addEventListener('pagehide',releaseAll);
  document.addEventListener('visibilitychange',()=>{if(document.hidden)releaseAll();});
  addEventListener('resize',configure);coarse.addEventListener('change',configure);
  if(window.visualViewport)visualViewport.addEventListener('resize',releaseAll);
  function clock(seconds){const ticks=Math.floor(Math.max(0,seconds)*10);return String(Math.floor(ticks/600)).padStart(2,'0')+':'+String(Math.floor(ticks/10)%60).padStart(2,'0')+'.'+ticks%10;}
  function text(id,value){const el=byId(id);value=String(value);if(el.textContent!==value)el.textContent=value;}
  window.raptorTouch={
    connect(game){instance=game;configure();},
    update(next){
      const changed=state.phase!==next.phase || state.paused!==next.paused;
      state=next;if(changed)releaseAll();
      const intro=state.phase===4,garage=state.phase===0,modal=!intro && (state.paused || state.phase===3);
      document.body.classList.toggle("intro-pending",intro);
      document.documentElement.dataset.introActive=String(intro);
      byId("touch-intro").hidden=!intro;byId("intro-skip").hidden=!intro || !state.introSkipVisible;
      byId('touch-garage').hidden=!garage;byId('touch-race').hidden=garage || intro;byId('touch-modal').hidden=!modal;
      byId('touch-controller').hidden=modal || garage || intro;
      for(const button of holdButtons)button.disabled=!canDrive();
      root.querySelector('.touch-tools').hidden=modal;
      byId('touch-resume').hidden=!state.paused;byId('touch-again').hidden=state.paused;
      text('touch-modal-title',state.paused?'PAUSED':'FINISH / P'+state.rank);
      text('touch-result',state.paused?'Take a breath. The dirt can wait.':clock(state.time));
      text('touch-motion','Reduce motion: '+(state.reducedMotion?'on':'off'));
      text('touch-truck-name',trucks[state.selected]);text('touch-truck-description',descriptions[state.selected]);
      root.querySelectorAll('.truck-options button').forEach((b,i)=>b.setAttribute('aria-pressed',String(i===state.selected)));
      text('touch-rank',state.rank+' / 4');text('touch-lap',state.lap+' / 3');text('touch-time',clock(state.time));
      text('touch-speed',String(state.speed).padStart(2,'0'));text('touch-nitro',Math.round(state.nitro*100)+'%');
      text('touch-surface',state.offCourse?'RETURN TO TRACK':'');
      const camera=root.querySelector('[data-action="camera"]');
      const views=['Whole track','Follow truck','Driver seat'],mode=state.cameraMode??0;
      camera.textContent='View: '+views[mode];
      camera.setAttribute('aria-label','Camera: '+views[mode]+'. Switch to '+views[(mode+1)%3]);
      document.documentElement.dataset.cameraMode=String(mode);
      byId('touch-countdown').hidden=state.phase!==1 || state.paused;text('touch-countdown',Math.max(1,Math.ceil(state.countdown)));
    }
  };
  configure();
})();
