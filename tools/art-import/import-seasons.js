// Seasons ("Year tree"): the four season cards and the falling bits, from two one-row sheets on flat #00FF00 in ~/Downloads:
//   season_cards.png (spring summer autumn winter) -> sciencelab/seasoncard_<id>.png
//   season_bits.png (petal maple_red maple_orange oak) -> sciencelab/seasonbit_<id>.png
// The four meadow pictures are imported with: node import-scene-bg.js season_<id>.png season_<id>
//   node tools/art-import/import-seasons.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut, runs } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const dl = (f) => path.join(os.homedir(), 'Downloads', f);

async function cut(file, ids, prefix, size, equalSlots) {
  const keyed = await keyOut(dl(file));
  const meta = await sharp(keyed).metadata();
  // The cards sit in equal slots (two badges touch, so the empty columns cannot tell them apart); the bits are separated by empty columns.
  const columns = equalSlots ? ids.map((_, i) => [Math.round(meta.width * i / ids.length), Math.round(meta.width * (i + 1) / ids.length)]) : await runs(keyed, 'x', ids.length);
  for (let i = 0; i < ids.length; i++) {
    const [left, right] = columns[i];
    const column = await sharp(keyed).extract({ left, top: 0, width: right - left, height: meta.height }).png().toBuffer();
    // A sliver of a touching neighbour's rim can cross into the slot: keep only the widest run of columns, then trim.
    const cols = await runs(column, 'x', 99);
    const [x0, x1] = cols.reduce((a, b) => (b[1] - b[0] > a[1] - a[0] ? b : a));
    const badge = await sharp(column).extract({ left: x0, top: 0, width: x1 - x0, height: meta.height }).png().toBuffer();
    const trimmed = await sharp(badge).trim({ threshold: 10 }).png().toBuffer();
    const name = prefix + ids[i] + '.png';
    await sharp(trimmed).resize({ width: size, height: size, fit: 'inside' }).png().toFile(path.join(out, name));
    const m = await sharp(path.join(out, name)).metadata();
    console.log(name, m.width + 'x' + m.height);
  }
}

(async () => {
  await cut('season_cards.png', ['spring', 'summer', 'autumn', 'winter'], 'seasoncard_', 360, true);
  await cut('season_bits.png', ['petal', 'maple_red', 'maple_orange', 'oak'], 'seasonbit_', 200);
})();
