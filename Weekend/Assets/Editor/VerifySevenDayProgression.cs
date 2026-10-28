using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Progression;
using DeathBySeventhsDawn.TimeSystem;
using DeathBySeventhsDawn.Knowledge;
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
    public static void Run()
    {
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
            Check(game.KnowledgeLevel==2 && game.HasAbility("NightVision") && abilityEvents==2 && sanityEvents>0,"Threshold events");
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
            Check(story!=null && story.dreamPages.Length==4,"Opening dream content");
            Check(Resources.Load<Texture2D>(story.ancientGodImageResource)!=null,"Ancient god illustration import");
            Check(loaded.loopCount==2 && loaded.knowledge==50 && loaded.mutation==28 && loaded.knowledgeIds.Count==3,"Disk persistence");
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
        EditorApplication.Exit(exit);
    }
    static void Restore(string path,string data) { if(data!="") File.WriteAllBytes(path,Convert.FromBase64String(data)); else if(File.Exists(path)) File.Delete(path); }
}