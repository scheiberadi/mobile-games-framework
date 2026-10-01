// Background-only mockup with a world-unit coordinate grid, to scout open grass regions before
// proposing new TapBox positions.
const path = require("path");
const sharp = require("sharp");

const ROOT = path.resolve(__dirname, "../../EvasLearningWorld/Assets/Eva/Resources/Art");
const WORLD_W = 3600, WORLD_H = 1350;
const toCanvasX = (worldX) => worldX + WORLD_W / 2;
const toCanvasY = (worldY) => WORLD_H / 2 - worldY;

async function main() {
  const leftTile = await sharp(path.join(ROOT, "world/map_world_left.png")).resize(WORLD_W / 2, WORLD_H).toBuffer();
  const rightTile = await sharp(path.join(ROOT, "world/map_world_right.png")).resize(WORLD_W / 2, WORLD_H).toBuffer();
  const composite = sharp({ create: { width: WORLD_W, height: WORLD_H, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 1 } } });

  let svg = `<svg width="${WORLD_W}" height="${WORLD_H}" xmlns="http://www.w3.org/2000/svg">`;
  for (let wx = -1800; wx <= 1800; wx += 200) {
    const cx = toCanvasX(wx);
    svg += `<line x1="${cx}" y1="0" x2="${cx}" y2="${WORLD_H}" stroke="magenta" stroke-width="${wx === 0 ? 3 : 1}" opacity="0.6"/>`;
    svg += `<text x="${cx + 4}" y="20" font-size="20" fill="magenta" font-family="sans-serif">${wx}</text>`;
  }
  for (let wy = -600; wy <= 600; wy += 200) {
    const cy = toCanvasY(wy);
    svg += `<line x1="0" y1="${cy}" x2="${WORLD_W}" y2="${cy}" stroke="magenta" stroke-width="${wy === 0 ? 3 : 1}" opacity="0.6"/>`;
    svg += `<text x="4" y="${cy - 4}" font-size="20" fill="magenta" font-family="sans-serif">${wy}</text>`;
  }
  svg += `</svg>`;

  const out = path.join(__dirname, "../../mockup-grid.png");
  await composite.composite([
    { input: leftTile, left: 0, top: 0 },
    { input: rightTile, left: WORLD_W / 2, top: 0 },
    { input: Buffer.from(svg) },
  ]).png().toFile(out);
  console.log("wrote", out);
}

main().catch((e) => { console.error(e); process.exit(1); });
