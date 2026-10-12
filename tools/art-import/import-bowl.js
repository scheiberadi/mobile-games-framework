// Cooking Measures: the bowl filling up. cook_bowl_states_sheet.png in ~/Downloads (2 x 2 on flat #00FF00, same bowl in the same place in every cell):
//   empty (the same bowl as before), a third full of batter, two thirds full, full -> sciencelab/cookbowl.png, cookbowl_1.png, cookbowl_2.png, cookbowl_3.png
// All four are cut with the SAME rectangle (the union of the four bowls) and scaled the same, so the bowl does not jump when the picture changes.
//   node tools/art-import/import-bowl.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const names = ['cookbowl', 'cookbowl_1', 'cookbowl_2', 'cookbowl_3'];
const Box = 520;

(async () => {
  const keyed = await keyOut(path.join(os.homedir(), 'Downloads', 'cook_bowl_states_sheet.png'));
  const meta = await sharp(keyed).metadata();
  const cellW = Math.floor(meta.width / 2), cellH = Math.floor(meta.height / 2);
  const cells = [];
  let left = Infinity, top = Infinity, right = -1, bottom = -1;
  for (let i = 0; i < 4; i++) {
    const cell = await sharp(keyed).extract({ left: (i % 2) * cellW, top: Math.floor(i / 2) * cellH, width: cellW, height: cellH }).png().toBuffer();
    const { data, info } = await sharp(cell).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    for (let y = 0; y < info.height; y++) for (let x = 0; x < info.width; x++) {
      if (data[(y * info.width + x) * 4 + 3] <= 20) continue;
      if (x < left) left = x; if (x > right) right = x; if (y < top) top = y; if (y > bottom) bottom = y;
    }
    cells.push(cell);
  }
  const width = right - left + 1, height = bottom - top + 1;
  const scale = Math.min(Box / width, Box / height);
  for (let i = 0; i < 4; i++) {
    const cut = await sharp(cells[i]).extract({ left, top, width, height }).png().toBuffer();
    await sharp(cut).resize({ width: Math.round(width * scale), height: Math.round(height * scale) }).png().toFile(path.join(out, names[i] + '.png'));
    console.log(names[i], Math.round(width * scale) + 'x' + Math.round(height * scale));
  }
})();
