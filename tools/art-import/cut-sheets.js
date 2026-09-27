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
};

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
      else if (m > 45) { data[i + 3] = Math.round(255 * (110 - m) / 65); data[i] = Math.min(r, g + 40); data[i + 2] = Math.min(b, g + 40); }
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
  const found = spec.grid ? gridBoxes(img, spec.grid, spec.names.length) : readingOrder(blobs(img));
  if (found.length !== spec.names.length) console.warn(`expected ${spec.names.length} items, found ${found.length}`);
  const outDir = path.join(ROOT, 'art/eva', spec.outDir);
  fs.mkdirSync(outDir, { recursive: true });
  const resDir = spec.resDir && path.join(ROOT, 'EvasLearningWorld/Assets/Eva/Resources/Art', spec.resDir);
  if (install && resDir) fs.mkdirSync(resDir, { recursive: true });
  for (let i = 0; i < Math.min(found.length, spec.names.length); i++) {
    const b = found[i];
    const w = b.x1 - b.x0 + 1, h = b.y1 - b.y0 + 1;
    const side = Math.round(Math.max(w, h) * 1.08);
    const bottom = Math.round(side * BOTTOM_MARGIN); // content sits on the bottom edge so the feet are at a known height
    const crop = await sharp(img.data, { raw: { width: img.w, height: img.h, channels: 4 } }).extract({ left: b.x0, top: b.y0, width: w, height: h }).png().toBuffer();
    const sprite = await sharp({ create: { width: side, height: side, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
      .composite([{ input: crop, left: Math.round((side - w) / 2), top: side - h - bottom }]).png().toBuffer();
    const small = await sharp(sprite).resize(spec.size, spec.size).png({ compressionLevel: 9 }).toBuffer();
    fs.writeFileSync(path.join(outDir, spec.names[i] + '.png'), small);
    if (install && resDir) fs.writeFileSync(path.join(resDir, spec.names[i] + '.png'), small);
    console.log(spec.names[i], `${w}x${h}`);
  }
})();
