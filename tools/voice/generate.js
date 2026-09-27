#!/usr/bin/env node
'use strict';

// Generates Eva's spoken clips from a tab-separated lines file with Google Cloud Text-to-Speech.
//
//   node tools/voice/generate.js <voice-lines.txt> <outDir> [--force]
//
// If GOOGLE_TTS_API_KEY is set, it is sent as a query parameter; neither the key nor the request
// URL is ever printed, since every message goes through redact(). If it is not set, requests are
// sent with no key param at all, so an environment that authenticates calls to
// texttospeech.googleapis.com by other means (e.g. an authenticating proxy) still works.

const fs = require('node:fs');
const path = require('node:path');

const ENDPOINT = 'https://texttospeech.googleapis.com/v1/text:synthesize';
const VOICE = { languageCode: 'en-US', name: 'en-US-Chirp3-HD-Leda' };

// One "key<TAB>text" per line. Blank lines and lines starting with '#' are ignored; only the
// first tab separates the key from the text.
function parseLines(text) {
  const lines = [];
  for (const raw of text.split(/\r?\n/)) {
    if (raw.trim() === '' || raw.startsWith('#')) continue;
    const tab = raw.indexOf('\t');
    if (tab < 0) continue;
    const key = raw.slice(0, tab).trim();
    const value = raw.slice(tab + 1).trim();
    if (key === '' || value === '') continue;
    lines.push({ key, text: value });
  }
  return lines;
}

// Removes anything that could leak the API key or the request URL from a message.
function redact(message, apiKey) {
  let result = String(message);
  if (apiKey) {
    result = result.split(apiKey).join('[redacted]');
    result = result.split(encodeURIComponent(apiKey)).join('[redacted]');
  }
  result = result.replace(/https?:\/\/\S+/gi, '[url]');
  result = result.replace(/\bkey=\S*/gi, '[redacted]');
  return result;
}

async function generate({ lines, outDir, apiKey, fetchImpl = fetch, force = false, log = console.log }) {
  fs.mkdirSync(outDir, { recursive: true });
  const url = apiKey ? ENDPOINT + '?key=' + encodeURIComponent(apiKey) : ENDPOINT;
  const result = { generated: 0, skipped: 0, failed: 0 };

  for (const { key, text } of lines) {
    const file = path.join(outDir, key + '.mp3');
    if (!force && fs.existsSync(file)) {
      result.skipped++;
      continue;
    }

    try {
      const response = await fetchImpl(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          input: { text },
          voice: VOICE,
          audioConfig: { audioEncoding: 'MP3' },
        }),
      });

      if (!response.ok) {
        let apiMessage = '';
        try {
          const body = await response.json();
          apiMessage = (body && body.error && body.error.message) || '';
        } catch (e) {
          // No readable JSON body: report the status alone.
        }
        log(`${key}: HTTP ${response.status}${apiMessage ? ' ' + redact(apiMessage, apiKey) : ''}`);
        result.failed++;
        continue;
      }

      const body = await response.json();
      const audio = body && body.audioContent ? Buffer.from(body.audioContent, 'base64') : null;
      if (!audio || audio.length === 0) {
        log(`${key}: the API returned no audio`);
        result.failed++;
        continue;
      }
      fs.writeFileSync(file, audio);
      result.generated++;
      log(`${key}: ok (${audio.length} bytes)`);
    } catch (e) {
      // Node's fetch errors can include the request URL, so only a redacted message is shown.
      log(`${key}: request failed (${redact(e && e.message ? e.message : e, apiKey)})`);
      result.failed++;
    }
  }

  return result;
}

async function main(argv, env) {
  const args = argv.filter((a) => !a.startsWith('--'));
  const force = argv.includes('--force');
  if (args.length !== 2) {
    console.error('Usage: node generate.js <voice-lines.txt> <outDir> [--force]');
    return 2;
  }
  const apiKey = env.GOOGLE_TTS_API_KEY;

  const lines = parseLines(fs.readFileSync(args[0], 'utf8'));
  const result = await generate({ lines, outDir: args[1], apiKey, force });
  console.log(`done: ${result.generated} generated, ${result.skipped} skipped, ${result.failed} failed`);
  return result.failed > 0 ? 1 : 0;
}

module.exports = { parseLines, generate };

if (require.main === module) {
  main(process.argv.slice(2), process.env).then(
    (code) => { process.exitCode = code; },
    (e) => {
      console.error(redact(e && e.message ? e.message : e, process.env.GOOGLE_TTS_API_KEY));
      process.exitCode = 1;
    },
  );
}
