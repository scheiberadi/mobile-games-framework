# Sudoku — Play Console Submission Runbook

Everything below is done by hand in Play Console (play.google.com/console) —
none of it is automatable from here, since it requires your account login
and agreement to Play's developer policies.

## 1. App already created — skip this step

The app entry already exists in Play Console:
- App name: `NoAdsGuy's Sudoku`
- Package name: `com.noadsguy.sudoku`
- Default language: English

Nothing to do here — go straight to Store listing below.

## 2. Store listing

**Short description** (80 char max):
```
Classic Sudoku puzzles with four difficulty levels and custom puzzles.
```

**Full description**:
```
Sharpen your mind with classic Sudoku, redesigned for a clean, modern
mobile experience.

FEATURES
- Four difficulty levels: Easy, Medium, Hard, and Expert, each puzzle
  guaranteed to have a unique solution.
- Custom mode: build your own puzzle from a blank grid, validated
  automatically so every custom puzzle you play has exactly one solution.
- Notes mode: pencil in candidate numbers in a clean 3x3 mini-grid inside
  each cell.
- Undo, Verify, and Autofill: check your work against the solution at any
  time, or reveal the answer when you're stuck (autofilled puzzles are
  marked and excluded from your high scores, so your leaderboard always
  reflects real solves).
- Dedicated High Scores page: your best times for each difficulty,
  ranked, with the option to clear a leaderboard and start fresh.
- Simple, distraction-free interface: number-first input, tap a number
  then tap the cells you want to fill.

No account required. No ads. No internet connection needed. Your
progress and times are saved privately on your own device.

Whether you're a Sudoku beginner working through Easy puzzles or an
expert chasing your best Expert time, Sudoku gives you a clean board
and gets out of your way.
```

**Graphics** (upload from `docs/store-assets/`):
- App icon: `icon-512.png`
- Feature graphic: `feature-graphic-1024x500.png`
- Phone screenshots (upload all four, in this order): `screenshots/menu.png`, `screenshots/gameplay.png`, `screenshots/success.png`, `screenshots/highscores.png`

**Category**: Puzzle

**Contact details**: your email; no website required (skip that field)

**Privacy policy URL**: the GitHub Pages URL confirmed in Task 4, e.g.
`https://scheiberadi.github.io/mobile-games-framework/privacy/sudoku.html`

## 3. Content rating questionnaire

Answer as: no violence, no sexual content, no profanity, no controlled
substances, no gambling (real or simulated), no user-generated content,
no shared/social features, no location sharing. This should produce an
"Everyone" rating.

## 4. Data safety form

Since ads/IAP are off for this release:
- Does your app collect or share any user data? **No**
- Data is encrypted in transit? N/A (no data collected)
- Users can request data deletion? N/A (no data collected)

## 5. App content declarations

- Ads: **No, my app does not contain ads**
- Target audience and content: pick an age range that fits a general
  puzzle game (e.g. 13+, or Everyone if prompted) — no specific
  children's-app declarations apply since there's no ad/data collection
  and no child-directed marketing.
- Government apps / COVID-19 apps / financial features: **No** to all.

## 6. Upload the build

- Go to **Release → Testing → Closed testing** (NOT Production — this
  account is new and Play requires a closed test first).
- Create a closed testing track, upload
  `Builds/Android/mobile-games-framework-sudoku-release.aab`.
- Fill in release notes, e.g. "Initial release."

## 7. Add testers

- In the closed testing track, add testers via email list (paste the
  email addresses of 12+ people willing to install and open the app) or
  a Google Group.
- Copy the opt-in URL Play Console generates and share it with your
  testers — they must open it and accept before they count.

## 8. Wait out the test window

- Play requires the closed test to run for **14 days** with **12+
  testers who have opted in** before the "Promote to production" option
  becomes available for a new developer account.
- Once eligible, go to the closed testing release, click **Promote
  release → Production**, and complete the rollout.
