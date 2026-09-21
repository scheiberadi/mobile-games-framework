#!/usr/bin/env bash
# Shared paths for the Unity and Android helper scripts. Source this file; do not run it.
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -W)"
UNITY_DIR="/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor"
UNITY="$UNITY_DIR/Unity.exe"
ANDROID_SDK="$UNITY_DIR/Data/PlaybackEngines/AndroidPlayer/SDK"
ADB="$ANDROID_SDK/platform-tools/adb.exe"
AAPT2="$ANDROID_SDK/build-tools/36.0.0/aapt2.exe"
DEVICE="R3CY30NNA6W"
EVA_PROJECT="$REPO_ROOT/EvasLearningWorld"
for tool_path in "$UNITY" "$ADB" "$AAPT2"; do
  if [ ! -f "$tool_path" ]; then echo "tools/env.sh: required file not found: $tool_path" >&2; return 1 2>/dev/null || exit 1; fi
done
