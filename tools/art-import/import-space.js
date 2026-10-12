// Space ("Build the solar system"): the eight planets, cut out of two one-row sheets on flat #00FF00 in ~/Downloads:
//   space_planets_inner.png (mercury venus earth mars), space_planets_outer.png (jupiter saturn uranus neptune) -> sciencelab/planet_<id>.png
// The background (sun on the left, eight orbit lines) is imported with: node import-scene-bg.js space_bg.png space_bg
//   node tools/art-import/import-space.js
// Neighbouring planets can touch (Saturn's ring reaches Jupiter), so each planet is its own connected piece of the keyed picture,
// found by flood fill, and only that piece is cut out.
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const { keyOut } = require('./keyout');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const dl = (f) => path.join(os.homedir(), 'Downloads', f);

async function cut(file, ids) {
  const keyed = await keyOut(dl(file));
  const { data, info } = await sharp(keyed).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width, h = info.height;
  const label = new Int32Array(w * h);
  const pieces = [];
  for (let start = 0; start < w * h; start++) {
    if (label[start] || data[start * 4 + 3] <= 40) continue;
    const id = pieces.length + 1;
    const stack = [start];
    label[start] = id;
    let minX = w, maxX = 0, minY = h, maxY = 0, size = 0;
    while (stack.length) {
      const k = stack.pop(), x = k % w, y = (k - x) / w;
      size++;
      if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y;
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const nx = x + dx, ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const n = ny * w + nx;
        if (!label[n] && data[n * 4 + 3] > 40) { label[n] = id; stack.push(n); }
      }
    }
    pieces.push({ id, minX, maxX, minY, maxY, size });
  }
  const planets = pieces.filter(p => p.size > 2000).sort((a, b) => a.minX - b.minX);
  if (planets.length !== ids.length) throw new Error(file + ': found ' + planets.length + ' planets, expected ' + ids.length);
  for (let i = 0; i < ids.length; i++) {
    const p = planets[i];
    const pw = p.maxX - p.minX + 1, ph = p.maxY - p.minY + 1;
    const piece = Buffer.alloc(pw * ph * 4);
    for (let y = 0; y < ph; y++) for (let x = 0; x < pw; x++) {
      const s = ((p.minY + y) * w + p.minX + x), d = (y * pw + x) * 4;
      if (label[s] === p.id) { piece[d] = data[s * 4]; piece[d + 1] = data[s * 4 + 1]; piece[d + 2] = data[s * 4 + 2]; piece[d + 3] = data[s * 4 + 3]; }
    }
    const name = 'planet_' + ids[i] + '.png';
    await sharp(piece, { raw: { width: pw, height: ph, channels: 4 } }).resize({ width: 400, height: 400, fit: 'inside' }).png().toFile(path.join(out, name));
    const m = await sharp(path.join(out, name)).metadata();
    console.log(name, m.width + 'x' + m.height);
  }
}

(async () => {
  await cut('space_planets_inner.png', ['mercury', 'venus', 'earth', 'mars']);
  await cut('space_planets_outer.png', ['jupiter', 'saturn', 'uranus', 'neptune']);
})();
