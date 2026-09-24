# Eva's House: multi-room cross-section (sub-project 1 of 3)

Date: 2026-09-24. Status: design, awaiting review. Milestone context: replaces M3 Task 7 as scoped in `docs/superpowers/plans/2026-09-24-eva-m3-real-cat.md` (the user explicitly asked for a real room-based house and shop).

## Goal

Replace the two-room, seven-slot House screen with a doll-house style cross-section: three levels, nine rooms, stairs, balcony, front door and windows. The child sees the whole house, taps a room to zoom into it, moves between rooms, and decorates by dragging owned furniture into fixed slots, as today.

## Sub-project split

1. **The house (this spec).** Shell, rooms, navigation, per-room slots, save migration. Furniture stays the current 7 items and the current Store.
2. **The shop (later spec).** Browse by room type, pages, prices rising with depth, room-restricted items.
3. **Furniture art (later, in batches by room).** Many items per room type.

Also in the backlog, not in this spec: map rework (multiple zones, several games each) and a School game-list screen.

## Layout

| Level | Rooms (left to right) |
|---|---|
| Attic | party room, playroom, (roof terrace, art only) |
| Upper floor | parents' bedroom, kids' bedroom, bathroom; balcony off the bathroom side |
| Ground floor | living room, dining room, kitchen; front door on the living room side |

Stairs are drawn between levels as shell art. Movement between levels is by the navigation buttons, not by walking the stairs.

## Design

### Data (Rules layer, pure C#)

- `HouseRoom { Id, Type, Level, Column, Left, Right, Up, Down }` in a static `HouseRooms.All` list. The four neighbour fields are room ids or null and are written out explicitly, so the navigation graph is data, not computed geometry.
- `HouseSlot` already has `Id`, `Room`, `Kind`. Slots become 3-4 per room across all nine rooms, using the existing `SlotKind`s (Bed only in bedrooms).
- `HouseLayout` (item id to slot id) is unchanged; `CanPlace` still checks kind and ownership. No room restriction yet (that arrives with the shop).
- Slot ids keep the `room_name` style. Existing ids `living_*` stay valid. The old `bedroom_*` ids move to the kids' bedroom and are renamed `kids_*`; `SaveStore.Load` remaps the prefix so an existing save keeps its placements.

### World and camera (App layer)

- All rooms live in one world container. Each room is a 1440 x 900 panel in room-local coordinates, the same coordinates the current House uses, so today's slot geometry rules (slot size 260, items inside the on-device frame) carry over per room.
- The camera is the container's scale and position: overview scale is about 0.31 (whole house plus margin fits the frame); zoomed scale is 1 centred on one room. Transitions ease over about 0.3 s.
- Placed furniture is drawn in world space, so it is visible in the overview at small size and full size when zoomed.

### Views

- **Overview** (default on entering the House): whole cross-section, each room tappable (tap area is the whole room panel). No dragging of furniture, no tray.
- **Room view**: zoomed into one room. Big arrow buttons (at least 240 tap size) for each existing neighbour, a house button back to the overview. Swiping on empty background moves to the neighbour in that direction. The tray of unplaced owned items shows along the bottom, as now. A drag that starts on a furniture piece moves the piece and never triggers a swipe.
- While the tutorial's place-the-sofa step is active, the House opens zoomed into the living room so the existing guide has a slot to point at.

### Shell art

Separate clean layers, same style as the School scene: house body and roof, floor slabs, stairs, balcony, front door, exterior windows, and one background panel per room type. Room panels come from one generator script with per-type wall and floor colours plus a couple of type-specific decorations, so nine rooms do not need nine hand-drawn images.

### Content in the first slice

All nine rooms exist and are reachable. Each has 3-4 slots. Furniture is the current 7 items (any owned item can go in any slot of matching kind, in any room). The Store is unchanged.

## Testing

- Room and slot data: every slot belongs to a real room, ids are unique, every room has 3-4 slots, bedrooms only have Bed slots where intended.
- Navigation graph: every room is reachable from every other, and neighbour links are symmetric (A's Right is B implies B's Left is A, same for Up/Down).
- Save: a save with `bedroom_*` placements loads as `kids_*`; a round trip keeps placements; unknown slot ids are dropped without error.
- Screen tests: overview shows nine tappable rooms; tapping a room enters it; the arrows shown match the room's neighbours; a placement made in a room appears in the overview; the existing tutorial place-the-sofa flow still passes.
- Touch feel, zoom transition, room arrangement and art are judged on the phone by the user.

## Out of scope for this slice

Room-restricted furniture, more or new furniture, the paged shop, more slots than 4 per room, pinch or free pan, walking between floors by the stairs, any change to the Map screen or School.

## Risks

- The tutorial guide points at slot rectangles; it must account for the camera (use world positions after zoom). Covered by keeping its existing test green.
- Drag maths under a scaled container: drag deltas must be divided by the container scale. Covered by a test that a drop lands in the intended slot at zoom 1 and by a phone check.
- Room art volume: mitigated by the single generator script.
