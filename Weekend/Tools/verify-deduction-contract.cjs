const fs=require('fs'),assert=require('assert');
const graph=JSON.parse(fs.readFileSync('Weekend/Assets/Resources/Investigation.json','utf8'));
const rows=[
['deduction-growing-shadow','first-night-silhouette','first-night-recheck','遮挡在扩大'],
['deduction-warning','401-warning','skt079-broadcast','他知道什么'],
['deduction-sealing','401-warning','401-barricade','封门'],
['deduction-not-looking','401-barricade','filter-401-underprint','挡住视线'],
['deduction-optical','balcony-telescope-day2-ancient-outline','first-night-silhouette','同一道弧线'],
['deduction-group-assignment','neighbor-order','skt079-broadcast','三角教与被分配的大楼'],
['deduction-source-location','deduction-group-assignment','pipe-pulse','地下供能源'],
['deduction-building-plan','deduction-source-location','city-fuse','净化装置位置'],
['deduction-tank-resident','tank-label','tank-eye','多了一张嘴'],
['deduction-waterline','tank-overflow','001-name','从鱼缸到屋里'],
['deduction-sealed-materials','prep-oil','prep-medium','封装准备组合'],
['deduction-sealed-tool','deduction-sealed-materials','prep-manual','单次准备工具'],
['deduction-city-orders','city-orders','city-routing','城市搜捕的指令源'],
['deduction-observatory','city-spectrum','city-lens','同步观测通路'],
['deduction-day7-plan','deduction-building-plan','city-maintenance','第七天爆破计划']];
assert.equal(graph.deductions.length,rows.length);
const ids=new Set([...graph.evidence,...graph.deductions].map(x=>x.id));assert.equal(ids.size,graph.evidence.length+graph.deductions.length);
for(const locale of ['StoryText','StoryTextEnglish']){
 const t=JSON.parse(fs.readFileSync('Weekend/Assets/Resources/'+locale+'.json','utf8')).investigation;
 for(const [id,left,right,title] of rows){const d=graph.deductions.find(x=>x.id===id);assert(d&&d.left===left&&d.right===right,id);assert(ids.has(left)&&ids.has(right),id+' dangling dependency');const text=t.deductions.filter(x=>x.id===id);assert.equal(text.length,1);assert(text[0].title&&text[0].body);if(locale==='StoryText')assert.equal(text[0].title,title);}
}
const code=fs.readFileSync('Weekend/Assets/Scripts/Knowledge/Investigation.cs','utf8');assert.equal((code.match(/d\.id != "deduction-warning" \|\| State\.neighborFlyersSeen/g)||[]).length,2,'Both Match overloads retain flyer prerequisite');
const seen=new Set(),active=new Set();function visit(id){if(seen.has(id))return;assert(!active.has(id),'cycle '+id);active.add(id);const d=graph.deductions.find(x=>x.id===id);if(d){visit(d.left);visit(d.right);}active.delete(id);seen.add(id);}for(const row of rows)visit(row[0]);
console.log('PASS: 15 exact recipes; bilingual conclusion titles; no missing nodes/cycles; flyer prerequisite in both Match overloads. Static verification only.');
