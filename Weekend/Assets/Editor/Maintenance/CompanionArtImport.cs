using UnityEditor;
using UnityEngine;

public sealed class CompanionArtImport : AssetPostprocessor
{
    public static void Apply()
    {
        foreach (string name in new[]
        {
            "FishDialogueInk",
            "BubbleDialogueInk",
            "EndingDistortedPair"
        }

        )
            AssetDatabase.ImportAsset("Assets/Resources/Story/" + name + ".png", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith("/FishDialogueInk.png") && !assetPath.EndsWith("/BubbleDialogueInk.png") && !assetPath.EndsWith("/EndingDistortedPair.png"))
            return;
        var i = (TextureImporter)assetImporter;
        i.textureType = TextureImporterType.Default;
        i.textureShape = TextureImporterShape.Texture2D;
        i.mipmapEnabled = false;
        i.filterMode = FilterMode.Point;
        i.npotScale = TextureImporterNPOTScale.None;
        i.alphaIsTransparency = true;
        i.textureCompression = TextureImporterCompression.Uncompressed;
        i.maxTextureSize = assetPath.EndsWith("/EndingDistortedPair.png") ? 1024 : 512;
        var p = i.GetDefaultPlatformTextureSettings();
        p.maxTextureSize = i.maxTextureSize;
        p.textureCompression = TextureImporterCompression.Uncompressed;
        i.SetPlatformTextureSettings(p);
    }
}
