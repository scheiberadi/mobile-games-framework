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
    const cellWidth = Math.floor(meta.width / stages);
    for (let i = 0; i < stages; i++) cells.push(await sharp(keyed).extract({ left: i * cellWidth, top: 0, width: cellWidth, height: meta.height }).png().toBuffer());
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
