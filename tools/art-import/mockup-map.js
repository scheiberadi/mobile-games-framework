// Renders a static top-down mockup of the map world at 1 canvas px = 1 world unit, matching how
// Unity actually stretches map_world_left/right.png onto the world (see handoff calibration notes).
// Draws each place's building sprite at its current TapBox, plus a labeled TapBox outline, so
// placement can be reviewed and approved here before touching game code or building an APK.
const path = require("path");
const sharp = require("sharp");

const ROOT = path.resolve(__dirname, "../../EvasLearningWorld/Assets/Eva/Resources/Art");
const WORLD_W = 3600, WORLD_H = 1350;
const VIEW_W = 1440, VIEW_H = 900;

// Mirrors Places.cs. Only fields needed for this mockup.
const PLACES = [
  { id: "House", tapBox: [60, 20, 320, 280], sprite: "place_house" },
  { id: "School", tapBox: [-470, 40, 280, 240], sprite: "place_school" },
  { id: "Store", tapBox: [540, -225, 280, 240], sprite: "place_store" },
  { id: "Playground", tapBox: [300, 430, 280, 240], sprite: "place_playground" },
  { id: "ZooFarm", tapBox: [-950, 430, 280, 240], sprite: "place_zoofarm" },
  { id: "ScienceLab", tapBox: [950, 380, 280, 240], sprite: "place_sciencelab" },
  { id: "Workshop", tapBox: [-1150, 50, 280, 240], sprite: "place_workshop" },
    { id: "ArtStudio", tapBox: [1000, -380, 280, 240], sprite: "place_artstudio" },
    { id: "BrainGym", tapBox: [-300, 430, 280, 240], sprite: "place_braingym" },
    { id: "FriendsPark", tapBox: [930, -40, 280, 240], sprite: "place_friendspark" },
    { id: "Arcade", tapBox: [-400, -380, 280, 240], sprite: "place_arcade" },
];

const toCanvasX = (worldX) => worldX + WORLD_W / 2;
const toCanvasY = (worldY) => WORLD_H / 2 - worldY;

async function main() {
  const leftTile = await sharp(path.join(ROOT, "world/map_world_left.png")).resize(WORLD_W / 2, WORLD_H).toBuffer();
  const rightTile = await sharp(path.join(ROOT, "world/map_world_right.png")).resize(WORLD_W / 2, WORLD_H).toBuffer();

  let composite = sharp({ create: { width: WORLD_W, height: WORLD_H, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 1 } } });
  const overlays = [
    { input: leftTile, left: 0, top: 0 },
    { input: rightTile, left: WORLD_W / 2, top: 0 },
  ];

  // Building sprites, resized to their TapBox footprint and placed at its centre.
  for (const place of PLACES) {
    const [x, y, w, h] = place.tapBox;
    const spritePath = path.join(ROOT, "world", place.sprite + ".png");
    const buf = await sharp(spritePath).resize(Math.round(w), Math.round(h), { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } }).toBuffer();
    overlays.push({ input: buf, left: Math.round(toCanvasX(x) - w / 2), top: Math.round(toCanvasY(y) - h / 2) });
  }

  let svg = `<svg width="${WORLD_W}" height="${WORLD_H}" xmlns="http://www.w3.org/2000/svg">`;
  // First-view viewport, centred at (0,0).
  const vx0 = toCanvasX(-VIEW_W / 2), vy0 = toCanvasY(VIEW_H / 2);
  svg += `<rect x="${vx0}" y="${vy0}" width="${VIEW_W}" height="${VIEW_H}" fill="none" stroke="cyan" stroke-width="4" stroke-dasharray="16,10"/>`;
  svg += `<text x="${vx0 + 8}" y="${vy0 + 26}" font-size="26" fill="cyan" font-family="sans-serif">first view</text>`;

  for (const place of PLACES) {
    const [x, y, w, h] = place.tapBox;
    const cx = toCanvasX(x), cy = toCanvasY(y);
    svg += `<rect x="${cx - w / 2}" y="${cy - h / 2}" width="${w}" height="${h}" fill="none" stroke="yellow" stroke-width="3"/>`;
    svg += `<text x="${cx - w / 2 + 4}" y="${cy - h / 2 - 8}" font-size="26" fill="yellow" font-family="sans-serif" font-weight="bold" stroke="black" stroke-width="0.5">${place.id}</text>`;
  }
  svg += `</svg>`;
  overlays.push({ input: Buffer.from(svg) });

  const out = path.join(__dirname, "../../mockup-current-placement.png");
  await composite.composite(overlays).png().toFile(out);
  console.log("wrote", out);
}

main().catch((e) => { console.error(e); process.exit(1); });
