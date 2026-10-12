// One garden plant: its growth stages as separate pictures Resources/Art/sciencelab/plant_<id>_<n>.png (n from 0), trimmed, all at ONE
// scale (the last stage is 512 px tall; earlier stages are drawn relatively smaller), so the screen can stand them on the soil by their
// bottom edge and read the plant's top from the picture height.
//   node tools/art-import/import-plant.js <id> <stages> [file]   file: ~/Downloads/<id>_sheet.png, ONE ROW of <stages> plants on flat #00FF00
//   node tools/art-import/import-plant.js daisy 5                the first plant, from the older single pictures stage_seed .. stage_fruit
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

const dir = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const FINAL_HEIGHT = 512;

// Green screen to alpha. The background is found by flood fill from the picture's border (pixels close to pure green), so green LEAVES
// inside the plant keep their exact colours; only a thin band along the outline gets a soft edge and loses its green cast.
async function keyOut(input) {
  const { data, info } = await sharp(input).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width, h = info.height;
  const isBackground = (i) => data[i] < 110 && data[i + 1] > 170 && data[i + 2] < 110 && data[i + 1] - Math.max(data[i], data[i + 2]) > 100;
  const bg = new Uint8Array(w * h);
  const stack = [];
  const push = (x, y) => { const k = y * w + x; if (!bg[k] && isBackground(k * 4)) { bg[k] = 1; stack.push(k); } };
  for (let x = 0; x < w; x++) { push(x, 0); push(x, h - 1); }
  for (let y = 0; y < h; y++) { push(0, y); push(w - 1, y); }
  while (stack.length) {
    const k = stack.pop(), x = k % w, y = (k - x) / w;
    if (x > 0) push(x - 1, y);
    if (x < w - 1) push(x + 1, y);
    if (y > 0) push(x, y - 1);
    if (y < h - 1) push(x, y + 1);
  }
  // Pockets of background between leaves are not reached from the border: those that are almost pure green are background too (a leaf never is).
  for (let k = 0; k < w * h; k++) { const i = k * 4; if (!bg[k] && data[i + 1] > 215 && data[i] < 90 && data[i + 2] < 90) bg[k] = 1; }
  // Pixels within BAND of the background: soft alpha from how green they still are, and the green cast removed.
  const BAND = 3;
  const near = new Uint8Array(w * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (bg[y * w + x]) continue;
    let hit = false;
    for (let dy = -BAND; dy <= BAND && !hit; dy++) for (let dx = -BAND; dx <= BAND; dx++) {
      const nx = x + dx, ny = y + dy;
      if (nx >= 0 && ny >= 0 && nx < w && ny < h && bg[ny * w + nx]) { hit = true; break; }
    }
    near[y * w + x] = hit ? 1 : 0;
  }
  for (let k = 0; k < w * h; k++) {
    const i = k * 4;
    if (bg[k]) { data[i + 3] = 0; continue; }
    if (!near[k]) continue;
    const r = data[i], g = data[i + 1], b = data[i + 2];
    const excess = g - Math.max(r, b);
    if (excess > 40) data[i + 3] = Math.round(Math.max(0, 1 - (excess - 40) / 90) * 255);
    if (excess > 0) data[i + 1] = Math.min(g, Math.max(r, b));
  }
  return sharp(data, { raw: { width: w, height: h, channels: 4 } }).png().toBuffer();
}

// Where each plant is: the runs of columns that have any picture in them, separated by empty columns. When there are more runs than plants
// (a stray leaf tip or a flower apart from its plant), the two runs with the smallest gap between them are joined, until there are
// as many as plants. If the plants touch (fewer runs), the sheet is cut into equal cells instead.
async function plantColumns(keyed, stages, width) {
  const { data, info } = await sharp(keyed).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const used = new Array(width).fill(false);
  for (let y = 0; y < info.height; y++) for (let x = 0; x < width; x++) if (data[(y * width + x) * 4 + 3] > 40) used[x] = true;
  let runs = [];
  for (let x = 0; x < width; x++) {
    if (!used[x]) continue;
    if (runs.length && x - runs[runs.length - 1][1] <= 1) runs[runs.length - 1][1] = x + 1;
    else runs.push([x, x + 1]);
  }
  while (runs.length > stages) {
    let best = 0;
    for (let i = 1; i < runs.length - 1; i++) if (runs[i + 1][0] - runs[i][1] < runs[best + 1][0] - runs[best][1]) best = i;
    runs.splice(best, 2, [runs[best][0], runs[best + 1][1]]);
  }
  if (runs.length < stages) {
    const cell = Math.floor(width / stages);
    runs = Array.from({ length: stages }, (_, i) => [i * cell, (i + 1) * cell]);
  }
  return runs;
}

(async () => {
  const [id, stagesText, file] = process.argv.slice(2);
  const stages = parseInt(stagesText, 10);
  if (!id || !stages) throw new Error('usage: import-plant.js <id> <stages> [file]');
  const cells = [];
  if (id === 'daisy' && !require('fs').existsSync(file || path.join(os.homedir(), 'Downloads', 'daisy_sheet.png'))) {
    for (const name of ['seed', 'sprout', 'seedling', 'flower', 'fruit']) cells.push(await sharp(path.join(dir, 'stage_' + name + '.png')).ensureAlpha().png().toBuffer());
  } else {
    const keyed = await keyOut(file || path.join(os.homedir(), 'Downloads', id + '_sheet.png'));
    const meta = await sharp(keyed).metadata();
    for (const [left, right] of await plantColumns(keyed, stages, meta.width))
      cells.push(await sharp(keyed).extract({ left, top: 0, width: right - left, height: meta.height }).png().toBuffer());
  }
  const trimmed = [];
  for (const cell of cells) {
    const buffer = await sharp(cell).trim({ threshold: 10 }).png().toBuffer();
    trimmed.push({ buffer, meta: await sharp(buffer).metadata() });
  }
  const scale = FINAL_HEIGHT / trimmed[trimmed.length - 1].meta.height;
  for (let i = 0; i < trimmed.length; i++) {
    const out = path.join(dir, 'plant_' + id + '_' + i + '.png');
    await sharp(trimmed[i].buffer).resize({ height: Math.max(1, Math.round(trimmed[i].meta.height * scale)) }).png().toFile(out);
    const m = await sharp(out).metadata();
    console.log(path.basename(out), m.width + 'x' + m.height);
  }
})();
