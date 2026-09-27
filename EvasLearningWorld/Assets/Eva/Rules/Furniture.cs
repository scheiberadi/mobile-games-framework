using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // The kind of spot a piece of furniture fits into. Serialised as an int, so keep the order stable.
    public enum SlotKind { Seat, Floor, Table, Corner, Bed, Wall }

    public sealed class FurnitureItem
    {
        public string Id;
        public SlotKind Kind;
        public int Price;

        public FurnitureItem(string id, SlotKind kind, int price)
        {
            Id = id;
            Kind = kind;
            Price = price;
        }
    }

    public static class FurnitureCatalog
    {
        // The free starter is given to the child when the character is created and is never sold.
        public const string StarterId = "sofa";

        public static readonly IReadOnlyList<FurnitureItem> All = new[]
        {
            new FurnitureItem("sofa", SlotKind.Seat, 0),
            new FurnitureItem("rug", SlotKind.Floor, 5),
            new FurnitureItem("table", SlotKind.Table, 8),
            new FurnitureItem("chest", SlotKind.Corner, 6),
            new FurnitureItem("plant", SlotKind.Corner, 6),
            new FurnitureItem("bed", SlotKind.Bed, 10),
            new FurnitureItem("bookshelf", SlotKind.Wall, 9),
        };

        // Over the items that are for sale; the starter costs 0 and is not one of them.
        public static int CheapestPrice
        {
            get
            {
                var cheapest = int.MaxValue;
                foreach (var item in All)
                    if (item.Id != StarterId && item.Price < cheapest) cheapest = item.Price;
                return cheapest;
            }
        }

        public static FurnitureItem Find(string id)
        {
            foreach (var item in All)
                if (item.Id == id) return item;
            return null;
        }
    }
}
