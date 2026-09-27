# Spike: Android Auto Backup for the local save

Date: 2026-09-21. Unity 6000.5.10f1, Samsung Galaxy S25 Ultra (SM-S938B, Android 16 / API 36).

Question: is the eventual local save (PlayerPrefs, which Unity stores as Android shared preferences) included in Android Auto Backup?

## Verdict

PASS. The built APK declares Auto Backup with rules that include the shared preferences domain, and an actual backup and restore on the phone was observed (same-device bmgr restore): the PlayerPrefs counter came back as `launches: 3` after uninstall and reinstall (see "Restore attempt"). Only the `dataExtractionRules` path (Android 12 and newer) was exercised; the `fullBackupContent` path (Android 8 to 11) and device-to-device transfer were not.

## Mechanism

Unity's generated launcher manifest is edited by an `IPostGenerateGradleAndroidProject` callback, `Assets/Editor/EvaAndroidPostProcess.cs` (the only file of this spike kept for the product). It writes two rule files into the launcher module's `src/main/res/xml` and sets three attributes on the `application` element:

- `android:allowBackup="true"`
- `android:fullBackupContent="@xml/eva_full_backup_content"` (Android 11 and older)
- `android:dataExtractionRules="@xml/eva_data_extraction_rules"` (Android 12 and newer)

The Main Manifest override slot (`Assets/Plugins/Android/AndroidManifest.xml`) is deliberately not used, because it removed the launcher activity in the Sudoku project. The generated launcher manifest was at the expected path, so no path fix was needed.

Rule files:

- `eva_full_backup_content.xml`: `<full-backup-content>` with `<include domain="sharedpref" path="."/>`.
- `eva_data_extraction_rules.xml`: `<data-extraction-rules>` with the same include inside both `<cloud-backup>` and `<device-transfer>`.

Including only the `sharedpref` domain means files, databases and caches are not backed up; the local save is the only thing meant to survive.

## SDK levels

minSdkVersion 26, targetSdkVersion 36 (from `aapt2 dump badging`). Because the target is 31 or higher, the `dataExtractionRules` file is the one Android 12 and newer read; `fullBackupContent` covers older devices (minSdk 26 to 30).

## Manifest evidence

Built with `EVA_SCENES="Assets/Spikes/Backup/BackupProbe.unity" bash tools/build-eva-debug.sh` (BUILD_RESULT Succeeded, 0 errors). Then, on `EvasLearningWorld/Builds/Android/eva-debug.apk`:

```
aapt2 dump xmltree <apk> --file AndroidManifest.xml | grep -iE "allowBackup|fullBackupContent|dataExtractionRules"
  A: ...res/android:allowBackup(0x01010280)=true
  A: ...res/android:fullBackupContent(0x010104eb)=@0x7f100001
  A: ...res/android:dataExtractionRules(0x0101063e)=@0x7f100000

aapt2 dump resources <apk> | grep -A1 -iE "eva_full|eva_data"
  resource 0x7f100000 xml/eva_data_extraction_rules  (file) res/0K.xml
  resource 0x7f100001 xml/eva_full_backup_content    (file) res/5V.xml

aapt2 dump badging <apk>: minSdkVersion:'26'  targetSdkVersion:'36'  package com.noadsguy.evas.dev
```

`tools/check-apk-compliance.sh` reported no AD_ID and a launchable activity present, and the manifest still has its MAIN/LAUNCHER intent filter. The only permission is `android.permission.INTERNET`, plus Unity's own `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`.

## Restore attempt

Observed on the phone (Samsung Galaxy S25 Ultra, Android 16), awake and unlocked, active transport `com.google.android.gms/.backup.BackupTransportService` (Google, not Samsung; `bmgr enabled` reported "Backup Manager currently enabled"). Transports listed by `bmgr list transports`: `com.android.localtransport/.LocalTransport`, `com.google.android.gms/.backup.migrate.service.D2dTransport`, `com.google.android.gms/.backup.BackupTransportService` (active), `com.google.android.apps.restore/.transport.BackupTransportService`.

An earlier attempt was stopped because the phone was asleep (`mWakefulness=Dozing`, keyguard showing), which is why the first launch was not counted. Once the phone was awake:

1. Screenshot after a launch showed `launches: 1`; after `am force-stop` and another launch it showed `launches: 2`.
2. `bmgr backupnow com.noadsguy.evas.dev`:
   ```
   Running incremental backup for 1 requested packages.
   Package @pm@ with result: Success
   Package com.noadsguy.evas.dev with progress: 2048/2048
   Package com.noadsguy.evas.dev with progress: 4096/2048
   Package com.noadsguy.evas.dev with result: Success
   Backup finished with result: Success
   ```
3. `bmgr list sets`: `3d4892a527fc9ef9 : Galaxy S25 Ultra`
4. `adb uninstall com.noadsguy.evas.dev` (Success), then `adb install` of the same APK (Success). The app was not launched in between.
5. `bmgr restore 3d4892a527fc9ef9 com.noadsguy.evas.dev`:
   ```
   Scheduling restore: Galaxy S25 Ultra
   restoreStarting: 1 packages
   onUpdate: 1 = com.noadsguy.evas.dev
   restoreFinished: 0
   done
   ```
6. Launch and screenshot: `launches: 3`. The restored value (2) was incremented by the new launch.

This confirms the shared preferences (PlayerPrefs) of a debug adb-installed build are included in Auto Backup through Google's transport and can be restored with `bmgr`. It is a `bmgr` restore of a cloud backup set on the same device; a real reinstall from Play on a new device (Android 12 and newer uses the device-to-device and cloud rules in `eva_data_extraction_rules.xml`) is still worth one look during the M5 Play internal test.

## Decision

Keep `EvaAndroidPostProcess.cs` as the product's backup declaration: it is small, it is not the Main Manifest slot, and the local save survives backup and restore. No further backup work is needed for M0.

## Note on INTERNET

`INTERNET` is not stripped. Play asset delivery (Task 5) needs it, and this task adds no permission stripping; the post-process only touches the three backup attributes.
