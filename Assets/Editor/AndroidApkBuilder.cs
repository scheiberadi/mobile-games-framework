using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

public static class AndroidApkBuilder
{
    // Real designed icon (Assets/Branding/ic_launcher_512.png), read as raw bytes rather
    // than via AssetDatabase so this doesn't depend on that file's own import settings -
    // SaveAndSetAndroidIcon re-encodes and configures it for icon use either way.
    private static Texture2D LoadSudokuIcon()
    {
        var bytes = File.ReadAllBytes("Assets/Branding/ic_launcher_512.png");
        var texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        return texture;
    }

    public static void Build()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mobilegamesframework.game01_2048");
        PlayerSettings.productName = "2048";
        IconGenerator.SaveAndSetAndroidIcon(IconGenerator.Generate2048Icon(), "Assets/Icons/icon_2048.png");

        var scenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Game.unity"
        };

        RunBuild(scenes, "Builds/Android/mobile-games-framework.apk");
    }

    public static void BuildSudoku()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mobilegamesframework.game02_sudoku");
        PlayerSettings.productName = "Sudoku";
        IconGenerator.SaveAndSetAndroidIcon(LoadSudokuIcon(), "Assets/Icons/icon_sudoku.png");

        var scenes = new[]
        {
            "Assets/Scenes/SudokuMenu.unity",
            "Assets/Scenes/Sudoku.unity",
            "Assets/Scenes/SudokuSettings.unity",
            "Assets/Scenes/SudokuHighScores.unity"
        };

        RunBuild(scenes, "Builds/Android/mobile-games-framework-sudoku.apk");
    }

    public static void BuildSudokuRelease()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.noadsguy.sudoku");
        PlayerSettings.productName = "NoAdsGuy's Sudoku";
        IconGenerator.SaveAndSetAndroidIcon(LoadSudokuIcon(), "Assets/Icons/icon_sudoku.png");

        var scenes = new[]
        {
            "Assets/Scenes/SudokuMenu.unity",
            "Assets/Scenes/Sudoku.unity",
            "Assets/Scenes/SudokuSettings.unity",
            "Assets/Scenes/SudokuHighScores.unity"
        };

        RunReleaseBuild(scenes, "Builds/Android/mobile-games-framework-sudoku-release.aab");
    }

    private static void RunReleaseBuild(string[] scenes, string locationPathName)
    {
        PlayerSettings.Android.requestedVisibleInsets = AndroidWindowInsetsType.StatusBars | AndroidWindowInsetsType.NavigationBars;

        var keystorePath = System.Environment.GetEnvironmentVariable("SUDOKU_KEYSTORE_PATH");
        var keystorePass = System.Environment.GetEnvironmentVariable("SUDOKU_KEYSTORE_PASS");
        var keyAlias = System.Environment.GetEnvironmentVariable("SUDOKU_KEY_ALIAS");
        var keyAliasPass = System.Environment.GetEnvironmentVariable("SUDOKU_KEY_ALIAS_PASS");

        if (string.IsNullOrEmpty(keystorePath))
        {
            UnityEngine.Debug.LogError("BUILD_RESULT: Failed - SUDOKU_KEYSTORE_PATH not set");
            EditorApplication.Exit(1);
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = keyAlias;
        PlayerSettings.Android.keyaliasPass = keyAliasPass;
        PlayerSettings.Android.bundleVersionCode += 1;

        var previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        EditorUserBuildSettings.buildAppBundle = true;

        try
        {
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
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
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            PlayerSettings.Android.useCustomKeystore = false;
        }
    }

    private static void RunBuild(string[] scenes, string locationPathName)
    {
        PlayerSettings.Android.requestedVisibleInsets = AndroidWindowInsetsType.StatusBars | AndroidWindowInsetsType.NavigationBars;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPathName,
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
