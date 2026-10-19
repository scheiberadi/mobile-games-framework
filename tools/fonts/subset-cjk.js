// Trims the Noto CJK fonts in Assets/Fonts to the characters the localization files use (plus ASCII),
// keeping the original files in Assets/Fonts-original (outside Unity's import) so it can be re-run
// whenever localization text changes. Usage: node tools/fonts/subset-cjk.js
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '../../EvasLearningWorld');
const fonts = path.join(root, 'Assets/Fonts');
const backup = path.join(root, 'FontsOriginal');
const locDir = path.join(root, 'Assets/Eva/Resources/Localization/Eva');
const subsetFont = require(path.join(__dirname, 'node_modules/subset-font'));

(async () => {
  let text = '';
  for (const f of fs.readdirSync(locDir)) if (f.endsWith('.json')) text += fs.readFileSync(path.join(locDir, f), 'utf8');
  for (let c = 0x20; c < 0x7f; c++) text += String.fromCharCode(c);
  text += '。、！？：；（）「」…・ー';
  const chars = [...new Set(text)].join('');
  fs.mkdirSync(backup, { recursive: true });
  for (const f of fs.readdirSync(fonts).filter(n => /^NotoSansCJK.*\.otf$/.test(n))) {
    const original = path.join(backup, f);
    if (!fs.existsSync(original)) fs.copyFileSync(path.join(fonts, f), original);
    const out = await subsetFont(fs.readFileSync(original), chars, { targetFormat: 'sfnt' });
    fs.writeFileSync(path.join(fonts, f), out);
    console.log(f, fs.statSync(original).size, '->', out.length);
  }
})();
