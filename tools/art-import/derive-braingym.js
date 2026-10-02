// Makes the Brain Gym sprites that are derived from other sprites instead of being drawn (art/eva/braingym/PROMPTS.md,
// "Derived in code"). Run after the sheets are cut and installed:  node tools/art-import/derive-braingym.js
//   found_<id>            = item_<id>              (Spot the Object choices)
//   match_<id>            = ref_<id>               (Same or Different choices)
//   rotated_<shape>       = shape_<shape> turned 90 degrees clockwise (Match Rotation choices)
//   incomplete_<id>/piece_<id> = Art Studio half_<id>/piece_<id> (Complete the Picture)
//   build_<tower>         = model_<tower>         (Copy the Construction choices)
//   puzzle_<puzzleN>      = puzzle_full_<N> with a square hole; piece2_<puzzleN> = the square that was cut out
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const ROOT = path.join(__dirname, '../..');
const ART = path.join(ROOT, 'EvasLearningWorld/Assets/Eva/Resources/Art');
const BG = path.join(ART, 'braingym');
const OUT = path.join(ROOT, 'art/eva/braingym/out/derived');
fs.mkdirSync(OUT, { recursive: true });

const copy = (from, to) => {
  fs.copyFileSync(from, path.join(BG, to));
  fs.copyFileSync(from, path.join(OUT, to));
};

const HOLES = [[0.62, 0.55], [0.36, 0.5], [0.5, 0.34], [0.4, 0.62], [0.6, 0.62], [0.5, 0.5]]; // hole centre as a fraction of the picture
const HOLE_FRACTION = 0.32; // hole side as a fraction of the picture side

(async () => {
  for (const id of ['apple', 'ball', 'cup', 'hat', 'kite', 'shoe']) copy(path.join(BG, `item_${id}.png`), `found_${id}.png`);
  for (const id of ['star', 'heart', 'cloud', 'leaf', 'shell', 'gem']) copy(path.join(BG, `ref_${id}.png`), `match_${id}.png`);
  for (const id of ['towera', 'towerb', 'towerc', 'towerd', 'towere', 'towerf']) copy(path.join(BG, `model_${id}.png`), `build_${id}.png`);
  for (const id of ['sun', 'flower', 'house', 'tree', 'car', 'balloon']) {
    copy(path.join(ART, `artstudio/half_${id}.png`), `incomplete_${id}.png`);
    copy(path.join(ART, `artstudio/piece_${id}.png`), `piece_${id}.png`);
  }
  for (const id of ['circle', 'square', 'triangle', 'star', 'arrow', 'heart']) {
    const buf = await sharp(path.join(BG, `shape_${id}.png`)).rotate(90, { background: { r: 0, g: 0, b: 0, alpha: 0 } }).png().toBuffer();
    fs.writeFileSync(path.join(BG, `rotated_${id}.png`), buf);
    fs.writeFileSync(path.join(OUT, `rotated_${id}.png`), buf);
  }

  for (let n = 1; n <= 6; n++) {
    const full = await sharp(path.join(BG, `puzzle_full_${n}.png`)).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const { width: w, height: h } = full.info;
    const side = Math.round(Math.min(w, h) * HOLE_FRACTION);
    const left = Math.max(0, Math.min(w - side, Math.round(HOLES[n - 1][0] * w - side / 2)));
    const top = Math.max(0, Math.min(h - side, Math.round(HOLES[n - 1][1] * h - side / 2)));
    const radius = Math.round(side * 0.12);
    const maskSvg = Buffer.from(`<svg width="${side}" height="${side}"><rect width="${side}" height="${side}" rx="${radius}" fill="#fff"/></svg>`);
    const outlineSvg = Buffer.from(`<svg width="${side}" height="${side}"><rect x="3" y="3" width="${side - 6}" height="${side - 6}" rx="${radius}" fill="none" stroke="#7a4b24" stroke-width="6"/></svg>`);

    const png = await sharp(full.data, { raw: { width: w, height: h, channels: 4 } }).png().toBuffer();
    const cut = await sharp(png).extract({ left, top, width: side, height: side }).composite([{ input: maskSvg, blend: 'dest-in' }]).png().toBuffer();
    const piece = await sharp(cut).composite([{ input: outlineSvg }]).png().toBuffer();

    const holeSvg = Buffer.from(`<svg width="${side}" height="${side}"><rect x="2" y="2" width="${side - 4}" height="${side - 4}" rx="${radius}" fill="#d8d2c4" stroke="#8d7a5c" stroke-width="4" stroke-dasharray="14 10"/></svg>`);
    const puzzle = await sharp(png).composite([{ input: holeSvg, left, top }]).png().toBuffer();

    for (const [name, buf] of [[`piece2_puzzle${n}.png`, piece], [`puzzle_puzzle${n}.png`, puzzle]]) {
      fs.writeFileSync(path.join(BG, name), buf);
      fs.writeFileSync(path.join(OUT, name), buf);
    }
  }
  console.log('derived sprites written');
})();
