// Dress for Weather: the four outfit cards (sciencelab/dressicon_<sun|rain|snow|wind>.png), one clothing item each, cut out of the
// green-screen sheets dress_head.png (sun hat, sunglasses, bobble hat) and dress_body.png (raincoat, windbreaker, scarf) in ~/Downloads.
const path = require('path');
const os = require('os');
const sharp = require('./node_modules/sharp');
const { keyOut, runs } = require('./keyout');
const OUT = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/sciencelab/');
const sheets = [
  ['dress_head', 3, { 0: 'sun', 2: 'snow' }],
  ['dress_body', 3, { 0: 'rain', 1: 'wind' }],
];
(async () => {
  for (const [file, count, wanted] of sheets) {
    const keyed = await keyOut(path.join(os.homedir(), 'Downloads', file + '.png'));
    const meta = await sharp(keyed).metadata();
    const xr = await runs(keyed, 'x', count);
    for (const [index, id] of Object.entries(wanted)) {
      const [x0, x1] = xr[index];
      const col = await sharp(keyed).extract({ left: x0, top: 0, width: x1 - x0, height: meta.height }).png().toBuffer();
      const [[y0, y1]] = await runs(col, 'y', 1);
      await sharp(col).extract({ left: 0, top: y0, width: x1 - x0, height: y1 - y0 }).resize({ height: 360, withoutEnlargement: true }).png()
        .toFile(OUT + 'dressicon_' + id + '.png');
      console.log('dressicon_' + id);
    }
  }
})();
