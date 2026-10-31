// 职责：日记、梦境与阳台观测页面的输入协调；阳台不触发天台古神事件。
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
    // 所有剧情弹窗统一占用输入，避免看图、读日记或梦境期间仍然走路/触发家具。
    public sealed class StoryJournal : MonoBehaviour
    {
        static bool modalOpen;
        static int blockedThroughFrame = -1;
        // 关闭弹窗的这一帧仍占用输入，避免同一个 E 又打开床、窗户或望远镜。
        public static bool WorldBlocked => modalOpen || RooftopAccess.WorldBlocked || GameSettingsUI.WorldBlocked || MorningRoutine.WorldBlocked || CultNeighbor.WorldBlocked || Time.frameCount <= blockedThroughFrame || FirstLoopPrologue.WorldBlocked || PerceptionClues.IsReading || EvidenceInspection.BlocksWorld || DayOneSupernovaEvent.WorldBlocked || AquariumEntity.WorldBlocked || MainMenuUI.WorldBlocked || PauseMenuUI.WorldBlocked || InvestigationDevTools.WorldBlocked;
        enum Page
        {
            None, Dream, Diary, Telescope
        }
        Page page;
        StoryContent content;
        int dreamPage, selectedEntry;
        readonly DialoguePages dreamCaption = new DialoguePages();
        readonly DialoguePages telescopeCaption=new DialoguePages();
        Vector2 scroll;
        GameObject telescope, player;
        bool nearby, revelation;
        float dreamEntered;int displayedDream=-1;Texture2D approachingBody;bool firstObjectView;
        string telescopeMessage;
        Texture2D[] dreamImages;
        bool dreamImagesLoaded;
        GUIStyle text, title, button;
        readonly JournalNotebookUI notebook = new JournalNotebookUI();
        readonly InvestigationBoardUI board = new InvestigationBoardUI();
        bool showBoard, boardReturnToGame;
        Texture2D whitePixel;
        Sprite square;
        public bool IsRevelation => page == Page.Telescope && revelation;
        public static bool IsNight(TimePeriod period) => period == TimePeriod.Evening || period == TimePeriod.Night;
        void Awake()
        {
            approachingBody=Resources.Load<Texture2D>("Story/ApproachingBody");
            content = Resources.Load<StoryContent>("StoryContent");
            if (content == null)
                content = ScriptableObject.CreateInstance<StoryContent>();
            board.CloseRequested = CloseBoard;
            // 补齐旧版本已解锁知识的日记，不发放第二次知识奖励。
            var state = GameManager.Instance.State;
            if (state.journal == null)
                state.journal = new System.Collections.Generic.List<JournalEntry>();
            state.journal.RemoveAll(e => e != null && (e.id == "opening-dream" || e.id == "day-one-supernova"));
            foreach (var entry in state.journal)
                if (entry != null && entry.id == StoryContent.AncientKnowledgeId && entry.imageResource == "Story/AncientGodDay2")
                    entry.imageResource = content.ancientGodImageResource;
            if (state.journalUnlocked)
                foreach (string id in state.knowledgeIds)
                {
                    if (state.journal.Exists(e => e != null && e.id == id))
                        continue;
                    content.Describe(id, out string heading, out string body, out string image);
                    state.journal.Add(new JournalEntry { id = id, title = heading, body = body, imageResource = image, tier = id == StoryContent.AncientKnowledgeId ? content.ancientGodTier : KnowledgeTier.Ordinary, day = id == StoryContent.WindowKnowledgeId || id == StoryContent.AncientKnowledgeId ? 2 : 1, loop = state.loopCount });
                }
            ProgressionSave.Write(state);
        }
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnLoaded;
            StoryText.LanguageChanged += LanguageChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset += ResetJourney;
        }
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            StoryText.LanguageChanged -= LanguageChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.JourneyReset -= ResetJourney;
            SetPage(Page.None);
        }
        void LanguageChanged()
        {
            board.Reset();
            if (page == Page.Telescope)
                telescopeMessage = Investigation.Evidence("apartment-telescope").body;
        }
        void ResetJourney()
        {
            dreamPage = 0;
            dreamCaption.Reset();
            selectedEntry = 0;
            revelation = false;
            nearby = false;
            telescopeMessage = null;
            notebook.ResetNavigation();
            board.Reset();
            showBoard = false;
            boardReturnToGame = false;
            SetPage(Page.Dream);
            blockedThroughFrame = Time.frameCount;
        }
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (page != Page.Dream)
                SetPage(Page.None);
            showBoard = false;
            boardReturnToGame = false;
            player = GameObject.FindWithTag("Player");
            telescope = null;
            nearby = false;
            if (scene.name == "Balcony" && !GameManager.Instance.State.telescopeCarried && !GameManager.Instance.State.telescopeOnRoof)
                BuildTelescope(scene);
        }
        void SetPage(Page next)
        {
            if (page != Page.None && next == Page.None)
                blockedThroughFrame = Time.frameCount;
            if(next==Page.Dream && page!=Page.Dream){dreamEntered=Time.unscaledTime;displayedDream=-1;}
            page = next;
            modalOpen = next != Page.None;
            scroll = Vector2.zero;
            if (next == Page.None)
            {
                showBoard = false;
                boardReturnToGame = false;
            }
            if (next == Page.Diary)
            {
                notebook.ResetNavigation();
                if (!showBoard)
                    ReadSelectedEntry();
            }
        }
        void CloseBoard()
        {
            showBoard = false;
            if (boardReturnToGame)
            {
                boardReturnToGame = false;
                SetPage(Page.None);
            }
        }
        void Update()
        {
            var game = GameManager.Instance;
            if (game == null)
                return;
            if (game.HasEnded)
            {
                SetPage(Page.None);
                return;
            }
            if (!game.State.dreamSeen && page != Page.Dream)
            {
                dreamPage = 0;
                dreamCaption.Reset();
                SetPage(Page.Dream);
            }
            if (RooftopAccess.WorldBlocked || MorningRoutine.WorldBlocked || CultNeighbor.WorldBlocked || ProgressionHUD.DebugConsoleOpen || FirstLoopPrologue.WorldBlocked || PerceptionClues.IsReading || EvidenceInspection.BlocksWorld || DayOneSupernovaEvent.WorldBlocked || AquariumEntity.WorldBlocked || MainMenuUI.WorldBlocked || PauseMenuUI.WorldBlocked || InvestigationDevTools.WorldBlocked)
                return;
            bool diary = false, interact = false, cancel = false;
            int diaryStep = 0;
#if ENABLE_INPUT_SYSTEM
            var keyboard=Keyboard.current;
            if(keyboard!=null) { diary=keyboard.jKey.wasPressedThisFrame; interact=keyboard.eKey.wasPressedThisFrame; cancel=keyboard.escapeKey.wasPressedThisFrame; diaryStep=(keyboard.rightArrowKey.wasPressedThisFrame?1:0)-(keyboard.leftArrowKey.wasPressedThisFrame?1:0); }
#elif ENABLE_LEGACY_INPUT_MANAGER
            diary=Input.GetKeyDown(KeyCode.J); interact=Input.GetKeyDown(KeyCode.E); cancel=Input.GetKeyDown(KeyCode.Escape); diaryStep=(Input.GetKeyDown(KeyCode.RightArrow)?1:0)-(Input.GetKeyDown(KeyCode.LeftArrow)?1:0);
#endif
            if (page == Page.Dream)
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    if (dreamCaption.Next())
                        return;
                    dreamCaption.Reset();
                    if (dreamPage < StoryText.Current.dream.pages.Length - 1)
                        dreamPage++;
                    else
                        WakeUp();
                }
                return;
            }
            if (page == Page.Diary)
            {
#if ENABLE_INPUT_SYSTEM
                if(game.State.investigationUnlocked&&keyboard!=null&&keyboard.bKey.wasPressedThisFrame){ if(!showBoard){showBoard=true;boardReturnToGame=false;} return; }
#endif
                if (cancel)
                {
                    if (showBoard)
                        CloseBoard();
                    else
                        SetPage(Page.None);
                    return;
                }
                if (showBoard)
                    board.KeyboardInput();
                else if (diaryStep != 0 && notebook.MoveSelection(game.State.journal, ref selectedEntry, diaryStep))
                    ReadSelectedEntry();
                return;
            }
            if (page == Page.Telescope)
            {
                if(keyboard!=null&&keyboard.eKey.wasPressedThisFrame)telescopeCaption.Next();
#if ENABLE_INPUT_SYSTEM
                if(keyboard!=null&&keyboard.rKey.wasPressedThisFrame)CollectTelescopeEvidence();
                if(!revelation&&keyboard!=null&&keyboard.tKey.wasPressedThisFrame)ObserveTelescope();
#endif
                if (cancel)
                    CloseTelescope();
                return;
            }
            var window = Object.FindAnyObjectByType<WindowLookout2D>();
            if (FurnitureInteraction.IsBusy || (window != null && window.IsOpen))
            {
                nearby = false;
                return;
            }
#if ENABLE_INPUT_SYSTEM
            if(game.State.investigationUnlocked&&keyboard!=null&&keyboard.bKey.wasPressedThisFrame) { selectedEntry=game.State.journal.FindLastIndex(e=>e!=null && !e.isRead); if(selectedEntry<0)selectedEntry=Mathf.Max(0,game.State.journal.Count-1); showBoard=true; boardReturnToGame=true; SetPage(Page.Diary); return; }
#endif
            if (diary && game.State.journalUnlocked)
            {
                selectedEntry = game.State.journal.FindLastIndex(e => e != null && !e.isRead);
                if (selectedEntry < 0)
                    selectedEntry = Mathf.Max(0, game.State.journal.Count - 1);
                SetPage(Page.Diary);
                return;
            }
            if (player == null)
                player = GameObject.FindWithTag("Player");
            nearby = !TutorialGate.IsBlocked("telescope") && game.State.journalUnlocked && telescope != null && player != null && Vector2.Distance(player.transform.position, telescope.transform.position) <= content.telescopeRange;
            if (nearby && interact && WorldPromptUI.CanActivate("telescope"))
                OpenTelescope();
        }
        public void ShowJournal()
        {
            if (!GameManager.Instance.State.journalUnlocked)
                return;
            selectedEntry = 0;
            showBoard = false;
            SetPage(Page.Diary);
        }
        void ReadSelectedEntry()
        {
            var entries = GameManager.Instance.State.journal;
            if (entries.Count > 0)
            {
                selectedEntry = Mathf.Clamp(selectedEntry, 0, entries.Count - 1);
                JournalUpdates.MarkRead(entries[selectedEntry].id);
            }
        }
        void WakeUp()
        {
            var state = GameManager.Instance.State;
            state.dreamSeen = true;
            ProgressionSave.Write(state);
            MorningRoutine.Instance?.QueueWake();
            SetPage(Page.None);
            // 正式新旅程醒在卧室；若未加入构建列表，保持当前场景，不破坏编辑器测试。
            if (Application.CanStreamedLevelBeLoaded("Bedroom"))
                SceneManager.LoadScene("Bedroom");
        }
        void CloseTelescope()
        {
            SetPage(Page.None);
        }
        void OpenTelescope()
        {
            if (TutorialGate.IsBlocked("telescope"))
                return;
            revelation = false;
            firstObjectView=false;
            telescopeMessage = Investigation.Evidence("apartment-telescope").body;
            SetPage(Page.Telescope);
        }
        public bool CollectTelescopeEvidence()
        {
            bool result = page == Page.Telescope && Investigation.Collect(revelation ? StoryContent.AncientKnowledgeId : "apartment-telescope");
            if (result && !revelation)
            {
                MorningRoutine.Instance?.TelescopeRecorded();
                if (GameManager.Instance.State.morningStage == 9)
                    telescopeMessage = StoryText.Current.morning.connect;
            }
            return result;
        }
        void ObserveTelescope()
        {
            var clock = TimeManager.Instance;
            var text = StoryText.Current.telescope;
            revelation = false;
            firstObjectView=false;
            if (!IsNight(clock.Period))
                telescopeMessage = text?.nightOnly ?? "白天的光太强，镜头里只有刺眼的反光。";
            // The balcony teaches observation; only the placed rooftop telescope can reveal the entity.
            else
                telescopeMessage = clock.Days==1?text.firstNight:text.wrongDay;
            if(IsNight(clock.Period)&&clock.Days==1){firstObjectView=true;GameManager.Instance.State.firstNightObjectSeen=true;ProgressionSave.Write(GameManager.Instance.State);}
            if (IsNight(clock.Period) && clock.Days == 1 && MorningRoutine.Instance != null)
            {
                var tutorial = MorningRoutine.Instance;
                if (tutorial.Stage == 7 || tutorial.Stage == 8)
                {
                    tutorial.TelescopeObserved();
                    telescopeMessage += "\n\n" + StoryText.Current.morning.investigate;
                }
                else if (tutorial.Stage == 9)
                    telescopeMessage += "\n\n" + StoryText.Current.morning.connect;
            }
            SetPage(Page.Telescope);
        }
        void BuildTelescope(Scene scene)
        {
            // 【改望远镜位置】在 Resources/StoryContent.asset 调 telescopePosition。
            // 暂时用简单图形组成望远镜；以后可替换成美术 prefab，保留互动中心即可。
            telescope = new GameObject("阳台望远镜");
            SceneManager.MoveGameObjectToScene(telescope, scene);
            telescope.transform.position = content.telescopePosition;
            if (square == null)
            {
                whitePixel = new Texture2D(1, 1);
                whitePixel.SetPixel(0, 0, Color.white);
                whitePixel.Apply();
                square = Sprite.Create(whitePixel, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            }
            Part("镜筒", new Vector2(0, .3f), new Vector2(.75f, .22f), new Color(.25f, .52f, .55f), -22);
            Part("物镜", new Vector2(.34f, .44f), new Vector2(.12f, .32f), new Color(.66f, .81f, .85f), -22);
            Part("目镜", new Vector2(-.37f, .15f), new Vector2(.16f, .12f), new Color(.10f, .14f, .17f), -22);
            Part("三脚架左", new Vector2(-.14f, -.18f), new Vector2(.065f, .7f), new Color(.56f, .48f, .31f), -25);
            Part("三脚架右", new Vector2(.14f, -.18f), new Vector2(.065f, .7f), new Color(.56f, .48f, .31f), 25);
            Part("支架", new Vector2(0, -.05f), new Vector2(.07f, .5f), new Color(.45f, .4f, .3f), 0);
        }
        void Part(string name, Vector2 position, Vector2 size, Color color, float angle)
        {
            var part = new GameObject(name);
            part.transform.SetParent(telescope.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = new Vector3(size.x, size.y, 1);
            part.transform.localRotation = Quaternion.Euler(0, 0, angle);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = 10;
            var tiles = GameObject.Find("Balcony tiles");
            if (tiles != null)
                renderer.sharedMaterial = tiles.GetComponent<SpriteRenderer>().sharedMaterial;
        }
        void Styles()
        {
            if (text != null)
                return;
            var font = DeathBySeventhsDawn.Progression.GameTypography.Current;
            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 26, wordWrap = true, padding = new RectOffset(18, 18, 12, 12) };
            title = new GUIStyle(text) { fontSize = 32, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUIStyle.none) { font = font, fontSize = 24, wordWrap = true, padding = new RectOffset(12, 12, 14, 14), alignment = TextAnchor.MiddleCenter };
            button.normal.textColor = new Color(.74f, .78f, .66f);
            button.hover.textColor = new Color(.96f, .94f, .82f);
            button.active.textColor = Color.white;
        }
        void OnGUI()
        {
            DeathBySeventhsDawn.Progression.GameTypography.Use(text, title, button);
            if (RooftopAccess.WorldBlocked || MorningRoutine.WorldBlocked || CultNeighbor.WorldBlocked)
                return;
            var game = GameManager.Instance;
            if (game == null || game.HasEnded || ProgressionHUD.DebugConsoleOpen || FirstLoopPrologue.WorldBlocked || PerceptionClues.IsReading || EvidenceInspection.BlocksWorld || DayOneSupernovaEvent.WorldBlocked || AquariumEntity.WorldBlocked || MainMenuUI.WorldBlocked || PauseMenuUI.WorldBlocked || InvestigationDevTools.WorldBlocked)
                return;
            Styles();
            DeathBySeventhsDawn.Progression.PrototypeButtons.Apply(button);
            var matrix = GUI.matrix;
            var color = GUI.color;
            float scale = Mathf.Min(Screen.height / 900f, Screen.width / 1100f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            try
            {
                if (page == Page.Dream)
                {
                    DrawDream(width, height);
                    return;
                }
                if (page == Page.Diary)
                {
                    GUI.depth = -20;
                    if (showBoard)
                    {
                        board.Draw(width, height);
                        return;
                    }
                    {
                        GUI.BeginGroup(new Rect(0, 82, width, height - 82));
                        notebook.Draw(game.State.journal, ref selectedEntry, ReadSelectedEntry, () => SetPage(Page.None), width, height - 82);
                        GUI.EndGroup();
                    }
                    GUI.color = new Color(.07f, .12f, .12f);
                    GUI.DrawTexture(new Rect(0, 0, width, 82), Texture2D.whiteTexture);
                    GUI.color = color;
                    if (game.State.investigationUnlocked && GUI.Button(new Rect(28, 18, 220, 48), StoryText.Local("● 线索连线 · B", "Evidence board · B"), button))
                    {
                        showBoard = true;
                        boardReturnToGame = false;
                    }
                    if (GUI.Button(new Rect(260, 18, 190, 48), DeathBySeventhsDawn.Story.StoryText.UI("合上 · Esc"), button))
                        SetPage(Page.None);
                    return;
                }
                if (page == Page.None)
                {
                    if (nearby)
                        WorldPromptUI.Offer("telescope", StoryText.UI("E  查看望远镜"),60);
                    return;
                }
                GUI.depth = -20;
                GUI.color = page == Page.Diary ? new Color(.87f, .81f, .66f, 1) : new Color(.025f, .03f, .05f, 1);
                GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                GUI.color = color;
                var telescopeText=StoryText.Current.telescope;
                if(firstObjectView&&approachingBody!=null)GUI.DrawTexture(new Rect(width*.16f,35,width*.68f,height*.46f),approachingBody,ScaleMode.ScaleToFit);
                var dialogue=NarrativeUI.DialogueRect(width,height);
                var subtitleStyle=new GUIStyle(text){fontSize=23,padding=new RectOffset()};
                telescopeCaption.Set(telescopeMessage,subtitleStyle,dialogue.width-50,dialogue.height-86);
                NarrativeUI.Dialogue(dialogue,StoryText.Local("雨果 · 望远镜","Hugo · Telescope"),telescopeCaption.Current,telescopeCaption.Count>1?telescopeCaption.Counter+StoryText.Local("E 继续","E Continue"):"",text);
                float rowY=dialogue.y-64,bw=(dialogue.width-24)/3;
                if(game.State.investigationUnlocked&&!Investigation.Has("apartment-telescope")&&GUI.Button(new Rect(dialogue.x,rowY,bw,48),telescopeText.collect,button))CollectTelescopeEvidence();
                if(GUI.Button(new Rect(dialogue.x+bw+12,rowY,bw,48),telescopeText.observe,button))ObserveTelescope();
                if(GUI.Button(new Rect(dialogue.x+(bw+12)*2,rowY,bw,48),telescopeText.close,button))CloseTelescope();
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.depth = 0; }
        }
        void DrawDream(float w, float h)
        {
            GUI.depth = -30;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var dream = StoryText.Current.dream;
            if (!dreamImagesLoaded)
            {
                dreamImagesLoaded = true;
                var paths = dream.images;
                dreamImages = new Texture2D[paths.Length];
                for (int i = 0; i < paths.Length; i++)
                    dreamImages[i] = Resources.Load<Texture2D>(paths[i]);
            }
            var image = dreamImages.Length > 0 ? dreamImages[Mathf.Clamp(dreamPage, 0, dreamImages.Length - 1)] : null;
            if (image != null)
            {
                if(displayedDream!=dreamPage){displayedDream=dreamPage;dreamEntered=Time.unscaledTime;}
                float elapsed=Time.unscaledTime-dreamEntered;
                float drift=(Mathf.PerlinNoise(dreamPage+2,elapsed*.12f)-.5f)*9;
                float opacity=DreamMotion.Opacity(elapsed,dreamPage);
                GUI.color = new Color(1,1,1,opacity);
                GUI.DrawTextureWithTexCoords(new Rect(w * .16f + drift, h * .07f, w * .68f, h * .52f), image, new Rect(.08f, .22f, .84f, .60f));
                GUI.color = Color.white;
            }
            dreamCaption.Set(dream.pages[Mathf.Clamp(dreamPage, 0, dream.pages.Length - 1)], text, NarrativeUI.DialogueRect(w,h).width-86,96);
            NarrativeUI.Dialogue(NarrativeUI.DialogueRect(w,h),StoryText.Local("梦里","Dream"),dreamCaption.Current,dreamCaption.Counter+StoryText.Current.morning.continueDream,text);
        }
        void OnDestroy()
        {
            modalOpen = false;
            blockedThroughFrame = -1;
            if (square != null)
                Destroy(square);
            if (whitePixel != null)
                Destroy(whitePixel);
        }
    }
}
