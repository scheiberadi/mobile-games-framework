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

## Fishing spec (built 2026-10-03, not yet seen on a phone)

- Same frame as Whack-a-Mole: levels 1-6 in a row, no target, every fish counts, no loss, `levelup` sound between levels, seamless carry-over of the fish still swimming, 1 coin at the end (also when the child leaves after at least one catch). Eva: "Catch the fish!" (`fishing_find`), repeats after 8 s of quiet, after 14 s the oldest fish pulses.
- Mechanic: the whole screen is a touch pad. While a finger is down a hook (`arcade/prop_hook`, on a thin line from the top) hangs 70 units above the finger; it stays in the water 0.25 s after the finger lifts so a quick tap still counts. A fish whose middle is within 120 units of the hook tip is caught: it leaps out in an arc with a sparkle burst.
- Fish (`arcade/fish_*`, 6 colours) surface in a pond (`world/pond` over `world/map_bg`), swim to and fro with a wobble (they face the way they swim), and dive away on their own (never a mistake).
- Pace per level: hits `{5, 8, 12, 18, 25, 35}`, max fish up `{2, 3, 3, 4, 5, 6}`, speed `{70..190}` u/s, life seconds `{9..5}`, spawn gap `{1.4..0.5}`. Initial tuning values.
- Logic `Rules/Fishing.cs` (`FishingDirector`), screen `App/Screens/FishingScreen.cs`, tests `Tests/FishingTests.cs`.

## Space Shooter spec (built 2026-10-03, not yet seen on a phone)

- Calm version for age 4-5, same frame as above: no target shape, every shape counts, no loss, 1 coin, Eva: "Blast the shapes!" (`spaceshooter_find`).
- Mechanic: the whole screen is a touch pad; the rocket (`arcade/ship_star`) glides to the finger's x (like the Fruit Catcher basket) and fires a star (`sciencelab/space_star`) straight up by itself every 0.45 s (0.30 s at level 6). There is no fire button: the child only steers. A shape (`arcade/shapecard_*`, 6 shapes) that a star touches pops in a burst of confetti (`arcade/prop_pop`); shapes drift down with a slow sway and a shape that is not hit just fades away at the bottom.
- Pace per level: hits `{8, 12, 18, 26, 36, 50}`, max shapes up `{2, 3, 4, 5, 6, 8}`, fall seconds `{7..4}`, spawn gap `{1.3..0.4}`, shot gap `{0.45..0.30}`.
- The night sky is drawn in code (gradient, twinkling stars, faint moon and Saturn from the Science Lab art); no space background image exists yet.
- Logic `Rules/SpaceShooter.cs` (`SpaceShooterDirector`), screen `App/Screens/SpaceShooterScreen.cs`, tests `Tests/SpaceShooterTests.cs`.
