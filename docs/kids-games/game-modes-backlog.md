# Kids learning app: game-mode backlog

Source: user-provided brainstorm list (2026-09-21). Working method: take modes one at a time, design and build each until the user is satisfied, then move to the next. Mark `[x]` only when the user says the mode is satisfying.

Design constraint: a 5-year-old must be able to open the app, understand what to do, play, make mistakes, recover and pick another game without reading instructions or asking a parent. Icons, voice instructions, demonstrations, tap/drag interactions, very little text.

Product status: adless, IAPless for now. Families Policy compliance applies (see the handoff of 2026-09-20).

## Status legend
- `[ ]` not started, `[~]` in design or build, `[x]` accepted by user

## 1. Logic and classification
- [ ] Sort by size
- [ ] Sort by type
- [ ] Recycling (sort waste)
- [ ] Match item to category
- [ ] Animal to habitat
- [ ] Animal to mother
- [ ] Item to shadow
- [ ] Odd one out (obvious category, then colour, function, habitat, abstract)
- [ ] What's missing (e.g. dog cat mouse dog cat _)
- [ ] Complete the pattern (colour/shape/fruit patterns, later AABAAB style)
- [ ] Sequence ordering (seed-plant-flower, baby-child-adult, morning-afternoon-night, caterpillar-butterfly, dirty-wash-clean)
- [ ] Which doesn't make sense (cow in ocean, fish in tree, polar bear in desert)

## 2. Math and numbers
- [ ] Counting
- [ ] Addition
- [ ] Subtraction
- [ ] Multiplication (visual grids first, symbols later)
- [ ] Number ordering, ascending/descending
- [ ] Number hunt ("find 7", later "all numbers greater than 5")
- [ ] Count the objects (tap the numeral, then visual reinforcement)
- [ ] One more / one less (drag a duck into the pond)
- [ ] Which has more (later: how many more)
- [ ] Number line (character jumps forward N spaces)
- [ ] Missing number (2 + ? = 5)
- [ ] Money / shop (coins, notes, prices, exact payment, change, budget)

## 3. Letters and reading
- [ ] Letter hunt ("find every A")
- [ ] Uppercase to lowercase
- [ ] Letter to sound (later C+A+T = CAT)
- [ ] Build a word (CA_ with options)
- [ ] Scrambled word
- [ ] Missing letter (C_T)
- [ ] Beginning sound
- [ ] Rhyming
- [ ] Word to image
- [ ] Image to word
- [ ] Simple sentence builder (pictograms first, words gradually)

## 4. Spatial reasoning and puzzles
- [ ] Puzzle blocks / tangram-style construction with ghost silhouette
- [ ] Rotate the piece
- [ ] Jigsaw (4, 6, 9, 16, 25+ pieces, auto-adjusting)
- [ ] What's behind the object (behind, in front, under...)
- [ ] Perspective (same from the other side), later-stage
- [ ] Copy the construction
- [ ] Find the missing piece

## 5. Mazes and pathfinding
- [ ] Finger maze (normal)
- [ ] Avoid obstacles
- [ ] Collect everything (3 stars then reach treasure)
- [ ] Follow numbers in order
- [ ] Follow letters in order
- [ ] Shortest path

## 6. Creativity
- [ ] Free drawing
- [ ] Guided drawing (roof, walls, door, windows)
- [ ] Trace shapes
- [ ] Trace letters
- [ ] Trace numbers
- [ ] Finish the drawing (half butterfly)
- [ ] Color by number
- [ ] Color by instruction ("make the roof red")
- [ ] Draw what you hear (big yellow circle, small blue triangle inside)
- [ ] AI-generated drawing challenges (only if useful, and only offline or compliant)

## 7. Character / life simulation
- [ ] Dress the character (dressing game)
- [ ] Dress for the weather
- [ ] Dress for the occasion (school, beach, winter, birthday, sports, camping)
- [ ] Pack a suitcase
- [ ] Morning routine (ordering)
- [ ] Clean your room (toys, clothes, trash)
- [ ] Cook a meal

## 8. World knowledge
- [ ] Animal world: habitat, food, baby, sound, footprint, body covering, domestic vs wild, land/sea/air
- [ ] Geography (which is Romania, continents, flags, landmarks, animals by continent, foods by country, globe)
- [ ] Seasons
- [ ] Day/night activities
- [ ] Space (planets, Earth/Moon, astronaut gear, planet size and order, gravity)

## 9. Early science
- [ ] Sink or float
- [ ] Magnet game
- [ ] Living vs non-living
- [ ] Plant growth stages
- [ ] Human body and five senses
- [ ] Healthy vs unhealthy (classification, not moralizing)
- [ ] Animal sounds
- [ ] Weather (and clothes for today)

## 10. Memory
- [ ] Classic memory (flip cards)
- [ ] Remember the sequence
- [ ] What disappeared
- [ ] Simon Says (colour sequence)
- [ ] Remember the location

## 11. Listening and comprehension
- [ ] Follow 1 instruction
- [ ] Follow 2 instructions
- [ ] Follow 3 instructions
- [ ] Listen and choose (sentence to picture)
- [ ] Listen for details (little red bird on the tree)

## 12. Visual perception
- [ ] Find the differences
- [ ] Spot the hidden object
- [ ] Same or different
- [ ] Match rotation
- [ ] Which is bigger
- [ ] Complete the picture (missing half)
- [ ] Follow the path through visual noise

## 13. Problem solving
- [ ] Help the character (dog, bone, fence)
- [ ] Tool selection (how to cut an apple)
- [ ] Cause and effect (plant needs water)
- [ ] Simple physics (ramps, blocks, platforms to reach a target)
- [ ] Bridge building
- [ ] Balance / scale (make both sides equal)

## 14. Real-life skills
- [ ] Shopping (see Money / shop)
- [ ] Clock (morning/night, then analog time)
- [ ] Calendar (days, months, seasons, yesterday/today/tomorrow)
- [ ] Cooking measures
- [ ] Sort laundry / chores
- [ ] Road safety (traffic light)
- [ ] Safety scenarios (hot stove, stranger, lost), handled gently

## 15. Social and emotional
- [ ] Emotion matching
- [ ] What would you do (two kids, one toy)
- [ ] Facial expression game
- [ ] Empathy (sad puppy, what helps)
- [ ] Social situations (someone falls down)

## 16. Building / engineering
- [ ] Build a car
- [ ] Build a rocket
- [ ] Build a house
- [ ] Build a boat
- [ ] Build a robot
- [ ] Test your creation (rocket flies, car drives, boat floats)

## 17. Mini-games that don't look educational
- [ ] Balloon popping (e.g. only even numbers)
- [ ] Fruit catcher (then "only red fruits")
- [ ] Space shooter (shoot the right answer)
- [ ] Fishing (catch fish with the right letter)
- [ ] Whack-a-mole
- [ ] Platformer (jump on 1-2-3-4-5)
- [ ] Treasure hunt (solve challenges to get keys)

## Meta features (scheduled like modes; discuss and accept one by one, after the first modes exist)
- [x] Character creator — superseded 2026-09-27: no animal head; boy/girl with real face/hair/
      clothes customization, see `docs/superpowers/plans/2026-09-27-m5-character-system.md`
- [ ] Little world (reward and collection layer; build a house for the character)
- [ ] Adaptive difficulty
- [ ] Daily Adventure
- [ ] Parent view (behind a parent gate)
- [ ] Engine with reusable interaction primitives

## Cross-cutting design tracks (discuss before the first mode is built; decisions feed every mode)
- [ ] Graphics: art style, character/mascot, palette, asset source (hand-made, AI-generated, purchased), resolution and size budget
- [ ] Audio: voice prompts (recorded vs TTS), languages, music, sound effects, mute/volume, feedback sounds for right/wrong
- [ ] UX / design: navigation with no reading (icons, home screen), touch-target sizes, mistake handling without punishment, reward feedback, session length, parent gate, orientation, accessibility

## Meta feature notes
- **Little world:** the child has a character and a house/island/town/spaceship. Mini-games earn stars, coins, building pieces, pets, clothes, furniture, decorations. Suggested mapping: math gives coins, reading gives stars, science gives materials, creative gives decorations, logic gives building pieces.
- **Adaptive difficulty:** no Easy/Medium/Hard. The game learns the child's level silently and moves them along progressions (numbers: count to 10, count to 20, addition, subtraction; letters: recognition, sounds, matching, words, sentences; spatial: 2D matching, rotations, patterns, construction, puzzles).
- **Daily Adventure:** 5 to 10 auto-assembled challenges from different categories, ending in a treasure chest.
- **Parent view:** per-skill progress (numbers, letters, logic, spatial, following instructions), behind a parent gate.
- **Engine idea:** about 12 reusable interaction primitives (tap, drag and drop, draw a path, connect, sort, match, sequence, assemble, trace, choose, draw, navigate). Games are content templates on top, e.g. `interaction=SORT, category=ANIMALS, criterion=HABITAT, difficulty=2`.

## Open decisions (fill in as answered)
- First game mode to build: (undecided)
- App structure: one app with many modes (assumed), world/meta layer later
- Target sub-ages, languages, voice approach, package/brand name, keystore: (undecided)
- Graphics, audio, UX tracks: see cross-cutting tracks above
- Graphics DECIDED (2026-09-21): style "3 Soft shaded" (gradients, soft drop shadows, glossy highlights, rounded shapes), and the final build must be animated. Budget: free preferred, up to about 10-20 USD total for assets is acceptable. Art source and animation technique: still to decide. Sample sketch of the three candidate styles was shown in chat (bear mascot, apples, number buttons).
- Character DECIDED (2026-09-21): the app has a player character the kid creates in a character selection screen: animal head (default suggestion: cat, plus a set of other animals), gender (male/female), colour, clothes. The body is one shared human-shaped body; only the head/face changes per animal. Little world direction: the kid builds a house for their character (rewards from modes become house pieces/furniture). Guide DECIDED (2026-09-21): a separate fixed guide character (parent-like: warm, guiding the kid) speaks the prompts and reacts; the kid's own character lives in the house and is not the narrator. Guide species/look and voice: still to decide.
- Audio DECIDED (2026-09-21): 100% AI-generated voice, pre-generated offline into bundled clips, as human-sounding as possible. Voice packs per language for the same 11 languages as Sudoku (en es pt de fr it ja ko ro ru zh-Hans). English and Romanian first. Guide voice persona DECIDED: warm female, mother-like. First release contains only English and Romanian. Wanted behaviour: app uses the phone's language voice pack if available, otherwise English; other languages downloadable from Settings when the user changes language. Delivery plan: voice access behind a per-language pack abstraction; v1 bundles en + ro (verify size), later languages as on-demand Google Play Asset Delivery packs (Play does the download, no own networking code; verify Unity support and that Play cannot target asset packs by language, so the app must request the pack itself). Open: TTS tool choice (verify current pricing and commercial-use terms before buying), music and sound effects.
- Voice packs DECIDED (2026-09-21, refines the above): English is the only pack shipped inside the app. Every other language, Romanian included, is a Play Asset Delivery pack the app requests itself: on first launch it requests the phone's language pack (English if none exists or offline), and Settings offers the rest. So v1 does not bundle Romanian.
- v1 scope DECIDED (2026-09-21): character creator; map with house and guide; house with place/move furniture; Store (buy furniture with coins); School and Playground as the two coin-earning places with 2-3 modes each (candidates: School = count the objects, number hunt, letter hunt; Playground = finger maze, jigsaw, pattern completion; final picks and order decided one mode at a time); English voice only, Romanian pack right after. Other places show "coming soon". Deliberately small so every piece gets polished.
- Orientation DECIDED (2026-09-21): landscape only. Note: Sudoku is portrait, so check whether framework UI helpers assume portrait; design for wide safe areas, notches and tablets; wide house and map scenes.
- Mistake handling DECIDED (2026-09-21): gentle retry with growing help. Wrong item wobbles, guide says a soft "try again", no buzzer. After 2 misses the guide hints (highlight or point). After 3 she shows the answer and the round moves on. Coins are slightly lower the more help was used, never zero.
- Difficulty DECIDED (2026-09-21): automatic level ladder per mode, no age question. Each mode has levels (e.g. counting 1-5, then 1-10, then 1-20); a few correct rounds in a row move the kid up, repeated struggle moves them down, silently. Target range about 5 to 8 years. This is the seed of the adaptive-difficulty meta feature.
- Parent gate DECIDED (2026-09-21): Settings (language, voice-pack downloads, volume, later parent progress view) sits behind a gate: a random multiplication question (e.g. 6 x 7) answered with big buttons plus a "hold to open" button. Needs no reading, too hard for the 5-8 target. Note: the multiplication mode may teach these facts later, so keep the gate question range wide or vary the question type.
- Music and SFX DECIDED (2026-09-21): sound effects from free CC0 packs (Kenney, OpenGameArt, Pixabay; check each licence). Background music AI-generated, a calm loop per place; verify the tool's commercial-use terms and cost before buying. Music and SFX have separate volume/mute in Settings.
- Guide and name DECIDED (2026-09-21): the app is "Eva's Learning World". Eva is the guide: warm, mother-like, and a CAT (the user first wrote "owl", then corrected to "definitely a cat"; cat is the final word). Consequence: the player character's default animal should not be a cat, to keep Eva distinct. Package id and store-name clash check still happen right before first publish (package id is permanent).
- Player character default DECIDED (2026-09-21): human (default), with animal heads as selectable options; shared body, gender, colour, clothes as before.
- First-run house tap DECIDED (2026-09-21): Eva greets the kid, gives one free starter furniture piece, and the kid drags it into the empty house (teaches tap and drag). Then she says "let's earn coins at School" and points to the map. Skippable.
- Profiles DECIDED (2026-09-21): one kid only. Save data still keyed by profile id internally so more profiles could be added without migration.
- House DECIDED (2026-09-21): landscape dollhouse cutaway with several rooms (living room, kitchen, bedroom, garden). Furniture drag-and-drop snaps into per-room slots, so it stays tidy, nothing overlaps and the kid cannot get stuck. House grows over time (new rooms and extras appear in the store).
- TECH: project placement DECIDED (2026-09-21): Eva is a NEW Unity project in the same git repo (sibling folder), sharing the framework as a local UPM package (one-time move of Assets/Framework into a package, Sudoku and 2048 must keep building). Reason: the current Unity project links Google Mobile Ads, Unity Purchasing and the Unity Analytics module project-wide, which is a Families-policy risk for a kids app; also separate orientation (landscape), package id, asset packs and heavy art/audio.
- TECH: art and animation DECIDED (2026-09-21): art drawn as SVG (style 3, sources kept in the repo) and rasterized to PNG by a script for Unity (Unity's SVG importer is preview-grade). Characters (Eva, player) are simple hierarchical cutout characters (revised 2026-09-21 after M0 review: SpriteRenderer parts under body parts, Animator clips, swappable parts; no deformable bones, no 2D feature package); animation clips (idle, wave, talk, cheer, shrug, walk) can be generated by editor scripts; character creator swaps head/clothes sprites. Spine or Rive stay possible later since the parts carry over. Tween helper for simple UI and object animation (squash/stretch, bounce, wobble, sparkle).
- TECH: activity architecture DECIDED (2026-09-21): shared round contract. A mode supplies a round generator per level plus a presenter that draws the round and reports the result (correct, mistakes, hints used); a shared runner handles Eva's voice prompt, the help ladder, coin payout, level ladder and the first-play tutorial. A maze or jigsaw is one round per puzzle. No full 12-primitive engine yet: extract shared pieces as the 2nd and 3rd modes show what repeats (user: "we can modify later").
- TECH: text alongside voice DECIDED (2026-09-21, user: helps kids who can read practice): every spoken line is also shown as text in the phone's language, tappable to replay. Text for all 11 languages ships inside the app (small); only voice is a per-language pack, so text follows the phone language even when the voice is still English. Later idea: highlight each word as it is spoken (needs word timings from the voice tool). Check font glyph coverage for ja/ko/zh-Hans/ru.
- TECH: local save, spec'd as JSON with version and profile key. Cloud save question raised by user: prefer no accounts and no own server; see the Android Auto Backup option in the next entry once decided.
- TECH: cloud save DECIDED (2026-09-21): no accounts, no own server. Use Android Auto Backup (Google Drive, parent's account) so progress restores on reinstall or a new phone. No live sync between devices. Verify Unity's manifest allowBackup handling and keep the save small (limit about 25 MB). Play Games Services and own servers rejected.
- SPEC REVIEW ROUND 1 (2026-09-21, user review of the design doc). These entries supersede earlier ones where they conflict:
  - Educational model: every mode eventually declares domain (Mathematics; Literacy; Logic and spatial reasoning; Executive function; General knowledge and science), skills, approximate age and level, and difficulty progression. Lightweight, not a standards framework. Use these four fields when a mode is taken up.
  - Difficulty: never changes after one round. Rolling window of the last 5 rounds at the level, minimum 5 rounds before any change; up when 4 of 5 clean, down when 3 of 5 ended in demonstration; window resets on change. Deterministic, initial constants tuned by play.
  - Help ladder replaces "show the answer": 1st mistake gentle retry, 2nd contextual hint, 3rd demonstrate and solve together, and the child still performs the correct action.
  - Parent gate replaces the multiplication question: random adult-knowledge question from a pool of about 20 culturally neutral questions, randomized question and answer choices, accidental-access barrier not security.
  - House is the meta-game; future deterministic collections (themed rooms, furniture sets, decorations, clothing, pets, garden objects); no random rewards.
  - Map replaces "coming soon" places: initial world has only House, School, Playground, Store; new places physically appear in later releases.
  - First-run stays a playable tutorial: character, house, free furniture, School, first game, coins, Store, furniture.
  - Voice-first with optional text: speech-bubble button and a parent setting (off by default) instead of permanent text; Eva is a warm, friendly female guide and companion, no longer explicitly a "mother".
  - Local-only parent progress view (stars per skill grouped by domain) built from save data.
  - Data-driven content: Activity Definition, Round Generator, Round Data, Presenter, Result. Offline AI content generation with validation and review; no runtime AI.
  - Vertical slice first (Count the objects end to end on a device), milestones M0 foundation, M1 slice, M2 polish, M3 more modes, M4 localization, M5 QA, M6 release. Guiding principle: simpler and better polished beats sophisticated.
- AUDIENCE BASELINE (2026-09-21, before M0 execution): UX baseline is a 4 to 5-year-old who may not read yet; learning progression extends to about age 8. Supersedes the earlier "5 to 8" target wording. Consequences (spec 4.0): instructions are voice plus demonstration, text is optional support only, touch targets at least 80 dp with main items 96 to 120 dp and forgiving drop zones, only taps and simple short drags, level 1 of every mode solvable by a non-reader (counting starts at 1 to 3).
- Name history (2026-09-21): working title was "Learning World". Store name, developer branding and Android package id are decided right before first publish (package id is permanent); check Play Store name clashes then. Avoid "NoAdsGuy" in the app title.
- UX flow DECIDED (2026-09-21): (1) first launch: character creation; (2) then a map with locked/disabled areas and the kid's house in the middle; (3) the kid must tap the house to start the adventure and the tutorial. Home screen is therefore the map, not a menu. Areas DECIDED (2026-09-21, supersedes earlier lock idea): map areas are themed places grouping modes (e.g. school for math and letters, playground for puzzles and mazes, arcade for whack-a-mole style games, store for shopping). NO progression locks: the kid can play any game any time. Sense of progression comes from building and furnishing the kid's house, not from gating games. Each game gets a quick tutorial (skippable, first play only). Places that are not built yet show as "coming soon". Rewards DECIDED (2026-09-21): coin economy. Every game pays coins; learning and harder games pay more, fun/non-learning games (arcade) pay less. The kid spends coins in the store on house items and furnishes the house. NO chests, loot boxes or any random rewards (user: feels like gambling); prices are visible and predictable. Store DECIDED (2026-09-21): one place with two jobs: buy house furniture with coins (paying can be a light money lesson: price shown in coins, kid taps coins to pay) and the shopping-math game (change, budgets) for older kids. Open: what the house tap does on first run, coin amounts and item prices. Open: exact area list and which ship in v1, what the tutorial teaches, how the house is decorated, session length, parent gate, orientation, touch-target sizes, mistake handling, music and SFX.
- M0 EXIT OUTCOMES (2026-09-21, technical foundation; details in `docs/superpowers/spikes/M0-summary.md`): framework moved into a local package with Sudoku and 2048 still building; Eva project builds a debug APK and the edit-mode tests run. Play Asset Delivery works with Unity-native asset packs (no plugin), so language voice packs stay as decided and Romanian is not bundled; it needs INTERNET plus five other Play-library permissions (M6 review item). Auto Backup works via a post-generate callback (same-device bmgr restore observed; Play install check in M5). Text: TextMeshPro with Noto fonts works for all 11 languages on the phone (+43 MB APK untrimmed; CJK font per language and trimming in M4). Cutout rig pattern works; the Eva project needed the Input System as the only input handler, and the phone needs a 120 fps request (vsync off) to feel smooth; battery policy decided in M1. SVG to PNG works with resvg-js. Voice DECIDED: Google Chirp 3 HD, voice Leda (English heard and approved by the user; Romanian pronunciation and diacritics to be confirmed at the first Romanian generation in M4; ElevenLabs and Azure not auditioned). The Boot-screen on-device check of the final build is still pending because the phone was not connected.
