#!/usr/bin/env node
'use strict';

// Synthesizes Eva's background music and ambience into seamless 16-bit mono WAV loops (no samples, no network, no licences to track).
//   node tools/music/generate.js [outDir]    default: EvasLearningWorld/Assets/Eva/Resources/Music
// Every track is a set of enveloped notes laid on a fixed grid; whatever rings past the end of the loop is folded back onto its start,
// and every drone fits a whole number of cycles in the loop, so the loop point cannot be heard. MusicTracks.cs maps screens to these names.

const fs = require('fs');
const path = require('path');

const RATE = 22050;
const TAU = Math.PI * 2;

let seed = 4242;
const rand = () => { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296 * 2 - 1; };
const rnd01 = () => (rand() + 1) / 2;

const midi = (n) => 440 * Math.pow(2, (n - 69) / 12);

// Renders `fn` into a buffer a little longer than the loop, then folds the overhang back onto the start.
function loop(seconds, fn) {
  const len = Math.round(seconds * RATE), tail = 3 * RATE;
  const b = new Float32Array(len + tail);
  fn(b);
  const out = new Float32Array(len);
  for (let i = 0; i < b.length; i++) out[i % len] += b[i];
  return out;
}

// One note. shape: 'marimba' (wooden, fast decay), 'pad' (slow, soft), 'bass' (round, plucked), 'chip' (soft square, arcade), 'bell'.
function note(buf, start, dur, f, amp, shape = 'marimba') {
  const first = Math.floor(start * RATE), n = Math.floor((dur + 0.05) * RATE);
  const p = {
    marimba: { attack: 0.004, decay: 9 },
    pad: { attack: 0.35, decay: 1.2 },
    bass: { attack: 0.01, decay: 5 },
    chip: { attack: 0.006, decay: 7 },
    bell: { attack: 0.003, decay: 6 },
  }[shape];
  let phase = 0;
  for (let i = 0; i < n && first + i < buf.length; i++) {
    const t = i / RATE;
    phase += TAU * f / RATE;
    let s;
    if (shape === 'marimba') s = Math.sin(phase) + 0.35 * Math.sin(4 * phase) * Math.exp(-t * 30);
    else if (shape === 'pad') s = Math.sin(phase) + 0.3 * Math.sin(2 * phase) + 0.15 * Math.sin(3.01 * phase);
    else if (shape === 'bass') s = Math.sin(phase) + 0.4 * Math.sin(2 * phase);
    else if (shape === 'chip') s = Math.sin(phase) + 0.33 * Math.sin(3 * phase) + 0.2 * Math.sin(5 * phase);
    else s = Math.sin(phase) + 0.5 * Math.sin(2.76 * phase) * Math.exp(-t * 14) + 0.25 * Math.sin(5.4 * phase) * Math.exp(-t * 22);
    const env = Math.min(1, t / p.attack) * Math.exp(-t * p.decay) * Math.min(1, (dur + 0.05 - t) / 0.05);
    buf[first + i] += s * amp * env;
  }
}

// Soft filtered noise burst (cutoff in Hz); `high` makes it a hiss instead of a rumble.
function puff(buf, start, dur, amp, cut, { high = false, attack = 0.01, decay = 4 } = {}) {
  const first = Math.floor(start * RATE), n = Math.floor(dur * RATE);
  const a = 1 - Math.exp(-TAU * cut / RATE);
  let y = 0;
  for (let i = 0; i < n && first + i < buf.length; i++) {
    const t = i / RATE;
    y += a * (rand() - y);
    const s = high ? rand() - y : y;
    buf[first + i] += s * amp * Math.min(1, t / attack) * Math.exp(-t * decay) * Math.min(1, (dur - t) / 0.03);
  }
}

// A drone: whole cycles per loop, so it joins itself.
function drone(buf, seconds, f, amp, wobbleHz) {
  const cycles = Math.round(f * seconds), ff = cycles / seconds, wob = Math.round(wobbleHz * seconds) / seconds;
  const len = Math.round(seconds * RATE);
  for (let i = 0; i < len; i++) {
    const t = i / RATE;
    buf[i] += amp * Math.sin(TAU * ff * t) * (0.75 + 0.25 * Math.sin(TAU * wob * t));
  }
}

// A short frequency glide with a soft ending: a bubble, a whistle, a bounce, a creak.
function sweep(buf, start, dur, f0, f1, amp) {
  const first = Math.floor(start * RATE), n = Math.floor(dur * RATE);
  let phase = 0;
  for (let i = 0; i < n && first + i < buf.length; i++) {
    const t = i / RATE, k = i / n;
    phase += TAU * f0 * Math.pow(f1 / f0, k) / RATE;
    buf[first + i] += amp * Math.sin(phase) * Math.min(1, t / 0.004) * Math.exp(-t * 6 / dur) * Math.min(1, (dur - t) / 0.02);
  }
}

// A few quick chirps, like a bird.
function birdSong(buf, start, count, f, amp) {
  for (let j = 0; j < count; j++) sweep(buf, start + j * 0.12, 0.09, f * (j % 2 ? 1.15 : 0.95), f * (j % 2 ? 0.9 : 1.3), amp);
}

// Chords as [bass root, three tones] in MIDI numbers.
const CM = [48, 60, 64, 67], GM = [43, 59, 62, 67], AM = [45, 57, 60, 64], FM = [41, 57, 60, 65], DM = [50, 62, 66, 69], AM2 = [45, 61, 64, 69];
const EM = [40, 56, 59, 64], BM = [47, 59, 63, 66], BbM = [46, 58, 62, 65];

// Lays a song on a bar grid: a pad on every chord, a bass pattern ([beat, semitones above the root]) and a tune per bar ([beat, note]).
function arrange(b, o) {
  const beats = o.beats || 4, beat = 60 / o.bpm, bar = beat * beats;
  const bassPat = o.bassPat || [[0, 0], [2, 7]];
  o.chords.forEach((chord, k) => {
    const [root, ...tones] = chord, t0 = k * bar;
    tones.slice(0, 3).forEach((m) => note(b, t0, bar * 1.05, midi(m), o.pad === undefined ? 0.07 : o.pad, 'pad'));
    bassPat.forEach(([p, off]) => note(b, t0 + p * beat, beat * 1.4, midi(root + off), o.bass === undefined ? 0.16 : o.bass, 'bass'));
    if (o.tune) o.tune[k].forEach(([pos, m]) => note(b, t0 + pos * beat, beat * (o.tuneLen || 1.2), midi(m), o.tuneAmp || 0.14, o.tuneShape || 'marimba'));
  });
}

const TRACKS = {
  // The map: slow, warm and sunny. Soft pad chords, a round bass and a wooden melody on the pentatonic scale. 96 bpm, 8 bars, 20 s.
  map: () => loop(20, (b) => {
    const beat = 60 / 96, bar = beat * 4;
    const chords = [[48, 60, 64, 67], [45, 57, 60, 64], [41, 53, 57, 60], [43, 55, 59, 62]];
    const melody = [
      [[0, 76], [1, 79], [1.5, 76], [2, 74], [3, 72]],
      [[0, 72], [1, 76], [2, 81], [2.5, 79], [3, 76]],
      [[0, 81], [1, 79], [1.5, 81], [2, 84], [3, 81]],
      [[0, 79], [1, 74], [1.5, 76], [2, 79], [3, 74]],
      [[0, 84], [1, 79], [2, 76], [3, 79], [3.5, 76]],
      [[0, 81], [1, 76], [2, 72], [3, 76]],
      [[0, 81], [1, 84], [2, 81], [3, 79]],
      [[0, 79], [1, 76], [2, 74], [3, 72]],
    ];
    for (let k = 0; k < 8; k++) {
      const [root, ...tones] = chords[k % 4], t0 = k * bar;
      tones.forEach((m) => note(b, t0, bar * 1.05, midi(m), 0.07, 'pad'));
      note(b, t0, beat * 2, midi(root), 0.2, 'bass');
      note(b, t0 + beat * 2, beat * 2, midi(root + 7), 0.14, 'bass');
      melody[k].forEach(([pos, m]) => note(b, t0 + pos * beat, beat, midi(m), 0.16, 'marimba'));
    }
  }),

  // The arcade hall: a low machine hum, a soft chip arpeggio with an echo, the odd coin ding and laser blip, a murmur of crowd. 19.2 s.
  arcade: () => loop(19.2, (b) => {
    const step = 0.12; // a 16th at 125 bpm; 160 of them make the loop
    drone(b, 19.2, 55, 0.05, 0.25);
    drone(b, 19.2, 110, 0.025, 0.5);
    const chords = [[57, 60, 64, 69], [53, 57, 60, 65], [48, 52, 55, 60], [55, 59, 62, 67]];
    const order = [0, 1, 2, 3, 2, 1, 3, 1];
    for (let i = 0; i < 160; i++) {
      const chord = chords[Math.floor(i / 40)], m = chord[order[i % 8] % 4];
      note(b, i * step, step * 1.6, midi(m + 12), 0.05, 'chip');
      note(b, i * step + step * 3, step * 1.6, midi(m + 12), 0.018, 'chip'); // the echo
    }
    for (let k = 0; k < 4; k++) note(b, k * 4.8, 1.2, midi(chords[k][0] - 24), 0.14, 'bass');
    // coin dings, laser blips and bleeps at fixed odd moments
    [1.9, 5.5, 8.3, 12.9, 15.7, 17.9].forEach((t, i) => note(b, t, 0.4, i % 2 ? 2093 : 1568, 0.05, 'bell'));
    [3.1, 7.2, 10.6, 14.3, 18.4].forEach((t) => {
      const first = Math.floor(t * RATE), n = Math.floor(0.18 * RATE);
      let phase = 0;
      for (let i = 0; i < n; i++) {
        phase += TAU * (1800 * Math.pow(300 / 1800, i / n)) / RATE;
        b[first + i] += 0.035 * Math.sin(phase) * Math.exp(-i / RATE * 14);
      }
    });
    [0.4, 2.6, 4.4, 6.3, 9.5, 11.7, 13.5, 16.4].forEach((t, i) => puff(b, t, 1.4, 0.05, 500 + 200 * (i % 3), { attack: 0.5, decay: 1.5 }));
  }),

  // Bunny Run: bouncy and light. Hopping bass, off-beat chord stabs, a happy pentatonic tune, a shaker. 120 bpm, 8 bars, 16 s.
  bunnyrun: () => loop(16, (b) => {
    const beat = 0.5, bar = 2;
    const chords = [[48, 55, 60, 64], [43, 55, 59, 62], [45, 57, 60, 64], [41, 53, 57, 60], [48, 55, 60, 64], [43, 55, 59, 62], [41, 53, 57, 60], [43, 55, 59, 62]];
    const tunes = [
      [[0, 79], [0.5, 81], [1, 84], [2, 81], [2.5, 79], [3, 76]],
      [[0, 74], [0.5, 76], [1, 79], [2, 76], [3, 74], [3.5, 71]],
      [[0, 81], [0.5, 84], [1, 88], [2, 84], [2.5, 81], [3, 76]],
      [[0, 77], [1, 81], [1.5, 84], [2, 81], [3, 77]],
      [[0, 84], [0.5, 81], [1, 79], [2, 76], [2.5, 79], [3, 84]],
      [[0, 83], [0.5, 79], [1, 74], [2, 79], [3, 83]],
      [[0, 81], [0.5, 84], [1, 81], [2, 77], [2.5, 81], [3, 84]],
      [[0, 79], [1, 76], [1.5, 74], [2, 72], [3, 67]],
    ];
    for (let k = 0; k < 8; k++) {
      const [root, ...tones] = chords[k], t0 = k * bar;
      note(b, t0, beat, midi(root - 12), 0.22, 'bass');
      note(b, t0 + beat, beat, midi(root - 5), 0.16, 'bass');
      note(b, t0 + beat * 2, beat, midi(root - 12), 0.22, 'bass');
      note(b, t0 + beat * 3, beat, midi(root - 5), 0.16, 'bass');
      for (let h = 0; h < 4; h++) tones.slice(1).forEach((m) => note(b, t0 + (h + 0.5) * beat, beat * 0.4, midi(m), 0.035, 'marimba'));
      tunes[k].forEach(([pos, m]) => note(b, t0 + pos * beat, beat * 1.2, midi(m), 0.15, 'marimba'));
      for (let e = 0; e < 8; e++) puff(b, t0 + e * beat / 2, 0.07, e % 2 ? 0.05 : 0.08, 5000, { high: true, attack: 0.002, decay: 40 });
    }
  }),

  // Whack-a-Mole: playful and springy. Short wooden plucks that pop like the moles, a bouncy bass, a wood-block tick. G major, 132 bpm, 8 bars.
  whack: () => loop(8 * 4 * 60 / 132, (b) => {
    const beat = 60 / 132, bar = beat * 4;
    const chords = [[43, 59, 62, 67], [40, 59, 64, 67], [48, 60, 64, 67], [50, 62, 66, 69], [43, 59, 62, 67], [40, 59, 64, 67], [48, 60, 64, 67], [50, 62, 66, 69]];
    const pops = [
      [[0, 79], [0.5, 83], [1.5, 79], [2, 86], [3, 83], [3.5, 79]],
      [[0, 76], [1, 83], [1.5, 86], [2.5, 83], [3, 79]],
      [[0, 84], [0.5, 79], [1, 76], [2, 84], [3, 88], [3.5, 84]],
      [[0, 81], [1, 86], [1.5, 81], [2, 78], [3, 81], [3.5, 86]],
      [[0, 83], [0.5, 86], [1, 91], [2, 86], [3, 83]],
      [[0, 88], [0.5, 83], [1.5, 79], [2, 76], [3, 79]],
      [[0, 84], [1, 88], [1.5, 84], [2, 79], [3, 76], [3.5, 79]],
      [[0, 81], [0.5, 86], [1, 90], [2, 86], [3, 81]],
    ];
    for (let k = 0; k < 8; k++) {
      const [root, ...tones] = chords[k], t0 = k * bar;
      [0, 1.5, 2, 3.5].forEach((p, i) => note(b, t0 + p * beat, beat * 0.5, midi(i % 2 ? root + 7 : root), 0.2, 'bass'));
      tones.slice(1).forEach((m) => note(b, t0 + 2 * beat, beat * 0.4, midi(m), 0.04, 'marimba'));
      pops[k].forEach(([pos, m]) => note(b, t0 + pos * beat, beat * 0.3, midi(m), 0.17, 'marimba'));
      for (let e = 0; e < 4; e++) puff(b, t0 + (e + 0.5) * beat, 0.04, 0.07, 2500, { high: true, attack: 0.001, decay: 70 });
    }
  }),

  // Balloon Popping: floating and light, in three. A slow bell arpeggio drifting up over a soft pad. F major, 90 bpm, 8 bars of 3 beats.
  balloon: () => loop(8 * 3 * 60 / 90, (b) => {
    const beat = 60 / 90, bar = beat * 3;
    const chords = [[41, 57, 60, 65, 69], [38, 57, 62, 65, 69], [46, 58, 62, 65, 70], [48, 55, 60, 64, 67], [41, 57, 60, 65, 69], [38, 57, 62, 65, 69], [46, 58, 62, 65, 70], [48, 55, 60, 64, 67]];
    const lift = [0, 1, 2, 3, 4, 3];
    for (let k = 0; k < 8; k++) {
      const [root, ...tones] = chords[k], t0 = k * bar;
      tones.slice(0, 3).forEach((m) => note(b, t0, bar * 1.05, midi(m), 0.07, 'pad'));
      note(b, t0, beat * 2.5, midi(root), 0.16, 'bass');
      for (let i = 0; i < 6; i++) note(b, t0 + i * beat / 2, beat * 1.2, midi(tones[lift[i] % 4] + 12 + (k % 2 ? 12 : 0)), 0.07, 'bell');
      note(b, t0 + beat * 2, beat, midi(tones[3] + 12), 0.1, 'marimba');
    }
  }),

  // Fruit Catcher: brisk and cheerful, like a ukulele on a sunny market day. Plucked chords, a hopping bass, a light shaker. A major, 144 bpm, 8 bars.
  fruitcatcher: () => loop(8 * 4 * 60 / 144, (b) => {
    const beat = 60 / 144, bar = beat * 4;
    const chords = [[45, 57, 61, 64, 69], [42, 57, 61, 66, 69], [50, 57, 62, 66, 69], [52, 56, 59, 64, 68], [45, 57, 61, 64, 69], [42, 57, 61, 66, 69], [50, 57, 62, 66, 69], [52, 56, 59, 64, 68]];
    const tunes = [
      [[0, 81], [1, 85], [1.5, 88], [2, 85], [3, 81]],
      [[0, 78], [1, 81], [1.5, 85], [2, 90], [3, 85]],
      [[0, 86], [1, 90], [2, 86], [2.5, 85], [3, 81]],
      [[0, 83], [1, 88], [1.5, 83], [2, 80], [3, 83], [3.5, 88]],
      [[0, 88], [0.5, 85], [1, 81], [2, 85], [3, 88]],
      [[0, 90], [1, 85], [1.5, 81], [2, 78], [3, 81]],
      [[0, 86], [0.5, 90], [1, 93], [2, 90], [3, 86]],
      [[0, 88], [1, 85], [2, 83], [3, 81]],
    ];
    for (let k = 0; k < 8; k++) {
      const [root, ...tones] = chords[k], t0 = k * bar;
      [0, 1, 2, 3].forEach((p) => note(b, t0 + p * beat, beat * 0.8, midi(p % 2 ? root + 7 : root), 0.2, 'bass'));
      [0.5, 1.5, 2.5, 3.5].forEach((p) => tones.slice(1).forEach((m, j) => note(b, t0 + p * beat + j * 0.012, beat * 0.4, midi(m), 0.035, 'marimba')));
      tunes[k].forEach(([pos, m]) => note(b, t0 + pos * beat, beat * 1.1, midi(m), 0.14, 'marimba'));
      for (let e = 0; e < 8; e++) puff(b, t0 + e * beat / 2, 0.06, e % 2 ? 0.04 : 0.07, 5500, { high: true, attack: 0.002, decay: 45 });
    }
  }),

  // Fishing: calm and watery. Slow pad chords, a few sparse notes like drops, waves that swell and fade, now and then a bubble. D major, 72 bpm, 6 bars.
  fishing: () => loop(6 * 4 * 60 / 72, (b) => {
    const beat = 60 / 72, bar = beat * 4;
    const chords = [[38, 54, 57, 62], [35, 54, 59, 62], [43, 55, 59, 62], [45, 57, 61, 64], [38, 54, 57, 62], [43, 55, 59, 62]];
    const drops = [[[0.5, 81], [2, 78], [3, 74]], [[1, 78], [2.5, 74], [3.5, 71]], [[0, 79], [1.5, 83], [3, 79]], [[0.5, 81], [2, 85], [3.5, 81]], [[1, 78], [2, 74], [3, 78]], [[0, 79], [2, 83], [3, 79]]];
    for (let k = 0; k < 6; k++) {
      const [root, ...tones] = chords[k], t0 = k * bar;
      tones.forEach((m) => note(b, t0, bar * 1.1, midi(m), 0.08, 'pad'));
      note(b, t0, beat * 3, midi(root), 0.14, 'bass');
      drops[k].forEach(([pos, m]) => note(b, t0 + pos * beat, beat * 1.5, midi(m), 0.1, 'bell'));
      puff(b, t0 + beat * (k % 2 ? 0.5 : 1.5), bar * 0.9, 0.09, 700, { attack: bar * 0.4, decay: 1.4 }); // a wave
    }
    [2.9, 7.7, 12.1, 17.3, 20.6].forEach((t, i) => { // bubbles: short rising blips
      const first = Math.floor(t * RATE), n = Math.floor(0.09 * RATE);
      let phase = 0;
      for (let j = 0; j < n; j++) { phase += TAU * (500 + 700 * (j / n) + 150 * (i % 3)) / RATE; b[first + j] += 0.05 * Math.sin(phase) * Math.exp(-j / RATE * 30); }
    });
  }),

  // Space Shooter: driving but friendly. A steady pulsing bass, a spacey arpeggio with a long echo, a slow pad, now and then a whoosh. A minor, 120 bpm, 8 bars.
  spaceshooter: () => loop(16, (b) => {
    const beat = 0.5, bar = 2;
    const chords = [[45, 57, 60, 64], [41, 57, 60, 65], [48, 55, 60, 64], [43, 55, 59, 62], [45, 57, 60, 64], [41, 57, 60, 65], [48, 55, 60, 64], [43, 55, 59, 62]];
    for (let k = 0; k < 8; k++) {
      const [root, ...tones] = chords[k], t0 = k * bar;
      tones.forEach((m) => note(b, t0, bar * 1.05, midi(m + 12), 0.05, 'pad'));
      for (let e = 0; e < 8; e++) note(b, t0 + e * beat / 2, beat * 0.45, midi(root - 12 + (e % 4 === 3 ? 12 : 0)), 0.17, 'bass');
      for (let s = 0; s < 8; s++) {
        const m = tones[[0, 1, 2, 1, 2, 0, 2, 1][s]] + 24;
        note(b, t0 + s * beat / 2, beat * 0.6, midi(m), 0.05, 'chip');
        note(b, t0 + s * beat / 2 + beat * 1.5, beat * 0.6, midi(m), 0.02, 'chip'); // the long echo
      }
    }
    [3.4, 11.4].forEach((t) => puff(b, t, 2.2, 0.1, 1800, { attack: 1.4, decay: 3 })); // a whoosh passing by
    [1.1, 5.3, 9.7, 13.1].forEach((t, i) => note(b, t, 0.5, i % 2 ? 2637 : 3136, 0.03, 'bell')); // a far-off twinkle
  }),

  // Treasure Hunt: curious and a little mysterious, still gentle. Plucked bass, a sparse wooden tune, glints like gold in the sand, a soft hiss of wind. D minor, 90 bpm, 6 bars.
  treasure: () => loop(6 * 4 * 60 / 90, (b) => {
    const beat = 60 / 90, bar = beat * 4;
    const chords = [[38, 53, 57, 62], [34, 53, 58, 62], [43, 55, 58, 62], [33, 52, 57, 61], [38, 53, 57, 62], [34, 53, 58, 62]];
    const tune = [
      [[0, 74], [1.5, 77], [2, 81], [3.5, 79]],
      [[0, 77], [1, 74], [2.5, 72], [3, 74]],
      [[0, 74], [1.5, 70], [2, 74], [3, 77]],
      [[0, 76], [1, 73], [2, 76], [3.5, 81]],
      [[0, 81], [1.5, 79], [2, 77], [3, 74]],
      [[0, 77], [1, 74], [2, 72], [3, 70]],
    ];
    for (let k = 0; k < 6; k++) {
      const [root, ...tones] = chords[k], t0 = k * bar;
      tones.forEach((m) => note(b, t0, bar * 1.05, midi(m), 0.06, 'pad'));
      note(b, t0, beat * 1.4, midi(root), 0.2, 'bass');
      note(b, t0 + beat * 2, beat * 1.4, midi(root + 7), 0.14, 'bass');
      tune[k].forEach(([pos, m]) => note(b, t0 + pos * beat, beat * 1.5, midi(m), 0.14, 'marimba'));
      puff(b, t0, bar, 0.04, 3000, { high: true, attack: bar * 0.5, decay: 1 });
    }
    [2.1, 6.4, 9.2, 13.8].forEach((t) => [2349, 3136].forEach((f, j) => note(b, t + j * 0.09, 0.5, f, 0.04, 'bell'))); // glints
  }),

  // ---- Building ambiences: calmer than the Arcade tracks, each with the sounds of its place. A game opened from a building keeps the building's. ----

  // School: a music-box lullaby over a soft pad, a quiet clock, now and then a chime. C major, 84 bpm, 8 bars.
  school: () => loop(8 * 4 * 60 / 84, (b) => {
    const beat = 60 / 84, bar = beat * 4;
    arrange(b, {
      bpm: 84,
      chords: [CM, GM, AM, FM, CM, GM, FM, CM],
      tune: [[[0, 76], [1, 76], [2, 79], [3, 76]], [[0, 74], [1, 74], [2, 79], [3, 74]], [[0, 72], [1, 76], [2, 81], [3, 79]], [[0, 77], [1, 76], [2, 74], [3, 72]],
        [[0, 76], [1, 79], [2, 84], [3, 79]], [[0, 74], [1, 79], [2, 83], [3, 79]], [[0, 77], [1, 81], [2, 79], [3, 77]], [[0, 76], [2, 72]]],
      tuneShape: 'bell', tuneAmp: 0.09, tuneLen: 1.5, pad: 0.07, bass: 0.1, bassPat: [[0, 0]],
    });
    for (let i = 0; i < 32; i++) puff(b, i * beat, 0.05, 0.05, i % 2 ? 900 : 1300, { attack: 0.001, decay: 60 }); // the clock
    [bar * 3.5, bar * 7.5].forEach((t) => [1568, 2093].forEach((f, j) => note(b, t + j * 0.18, 0.8, f, 0.05, 'bell')));
  }),

  // Playground: a bouncy tune, a creaking swing, slide whistles and a bouncing ball. G major, 112 bpm, 8 bars.
  playground: () => loop(8 * 4 * 60 / 112, (b) => {
    const beat = 60 / 112;
    arrange(b, {
      bpm: 112,
      chords: [GM, CM, GM, DM, GM, CM, DM, GM],
      tune: [[[0, 79], [0.5, 83], [1, 86], [1.5, 83], [2, 79], [3, 83]], [[0, 84], [0.5, 88], [1, 84], [2, 79], [3, 76]], [[0, 79], [0.5, 83], [1, 86], [2, 91], [3, 86]], [[0, 81], [1, 86], [1.5, 90], [2, 86], [3, 81]],
        [[0, 83], [0.5, 86], [1, 91], [2, 86], [3, 83]], [[0, 84], [1, 88], [1.5, 84], [2, 79]], [[0, 86], [0.5, 90], [1, 93], [1.5, 90], [2, 86], [3, 81]], [[0, 79], [1, 83], [2, 79]]],
      tuneShape: 'bell', tuneAmp: 0.1, tuneLen: 0.6, bassPat: [[0, 0], [1.5, 7], [2, 0], [3.5, 7]], bass: 0.17,
    });
    // the swing: a slow creak that rises and falls, twice
    [2, 10.5].forEach((t) => {
      const first = Math.floor(t * RATE), n = Math.floor(3.5 * RATE);
      let phase = 0;
      for (let i = 0; i < n; i++) {
        const k = i / n;
        phase += TAU * (330 + 60 * Math.sin(TAU * 2 * k)) / RATE;
        b[first + i] += 0.025 * Math.sin(phase) * Math.sin(Math.PI * k) * (0.6 + 0.4 * Math.sin(TAU * 11 * i / RATE));
      }
    });
    // slide whistles: up, and down
    [[5.6, 700, 2000], [14.2, 2200, 600]].forEach(([t, f0, f1]) => sweep(b, t, 0.6, f0, f1, 0.05));
    // a ball bouncing, each bounce shorter and softer
    let t = 8.2, gap = 0.5, amp = 0.2;
    for (let k = 0; k < 7; k++) { sweep(b, t, 0.1, 160, 70, amp); t += gap; gap *= 0.72; amp *= 0.75; }
  }),

  // Zoo and farm: a gentle country tune, birds, a breath of wind. D major, 96 bpm, 8 bars.
  zoofarm: () => loop(8 * 4 * 60 / 96, (b) => {
    arrange(b, {
      bpm: 96,
      chords: [DM, GM, DM, AM2, DM, GM, AM2, DM],
      tune: [[[0, 78], [1, 81], [2, 86], [3, 81]], [[0, 79], [1, 83], [2, 86], [3, 83]], [[0, 81], [1, 78], [2, 74], [3, 78]], [[0, 76], [1, 81], [2, 85], [3, 81]],
        [[0, 78], [1, 81], [2, 86], [3, 90]], [[0, 91], [1, 86], [2, 83], [3, 79]], [[0, 85], [1, 81], [2, 76], [3, 81]], [[0, 78], [2, 74]]],
      tuneAmp: 0.12, bassPat: [[0, 0], [2, 7]],
    });
    [1.3, 4.9, 8.8, 12.4, 16.1, 19.6].forEach((t, i) => birdSong(b, t, 3 + (i % 3), 3200 + 300 * (i % 4), 0.03));
    [0, 10].forEach((t) => puff(b, t, 10, 0.05, 500, { attack: 4, decay: 0.4 }));
  }),

  // Science lab: curious plinks, a quiet machine hum, bubbling and the tinkle of glass. A minor, 100 bpm, 8 bars.
  sciencelab: () => loop(8 * 4 * 60 / 100, (b) => {
    const len = 8 * 4 * 60 / 100;
    arrange(b, {
      bpm: 100,
      chords: [AM, FM, CM, GM, AM, FM, CM, GM],
      tune: [[[0, 81], [0.5, 84], [1, 88], [2.5, 84]], [[0, 81], [1, 77], [1.5, 81], [2, 84], [3, 81]], [[0, 79], [0.5, 84], [1, 88], [2, 91], [3.5, 88]], [[0, 86], [1, 83], [2, 79], [3, 74]],
        [[0, 88], [0.5, 84], [1, 81], [2, 84], [3, 88]], [[0, 89], [1, 84], [1.5, 81], [2, 77], [3, 81]], [[0, 91], [0.5, 88], [1, 84], [2, 88], [3, 91]], [[0, 86], [1, 83], [2, 79], [3, 83]]],
      tuneShape: 'chip', tuneAmp: 0.06, tuneLen: 0.4, bass: 0.12, bassPat: [[0, 0], [1.5, 12], [2, 0]], padShape: 'pad', pad: 0.05,
    });
    drone(b, len, 82, 0.03, 0.3);
    for (let i = 0; i < 34; i++) { // bubbles at fixed odd moments
      const t = (i * 0.97 + 0.31 * (i % 5)) % len;
      sweep(b, t, 0.08, 500 + 130 * (i % 6), 900 + 220 * (i % 4), 0.04);
    }
    [3.7, 9.3, 15.1, 21.2].forEach((t, i) => [3136, 3951, 4699].forEach((f, j) => note(b, t + j * 0.06, 0.4, f * (i % 2 ? 1 : 0.84), 0.025, 'bell')));
  }),

  // Workshop: busy and friendly. A hammer tapping a rhythm, a plucked tune, a clank of metal. C major, 108 bpm, 8 bars.
  workshop: () => loop(8 * 4 * 60 / 108, (b) => {
    const beat = 60 / 108, bar = beat * 4;
    arrange(b, {
      bpm: 108,
      chords: [CM, FM, CM, GM, CM, FM, GM, CM],
      tune: [[[0, 72], [1, 76], [2, 79], [3, 76]], [[0, 77], [1, 81], [2, 84], [3, 81]], [[0, 76], [1, 79], [2, 84], [3, 79]], [[0, 74], [1, 79], [2, 83], [3, 79]],
        [[0, 84], [1, 79], [2, 76], [3, 72]], [[0, 81], [1, 77], [2, 72], [3, 77]], [[0, 79], [1, 83], [2, 86], [3, 83]], [[0, 84], [2, 79]]],
      tuneAmp: 0.13, tuneLen: 0.7, bassPat: [[0, 0], [1, 7], [2, 0], [3, 7]], bass: 0.17, pad: 0.04,
    });
    for (let k = 0; k < 8; k++) {
      [0, 1, 1.5, 2, 3].forEach((p) => { // a hammer: a dry tap over a low knock
        puff(b, k * bar + p * beat, 0.04, 0.12, 2200, { attack: 0.001, decay: 80 });
        sweep(b, k * bar + p * beat, 0.07, 240, 120, 0.1);
      });
      if (k % 4 === 3) note(b, k * bar + beat * 3.5, 1.0, 880, 0.05, 'bell'); // a clank
    }
  }),

  // Art studio: dreamy and flowing. A slow harp arpeggio over a wide pad, the swish of a brush. 72 bpm, 8 bars.
  artstudio: () => loop(8 * 4 * 60 / 72, (b) => {
    const beat = 60 / 72, bar = beat * 4;
    const chords = [[48, 55, 59, 64], [45, 55, 60, 64], [41, 57, 60, 64], [43, 59, 62, 64], [48, 55, 59, 64], [45, 55, 60, 64], [41, 57, 60, 64], [43, 59, 62, 64]];
    arrange(b, { bpm: 72, chords, tune: null, pad: 0.1, bass: 0.12, bassPat: [[0, 0]] });
    const order = [0, 1, 2, 1, 0, 1, 2, 1];
    for (let k = 0; k < 8; k++) {
      const [, ...tones] = chords[k];
      order.forEach((o, i) => note(b, k * bar + i * beat / 2, beat * 2, midi(tones[o] + 12 + (i % 4 === 3 ? 12 : 0)), 0.07, 'marimba'));
      puff(b, k * bar + beat * (k % 2 ? 1 : 2.4), beat * 1.6, 0.05, 2400, { attack: 0.4, decay: 2 }); // a brush stroke
    }
  }),

  // Brain gym: calm and focused. Sparse notes with room to think, a soft pulse. A minor pentatonic, 80 bpm, 8 bars.
  braingym: () => loop(8 * 4 * 60 / 80, (b) => {
    const beat = 60 / 80;
    arrange(b, {
      bpm: 80,
      chords: [AM, FM, CM, [40, 55, 59, 64], AM, FM, CM, [40, 55, 59, 64]],
      tune: [[[0, 81], [2.5, 76]], [[1, 77], [3, 81]], [[0, 79], [1.5, 84], [3, 79]], [[0, 76], [2, 83]], [[0, 84], [2, 81], [3, 79]], [[0, 77], [1.5, 81], [3, 84]], [[0, 79], [2, 76]], [[0, 76], [2, 81]]],
      tuneAmp: 0.12, tuneLen: 1.6, bass: 0.1, bassPat: [[0, 0]], pad: 0.06,
    });
    for (let i = 0; i < 32; i++) puff(b, i * beat, 0.05, i % 4 ? 0.025 : 0.05, 1800, { attack: 0.001, decay: 70 }); // a soft pulse
  }),

  // Friends park: warm and sunny, a folk tune with birds far off. E major, 92 bpm, 8 bars.
  friendspark: () => loop(8 * 4 * 60 / 92, (b) => {
    arrange(b, {
      bpm: 92,
      chords: [EM, AM2, EM, BM, EM, AM2, BM, EM],
      tune: [[[0, 76], [1, 80], [2, 83], [3, 80]], [[0, 81], [1, 78], [2, 76], [3, 73]], [[0, 76], [1, 80], [2, 83], [3, 88]], [[0, 83], [1, 80], [2, 78], [3, 75]],
        [[0, 80], [1, 83], [2, 88], [3, 83]], [[0, 81], [1, 85], [2, 81], [3, 78]], [[0, 76], [1, 80], [2, 76], [3, 73]], [[0, 75], [1, 78], [2, 76]]],
      tuneAmp: 0.13, tuneLen: 1.3, bassPat: [[0, 0], [2, 7]], bass: 0.15,
    });
    [3.3, 11.8, 18.6].forEach((t, i) => birdSong(b, t, 3, 3600 + 250 * i, 0.02));
  }),

  // Store: a jaunty shop tune, the ka-ching of a till and a door chime. F major, 116 bpm, 8 bars.
  store: () => loop(8 * 4 * 60 / 116, (b) => {
    const beat = 60 / 116, bar = beat * 4;
    arrange(b, {
      bpm: 116,
      chords: [FM, BbM, FM, CM, FM, BbM, CM, FM],
      tune: [[[0, 77], [0.5, 81], [1, 84], [2, 81], [3, 77]], [[0, 82], [0.5, 79], [1, 77], [2, 82], [3, 77]], [[0, 81], [1, 84], [1.5, 81], [2, 77], [3, 72]], [[0, 79], [0.5, 82], [1, 79], [2, 76], [3, 79]],
        [[0, 84], [0.5, 81], [1, 77], [2, 81], [3, 84]], [[0, 82], [1, 79], [2, 82], [3, 86]], [[0, 84], [0.5, 79], [1, 76], [2, 79], [3, 84]], [[0, 81], [1, 77], [2, 72]]],
      tuneAmp: 0.13, tuneLen: 0.8, bassPat: [[0, 0], [1, 7], [2, 0], [3, 7]], bass: 0.17,
    });
    [bar * 3 + beat * 3, bar * 7 + beat * 3.5].forEach((t) => { // ka-ching
      puff(b, t, 0.05, 0.1, 4000, { high: true, attack: 0.001, decay: 80 });
      [2093, 3136].forEach((f, j) => note(b, t + 0.04 + j * 0.05, 0.6, f, 0.06, 'bell'));
    });
    [bar * 2 - 0.4, bar * 6 - 0.4].forEach((t) => [1568, 1175].forEach((f, j) => note(b, t + j * 0.28, 1.0, f, 0.05, 'bell'))); // the door
  }),
};

function wav(samples) {
  let peak = 0.0001;
  for (const s of samples) peak = Math.max(peak, Math.abs(s));
  const gain = 0.7 / peak; // every track at the same loudness; the player sets the volume
  const data = Buffer.alloc(samples.length * 2);
  samples.forEach((s, i) => data.writeInt16LE(Math.round(Math.max(-1, Math.min(1, s * gain)) * 32767), i * 2));
  const head = Buffer.alloc(44);
  head.write('RIFF', 0); head.writeUInt32LE(36 + data.length, 4); head.write('WAVE', 8); head.write('fmt ', 12);
  head.writeUInt32LE(16, 16); head.writeUInt16LE(1, 20); head.writeUInt16LE(1, 22); head.writeUInt32LE(RATE, 24);
  head.writeUInt32LE(RATE * 2, 28); head.writeUInt16LE(2, 32); head.writeUInt16LE(16, 34); head.write('data', 36); head.writeUInt32LE(data.length, 40);
  return Buffer.concat([head, data]);
}

const outDir = process.argv[2] || path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Music');
fs.mkdirSync(outDir, { recursive: true });
for (const [name, make] of Object.entries(TRACKS)) {
  seed = 4242;
  const file = path.join(outDir, name + '.wav');
  fs.writeFileSync(file, wav(make()));
  console.log(name, fs.statSync(file).size + ' bytes');
}
