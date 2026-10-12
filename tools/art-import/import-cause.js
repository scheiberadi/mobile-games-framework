// Cause and Effect ("Make it happen"): before / after pictures and round tool badges, all on flat #00FF00 in ~/Downloads:
//   cause_before_sheet.png / cause_after_sheet.png (3 x 2: plant balloon icecream / kite ground paper; only plant icecream ground paper used)
//   cause_balloon_before.png / cause_balloon_after.png (one picture each)
//   cause3_before_sheet.png / cause3_after_sheet.png (2 x 2: bread boot / ice lamp)
//   cause_tools_sheet.png (3 x 2: wateringcan pin sun / wind raincloud crayon; used wateringcan pin sun raincloud crayon)
//   cause3_tools_sheet.png (2 x 2: oven soap / snowflake switch)
// -> sciencelab/causebefore_<id>.png, causeafter_<id>.png, causetool_<id>.png. A before and its after share ONE crop frame (the union of both
// pictures' bounding boxes) so the object stays put when the picture changes.
//   node tools/art-import/import-cause.js
const path = require('path');
const os = require('os');
const fs = require('fs');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const dl = (f) => path.join(os.homedir(), 'Downloads', f);

async function cell(keyed, meta, cols, rows, col, row) {
  const left = Math.round(meta.width * col / cols), right = Math.round(meta.width * (col + 1) / cols);
  const top = Math.round(meta.height * row / rows), bottom = Math.round(meta.height * (row + 1) / rows);
  return sharp(keyed).extract({ left, top, width: right - left, height: bottom - top }).png().toBuffer();
}

async function bounds(buf) {
  const { info } = await sharp(buf).trim({ threshold: 10 }).png().toBuffer({ resolveWithObject: true });
  const left = -info.trimOffsetLeft, top = -info.trimOffsetTop;
  return { left, top, right: left + info.width, bottom: top + info.height };
}

async function pairs(beforeFile, afterFile, cols, rows, list) {
  const before = await keyOut(dl(beforeFile));
  const after = await keyOut(dl(afterFile));
  const meta = await sharp(before).metadata();
  for (const [id, col, row] of list) {
    const b = await cell(before, meta, cols, rows, col, row), a = await cell(after, meta, cols, rows, col, row);
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
}

async function tools(file, cols, rows, list) {
  const keyed = await keyOut(dl(file));
  const meta = await sharp(keyed).metadata();
  for (const [id, col, row] of list) {
    const c = await cell(keyed, meta, cols, rows, col, row);
    const trimmed = await sharp(c).trim({ threshold: 10 }).png().toBuffer();
    const target = path.join(out, 'causetool_' + id + '.png');
    await sharp(trimmed).resize({ width: 360, height: 360, fit: 'inside' }).png().toFile(target);
    const m = await sharp(target).metadata();
    console.log('causetool_' + id, m.width + 'x' + m.height);
  }
}

// Glass and glow let the green screen show through (a green tint in the glass, green between the lamp's rays): clear those pixels
// and pull the remaining green fringe back to the neighbouring colours.
async function scrub(id) {
  for (const kind of ['causebefore_', 'causeafter_']) {
    const file = path.join(out, kind + id + '.png');
    const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    for (let i = 0; i < data.length; i += 4) {
      const r = data[i], g = data[i + 1], b = data[i + 2];
      if (g - r > 35 && g - b > 35) data[i + 3] = 0;
      else if (data[i + 3] > 0 && g > Math.max(r, b)) data[i + 1] = Math.max(r, b);
    }
    await sharp(data, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toFile(file + '.tmp');
    fs.renameSync(file + '.tmp', file);
  }
  console.log('scrubbed', id);
}

(async () => {
  await pairs('cause_before_sheet.png', 'cause_after_sheet.png', 3, 2, [['plant', 0, 0], ['icecream', 2, 0], ['ground', 1, 1], ['paper', 2, 1]]);
  await pairs('cause_balloon_before.png', 'cause_balloon_after.png', 1, 1, [['balloon', 0, 0]]);
  await pairs('cause3_before_sheet.png', 'cause3_after_sheet.png', 2, 2, [['bread', 0, 0], ['boot', 1, 0], ['ice', 0, 1], ['lamp', 1, 1]]);
  await scrub('lamp');
  await scrub('ice');
  await tools('cause_tools_sheet.png', 3, 2, [['wateringcan', 0, 0], ['pin', 1, 0], ['sun', 2, 0], ['raincloud', 1, 1], ['crayon', 2, 1]]);
  await tools('cause3_tools_sheet.png', 2, 2, [['oven', 0, 0], ['soap', 1, 0], ['snowflake', 0, 1], ['switch', 1, 1]]);
})();
