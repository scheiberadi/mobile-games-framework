# Eva's Learning World - master to-do (2026-10-01)

State: all branches (M4, M5, answer-variety, eva-m1) merged into `claude/eva-m4-full-content`, also pushed to
`claude/eva-m5-character-system`. 734/734 EditMode tests pass; debug APK builds clean. Older `claude/eva-m4-full-content-*`
branches are fully contained (nothing to merge). Phone was disconnected after the final build, so this merged build is
not yet installed.

## A. Test on phone (nothing below has been seen running)
- [ ] Install merged APK; walk to every moved building (placement + no stone paths).
- [ ] M5: CreatorScreen (paged category rail, dice randomize), Dress the Character live preview, joy reactions (feel/frequency; tuning knobs in `JoyReactions`).
- [ ] M5 Task 6 spike: `CompanionPair` layouts (PC checklist in `docs/superpowers/spikes/character-everywhere.md`) - Gate 2 not approved.
- [ ] Answer-variety Prototype A: Item to Shadow as drag-to-target (acceptance criteria in `answer-variety-prototypes.md` 1.7).
- [ ] Voice: new lines `itemtoshadow_drag*` and the M5 "keep this look?" line.

## B. Game variety (plan: `answer-variety-plan.md`, order approved)
- [x] 1. Item to Shadow DT prototype (code done, needs phone test)
- [ ] 2. Sorting DS prototype
- [ ] 3. Phone evaluation of both -> 4. adjust shared presenters
- [ ] 5. Small conversions with existing `DragItem`
- [ ] 6. Roll DT/DS out to more games -> 7. Hotspot presenter -> 8. Arcade real-time (open question: calmer version for age 4-5?) -> 9. Paint and one-offs
- [ ] Some already-planned art sheets must change (DT/DS need separate cut-outs and target/bin art) - check before generating more gameplay art.

## C. M4 art (you generate in ChatGPT, I import; I paste prompts in chat)
- [x] 121/123 activity icons, 16 scene backgrounds, building sprites, Playground + Zoo&Farm gameplay art
- [ ] `listen_and_choose` icon (1 missing)
- [ ] Science Lab gameplay art batches 14-17 (Day/Night x2, Space, Plant Growth) - prompts in `art/eva/sciencelab/PROMPTS.md`
- [ ] Workshop (6 sheets/61 imgs) and Arcade (7 sheets/74 imgs): prompts written (`art/eva/workshop|arcade/PROMPTS.md`), nothing generated
- [ ] Art Studio, Brain Gym, Friends' Park: prompts NOT written yet (I write them first)
- [ ] Re-check prompts against the variety plan before generating Workshop/Arcade/etc.
- [ ] Known deferred bug: `Rules/Jigsaw.cs` reuses piece sprite keys across grid sizes (wrong crops at low levels)

## D. M5 art (character system)
- [ ] Reference set (boy 7 + girl 6 items) FIRST, confirm vs `art/character/STYLE.md`
- [ ] Then v1 wardrobe batch: 7 sheets / 46 images (`art/character/PROMPTS.md`)
- [ ] Eva cat redo: reference illustration + 2 part sheets (`art/eva/cat-v2/PROMPTS.md`)
- [ ] Missing `icons/dice` art; face/haircut ids not wired yet
- [ ] Import everything (`compose-cat-parts.js` for the cat layers)

## E. M5 development (plan: `docs/superpowers/plans/2026-09-27-m5-character-system.md`)
- [x] Tasks 1, 2 (approved), 4 (code); 3, 5, 6 written but unvalidated
- [ ] Validate Tasks 3/5/6 on phone, then Task 7 and Tasks 7-10 (not started)
- [ ] Ceiling counts (Task 8): boys 20 tops/5-10 pants/10 shoes/10 haircuts/10 glasses; girls 20 tops/20 pants-skirts/20 dresses/10 shoes/20 haircuts/10 glasses

## F. M6
- [ ] Map path visuals: gravel look (tiled texture strip likeliest). Stone paths currently switched off in `MapScreen`.
- [ ] Daily Adventure (deferred meta feature), Parent progress view (unassigned)

## G. M7
- [ ] QA and release; localization (moved here from M4), store assets
