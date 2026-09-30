const fs=require('node:fs'),vm=require('node:vm'),path=require('node:path'),assert=require('node:assert/strict');
const M=require('../track-model.js'),source=fs.readFileSync(path.join(__dirname,'../track-builder.js'),'utf8');
const draftKey='raptor-track-draft-v1',libraryKey='raptor-track-library-v1';
function editor(storage=new Map(),fail=false){
  const nodes=new Map(),blobs=[];
  function el(id){if(!nodes.has(id))nodes.set(id,{id,hidden:false,disabled:false,value:'',textContent:'',dataset:{},events:{},children:[],classList:{toggle(){}},setAttribute(){},addEventListener(k,f){this.events[k]=f},querySelector:el,querySelectorAll(){return []},appendChild(b){this.children.push(b)},replaceChildren(...b){this.children=b},add(b){this.children.push(b)},focus(){},click(){},contains(){return true}});return nodes.get(id)}
  const document={getElementById:el,body:el('body'),querySelector:el,createElement:()=>el('created'+nodes.size)},window={RaptorTrackModel:M};
  const localStorage={getItem:k=>storage.get(k)||null,setItem(k,v){if(fail)throw Error('Quota exceeded');storage.set(k,v)}};
  vm.runInNewContext(source,{window,document,localStorage,Option:class{constructor(text,value){this.text=text;this.value=value}},Blob:class{constructor(parts){blobs.push(parts.join(''))}},URL:{createObjectURL:()=>'',revokeObjectURL(){}},setTimeout(){},clearTimeout(){},requestAnimationFrame(){},console});
  const click=id=>el(id).onclick(),tile=c=>el('track-grid').children[c].events.click({detail:0});
  window.raptorBuilder.update({phase:5,custom:false,name:'Test',error:''});
  return {el,click,tile,blobs,storage};
}
(async()=>{
const old=M.preset(),memory=new Map([[libraryKey,JSON.stringify([old])]]),e=editor(memory);
e.click('builder-new');assert(e.el('builder-save').disabled);assert(e.el('builder-export').disabled);
[0,1,2].forEach(e.tile);assert(!e.el('builder-save').disabled);assert(!e.el('builder-export').disabled);assert(e.el('builder-race').disabled);
e.el('builder-name').value='Work in progress';e.el('builder-name').events.input();e.click('builder-save');
let list=JSON.parse(memory.get(libraryKey));assert.equal(list.length,2);assert.deepEqual(list[0].cells,old.cells);assert.deepEqual(list[1].cells,[0,1,2]);assert.equal(list[1].closed,false);assert.match(e.el('builder-save-state').textContent,/Saved.*Work in progress/);assert.equal(e.el('builder-library').value,'1');
e.click('builder-export');const exported=e.blobs.at(-1);assert.equal(M.parseDraft(exported).closed,false);assert.deepEqual(M.parseDraft(exported).design.cells,[0,1,2]);assert.throws(()=>M.parse(exported));
const reopened=editor(memory);assert.equal(reopened.el('builder-count').textContent,'3 / 48 tiles');assert.equal(reopened.el('builder-loop').textContent,'Drawing circuit');assert.equal(reopened.el('builder-library').children.length,3);assert.match(reopened.el('builder-library').children[2].text,/Draft/);
reopened.tile(3);assert.equal(reopened.el('builder-count').textContent,'4 / 48 tiles','reloaded open draft stays in drawing mode');reopened.click('builder-undo');
reopened.click('builder-preset');reopened.el('builder-library').value='1';reopened.el('builder-library').onchange();assert.equal(reopened.el('builder-count').textContent,'3 / 48 tiles');assert(reopened.el('builder-race').disabled);reopened.tile(3);reopened.click('builder-save');assert.deepEqual(JSON.parse(memory.get(libraryKey))[1].cells,[0,1,2,3]);assert.equal(JSON.parse(memory.get(libraryKey)).length,2);
reopened.el('builder-file').files=[{size:exported.length,text:async()=>exported}];await reopened.el('builder-file').onchange();assert.equal(reopened.el('builder-count').textContent,'3 / 48 tiles');assert.equal(reopened.el('builder-loop').textContent,'Drawing circuit');
const invalidStart={...M.rotate({...old,pieces:old.pieces.map(()=>0)},2),closed:true};assert(M.validate(invalidStart));const invalid=editor(new Map([[draftKey,JSON.stringify(invalidStart)]]));assert(invalid.el('builder-race').disabled);assert(!invalid.el('builder-save').disabled);invalid.click('builder-save');assert.equal(JSON.parse(invalid.storage.get(libraryKey))[0].closed,true);
const full=editor(new Map([[libraryKey,JSON.stringify(Array.from({length:12},(_,i)=>({...old,name:'Course '+i})))]]));full.click('builder-save');assert.match(full.el('builder-status').textContent,/12 courses saved/);
const blocked=editor(new Map(),true);blocked.click('builder-save');assert.match(blocked.el('builder-status').textContent,/Storage is unavailable/);assert(!blocked.el('builder-export').disabled);blocked.click('builder-export');assert.equal(blocked.blobs.length,1);
for(const value of [null,{}, {design:old,closed:'false'},{...old,cells:[35],pieces:[0]},{...old,cells:[1.5],pieces:[0]},{...old,pieces:[9]},{...old,version:99}])assert.throws(()=>M.parseDraft(JSON.stringify(value)));
assert.deepEqual(M.parseDraft(JSON.stringify(old)),{design:old,closed:true});assert.deepEqual(M.parseDraft(JSON.stringify({design:old,closed:true})),{design:old,closed:true});assert.throws(()=>M.parseDraft('x'.repeat(8193)));
const malformed=editor(new Map([[libraryKey,JSON.stringify([null,{bad:true},old])]]));assert.equal(malformed.el('builder-library').children.length,2);
console.log('PASS: incomplete/invalid-start drafts save and survive reload; library selection, continued editing, update without losing other tracks, export/import round trip, legacy saves, malformed entries, full library, storage failure and race validation.');
})().catch(e=>{console.error(e);process.exitCode=1});
