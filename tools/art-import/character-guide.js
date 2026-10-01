// Generates art/character/reference-guide.png: a labelled proportions diagram for the M5 character system's
// visual style lock (docs/superpowers/plans/2026-09-27-m5-character-system.md, Task 2). Attach this image to
// every wardrobe/hair/face ChatGPT prompt (art/character/PROMPTS.md), the same way art/eva/map/PROMPTS.md
// attaches layout-guide.png - a literal reference the model can see beats a text description alone.
//
// KEEP IN SYNC WITH RigFactory.cs BY HAND: the numbers below are RigFactory's own canonical-space constants
// (CanonicalHeight, LegLength, TorsoWidth/Height, ArmWidth/Length, ShoulderX/YDrop, HeadSize) multiplied by
// SCALE=4, plus the wardrobe-slot rect math CharacterRig/RigFactory.Build() actually uses (Top/Bottom/Dress/
// HairBack sizes, the Shoe/EyeIris/HairFront band fractions). If RigFactory.cs's constants ever change,
// regenerate this by re-deriving the numbers below from the new constants, then re-run this script.
const path = require('path');
const fs = require('fs');
const sharp = require('sharp');

const SCALE = 4; // canonical Unity units -> guide pixels; chosen so CanonicalHeight*SCALE is a round 896
const outDir = path.resolve(__dirname, '../../art/character');

// --- RigFactory.cs's own canonical-space constants, x SCALE ---
const CanonicalHeight = 224 * SCALE; // 896 - total standing height, ground to top of head
const LegLength = 70 * SCALE;        // 280 - ground to hip
const TorsoWidth = 78 * SCALE, TorsoHeight = 90 * SCALE; // 312 x 360
const ArmWidth = 24 * SCALE, ArmLength = 78 * SCALE;     // 96 x 312
const ShoulderX = 33 * SCALE, ShoulderYDrop = 6 * SCALE; // 132, 24
const HeadSize = 64 * SCALE; // 256

// --- derived, in a y-up world with ground at y=0 (converted to picture y-down just before drawing) ---
const hipY = LegLength;                 // 280
const torsoTopY = hipY + TorsoHeight;    // 640
const headTopY = torsoTopY + HeadSize;   // 896 == CanonicalHeight, by construction

// Wardrobe/hair slot rects, mirroring RigFactory.Build()'s own Part()/Band() calls exactly:
const rects = {
  // Bottom: anchor+pivot torso bottom, size (TorsoWidth+16, TorsoHeight*0.55)
  bottom: { w: TorsoWidth + 16 * SCALE, h: TorsoHeight * 0.55, cy: hipY }, // cy = its own anchor y (bottom of rect)
  // Top: anchor+pivot torso centre, size (TorsoWidth+ShoulderX*1.2, TorsoHeight+10)
  top: { w: TorsoWidth + ShoulderX * 1.2, h: TorsoHeight + 10 * SCALE, cy: hipY + TorsoHeight / 2 }, // cy = centre
  // Dress: anchor+pivot torso top, size (TorsoWidth+ShoulderX*1.2, TorsoHeight+LegLength*0.6)
  dress: { w: TorsoWidth + ShoulderX * 1.2, h: TorsoHeight + LegLength * 0.6, topY: torsoTopY }, // topY = top of rect
  // HairBack: anchor+pivot torso top (=head bottom), size (HeadSize*1.25, HeadSize*1.25)
  hairBack: { w: HeadSize * 1.25, h: HeadSize * 1.25, bottomY: torsoTopY },
  // Shoe: bottom 0-0.35 of the leg's own rect (leg spans ground..hipY)
  shoe: { topY: 0, bottomY: LegLength * 0.35 },
};
const legGap = 17 * SCALE, legWidth = 34 * SCALE;
const legX = { l: -(legGap + legWidth / 2), r: legGap + legWidth / 2 }; // leg centre x, mirroring RigFactory.Limb

const W = 760, H = 1290;
const originX = W / 2, groundY = 1140; // fixed so the legend below always has clear space under it in H
const px = (x) => originX + x; // world x (centre origin) -> picture x
const py = (yUp) => groundY - yUp; // world y-up (ground=0) -> picture y-down

// Draws a box from a y-up (bottom, top) span - explicit about which edge is which, since world space is
// y-up (ground=0) but SVG/picture space is y-down; every caller below passes span edges, never a size+anchor.
function spanBox(x, yBottom, yTop, w, stroke, label, dash) {
  const bx = px(x - w / 2), by = py(yTop), h = yTop - yBottom;
  return `<rect x="${bx}" y="${by}" width="${w}" height="${h}" fill="none" stroke="${stroke}" stroke-width="3" ${dash ? `stroke-dasharray="${dash}"` : ''}/>` +
    `<text x="${bx + 6}" y="${by + 22}" font-family="sans-serif" font-size="18" fill="${stroke}">${label}</text>`;
}

function build() {
  let body = '';

  // Ground line
  body += `<line x1="0" y1="${groundY}" x2="${W}" y2="${groundY}" stroke="#999" stroke-width="2" stroke-dasharray="6,6"/>`;
  body += `<text x="8" y="${groundY - 8}" font-family="sans-serif" font-size="16" fill="#999">ground (y=0)</text>`;

  // Neutral reference silhouette (not a real character - a generic proportion guide only): legs, torso, arms, head.
  const silhouette = `
    <rect x="${px(legX.l - legWidth / 2)}" y="${py(hipY)}" width="${legWidth}" height="${hipY}" rx="${legWidth / 3}" fill="#e6e6e6" stroke="#bbb" stroke-width="2"/>
    <rect x="${px(legX.r - legWidth / 2)}" y="${py(hipY)}" width="${legWidth}" height="${hipY}" rx="${legWidth / 3}" fill="#e6e6e6" stroke="#bbb" stroke-width="2"/>
    <rect x="${px(-TorsoWidth / 2)}" y="${py(torsoTopY)}" width="${TorsoWidth}" height="${TorsoHeight}" rx="${TorsoWidth / 6}" fill="#e6e6e6" stroke="#bbb" stroke-width="2"/>
    <rect x="${px(-ShoulderX - ArmWidth / 2)}" y="${py(torsoTopY - ShoulderYDrop)}" width="${ArmWidth}" height="${ArmLength}" rx="${ArmWidth / 3}" fill="#e6e6e6" stroke="#bbb" stroke-width="2"/>
    <rect x="${px(ShoulderX - ArmWidth / 2)}" y="${py(torsoTopY - ShoulderYDrop)}" width="${ArmWidth}" height="${ArmLength}" rx="${ArmWidth / 3}" fill="#e6e6e6" stroke="#bbb" stroke-width="2"/>
    <circle cx="${px(0)}" cy="${py(headTopY - HeadSize / 2)}" r="${HeadSize / 2}" fill="#e6e6e6" stroke="#bbb" stroke-width="2"/>
  `;
  body += silhouette;

  // Head-height rulers (this figure is exactly 3.5 head-heights tall - HeadSize x 3.5 = CanonicalHeight).
  for (let i = 0; i <= 3.5; i += 0.5) {
    const y = i * HeadSize;
    body += `<line x1="${px(-TorsoWidth)}" y1="${py(y)}" x2="${px(TorsoWidth)}" y2="${py(y)}" stroke="#ccc" stroke-width="1"/>`;
    body += `<text x="${px(TorsoWidth) + 6}" y="${py(y) + 5}" font-family="sans-serif" font-size="14" fill="#aaa">${i}h</text>`;
  }

  // Wardrobe/hair slot boxes, in the same colours/labels every prompt in PROMPTS.md refers back to. Labels
  // are short on purpose (collision-prone canvas) - the footer legend below carries the full explanation.
  body += spanBox(0, rects.top.cy - rects.top.h / 2, rects.top.cy + rects.top.h / 2, rects.top.w, '#2a7de1', 'Top');
  body += spanBox(0, rects.bottom.cy, rects.bottom.cy + rects.bottom.h, rects.bottom.w, '#7a4a1e', 'Bottom');
  body += spanBox(0, rects.dress.topY - rects.dress.h, rects.dress.topY, rects.dress.w, '#c23fa0', 'Dress*', '10,6');
  body += spanBox(0, rects.hairBack.bottomY, rects.hairBack.bottomY + rects.hairBack.h, rects.hairBack.w, '#8a5a2b', 'Hair (back)');
  body += spanBox(0, headTopY - HeadSize, headTopY, HeadSize, '#3fae4a', 'Face / Glasses / Hair-front**');
  body += spanBox(legX.l, rects.shoe.topY, rects.shoe.bottomY, legWidth + 8, '#d02020', '');
  body += spanBox(legX.r, rects.shoe.topY, rects.shoe.bottomY, legWidth + 8, '#d02020', 'Shoes');

  // Eye band and hair-front-fringe band, drawn INSIDE the face box (both are fractions of face height, see
  // CharacterRig.ApplyLook / RigFactory.Band): eyes at 32%-52% up the face, fringe coverage varies 12%-60%
  // by hairstyle (three examples shown as dashed lines, not boxes, since only the top edge moves per style).
  const faceBottom = headTopY - HeadSize, faceH = HeadSize;
  body += spanBox(0, faceBottom + faceH * 0.32, faceBottom + faceH * 0.52, HeadSize * 0.7, '#1a1a1a', 'Eyes');
  for (const cov of [0.12, 0.35, 0.60]) {
    const y = faceBottom + faceH * (1 - cov);
    body += `<line x1="${px(-HeadSize / 2)}" y1="${py(y)}" x2="${px(HeadSize / 2)}" y2="${py(y)}" stroke="#8a5a2b" stroke-width="2" stroke-dasharray="4,4"/>`;
  }

  const legend = [
    '* Dress replaces Top+Bottom at once - never worn together, never a third Bottom option.',
    '** Hair-front sits inside the face box, drawn over Face and Eyes; Glasses draws in front of both.',
    '   Dashed lines inside the face box: 3 example hair-front coverages (12% / 35% / 60% of face height).',
    'All wardrobe pieces drawn full colour (not tinted); Face/Hair/limbs are neutral art, tinted at runtime',
    'by Skin/HairColor. See art/character/STYLE.md for the full convention this guide illustrates.',
  ];
  let legendSvg = '';
  legend.forEach((line, i) => {
    legendSvg += `<text x="16" y="${groundY + 35 + i * 22}" font-family="sans-serif" font-size="14" fill="#555">${line}</text>`;
  });

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}">
    <rect width="${W}" height="${H}" fill="#ffffff"/>
    <text x="16" y="28" font-family="sans-serif" font-size="19" fill="#222">M5 character reference guide - proportions only, not a style example</text>
    <text x="16" y="50" font-family="sans-serif" font-size="14" fill="#666">Figure is 3.5 head-heights tall. Boxes show where each slot's art must land.</text>
    ${body}
    ${legendSvg}
  </svg>`;
  return svg;
}

(async () => {
  fs.mkdirSync(outDir, { recursive: true });
  const file = path.join(outDir, 'reference-guide.png');
  await sharp(Buffer.from(build())).png().toFile(file);
  console.log('wrote ' + path.relative(path.resolve(__dirname, '../..'), file));
})();
