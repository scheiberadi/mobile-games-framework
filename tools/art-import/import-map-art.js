const path = require('path');
const fs = require('fs');
const sharp = require('sharp');

const downloads = 'C:/Users/schei/Downloads';
const art = path.resolve(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/world');
const layout = JSON.parse(fs.readFileSync(path.join(__dirname, 'places-layout.json'), 'utf8'));
const box = id => layout.places.find(p => p.id === id);

// Magenta (#ff00ff) background to transparent, tolerance 90 on the distance from magenta; already transparent art is kept.
async function cutMagenta(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4) {
    const d = Math.abs(data[i] - 255) + data[i + 1] + Math.abs(data[i + 2] - 255);
    if (d < 90) data[i + 3] = 0;
    else if (d < 160) data[i + 3] = Math.min(data[i + 3], Math.round((d - 90) / 70 * 255));
  }
  // Defringe: pixels within 2 px of transparency that still lean magenta lose the spill (red and blue pulled down to green).
  const w = info.width, h = info.height, a = i => data[i + 3];
  const spill = [];
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const i = (y * w + x) * 4;
    if (a(i) === 0 || Math.min(data[i], data[i + 2]) - data[i + 1] < 25) continue;
    let edge = false;
    for (let dy = -2; dy <= 2 && !edge; dy++) for (let dx = -2; dx <= 2; dx++) {
      const xx = x + dx, yy = y + dy;
      if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
      if (a((yy * w + xx) * 4) === 0) { edge = true; break; }
    }
    if (edge) spill.push(i);
  }
  for (const i of spill) { const g = data[i + 1]; data[i] = Math.min(data[i], g + 10); data[i + 2] = Math.min(data[i + 2], g + 10); }
  return sharp(data, { raw: { width: w, height: h, channels: 4 } });
}

async function backdrop() {
  const src = path.join(downloads, 'map_world.png');
  if (!fs.existsSync(src)) return console.log('skipped map_world.png (not found)');
  const image = sharp(src).resize(2880, 1350, { fit: 'cover' });
  const buffer = await image.png().toBuffer();
  await sharp(buffer).extract({ left: 0, top: 0, width: 1440, height: 1350 }).png().toFile(path.join(art, 'map_world_left.png'));
  await sharp(buffer).extract({ left: 1440, top: 0, width: 1440, height: 1350 }).png().toFile(path.join(art, 'map_world_right.png'));
  console.log('imported map_world_left/right.png');
}

async function building(name, id) {
  const src = path.join(downloads, `place_${name}.png`);
  if (!fs.existsSync(src)) return console.log(`skipped place_${name}.png (not found)`);
  const target = box(id).tapBox;
  const cut = await (await cutMagenta(src)).trim().png().toBuffer();
  await sharp(cut)
    .resize(target.w * 2, target.h * 2, { fit: 'inside', background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png().toFile(path.join(art, `place_${name}.png`));
  // Report how much of the tap box each building fills so the three can be compared.
  const meta = await sharp(path.join(art, `place_${name}.png`)).metadata();
  console.log(`imported place_${name}.png, fills ${Math.round(100 * Math.max(meta.width / (target.w * 2), meta.height / (target.h * 2)))}% of its tap box on its longer side`);
}

async function road(name, id) {
  const src = path.join(downloads, `road_${name}.png`);
  if (!fs.existsSync(src)) return console.log(`skipped road_${name}.png (not found)`);
  const target = box(id).roadBox;
  const cut = await (await cutMagenta(src)).png().toBuffer();
  await sharp(cut).resize(target.w, target.h, { fit: 'fill' }).png().toFile(path.join(art, `road_${name}.png`));
  console.log(`imported road_${name}.png`);
}

(async () => {
  await backdrop();
  await building('house', 'House');
  await building('school', 'School');
  await building('store', 'Store');
  await road('school', 'School');
  await road('store', 'Store');
})();
