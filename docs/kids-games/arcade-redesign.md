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
