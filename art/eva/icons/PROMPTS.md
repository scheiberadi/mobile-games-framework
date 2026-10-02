# Icon prompts for ChatGPT (missing UI icons)

## `icons/activities` - the button in the Store (bottom right) that opens the games menu

Currently a pink placeholder square (`StoreScreen.cs`, `EvaUi.Sprite("icons/activities")`). Save the result as
`art/eva/icons/ai/sheet_icons_activities.png`, then add a `SHEETS` entry (`resDir: 'icons'`, one name `activities`).

"Draw ONE icon for a children's mobile game: the button that opens the games menu. A fan of three small rounded game
cards, one red, one yellow, one blue, slightly overlapping, with a big friendly green play triangle (a rounded right-pointing
triangle with a thin white outline) in front of them in the lower right. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto the background, no text,
letters or numbers anywhere, no people. Centred, filling most of the image with a small plain margin. Background: plain solid
magenta (#ff00ff) everywhere - not a checkered/transparent placeholder, an actual solid magenta fill. File name:
icons_activities.png."

## `icons/home`, `icons/gear`, `icons/back`, `icons/piggybank` - Hud and Map icons, no button plate

One sheet, four icons side by side (done 2026-10-02: `art/eva/icons/ai/sheet_ui_icons.png`, `cut-sheets.js ui_icons --install`).
Home, Back and the Map's settings gear are drawn inside a 240 tap area; the piggy bank replaces the coin next to the counter
(the plain `icons/coin` is still what flies). Prompt: a 4 x 1 sheet, soft polished 3D-look, thick warm brown outline, NO circle,
plate, disc, badge or frame behind any icon, solid magenta background; (1) a chunky red-roofed cottage, (2) a chunky
brown-orange gear with a round hole, (3) a chunky orange left arrow, (4) a pink side-view piggy bank with a gold star coin
dropping into its back slot and two sparkles.
