// The scrolling world backdrop: ~/Downloads/<source> (32:15, see art/eva/map/PROMPTS.md "v3") -> two 1440 x 1350 halves
// Resources/Art/world/map_world_left|right.png (the game stretches each half to 1800 x 1350 world units).
//   node tools/art-import/import-map-world.js "Colorful Meadow Hub Map.png"
const path = require('path');
const os = require('os');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const source = process.argv[2];
if (!source) { console.error('usage: node import-map-world.js <source.png>'); process.exit(1); }
const art = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'world');
(async () => {
  const buffer = await sharp(path.join(os.homedir(), 'Downloads', source)).resize(2880, 1350, { fit: 'cover' }).png().toBuffer();
  await sharp(buffer).extract({ left: 0, top: 0, width: 1440, height: 1350 }).png().toFile(path.join(art, 'map_world_left.png'));
  await sharp(buffer).extract({ left: 1440, top: 0, width: 1440, height: 1350 }).png().toFile(path.join(art, 'map_world_right.png'));
  console.log('imported map_world_left/right.png');
})();
