#!/usr/bin/env bash
# Usage: [EVA_SCENES="a.unity;b.unity"] tools/build-eva-debug.sh
set -u
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
LOG="$EVA_PROJECT/Logs/eva-build.log"
mkdir -p "$EVA_PROJECT/Logs"
"$UNITY" -batchmode -quit -projectPath "$EVA_PROJECT" -executeMethod EvaAndroidBuilder.BuildDebug -logFile "$LOG"
code=$?
echo "unity exit code: $code"
if [ ! -f "$LOG" ]; then echo "Unity did not write a log at $LOG (it may not have started, or another Unity instance is using the project)"; exit "${code:-1}"; fi
grep -E "BUILD_RESULT|BUILD_TOTAL_ERRORS" "$LOG"
exit $code
