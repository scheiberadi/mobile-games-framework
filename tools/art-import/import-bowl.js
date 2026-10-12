// Cooking Measures: the bowl filling up. cook_bowl_states_sheet.png in ~/Downloads (2 x 2 on flat #00FF00, the same bowl with the wooden spoon in every cell):
//   empty, a third full of batter, two thirds full, full -> sciencelab/cookbowl.png, cookbowl_1.png, cookbowl_2.png, cookbowl_3.png
// The generator does not keep the bowl in exactly the same place, so each cell is cleaned of strays (a neighbour's spoon tip poking in), trimmed, scaled to
// the same bowl width and set on the same floor: all four pictures are the same size and the bowl does not jump when the picture changes.
//   node tools/art-import/import-bowl.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const names = ['cookbowl', 'cookbowl_1', 'cookbowl_2', 'cookbowl_3'];
const Width = 520;

// Only the tallest block of rows with picture in it.
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

(async () => {
  const keyed = await keyOut(path.join(os.homedir(), 'Downloads', 'cook_bowl_states_sheet.png'));
  const meta = await sharp(keyed).metadata();
  const cellW = Math.floor(meta.width / 2), cellH = Math.floor(meta.height / 2);
  const pictures = [];
  for (let i = 0; i < 4; i++) {
    const cell = await sharp(keyed).extract({ left: (i % 2) * cellW, top: Math.floor(i / 2) * cellH, width: cellW, height: cellH }).png().toBuffer();
    const trimmed = await sharp(await bandOnly(cell)).trim({ threshold: 10 }).png().toBuffer();
    const m = await sharp(trimmed).metadata();
    const height = Math.round(m.height * Width / m.width);
    pictures.push({ buffer: await sharp(trimmed).resize({ width: Width, height }).png().toBuffer(), height });
  }
  const floor = Math.max(...pictures.map(p => p.height));
  for (let i = 0; i < 4; i++) {
    const canvas = await sharp({ create: { width: Width, height: floor, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
      .composite([{ input: pictures[i].buffer, left: 0, top: floor - pictures[i].height }]).png().toFile(path.join(out, names[i] + '.png'));
    console.log(names[i], canvas.width + 'x' + canvas.height);
  }
})();
