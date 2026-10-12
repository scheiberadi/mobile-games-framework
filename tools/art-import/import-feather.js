// Wake up! art: the feather ~/Downloads/feather_item.png (flat #00FF00 background) -> transparent, trimmed sprite Resources/Art/sciencelab/feather.png.
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

const source = path.join(os.homedir(), 'Downloads', 'feather_item.png');
const dest = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab', 'feather.png');

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

(async () => {
  const keyed = await (await keyOut(source)).png().toBuffer();
  const trimmed = await sharp(keyed).trim({ threshold: 8 }).png().toBuffer();
  await sharp(trimmed).resize({ width: 400 }).png().toFile(dest);
  const meta = await sharp(dest).metadata();
  console.log('feather.png', meta.width + 'x' + meta.height);
})();
