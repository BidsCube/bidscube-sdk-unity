using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.Tests
{
    public class AdMarkupExtractorTests
    {
        [Test]
        public void TryExtract_FlatAdm_ReturnsMarkup()
        {
            const string json = "{\"adm\":\"<VAST></VAST>\",\"width\":320,\"height\":50}";
            Assert.IsTrue(AdMarkupExtractor.TryExtractMarkup(json, out var adm, out var w, out var h));
            Assert.AreEqual("<VAST></VAST>", adm);
            Assert.AreEqual(320, w);
            Assert.AreEqual(50, h);
        }

        [Test]
        public void TryExtract_SeatbidAdm_ReturnsMarkupAndSize()
        {
            const string json = "{\"seatbid\":[{\"bid\":[{\"adm\":\"<html>ad</html>\",\"w\":300,\"h\":250}]}]}";
            Assert.IsTrue(AdMarkupExtractor.TryExtractMarkup(json, out var adm, out var w, out var h));
            Assert.AreEqual("<html>ad</html>", adm);
            Assert.AreEqual(300, w);
            Assert.AreEqual(250, h);
        }

        [Test]
        public void TryExtract_RawHtml_ReturnsFalse()
        {
            Assert.IsFalse(AdMarkupExtractor.TryExtractMarkup("<html>not json</html>", out var adm, out _, out _));
            Assert.IsNull(adm);
        }

        [Test]
        public void TryExtract_Empty_ReturnsFalse()
        {
            Assert.IsFalse(AdMarkupExtractor.TryExtractMarkup(null, out _, out _, out _));
            Assert.IsFalse(AdMarkupExtractor.TryExtractMarkup("  ", out _, out _, out _));
        }

        [Test]
        public void TryExtract_EscapedJsonAdm_Unescapes()
        {
            const string json = "{\"adm\":\"<VAST>\\\"quoted\\\"</VAST>\"}";
            Assert.IsTrue(AdMarkupExtractor.TryExtractMarkup(json, out var adm, out _, out _));
            StringAssert.Contains("\"quoted\"", adm);
        }
    }
}
