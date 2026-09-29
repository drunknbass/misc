// Execute after clicking the canvas and pressing Enter; reads window.safariBenchmark after 20 seconds.
setTimeout(()=>{
  const c=document.querySelector('canvas');
  c.dispatchEvent(new KeyboardEvent('keydown',{key:'w',code:'KeyW',keyCode:87,which:87,bubbles:true}));
  frameSamples=[];drawSamples=[];
  setTimeout(()=>{
    c.dispatchEvent(new KeyboardEvent('keyup',{key:'w',code:'KeyW',keyCode:87,which:87,bubbles:true}));
    const a=frameSamples.slice().sort((a,b)=>a-b);
    window.safariBenchmark={
      ua:navigator.userAgent,visibility:document.visibilityState,canvas:[c.width,c.height],
      frames:a.length,meanMs:a.reduce((x,y)=>x+y,0)/a.length,
      p95Ms:a[Math.floor(a.length*.95)],framesOver33:a.filter(x=>x>33.4).length,
      draws:drawSamples.reduce((x,y)=>x+y,0)/drawSamples.length,metrics:raptorGame.GetMetricsInfo()
    };
  },15000);
},4500);
