// 职责：开发用线索面板；不属于玩家正常流程。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    // 开发用线索面板：显示所有线索、它们参与的配方，以及全部连接；可一键发放/清空。
    public sealed class InvestigationDevTools : MonoBehaviour
    {
        public static InvestigationDevTools Instance { get; private set; }
        public static bool WorldBlocked => Instance != null && Instance.open;

        bool open, confirmClear;
        int openedFrame, tab;
        Vector2 scroll;
        GUIStyle title, body, small, button, tabStyle, tabSelected, mono;
        static readonly Color Ink = new Color(.82f, .88f, .78f), Muted = new Color(.50f, .58f, .50f), Accent = new Color(.49f, .74f, .57f), Panel = new Color(.055f, .085f, .075f);
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public static void Open()
        {
            if (Instance == null)
                return;
            Instance.open = true;
            Instance.openedFrame = Time.frameCount;
            Instance.confirmClear = false;
            Instance.scroll = Vector2.zero;
        }

        void Close()
        {
            open = false;
            confirmClear = false;
        }

        void Update()
        {
            if (!open)
                return;
            if (GameManager.Instance == null)
            {
                Close();
                return;
            }

            var k = Keyboard.current;
            if (k != null && Time.frameCount > openedFrame + 1 && k.escapeKey.wasPressedThisFrame)
                Close();
        }

        void Styles()
        {
            if (body != null)
                return;
            var font = DeathBySeventhsDawn.Progression.GameTypography.Current;
            body = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 18,
                wordWrap = true
            };
            body.normal.textColor = Ink;
            title = new GUIStyle(body)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold
            };
            small = new GUIStyle(body)
            {
                fontSize = 14,
                wordWrap = true
            };
            small.normal.textColor = Muted;
            mono = new GUIStyle(body)
            {
                fontSize = 16,
                wordWrap = true
            };
            mono.normal.textColor = Accent;
            button = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 17,
                wordWrap = true,
                padding = new RectOffset(10, 10, 8, 8)
            };
            tabStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 16,
                padding = new RectOffset(8, 8, 6, 6)
            };
            tabSelected = new GUIStyle(tabStyle)
            {
                fontStyle = FontStyle.Bold
            };
            tabSelected.normal.textColor = Accent;
            foreach (var style in new[]
            {
                body,
                title,
                small,
                mono
            }

            )
                foreach (var state in new[]
                {
                    style.normal,
                    style.hover,
                    style.active,
                    style.focused
                }

                )
                    state.textColor = Ink;
            foreach (var style in new[]
            {
                button,
                tabStyle
            }

            )
                foreach (var state in new[]
                {
                    style.normal,
                    style.hover,
                    style.active,
                    style.focused
                }

                )
                    state.textColor = new Color(.08f, .12f, .10f);
            small.normal.textColor = Muted;
            tabSelected.normal.textColor = Accent;
        }

        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(title, body, small, button, tabStyle, tabSelected, mono);
            if (!open)
                return;
            Styles();
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(button);
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            float scale = Mathf.Clamp(Screen.height / 900f, .8f, 2.4f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.depth = -60;
            float w = Screen.width / scale, h = Screen.height / scale;
            try
            {
                GUI.color = Panel;
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                GUI.color = color;
                GUI.Label(new Rect(28, 16, 600, 42), DeathBySeventhsDawn.Story.StoryText.UI("线索开发者面板"), title);
                GUI.Label(new Rect(30, 58, w - 760, 24), DeathBySeventhsDawn.Story.StoryText.UI("只读视图 + 一键发放：每个线索指向哪些推论、全部配方与当前连接。仅开发用。"), small);
                if (DeathBySeventhsDawn.Progression.PrototypeButtons.Button(new Rect(w - 676, 20, 200, 42), DeathBySeventhsDawn.Story.StoryText.UI("获得全部线索和连接"), button))
                {
                    GrantAll();
                    scroll = Vector2.zero;
                }

                if (DeathBySeventhsDawn.Progression.PrototypeButtons.Button(new Rect(w - 466, 20, 190, 42), confirmClear ? DeathBySeventhsDawn.Story.StoryText.UI("再次点击确认清空") : DeathBySeventhsDawn.Story.StoryText.UI("清空全部推理数据"), button))
                {
                    if (confirmClear)
                    {
                        ClearAll();
                        confirmClear = false;
                        scroll = Vector2.zero;
                    }
                    else
                        confirmClear = true;
                }

                if (DeathBySeventhsDawn.Progression.PrototypeButtons.Button(new Rect(w - 266, 20, 230, 42), DeathBySeventhsDawn.Story.StoryText.UI("关闭  ·  Esc"), button))
                {
                    Close();
                    return;
                }

                for (int i = 0; i < 3; i++)
                {
                    string label = i == 0 ? DeathBySeventhsDawn.Story.StoryText.UI("线索总览") : i == 1 ? DeathBySeventhsDawn.Story.StoryText.UI("全部连接") : DeathBySeventhsDawn.Story.StoryText.UI("配方规则");
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.Button(new Rect(28 + i * 124, 98, 116, 34), label, i == tab ? tabSelected : tabStyle) && tab != i)
                    {
                        tab = i;
                        scroll = Vector2.zero;
                        confirmClear = false;
                    }
                }

                var area = new Rect(28, 148, w - 56, h - 176);
                GUILayout.BeginArea(area);
                scroll = GUILayout.BeginScrollView(scroll);
                if (tab == 0)
                    DrawClues();
                else if (tab == 1)
                    DrawConnections();
                else
                    DrawRecipes();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.color = color;
                GUI.depth = depth;
            }
        }

        void DrawClues()
        {
            GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("所有线索（即使未获得也显示）"), title);
            GUILayout.Space(8);
            foreach (var e in Investigation.Catalog.evidence)
            {
                if (e == null)
                    continue;
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("[现场] ") + e.title + "   (" + e.id + ")   " + (Investigation.Has(e.id) ? DeathBySeventhsDawn.Story.StoryText.UI("已获得") : DeathBySeventhsDawn.Story.StoryText.UI("未获得")), mono);
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("来源：") + e.source + "    " + RecipeTargets(e.id), small);
                GUILayout.Space(6);
            }

            foreach (var d in Investigation.Catalog.deductions)
            {
                if (d == null)
                    continue;
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("[推论] ") + d.title + "   (" + d.id + ")   " + (Investigation.Has(d.id) ? DeathBySeventhsDawn.Story.StoryText.UI("已获得") : DeathBySeventhsDawn.Story.StoryText.UI("未获得")), mono);
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("由：") + ClueTitle(d.left) + "  +  " + ClueTitle(d.right) + "    " + RecipeTargets(d.id), small);
                GUILayout.Space(6);
            }
        }

        void DrawConnections()
        {
            GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("全部配方"), title);
            GUILayout.Space(6);
            foreach (var d in Investigation.Catalog.deductions)
            {
                if (d == null)
                    continue;
                bool done = Investigation.Has(d.id);
                GUILayout.Label((done ? DeathBySeventhsDawn.Story.StoryText.UI("[已成立] ") : DeathBySeventhsDawn.Story.StoryText.UI("[未建立] ")) + d.title + "  (" + d.id + ")", mono);
                GUILayout.Label(ClueTitle(d.left) + "  +  " + ClueTitle(d.right) + "  →  " + d.title + "   ·   " + Investigation.RelationLabel(d.relation), small);
                GUILayout.Space(4);
            }

            GUILayout.Space(18);
            GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("当前连线"), title);
            GUILayout.Space(6);
            int count = 0;
            foreach (var link in Investigation.Links)
            {
                if (link == null)
                    continue;
                count++;
                string status = string.IsNullOrEmpty(link.conclusion) ? DeathBySeventhsDawn.Story.StoryText.UI("待证") : "→ " + ClueTitle(link.conclusion);
                GUILayout.Label(status + "   " + ClueTitle(link.left) + "  ↔  " + ClueTitle(link.right), mono);
            }

            if (count == 0)
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("当前没有任何连线。"), small);
        }

        void DrawRecipes()
        {
            GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("配方规则（数据来自 Investigation.json）"), title);
            GUILayout.Space(8);
            foreach (var d in Investigation.Catalog.deductions)
            {
                if (d == null)
                    continue;
                GUILayout.Label(d.id + " : " + d.left + " + " + d.right + "  [" + Investigation.RelationLabel(d.relation) + "]  →  " + d.id, mono);
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("结果：") + d.title, small);
                GUILayout.Space(6);
            }
        }

        static string ClueTitle(string id)
        {
            var e = Investigation.Evidence(id);
            if (e != null)
                return e.title;
            var d = System.Array.Find(Investigation.Catalog.deductions, x => x.id == id);
            return d != null ? d.title : id;
        }

        string RecipeTargets(string id)
        {
            string result = DeathBySeventhsDawn.Story.StoryText.UI("指向：");
            bool any = false;
            foreach (var d in Investigation.Catalog.deductions)
            {
                if (d == null)
                    continue;
                if (d.left == id || d.right == id)
                {
                    any = true;
                    result += "\n  + " + ClueTitle(d.left == id ? d.right : d.left) + " → " + d.title + " (" + d.id + ")";
                }
            }

            return any ? result : DeathBySeventhsDawn.Story.StoryText.UI("指向：没有更多配方");
        }

        void GrantAll()
        {
            var game = GameManager.Instance;
            if (game == null || game.HasEnded)
                return;
            var state = game.State;
            state.neighborFlyersSeen = true;
            foreach (var e in Investigation.Catalog.evidence)
                if (e != null)
                    Investigation.Collect(e.id);
            for (int pass = 0; pass < Investigation.Catalog.deductions.Length; pass++)
                foreach (var d in Investigation.Catalog.deductions)
                    if (d != null)
                        Investigation.Connect(d.left, d.right, true);
            ProgressionSave.Write(state);
        }

        void ClearAll()
        {
            var game = GameManager.Instance;
            if (game == null)
                return;
            var state = game.State;
            state.investigationLinks.Clear();
            state.journal.RemoveAll(e => e != null && Investigation.IsNode(e.id));
            state.knowledgeIds.RemoveAll(id => Investigation.IsNode(id));
            ProgressionSave.Write(state);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
