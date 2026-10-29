using UnityEngine;
namespace DeathBySeventhsDawn.Interaction
{
    [CreateAssetMenu(menuName="Death by Seventh's Dawn/401 first-loop prologue")]
    public sealed class FirstLoopPrologueContent : ScriptableObject
    {
        public GameObject neighborPrefab;
        [TextArea(3,10)] public string news="本台消息：代号为 SKT-079 的超新星脉冲预计将扫过地球，可能带来不同程度的电子信号波动。相关观测仍在持续。\n\n播报声忽然失真，画面闪过一道横纹。\n\n我望向阳台。那台望远镜仍然架在老地方。";
        [TextArea(2,6)] public string flyer="传单上的字写得很用力：\n\n把窗户遮起来。不要相信那些平静的声音。\n天上有什么东西要来了。";
        [TextArea(2,6)] public string dayOneCaption="401 的住户正把一张传单贴在墙上，嘴里不住地念着什么。";
        [TextArea(2,6)] public string startledCaption="他看见了我。\n手里的动作猛地停住，随即转身跑回了 401。";
        [TextArea(2,6)] public string boardingCaption="401 的怪人正在往门上钉木板。\n他没有回答我，只把最后一块木板牢牢封在门上。";
        public string chapterTitle="天穹之后";
        [TextArea(2,6)] public string chapterText="我原本只是想观察星空。\n\n可我看到的不是任何一种天体。\n那道轮廓之后，似乎有目光正在看着我。";
    }
}
