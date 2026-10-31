// 职责：证据配对、兼容高亮与推论确认；旧待证线可删除。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Knowledge;

namespace DeathBySeventhsDawn.Interaction
{
    // 独立的全屏线索板：左侧选线索，右侧整理两条线索之间的关系。
    // 不再把卡片堆成一张自动布局的图；连接以列表记录，避免线越多越乱。
    public sealed class InvestigationBoardUI
    {
        public Action CloseRequested;
        string first, second, message = DeathBySeventhsDawn.Story.StoryText.Local("从左侧选择第一条线索，再选第二条；系统只在你确认时写下结论。", "Select two records. A conclusion is only written when you confirm.");
        int focus, filter;
        bool previousEnglish;
        Vector2 boardScroll, detailScroll;
        GUIStyle title, body, small, cardTitle, rowTitle, rowMeta, tag, button, filterButton, linkRow, headerTitle, headerSmall;
        static readonly Color Ink = new Color(.16f, .22f, .21f), Muted = new Color(.42f, .43f, .36f), Green = new Color(.22f, .42f, .36f), Paper = new Color(.92f, .88f, .75f), Deep = new Color(.045f, .075f, .075f);
        static string[] filters => new[]
        {
            DeathBySeventhsDawn.Story.StoryText.Local("全部", "All"),
            DeathBySeventhsDawn.Story.StoryText.Local("现场记录", "Evidence"),
            DeathBySeventhsDawn.Story.StoryText.Local("推论", "Deductions"),
            DeathBySeventhsDawn.Story.StoryText.Local("未连接", "Unconnected")
        };

        public void Reset()
        {
            first = second = null;
            focus = 0;
            filter = 0;
            boardScroll = detailScroll = Vector2.zero;
            message = DeathBySeventhsDawn.Story.StoryText.Local("从左侧选择第一条线索，再选第二条；系统只在你确认时写下结论。", "Select two records. A conclusion is only written when you confirm.");
        }

        public void Select(string id)
        {
            if (id == first)
            {
                first = second;
                second = null;
            }
            else if (id == second)
                second = null;
            else if (first == null)
                first = id;
            else if (second == null)
                second = id;
            else
            {
                first = second;
                second = id;
            }

            detailScroll = Vector2.zero;
            RefreshMessage();
        }

        void SelectPair(string a, string b)
        {
            first = a;
            second = b;
            detailScroll = Vector2.zero;
            RefreshMessage();
        }

        public void KeyboardInput()
        {
            var k = Keyboard.current;
            if (k == null)
                return;
            if (k.tabKey.wasPressedThisFrame)
            {
                filter = (filter + 1) % filters.Length;
                focus = 0;
                boardScroll = Vector2.zero;
                return;
            }

            var nodes = Filtered();
            if (nodes.Count > 0)
            {
                int step = (k.downArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame ? 1 : 0) - (k.upArrowKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame ? 1 : 0);
                if (step != 0)
                {
                    focus = (focus + step + nodes.Count) % nodes.Count;
                    boardScroll.y = Mathf.Max(0, focus * 80 - 90);
                }

                focus = Mathf.Clamp(focus, 0, nodes.Count - 1);
                if (k.spaceKey.wasPressedThisFrame)
                    Select(nodes[focus].id);
            }

            if (k.enterKey.wasPressedThisFrame && first != null && second != null)
            {
                var link = Investigation.FindLink(first, second);
                if (link != null && !string.IsNullOrEmpty(link.conclusion))
                    Commit();
                else if (Investigation.Match(first, second) != null)
                    Commit();
            }

            if (k.backspaceKey.wasPressedThisFrame)
            {
                first = second = null;
                RefreshMessage();
            }

            if (k.deleteKey.wasPressedThisFrame && first != null && second != null)
                Remove();
        }

        void RefreshMessage()
        {
            if (first == null && second == null)
            {
                message = DeathBySeventhsDawn.Story.StoryText.Local("从左侧选择第一条线索，再选第二条；系统只在你确认时写下结论。", "Select two records. A conclusion is only written when you confirm.");
                return;
            }

            if (second == null)
            {
                message = DeathBySeventhsDawn.Story.StoryText.Local("已选择①。再选一条线索，建立你怀疑的联系。", "First record selected. Choose a second.");
                return;
            }

            var link = Investigation.FindLink(first, second);
            if (link != null && !string.IsNullOrEmpty(link.conclusion))
                message = DeathBySeventhsDawn.Story.StoryText.Local("这条推论已经写下了。", "This deduction is already recorded.");
            else if (Investigation.Match(first, second) != null)
                message = DeathBySeventhsDawn.Story.StoryText.Local("这两条记录支持一个结论。", "These records support a conclusion.");
            else if (link != null)
                message = DeathBySeventhsDawn.Story.StoryText.Local("这条联系还没有依据。可以留着，也可以擦掉。", "This link has no supporting evidence yet. Keep it, or erase it.");
            else
                message = DeathBySeventhsDawn.Story.StoryText.Local("这两条没有足够的联系。换一条试试。", "These do not support a conclusion. Try another record.");
        }

        void LinkOnly()
        {
            var rule = Investigation.Match(first, second);
            bool changed = Investigation.Connect(first, second, false);
            message = changed ? (rule != null ? DeathBySeventhsDawn.Story.StoryText.Local("已留下待证连线。现在可以得出结论。", "Tentative link saved. You can now draw a conclusion.") : DeathBySeventhsDawn.Story.StoryText.Local("已留下待证连线；它不会被当作事实。", "Tentative link saved. It is not a fact.")) : DeathBySeventhsDawn.Story.StoryText.Local("这条联系已经保存在笔记里。", "This link is already saved.");
            detailScroll = Vector2.zero;
        }

        void Commit()
        {
            var rule = Investigation.Match(first, second);
            var existing = Investigation.FindLink(first, second);
            bool alreadyWritten = existing != null && !string.IsNullOrEmpty(existing.conclusion);
            bool changed = Investigation.Connect(first, second, rule != null);
            if (!changed)
                message = alreadyWritten ? DeathBySeventhsDawn.Story.StoryText.Local("这条推论已经写下了。", "This deduction is already recorded.") : DeathBySeventhsDawn.Story.StoryText.Local("这条联系已经保存在笔记里。", "This link is already saved.");
            else
                message = rule != null ? DeathBySeventhsDawn.Story.StoryText.Local("推论已写下。它也可以成为下一条推理的依据。", "Deduction recorded. It can support a later connection.") : DeathBySeventhsDawn.Story.StoryText.Local("已留下待证连线；它不会被当作事实。", "Tentative link saved. It is not a fact.");
            detailScroll = Vector2.zero;
        }

        void Remove()
        {
            message = Investigation.RemoveTentative(first, second) ? DeathBySeventhsDawn.Story.StoryText.Local("已擦去待证连线。", "Tentative link erased.") : DeathBySeventhsDawn.Story.StoryText.Local("只有待证连线可以擦去；已写下的推论保留其依据。", "Only tentative links can be erased. Deductions retain their sources.");
        }

        List<JournalEntry> Filtered()
        {
            var all = Investigation.Nodes();
            var result = new List<JournalEntry>();
            foreach (var entry in all)
            {
                if (entry == null)
                    continue;
                bool deduction = Investigation.Evidence(entry.id) == null;
                if (filter == 1 && deduction)
                    continue;
                if (filter == 2 && !deduction)
                    continue;
                if (filter == 3 && Connected(entry.id))
                    continue;
                result.Add(entry);
            }

            return result;
        }

        static bool Connected(string id)
        {
            foreach (var link in Investigation.Links)
                if (link != null && (link.left == id || link.right == id || link.conclusion == id))
                    return true;
            return false;
        }

        static int ConnectionCount(string id)
        {
            int count = 0;
            foreach (var link in Investigation.Links)
                if (link != null && (link.left == id || link.right == id || link.conclusion == id))
                    count++;
            return count;
        }

        static string Body(string id)
        {
            var entry = Investigation.Nodes().Find(e => e.id == id);
            return entry != null ? DeathBySeventhsDawn.Knowledge.Investigation.LocalBody(entry.id, entry.body) : "";
        }

        void Styles()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(title, body, small, cardTitle, rowTitle, rowMeta, tag, button, filterButton, linkRow, headerTitle, headerSmall);
            if (body != null)
                return;
            var font = DeathBySeventhsDawn.Progression.GameTypography.Current;
            body = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 19,
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
                fontSize = 16,
                wordWrap = true
            };
            small.normal.textColor = Muted;
            cardTitle = new GUIStyle(body)
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold
            };
            rowTitle = new GUIStyle(body)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            rowMeta = new GUIStyle(body)
            {
                fontSize = 14,
                wordWrap = true
            };
            rowMeta.normal.textColor = Muted;
            tag = new GUIStyle(body)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            tag.normal.textColor = Green;
            button = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 19,
                wordWrap = true,
                padding = new RectOffset(14, 14, 10, 10)
            };
            filterButton = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 16,
                padding = new RectOffset(8, 8, 6, 6)
            };
            linkRow = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 8, 8)
            };
            headerTitle = new GUIStyle(title)
            {
                fontSize = 34
            };
            headerTitle.normal.textColor = new Color(.92f, .88f, .75f);
            headerSmall = new GUIStyle(small)
            {
                fontSize = 16
            };
            headerSmall.normal.textColor = new Color(.62f, .68f, .62f);
            foreach (var style in new[]
            {
                body,
                title,
                small,
                cardTitle,
                rowTitle,
                rowMeta,
                button,
                linkRow
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
            small.normal.textColor = Muted;
            rowMeta.normal.textColor = Muted;
            button.hover.textColor = Green;
            filterButton.normal.textColor = Ink;
            filterButton.hover.textColor = Green;
            linkRow.hover.textColor = Green;
        }

        static void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public void Draw(float w, float h)
        {
            if (previousEnglish != DeathBySeventhsDawn.Story.StoryText.English)
            {
                previousEnglish = DeathBySeventhsDawn.Story.StoryText.English;
                RefreshMessage();
            }

            RefreshMessage();
            Styles();
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(button);
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(filterButton);
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(linkRow);
            linkRow.fixedHeight = 0;
            filterButton.padding = new RectOffset(6, 6, 2, 2);
            filterButton.fontSize = 16;
            filterButton.fixedHeight = 34;
            filterButton.wordWrap = false;
            Fill(new Rect(0, 0, w, h), Deep);
            Fill(new Rect(0, 0, w, 94), new Color(.07f, .12f, .12f));
            GUI.Label(new Rect(30, 16, 520, 44), DeathBySeventhsDawn.Story.StoryText.Local("线索连线", "Evidence board"), headerTitle);
            GUI.Label(new Rect(32, 58, w - 420, 24), DeathBySeventhsDawn.Story.StoryText.Local("先选一条线索，能与它推理的记录会亮起。", "Select a clue. Compatible records light up."), headerSmall);
            if (GUI.Button(new Rect(w - 232, 24, 202, 44), DeathBySeventhsDawn.Story.StoryText.Local("合上  ·  B / Esc", "Close \u00b7 B / Esc"), button))
            {
                CloseRequested?.Invoke();
                return;
            }

            for (int i = 0; i < filters.Length; i++)
            {
                if (GUI.Button(new Rect(30 + i * 192, 102, 184, 34), (filter == i ? "●  " : "") + filters[i], filterButton) && filter != i)
                {
                    filter = i;
                    focus = 0;
                    boardScroll = Vector2.zero;
                }
            }

            float leftWidth = Mathf.Clamp(w * .34f, 300, 480);
            var left = new Rect(28, 154, leftWidth, h - 184);
            var right = new Rect(left.xMax + 24, 154, w - leftWidth - 80, h - 184);
            Fill(left, new Color(.82f, .79f, .67f));
            Fill(right, Paper);
            var nodes = Filtered();
            GUI.Label(new Rect(left.x + 18, left.y + 12, left.width - 36, 36), DeathBySeventhsDawn.Story.StoryText.Local("线索 · ", "Clues \u00b7 ") + nodes.Count, cardTitle);
            GUI.Label(new Rect(left.x + 18, left.y + 50, left.width - 36, 28), DeathBySeventhsDawn.Story.StoryText.Local("① 第一条  ② 第二条  ·  点选或方向键 + 空格", "First / second \u00b7 Click or arrows + Space"), small);
            var viewport = new Rect(left.x + 12, left.y + 84, left.width - 24, left.height - 100);
            float rowWidth = viewport.width - 20, contentHeight = Mathf.Max(viewport.height, nodes.Count * 80 + 8);
            boardScroll = GUI.BeginScrollView(viewport, boardScroll, new Rect(0, 0, rowWidth + 8, contentHeight));
            for (int i = 0; i < nodes.Count; i++)
            {
                var entry = nodes[i];
                var row = new Rect(0, i * 80, rowWidth, 72);
                bool chosen = first == entry.id || second == entry.id;
                bool compatible = first != null && second == null && Investigation.CanCombine(first, entry.id);
                bool deduction = Investigation.Evidence(entry.id) == null;
                Fill(row, compatible ? new Color(.72f, .84f, .69f) : new Color(.90f, .87f, .75f));
                if (compatible)
                    Fill(new Rect(row.width - 7, row.y, 7, row.height), Green);
                Fill(new Rect(row.x, row.y, 4, row.height), deduction ? Green : new Color(.55f, .42f, .25f));
                if (i == focus)
                    Fill(new Rect(row.x, row.y, row.width, 2), Green);
                if (chosen)
                    Fill(new Rect(row.x, row.y, 5, row.height), DeathBySeventhsDawn.Core.GameManager.Instance.Sanity < 50 ? new Color(.65f, .08f, .08f) : Color.black);
                GUI.Label(new Rect(row.x + 14, row.y + 8, 80, 20), compatible ? DeathBySeventhsDawn.Story.StoryText.Local("可推理", "Can combine") : deduction ? DeathBySeventhsDawn.Story.StoryText.Local("推论", "Deduction") : DeathBySeventhsDawn.Story.StoryText.Local("现场记录", "Evidence"), tag);
                GUI.Label(new Rect(row.x + 14, row.y + 24, row.width - 24, 30), (chosen ? (first == entry.id ? "① " : "② ") : "") + DeathBySeventhsDawn.Knowledge.Investigation.LocalTitle(entry.id, entry.title), rowTitle);
                GUI.Label(new Rect(row.x + 14, row.y + 50, row.width - 24, 22), DeathBySeventhsDawn.Story.StoryText.Local("第 " + entry.day + " 天  ·  " + ConnectionCount(entry.id) + " 条连线", "Day " + entry.day + " · " + ConnectionCount(entry.id) + " links"), rowMeta);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                {
                    focus = i;
                    Select(entry.id);
                }
            }

            if (nodes.Count == 0)
                GUI.Label(new Rect(14, 18, viewport.width - 28, 100), DeathBySeventhsDawn.Story.StoryText.Local("这一栏还没有线索。\n\n先探索房间、查看现场记录并主动收录。", "No records in this category.\n\nExplore and actively record what you find."), body);
            GUI.EndScrollView();
            GUI.Label(new Rect(28, h - 34, w - 56, 28), DeathBySeventhsDawn.Story.StoryText.Local("方向键移动 · 空格选择 · Enter 写下推论 · Delete 擦除 · Tab 换筛选 · B / Esc 关闭", "Arrows Move \u00b7 Space Select \u00b7 Enter Record deduction \u00b7 Delete Erase \u00b7 Tab Filter \u00b7 B / Esc Close"), headerSmall);
            GUILayout.BeginArea(new Rect(right.x + 20, right.y + 16, right.width - 40, right.height - 32));
            detailScroll = GUILayout.BeginScrollView(detailScroll);
            GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("整理思路", "Arrange the evidence"), title);
            GUILayout.Label(message, small);
            if (first == null && second == null)
            {
                GUILayout.Space(16);
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("从左侧选择一条线索。", "Select a record on the left."), cardTitle);
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("选中后，绿色记录可以与它推理。读过两份记录，再按 Enter 写下结论。", "Green records can be combined with your selection. Read both, then press Enter to record the conclusion."), body);
                GUILayout.Space(8);
                DrawLinks();
            }
            else
            {
                GUILayout.Space(12);
                GUILayout.Label("① " + Investigation.Title(first), cardTitle);
                GUILayout.Label(Body(first), small);
                if (second != null)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("② " + Investigation.Title(second), cardTitle);
                    GUILayout.Label(Body(second), small);
                    GUILayout.Space(12);
                    var rule = Investigation.Match(first, second);
                    var link = Investigation.FindLink(first, second);
                    bool concluded = link != null && !string.IsNullOrEmpty(link.conclusion);
                    if (concluded)
                    {
                        GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("这条推论已经写下。", "This deduction is recorded."), small);
                        if (rule != null)
                        {
                            GUILayout.Label(rule.title, cardTitle);
                            GUILayout.Label(rule.body, body);
                        }
                    }
                    else if (rule != null)
                    {
                        GUILayout.Label(rule.title, cardTitle);
                        if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.Local("写下推论  ·  Enter", "Record deduction · Enter"), button, GUILayout.MinHeight(50)))
                            Commit();
                    }
                    else
                        GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("这两条暂时对不上。换一条试试。", "These don't support a conclusion. Try another record."), body);
                    if (link != null && !concluded && GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.Local("擦去这条待证线  ·  Delete", "Erase tentative link \u00b7 Delete"), button, GUILayout.MinHeight(42)))
                        Remove();
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.Local("清空选择  ·  Backspace", "Clear selection \u00b7 Backspace"), button, GUILayout.MinHeight(40)))
                    {
                        first = second = null;
                        RefreshMessage();
                    }
                }
                else
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("再选一条线索，建立你怀疑的联系。", "Choose a second record to test a connection."), body);
                GUILayout.Space(18);
                DrawLinks();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawLinks()
        {
            GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("已整理的连线", "Saved connections"), cardTitle);
            int count = 0;
            foreach (var link in Investigation.Links)
            {
                if (link == null || !Investigation.IsNode(link.left) || !Investigation.IsNode(link.right))
                    continue;
                count++;
                string status = string.IsNullOrEmpty(link.conclusion) ? DeathBySeventhsDawn.Story.StoryText.Local("待证 · 不当作事实", "Tentative \u00b7 Not a fact") : "→ " + Investigation.Title(link.conclusion);
                if (GUILayout.Button(Investigation.Title(link.left) + "  ↔  " + Investigation.Title(link.right) + "\n" + status, linkRow, GUILayout.MinHeight(50)))
                    SelectPair(link.left, link.right);
            }

            if (count == 0)
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Local("还没有连线。选两条线索，建立第一条。", "No connections yet. Select two records to begin."), small);
        }
    }
}
