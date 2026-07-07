using System;
using System.Collections.Generic;
using System.Linq;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB
{
    internal static class PoddedPlaybackPlanBuilder
    {
        internal static VideoPlaybackPlan Build(OpenRtbPoddedResponse response, SDKConfig config)
        {
            if (response == null || response.Markups == null || response.Markups.Count == 0)
                return null;

            config = config ?? CreateDefaultConfig();
            var strict = config.VideoPodDurationValidationMode == OpenRtbPodDurationValidationMode.Strict;

            try
            {
                var expanded = ExpandMarkups(response.Markups);
                if (expanded.Count == 0)
                    return null;

                var podType = DetectPodType(response.PodContext, expanded);
                if (response.PodContext != null)
                    response.PodContext.Type = podType;

                var ordered = SortMarkups(expanded, podType);
                var slots = BuildSlots(ordered, response.PodContext, podType, strict, config);

                if (slots.Count == 0)
                {
                    if (strict)
                        Logger.Info("[PoddedPlaybackPlanBuilder] Strict mode: no playable slots after validation.");
                    return null;
                }

                return new VideoPlaybackPlan
                {
                    PodContext = response.PodContext,
                    Slots = slots
                };
            }
            catch (Exception e)
            {
                Logger.Info($"[PoddedPlaybackPlanBuilder] Build failed: {e.Message}");
                return null;
            }
        }

        static SDKConfig CreateDefaultConfig()
        {
            return new SDKConfig.Builder().Build();
        }

        static List<ExpandedMarkup> ExpandMarkups(List<OpenRtbAdMarkup> markups)
        {
            var result = new List<ExpandedMarkup>();
            int responseOrder = 0;

            foreach (var markup in markups)
            {
                if (markup == null || string.IsNullOrWhiteSpace(markup.Adm))
                    continue;

                var adm = markup.Adm.Trim();
                if (VastAdSequenceParser.ContentLikelyContainsVast(adm))
                {
                    var docs = VastAdSequenceParser.ExtractAdDocuments(adm);
                    if (docs.Count > 1)
                    {
                        for (int i = 0; i < docs.Count; i++)
                        {
                            var doc = docs[i];
                            result.Add(new ExpandedMarkup
                            {
                                Source = markup,
                                Adm = doc,
                                VastXml = doc,
                                SlotInPod = null,
                                DurationSeconds = VastAdSequenceParser.FirstLinearDurationSeconds(doc) ?? markup.DurationSeconds,
                                VastSequence = VastAdSequenceParser.FirstAdSequence(doc),
                                ResponseOrder = responseOrder++
                            });
                        }
                        continue;
                    }
                }

                result.Add(new ExpandedMarkup
                {
                    Source = markup,
                    Adm = adm,
                    VastXml = VastAdSequenceParser.ContentLikelyContainsVast(adm) ? adm : null,
                    SlotInPod = markup.SlotInPod,
                    DurationSeconds = markup.DurationSeconds,
                    VastSequence = markup.VastSequence ?? (VastAdSequenceParser.ContentLikelyContainsVast(adm)
                        ? VastAdSequenceParser.FirstAdSequence(adm)
                        : null),
                    ResponseOrder = responseOrder++
                });

                if (IsLikelyUrl(adm) && !VastAdSequenceParser.ContentLikelyContainsVast(adm))
                {
                    var last = result[result.Count - 1];
                    OpenRtbVideoUrlHelper.AssignHttpAdmFields(adm, out last.VastAdTagUrl, out last.DirectVideoUrl);
                }
            }

            return result;
        }

        static OpenRtbPodType DetectPodType(OpenRtbPodContext ctx, List<ExpandedMarkup> markups)
        {
            if (markups.Count <= 1)
                return OpenRtbPodType.Single;

            bool hasSlotInPod = markups.Any(m => m.SlotInPod.HasValue);
            bool hasRqddurs = ctx?.RqddursSeconds != null && ctx.RqddursSeconds.Count > 0;
            bool hasPodDur = ctx?.PodDurSeconds.HasValue == true && ctx.PodDurSeconds.Value > 0;

            if (hasSlotInPod && (hasRqddurs || hasPodDur))
                return OpenRtbPodType.Hybrid;
            if (hasSlotInPod || hasRqddurs)
                return OpenRtbPodType.Structured;
            if (hasPodDur)
                return OpenRtbPodType.Dynamic;
            return OpenRtbPodType.Unknown;
        }

        static List<ExpandedMarkup> SortMarkups(List<ExpandedMarkup> markups, OpenRtbPodType podType)
        {
            return markups
                .OrderBy(m => m.SlotInPod ?? int.MaxValue)
                .ThenBy(m => m.VastSequence ?? int.MaxValue)
                .ThenBy(m => m.ResponseOrder)
                .ToList();
        }

        static List<VideoPlaybackSlot> BuildSlots(
            List<ExpandedMarkup> ordered,
            OpenRtbPodContext ctx,
            OpenRtbPodType podType,
            bool strict,
            SDKConfig config)
        {
            var slots = new List<VideoPlaybackSlot>();
            int budget = ctx?.PodDurSeconds ?? int.MaxValue;
            int usedBudget = 0;
            var rqddurs = ctx?.RqddursSeconds ?? new List<int>();
            int rqIndex = 0;

            var fixedSlots = podType == OpenRtbPodType.Hybrid
                ? ordered.Where(m => m.SlotInPod.HasValue).ToList()
                : ordered;

            var dynamicSlots = podType == OpenRtbPodType.Hybrid
                ? ordered.Where(m => !m.SlotInPod.HasValue).ToList()
                : new List<ExpandedMarkup>();

            IEnumerable<ExpandedMarkup> sequence;
            if (podType == OpenRtbPodType.Hybrid)
                sequence = fixedSlots.Concat(dynamicSlots);
            else
                sequence = ordered;

            int slotIndex = 0;
            foreach (var markup in sequence)
            {
                int duration = markup.DurationSeconds ?? 0;

                if (podType == OpenRtbPodType.Dynamic || (podType == OpenRtbPodType.Hybrid && !markup.SlotInPod.HasValue))
                {
                    if (duration > 0 && duration > budget - usedBudget)
                    {
                        Logger.Info($"[PoddedPlaybackPlanBuilder] Skipping slot exceeding poddur budget ({duration}s).");
                        continue;
                    }
                }

                if (podType == OpenRtbPodType.Structured && rqddurs.Count > 0)
                {
                    if (markup.SlotInPod.HasValue)
                    {
                        int expectedIndex = markup.SlotInPod.Value - 1;
                        if (expectedIndex >= 0 && expectedIndex < rqddurs.Count)
                        {
                            int expectedDur = rqddurs[expectedIndex];
                            if (duration > 0 && duration != expectedDur && strict)
                            {
                                Logger.Info("[PoddedPlaybackPlanBuilder] Strict: duration mismatch with rqddurs.");
                                return new List<VideoPlaybackSlot>();
                            }
                            if (duration > 0 && duration != expectedDur)
                                Logger.Info("[PoddedPlaybackPlanBuilder] Lenient: duration mismatch with rqddurs.");
                        }
                    }
                    else if (rqIndex < rqddurs.Count && duration > 0 && duration != rqddurs[rqIndex] && strict)
                    {
                        Logger.Info("[PoddedPlaybackPlanBuilder] Strict: rqddurs slot duration mismatch.");
                        return new List<VideoPlaybackSlot>();
                    }
                    rqIndex++;
                }

                if (ctx?.MaxSeq.HasValue == true && slots.Count >= ctx.MaxSeq.Value)
                    break;

                if (duration > 0)
                    usedBudget += duration;

                if (podType == OpenRtbPodType.Dynamic && usedBudget > budget)
                    break;

                slots.Add(new VideoPlaybackSlot
                {
                    Adm = markup.Adm,
                    VastXml = markup.VastXml,
                    VastAdTagUrl = markup.VastAdTagUrl,
                    DirectVideoUrl = markup.DirectVideoUrl,
                    SlotIndex = slotIndex++,
                    SlotInPod = markup.SlotInPod,
                    DurationSeconds = markup.DurationSeconds
                });
            }

            if (slots.Count == 0 && !strict && ordered.Count > 0)
            {
                Logger.Info("[PoddedPlaybackPlanBuilder] Lenient fallback: using response order slots.");
                for (int i = 0; i < ordered.Count; i++)
                {
                    var m = ordered[i];
                    slots.Add(new VideoPlaybackSlot
                    {
                        Adm = m.Adm,
                        VastXml = m.VastXml,
                        VastAdTagUrl = m.VastAdTagUrl,
                        DirectVideoUrl = m.DirectVideoUrl,
                        SlotIndex = i,
                        SlotInPod = m.SlotInPod,
                        DurationSeconds = m.DurationSeconds
                    });
                }
            }

            return slots;
        }

        static bool IsLikelyUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        sealed class ExpandedMarkup
        {
            public OpenRtbAdMarkup Source;
            public string Adm;
            public string VastXml;
            public string VastAdTagUrl;
            public string DirectVideoUrl;
            public int? SlotInPod;
            public int? DurationSeconds;
            public int? VastSequence;
            public int ResponseOrder;
        }
    }
}
