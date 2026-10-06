// A game's own full-screen background: ~/Downloads/<source file> -> Resources/Art/world/<name>.png (1920x900: cover-resized to 1920
// wide, then cropped vertically, `top` px cut from the top). Usage: node import-scene-bg.js <source.png> <name> [top]
//   node tools/art-import/import-scene-bg.js domestic_wild_bg2.png domestic_wild_bg
//   node tools/art-import/import-scene-bg.js land_sea_air_bg.png land_sea_air_bg
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

const [source, name, topArg] = process.argv.slice(2);
if (!source || !name) { console.error('usage: node import-scene-bg.js <source.png> <name> [top]'); process.exit(1); }
const src = path.join(os.homedir(), 'Downloads', source);
const dest = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'world', name + '.png');

(async () => {
  const meta = await sharp(src).metadata();
  const height = Math.round(meta.height * 1920 / meta.width);
  const resized = await sharp(src).resize(1920, height).toBuffer();
  const top = Math.max(0, Math.min(parseInt(topArg || '0', 10), height - 900));
  await sharp(resized).extract({ left: 0, top, width: 1920, height: 900 }).png().toFile(dest);
  console.log('written', dest, 'resized height', height, 'top crop', top);
})();
