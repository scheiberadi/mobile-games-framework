// Eva's talking mouth. ChatGPT paints a copy of each cat with an open mouth (~/Downloads/eva_talk.png for the plain cat, dress_<id>_talk.png
// for each outfit); the copies drift a little everywhere else, so only the mouth is used: each talking picture is registered to its closed
// picture (scale and shift searched on the eyes and nose, which do not move), the mouth is cut out with a soft oval and written as a
// 1000x1000 overlay: cat/cat_mouth.png (plain Eva) and cat/cat_dressed_<id>_mouth.png. CatMotion fades the overlay in while Eva talks.
// Usage: node import-talk.js [--preview=out.png] [plain|sun|rain|snow|wind ...]
const sharp = require('./node_modules/sharp');
const path = require('path');
const os = require('os');
const { key, bandOf, REF, OUTDIR } = require('./import-dressed');

const preview = (process.argv.find(a => a.startsWith('--preview=')) || '').slice(10);
const ids = process.argv.slice(2).filter(a => !a.startsWith('--'));
const EYES = { x0: 330, x1: 560, y0: 300, y1: 420 };   // compared when registering: eyes and nose, never the mouth
const MOUTH = { cx: 440, cy: 455, rx: 55, ry: 40 };       // the oval cut out of the talking picture (1000 canvas); refined per cat below
const FEATHER = 14;
const W = 1000;

async function load(file) {
  const meta = await sharp(file).metadata();
  if (meta.hasAlpha) {
    const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const alpha = new Float32Array(info.width * info.height);
    for (let i = 0; i < alpha.length; i++) alpha[i] = data[i * 4 + 3] / 255;
    return { out: data, alpha, W: info.width, H: info.height };
  }
  return key(file);
}

// The talking picture placed on a 1000 canvas at `scale` with its top-left at (left, top); RGBA raw.
async function place(src, scale, left, top) {
  const size = Math.round(src.W * scale);
  const scaled = await sharp(src.out, { raw: { width: src.W, height: src.H, channels: 4 } }).resize(size, size, { kernel: 'lanczos3' }).png().toBuffer();
  const big = await sharp({ create: { width: 3000, height: 3000, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
    .composite([{ input: scaled, left: Math.round(left) + 1000, top: Math.round(top) + 1000 }]).png().toBuffer();
  return sharp(big).extract({ left: 1000, top: 1000, width: W, height: W }).raw().toBuffer();
}

// Luminance over a rectangle, on mid grey where transparent.
function lumaRect(raw, rect, dx = 0, dy = 0) {
  const out = [];
  for (let y = rect.y0; y < rect.y1; y += 2) for (let x = rect.x0; x < rect.x1; x += 2) {
    const i = ((y - dy) * W + (x - dx)) * 4, a = raw[i + 3] / 255;
    out.push((0.3 * raw[i] + 0.59 * raw[i + 1] + 0.11 * raw[i + 2]) * a + 128 * (1 - a));
  }
  return out;
}

function ssd(a, b) { let s = 0; for (let i = 0; i < a.length; i++) { const d = a[i] - b[i]; s += d * d; } return s / a.length; }

async function register(closedRaw, closedAlpha, talk) {
  const rb = bandOf(closedAlpha, W, W), b = bandOf(talk.alpha, talk.W, talk.H);
  const s0 = (rb.maxX - rb.minX) / (b.maxX - b.minX);
  const left0 = (rb.maxX + rb.minX) / 2 - ((b.maxX + b.minX) / 2) * s0, top0 = rb.bottom - b.bottom * s0;
  const EX = 445, EY = 346;
  const target = lumaRect(closedRaw, EYES);
  let best = { err: Infinity };
  for (let ds = -0.06; ds <= 0.0601; ds += 0.015) {
    const scale = s0 * (1 + ds);
    // keep the eyes' middle fixed while the scale changes
    const left = EX - (EX - left0) * scale / s0, top = EY - (EY - top0) * scale / s0;
    const raw = await place(talk, scale, left, top);
    for (let dy = -30; dy <= 30; dy += 3) for (let dx = -30; dx <= 30; dx += 3) {
      const err = ssd(target, lumaRect(raw, EYES, dx, dy));
      if (err < best.err) best = { err, ds, dx, dy, scale, raw, left, top };
    }
  }
  // fine pass around the best shift
  const raw = best.raw;
  let fine = { err: best.err, dx: best.dx, dy: best.dy };
  for (let dy = best.dy - 3; dy <= best.dy + 3; dy++) for (let dx = best.dx - 3; dx <= best.dx + 3; dx++) {
    const err = ssd(target, lumaRect(raw, EYES, dx, dy));
    if (err < fine.err) fine = { err, dx, dy };
  }
  const shifted = await place(talk, best.scale, best.left + fine.dx, best.top + fine.dy);
  return { raw: shifted, err: fine.err, scale: best.scale, dx: fine.dx, dy: fine.dy };
}

// The soft oval of the mouth cut out of the registered talking picture (everything else transparent).
function cutMouth(talkRaw, mouth) {
  const out = Buffer.alloc(W * W * 4);
  for (let y = 0; y < W; y++) for (let x = 0; x < W; x++) {
    const nx = (x - mouth.cx) / mouth.rx, ny = (y - mouth.cy) / mouth.ry, d = Math.sqrt(nx * nx + ny * ny);
    if (d > 1.0) continue;
    const edge = Math.min(1, (1 - d) * Math.min(mouth.rx, mouth.ry) / FEATHER);
    const i = (y * W + x) * 4;
    out[i] = talkRaw[i]; out[i + 1] = talkRaw[i + 1]; out[i + 2] = talkRaw[i + 2];
    out[i + 3] = Math.round(talkRaw[i + 3] * Math.max(0, edge));
  }
  return out;
}

(async () => {
  const cases = (ids.length ? ids : ['plain', 'sun', 'rain', 'snow', 'wind']).map(id => id === 'plain'
    ? { id, closed: REF, talk: path.join(os.homedir(), 'Downloads', 'eva_talk.png'), out: path.join(OUTDIR, 'cat_mouth.png') }
    : { id, closed: path.join(OUTDIR, 'cat_dressed_' + id + '.png'), talk: path.join(os.homedir(), 'Downloads', 'dress_' + id + '_talk.png'), out: path.join(OUTDIR, 'cat_dressed_' + id + '_mouth.png') });
  const tiles = [];
  for (const c of cases) {
    const closed = await sharp(c.closed).ensureAlpha().raw().toBuffer();
    const closedAlpha = new Float32Array(W * W);
    for (let i = 0; i < closedAlpha.length; i++) closedAlpha[i] = closed[i * 4 + 3] / 255;
    const talk = await load(c.talk);
    const reg = await register(closed, closedAlpha, talk);
    const mouth = Buffer.from(cutMouth(reg.raw, MOUTH));
    await sharp(mouth, { raw: { width: W, height: W, channels: 4 } }).png().toFile(c.out);
    console.log(c.id, 'scale', reg.scale.toFixed(3), 'shift', reg.dx, reg.dy, 'err', reg.err.toFixed(1), '->', path.basename(c.out));
    if (preview) {
      const base = await sharp({ create: { width: W, height: W, channels: 4, background: '#9fd9ff' } })
        .composite([{ input: c.closed }, { input: await sharp(mouth, { raw: { width: W, height: W, channels: 4 } }).png().toBuffer() }]).png().toBuffer();
      tiles.push(await sharp(base).extract({ left: 250, top: 250, width: 400, height: 300 }).png().toBuffer());
    }
  }
  if (preview) {
    await sharp({ create: { width: 800, height: 300 * Math.ceil(tiles.length / 2), channels: 3, background: '#fff' } })
      .composite(tiles.map((t, i) => ({ input: t, left: (i % 2) * 400, top: Math.floor(i / 2) * 300 }))).png().toFile(preview);
  }
})();
