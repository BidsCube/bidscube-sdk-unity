using System;
using System.Collections.Generic;
using System.Linq;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB
{
    internal static class OpenRtbPoddedResponseNormalizer
    {
        internal static OpenRtbPoddedResponse Normalize(Dictionary<string, object> root)
        {
            if (root == null)
                return null;

            try
            {
                var response = new OpenRtbPoddedResponse
                {
                    PodContext = BuildPodContext(root)
                };

                CollectBids(root, response);

                if (response.Markups.Count == 0)
                    return null;

                return response;
            }
            catch (Exception e)
            {
                Logger.Info($"[OpenRtbPoddedResponseNormalizer] Normalize failed: {e.Message}");
                return null;
            }
        }

        static OpenRtbPodContext BuildPodContext(Dictionary<string, object> root)
        {
            var video = OpenRtbVideoObjectParser.FindVideoObject(root);
            var ctx = new OpenRtbPodContext();

            if (video != null)
            {
                ctx.PodId = OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(video, "podid"));
                ctx.PodSeq = OpenRtbVideoObjectParser.IntValue(OpenRtbVideoObjectParser.GetIgnoreCase(video, "podseq"));
                ctx.PodDurSeconds = OpenRtbVideoObjectParser.IntValue(OpenRtbVideoObjectParser.GetIgnoreCase(video, "poddur"));
                ctx.MaxSeq = OpenRtbVideoObjectParser.IntValue(OpenRtbVideoObjectParser.GetIgnoreCase(video, "maxseq"));
                ctx.MinCpmPerSec = OpenRtbVideoObjectParser.DoubleValue(OpenRtbVideoObjectParser.GetIgnoreCase(video, "mincpmpersec"));

                var rqddurs = OpenRtbVideoObjectParser.IntArrayValue(OpenRtbVideoObjectParser.GetIgnoreCase(video, "rqddurs"));
                if (rqddurs.Count == 0)
                    rqddurs = OpenRtbVideoObjectParser.IntArrayValue(OpenRtbVideoObjectParser.GetIgnoreCase(video, "rqdDurs"));
                ctx.RqddursSeconds = rqddurs;
            }

            return ctx;
        }

        static void CollectBids(Dictionary<string, object> root, OpenRtbPoddedResponse response)
        {
            var collected = new List<OpenRtbAdMarkup>();
            int order = 0;

            var seatbid = OpenRtbVideoObjectParser.ArrayValue(OpenRtbVideoObjectParser.GetIgnoreCase(root, "seatbid"));
            if (seatbid != null && seatbid.Count > 0)
            {
                var groups = new Dictionary<string, List<OpenRtbAdMarkup>>(StringComparer.OrdinalIgnoreCase);
                foreach (var seatObj in seatbid)
                {
                    var seat = OpenRtbVideoObjectParser.ObjectValue(seatObj);
                    if (seat == null)
                        continue;
                    var bidList = OpenRtbVideoObjectParser.ArrayValue(OpenRtbVideoObjectParser.GetIgnoreCase(seat, "bid"));
                    if (bidList == null)
                        continue;

                    foreach (var bidEntry in bidList)
                    {
                        var bidObj = OpenRtbVideoObjectParser.ObjectValue(bidEntry);
                        if (bidObj == null)
                            continue;
                        var adm = OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(bidObj, "adm"));
                        if (string.IsNullOrEmpty(adm))
                            continue;

                        var markup = BuildMarkupFromBid(bidObj, adm, response.PodContext, order++);
                        var podKey = markup.PodId ?? string.Empty;
                        if (!groups.TryGetValue(podKey, out var list))
                        {
                            list = new List<OpenRtbAdMarkup>();
                            groups[podKey] = list;
                        }
                        list.Add(markup);
                    }
                }

                if (groups.Count > 1)
                    Logger.Info("[OpenRtbPoddedResponseNormalizer] Multiple pod groups in seatbid; using first sorted podid.");

                if (groups.Count > 0)
                    collected.AddRange(groups.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).First().Value);
            }
            else
            {
                var bids = OpenRtbVideoObjectParser.ArrayValue(OpenRtbVideoObjectParser.GetIgnoreCase(root, "bids"));
                if (bids != null && bids.Count > 0)
                {
                    for (int i = 0; i < bids.Count; i++)
                    {
                        var bidObj = OpenRtbVideoObjectParser.ObjectValue(bids[i]);
                        if (bidObj == null)
                            continue;
                        var adm = OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(bidObj, "adm"));
                        if (string.IsNullOrEmpty(adm))
                            continue;
                        collected.Add(BuildMarkupFromBid(bidObj, adm, response.PodContext, order++));
                    }
                }
                else
                {
                    var rootAdm = OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(root, "adm"));
                    if (!string.IsNullOrEmpty(rootAdm))
                        collected.Add(BuildMarkupFromBid(root, rootAdm, response.PodContext, 0));
                }
            }

            response.Markups.AddRange(collected);
        }

        static OpenRtbAdMarkup BuildMarkupFromBid(
            Dictionary<string, object> bid,
            string adm,
            OpenRtbPodContext podContext,
            int orderIndex)
        {
            var ext = OpenRtbVideoObjectParser.ObjectValue(OpenRtbVideoObjectParser.GetIgnoreCase(bid, "ext"));

            var markup = new OpenRtbAdMarkup
            {
                Adm = adm,
                AdId = OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(bid, "id")),
                Crid = OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(bid, "crid")),
                Price = OpenRtbVideoObjectParser.DoubleValue(OpenRtbVideoObjectParser.GetIgnoreCase(bid, "price")),
                RawBid = bid
            };

            markup.SlotInPod = FirstInt(bid, ext, "slotinpod");
            markup.DurationSeconds = FirstInt(bid, ext, "duration");
            markup.PodId = FirstString(bid, ext, "podid") ?? podContext?.PodId;
            markup.PodSeq = FirstInt(bid, ext, "podseq") ?? podContext?.PodSeq;

            if (VastAdSequenceParser.ContentLikelyContainsVast(adm))
                markup.VastSequence = VastAdSequenceParser.FirstAdSequence(adm);

            if (!markup.DurationSeconds.HasValue && !string.IsNullOrEmpty(adm) &&
                VastAdSequenceParser.ContentLikelyContainsVast(adm))
            {
                markup.DurationSeconds = VastAdSequenceParser.FirstLinearDurationSeconds(adm);
            }

            return markup;
        }

        static int? FirstInt(Dictionary<string, object> bid, Dictionary<string, object> ext, string key)
        {
            var fromBid = OpenRtbVideoObjectParser.IntValue(OpenRtbVideoObjectParser.GetIgnoreCase(bid, key));
            if (fromBid.HasValue)
                return fromBid;
            if (ext != null)
                return OpenRtbVideoObjectParser.IntValue(OpenRtbVideoObjectParser.GetIgnoreCase(ext, key));
            return null;
        }

        static string FirstString(Dictionary<string, object> bid, Dictionary<string, object> ext, string key)
        {
            var fromBid = OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(bid, key));
            if (!string.IsNullOrEmpty(fromBid))
                return fromBid;
            if (ext != null)
                return OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(ext, key));
            return null;
        }
    }
}
