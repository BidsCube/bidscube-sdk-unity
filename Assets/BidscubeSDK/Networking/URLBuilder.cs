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
        /// Build ad request URL
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
            var url = new StringBuilder();
            url.Append(baseURL.TrimEnd('/'));
            url.Append($"/{adType.GetPathSegment()}");
            url.Append($"/{placementId}");

            var queryParams = new StringBuilder();
            queryParams.Append($"?position={(int)position}");
            queryParams.Append($"&timeout={timeoutMs}");
            queryParams.Append($"&debug={(debug ? 1 : 0)}");

            // Add device info
            var deviceInfo = DeviceInfo.GetDeviceInfo();
            foreach (var kvp in deviceInfo)
            {
                queryParams.Append($"&{kvp.Key}={Uri.EscapeDataString(kvp.Value.ToString())}");
            }

            // Add CTA text if provided
            if (!string.IsNullOrEmpty(ctaText))
            {
                queryParams.Append($"&cta={Uri.EscapeDataString(ctaText)}");
            }

            url.Append(queryParams);
            return url.ToString();
        }
    }
}