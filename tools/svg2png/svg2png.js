const fs = require('node:fs');
const path = require('node:path');
const { Resvg } = require('@resvg/resvg-js');

function renderSvg(svgBuffer, widthPx) {
  const image = new Resvg(svgBuffer, { fitTo: { mode: 'width', value: widthPx } }).render();
  return { width: image.width, height: image.height, pixels: image.pixels, asPng: () => image.asPng() };
}

module.exports = { renderSvg };

if (require.main === module) {
  const [inDir, outDir, width] = process.argv.slice(2);
  if (!inDir || !outDir) { console.error('usage: node svg2png.js <inDir> <outDir> [widthPx]'); process.exit(1); }
  fs.mkdirSync(outDir, { recursive: true });
  for (const file of fs.readdirSync(inDir).filter((f) => f.toLowerCase().endsWith('.svg'))) {
    const img = renderSvg(fs.readFileSync(path.join(inDir, file)), Number(width) || 512);
    fs.writeFileSync(path.join(outDir, file.replace(/\.svg$/i, '.png')), img.asPng());
    console.log(file + ' -> ' + img.width + 'x' + img.height);
  }
}
