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

House (attach `EvasLearningWorld/Assets/Eva/Resources/Art/house/shell.png`, the doll house cutaway): "Attached is the cutaway of a two-storey doll house. Draw the OUTSIDE of this same house, seen from the front and slightly from above: exactly two storeys, the same steep gabled roof with red-orange scalloped tiles and the same brick chimney on the left side of the roof, cream walls with a light brown base. Ground floor: a round front door in the middle and two windows. Upper floor: three windows in a row. A tiny flower box under one window. It must look like the outside of that exact doll house. The whole building isolated on a plain solid magenta (#ff00ff) background with nothing else, centred, filling most of the picture with only a small margin (aspect about 8:7). It will be shown at a similar visual size to a school and a shop, so keep the detail level and mass similar. File name: place_house.png."

School: "A small friendly school building with a bell tower and a flag, yellow walls, big door, front view slightly from above, isolated on plain solid magenta (#ff00ff), nothing else. File name: place_school.png."

Store: "A small shop with a striped awning, a big shop window and a wooden door, front view slightly from above, isolated on plain solid magenta (#ff00ff), nothing else. File name: place_store.png."

Add: "If your tool can produce a real transparent background, use that instead of magenta."

## Prompt 3: two roads

Attach `layout-guide.png`; one prompt each.

"Attached is a layout guide. Draw only the dirt path shown by the red curved line inside the blue box labelled 'School road picture box' (for the Store road use the other blue box). The path is a natural, soft, slightly wobbly worn-earth footpath with tiny pebbles and a few grass tufts along the edges, about 80 px wide, that follows the red line exactly from one end to the other (the game's walking route is that red line, so the path must run along it, not near it), with soft irregular edges that will blend into grass. Draw it isolated on a plain solid magenta (#ff00ff) background, the picture cropped exactly to the blue box (aspect 2:1 for the School road, 9:7 for the Store road), no buildings, no arrows, no lines, no dots, no text, no bushes, flowers, stones or other decoration lying across the path. The path must look like part of the landscape, never like a UI connector. File names: road_school.png and road_store.png."

## Checklist

- the backdrop has no roads or buildings, an open uncluttered middle, and no big tree or bush next to a yellow rectangle or a blue square
- nothing in the scenery looks like a building
- the three buildings are isolated, similar in visual size and detail
- each road follows its red line and nothing crosses it
- if a picture is wrong, ask ChatGPT for a new one with the same prompt (never ask us to move the game's positions or routes).
