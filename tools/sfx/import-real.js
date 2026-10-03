#!/usr/bin/env node
'use strict';

// Imports sound effects made with ElevenLabs Sound Effects (mp3 files in Downloads) into Resources/Sfx/<name>.wav, replacing
// the ones tools/sfx/generate.js used to synthesise (splash, flop, shiver, reelout, reelin; they are no longer in that generator).
//   node tools/sfx/import-real.js [srcDir] [outDir]     default src: ~/Downloads
// One-shots (splash, flop, shiver): silence trimmed at both ends, cut to MAX_SECONDS with a short fade-out.
// Loops (reelout, reelin, played with AudioSource.loop): the last SEAM seconds are cross-faded into the first SEAM seconds so the
// repeat has no click or gap. Everything is peak-normalised, mono 16-bit WAV.
// Needs the decoder installed in tools/animals (npm install there).

const fs = require('fs');
const os = require('os');
const path = require('path');
const { MPEGDecoder } = require('../animals/node_modules/mpg123-decoder');

const ONE_SHOTS = ['splash', 'flop', 'shiver'];
const LOOPS = ['reelout', 'reelin'];
const MAX_SECONDS = 2.0;
const FADE_SECONDS = 0.15;
const SEAM_SECONDS = 0.1;
const PEAK = 0.8;
const SILENCE = 0.02;

async function load(file) {
  const decoder = new MPEGDecoder();
  await decoder.ready;
  const { channelData, sampleRate } = decoder.decode(new Uint8Array(fs.readFileSync(file)));
  decoder.free();
  const mono = new Float32Array(channelData[0].length);
  for (const ch of channelData) for (let i = 0; i < mono.length; i++) mono[i] += ch[i] / channelData.length;
  return { samples: mono, rate: sampleRate };
}

function wav(samples, rate) {
  const data = Buffer.alloc(samples.length * 2);
  samples.forEach((s, i) => data.writeInt16LE(Math.round(Math.max(-1, Math.min(1, s)) * 32767), i * 2));
  const head = Buffer.alloc(44);
  head.write('RIFF', 0); head.writeUInt32LE(36 + data.length, 4); head.write('WAVE', 8); head.write('fmt ', 12);
  head.writeUInt32LE(16, 16); head.writeUInt16LE(1, 20); head.writeUInt16LE(1, 22); head.writeUInt32LE(rate, 24);
  head.writeUInt32LE(rate * 2, 28); head.writeUInt16LE(2, 32); head.writeUInt16LE(16, 34); head.write('data', 36); head.writeUInt32LE(data.length, 40);
  return Buffer.concat([head, data]);
}

const peakOf = (s) => s.reduce((p, v) => Math.max(p, Math.abs(v)), 0.0001);

function oneShot(samples, rate) {
  const peak = peakOf(samples);
  let start = 0;
  while (start < samples.length && Math.abs(samples[start]) < SILENCE * peak) start++;
  start = Math.max(0, start - Math.floor(0.02 * rate));
  let end = Math.min(samples.length, start + Math.floor(MAX_SECONDS * rate));
  while (end > start + 1 && Math.abs(samples[end - 1]) < SILENCE * peak) end--;
  const out = samples.slice(start, Math.min(samples.length, end + Math.floor(0.05 * rate)));
  const gain = PEAK / peakOf(out);
  const fade = Math.floor(FADE_SECONDS * rate);
  for (let i = 0; i < out.length; i++) out[i] *= gain * Math.min(1, (out.length - i) / fade);
  return out;
}

function loop(samples, rate) {
  const seam = Math.floor(SEAM_SECONDS * rate);
  const length = samples.length - seam;
  const out = new Float32Array(length);
  for (let i = 0; i < length; i++) out[i] = samples[i];
  // The tail (the seam's worth of samples after `length`) fades out while the head fades in, so the end flows into the start.
  for (let i = 0; i < seam; i++) {
    const k = i / seam;
    out[i] = samples[i] * k + samples[length + i] * (1 - k);
  }
  const gain = PEAK / peakOf(out);
  for (let i = 0; i < out.length; i++) out[i] *= gain;
  return out;
}

(async () => {
  const [srcDir = path.join(os.homedir(), 'Downloads'), outDir = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Sfx')] = process.argv.slice(2);
  fs.mkdirSync(outDir, { recursive: true });
  for (const [names, make] of [[ONE_SHOTS, oneShot], [LOOPS, loop]]) {
    for (const name of names) {
      const { samples, rate } = await load(path.join(srcDir, name + '.mp3'));
      const out = make(samples, rate);
      fs.writeFileSync(path.join(outDir, name + '.wav'), wav(out, rate));
      console.log(name.padEnd(8), (samples.length / rate).toFixed(2) + 's ->', (out.length / rate).toFixed(2) + 's', 'rate', rate);
    }
  }
})();
