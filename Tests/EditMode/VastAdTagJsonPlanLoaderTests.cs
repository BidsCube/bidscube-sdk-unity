using System.Collections.Generic;
using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class VastAdTagJsonPlanLoaderTests
    {
        const string Vast1 = "<VAST version=\"3.0\"><Ad sequence=\"1\"><InLine><Creatives><Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/1.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";
        const string Vast2 = "<VAST version=\"3.0\"><Ad sequence=\"2\"><InLine><Creatives><Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/2.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";

        static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Test]
        public void GetNestedPlanLoadMode_MultiSlotJson_ReturnsFullPlan()
        {
            var json = @"{
  ""openrtb"": { ""video"": { ""podid"": ""p1"", ""rqddurs"": [15, 30] } },
  ""bids"": [
    { ""adm"": """ + JsonEscape(Vast1) + @""", ""slotinpod"": 1, ""duration"": 15 },
    { ""adm"": """ + JsonEscape(Vast2) + @""", ""slotinpod"": 2, ""duration"": 30 }
  ]
}";
            var resolved = VideoAdPayloadResolver.Resolve(json, new SDKConfig.Builder().Build());
            Assert.AreEqual(VastAdTagJsonPlanLoader.NestedPlanLoadMode.FullPlan,
                VastAdTagJsonPlanLoader.GetNestedPlanLoadMode(resolved));
            Assert.AreEqual(2, resolved.PlaybackPlan.Slots.Count);
        }

        [Test]
        public void GetNestedPlanLoadMode_SingleSlotJson_ReturnsSingleSlot()
        {
            var json = "{\"adm\":\"" + JsonEscape(Vast1) + "\"}";
            var resolved = VideoAdPayloadResolver.Resolve(json, new SDKConfig.Builder().Build());
            Assert.AreEqual(VastAdTagJsonPlanLoader.NestedPlanLoadMode.SingleSlot,
                VastAdTagJsonPlanLoader.GetNestedPlanLoadMode(resolved));
        }

        [Test]
        public void GetNestedPlanLoadMode_Unresolvable_ReturnsNone()
        {
            Assert.AreEqual(VastAdTagJsonPlanLoader.NestedPlanLoadMode.None,
                VastAdTagJsonPlanLoader.GetNestedPlanLoadMode(null));
        }
    }
}
