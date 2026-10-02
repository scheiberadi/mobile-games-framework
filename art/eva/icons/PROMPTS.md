# Icon prompts for ChatGPT (missing UI icons)

## `icons/activities` - the button in the Store (bottom right) that opens the games menu

Currently a pink placeholder square (`StoreScreen.cs`, `EvaUi.Sprite("icons/activities")`). Save the result as
`art/eva/icons/ai/sheet_icons_activities.png`, then add a `SHEETS` entry (`resDir: 'icons'`, one name `activities`).

"Draw ONE icon for a children's mobile game: the button that opens the games menu. A fan of three small rounded game
cards, one red, one yellow, one blue, slightly overlapping, with a big friendly green play triangle (a rounded right-pointing
triangle with a thin white outline) in front of them in the lower right. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto the background, no text,
letters or numbers anywhere, no people. Centred, filling most of the image with a small plain margin. Background: plain solid
magenta (#ff00ff) everywhere - not a checkered/transparent placeholder, an actual solid magenta fill. File name:
icons_activities.png."

## `icons/home`, `icons/gear`, `icons/back`, `icons/piggybank` - Hud and Map icons, no button plate

One sheet, four icons side by side (done 2026-10-02: `art/eva/icons/ai/sheet_ui_icons.png`, `cut-sheets.js ui_icons --install`).
Home, Back and the Map's settings gear are drawn inside a 240 tap area; the piggy bank replaces the coin next to the counter
(the plain `icons/coin` is still what flies). Prompt: a 4 x 1 sheet, soft polished 3D-look, thick warm brown outline, NO circle,
plate, disc, badge or frame behind any icon, solid magenta background; (1) a chunky red-roofed cottage, (2) a chunky
brown-orange gear with a round hole, (3) a chunky orange left arrow, (4) a pink side-view piggy bank with a gold star coin
dropping into its back slot and two sparkles.

## `icons/blank_tile`, `icons/question`, `icons/x`, `icons/dice`, `icons/hop_marker`, `icons/tray` - small support icons (pink placeholders today)

One sheet, 3 columns x 2 rows, cut with a new `SHEETS` entry (`grid: {cols: 3, rows: 2}`, `bg: 'flood'`, names in reading order
`blank_tile, question, x, dice, hop_marker, tray`, `resDir: 'icons'`, size 256). Used by Pattern Completion and What's Missing
(blank_tile = the empty slot the child fills), the equation and missing letter/number games (question), Subtraction (x),
the character creator (dice), Number Line (hop_marker) and One More One Less (tray).

"Draw 6 small icons for a children's mobile game on one sheet, 3 columns x 2 rows, evenly spaced with wide plain margin so they can be cut apart. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting from the front, no shadows cast onto the background, no text, letters or numbers anywhere except the question mark, no people. Each icon centred in its cell, filling about 70 percent of it. Reading order:
1. An EMPTY SLOT tile: a soft rounded square, slightly sunken, pale cream inside with a dashed warm-brown outline, nothing drawn inside it (a place where a picture is dropped).
2. A big friendly QUESTION MARK, chunky and rounded, bright orange with a thin brown outline and a small highlight.
3. A big bold X mark (a cross-out), two chunky rounded crossing strokes, red with a thin dark-red outline.
4. A cheerful six-sided DICE seen in a slight three-quarter view, white with five or six visible dark dots, rounded corners, thin brown outline.
5. A HOP MARKER for a number line: a small round green circle-shaped stepping stone with a thin white ring and a tiny downward-pointing arrow tip above it, pointing at the spot where it lands.
6. A TRAY: a shallow rounded wooden serving tray seen from slightly above, empty, light wood with a darker rim, wide and low.
Background: plain solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent placeholder, an actual solid magenta fill. File name: icons_support_sheet.png."
