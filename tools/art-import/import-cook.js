// Cooking Measures ("Fill the cup"): cook_items_sheet.png in ~/Downloads (3 x 2 on flat #00FF00):
//   top row: water jug, flour bag, milk jug -> sciencelab/cookpour_<water|flour|milk>.png
//   bottom row: water stream, flour stream, milk stream -> sciencelab/cookstream_<water|flour|milk>.png
// The streams start a little above the middle of the sheet, so the rows are split at 47% of the height, not at 50%.
//   cook_bowl_dishes_sheet.png (2 x 2: bowl, pancakes / cake, cupcakes) -> sciencelab/cookbowl.png, cookdish_<pancakes|cake|cupcakes>.png
//   cook_extras_sheet.png (one row: egg, spoon of sugar; the spoon reaches past the middle, so the cut is at 43.7% of the width) -> sciencelab/cookitem_<egg|sugar>.png (when the file is there)
// The kitchen counter: node tools/art-import/import-scene-bg.js kitchen_bg.png kitchen_bg
//   node tools/art-import/import-cook.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const ids = ['water', 'flour', 'milk'];
const RowSplit = 0.47;
const fs = require('fs');

// Only the tallest block of rows with picture in it: a neighbour's cherry stem poking into the cell is dropped.
async function bandOnly(buffer) {
  const { data, info } = await sharp(buffer).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const mass = [];
  for (let y = 0; y < info.height; y++) { let n = 0; for (let x = 0; x < info.width; x++) if (data[(y * info.width + x) * 4 + 3] > 20) n++; mass.push(n); }
  let best = [0, 0, 0], start = -1, sum = 0;
  for (let y = 0; y <= info.height; y++) {
    if (y < info.height && mass[y] > 0) { if (start < 0) { start = y; sum = 0; } sum += mass[y]; }
    else if (start >= 0) { if (sum > best[2]) best = [start, y - 1, sum]; start = -1; }
  }
  return sharp(buffer).extract({ left: 0, top: best[0], width: info.width, height: best[1] - best[0] + 1 }).png().toBuffer();
}

// A sheet cut into equal cells, each trimmed and fitted into a box.
// The cake's cherry stem reaches up past the middle of the sheet into the bowl's cell.
const padTop = { cookdish_cake: 0.12 };

async function cells(file, cols, rows, list, box, splitX) {
  const keyed = await keyOut(path.join(os.homedir(), 'Downloads', file));
  const meta = await sharp(keyed).metadata();
  for (const [name, col, row] of list) {
    let left = Math.round(meta.width * col / cols), right = Math.round(meta.width * (col + 1) / cols);
    if (splitX) { left = col === 0 ? 0 : splitX; right = col === 0 ? splitX : meta.width; }
    const top = Math.max(0, Math.round(meta.height * row / rows) - Math.round(meta.height * (padTop[name] || 0))), bottom = Math.round(meta.height * (row + 1) / rows);
    const cell = await sharp(keyed).extract({ left, top, width: right - left, height: bottom - top }).png().toBuffer();
    const trimmed = await sharp(await bandOnly(cell)).trim({ threshold: 10 }).png().toBuffer();
    await sharp(trimmed).resize({ width: box, height: box, fit: 'inside' }).png().toFile(path.join(out, name + '.png'));
    const m = await sharp(path.join(out, name + '.png')).metadata();
    console.log(name, m.width + 'x' + m.height);
  }
}

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
  await cells('cook_bowl_dishes_sheet.png', 2, 2, [['cookbowl', 0, 0], ['cookdish_pancakes', 1, 0], ['cookdish_cake', 0, 1], ['cookdish_cupcakes', 1, 1]], 520);
  if (fs.existsSync(path.join(os.homedir(), 'Downloads', 'cook_extras_sheet.png'))) await cells('cook_extras_sheet.png', 2, 1, [['cookitem_egg', 0, 0], ['cookitem_sugar', 1, 0]], 300, Math.round(0.437 * 1774));
})();
