// Dress for Weather: Eva painted wearing a weather outfit (art/eva/sciencelab/dress_<id>.png in ~/Downloads, flat blue-grey background).
// Keys the background out (soft matte, edge colour unmixed from the local background), then fits each cat to cat/cat_whole.png:
// paws on the art ground line, and the lower body + tail band the same width and centre, so a cross-fade between plain and dressed
// Eva does not jump. Writes cat/cat_dressed_<id>.png (1000x1000). Usage: node import-dressed.js [--preview=out.png] [id...]
const sharp = require('./node_modules/sharp');
const path = require('path');
const os = require('os');
const REF = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/cat/cat_whole.png');
const OUTDIR = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/cat');
const ids = process.argv.slice(2).filter(a => !a.startsWith('--'));
const preview = (process.argv.find(a => a.startsWith('--preview=')) || '').slice(10);
const BAND = 0.18; // fraction of the frame, measured up from the cat's own paws

function bandOf(alpha, W, H) {
  let bottom = 0;
  for (let y = H - 1; y >= 0 && !bottom; y--) for (let x = 0; x < W; x++) if (alpha[y * W + x] > 0.5) { bottom = y; break; }
  let minX = W, maxX = 0;
  const top = Math.max(0, bottom - Math.round(BAND * H));
  for (let y = top; y <= bottom; y++) for (let x = 0; x < W; x++) if (alpha[y * W + x] > 0.5) { minX = Math.min(minX, x); maxX = Math.max(maxX, x); }
  return { bottom, minX, maxX };
}

async function key(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const W = info.width, H = info.height, N = W * H;
  // background colour at each pixel: linear blend of the left and right border colour of its row (the backdrop is a soft gradient)
  const edge = (x, y) => { let r = 0, g = 0, b = 0; for (let d = 0; d < 6; d++) { const i = (y * W + x + d * (x ? -1 : 1)) * 4; r += data[i]; g += data[i + 1]; b += data[i + 2]; } return [r / 6, g / 6, b / 6]; };
  const bgRow = [];
  for (let y = 0; y < H; y++) bgRow.push([edge(1, y), edge(W - 2, y)]);
  const bgAt = (x, y) => { const t = x / (W - 1), a = bgRow[y][0], b = bgRow[y][1]; return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t]; };
  const alpha = new Float32Array(N);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const i = (y * W + x) * 4, B = bgAt(x, y);
    const d = Math.hypot(data[i] - B[0], data[i + 1] - B[1], data[i + 2] - B[2]);
    alpha[y * W + x] = Math.min(1, Math.max(0, (d - 14) / 46));
  }
  // anything transparent but not connected to the border is inside the cat (eye shine, white of the pompom): keep opaque
  const bg = new Uint8Array(N), stack = [];
  const push = (x, y) => { const k = y * W + x; if (!bg[k] && alpha[k] < 0.98) { bg[k] = 1; stack.push(k); } };
  for (let x = 0; x < W; x++) { push(x, 0); push(x, H - 1); }
  for (let y = 0; y < H; y++) { push(0, y); push(W - 1, y); }
  while (stack.length) {
    const k = stack.pop(), x = k % W, y = (k / W) | 0;
    if (x > 0) push(x - 1, y); if (x < W - 1) push(x + 1, y); if (y > 0) push(x, y - 1); if (y < H - 1) push(x, y + 1);
  }
  for (let k = 0; k < N; k++) if (!bg[k]) alpha[k] = 1;
  const out = Buffer.alloc(N * 4);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const k = y * W + x, i = k * 4, a = alpha[k], B = bgAt(x, y);
    let c = [data[i], data[i + 1], data[i + 2]];
    if (a < 0.999) { const q = Math.max(a, 0.2); c = c.map((v, j) => (v - (1 - a) * B[j]) / q); }
    out[i] = Math.max(0, Math.min(255, c[0])); out[i + 1] = Math.max(0, Math.min(255, c[1])); out[i + 2] = Math.max(0, Math.min(255, c[2]));
    out[i + 3] = Math.round(a * 255);
  }
  return { out, alpha, W, H };
}

(async () => {
  const ref = await sharp(REF).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const rA = new Float32Array(ref.info.width * ref.info.height);
  for (let i = 0; i < rA.length; i++) rA[i] = ref.data[i * 4 + 3] / 255;
  const rb = bandOf(rA, ref.info.width, ref.info.height);
  const rw = rb.maxX - rb.minX, rc = (rb.maxX + rb.minX) / 2;
  const previews = [];
  for (const id of ids) {
    const { out, alpha, W, H } = await key(path.join(os.homedir(), 'Downloads', 'dress_' + id + '.png'));
    const b = bandOf(alpha, W, H);
    const s = rw / (b.maxX - b.minX);
    const size = Math.round(W * s);
    const scaled = await sharp(out, { raw: { width: W, height: H, channels: 4 } }).resize(size, size, { kernel: 'lanczos3' }).png().toBuffer();
    const left = Math.round(rc - ((b.maxX + b.minX) / 2) * s), top = Math.round(rb.bottom - b.bottom * s);
    const big = await sharp({ create: { width: 3000, height: 3000, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
      .composite([{ input: scaled, left: left + 1000, top: top + 1000 }]).png().toBuffer();
    const canvas = await sharp(big).extract({ left: 1000, top: 1000, width: 1000, height: 1000 }).png().toBuffer();
    // extract() cannot take offsets outside the picture, so composite already clipped; a clothing item may stick out above y=0 only if the hat is huge
    require('fs').writeFileSync(path.join(OUTDIR, 'cat_dressed_' + id + '.png'), canvas);
    console.log(id, 'scale', s.toFixed(3), 'left', left, 'top', top);
    previews.push(canvas);
  }
  if (preview) {
    const tiles = [];
    for (let i = 0; i < previews.length; i++) {
      const base = await sharp({ create: { width: 1000, height: 1000, channels: 4, background: { r: 120, g: 190, b: 60, alpha: 1 } } })
        .composite([{ input: previews[i] }, { input: await sharp(REF).ensureAlpha().linear([1, 1, 1, 0.35], [0, 0, 0, 0]).png().toBuffer() }]).png().toBuffer().then(b => sharp(b).resize(500).png().toBuffer());
      tiles.push({ input: base, left: (i % 2) * 500, top: Math.floor(i / 2) * 500 });
    }
    await sharp({ create: { width: 1000, height: 1000, channels: 3, background: '#fff' } }).composite(tiles).png().toFile(preview);
  }
})();
