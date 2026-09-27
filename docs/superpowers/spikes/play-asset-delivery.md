# Spike: Play Asset Delivery for the optional language voice pack

Date: 2026-09-21. Unity 6000.5.10f1, Samsung Galaxy S25 Ultra (SM-S938B, Android 16 / API 36).

Question: can English always be local while an optional language pack is delivered by Google Play,
with a clean English fallback when the pack is not obtained?

## Verdict

PASS, with Unity's own Android asset packs. No extra Unity package, no Google plugin and no
External Dependency Manager are needed, so the compliance test was left untouched.

Mechanism chosen: option 1 of the brief, Unity-native asset packs. A directory whose name ends in
`.androidpack` becomes a Play asset pack in the app bundle; Unity generates the Gradle module for it
and links `com.google.android.play:asset-delivery:2.3.0` into `unityLibrary`. Options 2 and 3
(Google's Play Asset Delivery Unity plugin, or a hand-written Gradle module driven by
`AndroidJavaObject` calls) were not needed and were not attempted.

### Recipe

1. Author the pack as a plain folder in the Unity project. The folder name minus `.androidpack` is
   the pack name; it must start with a letter and contain only English letters, digits or
   underscores, and must be unique.

   ```
   Assets/Spikes/Pad/voice_ro.androidpack/src/main/assets/voice_ro.txt
   ```

   Unity does not import anything under `.androidpack`, so the files never become Unity assets.

2. Delivery type. The default is on-demand, which is what a language pack wants, and Unity writes
   the module's Gradle file itself:

   ```gradle
   apply plugin: 'com.android.asset-pack'

   assetPack {
       packName = "voice_ro"
       dynamicDelivery {
           deliveryType = "on-demand"
       }
   }
   ```

   To pick `fast-follow` or `install-time` instead, put a `build.gradle` with that content in the
   `.androidpack` root. Nothing else in the project needs a Gradle template.

3. Build an app bundle. `.androidpack` folders are silently ignored in an APK build; Unity logs
   "Directories ending with .androidpack are present in the project, however they will not be packed
   into the final application" unless Build App Bundle is on.

   ```csharp
   EditorUserBuildSettings.buildAppBundle = true;
   BuildPipeline.BuildPlayer(new BuildPlayerOptions {
       scenes = new[] { "Assets/Spikes/Pad/PadSpike.unity" },
       locationPathName = "Builds/Android/pad-spike.aab",
       target = BuildTarget.Android,
       options = BuildOptions.None
   });
   ```

   Split Application Binary is not required and stayed off. IL2CPP, ARM64 and min SDK 26 are the
   project's existing settings.

4. Read it at runtime. `com.unity.modules.androidjni` must be in the package manifest; it already is.

   ```csharp
   using UnityEngine.Android;

   var op = AndroidAssetPacks.DownloadAssetPackAsync(new[] { "voice_ro" });
   while (!op.isDone) yield return null;            // op.progress, op.downloadedAssetPacks,
                                                    // op.downloadFailedAssetPacks
   var path = AndroidAssetPacks.GetAssetPackPath("voice_ro");
   // empty when the pack is not on the device; otherwise a real directory
   foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) { ... }
   ```

   `System.IO` works on the returned path for an on-demand pack, so `File.ReadAllBytes` is enough and
   Android's `AssetManager` is not needed. The two-argument `DownloadAssetPackAsync(names, callback)`
   returns `void`, not an operation, so use the one-argument form when you want to await it.

5. Test on device with bundletool local testing (bundletool 1.17.2 ships with the editor).

   ```bash
   source tools/env.sh
   JAVA="$UNITY_DIR/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe"
   BT="$UNITY_DIR/Data/PlaybackEngines/AndroidPlayer/Tools/bundletool-all-1.17.2.jar"
   "$JAVA" -jar "$BT" build-apks --bundle=EvasLearningWorld/Builds/Android/pad-spike.aab \
       --output=EvasLearningWorld/Builds/Android/pad-spike.apks \
       --local-testing --connected-device --adb="$ADB" --overwrite
   "$JAVA" -jar "$BT" install-apks --apks=EvasLearningWorld/Builds/Android/pad-spike.apks --adb="$ADB"
   "$ADB" -s "$DEVICE" shell monkey -p com.noadsguy.evas.dev -c android.intent.category.LAUNCHER 1
   ```

   `install-apks` pushes the asset packs to
   `/sdcard/Android/data/<id>/files/local_testing`, where the Play Core library picks them up.
   The "run-as: package not debuggable" warning it prints at the end is harmless for a fresh install.
   bundletool signs with the local debug keystore; the release keystore is never involved.

## Evidence

Prototype (disposable): `EvasLearningWorld/Assets/Spikes/Pad/`. English is a `TextAsset` in
`Resources`, read and shown before any request; Romanian lives only in the `voice_ro` pack.

- Build: `PAD_BUILD_RESULT: Succeeded`, `PAD_BUILD_TOTAL_ERRORS: 0` (third run; the first attempt
  succeeded as well, the second failed only on a compile error of my own).
- Generated Gradle, from `EvasLearningWorld/Library/Bee/Android/Prj/IL2CPP/Gradle/`:
  `settings.gradle` has `include ':voice_ro'`; `launcher/build.gradle` has `assetPacks = [":voice_ro"]`;
  `unityLibrary/build.gradle` has `implementation 'com.google.android.play:asset-delivery:2.3.0'`;
  `voice_ro/build.gradle` declares `deliveryType = "on-demand"`.
- Bundle: `pad-spike.aab` (22.9 MB) contains a separate `voice_ro/` module holding
  `voice_ro/assets/voice_ro.txt`. The pack manifest carries `dist:type="asset-pack"` and
  `dist:delivery > dist:on-demand`.
- APK set: `asset-slices/voice_ro-master.apk` plus `splits/base-master.apk` and
  `splits/base-arm64_v8a.apk`. `voice_ro.txt` is **not** present in the base APK, so the Romanian
  text on screen can only have come through delivery.
- Device: the app process loaded the Play Core library in local-testing mode
  (`PlayCore: ... FakeAssetPackService : syncPacks()` in logcat from our own pid), and after launch
  the screen showed both lines: `EN: hello from the local english file` and
  `RO: salut din pachetul romanesc`. This is a single manual observation: the screenshot used to
  read it was deleted, as required, so no captured artifact of it exists. It is consistent with the
  split-APK evidence above (`voice_ro.txt` exists only in `asset-slices/voice_ro-master.apk`), but the
  on-device PASS itself rests on that one observation.
- English before Romanian: English is loaded from `Resources` and rendered before
  `DownloadAssetPackAsync` is called, and every failure path (`request threw`, timeout, empty path)
  falls through to the same render with Romanian left at its placeholder. The fallback is one branch.
- Base APK permissions (aapt2): `INTERNET`, `ACCESS_NETWORK_STATE`, `FOREGROUND_SERVICE`,
  `FOREGROUND_SERVICE_DATA_SYNC`, `WAKE_LOCK`, `RECEIVE_BOOT_COMPLETED`, all contributed by the Play
  asset-delivery library and present in the AAB itself, not added by bundletool. No advertising ID,
  and `launchable-activity` is intact.

Two things cost time and are worth knowing:

- No `PAD_*` lines appeared in logcat (`logcat | grep PAD_` returned nothing); the cause was not
  isolated. The player was a release IL2CPP build, but that was not tested as the reason. The
  on-screen text is what confirmed the run. Untested suggestion: build a Development player and see
  whether the markers then reach logcat.
- With the phone asleep or on the lock screen, Unity never gets a window, so `Start()` does not run
  and nothing can be observed. `PlayerSettings.runInBackground` does not change this on Android. The
  device has to be awake and unlocked for an on-device asset pack test.

Not covered here, on purpose: a real Play download over the network. Local testing exercises the
same Play Core call path but takes the pack from local storage. First launch offline, and Play being
unavailable, can only be observed from an internal test track install, so that stays an M5 check.

## Decision

Build the `Voice` module on Unity's native asset packs: English audio ships inside the app, every
other language is an on-demand `<lang>.androidpack`, and `Voice` requests the pack for the device
language at first launch and from Settings, keeping English whenever the pack is absent. No
networking code of our own; Play performs the transfer. The bundled-Romanian fallback is not needed
(for reference it would have cost about 14 MB per language, roughly 140 MB for ten extra languages,
which is why packs are the better shape).

Two follow-ups for later milestones: the Play asset-delivery library adds `INTERNET` and five other
permissions to the manifest, which the Families policy review in M6 must account for, and the real
offline and Play-unavailable behaviour is verified on an internal test track in M5.

Spec impact: sections 7 and 8 stand as written; the language packs stay Play Asset Delivery with English in the app. Section 9 gained a note (applied at the M0 exit) that Play asset delivery requires INTERNET and adds five more permissions, which the M6 review must account for.
