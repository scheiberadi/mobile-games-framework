'use strict';

const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const { parseLines, generate } = require('./generate.js');

const KEY = 'SECRET-TEST-KEY-12345';

function tempDir() {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'eva-voice-'));
}

function okResponse(text) {
  const audio = Buffer.from('MP3:' + text).toString('base64');
  return { ok: true, status: 200, json: async () => ({ audioContent: audio }) };
}

function fakeFetch(calls, responder) {
  return async (url, options) => {
    calls.push({ url, options, body: JSON.parse(options.body) });
    return responder ? responder(url, options) : okResponse(JSON.parse(options.body).input.text);
  };
}

function capture() {
  const out = [];
  return { out, log: (line) => out.push(String(line)) };
}

test('parseLines ignores comments and blanks and splits on the first tab', () => {
  const text = '# a comment\n\nkey_one\tHello there\r\n   \nkey_two\tWith\ttab inside\n#another\n';
  assert.deepStrictEqual(parseLines(text), [
    { key: 'key_one', text: 'Hello there' },
    { key: 'key_two', text: 'With\ttab inside' },
  ]);
});

test('parseLines parses the real voice-lines file into one entry per non-comment line', () => {
  const file = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Voice', 'voice-lines.txt');
  const raw = fs.readFileSync(file, 'utf8');
  const expected = raw.split(/\r?\n/).filter((l) => l.trim() !== '' && !l.startsWith('#')).length;
  const lines = parseLines(raw);
  assert.strictEqual(lines.length, expected);
  assert.ok(lines.length > 0);
  for (const l of lines) {
    assert.match(l.key, /^[a-z0-9_]+$/);
    assert.ok(l.text.length > 0);
  }
});

test('existing files are skipped without a request', async () => {
  const dir = tempDir();
  fs.writeFileSync(path.join(dir, 'a.mp3'), 'old');
  const calls = [];
  const { log } = capture();
  const result = await generate({
    lines: [{ key: 'a', text: 'A' }, { key: 'b', text: 'B' }],
    outDir: dir, apiKey: KEY, fetchImpl: fakeFetch(calls), log,
  });
  assert.strictEqual(calls.length, 1);
  assert.strictEqual(calls[0].body.input.text, 'B');
  assert.strictEqual(fs.readFileSync(path.join(dir, 'a.mp3'), 'utf8'), 'old');
  assert.strictEqual(fs.readFileSync(path.join(dir, 'b.mp3'), 'utf8'), 'MP3:B');
  assert.strictEqual(result.failed, 0);
  assert.strictEqual(result.skipped, 1);
  assert.strictEqual(result.generated, 1);
});

test('force regenerates existing files', async () => {
  const dir = tempDir();
  fs.writeFileSync(path.join(dir, 'a.mp3'), 'old');
  const calls = [];
  const { log } = capture();
  await generate({
    lines: [{ key: 'a', text: 'A' }],
    outDir: dir, apiKey: KEY, fetchImpl: fakeFetch(calls), force: true, log,
  });
  assert.strictEqual(calls.length, 1);
  assert.strictEqual(fs.readFileSync(path.join(dir, 'a.mp3'), 'utf8'), 'MP3:A');
});

test('the request body carries the voice name, MP3 encoding and the text', async () => {
  const dir = tempDir();
  const calls = [];
  const { log } = capture();
  await generate({ lines: [{ key: 'a', text: 'Hello Eva' }], outDir: dir, apiKey: KEY, fetchImpl: fakeFetch(calls), log });
  assert.strictEqual(calls.length, 1);
  assert.ok(calls[0].url.startsWith('https://texttospeech.googleapis.com/v1/text:synthesize?key='));
  assert.strictEqual(calls[0].options.method, 'POST');
  assert.deepStrictEqual(calls[0].body, {
    input: { text: 'Hello Eva' },
    voice: { languageCode: 'en-US', name: 'en-US-Chirp3-HD-Leda' },
    audioConfig: { audioEncoding: 'MP3' },
  });
});

test('a 403 is reported without the key or URL in the output, the remaining lines are still tried, and failed is counted', async () => {
  const dir = tempDir();
  const calls = [];
  const { out, log } = capture();
  const responder = (url, options) => {
    const body = JSON.parse(options.body);
    if (body.input.text === 'A') {
      return {
        ok: false,
        status: 403,
        // A hostile API message that even echoes the key must still be redacted.
        json: async () => ({ error: { code: 403, message: 'Cloud Text-to-Speech API has not been used. key=' + KEY, status: 'PERMISSION_DENIED' } }),
      };
    }
    return okResponse(body.input.text);
  };
  const result = await generate({
    lines: [{ key: 'a', text: 'A' }, { key: 'b', text: 'B' }],
    outDir: dir, apiKey: KEY, fetchImpl: fakeFetch(calls, responder), log,
  });
  assert.strictEqual(calls.length, 2);
  assert.strictEqual(result.failed, 1);
  assert.strictEqual(result.generated, 1);
  assert.ok(!fs.existsSync(path.join(dir, 'a.mp3')));
  assert.ok(fs.existsSync(path.join(dir, 'b.mp3')));
  const all = out.join('\n');
  assert.ok(all.includes('403'));
  assert.ok(all.includes('has not been used'));
  assert.ok(!all.includes(KEY), 'output must not contain the key');
  assert.ok(!all.includes('googleapis.com'), 'output must not contain the request URL');
  assert.ok(!all.includes('key='), 'output must not contain a key query string');
});

test('a network error whose message contains the URL is sanitised', async () => {
  const dir = tempDir();
  const { out, log } = capture();
  const fetchImpl = async (url) => { throw new TypeError('fetch failed for ' + url); };
  const result = await generate({ lines: [{ key: 'a', text: 'A' }], outDir: dir, apiKey: KEY, fetchImpl, log });
  assert.strictEqual(result.failed, 1);
  const all = out.join('\n');
  assert.ok(!all.includes(KEY));
  assert.ok(!all.includes('googleapis.com'));
});

test('an empty audioContent counts as a failure and writes no file', async () => {
  const dir = tempDir();
  const { log } = capture();
  const fetchImpl = async () => ({ ok: true, status: 200, json: async () => ({}) });
  const result = await generate({ lines: [{ key: 'a', text: 'A' }], outDir: dir, apiKey: KEY, fetchImpl, log });
  assert.strictEqual(result.failed, 1);
  assert.ok(!fs.existsSync(path.join(dir, 'a.mp3')));
});

test('missing GOOGLE_TTS_API_KEY exits non-zero with a clear message before any request', () => {
  const dir = tempDir();
  const linesFile = path.join(dir, 'lines.txt');
  fs.writeFileSync(linesFile, 'a\tHello\n');
  const env = { ...process.env };
  delete env.GOOGLE_TTS_API_KEY;
  const r = spawnSync(process.execPath, [path.join(__dirname, 'generate.js'), linesFile, path.join(dir, 'out')], { env, encoding: 'utf8' });
  assert.notStrictEqual(r.status, 0);
  assert.match(r.stderr + r.stdout, /GOOGLE_TTS_API_KEY/);
  assert.ok(!fs.existsSync(path.join(dir, 'out')), 'no output directory is created before the key check');
});

test('generate itself rejects a missing key before any request', async () => {
  const calls = [];
  await assert.rejects(
    generate({ lines: [{ key: 'a', text: 'A' }], outDir: tempDir(), apiKey: '', fetchImpl: fakeFetch(calls) }),
    /GOOGLE_TTS_API_KEY/,
  );
  assert.strictEqual(calls.length, 0);
});
