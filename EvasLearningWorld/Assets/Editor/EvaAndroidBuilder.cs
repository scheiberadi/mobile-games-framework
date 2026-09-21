using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class EvaAndroidBuilder
{
    // Unity.exe -batchmode -quit -projectPath <eva> -executeMethod EvaAndroidBuilder.BuildDebug
    public static void BuildDebug()
    {
        EvaProjectSetup.Apply();
        var sceneList = Environment.GetEnvironmentVariable("EVA_SCENES");
        var scenes = string.IsNullOrEmpty(sceneList)
            ? new[] { "Assets/Scenes/Boot.unity" }
            : sceneList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        EditorUserBuildSettings.buildAppBundle = false; // a stuck AAB flag would make this "APK" an AAB
        Directory.CreateDirectory("Builds/Android");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Android/eva-debug.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log("BUILD_RESULT: " + report.summary.result);
        Debug.Log("BUILD_TOTAL_ERRORS: " + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
