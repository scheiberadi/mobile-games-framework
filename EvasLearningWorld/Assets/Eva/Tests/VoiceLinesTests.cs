using EvasLearningWorld.App;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class VoiceLinesTests
    {
        [Test]
        public void ParseIgnoresCommentsAndBlankLines()
        {
            var lines = VoiceLines.Parse("# header\n\n   \nhello\tHello Eva\n  # indented comment\nbye\tGoodbye\n");
            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("Hello Eva", lines["hello"]);
            Assert.AreEqual("Goodbye", lines["bye"]);
        }

        [Test]
        public void ParseSplitsOnTheFirstTabOnly()
        {
            var lines = VoiceLines.Parse("key\tfirst\tsecond");
            Assert.AreEqual("first\tsecond", lines["key"]);
        }

        [Test]
        public void ParseHandlesWindowsLineEndings()
        {
            var lines = VoiceLines.Parse("a\tOne\r\nb\tTwo\r\n");
            Assert.AreEqual("One", lines["a"]);
            Assert.AreEqual("Two", lines["b"]);
        }

        [Test]
        public void TextForReturnsTheKeyWhenMissing()
        {
            Assert.AreEqual("no_such_line_key", VoiceLines.TextFor("no_such_line_key"));
        }

        [Test]
        public void TextForReturnsParsedTextWhenPresent()
        {
            var lines = VoiceLines.Parse("greet\tHi!");
            Assert.AreEqual("Hi!", VoiceLines.TextFor("greet", lines));
            Assert.AreEqual("other", VoiceLines.TextFor("other", lines));
        }
    }
}
