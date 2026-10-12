// Cooking Measures ("Fill the cup"): cook_items_sheet.png in ~/Downloads (3 x 2 on flat #00FF00):
//   top row: water jug, flour bag, milk jug -> sciencelab/cookpour_<water|flour|milk>.png
//   bottom row: water stream, flour stream, milk stream -> sciencelab/cookstream_<water|flour|milk>.png
// The streams start a little above the middle of the sheet, so the rows are split at 47% of the height, not at 50%.
// The kitchen counter: node tools/art-import/import-scene-bg.js kitchen_bg.png kitchen_bg
//   node tools/art-import/import-cook.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const ids = ['water', 'flour', 'milk'];
const RowSplit = 0.47;

(async () => {
  const keyed = await keyOut(path.join(os.homedir(), 'Downloads', 'cook_items_sheet.png'));
  const meta = await sharp(keyed).metadata();
  for (let row = 0; row < 2; row++) {
    const top = row === 0 ? 0 : Math.round(meta.height * RowSplit);
    const height = (row === 0 ? Math.round(meta.height * RowSplit) : meta.height) - top;
    for (let col = 0; col < 3; col++) {
      const left = Math.round(meta.width * col / 3), right = Math.round(meta.width * (col + 1) / 3);
      const cell = await sharp(keyed).extract({ left, top, width: right - left, height }).png().toBuffer();
      const trimmed = await sharp(cell).trim({ threshold: 10 }).png().toBuffer();
      const name = (row === 0 ? 'cookpour_' : 'cookstream_') + ids[col] + '.png';
      const box = row === 0 ? { width: 420, height: 420 } : { width: 200, height: 640 };
      await sharp(trimmed).resize({ ...box, fit: 'inside' }).png().toFile(path.join(out, name));
      const m = await sharp(path.join(out, name)).metadata();
      console.log(name, m.width + 'x' + m.height);
    }
  }
})();
