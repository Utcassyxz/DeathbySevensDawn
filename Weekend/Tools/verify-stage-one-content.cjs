const fs=require('fs'),assert=require('assert'),cp=require('child_process');
const root='Weekend/Assets/Resources/';
const read=name=>JSON.parse(fs.readFileSync(root+name+'.json','utf8').replace(/^\uFEFF/,''));
const zh=read('StoryText'),en=read('StoryTextEnglish'),graph=read('Investigation');
const recipes=new Set(graph.deductions.flatMap(d=>[d.left,d.right]));
for(const e of graph.evidence)assert(recipes.has(e.id)||e.flavor===true,'unpaired evidence requires explicit flavor: '+e.id);
const required=['night-call','night-intro','night-twitch','night-body','night-controls','night-caught','day7-call','day7-confrontation','day7-return','day2-return','stage-complete'];
for(let day=1;day<=2;day++)for(let slot=0;slot<4;slot++){
 required.push(`beat-${day}-${slot}`,`echo-${day}-${slot}`);
 for(let beat=1;beat<=3;beat++)required.push(`after-${day}-${slot}-${beat}`);
}
for(const [name,locale] of [['StoryText',zh],['StoryTextEnglish',en]]){
 const rows=locale.neighborSlice.entries,ids=rows.map(x=>x.id);assert.equal(new Set(ids).size,ids.length,'duplicate lines: '+name);
 for(const id of required)assert(rows.some(r=>r.id===id&&r.body.trim()),name+' missing event '+id);
 const original=JSON.parse(cp.execFileSync('git',['show','cb74790:'+root+name+'.json'],{encoding:'utf8'}));
 assert.deepStrictEqual(locale.dayOneBroadcast,original.dayOneBroadcast,'original four advertisements changed');
 assert(!locale.investigation.deductions.find(d=>d.id==='deduction-building-plan').body.match(/第三日结束|by Day 3/),'obsolete fatal deadline');
}
const zhIds=zh.neighborSlice.entries.map(x=>x.id).sort(),enIds=en.neighborSlice.entries.map(x=>x.id).sort();assert.deepStrictEqual(zhIds,enIds,'locale ID drift');
for(const file of ['ApartmentEventDirector','SaveValidation'])assert(fs.existsSync(`Weekend/Assets/Scripts/Progression/${file}.cs.meta`),'missing GUID '+file);
for(const sound of ['NeighborRadio','NeighborBreath','ChairScrape','NightHeartbeat']){assert(fs.existsSync(root+'Audio/'+sound+'.wav'));assert(fs.existsSync(root+'Audio/'+sound+'.wav.meta'));}
console.log('PASS: original advertisements unchanged; bilingual event IDs and full Day1/2 cadence; every evidence paired or explicit flavor; additive assets have GUIDs. Static content verification only.');
