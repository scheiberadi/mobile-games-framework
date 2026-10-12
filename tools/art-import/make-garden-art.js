// Plant Growth art made in code: the water drop (drop.png), and two recoloured flowers (stage_flower_1/2, stage_fruit_1/2: the cream
// petals of the bud and the bloom turned pink and lilac, leaves and the yellow heart untouched).
//   node tools/art-import/make-garden-art.js
const path = require('path');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const dir = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');

const looks = [{ suffix: '_1', hue: 335, sat: 0.38 }, { suffix: '_2', hue: 270, sat: 0.34 }]; // pink, lilac

function hsvToRgb(h, s, v) {
  const c = v * s, x = c * (1 - Math.abs(((h / 60) % 2) - 1)), m = v - c;
  const [r, g, b] = h < 60 ? [c, x, 0] : h < 120 ? [x, c, 0] : h < 180 ? [0, c, x] : h < 240 ? [0, x, c] : h < 300 ? [x, 0, c] : [c, 0, x];
  return [Math.round((r + m) * 255), Math.round((g + m) * 255), Math.round((b + m) * 255)];
}

async function recolour(name, look) {
  const { data, info } = await sharp(path.join(dir, name + '.png')).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    const max = Math.max(r, g, b), min = Math.min(r, g, b);
    const sat = max === 0 ? 0 : (max - min) / max;
    const warmWhite = max > 200 && sat < 0.28 && r >= g - 6 && g >= b - 6; // cream/white petal, not the green leaves or the orange heart
    if (!warmWhite || data[i + 3] === 0) continue;
    const strength = Math.min(1, (0.28 - sat) / 0.1); // soft edge of the mask
    const [nr, ng, nb] = hsvToRgb(look.hue, look.sat * (1 - (max - 200) / 120 * 0.5), Math.min(1, max / 255 * 1.0));
    data[i] = Math.round(r + (nr - r) * strength);
    data[i + 1] = Math.round(g + (ng - g) * strength);
    data[i + 2] = Math.round(b + (nb - b) * strength);
  }
  await sharp(data, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toFile(path.join(dir, name + look.suffix + '.png'));
}

(async () => {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="128" height="192" viewBox="0 0 128 192">
    <defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#8fdcff"/><stop offset="1" stop-color="#1e88e5"/></linearGradient></defs>
    <path d="M64 8 C64 8 18 82 18 124 C18 156 38 180 64 180 C90 180 110 156 110 124 C110 82 64 8 64 8 Z" fill="url(#g)" stroke="#16609f" stroke-width="7" stroke-linejoin="round"/>
    <path d="M42 120 C42 138 50 152 62 158" fill="none" stroke="#ffffff" stroke-opacity="0.75" stroke-width="9" stroke-linecap="round"/></svg>`;
  await sharp(Buffer.from(svg)).png().toFile(path.join(dir, 'drop.png'));
  for (const look of looks) for (const name of ['stage_flower', 'stage_fruit']) await recolour(name, look);
  console.log('done');
})();
