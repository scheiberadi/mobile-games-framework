#!/usr/bin/env node
'use strict';

// Imports the animal sound clips made with ElevenLabs Sound Effects (14 mp3 files named <animal>.mp3, e.g. cow.mp3) into
// Resources/Animals/<animal>.wav: decoded, silence at the start trimmed, cut to MAX_SECONDS with a short fade-out,
// peak-normalised, mono 16-bit WAV.
//   node tools/animals/import.js [srcDir] [outDir]     default src: ~/Downloads
//   node tools/animals/import.js --report              only print each file's length, peak and leading silence

const fs = require('fs');
const os = require('os');
const path = require('path');
const { MPEGDecoder } = require('mpg123-decoder');

const ANIMALS = ['cow', 'lion', 'duck', 'owl', 'sheep', 'horse', 'eagle', 'pig', 'snake', 'chicken', 'frog', 'dog', 'cat', 'elephant'];
const MAX_SECONDS = 2.6;
const FADE_SECONDS = 0.25;
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

(async () => {
  const args = process.argv.slice(2);
  const report = args.includes('--report');
  const [srcDir = path.join(os.homedir(), 'Downloads'), outDir = path.join(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Animals')] = args.filter(a => !a.startsWith('--'));
  if (!report) fs.mkdirSync(outDir, { recursive: true });

  for (const animal of ANIMALS) {
    const { samples, rate } = await load(path.join(srcDir, animal + '.mp3'));
    let peak = 0, start = 0;
    for (const s of samples) peak = Math.max(peak, Math.abs(s));
    while (start < samples.length && Math.abs(samples[start]) < SILENCE * peak) start++;
    start = Math.max(0, start - Math.floor(0.02 * rate));
    const length = Math.min(samples.length - start, Math.floor(MAX_SECONDS * rate));
    // Cut at MAX_SECONDS or, if the clip is shorter, at the last audible sample.
    let end = start + length;
    while (end > start + 1 && Math.abs(samples[end - 1]) < SILENCE * peak) end--;
    const out = samples.slice(start, Math.min(samples.length, end + Math.floor(0.05 * rate)));
    let outPeak = 0.0001;
    for (const s of out) outPeak = Math.max(outPeak, Math.abs(s));
    const gain = PEAK / outPeak;
    const fade = Math.floor(FADE_SECONDS * rate);
    for (let i = 0; i < out.length; i++) {
      const tail = out.length - i;
      out[i] *= gain * (tail < fade ? tail / fade : 1);
    }
    console.log(animal.padEnd(9), 'total', (samples.length / rate).toFixed(2) + 's', 'lead-silence', (start / rate).toFixed(2) + 's', 'peak', peak.toFixed(2), '->', (out.length / rate).toFixed(2) + 's');
    if (!report) fs.writeFileSync(path.join(outDir, animal + '.wav'), wav(out, rate));
  }
})();
