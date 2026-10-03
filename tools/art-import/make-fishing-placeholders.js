// Placeholder art for the Fishing game (underwater scene, boat, six side-view fish), drawn as SVG and written as PNGs into
// Resources/Art/fishing. Real art replaces these under the same file names (see art/eva/arcade/PROMPTS.md, Batch 10).
// Usage: node tools/art-import/make-fishing-placeholders.js
const path = require('path');
const sharp = require('sharp');

const out = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art', 'fishing');

const fishColors = [
  { name: 'a', body: '#ff8a1f', belly: '#ffd08a', fin: '#e8590c', stripe: '#ffffff' },
  { name: 'b', body: '#3b9dff', belly: '#a9d6ff', fin: '#1c6fd1', stripe: '#0b3d91' },
  { name: 'c', body: '#4fc24a', belly: '#c8f0a8', fin: '#2f8f2c', stripe: '#1f6a1c' },
  { name: 'd', body: '#ffd93b', belly: '#fff3a8', fin: '#e0a800', stripe: '#c78a00' },
  { name: 'e', body: '#ff5a5a', belly: '#ffc2b8', fin: '#c92a2a', stripe: '#ffffff' },
  { name: 'f', body: '#a56cff', belly: '#dcc4ff', fin: '#6f3fd1', stripe: '#43229e' },
];

function fishSvg(c) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="400" height="220" viewBox="0 0 400 220">
  <defs><clipPath id="b"><ellipse cx="205" cy="112" rx="150" ry="78"/></clipPath></defs>
  <path d="M70 112 L6 36 Q-6 110 6 184 Z" fill="${c.fin}" stroke="#1b2a49" stroke-width="6" stroke-linejoin="round"/>
  <path d="M150 40 Q205 -2 270 44 Z" fill="${c.fin}" stroke="#1b2a49" stroke-width="6" stroke-linejoin="round"/>
  <path d="M170 178 Q200 214 245 176 Z" fill="${c.fin}" stroke="#1b2a49" stroke-width="6" stroke-linejoin="round"/>
  <ellipse cx="205" cy="112" rx="150" ry="78" fill="${c.body}"/>
  <g clip-path="url(#b)">
    <ellipse cx="215" cy="190" rx="170" ry="70" fill="${c.belly}"/>
    <rect x="150" y="20" width="22" height="190" fill="${c.stripe}" opacity="0.55"/>
    <rect x="205" y="20" width="22" height="190" fill="${c.stripe}" opacity="0.55"/>
  </g>
  <ellipse cx="205" cy="112" rx="150" ry="78" fill="none" stroke="#1b2a49" stroke-width="6"/>
  <circle cx="300" cy="94" r="21" fill="#fff" stroke="#1b2a49" stroke-width="5"/>
  <circle cx="306" cy="95" r="10" fill="#1b2a49"/>
  <circle cx="310" cy="90" r="3.5" fill="#fff"/>
  <path d="M338 130 Q322 142 306 134" fill="none" stroke="#1b2a49" stroke-width="5" stroke-linecap="round"/>
</svg>`;
}

const boatSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="620" height="230" viewBox="0 0 620 230">
  <path d="M10 40 L610 40 Q590 190 470 205 L150 205 Q30 190 10 40 Z" fill="#8a5a2b" stroke="#2e1d0f" stroke-width="8" stroke-linejoin="round"/>
  <path d="M40 90 Q310 110 580 90" fill="none" stroke="#2e1d0f" stroke-width="7"/>
  <path d="M62 140 Q310 162 558 140" fill="none" stroke="#2e1d0f" stroke-width="7"/>
  <rect x="2" y="26" width="616" height="26" rx="12" fill="#a9733c" stroke="#2e1d0f" stroke-width="8"/>
</svg>`;

function backgroundSvg() {
  const w = 1920, h = 900, horizon = 252;
  let bubbles = '';
  let seed = 11;
  const rand = () => { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };
  for (let i = 0; i < 26; i++) {
    const r = 6 + rand() * 22;
    bubbles += `<circle cx="${rand() * w}" cy="${horizon + 60 + rand() * (h - horizon - 160)}" r="${r}" fill="#ffffff" opacity="${0.08 + rand() * 0.14}"/>`;
  }
  let weed = '';
  for (let i = 0; i < 14; i++) {
    const x = (i < 7 ? 20 + i * 60 : w - 20 - (i - 7) * 60) + rand() * 20;
    const hh = 90 + rand() * 120;
    weed += `<path d="M${x} ${h} Q${x - 30} ${h - hh * 0.5} ${x} ${h - hh} Q${x + 30} ${h - hh * 0.5} ${x + 8} ${h} Z" fill="#1f7a4d" opacity="0.8"/>`;
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
  <defs>
    <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#9fd6f5"/><stop offset="1" stop-color="#d8f1fb"/></linearGradient>
    <linearGradient id="sea" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#3fb4e8"/><stop offset="0.45" stop-color="#1a78c2"/><stop offset="1" stop-color="#0b2f6b"/></linearGradient>
  </defs>
  <rect width="${w}" height="${horizon}" fill="url(#sky)"/>
  <g fill="#fff" opacity="0.95"><ellipse cx="330" cy="70" rx="120" ry="34"/><ellipse cx="410" cy="56" rx="80" ry="30"/><ellipse cx="1500" cy="110" rx="140" ry="36"/><ellipse cx="1580" cy="92" rx="90" ry="32"/></g>
  <path d="M120 ${horizon} Q260 ${horizon - 70} 420 ${horizon} Z" fill="#6cc070" stroke="#2f6a35" stroke-width="5"/>
  <path d="M330 ${horizon} Q520 ${horizon - 50} 700 ${horizon} Z" fill="#7fd07f" stroke="#2f6a35" stroke-width="5"/>
  <rect y="${horizon}" width="${w}" height="${h - horizon}" fill="url(#sea)"/>
  <path d="M0 ${horizon} H${w}" stroke="#ffffff" stroke-width="6" opacity="0.7"/>
  ${bubbles}
  ${weed}
</svg>`;
}

(async () => {
  for (const c of fishColors) await sharp(Buffer.from(fishSvg(c))).png().toFile(path.join(out, `fish_${c.name}.png`));
  await sharp(Buffer.from(boatSvg)).png().toFile(path.join(out, 'boat.png'));
  await sharp(Buffer.from(backgroundSvg())).png().toFile(path.join(out, 'bg.png'));
  console.log('written to', out);
})();
