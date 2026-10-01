// Checks every place's proposed Road polyline against every OTHER place's TapBox for intersection,
// so road redesigns after a placement change can be verified by computation instead of eyeballing.
// Usage: edit PLACES below to match the candidate Places.cs content, then `node verify-roads.js`.

const PLACES = {
  House:      { box: [60, 20, 320, 280] },
  School:     { box: [-470, 40, 280, 240], road: [[60,-190],[-40,-155],[-200,-150],[-350,-135],[-470,-120]] },
  Store:      { box: [510, -225, 280, 240], road: [[60,-190],[190,-260],[320,-345],[440,-405],[555,-420]] },
  Playground: { box: [300, 430, 280, 240], road: [[60,-190],[250,-190],[250,130],[345,215]] },
  ZooFarm:    { box: [-950, 430, 280, 240], road: [[60,-190],[-40,-150],[-950,-150],[-950,300]] },
  ScienceLab: { box: [950, 380, 280, 240], road: [[60,-190],[250,-190],[250,150],[700,150],[950,150],[950,220]] },
  Workshop:   { box: [-1150, 50, 280, 240], road: [[60,-190],[-40,-160],[-1150,-130],[-1150,-110]] },
  ArtStudio:  { box: [1150, -350, 280, 240], road: [[60,-190],[150,-260],[150,-400],[1000,-400],[1000,-190]] },
  BrainGym:   { box: [-300, 430, 280, 240], road: [[60,-190],[-200,-190],[-200,200],[-300,200],[-300,300]] },
  FriendsPark:{ box: [1300, 0, 280, 240], road: [[60,-190],[250,-190],[250,0],[1120,0]] },
  Arcade:     { box: [-550, -400, 280, 240], road: [[60,-190],[60,-250],[-550,-250],[-550,-240]] },
};

function boxOf(id) {
  const [x, y, w, h] = PLACES[id].box;
  return { xmin: x - w / 2, xmax: x + w / 2, ymin: y - h / 2, ymax: y + h / 2, id };
}

// Segment vs axis-aligned rect intersection: true if the segment enters the rect's interior at all
// (endpoint inside, or crossing any of the 4 edges).
function segRectIntersect(p0, p1, r) {
  const inside = (p) => p[0] > r.xmin && p[0] < r.xmax && p[1] > r.ymin && p[1] < r.ymax;
  if (inside(p0) || inside(p1)) return true;
  const edges = [
    [[r.xmin, r.ymin], [r.xmax, r.ymin]],
    [[r.xmax, r.ymin], [r.xmax, r.ymax]],
    [[r.xmax, r.ymax], [r.xmin, r.ymax]],
    [[r.xmin, r.ymax], [r.xmin, r.ymin]],
  ];
  const cross = (o, a, b) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
  const segCross = (a, b, c, d) => {
    const d1 = cross(a, b, c), d2 = cross(a, b, d), d3 = cross(c, d, a), d4 = cross(c, d, b);
    return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
  };
  for (const [e0, e1] of edges) if (segCross(p0, p1, e0, e1)) return true;
  return false;
}

let anyFail = false;

// Screen-fixed HUD zones (Places.SettingsZone / Places.CoinZone), expressed in the first view.
const SETTINGS_ZONE = { xmin: -570 - 120, xmax: -570 + 120, ymin: 365 - 120, ymax: 365 + 120 };
const COIN_ZONE = { xmin: 570 - 120, xmax: 570 + 120, ymin: 385 - 45, ymax: 385 + 45 };
for (const [id, place] of Object.entries(PLACES)) {
  const r = boxOf(id);
  for (const [zoneName, z] of [["SettingsZone", SETTINGS_ZONE], ["CoinZone", COIN_ZONE]]) {
    if (r.xmin < z.xmax && r.xmax > z.xmin && r.ymin < z.ymax && r.ymax > z.ymin) {
      console.log(`HUD-ZONE FAIL: ${id}'s TapBox overlaps ${zoneName}`);
      anyFail = true;
    }
  }
}

// Each place's own last road point must sit strictly outside its own TapBox (WorldBox.Contains is inclusive
// of the edge, so landing exactly on it still counts as "inside" and fails RoadsNeverEnterAnyTapBox).
for (const [id, place] of Object.entries(PLACES)) {
  if (!place.road) continue;
  const [sx, sy] = place.road[place.road.length - 1];
  const r = boxOf(id);
  if (sx >= r.xmin && sx <= r.xmax && sy >= r.ymin && sy <= r.ymax) {
    console.log(`SELF-CONTAINMENT FAIL: ${id}'s own last road point sits on/inside its own TapBox`);
    anyFail = true;
  }
}

// First-view containment for House/School/Store (PlacesTests.TheInitialThreeBuildingsFitInsideTheFirstViewWithMargin).
const VIEW = { xmin: -720 + 60, xmax: 720 - 60, ymin: -450 + 60, ymax: 450 - 60 };
for (const id of ["House", "School", "Store"]) {
  const r = boxOf(id);
  if (r.xmin < VIEW.xmin || r.xmax > VIEW.xmax || r.ymin < VIEW.ymin || r.ymax > VIEW.ymax) {
    console.log(`VIEW-MARGIN FAIL: ${id} TapBox [${r.xmin},${r.xmax}]x[${r.ymin},${r.ymax}] outside first-view margin`);
    anyFail = true;
  }
}

// Every pair of places needs >=100 unit gap on at least one axis (PlacesTests.BuildingsAreTappableSizedAndNeverOverlap).
const ids = Object.keys(PLACES);
for (let i = 0; i < ids.length; i++) {
  for (let j = i + 1; j < ids.length; j++) {
    const a = boxOf(ids[i]), b = boxOf(ids[j]);
    const gapX = Math.max(b.xmin - a.xmax, a.xmin - b.xmax);
    const gapY = Math.max(b.ymin - a.ymax, a.ymin - b.ymax);
    const gap = Math.max(gapX, gapY);
    if (gap < 100) {
      console.log(`GAP FAIL: ${ids[i]} and ${ids[j]} only ${gap} units apart (need >=100)`);
      anyFail = true;
    }
  }
}

// Each place's 240x240 StandingArea (centred on its road's last point) must not cover any OTHER
// place's TapBox (PlacesTests.CharacterAreasAreTappableSizedInsideTheWorldAndOnlyCoverTheirOwnBuildingsTapBox).
for (const [id, place] of Object.entries(PLACES)) {
  if (!place.road) continue;
  const [sx, sy] = place.road[place.road.length - 1];
  const area = { xmin: sx - 120, xmax: sx + 120, ymin: sy - 120, ymax: sy + 120 };
  for (const otherId of Object.keys(PLACES)) {
    if (otherId === id) continue;
    const r = boxOf(otherId);
    if (area.xmin < r.xmax && area.xmax > r.xmin && area.ymin < r.ymax && area.ymax > r.ymin) {
      console.log(`STANDING-AREA FAIL: ${id}'s StandingArea covers ${otherId}'s TapBox`);
      anyFail = true;
    }
  }
}

for (const [id, place] of Object.entries(PLACES)) {
  if (!place.road) continue;
  for (const [otherId, other] of Object.entries(PLACES)) {
    if (otherId === id) continue;
    const rect = boxOf(otherId);
    for (let i = 1; i < place.road.length; i++) {
      const p0 = place.road[i - 1], p1 = place.road[i];
      if (segRectIntersect(p0, p1, rect)) {
        console.log(`COLLISION: ${id}'s road segment ${i} [${p0}]->[${p1}] crosses ${otherId}'s TapBox`);
        anyFail = true;
      }
    }
  }
}
if (!anyFail) console.log("All roads clear of every other place's TapBox.");
