// CORE CONTRACT: 文案任务不要改动控制流程；工程变更先读 docs/CORE_CODE_MAP.md 并同步验收。
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
    // 本轮身体：第三天开口，第三夜长成人形，第四早叫醒雨果；死亡后全部重置。
    public sealed class AquariumEntity : MonoBehaviour
    {
        public const string NpcCode = "001";
        static Color C(double r, double g, double b, double a = 1) => new Color((float)r, (float)g, (float)b, (float)a);
        public static AquariumEntity Instance
        {
            get; private set;
        }
        public static bool WorldBlocked => Instance != null && (Instance.talking || Time.frameCount <= Instance.blockedUntilFrame);
        public bool IsTalking => talking;

        GameObject tank, formRoot, player;
        GameObject morningFish, travellingChair, borrowedChairSource;
        Vector2 chairStart, tankHome;
        public Vector2 ShiftedTankPosition => tankHome + Vector2.right * 1.95f;
        void MoveFurniture(float progress)
        {
            if(tank == null) return;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress));
            tank.transform.position=Vector2.Lerp(tankHome,ShiftedTankPosition,t);
            Physics2D.SyncTransforms();
        }
        public Vector2 CompanionSeatPosition => tank == null ? Vector2.zero : tankHome;
        public bool MorningWelcomePending => !StoryDebugActions.Enabled && GameManager.Instance != null && CompanionPresent && TimeManager.Instance != null && TimeManager.Instance.Days == 4 && TimeManager.Instance.Period == TimePeriod.Morning && GameManager.Instance.State.fishMorningLoop != GameManager.Instance.LoopCount;
        // MorningRoutine owns the timeline; this owner alone creates/moves the fish and borrowed furniture.
        public void ShowMorningVisitor(Vector2 position)
        {
            if (morningFish == null)
            {
                morningFish = new GameObject("Fish morning visitor");
                var r = morningFish.AddComponent<SpriteRenderer>();
                r.sprite = SmallPixelActors.FishCompanion();
                r.sortingOrder = 24;
                var reference = GameObject.FindWithTag("Player")?.GetComponentInChildren<SpriteRenderer>();
                if (reference != null)
                {
                    r.sharedMaterial = reference.sharedMaterial;
                    r.sortingLayerID = reference.sortingLayerID;
                }
            }
            morningFish.transform.position = position;
        }
        GameObject DiningChair()
        {
            if (borrowedChairSource == null || borrowedChairSource.scene != SceneManager.GetActiveScene())
                borrowedChairSource = GameObject.Find("Dining chair right");
            return borrowedChairSource;
        }
        GameObject CopyDiningChair(Transform parent, string name)
        {
            var source = DiningChair();
            if (source == null)
                return null;
            var copy = Instantiate(source, parent);
            copy.name = name;
            copy.SetActive(true);
            copy.transform.localScale = source.transform.lossyScale;
            foreach (var collider in copy.GetComponentsInChildren<Collider2D>())
            {
                collider.enabled = false;
                Destroy(collider);
            }
            foreach (var r in copy.GetComponentsInChildren<SpriteRenderer>())
                ApplyRenderer(r, 10);
            source.SetActive(false);
            return copy;
        }
        public void BeginBorrowingStool()
        {
            DestroyForm();
            var source = DiningChair();
            if (source == null)
                return;
            chairStart = source.transform.position;
            travellingChair = CopyDiningChair(null, "Stool being borrowed");
            source.SetActive(true);
            if(travellingChair != null) travellingChair.SetActive(false);
            AnimateBorrowedStool(0);
        }
        public void AnimateBorrowedStool(float progress)
        {
            if (travellingChair == null)
                return;
            MoveFurniture(progress / .36f);
            if(progress < .36f)
            {
                ShowMorningVisitor((Vector2)tank.transform.position + new Vector2(-1.35f,-.55f));
                return;
            }
            if(borrowedChairSource != null) borrowedChairSource.SetActive(false);
            travellingChair.SetActive(true);
            var end = CompanionSeatPosition;
            float t = Mathf.Clamp01((progress - .36f) / .64f) * 3;
            Vector2 a = chairStart, b = new Vector2(chairStart.x, end.y - .72f), c = new Vector2(end.x, end.y - .72f);
            Vector2 at = t < 1 ? Vector2.Lerp(a, b, t) : t < 2 ? Vector2.Lerp(b, c, t - 1) : Vector2.Lerp(c, end, t - 2);
            travellingChair.transform.position = at;
            ShowMorningVisitor(at + new Vector2(.39f, .04f + Mathf.Sin(progress * 38) * .035f));
        }
        public void SettleMorningVisitor(float progress)
        {
            AnimateBorrowedStool(1);
            ShowMorningVisitor(CompanionSeatPosition + Vector2.right * Mathf.Lerp(.39f, 0, Mathf.SmoothStep(0, 1, progress)));
        }
        public void FinishMorningWelcome()
        {
            var state = GameManager.Instance.State;
            state.fishMorningLoop = GameManager.Instance.LoopCount;
            state.fishStoolBorrowed = true;
            ClearMorningVisuals(false);
            RefreshForm();
            ProgressionSave.Write(state);
        }
        void ClearMorningVisuals(bool restoreUnfinishedChair)
        {
            if (morningFish != null)
                Destroy(morningFish);
            morningFish = null;
            if (travellingChair != null)
                Destroy(travellingChair);
            travellingChair = null;
            if (restoreUnfinishedChair && borrowedChairSource != null && GameManager.Instance != null && !GameManager.Instance.State.fishStoolBorrowed)
                borrowedChairSource.SetActive(true);
            if(restoreUnfinishedChair && tank != null && GameManager.Instance != null && !GameManager.Instance.State.fishStoolBorrowed) MoveFurniture(0);
        }

        SpriteRenderer tankRenderer;
        Color tankOriginal;
        bool hasOriginal;
        int visualDay = -1, page, openedFrame;
        int blockedUntilFrame = -1;
        bool talking, nearby, choosing, choiceFinished;
        public bool ChoicesVisible => talking && choosing;
        public bool CompanionReady => GameManager.Instance.State.fishAlly;
        public bool CompanionPresent => TimeManager.Instance != null && TimeManager.Instance.Days >= 4 && !GameManager.Instance.AquariumSealed;
        public bool IsAmbientConversation => talking && ambientConversation;
        public string CurrentConversation => pages == null ? "" : string.Join("\n", pages);
        bool ambientConversation, storyConversation;
        string activeEvidence;
        readonly System.Collections.Generic.List<int> chatBag = new System.Collections.Generic.List<int>();
        int lastChat = -1; bool lastSilent;

        string PickChat(bool silent)
        {
            var pool = silent ? StoryText.Current.expansion.fishSilentChats : StoryText.Current.expansion.fishChats;
            if (pool == null || pool.Length == 0)
                return StoryText.Local("……她在听。", "…She is listening.");
            if (silent != lastSilent)
            {
                chatBag.Clear();
                lastChat = -1;
                lastSilent = silent;
            }
            if (chatBag.Count == 0)
                for (int i = 0; i < pool.Length; i++)
                    chatBag.Add(i);
            int slot = UnityEngine.Random.Range(0, chatBag.Count);
            if (chatBag.Count > 1 && chatBag[slot] == lastChat)
                slot = (slot + 1) % chatBag.Count;
            lastChat = chatBag[slot];
            chatBag.RemoveAt(slot);
            return pool[lastChat];
        }
        void CompleteDailyStory()
        {
            if (!storyConversation)
                return;
            var state = GameManager.Instance.State;
            if (!state.fishStoryDays.Contains(visualDay))
                state.fishStoryDays.Add(visualDay);
            ProgressionSave.Write(state);
            storyConversation = false;
        }
        public bool TryTalk()
        {
            if (tank == null || GameManager.Instance.HasEnded || GameManager.Instance.AquariumSealed ||
                TutorialGate.IsBlocked("aquarium") || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy || !NearInteraction())
                return false;
            OpenDialogue();
            return talking;
        }

        int CurrentTopic => (GameManager.Instance.State.fishUnderstanding & 1) == 0 ? 0 : 1;

        public bool ChooseUnderstanding(bool explainTrueNeed)
        {
            if (!talking || !choosing || visualDay < 3 || (CurrentTopic == 1 && visualDay < 4))
                return false;
            int topic = CurrentTopic;
            var state = GameManager.Instance.State;
            if (explainTrueNeed)
                state.fishUnderstanding |= 1 << topic;
            state.fishAlly = (state.fishUnderstanding & 3) == 3;
            if (state.fishAlly && !state.fishCompanionUnlocked)
            {
                state.fishCompanionUnlocked = true;
                state.fishCompanionUnlockLoop = GameManager.Instance.LoopCount;
            }
            CompleteDailyStory();
            ProgressionSave.Write(state);
            pages = new[]
            {
                explainTrueNeed ? StoryText.Current.expansion.fishGoodReplies[topic] : StoryText.Current.expansion.fishBadReplies[topic]
            };
            page = 0;
            choosing = false;
            choiceFinished = true;
            subtitles.Reset();
            return true;
        }

        float range = .72f;
        readonly System.Collections.Generic.Dictionary<SpriteRenderer, Color> formTints = new System.Collections.Generic.Dictionary<SpriteRenderer, Color>();
        Sprite square, circle;
        Sprite[] metamorphosis;
        Texture2D pixel, circleTexture, portrait;
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
            portrait = Resources.Load<Texture2D>("Story/FishDialogueInk");
            metamorphosis = SmallPixelActors.Fish();
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
            ClearMorningVisuals(true);
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
            ClearMorningVisuals(true);
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

            tankHome = tank.transform.position;
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
            ClearMorningVisuals(true);
            if (borrowedChairSource != null)
                borrowedChairSource.SetActive(true);
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
            if (CompanionPresent && !GameManager.Instance.AquariumSealed)
            {
                if (MorningWelcomePending)
                    return;
                MoveFurniture(1);
                BuildSeatedCompanion();
                return;
            }
            if (RadiationRules.Fish(day, GameManager.Instance.AquariumSealed) == FishStage.Ordinary)
                return;
            EnsureSprites();
            formRoot = new GameObject("Aquarium entity form");
            formRoot.transform.SetParent(tank.transform, false);
            formRoot.transform.localPosition = Vector3.zero;
            if (metamorphosis != null)
            {
                var b = tankRenderer.bounds;
                formRoot.transform.localScale = new Vector3(1 / tank.transform.lossyScale.x, 1 / tank.transform.lossyScale.y, 1);
                bool human = day >= 7 && !CompanionReady;
                int index = human ? 3 : day >= 5 ? 2 : day == 4 ? 1 : 0;
                formRoot.transform.position = human ? new Vector3(b.max.x + .45f, b.min.y - .10f, 0) : new Vector3(b.center.x, b.center.y + b.size.y * .19f, 0);
                var image = new GameObject(human ? "Hugo double" : "Fish flank human face");
                image.transform.SetParent(formRoot.transform, false);
                var r = image.AddComponent<SpriteRenderer>();
                r.sprite = metamorphosis[index];
                float size = human ? .82f : day >= 5 ? .58f : .48f;
                float scale = size / (human ? r.sprite.bounds.size.y : r.sprite.bounds.size.x);
                image.transform.localScale = Vector3.one * scale;
                ApplyRenderer(r, 12);
            }
            else if (day >= 7)
                BuildImpostor();
            else if (day >= 5)
                BuildHalfBody();
            else
                BuildFishFace(day >= 4);
        }

        void BuildSeatedCompanion()
        {
            var b = tankRenderer.bounds;
            formRoot = new GameObject("Aquarium entity form");
            formRoot.transform.SetParent(tank.transform, false);
            formRoot.transform.localScale = new Vector3(1 / tank.transform.lossyScale.x, 1 / tank.transform.lossyScale.y, 1);
            formRoot.transform.position = CompanionSeatPosition;
            EnsureSprites();
            var solid = formRoot.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(.42f, .20f);
            solid.offset = new Vector2(0, -.18f);
            var chair = CopyDiningChair(formRoot.transform, "Fish companion stool seat");
            if (chair != null)
                chair.transform.position = CompanionSeatPosition;
            else
            {
                Part("Fish companion stool seat", new Vector3(0, -.04f), new Vector2(.52f, .14f), C(.40, .29, .23), 10);
                Part("Fish companion stool left leg", new Vector3(-.17f, -.24f), new Vector2(.08f, .32f), C(.24, .20, .19), 10);
                Part("Fish companion stool right leg", new Vector3(.17f, -.24f), new Vector2(.08f, .32f), C(.24, .20, .19), 10);
            }
            var actor = new GameObject("Seated fish companion");
            actor.transform.SetParent(formRoot.transform, false);
            var r = actor.AddComponent<SpriteRenderer>();
            r.sprite = SmallPixelActors.FishCompanion();
            ApplyRenderer(r, 12);
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

        void LateUpdate()
        {
            if (tankRenderer == null || TimeManager.Instance == null)
                return;
            var ambient = InteriorLighting.LitTint(TimeAtmosphere.Palette(TimeManager.Instance.Period));
            tankRenderer.color = tankOriginal * ambient;
            if (formRoot != null && metamorphosis != null)
                foreach (var r in formRoot.GetComponentsInChildren<SpriteRenderer>())
                    r.color = (formTints.TryGetValue(r, out var authored) ? authored : Color.white) * ambient;
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
            if (tank == null || TimeManager.Instance == null || (TimeManager.Instance.Days < 1))
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
            nearby = player != null && NearInteraction();
            if (nearby && InteractPressed() && WorldPromptUI.CanActivate("aquarium"))
            {
                TryTalk();
            }
        }

        public bool NearInteraction() => player != null && tank != null && (CompanionPresent ?
            Vector2.Distance(player.transform.position, CompanionSeatPosition) < .86f :
            (PropReach.Gap(tank, player) < .24f || (formRoot != null && (visualDay >= 7 || CompanionPresent) && Vector2.Distance(player.transform.position, InteractionPoint()) < .82f)));
        Vector2 InteractionPoint()
        {
            if (tank == null)
                return Vector2.zero;
            if (CompanionPresent && formRoot != null)
                return formRoot.transform.position;
            if (visualDay >= 7 && !CompanionReady && tankRenderer != null)
                return new Vector2(tankRenderer.bounds.max.x + .45f, tankRenderer.bounds.min.y - .1f);
            return tank.transform.position;
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

        public bool HasFoil => GameManager.Instance != null && !CompanionPresent && GameManager.Instance.State.inventory.Contains(CultNeighbor.Token);
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
            if (game == null || game.HasEnded || game.AquariumSealed || TutorialGate.IsBlocked("aquarium") || !HasFoil || tank == null || player == null || !NearInteraction())
                return false;
            game.State.inventory.Remove(CultNeighbor.Token);
            storyConversation = ambientConversation = choosing = false;
            choiceFinished = true;
            activeEvidence = null;
            if (!RadiationRules.CanSeal(TimeManager.Instance.Days))
            {
                ProgressionSave.Write(game.State);
                pages = new[]
                {
                    StoryText.Local("锡纸沉到了缸底。它看了一眼，继续用那张嘴呼吸。\n……已经太迟了。", "The foil sinks. It glances down, then keeps breathing through that mouth.\n…Too late.")
                };
                speaker = StoryText.Local("雨果", "Hugo");
                page = 0;
                openedFrame = Time.frameCount;
                talking = true;
                subtitles.Reset();
                return true;
            }

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
            choosing = false;
            choiceFinished = false;
            subtitles.Reset();
            var state = GameManager.Instance.State;
            state.fishStoryDays = state.fishStoryDays ?? new System.Collections.Generic.List<int>();
            ambientConversation = state.fishStoryDays.Contains(visualDay) && !(CompanionReady && !state.fishReportAvailable && (visualDay >= 5 || CompanionPresent));
            storyConversation = !ambientConversation;
            activeEvidence = null;
            var aq = StoryText.Current.aquarium;
            pages = visualDay < 3 ? new[]
            {
                StoryText.Local("……今天又没回音。投出去那么多份简历，连封拒信都没有。\n你倒是好，饿了还有人喂。", "…Still nothing. All those applications. Not even a rejection.\nAt least someone feeds you.")
            }

            : visualDay >= 5 ? aq?.day6Pages : visualDay >= 4 ? aq?.day5Pages : aq?.day4Pages;
            if (!ambientConversation && CompanionReady && (visualDay >= 5 || CompanionPresent))
            {
                pages = new[]
                {
                    StoryText.Current.expansion.fishAllyPages[visualDay >= 6 ? 1 : 0]
                };
                GameManager.Instance.State.fishReportAvailable = true;
                ProgressionSave.Write(GameManager.Instance.State);
            }

            if (ambientConversation)
            {
                pages = new[] { PickChat(visualDay < 3 && !CompanionPresent) };
                choiceFinished = true;
            }
            else if (visualDay >= 3 || CompanionPresent)
                activeEvidence = CompanionReady && state.fishReportAvailable ? "fish-help-promise" : visualDay >= 6 ? "001-name" : visualDay >= 5 ? "tank-overflow" : "tank-eye";
            if (pages == null || pages.Length == 0)
                return;
            speaker = CompanionReady ? StoryText.Local("小鱼", "Fish") : visualDay >= 7 ? (aq?.speakerDay7 ?? "长得像雨果的东西") : visualDay >= 6 ? (aq?.speakerDay6 ?? NpcCode) : visualDay >= 4 ? (aq?.speakerDay4 ?? "鱼") : (aq?.speakerDay3 ?? "鱼缸里的鱼");
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

            if (!string.IsNullOrEmpty(activeEvidence) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Investigation.Collect(activeEvidence);
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

            if (advance)
                AdvanceDialogue();
        }

        public void AdvanceDialogue()
        {
            if (!talking || choosing || pages == null)
                return;
            if (subtitles.Next())
                return;
            subtitles.Reset();
            page++;
            if (page >= pages.Length)
            {
                if (storyConversation && !choiceFinished && !CompanionReady && visualDay >= 3 && (CurrentTopic == 0 || visualDay >= 4))
                {
                    page = pages.Length - 1;
                    choosing = true;
                }
                else
                {
                    CompleteDailyStory();
                    CloseDialogue();
                }
            }
        }

        void CloseDialogue()
        {
            if (talking)
                blockedUntilFrame = Time.frameCount;
            talking = false;
            ambientConversation = storyConversation = false;
            activeEvidence = null;
            choosing = false;
            choiceFinished = false;
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
                    string prompt = CompanionPresent ? StoryText.Local("E  和小鱼说话", "E  Talk to Fish") : HasFoil ? (visualDay < 3 ? StoryText.Current.aquarium.waterPrompt : StoryText.Current.aquarium.offerPrompt) : visualDay >= 7 ? (aq?.promptDay7 ?? "E  看着它") : visualDay >= 6 ? (aq?.promptDay6 ?? "E  和 001 说话") : visualDay >= 4 ? (aq?.promptDay4 ?? "E  听鱼说话") : (aq?.promptDay3 ?? "E  看看鱼缸里的鱼");
                    WorldPromptUI.Offer("aquarium", prompt, 85);
                    return;
                }

                GUI.depth = -50;
                var dialogue = NarrativeUI.DialogueRect(w, h);
                if (portrait != null && (visualDay >= 3 || CompanionPresent))
                {
                    var art = new Rect(w * .73f, h * .29f, w * .16f, h * .26f);
                    NarrativeUI.Panel(art);
                    var uv = new Rect(0, 0, 1, 1);
                    float aspect = portrait.width * uv.width / (portrait.height * uv.height);
                    float aw = art.width - 36, ah = art.height - 28;
                    if (aw / ah > aspect)
                        aw = ah * aspect;
                    else
                        ah = aw / aspect;
                    GUI.color = Color.white;
                    GUI.DrawTextureWithTexCoords(new Rect(art.center.x - aw / 2, art.center.y - ah / 2, aw, ah), portrait, uv);
                }

                var subtitleStyle = new GUIStyle(text)
                {
                    fontSize = 23,
                    padding = new RectOffset()
                };
                subtitles.Set(pages != null ? pages[Mathf.Clamp(page, 0, pages.Length - 1)] : "", subtitleStyle, dialogue.width - 50, dialogue.height - 86);
                NarrativeUI.Dialogue(dialogue, speaker, subtitles.Current, subtitles.Counter + StoryText.Local("Enter 继续 · Esc 离开", "Enter Continue · Esc Leave"), text);
                if (choosing)
                {
                    var choice = StoryText.Current.expansion;
                    int topic = CurrentTopic;
                    float cw = (dialogue.width - 12) / 2;
                    GUI.Label(new Rect(dialogue.x, dialogue.y - 112, dialogue.width, 30), choice.fishChoicePrompt, text);
                    if (PrototypeButtons.Button(new Rect(dialogue.x, dialogue.y - 76, cw, 64), choice.fishGoodChoices[topic], button))
                        ChooseUnderstanding(true);
                    if (PrototypeButtons.Button(new Rect(dialogue.x + cw + 12, dialogue.y - 76, cw, 64), choice.fishBadChoices[topic], button))
                        ChooseUnderstanding(false);
                    return;
                }

                float x = dialogue.x, y = dialogue.y - 64, bw = (dialogue.width - 24) / 3;
                if (HasFoil)
                {
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.Button(new Rect(x, y, bw, 48), StoryText.Local(RadiationRules.CanSeal(TimeManager.Instance.Days) ? "F  把锡纸放进水里" : "F  投入锡纸（已无法逆转）", RadiationRules.CanSeal(TimeManager.Instance.Days) ? "F  Place foil in the water" : "F  Drop foil (cannot reverse it)"), button))
                        ThrowFoil();
                    x += bw + 12;
                }

                if (!string.IsNullOrEmpty(activeEvidence))
                {
                    string evidence = activeEvidence;
                    if (DeathBySeventhsDawn.Progression.PrototypeButtons.Button(new Rect(x, y, bw, 48), StoryText.Local(Investigation.Has(evidence) ? "已记录" : "R 记下这次观察", Investigation.Has(evidence) ? "Recorded" : "R Record observation"), button))
                        Investigation.Collect(evidence);
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
            formTints[renderer] = renderer.color;
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
            formTints.Clear();
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
            if(metamorphosis!=null)SmallPixelActors.ReleaseFish();
            metamorphosis=null;
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
