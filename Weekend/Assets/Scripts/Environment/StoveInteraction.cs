using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;

namespace DeathBySeventhsDawn.Interaction
{
    // Owns only the existing stove's on/off texture; no new cooking economy.
    public sealed class StoveInteraction : MonoBehaviour
    {
        GameObject stove, player;
        SpriteRenderer image;
        Sprite original;
        Sprite[] lit;
        Texture2D[] frames;
        public bool IsLit => GameManager.Instance != null && GameManager.Instance.State.stoveLit;
        public bool CanUse => stove != null && player != null && GameManager.Instance != null &&
            !GameManager.Instance.HasEnded && GameManager.Instance.State.tutorialComplete &&
            !StoryJournal.WorldBlocked && !FurnitureInteraction.IsBusy && !MainMenuUI.WorldBlocked &&
            !PauseMenuUI.WorldBlocked && !GameSettingsUI.WorldBlocked && !ProgressionHUD.DebugConsoleOpen && PropReach.Gap(stove, player) < .22f && image != null && player.transform.position.x > image.bounds.center.x;
        void OnEnable() { SceneManager.sceneLoaded += Loaded; }
        void Start() => Bind();
        void OnDisable() { SceneManager.sceneLoaded -= Loaded; Clear(); }
        void Loaded(Scene s, LoadSceneMode m) => Bind();
        void Clear()
        {
            if (image != null && original != null) image.sprite = original;
            if (lit != null) foreach(var s in lit) if(s != null) Destroy(s);
            if (frames != null) foreach(var t in frames) if(t != null) Destroy(t);
            stove = player = null; image = null; original = null; lit = null; frames = null;
        }
        void Bind()
        {
            Clear();
            if(SceneManager.GetActiveScene().name != "LivingRoom") return;
            stove=GameObject.Find("Kitchen counter and stove"); player=GameObject.FindWithTag("Player");
            image=stove != null ? stove.GetComponent<SpriteRenderer>() : null;
            if(image != null) original=image.sprite;
        }
        public bool Toggle()
        {
            if(!CanUse || original == null) return false;
            if(lit == null) BuildFrames();
            GameManager.Instance.State.stoveLit = !IsLit;
            ProgressionSave.Write(GameManager.Instance.State);
            Apply(); ApartmentAudio.Instance?.PlayUiPress(); return true;
        }
        void BuildFrames()
        {
            frames=new Texture2D[2]; lit=new Sprite[2];
            var previous=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(original.texture.width,original.texture.height,0,RenderTextureFormat.ARGB32);
            try
            {
                Graphics.Blit(original.texture,rt); RenderTexture.active=rt;
                for(int f=0;f<2;f++)
                {
                    var t=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
                    t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
                    var area=original.rect;
                    foreach(float x in new[]{.625f,.79f}) foreach(float y in new[]{.54f,.80f})
                    {
                        int cx=(int)(area.x+area.width*x),cy=(int)(area.y+area.height*y);
                        int radius=Mathf.Max(2,Mathf.RoundToInt(area.width*.018f));
                        for(int dy=-radius;dy<=radius*2;dy++) for(int dx=-radius;dx<=radius;dx++)
                        {
                            int px=cx+dx,py=cy+dy;
                            bool flame=Mathf.Abs(dx)<radius && dy>=0 && dy<(radius*2-Mathf.Abs(dx)*2+((dx+f)%2));
                            if(flame&&px>=0&&px<t.width&&py>=0&&py<t.height)
                                t.SetPixel(px,py,dy<radius/2?new Color(.68f,.89f,1):new Color(.16f,.43f,.91f));
                        }
                    }
                    t.Apply(); frames[f]=t;
                    lit[f]=Sprite.Create(t,area,new Vector2(original.pivot.x/area.width,original.pivot.y/area.height),original.pixelsPerUnit,0,SpriteMeshType.FullRect,original.border);
                    lit[f].name="Stove flame texture "+f;
                }
            }
            finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); }
        }
        void Apply()
        {
            if(image==null||original==null) return;
            if(IsLit && lit==null) BuildFrames();
            image.sprite=IsLit?lit[(int)(UnityEngine.Time.unscaledTime*5)%2]:original;
        }
        void Update()
        {
            Apply();
            if(CanUse && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && WorldPromptUI.CanActivate("stove")) Toggle();
        }
        void OnGUI()
        {
            if(CanUse) WorldPromptUI.Offer("stove",StoryText.Local(IsLit?"E  关火":"E  开火",IsLit?"E  Turn off stove":"E  Light stove"),60);
        }
    }
}
