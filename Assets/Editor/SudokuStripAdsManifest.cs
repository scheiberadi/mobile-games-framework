using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;

// Sudoku has no ads, but GoogleMobileAds.Editor.ManifestProcessor (part of the
// project-wide com.google.ads.mobile package, kept installed for 2048) unconditionally
// writes a real com.google.android.gms.ads.APPLICATION_ID meta-data into
// Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib/AndroidManifest.xml on every
// Android build - that file is a physical, project-wide folder Unity always includes as
// an androidlib module, so it isn't reachable by the Gradle-dependency excludes in
// SudokuNoAdsMainTemplate.gradle.txt (those only affect Maven artifact resolution, not a
// locally-included library module's own manifest). Play Console's automated compliance
// scan keys off that exact meta-data key as proof the app bundles AdMob, and rejected a
// Sudoku release ("Invalid data safety form ... Device Or Other IDs") over it even though
// Sudoku's own ad code (AdMobAdProvider, gated by SudokuController.AdsEnabled = false) is
// never invoked.
//
// This strips just that one meta-data line from the EXPORTED (Library/Bee/.../Gradle)
// copy of the androidlib's manifest, post-export but pre-Gradle-build, via the same hook
// GoogleMobileAds' own GradleProcessor uses. It never touches the source file under
// Assets/, so 2048's build (where StripForSudoku is never set) is unaffected. Scoped to
// Sudoku only via AndroidApkBuilder.StripAdsManifestForSudoku, set/reset around
// BuildPipeline.BuildPlayer the same way the other release-only settings are.
public class SudokuStripAdsManifest : IPostGenerateGradleAndroidProject
{
    public static bool StripForSudoku;

    // Must run after GoogleMobileAds.Editor.GradleProcessor (callbackOrder 0) has written
    // its own edits, so there is something to strip.
    public int callbackOrder => 10;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (!StripForSudoku) return;

        var manifestPath = Path.Combine(path, "GoogleMobileAdsPlugin.androidlib", "AndroidManifest.xml");
        if (!File.Exists(manifestPath)) return;

        var contents = File.ReadAllText(manifestPath);
        var stripped = Regex.Replace(
            contents,
            @"^.*com\.google\.android\.gms\.ads\.APPLICATION_ID.*(?:\r\n|\r|\n|$)",
            "",
            RegexOptions.Multiline);

        if (stripped != contents)
            File.WriteAllText(manifestPath, stripped);
    }
}
