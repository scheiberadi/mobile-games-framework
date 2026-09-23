using UnityEditor;
using UnityEngine;

// Every texture generated into Assets/Eva/Resources/Art/ must import as a single sprite, uncompressed,
// no mipmaps, with alpha as transparency and 100 pixels per unit, so it matches the framework's UI scale.
// Runs on first import (and on reimport) because it edits the .meta before Unity generates the asset.
public sealed class EvaArtImporter : AssetPostprocessor
{
    private const string ArtRoot = "Assets/Eva/Resources/Art/";

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
    }
}
