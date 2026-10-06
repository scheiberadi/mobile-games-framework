// A game's own full-screen background: ~/Downloads/<source file> -> Resources/Art/world/<name>.png (1920x900: cover-resized to 1920
// wide, then cropped vertically, `top` px cut from the top). Usage:
//   node import-scene-bg.js <source.png> <name> [top] [--fill=x,y,w,h[,srcX] ...]
// --fill (source pixels, repeatable) paints out the w x h rectangle at (x,y) by stretching a pixel column (the one just left of it, or at srcX) across it,
// row by row (the sun, which sits under the coin counter, becomes plain sky).
// --move=x,y,w,h,newX,grassX (source pixels) lifts the w x h piece at (x,y) out (soft edged), covers the hole with grass copied from
// (grassX,y) and puts the piece down at newX (the stump, moved right so the animal waiting on it clears the Back button).
//   node tools/art-import/import-scene-bg.js domestic_wild_bg2.png domestic_wild_bg
//   node tools/art-import/import-scene-bg.js land_sea_air_bg.png land_sea_air_bg 0 --fill=1585,0,147,180 --fill=1732,0,100,154,1575 --move=185,360,390,135,435,900
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

const args = process.argv.slice(2);
const fills = args.filter(a => a.startsWith('--fill=')).map(a => a.slice(7).split(',').map(Number));
const moves = args.filter(a => a.startsWith('--move=')).map(a => a.slice(7).split(',').map(Number));
const [source, name, topArg] = args.filter(a => !a.startsWith('--fill=') && !a.startsWith('--move='));
if (!source || !name) { console.error('usage: node import-scene-bg.js <source.png> <name> [top] [--fill=x,y,w,h]'); process.exit(1); }
const src = path.join(os.homedir(), 'Downloads', source);
const dest = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'world', name + '.png');

(async () => {
  let image = await sharp(src).toBuffer();
  if (fills.length) {
    const layers = [];
    for (const [x, y, w, h, srcX] of fills) {
      const column = await sharp(image).extract({ left: srcX !== undefined ? srcX : x - 8, top: y, width: 6, height: h }).resize(1, h, { fit: "fill" }).toBuffer();
      layers.push({ input: await sharp(column).resize(w, h, { kernel: "nearest", fit: "fill" }).blur(2).toBuffer(), left: x, top: y });
    }
    image = await sharp(image).composite(layers).toBuffer();
  }
  for (const [x, y, w, h, newX, grassX] of moves) {
    const soft = async (inset, blur) => sharp(Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}"><rect x="${inset}" y="${inset}" width="${w - 2 * inset}" height="${h - 2 * inset}" rx="${h / 3}" fill="white"/></svg>`)).blur(blur).png().toBuffer();
    const piece = await sharp(image).extract({ left: x, top: y, width: w, height: h }).composite([{ input: await soft(5, 5), blend: "dest-in" }]).png().toBuffer();
    const grass = await sharp(image).extract({ left: grassX, top: y, width: w, height: h }).composite([{ input: await soft(6, 7), blend: "dest-in" }]).png().toBuffer();
    image = await sharp(image).composite([{ input: grass, left: x, top: y }, { input: piece, left: newX, top: y }]).toBuffer();
  }
  const meta = await sharp(image).metadata();
  const height = Math.round(meta.height * 1920 / meta.width);
  const resized = await sharp(image).resize(1920, height).toBuffer();
  const top = Math.max(0, Math.min(parseInt(topArg || '0', 10), height - 900));
  await sharp(resized).extract({ left: 0, top, width: 1920, height: 900 }).png().toFile(dest);
  console.log('written', dest, 'resized height', height, 'top crop', top, 'fills', fills.length);
})();
