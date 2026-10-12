// Eva the astronaut talks: arcade/astronaut_eva.png (790 x 949) has a closed mouth under the nose; this draws the open mouth (the same one
// make-cat-overlays.js draws for the plain cat) at that spot on a transparent canvas of the same size -> sciencelab/astronaut_eva_mouth.png.
// The screen lays it over the astronaut and shows it while she talks.
//   node tools/art-import/make-astronaut-mouth.js [preview.png]
const path = require('path');
const sharp = require(path.join(__dirname, 'node_modules', 'sharp'));

const ART = path.join(__dirname, '..', '..', 'EvasLearningWorld', 'Assets', 'Eva', 'Resources', 'Art');
const CX = 312, CY = 392;   // the middle of the mouth, a little under the nose, measured on the picture

(async () => {
  const mouth = `<svg xmlns="http://www.w3.org/2000/svg" width="790" height="949"><defs><filter id="soft"><feGaussianBlur stdDeviation="0.7"/></filter></defs>
    <g filter="url(#soft)" transform="translate(${CX} ${CY}) scale(0.85) translate(-436 -449)">
      <path d="M 412 428 Q 436 423 460 428 Q 464 460 436 470 Q 408 460 412 428 Z" fill="#1d0a0c" stroke="#2a1410" stroke-width="2.4"/>
      <ellipse cx="436" cy="457" rx="15" ry="9" fill="#b8505f"/>
    </g></svg>`;
  const out = path.join(ART, 'sciencelab', 'astronaut_eva_mouth.png');
  await sharp(Buffer.from(mouth)).png().toFile(out);
  console.log('written', out);
  if (process.argv[2]) {
    const full = await sharp(path.join(ART, 'arcade', 'astronaut_eva.png')).composite([{ input: out }]).png().toBuffer();
    await sharp(full).extract({ left: 160, top: 260, width: 300, height: 220 }).resize(900, 660).flatten({ background: '#3c3c5a' }).png().toFile(process.argv[2]);
  }
})();
