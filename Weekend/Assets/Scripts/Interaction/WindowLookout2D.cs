using UnityEngine;
using UnityEngine.UI;
using DeathBySeventhsDawn.Player;
using DeathBySeventhsDawn.TimeSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeathBySeventhsDawn.Interaction
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WindowLookout2D : MonoBehaviour
    {
        [SerializeField] private WindowViewSequence sequence;
        private GameObject player;
        private BoxCollider2D area;
        private PlayerController2D movement;
        private bool restoreMovement;
        private bool discoveredKnowledge;
        private Canvas canvas;
        private GameObject overlay;
        private Text hint, heading, closeInstructions;
        private RawImage picture;
        private AspectRatioFitter aspect;

        public WindowViewSequence Sequence => sequence;
        public bool IsOpen { get; private set; }
        public int DisplayedDay { get; private set; }
        public Texture DisplayedTexture => picture != null ? picture.texture : null;
        public bool CanInteract
        {
            get
            {
                if (player == null) player = GameObject.FindWithTag("Player");
                if (area == null) area = GetComponent<BoxCollider2D>();
                var body = player != null ? player.GetComponent<Collider2D>() : null;
                return body != null && area.bounds.Intersects(body.bounds);
            }
        }

        public void Configure(WindowViewSequence data) { sequence = data; }
        private void Awake() { area = GetComponent<BoxCollider2D>(); }

        private void Update()
        {
            if(Core.GameManager.Instance!=null && Core.GameManager.Instance.HasEnded) { CloseView(); if(hint!=null) hint.gameObject.SetActive(false); return; }
            if(FurnitureInteraction.IsBusy || Progression.ProgressionHUD.DebugConsoleOpen || StoryJournal.WorldBlocked) { if(hint!=null) hint.gameObject.SetActive(false); return; }
            bool interact = false, cancel = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            interact = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            cancel = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            interact = Input.GetKeyDown(KeyCode.E);
            cancel = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (IsOpen)
            {
                #if ENABLE_INPUT_SYSTEM
                if(keyboard!=null&&keyboard.rKey.wasPressedThisFrame)CollectObservation();
#endif
                if (interact || cancel) CloseView();
                else RefreshView();
                return;
            }
            bool nearby = CanInteract;
            if (nearby) EnsureUI();
            if (hint != null) hint.gameObject.SetActive(nearby);
            if (nearby && interact) TryOpen();
        }

        public bool TryOpen()
        {
            if(Core.GameManager.Instance!=null && Core.GameManager.Instance.HasEnded) return false;
            if(FurnitureInteraction.IsBusy || Progression.ProgressionHUD.DebugConsoleOpen || StoryJournal.WorldBlocked) return false;
            if (IsOpen) return true;
            if (sequence == null || !CanInteract) return false;
            EnsureUI();
            movement = player.GetComponent<PlayerController2D>();
            restoreMovement = movement != null && movement.enabled;
            if (restoreMovement) movement.enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            IsOpen = true;
            hint.gameObject.SetActive(false);
            overlay.SetActive(true);
            discoveredKnowledge=false;

            RefreshView();
            return true;
        }

        public bool CollectObservation()
        {
            if(!IsOpen||TimeManager.Instance.Days!=2)return false;
            discoveredKnowledge=Knowledge.KnowledgeManager.Instance.Unlock(StoryContent.WindowKnowledgeId,Progression.KnowledgeTier.Ordinary);
            RefreshView();return discoveredKnowledge;
        }
        public void CloseView()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (overlay != null) overlay.SetActive(false);
            if (restoreMovement && movement != null) movement.enabled = true;
            restoreMovement = false;
        }

        private void RefreshView()
        {
            DisplayedDay = Mathf.Clamp(TimeManager.Instance != null ? TimeManager.Instance.Days : 1, 1, 7);
            var image = sequence.GetImage(DisplayedDay);
            if (picture.texture != image)
            {
                picture.texture = image;
                if (image != null) aspect.aspectRatio = (float)image.width / image.height;
            }
            heading.text = "窗外 · 第 " + DisplayedDay + " 天";
            closeInstructions.text=DisplayedDay==2 ? (Knowledge.Investigation.Has(StoryContent.WindowKnowledgeId)?"窗外观察已收录":"R  记录窗外的变化")+"\nE / Esc  返回房间" : "E / Esc  返回房间";
            foreach (var label in new[] { heading, closeInstructions })
            {
                label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
                label.SetAllDirty();
            }
        }

        private void EnsureUI()
        {
            if (canvas != null) return;
            var root = new GameObject("Window lookout UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" }, 28);
            hint = MakeText("Window hint", root.transform, font, "E  查看窗外", 25,
                new Vector2(.1f, .02f), new Vector2(.9f, .11f));
            overlay = MakeRect("Window view", root.transform, Vector2.zero, Vector2.one);
            var backdrop = overlay.AddComponent<Image>();
            backdrop.color = new Color(.025f, .035f, .045f, .97f);
            backdrop.raycastTarget = false;
            heading = MakeText("Window heading", overlay.transform, font, "", 28,
                new Vector2(.06f, .88f), new Vector2(.94f, .98f));
            var mount = MakeRect("Picture mount", overlay.transform, new Vector2(.08f, .16f), new Vector2(.92f, .84f));
            mount.AddComponent<Image>().color = new Color(.12f, .14f, .15f);
            var imageObject = MakeRect("Outside view", mount.transform, Vector2.zero, Vector2.one);
            picture = imageObject.AddComponent<RawImage>();
            picture.raycastTarget = false;
            aspect = imageObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            closeInstructions = MakeText("Close instructions", overlay.transform, font, "E / Esc  返回房间", 23,
                new Vector2(.06f, .02f), new Vector2(.94f, .12f));
            overlay.SetActive(false);
            hint.gameObject.SetActive(false);
        }

        private static GameObject MakeRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var g = new GameObject(name, typeof(RectTransform));
            var rect = g.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return g;
        }

        private static Text MakeText(string name, Transform parent, Font font, string content, int size, Vector2 min, Vector2 max)
        {
            var g = MakeRect(name, parent, min, max);
            var text = g.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.91f, .91f, .84f);
            text.raycastTarget = false;
            return text;
        }

        private void OnDisable() { CloseView(); if (hint != null) hint.gameObject.SetActive(false); }
        private void OnDestroy() { CloseView(); if (canvas != null) Destroy(canvas.gameObject); }
    }
}
