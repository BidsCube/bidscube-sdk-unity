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
        /// <param name="baseURL">Base URL</param>
        /// <param name="placementId">Placement ID</param>
        /// <param name="adType">Ad type</param>
        /// <param name="position">Ad position</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="debug">Debug mode</param>
        /// <param name="ctaText">CTA text (optional)</param>
        /// <returns>Built URL</returns>
        public static string BuildAdRequestURL(string baseURL, string placementId, AdType adType,
            AdPosition position, int timeoutMs, bool debug, string ctaText = null)
        {
            // Use iOS SDK format: https://ssp-bcc-ads.com?placementId=20212&c=b&m=api&res=js&app=1&bundle=...&name=...&app_store_url=...&language=...&deviceWidth=...&deviceHeight=...&ua=...&ifa=...&dnt=1&gdpr=1&gdpr_consent=0&us_privacy=1---&ccpa=0&coppa=0
            var url = new StringBuilder();
            url.Append(baseURL.TrimEnd('/'));
            url.Append($"?placementId={placementId}");
            url.Append($"&c={GetContentType(adType)}");
            url.Append($"&m=api");
            url.Append($"&res=js");
            url.Append($"&app=1");
            url.Append($"&bundle={Uri.EscapeDataString(DeviceInfo.BundleId)}");
            url.Append($"&name={Uri.EscapeDataString(DeviceInfo.AppName)}");
            url.Append($"&app_store_url={Uri.EscapeDataString(DeviceInfo.AppStoreURL)}");
            url.Append($"&language={Uri.EscapeDataString(DeviceInfo.Language)}");
            url.Append($"&deviceWidth={DeviceInfo.DeviceWidth}");
            url.Append($"&deviceHeight={DeviceInfo.DeviceHeight}");
            url.Append($"&ua={Uri.EscapeDataString(DeviceInfo.UserAgent)}");
            url.Append($"&ifa={Uri.EscapeDataString(DeviceInfo.AdvertisingIdentifier)}");
            url.Append($"&dnt={DeviceInfo.DoNotTrack}");
            url.Append($"&gdpr={DeviceInfo.GDPR}");
            url.Append($"&gdpr_consent={DeviceInfo.GDPRConsent}");
            url.Append($"&us_privacy={DeviceInfo.USPrivacy}");
            url.Append($"&ccpa={DeviceInfo.CCPA}");
            url.Append($"&coppa={DeviceInfo.COPPA}");

            Logger.Info($"Built URL: {url.ToString()}");
            return url.ToString();
        }

        /// <summary>
        /// Get content type for ad type (matches iOS SDK)
        /// </summary>
        /// <param name="adType">Ad type</param>
        /// <returns>Content type string</returns>
        private static string GetContentType(AdType adType)
        {
            switch (adType)
            {
                case AdType.Image:
                    return "b"; // banner
                case AdType.Video:
                    return "v"; // video
                case AdType.Native:
                    return "n"; // native
                default:
                    return "b";
            }
        }
    }
}