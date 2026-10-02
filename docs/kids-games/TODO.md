# Eva's Learning World - master to-do (2026-10-01)

State: all branches (M4, M5, answer-variety, eva-m1) merged into `claude/eva-m4-full-content`, also pushed to
`claude/eva-m5-character-system`. 734/734 EditMode tests pass; debug APK builds clean. Older `claude/eva-m4-full-content-*`
branches are fully contained (nothing to merge). Phone was disconnected after the final build, so this merged build is
not yet installed.

## A. Test on phone (nothing below has been seen running)
- [x] Install merged APK; walk to every moved building (placement + no stone paths). Third placement pass done 2026-10-01 (Art Studio under Tree House, characters never cover buildings); installed and seen on phone.
- [ ] M5: CreatorScreen (paged category rail, dice randomize), Dress the Character live preview, joy reactions (feel/frequency; tuning knobs in `JoyReactions`).
- [x] M5 Task 6 spike: seen on phone 2026-10-02, Gate 2 approved (Corner + Side, Eva shrinks to the pair's Eva). Side's feet were raised from y -190 to -90 so it clears the Dress for Occasion / Pack a Suitcase shelf - **not yet seen on the phone**.
- [ ] M5 Task 7 rollout (code done 2026-10-02, 46 generic screens on the pair): look at it on the phone - Count (all six levels), Free Drawing (Clear/Home buttons moved to the left column), Dress for Occasion, Pack a Suitcase, one Match screen, one Sequence screen.
- [ ] Answer-variety Prototype B: Sorting as drop-sort (code, tests, voice and art done, on the phone since 2026-10-02, waiting for the user verdict, see section B).
- [x] Hud polish 2026-10-02 (on the phone): Home + Back (to the building game list), plate-less AI icons (home/gear/back/piggybank, drawn 60 units inside the 240 tap area), piggy bank beside the coin number, coins fly into the piggy (earn) and out of it (spend). Earn and spend coin flights confirmed by the user on the phone.
- [x] 3D map buildings House/School/Store imported (`ab1cc77`). Sorting verdict: "acceptable for now".
- [x] Brain Gym Batches 19 (Recycling), 13 (icons), 20 (category items), 21 (chores) imported, not yet used by any screen (rollout step 6).
- [x] Answer-variety Prototype A: Item to Shadow as drag-to-target - drag, snap, wrong-drop spring-back, rounds and coins work on phone; user: looks very good, hover ring made thinner (`icons/ring_thin`). Hint/voice not yet judged.
- [ ] Voice: new lines `itemtoshadow_drag*` and the M5 "keep this look?" line.

## B. Game variety (plan: `answer-variety-plan.md`, order approved)
- [x] 1. Item to Shadow DT prototype (code done, needs phone test)
- [x] 2. Sorting DS prototype - code, tests and voice done 2026-10-02 (`DropSortScreen`, `Rules/DropSort.cs`, `EvaGame.SortingUsesDrop`); art imported (Batch 18, `dc46606`) and played on the Galaxy S25 Ultra. Step 4 so far: bin count badge now sits on a light disc, held copies sit under the bin so they never hide its picture. User verdict: "acceptable for now".
- [ ] 3. Phone evaluation of both -> 4. adjust shared presenters
- [ ] 5. Small conversions with existing `DragItem`
- [ ] 6. Roll DT/DS out to more games (DS done in code 2026-10-02 for Recycling, Match Item to Category, Sort Laundry/Chores: `DropSortRoundBuilder` catalogues, art Batches 19/13/20/21; Recycling seen on the phone (bins, drop, count badge OK), the other two not yet; these catalogues have one item for some categories so rounds can be shorter than the level table) -> 7. Hotspot presenter -> 8. Arcade real-time (open question: calmer version for age 4-5?) -> 9. Paint and one-offs
- [ ] Some already-planned art sheets must change (DT/DS need separate cut-outs and target/bin art) - check before generating more gameplay art.
- [ ] **Arcade: ALL 7 games must be rewritten as real arcade games (user, 2026-10-02: every Arcade game is currently a grid of answers to pick from, which is wrong).** Whack-a-Mole must be an actual whack-a-mole: moles pop up out of holes in real time and the child taps them before they hide (tap/hit feedback, no choose-the-matching-tile grid); the learning content rides on top (e.g. only whack the right ones). Same for Balloon Popping (balloons rise, tap to pop), Fishing (drag the hook to a fish), Space Shooter (aim and shoot, check suitability for age 4-5), Fruit Catcher (drag the basket under falling fruit), Treasure Hunt (dig where the clue says), Platformer (tap to jump, check suitability). Needs the real-time presenter (step 8 of `answer-variety-plan.md`), calmer pace for age 4-5, and probably new art (holes, moles, hammer/hit effect); check prompts in `art/eva/arcade/PROMPTS.md` against this before generating anything. Do not spend time on the current Arcade grid screens.

## C. M4 art (you generate in ChatGPT, I import; I paste prompts in chat)
- [x] 121/123 activity icons, 16 scene backgrounds, building sprites, Playground + Zoo&Farm gameplay art
- [ ] `listen_and_choose` icon (1 missing)
- [x] Science Lab gameplay art batches 14-18 imported (Day/Night x2, Space, Plant Growth, bins + orbit backdrop). Plant stage 4/5 are bud/open flower (no fruit)
- [x] Workshop and Arcade (batches 1-8) imported
- [x] Art Studio, Brain Gym, Friends' Park prompts written (`art/eva/<building>/PROMPTS.md`); not yet generated (no ChatGPT access as of 2026-10-02)
- [ ] Re-check prompts against the variety plan before generating Workshop/Arcade/etc. (Arcade art will change with the real-time rewrite, see section B)
- [x] 2026-10-02: Brain Gym batches 1-12 and 14-17 (+ derived sprites via `tools/art-import/derive-braingym.js`), support sprites (maze, trace, slots, pond, drawing background), emotion icons and `listen_and_choose` imported (`b139aef`). NOT yet seen on the phone.
- [x] Friends' Park: friendly cartoon children (user ruling 2026-10-02). All 10 sheets (faces, emotion icons, 4 scenario/response sheets, listen, details, actions, road safety) imported 2026-10-02: 84 sprites in `Resources/Art/friendspark`. Not yet seen on a device.
- [x] Jigsaw piece keys per grid size fixed 2026-10-02 (`tools/art-import/cut-jigsaw.js`, `grid<c>x<r>_<row>_<col>` sprites); look at levels 1-4 on the phone.

## D. M5 art (character system)
- [ ] Reference set (boy 7 + girl 6 items) FIRST, confirm vs `art/character/STYLE.md`
- [x] v1 wardrobe batch (46 images) imported and wired into RigFactory/CharacterRig/CreatorScreen; Creator and Dress the Character not yet checked on the phone, EyeColor has no visible effect (the faces have closed eyes)
- [x] Eva cat: approved reference used as ONE whole sprite (`tools/art-import/key-cat-whole.js`), blink + open mouth as overlays (`make-cat-overlays.js`)
- [ ] Eva cat later: correct/animate the mouth and tail
- [ ] Missing `icons/dice` art

## E. M5 development (plan: `docs/superpowers/plans/2026-09-27-m5-character-system.md`)
- [x] Tasks 1, 2 (approved), 4 (code); 3, 5, 6 written but unvalidated
- [ ] Validate Tasks 3/5 on phone. Task 7 code done (see section A); Tasks 8-10 not started.
- [ ] Ceiling counts (Task 8): boys 20 tops/5-10 pants/10 shoes/10 haircuts/10 glasses; girls 20 tops/20 pants-skirts/20 dresses/10 shoes/20 haircuts/10 glasses

## F. M6
- [ ] Map path visuals: gravel look (tiled texture strip likeliest). Stone paths currently switched off in `MapScreen`.
- [ ] Daily Adventure (deferred meta feature), Parent progress view (unassigned)

## G. M7
- [ ] QA and release; localization (moved here from M4), store assets

## Sound (2026-10-02)
- [x] 11 synthesized effects in `Resources/Sfx` (`node tools/sfx/generate.js`): tap, pick, drop, place, right, retry, coin, buy, win, hint, pop. Pick/drop on every drag, hint chime on every help-ladder hint, win fanfare + confetti on every EndPanel (`WinCelebration`). User confirmed tap sound and confetti ok on the phone.
- [x] Seven buildings had no spoken line (tapping them only made the tap sound); lines + clips added, `PlaceVoiceTests` guards it. `Voice.Say` logs "Voice: no clip for key" for any missing clip (grep logcat while playing).
- [ ] QA: Arcade Balloon Popping round with target 3 shows only 1- and 2-balloon groups; Simon Says 1 has a single tile; Friends' Park scenario pictures and the traffic light are small on screen.
- [ ] **Dress the Character (user, 2026-10-02): drag the clothes onto the CHARACTER, not into the four anonymous squares (the child has to guess which square takes which item, and an item dropped elsewhere just floats).** Rework `DressTheCharacterScreen` (and check `DressForOccasionScreen`, `PackASuitcaseScreen` for the same slot-guessing) so any piece dropped on the character goes to its right place. Also: the bare base body has no underwear; the body art needs simple briefs before it is shown undressed (new prompt for `art/character/PROMPTS.md` Prompt 3).
