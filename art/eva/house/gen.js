// Generates the doll-house art: eight 1440x900 room panels, the shell overlay and the outside backdrop.
// Usage: node art/eva/house/gen.js   (writes SVGs next to this file; render with tools/build-eva-art.sh)
const fs = require('fs');
const path = require('path');
const OUT = __dirname;
const K = '#5b3a29';
const st = (w = 6) => `stroke="${K}" stroke-width="${w}" stroke-linejoin="round"`;
const rect = (x, y, w, h, fill, rx = 0, w6 = 6) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${rx}" fill="${fill}" ${st(w6)}/>`;
const circ = (x, y, r, fill) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}" ${st()}/>`;
const tri = (a, b, c, fill) => `<polygon points="${a} ${b} ${c}" fill="${fill}" ${st(5)}/>`;
const line = (x1, y1, x2, y2, w = 5) => `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${K}" stroke-width="${w}" stroke-linecap="round"/>`;
const GLASS = '#cdeeff';

// Wall and floor colours: keep in sync with HouseScreen.RoomColors.
const ROOMS = {
  living:  { wall: '#ffe8c2', floor: '#d9a066' },
  dining:  { wall: '#ffd9d0', floor: '#c98d5a' },
  kitchen: { wall: '#dff3ff', floor: '#cfd8dc' },
  parents: { wall: '#e8dcff', floor: '#b98d6a' },
  kids:    { wall: '#d6f5d6', floor: '#d9a066' },
  bath:    { wall: '#cdeeff', floor: '#b8d8e8' },
  party:   { wall: '#f3d9ff', floor: '#e3b478' },
  play:    { wall: '#fff3b0', floor: '#a5d8a5' },
};

const window_ = (x, y, w, h) => rect(x, y, w, h, GLASS, 10) + line(x + w / 2, y, x + w / 2, y + h) + line(x, y + h / 2, x + w, y + h / 2);
const curtains = (x, y, w, h, c) => rect(x - 30, y - 10, 40 + 30, h + 20, c, 12) + rect(x + w - 40, y - 10, 70, h + 20, c, 12);
const frame = (x, y, w, h, c) => rect(x, y, w, h, '#fff', 8) + rect(x + 14, y + 14, w - 28, h - 28, c, 4, 0);
const bunting = (y, colors) => {
  let s = `<path d="M260 ${y} Q720 ${y + 90} 1180 ${y}" fill="none" ${st(5)}/>`;
  for (let i = 0; i < 12; i++) {
    const x = 300 + i * 74, yy = y + 22 + Math.sin((i / 11) * Math.PI) * 60;
    s += tri(`${x},${yy}`, `${x + 50},${yy}`, `${x + 25},${yy + 55}`, colors[i % colors.length]);
  }
  return s;
};
const balloon = (x, y, c) => `<line x1="${x}" y1="${y + 62}" x2="${x + 8}" y2="${y + 170}" stroke="${K}" stroke-width="4"/>` + `<ellipse cx="${x}" cy="${y}" rx="46" ry="58" fill="${c}" ${st(5)}/>`;
const blocks = (x, y) => rect(x, y, 60, 60, '#ff6b6b', 6) + rect(x + 64, y, 60, 60, '#4dabf7', 6) + rect(x + 30, y - 62, 60, 60, '#ffe066', 6);

const DECOR = {
  living: () => window_(300, 170, 200, 240) + curtains(300, 170, 200, 240, '#ff9d7a')
    + rect(900, 330, 220, 330, '#c9a27a', 10) + rect(940, 470, 140, 190, '#3a2a22', 8) + `<path d="M985 655 Q1010 560 1035 655 Z" fill="#ff8a3d" ${st(4)}/>`
    + frame(680, 200, 130, 100, '#9bd4a8') + frame(830, 190, 100, 130, '#f7b4a2'),
  dining: () => line(720, 0, 720, 150) + `<path d="M620 150 H820 L790 210 H650 Z" fill="#ffe066" ${st()}/>` + circ(650, 225, 12, '#fff3b0') + circ(790, 225, 12, '#fff3b0')
    + frame(340, 260, 150, 190, '#f7b4a2') + frame(960, 260, 150, 190, '#9bd4a8') + window_(560, 300, 320, 200) + rect(540, 500, 360, 26, '#c9945a', 8),
  kitchen: () => rect(300, 250, 200, 160, '#ffffff', 10) + rect(520, 250, 200, 160, '#ffffff', 10) + circ(470, 330, 8, K) + circ(550, 330, 8, K)
    + rect(300, 470, 460, 190, '#f5f5f5', 10) + rect(290, 450, 480, 30, '#8fa3ad', 8) + rect(340, 430, 130, 22, '#3a3a3a', 6)
    + window_(880, 250, 220, 220) + line(830, 60, 830, 150) + circ(830, 190, 40, '#b0b8bf') + line(920, 60, 920, 120) + circ(920, 150, 28, '#b0b8bf'),
  parents: () => window_(870, 200, 220, 260) + curtains(870, 200, 220, 260, '#b197fc')
    + frame(470, 200, 260, 150, '#9bd4a8') + rect(330, 540, 110, 120, '#d0a97a', 8) + circ(385, 500, 34, '#ffe066'),
  kids: () => bunting(70, ['#ff6b6b', '#ffe066', '#4dabf7', '#69db7c']) + window_(860, 230, 210, 230) + curtains(860, 230, 210, 230, '#ffa8c8')
    + rect(330, 300, 300, 24, '#c9945a', 8) + blocks(360, 240) + rect(330, 430, 300, 24, '#c9945a', 8) + circ(440, 405, 24, '#ff6b6b') + circ(520, 405, 24, '#4dabf7'),
  bath: () => `<g opacity="0.55">${[0, 1, 2, 3, 4, 5, 6, 7].map(i => line(240 + i * 120, 330, 240 + i * 120, 660, 3)).join('')}${[0, 1, 2, 3].map(i => line(240, 330 + i * 110, 1200, 330 + i * 110, 3)).join('')}</g>`
    + `<ellipse cx="640" cy="220" rx="95" ry="120" fill="#eaf7ff" ${st(8)}/>` + line(900, 300, 1120, 300, 8) + rect(920, 300, 90, 160, '#ff8fab', 10)
    + window_(330, 130, 180, 170),
  party: () => bunting(60, ['#ff6b6b', '#ffe066', '#4dabf7', '#69db7c', '#b197fc']) + balloon(330, 300, '#ff6b6b') + balloon(400, 340, '#4dabf7') + balloon(1080, 300, '#ffe066') + balloon(1010, 340, '#b197fc')
    + circ(720, 190, 60, '#e0e0ff') + line(720, 0, 720, 130),
  play: () => rect(300, 690, 840, 130, '#ff8fab', 40) + rect(360, 720, 720, 70, '#ffe066', 30) + rect(420, 740, 600, 30, '#4dabf7', 14)
    + rect(320, 300, 320, 24, '#c9945a', 8) + blocks(360, 240) + circ(560, 275, 28, '#69db7c')
    + tri('900,240', '960,340', '840,340', '#ffe066') + tri('1040,300', '1090,380', '990,380', '#ff8fab'),
};

function room(id) {
  const c = ROOMS[id];
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1440 900">
  <rect width="1440" height="660" fill="${c.wall}"/>
  <rect y="660" width="1440" height="240" fill="${c.floor}"/>
  <rect y="646" width="1440" height="26" fill="#ffffff" opacity="0.55"/>
  <line x1="0" y1="660" x2="1440" y2="660" stroke="${K}" stroke-width="6"/>
  ${DECOR[id]()}
</svg>
`;
}

// The shell overlay. Drawn in world units with svg y = -world y; viewBox x -2400..2400, svg y -1800..1400
// (world y -1400..1800). Rooms occupy world x -2160..2160, y -1350..1350 (svg y -1350..1350); the attic's
// right bay (x 720..2160, svg y -1350..-450) is a roof terrace with a railing and no room.
function shell() {
  const slab = (y) => rect(-2210, y - 25, 4420, 50, '#a9783f', 6, 8);
  const wall = (x, y1, y2) => rect(x - 15, y1, 30, y2 - y1, '#a9783f', 4, 6);
  let s = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="-2400 -1800 4800 3200">`;
  s += rect(-2210, -1350 - 25, 4420, 50, '#a9783f', 6, 8); // attic ceiling line (top of attic rooms)
  s += slab(-450) + slab(450) + slab(1350);
  s += wall(-2160, -1350, 1350) + wall(2160, -450, 1350) + wall(-720, -450, 1350) + wall(720, -450, 1350);
  s += wall(-720, -1350, -450) + wall(720, -1350, -450);
  // roof over the two attic rooms (svg y is up = negative)
  s += `<polygon points="-2240,-1375 -720,-1800 800,-1375" fill="#d95d4a" ${st(10)}/>`;
  s += rect(-1500, -1740, 100, 140, '#8d5b3a', 6, 8); // chimney
  // roof terrace railing (attic right bay, svg y -1350..-450)
  s += rect(720, -1350, 1440, 50, '#a9783f', 6, 8);
  for (let x = 780; x <= 2100; x += 110) s += line(x, -1350, x, -1100, 10);
  s += line(720, -1100, 2160, -1100, 14);
  // stairs: two flights of steps in the slabs between levels
  for (const y0 of [-450, 450]) for (let i = 0; i < 6; i++) s += rect(-260 + i * 90, y0 - 25 - i * 22, 90, 50 + i * 22, '#c9945a', 4, 5);
  // balcony off the upper floor, right side (world y -450..450 -> floor at svg y ~ 210)
  s += rect(2160, 210, 220, 40, '#a9783f', 6, 8);
  for (let x = 2190; x <= 2340; x += 90) s += line(x, 60, x, 210, 10);
  s += line(2160, 60, 2380, 60, 14);
  // front door with two steps, ground floor left
  s += rect(-2330, 810, 170, 300, '#b5673a', 10) + circ(-2200, 970, 12, '#ffe066');
  s += rect(-2390, 1110, 230, 40, '#a9783f', 6, 8) + rect(-2330, 1150, 170, 40, '#a9783f', 6, 8);
  return s + '</svg>\n';
}

function outside() {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 900">
  <defs><linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#9ad6ff"/><stop offset="1" stop-color="#e3f5ff"/></linearGradient></defs>
  <rect width="1920" height="900" fill="url(#sky)"/>
  <circle cx="1700" cy="130" r="80" fill="#ffe066"/>
  <ellipse cx="380" cy="150" rx="150" ry="50" fill="#fff"/><ellipse cx="500" cy="120" rx="110" ry="45" fill="#fff"/>
  <ellipse cx="1300" cy="230" rx="140" ry="42" fill="#fff"/>
  <path d="M0 700 Q480 620 960 690 T1920 680 V900 H0 Z" fill="#8fd16a"/>
  <path d="M0 780 Q600 720 1200 790 T1920 770 V900 H0 Z" fill="#6fbf55"/>
</svg>
`;
}

fs.writeFileSync(path.join(OUT, 'outside.svg'), outside());
console.log('outside backdrop written');
