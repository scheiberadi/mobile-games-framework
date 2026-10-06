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

function eraseSlats(data, w, h, y0, y1) {
  const lum = (i) => (data[i] + data[i + 1] + data[i + 2]) / 3;
  for (let y = y0; y < y1 && y < h; y++) {
    const dark = new Uint8Array(w);
    for (let x = 0; x < w; x++) { const i = (y * w + x) * 4; dark[x] = data[i + 3] > 200 && lum(i) < 124 ? 1 : 0; }
    const grown = new Uint8Array(w); // slat edges are anti-aliased: widen each dark run by 3 px
    for (let x = 0; x < w; x++) if (dark[x]) for (let d = -3; d <= 3; d++) if (x + d >= 0 && x + d < w) grown[x + d] = 1;
    for (let x = 0; x < w; x++) {
      if (!grown[x]) continue;
      let src = -1;
      for (let d = 1; d < 80 && src < 0; d++) {
        if (x - d >= 0 && !grown[x - d] && data[((y * w) + x - d) * 4 + 3] > 200) src = x - d;
        else if (x + d < w && !grown[x + d] && data[((y * w) + x + d) * 4 + 3] > 200) src = x + d;
      }
      if (src < 0 || data[(y * w + x) * 4 + 3] < 200) continue;
      for (let c = 0; c < 3; c++) data[(y * w + x) * 4 + c] = data[(y * w + src) * 4 + c];
    }
  }
}

(async () => {
  const belt = await sharp(path.join(OUT, 'belt.png')).trim().png().toBuffer();
  const meta = await sharp(belt).metadata();
  const scale = TILE / PERIOD;
  const width = Math.round(meta.width * scale), height = Math.round(meta.height * scale);
  // The still belt has no slats on its top: the screen draws the moving ones over it (inside the window below) and they slide out from
  // under the end rollers, so every slat of the picture is painted over with the light surface next to it, row by row.
  const scaled = await sharp(belt).resize(width, height).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  eraseSlats(scaled.data, scaled.info.width, scaled.info.height, Math.round(SURFACE_Y0 * scale), Math.round(SURFACE_Y1 * scale));
  await sharp(scaled.data, { raw: { width, height, channels: 4 } }).png({ compressionLevel: 9 }).toFile(path.join(RES, 'belt.png'));

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
