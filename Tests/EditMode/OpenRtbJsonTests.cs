using NUnit.Framework;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class OpenRtbJsonTests
    {
        [Test]
        public void TryParseObject_ValidObject_ReturnsTrue()
        {
            Assert.IsTrue(OpenRtbJson.TryParseObject("{\"a\":1}", out var root));
            Assert.IsTrue(root.ContainsKey("a"));
        }

        [Test]
        public void TryParseObject_TrailingGarbage_ReturnsFalse()
        {
            Assert.IsFalse(OpenRtbJson.TryParseObject("{\"a\":1}extra", out _));
        }

        [Test]
        public void TryParseObject_TrailingWhitespace_ReturnsTrue()
        {
            Assert.IsTrue(OpenRtbJson.TryParseObject("{\"a\":1} \n\t", out _));
        }
    }
}
