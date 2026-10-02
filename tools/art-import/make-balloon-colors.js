// Six plain (digit-free) balloon colours for Balloon Popping, made by turning the hue of arcade/balloon_odd:
//   node tools/art-import/make-balloon-colors.js   -> Resources/Art/arcade/bal_a..bal_f.png
const sharp = require('sharp');
const path = require('path');
const dir = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/arcade');
const hues = { a: 0, b: 60, c: 90, d: 180, e: 240, f: 300 };
const brighter = { c: 1.3 }; // the turned-yellow one comes out muddy
(async () => {
  for (const [id, hue] of Object.entries(hues)) {
    await sharp(path.join(dir, 'balloon_odd.png')).modulate({ hue, brightness: brighter[id] || 1, saturation: brighter[id] ? 1.2 : 1 }).toFile(path.join(dir, 'bal_' + id + '.png'));
  }
  const tiles = [];
  for (const [i, id] of Object.keys(hues).entries()) tiles.push({ input: await sharp(path.join(dir, 'bal_' + id + '.png')).resize(150, 150).toBuffer(), left: i * 150, top: 0 });
  await sharp({ create: { width: 900, height: 150, channels: 3, background: { r: 110, g: 175, b: 80 } } }).composite(tiles).png().toFile(process.argv[2] || 'bal_preview.png');
})();
