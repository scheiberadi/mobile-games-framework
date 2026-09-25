// Placeholder map art, the layout guide for ChatGPT, and the gear icon. Reads places-layout.json (kept equal to
// Rules/Places.cs by PlacesLayoutTests). World units are centre origin, y up; pictures are top-left origin, y down.
const path = require('path');
const fs = require('fs');
const sharp = require('sharp');

const layout = JSON.parse(fs.readFileSync(path.join(__dirname, 'places-layout.json'), 'utf8'));
const art = path.resolve(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art');
const guideDir = path.resolve(__dirname, '../../art/eva/map');
const force = process.argv.includes('--force');
const W = layout.world.w, H = layout.world.h;

// world point -> pixel in a picture whose top-left is world (left, top)
const px = (p, left, top) => ({ x: p.x - left, y: top - p.y });

function skip(file) {
  if (!force && fs.existsSync(file)) { console.log('kept ' + path.relative(art, file)); return true; }
  return false;
}

async function backdropHalves() {
  const svg = (side) => `<svg xmlns="http://www.w3.org/2000/svg" width="${W / 2}" height="${H}">
    <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#c9e9a8"/><stop offset="1" stop-color="#a9d68a"/></linearGradient></defs>
    <rect width="${W / 2}" height="${H}" fill="url(#g)"/>
    <ellipse cx="${side === 'left' ? 300 : 1100}" cy="260" rx="180" ry="70" fill="#b7de98"/>
    <ellipse cx="${side === 'left' ? 900 : 500}" cy="1050" rx="220" ry="80" fill="#9fcf80"/>
  </svg>`;
  for (const side of ['left', 'right']) {
    const file = path.join(art, `world/map_world_${side}.png`);
    if (skip(file)) continue;
    await sharp(Buffer.from(svg(side))).png().toFile(file);
    console.log('wrote ' + path.relative(art, file));
  }
}

async function roads() {
  for (const place of layout.places) {
    const box = place.roadBox;
    if (!box.w) continue;
    const file = path.join(art, `world/road_${place.id.toLowerCase()}.png`);
    if (skip(file)) continue;
    const left = box.x - box.w / 2, top = box.y + box.h / 2;
    const points = place.road.map(p => px(p, left, top)).map(p => `${p.x},${p.y}`).join(' ');
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${box.w}" height="${box.h}">
      <polyline points="${points}" fill="none" stroke="#a9773f" stroke-width="84" stroke-linecap="round" stroke-linejoin="round"/>
      <polyline points="${points}" fill="none" stroke="#d9b06e" stroke-width="64" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>`;
    await sharp(Buffer.from(svg)).png().toFile(file);
    console.log('wrote ' + path.relative(art, file));
  }
}

// The existing building icons stand in for the buildings until the real pictures arrive.
function buildings() {
  const from = { house: 'house_icon', school: 'school_icon', store: 'store_icon' };
  for (const name of Object.keys(from)) {
    const file = path.join(art, `world/place_${name}.png`);
    if (skip(file)) continue;
    fs.copyFileSync(path.join(art, `world/${from[name]}.png`), file);
    console.log('wrote ' + path.relative(art, file));
  }
}

async function guide() {
  fs.mkdirSync(guideDir, { recursive: true });
  const at = p => px(p, -W / 2, H / 2);
  const rect = (b, fill, stroke, label) => {
    const c = at({ x: b.x - b.w / 2, y: b.y + b.h / 2 });
    return `<rect x="${c.x}" y="${c.y}" width="${b.w}" height="${b.h}" fill="${fill}" stroke="${stroke}" stroke-width="4"/>` +
      `<text x="${c.x + 10}" y="${c.y + 30}" font-family="sans-serif" font-size="26" fill="#222">${label}</text>`;
  };
  let body = '';
  for (const place of layout.places) {
    if (place.roadBox.w) {
      body += rect(place.roadBox, 'none', '#3060d0', place.id + ' road picture box');
      const pts = place.road.map(p => at(p)).map(p => `${p.x},${p.y}`).join(' ');
      body += `<polyline points="${pts}" fill="none" stroke="#d02020" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>`;
    }
    body += rect(place.tapBox, 'rgba(255,200,80,0.45)', '#8a5a2b', place.id + ' building');
    // where the characters stand at this place (a 240 x 240 area): keep-clear ground, no building or scenery
    body += rect({ x: place.standing.x, y: place.standing.y, w: 240, h: 240 }, 'rgba(120,180,255,0.25)', '#3060d0', place.id + ' characters');
  }
  const view = at({ x: -layout.view.w / 2, y: layout.view.h / 2 });
  body += `<rect x="${view.x}" y="${view.y}" width="${layout.view.w}" height="${layout.view.h}" fill="none" stroke="#d020d0" stroke-width="6" stroke-dasharray="24 12"/>`;
  body += `<text x="${view.x + 10}" y="${view.y + 36}" font-family="sans-serif" font-size="30" fill="#d020d0">first view (1440 x 900)</text>`;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}"><rect width="${W}" height="${H}" fill="#cfe8b8"/>${body}</svg>`;
  await sharp(Buffer.from(svg)).png().toFile(path.join(guideDir, 'layout-guide.png'));
  console.log('wrote art/eva/map/layout-guide.png');
}

// A plain gear: eight teeth around a ring on a soft round plate (matches the Home button's look).
async function gear() {
  const file = path.join(art, 'icons/gear.png');
  if (skip(file)) return;
  let teeth = '';
  for (let i = 0; i < 8; i++) teeth += `<rect x="236" y="70" width="40" height="70" rx="8" fill="#7a5a3a" transform="rotate(${i * 45} 256 256)"/>`;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">
    <circle cx="256" cy="256" r="236" fill="#fff4d6" stroke="#8a5a2b" stroke-width="12"/>
    ${teeth}
    <circle cx="256" cy="256" r="118" fill="#7a5a3a"/>
    <circle cx="256" cy="256" r="52" fill="#fff4d6"/>
  </svg>`;
  await sharp(Buffer.from(svg)).png().toFile(file);
  console.log('wrote icons/gear.png');
}

(async () => {
  const mode = process.argv[2];
  if (mode === 'placeholders') { await backdropHalves(); await roads(); buildings(); }
  else if (mode === 'guide') await guide();
  else if (mode === 'gear') await gear();
  else { console.log('usage: node map-art.js placeholders [--force] | guide | gear'); process.exit(1); }
})();
