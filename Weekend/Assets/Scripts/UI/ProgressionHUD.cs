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
        public static bool DebugConsoleOpen { get; private set; }

        // 本次游戏只验证一次口令。此对象跨场景/轮回保留；关闭面板不会清除解锁状态。
        // 不写入存档，重新启动游戏后仍需首次输入 1997。
        bool unlockedThisSession;
        bool enteringCode, debugOpen, confirmNewJourney;
        string code = "";
        Vector2 scroll;
        int targetDay=1, targetPeriod;
        GUIStyle labelStyle, titleStyle, buttonStyle, boxStyle;
        void Update()
        {
            if (PauseMenuUI.WorldBlocked)
                return;
            if (DeathBySeventhsDawn.Interaction.InvestigationDevTools.WorldBlocked)
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
            targetDay=TimeManager.Instance.Days;
            targetPeriod=(int)TimeManager.Instance.Period;
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
            EndingCollectionUI.ClosePreview();
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
                StoryDebugActions.SetEnabled(true);
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
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23,
                wordWrap = true,
                padding = new RectOffset(10, 10, 5, 5)
            };
            titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 29,
                fontStyle = FontStyle.Bold
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 23,
                wordWrap = true,
                padding = new RectOffset(14, 14, 12, 12)
            };
            labelStyle.font = chineseFont;
            titleStyle.font = chineseFont;
            buttonStyle.font = chineseFont;
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(18, 18, 16, 16)
            };
        }

        void DrawStatus(GameManager game, TimeManager clock)
        {
            GUILayout.Label(StoryText.Local($"第 {clock.Days} 天  {PeriodName(clock.Period)}   |   轮回次数 {game.LoopCount}", $"Day {clock.Days}  {PeriodName(clock.Period)}   |   Loops {game.LoopCount}"), labelStyle);
            GUILayout.Label(StoryText.Local($"体力 {game.Vitality:0.#} / 上限 {game.MaxVitality:0.#}", $"Vitality {game.Vitality:0.#} / Maximum {game.MaxVitality:0.#}"), labelStyle);
            GUILayout.Label(StoryText.Local($"知识 {game.Knowledge:0.#}   |   等级 {game.KnowledgeLevel}", $"Knowledge {game.Knowledge:0.#}   |   Level {game.KnowledgeLevel}"), labelStyle);
            GUILayout.Label(StoryText.Local($"变异 {game.Mutation:0.#}   |   理智 {game.Sanity:0.#}（{LayerName(game.Layer)}）", $"Mutation {game.Mutation:0.#}   |   Sanity {game.Sanity:0.#} ({LayerName(game.Layer)})"), labelStyle);
        }

        static string PeriodName(TimePeriod period) => new[]
        {
            DeathBySeventhsDawn.Story.StoryText.UI("早晨"),
            DeathBySeventhsDawn.Story.StoryText.UI("下午"),
            DeathBySeventhsDawn.Story.StoryText.UI("傍晚"),
            DeathBySeventhsDawn.Story.StoryText.UI("深夜")
        }[(int)period];
        static string LayerName(SanityLayer layer) => new[]
        {
            DeathBySeventhsDawn.Story.StoryText.UI("清醒"),
            DeathBySeventhsDawn.Story.StoryText.UI("动摇"),
            DeathBySeventhsDawn.Story.StoryText.UI("破碎"),
            DeathBySeventhsDawn.Story.StoryText.UI("启示")
        }[(int)layer];
        static string TierName(KnowledgeTier tier) => new[]
        {
            DeathBySeventhsDawn.Story.StoryText.UI("普通"),
            DeathBySeventhsDawn.Story.StoryText.UI("禁忌"),
            DeathBySeventhsDawn.Story.StoryText.UI("宇宙")
        }[(int)tier];
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
            if ((MainMenuUI.WorldBlocked || PauseMenuUI.WorldBlocked) && !debugOpen && !enteringCode)
                return;
            if (DeathBySeventhsDawn.Interaction.InvestigationDevTools.WorldBlocked)
                return;
            PrepareStyles();
            PrototypeButtons.Apply(buttonStyle);
            var previousColor = GUI.color;
            // Scale with resolution, keeping large readable controls on high-DPI game views.
            var previousMatrix = GUI.matrix;
            int oldDepth = GUI.depth;
            if (debugOpen || enteringCode)
                GUI.depth = -500;
            float scale = Mathf.Clamp(Screen.height / 900f, .8f, 2.4f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            try
            {
                if (EndingCollectionUI.PreviewOpen)
                {
                    EndingCollectionUI.DrawPreview(width, height);
                    return;
                }

                if (WorldEndSequence.IsPresenting && !debugOpen && !enteringCode)
                    return;
                if (game.HasEnded && !debugOpen && !enteringCode)
                {
                    EndingCollectionUI.DrawResult(game, width, height);
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
                    if(PrototypeButtons.LayoutButton(StoryDebugActions.Enabled
                        ? StoryText.Local("自由调试：已开启 · 点击恢复正常限制", "Free debug: ON · Restore normal gates")
                        : StoryText.Local("自由调试：已关闭 · 点击跳过强制剧情与限制", "Free debug: OFF · Bypass mandatory story and gates"),buttonStyle))
                        StoryDebugActions.SetEnabled(!StoryDebugActions.Enabled);
                    GUILayout.BeginHorizontal();
                    if(PrototypeButtons.LayoutButton("−",buttonStyle))targetDay=Mathf.Max(1,targetDay-1);
                    GUILayout.Label("Days "+targetDay,labelStyle);
                    if(PrototypeButtons.LayoutButton("+",buttonStyle))targetDay++;
                    if(PrototypeButtons.LayoutButton(PeriodName((TimePeriod)targetPeriod),buttonStyle))targetPeriod=(targetPeriod+1)%4;
                    if(PrototypeButtons.LayoutButton(StoryText.Local("应用时间","Set time"),buttonStyle)) { StoryDebugActions.SetEnabled(true);clock.DebugSetTime(targetDay,(TimePeriod)targetPeriod); }
                    GUILayout.EndHorizontal();
                    var rooms=new[]{"Bedroom","LivingRoom","Bathroom","Balcony","Corridor","Stairwell","Rooftop"};
                    var roomNames=new[]{"卧室","客厅","卫生间","阳台","楼道","楼梯间","天台"};
                    for(int i=0;i<rooms.Length;i++)
                    {
                        if(i%2==0)GUILayout.BeginHorizontal();
                        if(PrototypeButtons.LayoutButton(StoryText.Local(roomNames[i],rooms[i]),buttonStyle)) StoryDebugActions.Jump(rooms[i],targetDay,targetPeriod);
                        if(i%2==1||i==rooms.Length-1)GUILayout.EndHorizontal();
                    }
                    bool resolved=game.State.buildingCrisisResolved,seven=game.State.daySevenCrisisResolved;
                    if(PrototypeButtons.LayoutButton(StoryText.Local("第五天爆楼：", "Day 5 bombing: ")+StoryText.Local(resolved?"已解除（点击还原）":"未解除（点击解除）",resolved?"Prevented (restore)":"Unresolved (prevent)"),buttonStyle))resolved=!resolved;
                    if(PrototypeButtons.LayoutButton(StoryText.Local("第七天危机：", "Day 7 crisis: ")+StoryText.Local(seven?"已解除（点击还原）":"未解除（点击解除）",seven?"Prevented (restore)":"Unresolved (prevent)"),buttonStyle))seven=!seven;
                    if(resolved!=game.State.buildingCrisisResolved||seven!=game.State.daySevenCrisisResolved)
                    {game.State.buildingCrisisResolved=resolved;game.State.daySevenCrisisResolved=seven;ProgressionSave.Write(game.State);}
                    if(PrototypeButtons.LayoutButton(StoryText.Local("原地等待至次日（不算睡眠）","Wait here until tomorrow (no sleep)"),buttonStyle))
                    {int day=clock.Days;for(int i=0;i<4&&clock.Days==day;i++)clock.AdvanceTime();}
                    if(PrototypeButtons.LayoutButton(StoryText.Local("播放第五天爆楼（关闭自由调试后致死）","Play Day 5 explosion (lethal outside free debug)"),buttonStyle)) WorldEndSequence.BeginFatalCrisis(true);

                    if (PrototypeButtons.LayoutButton(StoryText.Local("跳过当前强制剧情 · 进入自由探索", "Skip forced story · Free exploration"), buttonStyle))
                        StoryDebugActions.SkipForcedStory();
                    GUILayout.BeginHorizontal();
                    for (int ending = 1; ending <= 3; ending++)
                        if (PrototypeButtons.LayoutButton(StoryText.Local("预览结局 ", "Preview ending ") + ending, buttonStyle))
                            StoryDebugActions.PreviewEnding(ending);
                    GUILayout.EndHorizontal();
                    foreach (var ability in game.Settings.abilities)
                        if (ability != null && game.HasAbility(ability.id))
                            GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("已解锁能力：") + AbilityName(ability.id), labelStyle);
                    GUILayout.Space(12);
                    if (!confirmNewJourney)
                    {
                        if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("回到初始状态"), buttonStyle))
                            confirmNewJourney = true;
                    }
                    else
                    {
                        GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("清空这次旅程的日记、知识、变异、轮回和剧情记录，从第 1 天的开场梦境重新开始。"), labelStyle);
                        GUILayout.BeginHorizontal();
                        if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("确认重置"), buttonStyle))
                            RestoreInitialState();
                        if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("取消"), buttonStyle))
                            confirmNewJourney = false;
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(16);
                    GUI.enabled = !confirmNewJourney;
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("推进时段（每天四个时段）"), buttonStyle))
                        clock.AdvanceTime();
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("重播第一天广播"), buttonStyle))
                        DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.Replay();
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("线索开发者面板"), buttonStyle))
                        DeathBySeventhsDawn.Interaction.InvestigationDevTools.Open();
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("上床睡觉（跳到次日早晨）"), buttonStyle))
                        clock.SleepUntilNextMorning();
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("恢复全部体力"), buttonStyle))
                        game.Heal(game.MaxVitality);
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("损失 25 体力（归零后轮回）"), buttonStyle))
                        game.Damage(25);
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("死亡并开始下一轮"), buttonStyle))
                        game.Damage(game.MaxVitality);
                    foreach (KnowledgeTier tier in System.Enum.GetValues(typeof(KnowledgeTier)))
                        if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("获得") + TierName(tier) + DeathBySeventhsDawn.Story.StoryText.UI("知识"), buttonStyle))
                            KnowledgeManager.Instance.Unlock("prototype-" + tier + "-" + System.Guid.NewGuid().ToString("N"), tier);
                    GUI.enabled = true;
                    GUILayout.EndScrollView();
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("关闭"), buttonStyle))
                        Close();
                    GUILayout.EndArea();
                }
                else if (enteringCode)
                {
                    // 默认不显示测试状态、提示或按钮；按 ~ 后才显示口令框。
                    float promptWidth = Mathf.Min(610, width - 24);
                    GUILayout.BeginArea(new Rect((width - promptWidth) / 2, Mathf.Max(12, (height - 180) / 2), promptWidth, 180), boxStyle);
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("调试面板"), titleStyle);
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("输入口令：") + new string ('*', code.Length) + "_", labelStyle);
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("输入正确后自动打开 · Esc 取消"), labelStyle);
                    GUILayout.EndArea();
                }
            }
            finally
            {
                GUI.depth = oldDepth;
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
            }
        }
    }
}
