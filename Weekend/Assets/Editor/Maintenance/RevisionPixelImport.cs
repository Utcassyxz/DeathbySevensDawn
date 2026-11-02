using UnityEditor;
using UnityEngine;

// Import at the game's real pixel resolution, not merely a high-resolution painting with Point filtering.
public sealed class RevisionPixelImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith("/FishMapPixels.png") && !assetPath.EndsWith("/TelescopePixelPair.png"))
            return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = assetPath.EndsWith("/FishMapPixels.png") ? 128 : 512;
        var platform = importer.GetDefaultPlatformTextureSettings();
        platform.maxTextureSize = importer.maxTextureSize;
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(platform);
    }

    public override uint GetVersion() => 1;
    public static void Apply()
    {
        foreach (string name in new[]
        {
            "FishMapPixels",
            "TelescopePixelPair"
        }

        )
            AssetDatabase.ImportAsset("Assets/Resources/Story/" + name + ".png", ImportAssetOptions.ForceUpdate);
    }
}
