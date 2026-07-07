using System;

namespace BidscubeSDK.OpenRTB
{
    internal static class OpenRtbVideoUrlHelper
    {
        internal const int MaxVastAdTagUrlRedirectDepth = 5;

        internal static bool IsVastAdTagUrlRedirectDepthExceeded(int depth) => depth > MaxVastAdTagUrlRedirectDepth;

        internal static bool IsLikelyDirectVideoUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var lower = url.ToLowerInvariant();
            return lower.Contains(".mp4")
                   || lower.Contains(".webm")
                   || lower.Contains(".mov")
                   || lower.Contains(".m3u8")
                   || lower.Contains(".mpd");
        }

        internal static bool IsHttpUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        internal static void AssignHttpAdmFields(string url, out string vastAdTagUrl, out string directVideoUrl)
        {
            vastAdTagUrl = null;
            directVideoUrl = null;
            if (!IsHttpUrl(url))
                return;

            if (IsLikelyDirectVideoUrl(url))
                directVideoUrl = url;
            else
                vastAdTagUrl = url;
        }
    }
}
