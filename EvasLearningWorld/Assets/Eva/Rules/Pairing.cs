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
        public const int Slots = 4;

        public sealed class Entry
        {
            public string Id;
            public string Key;
        }

        private readonly Random _rng;
        private readonly Func<string, string, bool> _confusable;
        private readonly List<Entry> _queue;

        // The things on screen, by slot; null = nothing in that slot.
        public Entry[] Items { get; } = new Entry[Slots];
        public string[] Targets { get; } = new string[Slots];

        public int Total { get; }

        // `confusable(itemId, targetKey)` is true for a pairing that is wrong but believable (a duck on a farm); such a
        // pair is kept off the screen whenever the items left allow it.
        public PairingGame(IEnumerable<(string Id, string Key)> pairs, Random rng, Func<string, string, bool> confusable = null)
        {
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
            for (var i = 0; i < Slots; i++)
                for (var t = 0; t < Slots; t++)
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
            for (var t = 0; t < Slots; t++)
                if (Targets[t] != null && !KeyHasItems(Targets[t])) Targets[t] = null;
            FillItems();
            FillTargets();
            FixConfusable();
            EnsureMatch();
        }

        // A free slot takes a queued item; about half the time one whose partner is already on screen.
        private void FillItems()
        {
            for (var i = 0; i < Slots && _queue.Count > 0; i++)
            {
                if (Items[i] != null) continue;
                var matching = _queue.Where(e => TargetShown(e.Key)).ToList();
                var pick = matching.Count > 0 && _rng.NextDouble() < 0.5 ? matching[_rng.Next(matching.Count)] : _queue[_rng.Next(_queue.Count)];
                _queue.Remove(pick);
                Items[i] = pick;
            }
        }

        // A free slot takes the key of an item still to be placed that has no partner on screen (always, once the last four
        // items are out, so the end of the game shows everything); otherwise any key that still has items.
        private void FillTargets()
        {
            for (var t = 0; t < Slots; t++)
            {
                if (Targets[t] != null) continue;
                var candidates = Items.Where(i => i != null).Select(i => i.Key).Concat(_queue.Select(e => e.Key))
                    .Distinct().Where(k => !TargetShown(k)).ToList();
                if (candidates.Count == 0) return;
                var wanted = Items.Where(i => i != null).Select(i => i.Key).Distinct().Where(k => !TargetShown(k)).ToList();
                var endgame = Remaining <= Slots;
                var pool = wanted.Count > 0 && (endgame || _rng.NextDouble() < 0.6) ? wanted : candidates;
                Targets[t] = pool[_rng.Next(pool.Count)];
            }
        }

        // Swaps an item that sits next to a wrong-but-believable partner for a queued one that does not (when there is one).
        private void FixConfusable()
        {
            for (var i = 0; i < Slots; i++)
            {
                if (Items[i] == null || !ConfusesAny(Items[i])) continue;
                var swap = _queue.FirstOrDefault(e => !ConfusesAny(e));
                if (swap == null) continue;
                _queue.Remove(swap);
                _queue.Add(Items[i]);
                Items[i] = swap;
            }
        }

        private bool ConfusesAny(Entry item) => Targets.Any(k => k != null && k != item.Key && _confusable(item.Id, k));

        // Never leave the child with nothing to do: if no item on screen matches a target on screen, a target takes the
        // key of one item (only targets that no item matches can be on screen when this happens).
        private void EnsureMatch()
        {
            if (Finished) return;
            for (var i = 0; i < Slots; i++)
                for (var t = 0; t < Slots; t++)
                    if (Matches(i, t)) return;
            var item = Items.First(e => e != null);
            var free = Array.IndexOf(Targets, null);
            Targets[free >= 0 ? free : _rng.Next(Slots)] = item.Key;
        }
    }

    // The pair lists of the two Zoo & Farm games on PairingGame.
    public static class ZooPairs
    {
        // Mother and Baby: each baby belongs to the mother of its own kind (the key is the animal's id).
        public static IReadOnlyList<(string Id, string Key)> MotherAndBaby() => ZooFarmAnimals.All.Select(a => (a.Id, a.Id)).ToArray();

        // Habitat: each animal belongs to its own habitat.
        public static IReadOnlyList<(string Id, string Key)> AnimalAndHabitat() => ZooFarmAnimals.All.Select(a => (a.Id, a.Habitat)).ToArray();
    }
}
