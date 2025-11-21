namespace BidscubeSDK
{
    /// <summary>
    /// Ad type enumeration
    /// </summary>
    public enum AdType
    {
        Image,
        Video,
        Native
    }

    /// <summary>
    /// Ad type extensions
    /// </summary>
    public static class AdTypeExtensions
    {
        /// <summary>
        /// Get path segment for ad type
        /// </summary>
        /// <param name="adType">Ad type</param>
        /// <returns>Path segment string</returns>
        public static string GetPathSegment(this AdType adType)
        {
            switch (adType)
            {
                case AdType.Image:
                    return "image";
                case AdType.Video:
                    return "video";
                case AdType.Native:
                    return "native";
                default:
                    return "unknown";
            }
        }
    }
}

