using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.Tests
{
    public class VASTParserTests
    {
        [Test]
        public void Parse_Empty_ReturnsNull()
        {
            Assert.IsNull(VASTParser.Parse(null));
            Assert.IsNull(VASTParser.Parse(""));
        }

        [Test]
        public void Parse_MalformedXml_ReturnsNull()
        {
            Assert.IsNull(VASTParser.Parse("<not-vast>broken"));
        }

        [Test]
        public void Parse_NoCompanion_HasVideoUrl()
        {
            const string vast = @"<VAST version=""3.0""><Ad><InLine><Creatives>
<Creative><Linear><Duration>00:00:10</Duration>
<MediaFiles><MediaFile type=""video/mp4""><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles>
</Linear></Creative>
</Creatives></InLine></Ad></VAST>";

            var data = VASTParser.Parse(vast);
            Assert.NotNull(data);
            Assert.AreEqual("https://example.com/v.mp4", data.videoUrl);
            Assert.IsFalse(data.HasCompanion);
            Assert.AreEqual(VASTParser.VastCompanionResourceType.None, data.companionResourceType);
            Assert.AreEqual(-1, data.skipOffset);
            Assert.AreEqual(10, data.duration);
        }

        [Test]
        public void Parse_StaticCompanion_SetsPreviewAndClick()
        {
            const string vast = @"<VAST version=""3.0""><Ad><InLine><Creatives>
<Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles></Linear></Creative>
<Creative><CompanionAds>
  <Companion>
    <StaticResource creativeType=""image/jpeg""><![CDATA[https://example.com/end.jpg]]></StaticResource>
    <CompanionClickThrough><![CDATA[https://example.com/click]]></CompanionClickThrough>
  </Companion>
</CompanionAds></Creative>
</Creatives></InLine></Ad></VAST>";

            var data = VASTParser.Parse(vast);
            Assert.NotNull(data);
            Assert.IsTrue(data.HasCompanion);
            Assert.AreEqual(VASTParser.VastCompanionResourceType.StaticImage, data.companionResourceType);
            Assert.AreEqual("https://example.com/end.jpg", data.companionResource);
            Assert.AreEqual("https://example.com/click", data.previewClickThroughUrl);
        }

        [Test]
        public void Parse_SkipOffsetHhMmSs_IsFiveSeconds()
        {
            const string vast = @"<VAST version=""3.0""><Ad><InLine><Creatives>
<Creative><Linear skipoffset=""00:00:05""><MediaFiles>
<MediaFile><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles></Linear></Creative>
</Creatives></InLine></Ad></VAST>";

            var data = VASTParser.Parse(vast);
            Assert.NotNull(data);
            Assert.AreEqual(5, data.skipOffset);
        }

        [Test]
        public void ParseSkipOffset_Formats()
        {
            Assert.AreEqual(5, VASTParser.ParseSkipOffset("00:00:05"));
            Assert.AreEqual(5, VASTParser.ParseSkipOffset("5"));
            Assert.AreEqual(5, VASTParser.ParseSkipOffset("5s"));
            Assert.AreEqual(-1, VASTParser.ParseSkipOffset(""));
            Assert.AreEqual(-1, VASTParser.ParseSkipOffset(null));
        }

        [Test]
        public void Parse_TrackingUrls_AreCollected()
        {
            const string vast = @"<VAST version=""3.0""><Ad><InLine>
<Impression><![CDATA[https://example.com/imp]]></Impression>
<Creatives><Creative><Linear>
<TrackingEvents>
  <Tracking event=""start""><![CDATA[https://example.com/start]]></Tracking>
  <Tracking event=""complete""><![CDATA[https://example.com/complete]]></Tracking>
  <Tracking event=""skip""><![CDATA[https://example.com/skip]]></Tracking>
</TrackingEvents>
<MediaFiles><MediaFile><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles>
</Linear></Creative></Creatives></InLine></Ad></VAST>";

            var data = VASTParser.Parse(vast);
            Assert.NotNull(data);
            Assert.AreEqual(1, data.impressionUrls.Count);
            Assert.AreEqual("https://example.com/imp", data.impressionUrls[0]);
            Assert.AreEqual(1, data.startUrls.Count);
            Assert.AreEqual(1, data.completeUrls.Count);
            Assert.AreEqual(1, data.skipUrls.Count);
        }

        [Test]
        public void Wrapper_IsDetected_AndParseReturnsNull()
        {
            const string wrapper = @"<VAST version=""3.0""><Ad><Wrapper>
<VASTAdTagURI><![CDATA[https://example.com/vast.xml]]></VASTAdTagURI>
</Wrapper></Ad></VAST>";

            Assert.IsTrue(VASTParser.IsWrapperVAST(wrapper));
            Assert.IsNull(VASTParser.Parse(wrapper));
            Assert.AreEqual("https://example.com/vast.xml", VASTParser.ExtractVASTAdTagURI(wrapper));
        }

        [Test]
        public void QaVast_NoPreview_HasVideoAndNoCompanion()
        {
            var data = VASTParser.Parse(QaVastFixtures.NoPreview);
            Assert.NotNull(data);
            StringAssert.Contains(".mp4", data.videoUrl);
            Assert.IsFalse(data.HasCompanion);
            Assert.AreEqual(-1, data.skipOffset);
        }

        [Test]
        public void QaVast_WithPreview_ParsesSkipCompanionAndClick()
        {
            var data = VASTParser.Parse(QaVastFixtures.WithPreview);
            Assert.NotNull(data);
            StringAssert.Contains("big_buck_bunny.mp4", data.videoUrl);
            Assert.AreEqual(5, data.skipOffset);
            Assert.AreEqual(30, data.duration);
            Assert.IsTrue(data.HasCompanion);
            Assert.AreEqual(VASTParser.VastCompanionResourceType.StaticImage, data.companionResourceType);
            Assert.AreEqual("https://www.gstatic.com/webp/gallery/3.jpg", data.companionResource);
            Assert.AreEqual("https://www.google.com", data.previewClickThroughUrl);
            Assert.AreEqual("https://www.google.com", data.clickThroughUrl);
            Assert.AreEqual(1, data.impressionUrls.Count);
            Assert.AreEqual(1, data.startUrls.Count);
            Assert.AreEqual(1, data.completeUrls.Count);
            Assert.AreEqual(1, data.skipUrls.Count);
        }

        [Test]
        public void Parse_SecondAdImpression_IsNotAttachedToTheFirstAd()
        {
            const string vast = @"<VAST version=""3.0"">
<Ad id=""first""><InLine>
<Impression><![CDATA[https://track.example/first]]></Impression>
<Creatives><Creative><Linear>
<MediaFiles><MediaFile><![CDATA[https://cdn.example/first.mp4]]></MediaFile></MediaFiles>
</Linear></Creative></Creatives></InLine></Ad>
<Ad id=""second""><InLine>
<Impression><![CDATA[https://track.example/second]]></Impression>
<Creatives><Creative><Linear>
<MediaFiles><MediaFile><![CDATA[https://cdn.example/second.mp4]]></MediaFile></MediaFiles>
</Linear></Creative></Creatives></InLine></Ad>
</VAST>";

            var data = VASTParser.Parse(vast);
            Assert.NotNull(data);
            Assert.AreEqual("https://cdn.example/first.mp4", data.videoUrl);
            Assert.AreEqual(1, data.impressionUrls.Count);
            Assert.AreEqual("https://track.example/first", data.impressionUrls[0]);
            Assert.IsFalse(data.impressionUrls.Contains("https://track.example/second"));
        }

        [Test]
        public void ExpandTrackingUrl_ReplacesCacheAndTimestampOnly()
        {
            var expanded = VASTParser.ExpandTrackingUrl("https://track.example/imp?cb=[CACHEBUSTING]&ts=[TIMESTAMP]&err=[ERRORCODE]");
            Assert.IsFalse(expanded.Contains("[CACHEBUSTING]"));
            Assert.IsFalse(expanded.Contains("[TIMESTAMP]"));
            Assert.IsTrue(expanded.Contains("[ERRORCODE]"));
        }
    }
}
