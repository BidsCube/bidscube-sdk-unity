using System;
using System.Collections.Generic;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB
{
    internal static class VideoAdPayloadResolver
    {
        internal static ResolvedVideoAdPayload Resolve(string content, SDKConfig config)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            config = config ?? new SDKConfig.Builder().Build();
            var trimmed = content.TrimStart();

            if (trimmed.StartsWith("{"))
                return ResolveJson(trimmed, config);

            if (VastAdSequenceParser.ContentLikelyContainsVast(content))
                return BuildVastPayload(content, config);

            if (IsLikelyUrl(trimmed))
                return BuildDirectUrlPayload(trimmed);

            return null;
        }

        static ResolvedVideoAdPayload ResolveJson(string json, SDKConfig config)
        {
            if (!OpenRtbJson.TryParseObject(json, out var root))
                return TryLegacyRootAdm(json, config);

            var position = ExtractPosition(root);

            if (config.OpenRtbPodMetadataEnabled)
            {
                var normalized = OpenRtbPoddedResponseNormalizer.Normalize(root);
                if (normalized != null && normalized.Markups.Count > 0)
                {
                    var plan = PoddedPlaybackPlanBuilder.Build(normalized, config);
                    if (plan != null && plan.IsPlayable)
                    {
                        return new ResolvedVideoAdPayload
                        {
                            PlaybackPlan = plan,
                            Position = position
                        };
                    }
                }
            }

            return TryLegacyRootAdm(root, json, config, position);
        }

        static ResolvedVideoAdPayload TryLegacyRootAdm(string json, SDKConfig config)
        {
            if (OpenRtbJson.TryParseObject(json, out var root))
                return TryLegacyRootAdm(root, json, config, ExtractPosition(root));
            return TryLegacyAdmString(json, config);
        }

        static ResolvedVideoAdPayload TryLegacyRootAdm(
            Dictionary<string, object> root,
            string rawJson,
            SDKConfig config,
            AdPosition position)
        {
            var adm = OpenRtbVideoObjectParser.StringValue(OpenRtbVideoObjectParser.GetIgnoreCase(root, "adm"));
            if (string.IsNullOrEmpty(adm))
            {
                if (AdMarkupExtractor.TryExtractMarkup(rawJson, out var extracted, out _, out _))
                    adm = extracted;
            }

            if (string.IsNullOrEmpty(adm))
                return null;

            return BuildPayloadFromAdm(adm, config, position);
        }

        static ResolvedVideoAdPayload TryLegacyAdmString(string adm, SDKConfig config)
        {
            if (string.IsNullOrEmpty(adm))
                return null;
            return BuildPayloadFromAdm(adm, config, AdPosition.Unknown);
        }

        static ResolvedVideoAdPayload BuildPayloadFromAdm(string adm, SDKConfig config, AdPosition position)
        {
            adm = UnescapeAdm(adm);
            if (VastAdSequenceParser.ContentLikelyContainsVast(adm))
            {
                var payload = BuildVastPayload(adm, config);
                if (payload != null)
                    payload.Position = position;
                return payload;
            }

            if (IsLikelyUrl(adm))
            {
                OpenRtbVideoUrlHelper.AssignHttpAdmFields(adm, out var vastAdTagUrl, out var directVideoUrl);
                return new ResolvedVideoAdPayload
                {
                    PlaybackPlan = SingleSlotPlan(adm, null, vastAdTagUrl, directVideoUrl),
                    VastAdTagUrl = vastAdTagUrl,
                    DirectVideoUrl = directVideoUrl,
                    Position = position
                };
            }

            return new ResolvedVideoAdPayload
            {
                PlaybackPlan = SingleSlotPlan(adm, null, null, null),
                VastXml = adm,
                Position = position
            };
        }

        static ResolvedVideoAdPayload BuildVastPayload(string vastXml, SDKConfig config)
        {
            var docs = VastAdSequenceParser.ExtractAdDocuments(vastXml);
            if (docs.Count == 0)
                return null;

            if (docs.Count == 1)
            {
                return new ResolvedVideoAdPayload
                {
                    PlaybackPlan = SingleSlotPlan(docs[0], docs[0], null, null),
                    VastXml = docs[0]
                };
            }

            var response = new OpenRtbPoddedResponse
            {
                PodContext = new OpenRtbPodContext { Type = OpenRtbPodType.Single }
            };
            foreach (var doc in docs)
            {
                response.Markups.Add(new OpenRtbAdMarkup
                {
                    Adm = doc,
                    VastSequence = VastAdSequenceParser.FirstAdSequence(doc),
                    DurationSeconds = VastAdSequenceParser.FirstLinearDurationSeconds(doc)
                });
            }

            var plan = PoddedPlaybackPlanBuilder.Build(response, config);
            if (plan == null || !plan.IsPlayable)
                return null;

            return new ResolvedVideoAdPayload
            {
                PlaybackPlan = plan,
                VastXml = vastXml
            };
        }

        static ResolvedVideoAdPayload BuildDirectUrlPayload(string url)
        {
            OpenRtbVideoUrlHelper.AssignHttpAdmFields(url, out var vastAdTagUrl, out var directVideoUrl);
            return new ResolvedVideoAdPayload
            {
                PlaybackPlan = SingleSlotPlan(url, null, vastAdTagUrl, directVideoUrl),
                VastAdTagUrl = vastAdTagUrl,
                DirectVideoUrl = directVideoUrl
            };
        }

        static VideoPlaybackPlan SingleSlotPlan(string adm, string vastXml, string vastAdTagUrl, string directVideoUrl)
        {
            return new VideoPlaybackPlan
            {
                Slots = new List<VideoPlaybackSlot>
                {
                    new VideoPlaybackSlot
                    {
                        Adm = adm,
                        VastXml = vastXml,
                        VastAdTagUrl = vastAdTagUrl,
                        DirectVideoUrl = directVideoUrl,
                        SlotIndex = 0
                    }
                }
            };
        }

        static AdPosition ExtractPosition(Dictionary<string, object> root)
        {
            var pos = OpenRtbVideoObjectParser.IntValue(OpenRtbVideoObjectParser.GetIgnoreCase(root, "position"));
            if (!pos.HasValue)
                return AdPosition.Unknown;
            if (Enum.IsDefined(typeof(AdPosition), pos.Value))
                return (AdPosition)pos.Value;
            return AdPosition.Unknown;
        }

        static string UnescapeAdm(string adm)
        {
            if (string.IsNullOrEmpty(adm))
                return adm;

            var content = adm.Trim();
            if ((content.StartsWith("\"") && content.EndsWith("\"")) ||
                (content.StartsWith("'") && content.EndsWith("'")))
            {
                content = content.Substring(1, content.Length - 2);
            }

            return content
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\/", "/");
        }

        static bool IsLikelyUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
