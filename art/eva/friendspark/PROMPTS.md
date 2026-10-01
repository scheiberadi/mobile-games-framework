# Friends' Park art prompts for ChatGPT (M4.9, 12 games)

Written 2026-10-01 against `Rules/FriendsPark.cs` and `docs/kids-games/answer-variety-plan.md` (Friends' Park section).
Every sprite key comes from the code (`friendspark/<name>`). The variety plan keeps What Would You Do, Social Situations and
Safety Scenarios as **choices** (user ruling) and adds outcome animations later; Empathy and Facial Expression may become
drag games, so faces, scenarios and responses are drawn as separate pictures.

**Design decision to confirm with Adrian**: every other building uses no people, but a social-emotional building cannot work
without them. These prompts draw simple, friendly cartoon CHILDREN (round heads, big gentle eyes, same soft look as the
player character, mixed skin tones and hair, plain clothes, never scary or sad-to-the-point-of-distress), and cartoon animal
friends where a person is not needed. If you would rather keep the "no people" rule, say so and the scenario prompts get
rewritten with animal characters (bear cub, bunny, kitten).

Style paragraph (pasted into every prompt below): "Style for everything: soft polished 3D-look children's mobile-game
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text
or letters anywhere. Background: plain solid magenta (#ff00ff) everywhere, including between cells - not a checkered/
transparent placeholder, an actual solid magenta fill." Every prompt also asks for each picture alone, centred in its own cell
with generous plain margin, a similar visual size, nothing touching a cell edge.

Same sheet workflow as every other building (`docs/kids-games/m4-handover.md`): generate, save under
`art/eva/friendspark/ai/`, add a `SHEETS` entry to `tools/art-import/cut-sheets.js` (`dir: 'friendspark/ai'`, `resDir:
'friendspark'`, `size: 512`), cut with `--install`, verify, commit each batch.

## Batch 1: emotion faces (6) — `friendspark/face_<happy|sad|angry|scared|surprised|calm>`

"Draw a sprite sheet of 6 individual faces for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style].
Each is the face of the SAME friendly round-faced cartoon child (head and neck only, the same child every time), clearly
showing ONE feeling with the eyes, brows and mouth: (1) happy, a big smile; (2) sad, downturned mouth and a small tear; (3)
angry, frowning brows and a pressed mouth (cross, not scary); (4) scared, wide eyes and a small wobbly mouth; (5) surprised,
raised brows, round open mouth; (6) calm, relaxed closed eyes and a gentle smile. File name: friendspark_faces_sheet.png."

## Batch 2: emotion icons (6) — `friendspark/emotionicon_<same ids>`

Symbols (not faces) for each feeling, used as the answer tiles for the Facial Expression Game.

"Draw a sprite sheet of 6 individual symbol icons for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style].
Each icon is a simple symbol for a feeling, with NO face on it: (1) happy: a bright sun with rays; (2) sad: a small grey rain
cloud with raindrops; (3) angry: a red thunder cloud with a lightning bolt; (4) scared: a small round bunny hiding behind a green leaf; (5) surprised,
raised brows, round open mouth; (6) calm, relaxed closed eyes and a gentle smile. File name: friendspark_faces_sheet.png."

## Batch 2: emotion icons (6) — `friendspark/emotionicon_<same ids>`

Symbols (not faces) for each feeling, used as the answer tiles for the Facial Expression Game.

"Draw a sprite sheet of 6 individual symbol icons for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style].
Each icon is a simple symbol for a feeling, with NO face on it: (1) happy: a bright sun with rays; (2) sad: a small grey rain
cloud with raindrops; (3) angry: a red thunder cloud with a lightning bolt; (4) scared: a small shivering ghost-free blanket
tent with two peeking eyes? NO - instead a small round bunny hiding behind a leaf; (5) surprised: a bright yellow starburst
shape; (6) calm: a pink water lily floating on still water. File name: friendspark_emotion_icons_sheet.png."

## Batch 3: What Would You Do — scenarios (6) and responses (6)

`friendspark/scenario_<friend_falls|someone_crying|dropped_toy|cant_reach|someone_excluded|spilled_drink>` and
`friendspark/response_<help_up|comfort|pick_up_together|offer_help|invite_in|help_clean>`.

"Draw a sprite sheet of 12 individual pictures for a children's mobile game, arranged in a grid of 4 columns x 3 rows. [style].
Items 1-6 are six small gentle scenes with cartoon children in a park (mild situations, nobody badly hurt, nothing scary): (1) a
child has fallen over on the grass and sits there looking surprised, another child nearby; (2) a child sitting alone and crying
a little; (3) a child who has dropped a toy teddy on the ground; (4) a small child on tiptoe who cannot reach a ball on a high
shelf; (5) a child standing alone and looking at a group of children playing together; (6) a child looking at a glass of juice
spilled on the ground. Items 7-12 are six response pictures, each a different friendly ACTION by a cartoon child, in this order:
(7) helping a fallen child up by the hand; (8) hugging a sad child; (9) two children picking up a toy together; (10) a child
lifting the ball down for a small child; (11) a child waving a friend into a group; (12) a child wiping up the spill with a
cloth. File name: friendspark_whatwouldyoudo_sheet.png."

## Batch 4: Empathy — scenarios (6) and responses (6)

`friendspark/scenario_<friend_sad|friend_scared|friend_lost_toy|friend_left_out|friend_hurt|friend_happy>` and
`friendspark/response_<ask_whats_wrong|stay_close|help_look|include_them|get_grownup|celebrate_with>`.

"Draw a sprite sheet of 12 individual pictures for a children's mobile game, arranged in a grid of 4 columns x 3 rows. [style].
Items 1-6 are six small scenes, each showing one cartoon child and how that child feels (no scary content): (1) a child looking
sad with head down; (2) a child hugging their knees, a little scared of a loud thunder cloud far away; (3) a child searching
and looking worried because a toy is missing; (4) a child standing a bit apart from a group, looking left out; (5) a child
holding a scraped knee, wincing slightly; (6) a child jumping with joy and a big smile. Items 7-12 are six response pictures,
each a friendly ACTION by another cartoon child, in this order: (7) crouching beside a sad child with a gentle questioning
look, one hand on their shoulder; (8) sitting close beside a scared child, an arm around them; (9) searching for a lost toy
together, looking under a bench; (10) taking a lonely child by the hand towards the group; (11) running to fetch a grown-up
(an adult waving from a distance); (12) clapping and cheering with a happy child. File name: friendspark_empathy_sheet.png."

## Batch 5: Social Situations — scenarios (6) and responses (6)

`friendspark/scenario_<new_kid|someone_waiting_turn|want_to_join|someone_won|made_mistake|someone_shared>` and
`friendspark/response_<say_hello|wait_patiently|ask_to_play|say_congrats|say_sorry|say_thankyou>`. The responses are drawn as
speech-free gestures (no speech bubbles with words).

"Draw a sprite sheet of 12 individual pictures for a children's mobile game, arranged in a grid of 4 columns x 3 rows. [style].
Items 1-6 are six small scenes with cartoon children: (1) a new child arriving at the park gate with a backpack; (2) a child
waiting in a short queue at the slide while another child climbs it; (3) a child standing near a group playing ball, wanting to
join; (4) a child who has just won a race, holding a small gold medal; (5) a child who has bumped into another and knocked their
ice cream onto the ground (gently comic); (6) a child sharing a snack with another child. Items 7-12 are six response
pictures, each ONE child doing a friendly gesture, no speech bubbles and no words: (7) waving hello with a smile; (8) standing
patiently in line with hands by the sides; (9) a child asking to join, hands open and a friendly look, pointing at the game;
(10) clapping and smiling at a winner; (11) a child with a regretful friendly face, one hand on chest, saying sorry (no words);
(12) a child smiling and holding both hands to the heart, saying thanks (no words). File name: friendspark_social_sheet.png."

## Batch 6: Safety Scenarios — scenarios (6) and responses (6)

`friendspark/scenario_<hot_stove|stranger_offers_candy|lost_in_store|sharp_scissors|busy_road|unknown_medicine>` and
`friendspark/response_<dont_touch|say_no_tell_grownup|find_a_helper|ask_for_help|hold_a_hand|leave_it_alone>`. Handled
gently: nothing frightening, no violence, no distress; the stranger is a plain, neutral adult figure who simply holds out a
sweet.

"Draw a sprite sheet of 12 individual pictures for a children's mobile game, arranged in a grid of 4 columns x 3 rows. [style].
Items 1-6 are six small, calm, clear safety scenes: (1) a stove with a steaming pot and glowing hot ring, a small child standing
near it looking curious; (2) a child at the park with a grown-up they do not know holding out a sweet (the adult is a plain,
neutral cartoon figure, not scary); (3) a child standing alone in a shop aisle looking around, a little unsure; (4) a pair of
sharp scissors on a table beside a child; (5) a busy road with cars, a child at the kerb; (6) a small bottle of medicine tablets
on a low table beside a child. Items 7-12 are six response pictures, in this order: (7) a child with the hand held up in a
'stop, do not touch' gesture, the other hand behind the back; (8) a child shaking their head 'no' and then running to a waving
parent; (9) a child walking toward a shop assistant wearing a friendly badge; (10) a child holding up the scissors to a grown-up
who is smiling; (11) a child holding a grown-up's hand at the kerb; (12) a child stepping away from the medicine with hands
behind the back. File name: friendspark_safety_sheet.png."

## Batch 7: Listen and Choose pictures (6) — `friendspark/picture_<dog_runs|bird_flies|girl_jumps|boy_swings|cat_sleeps|kids_play_ball>`

"Draw a sprite sheet of 6 individual picture cards for a children's mobile game, arranged in a grid of 3 columns x 2 rows, each
picture on a soft rounded square card. [style]. Each is a clear, simple picture of one sentence, easy to tell apart: (1) a
brown dog running on grass; (2) a blue bird flying in a sky; (3) a girl jumping in the air with a skipping rope; (4) a boy
swinging on a swing; (5) a cat curled up asleep on a cushion; (6) two children playing with a ball. File name:
friendspark_listen_sheet.png."

## Batch 8: Listen for Details pictures (6) — `friendspark/detailpicture_<id>`

Details that must be visible: clothing colour, the thing the child is doing and the place. ids: boy_red_shirt_slide,
girl_blue_dress_swing, dog_brown_ball, cat_white_bench, boy_yellow_hat_sandbox, girl_green_shoes_seesaw.

"Draw a sprite sheet of 6 individual picture cards for a children's mobile game, arranged in a grid of 3 columns x 2 rows, each
picture on a soft rounded square card. [style]. Each shows one subject doing one thing in one place, with ONE colour detail
that must be easy to see: (1) a boy in a RED shirt going down a slide; (2) a girl in a BLUE dress on a swing; (3) a BROWN dog
with a ball in a park; (4) a WHITE cat sitting on a park bench; (5) a boy with a YELLOW hat in a sandbox; (6) a girl with GREEN
shoes on a seesaw. Keep the colour detail clear and the pictures similar in layout so they can be told apart only by looking
carefully. File name: friendspark_details_sheet.png."

## Batch 9: park action tiles (6) — `friendspark/action_<wave_hello|sit_on_bench|pick_up_ball|pet_the_dog|go_on_swing|slide_down>`

Used by Follow 1/2/3 Instructions (tapped in order) and later for the "carry out instructions with the character" version.

"Draw a sprite sheet of 6 individual rounded-square tiles for a children's mobile game, arranged in a grid of 3 columns x 2 rows.
[style]. Each shows ONE cartoon child doing one action in a park, clear and simple: (1) waving hello, (2) sitting on a bench,
(3) picking up a ball from the grass, (4) petting a friendly dog, (5) swinging on a swing, (6) sliding down a slide. File name:
friendspark_actions_sheet.png."

## Batch 10: Road Safety (6) — `friendspark/light_<green_light|red_light>`, `friendspark/action2_<cross|wait>`, plus
`friendspark/roadsafety_crossing` and `friendspark/roadsafety_car`

Light = a traffic-light scene; actions are the two answer icons. The crossing and the car are for the future "walk across
under the traffic light" version.

"Draw a sprite sheet of 6 individual pictures for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style].
(1) a traffic light for pedestrians showing GREEN, on a pole beside a zebra crossing; (2) the same traffic light showing RED;
(3) an answer icon 'cross': a big green rounded square with a white walking figure (a simple pictogram, like a pedestrian
signal) stepping forward; (4) an answer icon 'wait': a big red rounded square with a white hand held up (stop hand); (5) a wide
empty zebra crossing across a calm town road seen from the side of the pavement, no cars, no people; (6) a small red toy-like
car seen from the side. File name: friendspark_roadsafety_sheet.png."

## Batch 11: missing activity icon `listen_and_choose` — `activities/listen_and_choose`

The only activity icon still missing (121 of 123 imported). Attach an existing icon, e.g.
`EvasLearningWorld/Assets/Eva/Resources/Art/activities/listen_for_details.png`, so the frame matches.

"Attached is an existing game-menu icon: a rounded-square tile with a brown frame and yellow inner border, a scene inside, and a
round green play button at the bottom right. Draw one NEW icon in exactly the same frame, colours, finish and size, with a
transparent background outside the frame (or plain magenta #ff00ff if you cannot do transparency), no text. Scene inside the
frame: a friendly cartoon dog on a green lawn with soft yellow sound waves coming out of a speech bubble above it, and below it
three picture cards in a row (a dog, a bird and a ball) with the dog card slightly lifted and glowing, as if chosen. Keep the green
play button at the bottom right. File name: listen_and_choose.png."

## Checklist

- the same cartoon child is used within one scene sheet where the prompt says so; children look friendly, mixed skin tones and hair
- scenarios 1-6 and responses 7-12 of each sheet correspond in order (scenario N is answered by response N+6)
- nothing frightening, no tears beyond a small one, no injury beyond a scraped knee; the stranger is a plain neutral figure
- no text or letters anywhere, no speech bubbles with words; the only sign-like pictograms are the pedestrian figure and the stop hand
- nothing touching a cell edge or bleeding into a neighbour; if a piece is wrong, ask for a redo with the same prompt
