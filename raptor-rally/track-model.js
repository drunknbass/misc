/* Shared, dependency-free course rules for the editor and its Node checks. */
(function(root){
  'use strict';
  const adjacent=(a,b)=>Math.abs(a%7-b%7)+Math.abs(Math.floor(a/7)-Math.floor(b/7))===1;
  const straight=(cells,i)=>cells[(i+cells.length-1)%cells.length]+cells[(i+1)%cells.length]===2*cells[i];
  const cleanName=name=>String(name||'My dirt circuit').replace(/[^\p{L}\p{N} _-]/gu,'').trim().slice(0,28)||'My dirt circuit';
  function validate(d){
    if(!d||d.version!==1)return 'This course uses an unsupported file version.';
    if(!Array.isArray(d.cells)||d.cells.length<8||d.cells.length>34)return 'Draw a loop of 8 to 34 connected tiles.';
    if(!Array.isArray(d.pieces)||d.pieces.length!==d.cells.length)return 'Each track tile needs a piece.';
    const used=new Set();
    for(let i=0;i<d.cells.length;i++){
      const c=d.cells[i],p=d.pieces[i];
      if(!Number.isInteger(c)||c<0||c>=35||used.has(c))return 'Keep the circuit on the grid without crossing itself.';
      used.add(c);
      if(!adjacent(c,d.cells[(i+1)%d.cells.length]))return 'Connect the last tile back to the start to close your loop.';
      if(!Number.isInteger(p)||p<0||p>5)return 'Unknown track piece.';
      if(p!==0&&!straight(d.cells,i))return 'Jumps and surface pieces belong on straight tiles.';
    }
    if(!straight(d.cells,0)||!straight(d.cells,d.cells.length-1)||d.pieces[0]!==0||d.pieces.at(-1)!==0)return 'The start needs two flat straight tiles in a row. Use Start line to move it.';
    return '';
  }
  function parse(raw){if(raw.length>8192)throw Error('Course files must be smaller than 8 KB.');const d=JSON.parse(raw),error=validate(d);if(error)throw Error(error);return {version:1,name:cleanName(d.name),cells:[...d.cells],pieces:[...d.pieces]};}
  function preset(){return {version:1,name:'Raptor rhythm',cells:[24,25,26,19,12,11,10,9,8,15,22,23],pieces:[0,1,0,4,0,2,0,5,0,3,0,0]};}
  function rotate(d,index){return {...d,cells:d.cells.slice(index).concat(d.cells.slice(0,index)),pieces:d.pieces.slice(index).concat(d.pieces.slice(0,index))};}
  const api={adjacent,straight,cleanName,validate,parse,preset,rotate};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.RaptorTrackModel=api;
})(typeof window==='undefined'?this:window);
