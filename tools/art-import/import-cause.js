// Cause and Effect ("Make it happen"): three 3 x 2 sheets of flat #00FF00 in ~/Downloads:
//   cause_before_sheet.png / cause_after_sheet.png (plant balloon icecream / kite ground paper) -> sciencelab/causebefore_<id>.png, causeafter_<id>.png
//   cause_tools_sheet.png (wateringcan pin sun / wind raincloud crayon) -> sciencelab/causetool_<id>.png
// Only the pairs and tools the game uses are written (no balloon, no kite). A before and its after share ONE crop frame (the union of both
// pictures' bounding boxes) so the object stays put when the picture changes.
//   node tools/art-import/import-cause.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const dl = (f) => path.join(os.homedir(), 'Downloads', f);

const PAIRS = [['plant', 0, 0], ['icecream', 2, 0], ['ground', 1, 1], ['paper', 2, 1]]; // id, column, row
const TOOLS = [['wateringcan', 0, 0], ['sun', 2, 0], ['raincloud', 1, 1], ['crayon', 2, 1]];
const COLS = 3, ROWS = 2;

async function cell(keyed, meta, col, row) {
  const left = Math.round(meta.width * col / COLS), right = Math.round(meta.width * (col + 1) / COLS);
  const top = Math.round(meta.height * row / ROWS), bottom = Math.round(meta.height * (row + 1) / ROWS);
  return sharp(keyed).extract({ left, top, width: right - left, height: bottom - top }).png().toBuffer();
}

async function bounds(buf) {
  const { info } = await sharp(buf).trim({ threshold: 10 }).png().toBuffer({ resolveWithObject: true });
  const left = -info.trimOffsetLeft, top = -info.trimOffsetTop;
  return { left, top, right: left + info.width, bottom: top + info.height };
}

(async () => {
  const before = await keyOut(dl('cause_before_sheet.png'));
  const after = await keyOut(dl('cause_after_sheet.png'));
  const tools = await keyOut(dl('cause_tools_sheet.png'));
  const meta = await sharp(before).metadata();

  for (const [id, col, row] of PAIRS) {
    const b = await cell(before, meta, col, row), a = await cell(after, meta, col, row);
    const bb = await bounds(b), ab = await bounds(a);
    const u = { left: Math.min(bb.left, ab.left), top: Math.min(bb.top, ab.top), right: Math.max(bb.right, ab.right), bottom: Math.max(bb.bottom, ab.bottom) };
    const w = u.right - u.left, h = u.bottom - u.top;
    const scale = Math.min(1, 640 / Math.max(w, h));
    for (const [name, buf] of [['causebefore_', b], ['causeafter_', a]]) {
      const crop = await sharp(buf).extract({ left: u.left, top: u.top, width: w, height: h }).png().toBuffer();
      const file = path.join(out, name + id + '.png');
      await sharp(crop).resize({ width: Math.round(w * scale), height: Math.round(h * scale) }).png().toFile(file);
      const m = await sharp(file).metadata();
      console.log(name + id, m.width + 'x' + m.height);
    }
  }

  const tmeta = await sharp(tools).metadata();
  for (const [id, col, row] of TOOLS) {
    const c = await cell(tools, tmeta, col, row);
    const trimmed = await sharp(c).trim({ threshold: 10 }).png().toBuffer();
    const file = path.join(out, 'causetool_' + id + '.png');
    await sharp(trimmed).resize({ width: 360, height: 360, fit: 'inside' }).png().toFile(file);
    const m = await sharp(file).metadata();
    console.log('causetool_' + id, m.width + 'x' + m.height);
  }
})();
