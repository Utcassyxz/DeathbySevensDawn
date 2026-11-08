const fs=require('fs');
const root=__dirname+'/../Assets/';
function edit(path,from,to){let p=root+path,s=fs.readFileSync(p,'utf8');if(!s.includes(from))throw Error(path+' missing '+from.slice(0,60));fs.writeFileSync(p,s.replace(from,to));}
let p=root+'Scripts/Narrative/NeighborSlice.cs',s=fs.readFileSync(p,'utf8');s=s.slice(0,s.indexOf('        void OnGUI()'))+`        void OnGUI()
        {
            if(ProgressionHUD.DebugConsoleOpen||!open&&deathTimer<=0)return;
            var choices=new List<string>();var indices=new List<int>();
            if(deathTimer<=0)
            {
                if(node!=null&&node.choices!=null){for(int i=0;i<node.choices.Length;i++)if(Allowed(node.choices[i].condition)){choices.Add((i+1)+". "+Text(node.choices[i].line).body);indices.Add(i);}}
                else if(panel=="force-door"||panel=="routing"||panel=="source-core")
                {choices.Add("1. "+Text(panel=="force-door"?"force-choice":panel=="routing"?"isolate-choice":"lock-choice").body);indices.Add(0);if(panel=="force-door"){choices.Add("2. "+Text("knock-choice").body);indices.Add(1);}}
                else if(panel==Tool){choices.Add(Text("craft-choice").body);indices.Add(0);}
                else if(panel==Crowbar||panel==Oil||panel==Medium){choices.Add(Text("take").body);indices.Add(0);}
            }
            var line=Text(deathTimer>0?"neighbor-killed":panel);
            int pick=NarrativeCard.Draw(line.title,line.body,Notice,choices.ToArray(),deathTimer>0?"":Text("controls").body);
            if(pick>=0)Choose(indices[pick]);
        }
    }
}
`;fs.writeFileSync(p,s);
const copy={
 'crowbar':['撬棍','一根锈迹斑斑的撬棍。\n或许能撬开一扇门。也能撬开某些人的思路。','Crowbar','A crowbar, thick with rust.\nFor opening stubborn doors. And the occasional closed mind.'],
 'cooking-oil':['食用油','瓶底还剩一点油。\n煎不了晚饭，倒够给坏主意添把火。','Cooking oil','A little oil clings to the bottle.\nNot enough for supper. Plenty for a bad idea.'],
 'sealed-maintenance-medium':['封存维修芯','浴室柜里的一枚旧维修芯。外壳印着一簇褪色的火。\n说明书把它叫作安全装置。说明书通常不用负责。','Sealed repair core','An old repair core from the bathroom cabinet. A faded flame marks its casing.\nThe leaflet calls it a safety device. Leaflets rarely take responsibility.'],
 'sealed-ember-tool':['余烬匣','一只封好的小匣子，里面还记得火的样子。\n一次就够。希望如此。','Ember case','A sealed little case that remembers being fire.\nOne use. Hopefully enough.'],
 'foreshadow':['雨果 · 第二天','401的门后又响了一声。像搬家具，也像家具在搬人。\n\n厨房低柜里还有父亲留下的撬棍。灶边剩了半瓶油；浴室柜里的维修芯，标签上画着火。\n\n先找点能派上用场的东西。至少别空着手，替邻居添一道菜。','Hugo · Day two','Another thud behind 401. Furniture being moved. Or moving someone.\n\nDad left a crowbar in the low kitchen cupboard. There is oil by the stove; a repair core in the bathroom cabinet has a flame on its label.\n\nI should bring something useful. It would be rude to arrive as the only course.'],
 'hat-reveal':['他摘下帽子','“屋里太热。”\n帽子下面，少了半个能用来出汗的脑袋。','He takes off his hat','“Warm in here.”\nUnder the hat, half the head has stopped being a head.'],
 'contamination':['认知污染 · 理智 −20','雨果突然明白，帽子一直戴在谁的眼睛上。\n\n认知滤网破裂。现实露出了湿润的内侧。','Cognitive contamination · Sanity −20','Hugo understands, suddenly, whose eyes the hat was covering.\n\nThe perceptual filter tears. Reality shows its wet underside.'],
 'contamination-known':['认知滤网再次破裂','帽子离开头顶。那道旧伤，也离开了记忆。','The filter tears again','The hat leaves his head. The old wound leaves memory.'],
 'brain-warning':['脑组织 · 投掷','他的手伸向脑后。别站在他看着的位置。','Brain cast','His hand reaches behind his head. Move away from where he is looking.'],
 'brain-bound':['被缠住了','温热的脑组织收紧。还要 {0:0.0} 秒，才能把自己从这个念头里拔出来。','Entangled','Warm neural tissue tightens. {0:0.0} seconds until you can pull yourself out of this thought.'],
 'kick-warning':['踢击','他把重量交给一条腿。另一条腿有别的打算。','Kick','He puts his weight on one leg. The other has plans.'],
 'attack-warning':['拳击','他收起拳头。不是为了讲道理。','Punch','He draws back a fist. Negotiations have narrowed.'],
 'intro':['401 · 邻居','他还戴着那顶旧帽子。帽檐下的笑容僵了半拍。\n“我听见你撬门了。你也听见我的……工作了？”','401 · The neighbor','The old hat is still on. His smile arrives half a second late.\n“I heard the door. I suppose you heard my… work?”'],
 'ask':['401 · 邻居','“他们说，这是下一阶段。有人得先准备好。”\n他的手停在帽檐边，又放下。\n“你是来帮忙的，对吧？”','401 · The neighbor','“They call it the next stage. Someone has to get things ready.”\nHis hand touches the brim, then falls.\n“You came to help. Didn’t you?”'],
 'force-door':['401 · 门锁','锁眼里塞着灰。门后有人屏着呼吸。\n撬棍很沉。现在再说走错门，恐怕不太礼貌。','401 · The lock','Dust fills the keyhole. Someone is holding their breath inside.\nThe crowbar is heavy. “Wrong door” would be impolite at this point.'],
 'prep-manual':['维修说明','油、维修芯、封装匣。小字写着“仅限应急”。\n雨果看了眼401。应急，大概是个很宽泛的词。','Repair leaflet','Oil, repair core, sealed case. In small print: FOR EMERGENCIES ONLY.\nHugo thinks of 401. A generous definition of emergency.'],
 'controls':['','R 记下线索    E 使用    Enter / Esc 收起','','R Record a clue    E Use    Enter / Esc Close'],
 'prepared':['已封装','余烬安静下来。坏主意有了个体面的外壳。','Sealed','The ember settles. The bad idea has a respectable case.'],
 'neighbor-order':['净化分配令','401、管井、第三夜。三个词，挤在同一行。\n邻居只负责执行。有人把整栋楼写进了计划。','Purification assignment','401. Service shaft. Third night. All on the same line.\nThe neighbor is only carrying out an order. Someone has scheduled the whole building.'],
 'pipe-pulse':['墙里的敲击','管道每隔几秒轻响一次。楼上、楼下，依次回应。\n不像漏水。更像点名。','Knocking in the wall','A tap every few seconds. Upstairs and downstairs answer in turn.\nNot a leak. More like a roll call.'],
 'invitation':['邀请函','三角形压在纸角。没有写日期。\n他们大概觉得，迟早的事。','Invitation','A triangle pressed into one corner. No date.\nThey must assume it is only a matter of time.'],
 'venue-exit':['出口','E 离开，回到街口','Exit','E Leave for the street'],
 'sushi-venue':['宫崎寿司 · 店内','桌椅摆得很整齐。每一张嘴，都像在等位。\nWASD 移动 · 鼠标观察 · E 从门口离开','Miyazaki Sushi · Inside','The tables are neatly set. Every mouth seems to be waiting.\nWASD Move · Mouse Look · E Leave at the door'],
 'circus-venue':['威尔马戏团 · 帐内','幕布没有拉开。笼子里的声音，倒像已经入座。\nWASD 移动 · 鼠标观察 · E 从门口离开','Will Circus · Inside','The curtain is closed. The sounds inside the cages have taken their seats.\nWASD Move · Mouse Look · E Leave at the door'],
 'collected':['已收好','放进背包了。','Put away','In the bag.'],
 'recorded':['已记下','纸至少不会装作没看见。','Recorded','Paper, at least, will not pretend it saw nothing.'],
 'combat-controls':['','WASD 移动 · 鼠标观察 · Shift 冲刺\nQ 撬棍 · G 余烬匣 · V 肩眼','','WASD Move · Mouse Look · Shift Sprint\nQ Crowbar · G Ember case · V Shoulder eye']
};
for(const [file,index] of [['StoryText.json',0],['StoryTextEnglish.json',2]]){
 const path=root+'Resources/'+file,d=JSON.parse(fs.readFileSync(path,'utf8'));
 for(const [id,v] of Object.entries(copy)){let e=d.neighborSlice.entries.find(x=>x.id===id);if(!e)d.neighborSlice.entries.push(e={id});e.title=v[index];e.body=v[index+1];}
 fs.writeFileSync(path,JSON.stringify(d,null,2)+'\n');
}
const cityCopy={
 'caretaker':['值班人','他把钥匙攥得很紧。\n先让他相信，你是在问路，不是在挑选下一个房间。','Caretaker','He grips the keys.\nConvince him you are asking directions, not choosing the next room.'],
 'city-maintenance':['交接簿','第三夜，管井供能，401。\n值班人在旁边写了“已通知”。通知算不算救过人，要看谁填表。','Handover ledger','Third night. Service shaft supply. 401.\nSomeone has written NOTIFIED beside it. Whether that counts as saving a life depends on who fills in the form.'],
 'city-fuse':['接线图','几条线路绕开了住户的总闸。\n关灯不等于断电。也不等于天亮。','Wiring diagram','Several lines bypass the residential breaker.\nTurning out the lights is not the same as cutting the power. Or reaching morning.'],
 'city-orders':['搜捕令','名单上没有罪名，只有地址。\n广播会替他们解释。','Search orders','Addresses, no charges.\nThe broadcast will explain for them.'],
 'city-routing':['播送路由','同一段话，被送往每一条街。\n只要声音够整齐，就像所有人都同意。','Broadcast routing','The same words are routed to every street.\nEnough voices in unison can sound like agreement.'],
 'city-spectrum':['观测记录','曲线在同一秒偏折。仪器没有坏。\n坏消息通常很准时。','Observation log','Every curve bends at the same second. The instruments are working.\nBad news tends to be punctual.'],
 'city-lens':['阵列手册','校准、对齐、锁定。\n他们给每一步都起了个让人安心的名字。','Array manual','Calibrate. Align. Lock.\nThey gave every step a reassuring name.']
};
for(const [file,index]of [['StoryText.json',0],['StoryTextEnglish.json',2]]){
 const path=root+'Resources/'+file,d=JSON.parse(fs.readFileSync(path,'utf8'));
 for(const [id,v]of Object.entries(cityCopy)){const e=d.city.entries.find(x=>x.id===id);e.title=v[index];e.body=v[index+1];}
 for(const e of d.city.entries)e.success=index===0?'已记下。':'Recorded.';
 fs.writeFileSync(path,JSON.stringify(d,null,2)+'\n');
}
