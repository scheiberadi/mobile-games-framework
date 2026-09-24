// SPIKE (throwaway): procedurally draws Eva the black, fluffy, dwarf, bobtailed cat as stacked SVG layers.
// Every layer shares one 1000x1000 canvas so parts line up without per-part offsets; pivots are set in Unity.
// Usage: node art/eva/cat/gen.js  -> writes art/eva/cat/<layer>.svg and preview.svg (all layers stacked).
const fs = require('fs');
const path = require('path');

function rng(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const f = (n) => n.toFixed(1);

// Ellipse outline with fur tufts: valleys sit on the ellipse, tips stick out along the normal and lean with the sweep.
function tuftPath(cx, cy, rx, ry, n, lenFn, rand, lean = 0.25, wf = () => 1) {
  const pts = [];
  const ang = [];
  for (let i = 0; i <= n; i++) ang.push(((i + (i % n === 0 ? 0 : (rand() - 0.5) * 0.7)) / n) * Math.PI * 2);
  ang[n] = ang[0] + Math.PI * 2;
  for (let i = 0; i < n; i++) {
    const a0 = ang[i];
    const a1 = ang[i + 1];
    const am = a0 + (a1 - a0) * (0.35 + rand() * 0.3);
    const len = lenFn(am) * (0.6 + rand() * 0.5);
    const v0 = [cx + Math.cos(a0) * rx * wf(a0), cy + Math.sin(a0) * ry];
    const nx = Math.cos(am), ny = Math.sin(am);
    const bx = cx + Math.cos(am) * rx * wf(am), by = cy + Math.sin(am) * ry;
    const tx = -Math.sin(am), ty = Math.cos(am);
    const tip = [bx + nx * len + tx * len * lean, by + ny * len + ty * len * lean];
    const c1 = [(v0[0] + tip[0]) / 2 - tx * len * 0.12, (v0[1] + tip[1]) / 2 - ty * len * 0.12];
    pts.push({ v0, tip, c1 });
  }
  let d = `M${f(pts[0].v0[0])} ${f(pts[0].v0[1])}`;
  for (let i = 0; i < n; i++) {
    const p = pts[i];
    const nxt = pts[(i + 1) % n];
    const c2 = [(p.tip[0] + nxt.v0[0]) / 2, (p.tip[1] + nxt.v0[1]) / 2];
    d += ` Q${f(p.c1[0])} ${f(p.c1[1])} ${f(p.tip[0])} ${f(p.tip[1])} Q${f(c2[0])} ${f(c2[1])} ${f(nxt.v0[0])} ${f(nxt.v0[1])}`;
  }
  return d + 'Z';
}

// Fur strokes inside an ellipse; dirFn gives the growth angle at a point. Lit side (upper-left) gets more highlight strokes.
function strokes(shape, count, dirFn, rand, len = [14, 30], hlBoost = 1) {
  let out = '';
  let made = 0, guard = 0;
  while (made < count && guard++ < count * 6) {
    const x = shape.cx + (rand() * 2 - 1) * shape.rx;
    const y = shape.cy + (rand() * 2 - 1) * shape.ry;
    const ux = (x - shape.cx) / shape.rx, uy = (y - shape.cy) / shape.ry;
    if (ux * ux + uy * uy > 1.05) continue;
    made++;
    const a = dirFn(x, y) + (rand() - 0.5) * 0.5;
    const L = len[0] + rand() * (len[1] - len[0]);
    const bend = (rand() - 0.5) * 0.9;
    const ex = x + Math.cos(a) * L, ey = y + Math.sin(a) * L;
    const cxp = x + Math.cos(a + bend) * L * 0.55, cyp = y + Math.sin(a + bend) * L * 0.55;
    const lit = 0.5 - 0.5 * (ux * 0.6 + uy * 0.8); // 1 = lit (upper-left)
    const r = rand();
    let col, op, w;
    if (r < lit * 0.55 * hlBoost) { col = '#6b7188'; op = 0.3; w = 1.2; }
    else if (r < 0.55 + lit * 0.15) { col = '#30323e'; op = 0.4; w = 1.5; }
    else { col = '#000'; op = 0.3; w = 1.6; }
    out += `<path d="M${f(x)} ${f(y)}Q${f(cxp)} ${f(cyp)} ${f(ex)} ${f(ey)}" stroke="${col}" stroke-opacity="${op}" stroke-width="${w}" fill="none" stroke-linecap="round"/>`;
  }
  return out;
}

// A shaded, furry blob. Returns svg fragment (defs + shapes) with ids prefixed by `id`.
function furBlob(id, shape, o, rand) {
  const d = tuftPath(shape.cx, shape.cy, shape.rx, shape.ry, o.tufts, o.lenFn, rand, o.lean ?? 0.3, o.wf);
  const dir = o.dir;
  return `
<defs>
  <clipPath id="${id}c"><path d="${d}"/></clipPath>
  <linearGradient id="${id}b" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${o.top || '#25252e'}"/><stop offset="1" stop-color="${o.bot || '#09090c'}"/></linearGradient>
  <radialGradient id="${id}h" cx="${(shape.cx - shape.rx * 0.35)}" cy="${(shape.cy - shape.ry * 0.45)}" r="${shape.rx * 0.95}" gradientUnits="userSpaceOnUse"><stop offset="0" stop-color="#8b93b0" stop-opacity="${o.hl ?? 0.34}"/><stop offset="1" stop-color="#8b93b0" stop-opacity="0"/></radialGradient>
  <linearGradient id="${id}s" x1="0" y1="0" x2="0" y2="1"><stop offset="0.42" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="${o.shade ?? 0.6}"/></linearGradient>
  <linearGradient id="${id}r" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#9fb0d8" stop-opacity="0.5"/><stop offset="0.5" stop-color="#9fb0d8" stop-opacity="0"/></linearGradient>
</defs>
<path d="${d}" fill="url(#${id}b)"/>
<g clip-path="url(#${id}c)">${strokes(shape, o.strokes, dir, rand, o.strokeLen, o.hlBoost ?? 1)}</g>
<path d="${d}" fill="url(#${id}h)"/>
<path d="${d}" fill="url(#${id}s)"/>
<path d="${d}" fill="none" stroke="url(#${id}r)" stroke-width="2.5" stroke-linejoin="round"/>`;
}

const layers = {};
// Paw toes: separate rounded toe pads with dark gaps and a soft highlight, like a real cat's paw seen from the front.
function toes(cx, cy, n, span) {
  let s = '';
  const step = span * 2 / n;
  for (let i = 0; i < n; i++) {
    const x = cx - span + step * (i + 0.5);
    s += `<ellipse cx="${x}" cy="${cy - 2}" rx="${step * 0.46}" ry="13" fill="#2f3140" fill-opacity="0.7"/><path d="M${x - step * 0.25} ${cy - 9} q${step * 0.25} -5 ${step * 0.5} 0" stroke="#9aa3c0" stroke-opacity="0.45" stroke-width="2" fill="none" stroke-linecap="round"/>`;
  }
  for (let i = 1; i < n; i++) {
    const x = cx - span + step * i;
    s += `<path d="M${x} ${cy - 16} V${cy + 12}" stroke="#000" stroke-opacity="0.9" stroke-width="3.6" stroke-linecap="round"/>`;
  }
  return s;
}
const down = () => Math.PI / 2;
const radialFrom = (fx, fy) => (x, y) => Math.atan2(y - fy, x - fx);

// ---- ground shadow ----
layers.shadow = `
<defs><filter id="shb" x="-20%" y="-50%" width="140%" height="200%"><feGaussianBlur stdDeviation="14"/></filter></defs>
<ellipse cx="500" cy="948" rx="330" ry="26" fill="#000" fill-opacity="0.45" filter="url(#shb)"/>`;

// ---- tail: a normal long fluffy cat tail rising from behind the right haunch, curving up (chain of fur blobs) ----
{
  const r = rng(11);
  const P = [[690, 912], [800, 946], [890, 926], [912, 846]];
  const bez = (u) => {
    const v = 1 - u;
    return [0, 1].map((k) => v * v * v * P[0][k] + 3 * v * v * u * P[1][k] + 3 * v * u * u * P[2][k] + u * u * u * P[3][k]);
  };
  let s = '';
  const N = 16;
  for (let i = 0; i < N; i++) {
    const u = i / (N - 1);
    const [x, y] = bez(u);
    const rad = 30 - 6 * u + (u > 0.85 ? -5 * (u - 0.85) / 0.15 : 0);
    s += furBlob('tl' + i, { cx: x, cy: y, rx: rad, ry: rad },
      { tufts: 22, lenFn: () => 7, strokes: 40, strokeLen: [10, 20], dir: (px, py) => Math.atan2(py - y, px - x), lean: 0.5, shade: 0.35 }, r);
  }
  layers.tail = s;
}

// ---- body: one egg-shaped sitting mass, hind paws, short dwarf front legs, chest ruff, neck shadow ----
{
  const r = rng(23);
  let s = '';
  s += furBlob('to', { cx: 500, cy: 700, rx: 190, ry: 245 },
    { tufts: 130, lenFn: () => 13, lean: 0.55, strokes: 900, strokeLen: [16, 34], dir: (x, y) => Math.atan2(y - 520, (x - 500) * 0.55), wf: (a) => 1 + 0.3 * Math.sin(a), shade: 0.65 }, r);
  // hind legs: lower leg (hock) and a long foot on the ground sticking out beside the haunch, toes at the front
  for (const [px, mirror] of [[318, 1], [682, -1]]) {
    s += furBlob('hl' + px, { cx: px + mirror * 4, cy: 868, rx: 52, ry: 62 },
      { tufts: 40, lenFn: () => 6, strokes: 180, strokeLen: [10, 20], dir: down, shade: 0.5, top: '#2b2b34' }, r);
    s += furBlob('hp' + px, { cx: px - mirror * 4, cy: 924, rx: 70, ry: 30 },
      { tufts: 44, lenFn: () => 5, strokes: 140, strokeLen: [8, 16], dir: down, shade: 0.5, top: '#2b2b34' }, r);
    s += toes(px - mirror * 4, 934, 3, 24);
  }
  // soft crease between each haunch and the front leg
  for (const sx of [-1, 1]) s += `<path d="M${500 + sx * 122} 790 Q${500 + sx * 140} 850 ${500 + sx * 118} 905" stroke="#000" stroke-opacity="0.45" stroke-width="8" fill="none" stroke-linecap="round"/>`;
  layers.body = s;
  // short dwarf front legs, one layer each so they can move
  for (const [lx, name] of [[452, 'fa'], [548, 'fb']]) {
    s = '';
    s += furBlob(name, { cx: lx, cy: 825, rx: 42, ry: 122 },
      { tufts: 50, lenFn: () => 6, strokes: 320, strokeLen: [14, 26], dir: down, shade: 0.5, top: '#2b2b34' }, r);
    s += furBlob(name + 'p', { cx: lx, cy: 928, rx: 46, ry: 27 },
      { tufts: 30, lenFn: () => 4, strokes: 110, strokeLen: [8, 16], dir: down, shade: 0.45, top: '#2b2b34' }, r);
    s += toes(lx, 934, 4, 20);
    layers[name === 'fa' ? 'legL' : 'legR'] = s;
  }
  s = '';
  // gap between the legs
  s += `<path d="M500 760 V925" stroke="#000" stroke-opacity="0.55" stroke-width="10" stroke-linecap="round"/>`;
  // chest ruff
  s += furBlob('ch', { cx: 500, cy: 640, rx: 120, ry: 96 },
    { tufts: 80, lenFn: () => 17, strokes: 420, strokeLen: [20, 40], dir: (x, y) => Math.atan2(y - 540, (x - 500) * 0.6), hl: 0.3, lean: 0.4 }, r);
  // soft shadow the head casts on the chest
  s += `<defs><filter id="nsb" x="-30%" y="-60%" width="160%" height="220%"><feGaussianBlur stdDeviation="18"/></filter></defs>
<ellipse cx="500" cy="520" rx="170" ry="52" fill="#000" fill-opacity="0.6" filter="url(#nsb)"/>`;
  layers.chest = s;
}

// ---- ears (left drawn, right mirrored), separate layers so they can move ----
function ear(id, mirror) {
  const r = rng(mirror ? 71 : 61);
  const m = (x) => (mirror ? 1000 - x : x);
  const outer = `M${m(326)} 330 Q${m(310)} 236 ${m(338)} 156 Q${m(396)} 192 ${m(442)} 250 Q${m(400)} 302 ${m(326)} 330Z`;
  const inner = `M${m(346)} 298 Q${m(338)} 234 ${m(346)} 190 Q${m(392)} 214 ${m(426)} 254 Q${m(392)} 284 ${m(346)} 298Z`;
  let s = `<defs><clipPath id="${id}c"><path d="${outer}"/></clipPath><clipPath id="${id}i"><path d="${inner}"/></clipPath>
<linearGradient id="${id}g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#23232b"/><stop offset="1" stop-color="#0b0b0e"/></linearGradient>
<linearGradient id="${id}n" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#7d5a63"/><stop offset="1" stop-color="#2e1a20"/></linearGradient></defs>
<path d="${outer}" fill="url(#${id}g)"/>`;
  let st = '';
  for (let i = 0; i < 90; i++) {
    const t = r(), u = r();
    const x = m(326 + t * 110), y = 330 - t * 70 - u * 100 * (1 - t * 0.6);
    const ex = x + (mirror ? 4 : -4) - 2, ey = y - 18 - r() * 12;
    st += `<path d="M${f(x)} ${f(y)}L${f(ex)} ${f(ey)}" stroke="${r() < 0.4 ? '#6b7188' : '#000'}" stroke-opacity="0.5" stroke-width="1.6" stroke-linecap="round"/>`;
  }
  s += `<g clip-path="url(#${id}c)">${st}</g><path d="${outer}" fill="none" stroke="#9fb0d8" stroke-opacity="0.22" stroke-width="2"/>`;
  s += `<path d="${inner}" fill="url(#${id}n)"/><g clip-path="url(#${id}i)">`;
  for (let i = 0; i < 40; i++) {
    const t = r(), u = r();
    const x = m(346 + t * 78), y = 298 - t * 40 - u * 70 * (1 - t * 0.5);
    s += `<path d="M${f(x)} ${f(y)}l${mirror ? 3 : -3} -16" stroke="#c9b3b8" stroke-opacity="0.3" stroke-width="1.4" stroke-linecap="round"/>`;
  }
  s += `</g>`;
  return s;
}
layers.earL = ear('el', false);
layers.earR = ear('er', true);

// ---- head: wide face, short muzzle (shorthair/Persian midpoint), cheek ruff ----
{
  const r = rng(37);
  const cx = 500, cy = 388;
  const cheek = (a) => {
    const d = ((a % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
    const low = d > 0.1 * Math.PI && d < 0.4 * Math.PI || d > 0.6 * Math.PI && d < 0.9 * Math.PI;
    return low ? 20 : d > 1.2 * Math.PI && d < 1.8 * Math.PI ? 5 : 8;
  };
  let s = furBlob('hd', { cx, cy, rx: 192, ry: 146 },
    { wf: (a) => { const g = (c) => Math.exp(-Math.pow((((a % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI) - c) / 0.24, 2)); return 1 + 0.17 * (g(0.3 * Math.PI) + g(0.7 * Math.PI)) - 0.14 * g(0.5 * Math.PI); }, tufts: 150, lenFn: cheek, strokes: 900, strokeLen: [12, 24], dir: radialFrom(500, 430), lean: 0.25, hl: 0.3 }, r);
  // muzzle: a softer, slightly lighter patch of short fur
  let mz = '';
  const mr = rng(91);
  for (let i = 0; i < 160; i++) {
    const x = 500 + (mr() * 2 - 1) * 78, y = 442 + (mr() * 2 - 1) * 40;
    if (((x - 500) / 78) ** 2 + ((y - 442) / 40) ** 2 > 1) continue;
    const a = Math.atan2(y - 425, x - 500);
    mz += `<path d="M${f(x)} ${f(y)}l${f(Math.cos(a) * 9)} ${f(Math.sin(a) * 9)}" stroke="#6b7188" stroke-opacity="0.32" stroke-width="1.3" stroke-linecap="round"/>`;
  }
  s += mz;
  // whisker pads
  s += `<ellipse cx="462" cy="452" rx="40" ry="28" fill="#000" fill-opacity="0.18"/><ellipse cx="538" cy="452" rx="40" ry="28" fill="#000" fill-opacity="0.18"/>`;
  for (const [x, y] of [[440, 446], [452, 440], [466, 447], [448, 456], [462, 458], [560, 446], [548, 440], [534, 447], [552, 456], [538, 458]]) s += `<circle cx="${x}" cy="${y}" r="1.7" fill="#4a4d5c"/>`;
  // nose (small, dark rose) and mouth
  s += `<path d="M482 420 Q500 412 518 420 Q516 434 500 442 Q484 434 482 420Z" fill="#7b5560"/><path d="M486 421 Q500 416 514 421" stroke="#b58c96" stroke-opacity="0.55" stroke-width="2" fill="none"/>
<path d="M500 442 V456 M500 456 Q484 468 470 458 M500 456 Q516 468 530 458" stroke="#3c3d49" stroke-width="2.4" fill="none" stroke-linecap="round"/>`;
  // brow whiskers and long whiskers
  const wr = rng(5);
  for (const sx of [-1, 1]) {
    for (let i = 0; i < 3; i++) {
      const x0 = 500 + sx * (100 + i * 12), y0 = 332 - i * 4;
      s += `<path d="M${x0} ${y0} q${sx * 18} -22 ${sx * 34} -20" stroke="#e8ecf5" stroke-opacity="0.55" stroke-width="1.6" fill="none" stroke-linecap="round"/>`;
    }
    for (let i = 0; i < 5; i++) {
      const y0 = 440 + i * 9, ang = -0.28 + i * 0.16;
      const x0 = 500 + sx * 56, ex = x0 + sx * (150 + wr() * 30), ey = y0 + Math.sin(ang) * 120;
      s += `<path d="M${x0} ${y0} Q${x0 + sx * 80} ${y0 + Math.sin(ang) * 30 - 16} ${f(ex)} ${f(ey)}" stroke="#eef1fa" stroke-opacity="0.7" stroke-width="1.5" fill="none" stroke-linecap="round"/>`;
    }
  }
  layers.head = s;
}

// ---- eyes: bright amber, slit pupil, big highlight; one layer so it can blink ----
{
  const eye = (id, cx, cy, tilt) => `
<defs>
  <clipPath id="${id}c"><path d="M${cx - 46} ${cy} Q${cx} ${cy - 60} ${cx + 46} ${cy} Q${cx} ${cy + 54} ${cx - 46} ${cy}Z"/></clipPath>
  <radialGradient id="${id}i" cx="0.5" cy="0.45" r="0.6"><stop offset="0" stop-color="#eef58c"/><stop offset="0.55" stop-color="#b9d23c"/><stop offset="0.9" stop-color="#6f9020"/><stop offset="1" stop-color="#2c4409"/></radialGradient>
</defs>
<g transform="rotate(${tilt} ${cx} ${cy})">
  <g clip-path="url(#${id}c)">
    <circle cx="${cx}" cy="${cy}" r="50" fill="url(#${id}i)"/>
    <circle cx="${cx}" cy="${cy}" r="49" fill="none" stroke="#26370a" stroke-opacity="0.55" stroke-width="3"/>
    <ellipse cx="${cx}" cy="${cy}" rx="19" ry="25" fill="#050506"/>
    <ellipse cx="${cx - 15}" cy="${cy - 13}" rx="9" ry="7" fill="#fff" fill-opacity="0.95"/>
    <circle cx="${cx + 13}" cy="${cy + 11}" r="3.5" fill="#fff" fill-opacity="0.7"/>
    <path d="M${cx - 46} ${cy - 2} Q${cx} ${cy - 56} ${cx + 46} ${cy - 2} L${cx + 46} ${cy - 30} L${cx - 46} ${cy - 30}Z" fill="#000" fill-opacity="0.12"/>
  </g>
  <path d="M${cx - 47} ${cy} Q${cx} ${cy - 60} ${cx + 47} ${cy}" fill="none" stroke="#020203" stroke-width="7" stroke-linecap="round"/>
  <path d="M${cx - 44} ${cy + 3} Q${cx} ${cy + 54} ${cx + 44} ${cy + 3}" fill="none" stroke="#020203" stroke-width="4" stroke-linecap="round"/>
</g>`;
  layers.eyes = eye('eL', 420, 380, 0) + eye('eR', 580, 380, 0);
}

// ---- open mouth overlay (a cat's meow: small dark oval, tongue, tiny fangs) ----
layers.mouth = `
<defs><radialGradient id="mg" cx="0.5" cy="0.35" r="0.7"><stop offset="0" stop-color="#5a1420"/><stop offset="1" stop-color="#22080d"/></radialGradient></defs>
<path d="M472 452 Q500 448 528 452 Q532 490 500 504 Q468 490 472 452Z" fill="url(#mg)" stroke="#0a0304" stroke-width="3"/>
<path d="M484 484 Q500 470 516 484 Q512 500 500 503 Q488 500 484 484Z" fill="#c9506a"/>
<path d="M480 454 l6 14 l5 -15Z M520 454 l-6 14 l-5 -15Z" fill="#f4efe6"/>
<path d="M500 442 V452" stroke="#3c3d49" stroke-width="2.4" stroke-linecap="round"/>`;

const order = ['shadow', 'tail', 'body', 'legL', 'legR', 'chest', 'earL', 'earR', 'head', 'eyes', 'mouth'];
const dir = __dirname; fs.mkdirSync(path.join(dir,'preview'),{recursive:true});
for (const k of order) {
  fs.writeFileSync(path.join(dir, `cat_${k}.svg`), `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 1000">${layers[k]}</svg>`);
}
// preview: all layers stacked on a light and a dark background side by side
const stack = order.filter((k) => k !== 'mouth').map((k) => `<g>${layers[k]}</g>`).join('');
fs.writeFileSync(path.join(dir, 'preview', 'preview.svg'),
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 2000 1000"><rect width="1000" height="1000" fill="#bfe6ff"/><rect x="1000" width="1000" height="1000" fill="#3b4a66"/><g>${stack}</g><g transform="translate(1000 0)">${stack.replace(/id="/g, 'id="z').replace(/url\(#/g, 'url(#z').replace(/clip-path="url\(#z/g, 'clip-path="url(#z')}</g></svg>`);
console.log('wrote', order.length, 'layers');
