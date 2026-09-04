using System.IO;
using UnityEditor;
using UnityEditor.Android;

public static class AndroidApkBuilder
{
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
        IconGenerator.SaveAndSetAndroidIcon(IconGenerator.GenerateSudokuIcon(), "Assets/Icons/icon_sudoku.png");

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
        IconGenerator.SaveAndSetAndroidIcon(IconGenerator.GenerateSudokuIcon(), "Assets/Icons/icon_sudoku.png");

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

        // Ads are off for this release, but the GoogleMobileAds SDK stays linked so ads
        // can be re-enabled later - its own manifest unconditionally requests the
        // advertising ID permission, which Play flags since nothing here actually reads
        // it. Unity auto-merges any Assets/Plugins/Android/AndroidManifest.xml into the
        // build, so writing (and deleting) it only around this method scopes the removal
        // to this build - the 2048 build, which genuinely uses ads, keeps that permission.
        const string manifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllText(manifestPath,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" xmlns:tools=\"http://schemas.android.com/tools\">\n" +
            "    <uses-permission android:name=\"com.google.android.gms.permission.AD_ID\" tools:node=\"remove\" />\n" +
            "    <uses-permission android:name=\"android.permission.ACCESS_ADSERVICES_AD_ID\" tools:node=\"remove\" />\n" +
            "    <uses-permission android:name=\"android.permission.ACCESS_ADSERVICES_ATTRIBUTION\" tools:node=\"remove\" />\n" +
            "    <uses-permission android:name=\"android.permission.ACCESS_ADSERVICES_TOPICS\" tools:node=\"remove\" />\n" +
            "</manifest>\n");
        AssetDatabase.ImportAsset(manifestPath, ImportAssetOptions.ForceUpdate);

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
            AssetDatabase.DeleteAsset(manifestPath);
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
