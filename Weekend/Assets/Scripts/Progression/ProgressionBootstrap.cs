// 职责：场景加载前创建持久管理器，并安装运行时 UI 和剧情组件。
using UnityEngine;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.TimeSystem;

namespace DeathBySeventhsDawn.Progression
{
    public static class ProgressionBootstrap
    {
        // Initialize before scene objects: room-specific managers yield to these persistent singletons.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (GameManager.Instance == null)
                new GameObject("Progression Game Manager").AddComponent<GameManager>();
            if (TimeManager.Instance == null)
                new GameObject("Progression Clock").AddComponent<TimeManager>();
            if (KnowledgeManager.Instance == null)
                new GameObject("Progression Knowledge").AddComponent<KnowledgeManager>();
            if (LoopManager.Instance == null)
                new GameObject("Progression Loop").AddComponent<LoopManager>();
            if (Object.FindFirstObjectByType<ProgressionHUD>() == null)
            {
                var hud = new GameObject("Progression HUD");
                Object.DontDestroyOnLoad(hud);
                hud.AddComponent<ProgressionHUD>();
                hud.AddComponent<WorldEndSequence>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.RooftopAccess>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.FurnitureInteraction>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.FirstLoopPrologue>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.StoryJournal>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.AquariumEntity>();
                hud.AddComponent<GameplayFeedback>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.PerceptionClues>();
                hud.AddComponent<ApartmentAudio>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.EvidenceInspection>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.DayOneSupernovaEvent>();
                hud.AddComponent<SceneBackdrop>();
                hud.AddComponent<CorridorDetail>();
                hud.AddComponent<TimeAtmosphere>();
                hud.AddComponent<InteriorLighting>();
                hud.AddComponent<WorldPromptUI>();
                hud.AddComponent<StoryTransition>();
                hud.AddComponent<NightDreamSequence>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.CultNeighbor>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.MorningRoutine>();
                hud.AddComponent<GameSettingsUI>();
                hud.AddComponent<MainMenuUI>();
                hud.AddComponent<PauseMenuUI>();
                hud.AddComponent<DeathBySeventhsDawn.Interaction.InvestigationDevTools>();
            }
        }
    }
}
