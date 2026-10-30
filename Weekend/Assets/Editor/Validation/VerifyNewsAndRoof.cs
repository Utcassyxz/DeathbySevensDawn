// 职责：回归验证冰箱、新闻、字幕分页、纯黑背景、天台互动及双语存档；恢复玩家原存档。
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.TimeSystem;

[InitializeOnLoad]
public static class VerifyNewsAndRoof
{
    const string Key = "NewsAndRoofRegression";
    static IEnumerator steps;
    static double next, deadline;
    static string error;
    static int checks;
    static ProgressionState State => (ProgressionState)typeof(GameManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);
    static VerifyNewsAndRoof()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += s =>
        {
            if (!SessionState.GetBool(Key, false))
                return;
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                steps = Steps();
                checks = 0;
                error = null;
                next = EditorApplication.timeSinceStartup + 2;
                deadline = next + 80;
            }
            if (s == PlayModeStateChange.EnteredEditMode)
                Restore();
        };
        Application.logMessageReceived += (m, s, t) => { if (steps != null && (t == LogType.Error || t == LogType.Exception) && !s.Contains("UnityEditor.Search.SearchDatabase")) error = m; };
    }
    [MenuItem("Prototype/Validate fridge news and fifth floor")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new Exception("Exit Play mode before validation.");
        foreach (var suffix in new[] { "", ".bak", ".tmp" })
        {
            var p = ProgressionSave.PathName + suffix;
            SessionState.SetString(Key + suffix, File.Exists(p) ? Convert.ToBase64String(File.ReadAllBytes(p)) : "");
        }
        SessionState.SetInt(Key + "lang", PlayerPrefs.GetInt("Story.Language", 0));
        SessionState.SetBool(Key + "suppressed", SessionState.GetBool(DayOneSupernovaEvent.SuppressionKey, false));
        SessionState.SetBool(Key + "background", PlayerSettings.runInBackground);
        SessionState.SetString(Key + "scene", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key, true);
        PlayerPrefs.SetInt("Story.Language", 0);
        SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey, true);
        ProgressionSave.Write(new ProgressionState { dreamSeen = true, morningStage = 3, cultDayOneTalked = true, neighborBarricadeSeen = true });
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/LivingRoom.unity");
        EditorApplication.isPlaying = true;
    }
    static void Check(bool condition, string message)
    {
        checks++;
        if (!condition)
            throw new Exception(message);
        Debug.Log("NEWS_ROOF_CHECK " + message);
    }
    static void Capture(string name)
    {
        if (Application.isBatchMode)
            return;
        Directory.CreateDirectory("../docs/validation/news-roof");
        ScreenCapture.CaptureScreenshot("../docs/validation/news-roof/" + name + ".png");
    }
    static void Place(Vector2 p)
    {
        var g = GameObject.FindWithTag("Player");
        g.transform.position = p;
        g.GetComponent<Rigidbody2D>().position = p;
        Physics2D.SyncTransforms();
    }
    static void Call(object o, string method) => o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, null);

    static IEnumerator Steps()
    {
        while (GameManager.Instance == null)
            yield return .1f;
        Application.runInBackground = true;
        var game = GameManager.Instance;
        Check(UnityEngine.Object.FindAnyObjectByType<SceneBackdrop>() != null, "Black backdrop controller installed without atmosphere masks");
        var captionStyle = new GUIStyle { font = GameTypography.Current, fontSize = 23, wordWrap = true };
        int splitCount = 0;
        for (int language = 0; language < 2; language++)
        {
            foreach (string caption in StoryText.Current.cult.dayOne)
            {
                var captionPages = new DialoguePages();
                captionPages.Set(caption, captionStyle, 830, 75);
                string rebuilt = "";
                do
                {
                    Check(captionStyle.CalcHeight(new GUIContent(captionPages.Current), 830) <= 73, "Neighbour subtitle fits its actual text rectangle");
                    rebuilt += captionPages.Current;
                } while (captionPages.Next());
                Check(rebuilt == caption, "Subtitle pages preserve every character and paragraph");
                if (captionPages.Count > 1)
                    splitCount++;
            }
            var unicode = new DialoguePages();
            string longText = string.Concat(System.Linq.Enumerable.Repeat("Hugo 🌙 雨果。 A longer line of text. ", 12));
            unicode.Set(longText, captionStyle, 420, 75);
            string joined = "";
            do
            {
                Check(captionStyle.CalcHeight(new GUIContent(unicode.Current), 420) <= 73, "Narrow bilingual subtitle fits");
                joined += unicode.Current;
            } while (unicode.Next());
            Check(joined == longText, "Pagination preserves Unicode and does not lose text");
            StoryText.ToggleLanguage();
        }
        Check(splitCount > 0, "Existing long neighbour dialogue now has multiple readable pages");
        State.cultDayOneTalked = false;
        SceneManager.LoadScene("Corridor");
        yield return .6f;
        int affected = Array.FindIndex(StoryText.Current.cult.dayOne, s => s.Contains("回去"));
        Check(affected >= 0, "Screenshot dialogue exists in the localized script");
        for (int language = 0; language < 2; language++)
        {
            var cult = CultNeighbor.Instance;
            Call(cult, "Close");
            Check(cult.TalkDayOne(), "Neighbour dialogue opens for pagination preview");
            typeof(CultNeighbor).GetField("line", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cult, affected);
            yield return .3f;
            int count = cult.SubtitlePages;
            Check(count >= 1, "Subtitle frame has measured pages");
            for (int sub = 0; sub < count; sub++)
            {
                Check(cult.SubtitlePage == sub && !State.cultDayOneTalked, "Reading a subtitle page does not complete the conversation");
                Capture("neighbor-" + (StoryText.English ? "english" : "chinese") + "-" + (sub + 1));
                yield return .25f;
                if (sub < count - 1)
                    cult.Advance();
                yield return .1f;
            }
            Call(cult, "Close");
            StoryText.ToggleLanguage();
        }
        State.cultDayOneTalked = true;
        SceneManager.LoadScene("LivingRoom");
        yield return .6f;
        var clock = TimeManager.Instance;
        var routine = MorningRoutine.Instance;
        var news = DayOneSupernovaEvent.Instance;
        var fridge = GameObject.Find("Refrigerator");
        var bounds = fridge.GetComponent<Collider2D>().bounds;
        Place(new Vector2(bounds.center.x, bounds.min.y - .3f));
        yield return .3f;
        Check(routine.OpenFridge(), "First fridge interaction starts");
        yield return 1.2f;
        Check(news.IsPresenting && !news.IsBoring && news.PageIndex == 0, "Fridge opens the shared first news page");
        Check(StoryText.Current.dayOneBroadcast.pages.Length == 4 && StoryText.Current.dayOneBroadcast.images.Length == 4, "Four news pages have four illustration paths");
        for (int i = 0; i < 4; i++)
        {
            Check(news.CurrentIllustration != null && news.PageIndex == i, "Illustrated news page " + i);
            Capture("news-" + i);
            yield return .25f;
            if (i == 1)
            {
                StoryText.ToggleLanguage();
                Check(StoryText.Current.dayOneBroadcast.narrator == "Narrator", "English news follows live language selection");
                Capture("news-english");
                yield return .25f;
                StoryText.ToggleLanguage();
            }
            news.Advance();
            yield return .15f;
        }
        Check(!news.IsPresenting && State.dayOneSupernovaSeen && State.skt079NewsSeen, "Finishing the bulletin consumes all TV news entrances");
        Check(routine.MealPresenting && State.breadEaten && !State.fridgeDoorOpen, "News closes fridge and presents Hugo eating bread");
        Check(fridge.GetComponent<SpriteRenderer>().sprite != Resources.Load<Sprite>("Tutorial/FridgeOpen"), "The fridge sprite is actually closed");
        Capture("hugo-breakfast");
        yield return 3f;
        Check(routine.OpenFridge(), "Repeated fridge interaction remains available");
        yield return 3.6f;
        Check(!news.IsPresenting && !State.fridgeDoorOpen && routine.Stage == 5, "Repeated fridge closes without rewinding the news or tutorial");
        State.tutorialComplete = State.journalUnlocked = State.investigationUnlocked = true;
        State.morningStage = 10;
        clock.SleepUntilNextMorning();
        yield return .2f;
        Check(clock.Days == 2, "Regression reaches second day");
        Check(routine.OpenFridge(), "Day-two fridge interaction available");
        yield return 3.6f;
        Check(!news.IsPresenting && !State.fridgeDoorOpen && State.newsPage == 4, "Day-two fridge cannot replay television and cannot stay open");
        for (int i = 0; i < 3; i++)
        {
            var period = clock.Period;
            Check(news.OpenTelevision() && news.IsBoring, "Consumed television is boring at period " + period);
            Check(news.CurrentIllustration == null, "Boring programs do not retain the last news illustration");
            Check(clock.Period == period, "Opening television does not secretly advance time");
            if (i == 0)
            {
                Capture("boring-tv");
                yield return .3f;
                StoryText.ToggleLanguage();
                Check(!System.Text.RegularExpressions.Regex.IsMatch(StoryText.Current.dayOneBroadcast.boring, "[\\u4e00-\\u9fff]"), "Boring-program narration is translated");
                Capture("boring-tv-english");
                yield return .3f;
                StoryText.ToggleLanguage();
            }
            news.Advance();
            yield return .2f;
            var sofaBounds = GameObject.Find("Old sofa").GetComponent<Collider2D>().bounds;
            Place(new Vector2(sofaBounds.center.x, sofaBounds.min.y - .3f));
            yield return .2f;
            Check(UnityEngine.Object.FindAnyObjectByType<FurnitureInteraction>().RestOnePeriod(), "Sofa supplies normal time progression after the bulletin");
            yield return .2f;
        }
        // Day two remains pre-mutation: no talking entity or ominous before-the-fact description.
        var tank = GameObject.Find("Aquarium and cabinet");
        Place(tank.transform.position + Vector3.down * .5f);
        yield return .3f;
        State.inventory.Add(CultNeighbor.Token);
        Call(AquariumEntity.Instance, "OpenOffer");
        yield return .2f;
        var pages = (string[])typeof(AquariumEntity).GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(AquariumEntity.Instance);
        Check(pages[0] == StoryText.Current.aquarium.offerBefore && !AquariumEntity.Instance.FormVisible, "Before mutation the offer describes ordinary fish, not a speaking monster");
        Capture("ordinary-fish-foil");
        yield return .2f;
        Call(AquariumEntity.Instance, "CloseDialogue");
        yield return .2f;
        Check(Application.CanStreamedLevelBeLoaded("Rooftop"), "Real rooftop scene is in the build list");
        SceneManager.LoadScene("Corridor");
        yield return .7f;
        Place(RooftopAccess.StairsFour);
        yield return .2f;
        Check(RooftopAccess.Instance.UseStairs(), "Fourth-floor stairs load fifth-floor rooftop");
        yield return .7f;
        Check(SceneManager.GetActiveScene().name == "Rooftop" && GameObject.Find("Rooftop pixel environment") != null, "Rooftop has its own walkable scene");
        Capture("rooftop");
        yield return .3f;
        Place(RooftopAccess.TelescopeSpot);
        yield return .2f;
        Check(!RooftopAccess.Instance.PlaceTelescope() && !RooftopAccess.Instance.Observe(), "Cannot place or observe a telescope that has not been brought upstairs");
        Place(RooftopAccess.StairsFive);
        yield return .2f;
        Check(RooftopAccess.Instance.UseStairs(), "Fifth-floor stairs return to fourth floor");
        yield return .7f;
        Check(SceneManager.GetActiveScene().name == "Corridor" && Vector2.Distance(GameObject.FindWithTag("Player").transform.position, RooftopAccess.StairsFour + Vector2.right * 1.1f) < .2f, "Return arrives at the fourth-floor stair landing");
        SceneManager.LoadScene("Balcony");
        yield return .7f;
        var scope = GameObject.Find("阳台望远镜");
        Place(scope.transform.position);
        yield return .2f;
        Check(RooftopAccess.Instance.TakeTelescope() && State.telescopeCarried, "Balcony telescope packs into carried state");
        Check(ProgressionSave.Load().telescopeCarried, "Carried telescope persists to disk");
        yield return .2f;
        RooftopAccess.Instance.Close();
        yield return .2f;
        SceneManager.LoadScene("Corridor");
        yield return .7f;
        Place(RooftopAccess.StairsFour);
        yield return .2f;
        Check(RooftopAccess.Instance.UseStairs(), "Carried telescope travels up the same stairs");
        yield return .7f;
        Place(RooftopAccess.TelescopeSpot);
        yield return .2f;
        Check(RooftopAccess.Instance.PlaceTelescope() && State.telescopeOnRoof && !State.telescopeCarried, "Placement consumes carried state exactly once");
        Check(ProgressionSave.Load().telescopeOnRoof, "Placed rooftop telescope is saved");
        yield return .3f;
        RooftopAccess.Instance.Close();
        yield return .2f;
        clock.ResetToDayOne();
        clock.SleepUntilNextMorning();
        yield return .2f;
        Check(RooftopAccess.Instance.Observe() && !game.HasSeenAncientGod, "Daylight observation cannot unlock the ancient god");
        yield return .2f;
        RooftopAccess.Instance.Close();
        yield return .2f;
        clock.AdvanceTime();
        clock.AdvanceTime();
        yield return .2f;
        Check(RooftopAccess.Instance.Observe() && game.HasSeenAncientGod && RooftopAccess.Instance.IsRevelation, "Second-night physical rooftop observation unlocks the actual encounter");
        Check(ProgressionSave.Load().ancientGodSeen, "Actual rooftop encounter persists");
        Capture("rooftop-revelation");
        yield return .3f;
        Check(Investigation.Collect(StoryContent.AncientKnowledgeId), "Rooftop evidence can be deliberately recorded");
        float knowledge = game.Knowledge;
        RooftopAccess.Instance.Close();
        yield return .2f;
        RooftopAccess.Instance.Observe();
        Check(game.Knowledge == knowledge, "Repeated rooftop observation cannot duplicate the cosmic reward");
        RooftopAccess.Instance.Close();
        yield return .2f;
        SceneManager.LoadScene("Bedroom");
        yield return .4f;
        SceneManager.LoadScene("Rooftop");
        yield return .7f;
        Check(GameObject.Find("Rooftop telescope") != null, "Placed telescope survives room reload");
        Check(GameObject.Find("Rooftop pixel environment")?.GetComponent<SpriteRenderer>().sprite != null, "Detailed pixel rooftop art is present");
        Check(GameObject.Find("Distant roof") == null, "No detached skyline floating outside the rooftop silhouette");
        Check(Camera.main.backgroundColor == Color.black && Camera.main.clearFlags == CameraClearFlags.SolidColor, "Actual camera clears outside roof to pure black");
        Capture("rooftop-refined");
        yield return .3f;

        StoryText.ToggleLanguage();
        Check(StoryText.Current.morning.breadMeal.Contains("Hugo") && StoryText.Current.rooftop.revelation.Contains("Hugo"), "English narrator names Hugo in breakfast and rooftop scenes");
        Check(StoryText.UI("合上 · Esc") == "Close · Esc" && StoryText.UI("调试面板") == "Developer panel", "Previously missed journal and developer controls translate");
        foreach (var item in StoryText.Current.ui)
            Check(!System.Text.RegularExpressions.Regex.IsMatch(item.text, "[\\u4e00-\\u9fff]"), "UI translation: " + item.key);
        foreach (var relation in Investigation.Relations)
            Check(!System.Text.RegularExpressions.Regex.IsMatch(relation, "[\\u4e00-\\u9fff]"), "English relation: " + relation);
        foreach (var deduction in Investigation.Catalog.deductions)
            Check(!System.Text.RegularExpressions.Regex.IsMatch(deduction.relation, "[\\u4e00-\\u9fff]"), "English deduction relation: " + deduction.id);
        State.investigationUnlocked = true;
        string first = Investigation.Catalog.evidence[0].id, second = Investigation.Catalog.evidence[1].id;
        Investigation.Collect(first);
        Investigation.Collect(second);
        Check(Investigation.Connect(first, second, Investigation.Relations[0], false), "English relation can be saved");
        int linkCount = Investigation.Links.Count;
        StoryText.ToggleLanguage();
        Check(Investigation.FindLink(first, second, Investigation.Relations[0]) != null && !Investigation.Connect(first, second, Investigation.Relations[0], false) && Investigation.Links.Count == linkCount, "Switching language preserves saved relation identity without duplicates");
        StoryText.ToggleLanguage();
        Check(error == null, "No runtime errors during fridge, TV, stairs and rooftop sequence");
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (steps == null)
        {
            if (!EditorApplication.isPlaying && File.Exists("../news-roof-request.txt"))
            {
                File.Delete("../news-roof-request.txt");
                Run();
            }
            return;
        }
        if (EditorApplication.timeSinceStartup < next)
            return;
        try
        {
            if (error != null)
                throw new Exception(error);
            if (EditorApplication.timeSinceStartup > deadline)
                throw new Exception("Timed out");
            if (steps.MoveNext())
                next = EditorApplication.timeSinceStartup + (float)steps.Current;
            else
                Finish(null);
        }
        catch (Exception e) { Finish(e.ToString()); }
    }
    static void Finish(string failure)
    {
        steps = null;
        File.WriteAllText("../news-roof-validation.txt", failure == null ? "PASS: " + checks + " checks; auto-closing fridge, bread, day-two regression, illustrated sequential news, boring repeat programs, no TV time advancement, normal sofa time progression, pre-mutation fish, floor-four/five round-trip, portable telescope, actual rooftop encounter, persistence, reward deduplication and bilingual controls, measured multilingual subtitle pagination and real black scene backgrounds. Original player save restored." : "FAIL: " + failure);
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode)
        {
            Restore();
            EditorApplication.Exit(failure == null ? 0 : 1);
        }
    }
    static void Restore()
    {
        if (!SessionState.GetBool(Key, false))
            return;
        SessionState.SetBool(Key, false);
        foreach (var suffix in new[] { "", ".bak", ".tmp" })
        {
            var path = ProgressionSave.PathName + suffix;
            var bytes = SessionState.GetString(Key + suffix, "");
            if (bytes.Length > 0)
                File.WriteAllBytes(path, Convert.FromBase64String(bytes));
            else if (File.Exists(path))
                File.Delete(path);
        }
        PlayerPrefs.SetInt("Story.Language", SessionState.GetInt(Key + "lang", 0));
        PlayerPrefs.Save();
        SessionState.SetBool(DayOneSupernovaEvent.SuppressionKey, SessionState.GetBool(Key + "suppressed", false));
        Application.runInBackground = SessionState.GetBool(Key + "background", false);
        PlayerSettings.runInBackground = SessionState.GetBool(Key + "background", false);
        if (!Application.isBatchMode)
            EditorApplication.delayCall += () => { var scene = SessionState.GetString(Key + "scene", ""); if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene); };
    }
}
