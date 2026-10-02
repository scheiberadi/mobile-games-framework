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

## Support sprites for the maze games, puzzle slots and trace (generated 2026-10-02 audit: these keys are still pink placeholders)

Three sheets, then two backgrounds (the backgrounds are 1920 x 900, full-bleed, no controls drawn in).

### Sheet A (3 x 3): `fingermaze/character`, `fingermaze/checkpoint`, `fingermaze/corridor`, `fingermaze/finish`, `avoidobstacles/hazard`, `collecteverything/pickup`, `shortestpath/start`, `artstudio/pencil_tip`, `artstudio/trace_path`

Cut with a new `SHEETS` entry (`grid: {cols: 3, rows: 2}`... 3 x 3, `bg: 'flood'`, names in reading order, one `resDir` per folder - install by hand or split into three entries by `resDir`).

"Draw 9 small game sprites for a children's mobile game on one sheet, 3 columns x 3 rows, evenly spaced with wide plain margin so they can be cut apart. Style for everything: soft polished 3D-look children's mobile-game illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text or letters or numbers anywhere, no people. Background: plain solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent placeholder, an actual solid magenta fill. Each item is drawn alone, centred in its own cell, nothing touching a cell edge. Reading order: (1) a cute little round cartoon ladybug seen from above, friendly, the child's finger-controlled character; (2) a checkpoint marker: a round golden disc on the ground with a thin white ring, nothing written on it; (3) a path tile: a plain horizontal soft sandy-beige rounded bar (pill shape) with a thin darker beige outline and NO pattern along its length, so it can be stretched to any length; (4) a finish marker: a small chequered black-and-white flag on a short pole standing on a round grass patch; (5) a hazard: a friendly-looking grey rock with a small angry-but-cute face-free crack pattern and a few thorny little spikes, clearly something to steer around (not scary); (6) a collectible pickup: a shiny gold star with a soft white sparkle; (7) a start marker: a round green disc on the ground with a white arrow pointing right on it; (8) a pencil tip: a chunky yellow pencil seen from above pointing diagonally down-left, big and easy to see; (9) a trace line piece: a plain horizontal soft pale-blue rounded bar with a thin lighter outline and no pattern, so it can be stretched to any length. File name: support_maze_sheet.png."

### Sheet B (3 x 2): `workshop/slot_outline`, `jigsaw/slot`, `tangram/ghost_shape`, `rotatepiece/handle`, `dressup/suitcase_slot`, plus 1 spare

"Draw 5 small game sprites for a children's mobile game on one sheet, 3 columns x 2 rows (the 6th cell stays plain magenta), evenly spaced with wide plain margin. Style for everything: soft polished 3D-look children's mobile-game illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text or letters or numbers anywhere, no people. Background: plain solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent placeholder, an actual solid magenta fill. Each item centred in its own cell, nothing touching a cell edge. Reading order: (1) a rounded-square OUTLINE only: a dashed thick white line with a thin dark outline, completely empty and transparent inside (a place where a part is dropped); (2) a jigsaw slot: a flat square soft pale-grey recessed tile with slightly rounded corners and a faint inner shadow, nothing inside; (3) a ghost silhouette: a plain solid pale off-white rounded square shape, no outline, no detail (it is tinted in the game); (4) a rotate handle: a big round blue button with a curved white two-headed rotation arrow on it, no text; (5) a suitcase slot: a flat outline of an open suitcase seen from above as a dashed warm-brown rounded rectangle with two small clasps drawn at the top, empty inside. Cell 6 stays empty magenta. File name: support_slots_sheet.png."

### Background C: `world/pond` (a pond picture, 480 x 300 in game, wide, plain magenta around it)

"Draw ONE small pond scene for a children's mobile game: an oval blue pond with soft lily pads and tiny reeds at the edge, a few small smooth stones around it, seen slightly from above. Style: soft polished 3D-look children's mobile-game illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no text, no people, no animals. The pond fills most of the image, centred, plain solid magenta (#ff00ff) background everywhere around it - an actual solid magenta fill, not a checkered/transparent placeholder. File name: world_pond.png."

### Background D: `world/artstudio_canvas_bg` (full-screen background of the free drawing screen, 1920 x 900)

"Draw a wide background image for a children's drawing screen in a mobile game, 1920 x 900 pixels, no characters and no objects in the middle: a soft warm cream paper-like surface filling the whole picture, with a gentle wooden art-table edge along the very bottom and a few small paint splatters and a pencil and brush resting in the far bottom-left and far bottom-right corners only; the whole centre and upper area stays a calm, plain, slightly textured light cream so drawings stand out. Style: soft polished 3D-look children's mobile-game illustration, warm rounded shapes, gentle even lighting, no text or letters, no people. File name: world_artstudio_canvas_bg.png."
