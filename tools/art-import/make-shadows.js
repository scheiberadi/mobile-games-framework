// Zoo & Farm "Dress the animal" (Covering game): a dark shadow over each animal's trunk, so only the head, legs, arms/wings and tail show.
//   node tools/art-import/make-shadows.js                 writes Resources/Art/zoofarm/shadow_<id>.png for every animal in TRUNKS
//   node tools/art-import/make-shadows.js --check out.png id id ...   a review sheet (3 per row): the animal, the shadow, the polygon outline
// The shadow picture has the animal's own size (512 x 512) and is transparent everywhere but on the animal's trunk, so the screen just
// lays it over the animal. Each trunk is a hand-measured polygon in fractions of the 512 x 512 animal picture (x right, y down); the
// edge is feathered a few pixels so it reads as a shadow rather than a cut-out. Animals with no clear trunk (fish, dolphin, whale,
// shark, octopus, snake) or a trunk hidden behind a mane or paws (lion, monkey, parrot, squirrel, beaver) are left out and never used by the game.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const ROOT = path.join(__dirname, '../..');
const RES = path.join(ROOT, 'EvasLearningWorld/Assets/Eva/Resources/Art/zoofarm');
const S = 512;
const FEATHER = 3.5;
const SHADOW = { r: 30, g: 24, b: 48, strength: 0.88 };

const TRUNKS = {
  cow: [[0.16, 0.40], [0.30, 0.34], [0.44, 0.33], [0.52, 0.45], [0.60, 0.52], [0.63, 0.62], [0.57, 0.72], [0.46, 0.72], [0.30, 0.72], [0.20, 0.70], [0.15, 0.58]],
  sheep: [[0.10, 0.52], [0.17, 0.42], [0.30, 0.45], [0.36, 0.58], [0.58, 0.62], [0.72, 0.66], [0.74, 0.76], [0.55, 0.80], [0.30, 0.80], [0.13, 0.72]],
  horse: [[0.34, 0.47], [0.38, 0.40], [0.50, 0.38], [0.65, 0.36], [0.74, 0.38], [0.80, 0.48], [0.78, 0.62], [0.60, 0.70], [0.44, 0.68], [0.35, 0.60]],
  pig: [[0.12, 0.50], [0.20, 0.38], [0.30, 0.34], [0.36, 0.50], [0.40, 0.66], [0.66, 0.70], [0.74, 0.80], [0.60, 0.84], [0.30, 0.84], [0.14, 0.72]],
  dog: [[0.25, 0.55], [0.38, 0.50], [0.55, 0.50], [0.75, 0.48], [0.85, 0.55], [0.88, 0.68], [0.78, 0.78], [0.55, 0.78], [0.35, 0.78], [0.24, 0.70]],
  cat: [[0.22, 0.70], [0.30, 0.58], [0.45, 0.52], [0.62, 0.52], [0.75, 0.60], [0.78, 0.75], [0.68, 0.86], [0.45, 0.88], [0.28, 0.84]],
  elephant: [[0.14, 0.68], [0.22, 0.58], [0.40, 0.56], [0.58, 0.58], [0.72, 0.55], [0.82, 0.62], [0.80, 0.72], [0.60, 0.76], [0.35, 0.76], [0.18, 0.74]],
  deer: [[0.22, 0.60], [0.28, 0.50], [0.42, 0.47], [0.56, 0.50], [0.62, 0.60], [0.58, 0.72], [0.40, 0.72], [0.26, 0.68]],
  rhino: [[0.15, 0.60], [0.22, 0.45], [0.36, 0.38], [0.50, 0.42], [0.55, 0.55], [0.56, 0.70], [0.45, 0.78], [0.28, 0.78], [0.17, 0.72]],
  giraffe: [[0.22, 0.60], [0.32, 0.55], [0.45, 0.52], [0.55, 0.55], [0.62, 0.62], [0.60, 0.74], [0.45, 0.78], [0.30, 0.76], [0.20, 0.70]],
  zebra: [[0.22, 0.55], [0.32, 0.47], [0.45, 0.46], [0.56, 0.52], [0.56, 0.64], [0.50, 0.74], [0.30, 0.76], [0.20, 0.68]],
  goat: [[0.10, 0.52], [0.20, 0.42], [0.38, 0.40], [0.55, 0.46], [0.62, 0.56], [0.62, 0.72], [0.45, 0.78], [0.22, 0.76], [0.10, 0.68]],
  llama: [[0.15, 0.58], [0.22, 0.46], [0.40, 0.40], [0.58, 0.42], [0.68, 0.48], [0.72, 0.60], [0.68, 0.74], [0.50, 0.78], [0.30, 0.76], [0.18, 0.70]],
  tiger: [[0.15, 0.64], [0.22, 0.55], [0.35, 0.52], [0.50, 0.52], [0.72, 0.52], [0.82, 0.62], [0.80, 0.72], [0.60, 0.76], [0.38, 0.76], [0.18, 0.74]],
  wolf: [[0.20, 0.58], [0.28, 0.48], [0.42, 0.45], [0.58, 0.50], [0.75, 0.52], [0.82, 0.62], [0.80, 0.71], [0.62, 0.74], [0.40, 0.74], [0.25, 0.70]],
  fox: [[0.40, 0.60], [0.55, 0.55], [0.75, 0.55], [0.85, 0.64], [0.88, 0.74], [0.75, 0.80], [0.50, 0.80], [0.38, 0.74]],
  bear: [[0.10, 0.62], [0.20, 0.46], [0.40, 0.40], [0.54, 0.52], [0.62, 0.62], [0.88, 0.66], [0.90, 0.74], [0.75, 0.80], [0.52, 0.82], [0.30, 0.82], [0.12, 0.74]],
  snowleopard: [[0.25, 0.62], [0.35, 0.50], [0.50, 0.48], [0.65, 0.50], [0.78, 0.58], [0.80, 0.70], [0.70, 0.82], [0.45, 0.84], [0.28, 0.80]],
  gorilla: [[0.38, 0.55], [0.55, 0.52], [0.72, 0.55], [0.74, 0.68], [0.70, 0.80], [0.52, 0.84], [0.38, 0.78], [0.34, 0.65]],
  duck: [[0.15, 0.55], [0.28, 0.47], [0.45, 0.50], [0.58, 0.46], [0.72, 0.55], [0.74, 0.68], [0.66, 0.80], [0.50, 0.86], [0.32, 0.84], [0.18, 0.72], [0.12, 0.62]],
  chicken: [[0.22, 0.55], [0.38, 0.50], [0.55, 0.56], [0.66, 0.57], [0.82, 0.50], [0.85, 0.58], [0.85, 0.70], [0.72, 0.82], [0.50, 0.86], [0.32, 0.82], [0.20, 0.70]],
  eagle: [[0.30, 0.46], [0.50, 0.50], [0.70, 0.48], [0.80, 0.55], [0.80, 0.75], [0.65, 0.82], [0.45, 0.82], [0.30, 0.75], [0.25, 0.60]],
  owl: [[0.40, 0.58], [0.55, 0.55], [0.72, 0.56], [0.76, 0.68], [0.72, 0.82], [0.58, 0.86], [0.45, 0.86], [0.38, 0.75]],
  swan: [[0.25, 0.62], [0.40, 0.55], [0.55, 0.58], [0.75, 0.52], [0.88, 0.62], [0.88, 0.75], [0.70, 0.84], [0.50, 0.85], [0.30, 0.78]],
  turtle: [[0.12, 0.64], [0.24, 0.42], [0.50, 0.36], [0.62, 0.45], [0.62, 0.65], [0.52, 0.75], [0.30, 0.77], [0.15, 0.72]],
  frog: [[0.32, 0.62], [0.45, 0.58], [0.62, 0.58], [0.72, 0.66], [0.68, 0.78], [0.55, 0.84], [0.40, 0.84], [0.30, 0.76]],
};

// The animal's picture and its trunk shadow as raw RGBA, S x S.
async function shadowOf(id, poly) {
  const base = await sharp(path.join(RES, 'animal_' + id + '.png')).ensureAlpha().resize(S, S).raw().toBuffer();
  const points = poly.map(([x, y]) => [x * S, y * S].join(',')).join(' ');
  const mask = await sharp(Buffer.from('<svg width="' + S + '" height="' + S + '" xmlns="http://www.w3.org/2000/svg"><polygon points="' + points + '" fill="white"/></svg>'))
    .blur(FEATHER).greyscale().raw().toBuffer();
  const out = Buffer.alloc(S * S * 4);
  for (let p = 0; p < S * S; p++) {
    const alpha = base[p * 4 + 3] / 255;
    out[p * 4] = SHADOW.r; out[p * 4 + 1] = SHADOW.g; out[p * 4 + 2] = SHADOW.b;
    out[p * 4 + 3] = Math.round(255 * alpha * (mask[p] / 255) * SHADOW.strength);
  }
  return { base, shadow: out };
}

(async () => {
  const args = process.argv.slice(2);
  if (args[0] === '--check') {
    const [, outFile, ...ids] = args;
    const cell = 480, cells = [];
    for (let i = 0; i < ids.length; i++) {
      const { base, shadow } = await shadowOf(ids[i], TRUNKS[ids[i]]);
      const bg = { create: { width: S, height: S, channels: 4, background: { r: 255, g: 244, b: 214, alpha: 1 } } };
      const points = TRUNKS[ids[i]].map(([x, y]) => [x * S, y * S].join(',')).join(' ');
      const outline = Buffer.from('<svg width="' + S + '" height="' + S + '" xmlns="http://www.w3.org/2000/svg"><polygon points="' + points + '" fill="none" stroke="#e00" stroke-width="1.5"/><text x="' + (S - 90) + '" y="' + (S - 10) + '" font-size="22" font-weight="bold" fill="#c00">' + ids[i] + '</text></svg>');
      const img = await sharp(bg).composite([
        { input: await sharp(base, { raw: { width: S, height: S, channels: 4 } }).png().toBuffer() },
        { input: await sharp(shadow, { raw: { width: S, height: S, channels: 4 } }).png().toBuffer() },
        { input: outline },
      ]).png().toBuffer().then((b) => sharp(b).resize(cell, cell).png().toBuffer());
      cells.push({ input: img, left: (i % 3) * cell, top: Math.floor(i / 3) * cell });
    }
    await sharp({ create: { width: 3 * cell, height: Math.ceil(ids.length / 3) * cell, channels: 4, background: 'white' } }).composite(cells).png().toFile(outFile);
    return;
  }
  for (const id of Object.keys(TRUNKS)) {
    const { shadow } = await shadowOf(id, TRUNKS[id]);
    await sharp(shadow, { raw: { width: S, height: S, channels: 4 } }).png({ compressionLevel: 9 }).toFile(path.join(RES, 'shadow_' + id + '.png'));
  }
  // The game needs to know which animals have a shadow and where the middle of each trunk is (where a covering lands).
  const rows = Object.keys(TRUNKS).map((id) => {
    const poly = TRUNKS[id];
    const cx = poly.reduce((a, [x]) => a + x, 0) / poly.length, cy = poly.reduce((a, [, y]) => a + y, 0) / poly.length;
    return '            { "' + id + '", (' + cx.toFixed(3) + 'f, ' + cy.toFixed(3) + 'f) },';
  });
  fs.writeFileSync(path.join(ROOT, 'EvasLearningWorld/Assets/Eva/Rules/TrunkShadows.cs'),
    ['using System.Collections.Generic;', '', 'namespace EvasLearningWorld.Rules', '{',
      '    // Generated by tools/art-import/make-shadows.js - do not edit. The animals that have a trunk shadow (Resources/Art/zoofarm/shadow_<id>)',
      '    // and the middle of each trunk as fractions of the animal picture (x right, y down).',
      '    public static class TrunkShadows',
      '    {',
      '        public static readonly IReadOnlyDictionary<string, (float X, float Y)> Centres = new Dictionary<string, (float X, float Y)>',
      '        {', ...rows, '        };', '    }', '}', ''].join(String.fromCharCode(10)));
  console.log('wrote', Object.keys(TRUNKS).length, 'shadows and Rules/TrunkShadows.cs');
})();
