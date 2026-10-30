using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    [DefaultExecutionOrder(-80)]
    public sealed class CultNeighbor : MonoBehaviour
    {
        public const string Token="foil-triangle";
        public static CultNeighbor Instance{get;private set;}
        enum View{None,DayOne,Door,Refusal,Bag}
        View view; int line,frame,blockedFrame=-1;float age,cueTime,nextHit;string prompt;
        AudioSource impacts;AudioClip hammer,knock;Texture2D eyeArt,tokenArt;GUIStyle text,heading,button,small;
        ProgressionState State=>GameManager.Instance.State;
        public static bool WorldBlocked=>Instance!=null&&(Instance.view!=View.None||Time.frameCount<=Instance.blockedFrame);
        public bool DoorOpen=>view==View.Door;
        public int ImpactCount{get;private set;}
        public bool BagOpen=>view==View.Bag;
        public bool RouteEligible=>State.cultDayOneTalked&&State.inventory.Contains(Token);
        public bool HasToken=>State.inventory.Contains(Token);
        void Awake(){Instance=this;impacts=gameObject.AddComponent<AudioSource>();impacts.playOnAwake=false;impacts.spatialBlend=0;impacts.volume=.52f;hammer=Resources.Load<AudioClip>("Audio/WoodHammer");knock=Resources.Load<AudioClip>("Audio/DoorKnock");var mix=Resources.Load<AudioMixer>("ApartmentMix");if(mix!=null){var groups=mix.FindMatchingGroups("Master");if(groups.Length>0)impacts.outputAudioMixerGroup=groups[0];}BuildArt();}
        void OnEnable(){SceneManager.sceneLoaded+=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset+=Reset;}
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;if(GameManager.Instance!=null)GameManager.Instance.JourneyReset-=Reset;Close();}
        void Loaded(Scene s,LoadSceneMode m){Close();}
        void Reset(){Close();cueTime=nextHit=0;}
        void Close(){view=View.None;prompt=null;blockedFrame=Time.frameCount;}
        public void Hammer(){if(Time.unscaledTime>nextHit){Hit(hammer);nextHit=Time.unscaledTime+.8f;}}
        void Hit(AudioClip clip){if(clip==null)return;ImpactCount++;impacts.pitch=1+Mathf.Sin(Time.unscaledTime*7)*.035f;impacts.PlayOneShot(clip);}
        public bool TalkDayOne(){if(TimeManager.Instance.Days!=1||GameManager.Instance.LoopCount!=0||State.cultDayOneTalked)return false;Open(View.DayOne);return true;}
        public bool KnockDoor(){if(TimeManager.Instance.Days!=2||GameManager.Instance.LoopCount!=0)return false;Hit(knock);if(State.cultDayOneTalked&&Investigation.Has("cult-door-note")&&!HasToken){Open(View.Door);if(State.cultTokenOffered)line=StoryText.Current.cult.door.Length;}else Open(View.Refusal);return view==View.Door;}
        public bool TakeToken(){if(view!=View.Door||line<StoryText.Current.cult.door.Length||age<.9f||HasToken)return false;State.inventory.Add(Token);ProgressionSave.Write(State);Close();return true;}
        public void Advance(){if(view==View.DayOne){line++;if(line>=StoryText.Current.cult.dayOne.Length){var actor=GameObject.Find("401 eccentric neighbor");if(actor!=null)Destroy(actor);State.cultDayOneTalked=true;MorningRoutine.Instance?.NeighborTalked();ProgressionSave.Write(State);Close();}}else if(view==View.Door){if(line<StoryText.Current.cult.door.Length){line++;age=0;if(line==StoryText.Current.cult.door.Length){State.cultTokenOffered=true;ProgressionSave.Write(State);}}else TakeToken();}}
        void Open(View value){view=value;line=0;age=0;frame=Time.frameCount;prompt=null;var p=GameObject.FindWithTag("Player");if(p!=null){var rb=p.GetComponent<Rigidbody2D>();if(rb!=null)rb.linearVelocity=Vector2.zero;}}
        void Update(){
            if(GameManager.Instance==null||TimeManager.Instance==null)return;
            age+=Time.unscaledDeltaTime;var k=Keyboard.current;
            if(view!=View.None){if(Time.frameCount<=frame)return;if(k!=null&&k.escapeKey.wasPressedThisFrame){Close();return;}if(k!=null&&k.eKey.wasPressedThisFrame)Advance();return;}
            prompt=null;if(StoryJournal.WorldBlocked||ProgressionHUD.DebugConsoleOpen||FurnitureInteraction.IsBusy||GameManager.Instance.HasEnded)return;
            if(k!=null&&k.iKey.wasPressedThisFrame){Open(View.Bag);return;}
            int day=TimeManager.Instance.Days;string scene=SceneManager.GetActiveScene().name;
            if(day==1&&GameManager.Instance.LoopCount==0&&!State.neighborBarricadeSeen&&(scene=="Bedroom"||scene=="LivingRoom")){cueTime+=Time.unscaledDeltaTime;if(cueTime>4&&Time.unscaledTime>nextHit){Hit(hammer);nextHit=Time.unscaledTime+(cueTime%5<2?.72f:2.1f);}prompt=cueTime>4?StoryText.Current.cult.sound:null;}
            if(scene!="Corridor"||GameManager.Instance.LoopCount!=0)return;
            if(FirstLoopPrologue.Instance!=null&&FirstLoopPrologue.Instance.IsPresenting){if(Time.unscaledTime>nextHit){Hit(hammer);nextHit=Time.unscaledTime+.8f;}return;}
            var player=GameObject.FindWithTag("Player");if(player==null||Vector2.Distance(player.transform.position,new Vector2(-4.7f,1.0f))>.60f)return;
            if(day==1&&!State.cultDayOneTalked){prompt=StoryText.Local("E  和 401 的住户说话","E  Speak to the tenant of 401");if(k!=null&&k.eKey.wasPressedThisFrame)TalkDayOne();}
            else if(day==2){prompt=StoryText.Local(State.cultTokenOffered&&!HasToken?"E  敲门，取回门缝边的小三角":"E  敲 401 的门",State.cultTokenOffered&&!HasToken?"E  Knock; the triangle is by the crack":"E  Knock on 401");if(k!=null&&k.eKey.wasPressedThisFrame)KnockDoor();}
        }
        float lastEye;
        void AnimateEye(){if(Time.unscaledTime-lastEye<.07f)return;lastEye=Time.unscaledTime;
            float openness=line>0?1:Mathf.SmoothStep(0,1,age/1.1f);float gaze=Mathf.Sin(Time.unscaledTime*.8f)*5;
            for(int y=59;y<100;y++)for(int x=67;x<258;x++){
                Color c=new Color(.014f,.014f,.012f);float ex=(x-168)/49f,ey=(y-80)/Mathf.Max(.5f,10*openness);
                float edge=ex*ex+ey*ey;
                if(edge<1){float shade=.72f+Mathf.PerlinNoise(x*.3f,y*.3f)*.15f;c=new Color(.58f,.53f,.44f)*shade;float d=Vector2.Distance(new Vector2(x,y),new Vector2(174+gaze,80));if(d<9)c=new Color(.27f,.23f,.15f);if(d<4)c=new Color(.015f,.018f,.015f);if(d>7&&d<10)c*=.55f;if(x>(int)(175+gaze)&&x<(int)(178+gaze)&&y>81&&y<84)c=new Color(.78f,.76f,.63f);}
                else if(edge<1.28f)c=new Color(.20f,.13f,.09f);
                eyeArt.SetPixel(x,y,c);
            }eyeArt.Apply(false);
        }
        void Styles(){if(text!=null)return;var f=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},24);text=new GUIStyle(GUI.skin.label){font=f,fontSize=23,wordWrap=true};heading=new GUIStyle(text){fontSize=30,fontStyle=FontStyle.Bold};small=new GUIStyle(text){fontSize=17};button=new GUIStyle(GUI.skin.button){font=f,fontSize=20,padding=new RectOffset(16,16,8,8),wordWrap=true};foreach(var style in new[]{text,heading,small})foreach(var st in new[]{style.normal,style.hover,style.active,style.focused})st.textColor=new Color(.87f,.85f,.76f);}
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void OnGUI(){if(GameManager.Instance==null)return;Styles();PrototypeButtons.Apply(button);var matrix=GUI.matrix;int depth=GUI.depth;var color=GUI.color;float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            try{if(view==View.None){if(prompt!=null)GUI.Label(new Rect(w/2-380,h-120,760,90),prompt,text);return;}GUI.depth=-65;Fill(new Rect(0,0,w,h),new Color(.018f,.025f,.026f,.99f));float pw=Mathf.Min(880,w-80);Rect panel=new Rect((w-pw)/2,50,pw,h-100);
                if(view==View.Bag){GUI.Label(new Rect(panel.x,panel.y,pw,50),StoryText.Local("背包","Inventory"),heading);if(HasToken){GUI.DrawTexture(new Rect(panel.x+20,panel.y+70,220,220),tokenArt,ScaleMode.ScaleToFit);GUI.Label(new Rect(panel.x+280,panel.y+90,pw-300,50),StoryText.Current.cult.itemTitle,heading);GUI.Label(new Rect(panel.x+280,panel.y+155,pw-300,150),StoryText.Current.cult.itemBody,text);}else GUI.Label(new Rect(panel.x,panel.y+100,pw,100),StoryText.Local("还没有物品。","Nothing carried."),text);GUI.Label(new Rect(panel.x,h-70,pw,30),StoryText.Local("Esc  合上背包","Esc  Close inventory"),small);return;}
                string prose=view==View.DayOne?StoryText.Current.cult.dayOne[Mathf.Min(line,StoryText.Current.cult.dayOne.Length-1)]:view==View.Refusal?StoryText.Current.cult.refusal:line<StoryText.Current.cult.door.Length?StoryText.Current.cult.door[line]:StoryText.Current.cult.itemBody;
                if(view==View.Door){AnimateEye();float reveal=line>0?1:Mathf.SmoothStep(0,1,age/1.1f);GUI.color=new Color(1,1,1,reveal);GUI.DrawTexture(new Rect(panel.x,50,pw,h*.46f),eyeArt,ScaleMode.ScaleToFit);GUI.color=Color.white;if(line>=StoryText.Current.cult.door.Length){float t=Mathf.Clamp01(age/.9f);float size=Mathf.Lerp(28,145,t);var r=new Rect(w/2-size/2,Mathf.Lerp(h*.30f,h*.48f,t),size,size);GUIUtility.RotateAroundPivot(Mathf.Lerp(-100,0,t),r.center);GUI.DrawTexture(r,tokenArt);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);}}
                float top=view==View.Door?h*.64f:h*.30f;GUI.Label(new Rect(panel.x,top,pw,130),prose,text);GUI.Label(new Rect(panel.x,h-70,pw,30),StoryText.Local(view==View.Refusal?"Esc  离开":view==View.Door&&line>=StoryText.Current.cult.door.Length?"E  拾起并放入背包  ·  Esc  暂时离开":"E  继续  ·  Esc  离开",view==View.Refusal?"Esc  Leave":view==View.Door&&line>=StoryText.Current.cult.door.Length?"E  Take and put in inventory · Esc Leave":"E  Continue · Esc Leave"),small);
            }finally{GUI.matrix=matrix;GUI.depth=depth;GUI.color=color;}}
        void BuildArt(){eyeArt=new Texture2D(320,160,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};for(int y=0;y<160;y++)for(int x=0;x<320;x++){float grain=Mathf.PerlinNoise(x*.15f,y*.06f);Color c=new Color(.18f+grain*.08f,.115f+grain*.06f,.07f+grain*.04f);if(y%39<3)c=new Color(.05f,.035f,.025f);if(x>66&&x<258&&y>59&&y<100){c=new Color(.014f,.014f,.012f);float ex=(x-168)/57f,ey=(y-80)/16f;if(ex*ex+ey*ey<1)c=new Color(.57f,.51f,.35f);float d=Vector2.Distance(new Vector2(x,y),new Vector2(180,80));if(d<15)c=new Color(.18f,.24f,.16f);if(d<7)c=Color.black;if(x>181&&x<185&&y>83&&y<87)c=new Color(.8f,.78f,.6f);}if((x==35||x==285)&&y%39==18)c=new Color(.42f,.43f,.39f);eyeArt.SetPixel(x,y,c);}eyeArt.Apply();
            tokenArt=new Texture2D(96,96,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};for(int y=0;y<96;y++)for(int x=0;x<96;x++){Color c=Color.clear;if(y>15&&y<80&&x>48-(y-15)*.55f&&x<48+(y-15)*.55f){float n=Mathf.PerlinNoise(x*.3f,y*.3f);float v=.45f+n*.42f;c=new Color(v,v*.99f,v*.94f);if(Mathf.Abs(x-48)<2||Mathf.Abs(y-(x*.7f+23))<1.7f)c=new Color(.85f,.86f,.81f);}tokenArt.SetPixel(x,y,c);}tokenArt.Apply();}
        void OnDestroy(){if(Instance==this)Instance=null;if(eyeArt!=null)Destroy(eyeArt);if(tokenArt!=null)Destroy(tokenArt);}
    }
}
