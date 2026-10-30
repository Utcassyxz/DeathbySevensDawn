// 职责：中英文文案目录加载、切换和反序列化结构；剧情文字统一从资源读取。
using System;
using UnityEngine;
namespace DeathBySeventhsDawn.Story
{
    // ============================================================================
    // 【文案规则 · 必须长期遵守】
    // 所有面向玩家的叙事、对白、梦境、章节、结局、提示文案，统一写在：
    //     Assets/Resources/StoryText.json
    // 不要把这些文字重新硬编码到 MonoBehaviour 里。
    // 新增一段剧情/对白时：先改 StoryText.json，再在代码里通过 StoryText.Current 读取。
    //
    // 线索与推理的标题/正文也集中在 StoryText.json 的 investigation 段；
    // Investigation.json 只保留 id、左右节点、图片等结构数据。
    //
    // UI 控件文字（按钮、调试面板、状态栏）可以留在 UI 代码中；
    // 但任何“剧情文案”都必须走这个文件。
    // ============================================================================
    public static class StoryText
    {
        public const string ResourcePath = "StoryText";
        static StoryTextData data;
        static bool english; public static bool English => english;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadLanguage()
        {
            english = PlayerPrefs.GetInt("Story.Language", 0) == 1;
            Reload();
            DeathBySeventhsDawn.Knowledge.Investigation.ReloadLanguage();
        }
        public static event Action LanguageChanged;
        public static string Local(string chinese, string english) => English ? english : chinese;
        public static string UI(string key)
        {
            var entries = Current.ui;
            if (entries != null)
                foreach (var entry in entries)
                    if (entry.key == key)
                        return entry.text;
            return key;
        }
        public static void ToggleLanguage()
        {
            english = !english;
            PlayerPrefs.SetInt("Story.Language", english ? 1 : 0);
            PlayerPrefs.Save();
            Reload();
            DeathBySeventhsDawn.Knowledge.Investigation.ReloadLanguage();
            LanguageChanged?.Invoke();
        }
        public static StoryTextData Current
        {
            get
            {
                if (data == null)
                {
                    var asset = Resources.Load<TextAsset>(English ? "StoryTextEnglish" : ResourcePath);
                    if (asset == null)
                    {
                        Debug.LogError("StoryText.json 未找到：Assets/Resources/StoryText.json");
                        data = new StoryTextData();
                    }
                    else
                    {
                        try
                        {
                            data = JsonUtility.FromJson<StoryTextData>(asset.text);
                        }
                        catch (Exception ex) { Debug.LogError("StoryText.json 解析失败：" + ex.Message); }
                        if (data == null)
                            data = new StoryTextData();
                    }
                }
                return data;
            }
        }
        public static void Reload()
        {
            data = null;
        }
        public static string Value(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : value;
        public static string[] Pages(string[] pages, params string[] fallback) => pages != null && pages.Length > 0 ? pages : fallback;
    }

    [Serializable]
    public sealed class StoryTextData
    {
        public MorningText morning;
        public CultText cult;
        public DreamText dream;
        public KnowledgeText knowledge;
        public TelescopeText telescope;
        public FirstLoopText firstLoop;
        public BroadcastText dayOneBroadcast;
        public PerceptionText perception;
        public InvestigationText investigation;
        public AquariumText aquarium;
        public MainMenuText mainMenu;
        public PauseText pause;
        public EndingText ending;
        public EndingText worldEnd;
        public RooftopText rooftop;
        public UiTranslation[] ui;
    }

    [Serializable]
    public sealed class DreamText
    {
        public string title;
        public string[] pages;
        public string[] images;
        public string journalTitle;
        public string journalBody;
        public string fallbackPage;
    }

    [Serializable]
    public sealed class KnowledgeText
    {
        public string ancientTitle;
        public string ancientBody;
        public string windowTitle;
        public string windowBody;
        public string fallbackTitle;
        public string fallbackBodyPrefix;
    }

    [Serializable]
    public sealed class TelescopeText
    {
        public string title;
        public string revelationTitle;
        public string nightOnly;
        public string wrongDay;
        public string gainedSuffix;
        public string repeatSuffix;
        public string imageMissing;
        public string collect;
        public string collected;
        public string observe;
        public string close;
        public string revelationCollect;
        public string revelationCollected;
        public string revelationClose;
    }

    [Serializable]
    public sealed class FirstLoopText
    {
        public string news;
        public string flyer;
        public string dayOneCaption;
        public string startledCaption;
        public string boardingCaption;
        public string chapterTitle;
        public string chapterText;
        public string newsDay1;
        public string newsDefault;
    }

    [Serializable]
    public sealed class BroadcastText
    {
        public string kicker;
        public string kickerLate;
        public string title;
        public string lateFirstPage;
        public string[] pages;
        public string[] images;
        public string boring, narrator;
        public string journalTitle;
        public string journalBody;
        public string continuePrompt;
        public string closePrompt;
        public string liveLabel;
    }

    [Serializable]
    public sealed class PerceptionClueText
    {
        public string id;
        public string title;
        public string body;
    }

    [Serializable]
    public sealed class PerceptionText
    {
        public string prompt;
        public string collect;
        public string collected;
        public string close;
        public PerceptionClueText bedroom;
        public PerceptionClueText corridor;
        public PerceptionClueText balcony;
    }

    [Serializable]
    public sealed class InvestigationEvidenceText
    {
        public string id;
        public string title;
        public string body;
        public string source;
    }

    [Serializable]
    public sealed class InvestigationDeductionText
    {
        public string id;
        public string relation;
        public string title;
        public string body;
    }

    [Serializable]
    public sealed class InvestigationText
    {
        public string[] relations;
        public InvestigationEvidenceText[] evidence;
        public InvestigationDeductionText[] deductions;
    }

    [Serializable]
    public sealed class AquariumText
    {
        public string question, answerGap, answerDefault;
        public string offer, offerPrompt, throwFoil, sealedTitle, sealedBody, offerBefore, sealedBefore, waterPrompt;
        public string promptDay3;
        public string promptDay6;
        public string speakerDay3;
        public string speakerDay6;
        public string[] day3Pages;
        public string[] day5Pages;
        public string[] day6Pages;
    }

    [Serializable]
    public sealed class MainMenuText
    {
        public string title;
        public string subtitle;
        public string tagline;
        public string start;
        public string quit;
        public string hint;
        public string footer;
    }

    [Serializable]
    public sealed class PauseText
    {
        public string title;
        public string subtitle;
        public string resume;
        public string returnToTitle;
        public string quit;
        public string hint;
    }

    [Serializable]
    public sealed class EndingText
    {
        public string title;
        public string[] lines;
        public string summary;
        public string confirmWarning;
        public string restart;
        public string confirm;
        public string cancel;
        public string returnToTitle;
    }
}

[Serializable]
public sealed class UiTranslation
{
    public string key, text;
}
