// Cuts the AI-generated sheets (items on a solid magenta background) into single transparent sprites.
//   node tools/art-import/cut-sheets.js furniture [sheet.png]   -> art/eva/house/out/items/<name>.png
//   node tools/art-import/cut-sheets.js icons [sheet.png]       -> art/eva/house/out/icons/<name>.png
// Items are found as connected blobs (so a wandering grid does not matter) and named in reading order
// (rows top to bottom, then left to right). Add --install to also write them into the game's Resources/Art.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const ROOT = path.join(__dirname, '../..');
const BOTTOM_MARGIN = 0.04; // keep in sync with HouseScreen.FeetMargin
const SHEETS = {
  furniture: { file: 'sheet_furniture.png', names: ['sofa', 'rug', 'table', 'plant', 'chest', 'bed', 'bookshelf'], outDir: 'items', resDir: 'objects', size: 512 },
  icons: { file: 'sheet_icons.png', names: ['arrow', 'dollhouse'], outDir: 'icons', resDir: 'icons', size: 256 },
};

async function keyed(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    const m = Math.min(r, b) - g;
    if (m > 110) data[i + 3] = 0;
    else if (m > 45) { data[i + 3] = Math.round(255 * (110 - m) / 65); data[i] = Math.min(r, g + 40); data[i + 2] = Math.min(b, g + 40); }
  }
  return { data, w: info.width, h: info.height };
}

// Blobs of non-transparent pixels, merged across gaps of up to `gap` pixels; returns bounding boxes.
function blobs({ data, w, h }, gap = 6, minArea = 3000) {
  const solid = new Uint8Array(w * h);
  for (let i = 0; i < w * h; i++) solid[i] = data[i * 4 + 3] > 128 ? 1 : 0;
  const grow = (src, horizontal) => {
    const out = new Uint8Array(w * h);
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      if (!src[y * w + x]) continue;
      for (let d = -gap; d <= gap; d++) {
        const xx = horizontal ? x + d : x, yy = horizontal ? y : y + d;
        if (xx >= 0 && xx < w && yy >= 0 && yy < h) out[yy * w + xx] = 1;
      }
    }
    return out;
  };
  const dil = grow(grow(solid, true), false);
  const label = new Int32Array(w * h);
  const boxes = [];
  const stack = [];
  for (let start = 0; start < w * h; start++) {
    if (!dil[start] || label[start]) continue;
    const id = boxes.length + 1;
    const box = { x0: w, y0: h, x1: 0, y1: 0, area: 0 };
    stack.push(start); label[start] = id;
    while (stack.length) {
      const p = stack.pop(), x = p % w, y = (p - x) / w;
      if (solid[p]) { box.area++; box.x0 = Math.min(box.x0, x); box.x1 = Math.max(box.x1, x); box.y0 = Math.min(box.y0, y); box.y1 = Math.max(box.y1, y); }
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const nx = x + dx, ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const q = ny * w + nx;
        if (dil[q] && !label[q]) { label[q] = id; stack.push(q); }
      }
    }
    boxes.push(box);
  }
  return boxes.filter((b) => b.area >= minArea);
}

function readingOrder(boxes) {
  const rows = [];
  for (const b of [...boxes].sort((a, c) => (a.y0 + a.y1) - (c.y0 + c.y1))) {
    const cy = (b.y0 + b.y1) / 2;
    const row = rows.find((r) => Math.abs(r.cy - cy) < (b.y1 - b.y0) * 0.5);
    if (row) { row.items.push(b); row.cy = (row.cy * (row.items.length - 1) + cy) / row.items.length; } else rows.push({ cy, items: [b] });
  }
  return rows.sort((a, b) => a.cy - b.cy).flatMap((r) => r.items.sort((a, b) => a.x0 - b.x0));
}

(async () => {
  const kind = process.argv[2];
  const spec = SHEETS[kind];
  if (!spec) { console.error('usage: cut-sheets.js furniture|icons [sheet.png] [--install]'); process.exit(1); }
  const file = process.argv[3] && !process.argv[3].startsWith('--') ? process.argv[3] : path.join(ROOT, 'art/eva/house/ai', spec.file);
  const install = process.argv.includes('--install');
  const img = await keyed(file);
  const found = readingOrder(blobs(img));
  if (found.length !== spec.names.length) console.warn(`expected ${spec.names.length} items, found ${found.length}`);
  const outDir = path.join(ROOT, 'art/eva/house/out', spec.outDir);
  fs.mkdirSync(outDir, { recursive: true });
  const resDir = path.join(ROOT, 'EvasLearningWorld/Assets/Eva/Resources/Art', spec.resDir);
  for (let i = 0; i < Math.min(found.length, spec.names.length); i++) {
    const b = found[i];
    const w = b.x1 - b.x0 + 1, h = b.y1 - b.y0 + 1;
    const side = Math.round(Math.max(w, h) * 1.08);
    const bottom = Math.round(side * BOTTOM_MARGIN); // content sits on the bottom edge so the feet are at a known height
    const crop = await sharp(img.data, { raw: { width: img.w, height: img.h, channels: 4 } }).extract({ left: b.x0, top: b.y0, width: w, height: h }).png().toBuffer();
    const sprite = await sharp({ create: { width: side, height: side, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
      .composite([{ input: crop, left: Math.round((side - w) / 2), top: side - h - bottom }]).png().toBuffer();
    const small = await sharp(sprite).resize(spec.size, spec.size).png({ compressionLevel: 9 }).toBuffer();
    fs.writeFileSync(path.join(outDir, spec.names[i] + '.png'), small);
    if (install) fs.writeFileSync(path.join(resDir, spec.names[i] + '.png'), small);
    console.log(spec.names[i], `${w}x${h}`);
  }
})();
