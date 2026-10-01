// Keys the magenta background out of the approved Eva reference and writes it as one 1000x1000 sprite
// (cat/cat_whole.png): soft matte from "magentaness", edge colours unmixed from the background, then
// any leftover pink/violet clamped back to fur brown. Usage: node key-cat-whole.js [--preview=out.png]
const sharp = require('./node_modules/sharp');
const path = require('path');
const SRC = path.join(__dirname, '../../art/eva/cat-v2/eva_cat_reference.png');
const OUT = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/cat/cat_whole.png');
const preview = (process.argv.find(a => a.startsWith('--preview=')) || '').slice(10);

(async () => {
  const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const W = info.width, H = info.height, N = W * H;
  const alpha = new Float32Array(N);
  // magentaness: how much both R and B exceed G
  for (let i = 0; i < N; i++) {
    const r = data[i * 4], g = data[i * 4 + 1], b = data[i * 4 + 2];
    const m = Math.min(r, b) - g;
    alpha[i] = 1 - Math.min(1, Math.max(0, (m - 40) / 170));
  }
  // anything disconnected from the border that came out transparent is a hole inside the cat: keep it opaque
  const bg = new Uint8Array(N), stack = [];
  const push = (x, y) => { const i = y * W + x; if (!bg[i] && alpha[i] < 0.98) { bg[i] = 1; stack.push(i); } };
  for (let x = 0; x < W; x++) { push(x, 0); push(x, H - 1); }
  for (let y = 0; y < H; y++) { push(0, y); push(W - 1, y); }
  while (stack.length) {
    const i = stack.pop(), x = i % W, y = (i / W) | 0;
    if (x > 0) push(x - 1, y); if (x < W - 1) push(x + 1, y); if (y > 0) push(x, y - 1); if (y < H - 1) push(x, y + 1);
  }
  for (let i = 0; i < N; i++) if (!bg[i]) alpha[i] = 1;
  // erode 1px to drop the contaminated outer ring
  const a2 = new Float32Array(alpha);
  for (let y = 1; y < H - 1; y++) for (let x = 1; x < W - 1; x++) {
    const i = y * W + x;
    a2[i] = Math.min(alpha[i], alpha[i - 1], alpha[i + 1], alpha[i - W], alpha[i + W]);
  }
  const out = Buffer.alloc(N * 4);
  for (let i = 0; i < N; i++) {
    let a = a2[i];
    let r = data[i * 4], g = data[i * 4 + 1], b = data[i * 4 + 2];
    if (a < 0.999) {
      // unmix: p = a*c + (1-a)*M  ->  c = (p - (1-a)*M)/a
      const k = Math.max(a, 0.15);
      r = (r - (1 - a) * 255) / k; g = g / k; b = (b - (1 - a) * 255) / k;
    }
    // despill: fur is brown/black, so blue never exceeds green and red never far exceeds green+blue mix
    const edge = a < 0.999 || a2[i] < 1;
    if (edge) { b = Math.min(b, g); r = Math.min(r, g * 1.6 + 8); }
    out[i * 4] = Math.max(0, Math.min(255, r)); out[i * 4 + 1] = Math.max(0, Math.min(255, g));
    out[i * 4 + 2] = Math.max(0, Math.min(255, b)); out[i * 4 + 3] = Math.round(a * 255);
  }
  // global pass: any remaining magenta-ish pixel (violet fur edges, whiskers) is pulled to neutral
  for (let i = 0; i < N; i++) {
    const r = out[i * 4], g = out[i * 4 + 1], b = out[i * 4 + 2];
    if (out[i * 4 + 3] > 0 && b > g + 6 && r > g + 6) { const v = Math.round((r + g + b) / 3 - 4); out[i * 4] = v + 4; out[i * 4 + 1] = v; out[i * 4 + 2] = v; }
  }
  let minX = W, maxX = 0, minY = H, maxY = 0;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) if (out[(y * W + x) * 4 + 3] > 20) { minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y); }
  const bbox = { left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 };
  // body centre (not bbox centre, the tail pokes out right) at x=500, paws on the art ground line y=940, 800 units tall
  const scale = 800 / bbox.height, bodyX = (minX + 0.45 * (maxX - minX) );
  const cat = await sharp(out, { raw: { width: W, height: H, channels: 4 } }).extract(bbox)
    .resize(Math.round(bbox.width * scale), 800, { kernel: 'lanczos3' }).png().toBuffer();
  const left = Math.round(500 - (bodyX - minX) * scale), top = 940 - 800;
  const canvas = await sharp({ create: { width: 1000, height: 1000, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
    .composite([{ input: cat, left, top }]).png().toBuffer();
  require('fs').writeFileSync(OUT, canvas);
  console.log('bbox', bbox, 'scale', scale.toFixed(3), 'left', left, '->', OUT);
  if (preview) await sharp({ create: { width: 1000, height: 1000, channels: 3, background: { r: 120, g: 190, b: 60 } } })
    .composite([{ input: canvas }]).png().toFile(preview);
})();
