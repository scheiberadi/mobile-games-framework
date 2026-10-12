// Healthy vs Unhealthy ("Feed Eva"): the effect icons, from ~/Downloads/food_effects_sheet.png (ONE row on flat #00FF00:
// heart, star, star_off, yuck, rumble) -> sciencelab/foodfx_<id>.png. The kitchen: node import-scene-bg.js food_bg.png food_bg
//   node tools/art-import/import-food.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut, runs } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const IDS = ['heart', 'star', 'star_off', 'yuck', 'rumble'];

(async () => {
  const keyed = await keyOut(path.join(os.homedir(), 'Downloads', 'food_effects_sheet.png'));
  const meta = await sharp(keyed).metadata();
  const columns = await runs(keyed, 'x', IDS.length);
  if (columns.length !== IDS.length) throw new Error('found ' + columns.length + ' icons, expected ' + IDS.length);
  for (let i = 0; i < IDS.length; i++) {
    const [left, right] = columns[i];
    const piece = await sharp(keyed).extract({ left, top: 0, width: right - left, height: meta.height }).png().toBuffer();
    const trimmed = await sharp(piece).trim({ threshold: 10 }).png().toBuffer();
    const name = 'foodfx_' + IDS[i] + '.png';
    await sharp(trimmed).resize({ width: 256, height: 256, fit: 'inside' }).png().toFile(path.join(out, name));
    const m = await sharp(path.join(out, name)).metadata();
    console.log(name, m.width + 'x' + m.height);
  }
})();
