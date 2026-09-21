#!/usr/bin/env bash
# Usage: tools/eva-install.sh <apk> <package> [screenshot.png]
set -u
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
"$ADB" -s "$DEVICE" install -r "${1:?apk}" || exit 1
"$ADB" -s "$DEVICE" shell monkey -p "${2:?package}" -c android.intent.category.LAUNCHER 1 >/dev/null
if [ -n "${3:-}" ]; then sleep 6; "$ADB" -s "$DEVICE" exec-out screencap -p > "$3"; echo "screenshot: $3"; fi
