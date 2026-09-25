// Builds the doll-house art from the AI-generated sources in art/eva/house/ai:
//  - the shell overlay (transparent cell interiors): an equal-height wooden frame with a narrow hallway column,
//    the attic (hallway + party room) inset under a full-width pitched roof drawn here (chimney from the AI shell);
//  - the room and hallway pictures (1440x960), the outside backdrop and the shell into Resources/Art/house;
//  - art/eva/house/out/preview.png: the pictures composited inside the shell, cells.json.
// The party room's door is on its left (next to the attic hallway): the AI picture has it on the right, so it is
// mirrored here. Usage: node tools/art-import/build-house.js
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const AI = path.join(__dirname, '../../art/eva/house/ai');
const OUT = path.join(__dirname, '../../art/eva/house/out');
const RES = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/house');
fs.mkdirSync(OUT, { recursive: true });
fs.mkdirSync(RES, { recursive: true });

const K = 2.4; // world units per source pixel: a 600 px room cell is 1440 world units
const CELL_H = 400, WALL = 30, SLAB = 30, BASE = 50, X0 = 140, OVERHANG = 70;
const COL_W = [600, 300, 600, 600]; // column 1 is the hallway column
const cellX = (c) => X0 + WALL + COL_W.slice(0, c).reduce((a, w) => a + w + WALL, 0);
const HOUSE_W = COL_W.reduce((a, w) => a + w, 0) + WALL * 5;      // outer walls + 3 dividers
const XC = X0 + HOUSE_W / 2;
const ROOF_H = 760;                                               // apex y = 0, eave (attic floor) y = ROOF_H
const ROW_Y = [ROOF_H - CELL_H, ROOF_H + SLAB, ROOF_H + SLAB + CELL_H + SLAB]; // attic, upper, ground cell tops
const W = X0 * 2 + HOUSE_W, H = ROW_Y[2] + CELL_H + BASE + 20;

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

function roofGeometry() {
  const half = HOUSE_W / 2 + OVERHANG, t = 64;
  return {
    half,
    outer: `${XC - half},${ROOF_H} ${XC},0 ${XC + half},${ROOF_H}`,
    inner: `${XC - half + t * 2.2},${ROOF_H} ${XC},${t * 1.3} ${XC + half - t * 2.2},${ROOF_H}`,
    innerTop: t * 1.3,
  };
}

// Behind the room pictures: the inside of the roof (visible around the attic).
function roofBackSvg() {
  const g = roofGeometry();
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}"><polygon points="${g.inner}" fill="#f1d9a6" stroke="${LINE}" stroke-width="4"/>
    <line x1="${XC}" y1="${g.innerTop}" x2="${XC}" y2="${ROW_Y[0]}" stroke="${LINE}" stroke-width="3"/></svg>`;
}

// In front of the rooms: the tiled roof band around the inside.
function roofRingSvg() {
  const g = roofGeometry();
  const pts = (p) => 'M' + p.split(' ').join(' L') + ' Z';
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}"><defs>
    <pattern id="tiles" width="64" height="32" patternUnits="userSpaceOnUse">
      <rect width="64" height="32" fill="#e8664a"/>
      <g fill="#ef7a5c" stroke="#9c3a26" stroke-width="3"><circle cx="0" cy="0" r="32"/><circle cx="64" cy="0" r="32"/><circle cx="32" cy="16" r="32"/>
      <circle cx="0" cy="32" r="32"/><circle cx="64" cy="32" r="32"/></g>
    </pattern></defs>
    <path d="${pts(g.outer)} ${pts(g.inner)}" fill="url(#tiles)" fill-rule="evenodd" stroke="#7d2f1f" stroke-width="6" stroke-linejoin="round"/></svg>`;
}

function frameSvg() {
  let s = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}">`;
  const post = (x, y) => rect(x, y, WALL, CELL_H, CREAM) + `<rect x="${x + WALL - 9}" y="${y}" width="9" height="${CELL_H}" fill="${CREAM_SIDE}"/>`;
  const slab = (y, h = SLAB, x = X0 - 25, w = HOUSE_W + 50) => rect(x, y, w, h, WOOD) + `<rect x="${x}" y="${y}" width="${w}" height="8" fill="${WOOD_TOP}"/>`;
  // upper and ground floors: outer walls and dividers
  for (const r of [1, 2]) {
    const y = ROW_Y[r];
    s += post(X0, y);
    for (let c = 1; c <= 3; c++) s += post(cellX(c) - WALL, y);
    s += post(cellX(3) + COL_W[3], y);
  }
  // attic: only the hallway and the party room (columns 1 and 2), inset under the roof
  const y0 = ROW_Y[0];
  s += post(cellX(1) - WALL, y0) + post(cellX(2) - WALL, y0) + post(cellX(2) + COL_W[2], y0);
  s += slab(y0 - SLAB, SLAB, cellX(1) - WALL - 10, COL_W[1] + COL_W[2] + WALL * 3 + 20);
  s += slab(ROOF_H);                    // attic floor / eave beam
  s += slab(ROW_Y[2] - SLAB);
  s += slab(ROW_Y[2] + CELL_H, BASE);
  return s + '</svg>';
}

(async () => {
  const back = await sharp(Buffer.from(roofBackSvg())).png().toBuffer();
  const ring = await sharp(Buffer.from(roofRingSvg())).png().toBuffer();
  const chimney = await keyed('shell.png', { left: 283, top: 40, width: 95, height: 92 });
  const chimneyW = 120, chimneyH = 116;
  const half = HOUSE_W / 2 + OVERHANG;
  const cx = Math.round(XC - half * 0.5), roofYAt = (x) => ROOF_H * (1 - Math.abs(XC - x) / half);
  const layers = [
    { input: await sharp(chimney).resize(chimneyW, chimneyH).png().toBuffer(), left: cx - chimneyW / 2, top: Math.round(roofYAt(cx) - chimneyH + 40) },
    { input: ring, left: 0, top: 0 },
    { input: Buffer.from(frameSvg()), left: 0, top: 0 },
  ];
  const shellBack = await sharp(back).png().toBuffer();
  const shell = await sharp({ create: { width: W, height: H, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } }).composite(layers).png().toBuffer();
  fs.writeFileSync(path.join(OUT, 'shell.png'), shell);

  const LAYOUT = [
    [null, 'hall_attic', 'room_party', null],
    ['room_parents', 'hall_upper', 'room_kids', 'room_bath'],
    ['room_living', 'hall_ground', 'room_dining', 'room_kitchen'],
  ];
  const source = async (name) => {
    const img = sharp(path.join(AI, name + '.png'));
    return name === 'room_party' ? sharp(await img.flop().toBuffer()) : img; // door on the hallway side (left)
  };
  const cells = {};
  const previewLayers = [];
  for (let r = 0; r < 3; r++) for (let c = 0; c < 4; c++) {
    const name = LAYOUT[r][c];
    if (!name) continue;
    const rc = { x: cellX(c), y: ROW_Y[r], w: COL_W[c], h: CELL_H };
    cells[name] = rc;
    previewLayers.push({ input: await (await source(name)).resize(rc.w, rc.h, { fit: 'fill' }).png().toBuffer(), left: rc.x, top: rc.y });
    await (await source(name)).resize(1440, 960).png({ compressionLevel: 9 }).toFile(path.join(RES, name + '.png'));
  }
  fs.writeFileSync(path.join(OUT, 'cells.json'), JSON.stringify({ width: W, height: H, cells }, null, 2));
  previewLayers.unshift({ input: shellBack, left: 0, top: 0 });
  previewLayers.push({ input: shell, left: 0, top: 0 });
  await sharp({ create: { width: W, height: H, channels: 4, background: '#a6d8f7' } }).composite(previewLayers).png().toFile(path.join(OUT, 'preview.png'));

  await sharp(path.join(AI, 'outside.png')).resize(1920, 1280).png({ compressionLevel: 9 }).toFile(path.join(RES, 'outside.png'));
  await sharp(shell).resize(2048).png({ compressionLevel: 9 }).toFile(path.join(RES, 'shell.png'));
  await sharp(shellBack).resize(2048).png({ compressionLevel: 9 }).toFile(path.join(RES, 'shell_back.png'));

  const pivotY = 1 - (ROW_Y[1] + CELL_H / 2) / H;
  const sw = W * K, sh = H * K;
  const top = (1 - pivotY) * sh, bottom = -pivotY * sh;
  const scale = Math.min(880 / sh, 1400 / sw);
  console.log(`ShellWidth = ${Math.round(sw)}f, ShellHeight = ${Math.round(sh)}f, ShellPivotX = 0.5f, ShellPivotY = ${pivotY.toFixed(4)}f`);
  console.log(`OverviewScale ~ ${scale.toFixed(3)}, OverviewFocus = (0, ${Math.round((top + bottom) / 2)})`);
})();
