using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Interaction
{
    public sealed class FirstLoopPrologue : MonoBehaviour
    {
        public static FirstLoopPrologue Instance {get;private set;}
        public static bool WorldBlocked => Instance!=null && (Instance.IsPresenting || Time.frameCount<=Instance.blockedUntilFrame);
        public bool IsPresenting => clip!=Clip.None || titleOpen;
        public bool ChapterTitleOpen => titleOpen;
        enum Clip {None,Flyers,Boards}
        Clip clip;
        float elapsed,titleElapsed;
        SpriteRenderer[] actorRenderers;
        bool entryPending,titleOpen;
        int openedFrame,blockedUntilFrame=-1;
        FirstLoopPrologueContent content;
        GameObject scenery,actor;
        readonly GameObject[] boards=new GameObject[4];
        Sprite square;Texture2D pixel;Material material;
        GUIStyle text,title,button;
        bool FirstLoop => GameManager.Instance!=null&&GameManager.Instance.LoopCount==0&&!GameManager.Instance.HasEnded;
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(this);return;}
            Instance=this;content=Resources.Load<FirstLoopPrologueContent>("FirstLoopPrologue");
            if(content==null)content=ScriptableObject.CreateInstance<FirstLoopPrologueContent>();
        }
        void OnEnable(){SceneManager.sceneLoaded+=OnLoaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=ResetJourney;}
        void OnDisable(){SceneManager.sceneLoaded-=OnLoaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=ResetJourney;ResetJourney();}
        void ResetJourney(){clip=Clip.None;titleOpen=entryPending=false;elapsed=titleElapsed=0;blockedUntilFrame=Time.frameCount;if(scenery!=null)Destroy(scenery);actor=null;actorRenderers=null;}
        void OnLoaded(Scene scene,LoadSceneMode mode)
        {
            clip=Clip.None;titleOpen=false;entryPending=false;blockedUntilFrame=-1;
            if(scenery!=null)Destroy(scenery);actor=null;
            if(scene.name!="Corridor"||!FirstLoop)return;
            scenery=new GameObject("401 first-loop story props");SceneManager.MoveGameObjectToScene(scenery,scene);
            var floor=GameObject.Find("Corridor tiles");material=floor!=null?floor.GetComponent<SpriteRenderer>().sharedMaterial:null;
            EnsureSquare();
            if(TimeManager.Instance.Days>=2)BuildFlyers();
            if(TimeManager.Instance.Days>=1)BuildBoards(GameManager.Instance.State.neighborBarricadeSeen);
            entryPending=true;
        }
        void Update()
        {
            if(!FirstLoop){clip=Clip.None;titleOpen=false;return;}
            if(ProgressionHUD.DebugConsoleOpen)return;
            if(titleOpen)
            {
                titleElapsed+=Time.unscaledDeltaTime;
                if(Time.frameCount<=openedFrame+1)return;
                bool confirm=false;
#if ENABLE_INPUT_SYSTEM
                var keyboard=Keyboard.current;confirm=keyboard!=null&&(keyboard.eKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
                confirm=Input.GetKeyDown(KeyCode.E);
#endif
                if(confirm)EnterPrologue();return;
            }
            if(clip!=Clip.None)
            {
                elapsed+=Time.unscaledDeltaTime;
                if(clip==Clip.Boards&&elapsed<(TimeManager.Instance.Days==1?1.3f:2f))CultNeighbor.Instance?.Hammer();
                if(clip==Clip.Flyers)
                {
                    float run=Mathf.Clamp01((elapsed-2f)/.7f);
                    MoveActor(new Vector3(-3.95f,.93f,0),run);
                    if(elapsed>=4.2f)FinishEncounter();
                }
                else
                {
                    if(actor!=null)
                    {
                        float retreat=TimeManager.Instance.Days==1?0:Mathf.Clamp01((elapsed-2f)/.8f);
                        MoveActor(new Vector3(-4.1f,.98f,0),retreat);
                        var head=actor.transform.Find("Head");if(head!=null&&TimeManager.Instance.Days==1)head.localRotation=Quaternion.Euler(0,0,elapsed>1.3f?-10:0);
                        var hammer=actor.transform.Find("Hammer head");
                        if(hammer!=null)hammer.localRotation=Quaternion.Euler(0,0,retreat==0&&elapsed<1.3f?Mathf.Sin(elapsed*16)*18:0);
                    }
                    for(int i=0;i<3;i++)if(boards[i]!=null)boards[i].SetActive(elapsed>(i+1)*.8f);
                    if(elapsed>=4.5f)FinishEncounter();
                }
                return;
            }
            if(!entryPending||StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy)return;
            entryPending=false;var state=GameManager.Instance.State;
            if(TimeManager.Instance.Days==1&&!state.cultDayOneTalked)StartEncounter(Clip.Boards);
            else if(TimeManager.Instance.Days>=2&&!state.neighborBarricadeSeen)StartEncounter(Clip.Boards);
        }
        void MoveActor(Vector3 start,float progress)
        {
            if(actor==null)return;
            float t=Mathf.SmoothStep(0,1,progress);
            actor.transform.position=Vector3.Lerp(start,new Vector3(-4.7f,1.65f,0),t)+Vector3.up*(Mathf.Sin(progress*Mathf.PI*8)*.018f*(1-t));
            // Keep human proportions; disappear behind the doorway only at the end.
            float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1,progress));
            if(actorRenderers!=null)foreach(var renderer in actorRenderers){var color=renderer.color;color.a=alpha;renderer.color=color;}
        }
        void StartEncounter(Clip next)
        {
            clip=next;elapsed=0;
            if(content.neighborPrefab!=null){actor=Instantiate(content.neighborPrefab,scenery.transform);actor.name="401 eccentric neighbor";}
            else {actor=new GameObject("401 eccentric neighbor");actor.transform.SetParent(scenery.transform);Part("Coat",-.0f,.0f,.4f,.65f,new Color(.32f,.25f,.20f),12,actor.transform);}
            actor.transform.position=next==Clip.Flyers?new Vector3(-3.95f,.93f,0):new Vector3(-4.1f,.98f,0);
            actor.transform.localScale=Vector3.one*.85f;
            if(next==Clip.Boards)
            {
                var flyer=actor.transform.Find("Flyer in hand");if(flyer!=null)flyer.gameObject.SetActive(false);
                Part("Hammer grip",.28f,.08f,.045f,.22f,new Color(.40f,.27f,.16f),16,actor.transform,true);
                Part("Hammer head",.28f,.20f,.18f,.065f,new Color(.40f,.43f,.42f),17,actor.transform,true);
            }
            actorRenderers=actor.GetComponentsInChildren<SpriteRenderer>();
        }
        void FinishEncounter()
        {
            var state=GameManager.Instance.State;
            if(clip==Clip.Flyers){state.neighborFlyersSeen=true;ProgressionSave.Write(state);}
            else {if(TimeManager.Instance.Days==1)state.neighborFlyersSeen=true;state.neighborBarricadeSeen=true;if(boards[3]!=null)boards[3].SetActive(true);ProgressionSave.Write(state);}
            clip=Clip.None;blockedUntilFrame=Time.frameCount;if(actor!=null){if(TimeManager.Instance.Days==1){actor.transform.position=new Vector3(-4.1f,.98f,0);foreach(var r in actorRenderers){var c=r.color;c.a=1;r.color=c;}}else Destroy(actor);}
            if(TimeManager.Instance.Days==1&&!state.cultDayOneTalked)CultNeighbor.Instance?.TalkDayOne();
        }
        // 由电视确认按钮调用；看新闻仍只消耗电视原有的一个时段。
        public string WatchNews()
        {
            var text=StoryText.Current.firstLoop;
            if(!FirstLoop)return text?.newsDefault??"新闻与杂音交替闪过。屏幕里暂时没有新的消息。";
            if(TimeManager.Instance.Days!=2)return TimeManager.Instance.Days==1?(text?.newsDay1??"电视里只有杂音。"):(text?.newsDefault??"新闻与杂音交替闪过。");
            GameManager.Instance.State.skt079NewsSeen=true;
            ProgressionSave.Write(GameManager.Instance.State);return text?.news??"";
        }
        public bool BeginPrologue()
        {
            if(!FirstLoop||GameManager.Instance.State.prologueEntered||TimeManager.Instance.Days!=2||!StoryJournal.IsNight(TimeManager.Instance.Period)||!KnowledgeManager.Instance.Experienced(StoryContent.AncientKnowledgeId))return false;
            titleOpen=true;titleElapsed=0;openedFrame=Time.frameCount;return true;
        }
        public void EnterPrologue()
        {
            if(!titleOpen)return;
            GameManager.Instance.State.prologueEntered=true;titleOpen=false;blockedUntilFrame=Time.frameCount;
            var text=StoryText.Current.firstLoop;
            Record("prologue-entry",text?.chapterTitle??"天穹之后",text?.chapterText??"");
        }
        void Record(string id,string heading,string body)
        {
            var state=GameManager.Instance.State;
            var entry=state.journal.Find(e=>e!=null&&e.id==id);
            bool added=entry==null;
            if(added){entry=new JournalEntry{id=id,title=heading,body=body,isKnowledge=false,day=TimeManager.Instance.Days,loop=GameManager.Instance.LoopCount};state.journal.Add(entry);}
            ProgressionSave.Write(state);if(added)JournalUpdates.NotifyAdded(entry);
        }
        void EnsureSquare()
        {
            if(square!=null)return;pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();
            square=Sprite.Create(pixel,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        }
        void BuildFlyers()
        {
            Part("401 door note",-4.48f,1.48f,.26f,.28f,new Color(.84f,.81f,.63f),10,scenery.transform);
            for(int i=0;i<3;i++)Part("Door note writing",-4.48f,1.55f-i*.06f,.17f,.014f,new Color(.35f,.24f,.17f),11,scenery.transform);
        }
        void BuildBoards(bool visible)
        {
            for(int i=0;i<3;i++)
            {
                float x=i==2?-4.2f:-4.7f,y=i==0?1.43f:i==1?1.87f:1.65f;
                var plank=Part("401 barricade plank "+i,x,y,i==2?.12f:1.42f,i==2?.57f:.13f,new Color(.48f,.32f,.18f),14,scenery.transform);
                boards[i]=plank;plank.SetActive(visible);
                for(int nail=0;nail<2;nail++)Part("Nail",i==2?0:(nail==0?-.58f:.58f),i==2?(nail==0?-.21f:.21f):0,.028f,.028f,new Color(.18f,.19f,.18f),15,plank.transform,true);
            }
            // 邻居钉完门后，门下方又会多出一块从里面补上的木板。
            var extra=Part("401 barricade plank extra",-4.48f,1.16f,1.08f,.12f,new Color(.38f,.25f,.14f),14,scenery.transform);
            boards[3]=extra;extra.SetActive(visible);
            Part("Nail",-4.05f,1.16f,.028f,.028f,new Color(.18f,.19f,.18f),15,extra.transform,true);
        }
        GameObject Part(string name,float x,float y,float width,float height,Color color,int order,Transform parent,bool local=false)
        {
            var piece=new GameObject(name);piece.transform.SetParent(parent,false);
            if(local)piece.transform.localPosition=new Vector3(x/parent.lossyScale.x,y/parent.lossyScale.y,0);else piece.transform.position=new Vector3(x,y,0);
            // 保持子部件的物理尺寸，避免木板父缩放将钉子压成线。
            piece.transform.localScale=local?new Vector3(width/parent.lossyScale.x,height/parent.lossyScale.y,1):new Vector3(width,height,1);
            var renderer=piece.AddComponent<SpriteRenderer>();renderer.sprite=square;renderer.color=color;renderer.sortingOrder=order;if(material!=null)renderer.sharedMaterial=material;return piece;
        }
        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(text,title,button);
            if(!IsPresenting||ProgressionHUD.DebugConsoleOpen)return;
            if(text==null)
            {
                var font=DeathBySeventhsDawn.Progression.GameTypography.Current;
                text=new GUIStyle(GUI.skin.label){font=font,fontSize=25,wordWrap=true,alignment=TextAnchor.MiddleCenter};
                title=new GUIStyle(text){fontSize=42,fontStyle=FontStyle.Bold};button=new GUIStyle(GUIStyle.none){font=font,fontSize=22,alignment=TextAnchor.MiddleCenter};
                button.normal.textColor=new Color(.74f,.78f,.66f);button.hover.textColor=new Color(.96f,.94f,.82f);button.active.textColor=Color.white;
            }
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(button);var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;float scale=Mathf.Clamp(Screen.height/900f,.8f,2.4f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.depth=-40;float width=Screen.width/scale,height=Screen.height/scale;
            try
            {
                if(titleOpen)
                {
                    GUI.color=new Color(.025f,.035f,.045f,1);GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=new Color(.88f,.87f,.79f,Mathf.SmoothStep(0,1,titleElapsed/.8f));
                    float panel=Mathf.Min(850,width-48);GUI.Label(new Rect((width-panel)/2,height*.18f,panel,50),"序  章",text);
                    GUI.Label(new Rect((width-panel)/2,height*.29f,panel,80),StoryText.Current.firstLoop?.chapterTitle??"天穹之后",title);
                    GUI.Label(new Rect((width-panel)/2,height*.44f,panel,180),StoryText.Current.firstLoop?.chapterText??"",text);
                    if(GUI.Button(new Rect(width/2-160,height*.78f,320,58),"进入序章  ·  E",button))EnterPrologue();
                }
                else
                {
                    float panel=Mathf.Min(860,width-48),left=(width-panel)/2;
                    GUI.color=new Color(.035f,.045f,.045f,.91f);GUI.DrawTexture(new Rect(left,height-148,panel,112),Texture2D.whiteTexture);
                    GUI.color=new Color(.55f,.60f,.48f,.8f);GUI.DrawTexture(new Rect(left,height-148,3,112),Texture2D.whiteTexture);GUI.color=color;
                    var loopText=StoryText.Current.firstLoop;
                    GUI.Label(new Rect(left+26,height-138,panel-52,92),clip==Clip.Boards?(TimeManager.Instance.Days==1&&elapsed>=1.3f?(loopText?.startledCaption??""):(loopText?.boardingCaption??"")):elapsed<2f?(loopText?.dayOneCaption??""):(loopText?.startledCaption??""),text);
                }
            }
            finally {GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
        void OnDestroy(){if(Instance==this)Instance=null;if(square!=null)Destroy(square);if(pixel!=null)Destroy(pixel);}
    }
}
