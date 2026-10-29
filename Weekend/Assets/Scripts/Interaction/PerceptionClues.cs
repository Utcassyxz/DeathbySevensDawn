using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Interaction
{
    [DefaultExecutionOrder(-50)]
    public sealed class PerceptionClues : MonoBehaviour
    {
        public static PerceptionClues Instance {get;private set;}
        public static bool IsReading => Instance!=null && (Instance.reading||Time.frameCount<=Instance.blockedFrame);
        public bool FilterOpen => GameManager.Instance!=null && !GameManager.Instance.HasEnded && GameManager.Instance.Sanity<=GameManager.Instance.Settings.fracturedSanity;
        public bool ClueVisible => marker!=null&&marker.activeSelf;
        public string CurrentId=>id;
        public Vector2 CluePosition=>position;
        string id,heading,body;Vector2 position,scroll;GameObject marker,player;Sprite sprite;Texture2D pixel;
        bool reading,nearby;int blockedFrame=-1;GUIStyle text,title,button;
        void Awake(){Instance=this;}
        void OnEnable(){SceneManager.sceneLoaded+=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=ResetReading;}
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=ResetReading;ResetReading();}
        void ResetReading(){reading=nearby=false;scroll=Vector2.zero;blockedFrame=Time.frameCount;if(marker!=null)marker.SetActive(false);}
        void Loaded(Scene scene,LoadSceneMode mode)
        {
            ResetReading();if(marker!=null)Destroy(marker);marker=null;id=null;player=GameObject.FindWithTag("Player");
            switch(scene.name)
            {
                case "Bedroom":id="filter-bedroom-margins";heading="写在边缘的字";position=new Vector2(1.8f,-1.45f);body="桌角那几道划痕，原来是一行挤在一起的小字。\n\n“不是星星在移动。是它的眼睛。”\n\n我用手指沿着字迹摸了一遍。刻痕已经很旧了。";break;
                case "Corridor":id="filter-401-underprint";heading="传单的背面";position=new Vector2(-3.45f,1.25f);body="401 门旁的墙纸底下，还有一层字。\n\n“木板挡不住它。我只是不想再看见。”\n\n传单上的墨迹没有变，为什么刚才我读不到这一句？";break;
                case "Balcony":id="filter-railing-tally";heading="栏杆上的刻痕";position=new Vector2(-1.5f,-1.1f);body="栏杆上有七道很浅的刻痕。最后一道划过其余六道。\n\n下面刻着两个字：\n\n“别数。”";break;
            }
            if(id==null)return;
            marker=new GameObject("Perception clue - "+id);SceneManager.MoveGameObjectToScene(marker,scene);marker.transform.position=position;
            if(sprite==null){pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();sprite=Sprite.Create(pixel,new Rect(0,0,1,1),Vector2.one*.5f,1);}
            // Pixel-sized scratches only: no scene palette, camera or collider changes.
            for(int i=0;i<5;i++){var part=new GameObject("hidden scratch");part.transform.SetParent(marker.transform,false);part.transform.localPosition=new Vector3((i-2)*.08f,(i%2)*.035f,0);part.transform.localScale=new Vector3(.025f,.12f+(i%3)*.025f,1);var r=part.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=new Color(.84f,.76f,.56f);r.sortingOrder=18;}
            marker.SetActive(FilterOpen);
        }
        void Update()
        {
            if(marker!=null)marker.SetActive(FilterOpen);
            if(!FilterOpen){if(reading)Close();nearby=false;return;}
            if(ProgressionHUD.DebugConsoleOpen)return;
            bool pressed=false,cancel=false;
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;if(k!=null){pressed=k.eKey.wasPressedThisFrame;cancel=k.escapeKey.wasPressedThisFrame;}
#elif ENABLE_LEGACY_INPUT_MANAGER
            pressed=Input.GetKeyDown(KeyCode.E);cancel=Input.GetKeyDown(KeyCode.Escape);
#endif
            if(reading){if(pressed||cancel)Close();return;}
            if(StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy){nearby=false;return;}
            var window=Object.FindAnyObjectByType<WindowLookout2D>();if(window!=null&&window.IsOpen){nearby=false;return;}
            if(player==null)player=GameObject.FindWithTag("Player");
            nearby=marker!=null&&player!=null&&Vector2.Distance(player.transform.position,position)<.85f;
            if(nearby&&pressed)TryRead();
        }
        public bool TryRead()
        {
            if(!FilterOpen||marker==null||!nearby||StoryJournal.WorldBlocked||FurnitureInteraction.IsBusy||ProgressionHUD.DebugConsoleOpen)return false;
            reading=true;scroll=Vector2.zero;
            var clue=ScriptableObject.CreateInstance<KnowledgeData>();clue.id=id;clue.title=heading;clue.description=body;clue.tier=KnowledgeTier.Forbidden;clue.overrideEffect=true;clue.effect=new KnowledgeEffect(0,0,0);
            KnowledgeManager.Instance.Unlock(clue);Destroy(clue);return true;
        }
        public void Close(){reading=false;blockedFrame=Time.frameCount;}
        void OnGUI()
        {
            if(ProgressionHUD.DebugConsoleOpen||(!reading&&!nearby))return;
            if(text==null){var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Noto Sans CJK SC"},24);text=new GUIStyle(GUI.skin.label){font=font,fontSize=24,wordWrap=true};title=new GUIStyle(text){fontSize=30,fontStyle=FontStyle.Bold};button=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};button.normal.textColor=new Color(.78f,.80f,.65f);}
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Clamp(Screen.height/900f,.8f,2.4f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            try
            {
                if(!reading){GUI.Label(new Rect(w/2-210,h-80,420,60),"E  辨认那些痕迹",button);return;}
                GUI.depth=-35;GUI.color=new Color(.025f,.035f,.035f,.96f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=color;
                float width=Mathf.Min(820,w-64);GUILayout.BeginArea(new Rect((w-width)/2,h*.18f,width,h*.68f));scroll=GUILayout.BeginScrollView(scroll);
                GUILayout.Label(heading,title);GUILayout.Space(28);GUILayout.Label(body,text);GUILayout.EndScrollView();
                if(GUILayout.Button("记下，移开视线  ·  E / Esc",button,GUILayout.Height(60)))Close();GUILayout.EndArea();
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
        void OnDestroy(){if(Instance==this)Instance=null;if(sprite!=null)Destroy(sprite);if(pixel!=null)Destroy(pixel);}
    }
}
