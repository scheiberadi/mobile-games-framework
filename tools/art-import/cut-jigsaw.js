// Re-cuts the Jigsaw picture for the grid sizes below the 5x5 one.
//
// Resources/Art/jigsaw/piece_<row>_<col>.png is the 5x5 cut of one picture. The 2x2, 2x3, 3x3 and 4x4 levels used
// to borrow its top-left pieces, which only ever showed a corner of the picture. This stitches the 5x5 pieces back
// into the picture and cuts it again per grid as grid<cols>x<rows>_<row>_<col>.png.
//
//   node tools/art-import/cut-jigsaw.js
const fs = require('fs');
const path = require('path');
const sharp = require('./node_modules/sharp');

const DIR = path.resolve(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/jigsaw');
const SOURCE = 5;
const GRIDS = [[2, 2], [2, 3], [3, 3], [4, 4]]; // [columns, rows]

(async () => {
  // The pieces are 250 or 251 px (the original was cut at fractional positions), so place them by running offsets.
  const meta = [];
  for (let r = 0; r < SOURCE; r++)
    for (let c = 0; c < SOURCE; c++) meta.push(await sharp(path.join(DIR, `piece_${r}_${c}.png`)).metadata());
  const at = (r, c) => meta[r * SOURCE + c];
  const lefts = [0], tops = [0];
  for (let c = 0; c < SOURCE; c++) lefts.push(lefts[c] + at(0, c).width);
  for (let r = 0; r < SOURCE; r++) tops.push(tops[r] + at(r, 0).height);
  const size = lefts[SOURCE];
  if (tops[SOURCE] !== size) throw new Error('picture is not square: ' + size + ' x ' + tops[SOURCE]);
  const composites = [];
  for (let r = 0; r < SOURCE; r++)
    for (let c = 0; c < SOURCE; c++)
      composites.push({ input: path.join(DIR, `piece_${r}_${c}.png`), left: lefts[c], top: tops[r] });
  const picture = await sharp({ create: { width: size, height: size, channels: 3, background: '#ffffff' } })
    .composite(composites).png().toBuffer();
  if (process.argv.includes('--save-picture')) fs.writeFileSync(path.join(__dirname, 'jigsaw-picture.png'), picture);

  for (const [cols, rows] of GRIDS) {
    for (let r = 0; r < rows; r++)
      for (let c = 0; c < cols; c++) {
        const left = Math.round(c * size / cols), right = Math.round((c + 1) * size / cols);
        const top = Math.round(r * size / rows), bottom = Math.round((r + 1) * size / rows);
        await sharp(picture).extract({ left, top, width: right - left, height: bottom - top })
          .png().toFile(path.join(DIR, `grid${cols}x${rows}_${r}_${c}.png`));
      }
    console.log(`grid${cols}x${rows}: ${cols * rows} pieces`);
  }
})();
