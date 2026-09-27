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
        // An existing Boot scene is never overwritten (later milestones edit it by hand).
        if (!System.IO.File.Exists("Assets/Scenes/Boot.unity")) EvaSpikeScenes.Create("Assets/Scenes/Boot.unity", null);
        AssetDatabase.SaveAssets();
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
