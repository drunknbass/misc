const fs=require('node:fs'),vm=require('node:vm'),path=require('node:path'),assert=require('node:assert/strict');
const M=require('../track-model.js');
const source=fs.readFileSync(path.join(__dirname,'../track-builder.js'),'utf8');
const outer={version:2,name:'Keep my track',cells:[2,3,4,11,18,25,24,23,22,15,8,1],pieces:[0,0,0,3,0,0,0,4,0,0,0,0]};
function editor(design=outer){
 const nodes=new Map(),storage=new Map([['raptor-track-draft-v1',JSON.stringify({...design,closed:true})]]);
 function el(id){if(!nodes.has(id))nodes.set(id,{id,hidden:false,disabled:false,value:'',textContent:'',dataset:{},events:{},attrs:{},children:[],classList:{toggle(){}},setAttribute(k,v){this.attrs[k]=v},getAttribute(k){return this.attrs[k]},addEventListener(k,f){this.events[k]=f},querySelector:el,querySelectorAll(){return toolNodes},appendChild(b){this.children.push(b)},replaceChildren(...b){this.children=b},add(b){this.children.push(b)},focus(){},scrollIntoView(){},click(){},contains(){return true},setPointerCapture(){}});return nodes.get(id)}
 const toolNodes=['edit','route','start','0','1','3','4','5'].map(t=>{const b=el('tool-'+t);b.dataset.tool=t;return b});
 const document={getElementById:el,body:el('body'),querySelector:el,createElement:()=>el('created'+nodes.size)},window={RaptorTrackModel:M};
 vm.runInNewContext(source,{window,document,localStorage:{getItem:k=>storage.get(k)||null,setItem:(k,v)=>storage.set(k,v)},Option:class{},setTimeout(){},clearTimeout(){},requestAnimationFrame(){},console});
 window.raptorBuilder.update({phase:5,custom:false,name:'Test',error:''});
 return {el,storage,click:id=>el(id).onclick(),tile:c=>el('track-grid').children[c].events.click({detail:0}),tool:t=>el('tool-'+t).events.click(),read:()=>JSON.parse(storage.get('raptor-track-draft-v1')).design};
}
// Replace the corner 4 with the inside corner 10, without touching either other arc or start.
const e=editor();e.tile(3);e.tile(11);assert(!e.el('builder-edit-actions').hidden);assert(e.el('builder-save').disabled);assert.equal(e.el('builder-race').textContent,'FINISH ROAD EDIT');e.tile(10);e.tile(11);assert(!e.el('builder-apply-road').disabled);assert.deepEqual(e.read().cells,outer.cells,'drawing does not change the saved circuit');e.click('builder-apply-road');const changed=e.read();assert.deepEqual(changed.cells,[2,3,10,11,18,25,24,23,22,15,8,1]);assert.equal(changed.name,outer.name);assert.equal(changed.pieces[changed.cells.indexOf(23)],4,'terrain on untouched road survives');assert.equal(changed.pieces[changed.cells.indexOf(11)],3);assert.equal(changed.pieces[changed.cells.indexOf(10)],0);assert.equal(changed.cells[0],2);e.click('builder-undo');assert.deepEqual(e.read().cells,outer.cells);e.click('builder-redo');assert.deepEqual(e.read(),changed);
// Cancel leaves circuit and undo history intact; touching old road never silently trims it.
e.tile(3);e.tile(11);e.tile(4);e.click('builder-cancel-road');assert.deepEqual(e.read(),changed);e.click('builder-undo');assert.deepEqual(e.read().cells,outer.cells);
// A section across the original start still joins correctly and preserves the retained arc.
const wrapped=M.replaceSection(outer,10,2,[8,9,10,3,4]);assert.deepEqual(wrapped.cells,[4,11,18,25,24,23,22,15,8,9,10,3]);assert.equal(wrapped.pieces[wrapped.cells.indexOf(23)],4);assert(wrapped.cells.every((c,i)=>M.adjacent(c,wrapped.cells[(i+1)%wrapped.cells.length])));
assert.throws(()=>M.replaceSection(outer,1,3,[3,17,11]),/adjacent/);assert.throws(()=>M.replaceSection(outer,1,3,[3,10,3,4,11]),/doubling/);assert.throws(()=>M.replaceSection(outer,1,3,[3,10]),/all the way/);assert.throws(()=>M.replaceSection(outer,-1,3,[3,11]),/two different/);
const c=editor();c.tile(3);c.tile(11);c.tile(24);assert.match(c.el('builder-status').textContent,/beside/);c.tile(10);c.tile(3);assert(c.el('builder-apply-road').disabled,'backtracking removes the drawn path');c.tool('1');assert.match(c.el('builder-status').textContent,/Apply or cancel/);c.click('builder-undo');assert(c.el('builder-edit-actions').hidden);assert.deepEqual(c.read().cells,outer.cells);
// Existing terrain can be replaced and cleared, including imported reserved crossover terrain.
c.tool('4');c.tile(15);assert.equal(c.read().pieces[9],4);c.tool('0');c.tile(15);assert.equal(c.read().pieces[9],0);
const cross=M.figureEight();cross.pieces[5]=1;const x=editor(cross);x.tool('0');x.tile(cross.cells[5]);assert.equal(x.read().pieces[5],0);assert.equal(M.validate(x.read()),'');
// Valid start selection is independent of unrelated circuit errors.
const bad={...outer,pieces:[...outer.pieces]};bad.pieces[2]=1;const start=editor(bad);start.tool('start');start.tile(23);assert.equal(start.read().cells[0],2);start.tool('0');start.tile(23);start.tool('start');start.tile(23);assert.equal(start.read().cells[0],23);assert(M.validate(start.read()),'unrelated corner issue remains visible');
console.log('PASS: section replacement, retained terrain, start preservation, wrapped sections, undo/redo, cancel, backtracking, invalid paths, terrain repainting and independent start-line repairs.');
