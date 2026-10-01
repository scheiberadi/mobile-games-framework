// Generates art/eva/cat-v2/cat-guide.png: a labelled placement diagram for Eva's cat art redo (replacing
// the procedurally-drawn art/eva/cat/*.svg with real ChatGPT-illustrated layers, matching the soft-3D style
// the rest of the game already uses). Attach this to every prompt in art/eva/cat-v2/PROMPTS.md, the same way
// art/eva/map/PROMPTS.md attaches layout-guide.png and art/character/PROMPTS.md attaches reference-guide.png.
//
// KEEP IN SYNC BY HAND: the boxes below are the ACTUAL bounding boxes of the current
// EvasLearningWorld/Assets/Eva/Resources/Art/cat/*.png files (measured with Pillow's Image.getbbox(), not
// guessed), which is exactly the layout RigFactory.CreateEva's pivots (Assets/Eva/App/Characters/
// RigFactory.cs) already expect - reusing these guarantees the new art drops onto the existing rig with no
// code change. If RigFactory's pivots or the rig's proportions ever change, re-measure and update below.
const path = require('path');
const fs = require('fs');
const sharp = require('sharp');

const outDir = path.resolve(__dirname, '../../art/eva/cat-v2');

// name -> [xMin, yMin, xMax, yMax] on the shared 1000x1000 canvas (y down) - see cat-boxes.json's own
// comment for provenance/how to keep it in sync. compose-cat-parts.js reads the same file.
const catData = JSON.parse(fs.readFileSync(path.join(outDir, 'cat-boxes.json'), 'utf8'));
const boxes = catData.boxes;

// Draw order (Task 1-style: later = in front), matching RigFactory.CreateEva's own child order exactly -
// Shadow, Tail, Body, (LegL, LegR, Chest, Head > EarL, EarR, Eyes, Mouth). Colours are for THIS GUIDE only,
// arbitrary and not a style reference.
const order = catData.order;
const colours = {
  Shadow: '#999999', Tail: '#8a5a2b', Body: '#7a4a1e', LegL: '#c23fa0', LegR: '#c23fa0',
  Chest: '#2a7de1', Head: '#3fae4a', EarL: '#3fae4a', EarR: '#3fae4a', Eyes: '#1a1a1a', Mouth: '#d02020',
};

const W = 1000, H = 1000;

function rect(name) {
  const [x0, y0, x1, y1] = boxes[name];
  const c = colours[name];
  return `<rect x="${x0}" y="${y0}" width="${x1 - x0}" height="${y1 - y0}" fill="none" stroke="${c}" stroke-width="4"/>` +
    `<text x="${x0 + 4}" y="${y0 + 20}" font-family="sans-serif" font-size="20" fill="${c}">${name}</text>`;
}

(async () => {
  fs.mkdirSync(outDir, { recursive: true });

  let body = '';
  for (const name of order) body += rect(name);

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}">
    <rect width="${W}" height="${H}" fill="#ffffff"/>
    <text x="16" y="30" font-family="sans-serif" font-size="22" fill="#222">Eva cat-art placement guide - measured from the current rig, not a style example</text>
    <text x="16" y="54" font-family="sans-serif" font-size="15" fill="#666">Each box is where that layer's own content must land on the shared 1000x1000 canvas. Front-facing sitting pose.</text>
    ${body}
  </svg>`;

  await sharp(Buffer.from(svg)).png().toFile(path.join(outDir, 'cat-guide.png'));
  console.log('wrote art/eva/cat-v2/cat-guide.png');
})();
