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
  function parse(raw){if(raw.length>8192)throw Error('Course files must be smaller than 8 KB.');const d=JSON.parse(raw),error=validate(d);if(error)throw Error(error);return {version:d.version,name:cleanName(d.name),cells:[...d.cells],pieces:[...d.pieces]};}
  function preset(){return {version:1,name:'Raptor rhythm',cells:[24,25,26,19,12,11,10,9,8,15,22,23],pieces:[0,1,0,4,0,2,0,5,0,3,0,0]};}
  function figureEight(){return {version:2,name:'Junction jump',cells:[29,28,21,14,15,16,17,18,19,20,13,6,5,4,3,10,17,24,31,30],pieces:Array(20).fill(0)};}
  function rotate(d,index){return {...d,cells:d.cells.slice(index).concat(d.cells.slice(0,index)),pieces:d.pieces.slice(index).concat(d.pieces.slice(0,index))};}
  const api={adjacent,straight,cleanName,validate,parse,preset,figureEight,crossings,reserved,rotate};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.RaptorTrackModel=api;
})(typeof window==='undefined'?this:window);
