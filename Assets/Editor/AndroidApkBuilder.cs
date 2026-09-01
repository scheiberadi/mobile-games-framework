using UnityEditor;

public static class AndroidApkBuilder
{
    public static void Build()
    {
        var scenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Game.unity"
        };

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Android/mobile-games-framework.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"BUILD_RESULT: {report.summary.result}");
        UnityEngine.Debug.Log($"BUILD_TOTAL_ERRORS: {report.summary.totalErrors}");
        UnityEngine.Debug.Log($"BUILD_TOTAL_WARNINGS: {report.summary.totalWarnings}");

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
