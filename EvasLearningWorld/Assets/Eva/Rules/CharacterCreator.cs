using System;

namespace EvasLearningWorld.Rules
{
    // One step of the first-run Creator's category rail. Order here is the order the child pages through them.
    public enum CreatorCategory { Gender, Skin, Face, HairStyle, HairColor, EyeColor, Top, Bottom, Dress, Shoes, Glasses }

    // Pure rules behind CreatorScreen (M5 Task 3): which categories a gender sees, how many choices each has,
    // which one is currently picked, applying a pick, and the randomize roll. No Unity types, so it is testable
    // directly (Tests/CharacterCreatorTests.cs). The screen only draws what this reports.
    //
    // v1 counts come from art/character/STYLE.md's approved list: 3 faces per gender, 3 boy / 4 girl haircuts,
    // wardrobe pools from WardrobeCatalog. Skin/HairColor/EyeColor are the CharacterLook palette sizes.
    public static class CharacterCreator
    {
        public const int FacesPerGender = 3;
        public const int BoyHairStyles = 3;
        public const int GirlHairStyles = 4;

        private static readonly CreatorCategory[] BoyCategories =
        {
            CreatorCategory.Gender, CreatorCategory.Skin, CreatorCategory.Face, CreatorCategory.HairStyle,
            CreatorCategory.HairColor, CreatorCategory.EyeColor, CreatorCategory.Top, CreatorCategory.Bottom,
            CreatorCategory.Shoes, CreatorCategory.Glasses,
        };

        private static readonly CreatorCategory[] GirlCategories =
        {
            CreatorCategory.Gender, CreatorCategory.Skin, CreatorCategory.Face, CreatorCategory.HairStyle,
            CreatorCategory.HairColor, CreatorCategory.EyeColor, CreatorCategory.Top, CreatorCategory.Bottom,
            CreatorCategory.Dress, CreatorCategory.Shoes, CreatorCategory.Glasses,
        };

        public static CreatorCategory[] CategoriesFor(Gender gender) => gender == Gender.Boy ? BoyCategories : GirlCategories;

        // The largest OptionCount of any category, i.e. how many choice buttons the screen must be able to lay out.
        public const int MaxOptions = 6;

        // How many buttons the category shows. Glasses counts its "none" choice as the last button.
        public static int OptionCount(CreatorCategory category, Gender gender)
        {
            switch (category)
            {
                case CreatorCategory.Gender: return 2;
                case CreatorCategory.Skin: return CharacterLook.ColorCount;
                case CreatorCategory.Face: return FacesPerGender;
                case CreatorCategory.HairStyle: return gender == Gender.Boy ? BoyHairStyles : GirlHairStyles;
                case CreatorCategory.HairColor: return CharacterLook.HairColorCount;
                case CreatorCategory.EyeColor: return CharacterLook.EyeColorCount;
                case CreatorCategory.Top: return WardrobeCatalog.IdsFor(WardrobeSlot.Top, gender).Length;
                case CreatorCategory.Bottom: return WardrobeCatalog.IdsFor(WardrobeSlot.Bottom, gender).Length;
                case CreatorCategory.Dress: return WardrobeCatalog.IdsFor(WardrobeSlot.Dress, gender).Length;
                case CreatorCategory.Shoes: return WardrobeCatalog.IdsFor(WardrobeSlot.Shoes, gender).Length;
                case CreatorCategory.Glasses: return WardrobeCatalog.IdsFor(WardrobeSlot.Glasses, gender).Length + 1;
                default: throw new ArgumentOutOfRangeException(nameof(category));
            }
        }

        // The wardrobe id behind a wardrobe-category button, or null for Glasses' trailing "none" button (and for
        // non-wardrobe categories).
        public static string ItemIdAt(CreatorCategory category, Gender gender, int index)
        {
            var slot = SlotOf(category);
            if (slot == null) return null;
            var ids = WardrobeCatalog.IdsFor(slot.Value, gender);
            return index >= 0 && index < ids.Length ? ids[index] : null;
        }

        // A fresh character ready to confirm without touching anything: index 0 of every category, wearing the
        // first Top, Bottom and Shoes of the gender (Dress and Glasses start off).
        public static CharacterLook DefaultLook(Gender gender)
        {
            var look = new CharacterLook { Gender = gender };
            look.SetTop(WardrobeCatalog.IdsFor(WardrobeSlot.Top, gender)[0]);
            look.SetBottom(WardrobeCatalog.IdsFor(WardrobeSlot.Bottom, gender)[0]);
            look.Shoes = WardrobeCatalog.IdsFor(WardrobeSlot.Shoes, gender)[0];
            return look;
        }

        // Which button the look currently has picked in a category, or -1 if none is (nothing ringed).
        public static int SelectedIndex(CharacterLook look, CreatorCategory category)
        {
            switch (category)
            {
                case CreatorCategory.Gender: return (int)look.Gender;
                case CreatorCategory.Skin: return look.Skin;
                case CreatorCategory.Face: return look.Face;
                case CreatorCategory.HairStyle: return look.HairStyle;
                case CreatorCategory.HairColor: return look.HairColor;
                case CreatorCategory.EyeColor: return look.EyeColor;
                case CreatorCategory.Glasses:
                    return look.Glasses == null
                        ? OptionCount(category, look.Gender) - 1
                        : Array.IndexOf(WardrobeCatalog.IdsFor(WardrobeSlot.Glasses, look.Gender), look.Glasses);
                default:
                    var slot = SlotOf(category).Value;
                    return Array.IndexOf(WardrobeCatalog.IdsFor(slot, look.Gender), WornId(look, slot));
            }
        }

        // Applies one button tap. Picking a gender swaps the whole look to that gender's default outfit (item ids
        // are gendered) but keeps the child's colour/face choices that still make sense; picking a Top/Bottom
        // clears Dress and picking a Dress clears Top/Bottom, via CharacterLook's own setters.
        public static void Select(CharacterLook look, CreatorCategory category, int index)
        {
            switch (category)
            {
                case CreatorCategory.Gender: SetGender(look, (Gender)index); break;
                case CreatorCategory.Skin: look.Skin = index; break;
                case CreatorCategory.Face: look.Face = index; break;
                case CreatorCategory.HairStyle: look.HairStyle = index; break;
                case CreatorCategory.HairColor: look.HairColor = index; break;
                case CreatorCategory.EyeColor: look.EyeColor = index; break;
                case CreatorCategory.Top: look.SetTop(ItemIdAt(category, look.Gender, index)); break;
                case CreatorCategory.Bottom: look.SetBottom(ItemIdAt(category, look.Gender, index)); break;
                case CreatorCategory.Dress: look.SetDress(ItemIdAt(category, look.Gender, index)); break;
                case CreatorCategory.Shoes: look.Shoes = ItemIdAt(category, look.Gender, index); break;
                case CreatorCategory.Glasses: look.Glasses = ItemIdAt(category, look.Gender, index); break;
                default: throw new ArgumentOutOfRangeException(nameof(category));
            }
        }

        // Switching gender keeps Skin/HairColor/EyeColor and clamps Face/HairStyle into the new gender's range;
        // wardrobe goes back to that gender's default outfit with glasses kept (glasses are shared).
        public static void SetGender(CharacterLook look, Gender gender)
        {
            if (look.Gender == gender) return;
            var skin = look.Skin;
            var hairColor = look.HairColor;
            var eyeColor = look.EyeColor;
            var glasses = look.Glasses;
            var fresh = DefaultLook(gender);
            look.Gender = gender;
            look.Face = Math.Min(look.Face, FacesPerGender - 1);
            look.HairStyle = Math.Min(look.HairStyle, OptionCount(CreatorCategory.HairStyle, gender) - 1);
            look.Skin = skin; look.HairColor = hairColor; look.EyeColor = eyeColor;
            look.Top = fresh.Top; look.Bottom = fresh.Bottom; look.Dress = null; look.Shoes = fresh.Shoes;
            look.Glasses = glasses;
        }

        // One uniform pick per category, kept inside this gender's own options. Gender itself is NOT rerolled
        // (a reroll that flipped the child's chosen gender would be a surprise, not a delight). For girls the
        // outfit is a fair coin between Top+Bottom and a Dress, so Dress/Bottom exclusivity holds by
        // construction. `next(n)` returns a uniform int in [0, n) - injected so tests are deterministic.
        public static void Randomize(CharacterLook look, Func<int, int> next)
        {
            var gender = look.Gender;
            Pick(look, CreatorCategory.Skin, next);
            Pick(look, CreatorCategory.Face, next);
            Pick(look, CreatorCategory.HairStyle, next);
            Pick(look, CreatorCategory.HairColor, next);
            Pick(look, CreatorCategory.EyeColor, next);
            Pick(look, CreatorCategory.Shoes, next);
            Pick(look, CreatorCategory.Glasses, next);
            if (gender == Gender.Girl && next(2) == 0)
            {
                Pick(look, CreatorCategory.Dress, next);
            }
            else
            {
                Pick(look, CreatorCategory.Top, next);
                Pick(look, CreatorCategory.Bottom, next);
            }
        }

        private static void Pick(CharacterLook look, CreatorCategory category, Func<int, int> next) =>
            Select(look, category, next(OptionCount(category, look.Gender)));

        private static WardrobeSlot? SlotOf(CreatorCategory category)
        {
            switch (category)
            {
                case CreatorCategory.Top: return WardrobeSlot.Top;
                case CreatorCategory.Bottom: return WardrobeSlot.Bottom;
                case CreatorCategory.Dress: return WardrobeSlot.Dress;
                case CreatorCategory.Shoes: return WardrobeSlot.Shoes;
                case CreatorCategory.Glasses: return WardrobeSlot.Glasses;
                default: return null;
            }
        }

        private static string WornId(CharacterLook look, WardrobeSlot slot)
        {
            switch (slot)
            {
                case WardrobeSlot.Top: return look.Top;
                case WardrobeSlot.Bottom: return look.Bottom;
                case WardrobeSlot.Dress: return look.Dress;
                case WardrobeSlot.Shoes: return look.Shoes;
                default: return look.Glasses;
            }
        }
    }
}
