namespace BidscubeSDK.QA
{
    /// <summary>
    /// Offline fixtures for SDK-path location probe (video companion + native).
    /// Ported from Flutter <c>sdk_location_probe_fixtures.dart</c>.
    /// </summary>
    public static class SdkLocationProbeFixtures
    {
        public const string OfflineCompanionImg =
            "data:image/svg+xml;charset=utf-8," +
            "%3Csvg%20xmlns%3D%27http%3A//www.w3.org/2000/svg%27%20width%3D%27320%27%20height%3D%27180%27%3E" +
            "%3Crect%20width%3D%27100%25%27%20height%3D%27100%25%27%20fill%3D%27%235C4B8A%27/%3E" +
            "%3Ctext%20x%3D%27160%27%20y%3D%2796%27%20fill%3D%27%23fff%27%20font-size%3D%2716%27%20" +
            "text-anchor%3D%27middle%27%20font-family%3D%27sans-serif%27%3EHTML%20Companion%3C/text%3E" +
            "%3C/svg%3E";

        /// <summary>Same HTML as Flutter/iOS video companion probe fixture.</summary>
        public static string VideoCompanionHtml =>
            "<html>" +
            "<head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"></head>" +
            "<body style=\"margin:0;padding:0;background:#222;\">" +
            "<img src=\"" + OfflineCompanionImg + "\" width=\"320\" height=\"180\" " +
            "style=\"display:block;width:100%;height:auto;\" alt=\"\">" +
            "<script>(function(){ var hostname = window.location.hostname; " +
            "console.log('[BidscubeProbe] companion hostname=' + hostname); })();</script>" +
            "</body></html>";

        /// <summary>
        /// Minimal native-ad HTML equivalent to <c>NativeAdView.BuildNativeHtml</c> content shell
        /// (offline image, no network). Loaded with <c>baseUrl=""</c> like production.
        /// </summary>
        public static string NativeProbeHtml =>
            "<!DOCTYPE html><html><head>" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
            "<style>body{margin:0;padding:8px;font-family:sans-serif;background:#111;color:#fff;}" +
            ".title{font-size:16px;font-weight:bold;}.desc{font-size:12px;opacity:.85;}.cta{margin-top:8px;}</style>" +
            "</head><body>" +
            "<div class=\"title\">SDK Path Native Probe</div>" +
            "<div class=\"desc\">Offline native WebView template</div>" +
            "<img src=\"" + OfflineCompanionImg + "\" width=\"320\" height=\"180\" " +
            "style=\"display:block;width:100%;height:auto;margin-top:8px;\" alt=\"\">" +
            "<div class=\"cta\"><a href=\"https://example.com/click\" style=\"color:#8af;\">Install</a></div>" +
            "</body></html>";
    }
}
