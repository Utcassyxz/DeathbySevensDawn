// 职责：自由探索时的床和电视菜单；教程阶段由 TutorialGate 提供反馈。
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Player;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Interaction
{
    // Bind existing furniture at runtime; scene assets and their colliders stay unchanged.
    public sealed class FurnitureInteraction : MonoBehaviour
    {
        public static bool IsBusy { get; private set; }

        [SerializeField]
        float interactionDistance = .18f;
        GameObject bed, television, sofa, player, target;
        PlayerController2D movement;
        bool restoreMovement;
        GUIStyle label, button, box;
        Vector2 scroll;
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += CloseMenu;
            Bind();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= CloseMenu;
            CloseMenu();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CloseMenu();
            Bind();
        }

        void Bind()
        {
            bed = GameObject.Find("Bed");
            sofa = GameObject.Find("Old sofa");
            television = GameObject.Find("Television and stand");
            player = GameObject.FindWithTag("Player");
        }

        float Distance(GameObject item) => PropReach.Gap(item, player);
        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.HasEnded)
            {
                CloseMenu();
                target = null;
                return;
            }

            if (!GameManager.Instance.State.tutorialComplete)
            {
                CloseMenu();
                target = null;
                return;
            }

            if (ProgressionHUD.DebugConsoleOpen || StoryJournal.WorldBlocked)
                return;
            bool interact = false, cancel = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard=Keyboard.current;
            interact=keyboard!=null && keyboard.eKey.wasPressedThisFrame;
            cancel=keyboard!=null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            interact=Input.GetKeyDown(KeyCode.E); cancel=Input.GetKeyDown(KeyCode.Escape);
#endif
            if (IsBusy)
            {
                if (interact && target == bed)
                {
                    CloseMenu();
                    if (!NightDreamSequence.BeginSleep())
                        TimeManager.Instance.SleepUntilNextMorning();
                    return;
                }

                if (interact && target == sofa)
                {
                    RestOnePeriod();
                    return;
                }

                if (cancel)
                    CloseMenu();
                return;
            }

            if (player == null)
                Bind();
            // Give the existing window interaction priority if interaction areas overlap.
            var window = Object.FindAnyObjectByType<WindowLookout2D>();
            if (window != null && (window.IsOpen || window.CanInteract))
            {
                target = null;
                return;
            }

            float bedDistance = Distance(bed), tvDistance = Distance(television);
            target = bedDistance <= tvDistance ? bed : television;
            float nearest = Mathf.Min(bedDistance, tvDistance), sofaDistance = Distance(sofa);
            if (sofaDistance < nearest)
            {
                target = sofa;
                nearest = sofaDistance;
            }

            if (nearest > interactionDistance)
                target = null;
            if (target != null && interact && WorldPromptUI.CanActivate("furniture"))
            {
                movement = player.GetComponent<PlayerController2D>();
                restoreMovement = movement != null && movement.enabled;
                if (restoreMovement)
                    movement.enabled = false;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
                IsBusy = true;
                if (target == television)
                    WatchTelevision();
            }
        }

        public bool WatchTelevision()
        {
            if (!GameManager.Instance.State.tutorialComplete || !IsBusy || target != television || StoryJournal.WorldBlocked)
                return false;
            CloseMenu();
            return DayOneSupernovaEvent.Instance.OpenTelevision();
        }

        // A deliberate rest, rather than a repeated TV bulletin, provides normal period progression.
        public bool RestOnePeriod()
        {
            if (GameManager.Instance == null || GameManager.Instance.HasEnded || !GameManager.Instance.State.tutorialComplete || StoryJournal.WorldBlocked || Distance(sofa) > interactionDistance)
                return false;
            CloseMenu();
            TimeManager.Instance.AdvanceTime();
            return true;
        }

        void CloseMenu()
        {
            if (restoreMovement && movement != null)
                movement.enabled = true;
            restoreMovement = false;
            movement = null;
            IsBusy = false;
            scroll = Vector2.zero;
        }

        void PrepareStyles()
        {
            if (label != null)
                return;
            var font = DeathBySeventhsDawn.Progression.GameTypography.Current;
            label = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 26,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            button = new GUIStyle(GUIStyle.none)
            {
                font = font,
                fontSize = 25,
                wordWrap = true,
                padding = new RectOffset(16, 16, 14, 14),
                alignment = TextAnchor.MiddleCenter
            };
            button.normal.textColor = new Color(.74f, .78f, .66f);
            button.hover.textColor = new Color(.96f, .94f, .82f);
            button.active.textColor = Color.white;
            box = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(24, 24, 24, 24)
            };
        }

        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(label, button, box);
            if (GameManager.Instance == null || !GameManager.Instance.State.tutorialComplete || GameManager.Instance.HasEnded || ProgressionHUD.DebugConsoleOpen || StoryJournal.WorldBlocked)
                return;
            if (target == null && !IsBusy)
                return;
            PrepareStyles();
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(button);
            var matrix = GUI.matrix;
            var color = GUI.color;
            float scale = Mathf.Clamp(Screen.height / 900f, .8f, 2.4f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            try
            {
                if (!IsBusy)
                {
                    WorldPromptUI.Offer("furniture", target == bed ? StoryText.UI("copy.FurnitureInteraction.89") : target == sofa ? StoryText.UI("copy.FurnitureInteraction.90") : StoryText.UI("copy.FurnitureInteraction.91"), 40);
                    return;
                }

                GUI.color = new Color(0, 0, 0, .88f);
                GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                GUI.color = color;
                float panelWidth = Mathf.Min(760, width - 24), panelHeight = Mathf.Min(target == bed ? 480 : 620, height - 24);
                var frame = new Rect((width - panelWidth) / 2, (height - panelHeight) / 2, panelWidth, panelHeight);
                NarrativeUI.Panel(frame);
                GUILayout.BeginArea(frame, box);
                scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.Label(target == bed ? DeathBySeventhsDawn.Story.StoryText.UI("copy.FurnitureInteraction.92") : target == sofa ? DeathBySeventhsDawn.Story.StoryText.UI("copy.FurnitureInteraction.93") : DeathBySeventhsDawn.Story.StoryText.UI("copy.FurnitureInteraction.94"), label);
                GUILayout.Space(20);
                if (target == bed)
                {
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("copy.FurnitureInteraction.95"), label);
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("copy.FurnitureInteraction.96"), button))
                    {
                        CloseMenu();
                        if (!NightDreamSequence.BeginSleep())
                            TimeManager.Instance.SleepUntilNextMorning();
                    }
                }
                else if (target == sofa)
                {
                    GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.Current.morning.sofaFree, label);
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("copy.FurnitureInteraction.97"), button))
                        RestOnePeriod();
                }

                GUILayout.Space(20);
                GUILayout.EndScrollView();
                if (DeathBySeventhsDawn.Progression.PrototypeButtons.LayoutButton(DeathBySeventhsDawn.Story.StoryText.UI("copy.FurnitureInteraction.98"), button))
                    CloseMenu();
                GUILayout.Label(DeathBySeventhsDawn.Story.StoryText.UI("copy.FurnitureInteraction.99"), label);
                GUILayout.EndArea();
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.color = color;
            }
        }
    }
}
