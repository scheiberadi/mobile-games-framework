// Workshop build games: cut one sheet (finished picture on top, the same pieces taken apart below, on flat #ff00ff) into
//   Resources/Art/workshop/build_<game>_<part>.png   one sprite per distinct part, all at the finished picture's scale
//   tools/art-import/out/full_<game>.png, check_<game>.png  the finished picture, and the parts put back at their places (for checking by eye)
//   Resources/Art/workshop/build_<game>_<part>_slot.png  the part's dotted outline (white dashes with a dark edge), drawn from its own alpha, Pad px bigger on every side
//   Resources/Art/workshop/build_<game>.json         where each part sits, in finished-picture pixels (x/y = top-left of the sprite)
// Each part is found in the finished picture by template matching (position and scale), with optional hand fixes below.
//   node tools/art-import/import-workshop-build.js car [rocket house boat robot bridge]
const fs = require('fs');
const os = require('os');
const path = require('path');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

const resOut = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'workshop');
const jsonOut = path.join(__dirname, 'out');

// Parts in reading order of the lower half (rows top to bottom, left to right). Equal names are identical twins: one sprite, two slots.
const GAMES = {
  car: ['body', 'wheel', 'wheel', 'window', 'door', 'headlight', 'tailpipe'],
  rocket: ['body', 'nose', 'window', 'finl', 'finr', 'flame'],
  house: ['walls', 'roof', 'window', 'window', 'door', 'chimney', 'steps'],
  boat: ['hull', 'mast', 'sailbig', 'sailsmall', 'flag', 'ring'],
  robot: ['body', 'head', 'armL', 'armR', 'legL', 'legR', 'antenna'],
  bridge: ['bankl', 'river', 'pier', 'bankr', 'deck', 'rail', 'rail'],
};
// Where each part sits in the finished picture, read by eye off a grid: [x0, y0, x1, y1] in finished-picture pixels, one rect per
// slot (twins list two). The sprite is scaled to fit its rect (aspect kept) and centred in it. Automatic template matching was tried
// first and is unreliable on plain base pieces (a body, walls) and identical twins, so positions are hand-read.
// Parts stretched to exactly fill their rect (drawn narrower/shorter in the lower half than where they sit in the finished picture).
const STRETCH = new Set(['river', 'mast', 'headlight', 'pier', 'antenna', 'rocket:body']);
const RECTS = {
  car: { body: [[40, 2, 808, 422]], wheel: [[105, 305, 292, 495], [560, 305, 745, 495]], window: [[290, 50, 568, 210]], door: [[311, 200, 562, 382]], headlight: [[772, 230, 826, 332]], tailpipe: [[0, 355, 52, 402]] },
  rocket: { body: [[78, 130, 336, 487]], nose: [[100, 0, 318, 135]], window: [[125, 180, 290, 350]], finl: [[0, 342, 120, 528]], finr: [[295, 342, 415, 528]], flame: [[138, 487, 278, 665]] },
  house: { roof: [[0, 2, 663, 287]], walls: [[57, 270, 606, 600]], window: [[84, 330, 224, 474], [439, 330, 579, 474]], door: [[246, 312, 418, 532]], chimney: [[448, 12, 581, 165]], steps: [[206, 530, 457, 619]] },
  boat: { hull: [[2, 405, 583, 594]], mast: [[248, 2, 288, 435]], sailbig: [[285, 90, 555, 415]], sailsmall: [[64, 152, 250, 400]], flag: [[282, 20, 410, 90]], ring: [[383, 437, 520, 570]] },
  robot: { body: [[63, 318, 437, 528]], head: [[92, 103, 407, 315]], armL: [[0, 338, 125, 580]], armR: [[375, 338, 499, 580]], legL: [[100, 527, 240, 714]], legR: [[262, 527, 400, 714]], antenna: [[220, 0, 280, 105]] },
  bridge: { bankl: [[4, 81, 254, 323]], bankr: [[1159, 81, 1409, 323]], river: [[254, 201, 1162, 322]], pier: [[632, 185, 781, 314]], deck: [[192, 83, 1223, 191]], rail: [[211, 0, 632, 161], [783, 0, 1201, 161]] },
};

function unmix(data, i, m) {
  const c = Math.max(0.35, 1 - Math.max(0, m) / 255);
  const cl = (v) => Math.max(0, Math.min(255, v));
  const g = data[i + 1] / c;
  data[i] = cl(Math.min((data[i] - (1 - c) * 255) / c, g + 40));
  data[i + 1] = cl(g);
  data[i + 2] = cl(Math.min((data[i + 2] - (1 - c) * 255) / c, g + 40));
}

// Magenta to alpha: flood from the border through pixels close to #ff00ff, pockets very close to it count too, soft edge next to it.
async function keyMagenta(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width, h = info.height, n = w * h;
  const m = new Int16Array(n), d = new Int16Array(n);
  for (let p = 0; p < n; p++) { const r = data[p * 4], g = data[p * 4 + 1], b = data[p * 4 + 2]; m[p] = Math.min(r, b) - g; d[p] = Math.hypot(255 - r, g, 255 - b); }
  const bg = new Uint8Array(n), stack = [];
  const push = (p) => { if (!bg[p] && d[p] < 100) { bg[p] = 1; stack.push(p); } };
  for (let p = 0; p < n; p++) if (d[p] < 50) bg[p] = 1;
  for (let x = 0; x < w; x++) { push(x); push((h - 1) * w + x); }
  for (let y = 0; y < h; y++) { push(y * w); push(y * w + w - 1); }
  while (stack.length) { const p = stack.pop(), x = p % w; if (x > 0) push(p - 1); if (x < w - 1) push(p + 1); if (p >= w) push(p - w); if (p < n - w) push(p + w); }
  const near = (p) => { const x = p % w, y = (p - x) / w; for (let dy = -2; dy <= 2; dy++) for (let dx = -2; dx <= 2; dx++) { const xx = x + dx, yy = y + dy; if (xx >= 0 && yy >= 0 && xx < w && yy < h && bg[yy * w + xx]) return true; } return false; };
  for (let p = 0; p < n; p++) {
    if (m[p] <= 45 || (!bg[p] && !near(p))) continue;
    const i = p * 4;
    if (bg[p] && m[p] > 110) data[i + 3] = 0;
    else { data[i + 3] = Math.round(255 * Math.max(0, Math.min(1, (110 - m[p]) / 65))); unmix(data, i, m[p]); }
  }
  return { data, w, h };
}

// Connected pieces of the keyed sheet, nearby ones merged (a part drawn in two blobs).
function clusters(img) {
  const { data, w, h } = img, n = w * h, label = new Int32Array(n);
  const comps = [];
  for (let s = 0; s < n; s++) {
    if (label[s] || data[s * 4 + 3] <= 40) continue;
    const id = comps.length + 1, stack = [s];
    label[s] = id;
    const c = { id, x0: w, y0: h, x1: 0, y1: 0, area: 0 };
    while (stack.length) {
      const p = stack.pop(), x = p % w, y = (p - x) / w;
      c.area++; c.x0 = Math.min(c.x0, x); c.x1 = Math.max(c.x1, x); c.y0 = Math.min(c.y0, y); c.y1 = Math.max(c.y1, y);
      for (const q of [x > 0 ? p - 1 : -1, x < w - 1 ? p + 1 : -1, y > 0 ? p - w : -1, y < h - 1 ? p + w : -1]) if (q >= 0 && !label[q] && data[q * 4 + 3] > 40) { label[q] = id; stack.push(q); }
    }
    comps.push(c);
  }
  const biggest = Math.max(...comps.map(c => c.area));
  let list = comps.filter(c => c.area > biggest * 0.0008).map(c => ({ ids: [c.id], x0: c.x0, y0: c.y0, x1: c.x1, y1: c.y1, area: c.area }));
  const PAD = 3;
  for (let merged = true; merged;) {
    merged = false;
    for (let i = 0; i < list.length && !merged; i++) for (let j = i + 1; j < list.length && !merged; j++) {
      const a = list[i], b = list[j];
      if (a.x0 - PAD <= b.x1 && b.x0 - PAD <= a.x1 && a.y0 - PAD <= b.y1 && b.y0 - PAD <= a.y1) {
        list[i] = { ids: a.ids.concat(b.ids), x0: Math.min(a.x0, b.x0), y0: Math.min(a.y0, b.y0), x1: Math.max(a.x1, b.x1), y1: Math.max(a.y1, b.y1), area: a.area + b.area };
        list.splice(j, 1); merged = true;
      }
    }
  }
  return { label, list };
}

// The part's own pixels as a trimmed RGBA buffer.
async function cut(img, label, cl) {
  const w = img.w, cw = cl.x1 - cl.x0 + 1, ch = cl.y1 - cl.y0 + 1, out = Buffer.alloc(cw * ch * 4), ids = new Set(cl.ids);
  for (let y = 0; y < ch; y++) for (let x = 0; x < cw; x++) {
    const p = (cl.y0 + y) * w + cl.x0 + x;
    if (!ids.has(label[p])) continue;
    const o = (y * cw + x) * 4, i = p * 4;
    out[o] = img.data[i]; out[o + 1] = img.data[i + 1]; out[o + 2] = img.data[i + 2]; out[o + 3] = img.data[i + 3];
  }
  return { data: out, w: cw, h: ch };
}

const raw = (b) => sharp(b.data, { raw: { width: b.w, height: b.h, channels: 4 } });
async function scaled(b, f) {
  const w = Math.max(2, Math.round(b.w * f)), h = Math.max(2, Math.round(b.h * f));
  const { data, info } = await raw(b).resize({ width: w, height: h, kernel: 'lanczos3' }).raw().toBuffer({ resolveWithObject: true });
  return { data, w: info.width, h: info.height };
}

// Sample points of a part (opaque pixels, evenly spread).
function samples(b, count) {
  const pts = [];
  for (let y = 0; y < b.h; y++) for (let x = 0; x < b.w; x++) { const i = (y * b.w + x) * 4; if (b.data[i + 3] > 220) pts.push([x, y, b.data[i], b.data[i + 1], b.data[i + 2]]); }
  const step = Math.max(1, Math.floor(pts.length / count));
  return pts.filter((_, k) => k % step === 0);
}

// Best offset of part pts over scene (clipped colour distance), searched inside [ox0..ox1] x [oy0..oy1]; excluding given centres.
function search(scene, pts, pw, ph, box, avoid) {
  const CAP = 140;
  let best = { score: 1e9, x: 0, y: 0 };
  for (let oy = box.y0; oy <= box.y1; oy++) for (let ox = box.x0; ox <= box.x1; ox++) {
    if (ox < 0 || oy < 0 || ox + pw > scene.w || oy + ph > scene.h) continue;
    if (avoid && avoid.some(a => Math.abs(a.x - ox) < pw * 0.5 && Math.abs(a.y - oy) < ph * 0.5)) continue;
    let sum = 0;
    for (let k = 0; k < pts.length; k++) {
      const q = pts[k], i = ((oy + q[1]) * scene.w + ox + q[0]) * 4;
      if (scene.data[i + 3] < 200) { sum += CAP; continue; }
      const dr = scene.data[i] - q[2], dg = scene.data[i + 1] - q[3], db = scene.data[i + 2] - q[4];
      const dd = Math.sqrt(dr * dr + dg * dg + db * db);
      sum += dd > CAP ? CAP : dd;
    }
    const score = sum / pts.length;
    if (score < best.score) best = { score, x: ox, y: oy };
  }
  return best;
}

async function match(scene, part, count, fix) {
  const F = 4;
  const sceneS = await scaled(scene, 1 / F);
  const results = [];
  // coarse: every scale, the whole scene
  const trials = [];
  for (let s = 0.5; s <= 2.4; s += 0.05) {
    if (fix && fix.scale) { s = fix.scale; }
    const ps = await scaled(part, s / F);
    const pts = samples(ps, 220);
    if (pts.length < 20) continue;
    const found = [];
    for (let k = 0; k < count; k++) found.push(search(sceneS, pts, ps.w, ps.h, { x0: 0, y0: 0, x1: sceneS.w, y1: sceneS.h }, found));
    trials.push({ s, score: found.reduce((a, f) => a + f.score, 0), found });
    if (fix && fix.scale) break;
  }
  trials.sort((a, b) => a.score - b.score);
  const t = trials[0];
  // fine: around the winner at full size
  let bestS = t.s, positions = t.found.map(f => ({ x: f.x * F, y: f.y * F, score: f.score }));
  let refined = [];
  for (const pos of positions) {
    let top = { score: 1e9 };
    for (let ds = -0.06; ds <= 0.0601; ds += 0.015) {
      const s = fix && fix.scale ? fix.scale : t.s + ds;
      const ps = await scaled(part, s);
      const pts = samples(ps, 500);
      const r = search(scene, pts, ps.w, ps.h, { x0: pos.x - 8, y0: pos.y - 8, x1: pos.x + 8, y1: pos.y + 8 });
      if (r.score < top.score) top = { score: r.score, x: r.x, y: r.y, s };
    }
    refined.push(top);
  }
  return refined;
}

const Pad = 11, Reach = 6;

// A dotted outline of the sprite's silhouette: a Reach-wide band just outside it, cut into dashes by diagonal stripes, white with a dark edge.
async function slotSprite(sprite) {
  const w = sprite.w + Pad * 2, h = sprite.h + Pad * 2;
  const inside = new Uint8Array(w * h);
  for (let y = 0; y < sprite.h; y++) for (let x = 0; x < sprite.w; x++) if (sprite.data[(y * sprite.w + x) * 4 + 3] > 60) inside[(y + Pad) * w + x + Pad] = 1;
  const grow = (mask, r) => {
    const out = new Uint8Array(w * h);
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      let hit = 0;
      for (let dy = -r; dy <= r && !hit; dy++) for (let dx = -r; dx <= r; dx++) {
        if (dx * dx + dy * dy > r * r) continue;
        const nx = x + dx, ny = y + dy;
        if (nx >= 0 && ny >= 0 && nx < w && ny < h && mask[ny * w + nx]) { hit = 1; break; }
      }
      out[y * w + x] = hit;
    }
    return out;
  };
  const grown = grow(inside, Reach);
  const dash = new Uint8Array(w * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (grown[y * w + x] && !inside[y * w + x] && (x + y) % 30 < 17) dash[y * w + x] = 1;
  const edge = grow(dash, 1);
  const out = Buffer.alloc(w * h * 4);
  for (let p = 0; p < w * h; p++) {
    const o = p * 4;
    if (dash[p]) { out[o] = out[o + 1] = out[o + 2] = 255; out[o + 3] = 245; }
    else if (edge[p]) { out[o] = 70; out[o + 1] = 40; out[o + 2] = 15; out[o + 3] = 170; }
  }
  return { data: out, w, h };
}

async function importGame(game) {
  const names = GAMES[game];
  const file = path.join(os.homedir(), 'Downloads', 'workshop_build_' + game + '_sheet.png');
  const img = await keyMagenta(file);
  const { label, list } = clusters(img);
  // split: the widest run of empty rows inside the middle of the sheet
  const rowUsed = new Array(img.h).fill(false);
  for (const c of list) for (let y = c.y0; y <= c.y1; y++) rowUsed[y] = true;
  let split = 0, bestGap = 0;
  for (let y = Math.floor(img.h * 0.3), start = -1; y < Math.floor(img.h * 0.7); y++) {
    if (!rowUsed[y]) { if (start < 0) start = y; if (y - start + 1 > bestGap) { bestGap = y - start + 1; split = Math.floor((start + y) / 2); } } else start = -1;
  }
  const top = list.filter(c => (c.y0 + c.y1) / 2 < split);
  const low = list.filter(c => (c.y0 + c.y1) / 2 >= split);
  if (low.length !== names.length) throw new Error(game + ': found ' + low.length + ' lower pieces, expected ' + names.length + ' (split ' + split + ')');
  // reading order
  low.sort((a, b) => (a.y0 + a.y1) - (b.y0 + b.y1));
  const rows = [];
  for (const c of low) {
    const cy = (c.y0 + c.y1) / 2, row = rows[rows.length - 1];
    if (row && cy - row.cy < 110) { row.items.push(c); row.cy = (row.cy * (row.items.length - 1) + cy) / row.items.length; } else rows.push({ cy, items: [c] });
  }
  const ordered = [];
  for (const r of rows) { r.items.sort((a, b) => (a.x0 + a.x1) - (b.x0 + b.x1)); ordered.push(...r.items); }

  // the finished picture
  const box = { x0: Math.min(...top.map(c => c.x0)), y0: Math.min(...top.map(c => c.y0)), x1: Math.max(...top.map(c => c.x1)), y1: Math.max(...top.map(c => c.y1)) };
  const scene = await cut(img, label, { ids: top.flatMap(c => c.ids), ...box });
  fs.mkdirSync(jsonOut, { recursive: true });
  await raw(scene).png().toFile(path.join(jsonOut, 'full_' + game + '.png')); // the finished picture, only for checking by eye

  const distinct = [...new Set(names)];
  const placements = [], report = [];
  for (const name of distinct) {
    const idx = names.map((n, i) => n === name ? i : -1).filter(i => i >= 0);
    const part = await cut(img, label, ordered[idx[0]]);
    const rects = RECTS[game][name];
    if (!rects || rects.length !== idx.length) throw new Error(game + ' ' + name + ': needs ' + idx.length + ' rect(s)');
    const rw = rects[0][2] - rects[0][0], rh = rects[0][3] - rects[0][1];
    const k = Math.min(rw / part.w, rh / part.h);
    let sprite;
    if (STRETCH.has(name) || STRETCH.has(game + ':' + name)) {
      const r = await raw(part).resize({ width: rw, height: rh, fit: 'fill', kernel: 'lanczos3' }).raw().toBuffer({ resolveWithObject: true });
      sprite = { data: r.data, w: r.info.width, h: r.info.height };
    } else sprite = await scaled(part, k);
    await raw(sprite).png().toFile(path.join(resOut, 'build_' + game + '_' + name + '.png'));
    await raw(await slotSprite(sprite)).png().toFile(path.join(resOut, 'build_' + game + '_' + name + '_slot.png'));
    for (const r of rects) placements.push({ part: name, x: Math.round((r[0] + r[2]) / 2 - sprite.w / 2), y: Math.round((r[1] + r[3]) / 2 - sprite.h / 2), w: sprite.w, h: sprite.h });
    report.push(name + ' scale ' + k.toFixed(2) + ' ' + sprite.w + 'x' + sprite.h + ' from ' + part.w + 'x' + part.h);
  }
  fs.mkdirSync(jsonOut, { recursive: true });
  fs.writeFileSync(path.join(resOut, 'build_' + game + '.json'), JSON.stringify({ game, width: scene.w, height: scene.h, pad: Pad, parts: placements }, null, 1) + String.fromCharCode(10));
  console.log(game, scene.w + 'x' + scene.h, '\n  ' + report.join('\n  '));

  // check picture: parts composited at their found places on a grey ground
  const layers = [];
  for (const p of placements) layers.push({ input: path.join(resOut, 'build_' + game + '_' + p.part + '.png'), left: p.x, top: p.y });
  await sharp({ create: { width: scene.w, height: scene.h, channels: 4, background: { r: 200, g: 200, b: 200, alpha: 1 } } }).composite(layers).png().toFile(path.join(jsonOut, 'check_' + game + '.png'));
}

(async () => {
  for (const g of process.argv.slice(2)) await importGame(g);
})().catch(e => { console.error(e); process.exit(1); });
