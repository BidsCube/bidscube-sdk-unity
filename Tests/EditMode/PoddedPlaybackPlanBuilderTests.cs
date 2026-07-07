using System.Collections.Generic;
using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class PoddedPlaybackPlanBuilderTests
    {
        static SDKConfig Config => new SDKConfig.Builder().Build();

        [Test]
        public void DynamicPod_SkipsSlotExceedingPodDur()
        {
            var response = new OpenRtbPoddedResponse
            {
                PodContext = new OpenRtbPodContext { PodDurSeconds = 20, Type = OpenRtbPodType.Dynamic },
                Markups = new List<OpenRtbAdMarkup>
                {
                    new OpenRtbAdMarkup { Adm = "https://example.com/short.mp4", DurationSeconds = 10 },
                    new OpenRtbAdMarkup { Adm = "https://example.com/long.mp4", DurationSeconds = 30 }
                }
            };

            var plan = PoddedPlaybackPlanBuilder.Build(response, Config);
            Assert.NotNull(plan);
            Assert.AreEqual(1, plan.Slots.Count);
            StringAssert.Contains("short", plan.Slots[0].Adm);
        }

        [Test]
        public void MultiAdVast_ExpandsWithoutCopyingRootSlotInPod()
        {
            const string multi = @"<VAST version=""3.0"">
<Ad sequence=""1""><InLine><Creatives><Creative><Linear><Duration>00:00:10</Duration><MediaFiles><MediaFile><![CDATA[https://example.com/a.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad>
<Ad sequence=""2""><InLine><Creatives><Creative><Linear><Duration>00:00:20</Duration><MediaFiles><MediaFile><![CDATA[https://example.com/b.mp4]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad>
</VAST>";

            var response = new OpenRtbPoddedResponse
            {
                PodContext = new OpenRtbPodContext(),
                Markups = new List<OpenRtbAdMarkup>
                {
                    new OpenRtbAdMarkup { Adm = multi, SlotInPod = 5 }
                }
            };

            var plan = PoddedPlaybackPlanBuilder.Build(response, Config);
            Assert.AreEqual(2, plan.Slots.Count);
            Assert.IsNull(plan.Slots[0].SlotInPod);
            Assert.IsNull(plan.Slots[1].SlotInPod);
        }

        [Test]
        public void HybridPod_KeepsFixedSlotsFirst()
        {
            var response = new OpenRtbPoddedResponse
            {
                PodContext = new OpenRtbPodContext
                {
                    PodDurSeconds = 60,
                    RqddursSeconds = new List<int> { 15 },
                    Type = OpenRtbPodType.Hybrid
                },
                Markups = new List<OpenRtbAdMarkup>
                {
                    new OpenRtbAdMarkup { Adm = "https://example.com/dynamic.mp4", DurationSeconds = 20 },
                    new OpenRtbAdMarkup { Adm = "https://example.com/fixed.mp4", SlotInPod = 1, DurationSeconds = 15 }
                }
            };

            var plan = PoddedPlaybackPlanBuilder.Build(response, Config);
            Assert.GreaterOrEqual(plan.Slots.Count, 1);
            StringAssert.Contains("fixed", plan.Slots[0].Adm);
        }

        [Test]
        public void HybridPod_StrictMode_FixedSlotsExceedPodDur_ReturnsNull()
        {
            var response = new OpenRtbPoddedResponse
            {
                PodContext = new OpenRtbPodContext
                {
                    PodDurSeconds = 20,
                    RqddursSeconds = new List<int> { 15 },
                    Type = OpenRtbPodType.Hybrid
                },
                Markups = new List<OpenRtbAdMarkup>
                {
                    new OpenRtbAdMarkup { Adm = "https://example.com/fixed1.mp4", SlotInPod = 1, DurationSeconds = 15 },
                    new OpenRtbAdMarkup { Adm = "https://example.com/fixed2.mp4", SlotInPod = 2, DurationSeconds = 15 }
                }
            };

            var strictConfig = new SDKConfig.Builder()
                .VideoPodDurationValidationMode(OpenRtbPodDurationValidationMode.Strict)
                .Build();

            var plan = PoddedPlaybackPlanBuilder.Build(response, strictConfig);
            Assert.IsNull(plan);
        }

        [Test]
        public void StructuredPod_SortsBySlotInPod()
        {
            var response = new OpenRtbPoddedResponse
            {
                PodContext = new OpenRtbPodContext
                {
                    RqddursSeconds = new List<int> { 15, 30 },
                    Type = OpenRtbPodType.Structured
                },
                Markups = new List<OpenRtbAdMarkup>
                {
                    new OpenRtbAdMarkup { Adm = "https://example.com/2.mp4", SlotInPod = 2, DurationSeconds = 30 },
                    new OpenRtbAdMarkup { Adm = "https://example.com/1.mp4", SlotInPod = 1, DurationSeconds = 15 }
                }
            };

            var plan = PoddedPlaybackPlanBuilder.Build(response, Config);
            Assert.AreEqual(2, plan.Slots.Count);
            StringAssert.Contains("1.mp4", plan.Slots[0].Adm);
            StringAssert.Contains("2.mp4", plan.Slots[1].Adm);
        }
    }
}
