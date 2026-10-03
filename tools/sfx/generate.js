#!/usr/bin/env node
'use strict';

// Synthesizes Eva's sound effects into 16-bit mono WAV files (no samples, no network, no licences to track).
//   node tools/sfx/generate.js [outDir]    default: EvasLearningWorld/Assets/Eva/Resources/Sfx
// Every effect is a few layered partials / filtered noise with an envelope, so they are soft and round rather than beeps.
// Sfx.cs loads Resources/Sfx/<name>; the list of names below must stay in step with Sfx.Names.

const fs = require('fs');
const path = require('path');

const RATE = 44100;
const TAU = Math.PI * 2;

// Fixed-seed noise so a regenerate gives byte-identical files.
let seed = 12345;
const rand = () => { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296 * 2 - 1; };

const buffer = (seconds) => new Float32Array(Math.ceil(seconds * RATE));

// Adds a partial: frequency may sweep from f0 to f1 (exponential), amplitude follows attack + exponential decay.
function tone(buf, { start = 0, dur, f0, f1 = f0, amp = 0.3, attack = 0.004, decay = 6, shape = 'sine' }) {
  const first = Math.floor(start * RATE), n = Math.floor(dur * RATE);
  let phase = 0;
  for (let i = 0; i < n && first + i < buf.length; i++) {
    const t = i / RATE, k = i / n;
    const f = f0 * Math.pow(f1 / f0, k);
    phase += TAU * f / RATE;
    let s = Math.sin(phase);
    if (shape === 'soft') s = Math.sin(phase) + 0.25 * Math.sin(2 * phase) + 0.08 * Math.sin(3 * phase);
    if (shape === 'bell') s = Math.sin(phase) + 0.5 * Math.sin(2.76 * phase) * Math.exp(-t * 14) + 0.25 * Math.sin(5.4 * phase) * Math.exp(-t * 22);
    const env = Math.min(1, t / attack) * Math.exp(-t * decay) * Math.min(1, (dur - t) / 0.01);
    buf[first + i] += s * amp * env;
  }
}

// Adds low-passed noise (one-pole filter); `sweep` moves the cutoff from cut0 to cut1 Hz over the burst.
function noise(buf, { start = 0, dur, amp = 0.2, cut0 = 2000, cut1 = cut0, attack = 0.005, decay = 8 }) {
  const first = Math.floor(start * RATE), n = Math.floor(dur * RATE);
  let y = 0;
  for (let i = 0; i < n && first + i < buf.length; i++) {
    const t = i / RATE, k = i / n;
    const cut = cut0 * Math.pow(cut1 / cut0, k);
    const a = 1 - Math.exp(-TAU * cut / RATE);
    y += a * (rand() - y);
    buf[first + i] += y * amp * Math.min(1, t / attack) * Math.exp(-t * decay) * Math.min(1, (dur - t) / 0.01);
  }
}

const note = (name) => ({ C4: 261.63, D4: 293.66, E4: 329.63, F4: 349.23, G4: 392, A4: 440, B4: 493.88, C5: 523.25, D5: 587.33, E5: 659.25, G5: 783.99, A5: 880, C6: 1046.5, E6: 1318.5, G6: 1568 })[name];

// splash, flop, shiver, reelout and reelin are real recordings now (tools/sfx/import-real.js), not synthesised here.
const SOUNDS = {
  // Button / tile touch: a soft, low marimba-style note, no click or noise.
  tap: () => { const b = buffer(0.16); tone(b, { dur: 0.15, f0: 392, amp: 0.3, attack: 0.006, decay: 20 }); tone(b, { dur: 0.06, f0: 1568, amp: 0.05, attack: 0.002, decay: 60 }); return b; },
  // Finger picks something up: a quick rising bloop.
  pick: () => { const b = buffer(0.18); tone(b, { dur: 0.17, f0: 330, f1: 392, amp: 0.26, attack: 0.02, decay: 16 }); return b; },
  // Finger lets go: a soft low thud.
  drop: () => { const b = buffer(0.16); tone(b, { dur: 0.15, f0: 260, f1: 130, amp: 0.4, decay: 22 }); noise(b, { dur: 0.06, amp: 0.12, cut0: 900, decay: 45 }); return b; },
  // Something lands in its place: a wooden "tok" plus a little shine.
  place: () => { const b = buffer(0.3); tone(b, { dur: 0.1, f0: 520, f1: 420, amp: 0.35, decay: 28, shape: 'soft' }); noise(b, { dur: 0.03, amp: 0.15, cut0: 3500, decay: 70 }); tone(b, { start: 0.04, dur: 0.25, f0: note('G5'), amp: 0.12, decay: 12, shape: 'bell' }); return b; },
  // Correct answer: rising three-note bell arpeggio.
  right: () => { const b = buffer(0.62); [['C5', 0], ['E5', 0.1], ['G5', 0.2]].forEach(([n, s]) => tone(b, { start: s, dur: 0.4, f0: note(n), amp: 0.24, decay: 7, shape: 'bell' })); return b; },
  // Not quite: a gentle two-note drop, never harsh.
  retry: () => { const b = buffer(0.42); tone(b, { dur: 0.2, f0: note('G4'), f1: note('G4') * 0.97, amp: 0.26, attack: 0.01, decay: 9, shape: 'soft' }); tone(b, { start: 0.15, dur: 0.26, f0: note('E4'), f1: note('E4') * 0.94, amp: 0.26, attack: 0.01, decay: 9, shape: 'soft' }); return b; },
  // One coin: a bright ding.
  coin: () => { const b = buffer(0.32); tone(b, { dur: 0.3, f0: 1568, amp: 0.2, decay: 11, shape: 'bell' }); tone(b, { start: 0.05, dur: 0.25, f0: 2093, amp: 0.15, decay: 14 }); return b; },
  // Paying in the store: two dings and a sparkle.
  buy: () => { const b = buffer(0.7); tone(b, { dur: 0.3, f0: 1318.5, amp: 0.2, decay: 10, shape: 'bell' }); tone(b, { start: 0.11, dur: 0.5, f0: 1760, amp: 0.2, decay: 7, shape: 'bell' }); [2093, 2637, 3136].forEach((f, i) => tone(b, { start: 0.25 + i * 0.06, dur: 0.2, f0: f, amp: 0.07, decay: 16 })); return b; },
  // Game finished well: a short bright fanfare.
  win: () => { const b = buffer(1.15); [['C5', 0], ['E5', 0.13], ['G5', 0.26], ['C6', 0.42]].forEach(([n, s], i) => tone(b, { start: s, dur: i === 3 ? 0.7 : 0.3, f0: note(n), amp: 0.22, decay: i === 3 ? 4 : 8, shape: 'bell' })); [3136, 3951, 4699].forEach((f, i) => tone(b, { start: 0.55 + i * 0.07, dur: 0.3, f0: f, amp: 0.05, decay: 12 })); return b; },
  // Help appears (hint / demo): a soft two-note chime going up.
  hint: () => { const b = buffer(0.5); tone(b, { dur: 0.35, f0: note('E5'), amp: 0.2, decay: 8, shape: 'bell' }); tone(b, { start: 0.14, dur: 0.35, f0: note('A5'), amp: 0.2, decay: 8, shape: 'bell' }); return b; },
  // Next level of a real-time game: a quick rising run, short and busy so it says "faster now" (the win fanfare is slower and longer).
  levelup: () => { const b = buffer(0.62); ['C5', 'D5', 'E5', 'G5', 'C6'].forEach((n, i) => tone(b, { start: i * 0.06, dur: i === 4 ? 0.3 : 0.14, f0: note(n), amp: 0.2, decay: i === 4 ? 7 : 12, shape: 'bell' })); return b; },
  // Treasure Hunt metal detector. One bright beep as the detector nears something buried (the same over junk, so what it is stays a surprise).
  detbeep: () => { const b = buffer(0.14); tone(b, { dur: 0.13, f0: 1175, amp: 0.28, attack: 0.003, decay: 16, shape: 'soft' }); return b; },
  // Right over it: the continuous tone, a seamless loop (1200 and 2400 Hz and the 8 Hz shimmer all fit a whole number of times in 0.5 s).
  dethot: () => {
    const len = 0.5, b = buffer(len);
    for (let i = 0; i < b.length; i++) { const t = i / RATE; b[i] = 0.22 * Math.sin(TAU * 1200 * t) + 0.08 * Math.sin(TAU * 2400 * t) * (0.7 + 0.3 * Math.sin(TAU * 8 * t)); }
    return b;
  },
  // One shovel scoop: a rasp of sand over a soft thud.
  dig: () => { const b = buffer(0.32); noise(b, { dur: 0.3, amp: 0.55, cut0: 2200, cut1: 700, attack: 0.01, decay: 12 }); tone(b, { start: 0.04, dur: 0.2, f0: 150, f1: 70, amp: 0.3, decay: 18 }); return b; },
  pop: () => { const b = buffer(0.12); tone(b, { dur: 0.1, f0: 900, f1: 300, amp: 0.35, attack: 0.002, decay: 35 }); noise(b, { dur: 0.015, amp: 0.15, cut0: 6000, decay: 120 }); return b; },
};

function wav(samples) {
  let peak = 0.0001;
  for (const s of samples) peak = Math.max(peak, Math.abs(s));
  const gain = peak > 0.9 ? 0.9 / peak : 1;
  const data = Buffer.alloc(samples.length * 2);
  samples.forEach((s, i) => data.writeInt16LE(Math.round(Math.max(-1, Math.min(1, s * gain)) * 32767), i * 2));
  const head = Buffer.alloc(44);
  head.write('RIFF', 0); head.writeUInt32LE(36 + data.length, 4); head.write('WAVE', 8); head.write('fmt ', 12);
  head.writeUInt32LE(16, 16); head.writeUInt16LE(1, 20); head.writeUInt16LE(1, 22); head.writeUInt32LE(RATE, 24);
  head.writeUInt32LE(RATE * 2, 28); head.writeUInt16LE(2, 32); head.writeUInt16LE(16, 34); head.write('data', 36); head.writeUInt32LE(data.length, 40);
  return Buffer.concat([head, data]);
}

const outDir = process.argv[2] || path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Sfx');
fs.mkdirSync(outDir, { recursive: true });
for (const [name, make] of Object.entries(SOUNDS)) {
  seed = 12345;
  const file = path.join(outDir, name + '.wav');
  fs.writeFileSync(file, wav(make()));
  console.log(name, fs.statSync(file).size + ' bytes');
}
