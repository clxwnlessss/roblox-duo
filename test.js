const {test}=require('node:test');const assert=require('node:assert/strict');const c=require('./core');
test('settings validation blocks malformed repositories and profiles',()=>{assert.equal(c.validate(c.defaults).browser,'Edge');for(const repository of ['https://github.com/a/b','../x','a/b?x','a/b/c'])assert.throws(()=>c.validate({...c.defaults,repository}));assert.throws(()=>c.validate({...c.defaults,names:['','Alt']}));assert.throws(()=>c.validate({...c.defaults,browser:'Other'}));});
test('six isolated profiles with legacy Edge path',()=>{const paths=c.browsers.flatMap(b=>[0,1].map(i=>c.profile('root',b,i)));assert.equal(new Set(paths).size,6);assert.equal(paths[0],require('node:path').join('root','Account1'));assert.deepEqual(c.args('Firefox','space path').slice(0,3),['-no-remote','-profile','space path']);});
test('numeric release comparison',()=>{assert.ok(c.newer('v0.10.0','0.4.0'));assert.ok(!c.newer('v0.3.0','0.4.0'));assert.ok(!c.newer('v0.4.0','0.4.0'));assert.throws(()=>c.newer('latest','0.4.0'));});
test('Windows helper holds locks, rejects collision, and releases on EOF',async t=>{
 if(process.platform!=='win32')return t.skip('Windows only');
 const {spawn}=require('node:child_process');const {createInterface}=require('node:readline');
 const testId=require("node:crypto").randomUUID(); function helper(){const p=spawn(require('node:path').join(__dirname,'native','DuoBridge.exe'),[testId],{windowsHide:true});const q=[];createInterface({input:p.stdout}).on('line',s=>q.shift()?.(s));return {p,send:s=>new Promise(resolve=>{q.push(resolve);p.stdin.write(s+'\n');})};}
 const a=helper(),b=helper();
 try{const count=await a.send('count');assert.match(count,/^COUNT:\d+$/);if(count!=='COUNT:0')return t.skip('Roblox is running');assert.equal(await a.send('enable'),'OK');assert.match(await b.send('enable'),/^ERROR:/);a.p.stdin.end();await new Promise(r=>a.p.once('exit',r));assert.equal(await b.send('enable'),'OK');assert.equal(await b.send('disable'),'OK');}finally{a.p.stdin.end();b.p.stdin.end();}
});
