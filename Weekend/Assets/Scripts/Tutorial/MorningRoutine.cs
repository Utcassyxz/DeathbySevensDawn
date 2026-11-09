// 职责：第一天的动作编排：醒来、邻居、冰箱、休息、写日记与调查教学。
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Knowledge;

namespace DeathBySeventhsDawn.Interaction
{
    // 1 wake, 2 neighbour, 3 kitchen, 4 bulletin, 5 sofa, 6 desk, 7 telescope, 8 record, 9 connect, 10 free exploration.
    [DefaultExecutionOrder(-90)]
    public sealed class MorningRoutine : MonoBehaviour
    {
        public static MorningRoutine Instance
        {
            get; private set;
        }

        enum Activity
        {
            None,
            Wake,
            Fridge,
            Meal,
            Nap,
            Write,
            ReturnHome,
            FishWake
        }

        Activity activity;
        int fishMorningPage;
        bool carryingBreakfastScene;
        Vector2 visitorPoint;
        Transform wakingBed;
        Vector3 wakingBedPosition;
        public bool FishMorningPlaying => activity == Activity.FishWake;
        public int FishMorningPage => fishMorningPage;
        public static bool WorldBlocked => Instance != null && (Instance.activity != Activity.None || Time.frameCount <= Instance.blockedFrame);
        public bool IsWaking => activity == Activity.Wake;
        public bool IsWriting => activity == Activity.Write;
        public bool FridgeAnimating => activity == Activity.Fridge;
        public Vector2 Bedside
        {
            get; private set;
        }

        bool queued, simulated, rendererEnabled, firstFridge, fridgeVisibleOpen;
        string mealMessage;
        bool napTimeChanged;
        bool advanceRequested;
        public void AdvanceStory()
        {
            advanceRequested = true;
        }

        float age;
        int hit, blockedFrame = -1;
        GameObject player, fridge, sofa, placeholder;
        Rigidbody2D body;
        SpriteRenderer original, fridgeRenderer;
        Sprite closedFridge, openedFridge, square;
        Texture2D pixel;
        Vector2 bedPoint, standPoint;
        GUIStyle label;
        ProgressionState State => GameManager.Instance.State;
        public int Stage => State.morningStage;

        void Awake()
        {
            Instance = this;
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            square = Sprite.Create(pixel, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            openedFridge = Resources.Load<Sprite>("Tutorial/FridgeOpen");
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
            RestorePlayer();
        }

        void Bind()
        {
            player = GameObject.FindWithTag("Player");
            fridge = GameObject.Find("Refrigerator");
            sofa = GameObject.Find("Old sofa");
            fridgeRenderer = fridge != null ? fridge.GetComponent<SpriteRenderer>() : null;
            if (fridgeRenderer != null && fridgeRenderer.sprite != openedFridge)
                closedFridge = fridgeRenderer.sprite;
            fridgeVisibleOpen = false;
            if (State.fridgeDoorOpen)
                SetFridge(true);
        }

        void Loaded(Scene scene, LoadSceneMode mode)
        {
            RestorePlayer();
            activity = Activity.None;
            Bind();
            if (carryingBreakfastScene && scene.name == "LivingRoom")
            {
                carryingBreakfastScene = false;
                FreezePlayer();
                standPoint = player.transform.position;
                placeholder.transform.position = standPoint;
                activity = Activity.FishWake;
                age = 0;
                AquariumEntity.Instance.BeginBorrowingStool();
                return;
            }
            if (queued && scene.name == "Bedroom")
                BeginWake();
            if (Stage == 4 && !State.dayOneSupernovaSeen && scene.name == "LivingRoom")
                SetFridge(true);
        }

        void Start()
        {
            Bind();
            if (Stage == 1)
            {
                queued = true;
                if (SceneManager.GetActiveScene().name != "Bedroom")
                    SceneManager.LoadScene("Bedroom");
            }
            else if (Stage == 4 && !State.dayOneSupernovaSeen)
            {
                if (SceneManager.GetActiveScene().name != "LivingRoom")
                    SceneManager.LoadScene("LivingRoom");
            }

            if (Stage >= 6 && Stage < 10)
            {
                RestoreEvening();
                if (Stage >= 7 && TimeManager.Instance.Period == TimePeriod.Evening)
                    TimeManager.Instance.AdvanceTime();
            }
        }

        public void DebugSkip()
        {
            if (FishMorningPlaying)
                AquariumEntity.Instance?.FinishMorningWelcome();
            Reset();
            State.morningStage = 10;
            State.dreamSeen = State.tutorialComplete = State.journalUnlocked = State.investigationUnlocked = true;
            Save();
        }

        void Reset()
        {
            queued = false;
            carryingBreakfastScene = false;
            RestorePlayer();
            activity = Activity.None;
            SetFridge(false);
            blockedFrame = Time.frameCount;
        }

        void Save() => ProgressionSave.Write(State);
        public void QueueWake()
        {
            queued = true;
            State.morningStage = 1;
            Save();
        }

        void FreezePlayer()
        {
            if (player == null)
                player = GameObject.FindWithTag("Player");
            body = player.GetComponent<Rigidbody2D>();
            original = player.GetComponentInChildren<SpriteRenderer>();
            simulated = body != null && body.simulated;
            rendererEnabled = original != null && original.enabled;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            if (original != null)
                original.enabled = false;
            placeholder = new GameObject("Tutorial posture");
            var sr = placeholder.AddComponent<SpriteRenderer>();
            sr.sprite = original != null ? original.sprite : square;
            sr.color = original != null ? original.color : new Color(.48f, .65f, .58f);
            sr.sortingOrder = 20;
            placeholder.transform.localScale = original != null ? original.transform.lossyScale : Vector3.one;
        }

        void MoveBody(Vector2 p)
        {
            player.transform.position = p;
            if (body != null)
                body.position = p;
        }

        void RestorePlayer()
        {
            if (wakingBed != null)
                wakingBed.position = wakingBedPosition;
            wakingBed = null;
            if (body != null)
                body.simulated = simulated;
            if (original != null)
                original.enabled = rendererEnabled;
            if (placeholder != null)
                Destroy(placeholder);
            placeholder = null;
        }

        void BeginWake()
        {
            queued = false;
            var bed = GameObject.Find("Bed");
            if (bed == null || player == null)
                return;
            var r = bed.GetComponent<SpriteRenderer>();
            var b = r.bounds;
            bedPoint = b.center;
            Bedside = new Vector2(b.max.x + .48f, b.min.y - .25f);
            FreezePlayer();
            MoveBody(bedPoint);
            activity = Activity.Wake;
            age = 0;
            hit = 0;
        }

        void BeginFishWake()
        {
            var bed = GameObject.Find("Bed");
            if (player == null) return;
            queued = false;
            bool inBed=bed!=null && TimeManager.Instance != null && GameManager.Instance.State.sleeplessDays==0;
            if(inBed)
            {
                var bounds=bed.GetComponent<SpriteRenderer>().bounds;
                bedPoint=bounds.center;
                Bedside=new Vector2(bounds.max.x+.48f,bounds.min.y-.25f);
                visitorPoint=new Vector2(bounds.max.x+.65f,bounds.center.y-.12f);
                wakingBed=bed.transform;wakingBedPosition=wakingBed.position;
            }
            else
            {
                bedPoint=Bedside=player.transform.position;
                visitorPoint=bedPoint+Vector2.right*.65f;
                wakingBed=null;
            }
            FreezePlayer();
            var pose=placeholder.GetComponent<SpriteRenderer>();pose.sprite=SmallPixelActors.Seated();pose.color=Color.white;placeholder.transform.localScale=Vector3.one;
            MoveBody(bedPoint);placeholder.transform.position=bedPoint;
            placeholder.transform.localRotation=Quaternion.Euler(0,0,inBed?75:0);
            activity = Activity.FishWake;
            fishMorningPage = 0;
            age = 0;
            AquariumEntity.Instance.ShowMorningVisitor(visitorPoint);
        }
        void TickFishWake(bool advance)
        {
            var fish = AquariumEntity.Instance;
            if (fishMorningPage < 4)
            {
                float rise = fishMorningPage < 3 ? 0 : Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / 1.1f));
                Vector2 at = Vector2.Lerp(bedPoint, Bedside, rise);
                MoveBody(at);
                placeholder.transform.position = at;
                placeholder.transform.localRotation = Quaternion.Euler(0, 0, wakingBed!=null?Mathf.Lerp(75, 0, rise):0);
                float shake = fishMorningPage == 0 && age < 1.5f ? Mathf.Sin(age * 28) * .035f : 0;
                if (wakingBed != null)
                    wakingBed.position = wakingBedPosition + Vector3.right * shake;
                fish.ShowMorningVisitor(visitorPoint + Vector2.up * (Mathf.Sin(age * 8) * .035f));
            }
            else if (fishMorningPage == 4)
                fish.AnimateBorrowedStool(age / 6f);
            else if (fishMorningPage == 5)
                fish.SettleMorningVisitor(Mathf.Clamp01(age / .9f));
            float minimum = fishMorningPage == 3 ? 1.1f : fishMorningPage == 4 ? 6f : fishMorningPage == 5 ? .9f : .35f;
            if (!advance || age < minimum)
                return;
            if (fishMorningPage == 5)
                fish.FinishMorningWelcome();
            fishMorningPage++;
            age = 0;
            if (fishMorningPage == 4)
            {
                carryingBreakfastScene = true;
                SceneManager.LoadScene("LivingRoom");
                return;
            }
            if (fishMorningPage >= StoryText.Current.morning.fishMorningPages.Length)
            {
                RestorePlayer();
                activity = Activity.None;
                blockedFrame = Time.frameCount;
                Save();
            }
        }

        public void NeighborTalked()
        {
            if (Stage == 2)
            {
                State.morningStage = 3;
                activity = Activity.ReturnHome;
                age = 0;
                Save();
            }
        }

        float Distance(GameObject target) => PropReach.Gap(target, player);
        public bool CanOpenFridge => fridge != null && !TutorialGate.IsBlocked("fridge") && Distance(fridge) < .16f;
        public bool CanNap => Stage == 5 && sofa != null && Distance(sofa) < .18f;

        public bool CanWrite
        {
            get
            {
                var chair = GameObject.Find("Desk chair");
                return Stage == 6 && SceneManager.GetActiveScene().name == "Bedroom" && player != null && chair != null && PropReach.Gap(GameObject.Find("Writing desk"), player) < .25f;
            }
        }

        void SetFridge(bool open)
        {
            fridgeVisibleOpen = open;
            if (fridgeRenderer != null)
                fridgeRenderer.sprite = open && openedFridge != null ? openedFridge : closedFridge;
        }

        public bool OpenFridge()
        {
            if (!CanOpenFridge || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                return false;
            firstFridge = !State.fridgeOpened && !State.dayOneNewsStarted && !State.dayOneSupernovaSeen && (TimeManager.Instance.Days == 1||!CrisisRules.LegacyValidation);
            State.fridgeOpened = true;
            if (firstFridge)
                State.morningStage = 4;
            Save();
            SetFridge(true);
            State.fridgeDoorOpen = fridgeVisibleOpen;
            Save();
            activity = Activity.Fridge;
            age = 0;
            return true;
        }

        public void NewsFinished()
        {
            if (Stage == 4)
                State.morningStage = 5;
            CompleteMeal();
        }

        // News owns the screen until it ends. Then close the actual sprite and narrate breakfast once.
        void CompleteMeal()
        {
            SetFridge(false);
            State.fridgeDoorOpen = false;
            mealMessage = State.breadEaten ? StoryText.Current.morning.fridgeEmpty : StoryText.Current.morning.breadMeal;
            State.breadEaten = true;
            Save();
            if (Stage == 5 && TimeManager.Instance.Days == 1 && TimeManager.Instance.Period == TimePeriod.Morning)
                TimeManager.Instance.AdvanceTime();
            activity = Activity.Meal;
            age = 0;
        }

        public bool MealPresenting => activity == Activity.Meal;

        public bool BeginNap()
        {
            if (!CanNap || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                return false;
            FreezePlayer();
            var b = sofa.GetComponent<SpriteRenderer>().bounds;
            bedPoint = b.center;
            standPoint = new Vector2(b.center.x, b.min.y - .45f);
            MoveBody(bedPoint);
            placeholder.transform.position = bedPoint;
            placeholder.transform.localRotation = Quaternion.Euler(0, 0, 75);
            activity = Activity.Nap;
            napTimeChanged = false;
            Object.FindAnyObjectByType<TimeAtmosphere>()?.BeginDuskNap();
            age = 0;
            return true;
        }

        void RestoreEvening()
        {
            var t = TimeManager.Instance;
            for (int i = 0; i < 2 && t.Days == 1 && t.Period < TimePeriod.Evening && !GameManager.Instance.HasEnded; i++)
                t.AdvanceTime();
        }

        public bool BeginWriting()
        {
            if (!CanWrite || StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                return false;
            FreezePlayer();
            standPoint = player.transform.position;
            var chair = GameObject.Find("Desk chair");
            if (chair == null)
            {
                RestorePlayer();
                return false;
            }

            var seat = chair.GetComponent<SpriteRenderer>().bounds;
            Vector2 seated = new Vector2(seat.center.x, seat.center.y + seat.size.y * .18f);
            MoveBody(seated);
            placeholder.transform.position = seated;
            placeholder.GetComponent<SpriteRenderer>().sprite = SmallPixelActors.Seated();
            placeholder.GetComponent<SpriteRenderer>().color = Color.white;
            placeholder.transform.localScale = Vector3.one;
            placeholder.transform.localRotation = Quaternion.identity;
            placeholder.GetComponent<SpriteRenderer>().sortingOrder = chair.GetComponent<SpriteRenderer>().sortingOrder + 1;
            activity = Activity.Write;
            age = 0;
            return true;
        }

        void Seed(string id, bool evidence)
        {
            if (State.journal.Exists(e => e.id == id))
                return;
            var def = Investigation.Evidence(id);
            State.journal.Add(new JournalEntry { id = id, title = def != null ? def.title : StoryText.Current.morning.journalTitle, body = def != null ? def.body : StoryText.Current.morning.journalBody, day = 1, loop = GameManager.Instance.LoopCount, isKnowledge = evidence });
            if (evidence && !State.knowledgeIds.Contains(id))
                State.knowledgeIds.Add(id);
        }

        void FinishWriting()
        {
            State.journalUnlocked = true;
            Seed("first-evening-journal", false);
            Seed("my-observation-notes", true);
            if (State.cultDayOneTalked)
                Seed("401-warning", true);
            if (State.dayOneSupernovaSeen)
                Seed("skt079-broadcast", true);
            foreach (var id in State.initialEvidenceIds)
                Seed(id, true);
            State.initialEvidenceIds.Clear();
            State.morningStage = 7;
            // Writing takes the last light of dusk; the first telescope lesson happens after dark.
            if (TimeManager.Instance.Days == 1 && TimeManager.Instance.Period == TimePeriod.Evening)
                TimeManager.Instance.AdvanceTime();
            Save();
            Object.FindAnyObjectByType<StoryJournal>()?.ShowJournal();
        }

        public void TelescopeObserved()
        {
            if (Stage == 7)
            {
                State.investigationUnlocked = true;
                State.morningStage = 8;
                Save();
            }
        }

        public void TelescopeRecorded()
        {
            if (Stage == 8)
            {
                State.morningStage = 9;
                Save();
            }
        }

        void Update()
        {
            if (ProgressionHUD.DebugConsoleOpen)
                return;
            if (GameManager.Instance == null || GameManager.Instance.HasEnded || MainMenuUI.IsVisible || GameSettingsUI.WorldBlocked)
                return;
            age += Time.unscaledDeltaTime;
            bool advance = StoryControls.ContinuePressed || advanceRequested;
            advanceRequested = false;
            if (!StoryDebugActions.Enabled && activity == Activity.None && AquariumEntity.Instance != null && AquariumEntity.Instance.MorningWelcomePending && !StoryJournal.WorldBlocked)
            {
                BeginFishWake();
            }
            if (activity == Activity.FishWake)
            {
                TickFishWake(advance);
                return;
            }
            if (activity == Activity.ReturnHome)
            {
                if (age > .35f && advance)
                {
                    activity = Activity.None;
                    blockedFrame = Time.frameCount;
                }

                return;
            }

            if (Stage == 9 && Investigation.Has("deduction-growing-shadow"))
            {
                State.morningStage = 10;
                State.tutorialComplete = true;
                Save();
            }

            if (queued && SceneManager.GetActiveScene().name == "Bedroom")
                BeginWake();
            if (activity == Activity.Wake)
            {
                if (age > .25f + hit * .85f && hit < 3)
                {
                    CultNeighbor.Instance?.Hammer(.35f + hit * .28f);
                    hit++;
                }

                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - 1.8f) / 1.5f));
                var p = Vector2.Lerp(bedPoint, Bedside, t);
                MoveBody(p);
                placeholder.transform.position = p;
                placeholder.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(80, 0, t));
                if (age >= 3.5f && advance)
                {
                    MoveBody(Bedside);
                    RestorePlayer();
                    activity = Activity.None;
                    blockedFrame = Time.frameCount;
                    State.morningStage = 2;
                    Save();
                }

                return;
            }

            if (activity == Activity.Fridge)
            {
                if (age >= .75f && advance)
                {
                    activity = Activity.None;
                    blockedFrame = Time.frameCount;
                    if (firstFridge)
                    {
                        firstFridge = false;
                        if (!DayOneSupernovaEvent.Instance.PlayFromFridge())
                            NewsFinished();
                    }
                    else
                    {
                        SetFridge(false);
                        State.fridgeDoorOpen = false;
                        Save();
                    }
                }

                return;
            }

            if (activity == Activity.Meal)
            {
                if (age >= .7f && advance)
                {
                    activity = Activity.None;
                    blockedFrame = Time.frameCount;
                }

                return;
            }

            if (activity == Activity.Nap || activity == Activity.Write)
            {
                bool nap = activity == Activity.Nap;
                if (nap && age > 4.4f && placeholder != null)
                {
                    float wake = Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - 4.4f) / .9f));
                    placeholder.transform.position = Vector2.Lerp(bedPoint, standPoint, wake);
                    placeholder.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(75, 0, wake));
                }

                if (nap && age >= 1.3f && !napTimeChanged)
                {
                    napTimeChanged = true;
                    RestoreEvening();
                }

                if (age >= (nap ? 5.3f : 2.0f) && advance)
                {
                    MoveBody(standPoint);
                    RestorePlayer();
                    activity = Activity.None;
                    blockedFrame = Time.frameCount;
                    if (nap)
                    {
                        RestoreEvening();
                        State.morningStage = 6;
                        Save();
                    }
                    else
                        FinishWriting();
                }

                return;
            }

            if (Stage == 4 && !State.dayOneSupernovaSeen && SceneManager.GetActiveScene().name == "LivingRoom" && !DayOneSupernovaEvent.Instance.IsPresenting)
            {
                if (TimeManager.Instance.Days != 1&&CrisisRules.LegacyValidation)
                {
                    NewsFinished();
                    return;
                }

                if (State.dayOneNewsStarted)
                    DayOneSupernovaEvent.Instance.Resume();
                else
                    DayOneSupernovaEvent.Instance.PlayFromFridge();
            }

            if (StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                return;
            if (player == null)
                Bind();
            var k = Keyboard.current;
            if (k == null || !k.eKey.wasPressedThisFrame || !WorldPromptUI.CanActivate("morning"))
                return;
            if (CanOpenFridge && (Stage == 3 || State.tutorialComplete))
                OpenFridge();
            else if (CanNap)
                BeginNap();
            else if (CanWrite)
                BeginWriting();
        }

        public string ActionPrompt => (Stage == 3 || State.tutorialComplete) && CanOpenFridge ? StoryText.Current.morning.fridgePrompt : CanNap ? StoryText.UI("copy.MorningRoutine.136") : CanWrite ? StoryText.UI("copy.MorningRoutine.137") : null;

        void OnGUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.HasEnded || MainMenuUI.WorldBlocked || GameSettingsUI.WorldBlocked)
                return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label)
                {
                    font = GameTypography.Current,
                    fontSize = 24,
                    wordWrap = true
                };
            }

            GameTypography.Use(label);
            var matrix = GUI.matrix;
            var color = GUI.color;
            int depth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            try
            {
                if (activity == Activity.FishWake)
                {
                    var story = StoryText.Current.morning;
                    NarrativeUI.Dialogue(NarrativeUI.DialogueRect(w, h), story.fishMorningSpeakers[fishMorningPage], story.fishMorningPages[fishMorningPage], StoryText.UI("copy.MorningRoutine.138"), label);
                    return;
                }
                if (activity == Activity.ReturnHome)
                {
                    NarrativeUI.Dialogue(NarrativeUI.DialogueRect(w, h), StoryText.UI("copy.MorningRoutine.139"), StoryText.Current.morning.food, StoryText.UI("copy.MorningRoutine.140"), label);
                    return;
                }

                if (activity == Activity.Wake || activity == Activity.Nap || activity == Activity.Write)
                {
                    GUI.depth = -80;
                    if (activity == Activity.Wake)
                        DreamMotion.Eyelids(w, h, DreamMotion.WakeOpenness(age));
                    else if (activity == Activity.Nap)
                    {
                        GUI.color = new Color(0, 0, 0, Mathf.Sin(Mathf.Clamp01(age / 5.3f) * Mathf.PI) * .42f);
                        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }

                    NarrativeUI.Dialogue(NarrativeUI.DialogueRect(w, h), StoryText.UI("copy.MorningRoutine.141"), activity == Activity.Wake ? StoryText.Current.morning.wake : activity == Activity.Nap ? (age < 3.8f ? StoryText.Current.morning.nap : StoryText.Current.morning.desk) : StoryText.Current.morning.writing, StoryText.UI("copy.MorningRoutine.142"), label);
                    return;
                }

                if (activity == Activity.Meal)
                {
                    GUI.depth = -46;
                    NarrativeUI.Dialogue(NarrativeUI.DialogueRect(w, h), StoryText.UI("copy.MorningRoutine.143"), mealMessage, StoryText.UI("copy.MorningRoutine.144"), label);
                    return;
                }

                if (activity == Activity.Fridge)
                {
                    NarrativeUI.Dialogue(NarrativeUI.DialogueRect(w, h), StoryText.UI("copy.MorningRoutine.145"), firstFridge ? StoryText.Current.morning.fridgeOpen : StoryText.Current.morning.fridgeEmpty, StoryText.UI("copy.MorningRoutine.146"), label);
                    return;
                }

                if (StoryJournal.WorldBlocked || FurnitureInteraction.IsBusy)
                    return;
                if (!string.IsNullOrEmpty(ActionPrompt))
                    WorldPromptUI.Offer("morning", ActionPrompt, 80);
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
            RestorePlayer();
            if (square != null)
                Destroy(square);
            if (pixel != null)
                Destroy(pixel);
        }
    }
}
