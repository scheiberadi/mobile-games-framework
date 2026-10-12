// Shared green-screen helpers for the art importers (sheets on flat #00FF00).
const path = require('path');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

// Green screen to alpha. The background is found by flood fill from the picture's border (pixels close to pure green), so green parts of
// the picture keep their exact colours; pockets of almost pure green inside (between leaves) are background too; only a thin band along the
// outline gets a soft edge and loses its green cast.
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
  for (let k = 0; k < w * h; k++) { const i = k * 4; if (!bg[k] && data[i + 1] > 215 && data[i] < 90 && data[i + 2] < 90) bg[k] = 1; }
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

// Runs of non-empty lines along one axis of a keyed picture region ('x': columns, 'y': rows), merged down to `count` runs by joining the
// two neighbours with the smallest gap (a stray tip apart from its picture).
async function runs(keyed, axis, count) {
  const { data, info } = await sharp(keyed).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width, h = info.height, n = axis === 'x' ? w : h;
  const used = new Array(n).fill(false);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (data[(y * w + x) * 4 + 3] > 40) used[axis === 'x' ? x : y] = true;
  let r = [];
  for (let i = 0; i < n; i++) {
    if (!used[i]) continue;
    if (r.length && i - r[r.length - 1][1] <= 1) r[r.length - 1][1] = i + 1;
    else r.push([i, i + 1]);
  }
  while (r.length > count) {
    let best = 0;
    for (let i = 1; i < r.length - 1; i++) if (r[i + 1][0] - r[i][1] < r[best + 1][0] - r[best][1]) best = i;
    r.splice(best, 2, [r[best][0], r[best + 1][1]]);
  }
  return r;
}

module.exports = { keyOut, runs };
