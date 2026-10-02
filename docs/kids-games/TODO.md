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

## C. M4 art (you generate in ChatGPT, I import; I paste prompts in chat)
- [x] 121/123 activity icons, 16 scene backgrounds, building sprites, Playground + Zoo&Farm gameplay art
- [ ] `listen_and_choose` icon (1 missing)
- [x] Science Lab gameplay art batches 14-18 imported (Day/Night x2, Space, Plant Growth, bins + orbit backdrop). Plant stage 4/5 are bud/open flower (no fruit)
- [x] Workshop and Arcade (batches 1-8) imported
- [x] Art Studio, Brain Gym, Friends' Park prompts written (`art/eva/<building>/PROMPTS.md`); not yet generated (no ChatGPT access as of 2026-10-02)
- [ ] Re-check prompts against the variety plan before generating Workshop/Arcade/etc.
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
