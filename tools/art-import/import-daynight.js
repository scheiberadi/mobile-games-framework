// Day/Night ("Eva's day"): the eight activity cards from two one-row sheets of round badges on flat #00FF00 in ~/Downloads:
//   daynight_cards_day.png (breakfast ball school kite) -> sciencelab/daynightcard_day_<id>.png
//   daynight_cards_night.png (sleep story stars pyjamas) -> sciencelab/daynightcard_night_<id>.png
// The night meadow is imported with: node import-scene-bg.js daynight_night.png daynight_night (the day meadow is world/season_summer).
//   node tools/art-import/import-daynight.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut, runs } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const dl = (f) => path.join(os.homedir(), 'Downloads', f);

// The badges sit in equal slots (neighbours can touch after keying, so empty columns cannot tell them apart).
async function cut(file, ids, prefix, size) {
  const keyed = await keyOut(dl(file));
  const meta = await sharp(keyed).metadata();
  for (let i = 0; i < ids.length; i++) {
    const left = Math.round(meta.width * i / ids.length), right = Math.round(meta.width * (i + 1) / ids.length);
    const slot = await sharp(keyed).extract({ left, top: 0, width: right - left, height: meta.height }).png().toBuffer();
    // A sliver of the neighbour's rim can cross into the slot: keep only the widest run of columns, then trim.
    const columns = await runs(slot, 'x', 99);
    const [x0, x1] = columns.reduce((a, b) => (b[1] - b[0] > a[1] - a[0] ? b : a));
    const badge = await sharp(slot).extract({ left: x0, top: 0, width: x1 - x0, height: meta.height }).png().toBuffer();
    const trimmed = await sharp(badge).trim({ threshold: 10 }).png().toBuffer();
    const name = prefix + ids[i] + '.png';
    await sharp(trimmed).resize({ width: size, height: size, fit: 'inside' }).png().toFile(path.join(out, name));
    const m = await sharp(path.join(out, name)).metadata();
    console.log(name, m.width + 'x' + m.height);
  }
}

(async () => {
  await cut('daynight_cards_day.png', ['breakfast', 'ball', 'school', 'kite'], 'daynightcard_day_', 360);
  await cut('daynight_cards_night.png', ['sleep', 'story', 'stars', 'pyjamas'], 'daynightcard_night_', 360);
})();
