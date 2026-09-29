// Inject after Unity is ready, before Enter. No synchronous GPU queries.
window.frameSamples=[];window.drawSamples=[];
(() => {
  let last=0,draws=0;
  for(const C of [WebGLRenderingContext,WebGL2RenderingContext])
    for(const name of ['drawArrays','drawElements','drawArraysInstanced','drawElementsInstanced']) {
      const old=C.prototype[name];
      if(old) C.prototype[name]=function(...args){draws++;return old.apply(this,args);};
    }
  function tick(t){if(last){frameSamples.push(t-last);drawSamples.push(draws);}last=t;draws=0;requestAnimationFrame(tick);}
  requestAnimationFrame(tick);
})();
