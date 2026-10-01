# Task 6 spike: character-everywhere (player left, Eva right)

Status: **mechanism built, layouts PROPOSED, nothing measured or seen yet.** Written in a cloud container
with no Unity and no device, so this file does not claim the layouts work. Gate 2 (the user approving the
standard layouts) is still open and needs a look on a real screen.

## What exists

- `Rules/CompanionLayout.cs`: the standard layouts as data (player height/x, Eva height/x, feet y) plus an
  estimated footprint box per character and for the pair. Unit-tested (`Tests/CompanionPairTests.cs`): inside
  the 1440 x 900 frame, the two characters never overlap each other, player is left of Eva.
- `App/Ui/CompanionPair.cs`: the shared component a screen opts into with
  `CompanionPair.Create(Root, game, CompanionLayout.Corner)`. It builds the player rig (from
  `Progress.Look`) and Eva (mirrored to face the player), exposes `.Player` / `.Eva`, adds no TapTarget and
  turns off raycasts on everything, so it cannot swallow a tap.
- No screen uses it yet. The spike measures against the three test screens without modifying them.

## Proposed standard layouts (not yet confirmed)

| Layout | Characters | Placement | Intended for |
|---|---|---|---|
| Corner (default) | 200 tall each | bottom-right, feet at y -440; player x 450, Eva x 610 (footprint x 400..710, y -440..-240) | most screens |
| Open | 320 tall each | bottom-right, feet at y -440; player x 310, Eva x 560 | open screens like Free Drawing |

A third layout (for screens that are full-bleed or have a full-width bottom answer row) is deliberately not
invented yet: whether one is needed is exactly what the measurement below should show.

Footprint widths are estimates (player 0.5 x height, Eva 1.0 x height, generous on purpose), taken from
`RigFactory`'s constants, not from measuring the rendered art.

## How to get the real numbers (needs Unity)

Run `CompanionPairTests.ReportFootprintConflictsOnTheThreeSpikeScreens` in the Test Runner and read the
console. It prints, for Count at level 6 (six answer tiles), Jigsaw and Free Drawing, which visible TapTargets
each layout's footprint overlaps and by how much. Then look at the pairing on a device in each of those
screens: are the characters still recognisable at 200 tall, and are they ever in the way of a finger?

## Expected problem spots (reasoning only, not measured)

- **Count**: it already draws its own Eva (430 tall at x 627) and the six answer tiles sit below her feet, so
  a bottom-right pairing will very likely collide with the tile row. A retrofit would replace Count's own Eva
  with the pair's `.Eva` and probably needs a different arrangement (or Eva-only scale reduction); this is
  the screen that decides whether Corner can really be "the default".
- **Jigsaw**: pieces are dragged across the play area; the pairing must stay out of the drag region and
  trays. Unknown until seen.
- **Free Drawing**: expected to be fine, and the one place Open might be worth having.

## Estimate of rollout effort

Not possible yet. The plan asks for how many existing, finished screens take a standard layout unmodified
vs. need adjusting; that needs the measurement above run across the finished screens first.

## Decisions needed from the user (Gate 2)

1. Are 200 tall (Corner) and 320 tall (Open) the right order of size once seen on a device?
2. Bottom-right as the default home for the pair, or somewhere else?
3. Does Count keep its own bigger Eva as a special case, or does it adopt the pair?
