using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class VideoAutoCloseConfigTests
    {
        [Test]
        public void AutoClose_Default_IsFalse()
        {
            var config = new SDKConfig.Builder().Build();
            Assert.IsFalse(config.AutoClose);
        }

        [Test]
        public void AutoClose_ExplicitTrue_IsTrue()
        {
            var config = new SDKConfig.Builder().AutoClose(true).Build();
            Assert.IsTrue(config.AutoClose);
        }

        [Test]
        public void AutoClose_ExplicitFalse_IsFalse()
        {
            var config = new SDKConfig.Builder().AutoClose(false).Build();
            Assert.IsFalse(config.AutoClose);
        }
    }

    public class VideoSessionEndPolicyTests
    {
        [Test]
        public void Resolve_AutoCloseTrue_WithCompanion_StillAutoCloses()
        {
            Assert.AreEqual(
                VideoSessionEndAction.AutoClose,
                VideoSessionEndPolicy.Resolve(autoClose: true, hasCompanion: true));
        }

        [Test]
        public void Resolve_AutoCloseTrue_WithoutCompanion_AutoCloses()
        {
            Assert.AreEqual(
                VideoSessionEndAction.AutoClose,
                VideoSessionEndPolicy.Resolve(autoClose: true, hasCompanion: false));
        }

        [Test]
        public void Resolve_AutoCloseFalse_WithCompanion_ShowsCompanion()
        {
            Assert.AreEqual(
                VideoSessionEndAction.ShowCompanionEndCard,
                VideoSessionEndPolicy.Resolve(autoClose: false, hasCompanion: true));
        }

        [Test]
        public void Resolve_AutoCloseFalse_WithoutCompanion_KeepsLastFrame()
        {
            Assert.AreEqual(
                VideoSessionEndAction.KeepLastFrameOrPostVideoContent,
                VideoSessionEndPolicy.Resolve(autoClose: false, hasCompanion: false));
        }

        [Test]
        public void ShouldGrantReward_OnlyRewardedNaturalComplete()
        {
            Assert.IsTrue(VideoSessionEndPolicy.ShouldGrantReward(
                VideoAdFormat.Rewarded, completedNaturally: true, wasSkipped: false, alreadyRewarded: false));
            Assert.IsFalse(VideoSessionEndPolicy.ShouldGrantReward(
                VideoAdFormat.Rewarded, completedNaturally: false, wasSkipped: true, alreadyRewarded: false));
            Assert.IsFalse(VideoSessionEndPolicy.ShouldGrantReward(
                VideoAdFormat.Interstitial, completedNaturally: true, wasSkipped: false, alreadyRewarded: false));
            Assert.IsFalse(VideoSessionEndPolicy.ShouldGrantReward(
                VideoAdFormat.Rewarded, completedNaturally: true, wasSkipped: false, alreadyRewarded: true));
        }
    }

    public class VastCompanionParserTests
    {
        [Test]
        public void Parse_StaticCompanion_SetsStaticImage()
        {
            const string vast = @"<VAST version=""3.0""><Ad><InLine><Creatives>
<Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles></Linear></Creative>
<Creative><CompanionAds>
  <Companion width=""1080"" height=""1920"">
    <StaticResource creativeType=""image/jpeg""><![CDATA[https://example.com/end-screen.jpg]]></StaticResource>
    <CompanionClickThrough><![CDATA[https://example.com/click]]></CompanionClickThrough>
    <CompanionClickTracking><![CDATA[https://example.com/click-track]]></CompanionClickTracking>
    <TrackingEvents><Tracking event=""creativeView""><![CDATA[https://example.com/view]]></Tracking></TrackingEvents>
  </Companion>
</CompanionAds></Creative>
</Creatives></InLine></Ad></VAST>";

            var data = VASTParser.Parse(vast);
            Assert.NotNull(data);
            Assert.IsTrue(data.HasCompanion);
            Assert.AreEqual(VASTParser.VastCompanionResourceType.StaticImage, data.companionResourceType);
            Assert.AreEqual("https://example.com/end-screen.jpg", data.companionResource);
            Assert.AreEqual("https://example.com/click", data.previewClickThroughUrl);
            Assert.AreEqual(1, data.companionClickTrackingUrls.Count);
            Assert.AreEqual(1, data.companionViewTrackingUrls.Count);
        }

        [Test]
        public void Parse_HtmlPreferredOverStaticAndIFrame()
        {
            const string vast = @"<VAST version=""3.0""><Ad><InLine><Creatives>
<Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles></Linear></Creative>
<Creative><CompanionAds>
  <Companion width=""300"" height=""250"">
    <StaticResource creativeType=""image/jpeg""><![CDATA[https://example.com/static.jpg]]></StaticResource>
  </Companion>
  <Companion width=""300"" height=""250"">
    <IFrameResource><![CDATA[https://example.com/iframe.html]]></IFrameResource>
  </Companion>
  <Companion width=""300"" height=""250"">
    <HTMLResource><![CDATA[<div>end</div>]]></HTMLResource>
  </Companion>
</CompanionAds></Creative>
</Creatives></InLine></Ad></VAST>";

            var data = VASTParser.Parse(vast);
            Assert.NotNull(data);
            Assert.AreEqual(VASTParser.VastCompanionResourceType.Html, data.companionResourceType);
            StringAssert.Contains("end", data.companionResource);
        }

        [Test]
        public void Parse_IFramePreferredOverStatic()
        {
            const string vast = @"<VAST version=""3.0""><Ad><InLine><Creatives>
<Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles></Linear></Creative>
<Creative><CompanionAds>
  <Companion>
    <StaticResource creativeType=""image/png""><![CDATA[https://example.com/static.png]]></StaticResource>
  </Companion>
  <Companion>
    <IFrameResource><![CDATA[https://example.com/iframe.html]]></IFrameResource>
  </Companion>
</CompanionAds></Creative>
</Creatives></InLine></Ad></VAST>";

            var data = VASTParser.Parse(vast);
            Assert.AreEqual(VASTParser.VastCompanionResourceType.IFrame, data.companionResourceType);
            Assert.AreEqual("https://example.com/iframe.html", data.companionResource);
        }

        [Test]
        public void Parse_NoCompanion_HasCompanionFalse()
        {
            const string vast = @"<VAST version=""3.0""><Ad><InLine><Creatives>
<Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles></Linear></Creative>
</Creatives></InLine></Ad></VAST>";

            var data = VASTParser.Parse(vast);
            Assert.NotNull(data);
            Assert.IsFalse(data.HasCompanion);
            Assert.AreEqual(VASTParser.VastCompanionResourceType.None, data.companionResourceType);
        }
    }
}
