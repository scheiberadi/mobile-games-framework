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

        WithSudokuNoAdsGradleTemplate(() => RunBuild(scenes, "Builds/Android/mobile-games-framework-sudoku.apk"));
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

        WithSudokuNoAdsGradleTemplate(() => RunReleaseBuild(scenes, "Builds/Android/mobile-games-framework-sudoku-release.aab"));
    }

    // Sudoku has no ads/IAP, but the GoogleMobileAds SDK stays linked project-wide so
    // 2048's ads keep working - its play-services-ads AAR unconditionally declares the
    // AD_ID permission, which Play Console flags as an incomplete/inconsistent
    // advertising-ID declaration since Sudoku truthfully says it doesn't use advertising
    // ID. Excluding the dependency at the Gradle level (verified via aapt2 dump badging
    // and an on-device install: AD_ID gone, launcher activity/icon intact) keeps the AAR,
    // and the permission it brings, out of Sudoku's build only - the template is copied
    // in and deleted around just this build, so Build() (2048) never sees it.
    //
    // This is NOT the same mechanism as the old Assets/Plugins/Android/AndroidManifest.xml
    // override that briefly shipped and broke the launcher icon - that file sits in the
    // "Main Manifest" slot, which made Gradle regenerate the unityLibrary module without
    // its launcher activity (a module-generation issue, confirmed by investigation, not a
    // mergeable one). mainTemplate.gradle is a normal, supported per-project Gradle
    // customization point and doesn't touch manifest merging at all.
    private static void WithSudokuNoAdsGradleTemplate(System.Action build)
    {
        const string templatePath = "Assets/Plugins/Android/mainTemplate.gradle";
        File.Copy("Assets/Editor/SudokuNoAdsMainTemplate.gradle.txt", templatePath, true);
        AssetDatabase.ImportAsset(templatePath, ImportAssetOptions.ForceUpdate);

        try
        {
            build();
        }
        finally
        {
            AssetDatabase.DeleteAsset(templatePath);
        }
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
