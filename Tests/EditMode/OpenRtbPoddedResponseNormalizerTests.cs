using System.Collections.Generic;
using NUnit.Framework;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class OpenRtbPoddedResponseNormalizerTests
    {
        const string Vast1 = "<VAST version=\"3.0\"><Ad><InLine><Creatives><Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/1.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";
        const string Vast2 = "<VAST version=\"3.0\"><Ad><InLine><Creatives><Creative><Linear><MediaFiles><MediaFile><![CDATA[https://example.com/2.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";

        [Test]
        public void Normalize_BidsArray_SortsBySlotInPod()
        {
            var json = @"{
  ""openrtb"": { ""video"": { ""podid"": ""pod-1"", ""poddur"": 60, ""rqddurs"": [15, 30], ""maxseq"": 3 } },
  ""bids"": [
    { ""adm"": """ + JsonEscape(Vast2) + @""", ""slotinpod"": 2, ""duration"": 30 },
    { ""adm"": """ + JsonEscape(Vast1) + @""", ""slotinpod"": 1, ""duration"": 15 }
  ]
}";
            Assert.IsTrue(OpenRtbJson.TryParseObject(json, out var root));
            var response = OpenRtbPoddedResponseNormalizer.Normalize(root);
            Assert.NotNull(response);
            Assert.AreEqual(2, response.Markups.Count);
            Assert.IsNotNull(response.Markups.Find(m => m.SlotInPod == 1));
            Assert.IsNotNull(response.Markups.Find(m => m.SlotInPod == 2));
        }

        [Test]
        public void Normalize_SeatBidExtFields()
        {
            var json = @"{
  ""seatbid"": [{ ""bid"": [{
    ""id"": ""bid-1"",
    ""adm"": """ + JsonEscape(Vast1) + @""",
    ""crid"": ""creative-1"",
    ""price"": 1.2,
    ""ext"": { ""slotinpod"": 1, ""duration"": 15, ""podid"": ""pod-1"" }
  }]}],
  ""openrtb"": { ""video"": { ""podid"": ""pod-1"", ""rqdDurs"": [15] } }
}";
            Assert.IsTrue(OpenRtbJson.TryParseObject(json, out var root));
            var response = OpenRtbPoddedResponseNormalizer.Normalize(root);
            Assert.NotNull(response);
            Assert.AreEqual(1, response.Markups.Count);
            Assert.AreEqual(1, response.Markups[0].SlotInPod);
            Assert.AreEqual(15, response.Markups[0].DurationSeconds);
            Assert.AreEqual("pod-1", response.Markups[0].PodId);
            Assert.AreEqual("creative-1", response.Markups[0].Crid);
        }

        static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Test]
        public void Normalize_MultiplePodGroups_SelectsFirstSortedPodId()
        {
            var json = @"{
  ""seatbid"": [{ ""bid"": [
    { ""id"": ""b-z"", ""adm"": """ + JsonEscape(Vast2) + @""", ""ext"": { ""podid"": ""pod-z"", ""slotinpod"": 1 } },
    { ""id"": ""b-a"", ""adm"": """ + JsonEscape(Vast1) + @""", ""ext"": { ""podid"": ""pod-a"", ""slotinpod"": 1 } }
  ]}],
  ""openrtb"": { ""video"": { ""podid"": ""pod-a"" } }
}";
            Assert.IsTrue(OpenRtbJson.TryParseObject(json, out var root));
            var response = OpenRtbPoddedResponseNormalizer.Normalize(root);
            Assert.NotNull(response);
            Assert.AreEqual(1, response.Markups.Count);
            StringAssert.Contains("1.mp4", response.Markups[0].Adm);
            Assert.AreEqual("pod-a", response.Markups[0].PodId);
        }
    }
}
