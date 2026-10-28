using UnityEngine;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
namespace DeathBySeventhsDawn.Interaction
{
    [CreateAssetMenu(menuName="Death by Seventh's Dawn/梦境与望远镜剧情",fileName="StoryContent")]
    public sealed class StoryContent : ScriptableObject
    {
        // 【改开场文字】每个元素是一幕；玩家点击“继续”，最后一幕点击“惊醒”。
        [TextArea(3,10)] public string[] dreamPages={
            "我又回到了小时候。\n\n父母站在路边，争吵声像隔着一层水传来。我也在喊，却记不清自己究竟在争辩什么。",
            "没有人听我说话。\n\n我低下头，看见脚边隆起的蚁穴。细小的蚂蚁沿着入口爬进爬出，仿佛我们的争吵与它们毫无关系。",
            "我猛地踹了过去。\n\n土屑散开，蚁穴塌了。蚂蚁在我的鞋边四散奔逃。父母的声音突然远去，只剩下我急促的呼吸。",
            "黑暗里，我像是看见什么庞大的东西抬起了脚。\n\n我猛然惊醒。房间安静得可怕，心脏还在剧烈跳动。"
        };
        // 【换古神图片】替换 Assets/Resources/Story/AncientGodDay2.png，保留文件名和 .meta。
        // 也可修改下面的路径（Resources 相对路径，不含扩展名），旧日记仍使用原来保存的路径。
        public string ancientGodImageResource="Story/AncientGodDay2";
        [TextArea(2,8)] public string discoveryText="那不是云，也不是山。星光沿着一个不该存在的轮廓弯折。\n我终于明白，天空后面还有某种活着的东西。";
        // 【高级知识配置】默认使用已有的宇宙知识档位：知识+30、变异+20，并降低理智。
        public KnowledgeTier ancientGodTier=KnowledgeTier.Cosmic;
        public Vector2 telescopePosition=new Vector2(2.3f,-1.15f);
        [Min(.2f)] public float telescopeRange=1.2f;
        public const string AncientKnowledgeId="balcony-telescope-day2-ancient-outline";
        public const string WindowKnowledgeId="bedroom-window-day2-observation";
        public void Describe(string id,out string title,out string body,out string image)
        {
            image="";
            if(id==AncientKnowledgeId) { title="天穹后的轮廓"; body=discoveryText; image=ancientGodImageResource; }
            else if(id==WindowKnowledgeId) { title="第二天的窗外观察"; body="今天的窗外似乎与昨天不同。我把那些细小的变化记了下来，也许以后会用得上。"; }
            else { title="新的发现"; body="我记下了一条线索。\n\n线索编号："+id; }
        }
    }
}