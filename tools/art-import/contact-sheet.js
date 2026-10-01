// Contact sheet for eyeballing cut sprites: node contact-sheet.js out.png dir1/*.png ... (files on a mid-green background, 6 per row)
const sharp = require('sharp');
(async () => {
  const [out, ...files] = process.argv.slice(2);
  const cell = 200, cols = 6, rows = Math.ceil(files.length / cols);
  const tiles = [];
  for (let i = 0; i < files.length; i++) {
    tiles.push({ input: await sharp(files[i]).resize(cell - 8, cell - 8, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } }).png().toBuffer(), left: (i % cols) * cell + 4, top: Math.floor(i / cols) * cell + 4 });
  }
  await sharp({ create: { width: cols * cell, height: rows * cell, channels: 3, background: { r: 110, g: 175, b: 80 } } }).composite(tiles).png().toFile(out);
})();
