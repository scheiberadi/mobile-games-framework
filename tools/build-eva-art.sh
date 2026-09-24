#!/usr/bin/env bash
# Rasterises art/eva/**/*.svg into EvasLearningWorld/Assets/Eva/Resources/Art/**/*.png.
# Usage: bash tools/build-eva-art.sh
# icons and objects render at 256px, except icons/tile which is 300x300 (an answer-tile background,
# not an icon-sized sprite) via the exception list below; world icons (house/school/store) render at
# 512px; the three backgrounds (map_bg, school_bg, house_bg) render at 1920px, also via a per-file
# exception list.
set -eu
cd "$(dirname "${BASH_SOURCE[0]}")/.."

SVG2PNG=tools/svg2png/svg2png.js
OUT=EvasLearningWorld/Assets/Eva/Resources/Art
WIDE_ICONS_FILES="tile"
WIDE_WORLD_FILES="map_bg school_bg house_bg store_bg"

TMP_ICONS_256="$(mktemp -d)"
TMP_ICONS_300="$(mktemp -d)"
TMP_WIDE="$(mktemp -d)"
TMP_NARROW="$(mktemp -d)"
trap 'rm -rf "$TMP_ICONS_256" "$TMP_ICONS_300" "$TMP_WIDE" "$TMP_NARROW"' EXIT

# icons has a mix of widths (tile is 300px, the rest are 256px), so split into temp copies too.
for f in art/eva/icons/*.svg; do
  name="$(basename "$f" .svg)"
  is_wide=0
  for w in $WIDE_ICONS_FILES; do
    [ "$name" = "$w" ] && is_wide=1
  done
  if [ "$is_wide" = "1" ]; then
    cp "$f" "$TMP_ICONS_300/"
  else
    cp "$f" "$TMP_ICONS_256/"
  fi
done

node "$SVG2PNG" "$TMP_ICONS_256" "$OUT/icons" 256
node "$SVG2PNG" "$TMP_ICONS_300" "$OUT/icons" 300
node "$SVG2PNG" art/eva/objects "$OUT/objects" 256

# world has a mix of widths, so split into a wide batch and a narrow batch of temp copies.
for f in art/eva/world/*.svg; do
  name="$(basename "$f" .svg)"
  is_wide=0
  for w in $WIDE_WORLD_FILES; do
    [ "$name" = "$w" ] && is_wide=1
  done
  if [ "$is_wide" = "1" ]; then
    cp "$f" "$TMP_WIDE/"
  else
    cp "$f" "$TMP_NARROW/"
  fi
done

node "$SVG2PNG" "$TMP_WIDE" "$OUT/world" 1920
node "$SVG2PNG" "$TMP_NARROW" "$OUT/world" 512

node "$SVG2PNG" art/eva/characters "$OUT/characters" 512
node "$SVG2PNG" art/eva/cat "$OUT/cat" 1000

echo "build-eva-art: done"
