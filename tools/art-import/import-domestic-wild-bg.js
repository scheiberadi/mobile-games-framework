// Domestic vs Wild's own background: ~/Downloads/domestic_wild_bg2.png -> Resources/Art/world/domestic_wild_bg.png (1920x900,
// cover-resized to 1920 wide, then cropped vertically; TOP_CROP px are cut from the top so the road at the bottom stays whole).
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

const TOP_CROP = 0;
const src = path.join(os.homedir(), 'Downloads', 'domestic_wild_bg2.png');
const dest = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'world', 'domestic_wild_bg.png');

(async () => {
  const meta = await sharp(src).metadata();
  const height = Math.round(meta.height * 1920 / meta.width);
  const resized = await sharp(src).resize(1920, height).toBuffer();
  const top = Math.min(TOP_CROP, height - 900);
  await sharp(resized).extract({ left: 0, top, width: 1920, height: 900 }).png().toFile(dest);
  console.log('written', dest, 'resized height', height, 'top crop', top);
})();
