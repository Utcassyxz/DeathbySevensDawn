// 职责：统一结算页、首次收录提示与主菜单的结局册。
using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Story;

namespace DeathBySeventhsDawn.Progression
{
    public static class EndingCollectionUI
    {
        public static bool GalleryOpen { get; private set; }

        static Texture2D haze;
        static bool confirm;
        public static void OpenGallery()
        {
            GalleryOpen = true;
        }

        public static void CloseGallery()
        {
            GalleryOpen = false;
        }

        static GUIStyle Text(int size, Color color, bool wrap = true)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                font = GameTypography.Current,
                fontSize = size,
                wordWrap = wrap
            };
            s.normal.textColor = color;
            return s;
        }

        static GUIStyle Button()
        {
            var s = new GUIStyle(GUI.skin.button)
            {
                font = GameTypography.Current,
                fontSize = 20,
                wordWrap = true
            };
            PrototypeButtons.Apply(s);
            return s;
        }

        static void Background(float w, float h)
        {
            if (haze == null)
            {
                haze = new Texture2D(1, 128)
                {
                    filterMode = FilterMode.Bilinear
                };
                for (int y = 0; y < 128; y++)
                    haze.SetPixel(0, y, Color.Lerp(new Color(.09f, .035f, .045f), new Color(.012f, .018f, .028f), y / 127f));
                haze.Apply(false);
            }

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, w, h), haze);
            var old = GUI.color;
            GUI.color = new Color(.46f, .31f, .25f, .7f);
            GUI.DrawTexture(new Rect(w * .10f, h * .19f, w * .80f, 1), Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void DrawGallery(float w, float h)
        {
            Background(w, h);
            GUI.Label(new Rect(w * .10f, h * .08f, w * .7f, 60), StoryText.Local("结局收录", "ENDING COLLECTION"), Text(38, NarrativeUI.Ink));
            GUI.Label(new Rect(w * .10f, h * .15f, w * .7f, 30), EndingCollection.Count + " / " + EndingCollection.Ids.Length, Text(18, new Color(.65f, .56f, .46f)));
            float cardW = w * .38f, cardH = Mathf.Min(330, h * .44f);
            for (int i = 0; i < EndingCollection.Ids.Length; i++)
            {
                string id = EndingCollection.Ids[i];
                bool known = EndingCollection.Has(id);
                var r = new Rect(w * .10f + i * w * .42f, h * .28f, cardW, cardH);
                NarrativeUI.Panel(r);
                GUI.Label(new Rect(r.x + 24, r.y + 18, 100, 70), (i + 1).ToString("00"), Text(48, new Color(.56f, .40f, .32f)));
                GUI.Label(new Rect(r.x + 24, r.y + 105, r.width - 48, 90), known ? EndingCollection.Title(id) : StoryText.Local("尚未收录", "UNDISCOVERED"), Text(28, known ? NarrativeUI.Ink : new Color(.45f, .46f, .47f)));
                GUI.Label(new Rect(r.x + 24, r.y + 210, r.width - 48, 86), known ? (id == "world-end" ? StoryText.Local("七天过去了。雨果没有再抬头。", "Seven days passed. Hugo never looked up again.") : StoryText.Local("每次醒来，身体都比上一次更沉。", "Each return left his body a little heavier.")) : StoryText.Local("还没有走到这里。", "This page is still blank."), Text(18, new Color(.68f, .65f, .60f)));
            }

            if (GUI.Button(new Rect(w * .10f, h * .82f, 250, 52), StoryText.Local("返回 · Esc", "Back · Esc"), Button()))
                CloseGallery();
        }

        public static void DrawResult(GameManager game, float w, float h)
        {
            Background(w, h);
            string id = game.WorldEndEnding ? "world-end" : "bad-ending-1";
            var data = game.WorldEndEnding ? StoryText.Current.worldEnd : StoryText.Current.ending;
            float left = w * .14f, width = w * .72f;
            GUI.Label(new Rect(left, h * .09f, width, 36), StoryText.Local("结 局  ", "ENDING  ") + (game.WorldEndEnding ? "01" : "02"), Text(20, new Color(.68f, .50f, .37f)));
            GUI.Label(new Rect(left, h * .24f, width, 110), data.title, Text(44, NarrativeUI.Ink));
            GUI.Label(new Rect(left, h * .43f, width, 100), game.WorldEndEnding ? StoryText.Local("雨果关好了窗，过完了这一个星期。\n第七天夜里，地球没有等来下一个清晨。", "Hugo shut the window and let the week pass.\nOn the seventh night, Earth never reached another morning.") : StoryText.Local("他记得每一次醒来。\n身体却一次也没能重新开始。", "He remembered every return.\nHis body never got to start over."), Text(23, new Color(.76f, .73f, .67f)));
            var badge = new Rect(left, h * .61f, width, 64);
            NarrativeUI.Panel(badge);
            GUI.Label(new Rect(badge.x + 18, badge.y + 12, badge.width - 36, 48), StoryText.Local(EndingCollection.FreshlyUnlocked == id ? "新结局已收录  ·  可在主页面的「结局收录」查看" : "结局已收录  ·  主页面可查看", "ENDING RECORDED  ·  Available in the title screen collection"), Text(18, new Color(.77f, .66f, .45f)));
            var b = Button();
            if (!confirm)
            {
                if (GUI.Button(new Rect(left, h * .79f, 280, 56), StoryText.Local("开始新的旅程", "Begin a new journey"), b))
                    confirm = true;
            }
            else
            {
                GUI.Label(new Rect(left, h * .71f, width, 46), StoryText.Local("清除本次旅程？结局收录会保留。", "Clear this journey? Collected endings will remain."), Text(18, NarrativeUI.Ink));
                if (GUI.Button(new Rect(left, h * .79f, 210, 56), StoryText.Local("确认重新开始", "Confirm restart"), b))
                {
                    confirm = false;
                    game.StartNewJourney();
                }

                if (GUI.Button(new Rect(left + 226, h * .79f, 110, 56), StoryText.Local("取消", "Cancel"), b))
                    confirm = false;
            }

            if (GUI.Button(new Rect(left + width - 280, h * .79f, 280, 56), StoryText.Local("回到主页面", "Return to title"), b))
            {
                confirm = false;
                MainMenuUI.ReturnToMainMenu();
            }
        }
    }
}
