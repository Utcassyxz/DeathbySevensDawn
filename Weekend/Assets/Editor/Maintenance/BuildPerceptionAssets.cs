// 职责：场景维护工具：BuildPerceptionAssets；仅显式运行，日常开发无需重建已有资源。
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
public static class BuildPerceptionAssets
{
    [MenuItem("Prototype/Prepare perception and music assets")]
    public static void Run()
    {
        const string path = "Assets/Resources/ApartmentMix.mixer";
        if (AssetDatabase.LoadAssetAtPath<AudioMixer>(path) == null)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(t => t != null);
            var create = type?.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (create == null)
                throw new InvalidOperationException("Unity mixer factory is unavailable.");
            create.Invoke(null, new object[] { path });
        }
        foreach (string name in new[] { "ApartmentHome", "BehindTheFilter", "Revelation" })
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/Resources/Audio/" + name + ".wav");
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }
        var image = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Story/AncientGodRealistic.png");
        image.textureType = TextureImporterType.Default;
        image.mipmapEnabled = false;
        image.filterMode = FilterMode.Bilinear;
        image.maxTextureSize = 2048;
        image.SaveAndReimport();
        var story = Resources.Load<DeathBySeventhsDawn.Interaction.StoryContent>("StoryContent");
        story.ancientGodImageResource = "Story/AncientGodRealistic";
        EditorUtility.SetDirty(story);
        AssetDatabase.SaveAssets();
        Debug.Log("PERCEPTION_ASSETS_READY");
    }
    public static void PrepareAndTest()
    {
        Run();
        VerifyPerception.Run();
    }
}
