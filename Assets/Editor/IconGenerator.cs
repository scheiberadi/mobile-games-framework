using System.IO;
using UnityEditor;
using UnityEngine;

public static class IconGenerator
{
    private const int Size = 1024;
    private const int OuterRadius = 200;

    public static Texture2D Generate2048Icon()
    {
        var texture = NewTransparentTexture();
        DrawRoundedSquare(texture, new Color(0.98f, 0.85f, 0.35f));

        var tileColors = new[]
        {
            new Color(0.95f, 0.69f, 0.47f),
            new Color(0.96f, 0.37f, 0.23f),
            new Color(0.93f, 0.88f, 0.78f),
            new Color(0.93f, 0.76f, 0.18f),
        };
        DrawTileGrid(texture, tileColors);

        texture.Apply();
        return texture;
    }

    public static Texture2D GenerateSudokuIcon()
    {
        var texture = NewTransparentTexture();
        DrawRoundedSquare(texture, new Color(0.75f, 0.85f, 0.97f));
        DrawSudokuGrid(texture);
        texture.Apply();
        return texture;
    }

    public static void SaveAndSetAndroidIcon(Texture2D texture, string assetRelativePath)
    {
        var fullPath = Path.Combine(Application.dataPath, "..", assetRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());

        AssetDatabase.ImportAsset(assetRelativePath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetRelativePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(assetRelativePath);
        var requiredSlots = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android).Length;
        var icons = new Texture2D[requiredSlots];
        for (var i = 0; i < requiredSlots; i++) icons[i] = imported;
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
    }

    private static Texture2D NewTransparentTexture()
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
            texture.SetPixel(x, y, Color.clear);
        return texture;
    }

    private static void DrawRoundedSquare(Texture2D texture, Color color)
    {
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
            if (IsInsideRoundedRect(x, y, Size, OuterRadius))
                texture.SetPixel(x, y, color);
    }

    private static void DrawTileGrid(Texture2D texture, Color[] tileColors)
    {
        const int gridSize = 2;
        const int margin = 140;
        const int gap = 40;
        var cell = (Size - margin * 2 - gap * (gridSize - 1)) / gridSize;

        for (var row = 0; row < gridSize; row++)
        for (var col = 0; col < gridSize; col++)
        {
            var x0 = margin + col * (cell + gap);
            var y0 = margin + row * (cell + gap);
            var color = tileColors[row * gridSize + col];

            for (var y = y0; y < y0 + cell; y++)
            for (var x = x0; x < x0 + cell; x++)
                if (IsInsideRoundedRect(x - x0, y - y0, cell, cell / 6))
                    texture.SetPixel(x, y, color);
        }
    }

    private static void DrawSudokuGrid(Texture2D texture)
    {
        const int gridSize = 3;
        const int margin = 160;
        const int lineWidth = 14;
        var cell = (Size - margin * 2) / gridSize;
        var cellColor = new Color(0.98f, 0.98f, 1f);
        var lineColor = new Color(0.15f, 0.25f, 0.45f);
        var filledColor = new Color(0.20f, 0.45f, 0.85f);
        var filledCells = new[] { (0, 0), (1, 1), (2, 0) };

        for (var row = 0; row < gridSize; row++)
        for (var col = 0; col < gridSize; col++)
        {
            var x0 = margin + col * cell;
            var y0 = margin + row * cell;
            var isFilled = System.Array.IndexOf(filledCells, (row, col)) >= 0;
            var fillColor = isFilled ? filledColor : cellColor;

            for (var y = y0; y < y0 + cell; y++)
            for (var x = x0; x < x0 + cell; x++)
                texture.SetPixel(x, y, fillColor);
        }

        for (var i = 0; i <= gridSize; i++)
        {
            var offset = margin + i * cell;
            for (var w = -lineWidth / 2; w < lineWidth / 2; w++)
            {
                for (var y = margin; y < margin + cell * gridSize; y++)
                    SetPixelSafe(texture, offset + w, y, lineColor);
                for (var x = margin; x < margin + cell * gridSize; x++)
                    SetPixelSafe(texture, x, offset + w, lineColor);
            }
        }
    }

    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size) return;
        texture.SetPixel(x, y, color);
    }

    private static bool IsInsideRoundedRect(int x, int y, int size, int radius)
    {
        var nearestX = Mathf.Clamp(x, radius, size - radius - 1);
        var nearestY = Mathf.Clamp(y, radius, size - radius - 1);
        var dx = x - nearestX;
        var dy = y - nearestY;
        return dx * dx + dy * dy <= radius * radius;
    }
}
