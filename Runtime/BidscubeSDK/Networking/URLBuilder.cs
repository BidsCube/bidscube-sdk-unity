using System;
using System.Text;

namespace BidscubeSDK
{
    /// <summary>
    /// URL builder for ad requests
    /// </summary>
    public static class URLBuilder
    {
        /// <summary>
        /// Build ad request URL - matches iOS SDK format
        /// </summary>
        public static string BuildAdRequestURL(string baseURL, string placementId, AdType adType,
            AdPosition position, int timeoutMs, bool debug, string ctaText = null, string userId = null)
        {
            return BuildAdRequestURL(
                baseURL,
                placementId,
                adType,
                position,
                timeoutMs,
                debug,
                DeviceSnapshot.FromDeviceInfo(),
                ctaText,
                userId);
        }

        /// <summary>
        /// Build ad request URL with an injected device snapshot (for tests).
        /// <paramref name="timeoutMs"/>, <paramref name="debug"/>, <paramref name="ctaText"/>, and
        /// <paramref name="position"/> are accepted for API compatibility and are not added to the query.
        /// </summary>
        public static string BuildAdRequestURL(
            string baseURL,
            string placementId,
            AdType adType,
            AdPosition position,
            int timeoutMs,
            bool debug,
            DeviceSnapshot device,
            string ctaText = null,
            string userId = null)
        {
            _ = position;
            _ = timeoutMs;
            _ = debug;
            _ = ctaText;

            var url = new StringBuilder();
            url.Append((baseURL ?? "").TrimEnd('/'));
            url.Append($"?placementId={placementId}");
            url.Append($"&c={GetContentType(adType)}");
            url.Append($"&m=api");
            url.Append($"&res={GetResponseResource(adType)}");
            url.Append($"&app=1");
            url.Append($"&bundle={Uri.EscapeDataString(device.BundleId)}");
            url.Append($"&name={Uri.EscapeDataString(device.AppName)}");
            url.Append($"&app_store_url={Uri.EscapeDataString(device.AppStoreURL)}");
            url.Append($"&language={Uri.EscapeDataString(device.Language)}");
            url.Append($"&deviceWidth={device.DeviceWidth}");
            url.Append($"&deviceHeight={device.DeviceHeight}");
            url.Append($"&ua={Uri.EscapeDataString(device.UserAgent)}");
            url.Append($"&ifa={Uri.EscapeDataString(device.AdvertisingIdentifier)}");
            url.Append($"&dnt={device.DoNotTrack}");
            if (!string.IsNullOrEmpty(device.GDPR))
                url.Append($"&gdpr={Uri.EscapeDataString(device.GDPR)}");
            if (!string.IsNullOrEmpty(device.GDPRConsent))
                url.Append($"&gdpr_consent={Uri.EscapeDataString(device.GDPRConsent)}");
            url.Append($"&us_privacy={device.USPrivacy}");
            url.Append($"&ccpa={device.CCPA}");
            url.Append($"&coppa={device.COPPA}");

            if (!string.IsNullOrWhiteSpace(userId))
                url.Append($"&user_id={Uri.EscapeDataString(userId.Trim())}");

            var built = url.ToString();
            Logger.Info("Built ad request placement=" + placementId
                + " gdpr=" + (string.IsNullOrEmpty(device.GDPR) ? "absent" : "set")
                + " consent=" + (string.IsNullOrEmpty(device.GDPRConsent) ? "absent" : "set"));
            return built;
        }

        internal static string GetContentType(AdType adType)
        {
            switch (adType)
            {
                case AdType.Image:
                    return "b";
                case AdType.Video:
                    return "v";
                case AdType.Native:
                    return "n";
                default:
                    return "b";
            }
        }

        internal static string GetResponseResource(AdType adType)
        {
            if (adType == AdType.Native || adType == AdType.Video)
                return "json";
            return "js";
        }
    }
}
