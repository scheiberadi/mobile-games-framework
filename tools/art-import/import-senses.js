// Senses ("What do I use?"): the things on the table and the effect icons, from two sheets on flat #00FF00 in ~/Downloads:
//   senses_things_sheet.png (5 x 2: rainbow butterfly bell drum roses / skunk lemon strawberry bunny ice) -> sciencelab/sensething_<id>.png
//   senses_effects_sheet.png (1 x 3 used (a 4th, a glow, has a green halo and is not imported): scent sound sparkle) -> sciencelab/senseeffect_<id>.png
// The background picture is imported with: node import-scene-bg.js senses_bg.png senses_bg
//   node tools/art-import/import-senses.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut, runs } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const dl = (f) => path.join(os.homedir(), 'Downloads', f);
const THINGS = ['rainbow', 'butterfly', 'bell', 'drum', 'roses', 'skunk', 'lemon', 'strawberry', 'bunny', 'ice'];
const EFFECTS = ['scent', 'sound', 'sparkle'];

// Columns first (the pictures of one column never touch the next column), then the rows inside each column; ids run row by row.
async function cut(file, rows, cols, ids, prefix, size) {
  const keyed = await keyOut(dl(file));
  const meta = await sharp(keyed).metadata();
  const found = [];
  const columns = await runs(keyed, 'x', cols);
  for (let c = 0; c < columns.length; c++) {
    const [left, right] = columns[c];
    const column = await sharp(keyed).extract({ left, top: 0, width: right - left, height: meta.height }).png().toBuffer();
    const bands = await runs(column, 'y', rows);
    for (let r = 0; r < bands.length; r++) {
      const [top, bottom] = bands[r];
      found[r * cols + c] = await sharp(column).extract({ left: 0, top, width: right - left, height: bottom - top }).png().toBuffer();
    }
  }
  const count = found.filter(Boolean).length;
  if (count !== ids.length) throw new Error(file + ': found ' + count + ' pictures, expected ' + ids.length);
  for (let i = 0; i < ids.length; i++) {
    const trimmed = await sharp(found[i]).trim({ threshold: 10 }).png().toBuffer();
    const name = prefix + ids[i] + '.png';
    await sharp(trimmed).resize({ width: size, height: size, fit: 'inside' }).png().toFile(path.join(out, name));
    const m = await sharp(path.join(out, name)).metadata();
    console.log(name, m.width + 'x' + m.height);
  }
}

(async () => {
  await cut('senses_things_sheet.png', 2, 5, THINGS, 'sensething_', 420);
  await cut('senses_effects_sheet.png', 1, 4, [...EFFECTS, 'glow'], 'senseeffect_', 300);
})();
