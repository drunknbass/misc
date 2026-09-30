const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const nodes=new Map(),sent=[],globalEvents={};
function node(id){if(!nodes.has(id))nodes.set(id,{hidden:false,dataset:{},style:{},textContent:'',attrs:{},listeners:{},classList:{toggle(){}},setAttribute(k,v){this.attrs[k]=v;},addEventListener(k,f){this.listeners[k]=f;},querySelector(s){return node(s);},querySelectorAll(){return [];},closest(){return null;},setPointerCapture(){}});return nodes.get(id);}
const buttons=[1,2,4,16,8].map(bit=>{const b=node('bit'+bit);b.dataset.bit=String(bit);return b;});
const pedals=buttons.slice(2);pedals.forEach((b,i)=>{b.closest=()=>node('pedal-group');b.getBoundingClientRect=()=>({left:150+i*60,right:200+i*60,top:400+(i===0?0:20),bottom:488});});
node('touch-ui').querySelectorAll=s=>s==='[data-bit]'?buttons:[];
const document={getElementById:node,body:node('body'),documentElement:node('html'),addEventListener(k,f){globalEvents[k]=f;}};
const window={location:{search:''}};
vm.runInNewContext(fs.readFileSync(require('node:path').join(__dirname,'../touch-controls.js'),'utf8'),{document,window,Image:class {},matchMedia:()=>({matches:true,addEventListener(){}}),navigator:{maxTouchPoints:1},innerWidth:390,addEventListener(k,f){globalEvents[k]=f;},URLSearchParams,Map,Math,String,Number});
window.raptorTouch.connect({SendMessage(...a){sent.push(a);}});
const state={phase:2,paused:false,selected:0,rank:1,lap:1,time:0,speed:0,nitro:1,countdown:0};
const update=patch=>window.raptorTouch.update({...state,...patch});update({});
const last=()=>sent.filter(a=>a[1]==='SetTouchInput').at(-1)[2];
function event(bit,type,id=10,x=175,y=435){node('bit'+bit).listeners[type]({pointerId:id,clientX:x,clientY:y,pointerType:'touch',button:0,preventDefault(){},key:' '});}
event(1,'pointerdown',1);event(4,'pointerdown');assert.equal(last(),5);
event(4,'pointermove',10,205);assert.equal(last(),5,'gap retains gas');
event(4,'pointermove',10,235,405);assert.equal(last(),21,'horizontal slide reaches shorter nitro and keeps throttle/steering');
assert.equal(node('bit16').attrs['aria-pressed'],'true');
event(4,'pointermove',10,295);assert.equal(last(),9,'brake replaces boost and throttle');
event(4,'pointermove',10,235);assert.equal(last(),21,'slide back to nitro');
event(4,'pointermove',10,175);assert.equal(last(),5,'slide back to gas');
event(4,'pointermove',10,149);assert.equal(last(),1,'exit cluster releases pedal only');
event(4,'pointermove',10,235);assert.equal(last(),21,'reentry resumes');
event(4,'pointerup');assert.equal(last(),1,'lift releases captured slide');event(1,'pointerup',1);assert.equal(last(),0);
event(16,'pointerdown');assert.equal(last(),20,'direct nitro accelerates');event(8,'pointerdown',11);assert.equal(last(),8,'second finger brake wins');event(8,'pointerup',11);assert.equal(last(),20);event(16,'pointercancel');assert.equal(last(),0);
event(4,'pointerdown');event(4,'lostpointercapture');assert.equal(last(),0);
event(16,'keydown');assert.equal(last(),20);event(16,'keyup');assert.equal(last(),0);
event(4,'pointerdown');update({paused:true});assert.equal(last(),0);event(4,'pointermove',10,235);assert.equal(last(),0,'paused stale capture cannot restart');update({});event(4,'pointermove',10,235);assert.equal(last(),0,'resume requires new touch');
for(const lifecycle of ['blur','pagehide','resize']){event(4,'pointerdown');globalEvents[lifecycle]();assert.equal(last(),0,lifecycle);}
event(4,'pointerdown');document.hidden=true;globalEvents.visibilitychange();assert.equal(last(),0);
update({phase:4});event(4,'pointerdown');assert.equal(last(),0,'intro ignores drive input');
console.log('PASS: continuous gas/nitro/brake slides, gaps, differing heights, both directions, exit/reentry, steering multitouch, brake priority, nitro throttle, release/cancel/capture loss, keyboard, pause/resume, blur/resize/hide and intro gating.');
// Native gestures must be cancelled without interfering with driving or action clicks.
update({});event(4,'pointerdown');
for(const type of ['selectstart','contextmenu','dragstart','dblclick']){
 let prevented=false;node('body').listeners[type]({preventDefault(){prevented=true;}});
 assert(prevented,type+' is cancelled');assert.equal(last(),4,type+' leaves gas held');
}
for(const [name,race,action,expected] of [['HUD',true,false,true],['canvas',true,false,true],['pedal',true,false,true],['pause',true,true,false],['garage button',false,true,false],['menu scrolling',false,false,false]]){
 let prevented=false;node('body').listeners.touchstart({target:{closest(selector){return selector.includes('#touch-race')?race:action;}},preventDefault(){prevented=true;}});
 assert.equal(prevented,expected,name);
}
event(4,'pointerup');assert.equal(last(),0);
console.log('PASS: selection/callout/drag/double-click guards cancel native defaults, preserve held driving input, and allow action-button taps and menu scrolling.');
