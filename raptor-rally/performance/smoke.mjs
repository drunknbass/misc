import fs from 'node:fs/promises';
const [url,out]=process.argv.slice(2);
const target=(await (await fetch('http://127.0.0.1:9334/json/list')).json()).find(x=>x.type==='page');
const ws=new WebSocket(target.webSocketDebuggerUrl);await new Promise(r=>ws.addEventListener('open',r,{once:true}));
let id=0;const pending=new Map(),errors=[];
ws.addEventListener('message',e=>{const m=JSON.parse(e.data);if(m.id){const p=pending.get(m.id);pending.delete(m.id);m.error?p.reject(m.error):p.resolve(m.result);}else if(m.method==='Runtime.exceptionThrown')errors.push(m.params.exceptionDetails.text);else if(m.method==='Runtime.consoleAPICalled'&&m.params.type==='error')errors.push(m.params.args.map(a=>a.value||a.description).join(' '));});
const call=(method,params={})=>new Promise((resolve,reject)=>{const i=++id;pending.set(i,{resolve,reject});ws.send(JSON.stringify({id:i,method,params}));});
const wait=ms=>new Promise(r=>setTimeout(r,ms));
async function evaluate(expression){const r=await call('Runtime.evaluate',{expression,returnByValue:true});if(r.exceptionDetails)throw Error(JSON.stringify(r.exceptionDetails));return r.result.value;}
async function key(key,code,vk,hold=80){await call('Input.dispatchKeyEvent',{type:'keyDown',key,code,windowsVirtualKeyCode:vk});await wait(hold);await call('Input.dispatchKeyEvent',{type:'keyUp',key,code,windowsVirtualKeyCode:vk});}
async function shot(name){const s=await call('Page.captureScreenshot',{format:'png'});await fs.writeFile(out+'-'+name+'.png',Buffer.from(s.data,'base64'));}
async function size(){return evaluate("({width:document.querySelector('canvas').width,height:document.querySelector('canvas').height,fullscreen:!!document.fullscreenElement})");}
await call('Page.enable');await call('Runtime.enable');await call('Emulation.setCPUThrottlingRate',{rate:1});
await call('Emulation.setDeviceMetricsOverride',{width:3840,height:2160,deviceScaleFactor:2,mobile:false});await call('Page.navigate',{url});
let ready=false;for(let i=0;i<120;i++){if(await evaluate("document.documentElement?.dataset.gameReady==='true'")){ready=true;break;}await wait(500);}if(!ready)throw Error('Startup timed out');
await wait(4500);const initial=await size();if(initial.width>1920||initial.height>1080)throw Error('4K pixel budget exceeded');
await call('Emulation.setDeviceMetricsOverride',{width:1600,height:900,deviceScaleFactor:1,mobile:false});await wait(300);
await key('2','Digit2',50);await wait(300);await shot('bronco');await key('3','Digit3',51);await wait(300);await shot('ranger');
await key('Enter','Enter',13);await wait(4000);await key('c','KeyC',67);await key('m','KeyM',77);
await call('Input.dispatchKeyEvent',{type:'keyDown',key:' ',code:'Space',windowsVirtualKeyCode:32});await key('w','KeyW',87,2500);await call('Input.dispatchKeyEvent',{type:'keyUp',key:' ',code:'Space',windowsVirtualKeyCode:32});await wait(100);await shot('driving');
await key('Escape','Escape',27);await wait(200);await shot('paused');
const p=await evaluate("(()=>{const r=document.getElementById('fullscreen').getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2};})()");
await call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',clickCount:1});await call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',clickCount:1});await wait(500);const fullscreen=await size();if(fullscreen.width>1920||fullscreen.height>1080)throw Error('Fullscreen pixel budget exceeded');
await fs.writeFile(out+'.json',JSON.stringify({url,initial,fullscreen,errors},null,2));console.log(JSON.stringify({initial,fullscreen,errors}));await call('Page.navigate',{url:'about:blank'});ws.close();
