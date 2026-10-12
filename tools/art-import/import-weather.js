// Weather ("Make the weather"): things in the village and the sky pieces, from two one-row sheets on flat #00FF00 in ~/Downloads:
//   weather_things_sheet.png (tree tulip_open tulip_closed puddle snow) -> sciencelab/weatherthing_<id>.png
//   weather_sky_sheet.png (cloud rain storm snowflake wind leaf) -> sciencelab/weathersky_<id>.png
// The background picture is imported with: node import-scene-bg.js weather_bg.png weather_bg
//   node tools/art-import/import-weather.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut, runs } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const dl = (f) => path.join(os.homedir(), 'Downloads', f);
const THINGS = ['tree', 'tulip_open', 'tulip_closed', 'puddle', 'snow'];
const SKY = ['cloud', 'rain', 'storm', 'snowflake', 'wind', 'leaf'];

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
  await cut('weather_things_sheet.png', 1, 5, THINGS, 'weatherthing_', 480);
  await cut('weather_sky_sheet.png', 1, 6, SKY, 'weathersky_', 360);
})();
