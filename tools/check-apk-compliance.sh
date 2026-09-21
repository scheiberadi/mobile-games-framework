#!/usr/bin/env bash
# Usage: tools/check-apk-compliance.sh <apk>  (fails when AD_ID is declared or no launcher activity; prints all permissions)
set -u
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
APK="${1:?apk}"
PERMS="$("$AAPT2" dump permissions "$APK")"
code=$?
if [ $code -ne 0 ] || [ -z "$PERMS" ]; then echo "FAIL: aapt2 dump permissions failed or printed nothing for $APK (exit $code)"; exit 1; fi
echo "$PERMS"
if echo "$PERMS" | grep -q "permission.AD_ID"; then echo "FAIL: AD_ID present"; exit 1; fi
echo "OK: no AD_ID"
if ! "$AAPT2" dump badging "$APK" | grep -q "launchable-activity"; then echo "FAIL: no launchable-activity"; exit 1; fi
echo "OK: launchable activity present"
