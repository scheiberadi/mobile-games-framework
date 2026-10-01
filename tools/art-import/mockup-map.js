// Renders a static top-down mockup of the map world at 1 canvas px = 1 world unit, matching how
// Unity actually stretches map_world_left/right.png onto the world (see handoff calibration notes).
// Draws each place's building sprite at its current TapBox, plus a labeled TapBox outline, so
// placement can be reviewed and approved here before touching game code or building an APK.
const path = require("path");
const sharp = require("sharp");

const ROOT = path.resolve(__dirname, "../../EvasLearningWorld/Assets/Eva/Resources/Art");
const WORLD_W = 3600, WORLD_H = 1350;
const VIEW_W = 1440, VIEW_H = 900;

// Reads places-layout.json (kept in sync with Places.cs). Usage: node mockup-map.js [outDir] [--debug]
// Without --debug nothing covers the scenery except the buildings and the two characters at the current place's spot.
const LAYOUT = require("./places-layout.json");
const PLACES = LAYOUT.places.map((p) => ({ id: p.id, tapBox: [p.tapBox.x, p.tapBox.y, p.tapBox.w, p.tapBox.h], sprite: "place_" + p.id.toLowerCase(), road: p.road, standing: p.standing }));
const OUT_DIR = process.argv[2] ? path.resolve(process.argv[2]) : path.join(__dirname, "../..");
const DEBUG = process.argv.includes("--debug");
// --chars=Arcade,ArtStudio draws the player and Eva at those places' standing spots (default: none).
const CHARS = ((process.argv.find((a) => a.startsWith("--chars=")) || "").slice(8)).split(",").filter(Boolean);

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

  // First-view viewport, centred at (0,0).
  const vx0 = toCanvasX(-VIEW_W / 2), vy0 = toCanvasY(VIEW_H / 2);
  let svg = `<svg width="${WORLD_W}" height="${WORLD_H}" xmlns="http://www.w3.org/2000/svg">`;
  if (DEBUG) svg += `<rect x="${vx0}" y="${vy0}" width="${VIEW_W}" height="${VIEW_H}" fill="none" stroke="cyan" stroke-width="4" stroke-dasharray="16,10"/>`;
  for (const place of PLACES) {
    const [x, y, w, h] = place.tapBox;
    const cx = toCanvasX(x), cy = toCanvasY(y);
    if (DEBUG) {
      svg += `<rect x="${cx - w / 2}" y="${cy - h / 2}" width="${w}" height="${h}" fill="none" stroke="yellow" stroke-width="3"/>`;
      svg += `<polyline points="${place.road.map((p) => toCanvasX(p.x) + "," + toCanvasY(p.y)).join(" ")}" fill="none" stroke="rgba(255,255,255,0.7)" stroke-width="6" stroke-dasharray="14,10"/>`;
    }
    if (DEBUG) svg += `<text x="${cx - w / 2 + 4}" y="${cy - h / 2 - 8}" font-size="26" fill="yellow" font-family="sans-serif" font-weight="bold" stroke="black" stroke-width="0.5">${place.id}</text>`;
    // Characters at the standing spot, exactly as MapScreen.PlaceCharacters: player feet at spot+(-60,-80), Eva at spot+(60,-80), 160 tall.
    if (!CHARS.includes(place.id)) continue;
    const sx = toCanvasX(place.standing.x), sy = toCanvasY(place.standing.y);
    svg += `<rect x="${sx - 60 - 45}" y="${sy - 80}" width="90" height="160" rx="14" fill="white" stroke="#c00" stroke-width="4"/>`;
    svg += `<ellipse cx="${sx + 60}" cy="${sy + 20}" rx="60" ry="60" fill="black" stroke="#0c0" stroke-width="4"/>`;
    if (DEBUG) svg += `<rect x="${sx - 105}" y="${sy - 80}" width="230" height="160" fill="none" stroke="magenta" stroke-width="2"/>`;
  }
  svg += `</svg>`;
  overlays.push({ input: Buffer.from(svg) });

  const out = path.join(OUT_DIR, DEBUG ? "mockup-debug.png" : "mockup-clean.png");
  await composite.composite(overlays).png().toFile(out);
  console.log("wrote", out);
}

main().catch((e) => { console.error(e); process.exit(1); });
