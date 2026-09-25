using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // What the pointer hand does for a guidance row: which fixed spot it pulses on, or which drag it repeats.
    // Serialised nowhere (not saved), so the enum order is free to change.
    public enum GuideTarget { None, HouseBuilding, SchoolBuilding, StoreBuilding, CountTile, StarterToLivingSeat, TrayToSlot, CheapestItem }

    // Eva's tutorial guidance: on every screen show, and after every tutorial state change, looks at
    // Progress.Tutorial and says the right line (once per entry to that step, not per screen) while pointing
    // the shared hand at the right thing. Never blocks input and never disables anything - the hand has no
    // TapTarget and raycastTarget off (see PointerHand), so every place stays reachable and the child can
    // freely ignore Eva.
    //
    // Plan is the pure decision half (Step 1 of the brief, tested directly in TutorialGuideTests before this
    // class existed at all). Refresh and the coroutines below are the screen-agnostic wiring on top: they
    // locate the live on-screen positions to point at by name, the same way NoReadingAuditTests and the
    // screens' own code already reach into each other's hierarchy (Transform.Find), rather than duplicating
    // each screen's private layout constants here.
    public sealed class TutorialGuide
    {
        private sealed class Runner : MonoBehaviour { }

        private const float MoveSeconds = 0.6f;
        private const float RestSeconds = 0.4f;
        private const float CycleSeconds = 6f; // PlaceStarter/House row: repeat the drag demo every 6 s

        private readonly EvaGame _game;
        private readonly Runner _runner;
        private readonly PointerHand _hand;

        // Dedup for Refresh's "say the line once per entry to the step" rule (Critical fix): keyed off the
        // current step alone, not the (step, screen) pair. A line already said during this step must never be
        // said again while the step hasn't changed, no matter how many screens the child bounces through in
        // between (e.g. PlaceStarter: House -> Map -> House without placing the sofa must not re-speak
        // house_welcome, even though the Map visit in between changes the screen and Voice.LastKey). The set is
        // cleared only when the step itself advances, which is exactly when a fresh line is allowed again.
        private TutorialStep? _stepForSaidKeys;
        private readonly HashSet<string> _saidKeysThisStep = new HashSet<string>();
        private Coroutine _pointRoutine;

        public TutorialGuide(EvaGame game, RectTransform root)
        {
            _game = game;
            _runner = root.gameObject.AddComponent<Runner>();
            _hand = new PointerHand(root, _runner);
        }

        // The pure decision function: given the current tutorial step and which screen is showing, what Eva
        // says (null if nothing, and only once per entry - Refresh below owns that bookkeeping) and where the
        // hand points. Every row of the brief's table, and the default (null, None) for everything else -
        // including FirstGame on every screen except the School list (which points at the Count tile), since
        // Tasks 7/8 already own the Count screen's own help ladder.
        public static (string voiceKey, GuideTarget target) Plan(TutorialStep step, ScreenId screen)
        {
            if (step == TutorialStep.PlaceStarter && screen == ScreenId.House) return ("house_welcome", GuideTarget.StarterToLivingSeat);
            if (step == TutorialStep.PlaceStarter && screen == ScreenId.Map) return ("map_house", GuideTarget.HouseBuilding);
            if (step == TutorialStep.GoToSchool && screen == ScreenId.Map) return ("map_school", GuideTarget.SchoolBuilding);
            if (step == TutorialStep.FirstGame && screen == ScreenId.School) return ("school_welcome", GuideTarget.CountTile);
            if (step == TutorialStep.GoToStore && screen == ScreenId.Map) return ("map_store", GuideTarget.StoreBuilding);
            if (step == TutorialStep.FirstPurchase && screen == ScreenId.Store) return ("store_welcome", GuideTarget.CheapestItem);
            if (step == TutorialStep.PlacePurchase && screen == ScreenId.Map) return ("map_house", GuideTarget.HouseBuilding);
            if (step == TutorialStep.PlacePurchase && screen == ScreenId.House) return ("house_new", GuideTarget.TrayToSlot);
            if (step == TutorialStep.Done && screen == ScreenId.House) return ("tut_done", GuideTarget.None);
            return (null, GuideTarget.None);
        }

        // Call on every screen show, and again after any Progress.Advance while staying on the same screen
        // (a placement or a purchase can move the tutorial step without a screen change).
        public void Refresh(ScreenId screen)
        {
            var step = _game.Progress.Tutorial;
            var plan = Plan(step, screen);

            // A genuine step advance clears what's been said so far - only then is a fresh line allowed.
            if (step != _stepForSaidKeys)
            {
                _saidKeysThisStep.Clear();
                _stepForSaidKeys = step;
            }

            // Once per entry to this line, for as long as the step hasn't changed - not on every Refresh call
            // (e.g. an unrelated Commit) and not on a screen bounce that doesn't advance the step (e.g.
            // PlaceStarter: House -> Map -> House without placing the sofa must not re-speak house_welcome on
            // the second House entry, even though the Map visit in between changed Voice.LastKey away from it).
            // A key already heard this step - whether spoken here or by the screen's own direct Voice.Say, per
            // the LastKey check below (guards the one row, FirstPurchase/Store, whose screen's own OnShow
            // speaks the same line itself; see StoreScreen.OnShow's store_welcome call) - is marked said either
            // way, so a later re-entry to that same screen this step doesn't re-speak it either.
            if (plan.voiceKey != null && !_saidKeysThisStep.Contains(plan.voiceKey))
            {
                if (_game.Voice.LastKey != plan.voiceKey) _game.Voice.Say(plan.voiceKey);
                _saidKeysThisStep.Add(plan.voiceKey);
            }

            StartPointing(plan.target);
        }

        private void StartPointing(GuideTarget target)
        {
            if (_pointRoutine != null)
            {
                _runner.StopCoroutine(_pointRoutine);
                _pointRoutine = null;
            }
            _hand.Hide();

            switch (target)
            {
                case GuideTarget.HouseBuilding: _pointRoutine = _runner.StartCoroutine(PulseAt(MapScreen.HouseButtonPosition)); break;
                case GuideTarget.SchoolBuilding: _pointRoutine = _runner.StartCoroutine(PulseAt(MapScreen.SchoolButtonPosition)); break;
                case GuideTarget.StoreBuilding: _pointRoutine = _runner.StartCoroutine(PulseAt(MapScreen.StoreButtonPosition)); break;
                case GuideTarget.CountTile: _pointRoutine = _runner.StartCoroutine(PulseAt(BuildingScreen.TilePosition(BuildingId.School, 0))); break;
                case GuideTarget.CheapestItem: _pointRoutine = _runner.StartCoroutine(PulseAtCheapestItem()); break;
                case GuideTarget.StarterToLivingSeat: _pointRoutine = _runner.StartCoroutine(DragLoop("sofa", "living_seat")); break;
                case GuideTarget.TrayToSlot: _pointRoutine = _runner.StartCoroutine(DragLoop(null, null)); break;
            }
        }

        private IEnumerator PulseAt(Vector2 position)
        {
            yield return _hand.MoveTo(position, MoveSeconds);
            _hand.Pulse(true);
        }

        // FirstPurchase/Store row: pulses on the cheapest item the child does not already own (rug, per
        // FurnitureCatalog - "affordable" in the brief's own words, since a full Count session already pays
        // more than its price).
        private IEnumerator PulseAtCheapestItem()
        {
            var itemId = CheapestUnownedItemId();
            if (itemId == null) yield break;
            var shelfItem = _game.ScreenRoot.Find("StoreScreen/Shelf/Item_" + itemId);
            if (shelfItem == null) yield break;
            yield return _hand.MoveTo(((RectTransform)shelfItem).anchoredPosition, MoveSeconds);
            _hand.Pulse(true);
        }

        private string CheapestUnownedItemId()
        {
            string best = null;
            var bestPrice = int.MaxValue;
            foreach (var item in FurnitureCatalog.All)
            {
                if (item.Id == FurnitureCatalog.StarterId || _game.Progress.Owned.Contains(item.Id)) continue;
                if (item.Price < bestPrice)
                {
                    bestPrice = item.Price;
                    best = item.Id;
                }
            }
            return best;
        }

        // The PlaceStarter/House and PlacePurchase/House rows: the hand glides tray item -> slot -> rest, then
        // hides and waits out the remainder of CycleSeconds before repeating, for as long as this coroutine
        // stays the active one (Refresh replaces it the moment the child actually drags the item, since
        // HouseScreen calls Refresh again right after Progress.Advance(ItemPlaced)).
        // `fixedItemId`/`fixedSlotId` pin the starter's own well-known drag (sofa -> living_seat, per the
        // brief); null/null resolves to whichever owned item is not yet placed and a slot it can go in, for
        // the PlacePurchase row, where the item varies with what was bought.
        private IEnumerator DragLoop(string fixedItemId, string fixedSlotId)
        {
            while (true)
            {
                var move = ResolveDrag(fixedItemId, fixedSlotId);
                if (move == null) yield break;
                yield return _hand.MoveTo(move.Value.from, MoveSeconds);
                yield return new WaitForSeconds(RestSeconds);
                yield return _hand.MoveTo(move.Value.to, MoveSeconds);
                yield return new WaitForSeconds(RestSeconds);
                _hand.Hide();
                var elapsed = 2f * MoveSeconds + 2f * RestSeconds;
                yield return new WaitForSeconds(Mathf.Max(0f, CycleSeconds - elapsed));
            }
        }

        private (Vector2 from, Vector2 to)? ResolveDrag(string fixedItemId, string fixedSlotId)
        {
            var owned = _game.Progress.Owned;
            var house = _game.Progress.House;

            var itemId = fixedItemId;
            if (itemId == null)
                foreach (var id in owned)
                    if (house.SlotOf(id) == null) { itemId = id; break; }
            if (itemId == null) return null;

            var houseRoot = _game.ScreenRoot.Find("HouseScreen");
            if (houseRoot == null) return null;
            var dragTransform = houseRoot.Find("Items/Drag_" + itemId);
            if (dragTransform == null) return null;

            var slotId = fixedSlotId;
            if (slotId == null)
                foreach (var slot in HouseSlots.All)
                    if (house.CanPlace(itemId, slot.Id, owned) && houseRoot.Find("Slots/Slot_" + slot.Id) != null) { slotId = slot.Id; break; }
            if (slotId == null) return null;
            var slotTransform = houseRoot.Find("Slots/Slot_" + slotId);
            if (slotTransform == null) return null;

            return (((RectTransform)dragTransform).anchoredPosition, ((RectTransform)slotTransform).anchoredPosition);
        }
    }
}
