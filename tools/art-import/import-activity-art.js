const path = require('path');
const fs = require('fs');
const sharp = require('sharp');

const downloads = 'C:/Users/schei/Downloads';
const root = path.resolve(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art');
const size = 1024;
const radius = 120;

async function backdrop() {
  // Same 1920x900 frame as map_bg / school_bg: cover-resize to 1920 wide, then crop vertically.
  const src = path.join(downloads, 'School_background.png');
  const meta = await sharp(src).metadata();
  const height = Math.round(meta.height * 1920 / meta.width);
  const top = Math.round((height - 900) * 0.3);
  const resized = await sharp(src).resize(1920, height).toBuffer();
  await sharp(resized).extract({ left: 0, top, width: 1920, height: 900 }).png().toFile(path.join(root, 'world/school_list_bg.png'));
}

async function countTile() {
  // A button plate around the picture (cream frame, brown outline, soft shadow) plus a green play badge, so the tile
  // reads as something to press and does not blend into the classroom backdrop.
  const src = path.join(downloads, 'counting_tile.png');
  const meta = await sharp(src).metadata();
  const side = Math.min(meta.width, meta.height);
  const inset = 64;
  const inner = size - 2 * inset - 20;
  const innerRadius = 80;
  const picture = await sharp(src)
    .extract({ left: Math.floor((meta.width - side) / 2), top: Math.floor((meta.height - side) / 2), width: side, height: side })
    .resize(inner, inner)
    .ensureAlpha()
    .composite([{ input: Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${inner}" height="${inner}"><rect width="${inner}" height="${inner}" rx="${innerRadius}" fill="#fff"/></svg>`), blend: 'dest-in' }])
    .png()
    .toBuffer();
  const plate = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}">
    <defs><filter id="s" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="14"/></filter></defs>
    <rect x="30" y="44" width="${size - 70}" height="${size - 70}" rx="150" fill="#000" opacity="0.35" filter="url(#s)"/>
    <rect x="20" y="20" width="${size - 60}" height="${size - 60}" rx="150" fill="#fff4d6" stroke="#7a4a1e" stroke-width="12"/>
    <rect x="44" y="44" width="${size - 108}" height="${size - 108}" rx="128" fill="none" stroke="#ffd98a" stroke-width="10"/>
  </svg>`);
  const badge = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}">
    <circle cx="${size - 175}" cy="${size - 175}" r="118" fill="#3fae4a" stroke="#fff" stroke-width="14"/>
    <circle cx="${size - 175}" cy="${size - 175}" r="118" fill="none" stroke="#7a4a1e" stroke-width="5"/>
    <polygon points="${size - 210},${size - 235} ${size - 210},${size - 115} ${size - 120},${size - 175}" fill="#fff"/>
  </svg>`);
  await sharp(plate)
    .composite([{ input: picture, left: inset - 2 + 0, top: inset - 2 }, { input: badge, left: 0, top: 0 }])
    .png()
    .toFile(path.join(root, 'activities/count.png'));
}

(async () => {
  fs.mkdirSync(path.join(root, 'activities'), { recursive: true });
  await backdrop();
  await countTile();
  console.log('school_list_bg.png and activities/count.png written');
})();
