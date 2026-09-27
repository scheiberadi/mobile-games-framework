# Eva's Learning World: design

Date: 2026-09-21 (revised after user review the same day). Status: draft for user approval. Working title "Eva's Learning World". The full mode wish-list and the running decision log live in `docs/kids-games/game-modes-backlog.md`; this document is the consolidated design for the first release (v1).

## 1. Summary

A landscape Android learning game whose UX baseline is a 4 to 5-year-old who may not read yet, while the learning progression extends through roughly age 8. The child creates a character and enters a small world with a warm, friendly guide, Eva the cat. Playing short learning games earns coins, and coins buy furniture that the child places in a dollhouse. Progress comes from building the house, never from locking games. The app is adless, has no in-app purchases, no accounts, no server, no analytics and no runtime AI. It is built as a new Unity project in this repository and reuses the existing framework as a package.

## 2. Guiding principle

The goal is not the most sophisticated educational engine. The goal is something a 4 to 5-year-old who cannot read yet genuinely enjoys playing independently, while the activities quietly teach useful skills. Whenever there is a choice between more sophisticated architecture and a simpler implementation, more features and better polish, or more educational complexity and more fun, v1 takes the simpler, better-polished option.

## 3. Non-goals

- No ads, in-app purchases, analytics, advertising ID, accounts, leaderboards or network code of our own.
- No runtime AI and no backend. AI is used only offline to help produce content (see 8.3).
- No random rewards of any kind (no chests, loot boxes, spin wheels). Prices and rewards are visible and predictable, and the child chooses what to buy.
- No progression locks: every built game is playable at any time.
- No locked or "coming soon" places on the map. Places that do not exist yet are simply absent.
- No formal educational-standards framework, no sophisticated personalization in v1.
- No multiple profiles in v1 (the save format keeps a profile id so profiles can be added later without migration).
- No live sync between devices.
- No generic 12-primitive engine in v1; shared pieces are extracted as the second and third modes show what repeats.

## 4. Player experience

### 4.0 Audience baseline: UX for age 4 to 5, learning up to about age 8

Two different things are deliberately kept apart:

- **UX baseline (everything the child touches, hears and sees): a 4 to 5-year-old who may not read yet.** Every screen, instruction, tutorial and control must work for that child alone, without a parent and without reading. If a design choice only works for a child who reads or has fine motor control, it fails the baseline.
- **Learning progression: roughly ages 4 to 8.** The content ladder of each mode starts at what a 4 to 5-year-old can do and stretches up to about age 8 (see 4.7 and 4.8). Older children are served by higher levels, never by changing the baseline UX.

Consequences, applied throughout this document:

- **Instructions:** always spoken by Eva and always shown by demonstration (Eva points, moves or does the first step), one short step at a time. No written instruction is ever required.
- **Text:** optional support for early readers and for accessibility (4.9), never a way to give instructions or tell the child what to do.
- **Touch and motor:** large targets, generous drop and snap areas, single taps and simple drags only (4.10).
- **Difficulty:** level 1 of every mode must be solvable by a 4 to 5-year-old who cannot read, and the ladder is built upward from there (4.7).
- **Parent-only screens** (Settings, parent gate, progress view) are the only place where reading and adult-level knowledge are assumed.

### 4.1 First-run playable tutorial

The first run is one guided, playable loop, not instruction screens. The child learns the core loop by doing it:

1. Character creation. Choices: head (human by default, animal heads as options; Eva is a cat, so there is no cat default), gender (male or female), colour, clothes. One shared human-shaped body; only the head and appearance layers change.
2. The child enters the house. Eva greets them and gives one free starter furniture piece; the child drags it into the empty house (teaches tap and drag).
3. Eva points to School. The child plays the first game with its in-play tutorial (the slice game, "Count the objects").
4. The game pays coins. The first game is guaranteed to pay at least the price of one starter furniture item, so the first purchase is always possible.
5. Eva walks the child to the Store; the child pays with coins, buys a piece of furniture and places it in the house.

Each step is skippable with a small, unobtrusive control. Skipping lands the child on the map with the starter furniture placed and a starter coin balance, so nothing is lost.

### 4.2 Map

After the tutorial the map is the home screen: the house in the middle and the other places around it. Every place on the map is accessible. The initial world contains only **House, School, Playground and Store**. Later releases add places that physically appear on the map (new art in the world) rather than showing locked or incomplete content. Planned future places live in the backlog document (Arcade, Zoo and farm, Science lab, Workshop, Art studio, Brain gym, Friends' park).

### 4.3 Store

One place with two jobs: buying house furniture with coins (the price is shown in coins and the child taps coins to pay, a light money lesson) and the shopping-maths game (change, budgets) for older children.

### 4.4 House as the meta-game

A landscape dollhouse cutaway with several rooms (living room, kitchen, bedroom, garden). Each room has furniture slots; dragged furniture snaps into a slot, so the house always looks tidy and the child cannot get stuck. The house is the persistent progression system.

The catalog is data-driven so deterministic collections can be added over time without code changes: themed rooms, furniture sets, decorations, clothing, pets, and garden or outdoor objects. Every item has a visible price, the child chooses what to buy, and sets are visible "complete the set" goals, never random. Only basic furniture and rooms are in v1; the item data already carries a set id and category so later collections need no schema change.

### 4.5 Rewards and coins

Every game pays coins. Learning and harder games pay more; fun or arcade games pay less. Coins for a round drop slightly for each help step used but never reach zero. Concrete amounts and furniture prices are set during planning and tuned by playing.

### 4.6 Help and mistakes: help that teaches

Revealing the answer after a fixed number of failures would let a child learn to fail on purpose. Instead the help ladder ends with the child performing the correct action.

1. **First mistake:** gentle retry. The chosen item wobbles and Eva says a soft "try again". No buzzer.
2. **Second mistake:** contextual hint. Eva points to or highlights the relevant part of the round and reminds the child of the rule (for example, in counting she points and says the numbers aloud while the child watches).
3. **Third mistake:** demonstrate and solve together. Eva demonstrates the method step by step (in counting, she counts each object aloud and the child taps along), and the child then performs the correct action themselves. The round completes only when the child has done it.

Failing on purpose does not pay: a demonstrated round pays the lowest coin tier, takes longer than getting it right, and counts toward the difficulty going down (4.7). Each mode's presenter must implement its own hint and its own demonstration; those are part of the mode, not generic UI.

### 4.7 Difficulty progression

Each mode has a level ladder that starts at what a 4 to 5-year-old who cannot read can do and reaches up to about age 8 (for example counting 1 to 3, then 1 to 5, then 1 to 10, then 1 to 20). Level 1 of every mode assumes no reading, minimal precision and one simple action per round. The rule is deterministic and simple, and never reacts to a single round:

- The game keeps a rolling window of the last 5 rounds played at the current level.
- **Minimum before any change:** 5 rounds at the current level.
- **Level up:** at least 4 of the last 5 rounds were "clean" (solved with at most a hint, without the demonstration step).
- **Level down:** at least 3 of the last 5 rounds ended in the demonstration step.
- After any change the window resets. The level never goes below 1 or above the mode's top level.
- These numbers are initial constants in one place, tuned by playing. No age question is asked and there is no AI personalization in v1.

### 4.8 Educational model

Lightweight, not a standards framework. Every mode (eventually) declares four things in its data:

- **Domain**, one of: Mathematics; Literacy; Logic and spatial reasoning; Executive function (attention, memory, following instructions, sequencing); General knowledge and science.
- **Skills**: one or a few specific skills (for example "counting", "number recognition", "letter recognition", "patterns").
- **Approximate age and level range.**
- **Difficulty progression:** what changes per level (range of numbers, number of distractors, piece count, and so on).

Examples for the v1 candidates:

- Count the objects: Mathematics, counting and number recognition, about 4 to 6, quantity range (starting at 1 to 3) and distractors grow.
- Number hunt: Mathematics, number recognition, about 4 to 6, larger numbers and more distractors.
- Letter hunt: Literacy, letter recognition, about 4 to 6, uppercase then lowercase then more letters (the target letter is spoken by Eva, never read).
- Finger maze: Logic and spatial reasoning, path planning, about 4 to 8, maze size and obstacles grow (wide paths at level 1).
- Jigsaw: Logic and spatial reasoning, spatial reasoning, about 4 to 8, 4 up to 25 pieces.
- Pattern completion: Logic and spatial reasoning, patterns, about 4 to 7, from AB to ABB to ABC.
- Store shopping game: Mathematics, coin recognition, addition, change, about 6 to 8.

Skill ids are stable keys and are also what the save data and the parent progress view use.

### 4.9 Voice, text and Eva's persona

- The experience is voice-first. Eva's voice is a warm, friendly female guide and companion. Clarity, warmth and consistency matter more than perfect realism.
- Text is optional support for early readers (typically the older end of the range) and for accessibility. It is never required for gameplay and never carries an instruction the voice does not also carry, because the baseline child (4 to 5) may not read at all. The screen stays uncluttered for children who cannot read. Eva has a small speech-bubble button; tapping it shows the current line as text in the phone's language and replays the voice. A parent-controlled setting "Show text with Eva's voice" (off by default) shows a speech bubble with every spoken line, for reading practice and for accessibility (for example a child with hearing difficulty).
- Text for all 11 Sudoku languages ships inside the app (it is small); only voice is a per-language pack, so text follows the phone language even while the voice is still English. A later idea: highlight each word as it is spoken, if the voice tool provides word timings. Check font glyph coverage for ja, ko, zh-Hans and ru.

### 4.10 UX rules

- Landscape only. Design for wide safe areas and notches; test on a tablet.
- Sized for a 4 to 5-year-old's motor control: touch targets at least 80 dp, main buttons and draggable items about 96 to 120 dp, generous drop and snap zones. Only single taps and simple short drags: no double taps, long presses, precise swipes or small handles in child-facing screens. Forgive near misses (a tap close to an item counts).
- No timers or pressure in learning modes.
- No gameplay depends on reading. Instructions and tutorials are voice plus demonstration; icons and pictures carry meaning; a 4 to 5-year-old must be able to play every screen without help.
- A gentle "time for a break" message from Eva after about 20 minutes, which the child can dismiss.
- Music and effects have separate volume and mute controls in Settings.

### 4.11 Parent gate

Settings and all parent-only features (language and voice-pack downloads, the text-with-voice option, volume, the progress view) sit behind a randomized adult-knowledge challenge.

- Each access attempt selects one question at random from a pool of about 20 simple, culturally neutral questions. Both the question and the answer choices are randomized (order and distractors).
- Questions are trivial for a typical adult but hard enough that a young child cannot reliably answer them: basic arithmetic, simple number relationships, elementary shapes and universally familiar facts.
- The gate needs no reading of long text, uses big answer buttons, and is localized with the rest of the text.
- It is an accidental-access barrier, not a security or authentication mechanism. The pool is reviewed so it does not overlap too much with what the maths modes teach, and it is a data file so it can be refreshed.

### 4.12 Parent progress view

A local-only view behind the parent gate that shows what the child has been practicing, grouped by domain, with a star rating per skill (1 to 5):

- Mathematics: counting, number recognition, addition.
- Literacy: letter recognition, phonics.
- Logic and spatial reasoning: sorting, patterns.

The view is generated entirely from local save data (per-skill level, rounds played, recent clean-round ratio); nothing leaves the device. The save schema records this per-skill data from the start, so the view can come later without migration. The exact star mapping (for example level reached within the mode's ladder, adjusted by recent performance) is decided during planning.

## 5. v1 scope

The scope is deliberately small and is built as a **vertical slice first**, then scaled.

### 5.1 Vertical slice (must be playable on a real Android device)

Character, Map, Eva, House, School, ONE game ("Count the objects"), Voice, Help and mistake system, Reward, Store, Furniture, House placement, Save and restore. English voice only. Only after the slice works and feels good do the remaining modes get built.

### 5.2 After the slice

- School and Playground each grow to 2 to 3 modes. Candidates in 4.8; final picks and order are decided one mode at a time, each accepted before the next.
- English voice shipped in the app; Romanian voice as the first downloadable pack.
- Text for all 11 languages.
- Local parent progress view.

## 6. Graphics and animation

- Style: soft shaded (gradients, soft drop shadows, glossy highlights, rounded shapes), animated in the final build.
- Art is drawn as SVG and the sources stay in the repository. A script rasterizes it to PNG for Unity, because Unity's SVG importer is preview-grade.
- Characters (Eva and the player) are simple hierarchical cutout characters inside Unity: `SpriteRenderer` parts parented under body parts, animated by `Animator` clips, with swappable parts. No deformable bones or skinning, and no extra 2D packages are needed. Editor scripts can generate the clips (idle, wave, talk, cheer, shrug, walk, and the pointing and counting gestures used by help). The character creator swaps head and clothing sprites and tints colours. Spine or Rive remain possible later because the parts carry over.
- Frame rate: request 120 fps with vsync off on 120 Hz phones (on the S25 Ultra the 60 fps cap made a fast arm swing feel choppy; 120 measured 120.2 fps and the user judged it good). The battery policy (for example lower rates on static screens) is decided and measured in M1. Do not rely on `Screen.currentResolution` for the refresh rate on this device.
- A small tween helper handles simple UI and object animation (squash and stretch, bounce, wobble, sparkle).
- Asset budget: free preferred, up to about 10 to 20 USD in total is acceptable. Most art is produced by us.

## 7. Audio

- Voice: 100% AI-generated, warm friendly female guide voice for Eva. Generated once offline from a script (CSV rows: key, language, line) and committed as clips. No text-to-speech at runtime. Tool and voice DECIDED after the M0 audition (`docs/superpowers/spikes/voice-audition.md`): Google Cloud Text-to-Speech, Chirp 3 HD, voice Leda (`en-US-Chirp3-HD-Leda`, `ro-RO-Chirp3-HD-Leda`). The user heard the English voice and liked it; ElevenLabs and Azure were not auditioned. About 600 lines per language is about 36k characters, well inside Google's free 1 million characters per month (30 US dollars per 1 million after that), so the expected cost is zero. Still to confirm by ear at the first Romanian generation (M4): Romanian pronunciation and the diacritics `ă â î ș ț`; Korean quality must also be auditioned before its pack is committed. Google's terms have not been checked for a child-directed clause beyond the pages read (privacy work, M7, was M6). Generation needs a Google Cloud account and API key, kept in an environment variable and never in the repository.
- Voice packs per language for the 11 Sudoku languages. English ships in the app; every other language, Romanian included, is a Google Play Asset Delivery pack. On first launch the app requests the pack for the phone's language (English when none exists or the device is offline); Settings offers the others. The app requests packs itself because Play does not select asset packs by language automatically. Play performs the download, so the app has no networking code.
- Sound effects: free CC0 packs (Kenney, OpenGameArt, Pixabay; check each licence).
- Music: AI-generated calm loop per place; verify the tool's commercial-use terms and cost first.

## 8. Architecture

### 8.1 Repository and project layout

- New Unity project in a sibling folder of the current one, in the same git repository.
- The current `Assets/Framework` moves into a local UPM package that both the existing project and the new project reference. Sudoku and 2048 must keep building unchanged. The move is a separate, verified step.
- The Eva project uses the Input System only (`activeInputHandler` 1), set by `EvaProjectSetup.Apply`; all Eva UI depends on it (with the old Input Manager the UI buttons received no touch).
- Reason for a new project: the existing project links Google Mobile Ads, Unity Purchasing and the Unity Analytics module project-wide. Sudoku works around this with a Gradle template. A kids' app must not contain an ad SDK. The new project also has its own landscape settings, package id, asset packs and heavy art and audio without slowing Sudoku and 2048.

### 8.2 Modules (each an assembly with one clear purpose)

- **Rules** (pure C#, no engine references, unit-tested): level ladder and rolling window, help ladder, coin calculation, round generators, content validation, save schema and migration, house layout model, progress and star calculation.
- **Activities**: the shared round contract and runner (see 8.3).
- **Characters**: rig prefabs, character creator model, appearance data, animation clips.
- **World**: map, house and store screens.
- **Voice**: `Voice.Play(key)`, pack lookup, English fallback, pack download requests, optional text bubble.
- **Persistence**: JSON save through the framework's `IKeyValueStore`, version number, profile key.
- **App**: bootstrap, scene flow, settings, parent gate and progress view.

### 8.3 Data-driven content and the activity contract

Individual exercises are not hardcoded in gameplay code. The pipeline is conceptually:

`Activity Definition -> Round Generator -> Round Data -> Presenter -> Result`

- **Activity Definition** (data file): the mode's id, place, domain, skills, age and level range, level ladder (what changes per level), the generator to use, and the content set. Modes share a schema so a new mode is mostly data plus a generator and a presenter.
- **Round Generator**: produces Round Data for a level. It is either algorithmic (counting: pick N and objects) or picks from an authored content list (pattern sequences, words).
- **Round Data**: content and asset references, the correct answer, difficulty, the learning objective and skill id, and the localization and voice keys for Eva's prompt, hint and demonstration lines.
- **Presenter**: draws the round, accepts input, implements the mode's own hint and demonstration (see 4.6), and reports a **Result**: correct or not, mistakes, help step reached, time.
- **Runner** (shared): owns the generic activity lifecycle, Eva's prompt, the help ladder, coin payout, difficulty progression and per-skill stats, and the first-play tutorial for each mode. A maze or jigsaw counts as one round per puzzle.

Content files are JSON with a versioned schema, loaded from the app's data. An editor-time validator (also run as an edit-mode test) checks schemas, value ranges, that math answers are actually correct, that referenced assets exist, and that every voice and text key exists.

### 8.4 Offline AI content generation

The architecture is built to support this workflow later, without any runtime AI:

- A request such as "generate 100 level-2 counting exercises for ages 5 to 6 using animals and quantities 1 to 10" produces structured content in the content schema.
- The validator (8.3) checks it automatically, a person reviews it, then it is committed and shipped with the game. Voice lines needed by the new content go through the normal voice script pipeline.
- v1 only needs the schema and the validator; a generation tool is not built in v1.

### 8.5 Save data

One local JSON save: character appearance, coins, house layout and owned furniture, per-mode level and rolling window, per-skill stats (feeds the parent progress view), tutorials seen, settings. Versioned, keyed by profile id (one profile in v1). Stored in PlayerPrefs or a file inside the app's data folder, whichever Auto Backup includes. Local only; nothing is collected.

### 8.6 Backup

Android Auto Backup (Google Drive of the parent's account) restores progress after reinstalling or moving to a new phone. There are no accounts, no sign-in screen and no server. Progress does not sync live between devices. Verified in the M0 spike: a post-generate callback (`EvaAndroidPostProcess`) declares `allowBackup` and rules that include the shared preferences (PlayerPrefs), and a same-device `bmgr` backup and restore brought the value back; the check on a Play-installed build on a new phone stays in M6 (was M5). Keep the save small (backup limit is about 25 MB).

### 8.7 Build and release

An Android builder script in the new project, modelled on the existing `AndroidApkBuilder` (debug and release, auto-incremented version code, R8 mapping, native debug symbols). Eva gets its own new upload keystore, separate from Sudoku's. The package id and store name are decided just before the first publish (the package id is permanent); check Play Store name clashes then and avoid "NoAdsGuy" in the app title.

## 9. Compliance (Google Play Families Policy)

- No ad or analytics or purchasing packages in the project, no advertising ID, minimal permissions, no runtime AI or backend.
- No own networking code. Voice pack downloads are performed by Play. Play asset delivery requires `INTERNET` and its library also adds `ACCESS_NETWORK_STATE`, `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_DATA_SYNC`, `WAKE_LOCK` and `RECEIVE_BOOT_COMPLETED` to the manifest (observed in the M0 spike). Do not strip them; the M7 Families and data-safety review must account for them (Play-managed network use, no data sent by us).
- Play Console: declare the target age groups, complete the data-safety form (no data collected by us; confirm how Play treats OS-level Auto Backup), publish a privacy policy based on the Sudoku one in `docs/privacy`.
- Settings, downloads and the progress view sit behind the parent gate.
- Confirm all of the above against current Play policy pages before submission rather than relying on this summary.

## 10. Testing

Game rules live in engine-free assemblies and are covered by edit-mode tests: the rolling-window level ladder (no change before 5 rounds, promotion and demotion thresholds, window reset), the help ladder, coin payout, round generators, content validation, save migration, house slot rules, progress and star calculation, and the parent-gate question pool (unique ids, exactly one correct answer, randomized order). The user reports Sudoku's Unity tests are broken and the cause was never investigated, so getting the new project's test runner working is an early task. UI and animation are verified on the device (Samsung Galaxy S25 Ultra, gesture navigation only) plus at least one tablet layout check.

## 11. Risks and early spikes (in M0, before feature work)

1. Play Asset Delivery in Unity 6000.5: request a language pack at runtime with English fallback. Fallback plan if it fails: bundle Romanian and revisit delivery.
2. Android Auto Backup with Unity's manifest and the chosen save location.
3. Cutout character rig proof: one hierarchical sprite character with idle and wave clips and a swappable head, on device.
4. SVG to PNG rasterizing script (Node is available; Python is not) producing correct gradients and shadows.
5. Framework-as-package move without breaking the existing Sudoku and 2048 builds.
6. Voice quality audition (English and Romanian) before choosing the tool.
7. CJK and Cyrillic font coverage for the text lines.

## 12. Milestones

- **M0 Technical foundation:** new project, framework package, test runner working, builder script producing a debug APK, and the spikes in section 11.
- **M1 Vertical slice:** the slice in 5.1 playable end to end on a real Android device, including save and restore. Stop and judge it: does it feel good and does a child enjoy it?
- **M2 UX and polish:** first-run tutorial, help and mistake system, voice, animation, difficulty progression, tuning of coins and prices.
- **M3 Additional content and modes:** remaining School and Playground modes one at a time (each accepted before the next), content schema and validator hardened, local parent progress view.
- **M4 English and Romanian localization:** redefined 2026-09-26 as full content build-out
  (`docs/superpowers/plans/2026-09-26-m4-full-content-plan.md`); localization now ships alongside
  M7 instead.
- **M5 Real player character system:** boy/girl creation, working dress-up, character on every
  gameplay screen, Eva motion/art coherence pass
  (`docs/superpowers/plans/2026-09-27-m5-character-system.md`). Inserted 2026-09-27, pushing QA and
  Release out by one milestone each.
- **M6 Device QA, backup, accessibility and performance:** Auto Backup restore test, tablet layout, hearing and reading accessibility options, low-end device performance.
- **M7 Release:** compliance declarations, privacy policy, store listing and assets, keystore, final name and package id, release build, plus the localization deferred from M4.

## 13. Deliberately undecided (settle during planning)

Exact v1 mode list and order after the first mode; coin amounts and furniture prices; the furniture and clothing catalog; the animal list for character heads; the parent gate question pool; the star mapping for the progress view; the voice and music tools; final store name and package id; how Eva looks.
