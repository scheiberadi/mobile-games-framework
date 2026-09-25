const path = require('path');
const fs = require('fs');
const sharp = require('sharp');

const downloads = 'C:/Users/schei/Downloads';
const root = path.resolve(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art');
const size = 1024;
const radius = 120;

async function backdrop() {
  fs.copyFileSync(path.join(downloads, 'School_background.png'), path.join(root, 'world/school_list_bg.png'));
}

async function countTile() {
  const src = path.join(downloads, 'counting_tile.png');
  const meta = await sharp(src).metadata();
  const side = Math.min(meta.width, meta.height);
  const square = await sharp(src)
    .extract({ left: Math.floor((meta.width - side) / 2), top: Math.floor((meta.height - side) / 2), width: side, height: side })
    .resize(size, size)
    .ensureAlpha()
    .toBuffer();
  const mask = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}"><rect width="${size}" height="${size}" rx="${radius}" fill="#fff"/></svg>`);
  const outline = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}"><rect x="5" y="5" width="${size - 10}" height="${size - 10}" rx="${radius - 5}" fill="none" stroke="#7a4a1e" stroke-width="10"/></svg>`);
  await sharp(square)
    .composite([{ input: mask, blend: 'dest-in' }, { input: outline, blend: 'over' }])
    .png()
    .toFile(path.join(root, 'activities/count.png'));
}

(async () => {
  fs.mkdirSync(path.join(root, 'activities'), { recursive: true });
  await backdrop();
  await countTile();
  console.log('school_list_bg.png and activities/count.png written');
})();
