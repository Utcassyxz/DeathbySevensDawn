using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Interaction
{
    // 所有剧情弹窗统一占用输入，避免看图、读日记或梦境期间仍然走路/触发家具。
    public sealed class StoryJournal : MonoBehaviour
    {
        static bool modalOpen;
        static int blockedThroughFrame=-1;
        // 关闭弹窗的这一帧仍占用输入，避免同一个 E 又打开床、窗户或望远镜。
        public static bool WorldBlocked => modalOpen || Time.frameCount<=blockedThroughFrame;
        enum Page { None, Dream, Diary, Telescope }
        Page page;
        StoryContent content;
        int dreamPage, selectedEntry;
        Vector2 scroll;
        GameObject telescope, player;
        bool nearby, revelation;
        string telescopeMessage;
        Texture2D ancientImage;
        GUIStyle text,title,button,paperText,paperTitle;
        Texture2D whitePixel;
        Sprite square;
        public static bool IsNight(TimePeriod period) => period==TimePeriod.Evening || period==TimePeriod.Night;
        void Awake()
        {
            content=Resources.Load<StoryContent>("StoryContent");
            if(content==null) content=ScriptableObject.CreateInstance<StoryContent>();
            ancientImage=Resources.Load<Texture2D>(content.ancientGodImageResource);
            // 补齐旧版本已解锁知识的日记，不发放第二次知识奖励。
            var state=GameManager.Instance.State;
            if(state.journal==null) state.journal=new System.Collections.Generic.List<JournalEntry>();
            foreach(string id in state.knowledgeIds) {
                if(state.journal.Exists(e=>e!=null && e.id==id)) continue;
                content.Describe(id,out string heading,out string body,out string image);
                state.journal.Add(new JournalEntry {id=id,title=heading,body=body,imageResource=image,tier=id==StoryContent.AncientKnowledgeId?content.ancientGodTier:KnowledgeTier.Ordinary,day=id==StoryContent.WindowKnowledgeId || id==StoryContent.AncientKnowledgeId?2:1,loop=state.loopCount});
            }
            ProgressionSave.Write(state);
        }
        void OnEnable() { SceneManager.sceneLoaded+=OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded-=OnLoaded; SetPage(Page.None); }
        void OnLoaded(Scene scene,LoadSceneMode mode)
        {
            if(page!=Page.Dream) SetPage(Page.None);
            player=GameObject.FindWithTag("Player"); telescope=null; nearby=false;
            if(scene.name=="Balcony") BuildTelescope(scene);
        }
        void SetPage(Page next)
        {
            if(page!=Page.None && next==Page.None) blockedThroughFrame=Time.frameCount;
            page=next; modalOpen=next!=Page.None; scroll=Vector2.zero;
        }
        void Update()
        {
            var game=GameManager.Instance;
            if(game==null) return;
            if(game.HasEnded) { SetPage(Page.None); return; }
            if(!game.State.dreamSeen && page!=Page.Dream) { dreamPage=0; SetPage(Page.Dream); }
            if(ProgressionHUD.DebugConsoleOpen) return;
            bool diary=false,interact=false,cancel=false;
#if ENABLE_INPUT_SYSTEM
            var keyboard=Keyboard.current;
            if(keyboard!=null) { diary=keyboard.jKey.wasPressedThisFrame; interact=keyboard.eKey.wasPressedThisFrame; cancel=keyboard.escapeKey.wasPressedThisFrame; }
#elif ENABLE_LEGACY_INPUT_MANAGER
            diary=Input.GetKeyDown(KeyCode.J); interact=Input.GetKeyDown(KeyCode.E); cancel=Input.GetKeyDown(KeyCode.Escape);
#endif
            if(page==Page.Dream) return;
            if(page==Page.Diary) { if(diary || cancel) SetPage(Page.None); return; }
            if(page==Page.Telescope) { if(interact || cancel) SetPage(Page.None); return; }
            var window=Object.FindAnyObjectByType<WindowLookout2D>();
            if(FurnitureInteraction.IsBusy || (window!=null && window.IsOpen)) { nearby=false; return; }
            if(diary) { selectedEntry=Mathf.Max(0,game.State.journal.Count-1); SetPage(Page.Diary); return; }
            if(player==null) player=GameObject.FindWithTag("Player");
            nearby=telescope!=null && player!=null && Vector2.Distance(player.transform.position,telescope.transform.position)<=content.telescopeRange;
            if(nearby && interact) OpenTelescope();
        }
        void WakeUp()
        {
            var state=GameManager.Instance.State;
            state.dreamSeen=true;
            if(!state.journal.Exists(e=>e!=null && e.id=="opening-dream")) state.journal.Add(new JournalEntry {id="opening-dream",title="梦里的蚁穴",body="我梦见小时候与父母争吵，愤怒中踹塌了一个蚁穴。随后，我看见黑暗里有什么东西抬起了脚。\n\n醒来时，我的心跳仍未平复。",isKnowledge=false,day=1,loop=state.loopCount});
            ProgressionSave.Write(state);
            SetPage(Page.None);
            // 正式新旅程醒在卧室；若未加入构建列表，保持当前场景，不破坏编辑器测试。
            if(Application.CanStreamedLevelBeLoaded("Bedroom")) SceneManager.LoadScene("Bedroom");
        }
        void OpenTelescope()
        {
            var clock=TimeManager.Instance;
            revelation=false;
            if(!IsNight(clock.Period)) telescopeMessage="白天的光太强，镜头里只有刺眼的反光。\n等到傍晚或深夜，再来使用望远镜。";
            else if(clock.Days!=2) telescopeMessage="我调整了焦距。远处只有沉默的楼群与星光。\n今晚，没有发现新的异常。";
            else {
                revelation=true;
                bool gained=KnowledgeManager.Instance.Unlock(StoryContent.AncientKnowledgeId,content.ancientGodTier);
                telescopeMessage=content.discoveryText+ (gained?"\n\n获得高级知识：天穹后的轮廓。已写入日记。":"\n\n这道轮廓已经记在日记里了。没有重复获得知识。");
            }
            SetPage(Page.Telescope);
        }
        void BuildTelescope(Scene scene)
        {
            // 【改望远镜位置】在 Resources/StoryContent.asset 调 telescopePosition。
            // 暂时用简单图形组成望远镜；以后可替换成美术 prefab，保留互动中心即可。
            telescope=new GameObject("阳台望远镜"); SceneManager.MoveGameObjectToScene(telescope,scene);
            telescope.transform.position=content.telescopePosition;
            if(square==null) { whitePixel=new Texture2D(1,1); whitePixel.SetPixel(0,0,Color.white); whitePixel.Apply(); square=Sprite.Create(whitePixel,new Rect(0,0,1,1),new Vector2(.5f,.5f),1); }
            Part("镜筒",new Vector2(0,.3f),new Vector2(.75f,.22f),new Color(.25f,.52f,.55f),-22);
            Part("物镜",new Vector2(.34f,.44f),new Vector2(.12f,.32f),new Color(.66f,.81f,.85f),-22);
            Part("目镜",new Vector2(-.37f,.15f),new Vector2(.16f,.12f),new Color(.10f,.14f,.17f),-22);
            Part("三脚架左",new Vector2(-.14f,-.18f),new Vector2(.065f,.7f),new Color(.56f,.48f,.31f),-25);
            Part("三脚架右",new Vector2(.14f,-.18f),new Vector2(.065f,.7f),new Color(.56f,.48f,.31f),25);
            Part("支架",new Vector2(0,-.05f),new Vector2(.07f,.5f),new Color(.45f,.4f,.3f),0);
        }
        void Part(string name,Vector2 position,Vector2 size,Color color,float angle)
        {
            var part=new GameObject(name); part.transform.SetParent(telescope.transform,false); part.transform.localPosition=position;
            part.transform.localScale=new Vector3(size.x,size.y,1); part.transform.localRotation=Quaternion.Euler(0,0,angle);
            var renderer=part.AddComponent<SpriteRenderer>(); renderer.sprite=square; renderer.color=color; renderer.sortingOrder=10;
            var tiles=GameObject.Find("Balcony tiles"); if(tiles!=null) renderer.sharedMaterial=tiles.GetComponent<SpriteRenderer>().sharedMaterial;
        }
        void Styles()
        {
            if(text!=null) return;
            var font=Font.CreateDynamicFontFromOSFont(new[] {"Microsoft YaHei","SimHei","Noto Sans CJK SC"},26);
            text=new GUIStyle(GUI.skin.label) {font=font,fontSize=26,wordWrap=true,padding=new RectOffset(18,18,12,12)};
            title=new GUIStyle(text) {fontSize=32,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button) {font=font,fontSize=24,wordWrap=true,padding=new RectOffset(12,12,14,14)};
            paperText=new GUIStyle(text); paperText.normal.textColor=new Color(.19f,.12f,.07f);
            paperTitle=new GUIStyle(title); paperTitle.normal.textColor=new Color(.19f,.12f,.07f);
        }
        void OnGUI()
        {
            var game=GameManager.Instance;
            if(game==null || game.HasEnded || ProgressionHUD.DebugConsoleOpen) return;
            Styles(); var matrix=GUI.matrix; var color=GUI.color;
            float scale=Mathf.Clamp(Screen.height/900f,.8f,2.4f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;
            try {
                if(page==Page.None) {
                    if(!FurnitureInteraction.IsBusy) GUI.Label(new Rect(width-235,12,220,55),"J  翻开日记",text);
                    if(nearby) GUI.Label(new Rect(12,height-100,width-24,80),IsNight(TimeManager.Instance.Period)?"E  使用望远镜":"望远镜：傍晚、深夜可用",text);
                    return;
                }
                GUI.depth=-20;
                GUI.color=page==Page.Diary?new Color(.87f,.81f,.66f,1):new Color(.025f,.03f,.05f,1);
                GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture); GUI.color=color;
                float panel=Mathf.Min(1120,width-24);
                GUILayout.BeginArea(new Rect((width-panel)/2,12,panel,height-24));
                scroll=GUILayout.BeginScrollView(scroll);
                if(page==Page.Dream) {
                    GUILayout.Space(50); GUILayout.Label("梦 · 蚁穴",title);
                    var pages=content.dreamPages;
                    GUILayout.Label(pages!=null && pages.Length>0?pages[Mathf.Clamp(dreamPage,0,pages.Length-1)]:"我从一个关于蚁穴的梦中惊醒。",text);
                    GUILayout.Space(30);
                    bool last=pages==null || dreamPage>=pages.Length-1;
                    if(GUILayout.Button(last?"惊醒":"继续",button)) { if(last) WakeUp(); else {dreamPage++; scroll=Vector2.zero;} }
                } else if(page==Page.Diary) {
                    GUILayout.Label("我的日记",paperTitle);
                    var entries=game.State.journal;
                    if(entries.Count==0) GUILayout.Label("纸页还是空白的。",paperText);
                    else {
                        selectedEntry=Mathf.Clamp(selectedEntry,0,entries.Count-1);
                        for(int i=0;i<entries.Count;i++) if(GUILayout.Button((i==selectedEntry?"▶ ":"")+entries[i].title,button)) selectedEntry=i;
                        var entry=entries[selectedEntry];
                        GUILayout.Space(20); GUILayout.Label(entry.title,paperTitle);
                        GUILayout.Label($"第 {entry.loop+1} 次旅程 · 第 {entry.day} 天"+(entry.isKnowledge?" · "+(entry.tier==KnowledgeTier.Cosmic?"高级知识（宇宙）":entry.tier==KnowledgeTier.Forbidden?"禁忌知识":"普通知识"):" · 梦境记录"),paperText);
                        GUILayout.Label(entry.body,paperText);
                        if(!string.IsNullOrEmpty(entry.imageResource)) DrawImage(Resources.Load<Texture2D>(entry.imageResource),panel-48);
                    }
                    if(GUILayout.Button("合上日记（J / Esc）",button)) SetPage(Page.None);
                } else {
                    GUILayout.Label(revelation?"第二夜 · 天穹后的轮廓":"望远镜",title);
                    if(revelation) DrawImage(ancientImage,panel-48);
                    GUILayout.Label(telescopeMessage,text);
                    if(GUILayout.Button("移开视线（E / Esc）",button)) SetPage(Page.None);
                }
                GUILayout.EndScrollView(); GUILayout.EndArea();
            } finally { GUI.matrix=matrix; GUI.color=color; GUI.depth=0; }
        }
        void DrawImage(Texture image,float maxWidth)
        {
            if(image==null) { GUILayout.Label("古神图像未加载，请检查 Resources/Story/AncientGodDay2。",text); return; }
            float imageHeight=Mathf.Min(420,maxWidth*image.height/image.width);
            var rect=GUILayoutUtility.GetRect(maxWidth,imageHeight);
            GUI.DrawTexture(rect,image,ScaleMode.ScaleToFit);
        }
        void OnDestroy() { modalOpen=false; blockedThroughFrame=-1; if(square!=null) Destroy(square); if(whitePixel!=null) Destroy(whitePixel); }
    }
}