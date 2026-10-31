// 职责：鱼缸的逐日形态、对话及锡纸封缸；封缸后不再生成生物。
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeathBySeventhsDawn.Interaction
{
    // 客厅鱼缸的逐日异变；第三天开始可以对话，第六天成为 NPC 001。
    public sealed class AquariumEntity : MonoBehaviour
    {
        public const string NpcCode = "001";
        static Color C(double r, double g, double b, double a = 1) => new Color((float)r, (float)g, (float)b, (float)a);
        public static AquariumEntity Instance { get; private set; }
        public static bool WorldBlocked => Instance != null && (Instance.talking || Time.frameCount <= Instance.blockedUntilFrame);
        public bool IsTalking => talking;

        GameObject tank, formRoot, player;
        SpriteRenderer tankRenderer;
        Color tankOriginal;
        bool hasOriginal;
        int visualDay = -1, page, openedFrame;
        int blockedUntilFrame = -1;
        bool talking, nearby;
        float range = 2.05f;
        Sprite square, circle;
        Texture2D pixel, circleTexture;
        string[] pages;
        string speaker;
        GUIStyle text, title, button, small;
        readonly DialoguePages subtitles = new DialoguePages();
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnLoaded;
            if (TimeManager.Instance != null)
                TimeManager.Instance.TimeChanged += OnTimeChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += ResetEntity;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            if (TimeManager.Instance != null)
                TimeManager.Instance.TimeChanged -= OnTimeChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= ResetEntity;
            CloseDialogue();
            DestroyForm();
        }

        void Start()
        {
            Setup(SceneManager.GetActiveScene());
        }

        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            Setup(scene);
        }

        void Setup(Scene scene)
        {
            CloseDialogue();
            DestroyForm();
            RestoreTank();
            tank = null;
            tankRenderer = null;
            player = null;
            visualDay = -1;
            nearby = false;
            if (scene.name != "LivingRoom")
                return;
            tank = GameObject.Find("Aquarium and cabinet");
            if (tank == null)
                return;
            tankRenderer = tank.GetComponent<SpriteRenderer>();
            if (tankRenderer != null)
            {
                tankOriginal = tankRenderer.color;
                hasOriginal = true;
            }

            player = GameObject.FindWithTag("Player");
            RefreshForm();
        }

        void OnTimeChanged(int day, TimePeriod period)
        {
            if (day != visualDay)
                RefreshForm();
        }

        void ResetEntity()
        {
            CloseDialogue();
            DestroyForm();
            RestoreTank();
            visualDay = -1;
            nearby = false;
        }

        void RefreshForm()
        {
            if (tank == null)
                return;
            int day = TimeManager.Instance != null ? TimeManager.Instance.Days : 1;
            DestroyForm();
            RestoreTank();
            visualDay = day;
            if (day < 3 || GameManager.Instance.AquariumSealed)
                return;
            EnsureSprites();
            formRoot = new GameObject("Aquarium entity form");
            formRoot.transform.SetParent(tank.transform, false);
            formRoot.transform.localPosition = Vector3.zero;
            if (day >= 7)
                BuildImpostor();
            else if (day >= 6)
                BuildNpc();
            else if (day >= 5)
                BuildHalfBody();
            else if (day >= 4)
                BuildTalkingFish();
            else
                BuildDayThree();
        }

        void BuildDayThree() => BuildFishFace(false);
        void BuildTalkingFish() => BuildFishFace(true);

        void BuildFishFace(bool speaking)
        {
            if (tankRenderer != null)
                tankRenderer.color = Color.Lerp(tankOriginal, C(.34f, .50, .43f), .48f);
            // The fish silhouette stays readable; the human face is the wrong part.
            Circle("fish body", new Vector3(0, -.20f, 0), new Vector2(.92f, .55f), C(.16f, .27, .24f), 1);
            PartRot("tail upper", new Vector3(-.48f, -.10f, 0), new Vector2(.34f, .18f), C(.14f, .24, .21f), 1, 32);
            PartRot("tail lower", new Vector3(-.48f, -.35f, 0), new Vector2(.34f, .18f), C(.14f, .24, .21f), 1, -32);
            Part("dorsal fin", new Vector3(.02f, .10f, 0), new Vector2(.46f, .10f), C(.13f, .23, .20f), 1);
            Circle("human face", new Vector3(.13f, -.02f, 0), new Vector2(.30f, .36f), C(.72f, .62, .50f), 2);
            Circle("left eye white", new Vector3(.04f, .04f, 0), new Vector2(.09f, .07f), C(.84f, .82, .70f), 3);
            Circle("right eye white", new Vector3(.21f, .04f, 0), new Vector2(.09f, .07f), C(.84f, .82, .70f), 3);
            Circle("left pupil", new Vector3(.05f, .04f, 0), new Vector2(.035f, .035f), C(.04f, .05, .04f), 4);
            Circle("right pupil", new Vector3(.20f, .04f, 0), new Vector2(.035f, .035f), C(.04f, .05, .04f), 4);
            Part("nose bridge", new Vector3(.13f, -.06f, 0), new Vector2(.035f, .12f), C(.54f, .44, .35f), 4);
            Part("mouth", new Vector3(.13f, -.15f, 0), new Vector2(speaking ? .14f : .10f, speaking ? .035f : .014f), C(.22f, .10, .09f), 4);
            PartRot("gill", new Vector3(-.14f, -.18f, 0), new Vector2(.03f, .20f), C(.35f, .45, .38f), 3, 12);
            if (speaking)
            {
                Circle("bubble a", new Vector3(.30f, .34f, 0), new Vector2(.06f, .06f), C(.60f, .76, .72f, .55f), 3);
                Circle("bubble b", new Vector3(.40f, .48f, 0), new Vector2(.04f, .04f), C(.60f, .76, .72f, .40f), 3);
            }
        }

        void BuildImpostor()
        {
            if (tankRenderer != null)
                tankRenderer.color = Color.Lerp(tankOriginal, C(.25f, .31, .29f), .55f);
            Vector3 root = new Vector3(1.30f, -1.05f, 0);
            Part("hugo left leg", root + new Vector3(-.15f, .45f, 0), new Vector2(.20f, .92f), C(.13f, .14, .15f), 8);
            Part("hugo right leg", root + new Vector3(.16f, .45f, 0), new Vector2(.20f, .92f), C(.13f, .14, .15f), 8);
            Part("hugo coat", root + new Vector3(0, 1.28f, 0), new Vector2(.62f, 1.0f), C(.23f, .20, .16f), 9);
            Part("hugo left arm", root + new Vector3(-.40f, 1.23f, 0), new Vector2(.15f, .88f), C(.23f, .20, .16f), 9);
            Part("hugo right arm", root + new Vector3(.41f, 1.21f, 0), new Vector2(.15f, .90f), C(.23f, .20, .16f), 9);
            Circle("hugo face", root + new Vector3(0, 2.02f, 0), new Vector2(.43f, .50f), C(.66f, .54, .43f), 10);
            Circle("hugo hair", root + new Vector3(0, 2.20f, 0), new Vector2(.46f, .22f), C(.13f, .11, .09f), 11);
            Circle("hugo eye left", root + new Vector3(-.09f, 2.04f, 0), new Vector2(.07f, .05f), C(.06f, .06, .05f), 12);
            Circle("hugo eye right", root + new Vector3(.09f, 2.04f, 0), new Vector2(.07f, .05f), C(.06f, .06, .05f), 12);
            Part("hugo mouth", root + new Vector3(0, 1.88f, 0), new Vector2(.12f, .015f), C(.28f, .14, .12f), 12);
        }

        void BuildHalfBody()
        {
            if (tankRenderer != null)
                tankRenderer.color = Color.Lerp(tankOriginal, C(.28f, .47, .39f), .72f);
            Part("torso", new Vector3(0, .78f, 0), new Vector2(.85f, 1.35f), C(.16f, .27, .23f), 2);
            Part("left arm", new Vector3(-.55f, .72f, 0), new Vector2(.18f, .85f), C(.14f, .24, .21f), 3);
            Part("right arm", new Vector3(.58f, .88f, 0), new Vector2(.18f, .95f), C(.14f, .24, .21f), 3);
            Circle("head", new Vector3(0, 1.58f, 0), new Vector2(.55f, .55f), C(.25f, .38, .32f), 4);
            Circle("eye", new Vector3(.08f, 1.65f, 0), new Vector2(.13f, .13f), C(.86f, .83, .61f), 5);
            Circle("pupil", new Vector3(.08f, 1.65f, 0), new Vector2(.055f, .055f), C(.04f, .06, .05f), 6);
            Part("water line", new Vector3(0, 1.02f, 0), new Vector2(1.1f, .045f), C(.55f, .72, .64f), 7);
        }

        void BuildNpc()
        {
            if (tankRenderer != null)
                tankRenderer.color = Color.Lerp(tankOriginal, C(.24f, .40, .35f), .48f);
            // 001 站在鱼缸右侧，脚踩在地板上。
            Vector3 root = new Vector3(1.42f, -1.05f, 0);
            Part("001 left leg", root + new Vector3(-.16f, .45f, 0), new Vector2(.22f, .95f), C(.12f, .21, .18f), 8);
            Part("001 right leg", root + new Vector3(.17f, .45f, 0), new Vector2(.22f, .95f), C(.12f, .21, .18f), 8);
            Part("001 torso", root + new Vector3(0, 1.3f, 0), new Vector2(.62f, .95f), C(.16f, .27, .23f), 9);
            Part("001 left arm", root + new Vector3(-.42f, 1.25f, 0), new Vector2(.16f, .9f), C(.13f, .23, .20f), 9);
            Part("001 right arm", root + new Vector3(.43f, 1.22f, 0), new Vector2(.16f, .92f), C(.13f, .23, .20f), 9);
            Circle("001 head", root + new Vector3(0, 2.05f, 0), new Vector2(.48f, .48f), C(.24f, .37, .31f), 10);
            Circle("001 eye left", root + new Vector3(-.1f, 2.09f, 0), new Vector2(.10f, .10f), C(.88f, .84, .62f), 11);
            Circle("001 eye right", root + new Vector3(.1f, 2.07f, 0), new Vector2(.08f, .08f), C(.88f, .84, .62f), 11);
            Part("001 water drip", root + new Vector3(.3f, .72f, 0), new Vector2(.05f, .55f), C(.40f, .55, .50f, 0.65f), 7);
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.HasEnded)
            {
                nearby = false;
                CloseDialogue();
                return;
            }

            if (GameManager.Instance.AquariumSealed)
            {
                if (formRoot != null)
                    DestroyForm();
                nearby = false;
                if (talking)
                    TickDialogue();
                return;
            }

            if (TutorialGate.IsBlocked("aquarium"))
            {
                nearby = false;
                return;
            }

            if (talking)
            {
                TickDialogue();
                return;
            }

            if (TimeManager.Instance != null && TimeManager.Instance.Days != visualDay)
                RefreshForm();
            if (tank == null || TimeManager.Instance == null || (TimeManager.Instance.Days < 3 && !HasFoil))
            {
                nearby = false;
                return;
            }

            if (ProgressionHUD.DebugConsoleOpen || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
            {
                nearby = false;
                return;
            }

            var window = Object.FindAnyObjectByType<WindowLookout2D>();
            if (window != null && window.IsOpen)
            {
                nearby = false;
                return;
            }

            if (player == null)
                player = GameObject.FindWithTag("Player");
            Vector2 point = InteractionPoint();
            nearby = player != null && Vector2.Distance(player.transform.position, point) <= range;
            if (nearby && InteractPressed() && WorldPromptUI.CanActivate("aquarium"))
            {
                if (HasFoil)
                    OpenOffer();
                else
                    OpenDialogue();
            }
        }

        Vector2 InteractionPoint()
        {
            if (tank == null)
                return Vector2.zero;
            Vector3 point = tank.transform.position;
            if (visualDay >= 6)
                point += new Vector3(1.42f, -.35f, 0);
            return point;
        }

        bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;return k!=null&&k.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }

        public bool HasFoil => GameManager.Instance != null && GameManager.Instance.State.inventory.Contains(CultNeighbor.Token);
        public bool FormVisible => formRoot != null && !GameManager.Instance.AquariumSealed;

        void OpenOffer()
        {
            pages = new[]
            {
                TimeManager.Instance.Days < 3 ? StoryText.Current.aquarium.offerBefore : StoryText.Current.aquarium.offer
            };
            speaker = StoryText.Current.aquarium.sealedTitle;
            page = 0;
            openedFrame = Time.frameCount;
            talking = true;
        }

        /// <summary>Consume exactly one token only while standing at the tank; the seal survives reloads and loops.</summary>
        public bool ThrowFoil()
        {
            var game = GameManager.Instance;
            if (game == null || game.HasEnded || game.AquariumSealed || TutorialGate.IsBlocked("aquarium") || !HasFoil || tank == null || player == null || Vector2.Distance(player.transform.position, InteractionPoint()) > range)
                return false;
            game.State.inventory.Remove(CultNeighbor.Token);
            game.State.aquariumSealed = true;
            ProgressionSave.Write(game.State);
            DestroyForm();
            RestoreTank();
            pages = new[]
            {
                TimeManager.Instance.Days < 3 ? StoryText.Current.aquarium.sealedBefore : StoryText.Current.aquarium.sealedBody
            };
            speaker = StoryText.Current.aquarium.sealedTitle;
            page = 0;
            openedFrame = Time.frameCount;
            talking = true;
            return true;
        }

        void OpenDialogue()
        {
            if (GameManager.Instance.HasEnded || GameManager.Instance.AquariumSealed || TutorialGate.IsBlocked("aquarium"))
                return;
            var aq = StoryText.Current.aquarium;
            pages = visualDay >= 7 ? aq?.day7Pages : visualDay >= 6 ? aq?.day6Pages : visualDay >= 5 ? aq?.day5Pages : visualDay >= 4 ? aq?.day4Pages : aq?.day3Pages;
            if (pages == null || pages.Length == 0)
                return;
            speaker = visualDay >= 7 ? (aq?.speakerDay7 ?? "长得像雨果的东西") : visualDay >= 6 ? (aq?.speakerDay6 ?? NpcCode) : visualDay >= 4 ? (aq?.speakerDay4 ?? "鱼") : (aq?.speakerDay3 ?? "鱼缸里的鱼");
            page = 0;
            openedFrame = Time.frameCount;
            talking = true;
        }

        void TickDialogue()
        {
            if (pages == null || pages.Length == 0)
            {
                CloseDialogue();
                return;
            }

            if (Time.frameCount <= openedFrame + 2)
                return;
            bool advance = false, close = false;
            if (HasFoil && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                ThrowFoil();
                return;
            }

            if (!GameManager.Instance.AquariumSealed && visualDay >= 3 && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                Investigation.Collect(visualDay >= 6 ? "001-name" : visualDay >= 5 ? "tank-overflow" : "tank-eye");
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;
            if(k!=null){advance=k.enterKey.wasPressedThisFrame;close=k.escapeKey.wasPressedThisFrame;}
#elif ENABLE_LEGACY_INPUT_MANAGER
            advance=Input.GetKeyDown(KeyCode.Return);
            close=Input.GetKeyDown(KeyCode.Escape);
#endif
            if (close)
            {
                CloseDialogue();
                return;
            }

            if (!advance)
                return;
            if (subtitles.Next())
                return;
            subtitles.Reset();
            page++;
            if (page >= pages.Length)
                CloseDialogue();
        }

        void CloseDialogue()
        {
            if (talking)
                blockedUntilFrame = Time.frameCount;
            talking = false;
            subtitles.Reset();
            page = 0;
            pages = null;
        }

        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(text, title, button, small);
            if (GameManager.Instance == null || GameManager.Instance.HasEnded || (!talking && !nearby))
                return;
            if (talking && (ProgressionHUD.DebugConsoleOpen || MainMenuUI.WorldBlocked || PauseMenuUI.WorldBlocked))
                return;
            Styles();
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(button);
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            float scale = Mathf.Clamp(Screen.height / 900f, .8f, 2.4f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            try
            {
                if (!talking)
                {
                    var aq = StoryText.Current.aquarium;
                    string prompt = HasFoil ? (visualDay < 3 ? StoryText.Current.aquarium.waterPrompt : StoryText.Current.aquarium.offerPrompt) : visualDay >= 7 ? (aq?.promptDay7 ?? "E  看着它") : visualDay >= 6 ? (aq?.promptDay6 ?? "E  和 001 说话") : visualDay >= 4 ? (aq?.promptDay4 ?? "E  听鱼说话") : (aq?.promptDay3 ?? "E  看看鱼缸里的鱼");
                    WorldPromptUI.Offer("aquarium", prompt, 85);
                    return;
                }

                GUI.depth = -50;
                var dialogue = NarrativeUI.DialogueRect(w, h);
                var subtitleStyle = new GUIStyle(text)
                {
                    fontSize = 23,
                    padding = new RectOffset()
                };
                subtitles.Set(pages != null ? pages[Mathf.Clamp(page, 0, pages.Length - 1)] : "", subtitleStyle, dialogue.width - 50, dialogue.height - 86);
                NarrativeUI.Dialogue(dialogue, speaker, subtitles.Current, subtitles.Counter + StoryText.Local("Enter 继续 · Esc 离开", "Enter Continue · Esc Leave"), text);
                float x = dialogue.x, y = dialogue.y - 64, bw = (dialogue.width - 24) / 3;
                if (HasFoil)
                {
                    if (GUI.Button(new Rect(x, y, bw, 48), StoryText.Current.aquarium.throwFoil, button))
                        ThrowFoil();
                    x += bw + 12;
                }

                if (!GameManager.Instance.AquariumSealed && visualDay >= 3)
                {
                    string evidence = visualDay >= 6 ? "001-name" : visualDay >= 5 ? "tank-overflow" : "tank-eye";
                    if (GUI.Button(new Rect(x, y, bw, 48), StoryText.Local(Investigation.Has(evidence) ? "已记录" : "R 记下这次观察", Investigation.Has(evidence) ? "Recorded" : "R Record observation"), button))
                        Investigation.Collect(evidence);
                    if (GUI.Button(new Rect(x + bw + 12, y, bw, 48), StoryText.Current.aquarium.question, button))
                    {
                        pages = new[]
                        {
                            Investigation.Has("notes-gap") ? StoryText.Current.aquarium.answerGap : StoryText.Current.aquarium.answerDefault
                        };
                        page = 0;
                        subtitles.Reset();
                    }
                }
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.color = color;
                GUI.depth = depth;
            }
        }

        void Styles()
        {
            if (text != null)
                return;
            var font = DeathBySeventhsDawn.Progression.GameTypography.Current;
            text = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 25,
                wordWrap = true
            };
            text.normal.textColor = C(.92f, .89, .80f);
            title = new GUIStyle(text)
            {
                fontSize = 38,
                fontStyle = FontStyle.Bold
            };
            title.normal.textColor = C(.72f, .86, .74f);
            small = new GUIStyle(text)
            {
                fontSize = 17
            };
            small.normal.textColor = C(.58f, .68, .60f);
            button = new GUIStyle(GUIStyle.none)
            {
                font = font,
                fontSize = 23,
                alignment = TextAnchor.MiddleCenter
            };
            button.normal.textColor = C(.78f, .82, .68f);
        }

        void EnsureSprites()
        {
            if (square == null)
            {
                pixel = new Texture2D(1, 1);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
                square = Sprite.Create(pixel, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            }

            if (circle == null)
            {
                circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                        float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                        circleTexture.SetPixel(x, y, C(1, 1, 1, a));
                    }

                circleTexture.Apply(false);
                circle = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
            }
        }

        void Part(string name, Vector3 localPosition, Vector2 size, Color color, int order)
        {
            PartRot(name, localPosition, size, color, order, 0);
        }

        void PartRot(string name, Vector3 localPosition, Vector2 size, Color color, int order, float angle)
        {
            var part = new GameObject(name);
            part.transform.SetParent(formRoot.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = new Vector3(size.x, size.y, 1);
            part.transform.localRotation = Quaternion.Euler(0, 0, angle);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            ApplyRenderer(renderer, order);
        }

        void Circle(string name, Vector3 localPosition, Vector2 size, Color color, int order)
        {
            var part = new GameObject(name);
            part.transform.SetParent(formRoot.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = color;
            ApplyRenderer(renderer, order);
        }

        void ApplyRenderer(SpriteRenderer renderer, int order)
        {
            if (tankRenderer != null)
            {
                renderer.sortingLayerID = tankRenderer.sortingLayerID;
                renderer.sortingOrder = tankRenderer.sortingOrder + order;
                if (tankRenderer.sharedMaterial != null)
                    renderer.sharedMaterial = tankRenderer.sharedMaterial;
            }
            else
                renderer.sortingOrder = order;
        }

        void DestroyForm()
        {
            if (formRoot != null)
                Destroy(formRoot);
            formRoot = null;
            if (tankRenderer != null && hasOriginal)
                tankRenderer.color = tankOriginal;
        }

        void RestoreTank()
        {
            if (tankRenderer != null && hasOriginal)
                tankRenderer.color = tankOriginal;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            DestroyForm();
            if (square != null)
                Destroy(square);
            if (circle != null)
                Destroy(circle);
            if (pixel != null)
                Destroy(pixel);
            if (circleTexture != null)
                Destroy(circleTexture);
        }
    }
}
