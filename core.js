const path = require('node:path');
const browsers = ['Edge','Chrome','Firefox'];
const defaults = {names:['Main','Alt'],browser:'Edge',repository:'clxwnlessss/roblox-duo'};
function validate(s){if(!s || !Array.isArray(s.names)||s.names.length!==2||s.names.some(n=>typeof n!=='string'||!n.trim()||n.length>24)||!browsers.includes(s.browser)||typeof s.repository!=='string'||!/^([a-z\d][a-z\d-]{0,38}\/[\w][\w.-]{0,99})?$/i.test(s.repository))throw Error('Use two names (1–24 characters), a supported browser, and owner/repository.');return {names:s.names.map(n=>n.trim()),browser:s.browser,repository:s.repository};}
function profile(root,browser,i){return browser==='Edge'?path.join(root,'Account'+(i+1)):path.join(root,browser,'Account'+(i+1));}
function args(browser,p){return browser==='Firefox'?['-no-remote','-profile',p,'https://www.roblox.com/home']:['--user-data-dir='+p,'--no-first-run','--new-window','https://www.roblox.com/home'];}
function version(tag){const m=/^v?(\d+)\.(\d+)\.(\d+)$/.exec(tag);if(!m)throw Error('Release tag must use vMAJOR.MINOR.PATCH.');return m.slice(1).map(Number);}
function newer(a,b){a=version(a);b=version(b);for(let i=0;i<3;i++)if(a[i]!==b[i])return a[i]>b[i];return false;}
module.exports={defaults,browsers,validate,profile,args,newer};
