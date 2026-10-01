// Draws the two small overlays that animate the whole-cat sprite (1000x1000 canvas, same as cat_whole.png):
// cat_eyes.png = closed eyelids (faded in for a blink), cat_mouth.png = open mouth (shown while talking).
// Positions were measured on cat_whole.png: eye centres (370,346) and (502,347), nose bottom (436,413).
const sharp = require('./node_modules/sharp');
const path = require('path');
const DIR = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/cat');

(async () => {
  const { data, info } = await sharp(path.join(DIR, 'cat_whole.png')).raw().toBuffer({ resolveWithObject: true });
  const px = (x, y) => { const i = (y * info.width + x) * info.channels; return [data[i], data[i + 1], data[i + 2]]; };
  // fur colour around the eyes: average of points just above and beside them
  const pts = [[370, 305], [335, 346], [502, 306], [538, 347], [436, 330], [420, 300], [455, 300]];
  const avg = [0, 1, 2].map(c => Math.round(pts.reduce((s, p) => s + px(p[0], p[1])[c], 0) / pts.length));
  const fur = `rgb(${avg.join(',')})`;
  const dark = `rgb(${avg.map(v => Math.round(v * 0.45)).join(',')})`;
  const lid = (cx, cy, id) => `
    <radialGradient id="g${id}"><stop offset="0.88" stop-color="${fur}" stop-opacity="1"/><stop offset="1" stop-color="${fur}" stop-opacity="0"/></radialGradient>
    <ellipse cx="${cx}" cy="${cy}" rx="38" ry="34" fill="url(#g${id})"/>
    <path d="M ${cx - 25} ${cy + 4} Q ${cx} ${cy + 15} ${cx + 25} ${cy + 4}" fill="none" stroke="${dark}" stroke-width="2.6" stroke-linecap="round" opacity="0.85"/>`;
  const eyes = `<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="1000"><defs><filter id="soft"><feGaussianBlur stdDeviation="1.4"/></filter></defs>${lid(370, 346, 1)}${lid(502, 347, 2)}</svg>`;
  const mouth = `<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="1000"><defs><filter id="soft"><feGaussianBlur stdDeviation="0.7"/></filter></defs>
    <g filter="url(#soft)">
      <path d="M 423 429 Q 436 426 449 429 Q 451 447 436 453 Q 421 447 423 429 Z" fill="#1d0a0c" stroke="${dark}" stroke-width="1.8"/>
      <ellipse cx="436" cy="445" rx="8" ry="5" fill="#b8505f"/>
    </g></svg>`;
  await sharp(Buffer.from(eyes)).png().toFile(path.join(DIR, 'cat_eyes.png'));
  await sharp(Buffer.from(mouth)).png().toFile(path.join(DIR, 'cat_mouth.png'));
  console.log('fur', fur);
  if (process.argv[2]) {
    const comp = [{ input: path.join(DIR, 'cat_eyes.png') }, { input: path.join(DIR, 'cat_mouth.png') }];
    const full = await sharp(path.join(DIR, 'cat_whole.png')).composite(comp).png().toBuffer();
    await sharp(full).extract({ left: 316, top: 296, width: 240, height: 200 })
      .resize(960, 800, { kernel: 'lanczos3' }).flatten({ background: '#7ab83c' }).png().toFile(process.argv[2]);
  }
})();
