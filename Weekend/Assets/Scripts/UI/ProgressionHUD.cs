// 职责：结局页与开发调试控制台；末日演出播放时暂不覆盖画面。
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Progression
{
    // Scene-independent HUD. Debug access: backquote/tilde, then 1997.
    public class ProgressionHUD : MonoBehaviour
    {
        public static bool DebugConsoleOpen
        {
            get; private set;
        }
        // 本次游戏只验证一次口令。此对象跨场景/轮回保留；关闭面板不会清除解锁状态。
        // 不写入存档，重新启动游戏后仍需首次输入 1997。
        bool unlockedThisSession;
        bool enteringCode, debugOpen, confirmNewJourney;
        string code = "";
        Vector2 scroll;
        GUIStyle labelStyle, titleStyle, buttonStyle, boxStyle;

        void Update()
        {
            if (MainMenuUI.WorldBlocked || PauseMenuUI.WorldBlocked)
                return;
            if (DeathBySeventhsDawn.Interaction.InvestigationDevTools.WorldBlocked)
                return;
            if (DeathBySeventhsDawn.Interaction.AquariumEntity.WorldBlocked)
                return;
#if ENABLE_INPUT_SYSTEM
            var keyboard=Keyboard.current;
            if(keyboard==null) return;
            if(keyboard.backquoteKey.wasPressedThisFrame) { ToggleConsole(); return; }
            if(keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if(!enteringCode) return;
            if(keyboard.backspaceKey.wasPressedThisFrame && code.Length>0) code=code.Substring(0,code.Length-1);
            var top=new[] { keyboard.digit0Key,keyboard.digit1Key,keyboard.digit2Key,keyboard.digit3Key,keyboard.digit4Key,keyboard.digit5Key,keyboard.digit6Key,keyboard.digit7Key,keyboard.digit8Key,keyboard.digit9Key };
            var pad=new[] { keyboard.numpad0Key,keyboard.numpad1Key,keyboard.numpad2Key,keyboard.numpad3Key,keyboard.numpad4Key,keyboard.numpad5Key,keyboard.numpad6Key,keyboard.numpad7Key,keyboard.numpad8Key,keyboard.numpad9Key };
            for(int i=0;i<10;i++) if(top[i].wasPressedThisFrame || pad[i].wasPressedThisFrame) EnterDigit((char)('0'+i));
#elif ENABLE_LEGACY_INPUT_MANAGER
            if(Input.GetKeyDown(KeyCode.BackQuote)) { ToggleConsole(); return; }
            if(Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if(!enteringCode) return;
            foreach(char c in Input.inputString) {
                if(c=='\b' && code.Length>0) code=code.Substring(0,code.Length-1);
                else if(c>='0' && c<='9') EnterDigit(c);
            }
#endif
        }
        void ToggleConsole()
        {
            if (debugOpen || enteringCode)
            {
                Close();
                return;
            }
            DebugConsoleOpen = true;
            if (unlockedThisSession)
            {
                debugOpen = true;
                enteringCode = false;
                scroll = Vector2.zero;
            }
            else
            {
                enteringCode = true;
                code = "";
            }
        }
        void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += Close;
        }
        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= Close;
            Close();
        }
        void Close()
        {
            DebugConsoleOpen = false;
            enteringCode = false;
            debugOpen = false;
            confirmNewJourney = false;
            code = "";
        }
        void RestoreInitialState()
        {
            Close();
            GameManager.Instance.StartNewJourney();
        }
        void EnterDigit(char digit)
        {
            if (!enteringCode)
                return;
            code += digit;
            if (code == "1997")
            {
                unlockedThisSession = true;
                enteringCode = false;
                debugOpen = true;
                code = "";
                scroll = Vector2.zero;
            }
            else if (code.Length >= 4)
                code = "";
        }
        void OnDestroy()
        {
            DebugConsoleOpen = false;
        }
        void PrepareStyles()
        {
            if (labelStyle != null)
                return;
            // A dynamic OS font provides Chinese glyphs missing from the default IMGUI font.
            var chineseFont = DeathBySeventhsDawn.Progression.GameTypography.Current;
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 23, wordWrap = true, padding = new RectOffset(10, 10, 5, 5) };
            titleStyle = new GUIStyle(labelStyle) { fontSize = 29, fontStyle = FontStyle.Bold };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 23, wordWrap = true, padding = new RectOffset(14, 14, 12, 12) };
            labelStyle.font = chineseFont;
            titleStyle.font = chineseFont;
            buttonStyle.font = chineseFont;
            boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(18, 18, 16, 16) };
        }
        void DrawStatus(GameManager game, TimeManager clock)
        {
            GUILayout.Label(StoryText.Local($"第 {clock.Days} / 7 天  {PeriodName(clock.Period)}   |   轮回次数 {game.LoopCount}", $"Day {clock.Days} / 7  {PeriodName(clock.Period)}   |   Loops {game.LoopCount}"), labelStyle);
            GUILayout.Label(StoryText.Local($"体力 {game.Vitality:0.#} / 上限 {game.MaxVitality:0.#}", $"Vitality {game.Vitality:0.#} / Maximum {game.MaxVitality:0.#}"), labelStyle);
            GUILayout.Label(StoryText.Local($"知识 {game.Knowledge:0.#}   |   等级 {game.KnowledgeLevel}", $"Knowledge {game.Knowledge:0.#}   |   Level {game.KnowledgeLevel}"), labelStyle);
            GUILayout.Label(StoryText.Local($"变异 {game.Mutation:0.#}   |   理智 {game.Sanity:0.#}（{LayerName(game.Layer)}）", $"Mutation {game.Mutation:0.#}   |   Sanity {game.Sanity:0.#} ({LayerName(game.Layer)})"), labelStyle);
        }
        static string PeriodName(TimePeriod period) => new[] { DeathBySeventhsDawn.Story.StoryText.UI("早晨"), DeathBySeventhsDawn.Story.StoryText.UI("下午"), DeathBySeventhsDawn.Story.StoryText.UI("傍晚"), DeathBySeventhsDawn.Story.StoryText.UI("深夜") }[(int)period];
        static string LayerName(SanityLayer layer) => new[] { DeathBySeventhsDawn.Story.StoryText.UI("清醒"), DeathBySeventhsDawn.Story.StoryText.UI("动摇"), DeathBySeventhsDawn.Story.StoryText.UI("破碎"), DeathBySeventhsDawn.Story.StoryText.UI("启示") }[(int)layer];
        static string TierName(KnowledgeTier tier) => new[] { DeathBySeventhsDawn.Story.StoryText.UI("普通"), DeathBySeventhsDawn.Story.StoryText.UI("禁忌"), DeathBySeventhsDawn.Story.StoryText.UI("宇宙") }[(int)tier];
        static string AbilityName(string id)
        {
            switch (id)
            {
                case "NightVision":
                    return DeathBySeventhsDawn.Story.StoryText.UI("夜视");
                case "AberrantHearing":
                    return DeathBySeventhsDawn.Story.StoryText.UI("异化听觉");
                case "Regeneration":
                    return DeathBySeventhsDawn.Story.StoryText.UI("血肉再生");
                case "SpatialPerception":
                    return DeathBySeventhsDawn.Story.StoryText.UI("空间感知");
                case "NonHumanForm":
                    return DeathBySeventhsDawn.Story.StoryText.UI("非人形态");
                default:
                    return id;
            }
        }
        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(labelStyle, titleStyle, buttonStyle, boxStyle);
            var game = GameManager.Instance;
            var clock = TimeManager.Instance;
            if (game == null || clock == null)
                return;
            if (MainMenuUI.WorldBlocked || PauseMenuUI.WorldBlocked)
                return;
            if (DeathBySeventhsDawn.Interaction.InvestigationDevTools.WorldBlocked)
                return;
            if (DeathBySeventhsDawn.Interaction.AquariumEntity.WorldBlocked)
                return;
            PrepareStyles();
            PrototypeButtons.Apply(buttonStyle);
            var previousColor = GUI.color;
            // Scale with resolution, keeping large readable controls on high-DPI game views.
            var previousMatrix = GUI.matrix;
            float scale = Mathf.Clamp(Screen.height / 900f, .8f, 2.4f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            try
            {
                if (WorldEndSequence.IsPresenting)
                    return;
                if (game.HasEnded && !debugOpen && !enteringCode)
                {
                    GUI.color = new Color(0, 0, 0, .97f);
                    GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                    GUI.color = previousColor;
                    float endingWidth = Mathf.Min(820, width - 24);
                    GUILayout.BeginArea(new Rect((width - endingWidth) / 2, 24, endingWidth, height - 48), boxStyle);
                    scroll = GUILayout.BeginScrollView(scroll);
                    var ending = game.WorldEndEnding ? StoryText.Current.worldEnd : StoryText.Current.ending;
                    GUILayout.Label(ending?.title ?? "坏结局 1：无能的勇者", titleStyle);
                    GUILayout.Space(24);
                    if (ending?.lines != null)
                        foreach (var line in ending.lines)
                            GUILayout.Label(line, labelStyle);
                    GUILayout.Space(20);
                    GUILayout.Label(string.Format(ending?.summary ?? "历经 {0} 次轮回，体力上限 {1:0.#}。", game.LoopCount, game.MaxVitality), labelStyle);
                    GUILayout.Space(20);
                    if (!confirmNewJourney)
                    {
                        if (GUILayout.Button(ending?.restart ?? "重新开始旅程", buttonStyle))
                            confirmNewJourney = true;
                    }
                    else
                    {
                        GUILayout.Label(ending?.confirmWarning ?? "重新开始会清除本次旅程的知识、变异和轮回进度。", labelStyle);
                        if (GUILayout.Button(ending?.confirm ?? "确认重新开始", buttonStyle))
                        {
                            RestoreInitialState();
                        }
                        if (GUILayout.Button(ending?.cancel ?? DeathBySeventhsDawn.Story.StoryText.UI("取消"), buttonStyle))
                            confirmNewJourney = false;
                    }
                    GUILayout.Space(10);
                    if (GUILayout.Button(ending?.returnToTitle ?? "回到主页面", buttonStyle))
                        MainMenuUI.ReturnToMainMenu();
                    GUILayout.EndScrollView();
                    GUILayout.EndArea();
                }
                else if (debugOpen)
                {
                    GUI.color = new Color(0, 0, 0, .88f);
                    GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                    GUI.color = previousColor;
                    float panelWidth = Mathf.Min(820, width - 24);
                    GUILayout.BeginArea(new Rect((width - panelWidth) / 2, 12, panelWidth, height - 24), boxStyle);
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("调试面板"), titleStyle);
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("本次游戏已解锁全部测试功能 · 按 ~ 或 Esc 关闭"), labelStyle);
                    scroll = GUILayout.BeginScrollView(scroll);
                    DrawStatus(game, clock);
                    foreach (var ability in game.Settings.abilities)
                        if (ability != null && game.HasAbility(ability.id))
                            GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("已解锁能力：") + AbilityName(ability.id), labelStyle);
                    GUILayout.Space(12);
                    if (!confirmNewJourney)
                    {
                        if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("回到初始状态"), buttonStyle))
                            confirmNewJourney = true;
                    }
                    else
                    {
                        GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("清空这次旅程的日记、知识、变异、轮回和剧情记录，从第 1 天的开场梦境重新开始。"), labelStyle);
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("确认重置"), buttonStyle))
                            RestoreInitialState();
                        if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("取消"), buttonStyle))
                            confirmNewJourney = false;
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Space(16);
                    GUI.enabled = !confirmNewJourney;
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("推进时段（每天四个时段）"), buttonStyle))
                        clock.AdvanceTime();
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("重播第一天广播"), buttonStyle))
                        DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.Replay();
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("线索开发者面板"), buttonStyle))
                        DeathBySeventhsDawn.Interaction.InvestigationDevTools.Open();
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("上床睡觉（跳到次日早晨）"), buttonStyle))
                        clock.SleepUntilNextMorning();
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("恢复全部体力"), buttonStyle))
                        game.Heal(game.MaxVitality);
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("损失 25 体力（归零后轮回）"), buttonStyle))
                        game.Damage(25);
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("死亡并开始下一轮"), buttonStyle))
                        LoopManager.Instance.RestartLoop();
                    foreach (KnowledgeTier tier in System.Enum.GetValues(typeof(KnowledgeTier)))
                        if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("获得") + TierName(tier) + DeathBySeventhsDawn.Story.StoryText.UI("知识"), buttonStyle))
                            KnowledgeManager.Instance.Unlock("prototype-" + tier + "-" + System.Guid.NewGuid().ToString("N"), tier);
                    GUI.enabled = true;
                    GUILayout.EndScrollView();
                    if (GUILayout.Button(DeathBySeventhsDawn.Story.StoryText.UI("关闭"), buttonStyle))
                        Close();
                    GUILayout.EndArea();
                }
                else if (enteringCode)
                {
                    // 默认不显示测试状态、提示或按钮；按 ~ 后才显示口令框。
                    float promptWidth = Mathf.Min(610, width - 24);
                    GUILayout.BeginArea(new Rect((width - promptWidth) / 2, Mathf.Max(12, (height - 180) / 2), promptWidth, 180), boxStyle);
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("调试面板"), titleStyle);
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("输入口令：") + new string('*', code.Length) + "_", labelStyle);
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("输入正确后自动打开 · Esc 取消"), labelStyle);
                    GUILayout.EndArea();
                }
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; }
        }
    }
}
