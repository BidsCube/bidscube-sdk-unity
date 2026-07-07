using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class VideoAdPayloadResolverTests
    {
        const string Vast = "<VAST version=\"3.0\"><Ad><InLine><Creatives><Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/v.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";
        const string Vast1 = "<VAST version=\"3.0\"><Ad sequence=\"1\"><InLine><Creatives><Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/1.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";
        const string Vast2 = "<VAST version=\"3.0\"><Ad sequence=\"2\"><InLine><Creatives><Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/2.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";

        static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Test]
        public void Resolve_RawVast_ReturnsPlayablePlan()
        {
            var resolved = VideoAdPayloadResolver.Resolve(Vast, new SDKConfig.Builder().Build());
            Assert.NotNull(resolved);
            Assert.IsTrue(resolved.PlaybackPlan.IsPlayable);
        }

        [Test]
        public void Resolve_RootJsonAdm_ReturnsPlayablePlan()
        {
            var json = "{\"adm\":\"" + JsonEscape(Vast) + "\"}";
            var resolved = VideoAdPayloadResolver.Resolve(json, new SDKConfig.Builder().Build());
            Assert.NotNull(resolved);
            Assert.IsTrue(resolved.PlaybackPlan.IsPlayable);
        }

        [Test]
        public void Resolve_OpenRtbBidsPod_ReturnsMultipleSlots()
        {
            var json = @"{
  ""openrtb"": { ""video"": { ""podid"": ""p1"", ""rqddurs"": [15, 30] } },
  ""bids"": [
    { ""adm"": """ + JsonEscape(Vast1) + @""", ""slotinpod"": 1, ""duration"": 15 },
    { ""adm"": """ + JsonEscape(Vast2) + @""", ""slotinpod"": 2, ""duration"": 30 }
  ]
}";
            var resolved = VideoAdPayloadResolver.Resolve(json, new SDKConfig.Builder().Build());
            Assert.NotNull(resolved);
            Assert.AreEqual(2, resolved.PlaybackPlan.Slots.Count);
        }

        [Test]
        public void Resolve_OpenRtbMetadataDisabled_StillResolvesLegacyAdm()
        {
            var json = "{\"adm\":\"" + JsonEscape(Vast) + "\"}";
            var config = new SDKConfig.Builder().OpenRtbPodMetadataEnabled(false).Build();
            var resolved = VideoAdPayloadResolver.Resolve(json, config);
            Assert.NotNull(resolved);
            Assert.IsTrue(resolved.PlaybackPlan.IsPlayable);
        }

        [Test]
        public void Resolve_OpenRtbMetadataDisabled_SkipsBidsArrayPod()
        {
            var json = @"{
  ""openrtb"": { ""video"": { ""podid"": ""p1"" } },
  ""bids"": [
    { ""adm"": """ + JsonEscape(Vast1) + @""", ""slotinpod"": 1 }
  ]
}";
            var config = new SDKConfig.Builder().OpenRtbPodMetadataEnabled(false).Build();
            var resolved = VideoAdPayloadResolver.Resolve(json, config);
            Assert.IsNull(resolved);
        }

        [Test]
        public void Resolve_DirectMp4Url_UsesDirectVideoUrl()
        {
            var resolved = VideoAdPayloadResolver.Resolve("https://example.com/video.mp4", new SDKConfig.Builder().Build());
            Assert.NotNull(resolved);
            Assert.AreEqual(1, resolved.PlaybackPlan.Slots.Count);
            Assert.AreEqual("https://example.com/video.mp4", resolved.PlaybackPlan.Slots[0].DirectVideoUrl);
            Assert.IsNull(resolved.PlaybackPlan.Slots[0].VastAdTagUrl);
        }

        [Test]
        public void Resolve_VastAdTagUrl_UsesVastAdTagUrlNotDirectVideo()
        {
            var url = "https://example.com/vast?type=wrapper";
            var resolved = VideoAdPayloadResolver.Resolve(url, new SDKConfig.Builder().Build());
            Assert.NotNull(resolved);
            Assert.AreEqual(url, resolved.PlaybackPlan.Slots[0].VastAdTagUrl);
            Assert.IsNull(resolved.PlaybackPlan.Slots[0].DirectVideoUrl);
        }

        [Test]
        public void Resolve_RqdDursFallback_ThroughResolverPath()
        {
            var json = @"{
  ""openrtb"": { ""video"": { ""podid"": ""p1"", ""rqdDurs"": [15, 30] } },
  ""bids"": [
    { ""adm"": """ + JsonEscape(Vast1) + @""", ""slotinpod"": 1, ""duration"": 15 },
    { ""adm"": """ + JsonEscape(Vast2) + @""", ""slotinpod"": 2, ""duration"": 30 }
  ]
}";
            var resolved = VideoAdPayloadResolver.Resolve(json, new SDKConfig.Builder().Build());
            Assert.NotNull(resolved);
            Assert.AreEqual(2, resolved.PlaybackPlan.Slots.Count);
            Assert.AreEqual(15, resolved.PlaybackPlan.PodContext.RqddursSeconds[0]);
            Assert.AreEqual(30, resolved.PlaybackPlan.PodContext.RqddursSeconds[1]);
        }

        [Test]
        public void Resolve_MalformedJson_FallsBackOrReturnsNull_Safely()
        {
            Assert.DoesNotThrow(() => VideoAdPayloadResolver.Resolve("{not json", new SDKConfig.Builder().Build()));
        }
    }
}
