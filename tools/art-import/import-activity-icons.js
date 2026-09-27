// Imports the "menu-tile icon" layer (one illustrated button per game, shown on BuildingScreen's
// game-select grid). Distinct from import-map-art.js (map/building/road art) and cut-sheets.js
// (in-game gameplay object art) - see docs/kids-games/m4-handover.md's "three separate layers".
//
// Each activities_<prompt>.txt prompt (docs/kids-games/m4-chatgpt-prompts/) already asks ChatGPT
// for a real transparent background (a rounded-square button baked into the image), not magenta -
// so this importer only resizes and verifies the alpha is real, it never chroma-keys.
const path = require('path');
const fs = require('fs');
const sharp = require('sharp');

const downloads = process.env.MAP_ART_DIR || 'C:/Users/schei/Downloads';
const art = path.resolve(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art/activities');

// Every `"activities/<id>"` IconSprite value referenced by Rules/Activities.cs and
// App/Screens/StoreActivitiesScreen.cs (regenerate with:
// grep -hoE '"activities/[a-z0-9_]+"' EvasLearningWorld/Assets/Eva/Rules/Activities.cs EvasLearningWorld/Assets/Eva/App/Screens/StoreActivitiesScreen.cs | sed 's/"activities\///;s/"//' | sort -u)
const IDS = [
  'addition', 'animal_babies', 'animal_classification', 'avoid_obstacles', 'balance', 'balloon_popping',
  'beginning_sound', 'bridge_building', 'build_a_boat', 'build_a_car', 'build_a_house', 'build_a_robot',
  'build_a_rocket', 'build_a_word', 'cause_and_effect', 'classic_memory', 'collect_everything',
  'color_by_instruction', 'color_by_number', 'complete_the_picture', 'cooking_measures',
  'copy_the_construction', 'count', 'day_night', 'domestic_vs_wild', 'draw_what_you_hear',
  'drawing_challenges', 'dress_for_occasion', 'dress_for_weather', 'dress_the_character',
  'emotion_matching', 'empathy', 'facial_expression', 'find_the_differences', 'find_the_missing_piece',
  'finger_maze', 'finish_the_drawing', 'fishing', 'follow_1_instruction', 'follow_2_instructions',
  'follow_3_instructions', 'follow_letters', 'follow_numbers', 'follow_the_path', 'free_drawing',
  'fruit_catcher', 'geography', 'guided_drawing', 'healthy_vs_unhealthy', 'help_the_character',
  'human_senses', 'image_to_word', 'item_to_shadow', 'jigsaw', 'land_sea_air', 'letter_to_sound',
  'letterhunt', 'listen_and_choose', 'listen_for_details', 'living_vs_nonliving', 'magnet',
  'match_item_to_category', 'match_rotation', 'missing_letter', 'missing_number', 'multiplication',
  'number_line', 'number_ordering', 'numhunt', 'odd_one_out', 'one_more_one_less', 'pack_a_suitcase',
  'pattern_completion', 'perspective', 'plant_growth', 'platformer', 'recycling',
  'remember_the_location', 'remember_the_sequence', 'rhyming', 'road_safety', 'rotate_piece',
  'safety_scenarios', 'same_or_different', 'scrambled_word', 'seasons', 'sentence_builder',
  'sequence_ordering_bg', 'shopping', 'shortest_path', 'simon_says', 'simple_physics', 'sink_or_float',
  'social_situations', 'sort_laundry_chores', 'sorting', 'space', 'space_shooter', 'spot_the_object',
  'subtraction', 'tangram', 'tool_selection', 'trace_letters', 'trace_numbers', 'trace_shapes',
  'treasure_hunt', 'uppercase_to_lowercase', 'weather', 'whack_a_mole', 'what_would_you_do',
  'whats_behind', 'whats_disappeared', 'whats_missing', 'which_doesnt_make_sense', 'which_has_more',
  'which_is_bigger', 'word_to_image', 'zoofarm_covering', 'zoofarm_food', 'zoofarm_footprint',
  'zoofarm_habitat', 'zoofarm_mother', 'zoofarm_sound',
];

async function icon(id) {
  const src = path.join(downloads, `activities_${id.replace(/_/g, '')}.png`);
  if (!fs.existsSync(src)) return 'missing';
  const meta = await sharp(src).metadata();
  if (!meta.hasAlpha) { console.log(`REJECTED ${id}: no alpha channel (fake/checkered transparency?)`); return 'bad'; }
  const { data } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  if (data[3] > 20) { console.log(`REJECTED ${id}: corner pixel alpha=${data[3]}, not transparent`); return 'bad'; }
  await sharp(src).resize(1024, 1024, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png().toFile(path.join(art, `${id}.png`));
  console.log(`imported ${id}.png`);
  return 'ok';
}

(async () => {
  let ok = 0, missing = 0, bad = 0;
  for (const id of IDS) {
    const r = await icon(id);
    if (r === 'ok') ok++; else if (r === 'bad') bad++; else missing++;
  }
  console.log(`\n${ok} imported, ${missing} not yet downloaded, ${bad} rejected`);
})();
