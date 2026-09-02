using System.IO;
using UnityEditor;
using UnityEngine;

public static class StoreAssetGenerator
{
    private static readonly Color BackgroundTop = new Color(0.75f, 0.85f, 0.97f);
    private static readonly Color BackgroundBottom = new Color(0.98f, 0.98f, 1f);

    public static void GenerateAll()
    {
        GenerateHiResIcon();
        GenerateFeatureGraphic();
        Debug.Log("STORE_ASSETS_DONE");
    }

    private static void GenerateHiResIcon()
    {
        var source = IconGenerator.GenerateSudokuIcon();
        var opaque = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        for (var y = 0; y < source.height; y++)
        for (var x = 0; x < source.width; x++)
        {
            var pixel = source.GetPixel(x, y);
            opaque.SetPixel(x, y, Color.Lerp(BackgroundTop, pixel, pixel.a));
        }
        opaque.Apply();

        SavePng(Resize(opaque, 512, 512), "docs/store-assets/icon-512.png");
    }

    private static void GenerateFeatureGraphic()
    {
        const int width = 1024;
        const int height = 500;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        for (var y = 0; y < height; y++)
        {
            var color = Color.Lerp(BackgroundBottom, BackgroundTop, (float)y / (height - 1));
            for (var x = 0; x < width; x++)
                texture.SetPixel(x, y, color);
        }

        const int iconSize = 400;
        var icon = Resize(IconGenerator.GenerateSudokuIcon(), iconSize, iconSize);
        var offsetX = (width - iconSize) / 2;
        var offsetY = (height - iconSize) / 2;
        for (var y = 0; y < iconSize; y++)
        for (var x = 0; x < iconSize; x++)
        {
            var pixel = icon.GetPixel(x, y);
            if (pixel.a < 0.01f) continue;
            var destX = offsetX + x;
            var destY = offsetY + y;
            texture.SetPixel(destX, destY, Color.Lerp(texture.GetPixel(destX, destY), pixel, pixel.a));
        }
        texture.Apply();

        SavePng(texture, "docs/store-assets/feature-graphic-1024x500.png");
    }

    private static Texture2D Resize(Texture2D source, int width, int height)
    {
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var srcX = x * source.width / width;
            var srcY = y * source.height / height;
            result.SetPixel(x, y, source.GetPixel(srcX, srcY));
        }
        result.Apply();
        return result;
    }

    private static void SavePng(Texture2D texture, string relativePath)
    {
        var fullPath = Path.Combine(Application.dataPath, "..", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        Debug.Log($"STORE_ASSET_SAVED: {relativePath}");
    }
}
