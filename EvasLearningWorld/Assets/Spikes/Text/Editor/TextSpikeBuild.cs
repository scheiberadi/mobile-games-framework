using System.IO;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Disposable spike builder: imports TMP essentials, makes dynamic Noto font assets and the text scene.
public static class TextSpikeBuild
{
    private const string ScenePath = "Assets/Spikes/Text/TextSpike.unity";

    // Unity.exe -batchmode -projectPath <eva> -executeMethod TextSpikeBuild.ImportEssentials
    public static void ImportEssentials()
    {
        // ImportPackage is queued, so this must run WITHOUT -quit and exit from the completion callback.
        AssetDatabase.importPackageCompleted += name =>
        {
            Debug.Log("TEXT_ESSENTIALS_IMPORTED " + name);
            EditorApplication.Exit(0);
        };
        AssetDatabase.importPackageFailed += (name, error) =>
        {
            Debug.Log("TEXT_ESSENTIALS_FAILED " + name + " " + error);
            EditorApplication.Exit(1);
        };
        TMP_PackageResourceImporter.ImportResources(true, false, false);
    }

    // Unity.exe -batchmode -quit -projectPath <eva> -executeMethod TextSpikeBuild.CreateFontsAndScene
    public static void CreateFontsAndScene()
    {
        var baseAsset = MakeAsset("NotoSans-Variable");
        var jp = MakeAsset("NotoSansCJKjp-Regular");
        var kr = MakeAsset("NotoSansCJKkr-Regular");
        var sc = MakeAsset("NotoSansCJKsc-Regular");
        baseAsset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { jp, kr, sc };
        EditorUtility.SetDirty(baseAsset);
        AssetDatabase.SaveAssets();

        EvaSpikeScenes.Create(ScenePath, typeof(TextSpike));
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var spike = Object.FindFirstObjectByType<TextSpike>();
        spike.baseFont = baseAsset;
        EditorUtility.SetDirty(spike);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("TEXT_SCENE_CREATED " + ScenePath);
    }

    private static string FontPath(string fontName)
    {
        return "Assets/Fonts/" + fontName + (fontName.EndsWith("-Regular") ? ".otf" : ".ttf");
    }

    private static TMP_FontAsset MakeAsset(string fontName)
    {
        var fontPath = FontPath(fontName);
        var assetPath = "Assets/Fonts/" + fontName + " SDF.asset";
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null) throw new FileNotFoundException(fontPath);
        AssetDatabase.DeleteAsset(assetPath);

        var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        asset.name = fontName + " SDF";
        AssetDatabase.CreateAsset(asset, assetPath);
        asset.material.name = fontName + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var texture in asset.atlasTextures)
        {
            texture.name = fontName + " Atlas";
            AssetDatabase.AddObjectToAsset(texture, asset);
        }
        EditorUtility.SetDirty(asset);
        Debug.Log("TEXT_FONT_ASSET " + assetPath + " mode=" + asset.atlasPopulationMode);
        return asset;
    }
}
