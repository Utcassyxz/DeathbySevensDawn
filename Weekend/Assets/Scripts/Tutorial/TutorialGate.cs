// 职责：教程交互策略：统一判断当前允许的动作，并给未开放物件提供剧情内提示。
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Core;
using DeathBySeventhsDawn.Story;

namespace DeathBySeventhsDawn.Interaction
{
    /// <summary>Maps optional interactions to the current lesson without consuming movement or door travel.</summary>
    public static class TutorialGate
    {
        public static bool IsBlocked(string action)
        {
            var game = GameManager.Instance;
            if (game == null || game.State.tutorialComplete)
                return false;
            int stage = game.State.morningStage;
            switch (action)
            {
                case "neighbor":
                    return stage != 2;
                case "fridge":
                    return stage < 3;
                case "sofa":
                    return stage != 5;
                case "desk":
                    return stage != 6;
                case "telescope":
                    return stage < 7;
                case "journal":
                    return !game.State.journalUnlocked;
                case "board":
                    return !game.State.investigationUnlocked;
                default:
                    return true;
            }
        }

        // Diegetic reminders live in both language files, alongside the rest of the narrative.
        public static string Reminder
        {
            get
            {
                int stage = GameManager.Instance != null ? GameManager.Instance.State.morningStage : 0;
                var t = StoryText.Current.morning;
                switch (stage)
                {
                    case 3:
                        return t.kitchen;
                    case 4:
                        return t.afterNews;
                    case 5:
                        return t.sofa;
                    case 6:
                        return t.desk;
                    case 7:
                        return t.telescope;
                    case 8:
                        return t.investigate;
                    case 9:
                        return t.connect;
                    default:
                        return t.neighbor;
                }
            }
        }

        public static string Prompt(string action, string normal) => IsBlocked(action) ? Reminder : normal;

        /// <summary>Returns a nearby blocked prop, including furniture that has no free-exploration action yet.</summary>
        public static bool NearBlockedProp(Vector2 position)
        {
            string[,] props ={
                {"Bed","bed"},{"Television and stand","television"},{"Refrigerator","fridge"},
                {"Old sofa","sofa"},{"Aquarium and cabinet","aquarium"},{"阳台望远镜","telescope"}
            };
            for (int i = 0; i < props.GetLength(0); i++)
            {
                if (!IsBlocked(props[i, 1]))
                    continue;
                var prop = GameObject.Find(props[i, 0]);
                if (prop == null)
                    continue;
                var c = prop.GetComponent<Collider2D>();
                var r = prop.GetComponent<Renderer>();
                Vector2 nearest = c != null ? c.ClosestPoint(position) : r != null ? (Vector2)r.bounds.ClosestPoint(position) : (Vector2)prop.transform.position;
                if (Vector2.Distance(position, nearest) < .9f)
                    return true;
            }
            if (SceneManager.GetActiveScene().name == "Bedroom")
            {
                if (IsBlocked("desk") && Vector2.Distance(position, new Vector2(2.7f, -1.1f)) < 1.05f)
                    return true;
                var window = Object.FindAnyObjectByType<WindowLookout2D>();
                if (window != null && window.CanInteract)
                    return true;
            }
            if (SceneManager.GetActiveScene().name == "Corridor" && IsBlocked("evidence"))
                return Vector2.Distance(position, new Vector2(4.7f, 1.15f)) < 1f ||
                    (IsBlocked("neighbor") && Vector2.Distance(position, new Vector2(-4.7f, 1)) < .8f);
            return false;
        }
    }
}
