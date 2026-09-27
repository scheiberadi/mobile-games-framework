# Spike: SVG to PNG rasterization

## Verdict

PASS. A 20-line Node script using @resvg/resvg-js 2.6.2 rasterizes SVG with radial gradients and `feDropShadow` soft shadows to correct transparent PNGs, with no fallback needed.

## Evidence

- Sample art: `art/spike/apple.svg` (radial gradient body plus `feDropShadow` dy=6, stdDeviation=4, opacity 0.35).
- Script: `tools/svg2png/svg2png.js`; tests: `tools/svg2png/svg2png.test.js` (`npm test`, Node built-in runner).
- RED: before the script existed, `npm test` failed with `Error: Cannot find module './svg2png'`.
- GREEN: after implementing it, all 3 tests pass (3 pass, 0 fail, about 0.6 s).
- Measured pixels at 200 px width (RGBA):
  - body (100,90): 242,108,93,255 (fully opaque)
  - corner (2,2): 0,0,0,0 (fully transparent background)
  - gradient highlight (85,75): 251,128,113 versus rim (120,130): 223,64,53 (gradient renders, highlight lighter)
  - shadow below the body (100,160): 0,0,0,48 (soft, semi-transparent)
- CLI: `node tools/svg2png/svg2png.js art/spike <outDir> 400` printed `apple.svg -> 400x400` and wrote a 49 KB PNG. Transparency was proven by the raw-pixel test above (corner pixel 0,0,0,0); the image viewer showed a white background, so it does not confirm transparency by eye.
- `feDropShadow` is supported, so the `feGaussianBlur`/`feOffset`/`feMerge` fallback was not needed.

## Decision

SVG is viable as the art source format for the gradient-and-soft-shadow style. Author art as SVG and rasterize with this script at the needed pixel width. Not yet checked (out of scope for this spike): text and fonts, clip paths and masks, blend modes, and batch throughput on a large art set; verify those when the first real art needs them.
