using UnityEditor;
using UnityEngine;

// Every texture generated into Assets/Eva/Resources/Art/ must import as a single sprite, uncompressed,
// no mipmaps, with alpha as transparency and 100 pixels per unit, so it matches the framework's UI scale.
// Runs on first import (and on reimport) because it edits the .meta before Unity generates the asset.
public sealed class EvaArtImporter : AssetPostprocessor
{
    private const string ArtRoot = "Assets/Eva/Resources/Art/";

    // Bump to make Unity reimport every art texture with the new settings.
    public override uint GetVersion() => 3;

    // Uncompressed RGBA made the APK ~880 MB. Android builds use ASTC (6x6 for full-screen scenery, 5x5 for everything else).
    // The game tile icons are 1024 px squares shown at a fraction of that; half the size looks the same on a phone.
    private const string IconFolder = ArtRoot + "activities/";
    private static string[] SceneryFolders = { ArtRoot + "world/", ArtRoot + "house/" };

    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').StartsWith(ArtRoot)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100f;

        var scenery = System.Array.Exists(SceneryFolders, f => assetPath.StartsWith(f));
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = assetPath.StartsWith(IconFolder) ? 512 : 2048,
            format = scenery ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_5x5,
            compressionQuality = 100,
        });
    }
}
