using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    // CharacterLook's own exclusivity/copy guarantees (M5 Task 1), exercised directly here rather than only
    // indirectly through RigTests - these are exactly what Dress the Character's "keep this look?" flow
    // (M5 Task 4) depends on: Clone() must be a real independent copy (so a discarded try-on never touches
    // the saved look), and Set*/Normalize must keep Dress/Top/Bottom mutually exclusive no matter how a look
    // was assembled.
    public class CharacterLookTests
    {
        [Test]
        public void CloneIsAnIndependentCopy()
        {
            var original = new CharacterLook { Gender = Gender.Girl, Face = 2, Skin = 3 };
            original.SetTop("top_a");
            var clone = original.Clone();

            clone.SetDress("dress_a");
            clone.Skin = 4;

            Assert.That(original.Top, Is.EqualTo("top_a"), "mutating the clone must not affect the original");
            Assert.That(original.Dress, Is.Null);
            Assert.That(original.Skin, Is.EqualTo(3));
            Assert.That(clone.Dress, Is.EqualTo("dress_a"));
            Assert.That(clone.Top, Is.Null, "SetDress on the clone must still clear the clone's own Top");
        }

        [Test]
        public void SetDressClearsTopAndBottom()
        {
            var look = new CharacterLook();
            look.SetTop("top_a");
            look.SetBottom("bottom_a");
            look.SetDress("dress_a");
            Assert.That(look.Top, Is.Null);
            Assert.That(look.Bottom, Is.Null);
            Assert.That(look.Dress, Is.EqualTo("dress_a"));
        }

        [Test]
        public void SetTopOrBottomClearsDress()
        {
            var look = new CharacterLook();
            look.SetDress("dress_a");
            look.SetTop("top_a");
            Assert.That(look.Dress, Is.Null);
            Assert.That(look.Top, Is.EqualTo("top_a"));

            look.SetDress("dress_a");
            look.SetBottom("bottom_a");
            Assert.That(look.Dress, Is.Null);
            Assert.That(look.Bottom, Is.EqualTo("bottom_a"));
        }

        [Test]
        public void SettingAFieldToNullDoesNotDisturbTheOtherSlot()
        {
            var look = new CharacterLook();
            look.SetTop("top_a");
            look.SetBottom(null);
            Assert.That(look.Top, Is.EqualTo("top_a"), "clearing Bottom must not clear an already-set Top");
        }

        // Normalize is the belt-and-braces half of the exclusivity rule - it has to hold even for a
        // CharacterLook assembled by JsonUtility's field-by-field deserialization, which bypasses every
        // setter above entirely (see SaveStore.Load()'s own call to it).
        [Test]
        public void NormalizeClearsTopAndBottomWhenDressIsSet()
        {
            var look = new CharacterLook { Gender = Gender.Girl, Top = "top_a", Bottom = "bottom_a", Dress = "dress_a" };
            look.Normalize();
            Assert.That(look.Top, Is.Null);
            Assert.That(look.Bottom, Is.Null);
            Assert.That(look.Dress, Is.EqualTo("dress_a"));
        }

        [Test]
        public void NormalizeClearsDressForABoy()
        {
            var look = new CharacterLook { Gender = Gender.Boy, Dress = "dress_a" };
            look.Normalize();
            Assert.That(look.Dress, Is.Null);
        }

        [Test]
        public void NormalizeLeavesAValidGirlsLookUntouched()
        {
            var look = new CharacterLook { Gender = Gender.Girl, Top = "top_a", Bottom = "bottom_a", Shoes = "shoes_a" };
            look.Normalize();
            Assert.That(look.Top, Is.EqualTo("top_a"));
            Assert.That(look.Bottom, Is.EqualTo("bottom_a"));
            Assert.That(look.Shoes, Is.EqualTo("shoes_a"));
        }
    }
}
