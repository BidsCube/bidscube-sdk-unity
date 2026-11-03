namespace BidscubeSDK
{
    /// <summary>
    /// Pure end-card resource/click resolution for VAST Companion + fallback preview.
    /// </summary>
    public static class VideoEndCardResolver
    {
        public static bool ShouldRenderInWebView(VASTParser.VastCompanionResourceType type)
        {
            return type == VASTParser.VastCompanionResourceType.Html
                || type == VASTParser.VastCompanionResourceType.IFrame;
        }

        /// <summary>
        /// Click-through for end card: CompanionClickThrough, else linear ClickThrough.
        /// </summary>
        public static string ResolveClickUrl(VASTParser.VASTData data)
        {
            if (data == null)
                return null;
            if (!string.IsNullOrEmpty(data.previewClickThroughUrl))
                return data.previewClickThroughUrl;
            return data.clickThroughUrl;
        }

        /// <summary>
        /// Static image for the end card: Companion StaticResource, else VAST previewImageUrl.
        /// HTML/IFrame companions are not image URLs.
        /// </summary>
        public static string ResolveStaticPreviewImageUrl(VASTParser.VASTData data)
        {
            if (data == null)
                return null;

            if (data.companionResourceType == VASTParser.VastCompanionResourceType.StaticImage
                && !string.IsNullOrEmpty(data.companionResource))
            {
                return data.companionResource;
            }

            if (!string.IsNullOrEmpty(data.previewImageUrl))
                return data.previewImageUrl;

            return null;
        }

        public static string ResolveCtaLabel(string clickUrl)
        {
            return string.IsNullOrEmpty(clickUrl) ? "Preview" : "Learn More";
        }
    }
}
