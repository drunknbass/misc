(()=>{
 let last=0, draws=0, triangles=0, binds=0, programs=0, frame=0, targets={}, gpuPending=[], gpuTimes={};
 const ids=new WeakMap();let nextId=1;
 window.frameSamples=[];window.drawSamples=[];window.longTasks=[];window.renderSamples=[];window.gpuTimes=gpuTimes;
 const getId=o=>o?(ids.has(o)?ids.get(o):(ids.set(o,nextId),nextId++)):0;
 for(const C of [WebGLRenderingContext,WebGL2RenderingContext]){
  const state=new WeakMap();
  function s(gl){if(!state.has(gl))state.set(gl,{fb:null,viewport:[0,0,0,0],color:true,program:null,ext:undefined,query:null});return state.get(gl);}
  for(const name of ['bindFramebuffer','viewport','colorMask','useProgram']) {
   const original=C.prototype[name];
   C.prototype[name]=function(...args){const x=s(this);
    if(name==='bindFramebuffer'&&(args[0]===this.FRAMEBUFFER||args[0]===this.DRAW_FRAMEBUFFER)){if(x.fb!==args[1])binds++;x.fb=args[1];}
    if(name==='viewport')x.viewport=args;
    if(name==='colorMask')x.color=args.some(Boolean);
    if(name==='useProgram'){if(x.program!==args[0])programs++;x.program=args[0];}
    return original.apply(this,args);
   };
  }
  for(const name of ['drawArrays','drawElements','drawArraysInstanced','drawElementsInstanced']){
   const original=C.prototype[name];if(!original)continue;
   C.prototype[name]=function(...args){const x=s(this);draws++;
    const count=name.includes('Elements')?args[1]:args[2],instances=name.endsWith('Instanced')?args[name.includes('Elements')?4:3]:1;
    if(args[0]===this.TRIANGLES)triangles+=count/3*instances;
    const key=(x.fb?'offscreen':'canvas')+' '+x.viewport[2]+'x'+x.viewport[3]+(x.color?' color':' depth');targets[key]=(targets[key]||0)+1;
    if(this instanceof WebGL2RenderingContext){
     if(x.ext===undefined)x.ext=this.getExtension('EXT_disjoint_timer_query_webgl2');
     if(x.ext&&!x.query&&!this.getQuery(x.ext.TIME_ELAPSED_EXT,this.CURRENT_QUERY)){
      const gl=this,q=gl.createQuery(); x.query=q; gl.beginQuery(x.ext.TIME_ELAPSED_EXT,q);const number=frame;
      queueMicrotask(()=>{gl.endQuery(x.ext.TIME_ELAPSED_EXT);gpuPending.push({gl,q,number,ext:x.ext});x.query=null;});
     }
    }
    return original.apply(this,args);
   };
  }
 }
 function tick(t){
  if(last){window.frameSamples.push(t-last);window.drawSamples.push(draws);window.renderSamples.push({frame,draws,triangles,targetChanges:binds,programChanges:programs,targets});}
  for(let i=gpuPending.length-1;i>=0;i--){const p=gpuPending[i];if(p.gl.getQueryParameter(p.q,p.gl.QUERY_RESULT_AVAILABLE)){
   if(!p.gl.getParameter(p.ext.GPU_DISJOINT_EXT))gpuTimes[p.number]=(gpuTimes[p.number]||0)+p.gl.getQueryParameter(p.q,p.gl.QUERY_RESULT)/1e6;
   p.gl.deleteQuery(p.q);gpuPending.splice(i,1);
  }}
  draws=triangles=binds=programs=0;targets={};last=t;frame++;requestAnimationFrame(tick);
 }
 requestAnimationFrame(tick);
 new PerformanceObserver(list=>window.longTasks.push(...list.getEntries().map(e=>e.duration))).observe({entryTypes:['longtask']});
})();
