using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Every sprite the app asks for by name must exist and be imported as a sprite. EvaUi.Sprite falls back to a
    // placeholder for a missing file, so these tests load the files directly to notice a missing or badly imported one.
    public class ArtTests
    {
        private static readonly string[] Names =
        {
            "icons/coin", "icons/check", "icons/cross", "icons/home", "icons/replay", "icons/bubble", "icons/hand",
            "icons/dot", "icons/tile", "icons/arrow", "icons/dollhouse",
            "house/room_living", "house/room_dining", "house/room_kitchen", "house/room_parents",
            "house/room_kids", "house/room_bath", "house/room_party",
            "house/hall_ground", "house/hall_upper", "house/shell", "house/shell_back", "house/outside",
            "objects/apple", "objects/star", "objects/duck", "objects/flower",
            "objects/sofa", "objects/rug", "objects/table", "objects/chest", "objects/plant", "objects/bed", "objects/bookshelf",
            "world/map_bg", "world/school_bg", "world/house_icon", "world/school_icon", "world/store_icon", "world/house_bg", "world/store_bg", "world/school_list_bg", "activities/count",
            "world/map_world_left", "world/map_world_right", "world/place_house", "world/place_school", "world/place_store",
            "icons/gear",
            "characters/char_torso", "characters/char_arm", "characters/char_leg",
            "characters/char_head_0", "characters/char_head_1", "characters/char_head_2", "characters/char_head_3",
            "cat/cat_shadow", "cat/cat_tail", "cat/cat_body", "cat/cat_legL", "cat/cat_legR", "cat/cat_chest",
            "cat/cat_earL", "cat/cat_earR", "cat/cat_head", "cat/cat_eyes", "cat/cat_mouth"
        };

        [Test]
        public void EverySpriteResolvesThroughResources()
        {
            foreach (var name in Names)
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + name), "missing or not imported as a sprite: Art/" + name);
        }

        [Test]
        public void NoSpriteIsSmallerThan64Pixels()
        {
            foreach (var name in Names)
            {
                var sprite = Resources.Load<Sprite>("Art/" + name);
                Assert.IsNotNull(sprite, name);
                Assert.GreaterOrEqual(sprite.rect.width, 64f, name + " width");
                Assert.GreaterOrEqual(sprite.rect.height, 64f, name + " height");
            }
        }

        [Test]
        public void ImporterAppliedTheSpriteSettingsOnFirstImport()
        {
            foreach (var name in Names)
            {
                var importer = AssetImporter.GetAtPath("Assets/Eva/Resources/Art/" + name + ".png") as TextureImporter;
                Assert.IsNotNull(importer, name);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name);
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, name);
                Assert.IsTrue(importer.alphaIsTransparency, name);
                Assert.IsFalse(importer.mipmapEnabled, name);
                Assert.AreEqual(100f, importer.spritePixelsPerUnit, name);
            }
        }
    }
}
