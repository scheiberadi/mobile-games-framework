using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Disposable spike builder: makes the Play Asset Delivery scene and a debug-signed app bundle.
public static class PadSpikeBuild
{
    private const string ScenePath = "Assets/Spikes/Pad/PadSpike.unity";

    public static void CreateScene()
    {
        EvaSpikeScenes.Create(ScenePath, typeof(PadSpike));
        AssetDatabase.SaveAssets();
        Debug.Log("PAD_SCENE_CREATED " + ScenePath);
    }

    // Unity.exe -batchmode -quit -projectPath <eva> -executeMethod PadSpikeBuild.BuildAab
    public static void BuildAab()
    {
        EvaProjectSetup.Apply();
        CreateScene();

        PlayerSettings.Android.splitApplicationBinary = false;
        EditorUserBuildSettings.buildAppBundle = true;
        Directory.CreateDirectory("Builds/Android");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Android/pad-spike.aab",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        EditorUserBuildSettings.buildAppBundle = false;
        Debug.Log("PAD_BUILD_RESULT: " + report.summary.result);
        Debug.Log("PAD_BUILD_TOTAL_ERRORS: " + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
