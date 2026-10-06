// Zoo & Farm Feeding art, from the cut-sheets output (art/eva/zoofarm/out/feeding/belt.png and bubble.png):
//   node tools/art-import/make-feeding-art.js        (run `cut-sheets.js zoofarm_belt zoofarm_bubble --install` first)
// Writes into the game's Resources/Art/zoofarm:
//   belt.png        the conveyor belt, trimmed and scaled so one slat period is exactly TILE pixels (the screen draws it 1:1)
//   belt_slats.png  one period of the belt's slat pattern (TILE x SURFACE_H); the screen scrolls it as a repeating texture over the
//                   belt's flat top (between SURFACE_X0 and SURFACE_X1 of belt.png) so the belt looks like it moves
//   bubble.png      the thought bubble, trimmed
// The numbers below were measured on the first generated belt (slats are vertical and 91 px apart between x 181 and 1000, the
// light surface is rows 4-46 of the trimmed picture); re-measure if the belt art is generated again.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const ROOT = path.join(__dirname, '../..');
const OUT = path.join(ROOT, 'art/eva/zoofarm/out/feeding');
const RES = path.join(ROOT, 'EvasLearningWorld/Assets/Eva/Resources/Art/zoofarm');

const PERIOD = 91;       // source px between two slats
const TILE = 88;         // the same, in the installed belt.png
const SLAT_X = 182;      // a column where a dark slat begins (the crop starts there)
const SURFACE_Y0 = 4, SURFACE_Y1 = 46;

(async () => {
  const belt = await sharp(path.join(OUT, 'belt.png')).trim().png().toBuffer();
  const meta = await sharp(belt).metadata();
  const scale = TILE / PERIOD;
  const width = Math.round(meta.width * scale), height = Math.round(meta.height * scale);
  await sharp(belt).resize(width, height).png({ compressionLevel: 9 }).toFile(path.join(RES, 'belt.png'));

  const slats = await sharp(belt)
    .extract({ left: SLAT_X, top: SURFACE_Y0, width: PERIOD, height: SURFACE_Y1 - SURFACE_Y0 })
    .resize(TILE, Math.round((SURFACE_Y1 - SURFACE_Y0) * scale)).png({ compressionLevel: 9 }).toBuffer();
  await sharp(slats).toFile(path.join(RES, 'belt_slats.png'));
  const slatsMeta = await sharp(slats).metadata();
  console.log('belt.png', width, 'x', height, '; belt_slats.png', slatsMeta.width, 'x', slatsMeta.height,
    '; surface x', Math.round(113 * scale), '-', Math.round(1000 * scale), 'y', Math.round(SURFACE_Y0 * scale), '-', Math.round(SURFACE_Y1 * scale));

  const bubble = await sharp(path.join(OUT, 'bubble.png')).trim().png({ compressionLevel: 9 }).toBuffer();
  await sharp(bubble).toFile(path.join(RES, 'bubble.png'));
  const bubbleMeta = await sharp(bubble).metadata();
  console.log('bubble.png', bubbleMeta.width, 'x', bubbleMeta.height);
})();
