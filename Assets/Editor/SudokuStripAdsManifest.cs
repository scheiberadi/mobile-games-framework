using System.IO;
using System.IO.Compression;
using System.Linq;
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

        // Each step is independent - the androidlib manifest only exists when the
        // GoogleMobileAds package is still installed, but unity-classes.jar always exists
        // regardless, so an early-out from the first must never skip the second.
        var manifestPath = Path.Combine(path, "GoogleMobileAdsPlugin.androidlib", "AndroidManifest.xml");
        if (File.Exists(manifestPath))
        {
            var contents = File.ReadAllText(manifestPath);
            var stripped = Regex.Replace(
                contents,
                @"^.*com\.google\.android\.gms\.ads\.APPLICATION_ID.*(?:\r\n|\r|\n|$)",
                "",
                RegexOptions.Multiline);

            if (stripped != contents)
                File.WriteAllText(manifestPath, stripped);
        }

        StripAdvertisingIdHelperFromUnityClasses(path);
    }

    // com.unity3d.player.AndroidAdvertisingIdHelper and .FirebaseIdentifiersHelper are not
    // from any package or app code at all - they're precompiled into unity-classes.jar,
    // which Unity's own Android Player module copies from its own installation into every
    // exported project unconditionally, as part of its core player support library. This is
    // why removing GoogleMobileAds, Unity Analytics, and the unused Unity.Purchasing asmdef
    // reference (all confirmed via DEX/native-binary string scans to have zero footprint
    // afterward) never touched this: Play kept rejecting Sudoku ("Device Or Other IDs") on
    // version codes from before any of that work and after all of it, identically, because
    // this jar is the same regardless. Neither class is reachable from Sudoku's own code
    // (Sudoku never calls Application.RequestAdvertisingIdentifierAsync or
    // UnityEngine.Analytics.*), so removing their two .class entries directly from the
    // jar - a zip archive - after Unity exports the Gradle project is safe for Sudoku
    // specifically; 2048's build never sets StripForSudoku, so it is unaffected.
    private static void StripAdvertisingIdHelperFromUnityClasses(string unityLibraryPath)
    {
        var jarPath = Path.Combine(unityLibraryPath, "libs", "unity-classes.jar");
        UnityEngine.Debug.Log($"[SudokuStripAdsManifest] jarPath={jarPath} exists={File.Exists(jarPath)}");
        if (!File.Exists(jarPath)) return;

        // FirebaseIdentifiersHelper has one obfuscated nest member (found by checking every
        // class in the jar for a NestHost attribute pointing at either target class, via
        // javap -v; only this one exists). D8 refuses to dex a class whose NestHost isn't on
        // the classpath, so removing FirebaseIdentifiersHelper.class without also removing
        // its nest member fails the build with "Class r requires its nest host
        // FirebaseIdentifiersHelper to be on program or class path." AndroidAdvertisingIdHelper
        // has no nest members. If Unity ever changes which obfuscated name this is, re-run:
        // for f in com/unity3d/player/*.class; do javap -v "$f" | grep NestHost; done
        // over an extracted copy of unity-classes.jar and look for AndroidAdvertisingIdHelper
        // or FirebaseIdentifiersHelper as the NestHost.
        var entriesToRemove = new System.Collections.Generic.HashSet<string>
        {
            "com/unity3d/player/AndroidAdvertisingIdHelper.class",
            "com/unity3d/player/FirebaseIdentifiersHelper.class",
            "com/unity3d/player/r.class"
        };

        // ZipArchiveMode.Update patches the existing archive's bytes in place rather than
        // rewriting it, and that corrupted a DIFFERENT entry's local header the first time
        // this was tried here (Gradle's Jetifier then failed the whole build with
        // "java.util.zip.ZipException - invalid entry size"). Copying every surviving entry
        // into a fresh archive avoids that in-place-patching bug entirely.
        var tempPath = jarPath + ".stripped";
        try
        {
            using (var source = ZipFile.OpenRead(jarPath))
            using (var destStream = new FileStream(tempPath, FileMode.Create))
            using (var dest = new ZipArchive(destStream, ZipArchiveMode.Create))
            {
                foreach (var entry in source.Entries)
                {
                    if (entriesToRemove.Contains(entry.FullName))
                    {
                        UnityEngine.Debug.Log($"[SudokuStripAdsManifest] dropping entry={entry.FullName}");
                        continue;
                    }

                    var newEntry = dest.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                    newEntry.LastWriteTime = entry.LastWriteTime;
                    using var entrySource = entry.Open();
                    using var entryDest = newEntry.Open();
                    entrySource.CopyTo(entryDest);
                }
            }

            File.Delete(jarPath);
            File.Move(tempPath, jarPath);
            UnityEngine.Debug.Log("[SudokuStripAdsManifest] jar rebuilt without exception");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[SudokuStripAdsManifest] FAILED to strip unity-classes.jar: {e}");
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
    }
}
