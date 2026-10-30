// 职责：编辑器回归检查：Expansion；修改功能后从 Prototype 菜单运行。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DeathBySeventhsDawn.Story;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
[InitializeOnLoad]
public static class VerifyExpansion
{
    static VerifyExpansion()
    {
        EditorApplication.delayCall += Auto;
    }
    static void Auto()
    {
        if (File.Exists("../expansion-request.txt") && !EditorApplication.isPlaying)
        {
            File.Delete("../expansion-request.txt");
            Run();
        }
    }
    static void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
    }
    [MenuItem("Prototype/Validate bilingual expansion and opening")]
    public static void Run()
    {
        bool original = StoryText.English;
        try
        {
            if (original)
                StoryText.ToggleLanguage();
            var zh = StoryText.Current;
            var cat = Investigation.Catalog;
            Check(cat.evidence.Length == 17 && cat.deductions.Length == 9, "Expanded catalog counts");
            Check(cat.evidence.Select(x => x.id).Distinct().Count() == 17, "Unique evidence IDs");
            foreach (var d in cat.deductions)
            {
                Check(Investigation.IsNode(d.left) && Investigation.IsNode(d.right), "Valid rule references");
                Check(!string.IsNullOrEmpty(d.relation), "Stable relation key");
            }
            StoryText.ToggleLanguage();
            var en = StoryText.Current;
            Check(en.dream.pages.Length == 4 && en.dayOneBroadcast.pages.Length == 4, "Localized story page counts");
            Check(en.investigation.evidence.Length == 17 && en.investigation.deductions.Length == 9, "English catalog parity");
            Check(Investigation.Evidence("notes-gap").title == "Missing pages", "Catalog refreshes language");
            Check(en.aquarium.day6Pages.All(x => !string.IsNullOrEmpty(x)), "English NPC dialogue");
            Check(en.mainMenu.start != zh.mainMenu.start, "Menu language switches");
            Check(Investigation.Catalog.deductions[0].relation == zh.investigation.deductions[0].relation, "Language-independent save relations");
            using (var sky = new OpeningSky())
            {
                var first = sky.Render(1, 0);
                Check(first.width == 480 && first.filterMode == FilterMode.Point, "Pixel rendering");
                File.WriteAllBytes("../opening-idle.png", first.EncodeToPNG());
                var before = first.GetPixels32();
                var later = sky.Render(8, .72f);
                var after = later.GetPixels32();
                Check(before.Where((c, i) => !c.Equals(after[i])).Count() > 500, "Opening visibly evolves");
                File.WriteAllBytes("../opening-corruption.png", later.EncodeToPNG());
                var end = sky.Render(12, 1).GetPixels32();
                Check(end.All(c => c.r == 0 && c.g == 0 && c.b == 0), "Opening fades to black");
            }
            File.WriteAllText("../expansion-validation.txt", "PASS: 16 evidence, 9 rules; valid references; Chinese/English resource parity; live catalog refresh; stable save keys; pixel idle / corruption / fade.\n");
            Debug.Log("EXPANSION_VALIDATION_PASS");
        }
        catch (Exception e) { File.WriteAllText("../expansion-validation.txt", "FAIL: " + e); Debug.LogException(e); }
        finally { if (StoryText.English != original) StoryText.ToggleLanguage(); }
    }
}
