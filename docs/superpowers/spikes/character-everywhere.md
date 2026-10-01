# Task 6 spike: character-everywhere (player left, Eva right)

## Status: CLOUD-SIDE SPIKE COMPLETE — UNITY/DEVICE VALIDATION PENDING

**Gate 2 is NOT approved.** This pass was done in claude.ai, which has no access to the Unity project or a
device build. Nothing here was compiled, no Unity Test Runner test was run, no rendered UI geometry was
measured, and none of the three spike screens was seen. "Architecturally prepared" below does not mean
"visually or gameplay validated". Everything in the "Estimated / unverified" and "Requires PC/Unity
validation" sections is open.

## Product requirement (unchanged, not weakened)

Every minigame shows both the player character and Eva, player on the left, Eva on the right. **Count is not
an exception.** When Count is eventually retrofitted, its existing standalone Eva is removed or replaced by
the shared pair's Eva, and Count may use a different size or position, but it still uses the shared
player + Eva pairing concept.

## What exists in the branch

- `Rules/CompanionLayout.cs`: the standard layouts as data (player height/x, Eva height/x, feet y) plus an
  estimated footprint box per character and for the pair.
- `App/Ui/CompanionPair.cs`: the shared component a screen opts into with
  `CompanionPair.Create(Root, game, CompanionLayout.Corner)`. Builds the player rig (from `Progress.Look`) and
  Eva (mirrored to face the player), exposes `.Player` / `.Eva`.
- `Tests/CompanionPairTests.cs`: structural checks on the layout data, a check that the pair is decorative,
  and a report test that prints footprint-vs-TapTarget overlaps for the three spike screens.
- No existing screen uses the pair. Count, Jigsaw and Free Drawing are untouched. No Task 7 rollout started,
  no new character art generated.

## Findings

### 1. Structurally established (by construction and by the encoded tests' assumptions; tests not yet run)

- A single shared pair mechanism that screens opt into, instead of per-screen hand-rolled rig placement.
- Ordering: player on the left, Eva on the right (encoded as `PlayerX < EvaX` in the layout data and checked
  by a test).
- The layout data model: height, x position and feet y per character, one footprint estimate per layout.
- Non-interactive presentation: the pair adds no `TapTarget`, and raycasts are disabled on every `Graphic`
  under it, so it cannot swallow input.
- The two proposed standard layouts, as numbers:

  | Layout | Characters | Placement |
  |---|---|---|
  | Corner (default) | 200 tall each | bottom-right, feet y -440; player x 450, Eva x 610 (estimated footprint x 400..710, y -440..-240) |
  | Open | 320 tall each | bottom-right, feet y -440; player x 310, Eva x 560 |

- The structural assumptions the current tests encode: each layout's estimated footprint lies inside the
  1440 x 900 frame, and the player and Eva footprints do not overlap each other.

These are structural properties of the data and code as written. They are proposals, not validated choices.

### 2. Estimated / unverified

- The actual rendered footprint of each character. Footprint widths are estimates (player 0.5 x height,
  Eva 1.0 x height, deliberately generous) derived from rig constants, not measured from rendered art.
- Whether 200 and 320 units feel visually appropriate in play.
- Whether bottom-right is the best practical placement.
- Whether Corner fits Count.
- Whether Jigsaw stays comfortable for drag interaction.
- Whether Open is needed beyond Free Drawing.
- Whether a third layout is necessary. **No third layout is proposed**, and none should be added unless Unity
  evidence shows Corner and Open are insufficient.
- How much existing-screen adjustment will be required, so no rollout effort estimate exists yet.

### 3. Requires later PC/Unity validation

- Compilation.
- Unity Test Runner execution.
- Actual rendered footprint measurement.
- Real overlap with gameplay and content regions (not only TapTargets).
- Touch and drag accessibility.
- Visual recognisability and perceived scale.
- Final layout assignment per screen.
- Rollout effort estimate.

## Screen hypotheses (hypotheses, not measurements)

- **Count**: hypothesis that Corner is likely to conflict with Count's bottom answer row, because Count
  already has its own larger Eva (430 tall at x 627) and the six answer tiles at higher levels sit below her
  feet. Count may need a smaller or specialised placement of the same shared pair. Unmeasured.
- **Jigsaw**: unverified spatial-fit case. Pieces are dragged through the play area, so the pair must stay
  out of the drag region and trays. Unknown until seen.
- **Free Drawing**: Open is the proposed candidate. Unverified until seen in Unity.

## PC validation checklist (do this when the branch is opened in Unity)

1. Compile the project.
2. Run the companion/layout tests (`CompanionPairTests`) and inspect the footprint-conflict report
   (`ReportFootprintConflictsOnTheThreeSpikeScreens` writes it to the console).
3. Instantiate the pair on Count, Jigsaw and Free Drawing.
4. Measure the actual rendered footprint rather than relying only on the estimated rig constants.
5. Check overlap with gameplay/content regions, not just TapTargets.
6. Verify comfortable touch/drag interaction.
7. Verify both characters remain visually recognisable at their actual in-game size.
8. Decide the final layout/size for each spike screen.
9. Determine whether Corner can remain the default.
10. Determine whether Open is actually needed.
11. Add a third layout only if the Unity evidence demonstrates Corner/Open are insufficient.
12. Only after that, estimate Task 7 rollout effort across the finished screens.

Gate 2 (user approval of the standard layouts) happens after this checklist, not before. Task 7 does not
start until then.
