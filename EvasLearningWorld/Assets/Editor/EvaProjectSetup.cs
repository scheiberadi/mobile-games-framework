using UnityEditor.Android;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class EvaProjectSetup
{
    // Unity.exe -batchmode -quit -projectPath <eva> -executeMethod EvaProjectSetup.Apply
    // The application id is a development id; the permanent package id is chosen right
    // before the first Play Store publish.
    public static void Apply()
    {
        var android = NamedBuildTarget.Android;
        PlayerSettings.companyName = "NoAdsGuy";
        PlayerSettings.productName = "Eva";
        PlayerSettings.SetApplicationIdentifier(android, "com.noadsguy.evas.dev");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.forceInternetPermission = false;
        PlayerSettings.Android.forceSDCardPermission = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.runInBackground = false;
        UseInputSystemOnly();
        ApplyAppIcon();
        // An existing Boot scene is never overwritten (later milestones edit it by hand).
        if (!System.IO.File.Exists("Assets/Scenes/Boot.unity")) EvaSpikeScenes.Create("Assets/Scenes/Boot.unity", null);
        AssetDatabase.SaveAssets();
    }

    // The launcher icon: Eva's face. Android 8+ gets an adaptive icon (the face on a solid yellow background, kept inside the safe
    // zone so round and squircle masks never cut it); older phones get the plain square picture. The sources sit in Assets/Editor/AppIcon.
    private static void ApplyAppIcon()
    {
        const string folder = "Assets/Editor/AppIcon/";
        var legacy = LoadIcon(folder + "icon_legacy.png");
        var foreground = LoadIcon(folder + "icon_adaptive_fg.png");
        var background = LoadIcon(folder + "icon_adaptive_bg.png");
        if (legacy == null || foreground == null || background == null) return;

        var android = NamedBuildTarget.Android;
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { legacy }, IconKind.Any); // the default icon every platform falls back on
        FillIcons(android, AndroidPlatformIconKind.Legacy, legacy, null);
        FillIcons(android, AndroidPlatformIconKind.Round, legacy, null);
        FillIcons(android, AndroidPlatformIconKind.Adaptive, foreground, background);
    }

    private static Texture2D LoadIcon(string path)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) return null;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null && (importer.mipmapEnabled || importer.npotScale != TextureImporterNPOTScale.None || !importer.alphaIsTransparency))
        {
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return texture;
    }

    // Every size of one icon kind gets the same picture (Unity scales it at build time); the adaptive kind has two layers.
    private static void FillIcons(NamedBuildTarget target, PlatformIconKind kind, Texture2D first, Texture2D second)
    {
        var icons = PlayerSettings.GetPlatformIcons(target, kind);
        foreach (var icon in icons)
        {
            icon.SetTexture(first, 0);
            if (second != null) icon.SetTexture(second, 1);
        }
        PlayerSettings.SetPlatformIcons(target, kind, icons);
    }

    // UiFactory builds an InputSystemUIInputModule, which receives no touch while the project is on the old
    // Input Manager only (activeInputHandler 0). 1 = Input System package only.
    private static void UseInputSystemOnly()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var handler = settings.FindProperty("activeInputHandler");
        if (handler.intValue == 1) return;
        handler.intValue = 1;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
