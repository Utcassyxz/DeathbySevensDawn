from pathlib import Path
import json,re
base=Path(__file__).resolve().parents[2]
source=(base/'Weekend/Assets/Scripts/Development/GameCommandConsole.cs').read_text(encoding='utf-8-sig')
text=json.loads((base/'Weekend/Assets/Resources/StoryText.json').read_text(encoding='utf-8-sig'))
copy={x['key']:x['text'] for x in text['ui']}
syntax=re.findall(r'"([^"]+)"',source.split('public static readonly string[] Syntax = {')[1].split('};')[0])
sceneSource=(base/'Weekend/Assets/Scripts/Development/NeighborDebugCommands.cs').read_text(encoding='utf-8-sig')
scenes=re.findall(r'"([^"]+)"',sceneSource.split('string[] Scenes={')[1].split('};')[0])
events=re.findall(r'"([^"]+)"',source.split('string[] Events = {')[1].split('};')[0])
items={'crowbar':'撬棍','cooking-oil':'食用油','sealed-maintenance-medium':'封存维修芯','sealed-ember-tool':'单次准备工具','foil-triangle':'锡纸三角','invitation':'邀请函','initiation-controller':'启动控制器'}
eventDesc={
'sushi_normal':'寿司店，理智 100 的正常外观。','sushi_corrupt':'寿司店，理智 15 的污染外观。','circus_normal':'马戏团，理智 100 的正常外观。','circus_corrupt':'马戏团，理智 15 的污染外观。',
'neighbor_encounter':'第二天邻居家 2D 敌对入口；靠近住户按 E 开始遭遇。','neighbor_peace':'第二天邻居家沟通入口；所有正式分支最终进入战斗。','neighbor_known':'授予分配令及新闻证据，进入已知命令的对话分支。',
'neighbor_prepared':'授予准备推论与单次工具，进入邻居 3D 战斗；G 使用工具。','neighbor_eye':'授予肩眼与撬棍，进入邻居 3D 战斗；V 肩眼、Q 闪避、鼠标左键攻击。',
'neighbor_death':'播放邻居致死演出。','neighbor_victory':'在 NeighborArena3D 中触发胜利及返回。','apartment_explosion':'爆破演出预览；自由调试开启时不发生正式死亡。','building_saved':'正式规则下第七天检修间干预预设，授予四段前置推理，设备间只供调查；需完成邻居遭遇并取得控制器。'}
lines=['# 《七日》控制台指令手册','', '适用：0.3.3。由实际命令注册表、线索目录及中英文文案生成。', '',
'## 打开与输入','',
'进入游戏后按键盘左上角、数字 1 左边的 波浪号（~） 键直接打开，无需输入旧口令。Enter 执行，Tab 循环补全，↑/↓ 查历史并保留未完成输入，Esc 或 ~ 收起。窗口可滚动；最多保留 200 行输出、100 条输入。指令和 ID 使用英文；动词、场景、物品及事件 ID 不区分大小写，推理节点 ID 请按表原样输入。每次输入一条，不支持分号串联。', '',
'首次打开会复制当前旅程到临时调试会话，并开启自由调试。**收起窗口仍停留在临时旅程**；输入 `session end` 才返回进入前的场景、位置、日期、钟点、物品与剧情状态。调试旅程和收藏使用独立存储，不覆盖正常旅程。关闭整个程序后重新启动走正常存档；不要用重启代替现场恢复。','',
'`debug on` 会跳过强制引导并阻止部分致死危机，适合跳场景；`debug off` 恢复正式剧情/时钟规则。收起窗口后时钟才可继续；需要测试真实危机时先关闭自由调试。`scene` 传送、`event` 预设、`flags reset`、`sanity` 需要自由调试开启。`session end` 在两种模式下都有效。','',
'## 全部指令','', '| 指令 | 作用 |','|---|---|']
for s in syntax: lines.append('| `'+s+'` | '+copy['console.cmd.'+s.split()[0]].replace('|','/').replace('\n',' ')+' |')
lines += ['', '## 常用示例','', '```text','help','help event','status','date 2 23:10','scene Corridor','item give crowbar','evidence unlock prep-oil','sanity 25','event neighbor_eye','debug off','session end','```','',
'以上为独立示例，不是一套连续通关流程。`day` / `date` 直接设置日期，不补齐前几天的剧情；`next period` 使用既有时段结算，`next day` 则直接跳到次日早晨。`sleep` 仍走实际梦境规则。体力归零会测试真实死亡/回档，回档恩赐仍须按剧情获得。已结束旅程不能用 heal 复活；请恢复原旅程或重开临时旅程。','',
'`crisis building off` 将第七日爆破标记为已解除；`on` 撤销该标记。city / cosmic 保留旧系统标记，**不会给当前第一阶段凭空增加旧版第五/第七日危机**。','',
'兼容旧写法：`scene goto Bedroom`、`day set 2`、`time set 0`、`sanity set 100`、`event trigger neighbor_eye`。数值以小数点分隔，禁止 NaN、无穷大和超范围值。','', '## 场景 ID','', '```text',*scenes,'```','',
'这些包含开发用场景。直接传送 3D 场景属于测试，不代表普通探索改为 3D；正式探索仍然是 2D。邻居遭遇及店铺会设置正确的返回场景。','', '## 物品 ID','', '| ID | 物品 |','|---|---|']
lines += ['| `'+k+'` | '+v+' |' for k,v in items.items()]
lines += ['', '`item give` 直接加入临时背包，不重复生成已有物品，也不替代打开壁橱、拾取演出或工具合成的正式流程。','', '## 事件 ID','', '| ID | 实际预设 |','|---|---|']
lines += ['| `'+k+'` | '+eventDesc[k]+' |' for k in events]
lines += ['', '预设会改写临时剧情条件，常会收起控制台；可按 ~ 再打开。它们用于复现单独场景，不保证任意混用后的剧情连续性。','', '## 原始线索 ID','', '`evidence list` 可在游戏中查看实时目录。`evidence unlock <id>` 收集一条原始线索，推论需使用 deduce。','', '| ID | 标题 |','|---|---|']
lines += ['| `'+e['id']+'` | '+e['title']+' |' for e in text['investigation']['evidence']]
catalog=json.loads((base/'Weekend/Assets/Resources/Investigation.json').read_text(encoding='utf-8-sig'))
lines += ['', '## 推理配方','', '用法：`deduce <左节点> <右节点>`。必须已持有两节点；日期、前置推论及传单限制仍生效。','', '| 左节点 | 右节点 | 结论 ID | 最早日 |','|---|---|---|---|']
for d in catalog['deductions']:lines.append('| `'+d['left']+'` | `'+d['right']+'` | `'+d['id']+'` | '+str(max(1,d.get('minDay',0)))+' |')
lines += ['']
content='\n'.join(lines)
(base/'docs/CONSOLE_COMMANDS.md').write_text(content,encoding='utf-8')
out=base.parent/'outputs/控制台指令手册.md';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(content,encoding='utf-8')
print(f'{len(syntax)} commands, {len(scenes)} scenes, {len(items)} items, {len(events)} events, {len(text["investigation"]["evidence"])} evidence, {len(catalog["deductions"])} deductions')
