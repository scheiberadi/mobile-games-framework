using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    // Rules behind the rebuilt CreatorScreen (M5 Task 3): category lists, option counts, selection, gender switch
    // and the randomize roll's validity guarantees.
    public class CharacterCreatorTests
    {
        [Test]
        public void EveryCategoryFitsTheScreensChoiceGrid()
        {
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            foreach (var category in CharacterCreator.CategoriesFor(gender))
            {
                var count = CharacterCreator.OptionCount(category, gender);
                Assert.That(count, Is.InRange(1, CharacterCreator.MaxOptions), category + " " + gender);
            }
        }

        [Test]
        public void OnlyGirlsGetADressStep()
        {
            Assert.That(CharacterCreator.CategoriesFor(Gender.Boy), Does.Not.Contain(CreatorCategory.Dress));
            Assert.That(CharacterCreator.CategoriesFor(Gender.Girl), Does.Contain(CreatorCategory.Dress));
        }

        [Test]
        public void DefaultLookIsValidAndEveryCategoryReportsASelection()
        {
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            {
                var look = CharacterCreator.DefaultLook(gender);
                foreach (var category in CharacterCreator.CategoriesFor(gender))
                {
                    var selected = CharacterCreator.SelectedIndex(look, category);
                    if (category == CreatorCategory.Dress) { Assert.That(selected, Is.EqualTo(-1)); continue; }
                    Assert.That(selected, Is.InRange(0, CharacterCreator.OptionCount(category, gender) - 1), category.ToString());
                }
                Assert.That(look.Dress, Is.Null);
            }
        }

        [Test]
        public void PickingADressClearsTopAndBottomAndPickingATopClearsTheDress()
        {
            var look = CharacterCreator.DefaultLook(Gender.Girl);
            CharacterCreator.Select(look, CreatorCategory.Dress, 1);
            Assert.That(look.Dress, Is.EqualTo("dress_girl_1"));
            Assert.That(look.Top, Is.Null);
            Assert.That(look.Bottom, Is.Null);
            Assert.That(CharacterCreator.SelectedIndex(look, CreatorCategory.Dress), Is.EqualTo(1));

            CharacterCreator.Select(look, CreatorCategory.Top, 2);
            Assert.That(look.Dress, Is.Null);
            Assert.That(look.Top, Is.EqualTo("top_girl_2"));
        }

        [Test]
        public void GlassesNoneIsTheLastButtonAndClearsTheSlot()
        {
            var look = CharacterCreator.DefaultLook(Gender.Boy);
            CharacterCreator.Select(look, CreatorCategory.Glasses, 1);
            Assert.That(look.Glasses, Is.EqualTo("glasses_1"));
            var none = CharacterCreator.OptionCount(CreatorCategory.Glasses, Gender.Boy) - 1;
            CharacterCreator.Select(look, CreatorCategory.Glasses, none);
            Assert.That(look.Glasses, Is.Null);
            Assert.That(CharacterCreator.SelectedIndex(look, CreatorCategory.Glasses), Is.EqualTo(none));
        }

        [Test]
        public void SwitchingGenderKeepsColoursAndSwapsToTheNewGendersOutfit()
        {
            var look = CharacterCreator.DefaultLook(Gender.Boy);
            look.Skin = 3; look.HairColor = 4; look.EyeColor = 5; look.HairStyle = 2;
            CharacterCreator.Select(look, CreatorCategory.Glasses, 0);

            CharacterCreator.Select(look, CreatorCategory.Gender, (int)Gender.Girl);

            Assert.That(look.Gender, Is.EqualTo(Gender.Girl));
            Assert.That(look.Skin, Is.EqualTo(3));
            Assert.That(look.HairColor, Is.EqualTo(4));
            Assert.That(look.EyeColor, Is.EqualTo(5));
            Assert.That(look.HairStyle, Is.EqualTo(2));
            Assert.That(look.Top, Is.EqualTo("top_girl_0"));
            Assert.That(look.Bottom, Is.EqualTo("bottom_girl_0"));
            Assert.That(look.Shoes, Is.EqualTo("shoes_girl_0"));
            Assert.That(look.Glasses, Is.EqualTo("glasses_0"));
        }

        [Test]
        public void SwitchingToBoyClampsGirlOnlyHairStyleAndDropsTheDress()
        {
            var look = CharacterCreator.DefaultLook(Gender.Girl);
            CharacterCreator.Select(look, CreatorCategory.HairStyle, 3);
            CharacterCreator.Select(look, CreatorCategory.Dress, 0);

            CharacterCreator.SetGender(look, Gender.Boy);

            Assert.That(look.HairStyle, Is.EqualTo(CharacterCreator.BoyHairStyles - 1));
            Assert.That(look.Dress, Is.Null);
            Assert.That(look.Top, Is.EqualTo("top_boy_0"));
        }

        [Test]
        public void RandomizeAlwaysProducesAValidLookWithinRange()
        {
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            {
                var rng = new Random(7);
                var sawDress = false; var sawTop = false;
                for (var run = 0; run < 300; run++)
                {
                    var look = CharacterCreator.DefaultLook(gender);
                    CharacterCreator.Randomize(look, n => rng.Next(0, n));

                    Assert.That(look.Gender, Is.EqualTo(gender));
                    Assert.That(look.Dress != null && (look.Top != null || look.Bottom != null), Is.False, "Dress with Top/Bottom");
                    Assert.That(look.Dress != null || (look.Top != null && look.Bottom != null), Is.True, "outfit incomplete");
                    if (gender == Gender.Boy) Assert.That(look.Dress, Is.Null);
                    foreach (var category in CharacterCreator.CategoriesFor(gender))
                    {
                        if (category == CreatorCategory.Gender) continue;
                        var selected = CharacterCreator.SelectedIndex(look, category);
                        if (category == CreatorCategory.Dress) continue;
                        if (look.Dress != null && (category == CreatorCategory.Top || category == CreatorCategory.Bottom)) continue;
                        Assert.That(selected, Is.InRange(0, CharacterCreator.OptionCount(category, gender) - 1), category.ToString());
                    }
                    if (look.Dress != null) sawDress = true; else sawTop = true;
                }
                Assert.That(sawTop, Is.True);
                Assert.That(sawDress, Is.EqualTo(gender == Gender.Girl), "girls should sometimes roll a dress, boys never");
            }
        }

        [Test]
        public void CatalogHasUniqueIdsAndTheApprovedV1Counts()
        {
            Assert.That(WardrobeCatalog.All.Select(i => i.Id).Distinct().Count(), Is.EqualTo(WardrobeCatalog.All.Length));
            Assert.That(WardrobeCatalog.All.Length, Is.EqualTo(4 + 4 + 3 + 3 + 3 + 3 + 3 + 3)); // 26 wardrobe items (faces/hair are 20 more of the 46)
            Assert.That(WardrobeCatalog.IdsFor(WardrobeSlot.Dress, Gender.Boy), Is.Empty);
            Assert.That(WardrobeCatalog.IdsFor(WardrobeSlot.Glasses, Gender.Boy), Is.EqualTo(WardrobeCatalog.IdsFor(WardrobeSlot.Glasses, Gender.Girl)));
        }
    }
}
