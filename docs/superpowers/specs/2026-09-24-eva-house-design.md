# Eva's House: multi-room cross-section (sub-project 1 of 3)

Date: 2026-09-24. Status: design, reviewed with adjustments applied (see Review decisions). Milestone context: replaces M3 Task 7 as scoped in `docs/superpowers/plans/2026-09-24-eva-m3-real-cat.md` (the user explicitly asked for a real room-based house and shop).

## Goal

Replace the two-room, seven-slot House screen with a doll-house style cross-section: three levels, eight rooms, stairs, balcony, front door and windows. The child sees the whole house, taps a room to zoom into it, moves between rooms, and decorates by dragging owned furniture into fixed slots, as today.

## Sub-project split

1. **The house (this spec).** Shell, rooms, navigation, per-room slots, save migration. Furniture stays the current 7 items and the current Store.
2. **The shop (later spec).** Browse by room type, pages, prices rising with depth, room-restricted items.
3. **Furniture art (later, in batches by room).** Many items per room type.

Also in the backlog, not in this spec: map rework (multiple zones, several games each) and a School game-list screen.

## Layout

| Level | Rooms (left to right) |
|---|---|
| Attic | party room, playroom; the third attic bay is a roof terrace, art only (not a room: no slots, not tappable, not in `HouseRooms`) |
| Upper floor | parents' bedroom, kids' bedroom, bathroom; balcony off the bathroom side |
| Ground floor | living room, dining room, kitchen; front door on the living room side |

Stairs are drawn between levels as shell art. Movement between levels is by the navigation buttons, not by walking the stairs.

**Room count, defined once:** exactly **8 navigable, decoratable rooms**. The roof terrace is scenery. `HouseRooms.All` has 8 entries and every test uses 8.

## Design

### Data (Rules layer, pure C#)

- `HouseRoom { Id, Level, Column, Left, Right, Up, Down }` (`Id` is also the room type; there is one room of each type) in a static `HouseRooms.All` list. The four neighbour fields are room ids or null and are written out explicitly, so the navigation graph is data, not computed geometry.
- `HouseSlot` already has `Id`, `Room`, `Kind`. Slots become 3-4 per room across all eight rooms (24-32 slots total), using the existing `SlotKind`s (Bed only in bedrooms).
- `HouseLayout` (item id to slot id) is unchanged; `CanPlace` still checks kind and ownership. No room restriction yet (that arrives with the shop).
- Slot ids keep the `room_name` style. Existing ids `living_*` stay valid. The old `bedroom_*` ids move to the kids' bedroom and are renamed `kids_*`; `SaveStore.Load` remaps the prefix so an existing save keeps its placements.

### World and camera (App layer)

- All rooms live in one world container. Each room is a 1440 x 900 panel in room-local coordinates, the same coordinates the current House uses, so today's slot geometry rules (slot size 260, items inside the on-device frame) carry over per room.
- The camera is the container's scale and position: overview scale is initially 0.25 (whole shell incl. roof, balcony and door fits the frame; provisional, see review decisions); zoomed scale is 1 centred on one room. Transitions ease over about 0.3 s.
- Placed furniture is drawn in world space, so it is visible in the overview at small size and full size when zoomed.

### Views

- **Overview** (default on entering the House): the child's "that's my house" view and nothing more than a room chooser. Whole cross-section visible, each room clearly tappable (whole room panel is the tap area). No furniture dragging, no tray. Tapping a room gives an obvious immediate response (short highlight/pop of that room), then the camera zooms into it.
- **Room view**: zoomed into one room. Big arrow buttons (at least 240 tap size) for each existing neighbour and a house button back to the overview are the only navigation. **No swipe navigation in this slice**; it can be added later as separate polish if arrows prove cumbersome on the phone. The tray of unplaced owned items shows along the bottom, as now.
- While the tutorial's place-the-sofa step is active, the House opens zoomed into the living room so the existing guide has a slot to point at.

### Shell art

Separate clean layers, same style as the School scene: house body and roof, floor slabs, stairs, balcony, front door, exterior windows, and one background panel per room type. Room panels come from one generator script. They are child-readable environments, not coloured rectangles: a pre-reader must recognise each room type without text. Each type gets wall and floor colours plus a few type-specific environmental elements baked into the panel (not bespoke art passes): kitchen (counter and cupboards, hob, hanging pots), bathroom (tiled wall, window, mirror, towel rail), parents' and kids' bedrooms (headboard-side window, curtains, bedroom wall details; kids' with bunting or toys on a shelf), living room (fireplace or wall frames, curtains), dining room (chandelier, wall frames), playroom (colourful mat, toy shelf), party room (bunting, balloons, disco lights). Empty rooms must look intentionally designed, not unfinished, since only 7 furniture items exist yet.

### Content in the first slice

All eight rooms exist and are reachable. Each has 3-4 slots (no inventory, filtering or dynamic slot framework). Furniture is the current 7 items (any owned item can go in any slot of matching kind, in any room). The Store is unchanged.

## Testing

- Room and slot data: every slot belongs to a real room, ids are unique, every room has 3-4 slots, bedrooms only have Bed slots where intended.
- Navigation graph: every room is reachable from every other, and neighbour links are symmetric (A's Right is B implies B's Left is A, same for Up/Down).
- Save: a save with `bedroom_*` placements loads as `kids_*`; a round trip keeps placements; unknown slot ids are dropped without error.
- Screen tests: overview shows eight tappable rooms; tapping a room enters it; the arrows shown match the room's neighbours; a placement made in a room appears in the overview; the existing tutorial place-the-sofa flow still passes.
- Phone review is a real product gate; passing tests is not enough. The user judges: whole-house overview readability at the ~0.31 scale, whether rooms are visually distinct without text, whether the zoom feels natural, whether the navigation buttons are obvious to a 4-5-year-old, whether furniture stays correctly positioned at both overview and room scale, and whether it feels like a charming dollhouse rather than a technical room selector.

## Out of scope for this slice

Room-restricted furniture, more or new furniture, the paged shop, more slots than 4 per room, swipe navigation, pinch or free pan, walking between floors by the stairs, any change to the Map screen or School.

## Risks

- The tutorial guide points at slot rectangles; it must account for the camera (use world positions after zoom). Covered by keeping its existing test green.
- Drag maths under a scaled container: drag deltas must be divided by the container scale. Covered by a test that a drop lands in the intended slot at zoom 1 and by a phone check.
- Room art volume: mitigated by the single generator script and a small fixed set of per-type decorations.

## Review decisions (2026-09-24, second round)

- Overview scale 0.25 is provisional (initial value that fits the shell); the phone review may change camera and shell composition.
- The 31 slots are the initial M3 placement capacity, not a permanent maximum; later work may add capacity or decorative non-slot furniture (not now, no slot/inventory framework).
- Overview furniture only needs to convey a decorated house; reuse existing sprites, adjust sizing if unreadable, no separate rendering system.
- `HouseRoom.Id` stays the room type; no `Type` field.
- Navigation layout is provisional and judged by child usability; no swipe.
- Empty rooms must look intentionally designed through the per-type room environments.

## Review decisions (2026-09-24)

Room count fixed at 8 plus a non-room terrace; overview is a pure room chooser; swipe deferred; room backgrounds carry type-specific cues; 3-4 slots per room kept with no extra frameworks; static room graph, 1440x900 room-local coordinates and world-space furniture kept as specified.

## Implementation notes

- `HouseRoom` has no separate `Type` field; the room id is the type.
- Overview scale is 0.25 with focus (0, 290) (moved down from 150 so the attic-left room does not overlap the HUD home button); provisional, judged on the phone.
- Navigation layout: arrows left/right at mid-edge, up / overview / down along the top; provisional.
- The overlap audit ignores non-interactable buttons (room panels are inert in room view).
