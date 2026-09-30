const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const nodes=new Map(),sent=[];
function node(id){if(!nodes.has(id))nodes.set(id,{hidden:false,dataset:{},style:{},textContent:'',listeners:{},classList:{toggle(){}},setAttribute(){},addEventListener(k,f){this.listeners[k]=f;},querySelector(s){return node(s);},querySelectorAll(){return [];}});return nodes.get(id);}
const root=node('touch-ui'),skip=node('intro-skip');skip.dataset.action='skip';
root.querySelectorAll=s=>s==='[data-action]'?[skip]:[];
const document={getElementById:node,body:node('body'),documentElement:node('html'),addEventListener(){}};
const window={location:{search:''}};
vm.runInNewContext(fs.readFileSync(require('node:path').join(__dirname,'../touch-controls.js'),'utf8'),{document,window,Image:class {},matchMedia:()=>({matches:true,addEventListener(){}}),navigator:{maxTouchPoints:1},innerWidth:844,addEventListener(){},URLSearchParams,Map,Math,String,Number});
window.raptorTouch.connect({SendMessage(...args){sent.push(args);}});
const state={phase:4,paused:false,selected:0,rank:1,lap:1,time:0,speed:0,nitro:1,countdown:0,introSkipVisible:false};
window.raptorTouch.update(state);
assert.equal(node('touch-garage').hidden,true);assert.equal(node('touch-race').hidden,true);assert.equal(node('touch-controller').hidden,true);assert.equal(skip.hidden,false);
window.raptorTouch.update({...state,introSkipVisible:true});assert.equal(skip.hidden,false);skip.listeners.click();assert(sent.some(x=>x[1]==='TouchAction'&&x[2]==='skip'));
window.raptorTouch.update({...state,phase:0});assert.equal(node('touch-intro').hidden,true);assert.equal(node('touch-garage').hidden,false);
window.raptorTouch.update({...state,phase:1});assert.equal(node('touch-race').hidden,false);assert.equal(node('touch-countdown').hidden,false);
for(const visibility of [false,true,false]){window.raptorTouch.update({...state,introSkipVisible:visibility});assert.equal(skip.hidden,false,'skip remains visible through all artwork states');}
window.raptorTouch.update({...state,phase:0});assert.equal(skip.hidden,true);
console.log('PASS: mobile intro hides garage/race/controller; skip stays visible for the complete intro and click sends skip; menu/countdown recover after intro.');

for (const [mode,label,next] of [[0,'Whole track','Follow truck'],[1,'Follow truck','Driver seat'],[2,'Driver seat','Whole track']]) { window.raptorTouch.update({...state,phase:2,cameraMode:mode}); assert.equal(node('[data-action="camera"]').textContent,'View: '+label); assert.equal(node('html').dataset.cameraMode,String(mode)); }
console.log('PASS: whole-track, follow and driver-seat mobile camera labels match the bridged mode.');
