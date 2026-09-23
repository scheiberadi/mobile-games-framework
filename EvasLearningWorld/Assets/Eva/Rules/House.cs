using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class HouseSlot
    {
        public string Id;
        public string Room;
        public SlotKind Kind;

        public HouseSlot(string id, string room, SlotKind kind)
        {
            Id = id;
            Room = room;
            Kind = kind;
        }
    }

    public static class HouseSlots
    {
        public static readonly IReadOnlyList<HouseSlot> All = new[]
        {
            new HouseSlot("living_seat", "living", SlotKind.Seat),
            new HouseSlot("living_floor", "living", SlotKind.Floor),
            new HouseSlot("living_table", "living", SlotKind.Table),
            new HouseSlot("living_corner", "living", SlotKind.Corner),
            new HouseSlot("bedroom_bed", "bedroom", SlotKind.Bed),
            new HouseSlot("bedroom_corner", "bedroom", SlotKind.Corner),
            new HouseSlot("bedroom_wall", "bedroom", SlotKind.Wall),
        };

        public static HouseSlot Find(string id)
        {
            foreach (var slot in All)
                if (slot.Id == id) return slot;
            return null;
        }
    }

    [Serializable]
    public sealed class Placement
    {
        public string ItemId;
        public string SlotId;
    }

    // Which owned item sits in which slot. Placing an item that is already placed moves it.
    [Serializable]
    public sealed class HouseLayout
    {
        public List<Placement> Placements = new List<Placement>();

        public string SlotOf(string itemId) => PlacementOf(itemId)?.SlotId;

        public string ItemIn(string slotId)
        {
            foreach (var placement in Placements)
                if (placement.SlotId == slotId) return placement.ItemId;
            return null;
        }

        public bool CanPlace(string itemId, string slotId, IEnumerable<string> owned)
        {
            var item = FurnitureCatalog.Find(itemId);
            var slot = HouseSlots.Find(slotId);
            if (item == null || slot == null || item.Kind != slot.Kind) return false;

            var isOwned = false;
            foreach (var id in owned)
                if (id == itemId) { isOwned = true; break; }
            if (!isOwned) return false;

            var occupant = ItemIn(slotId);
            return occupant == null || occupant == itemId;
        }

        public bool TryPlace(string itemId, string slotId, IEnumerable<string> owned)
        {
            if (!CanPlace(itemId, slotId, owned)) return false;
            var placement = PlacementOf(itemId);
            if (placement == null) Placements.Add(new Placement { ItemId = itemId, SlotId = slotId });
            else placement.SlotId = slotId;
            return true;
        }

        public bool Remove(string itemId)
        {
            var placement = PlacementOf(itemId);
            return placement != null && Placements.Remove(placement);
        }

        private Placement PlacementOf(string itemId)
        {
            foreach (var placement in Placements)
                if (placement.ItemId == itemId) return placement;
            return null;
        }
    }
}
