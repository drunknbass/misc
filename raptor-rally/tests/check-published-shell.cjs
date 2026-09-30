// Test the actual published shell, not a fixture that removes its startup script.
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..'),html=fs.readFileSync(path.join(root,'index.html'),'utf8');
assert(!/\{\{\{|^\s*#(?:if|else|endif)\b/m.test(html),'Published HTML contains unprocessed Unity template directives');
let inline=0;
for(const m of html.matchAll(/<script\b([^>]*)>([\s\S]*?)<\/script>/g)){
 const src=/\bsrc="([^"]+)"/.exec(m[1]);
 if(src){const file=path.join(root,src[1].split('?')[0]);assert(fs.existsSync(file),`Missing script: ${src[1]}`);new vm.Script(fs.readFileSync(file,'utf8'),{filename:file});}
 else{inline++;new vm.Script(m[2],{filename:'published-index-inline.js'});}
}
assert(inline>0,'Missing Unity startup script');
for(const field of ['loader.src','dataUrl','frameworkUrl','codeUrl']){
 const escaped=field.replace('.','\\.');
 const m=html.match(new RegExp(escaped+"\\s*[:=]\\s*'([^']+)'"));
 assert(m,`Missing compiled ${field}`);assert(fs.existsSync(path.join(root,m[1])),`Missing build asset: ${m[1]}`);
}
assert.match(html,/createUnityInstance\(canvas/);assert.match(html,/window\.raptorBuilder\.connect\(instance\)/);
console.log('PASS: published shell has no Unity template directives; startup and external scripts parse; all compiled Unity assets exist.');
