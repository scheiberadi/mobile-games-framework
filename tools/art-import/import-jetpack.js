// Jetpack Cat art: ~/Downloads/floppy_{cat,flame,pillar}.png (flat #00FF00 backgrounds) -> transparent, trimmed sprites in Resources/Art/arcade,
// and the sky ~/Downloads/floppy_bg.png -> Resources/Art/world/jetpack_bg.png (via import-scene-bg.js).
//   node tools/art-import/import-jetpack.js
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

const downloads = path.join(os.homedir(), 'Downloads');
const art = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'arcade');

// Green screen to alpha: how much greener than red/blue a pixel is says how much of it is background; the green cast on the edges is removed.
async function keyOut(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    const excess = g - Math.max(r, b);
    let alpha = 1;
    if (excess > 25) alpha = Math.max(0, 1 - (excess - 25) / 90);
    data[i + 3] = Math.round(alpha * 255);
    if (excess > 0 && alpha < 1) data[i + 1] = Math.min(g, Math.max(r, b)); // despill the edge
  }
  return sharp(data, { raw: { width: info.width, height: info.height, channels: 4 } });
}

async function sprite(name, out, width) {
  const keyed = await (await keyOut(path.join(downloads, name))).png().toBuffer();
  const trimmed = await sharp(keyed).trim({ threshold: 8 }).png().toBuffer();
  await sharp(trimmed).resize({ width }).png().toFile(path.join(art, out));
  const meta = await sharp(path.join(art, out)).metadata();
  console.log(out, meta.width + 'x' + meta.height);
}

(async () => {
  await sprite('floppy_cat.png', 'jetpack_cat.png', 640);
  await sprite('floppy_flame.png', 'jetpack_flame.png', 200);
  await sprite('floppy_pillar.png', 'jetpack_pillar.png', 300);
})();
