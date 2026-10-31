// 职责：四楼与五楼天台往返、望远镜搬运与放置，以及真实天台观测的剧情和输入锁。
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Interaction
{
    public sealed class RooftopAccess : MonoBehaviour
    {
        public static RooftopAccess Instance { get; private set; }
        public static bool WorldBlocked => Instance != null && (Instance.open || Time.frameCount <= Instance.blockedFrame);
        public bool IsOpen => open;
        public bool IsRevelation => open && revelation;

        public static readonly Vector2 StairsFour = new Vector2(-6.05f, -.2f);
        public static readonly Vector2 StairsFive = new Vector2(-4.8f, -2.1f);
        public static readonly Vector2 TelescopeSpot = new Vector2(3.4f, 1.25f);
        ProgressionState State => GameManager.Instance.State;

        GameObject player, roofScope;
        string message, prompt, arrivalScene;
        Vector2 arrivalPosition;
        bool open, revelation;
        int openedFrame, blockedFrame = -1;
        Sprite square;
        Texture2D pixel, god;
        GUIStyle title, body, small, button;
        readonly DialoguePages subtitles = new DialoguePages();
        void Awake()
        {
            Instance = this;
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            square = Sprite.Create(pixel, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            god = Resources.Load<Texture2D>("Story/AncientGodRealistic");
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += Loaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += Reset;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= Loaded;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= Reset;
            Close();
        }

        void Start() => Bind(SceneManager.GetActiveScene());
        void Loaded(Scene scene, LoadSceneMode mode)
        {
            Close();
            Bind(scene);
            if (scene.name == arrivalScene && player != null)
            {
                player.transform.position = arrivalPosition;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.position = arrivalPosition;
                    rb.linearVelocity = Vector2.zero;
                }

                Physics2D.SyncTransforms();
                arrivalScene = null;
            }
        }

        void Reset()
        {
            Close();
            prompt = null;
        }

        void Bind(Scene scene)
        {
            player = GameObject.FindWithTag("Player");
            roofScope = null;
            if (scene.name == "Corridor" && GameObject.Find("Stairs to floor five") == null)
            {
                var root = new GameObject("Stairs to floor five");
                SceneManager.MoveGameObjectToScene(root, scene);
                for (int i = 0; i < 6; i++)
                    Part(root.transform, "Stair tread", StairsFour + new Vector2(0, (i - 2) * .13f), new Vector2(.95f, .08f), new Color(.55f - i * .035f, .51f - i * .03f, .43f - i * .025f), 2);
                Part(root.transform, "Stair rail", StairsFour + new Vector2(-.57f, .05f), new Vector2(.055f, .9f), new Color(.2f, .23f, .22f), 3);
            }

            if (scene.name == "Rooftop" && State.telescopeOnRoof)
                BuildScope();
        }

        void Part(Transform parent, string name, Vector2 p, Vector2 size, Color tint, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = p;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = square;
            r.color = tint;
            r.sortingOrder = order;
            var floor = GameObject.Find("Rooftop pixel environment") ?? GameObject.Find("Rooftop tiles") ?? GameObject.Find("Corridor tiles");
            if (floor != null)
                r.sharedMaterial = floor.GetComponent<SpriteRenderer>().sharedMaterial;
        }

        void BuildScope()
        {
            if (roofScope != null)
                return;
            roofScope = new GameObject("Rooftop telescope");
            SceneManager.MoveGameObjectToScene(roofScope, SceneManager.GetActiveScene());
            roofScope.transform.position = TelescopeSpot;
            TelescopeVisual.Attach(roofScope);
        }

        bool Near(Vector2 p, float distance = 1.05f) => player != null && Vector2.Distance(player.transform.position, p) <= distance;
        public bool UseStairs()
        {
            if (GameManager.Instance.HasEnded || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                return false;
            string scene = SceneManager.GetActiveScene().name;
            if (!State.tutorialComplete)
                return false;
            if (scene == "Corridor" && Near(StairsFour))
            {
                arrivalScene = "Rooftop";
                arrivalPosition = StairsFive + Vector2.right * 1.1f;
                SceneManager.LoadScene("Rooftop");
                return true;
            }

            if (scene == "Rooftop" && Near(StairsFive))
            {
                arrivalScene = "Corridor";
                arrivalPosition = StairsFour + Vector2.right * 1.1f;
                SceneManager.LoadScene("Corridor");
                return true;
            }

            return false;
        }

        public bool TakeTelescope()
        {
            if (!State.tutorialComplete || State.telescopeCarried || State.telescopeOnRoof || SceneManager.GetActiveScene().name != "Balcony" || GameManager.Instance.HasEnded || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                return false;
            var scope = GameObject.Find("阳台望远镜");
            if (scope == null || !Near(scope.transform.position, 1.2f))
                return false;
            State.telescopeCarried = true;
            ProgressionSave.Write(State);
            Destroy(scope);
            Show(StoryText.Current.rooftop.carried, false);
            return true;
        }

        public bool PlaceTelescope()
        {
            if (GameManager.Instance.HasEnded || StoryJournal.WorldBlocked || !State.tutorialComplete || !State.telescopeCarried || State.telescopeOnRoof || SceneManager.GetActiveScene().name != "Rooftop" || !Near(TelescopeSpot))
                return false;
            State.telescopeCarried = false;
            State.telescopeOnRoof = true;
            ProgressionSave.Write(State);
            BuildScope();
            Show(StoryText.Current.rooftop.placed, false);
            return true;
        }

        public bool Observe()
        {
            if (GameManager.Instance.HasEnded || StoryJournal.WorldBlocked || SceneManager.GetActiveScene().name != "Rooftop" || !State.telescopeOnRoof || !Near(TelescopeSpot))
                return false;
            var text = StoryText.Current.rooftop;
            var clock = TimeManager.Instance;
            if (!StoryJournal.IsNight(clock.Period))
            {
                Show(text.daylight, false);
                return true;
            }

            if (clock.Days < 2)
            {
                State.firstNightObjectSeen = true;
                ProgressionSave.Write(State);
                Show(text.firstNight, false);
                return true;
            }

            // Physical placement plus a night observation sets the route flag. Evidence alone never does.
            GameManager.Instance.RecordAncientGodEncounter();
            KnowledgeManager.Instance.Observe(StoryContent.AncientKnowledgeId, KnowledgeTier.Cosmic);
            Show(text.revelation, true);
            return true;
        }

        void Show(string text, bool revealed)
        {
            subtitles.Reset();
            message = text;
            revelation = revealed;
            open = true;
            openedFrame = Time.frameCount;
        }

        public void Close()
        {
            if (open)
                blockedFrame = Time.frameCount;
            open = false;
            revelation = false;
            message = null;
        }

        void Update()
        {
            if (GameManager.Instance == null)
                return;
            if (GameManager.Instance.HasEnded)
            {
                Close();
                prompt = null;
                return;
            }

            var k = Keyboard.current;
            if (open)
            {
                if (Time.frameCount <= openedFrame + 1)
                    return;
                if (revelation && k != null && k.rKey.wasPressedThisFrame)
                    Investigation.Collect(StoryContent.AncientKnowledgeId);
                if (StoryControls.ContinuePressed)
                {
                    if (!subtitles.Next())
                        Close();
                }
                else if (k != null && k.escapeKey.wasPressedThisFrame)
                    Close();
                return;
            }

            prompt = null;
            if (StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy || ProgressionHUD.DebugConsoleOpen)
                return;
            if (player == null)
                player = GameObject.FindWithTag("Player");
            string scene = SceneManager.GetActiveScene().name;
            var text = StoryText.Current.rooftop;
            if (scene == "Corridor" && Near(StairsFour))
            {
                prompt = State.tutorialComplete ? text.up : TutorialGate.Reminder;
                if (k != null && k.eKey.wasPressedThisFrame && WorldPromptUI.CanActivate("rooftop"))
                    UseStairs();
            }
            else if (scene == "Rooftop" && Near(StairsFive))
            {
                prompt = text.down;
                if (k != null && k.eKey.wasPressedThisFrame && WorldPromptUI.CanActivate("rooftop"))
                    UseStairs();
            }
            else if (scene == "Rooftop" && Near(TelescopeSpot))
            {
                prompt = State.telescopeOnRoof ? text.observe : State.telescopeCarried ? text.place : text.missing;
                if (k != null && k.eKey.wasPressedThisFrame && WorldPromptUI.CanActivate("rooftop"))
                {
                    if (State.telescopeOnRoof)
                        Observe();
                    else
                        PlaceTelescope();
                }
            }
            else if (scene == "Balcony" && State.tutorialComplete && !State.telescopeCarried && !State.telescopeOnRoof)
            {
                var scope = GameObject.Find("阳台望远镜");
                if (scope != null && Near(scope.transform.position, 1.2f))
                {
                    prompt = text.carry;
                    if (k != null && k.fKey.wasPressedThisFrame)
                        TakeTelescope();
                }
            }
        }

        void OnGUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.HasEnded || MainMenuUI.WorldBlocked || GameSettingsUI.WorldBlocked)
                return;
            if (body == null)
            {
                body = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 25,
                    wordWrap = true
                };
                title = new GUIStyle(body)
                {
                    fontSize = 33,
                    fontStyle = FontStyle.Bold
                };
                small = new GUIStyle(body)
                {
                    fontSize = 18
                };
                button = new GUIStyle(body)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            GameTypography.Use(body, title, small, button);
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            var t = StoryText.Current.rooftop;
            try
            {
                if (!open)
                {
                    if (prompt != null)
                        WorldPromptUI.Offer("rooftop", prompt, 65);
                    string scene = SceneManager.GetActiveScene().name;
                    if (scene == "Corridor" || scene == "Rooftop")
                        GUI.Label(new Rect(25, 25, w - 50, 40), scene == "Corridor" ? t.floorFour : t.floorFive, small);
                    return;
                }

                GUI.depth = -50;
                if (revelation)
                {
                    GUI.color = new Color(.01f, .01f, .015f, .99f);
                    GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                float left = w * .12f, pw = w * .76f;
                if (revelation && god != null)
                    GUI.DrawTexture(new Rect(left, 45, pw, h * .48f), god, ScaleMode.ScaleToFit);
                var speech = NarrativeUI.DialogueRect(w, h);
                var subtitleStyle = new GUIStyle(body)
                {
                    fontSize = 23,
                    padding = new RectOffset()
                };
                subtitles.Set(message, subtitleStyle, speech.width - 50, speech.height - 86);
                string record = revelation ? (Investigation.Has(StoryContent.AncientKnowledgeId) ? t.recorded : t.record) + " · " : "";
                NarrativeUI.Dialogue(speech, revelation ? t.title : StoryText.Local("雨果", "Hugo"), subtitles.Current, subtitles.Counter + record + t.close, body);
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.color = color;
                GUI.depth = depth;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (square != null)
                Destroy(square);
            if (pixel != null)
                Destroy(pixel);
        }
    }
}
