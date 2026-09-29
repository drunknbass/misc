import fs from 'node:fs/promises';
const url=process.argv[2]||'http://127.0.0.1:8874/outputs/Web/';
const target=(await(await fetch('http://127.0.0.1:9334/json/list')).json()).find(t=>t.type==='page');
const ws=new WebSocket(target.webSocketDebuggerUrl);await new Promise(r=>ws.addEventListener('open',r,{once:true}));
let id=0;const pending=new Map(),errors=[],checks=[];
ws.addEventListener('message',e=>{const m=JSON.parse(e.data);if(m.id){const p=pending.get(m.id);pending.delete(m.id);m.error?p.reject(m.error):p.resolve(m.result);}else if(m.method==='Runtime.exceptionThrown')errors.push(m.params.exceptionDetails);else if(m.method==='Runtime.consoleAPICalled'&&m.params.type==='error')errors.push(m.params.args.map(a=>a.value||a.description).join(' '));});
const call=(method,params={})=>new Promise((resolve,reject)=>{const i=++id;pending.set(i,{resolve,reject});ws.send(JSON.stringify({id:i,method,params}));});
const wait=ms=>new Promise(r=>setTimeout(r,ms));
async function ev(expression){const r=await call('Runtime.evaluate',{expression,returnByValue:true});if(r.exceptionDetails)throw Error(JSON.stringify(r.exceptionDetails));return r.result.value;}
function check(name,value){checks.push({name,passed:!!value});console.log(name,value);if(!value)throw Error(name);}
async function shot(name){const s=await call('Page.captureScreenshot',{format:'png'});await fs.writeFile('work/performance/mobile-'+name+'.png',Buffer.from(s.data,'base64'));}
async function center(selector){return ev(`(()=>{const r=document.querySelector(${JSON.stringify(selector)}).getBoundingClientRect();return{x:r.x+r.width/2,y:r.y+r.height/2}})()`);}
async function tap(selector){await call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...await center(selector),id:1}]});await call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await wait(300);}
async function resize(width,height){await call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:3,mobile:true});await wait(600);}
try{
await call('Page.enable');await call('Runtime.enable');await call('Emulation.setTouchEmulationEnabled',{enabled:true,maxTouchPoints:5});await resize(390,844);await call('Page.navigate',{url});
let ready=false;for(let i=0;i<120;i++){if(await ev("document.documentElement.dataset.gameReady==='true'")){ready=true;break;}await wait(500);}check('Unity loaded',ready);await wait(1000);
await ev("window.mobileStates=[];window.mobileMasks=[];const oldUpdate=raptorTouch.update;raptorTouch.update=s=>{mobileStates.push(s);if(mobileStates.length>100)mobileStates.shift();oldUpdate(s)};const oldSend=raptorGame.SendMessage;raptorGame.SendMessage=(...args)=>{if(args[1]==='SetTouchInput')mobileMasks.push(args[2]);return oldSend.apply(raptorGame,args)}");await wait(200);
check('Mobile HUD active',await ev("document.body.classList.contains('touch-enabled')&&!document.getElementById('touch-ui').hidden&&mobileStates.length>0"));await shot('portrait-garage');await resize(320,568);await shot('small-portrait-garage');await resize(390,844);
await tap('[data-action="truck1"]');check('Bronco selected in Unity',await ev('mobileStates.at(-1).selected===1'));await tap('[data-action="truck2"]');check('Ranger selected in Unity',await ev('mobileStates.at(-1).selected===2'));
await tap('.garage-actions [data-action="start"]');await wait(4000);check('Race started',await ev('mobileStates.at(-1).phase===2'));
await call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...await center('[data-bit="4"]'),id:1},{...await center('[data-bit="2"]'),id:2},{...await center('[data-bit="16"]'),id:3}]});await wait(900);
check('Three simultaneous controls',await ev('mobileMasks.at(-1)===22'));check('Truck accelerates and boosts',await ev('mobileStates.some(s=>s.speed>5&&s.nitro<.99)'));await shot('portrait-driving');
await call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});check('Release resets inputs',await ev('mobileMasks.at(-1)===0'));
await tap('[data-action="pause"]');check('Pause works',await ev('mobileStates.at(-1).paused'));let t=await ev('mobileStates.at(-1).time');await wait(500);check('Pause freezes race clock',await ev(`mobileStates.at(-1).time===${t}`));await tap('[data-action="resume"]');
await call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...await center('[data-bit="4"]'),id:1}]});await wait(200);await resize(844,390);check('Rotation clears throttle',await ev('mobileMasks.at(-1)===0'));await call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await shot('landscape-driving');
await tap('[data-action="camera"]');check('Whole track camera works',await ev('!mobileStates.at(-1).following'));await tap('[data-action="camera"]');check('Follow camera works',await ev('mobileStates.at(-1).following'));
await call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...await center('[data-bit="8"]'),id:1},{...await center('[data-bit="1"]'),id:2}]});await wait(200);check('Brake and left steering combine',await ev('mobileMasks.at(-1)===9'));await ev("dispatchEvent(new Event('blur'))");check('App focus loss releases controls',await ev('mobileMasks.at(-1)===0'));await call('Input.dispatchTouchEvent',{type:'touchCancel',touchPoints:[]});
await call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...await center('[data-bit="4"]'),id:1}]});await wait(100);await call('Input.dispatchTouchEvent',{type:'touchCancel',touchPoints:[]});check('Interrupted touch releases throttle',await ev('mobileMasks.at(-1)===0'));

for(const [width,height]of[[320,568],[568,320],[844,390]]){await resize(width,height);check(`Controls fit ${width}x${height}`,await ev("[...document.querySelectorAll('[data-bit]')].every(b=>{const r=b.getBoundingClientRect();return r.left>=0&&r.right<=innerWidth&&r.bottom<=innerHeight&&r.width>=44&&r.height>=44})"));check(`Stats and tools do not overlap ${width}x${height}`,await ev("(()=>{const a=document.querySelector('.touch-stats').getBoundingClientRect(),b=document.querySelector('.touch-tools').getBoundingClientRect();return a.right<=b.left||a.bottom<=b.top})()"));}
await tap('[data-action="pause"]');await tap('[data-action="garage"]');check('Return to garage',await ev('mobileStates.at(-1).phase===0'));await shot('landscape-garage');
await call('Emulation.setTouchEmulationEnabled',{enabled:false});await call('Emulation.setDeviceMetricsOverride',{width:1600,height:900,deviceScaleFactor:1,mobile:false});await wait(500);check('Desktop UI restored',await ev("document.getElementById('touch-ui').hidden&&!document.body.classList.contains('touch-enabled')"));await shot('desktop');check('No runtime errors',errors.length===0);
}finally{await fs.writeFile('work/performance/mobile.json',JSON.stringify({url,checks,errors},null,2));ws.close();}
