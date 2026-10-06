using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // The state of a "take each thing to its partner" game that runs until every pair is made (Zoo & Farm's Mother
    // and Habitat): a few things (Items, the draggable ones: babies, animals) and a few partners (Targets: mothers,
    // habitats) are on screen at once, in four slots each. When the child makes a match the item is used up and a new
    // one takes its slot; a target is used up only when no item that belongs to it is left (a mother has one baby, a
    // habitat has several animals), and then a new target takes its slot. The game is over when every item is used up.
    //
    // An item belongs to the target with the same Key. Rules the screen can rely on after every change:
    //  - at least one item on screen matches a target on screen (the child is never stuck), so some of the items on screen
    //    can be decoys whose partner is not on screen yet;
    //  - no target key is shown twice and no item is shown twice;
    //  - once four or fewer items are left, the partner of every one of them is on screen.
    public sealed class PairingGame
    {
        // The most things on screen at once (Habitat and Mother); a game may show fewer (the `slots` of the constructor).
        public const int MaxSlots = 4;
        public int SlotCount { get; }

        public sealed class Entry
        {
            public string Id;
            public string Key;
        }

        private readonly Random _rng;
        private readonly Func<string, string, bool> _confusable;
        private readonly List<Entry> _queue;

        // The things on screen, by slot; null = nothing in that slot.
        public Entry[] Items { get; }
        public string[] Targets { get; }

        public int Total { get; }

        // `confusable(itemId, targetKey)` is true for a pairing that is wrong but believable (a duck on a farm); such a
        // pair is kept off the screen whenever the items left allow it.
        public PairingGame(IEnumerable<(string Id, string Key)> pairs, Random rng, Func<string, string, bool> confusable = null, int slots = MaxSlots)
        {
            SlotCount = slots;
            Items = new Entry[slots];
            Targets = new string[slots];
            _rng = rng;
            _confusable = confusable ?? ((_, __) => false);
            _queue = pairs.Select(p => new Entry { Id = p.Id, Key = p.Key }).OrderBy(_ => rng.Next()).ToList();
            Total = _queue.Count;
            Refill();
        }

        public int Remaining => _queue.Count + Items.Count(i => i != null);
        public bool Finished => Remaining == 0;
        public int Done => Total - Remaining;

        public bool Matches(int itemSlot, int targetSlot) =>
            Items[itemSlot] != null && Targets[targetSlot] != null && Items[itemSlot].Key == Targets[targetSlot];

        // Some matching pair on screen (a hint or demonstration helps with it), or false when the game is over.
        public bool TryFindMatch(out int itemSlot, out int targetSlot)
        {
            var pairs = new List<(int, int)>();
            for (var i = 0; i < SlotCount; i++)
                for (var t = 0; t < SlotCount; t++)
                    if (Matches(i, t)) pairs.Add((i, t));
            if (pairs.Count == 0) { itemSlot = targetSlot = -1; return false; }
            (itemSlot, targetSlot) = pairs[_rng.Next(pairs.Count)];
            return true;
        }

        // The item in the slot is matched: it is used up and the screen is refilled (see the class comment).
        public void Resolve(int itemSlot)
        {
            if (Items[itemSlot] == null) throw new InvalidOperationException("no item in slot " + itemSlot);
            Items[itemSlot] = null;
            Refill();
        }

        private bool KeyHasItems(string key) => Items.Any(i => i != null && i.Key == key) || _queue.Any(e => e.Key == key);

        private bool TargetShown(string key) => Targets.Contains(key);

        private void Refill()
        {
            for (var t = 0; t < SlotCount; t++)
                if (Targets[t] != null && !KeyHasItems(Targets[t])) Targets[t] = null;
            var fresh = FillItems();
            FillTargets();
            FixConfusable(fresh);
            EnsureMatch();
        }

        // A free slot takes a queued item; about half the time one whose partner is already on screen. Returns the slots filled.
        private List<int> FillItems()
        {
            var fresh = new List<int>();
            for (var i = 0; i < SlotCount && _queue.Count > 0; i++)
            {
                if (Items[i] != null) continue;
                var matching = _queue.Where(e => TargetShown(e.Key)).ToList();
                // Always one that matches when nothing on screen does, so no target on screen has to be swapped (see EnsureMatch).
                var pick = matching.Count > 0 && (!AnyMatch() || _rng.NextDouble() < 0.5) ? matching[_rng.Next(matching.Count)] : _queue[_rng.Next(_queue.Count)];
                _queue.Remove(pick);
                Items[i] = pick;
                fresh.Add(i);
            }
            return fresh;
        }

        // A free slot takes the key of an item still to be placed that has no partner on screen (always, once the last four
        // items are out, so the end of the game shows everything); otherwise any key that still has items.
        private void FillTargets()
        {
            for (var t = 0; t < SlotCount; t++)
            {
                if (Targets[t] != null) continue;
                var candidates = Items.Where(i => i != null).Select(i => i.Key).Concat(_queue.Select(e => e.Key))
                    .Distinct().Where(k => !TargetShown(k)).ToList();
                if (candidates.Count == 0) return;
                var wanted = Items.Where(i => i != null).Select(i => i.Key).Distinct().Where(k => !TargetShown(k)).ToList();
                var endgame = Remaining <= SlotCount;
                // The last free slot must leave the child something to do, or EnsureMatch would have to swap a target
                // they are already looking at.
                var lastFree = Targets.Count(k => k == null) == 1;
                var needMatch = lastFree && !AnyMatch();
                var pool = wanted.Count > 0 && (endgame || needMatch || _rng.NextDouble() < 0.6) ? wanted : candidates;
                // Prefer a partner no item on screen could be mistaken for: an animal the child already sees never changes.
                var calm = pool.Where(k => !Items.Any(i => i != null && i.Key != k && _confusable(i.Id, k))).ToList();
                if (calm.Count > 0) pool = calm;
                Targets[t] = pool[_rng.Next(pool.Count)];
            }
        }

        // Swaps an item that was just put on screen (never one the child has been looking at) and sits next to a
        // wrong-but-believable partner for a queued one that does not (when there is one).
        private void FixConfusable(List<int> fresh)
        {
            foreach (var i in fresh)
            {
                if (Items[i] == null || !ConfusesAny(Items[i])) continue;
                var original = Items[i];
                // A swap must not take the last matching pair off the screen (EnsureMatch would then swap a target).
                var swap = _queue.FirstOrDefault(e => !ConfusesAny(e) && SwapKeepsAMatch(i, e));
                if (swap == null) continue;
                _queue.Remove(swap);
                _queue.Add(original);
                Items[i] = swap;
            }
        }

        private bool ConfusesAny(Entry item) => Targets.Any(k => k != null && k != item.Key && _confusable(item.Id, k));

        private bool SwapKeepsAMatch(int slot, Entry candidate)
        {
            var kept = Items[slot];
            Items[slot] = candidate;
            var ok = AnyMatch();
            Items[slot] = kept;
            return ok;
        }

        private bool AnyMatch()
        {
            for (var i = 0; i < SlotCount; i++)
                for (var t = 0; t < SlotCount; t++)
                    if (Matches(i, t)) return true;
            return false;
        }

        // Never leave the child with nothing to do: if no item on screen matches a target on screen, a target takes the
        // key of one item (only targets that no item matches can be on screen when this happens).
        private void EnsureMatch()
        {
            if (Finished) return;
            for (var i = 0; i < SlotCount; i++)
                for (var t = 0; t < SlotCount; t++)
                    if (Matches(i, t)) return;
            var item = Items.First(e => e != null);
            var free = Array.IndexOf(Targets, null);
            Targets[free >= 0 ? free : _rng.Next(SlotCount)] = item.Key;
        }
    }

    public static class PairingSession
    {
        // `count` of the pairs for one game (all of them when count is 0 or more than there are): taken round-robin over
        // the keys, so every target gets a share (the habitats get about the same number of animals) and the choice
        // within a key is random.
        public static IReadOnlyList<(string Id, string Key)> Pick(IReadOnlyList<(string Id, string Key)> pairs, int count, Random rng)
        {
            if (count <= 0 || count >= pairs.Count) return pairs;
            var groups = pairs.GroupBy(p => p.Key).OrderBy(_ => rng.Next())
                .Select(g => new Queue<(string Id, string Key)>(g.OrderBy(_ => rng.Next()))).ToList();
            var picked = new List<(string Id, string Key)>();
            while (picked.Count < count)
                foreach (var group in groups)
                {
                    if (picked.Count == count) break;
                    if (group.Count > 0) picked.Add(group.Dequeue());
                }
            return picked;
        }
    }

    // The pair lists of the two Zoo & Farm games on PairingGame.
    public static class ZooPairs
    {
        // Every animal would be more than a four-year-old wants to play in one go.
        public const int PairsPerGame = 12;

        // Mother and Baby: each baby belongs to the mother of its own kind (the key is the animal's id).
        public static IReadOnlyList<(string Id, string Key)> MotherAndBaby() => ZooFarmAnimals.All.Select(a => (a.Id, a.Id)).ToArray();

        // Footprint: each footprint belongs to the animal that left it (only animals with a print of their own, see Animal.Footprint).
        public static IReadOnlyList<(string Id, string Key)> FootprintAndAnimal() =>
            ZooFarmAnimals.All.Where(a => a.Footprint != null).Select(a => (a.Footprint, a.Id)).ToArray();

        // Habitat: each animal belongs to its own habitat.
        public static IReadOnlyList<(string Id, string Key)> AnimalAndHabitat() => ZooFarmAnimals.All.Select(a => (a.Id, a.Habitat)).ToArray();
    }
}
