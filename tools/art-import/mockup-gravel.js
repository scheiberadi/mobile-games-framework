// Gravel-road mockup: each Road polyline is covered with many tiny overlapping stones (stone_0/1/2 sprites).
// Usage: node mockup-gravel.js [out.png]; writes full-world and first-view crop.
const path = require("path");
const sharp = require("sharp");
const ROOT = path.resolve(__dirname, "../../EvasLearningWorld/Assets/Eva/Resources/Art");
const W = 3600, H = 1350;
const cx = (x) => x + W / 2, cy = (y) => H / 2 - y;
const PLACES = {
  House: [60, 20, 320, 280, "place_house"], School: [-470, 40, 280, 240, "place_school"], Store: [510, -225, 280, 240, "place_store"],
  Playground: [300, 430, 280, 240, "place_playground"], ZooFarm: [-950, 430, 280, 240, "place_zoofarm"], ScienceLab: [950, 380, 280, 240, "place_sciencelab"],
  Workshop: [-1150, 50, 280, 240, "place_workshop"], ArtStudio: [1150, -350, 280, 240, "place_artstudio"], BrainGym: [-300, 430, 280, 240, "place_braingym"],
  FriendsPark: [1300, 0, 280, 240, "place_friendspark"], Arcade: [-550, -400, 280, 240, "place_arcade"],
};
const ROADS = {
  School: [[60,-190],[-40,-155],[-200,-150],[-350,-135],[-470,-120]], Store: [[60,-190],[190,-260],[320,-345],[440,-405],[555,-420]],
  Playground: [[60,-190],[250,-190],[250,130],[345,215]], ZooFarm: [[60,-190],[-40,-150],[-950,-150],[-950,300]],
  ScienceLab: [[60,-190],[250,-190],[250,150],[700,150],[950,150],[950,220]], Workshop: [[60,-190],[-40,-160],[-1150,-130],[-1150,-110]],
  ArtStudio: [[60,-190],[150,-260],[150,-400],[1000,-400],[1000,-190]], BrainGym: [[60,-190],[-200,-190],[-200,200],[-300,200],[-300,300]],
  FriendsPark: [[60,-190],[250,-190],[250,0],[1120,0]], Arcade: [[60,-190],[60,-250],[-550,-250],[-550,-240]],
};
const SPACING = +process.env.SPACING || 4, HALF_WIDTH = +process.env.HALF || 26, MIN = +process.env.MINS || 9, MAX = +process.env.MAXS || 17;
let seed = 12345; const rnd = () => ((seed = (seed * 1664525 + 1013904223) >>> 0) / 4294967296);

function* along(road, step) {
  let carry = 0;
  for (let i = 1; i < road.length; i++) {
    const [ax, ay] = road[i - 1], [bx, by] = road[i], len = Math.hypot(bx - ax, by - ay);
    const nx = -(by - ay) / len, ny = (bx - ax) / len;
    for (let d = carry; d < len; d += step) yield [ax + (bx - ax) * d / len, ay + (by - ay) * d / len, nx, ny];
    carry = ((Math.ceil((len - carry) / step) * step) + carry) - len;
  }
}
async function main() {
  const base = await sharp({ create: { width: W, height: H, channels: 4, background: "#000" } }).composite([
    { input: await sharp(path.join(ROOT, "world/map_world_left.png")).resize(W / 2, H).toBuffer(), left: 0, top: 0 },
    { input: await sharp(path.join(ROOT, "world/map_world_right.png")).resize(W / 2, H).toBuffer(), left: W / 2, top: 0 }]).png().toBuffer();
  const stones = [0, 1, 2].map((i) => path.join(ROOT, `world/stone_${i}.png`));
  const cache = new Map();
  const getStone = async (v, size, rot, bright) => {
    const key = [v, size, rot, bright].join();
    if (!cache.has(key)) cache.set(key, await sharp(stones[v]).resize(size, size).rotate(rot, { background: { r: 0, g: 0, b: 0, alpha: 0 } }).modulate({ brightness: bright }).toBuffer());
    return cache.get(key);
  };
  const seen = new Set(), ov = [];
  for (const road of Object.values(ROADS)) {
    for (const [x, y, nx, ny] of along(road, SPACING)) {
      const k = Math.round(x) + "," + Math.round(y); if (seen.has(k)) continue; seen.add(k);
      for (let n = 0; n < (+process.env.PER || 2); n++) {
        const t = (rnd() + rnd() - 1) * HALF_WIDTH; // centre-weighted across the road width
        const size = MIN + Math.floor(rnd() * (MAX - MIN + 1));
        const rot = Math.floor(rnd() * 8) * 45;
        const bright = 0.75 + Math.floor(rnd() * 5) * 0.1;
        const buf = await getStone(Math.floor(rnd() * 3), size, rot, bright);
        const meta = size; // rotation of a square may grow the buffer; read real size
        const m = await sharp(buf).metadata();
        ov.push({ input: buf, left: Math.round(cx(x + nx * t + (rnd() - .5) * 4) - m.width / 2), top: Math.round(cy(y + ny * t + (rnd() - .5) * 4) - m.height / 2) });
      }
    }
  }
  console.log("stones:", ov.length);
  for (const [, [x, y, w, h, s]] of Object.entries(PLACES)) {
    ov.push({ input: await sharp(path.join(ROOT, "world", s + ".png")).resize(w, h, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } }).toBuffer(), left: Math.round(cx(x) - w / 2), top: Math.round(cy(y) - h / 2) });
  }
  const out = sharp(base).composite(ov);
  const full = await out.png().toBuffer();
  const o = process.argv[2] || "mockup-gravel.png";
  await sharp(full).extract({ left: cx(-720), top: cy(450), width: 1440, height: 900 }).toFile(o.replace(".png", "-firstview.png"));
  await sharp(full).resize(1800).toFile(o);
}
main();
