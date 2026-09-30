(() => {
  'use strict';
  const M=window.RaptorTrackModel,$=id=>document.getElementById(id),panel=$('track-builder'),grid=$('track-grid');
  const names=['Flat','Jump','Tabletop','Rollers','Mud','Nitro pad'],icons=['━','▲','▰','≋','∷','ϟ'],colors=['#ccb68d','#edbd76','#edbd76','#edbd76','#9b6a40','#7ee6ed'];
  const draftKey='raptor-track-draft-v1',libraryKey='raptor-track-library-v1';
  let design=M.preset(),closed=true,tool=1,history=[],redo=[],instance=null,phase=4,pending=false,pendingTimer,pendingAction=null,dispatched=false,operation=0,lastState={custom:false},drag=null,lastCell=-1,library=[];
  const send=(method,value)=>{if(instance)instance.SendMessage('Raptor Rally',method,...(value===undefined?[]:[value]));};
  function message(text,error=false){$('builder-status').textContent=text;$('builder-status').dataset.error=String(error);}
  function snapshot(){return JSON.stringify({design,closed});}
  function remember(){history.push(snapshot());if(history.length>60)history.shift();redo=[];}
  function store(){try{localStorage.setItem(draftKey,snapshot());$('builder-save-state').textContent='Draft saved on this device';}catch{$('builder-save-state').textContent='Storage unavailable — export to keep this course';}}
  function error(){const issue=M.diagnose(design,closed)[0];return issue?issue.title+'. '+issue.message:'';}
  function ready(){const e=error();message((e?'Fix the highlighted track issues above before racing.':'')||(tool==='route'?'Circuit closed. Choose a piece to add terrain, or tap a route tile to trim.':tool==='start'?'Choose a flat straight with another flat straight behind it.':'Ready to race. Tap a straight tile to place a piece.'),!!e);}
  function savedDraft(){const d=M.parseDraft(snapshot());return {...d.design,closed:d.closed};}
  function refreshLibrary(selected=''){const select=$('builder-library');select.replaceChildren(new Option('Load saved course…',''));library.forEach((d,i)=>select.add(new Option(d.name+(!d.closed||M.validate(d)?' · Draft':''),String(i))));select.value=selected;}
  try{const raw=localStorage.getItem(draftKey);if(raw){const d=M.parseDraft(raw);design=d.design;closed=d.closed;tool=closed?1:'route';}}catch{}
  try{const list=JSON.parse(localStorage.getItem(libraryKey));if(Array.isArray(list))for(const item of list.slice(0,12)){try{const d=M.parseDraft(JSON.stringify(item));library.push({...d.design,closed:d.closed});}catch{}}}catch{}
  refreshLibrary();
  const cells=[];
  for(let c=0;c<35;c++){
    const b=document.createElement('button');b.className='track-cell';b.dataset.cell=c;b.type='button';
    b.addEventListener('click',e=>{if(e.detail===0){remember();edit(c);store();render();}});
    b.addEventListener('keydown',e=>{const offsets={ArrowLeft:-1,ArrowRight:1,ArrowUp:-7,ArrowDown:7};if(e.key in offsets){e.preventDefault();const next=c+offsets[e.key];if(next>=0&&next<35&&M.adjacent(c,next))cells[next].focus();}});
    cells.push(b);grid.appendChild(b);
  }
  function render(){
    $('builder-name').value=design.name;
    const upper=new Set();for(const v of M.crossings(design))for(const i of v)if(Math.abs(design.cells[(i+1)%design.cells.length]-design.cells[i])===1)for(let k=-1;k<=1;k++)upper.add((i+k+design.cells.length)%design.cells.length);
    const issues=M.diagnose(design,closed),badTiles=new Set(issues.flatMap(issue=>issue.cells));
    const checkSummary=issues.length?issues.length+' '+(issues.length===1?'issue':'issues')+' to fix before racing':'Ready to race';
    $('builder-check-summary').textContent=checkSummary;
    $('builder-checks').dataset.error=String(issues.length>0);
    $('builder-check-help').textContent=issues.length?'Orange outlines mark the tiles to change. R = row from the top; C = column from the left. Saving keeps your draft.':'Your loop and starting grid pass all racing checks.';
    const list=$('builder-issues');list.replaceChildren();
    issues.forEach(issue=>{
      const item=document.createElement('li'),title=document.createElement('strong'),detail=document.createElement('p');
      title.textContent=issue.title;detail.textContent=issue.message;item.appendChild(title);item.appendChild(detail);list.appendChild(item);
    });
    $('builder-race').textContent=issues.length?'CHECK '+issues.length+' TRACK '+(issues.length===1?'ISSUE':'ISSUES'):'RACE THIS TRACK →';
    $('builder-race').dataset.blocked=String(issues.length>0);
    $('builder-race').disabled=pending;$('builder-export').disabled=!design.cells.length||pending;$('builder-save').disabled=!design.cells.length||pending;
    $('builder-undo').disabled=!history.length;$('builder-redo').disabled=!redo.length;
    $('builder-count').textContent=design.cells.length+' / 48 tiles';$('builder-loop').textContent=closed?'Closed circuit':'Drawing circuit';
    panel.querySelectorAll('[data-tool]').forEach(b=>b.setAttribute('aria-pressed',String(String(tool)===b.dataset.tool)));
    cells.forEach((b,c)=>{
      const i=design.cells.indexOf(c),on=i>=0,kind=on?design.pieces[i]:0,cross=on&&design.cells.lastIndexOf(c)!==i;
      b.classList.toggle('track-problem',badTiles.has(c));
      b.title=issues.filter(issue=>issue.cells.includes(c)).map(issue=>issue.title+'. '+issue.message).join(' ');
      b.classList.toggle('on-route',on);b.classList.toggle('start-cell',i===0);b.classList.toggle('route-end',on&&!closed&&design.cells.lastIndexOf(c)===design.cells.length-1);
      b.setAttribute('aria-label','Row '+(Math.floor(c/7)+1)+', column '+(c%7+1)+(on?', '+(i===0?'start, ':'')+(cross?'open jump, east–west launches across north–south':upper.has(i)?'dirt jump ramp':names[kind]):', empty'));
      let drawing='<circle cx="32" cy="32" r="1.5" fill="#36515b"/>';
      if(on){
        const previous=i>0?design.cells[i-1]:closed?design.cells.at(-1):null,next=i<design.cells.length-1?design.cells[i+1]:closed?design.cells[0]:null;
        const edge=n=>n===null?[32,32]:[32+(n%7-c%7)*32,32+(Math.floor(n/7)-Math.floor(c/7))*32];
        const a=edge(previous),z=edge(next),path=`M${a} Q32,32 ${z}`;
        drawing=`<path d="${path}" fill="none" stroke="#6f6853" stroke-width="29"/><path d="${path}" fill="none" stroke="${upper.has(i)?colors[1]:colors[kind]}" stroke-opacity=".7" stroke-width="22"/>`;
        if(i===0){const arrow=next===null?'→':next===c+1?'→':next===c-1?'←':next>c?'↓':'↑';drawing+=`<rect x="20" y="23" width="24" height="17" rx="3" fill="#c8f794"/><text x="32" y="35" fill="#102018" text-anchor="middle" font-size="11" font-weight="900">S ${arrow}</text>`;}
        else if(upper.has(i))drawing+=`<path transform="${next<c?'translate(64 0) scale(-1 1)':''}" d="M21,22 L31,32 L21,42 M33,22 L43,32 L33,42" fill="none" stroke="#644722" stroke-width="3"/>`;
        else if(kind)drawing+=`<text x="32" y="38" fill="#06161a" text-anchor="middle" font-size="25" font-weight="900">${icons[kind]}</text>`;
      }
      if(cross)drawing='<path d="M32,0 V64" stroke="#ccb68d" stroke-width="22"/><path d="M0,32 H13 M51,32 H64" stroke="#edbd76" stroke-width="22"/><path d="M8,25 Q32,1 56,25" fill="none" stroke="#edbd76" stroke-width="3" stroke-dasharray="4 3"/><path d="M13,20 V44 M51,20 V44" stroke="#644722" stroke-width="3"/>';
      b.innerHTML=`<svg aria-hidden="true" viewBox="0 0 64 64">${drawing}</svg>`+(badTiles.has(c)?`<span class="tile-issue" aria-hidden="true">${M.coordinate(c)} !</span>`:'');
      if(badTiles.has(c))b.setAttribute('aria-label',b.getAttribute('aria-label')+'. Needs repair. '+b.title);
    });
  }
  function edit(c){
    const i=design.cells.indexOf(c);
    if(tool==='route'){
      const end=design.cells.at(-1),before=design.cells.at(-2);
      if(!closed&&design.cells.indexOf(end)!==design.cells.length-1&&c!==2*end-before){message('Continue straight through the crossover junction.',true);return;}
      if(!closed&&i>0&&i<design.cells.length-2&&design.cells.lastIndexOf(c)===i&&M.adjacent(end,c)&&M.straight(design.cells,i)&&Math.abs(end-c)!==Math.abs(design.cells[i-1]-c)){
        if(design.cells.length>=48){message('Maximum 48 route tiles.',true);return;}
        design.version=2;design.cells.push(c);design.pieces.push(0);message('Crossover added. Continue straight; keep one straight approach tile on each side.');return;
      }
      if(i===0&&!closed&&design.cells.length>=8&&M.adjacent(design.cells.at(-1),c)){
        closed=true;
        const reserved=M.reserved(design);const start=design.cells.findIndex((_,n)=>!reserved.has(n)&&!reserved.has((n+design.cells.length-1)%design.cells.length)&&M.straight(design.cells,n)&&M.straight(design.cells,(n+design.cells.length-1)%design.cells.length));
        if(start>=0){design=M.rotate(design,start);design.pieces[0]=design.pieces[design.pieces.length-1]=0;tool=1;}
        ready();return;
      }
      if(i>=0){design.cells=design.cells.slice(0,i+1);design.pieces=design.pieces.slice(0,i+1);closed=false;message('Route trimmed. Continue from the highlighted end tile.');return;}
      if(design.cells.length>=48){message('Maximum 48 tiles. Tap the first tile to close the loop.',true);return;}
      if(design.cells.length&&!M.adjacent(design.cells.at(-1),c)){message('Add a tile beside the highlighted end of the route.',true);return;}
      if(closed){message('Use New loop to redraw, or tap a route tile to trim it.',true);return;}
      design.version=2;design.cells.push(c);design.pieces.push(0);message('Continue from the highlighted tile. Tap the first tile to close.');return;
    }
    if(!closed){message('Close your loop before placing pieces.',true);return;}
    if(i<0){message('Choose a tile on the circuit.',true);return;}
    if(tool==='start'){
      const d=M.rotate(design,i);if(M.validate(d)){message('Choose a flat straight with another flat straight behind it.',true);return;}
      design=d;message('Start line moved. The arrow follows the route you drew.');return;
    }
    if(M.reserved(design).has(i)){message('Takeoff and landing ramps are automatic. Keep this junction and its approaches clear.',true);return;}
    if(tool!==0&&(!M.straight(design.cells,i)||i===0||i===design.cells.length-1)){message('Keep corners and the two starting tiles flat.',true);return;}
    design.pieces[i]=tool;message(names[tool]+' placed. '+(tool===4?'Mud slows trucks down.':tool===5?'Drive over this pad to refill nitro.':'Race to try your changes.'));
  }
  grid.addEventListener('pointerdown',e=>{const b=e.target.closest('[data-cell]');if(!b||pending||e.pointerType==='mouse'&&e.button!==0)return;e.preventDefault();drag=e.pointerId;lastCell=Number(b.dataset.cell);remember();grid.setPointerCapture(drag);edit(lastCell);store();render();});
  grid.addEventListener('pointermove',e=>{if(e.pointerId!==drag)return;e.preventDefault();const b=document.elementFromPoint(e.clientX,e.clientY)?.closest('[data-cell]');if(!b||!grid.contains(b))return;const c=Number(b.dataset.cell);if(c===lastCell)return;lastCell=c;edit(c);store();render();});
  for(const type of ['pointerup','pointercancel','lostpointercapture'])grid.addEventListener(type,()=>{drag=null;lastCell=-1;});
  panel.querySelectorAll('[data-tool]').forEach(b=>b.addEventListener('click',()=>{tool=isNaN(Number(b.dataset.tool))?b.dataset.tool:Number(b.dataset.tool);render();message(tool==='route'?'Tap or drag adjacent tiles. Cross an existing straight at right angles to add a jump junction; other existing tiles trim the route.':tool==='start'?'Tap a flat straight with another flat straight behind it.':'Tap or drag on straight tiles to place '+names[tool].toLowerCase()+'.');}));
  function restore(raw){const d=JSON.parse(raw);design=d.design;closed=d.closed;store();render();ready();}
  $('builder-undo').onclick=()=>{if(!history.length)return;redo.push(snapshot());restore(history.pop());};
  $('builder-redo').onclick=()=>{if(!redo.length)return;history.push(snapshot());restore(redo.pop());};
  $('builder-new').onclick=()=>{remember();design={version:2,name:'My dirt circuit',cells:[],pieces:[]};closed=false;tool='route';store();render();message('Tap any tile to start, then draw through adjacent tiles. Undo brings the previous course back.');};
  $('builder-preset').onclick=()=>{remember();design=M.preset();closed=true;tool=1;store();render();ready();};
  $('builder-eight').onclick=()=>{remember();design=M.figureEight();closed=true;tool='route';store();render();message('Jump ready: carry speed off the dirt ramp, fly across the open junction, and land on the far ramp.');};
  $('builder-name').addEventListener('focus',remember);
  $('builder-name').addEventListener('input',()=>{design.name=M.cleanName($('builder-name').value);store();$('builder-undo').disabled=!history.length;});
  $('builder-name').addEventListener('change',()=>{design.name=M.cleanName($('builder-name').value);store();render();});
  $('builder-save').onclick=()=>{
    if(pending||!design.cells.length)return;
    try{
      const copy=savedDraft(),index=library.findIndex(d=>d.name===copy.name),next=library.slice();
      if(index>=0)next[index]=copy;else if(next.length<12)next.push(copy);else {message('12 courses saved. Export a course or reuse a saved name.',true);return;}
      localStorage.setItem(libraryKey,JSON.stringify(next));library=next;store();refreshLibrary(String(index>=0?index:next.length-1));
      const feedback='Saved “'+copy.name+'” on this device.'+(error()?' Draft kept — finish the circuit before racing.':'');
      $('builder-save-state').textContent=feedback;message(feedback);
    }catch{message('Storage is unavailable. Export the course instead.',true);}
  };
  $('builder-library').onchange=()=>{const d=library[Number($('builder-library').value)];if(!d||$('builder-library').value==='')return;remember();const restored=M.parseDraft(JSON.stringify(d));design=restored.design;closed=restored.closed;tool=closed?1:'route';store();render();ready();};
  $('builder-export').onclick=()=>{if(pending||!design.cells.length)return;const blob=new Blob([JSON.stringify(savedDraft(),null,2)],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=M.cleanName(design.name).replace(/ /g,'-')+'.raptor.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);message(error()?'Draft exported. Import it to continue editing.':'Course exported. Import the JSON file on another device to race it.');};
  $('builder-import').onclick=()=>$('builder-file').click();
  $('builder-file').onchange=async()=>{const file=$('builder-file').files[0];if(!file)return;try{if(file.size>8192)throw Error('Course files must be smaller than 8 KB.');const d=M.parseDraft(await file.text());remember();design=d.design;closed=d.closed;tool=closed?1:'route';store();render();ready();}catch(e){message(e.message||'Could not read course.',true);}finally{$('builder-file').value='';}};
  function sync(){
    const shown=phase===5||pendingAction==='open';
    panel.hidden=!shown;document.body.classList.toggle('builder-open',shown);
    $('stage').inert=shown;document.querySelector('header').inert=shown;
    panel.setAttribute('aria-busy',String(pending));
    panel.querySelector('.builder-body').inert=pending;panel.querySelector('.builder-footer').inert=pending;
    $('builder-close').disabled=pending;$('builder-original').disabled=pending;
    $('builder-busy').hidden=!pending;
    $('open-builder').disabled=!instance||phase===4||pending;
    $('open-builder').setAttribute('aria-busy',String(pendingAction==='open'));
    $('open-builder').textContent=pendingAction==='open'?'Opening…':lastState.custom?'Edit track':'Track builder';
  }
  function finish(errorText){
    pending=false;pendingAction=null;operation++;clearTimeout(pendingTimer);render();sync();
    if(errorText){message(errorText,true);if(phase!==5){$('builder-feedback').textContent=errorText;$('builder-feedback').hidden=false;$('open-builder').focus();}}
  }
  function request(method,value,action,title,detail){
    if(pending||!instance||phase===4)return;
    pending=true;pendingAction=action;dispatched=false;const id=++operation;
    $('builder-feedback').hidden=true;$('builder-busy-title').textContent=title;$('builder-busy-detail').textContent=detail;
    render();sync();message(title);
    pendingTimer=setTimeout(()=>{if(id===operation)finish('The game did not respond. Please try again.');},20000);
    // Give the browser a full paint before synchronous Unity terrain work can block its main thread.
    requestAnimationFrame(()=>requestAnimationFrame(()=>{
      if(id!==operation)return;
      try{dispatched=true;send(method,value);}catch{finish('Could not reach the game. Please try again.');}
    }));
  }
  function race(method,value){request(method,value,'race','Building your circuit…','Shaping the dirt, jumps and racing line.');}
  $('builder-race').onclick=()=>{
    if(pending)return;
    if(error()){const checks=$('builder-checks');checks.scrollIntoView({block:'center',behavior:'auto'});checks.focus();return;}
    store();race('BuildCustomTrack',JSON.stringify(design));
  };
  $('builder-original').onclick=()=>{if(!pending)race('RaceOriginalTrack');};
  $('builder-close').onclick=()=>request('CloseTrackBuilder',undefined,'close','Returning to garage…','Getting your Raptor ready.');
  $('open-builder').onclick=()=>request('OpenTrackBuilder',undefined,'open','Opening track builder…','Getting your saved circuit ready.');
  panel.addEventListener('keydown',e=>{if(e.key==='Escape'&&!pending){e.preventDefault();$('builder-close').onclick();}});
  window.raptorBuilder={
    connect(game){instance=game;sync();},
    update(next){
      const opening=phase!==5&&next.phase===5,leaving=phase===5&&next.phase!==5;phase=next.phase;lastState=next;
      if(pending&&dispatched&&(next.error||(pendingAction==='open'?phase===5:phase!==5)))finish(next.error);
      sync();
      const course=document.querySelector('.garage-heading .eyebrow');if(course)course.textContent=next.name+' / 3 LAPS';
      if(opening){render();ready();$('builder-close').focus();}
      if(next.error&&!pending){render();message(next.error,true);}
      if(leaving)$('unity-canvas').focus();
    }
  };
  render();
})();
