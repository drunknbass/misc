/* Shared, dependency-free course rules for the editor and its Node checks. */
(function(root){
  'use strict';
  const adjacent=(a,b)=>Math.abs(a%7-b%7)+Math.abs(Math.floor(a/7)-Math.floor(b/7))===1;
  const straight=(cells,i)=>cells[(i+cells.length-1)%cells.length]+cells[(i+1)%cells.length]===2*cells[i];
  const cleanName=name=>String(name||'My dirt circuit').replace(/[^\p{L}\p{N} _-]/gu,'').trim().slice(0,28)||'My dirt circuit';
  function crossings(d){
    const visits=new Map();d.cells.forEach((c,i)=>{if(!visits.has(c))visits.set(c,[]);visits.get(c).push(i);});
    return [...visits.values()].filter(v=>v.length>1);
  }
  function reserved(d){const r=new Set(),n=d.cells.length;for(const v of crossings(d))for(const i of v)for(let k=-1;k<=1;k++)r.add((i+k+n)%n);return r;}
  function validate(d){
    if(!d||![1,2].includes(d.version))return 'This course uses an unsupported file version.';
    const limit=d.version===1?34:48;
    if(!Array.isArray(d.cells)||d.cells.length<8||d.cells.length>limit)return `Draw a loop of 8 to ${limit} connected tiles.`;
    if(!Array.isArray(d.pieces)||d.pieces.length!==d.cells.length)return 'Each track tile needs a piece.';
    for(let i=0;i<d.cells.length;i++){
      const c=d.cells[i],p=d.pieces[i];
      if(!Number.isInteger(c)||c<0||c>=35)return 'Keep the circuit on the grid.';
      if(!adjacent(c,d.cells[(i+1)%d.cells.length]))return 'Connect every tile, including the last tile back to the start.';
      if(!Number.isInteger(p)||p<0||p>5)return 'Unknown track piece.';
      if(p!==0&&!straight(d.cells,i))return 'Jumps and surface pieces belong on straight tiles.';
    }
    const used=new Set(),n=d.cells.length;
    for(const visits of crossings(d)){
      if(d.version===1||visits.length!==2)return 'A crossover can carry two perpendicular straights only.';
      const [a,b]=visits,axis=i=>Math.abs(d.cells[(i+1)%n]-d.cells[i]);
      if(!straight(d.cells,a)||!straight(d.cells,b)||axis(a)===axis(b))return 'Cross straight through a junction at right angles; do not turn or retrace there.';
      for(const center of visits)for(let k=-1;k<=1;k++){
        const i=(center+k+n)%n;
        if(!straight(d.cells,i)||d.pieces[i])return 'Keep each crossover and its approaches straight and flat. The jump is added automatically.';
        if(used.has(i)||k!==0&&d.cells.indexOf(d.cells[i])!==d.cells.lastIndexOf(d.cells[i]))return 'Space crossover junctions farther apart.';
        used.add(i);
      }
    }
    if(!straight(d.cells,0)||!straight(d.cells,n-1)||d.pieces[0]||d.pieces[n-1]||used.has(0)||used.has(n-1))return 'The start needs two flat straight tiles away from crossover ramps.';
    return '';
  }
  const coordinate=c=>'R'+(Math.floor(c/7)+1)+' C'+(c%7+1);
  function diagnose(d,closed=true){
    const issues=[],add=(code,title,message,cells=[])=>issues.push({code,title,message,cells:[...new Set(cells)]});
    if(!closed){
      const cells=d&&Array.isArray(d.cells)?d.cells:[];
      add('open-loop','Finish the loop',cells.length?'Choose Draw route. Continue from '+coordinate(cells.at(-1))+' through adjacent tiles, then tap the start at '+coordinate(cells[0])+'. You can still save this draft.':'Choose Draw route and add at least 8 adjacent tiles, then tap the first tile to close the loop.',cells.length?[cells[0],cells.at(-1)]:[]);
      return issues;
    }
    const problem=validate(d);if(!problem)return issues;
    try{parseDraft(JSON.stringify(d));}catch{add('invalid-file','Course data needs repair',problem);return issues;}
    const n=d.cells.length;
    if(n<8||n>(d.version===1?34:48)){add('tile-count','Check the circuit length',problem,d.cells);return issues;}
    for(let i=0;i<n;i++){
      const c=d.cells[i],next=d.cells[(i+1)%n];
      if(!adjacent(c,next))add('disconnected','Connect '+coordinate(c)+' to '+coordinate(next),'Use Draw route to connect these tiles through adjacent grid squares.',[c,next]);
      if(d.pieces[i]&&!straight(d.cells,i))add('corner-piece','Remove the piece at '+coordinate(c),'This tile is a corner. Choose Flat and tap it to remove the terrain piece, or redraw it as a straight.',[c]);
    }
    const used=new Set();
    for(const visits of crossings(d)){
      const junction=d.cells[visits[0]],label=coordinate(junction),axis=i=>Math.abs(d.cells[(i+1)%n]-d.cells[i]);
      if(d.version===1||visits.length!==2||visits.some(i=>!straight(d.cells,i))||axis(visits[0])===axis(visits[1])){
        add('crossing-shape','Redraw the crossing at '+label,'Only two straight routes may cross here, at right angles. Use Draw route to remove any turn or retraced section.',[junction]);continue;
      }
      const indices=visits.flatMap(i=>[-1,0,1].map(k=>(i+k+n)%n));
      const turns=indices.filter(i=>!straight(d.cells,i)).map(i=>d.cells[i]),terrain=indices.filter(i=>d.pieces[i]).map(i=>d.cells[i]);
      if(turns.length||terrain.length){
        const instructions=[];
        if(turns.length)instructions.push([...new Set(turns)].map(coordinate).join(', ')+' turn too close to the crossing. Use Draw route to move these corners at least one tile farther away. Flat changes terrain; it does not straighten a corner.');
        if(terrain.length)instructions.push('Choose Flat and clear the terrain pieces at '+[...new Set(terrain)].map(coordinate).join(', ')+'. The crossing adds its own ramps.');
        add('crossover-approach','Crossover at '+label+' needs straight, flat approaches',instructions.join(' '),turns.concat(terrain));
      }
      const crowded=indices.filter(i=>used.has(i)||!visits.includes(i)&&d.cells.indexOf(d.cells[i])!==d.cells.lastIndexOf(d.cells[i]));
      if(crowded.length)add('crossing-spacing','Move the crossing at '+label,'Its approaches overlap another crossing. Use Draw route to put at least one separate straight approach tile on each side.',[junction,...crowded.map(i=>d.cells[i])]);
      indices.forEach(i=>used.add(i));
    }
    if(!straight(d.cells,0)||!straight(d.cells,n-1)||d.pieces[0]||d.pieces[n-1]||used.has(0)||used.has(n-1))add('start-line','Move or clear the start line','The start at '+coordinate(d.cells[0])+' and the tile behind it at '+coordinate(d.cells[n-1])+' must both be flat straights away from crossings. Clear terrain with Flat, then use Start line to choose a valid pair.',[d.cells[0],d.cells[n-1]]);
    if(!issues.length)add('course-rule','Fix the circuit before racing',problem);
    return issues;
  }
  function parse(raw){if(raw.length>8192)throw Error('Course files must be smaller than 8 KB.');const d=JSON.parse(raw),error=validate(d);if(error)throw Error(error);return {version:d.version,name:cleanName(d.name),cells:[...d.cells],pieces:[...d.pieces]};}
  // Saving an editable draft must not require a race-ready circuit.
  function parseDraft(raw){
    if(typeof raw!=='string'||raw.length>8192)throw Error('Course files must be smaller than 8 KB.');
    const value=JSON.parse(raw),wrapped=value&&Object.prototype.hasOwnProperty.call(value,'design'),d=wrapped?value.design:value;
    const closed=value&&value.closed!==undefined?value.closed:true;
    if(!d||![1,2].includes(d.version)||typeof closed!=='boolean')throw Error('Invalid course draft.');
    if(!Array.isArray(d.cells)||d.cells.length>48||!Array.isArray(d.pieces)||d.pieces.length!==d.cells.length)throw Error('Each track tile needs a piece; use at most 48 tiles.');
    if(!d.cells.every(c=>Number.isInteger(c)&&c>=0&&c<35)||!d.pieces.every(p=>Number.isInteger(p)&&p>=0&&p<=5))throw Error('Invalid track tile or piece.');
    return {design:{version:d.version,name:cleanName(d.name),cells:[...d.cells],pieces:[...d.pieces]},closed};
  }
  function preset(){return {version:1,name:'Raptor rhythm',cells:[24,25,26,19,12,11,10,9,8,15,22,23],pieces:[0,1,0,4,0,2,0,5,0,3,0,0]};}
  function figureEight(){return {version:2,name:'Junction jump',cells:[29,28,21,14,15,16,17,18,19,20,13,6,5,4,3,10,17,24,31,30],pieces:Array(20).fill(0)};}
  function rotate(d,index){return {...d,cells:d.cells.slice(index).concat(d.cells.slice(0,index)),pieces:d.pieces.slice(index).concat(d.pieces.slice(0,index))};}
  const api={adjacent,straight,cleanName,validate,diagnose,coordinate,parse,parseDraft,preset,figureEight,crossings,reserved,rotate};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.RaptorTrackModel=api;
})(typeof window==='undefined'?this:window);
