// Builds the doll-house shell overlay: an equal-cell 4x3 wooden frame drawn here, plus the roof pieces cut from the
// AI-generated art/eva/house/ai/shell.png (magenta keyed out). Writes art/eva/house/out/shell.png (transparent
// cell interiors), cells.json (cell rectangles in shell pixels) and preview.png (rooms composited behind the shell).
// Usage: node tools/art-import/build-house.js
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const AI = path.join(__dirname, '../../art/eva/house/ai');
const OUT = path.join(__dirname, '../../art/eva/house/out');
const RES = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/house');
fs.mkdirSync(OUT, { recursive: true });
fs.mkdirSync(RES, { recursive: true });

const CELL_W = 600, CELL_H = 400, WALL = 30, SLAB = 30, BASE = 50, X0 = 120;
const PITCH = CELL_W + WALL;
const cellX = (c) => X0 + WALL + c * PITCH;
const S = (cellX(2) + CELL_W + WALL - X0) / 950; // AI gable spans AI x 95..1045 over the three attic columns
const GABLE_TOP_AI = 20, EAVE_AI = 283;
const ROOF_SQUASH = 0.65; // flatter roof than the AI drew, so the whole house fits the screen height
const GABLE_H = Math.round((EAVE_AI - GABLE_TOP_AI) * S * ROOF_SQUASH);

const ROOF_Y = GABLE_H;                    // top of the attic-row slab
const rowY = (r) => ROOF_Y + SLAB + r * (CELL_H + SLAB); // r: 0 attic, 1 upper, 2 ground
const W = cellX(3) + CELL_W + WALL + X0;
const H = rowY(2) + CELL_H + BASE + 20;

const CREAM = '#fef0d0', CREAM_SIDE = '#e1b987', WOOD = '#df9139', WOOD_TOP = '#eda85a', LINE = '#8a5a34';

async function keyed(file, box) {
  const { data, info } = await sharp(path.join(AI, file)).extract(box).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    const m = Math.min(r, b) - g; // magenta-ness
    if (m > 110) { data[i + 3] = 0; }
    else if (m > 45) { data[i + 3] = Math.round(255 * (110 - m) / 65); data[i] = Math.min(r, g + 40); data[i + 2] = Math.min(b, g + 40); }
  }
  return sharp(data, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toBuffer();
}

const rect = (x, y, w, h, fill, extra = '') => `<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${fill}" stroke="${LINE}" stroke-width="3" ${extra}/>`;

function frameSvg() {
  let s = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}">`;
  // attic bay 4 is sealed (no room): a plain cream wall under the lean-to roof
  s += rect(cellX(3), rowY(0), CELL_W, CELL_H, '#f5e7c6');
  // posts: outer walls and dividers, every row
  for (let r = 0; r < 3; r++) {
    const y = rowY(r);
    for (let c = 0; c <= 4; c++) {
      const x = c === 0 ? X0 : c === 4 ? cellX(3) + CELL_W : cellX(c) - WALL;
      s += rect(x, y, WALL, CELL_H, CREAM);
      s += `<rect x="${x + WALL - 9}" y="${y}" width="9" height="${CELL_H}" fill="${CREAM_SIDE}"/>`;
    }
  }
  // slabs: under the roof, between storeys, base
  const slab = (y, h = SLAB) => rect(X0 - 20, y, W - 2 * X0 + 40, h, WOOD) + `<rect x="${X0 - 20}" y="${y}" width="${W - 2 * X0 + 40}" height="8" fill="${WOOD_TOP}"/>`;
  s += slab(ROOF_Y, SLAB);
  s += slab(rowY(1) - SLAB);
  s += slab(rowY(2) - SLAB);
  s += slab(rowY(2) + CELL_H, BASE);
  return s + '</svg>';
}

(async () => {
  const gable = await keyed('shell.png', { left: 50, top: GABLE_TOP_AI, width: 1000, height: EAVE_AI - GABLE_TOP_AI });
  const lean = await keyed('shell.png', { left: 1045, top: 285, width: 435, height: 155 });
  const gableW = Math.round(1000 * S);
  const leanW = Math.round(cellX(3) + CELL_W + WALL + 50 - (cellX(2) + CELL_W + WALL));
  const leanH = Math.round(155 * S * ROOF_SQUASH);

  const layers = [
    { input: Buffer.from(frameSvg()), left: 0, top: 0 },
    { input: await sharp(gable).resize(gableW, GABLE_H).png().toBuffer(), left: Math.round(X0 - 45 * S), top: 0 },
    { input: await sharp(lean).resize(leanW, leanH).png().toBuffer(), left: cellX(2) + CELL_W + WALL, top: ROOF_Y - 10 },
  ];
  const shell = await sharp({ create: { width: W, height: H, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } }).composite(layers).png().toBuffer();
  fs.writeFileSync(path.join(OUT, 'shell.png'), shell);

  const LAYOUT = [
    ['room_party', 'hall_attic', 'room_play', null],
    ['room_parents', 'hall_upper', 'room_kids', 'room_bath'],
    ['room_living', 'hall_ground', 'room_dining', 'room_kitchen'],
  ];
  const cells = {};
  const previewLayers = [];
  for (let r = 0; r < 3; r++) for (let c = 0; c < 4; c++) {
    const name = LAYOUT[r][c];
    if (!name) continue;
    const rect_ = { x: cellX(c), y: rowY(r), w: CELL_W, h: CELL_H };
    cells[name.replace(/^room_|^hall_/, (m) => m === 'hall_' ? 'hall_' : '')] = rect_;
    previewLayers.push({ input: await sharp(path.join(AI, name + '.png')).resize(CELL_W, CELL_H).png().toBuffer(), left: rect_.x, top: rect_.y });
  }
  fs.writeFileSync(path.join(OUT, 'cells.json'), JSON.stringify({ width: W, height: H, cells }, null, 2));
  previewLayers.push({ input: shell, left: 0, top: 0 });
  await sharp({ create: { width: W, height: H, channels: 4, background: '#a6d8f7' } }).composite(previewLayers).png().toFile(path.join(OUT, 'preview.png'));
  // Game assets: every room and hallway at 1440x960 (the cell size in world units), the shell at 2048 wide.
  for (const row of LAYOUT) for (const name of row) {
    if (!name) continue;
    await sharp(path.join(AI, name + '.png')).resize(1440, 960).png({ compressionLevel: 9 }).toFile(path.join(RES, name + '.png'));
  }
  await sharp(path.join(AI, 'outside.png')).resize(1920, 1280).png({ compressionLevel: 9 }).toFile(path.join(RES, 'outside.png'));
  await sharp(shell).resize(2048).png({ compressionLevel: 9 }).toFile(path.join(RES, 'shell.png'));
  const K = 1440 / CELL_W; // world units per source pixel
  const gridCentreX = (cellX(0) + CELL_W / 2 + cellX(3) + CELL_W / 2) / 2, gridCentreY = rowY(1) + CELL_H / 2;
  console.log('shell', W, 'x', H, '| world size', Math.round(W * K), 'x', Math.round(H * K),
    '| pivot', (gridCentreX / W).toFixed(4), (1 - gridCentreY / H).toFixed(4),
    '| column pitch', PITCH * K, 'row pitch', (CELL_H + SLAB) * K);
})();
