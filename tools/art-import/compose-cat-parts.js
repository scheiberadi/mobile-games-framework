// Composites ChatGPT-generated, isolated-on-magenta cat-part sheets into full 1000x1000 transparent
// canvases, each part placed at its own measured box from cat-boxes.json - the exact layout
// RigFactory.CreateEva's pivots (Assets/Eva/App/Characters/RigFactory.cs) already expect, so the result
// drops straight into Resources/Art/cat/<name>.png with no code change.
//
// Unlike tools/art-import/cut-sheets.js (which crops each item to its own tight square, bottom-anchored,
// for a UI icon/object), a cat part must land inside a SPECIFIC rectangle on a canvas shared with every
// other part, or the pieces won't line up into one cat. This script fits (not stretches) each found blob's
// content into its target box, preserving aspect ratio, centred within the box, then pastes that onto a
// full transparent 1000x1000 canvas.
//
// Usage: node tools/art-import/compose-cat-parts.js <sheet.png> <name1> [<name2> ...] [--grid COLSxROWS] [--install]
//   - names must be a subset of cat-boxes.json's own "order" list, in the sheet's own reading order.
//   - --grid is recommended whenever a sheet has more than one part (a cat's parts are exactly the kind of
//     "richer than a plain isolated icon" content docs/kids-games/m4-handover.md says needs it - limbs/ears
//     can plausibly touch or bleed between cells otherwise).
//   - with a single name and no --grid, the whole (non-grid) sheet is treated as one part.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const ROOT = path.resolve(__dirname, '../..');
const catDir = path.join(ROOT, 'art/eva/cat-v2');
const resDir = path.join(ROOT, 'EvasLearningWorld/Assets/Eva/Resources/Art/cat');
const CANVAS = 1000;

const catData = JSON.parse(fs.readFileSync(path.join(catDir, 'cat-boxes.json'), 'utf8'));
const boxes = catData.boxes;

// Copied verbatim from tools/art-import/cut-sheets.js's own keyed() - do not approximate this (see
// docs/kids-games/m4-handover.md's "things that went wrong" #3 on why a hand-rolled key threshold left a
// visible fringe last time this warning was ignored).
async function keyed(file, bg = 'magenta') {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  if (bg === 'magenta') {
    for (let i = 0; i < data.length; i += 4) {
      const r = data[i], g = data[i + 1], b = data[i + 2];
      const m = Math.min(r, b) - g;
      if (m > 110) data[i + 3] = 0;
      else if (m > 45) { data[i + 3] = Math.round(255 * (110 - m) / 65); data[i] = Math.min(r, g + 40); data[i + 2] = Math.min(b, g + 40); }
    }
  }
  return { data, w: info.width, h: info.height };
}

// Largest connected blob within a rect - copied from cut-sheets.js's largestBlobInRect (same reasoning:
// a cell's own small bleed fragment from a neighbour must never be mistaken for that cell's real content).
function largestBlobInRect({ data, w: imgW }, cx0, cy0, cx1, cy1, gap = 4) {
  const w = cx1 - cx0, h = cy1 - cy0;
  const solid = new Uint8Array(w * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) solid[y * w + x] = data[((cy0 + y) * imgW + (cx0 + x)) * 4 + 3] > 40 ? 1 : 0;
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
  let best = null;
  for (let start = 0; start < w * h; start++) {
    if (!dil[start] || label[start]) continue;
    const box = { x0: w, y0: h, x1: 0, y1: 0, area: 0 };
    const stack = [start]; label[start] = start + 1;
    while (stack.length) {
      const p = stack.pop(), x = p % w, y = (p - x) / w;
      if (solid[p]) { box.area++; box.x0 = Math.min(box.x0, x); box.x1 = Math.max(box.x1, x); box.y0 = Math.min(box.y0, y); box.y1 = Math.max(box.y1, y); }
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const nx = x + dx, ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const q = ny * w + nx;
        if (dil[q] && !label[q]) { label[q] = start + 1; stack.push(q); }
      }
    }
    if (!best || box.area > best.area) best = box;
  }
  return best && { x0: best.x0 + cx0, y0: best.y0 + cy0, x1: best.x1 + cx0, y1: best.y1 + cy0, area: best.area };
}

function gridBoxes(img, cols, rows, count) {
  const cellW = img.w / cols, cellH = img.h / rows;
  const found = [];
  for (let i = 0; i < count; i++) {
    const row = Math.floor(i / cols), col = i % cols;
    const lastInRow = col === cols - 1 || i === count - 1;
    const cx0 = Math.round(col * cellW), cy0 = Math.round(row * cellH);
    const cx1 = Math.round((lastInRow ? cols : col + 1) * cellW), cy1 = Math.round((row + 1) * cellH);
    found.push(largestBlobInRect(img, cx0, cy0, cx1, cy1));
  }
  return found;
}

(async () => {
  const args = process.argv.slice(2);
  const install = args.includes('--install');
  const gridArg = args.find((a) => a.startsWith('--grid'));
  const rest = args.filter((a) => a !== '--install' && a !== gridArg);
  const [sheetFile, ...names] = rest;
  if (!sheetFile || names.length === 0) {
    console.error('usage: compose-cat-parts.js <sheet.png> <name1> [<name2> ...] [--grid COLSxROWS] [--install]');
    console.error('names must be from: ' + catData.order.join(', '));
    process.exit(1);
  }
  for (const name of names) {
    if (!boxes[name]) { console.error('unknown part name: ' + name + ' (not in cat-boxes.json)'); process.exit(1); }
  }

  const img = await keyed(sheetFile, 'magenta');
  let found;
  if (gridArg) {
    const [cols, rows] = gridArg.split('=')[1].split('x').map(Number);
    found = gridBoxes(img, cols, rows, names.length);
  } else if (names.length === 1) {
    found = [largestBlobInRect(img, 0, 0, img.w, img.h)];
  } else {
    console.error('multiple names need --grid=COLSxROWS to know how to split the sheet');
    process.exit(1);
  }

  fs.mkdirSync(catDir, { recursive: true });
  if (install) fs.mkdirSync(resDir, { recursive: true });

  for (let i = 0; i < names.length; i++) {
    const name = names[i];
    const blob = found[i];
    if (!blob) { console.warn(name + ': no content found in its cell - skipped'); continue; }
    const [tx0, ty0, tx1, ty1] = boxes[name];
    const targetW = tx1 - tx0, targetH = ty1 - ty0;
    const contentW = blob.x1 - blob.x0 + 1, contentH = blob.y1 - blob.y0 + 1;

    // Fit (not stretch) the found content into its target box, preserving aspect ratio, centred within
    // the box - a hand-drawn isolated part won't naturally match the target box's exact aspect ratio.
    const scale = Math.min(targetW / contentW, targetH / contentH);
    const placedW = Math.round(contentW * scale), placedH = Math.round(contentH * scale);
    const placedX = tx0 + Math.round((targetW - placedW) / 2);
    const placedY = ty0 + Math.round((targetH - placedH) / 2);

    const crop = await sharp(img.data, { raw: { width: img.w, height: img.h, channels: 4 } })
      .extract({ left: blob.x0, top: blob.y0, width: contentW, height: contentH })
      .resize(placedW, placedH)
      .png().toBuffer();

    const canvas = await sharp({ create: { width: CANVAS, height: CANVAS, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
      .composite([{ input: crop, left: placedX, top: placedY }])
      .png({ compressionLevel: 9 }).toBuffer();

    const outFile = path.join(catDir, 'out_' + name + '.png');
    fs.mkdirSync(path.dirname(outFile), { recursive: true });
    fs.writeFileSync(outFile, canvas);
    // "EarL" -> "cat_earL.png" etc - first letter only lowercased, matching the existing Resources/Art/cat
    // filenames exactly (RigFactory.CreateEva looks these up as literal, case-sensitive sprite keys).
    const fileName = 'cat_' + name.charAt(0).toLowerCase() + name.slice(1) + '.png';
    if (install) fs.writeFileSync(path.join(resDir, fileName), canvas);
    console.log(name, `content ${contentW}x${contentH} -> placed ${placedW}x${placedH} at (${placedX},${placedY}) in a ${targetW}x${targetH} box`);
  }
})();
