#!/usr/bin/env bash
# Usage: tools/run-editmode-tests.sh <absolute project path>
# Do NOT pass -quit: it is incompatible with -runTests.
set -u
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
PROJECT="${1:?absolute project path required}"
RESULTS="$PROJECT/Logs/editmode-results.xml"
LOG="$PROJECT/Logs/editmode.log"
mkdir -p "$PROJECT/Logs"
rm -f "$RESULTS"
"$UNITY" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform EditMode -testResults "$RESULTS" -logFile "$LOG"
code=$?
echo "unity exit code: $code"
if [ -f "$RESULTS" ]; then grep -o '<test-run [^>]*' "$RESULTS" | head -1; else tail -n 40 "$LOG"; fi
exit $code
