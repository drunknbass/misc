const assert=require('node:assert/strict');
const M=require('../track-model.js');
const d=M.preset();assert.equal(M.validate(d),'');assert.deepEqual(M.parse(JSON.stringify(d)),d);
for(const transform of [d=>({...d,version:2}),d=>({...d,cells:[24,24,...d.cells.slice(2)]}),d=>({...d,cells:[24,20,...d.cells.slice(2)]}),d=>({...d,pieces:[0,1,1,...d.pieces.slice(3)]}),d=>({...d,pieces:[1,...d.pieces.slice(1)]}),d=>({...d,pieces:[0,9,...d.pieces.slice(2)]}),d=>({...d,pieces:d.pieces.slice(1)}),d=>({...d,cells:[24.5,...d.cells.slice(1)]})])assert.notEqual(M.validate(transform(d)),'');
for(const raw of ['null','{}','{broken','x'.repeat(8193)])assert.throws(()=>M.parse(raw));
assert.equal(M.validate(M.rotate({...d,pieces:d.pieces.map(()=>0)},6)),'');assert(M.validate(M.rotate(d,2)));
const safe=M.parse(JSON.stringify({...d,name:'<b>Fast</b>\nDirt!'}));assert(!safe.name.includes('<'));assert(!safe.name.includes('\n'));
// Every simple loop within the 7x5 board must have even length and pass adjacency checks.
const outer={version:1,name:'Outer loop',cells:[3,4,5,6,13,20,27,34,33,32,31,30,29,28,21,14,7,0,1,2],pieces:Array(20).fill(0)};
assert.equal(M.validate(outer),'');assert.equal(M.validate(M.rotate(outer,10)),'');
console.log('PASS: starter/outer loops, serialization, start-line rotation, malformed/disconnected/crossing/bad-piece/bad-start rejection, file size limit and name sanitization.');
