// 职责：可调查物件的距离检测、查看与主动记录入口。
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    [DefaultExecutionOrder(-40)]
    public sealed class EvidenceInspection : MonoBehaviour
    {
        public static EvidenceInspection Instance
        {
            get; private set;
        }
        public static bool BlocksWorld => Instance != null && (Instance.IsOpen || Time.frameCount <= Instance.blockedFrame);
        public bool IsOpen => opened != null;
        public string CurrentId => opened;
        string target, opened; int blockedFrame = -1, openedFrame; Vector2 scroll; GUIStyle text, title, button;
        void Awake()
        {
            Instance = this;
        }
        void OnEnable()
        {
            SceneManager.sceneLoaded += Loaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += Close;
        }
        void OnDisable()
        {
            SceneManager.sceneLoaded -= Loaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= Close;
            Close();
        }
        void Loaded(Scene scene, LoadSceneMode mode)
        {
            target = null;
            Close();
        }
        public void Close()
        {
            opened = null;
            target = null;
            blockedFrame = Time.frameCount;
            scroll = Vector2.zero;
        }
        public bool CollectCurrent() => IsOpen && Investigation.Collect(opened);
        public bool TryOpen(string id)
        {
            if (GameManager.Instance == null || TutorialGate.IsBlocked("evidence") || !GameManager.Instance.State.investigationUnlocked || id != target || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy || ProgressionHUD.DebugConsoleOpen || (GameManager.Instance == null || GameManager.Instance.HasEnded))
                return false;
            opened = id;
            openedFrame = Time.frameCount;
            scroll = Vector2.zero;
            return true;
        }
        void Update()
        {
            if ((GameManager.Instance == null || GameManager.Instance.HasEnded))
            {
                Close();
                return;
            }
            if (ProgressionHUD.DebugConsoleOpen)
                return;
            var k = Keyboard.current;
            if (IsOpen)
            {
                if (Time.frameCount <= openedFrame)
                    return;
                if (k != null && k.rKey.wasPressedThisFrame)
                    CollectCurrent();
                if (k != null && k.escapeKey.wasPressedThisFrame)
                    Close();
                return;
            }
            target = null;
            if (TutorialGate.IsBlocked("evidence") || !GameManager.Instance.State.investigationUnlocked)
                return;
            if (StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                return;
            var window = Object.FindAnyObjectByType<WindowLookout2D>();
            if (window != null && window.IsOpen)
                return;
            var player = GameObject.FindWithTag("Player");
            if (player == null)
                return;
            var pos = (Vector2)player.transform.position;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "Bedroom" && Vector2.Distance(pos, new Vector2(2.7f, -1.1f)) < 1.05f)
                target = "my-observation-notes";
            if (scene == "Corridor" && GameManager.Instance.LoopCount == 0)
            {
                if (TimeManager.Instance.Days == 2 && !Investigation.Has("cult-door-note") && Vector2.Distance(pos, new Vector2(-4.35f, .65f)) < .8f)
                    target = "cult-door-note";
                else if (Vector2.Distance(pos, new Vector2(-4.35f, .35f)) < .65f && TimeManager.Instance.Days >= 2)
                    target = "401-barricade";
            }
            if (scene == "LivingRoom" && TimeManager.Instance.Days < 3)
            {
                var tank = GameObject.Find("Aquarium and cabinet");
                if (tank != null && Vector2.Distance(pos, tank.transform.position) < 2.05f)
                    target = "tank-label";
            }
            if (scene == "Bedroom" && target == "my-observation-notes" && Investigation.Has(target) && TimeManager.Instance.Days >= 2)
                target = "notes-gap";
            if (scene == "Corridor" && Vector2.Distance(pos, new Vector2(4.7f, 1.15f)) < 1f)
                target = "403-silence";

            if (target != null && k != null && k.eKey.wasPressedThisFrame)
                TryOpen(target);
        }
        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(text, title, button);
            if (ProgressionHUD.DebugConsoleOpen || (target == null && !IsOpen) || (GameManager.Instance == null || GameManager.Instance.HasEnded))
                return;
            if (text == null)
            {
                var font = DeathBySeventhsDawn.Progression.GameTypography.Current;
                text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 24, wordWrap = true };
                title = new GUIStyle(text) { fontSize = 32, fontStyle = FontStyle.Bold };
                button = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
                button.normal.textColor = new Color(.79f, .83f, .68f);
            }
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(button);
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            float scale = Mathf.Min(Screen.height / 900f, Screen.width / 1000f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            try
            {
                if (!IsOpen)
                {
                    GUI.Label(new Rect(w / 2 - 310, h - 105, 620, 70), DeathBySeventhsDawn.Story.StoryText.Local("E  查看", "E  Examine ") + Investigation.Evidence(target).title, button);
                    return;
                }
                GUI.depth = -36;
                GUI.color = new Color(.03f, .045f, .045f, .98f);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
                float width = Mathf.Min(850, w - 80);
                GUILayout.BeginArea(new Rect((w - width) / 2, h * .14f, width, h * .76f));
                scroll = GUILayout.BeginScrollView(scroll);
                var evidence = Investigation.Evidence(opened);
                GUILayout.Label(evidence.source, text);
                GUILayout.Space(20);
                GUILayout.Label(evidence.title, title);
                GUILayout.Space(26);
                GUILayout.Label(evidence.body, text);
                GUILayout.EndScrollView();
                if (Investigation.Has(opened))
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("已收录。可以在笔记的「线索连线」页整理它。", "Recorded. Open the evidence board to connect it."), text);
                else if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.Local("R  抄录到笔记", "R  Record in journal"), button, GUILayout.Height(60)))
                    CollectCurrent();
                if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.Local("先放回原处  ·  Esc", "Leave it here \u00b7 Esc"), button, GUILayout.Height(55)))
                    Close();
                GUILayout.EndArea();
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.depth = depth; }
        }
        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
