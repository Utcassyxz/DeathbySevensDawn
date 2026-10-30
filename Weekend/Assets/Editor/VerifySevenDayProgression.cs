using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Story;
public static class VerifySevenDayProgression
{
    const string Key="VerifySevenDayProgression";
    [MenuItem("Prototype/Create progression settings")]
    public static void CreateSettings()
    {
        if(!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets","Resources");
        if(AssetDatabase.LoadAssetAtPath<ProgressionSettings>("Assets/Resources/SevenDayProgression.asset")==null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ProgressionSettings>(),"Assets/Resources/SevenDayProgression.asset");
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Prototype/Validate synced seven day progression")]
    public static void Run()
    {
        SessionState.SetBool(DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent.SuppressionKey,true);
        CreateSettings();
        string path=ProgressionSave.PathName;
        // Exercise a clean save without changing the player's progress.
        SessionState.SetString(Key+"Save",File.Exists(path)?Convert.ToBase64String(File.ReadAllBytes(path)):"");
        SessionState.SetString(Key+"Backup",File.Exists(path+".bak")?Convert.ToBase64String(File.ReadAllBytes(path+".bak")):"");
        if(File.Exists(path)) File.Delete(path);
        SessionState.SetBool(Key,true);
        EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod] static void Subscribe() { EditorApplication.update-=Tick; EditorApplication.update+=Tick; }
    static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || GameManager.Instance==null) return;
        SessionState.SetBool(Key,false);
        int exit=0;
        try {
            var game=GameManager.Instance; var clock=TimeManager.Instance; var knowledge=KnowledgeManager.Instance;
            Check(clock.Days==1 && game.LoopCount==0,"Initial state");
            foreach(TimePeriod period in Enum.GetValues(typeof(TimePeriod))) {
                clock.ResetToDayOne();
                for(int p=0;p<(int)period;p++) clock.AdvanceTime();
                clock.SleepUntilNextMorning();
                Check(clock.Days==2 && clock.Period==TimePeriod.Morning,"Sleep from "+period);
            }
            clock.ResetToDayOne();
            int abilityEvents=0, sanityEvents=0; game.AbilityUnlocked+=_=>abilityEvents++; game.SanityLayerChanged+=_=>sanityEvents++;
            Check(knowledge.Unlock("test-ordinary",KnowledgeTier.Ordinary),"Ordinary grant");
            Check(!knowledge.Unlock("test-ordinary",KnowledgeTier.Cosmic),"Duplicate prevention");
            knowledge.Unlock("test-forbidden",KnowledgeTier.Forbidden);
            knowledge.Unlock("test-cosmic",KnowledgeTier.Cosmic);
            Check(game.Knowledge==50 && game.Mutation==28 && game.Sanity<100,"Knowledge effects");
            // Sanity is 69.2 here, above the configured unstable threshold 65.
            Check(game.KnowledgeLevel==2 && game.HasAbility("NightVision") && abilityEvents==2 && sanityEvents==0,"Threshold events");
            Check(JournalUpdates.UnreadCount==3,"New journal entries are unread and deduplicated");
            var feedback=UnityEngine.Object.FindAnyObjectByType<DeathBySeventhsDawn.Progression.GameplayFeedback>();
            Check(feedback!=null && !string.IsNullOrEmpty(feedback.JournalMessage),"Journal update feedback");
            Check(JournalUpdates.MarkRead("test-ordinary") && !JournalUpdates.MarkRead("test-ordinary") && JournalUpdates.UnreadCount==2,"Read individual entry only once");
            Check(ProgressionSave.Load().journal.Find(e=>e.id=="test-ordinary").isRead,"Read state saved to disk");
            Check(feedback.TimeMessage=="第 1 天 · 早晨","Latest time feedback after reset");
            Check(!game.CanUseHumanRoute(25) && game.MeetsKnowledgeRequirement(50),"Route and knowledge gates");
            for(int i=0;i<27;i++) clock.AdvanceTime();
            Check(clock.Days==7 && clock.Period==TimePeriod.Night && game.LoopCount==0,"Seven day boundary");
            clock.AdvanceTime();
            Check(clock.Days==1 && game.LoopCount==1 && game.MaxVitality==95 && game.Vitality==95,"Automatic restart and aging");
            Check(game.Knowledge==50 && game.Mutation==28 && knowledge.Has("test-cosmic"),"Loop retention");
            game.Damage(1000);
            Check(game.LoopCount==2 && game.MaxVitality==90 && clock.Days==1,"Vitality death");
            var loaded=ProgressionSave.Load();
            Check(loaded.journal!=null && loaded.journal.Count==3,"Knowledge journal persistence and duplicate prevention");
            Check(DeathBySeventhsDawn.Interaction.StoryJournal.IsNight(TimePeriod.Evening) && DeathBySeventhsDawn.Interaction.StoryJournal.IsNight(TimePeriod.Night),"Telescope evening/night availability");
            Check(!DeathBySeventhsDawn.Interaction.StoryJournal.IsNight(TimePeriod.Morning) && !DeathBySeventhsDawn.Interaction.StoryJournal.IsNight(TimePeriod.Afternoon),"Telescope daytime restriction");
            var story=Resources.Load<DeathBySeventhsDawn.Interaction.StoryContent>("StoryContent");
            Check(story!=null && StoryText.Current.dream!=null && StoryText.Current.dream.pages.Length==4,"Opening dream content");
            Check(Resources.Load<Texture2D>(story.ancientGodImageResource)!=null,"Ancient god illustration import");
            Check(loaded.loopCount==2 && loaded.knowledge==50 && loaded.mutation==28 && loaded.knowledgeIds.Count==3,"Disk persistence");
            Check(JournalUpdates.UnreadCount==2,"Read status retained across loops");
            knowledge.Unlock("test-sanity-transition",KnowledgeTier.Cosmic);
            Check(game.Layer!=SanityLayer.Stable && sanityEvents>0,"Sanity transition event at actual threshold");
            // At loop 2 the original default maximum is 90: temporarily use that as the floor.
            float oldFloor=game.Settings.minimumVitality;
            try {
                game.Settings.minimumVitality=game.MaxVitality;
                clock.ResetToDayOne();
                for(int day=0;day<5;day++) clock.SleepUntilNextMorning();
                Check(clock.Days==6 && !game.HasEnded,"No early ending");
                clock.SleepUntilNextMorning();
                Check(clock.Days==7 && game.HasEnded,"Sleeping to Day 7 triggers bad ending");
                int loops=game.LoopCount;
                clock.AdvanceTime(); clock.SleepUntilNextMorning(); LoopManager.Instance.RestartLoop();
                Check(clock.Days==7 && game.LoopCount==loops,"Ending blocks time and loops");
                Check(ProgressionSave.Load().badEndingOne,"Ending persistence");
            } finally { game.Settings.minimumVitality=oldFloor; }
            Debug.Log("SEVEN_DAY_PROGRESSION_PASS: boundary, death, aging, rewards, deduplication, levels, ability/sanity events, gates and disk persistence.");
        } catch(Exception e) { Debug.LogException(e); exit=1; }
        finally {
            Restore(ProgressionSave.PathName,SessionState.GetString(Key+"Save",""));
            Restore(ProgressionSave.PathName+".bak",SessionState.GetString(Key+"Backup",""));
        }
        File.WriteAllText("../seven-day-validation.txt",exit==0?"PASS: seven-day boundary, sleep, death, aging, knowledge, journal, ending and save persistence. Original save restored.":"FAIL: inspect Unity log. Original save restored.");
        if(Application.isBatchMode) EditorApplication.Exit(exit); else EditorApplication.isPlaying=false;
    }
    static void Restore(string path,string data) { if(data!="") File.WriteAllBytes(path,Convert.FromBase64String(data)); else if(File.Exists(path)) File.Delete(path); }
}