using System.Collections.Generic;

namespace BidscubeSDK.OpenRTB
{
    internal enum OpenRtbPodType
    {
        Single,
        Structured,
        Dynamic,
        Hybrid,
        Unknown
    }

    internal sealed class OpenRtbPodContext
    {
        public string PodId;
        public int? PodSeq;
        public int? PodDurSeconds;
        public List<int> RqddursSeconds = new List<int>();
        public int? MaxSeq;
        public double? MinCpmPerSec;
        public OpenRtbPodType Type;
    }

    internal sealed class OpenRtbAdMarkup
    {
        public string Adm;
        public string AdId;
        public string Crid;
        public double? Price;
        public string PodId;
        public int? PodSeq;
        public int? SlotInPod;
        public int? DurationSeconds;
        public int? VastSequence;
        public Dictionary<string, object> RawBid;
    }

    internal sealed class OpenRtbPoddedResponse
    {
        public OpenRtbPodContext PodContext;
        public List<OpenRtbAdMarkup> Markups = new List<OpenRtbAdMarkup>();
    }

    internal sealed class VideoPlaybackSlot
    {
        public string Adm;
        public string VastXml;
        /// <summary>HTTP(S) URL that returns VAST XML or JSON — fetch before play.</summary>
        public string VastAdTagUrl;
        /// <summary>Direct progressive/streaming media URL for VideoPlayer.</summary>
        public string DirectVideoUrl;
        public int SlotIndex;
        public int? SlotInPod;
        public int? DurationSeconds;
    }

    internal sealed class VideoPlaybackPlan
    {
        public OpenRtbPodContext PodContext;
        public List<VideoPlaybackSlot> Slots = new List<VideoPlaybackSlot>();

        public bool IsPlayable => Slots != null && Slots.Count > 0;
    }

    internal sealed class ResolvedVideoAdPayload
    {
        public VideoPlaybackPlan PlaybackPlan;
        public string VastXml;
        public string VastAdTagUrl;
        public string DirectVideoUrl;
        public AdPosition Position;
    }
}
