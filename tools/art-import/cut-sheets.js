// Cuts the AI-generated sheets (items on a solid magenta background) into single transparent sprites.
//   node tools/art-import/cut-sheets.js furniture [sheet.png]   -> art/eva/house/out/items/<name>.png
//   node tools/art-import/cut-sheets.js icons [sheet.png]       -> art/eva/house/out/icons/<name>.png
// Items are found as connected blobs (so a wandering grid does not matter) and named in reading order
// (rows top to bottom, then left to right). Add --install to also write them into the game's Resources/Art.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const ROOT = path.join(__dirname, '../..');
const BOTTOM_MARGIN = 0.04; // keep in sync with HouseScreen.FeetMargin
const SHEETS = {
  furniture: { file: 'sheet_furniture.png', dir: 'house/ai', names: ['sofa', 'rug', 'table', 'plant', 'chest', 'bed', 'bookshelf'], outDir: 'house/out/items', resDir: 'objects', size: 512 },
  icons: { file: 'sheet_icons.png', dir: 'house/ai', names: ['arrow', 'dollhouse'], outDir: 'house/out/icons', resDir: 'icons', size: 256 },
  // Odd One Out + Item to Shadow's shared object catalogue (art/eva/playground/PROMPTS.md's "Attempt 1"
  // sheet). Reading order = the prompt's 1-29 list. No resDir: the two games need overlapping subsets
  // under different Resources/Art folders (oddoneout/, itemtoshadow/), done by a copy step, not here.
  playground_objects: {
    file: 'sheet_playground_objects.png', dir: 'playground/ai',
    names: [
      'cow', 'pig', 'sheep', 'horse', 'goat',
      'lion', 'tiger', 'bear', 'elephant', 'zebra',
      'car', 'bus', 'bike', 'truck', 'train',
      'apple', 'banana', 'orange', 'grape', 'pear',
      'ball', 'balloon', 'carrot', 'pencil', 'candle',
      'cat', 'dog', 'fox', 'rabbit',
    ],
    outDir: 'playground/out/objects', resDir: null, size: 512,
  },
  // Pattern Completion + What's Missing's shared 5-symbol pool (Rules/PatternCompletion.cs's
  // Symbols = {A,B,C,D,E}, sprite key `pattern/shape_<letter>`). This sheet came back with a real
  // transparent background already, not magenta.
  pattern_shapes: {
    file: 'sheet_pattern_shapes.png', dir: 'playground/ai', bg: 'alpha',
    names: ['shape_a', 'shape_b', 'shape_c', 'shape_d', 'shape_e'], // star, circle, triangle, square, heart
    outDir: 'playground/out/pattern', resDir: 'pattern', size: 512,
  },
  // Brain Gym Sorting (drop-sort prototype, art/eva/braingym/PROMPTS.md Batch 18): 12 items then 4 bins, 4 x 4 grid.
  braingym_sorting: {
    file: 'sheet_braingym_sorting.png', dir: 'braingym/ai',
    names: [
      'sortitem_apple', 'sortitem_banana', 'sortitem_orange', 'sortitem_carrot',
      'sortitem_tomato', 'sortitem_corn', 'sortitem_shirt', 'sortitem_pants',
      'sortitem_sock', 'sortitem_truck', 'sortitem_bus', 'sortitem_bike',
      'category_fruit', 'category_vegetable', 'category_clothes', 'category_vehicle',
    ],
    outDir: 'braingym/out/sorting', resDir: 'braingym', size: 512,
  },
  // Brain Gym drop-sort rollout (PROMPTS.md Batches 19, 13, 20, 21): Recycling waste + bins, icon set, category items, chores.
  braingym_recycling: {
    file: 'sheet_braingym_recycling.png', dir: 'braingym/ai',
    names: [
      'waste_bottle', 'waste_newspaper', 'waste_jar', 'waste_bananapeel', 'waste_can',
      'waste_cardboard', 'bin_plastic', 'bin_paper', 'bin_glass', 'bin_organic',
    ],
    outDir: 'braingym/out/recycling', resDir: 'braingym', size: 512,
  },
  braingym_icons: {
    file: 'sheet_braingym_icons.png', dir: 'braingym/ai',
    names: [
      'destination_house', 'destination_tree', 'destination_star', 'destination_flag',
      'categorylabel_music', 'categorylabel_sports', 'categorylabel_reading', 'categorylabel_art',
      'room_hamper', 'room_kitchen', 'room_bedroom', 'room_bathroom',
    ],
    outDir: 'braingym/out/icons', resDir: 'braingym', size: 512,
  },
  braingym_categoryitems: {
    file: 'sheet_braingym_categoryitems.png', dir: 'braingym/ai',
    names: ['catitem_guitar', 'catitem_ball', 'catitem_book', 'catitem_paintbrush', 'catitem_drum', 'catitem_bat'],
    outDir: 'braingym/out/categoryitems', resDir: 'braingym', size: 512,
  },
  braingym_chores: {
    file: 'sheet_braingym_chores.png', dir: 'braingym/ai',
    names: ['choreitem_shirt', 'choreitem_dish', 'choreitem_toy', 'choreitem_towel', 'choreitem_sock', 'choreitem_book'],
    outDir: 'braingym/out/chores', resDir: 'braingym', size: 512,
  },
  // Tangram's 7 placeholder pieces (Rules/Tangram.cs), sprite key `tangram/shape_<0-6>`.
  tangram_pieces: {
    file: 'sheet_tangram_pieces.png', dir: 'playground/ai',
    names: ['shape_0', 'shape_1', 'shape_2', 'shape_3', 'shape_4', 'shape_5', 'shape_6'],
    outDir: 'playground/out/tangram', resDir: 'tangram', size: 512,
  },
  // Rotate the Piece's 6 placeholder pieces (Rules/RotateThePiece.cs), sprite key `rotatepiece/piece_<0-5>`.
  rotatepiece_pieces: {
    file: 'sheet_rotatepiece_pieces.png', dir: 'playground/ai',
    names: ['piece_0', 'piece_1', 'piece_2', 'piece_3', 'piece_4', 'piece_5'],
    outDir: 'playground/out/rotatepiece', resDir: 'rotatepiece', size: 512,
  },
  // Which Doesn't Make Sense's 40-key Pool (Rules/WhichDoesntMakeSense.cs), sprite key
  // `whichdoesntmakesense/<key>`, generated in 5 batches of 8 (art/eva/playground/PROMPTS.md).
  whichdoesntmakesense_1: {
    file: 'sheet_whichdoesntmakesense_1.png', dir: 'playground/ai',
    names: ['cow_in_field', 'dog_in_yard', 'duck_in_pond', 'fish_in_tree', 'bird_in_nest', 'bee_in_hive', 'ant_in_anthill', 'fish_in_desert'],
    outDir: 'playground/out/whichdoesntmakesense', resDir: 'whichdoesntmakesense', size: 512,
  },
  whichdoesntmakesense_2: {
    file: 'sheet_whichdoesntmakesense_2.png', dir: 'playground/ai',
    names: ['boat_on_water', 'fish_in_water', 'duck_on_water', 'cow_in_ocean', 'car_on_road', 'bike_on_road', 'bus_on_road', 'fish_on_road'],
    outDir: 'playground/out/whichdoesntmakesense', resDir: 'whichdoesntmakesense', size: 512,
  },
  whichdoesntmakesense_3: {
    file: 'sheet_whichdoesntmakesense_3.png', dir: 'playground/ai',
    names: ['bird_flying_sky', 'plane_flying_sky', 'kite_flying_sky', 'elephant_flying_sky', 'penguin_on_ice', 'polar_bear_on_ice', 'seal_on_ice', 'camel_on_ice'],
    outDir: 'playground/out/whichdoesntmakesense', resDir: 'whichdoesntmakesense', size: 512,
  },
  whichdoesntmakesense_4: {
    file: 'sheet_whichdoesntmakesense_4.png', dir: 'playground/ai',
    names: ['cactus_in_desert', 'camel_in_desert', 'snake_in_desert', 'penguin_in_desert', 'monkey_in_jungle', 'parrot_in_jungle', 'snake_in_jungle', 'polar_bear_in_jungle'],
    outDir: 'playground/out/whichdoesntmakesense', resDir: 'whichdoesntmakesense', size: 512,
  },
  // Pool entry 10's "duck_in_pond2" is a distinct key from batch 1's "duck_in_pond" (same idea, kept
  // separate because Rules/WhichDoesntMakeSense.cs's Pool has two different pond entries).
  whichdoesntmakesense_5: {
    file: 'sheet_whichdoesntmakesense_5.png', dir: 'playground/ai',
    names: ['sheep_in_pasture', 'goat_in_pasture', 'horse_in_pasture', 'shark_in_pasture', 'frog_in_pond', 'turtle_in_pond', 'duck_in_pond2', 'lion_in_pond'],
    outDir: 'playground/out/whichdoesntmakesense', resDir: 'whichdoesntmakesense', size: 512,
  },
  // Zoo & Farm's 15-animal shared table (Rules/ZooFarm.cs), sprite key `zoofarm/animal_<id>` - the
  // "target" picture for every game except Animal -> Sound.
  zoofarm_animals: {
    file: 'sheet_zoofarm_animals.png', dir: 'zoofarm/ai',
    names: [
      'animal_cow', 'animal_lion', 'animal_duck', 'animal_owl', 'animal_sheep',
      'animal_fish', 'animal_horse', 'animal_eagle', 'animal_pig', 'animal_snake',
      'animal_chicken', 'animal_frog', 'animal_dog', 'animal_cat', 'animal_elephant',
    ],
    outDir: 'zoofarm/out/animals', resDir: 'zoofarm', size: 512,
  },
  // Babies game's choice pictures, sprite key `zoofarm/baby_<id>`.
  zoofarm_babies: {
    file: 'sheet_zoofarm_babies.png', dir: 'zoofarm/ai',
    names: [
      'baby_cow', 'baby_lion', 'baby_duck', 'baby_owl', 'baby_sheep',
      'baby_fish', 'baby_horse', 'baby_eagle', 'baby_pig', 'baby_snake',
      'baby_chicken', 'baby_frog', 'baby_dog', 'baby_cat', 'baby_elephant',
    ],
    outDir: 'zoofarm/out/babies', resDir: 'zoofarm', size: 512,
  },
  // Mother game's choice pictures, sprite key `zoofarm/mother_<id>`.
  zoofarm_mothers: {
    file: 'sheet_zoofarm_mothers.png', dir: 'zoofarm/ai',
    names: [
      'mother_cow', 'mother_lion', 'mother_duck', 'mother_owl', 'mother_sheep',
      'mother_fish', 'mother_horse', 'mother_eagle', 'mother_pig', 'mother_snake',
      'mother_chicken', 'mother_frog', 'mother_dog', 'mother_cat', 'mother_elephant',
    ],
    outDir: 'zoofarm/out/mothers', resDir: 'zoofarm', size: 512,
  },
  // Footprint game's choice pictures, sprite key `zoofarm/footprint_<id>`. Only the 11 animals with
  // a real footprint (owl, fish, eagle, snake excluded - see Rules/ZooFarm.cs's Animal.Footprint).
  zoofarm_footprints: {
    file: 'sheet_zoofarm_footprints.png', dir: 'zoofarm/ai',
    names: [
      'footprint_cow', 'footprint_lion', 'footprint_duck', 'footprint_sheep',
      'footprint_horse', 'footprint_pig', 'footprint_chicken', 'footprint_frog',
      'footprint_dog', 'footprint_cat', 'footprint_elephant',
    ],
    outDir: 'zoofarm/out/footprints', resDir: 'zoofarm', size: 512,
  },
  // Food game's choice pictures, sprite key `zoofarm/food_<name>`.
  zoofarm_foods: {
    file: 'sheet_zoofarm_foods.png', dir: 'zoofarm/ai',
    names: [
      'food_grass', 'food_meat', 'food_seeds', 'food_mice',
      'food_plankton', 'food_hay', 'food_feed', 'food_insects',
      'food_kibble', 'food_catfood', 'food_leaves',
    ],
    outDir: 'zoofarm/out/foods', resDir: 'zoofarm', size: 512,
  },
  // Habitat game's choice pictures, sprite key `zoofarm/habitat_<name>`.
  zoofarm_habitats: {
    file: 'sheet_zoofarm_habitats.png', dir: 'zoofarm/ai',
    names: ['habitat_farm', 'habitat_savanna', 'habitat_pond', 'habitat_forest', 'habitat_ocean', 'habitat_mountain', 'habitat_jungle'],
    outDir: 'zoofarm/out/habitats', resDir: 'zoofarm', size: 512,
  },
  // Covering game's choice pictures, sprite key `zoofarm/covering_<name>`.
  zoofarm_coverings: {
    file: 'sheet_zoofarm_coverings.png', dir: 'zoofarm/ai',
    names: ['covering_fur', 'covering_feathers', 'covering_wool', 'covering_scales', 'covering_skin'],
    outDir: 'zoofarm/out/coverings', resDir: 'zoofarm', size: 512,
  },
  // Domestic vs Wild / Land-Sea-Air / Classification's shared sorting buckets, sprite key
  // `zoofarm/bucket_<name>` - exact keys per Rules/ZooFarm.cs (Realm.ToString().ToLowerInvariant()
  // for land/sea/air, "domestic_"/"wild_" + "land"/"water" for Classification's compound buckets).
  zoofarm_buckets: {
    file: 'sheet_zoofarm_buckets.png', dir: 'zoofarm/ai',
    names: [
      'bucket_domestic', 'bucket_wild', 'bucket_land',
      'bucket_sea', 'bucket_air', 'bucket_domestic_land',
      'bucket_domestic_water', 'bucket_wild_land', 'bucket_wild_water',
    ],
    outDir: 'zoofarm/out/buckets', resDir: 'zoofarm', size: 512,
  },
  // Geography's Flag mode, sprite key `geo/flag_<id>`.
  geo_flags: {
    file: 'sheet_geo_flags.png', dir: 'zoofarm/ai',
    names: [
      'flag_romania', 'flag_france', 'flag_spain', 'flag_usa',
      'flag_brazil', 'flag_egypt', 'flag_kenya', 'flag_china',
      'flag_japan', 'flag_india', 'flag_australia',
    ],
    outDir: 'zoofarm/out/geo_flags', resDir: 'geo', size: 512,
  },
  // Geography's Continent mode, sprite key `geo/continent_<name>`.
  geo_continents: {
    file: 'sheet_geo_continents.png', dir: 'zoofarm/ai',
    names: ['continent_europe', 'continent_north_america', 'continent_south_america', 'continent_africa', 'continent_asia', 'continent_oceania'],
    outDir: 'zoofarm/out/geo_continents', resDir: 'geo', size: 512,
  },
  // Geography's Landmark mode, sprite key `geo/landmark_<id>` (reuses the country id). Richer scene
  // content than the other sheets: neighbouring landmarks' foliage/water touch across the grid, and a
  // taller item (Eiffel Tower, Statue of Liberty) overhangs into the row below - plain whole-image blob
  // detection either merges a whole row into one blob or picks up bleed from the row above. `grid` cuts
  // per-nominal-cell instead: largest connected blob within each cell (ignores small bleed fragments
  // poking in from a neighbour), with the last item of an incomplete row allowed to overflow into the
  // row's empty trailing cell(s) (Sydney Opera House's boat extended slightly past its own column).
  geo_landmarks: {
    file: 'sheet_geo_landmarks.png', dir: 'zoofarm/ai',
    names: [
      'landmark_romania', 'landmark_france', 'landmark_spain', 'landmark_usa',
      'landmark_brazil', 'landmark_egypt', 'landmark_kenya', 'landmark_china',
      'landmark_japan', 'landmark_india', 'landmark_australia',
    ],
    grid: { cols: 4, rows: 3 },
    outDir: 'zoofarm/out/geo_landmarks', resDir: 'geo', size: 512,
  },
  // Science Lab's Sink or Float target objects, sprite key `sciencelab/object_<id>`.
  sciencelab_sinkfloat_objects: {
    file: 'sheet_sciencelab_sinkfloat_objects.png', dir: 'sciencelab/ai',
    names: [
      'object_rock', 'object_leaf', 'object_key', 'object_balloon',
      'object_coin', 'object_cork', 'object_spoon', 'object_sponge',
      'object_marble', 'object_rubber_duck', 'object_hammer', 'object_apple',
    ],
    outDir: 'sciencelab/out/sinkfloat_objects', resDir: 'sciencelab', size: 512,
  },
  // Science Lab's Magnet target objects, sprite key `sciencelab/object_<id>`.
  sciencelab_magnet_objects: {
    file: 'sheet_sciencelab_magnet_objects.png', dir: 'sciencelab/ai',
    names: [
      'object_nail', 'object_pencil', 'object_paperclip', 'object_leaf2',
      'object_scissors', 'object_button', 'object_fork', 'object_plastic_cup',
      'object_bottle_cap', 'object_wooden_block', 'object_screw', 'object_cotton_ball',
    ],
    outDir: 'sciencelab/out/magnet_objects', resDir: 'sciencelab', size: 512,
  },
  // Science Lab's Living vs Non-Living target objects, sprite key `sciencelab/object_<id>`.
  sciencelab_livingnonliving_objects: {
    file: 'sheet_sciencelab_livingnonliving_objects.png', dir: 'sciencelab/ai',
    names: [
      'object_dog', 'object_stone', 'object_tree', 'object_car',
      'object_flower', 'object_chair', 'object_goldfish', 'object_cloud',
      'object_bird', 'object_ball', 'object_ant', 'object_book',
      'object_cat', 'object_table',
    ],
    outDir: 'sciencelab/out/livingnonliving_objects', resDir: 'sciencelab', size: 512,
  },
  // Science Lab's Healthy vs Unhealthy target foods, sprite key `sciencelab/food_<id>`.
  sciencelab_healthyunhealthy_foods: {
    file: 'sheet_sciencelab_healthyunhealthy_foods.png', dir: 'sciencelab/ai',
    names: [
      'food_apple', 'food_candy', 'food_broccoli', 'food_soda',
      'food_carrot', 'food_chips', 'food_banana', 'food_cake',
      'food_yogurt', 'food_donut', 'food_grilled_fish', 'food_fries',
      'food_salad', 'food_pizza',
    ],
    grid: { cols: 4, rows: 4 },
    outDir: 'sciencelab/out/healthyunhealthy_foods', resDir: 'sciencelab', size: 512,
  },
  // Sink or Float / Magnet / Living vs Non-Living / Healthy vs Unhealthy's shared sorting buckets,
  // sprite key `sciencelab/bucket_<name>`.
  sciencelab_buckets: {
    file: 'sheet_sciencelab_buckets.png', dir: 'sciencelab/ai',
    names: [
      'bucket_sink', 'bucket_float', 'bucket_magnetic', 'bucket_nonmagnetic',
      'bucket_living', 'bucket_nonliving', 'bucket_healthy', 'bucket_unhealthy',
    ],
    outDir: 'sciencelab/out/buckets', resDir: 'sciencelab', size: 512,
  },
  // Human Senses: organs (target) + sense symbols (choice), sprite keys `sciencelab/organ_<id>` and
  // `sciencelab/sense_<name>`. One sheet, two different prefixes by row - split manually below.
  sciencelab_senses: {
    file: 'sheet_sciencelab_senses.png', dir: 'sciencelab/ai',
    names: [
      'organ_eye', 'organ_ear', 'organ_nose', 'organ_tongue', 'organ_hand',
      'sense_sight', 'sense_hearing', 'sense_smell', 'sense_taste', 'sense_touch',
    ],
    outDir: 'sciencelab/out/senses', resDir: 'sciencelab', size: 512,
  },
  // Weather (identify) + Dress for the Weather's target scenes, sprite key `sciencelab/weather_<name>`.
  sciencelab_weather: {
    file: 'sheet_sciencelab_weather.png', dir: 'sciencelab/ai',
    names: [
      'weather_sunny', 'weather_rainy', 'weather_cloudy', 'weather_snowy',
      'weather_windy', 'weather_stormy', 'weather_hot', 'weather_cold',
    ],
    grid: { cols: 4, rows: 2 },
    outDir: 'sciencelab/out/weather', resDir: 'sciencelab', size: 512,
  },
  // Small combo sheet: Dress for the Weather's clothing choices (`sciencelab/clothing_<name>`),
  // Cooking Measures' bucket levels (`sciencelab/measure_<name>`), Seasons' choices
  // (`sciencelab/season_<name>`), Day/Night's choices (`sciencelab/daynight_<name>`). Different
  // prefixes per item, assigned below by position - grid mode since the measuring-cup handles and
  // the night icon's badge background are close enough to risk touching their neighbours.
  sciencelab_smallicons: {
    file: 'sheet_sciencelab_smallicons.png', dir: 'sciencelab/ai',
    names: [
      'clothing_sunhat', 'clothing_raincoat', 'clothing_mittens', 'clothing_jacket', 'clothing_shorts',
      'clothing_scarf', 'measure_full', 'measure_half', 'measure_empty', 'season_spring',
      'season_summer', 'season_fall', 'season_winter', 'daynight_day', 'daynight_night',
    ],
    grid: { cols: 5, rows: 3 },
    outDir: 'sciencelab/out/smallicons', resDir: 'sciencelab', size: 512,
  },
  // Cause and Effect's cause half, sprite key `sciencelab/cause_<id>`. Richer action scenes -
  // grid mode since motion swirls/splashes could bleed toward a neighbouring cell.
  sciencelab_causes: {
    file: 'sheet_causes.png', dir: 'sciencelab/ai',
    names: [
      'cause_rain', 'cause_drop_glass', 'cause_water_plant', 'cause_wind',
      'cause_sun_on_icecream', 'cause_kick_ball', 'cause_press_switch', 'cause_pin_balloon',
    ],
    grid: { cols: 4, rows: 2 },
    outDir: 'sciencelab/out/causes', resDir: 'sciencelab', size: 512,
  },
  // Cause and Effect's effect half, sprite key `sciencelab/effect_<id>`, same order as the causes
  // above so each cause's correct answer lines up. Items 7-8 (light_on, popped_balloon) came back
  // wrong on the first pass (a glowing switch instead of a bulb; a balloon that read as bitten fruit
  // rather than burst) - see sciencelab_effects_fix below for the replacement pair; this entry's
  // effect_light_on/effect_popped_balloon outputs get overwritten by that one.
  sciencelab_effects: {
    file: 'sheet_effects.png', dir: 'sciencelab/ai',
    names: [
      'effect_wet_ground', 'effect_broken_glass', 'effect_grown_plant', 'effect_flying_kite',
      'effect_melted_icecream', 'effect_rolling_ball', 'effect_light_on', 'effect_popped_balloon',
    ],
    grid: { cols: 4, rows: 2 },
    outDir: 'sciencelab/out/effects', resDir: 'sciencelab', size: 512,
  },
  // Corrective 2-item re-do of effect_light_on and effect_popped_balloon from the sheet above.
  sciencelab_effects_fix: {
    file: 'sheet_effects_fix.png', dir: 'sciencelab/ai',
    names: ['effect_light_on', 'effect_popped_balloon'],
    grid: { cols: 2, rows: 1 },
    outDir: 'sciencelab/out/effects_fix', resDir: 'sciencelab', size: 512,
  },
  // Cooking Measures' 9 cup targets, sprite key `sciencelab/cup_<a-i>`. The prompt laid the sheet out
  // by ingredient (water/flour/milk x full/half/empty) for a sane reading order, but
  // `CookingMeasuresItems` groups ids by fill-level bucket instead (a/b/c=full, d/e/f=half,
  // g/h/i=empty, one ingredient per bucket slot for visual variety) - names below are remapped from
  // the prompt's by-ingredient reading order to the code's actual id order, per the note in
  // sciencelab/PROMPTS.md right after batch 11.
  sciencelab_cups: {
    file: 'sheet_cups.png', dir: 'sciencelab/ai',
    names: [
      'cup_a', 'cup_d', 'cup_g', 'cup_b', 'cup_e', 'cup_h', 'cup_c', 'cup_f', 'cup_i',
    ],
    grid: { cols: 3, rows: 3 },
    outDir: 'sciencelab/out/cups', resDir: 'sciencelab', size: 512,
  },
  // Seasons' first 6 activity scenes (of 12), sprite key `sciencelab/activity_<id>`. Grid mode -
  // foliage/detail elements (butterfly, falling leaves, plant sprouts) sit close enough to risk
  // bleeding into neighbours.
  sciencelab_seasons_a: {
    file: 'sheet_seasons_a.png', dir: 'sciencelab/ai',
    names: [
      'activity_blooming_flowers', 'activity_swimming', 'activity_falling_leaves',
      'activity_building_snowman', 'activity_planting_seeds', 'activity_sandcastle',
    ],
    grid: { cols: 3, rows: 2 },
    outDir: 'sciencelab/out/seasons_a', resDir: 'sciencelab', size: 512,
  },
  // Seasons' remaining 6 activity scenes (of 12), sprite key `sciencelab/activity_<id>`. Grid mode -
  // apples on the tree/in the basket and scattered leaves risk bleeding into neighbours.
  sciencelab_seasons_b: {
    file: 'sheet_seasons_b.png', dir: 'sciencelab/ai',
    names: [
      'activity_picking_apples', 'activity_sledding', 'activity_rainbow',
      'activity_sunbathing', 'activity_raking_leaves', 'activity_wearing_coat',
    ],
    grid: { cols: 3, rows: 2 },
    outDir: 'sciencelab/out/seasons_b', resDir: 'sciencelab', size: 512,
  },
  // Workshop (art/eva/workshop/PROMPTS.md). Sprite keys `workshop/<name>`, names straight from Rules/Workshop.cs.
  workshop_parts_a: {
    file: 'sheet_workshop_parts_a.png', dir: 'workshop/ai',
    names: ['part_car_body', 'part_car_wheels', 'part_car_windows', 'part_rocket_body', 'part_rocket_fins', 'part_rocket_nosecone',
      'part_house_walls', 'part_house_roof', 'part_house_door', 'part_boat_hull', 'part_boat_sail', 'part_boat_mast'],
    outDir: 'workshop/out', resDir: 'workshop', size: 512,
  },
  workshop_parts_b: {
    file: 'sheet_workshop_parts_b.png', dir: 'workshop/ai',
    gap: 30, // the robot's two arms are one piece but two separate blobs
    names: ['part_robot_body', 'part_robot_arms', 'part_robot_head', 'part_bridge_block_a', 'part_bridge_block_b', 'part_bridge_block_c',
      'part_physics_ramp', 'part_physics_block', 'part_physics_balltrack'],
    outDir: 'workshop/out', resDir: 'workshop', size: 512,
  },
  workshop_tests: {
    file: 'sheet_workshop_tests.png', dir: 'workshop/ai',
    names: ['test_car', 'test_rocket', 'test_house', 'test_boat', 'test_robot', 'test_bridge', 'test_simplephysics'],
    outDir: 'workshop/out', resDir: 'workshop', size: 512,
  },
  workshop_toolselection: {
    file: 'sheet_workshop_toolselection.png', dir: 'workshop/ai',
    names: ['problem_cut_apple', 'problem_hammer_nail', 'problem_tighten_screw', 'problem_cut_paper', 'problem_cut_wood', 'problem_tighten_bolt',
      'tool_knife', 'tool_hammer', 'tool_screwdriver', 'tool_scissors', 'tool_saw', 'tool_wrench'],
    outDir: 'workshop/out', resDir: 'workshop', size: 512,
  },
  // The first sheet came back with 13 items (a second wilting plant, middle of row 2): '_' names are cut but never installed.
  workshop_helpthecharacter: {
    file: 'sheet_workshop_helpthecharacter.png', dir: 'workshop/ai',
    names: ['scenario_hungry_dog', 'scenario_thirsty_plant', 'scenario_cold_bird', 'scenario_lost_kitten', 'scenario_messy_room', '_extra_plant', 'scenario_flat_tire',
      'action_give_bone', 'action_water_it', 'action_give_nest', 'action_lead_home', 'action_tidy_up', 'action_pump_it'],
    outDir: 'workshop/out', resDir: 'workshop', size: 512,
  },
  // Light/medium/heavy x left/right tipped scales (code ids light_/medium_/heavy_), the level scale (unused by code yet), 3 weights.
  workshop_balance: {
    file: 'sheet_workshop_balance.png', dir: 'workshop/ai',
    // The right-hand tilts that came back from ChatGPT were not distinct enough (slight 12.5 vs medium 17.7 degrees), so the
    // right sprites are mirrors of the left ones (the scale is symmetric): left tilts measure 7.6 / 17.5 / 29.7 degrees.
    sameScale: ['weight_small_weight', 'weight_medium_weight', 'weight_large_weight'],
    mirror: { scale_light_right: 'scale_light_left', scale_heavy_right: 'scale_heavy_left', scale_medium_right: 'scale_medium_left' },
    names: ['scale_light_left', 'scale_heavy_left', 'scale_light_right', 'scale_heavy_right', 'scale_medium_left', 'scale_medium_right',
      'scale_level', 'weight_small_weight', 'weight_medium_weight', 'weight_large_weight'],
    outDir: 'workshop/out', resDir: 'workshop', size: 512,
  },
  // Art Studio (art/eva/artstudio/PROMPTS.md). Sprite keys `artstudio/<name>`.
  artstudio_swatches_stamps: {
    file: 'sheet_artstudio_swatches_stamps.png', dir: 'artstudio/ai', bg: 'flood', merge: true, // the paint dabs have small splatter droplets
    names: ['swatch_red', 'swatch_orange', 'swatch_yellow', 'swatch_green', 'swatch_blue', 'swatch_purple', 'swatch_brown',
      'stamp_circle', 'stamp_star', 'stamp_heart', 'stamp_sun', 'stamp_tree', 'stamp_flower'],
    outDir: 'artstudio/out', resDir: 'artstudio', size: 512,
  },
  artstudio_regions: {
    file: 'sheet_artstudio_regions.png', dir: 'artstudio/ai', merge: true, blobGap: 2, // sun rays are separate blobs; the numbered wall and tree almost touch
    names: ['plain_region_roof', 'plain_region_wall', 'plain_region_door', 'plain_region_window', 'plain_region_sun', 'plain_region_tree',
      'numbered_region_roof', 'numbered_region_wall', 'numbered_region_door', 'numbered_region_window', 'numbered_region_sun', 'numbered_region_tree'],
    outDir: 'artstudio/out', resDir: 'artstudio', size: 512,
  },
  artstudio_coloringpage: {
    file: 'sheet_artstudio_coloringpage.png', dir: 'artstudio/ai', merge: true,
    names: ['coloringpage_blank', 'coloringpage_done'],
    outDir: 'artstudio/out', resDir: 'artstudio', size: 1024,
  },
  // Six complete pictures; split down the middle into half_<id> (left half) and piece_<id> (right half) so they line up exactly.
  artstudio_full: {
    file: 'sheet_artstudio_full.png', dir: 'artstudio/ai', bg: 'flood', merge: true,
    names: ['full_sun', 'full_flower', 'full_house', 'full_tree', 'full_car', 'full_balloon'],
    split: { full_sun: 'sun', full_flower: 'flower', full_house: 'house', full_tree: 'tree', full_car: 'car', full_balloon: 'balloon' },
    outDir: 'artstudio/out', resDir: 'artstudio', size: 512,
  },
  artstudio_scenes: {
    file: 'sheet_artstudio_scenes.png', dir: 'artstudio/ai',
    names: ['scene_scene1', 'scene_scene2', 'scene_scene3', 'scene_scene4', 'scene_scene5', 'scene_scene6'],
    outDir: 'artstudio/out', resDir: 'artstudio', size: 512,
  },
  artstudio_steps: {
    file: 'sheet_artstudio_steps.png', dir: 'artstudio/ai',
    names: ['step_roof', 'step_walls', 'step_door', 'step_windows', 'challenge_step_body', 'challenge_step_head', 'challenge_step_arms', 'challenge_step_legs'],
    sameScale: ['step_roof', 'step_walls', 'step_door', 'step_windows', 'challenge_step_body', 'challenge_step_head', 'challenge_step_arms', 'challenge_step_legs'],
    outDir: 'artstudio/out', resDir: 'artstudio', size: 512,
  },
  // Arcade (art/eva/arcade/PROMPTS.md). Sprite keys `arcade/<name>`.
  arcade_platformer: {
    file: 'sheet_arcade_platformer.png', dir: 'arcade/ai', bg: 'flood', // JPEG from Gemini
    names: ['platform_p1', 'platform_p2', 'platform_p3', 'platform_p4', 'platform_p5', 'platform_p6'],
    outDir: 'arcade/out', resDir: 'arcade', size: 512,
  },
  arcade_props: {
    file: 'sheet_arcade_props.png', dir: 'arcade/ai', bg: 'flood', merge: true, // confetti and sparkles are many small blobs
    names: ['prop_basket', 'prop_hook', 'prop_mole_hole', 'prop_dirt_mound', 'prop_pop', 'prop_shot', 'prop_sparkle', 'prop_lilypad'],
    outDir: 'arcade/out', resDir: 'arcade', size: 512,
  },
  // Science Lab batches 14-18 (art/eva/sciencelab/PROMPTS.md). Day/Night activity scenes, `sciencelab/activity_<id>`.
  sciencelab_daynight_a: {
    file: 'sheet_sciencelab_daynight_a.png', dir: 'sciencelab/ai', bg: 'flood',
    names: ['activity_sun', 'activity_breakfast', 'activity_school_bus', 'activity_playing_outside', 'activity_daytime_walk'],
    outDir: 'sciencelab/out/daynight_a', resDir: 'sciencelab', size: 512,
  },
  sciencelab_daynight_b: {
    file: 'sheet_sciencelab_daynight_b.png', dir: 'sciencelab/ai', bg: 'flood',
    names: ['activity_moon', 'activity_stars', 'activity_sleeping', 'activity_pajamas', 'activity_owl'],
    outDir: 'sciencelab/out/daynight_b', resDir: 'sciencelab', size: 512,
  },
  // Space items `sciencelab/space_<name>` (the sun's rays and the star's sparkles are separate blobs: merge).
  sciencelab_space: {
    file: 'sheet_sciencelab_space.png', dir: 'sciencelab/ai', bg: 'flood', merge: true,
    names: ['space_sun', 'space_earth', 'space_moon', 'space_mars', 'space_star', 'space_rocket', 'space_astronaut', 'space_saturn'],
    outDir: 'sciencelab/out/space', resDir: 'sciencelab', size: 512,
  },
  // Plant growth stages `sciencelab/stage_<name>`. The generated 4th stage is a flower bud and the 5th an open flower
  // (no fruit); they are installed as stage_flower / stage_fruit so the growth order still reads.
  sciencelab_plantgrowth: {
    file: 'sheet_sciencelab_plantgrowth.png', dir: 'sciencelab/ai', bg: 'flood', merge: true, singleRow: true,
    names: ['stage_seed', 'stage_sprout', 'stage_seedling', 'stage_flower', 'stage_fruit'],
    sameScale: ['stage_seed', 'stage_sprout', 'stage_seedling', 'stage_flower', 'stage_fruit'],
    outDir: 'sciencelab/out/plantgrowth', resDir: 'sciencelab', size: 512,
  },
  // Day/night panels and the space orbit backdrop (not used by code yet).
  sciencelab_bins_orbit: {
    file: 'sheet_sciencelab_bins_orbit.png', dir: 'sciencelab/ai', bg: 'flood', merge: true,
    names: ['bin_day', 'bin_night', 'orbit_backdrop'],
    outDir: 'sciencelab/out/bins_orbit', resDir: 'sciencelab', size: 1024,
  },
  // Arcade batches 1-6 (art/eva/arcade/PROMPTS.md): targets (cards) first, then choices, ids from Rules/Arcade.cs.
  arcade_balloonpopping: {
    file: 'sheet_arcade_balloonpopping.png', dir: 'arcade/ai', bg: 'flood', merge: true, blobGap: 1, // the strings are thin
    names: ['balloonrule_one', 'balloonrule_two', 'balloonrule_three', 'balloonrule_four', 'balloonrule_five', 'balloonrule_six', 'balloon_odd', 'balloon_even'],
    outDir: 'arcade/out', resDir: 'arcade', size: 512,
  },
  // The sheet has a 7th portrait (a second reddish mole at the right of row 2): '_extra_mole' is cut but not installed.
  arcade_whackamole: {
    file: 'sheet_arcade_whackamole.png', dir: 'arcade/ai', bg: 'flood', merge: true, blobGap: 1,
    seeds: [[0.125, 0.198], [0.385, 0.198], [0.620, 0.198], [0.860, 0.198], [0.140, 0.502], [0.500, 0.502], [0.870, 0.502], [0.095, 0.806], [0.260, 0.806], [0.425, 0.806], [0.580, 0.806], [0.740, 0.806], [0.900, 0.806]],
    names: ['molecard_a', 'molecard_b', 'molecard_c', 'molecard_d', 'molecard_e', 'molecard_f', '_extra_mole',
      'mole_a', 'mole_b', 'mole_c', 'mole_d', 'mole_e', 'mole_f'],
    outDir: 'arcade/out', resDir: 'arcade', size: 512,
  },
  arcade_fishing: {
    file: 'sheet_arcade_fishing.png', dir: 'arcade/ai', bg: 'flood', merge: true, blobGap: 1, // bubbles around the swimming fish
    seeds: [[0.150, 0.167], [0.385, 0.167], [0.620, 0.167], [0.850, 0.167], [0.155, 0.453], [0.395, 0.453], [0.095, 0.727], [0.265, 0.713], [0.425, 0.700], [0.590, 0.713], [0.750, 0.700], [0.910, 0.713]],
    names: ['fishcard_red', 'fishcard_blue', 'fishcard_green', 'fishcard_yellow', 'fishcard_purple', 'fishcard_orange',
      'fish_red', 'fish_blue', 'fish_green', 'fish_yellow', 'fish_purple', 'fish_orange'],
    outDir: 'arcade/out', resDir: 'arcade', size: 512,
  },
  arcade_spaceshooter: {
    file: 'sheet_arcade_spaceshooter.png', dir: 'arcade/ai', bg: 'flood', merge: true, blobGap: 1, // the ships' flames
    cutY: [0.73], // the flame tips of the 3rd ship row touch the ships below
    seeds: [[0.150, 0.140], [0.380, 0.140], [0.615, 0.140], [0.845, 0.140], [0.150, 0.373], [0.375, 0.373], [0.140, 0.627], [0.380, 0.627], [0.620, 0.627], [0.860, 0.627], [0.140, 0.867], [0.380, 0.867]],
    names: ['shapecard_circle', 'shapecard_square', 'shapecard_triangle', 'shapecard_star', 'shapecard_heart', 'shapecard_diamond',
      'ship_circle', 'ship_square', 'ship_triangle', 'ship_star', 'ship_heart', 'ship_diamond'],
    outDir: 'arcade/out', resDir: 'arcade', size: 512,
  },
  arcade_fruitcatcher: {
    file: 'sheet_arcade_fruitcatcher.png', dir: 'arcade/ai', bg: 'flood', merge: true, blobGap: 1, // motion lines around the falling fruit
    seeds: [[0.155, 0.160], [0.380, 0.160], [0.615, 0.153], [0.845, 0.153], [0.145, 0.427], [0.390, 0.427], [0.140, 0.667], [0.370, 0.653], [0.620, 0.653], [0.860, 0.653], [0.145, 0.867], [0.390, 0.867]],
    names: ['fruitcard_apple', 'fruitcard_banana', 'fruitcard_grape', 'fruitcard_orange', 'fruitcard_pear', 'fruitcard_plum',
      'fruit_apple', 'fruit_banana', 'fruit_grape', 'fruit_orange', 'fruit_pear', 'fruit_plum'],
    outDir: 'arcade/out', resDir: 'arcade', size: 512,
  },
  arcade_treasurehunt: {
    file: 'sheet_arcade_treasurehunt.png', dir: 'arcade/ai', bg: 'flood', merge: true, blobGap: 1, // sparkles and water splashes
    seeds: [[0.145, 0.153], [0.380, 0.153], [0.620, 0.153], [0.855, 0.153], [0.145, 0.427], [0.380, 0.427], [0.145, 0.667], [0.370, 0.720], [0.610, 0.707], [0.850, 0.720], [0.210, 0.867], [0.505, 0.887]],
    names: ['mapsymbol_balloon', 'mapsymbol_mole', 'mapsymbol_fish', 'mapsymbol_ship', 'mapsymbol_fruit', 'mapsymbol_gem',
      'treasure_balloon', 'treasure_mole', 'treasure_fish', 'treasure_ship', 'treasure_fruit', 'treasure_gem'],
    outDir: 'arcade/out', resDir: 'arcade', size: 512,
  },
  // M5 character wardrobe (art/character/PROMPTS.md, STYLE.md). Sprite keys `character/<name>`. `tight`: each sprite is its
  // own tight bounding box (not a square) so the rig can fit it into its slot rectangle with preserveAspect. `scale` instead of
  // fit-to-512 keeps one shared pixel scale across sheets (0.96: a face is about 400 px wide) so hair and glasses keep their
  // size relative to the face; RigFactory sizes those by HeadSize / 400 per pixel.
  character_faces: {
    file: 'sheet_character_faces.png', dir: 'character/ai', bg: 'flood', merge: true, tight: true, scale: 0.96,
    names: ['face_boy_0', 'face_boy_1', 'face_boy_2', 'face_girl_0', 'face_girl_1', 'face_girl_2'],
    outDir: 'character/out', resDir: 'character', size: 512,
  },
  character_hairboy: {
    file: 'sheet_character_hairboy.png', dir: 'character/ai', bg: 'flood', merge: true, tight: true, scale: 0.96,
    names: ['hairboy_0_back', 'hairboy_1_back', 'hairboy_2_back', 'hairboy_0_front', 'hairboy_1_front', 'hairboy_2_front'],
    outDir: 'character/out', resDir: 'character', size: 512,
  },
  // Girl styles in code order (CharacterCreator.GirlHairStyles = 4): 0 long, 1 short, 2 ponytail, 3 pigtails.
  character_hairgirl: {
    file: 'sheet_character_hairgirl.png', dir: 'character/ai', bg: 'flood', merge: true, tight: true, scale: 0.96,
    names: ['hairgirl_0_back', 'hairgirl_1_back', 'hairgirl_2_back', 'hairgirl_3_back',
      'hairgirl_0_front', 'hairgirl_1_front', 'hairgirl_2_front', 'hairgirl_3_front'],
    outDir: 'character/out', resDir: 'character', size: 512,
  },
  character_tops: {
    file: 'sheet_character_tops.png', dir: 'character/ai', bg: 'flood', merge: true, tight: true,
    names: ['top_boy_0', 'top_boy_1', 'top_boy_2', 'top_boy_3', 'top_girl_0', 'top_girl_1', 'top_girl_2', 'top_girl_3'],
    outDir: 'character/out', resDir: 'character', size: 512,
  },
  character_bottoms: {
    file: 'sheet_character_bottoms.png', dir: 'character/ai', bg: 'flood', merge: true, tight: true,
    names: ['bottom_boy_0', 'bottom_boy_1', 'bottom_boy_2', 'bottom_girl_0', 'bottom_girl_1', 'bottom_girl_2'],
    outDir: 'character/out', resDir: 'character', size: 512,
  },
  character_dresses_glasses: {
    file: 'sheet_character_dresses_glasses.png', dir: 'character/ai', bg: 'flood', merge: true, tight: true, scale: 0.96,
    names: ['dress_girl_0', 'dress_girl_1', 'dress_girl_2', 'glasses_0', 'glasses_1', 'glasses_2'],
    outDir: 'character/out', resDir: 'character', size: 512,
  },
  character_shoes: {
    file: 'sheet_character_shoes.png', dir: 'character/ai', bg: 'flood', merge: true, tight: true,
    names: ['shoes_boy_0', 'shoes_boy_1', 'shoes_boy_2', 'shoes_girl_0', 'shoes_girl_1', 'shoes_girl_2'],
    outDir: 'character/out', resDir: 'character', size: 512,
  },
  // Store's games-menu button (art/eva/icons/PROMPTS.md), made with Gemini (JPEG).
  icons_activities: {
    file: 'sheet_icons_activities.png', dir: 'icons/ai', bg: 'flood', merge: true,
    names: ['activities'],
    outDir: 'icons/out', resDir: 'icons', size: 256,
  },
};

// Edge pixels are a blend of the picture and the magenta background: estimate the background share from the pixel's
// magenta-ness (min(r,b)-g is about 255 per unit of background share for a neutral picture), subtract it, then clamp
// red/blue back toward green so no pink/violet halo survives on the cut-out.
function unmixMagenta(data, i, m) {
  const c = Math.max(0.35, 1 - Math.max(0, m) / 255);
  const r = (data[i] - (1 - c) * 255) / c, g = data[i + 1] / c, b = (data[i + 2] - (1 - c) * 255) / c;
  const cl = (v) => Math.max(0, Math.min(255, v));
  data[i + 1] = cl(g);
  data[i] = cl(Math.min(r, g + 40));
  data[i + 2] = cl(Math.min(b, g + 40));
}

// bg 'magenta' (default) chroma-keys a solid #ff00ff background to transparent; bg 'alpha' trusts a
// sheet that already came back with a real transparent background (some tools produce this directly)
// and only normalises it to straight RGBA.
async function keyed(file, bg = 'magenta') {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  if (bg === 'magenta') {
    for (let i = 0; i < data.length; i += 4) {
      const r = data[i], g = data[i + 1], b = data[i + 2];
      const m = Math.min(r, b) - g;
      if (m > 110) data[i + 3] = 0;
      else if (m > 45) { data[i + 3] = Math.round(255 * (110 - m) / 65); unmixMagenta(data, i, m); }
    }
  }
  if (bg === 'flood') {
    // Only background connected to the sheet's border is removed, so pictures that are themselves magenta-ish (a purple paint
    // dab, a pink flower) survive. Edge pixels next to the removed background get the usual soft key.
    const w = info.width, h = info.height;
    const m = new Int16Array(w * h), d = new Int16Array(w * h); // m: magenta-ness; d: distance to pure magenta
    for (let i = 0; i < w * h; i++) { const r = data[i * 4], g = data[i * 4 + 1], b = data[i * 4 + 2]; m[i] = Math.min(r, b) - g; d[i] = Math.hypot(255 - r, g, 255 - b); }
    const bgMask = new Uint8Array(w * h);
    const stack = [];
    // flood only through pixels CLOSE to pure magenta (a purple or pink picture is magenta-ish but not that close), and also
    // treat very-near-magenta pockets enclosed by the picture (between a flower's leaves) as background.
    const push = (p) => { if (!bgMask[p] && d[p] < 100) { bgMask[p] = 1; stack.push(p); } };
    for (let p = 0; p < w * h; p++) if (d[p] < 50 && !bgMask[p]) { bgMask[p] = 1; }
    for (let x = 0; x < w; x++) { push(x); push((h - 1) * w + x); }
    for (let y = 0; y < h; y++) { push(y * w); push(y * w + w - 1); }
    while (stack.length) {
      const p = stack.pop(), x = p % w, y = (p - x) / w;
      if (x > 0) push(p - 1); if (x < w - 1) push(p + 1); if (y > 0) push(p - w); if (y < h - 1) push(p + w);
    }
    const near = (p) => { const x = p % w, y = (p - x) / w; for (let dy = -2; dy <= 2; dy++) for (let dx = -2; dx <= 2; dx++) { const xx = x + dx, yy = y + dy; if (xx >= 0 && yy >= 0 && xx < w && yy < h && bgMask[yy * w + xx]) return true; } return false; };
    for (let p = 0; p < w * h; p++) {
      if (m[p] <= 45 || (!bgMask[p] && !near(p))) continue;
      const i = p * 4;
      if (bgMask[p] && m[p] > 110) data[i + 3] = 0;
      else { data[i + 3] = Math.round(255 * Math.max(0, Math.min(1, (110 - m[p]) / 65))); unmixMagenta(data, i, m[p]); }
    }
  }
  return { data, w: info.width, h: info.height };
}

// Blobs of non-transparent pixels, merged across gaps of up to `gap` pixels; returns bounding boxes.
function blobs({ data, w, h }, gap = 6, minArea = 3000) {
  const solid = new Uint8Array(w * h);
  for (let i = 0; i < w * h; i++) solid[i] = data[i * 4 + 3] > 128 ? 1 : 0;
  const grow = (src, horizontal) => {
    const out = new Uint8Array(w * h);
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      if (!src[y * w + x]) continue;
      for (let d = -gap; d <= gap; d++) {
        const xx = horizontal ? x + d : x, yy = horizontal ? y : y + d;
        if (xx >= 0 && xx < w && yy >= 0 && yy < h) out[yy * w + xx] = 1;
      }
    }
    return out;
  };
  const dil = grow(grow(solid, true), false);
  const label = new Int32Array(w * h);
  const boxes = [];
  const stack = [];
  for (let start = 0; start < w * h; start++) {
    if (!dil[start] || label[start]) continue;
    const id = boxes.length + 1;
    const box = { x0: w, y0: h, x1: 0, y1: 0, area: 0 };
    stack.push(start); label[start] = id;
    while (stack.length) {
      const p = stack.pop(), x = p % w, y = (p - x) / w;
      if (solid[p]) { box.area++; box.x0 = Math.min(box.x0, x); box.x1 = Math.max(box.x1, x); box.y0 = Math.min(box.y0, y); box.y1 = Math.max(box.y1, y); }
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const nx = x + dx, ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const q = ny * w + nx;
        if (dil[q] && !label[q]) { label[q] = id; stack.push(q); }
      }
    }
    boxes.push(box);
  }
  return boxes.filter((b) => b.area >= minArea);
}

// Largest connected blob within [cx0,cx1)x[cy0,cy1) (dilated by `gap` to bridge anti-aliased seams).
// Used per-cell by gridBoxes so a bleed fragment poking in from a neighbouring cell (smaller than the
// cell's own content) doesn't get picked up instead of - or merged into - the real content.
function largestBlobInRect({ data, w: imgW }, cx0, cy0, cx1, cy1, gap = 4) {
  const w = cx1 - cx0, h = cy1 - cy0;
  const solid = new Uint8Array(w * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) solid[y * w + x] = data[((cy0 + y) * imgW + (cx0 + x)) * 4 + 3] > 40 ? 1 : 0;
  const grow = (src, horizontal) => {
    const out = new Uint8Array(w * h);
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      if (!src[y * w + x]) continue;
      for (let d = -gap; d <= gap; d++) {
        const xx = horizontal ? x + d : x, yy = horizontal ? y : y + d;
        if (xx >= 0 && xx < w && yy >= 0 && yy < h) out[yy * w + xx] = 1;
      }
    }
    return out;
  };
  const dil = grow(grow(solid, true), false);
  const label = new Int32Array(w * h);
  let best = null;
  for (let start = 0; start < w * h; start++) {
    if (!dil[start] || label[start]) continue;
    const box = { x0: w, y0: h, x1: 0, y1: 0, area: 0 };
    const stack = [start]; label[start] = start + 1;
    while (stack.length) {
      const p = stack.pop(), x = p % w, y = (p - x) / w;
      if (solid[p]) { box.area++; box.x0 = Math.min(box.x0, x); box.x1 = Math.max(box.x1, x); box.y0 = Math.min(box.y0, y); box.y1 = Math.max(box.y1, y); }
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const nx = x + dx, ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const q = ny * w + nx;
        if (dil[q] && !label[q]) { label[q] = start + 1; stack.push(q); }
      }
    }
    if (!best || box.area > best.area) best = box;
  }
  return best && { x0: best.x0 + cx0, y0: best.y0 + cy0, x1: best.x1 + cx0, y1: best.y1 + cy0, area: best.area };
}

// One box per name, in name order, cut from a fixed cols x rows grid instead of whole-image blob
// detection - see geo_landmarks above for why. The last name in an incomplete row gets its cell
// widened through the row's remaining (unused) columns, since nothing else claims that space.
function gridBoxes(img, { cols, rows }, count) {
  const cellW = img.w / cols, cellH = img.h / rows;
  const boxes = [];
  for (let i = 0; i < count; i++) {
    const row = Math.floor(i / cols), col = i % cols;
    const lastInRow = col === cols - 1 || i === count - 1;
    const cx0 = Math.round(col * cellW), cy0 = Math.round(row * cellH);
    const cx1 = Math.round((lastInRow ? cols : col + 1) * cellW), cy1 = Math.round((row + 1) * cellH);
    boxes.push(largestBlobInRect(img, cx0, cy0, cx1, cy1));
  }
  return boxes;
}

// merge: true - keep tiny blobs (sun rays, paint droplets) and merge the closest blobs until there are as many as names, so a
// multi-part picture is one item without a hand-tuned dilation gap that would also glue neighbouring items together.
function mergeToCount(boxes, count) {
  let list = boxes.map((b) => ({ ...b }));
  const dist = (a, b) => Math.hypot(Math.max(0, Math.max(a.x0, b.x0) - Math.min(a.x1, b.x1)), Math.max(0, Math.max(a.y0, b.y0) - Math.min(a.y1, b.y1)));
  while (list.length > count) {
    let bi = 0, bj = 1, bd = Infinity;
    for (let i = 0; i < list.length; i++) for (let j = i + 1; j < list.length; j++) { const d = dist(list[i], list[j]); if (d < bd) { bd = d; bi = i; bj = j; } }
    const a = list[bi], b = list[bj];
    list = list.filter((_, k) => k !== bi && k !== bj);
    list.push({ x0: Math.min(a.x0, b.x0), y0: Math.min(a.y0, b.y0), x1: Math.max(a.x1, b.x1), y1: Math.max(a.y1, b.y1), area: a.area + b.area });
  }
  return list;
}

function readingOrder(boxes) {
  const rows = [];
  for (const b of [...boxes].sort((a, c) => (a.y0 + a.y1) - (c.y0 + c.y1))) {
    const cy = (b.y0 + b.y1) / 2;
    const row = rows.find((r) => Math.abs(r.cy - cy) < (b.y1 - b.y0) * 0.5);
    if (row) { row.items.push(b); row.cy = (row.cy * (row.items.length - 1) + cy) / row.items.length; } else rows.push({ cy, items: [b] });
  }
  return rows.sort((a, b) => a.cy - b.cy).flatMap((r) => r.items.sort((a, b) => a.x0 - b.x0));
}

(async () => {
  const kind = process.argv[2];
  const spec = SHEETS[kind];
  if (!spec) { console.error('usage: cut-sheets.js ' + Object.keys(SHEETS).join('|') + ' [sheet.png] [--install]'); process.exit(1); }
  const file = process.argv[3] && !process.argv[3].startsWith('--') ? process.argv[3] : path.join(ROOT, 'art/eva', spec.dir, spec.file);
  const install = process.argv.includes('--install');
  const img = await keyed(file, spec.bg);
  // cutY: sheet-height fractions where two rows of sprites touch (a flame tip into the next ship): clear the emptiest pixel
  // row near each fraction so the blob finder sees two items.
  for (const f of spec.cutY || []) {
    let bestY = 0, bestN = Infinity;
    for (let y = Math.round((f - 0.05) * img.h); y <= Math.round((f + 0.05) * img.h); y++) {
      let n = 0; for (let x = 0; x < img.w; x++) if (img.data[(y * img.w + x) * 4 + 3] > 128) n++;
      if (n < bestN) { bestN = n; bestY = y; }
    }
    for (let y = bestY - 1; y <= bestY + 1; y++) for (let x = 0; x < img.w; x++) img.data[(y * img.w + x) * 4 + 3] = 0;
  }
  // seeds: one [x,y] sheet-fraction point on the main part of each item, for sheets where items sit so close that their
  // bounding boxes overlap (a balloon string beside a fruit). Every connected piece is given to the item whose main piece
  // is nearest (sparkles, bubbles, motion lines follow their item) and pixels of other items inside the box are cleared.
  const seedBoxes = () => {
    const { w, h, data } = img;
    const label = new Int32Array(w * h), comps = [null];
    for (let st = 0; st < w * h; st++) {
      if (label[st] || data[st * 4 + 3] <= 8) continue;
      const id = comps.length, c = { x0: w, y0: h, x1: 0, y1: 0, area: 0 }, stack = [st];
      label[st] = id;
      while (stack.length) {
        const p = stack.pop(), x = p % w, y = (p - x) / w;
        c.area++; c.x0 = Math.min(c.x0, x); c.x1 = Math.max(c.x1, x); c.y0 = Math.min(c.y0, y); c.y1 = Math.max(c.y1, y);
        for (const q of [x > 0 ? p - 1 : -1, x < w - 1 ? p + 1 : -1, y > 0 ? p - w : -1, y < h - 1 ? p + w : -1]) {
          if (q >= 0 && !label[q] && data[q * 4 + 3] > 8) { label[q] = id; stack.push(q); }
        }
      }
      comps.push(c);
    }
    const boxDist = (a, b) => Math.hypot(Math.max(0, Math.max(a.x0, b.x0) - Math.min(a.x1, b.x1)), Math.max(0, Math.max(a.y0, b.y0) - Math.min(a.y1, b.y1)));
    const owner = new Int32Array(comps.length).fill(-1);
    spec.seeds.forEach((sd, i) => {
      const sx = Math.round(sd[0] * w), sy = Math.round(sd[1] * h);
      let id = 0;
      for (let r = 0; r < 40 && !id; r++) for (let dy = -r; dy <= r && !id; dy++) for (let dx = -r; dx <= r && !id; dx++) {
        const x = sx + dx, y = sy + dy;
        if (x >= 0 && y >= 0 && x < w && y < h && label[y * w + x] && data[(y * w + x) * 4 + 3] > 128) id = label[y * w + x];
      }
      if (!id) console.warn('seed', i, spec.names[i], 'is not on a picture');
      else owner[id] = i;
    });
    const mains = spec.seeds.map((_, i) => owner.indexOf(i));
    for (let id = 1; id < comps.length; id++) {
      if (owner[id] >= 0 || comps[id].area < 20) continue;
      let best = -1, bd = Infinity;
      mains.forEach((m, i) => { if (m < 1) return; const d = boxDist(comps[id], comps[m]); if (d < bd) { bd = d; best = i; } });
      owner[id] = best;
    }
    return spec.seeds.map((_, i) => {
      const box = { x0: w, y0: h, x1: 0, y1: 0, area: 0, owner: i, label, ownerOf: owner };
      for (let id = 1; id < comps.length; id++) if (owner[id] === i) { const c = comps[id]; box.x0 = Math.min(box.x0, c.x0); box.y0 = Math.min(box.y0, c.y0); box.x1 = Math.max(box.x1, c.x1); box.y1 = Math.max(box.y1, c.y1); box.area += c.area; }
      return box;
    });
  };
  const order = (boxes) => spec.singleRow ? [...boxes].sort((a, b) => a.x0 - b.x0) : readingOrder(boxes);
  const found = spec.seeds ? seedBoxes() : spec.grid ? gridBoxes(img, spec.grid, spec.names.length) : spec.merge ? order(mergeToCount(blobs(img, spec.blobGap || 6, 150), spec.names.length)) : order(blobs(img, spec.gap || 6));
  if (found.length !== spec.names.length) console.warn(`expected ${spec.names.length} items, found ${found.length}`);
  const outDir = path.join(ROOT, 'art/eva', spec.outDir);
  fs.mkdirSync(outDir, { recursive: true });
  const resDir = spec.resDir && path.join(ROOT, 'EvasLearningWorld/Assets/Eva/Resources/Art', spec.resDir);
  if (install && resDir) fs.mkdirSync(resDir, { recursive: true });
  // sameScale: names whose RELATIVE size matters (small/medium/large weights): they share one canvas side instead of each
  // being scaled up to fill its own sprite.
  const sharedSide = spec.sameScale ? Math.max(...spec.sameScale.map((n) => { const k = found[spec.names.indexOf(n)]; return k ? Math.max(k.x1 - k.x0 + 1, k.y1 - k.y0 + 1) : 0; })) : 0;
  for (let i = 0; i < Math.min(found.length, spec.names.length); i++) {
    const b = found[i];
    const w = b.x1 - b.x0 + 1, h = b.y1 - b.y0 + 1;
    const own = Math.round(Math.max(w, h) * 1.08);
    const side = spec.sameScale && spec.sameScale.includes(spec.names[i]) ? Math.round(sharedSide * 1.08) : own;
    const bottom = Math.round(side * BOTTOM_MARGIN); // content sits on the bottom edge so the feet are at a known height
    let src = img.data;
    if (b.owner !== undefined) { src = Buffer.from(img.data); for (let y = b.y0; y <= b.y1; y++) for (let x = b.x0; x <= b.x1; x++) if (b.ownerOf[b.label[y * img.w + x]] !== b.owner) src[(y * img.w + x) * 4 + 3] = 0; }
    const crop = await sharp(src, { raw: { width: img.w, height: img.h, channels: 4 } }).extract({ left: b.x0, top: b.y0, width: w, height: h }).png().toBuffer();
    if (spec.tight) {
      const pad = 3;
      const tight = await sharp(crop).extend({ top: pad, bottom: pad, left: pad, right: pad, background: { r: 0, g: 0, b: 0, alpha: 0 } })
        .resize(spec.scale ? { width: Math.round((w + 2 * pad) * spec.scale), height: Math.round((h + 2 * pad) * spec.scale) } : { width: spec.size, height: spec.size, fit: 'inside' }).png({ compressionLevel: 9 }).toBuffer();
      fs.writeFileSync(path.join(outDir, spec.names[i] + '.png'), tight);
      if (install && resDir && !spec.names[i].startsWith('_')) fs.writeFileSync(path.join(resDir, spec.names[i] + '.png'), tight);
      console.log(spec.names[i], `${w}x${h}`);
      continue;
    }
    const sprite = await sharp({ create: { width: side, height: side, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
      .composite([{ input: crop, left: Math.round((side - w) / 2), top: side - h - bottom }]).png().toBuffer();
    const small = await sharp(sprite).resize(spec.size, spec.size).png({ compressionLevel: 9 }).toBuffer();
    fs.writeFileSync(path.join(outDir, spec.names[i] + '.png'), small);
    if (install && resDir && !spec.names[i].startsWith('_')) fs.writeFileSync(path.join(resDir, spec.names[i] + '.png'), small);
    console.log(spec.names[i], `${w}x${h}`);
  }
  for (const [to, from] of Object.entries(spec.mirror || {})) {
    const flipped = await sharp(path.join(outDir, from + '.png')).flop().png({ compressionLevel: 9 }).toBuffer();
    fs.writeFileSync(path.join(outDir, to + '.png'), flipped);
    if (install && resDir) fs.writeFileSync(path.join(resDir, to + '.png'), flipped);
    console.log(to, '= mirror of', from);
  }
  // split: { <full sprite name>: <id> } writes half_<id> (left half of the centred picture) and piece_<id> (right half) next to it.
  for (const [full, id] of Object.entries(spec.split || {})) {
    const { data, info } = await sharp(path.join(outDir, full + '.png')).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const mid = info.width >> 1;
    const left = Buffer.from(data), right = Buffer.from(data);
    for (let y = 0; y < info.height; y++) for (let x = 0; x < info.width; x++) {
      const a = (y * info.width + x) * 4 + 3;
      if (x >= mid) left[a] = 0; else right[a] = 0;
    }
    for (const [name, buf] of [['half_' + id, left], ['piece_' + id, right]]) {
      const png = await sharp(buf, { raw: { width: info.width, height: info.height, channels: 4 } }).png({ compressionLevel: 9 }).toBuffer();
      fs.writeFileSync(path.join(outDir, name + '.png'), png);
      if (install && resDir) fs.writeFileSync(path.join(resDir, name + '.png'), png);
      console.log(name, '(split of', full + ')');
    }
  }
})();
