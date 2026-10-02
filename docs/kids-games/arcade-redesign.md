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

## Whack-a-Mole spec

- Per level (index 1-6): moles up at the same time at most `{1, 2, 2, 3, 4, 5}`; seconds a mole stays up `{3.0, 2.6, 2.2, 1.9, 1.6, 1.3}`; seconds between spawns `{2.0, 1.7, 1.4, 1.2, 1.0, 0.8}`; chance a spawned mole is a decoy `{0, 0.25, 0.4, 0.5, 0.55, 0.6}`; hits per round `{4, 4, 5, 5, 6, 6}`. These are initial tuning values, to be judged on a device.
- Level 1 is one mole at a time except for a "teaching pair" (30% of spawns): a target plus one decoy at once. This is a deliberate deviation from "one visible at a time": a lone decoy would break the invariant below, and the pair is the only way to keep both.
- Invariant: whenever any mole is up, at least one target is up (a spawn is forced to be a target if none is up; if the last target is gone and only decoys remain, a target is brought up at once and, if the board is full, the soonest-leaving decoy hides to make room).
- Decoys: only moles that look clearly different from the target at the size they are drawn (table `DistinctFrom` in `WhackAMoleDirector`). Brown/red/tan and grey/spotted grey are never paired, at any level.
- A mole that hides on its own is not a mistake. A wrong whack = tapping a decoy (it shakes its head and stays up).
- Help: wrong whack 1 = shake only; 2 = Eva repeats the target; 3 = Eva repeats and the targets pulse (ladder "demonstrated"). Idle: after 8 s Eva repeats the target, after 14 s a visible target pulses. Nothing is ever whacked for the child.
- Art in use: `arcade/mole_<a-f>`, `arcade/molecard_<a-f>`, `arcade/prop_mole_hole`, `arcade/prop_sparkle`. No hammer art: a hit squashes the mole and sparkles.
- Coins: 1 at the end of the session (see rules above).
