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

// Green screen to alpha: how much greener than red/blue a pixel is says how much of it is background; the green cast on the edges is removed.
async function keyOut(input) {
  const { data, info } = await sharp(input).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    const excess = g - Math.max(r, b);
    let alpha = 1;
    if (excess > 40) alpha = Math.max(0, 1 - (excess - 40) / 90);
    data[i + 3] = Math.min(data[i + 3], Math.round(alpha * 255));
    if (excess > 0 && alpha < 1) data[i + 1] = Math.min(g, Math.max(r, b));
  }
  return sharp(data, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toBuffer();
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
  if (id === 'daisy') {
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
