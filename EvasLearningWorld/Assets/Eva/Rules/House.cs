using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class HouseSlot
    {
        public string Id;
        public string Room;
        public SlotKind Kind;
        public float X, Y; // room-local canvas units (room centre = 0,0; y up); see HouseScreen

        public HouseSlot(string id, string room, SlotKind kind, float x, float y)
        {
            Id = id;
            Room = room;
            Kind = kind;
            X = x;
            Y = y;
        }
    }

    public static class HouseSlots
    {
        // 31 slots, 3-4 per room (hallways have none): the initial placement capacity, not a permanent maximum.
        // Positions are room-local canvas units on the room art (1440x960 cell, room view shows the middle 900),
        // chosen against the art: floor items between the back wall's base (y about -30) and the tray (y -210),
        // wall and corner items just in front of the back wall, corners on the side away from the doors, wall
        // slots over the bare wall zone left free of decoration. They keep clear of the top navigation band
        // (y + 130 <= 210) and inside the +-720 frame. Ids are room_kind; living_* keep their old ids, the old
        // bedroom_* slots are now kids_* (SaveStore remaps them).
        public static readonly IReadOnlyList<HouseSlot> All = new[]
        {
            new HouseSlot("living_seat", "living", SlotKind.Seat, 0f, -95f),
            new HouseSlot("living_floor", "living", SlotKind.Floor, 0f, -130f),
            new HouseSlot("living_table", "living", SlotKind.Table, 0f, -30f),
            new HouseSlot("living_corner", "living", SlotKind.Corner, -560f, 45f),

            new HouseSlot("dining_seat", "dining", SlotKind.Seat, 0f, -95f),
            new HouseSlot("dining_table", "dining", SlotKind.Table, 0f, -30f),
            new HouseSlot("dining_corner", "dining", SlotKind.Corner, 500f, 45f),
            new HouseSlot("dining_wall", "dining", SlotKind.Wall, -290f, 70f),

            new HouseSlot("kitchen_table", "kitchen", SlotKind.Table, 0f, -30f),
            new HouseSlot("kitchen_floor", "kitchen", SlotKind.Floor, 0f, -130f),
            new HouseSlot("kitchen_wall", "kitchen", SlotKind.Wall, 400f, 70f),
            new HouseSlot("kitchen_corner", "kitchen", SlotKind.Corner, 590f, 45f),

            new HouseSlot("parents_bed", "parents", SlotKind.Bed, 14f, 40f),
            new HouseSlot("parents_floor", "parents", SlotKind.Floor, 0f, -130f),
            new HouseSlot("parents_wall", "parents", SlotKind.Wall, -317f, 70f),
            new HouseSlot("parents_corner", "parents", SlotKind.Corner, -560f, 45f),

            new HouseSlot("kids_bed", "kids", SlotKind.Bed, 36f, 40f),
            new HouseSlot("kids_floor", "kids", SlotKind.Floor, 0f, -130f),
            new HouseSlot("kids_wall", "kids", SlotKind.Wall, -317f, 70f),
            new HouseSlot("kids_corner", "kids", SlotKind.Corner, 480f, 45f),

            new HouseSlot("bath_floor", "bath", SlotKind.Floor, 0f, -130f),
            new HouseSlot("bath_wall", "bath", SlotKind.Wall, -410f, 70f),
            new HouseSlot("bath_corner", "bath", SlotKind.Corner, 560f, 45f),

            new HouseSlot("party_seat", "party", SlotKind.Seat, 0f, -95f),
            new HouseSlot("party_floor", "party", SlotKind.Floor, 0f, -130f),
            new HouseSlot("party_table", "party", SlotKind.Table, 0f, -30f),
            new HouseSlot("party_corner", "party", SlotKind.Corner, -560f, 45f),

            new HouseSlot("play_floor", "play", SlotKind.Floor, 0f, -130f),
            new HouseSlot("play_table", "play", SlotKind.Table, 0f, -30f),
            new HouseSlot("play_wall", "play", SlotKind.Wall, -200f, 70f),
            new HouseSlot("play_corner", "play", SlotKind.Corner, 560f, 45f),
        };

        public static HouseSlot Find(string id)
        {
            foreach (var slot in All)
                if (slot.Id == id) return slot;
            return null;
        }

        public static IEnumerable<HouseSlot> InRoom(string roomId)
        {
            foreach (var slot in All)
                if (slot.Room == roomId) yield return slot;
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
