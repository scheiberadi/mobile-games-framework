// Weather, the sunny day's life: critters and flowers from two one-row sheets on flat #00FF00 in ~/Downloads, plus the sunbeams:
//   sunny_critters_sheet.png (butterfly_orange butterfly_blue butterfly_yellow bee ladybug) -> sciencelab/sunnycritter_<id>.png
//   sunny_flowers_sheet.png (tulip sunflower poppy bell daisy) -> sciencelab/sunnyflower_<id>.png
//   sun_rays.png (gold light on flat black: the brightness becomes the transparency) -> sciencelab/sunrays.png
//   node tools/art-import/import-sunny.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut, runs } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const dl = (f) => path.join(os.homedir(), 'Downloads', f);
const CRITTERS = ['butterfly_orange', 'butterfly_blue', 'butterfly_yellow', 'bee', 'ladybug'];
const FLOWERS = ['tulip', 'sunflower', 'poppy', 'bell', 'daisy'];

// One row: the columns of non-empty pixels, one picture each.
async function cutRow(file, ids, prefix, size) {
  const keyed = await keyOut(dl(file));
  const meta = await sharp(keyed).metadata();
  const columns = await runs(keyed, 'x', ids.length);
  if (columns.length !== ids.length) throw new Error(file + ': found ' + columns.length + ' pictures, expected ' + ids.length);
  for (let i = 0; i < ids.length; i++) {
    const [left, right] = columns[i];
    const piece = await sharp(keyed).extract({ left, top: 0, width: right - left, height: meta.height }).png().toBuffer();
    const trimmed = await sharp(piece).trim({ threshold: 10 }).png().toBuffer();
    const name = prefix + ids[i] + '.png';
    await sharp(trimmed).resize({ width: size, height: size, fit: 'inside' }).png().toFile(path.join(out, name));
    const m = await sharp(path.join(out, name)).metadata();
    console.log(name, m.width + 'x' + m.height);
  }
}

// Light on black: the brightest channel is the opacity and the colour is divided by it (so it fades, not greys, over the scene).
async function rays() {
  const { data, info } = await sharp(dl('sun_rays.png')).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const rgba = Buffer.alloc(info.width * info.height * 4);
  for (let i = 0; i < info.width * info.height; i++) {
    const r = data[i * 3], g = data[i * 3 + 1], b = data[i * 3 + 2];
    const a = Math.max(r, g, b);
    rgba[i * 4] = a ? Math.min(255, Math.round(r * 255 / a)) : 255;
    rgba[i * 4 + 1] = a ? Math.min(255, Math.round(g * 255 / a)) : 220;
    rgba[i * 4 + 2] = a ? Math.min(255, Math.round(b * 255 / a)) : 120;
    rgba[i * 4 + 3] = a;
  }
  await sharp(rgba, { raw: { width: info.width, height: info.height, channels: 4 } }).resize({ width: 1280 }).png().toFile(path.join(out, 'sunrays.png'));
  console.log('sunrays.png');
}

(async () => {
  await cutRow('sunny_critters_sheet.png', CRITTERS, 'sunnycritter_', 260);
  await cutRow('sunny_flowers_sheet.png', FLOWERS, 'sunnyflower_', 360);
  await rays();
})();
