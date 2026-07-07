using NUnit.Framework;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class VastAdSequenceParserTests
    {
        const string MultiAdVast = @"<VAST version=""3.0"">
<Ad sequence=""2""><InLine><Creatives><Creative><Linear><Duration>00:00:30</Duration><MediaFiles><MediaFile><![CDATA[https://example.com/2.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad>
<Ad sequence=""1""><InLine><Creatives><Creative><Linear><Duration>00:00:15</Duration><MediaFiles><MediaFile><![CDATA[https://example.com/1.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad>
</VAST>";

        [Test]
        public void ExtractAdDocuments_ReturnsMultipleStandaloneVasts()
        {
            var docs = VastAdSequenceParser.ExtractAdDocuments(MultiAdVast);
            Assert.AreEqual(2, docs.Count);
            StringAssert.Contains("sequence=\"2\"", docs[0]);
            StringAssert.Contains("sequence=\"1\"", docs[1]);
        }

        [Test]
        public void FirstLinearDurationSeconds_ParsesHms()
        {
            Assert.AreEqual(15, VastAdSequenceParser.FirstLinearDurationSeconds(
                "<VAST><Ad><InLine><Creatives><Creative><Linear><Duration>00:00:15</Duration></Linear></Creative></Creatives></InLine></Ad></VAST>"));
        }

        [Test]
        public void ContentLikelyContainsVast_DetectsRoot()
        {
            Assert.IsTrue(VastAdSequenceParser.ContentLikelyContainsVast("<VAST version=\"3.0\"></VAST>"));
            Assert.IsFalse(VastAdSequenceParser.ContentLikelyContainsVast("not vast"));
        }
    }
}
