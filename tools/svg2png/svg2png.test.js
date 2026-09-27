const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const { renderSvg } = require('./svg2png');

const apple = fs.readFileSync(path.join(__dirname, '..', '..', 'art', 'spike', 'apple.svg'));
const px = (img, x, y) => Array.from(img.pixels.slice((y * img.width + x) * 4, (y * img.width + x) * 4 + 4));

test('renders at the requested width, body opaque, corner transparent', () => {
  const img = renderSvg(apple, 200);
  assert.strictEqual(img.width, 200);
  assert.strictEqual(px(img, 100, 90)[3], 255);
  assert.strictEqual(px(img, 2, 2)[3], 0);
});

test('radial gradient: highlight is lighter than the rim', () => {
  const img = renderSvg(apple, 200);
  assert.ok(px(img, 85, 75)[1] > px(img, 120, 130)[1] + 20);
});

test('drop shadow: soft semi-transparent pixels below the body', () => {
  const alpha = px(renderSvg(apple, 200), 100, 160)[3];
  assert.ok(alpha > 0 && alpha < 255, 'shadow alpha=' + alpha);
});
