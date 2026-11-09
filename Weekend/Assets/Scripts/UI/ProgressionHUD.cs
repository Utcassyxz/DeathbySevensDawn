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
    // Scene-independent ending UI and command console. Backquote opens directly.
    public class ProgressionHUD : MonoBehaviour
    {
        public static bool DebugConsoleOpen { get; private set; }

        // Compatibility fields for historical editor fixtures; no PIN is required by the player UI.
        bool unlockedThisSession;
        bool enteringCode, debugOpen, confirmNewJourney;
        string command="";
        readonly ConsoleBuffer console = new ConsoleBuffer();
        bool focusInput;
        string completionSeed;
        string[] completions;
        int completionIndex;
        GUIStyle consoleText, inputStyle;
        Vector2 scroll;
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
#elif ENABLE_LEGACY_INPUT_MANAGER
            if(Input.GetKeyDown(KeyCode.BackQuote)) { ToggleConsole(); return; }
            if(Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
#endif
        }

        void ToggleConsole()
        {
            if (debugOpen || enteringCode)
            {
                Close();
                return;
            }

            if (GameManager.Instance == null || TimeManager.Instance == null) return;
            // Snapshot before enabling debug: enabling also skips the opening story.
            bool freshSession = !NeighborDebugCommands.Isolated;
            NeighborDebugCommands.BeginSession();
            if (freshSession) StoryDebugActions.SetEnabled(true);
            unlockedThisSession = true;
            DebugConsoleOpen = debugOpen = true;
            enteringCode = false;
            command = ""; focusInput = true;
            if (console.Lines.Count == 0) console.Add(StoryText.UI("console.welcome"));
            scroll.y = float.MaxValue;
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

        public void CloseForEvent()=>Close();
        void Close()
        {
            DebugConsoleOpen = false;
            enteringCode = false;
            debugOpen = false;
            confirmNewJourney = false;
            EndingCollectionUI.ClosePreview();
        }

        void RestoreInitialState()
        {
            Close();
            GameManager.Instance.StartNewJourney();
        }

        // Kept for historical fixtures that invoked the removed PIN entry by reflection.
        void EnterDigit(char digit) { }

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

        void SubmitCommand()
        {
            var text = command.Trim();
            if (text.Length == 0) return;
            command = ""; completionSeed = null; focusInput = true;
            console.Record(text);
            if (text.Equals("clear", System.StringComparison.OrdinalIgnoreCase)) console.Lines.Clear();
            else
            {
                console.Add("> " + text);
                if (text.Equals("history", System.StringComparison.OrdinalIgnoreCase)) console.Add(string.Join("\n", console.History));
                else console.Add(GameCommandConsole.Execute(text));
            }
            if (text.Equals("close", System.StringComparison.OrdinalIgnoreCase)) Close();
            scroll.y = float.MaxValue;
        }

        public static Rect ConsoleRect(float width, float height) => new Rect(12, 12, Mathf.Max(1, width - 24), Mathf.Max(1, Mathf.Min(height - 24, 600)));
        void DrawConsole(float width, float height)
        {
            if (consoleText == null)
            {
                consoleText = new GUIStyle(labelStyle) { fontSize = 19, richText = false, padding = new RectOffset(4,4,2,2) };
                consoleText.normal.textColor = new Color(.78f,.86f,.77f);
                inputStyle = new GUIStyle(GUI.skin.textField) { font = GameTypography.Current, fontSize = 21, padding = new RectOffset(8,8,7,7), richText = false };
            }
            GameTypography.Use(consoleText, inputStyle);
            var panel = ConsoleRect(width, height);
            GUI.color = new Color(.025f,.035f,.03f,.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(panel.x+12,panel.y+10,panel.width-24,panel.height-20));
            GUILayout.Label(StoryText.UI("console.title") + "  /  " + StoryText.UI("console.isolated") + "  /  debug " + (StoryDebugActions.Enabled ? "on" : "off"), consoleText);
            GUILayout.Label(StoryText.UI("console.keys"), consoleText);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            GUILayout.Label(string.Join("\n", console.Lines), consoleText);
            GUILayout.EndScrollView();
            var e = Event.current;
            if (e.type == EventType.KeyDown && GUI.GetNameOfFocusedControl() == "neighbor-console-command")
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { SubmitCommand(); e.Use(); }
                else if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
                { command = console.Navigate(e.keyCode == KeyCode.UpArrow ? -1 : 1, command); completionSeed = null; focusInput = true; e.Use(); }
                else if (e.keyCode == KeyCode.Tab)
                {
                    if (completionSeed == null) { completionSeed = command; completions = GameCommandConsole.Complete(command); completionIndex = -1; }
                    if (completions.Length > 0) { completionIndex = (completionIndex + 1) % completions.Length; command = completions[completionIndex]; focusInput = true; }
                    e.Use();
                }
                else if (e.keyCode == KeyCode.BackQuote) { e.Use(); }
                else if (e.keyCode == KeyCode.Escape) { Close(); e.Use(); }
                else completionSeed = null;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label(">", consoleText, GUILayout.Width(22));
            GUI.SetNextControlName("neighbor-console-command");
            command = GUILayout.TextField(command, 256, inputStyle, GUILayout.Height(42));
            if (GUILayout.Button(StoryText.UI("console.run"), buttonStyle, GUILayout.Width(90), GUILayout.Height(42))) SubmitCommand();
            GUILayout.EndHorizontal();
            if (focusInput && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("neighbor-console-command");
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                editor.cursorIndex = editor.selectIndex = command.Length;
                focusInput = false;
            }
            GUILayout.EndArea();
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
                if(game.State.stageOneComplete && !debugOpen)return;
                if (game.HasEnded && !debugOpen && !enteringCode)
                {
                    EndingCollectionUI.DrawResult(game, width, height);
                }
                else if (debugOpen)
                {
                    DrawConsole(width, height);
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
