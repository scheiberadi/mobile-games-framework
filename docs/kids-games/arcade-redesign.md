# Arcade redesign: real arcade games (decided 2026-10-02)

Every Arcade game used to be a grid of answers to pick from (the generic MATCH screen with arcade art). User ruling: they must be real
arcade games. Learning content rides on top of the action: Eva says (and a card shows) the target, the child acts only on the target.

## Rules for all seven (user, 2026-10-02)

- Real time, calm pace for age 4-5. Six difficulty levels like every other game (`DifficultyLadder`): speed goes up and time-on-screen
  goes down with the level.
- **No loss at all**: no lives, no game over, no timer pressure. A wrong action only makes the object wobble or shake its head.
- A round ends after a fixed number of correct hits (4-6), a session is 3 rounds, each with a new target.
- **Coins: every Arcade game pays exactly 1 coin at the end of the session, however it went** (user, 2026-10-02). No per-round coins.
- Not catching or hitting something that goes away is never a mistake: no feedback, no penalty. Only a wrong action (hitting a decoy) gets the harmless shake.
- Idle help, in two steps, and it never plays the game for the child: first Eva repeats the target; if the quiet goes on, a visible correct object pulses. The child still does the tap.
- Difficulty ladder: the same `HelpLadder` as every other game. A wrong action is the only mistake; the 3rd one is "demonstrated" (not clean for `DifficultyLadder`). Coins are separate (above).
- Sounds from `Sfx` (pop, place, right, coin, hint, win); the win panel already has fanfare and confetti.

## Per game

| Game | Verdict | Mechanic |
| --- | --- | --- |
| Whack-a-Mole | **go, build first** | 9 holes (3x3). Moles pop out and hide again. Target = the mole shown on the card; others are decoys (shake head when hit). Level 1: one mole at a time, long stay, but now and then a target comes up together with one clearly different decoy, so the matching rule is taught from the start. Level 6: up to 5 up at once, short stay, fast spawns. |
| Balloon Popping | go | Balloons rise slowly; pop the ones that match the target (number of balloons / odd or even rule from the existing art). |
| Fruit Catcher | go | Drag the basket left-right under falling fruit, catch the target fruit; other fruit just falls through. |
| Fishing | prototype, user wants to see it first | Fish swim across; drag the hook over the target fish (colour). |
| Treasure Hunt | prototype, user is not sold | Tap the sand spot the clue points to; no real time. |
| Space Shooter | prototype, "calm" version | Drag the ship, tap to shoot, shoot only the target shape. |
| Platformer | prototype, "calm" version | Auto-run, tap to jump over gaps, collect the target. |

Build order: Whack-a-Mole, Balloon Popping, Fruit Catcher, then prototypes of Fishing, Treasure Hunt, Space Shooter, Platformer to
show the user before committing. This is step 8 of `answer-variety-plan.md` (real-time presenter).

## Whack-a-Mole spec (revised 2026-10-02, after playing it)

- No target, no decoys, no card: every mole counts. Eva says only "Whack the moles!" (repeats it after 8 s of quiet; after 14 s a mole pulses).
- One game = levels 1-6 in a row, always from level 1. Hits needed per level `{5, 8, 12, 18, 25, 35}`, and each level runs faster (a level lasts about 10-28 s if the child keeps up, so a faster level is never over sooner). The next level starts by itself; only the `levelup` sound says "faster now". The game ends after level 6 or when the child leaves. Saved level progress is not used.
- Pace per level: max moles up `{1, 2, 2, 3, 4, 5}`, stay seconds `{3.0, 2.6, 2.2, 1.9, 1.6, 1.3}`, spawn gap `{2.0, 1.7, 1.4, 1.2, 1.0, 0.8}`. Initial tuning values, judge on a device.
- A mole that hides on its own is never a mistake. No wrong whacks, no difficulty ladder.
- Coins: 1 at the end of the game (also when the child leaves after at least one hit).
- Art: `arcade/wam_hole` (empty wide hole, thin rim) and `arcade/wam_mole` (dirt-free bust). The mole slides up out of the hole's centre line (clipped by a RectMask2D) and the front half of the hole stays in front. Progress: a bar for the hits of the level plus six stars for the levels.

## Fishing spec (redesigned 2026-10-03 after the user saw the first version; not yet seen on a phone)

- The first version (a pond seen from above, fish "flying" in an upright pose, hook under the finger) was rejected: "nu-i ce trebuie". New design from the user's sketch: side view, the lake fills the lower part of the screen, the child and Eva sit in a boat on the surface at the top right, the child holds a rod.
- Mechanic: a tap in the water sends the hook (on its line from the rod tip) to that spot at a fixed speed (1000 u/s); it comes back (1200 u/s, 900 with a fish). A fish the hook touches anywhere on its body, on the way out or on the way back, is hooked, pulled to the boat, jumps in and is gone; a hook that touches nothing returns empty. One hook out at a time; a tap above the water sends it just under the surface. Fish never stop, so the child has to aim ahead.
- Fish: three sizes worth 1 / 2 / 3 points (widths 120 / 190 / 290), six colours (`fishing/fish_a..f`, side view facing right, mirrored when swimming left). Five rows from the surface down (y 105, -5, -120, -235, -350): small in the top two (row 2 also medium), medium in rows 3-4, large only in the bottom row; neighbouring rows swim opposite ways, every fish in a row has the same speed so none overlap. Fish enter beyond the screen sides and leave on the far side (never a mistake).
- Frame as the other real-time games: levels 1-6 in a row, no loss, counter = points of the level (bar + 6 stars, `ArcadeProgress`), points to pass `{8, 12, 18, 26, 36, 50}`, base speed `{150..230}`, spawn gap `{7..4.5}` s, max fish per row `{2, 2, 3, 3, 3, 4}`; `levelup` sound, fish and hook carry into the next level, 1 coin at the end (early-leave coin decided later with the economy). Eva: "Catch the fish!" (`fishing_find`), repeated after 8 s idle, after 14 s the fish nearest the boat pulses.
- Landscape is kept (a portrait switch would change the whole app): the water is 650 units tall, enough for five rows; the width is the swimming direction.
- Art (ChatGPT, imported 2026-10-03 with `cut-sheets.js` entries `fishing_fish`, `fishing_boat`, `fishing_props`, trimmed afterwards; sources in `art/eva/fishing/ai`): `fishing/bg` (horizon at 28% from the top), `fishing/boat`, `fishing/fish_a..f`, `fishing/rod` (mirrored in code, its dangling line erased: the game draws the line), `fishing/hook`. New `CompanionLayout.Boat` places the two in the boat; the boat is drawn over their legs.
- Logic `Rules/Fishing.cs` (`FishingDirector`), screen `App/Screens/FishingScreen.cs`, tests `Tests/FishingTests.cs`.

## Space Shooter spec (built 2026-10-03, not yet seen on a phone)

- Calm version for age 4-5, same frame as above: no target shape, every shape counts, no loss, 1 coin, Eva: "Blast the shapes!" (`spaceshooter_find`).
- Mechanic: the whole screen is a touch pad; the rocket (`arcade/ship_star`) glides to the finger's x (like the Fruit Catcher basket) and fires a star (`sciencelab/space_star`) straight up by itself every 0.45 s (0.30 s at level 6). There is no fire button: the child only steers. A shape (`arcade/shapecard_*`, 6 shapes) that a star touches pops in a burst of confetti (`arcade/prop_pop`); shapes drift down with a slow sway and a shape that is not hit just fades away at the bottom.
- Pace per level: hits `{8, 12, 18, 26, 36, 50}`, max shapes up `{2, 3, 4, 5, 6, 8}`, fall seconds `{7..4}`, spawn gap `{1.3..0.4}`, shot gap `{0.45..0.30}`.
- The night sky is drawn in code (gradient, twinkling stars, faint moon and Saturn from the Science Lab art); no space background image exists yet.
- Logic `Rules/SpaceShooter.cs` (`SpaceShooterDirector`), screen `App/Screens/SpaceShooterScreen.cs`, tests `Tests/SpaceShooterTests.cs`.

## Platformer = Bunny Run v2 spec (redesigned 2026-10-03 after the user saw v1: "nu-mi place apa, nici blocurile de pamant, prea monoton, iepurele are doar 2 imagini de fuga")

- Same frame as the other real-time games: levels 1-6 in a row, no loss, 1 coin at the end, `ArcadeProgress` bar. Eva: "Hop over the things!" (`platformer_prompt`).
- One continuous trail (no water, no gaps, no ground blocks). The bunny hops along by itself (a small continuous hop with crouch / push-off / tuck / fall / land poses), the ground and a slow background scroll past (strips are placed side by side with every second copy mirrored, so any strip joins seamlessly). A tap anywhere makes the bunny jump high; obstacles (low / medium / tall, 3 per world) lie on the trail.
- A bunny that bumps an obstacle stumbles (stumble pose) and is slid back about one second of running before it, then runs again; nothing is lost. Obstacles are forgiving: narrow hit box, and the jump keeps the bunny above even the tallest one for over half a second.
- Collectibles: carrots on the ground and in an arc over each obstacle (the arc follows the ideal jump), every third obstacle a golden star (worth 2) instead of the middle carrot. Carrots to pass per level `{18, 24, 30, 38, 46, 56}`, speed `{230..360}`.
- A new world per level (1 meadow, 2 forest, 3 autumn, 4 beach, 5 snow, 6 twilight): its own background, ground strip and 3 obstacles; a world with no art yet falls back to world 1. Flying friends (bluebird, butterfly, bee) drift across the sky.
- Art: PROMPTS.md Batch 13. Logic `Rules/BunnyRun.cs`, screen `App/Screens/PlatformerScreen.cs`, tests `Tests/BunnyRunTests.cs`.

## Treasure Hunt = metal detector (built 2026-10-03)

The child slides a metal detector over a sand beach (one screen). It beeps faster the closer it gets to something buried and goes continuous right over it; after half a second of continuous tone the spot is marked with a red X, and every tap digs (a number shows the taps, Eva does not count aloud) until the buried thing comes out. Treasures fly to the progress bar; junk (can, boot, bottle: sounds exactly like a treasure, so it is a surprise; 2 taps) is a funny find that does not count. Nothing is lost. 1 coin at the end like the other games.

Rules in `Rules/TreasureHunt.cs` (`TreasureHuntDirector`, pure logic), screen `App/Screens/TreasureHuntScreen.cs`, tests `Tests/TreasureHuntTests.cs`. The coil of the detector (not the fingertip, which is 90 units below) is what is measured. Each level starts with the detector at the spot farthest from everything buried.

| Level | Treasures | Junk | Hot zone | Hearing | Taps | Glow on the detector |
|---|---|---|---|---|---|---|
| 1 | 1 | 0 | 130 | 500 | 3 | yes |
| 2 | 1 | 0 | 115 | 470 | 4 | yes |
| 3 | 2 | 1 | 100 | 430 | 4 | no |
| 4 | 2 | 1 | 90 | 390 | 5 | no |
| 5 | 3 | 2 | 80 | 350 | 6 | no |
| 6 | 3 | 2 | 70 | 320 | 8 | no |

Initial tuning values, to be judged on a device. One beach for all levels for now (a world per level is possible later, same fallback idea as Bunny Run). The old MATCH-screen Treasure Hunt (`ArcadeMatchGameKind.TreasureHunt`) is dead code, like the other replaced games. Art: `art/eva/arcade/PROMPTS.md` Batch 15 (`Resources/Art/treasure/`), sounds `detbeep dethot dig`, voice `treasure_prompt/dig/found/junk`.
