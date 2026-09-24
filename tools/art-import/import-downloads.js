// Copies the AI-generated house images from the Downloads folder into art/eva/house/ai under their expected names
// (only files that exist; existing sources are overwritten), then run build-house.js / cut-sheets.js.
//   node tools/art-import/import-downloads.js [downloads-dir]
const fs = require('fs');
const os = require('os');
const path = require('path');

const from = process.argv[2] || path.join(os.homedir(), 'Downloads');
const to = path.join(__dirname, '../../art/eva/house/ai');
const NAMES = ['room_living', 'room_dining', 'room_kitchen', 'room_parents', 'room_kids', 'room_bath', 'room_party', 'room_play',
  'hall_ground', 'hall_upper', 'hall_attic', 'shell', 'outside', 'sheet_furniture', 'sheet_icons'];
fs.mkdirSync(to, { recursive: true });
for (const name of NAMES) {
  const src = path.join(from, name + '.png');
  if (!fs.existsSync(src)) continue;
  fs.copyFileSync(src, path.join(to, name + '.png'));
  console.log('imported', name);
}
