# Map art prompts for ChatGPT

Style for everything: the same soft polished 3D-look children's mobile-game illustration as the house art (`art/eva/house`): warm, rounded, thin brown outlines, no text or letters, no people, no animals.

The game's building positions, sizes and road routes are fixed by the layout guide. Your pictures are fitted to them; they cannot move. If a picture does not match the guide, regenerate it.

## Prompt 1: landscape backdrop

Attach `layout-guide.png`.

"Attached is a layout guide. Draw ONE wide landscape scene, 2880 x 1350 px (aspect 32:15), seen from a slightly raised angle, as a bright cheerful meadow. The scenery must frame the places, never compete with them. Rules: (1) The middle of the picture, including the whole dashed 'first view' frame, must be open, calm, uncluttered grass: only flat grass, very small flowers and tiny pebbles there. (2) Put trees, bushes, rocks and the pond only in the outer areas: along the far left and right edges, the top band and the bottom band, outside the dashed frame. (3) The yellow rectangles (future buildings), the light-blue squares (where the little characters stand) and the blue boxes with red lines (future dirt paths) must be plain open grass, and keep at least 150 px of plain grass all around each of them: NO large trees or bushes directly behind or beside a yellow rectangle, and nothing tall or bulky near them. (4) Nothing in the scenery may look like a building, house, hut, tent, shed, wall, fence line or sign, and no stone circle or structure that could be mistaken for one. (5) No decorative element may lie on or cross the red lines or the blue path boxes. Add a small pond near the far right edge and gentle hills at the top. Do NOT draw any buildings, roads, paths, people, animals, signs, text, or the guide's boxes/lines/labels themselves. Soft even lighting, no strong shadows, no vignette. File name: map_world.png."

Then: "If it comes out not exactly 32:15, that is fine, tell me the size."

## Prompt 2: three buildings

One prompt each, so they can be regenerated separately; attach the current placeholder pictures only if a shape reference helps.

Add to every building prompt: "The three buildings will be shown at similar visual size, so make the building fill most of the picture, with only a small margin around it (House about 8:7 wide, School and Store about 7:6 wide), and keep the level of detail and overall mass similar between them."

House (no attachment; `shell.png` is code-drawn and not a reference): "Draw a cute two-storey family house, seen from the front and slightly from above, as a single building for a children's mobile game map. Roof: one steep gabled roof in red-orange with a scalloped fish-scale tile pattern and a thin dark red outline, small overhang at both sides. Chimney: a small brick chimney, red-orange bricks with a cream cap, on the left side of the roof. Walls: warm cream with a light brown base strip along the bottom; exactly two storeys, clearly one floor above the other. Ground floor: a round wooden front door in the middle and one window on each side of it. Upper floor: three windows in a row, evenly spaced. A tiny flower box under one ground-floor window. Show only the house, centred and filling most of the picture, on a plain solid magenta (#ff00ff) background with a small margin (aspect about 8:7). No ground, grass, path, trees, shadows, text or people. It will be shown next to a small school and a small shop at a similar size, so keep the detail level modest. File name: place_house.png."

School (must look clearly different from the House: different colours and silhouette): "Draw a small friendly school building, seen from the front and slightly from above, as a single building for a children's mobile game map. It must look clearly different from a cosy cottage. Shape: a wide, low, one-storey building with a flat-topped, gently sloped roof, plus a tall square clock tower rising from the middle of the front; no steep gabled roof and no dormers. Colours: red-brick walls (visible brick pattern, warm brick red), a teal-blue roof (flat blue-green tiles, not orange), white window frames and trim; no cream or orange anywhere on the walls or roof. Tower: the square brick tower has a big round white clock face with clock hands, a small teal pyramid roof and a small yellow flag on top. Front: a wide double door in the middle with a small flat blue canopy above it and a short row of three steps; four large square windows with white frames, two on each side of the door (rectangular, not arched); a small yellow star sign above the door. Show only the building, centred and filling most of the picture, on a plain solid magenta (#ff00ff) background with a small margin (aspect about 7:6). No ground, grass, trees, fence, shadows, text or people. Style: soft polished 3D-look children's mobile-game illustration, warm rounded shapes, thin brown outlines, medium detail. File name: place_school.png."

Store: "A small shop with a striped awning, a big shop window and a wooden door, front view slightly from above, isolated on plain solid magenta (#ff00ff), nothing else. File name: place_store.png."

Add: "If your tool can produce a real transparent background, use that instead of magenta."

## Prompt 3: two roads

Attach `layout-guide.png`; one prompt each.

Use the small per-road guides instead of the big layout guide: attach `road_school_guide.png` for the School road and `road_store_guide.png` for the Store road (one prompt each). "Attached is a guide picture with a red line, a blue dot at the start and a green dot at the end. Draw ONE natural dirt footpath for a children's game map that runs exactly along the red line, from the blue dot to the green dot: the middle of the path must lie on the red line the whole way, and the path must have the same overall direction and the same gentle bends as the line (do not add extra curves, loops or S-bends, and do not flip it). The path is about 180 px wide in this picture, worn earth with a few tiny pebbles and a few grass tufts along the edges, soft irregular edges that blend into grass. Both ends are rounded and fade softly into grass exactly at the two dots (do not run the path off the picture edge). Draw only the path on a plain solid magenta (#ff00ff) background, the picture exactly the same size and shape as the guide (2:1 for the School road, 9:7 for the Store road), nothing else: no red line, no dots, no buildings, no arrows, no text, no bushes, flowers or stones lying across the path. Same soft polished 3D-look children's game illustration style as the rest. File name: road_school.png (or road_store.png)."

## Checklist

- the backdrop has no roads or buildings, an open uncluttered middle, and no big tree or bush next to a yellow rectangle or a blue square
- nothing in the scenery looks like a building
- the three buildings are isolated, similar in visual size and detail
- each road follows its red line and nothing crosses it
- if a picture is wrong, ask ChatGPT for a new one with the same prompt (never ask us to move the game's positions or routes).
