// Builds the doll-house art from the AI-generated sources in art/eva/house/ai:
//  - the shell overlay (transparent cell interiors): an equal-height wooden frame with a narrow hallway column under
//    a full-width tiled roof drawn here (chimney from the AI shell). The attic is ONE big room: its picture
//    (attic.png, a wide flat gable on a black/magenta background) is masked to its silhouette and cropped, and the
//    roof's slope is matched to the picture's so the room sits exactly inside the roof;
//  - the room and hallway pictures (1440x960), the attic (native crop), the outside backdrop and the shell into
//    Resources/Art/house;
//  - art/eva/house/out/preview.png: the pictures composited inside the shell, cells.json.
// Usage: node tools/art-import/build-house.js
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const AI = path.join(__dirname, '../../art/eva/house/ai');
const OUT = path.join(__dirname, '../../art/eva/house/out');
const RES = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/house');
fs.mkdirSync(OUT, { recursive: true });
fs.mkdirSync(RES, { recursive: true });

const K = 2.4; // world units per source pixel: a 600 px room cell is 1440 world units
const CELL_H = 400, WALL = 30, SLAB = 30, BASE = 50, X0 = 140, OVERHANG = 70, BAND = 70;
const COL_W = [600, 300, 600, 600]; // column 1 is the hallway column
const cellX = (c) => X0 + WALL + COL_W.slice(0, c).reduce((a, w) => a + w + WALL, 0);
const HOUSE_W = COL_W.reduce((a, w) => a + w, 0) + WALL * 5;      // outer walls + 3 dividers
const X1 = X0 + HOUSE_W, XC = X0 + HOUSE_W / 2;
const ATTIC_W = HOUSE_W - 2 * WALL;

const CREAM = '#fef0d0', CREAM_SIDE = '#e1b987', WOOD = '#df9139', WOOD_TOP = '#eda85a', LINE = '#8a5a34';
const rect = (x, y, w, h, fill, extra = '') => `<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${fill}" stroke="${LINE}" stroke-width="3" ${extra}/>`;

async function keyed(file, box) {
  const { data, info } = await sharp(path.join(AI, file)).extract(box).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    const m = Math.min(r, b) - g;
    if (m > 110) data[i + 3] = 0;
    else if (m > 45) { data[i + 3] = Math.round(255 * (110 - m) / 65); data[i] = Math.min(r, g + 40); data[i + 2] = Math.min(b, g + 40); }
  }
  return sharp(data, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toBuffer();
}

// The attic picture: everything above the roof edge (the AI painted it black with a glow) becomes transparent, the
// empty top rows are cropped. Returns the picture, its size and the slope of the roof edge (rise per run).
async function attic() {
  const { data, info } = await sharp(path.join(AI, 'attic.png')).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width, h = info.height;
  const edge = new Array(w).fill(h);
  for (let x = 0; x < w; x++) {
    for (let y = 0; y < h; y++) {
      const i = (y * w + x) * 4;
      if ((data[i] + data[i + 1] + data[i + 2]) / 3 > 170 && data[i] > 200) { edge[x] = y; break; }
    }
  }
  const top = Math.min(...edge);
  for (let x = 0; x < w; x++) for (let y = 0; y < h; y++) if (y < edge[x] + 2) data[(y * w + x) * 4 + 3] = 0;
  const apexX = edge.indexOf(top);
  const slope = ((edge[0] - top) / apexX + (edge[w - 1] - top) / (w - 1 - apexX)) / 2;
  const kneeFrac = ((edge[0] + edge[w - 1]) / 2 - top) / (h - top);
  const picture = await sharp(data, { raw: { width: w, height: h, channels: 4 } }).extract({ left: 0, top, width: w, height: h - top }).png().toBuffer();
  return { picture, w, h: h - top, slope, kneeFrac };
}

function geometry(a) {
  const picH = Math.round(ATTIC_W * a.h / a.w);
  const roofH = BAND + picH;                 // attic floor beam (eave) y; apex of the roof is y = 0
  const rowY = [null, roofH + SLAB, roofH + SLAB + CELL_H + SLAB]; // upper, ground cell tops
  return { picH, roofH, rowY, W: X0 * 2 + HOUSE_W, H: rowY[2] + CELL_H + BASE + 20, s: a.slope };
}

function roofBackSvg(g) {
  const yb = (x) => g.s * Math.abs(x - XC) + BAND;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${g.W}" height="${g.H}"><polygon points="${X0 + WALL},${g.roofH} ${X0 + WALL},${yb(X0 + WALL)} ${XC},${BAND} ${X1 - WALL},${yb(X1 - WALL)} ${X1 - WALL},${g.roofH}" fill="#f1d9a6"/></svg>`;
}

// In front of the rooms: the tiled roof band (constant vertical thickness), the knee-wall posts under it.
function roofRingSvg(g) {
  const yt = (x) => g.s * Math.abs(x - XC);
  const xl = X0 - OVERHANG, xr = X1 + OVERHANG;
  const band = `${xl},${yt(xl)} ${XC},0 ${xr},${yt(xr)} ${xr},${yt(xr) + BAND} ${XC},${BAND} ${xl},${yt(xl) + BAND}`;
  let s = `<svg xmlns="http://www.w3.org/2000/svg" width="${g.W}" height="${g.H}"><defs>
    <pattern id="tiles" width="64" height="32" patternUnits="userSpaceOnUse">
      <rect width="64" height="32" fill="#e8664a"/>
      <g fill="#ef7a5c" stroke="#9c3a26" stroke-width="3"><circle cx="0" cy="0" r="32"/><circle cx="64" cy="0" r="32"/><circle cx="32" cy="16" r="32"/>
      <circle cx="0" cy="32" r="32"/><circle cx="64" cy="32" r="32"/></g>
    </pattern></defs>`;
  s += `<polygon points="${band}" fill="url(#tiles)" stroke="#7d2f1f" stroke-width="6" stroke-linejoin="round"/>`;
  // knee walls of the attic: posts from under the band down to the attic floor beam
  for (const x of [X0, X1 - WALL]) {
    const top = yt(x) + BAND - 4;
    s += rect(x, top, WALL, g.roofH - top, CREAM) + `<rect x="${x + WALL - 9}" y="${top}" width="9" height="${g.roofH - top}" fill="${CREAM_SIDE}"/>`;
  }
  return s + '</svg>';
}

function frameSvg(g) {
  let s = `<svg xmlns="http://www.w3.org/2000/svg" width="${g.W}" height="${g.H}">`;
  const post = (x, y) => rect(x, y, WALL, CELL_H, CREAM) + `<rect x="${x + WALL - 9}" y="${y}" width="9" height="${CELL_H}" fill="${CREAM_SIDE}"/>`;
  const slab = (y, h = SLAB, x = X0 - 25, w = HOUSE_W + 50) => rect(x, y, w, h, WOOD) + `<rect x="${x}" y="${y}" width="${w}" height="8" fill="${WOOD_TOP}"/>`;
  for (const r of [1, 2]) {
    const y = g.rowY[r];
    s += post(X0, y);
    for (let c = 1; c <= 3; c++) s += post(cellX(c) - WALL, y);
    s += post(cellX(3) + COL_W[3], y);
  }
  s += slab(g.roofH);                    // attic floor / eave beam
  s += slab(g.rowY[2] - SLAB);
  s += slab(g.rowY[2] + CELL_H, BASE);
  return s + '</svg>';
}

(async () => {
  const a = await attic();
  const g = geometry(a);
  const ring = await sharp(Buffer.from(roofRingSvg(g))).png().toBuffer();
  const back = await sharp(Buffer.from(roofBackSvg(g))).png().toBuffer();
  const chimney = await keyed('shell.png', { left: 283, top: 40, width: 95, height: 92 });
  const chimneyW = 120, chimneyH = 116;
  const cx = Math.round(XC - 0.55 * (HOUSE_W / 2 + OVERHANG)), roofYAt = (x) => g.s * Math.abs(x - XC);
  const layers = [
    { input: await sharp(chimney).resize(chimneyW, chimneyH).png().toBuffer(), left: cx - chimneyW / 2, top: Math.round(roofYAt(cx) - chimneyH + 50) },
    { input: ring, left: 0, top: 0 },
    { input: Buffer.from(frameSvg(g)), left: 0, top: 0 },
  ];
  const shell = await sharp({ create: { width: g.W, height: g.H, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } }).composite(layers).png().toBuffer();

  const LAYOUT = [
    ['room_parents', 'hall_upper', 'room_kids', 'room_bath'],
    ['room_living', 'hall_ground', 'room_dining', 'room_kitchen'],
  ];
  const previewLayers = [{ input: back, left: 0, top: 0 }];
  const cells = {};
  for (let r = 0; r < 2; r++) for (let c = 0; c < 4; c++) {
    const name = LAYOUT[r][c];
    const rc = { x: cellX(c), y: g.rowY[r + 1], w: COL_W[c], h: CELL_H };
    cells[name] = rc;
    previewLayers.push({ input: await sharp(path.join(AI, name + '.png')).resize(rc.w, rc.h, { fit: 'fill' }).png().toBuffer(), left: rc.x, top: rc.y });
    await sharp(path.join(AI, name + '.png')).resize(1440, 960).png({ compressionLevel: 9 }).toFile(path.join(RES, name + '.png'));
  }
  const atticRect = { x: X0 + WALL, y: BAND, w: ATTIC_W, h: g.picH };
  cells.room_party = atticRect;
  previewLayers.push({ input: await sharp(a.picture).resize(atticRect.w, atticRect.h, { fit: 'fill' }).png().toBuffer(), left: atticRect.x, top: atticRect.y });
  await sharp(a.picture).png({ compressionLevel: 9 }).toFile(path.join(RES, 'room_party.png'));
  fs.writeFileSync(path.join(OUT, 'cells.json'), JSON.stringify({ width: g.W, height: g.H, cells }, null, 2));
  previewLayers.push({ input: shell, left: 0, top: 0 });
  await sharp({ create: { width: g.W, height: g.H, channels: 4, background: '#a6d8f7' } }).composite(previewLayers).png().toFile(path.join(OUT, 'preview.png'));

  await sharp(path.join(AI, 'outside.png')).resize(1920, 1280).png({ compressionLevel: 9 }).toFile(path.join(RES, 'outside.png'));
  await sharp(shell).resize(2048).png({ compressionLevel: 9 }).toFile(path.join(RES, 'shell.png'));
  await sharp(back).resize(2048).png({ compressionLevel: 9 }).toFile(path.join(RES, 'shell_back.png'));

  const pivotPx = g.rowY[1] + CELL_H / 2, pivotY = 1 - pivotPx / g.H;
  const sw = g.W * K, sh = g.H * K;
  const top = (1 - pivotY) * sh, bottom = -pivotY * sh;
  const scale = Math.min(880 / sh, 1400 / sw);
  console.log(`ShellWidth = ${Math.round(sw)}f, ShellHeight = ${Math.round(sh)}f, ShellPivotX = 0.5f, ShellPivotY = ${pivotY.toFixed(4)}f`);
  console.log(`OverviewScale ~ ${scale.toFixed(3)}, OverviewFocus = (0, ${Math.round((top + bottom) / 2)})`);
  console.log(`Attic overview size ${Math.round(atticRect.w * K)} x ${Math.round(atticRect.h * K)}, centre y ${Math.round(-((atticRect.y + atticRect.h / 2) - pivotPx) * K)}; picture ${a.w}x${a.h}, roof slope ${a.slope.toFixed(3)}, knee ${a.kneeFrac.toFixed(2)}`);
})();
