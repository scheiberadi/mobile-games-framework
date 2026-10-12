// The water drop (Resources/Art/sciencelab/drop.png), drawn from an SVG: the icon for "water" in a plant's thought cloud and the falling
// drops. node tools/art-import/make-drop.js
const path = require('path');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));
const dir = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'sciencelab');
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="128" height="192" viewBox="0 0 128 192">
  <defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#8fdcff"/><stop offset="1" stop-color="#1e88e5"/></linearGradient></defs>
  <path d="M64 8 C64 8 18 82 18 124 C18 156 38 180 64 180 C90 180 110 156 110 124 C110 82 64 8 64 8 Z" fill="url(#g)" stroke="#16609f" stroke-width="7" stroke-linejoin="round"/>
  <path d="M42 120 C42 138 50 152 62 158" fill="none" stroke="#ffffff" stroke-opacity="0.75" stroke-width="9" stroke-linecap="round"/></svg>`;
sharp(Buffer.from(svg)).png().toFile(path.join(dir, 'drop.png')).then(() => console.log('drop.png'));
