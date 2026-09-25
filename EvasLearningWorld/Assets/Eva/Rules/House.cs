using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class HouseSlot
    {
        public string Id;
        public string Room;
        public SlotKind Kind;
        public float X, Y; // where the item's feet touch the floor, room-local canvas units (centre = 0,0; y up)

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
        // 34 slots: 3-4 per room, 11 in the big attic (hallways have none): the initial placement capacity, not a permanent maximum.
        // X, Y is where an item's feet touch the floor, in room-local canvas units on the room art (1440x960
        // cell, room view shows the middle 900), chosen against the art: the back wall's base is at y about -31
        // (-116 in dining and kids) and the tray covers y below -210, so floor items sit between; wall and corner
        // items stand just in front of the back wall, corners on the side away from the doors, wall slots over
        // bare wall. Ids are room_kind; living_* keep their old ids, the old bedroom_* slots are now kids_*
        // (SaveStore remaps them).
        public static readonly IReadOnlyList<HouseSlot> All = new[]
        {
            new HouseSlot("living_seat", "living", SlotKind.Seat, 0f, -212f),
            new HouseSlot("living_floor", "living", SlotKind.Floor, 0f, -180f),
            new HouseSlot("living_table", "living", SlotKind.Table, 0f, -85f),
            new HouseSlot("living_corner", "living", SlotKind.Corner, -500f, -60f),

            new HouseSlot("dining_seat", "dining", SlotKind.Seat, 0f, -212f),
            new HouseSlot("dining_table", "dining", SlotKind.Table, 0f, -120f),
            new HouseSlot("dining_corner", "dining", SlotKind.Corner, 470f, -140f),
            new HouseSlot("dining_wall", "dining", SlotKind.Wall, -315f, -125f),

            new HouseSlot("kitchen_table", "kitchen", SlotKind.Table, 0f, -95f),
            new HouseSlot("kitchen_floor", "kitchen", SlotKind.Floor, 0f, -185f),
            new HouseSlot("kitchen_wall", "kitchen", SlotKind.Wall, 390f, -40f),
            new HouseSlot("kitchen_corner", "kitchen", SlotKind.Corner, 575f, -80f),

            new HouseSlot("parents_bed", "parents", SlotKind.Bed, 14f, -90f),
            new HouseSlot("parents_floor", "parents", SlotKind.Floor, 0f, -190f),
            new HouseSlot("parents_wall", "parents", SlotKind.Wall, -317f, -40f),
            new HouseSlot("parents_corner", "parents", SlotKind.Corner, -540f, -60f),

            new HouseSlot("kids_bed", "kids", SlotKind.Bed, 36f, -135f),
            new HouseSlot("kids_floor", "kids", SlotKind.Floor, 0f, -195f),
            new HouseSlot("kids_wall", "kids", SlotKind.Wall, -315f, -125f),
            new HouseSlot("kids_corner", "kids", SlotKind.Corner, 470f, -140f),

            new HouseSlot("bath_floor", "bath", SlotKind.Floor, 0f, -185f),
            new HouseSlot("bath_wall", "bath", SlotKind.Wall, -410f, -40f),
            new HouseSlot("bath_corner", "bath", SlotKind.Corner, 520f, -70f),

            new HouseSlot("party_seat", "party", SlotKind.Seat, -320f, -200f),
            new HouseSlot("party_seat_2", "party", SlotKind.Seat, 0f, -205f),
            new HouseSlot("party_seat_3", "party", SlotKind.Seat, 600f, -205f),
            new HouseSlot("party_floor", "party", SlotKind.Floor, -520f, -130f),
            new HouseSlot("party_floor_2", "party", SlotKind.Floor, 560f, -130f),
            new HouseSlot("party_table", "party", SlotKind.Table, 110f, -115f),
            new HouseSlot("party_table_2", "party", SlotKind.Table, 430f, -130f),
            new HouseSlot("party_corner", "party", SlotKind.Corner, -730f, -120f),
            new HouseSlot("party_corner_2", "party", SlotKind.Corner, 730f, -120f),
            new HouseSlot("party_wall", "party", SlotKind.Wall, 90f, -45f),
            new HouseSlot("party_wall_2", "party", SlotKind.Wall, 390f, -45f),
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
