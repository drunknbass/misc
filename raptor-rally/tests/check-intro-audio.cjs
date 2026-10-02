const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict');
const button={hidden:true,disabled:false,dataset:{},textContent:'Play intro with sound'},sent=[];
let library;
const audio={state:'suspended',resume(){this.state='running';return Promise.resolve();}};
vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../unity/Assets/Plugins/WebGL/RaptorTouch.jslib'),'utf8'),{
 LibraryManager:{library:{}},mergeInto(_,value){library=value;},WEBAudio:{audioContext:audio},
 document:{getElementById(){return button;}},window:{raptorGame:{SendMessage(...args){sent.push(args);}}}
});
(async()=>{
 assert.equal(library.RaptorIntroAudio(1),0);assert.equal(button.hidden,false);
 button.onclick();assert.equal(button.disabled,true);
 // A Unity frame can hide the button after audio resumes but before the promise settles.
 assert.equal(library.RaptorIntroAudio(1),1);assert.equal(button.hidden,true);
 await new Promise(setImmediate);assert.deepEqual(sent,[['Raptor Rally','RestartIntroWithSound']]);assert.equal(button.disabled,false);
 audio.state='suspended';library.RaptorIntroAudio(1);button.onclick();library.RaptorIntroAudio(0);
 await new Promise(setImmediate);assert.equal(sent.length,1,'Do not restart an intro skipped while resuming audio');assert.equal(button.hidden,true);
 audio.state='suspended';audio.resume=()=>Promise.reject(new Error('blocked'));library.RaptorIntroAudio(1);button.onclick();
 await new Promise(setImmediate);assert.equal(button.disabled,false);assert.equal(button.textContent,'Try sound again');assert.equal(sent.length,1);
 console.log('PASS: suspended audio shows unlock; running audio hides it; gesture replays the intro once; skip cancels pending replay; rejected resume can retry.');
})().catch(error=>{console.error(error);process.exitCode=1;});
