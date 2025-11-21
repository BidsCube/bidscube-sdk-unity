using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

namespace BidscubeSDK
{
    /// <summary>
    /// VAST (Video Ad Serving Template) parser
    /// Supports VAST 2.0, 3.0, and 4.0 formats
    /// </summary>
    public static class VASTParser
    {
        /// <summary>
        /// Default test VAST XML for local testing
        /// </summary>
        private static readonly string DefaultTestVAST = @"<VAST version=""3.0""><Ad id=""countdown-1"" sequence=""1""><InLine><AdSystem>GDFP</AdSystem><AdTitle>COUNTDOWN30</AdTitle><Impression /><Creatives><Creative id=""broadpeakio"" sequence=""1""><Linear skipoffset=""00:00:05""><Duration>00:30:00</Duration><MediaFiles><MediaFile id=""GDFP"" delivery=""progressive"" type=""video/mp4"" scalable=""true"" maintainAspectRatio=""true"" height=""480"" width=""240""><![CDATA[https://bpkcscreatives.s3.eu-west-1.amazonaws.com/commercial-slate.png_30s.mp4]]></MediaFile><MediaFile id=""GDFP"" delivery=""streaming"" width=""426"" height=""236"" type=""application/x-mpegURL"" minBitrate=""45"" maxBitrate=""3240"" scalable=""true"" maintainAspectRatio=""true""><![CDATA[https://origin.broadpeak.io/bpk-vod/voddemo/hlsv4/bpkiocreatives/30s/index.m3u8]]></MediaFile><MediaFile id=""GDFP"" delivery=""streaming"" width=""426"" height=""236"" type=""application/dash+xml"" minBitrate=""45"" maxBitrate=""3240"" scalable=""true"" maintainAspectRatio=""true""><![CDATA[https://origin.broadpeak.io/bpk-vod/voddemo/default/bpkiocreatives/30s/index.mpd]]></MediaFile></MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";

        /// <summary>
        /// Test parsing with default VAST XML
        /// </summary>
        /// <returns>True if parsing succeeds, false otherwise</returns>
        public static bool TestDefaultVAST()
        {
            Logger.Info("[VASTParser] Testing with default VAST XML...");
            var result = Parse(DefaultTestVAST);
            if (result != null && !string.IsNullOrEmpty(result.videoUrl))
            {
                Logger.Info($"[VASTParser] Default VAST test SUCCESS - Video URL: {result.videoUrl}");
                return true;
            }
            else
            {
                Logger.InfoError("[VASTParser] Default VAST test FAILED - No video URL found");
                return false;
            }
        }

        [System.Serializable]
        public class VASTData
        {
            public string videoUrl;
            public string clickThroughUrl;
            public List<string> impressionUrls = new List<string>();
            public List<string> startUrls = new List<string>();
            public List<string> firstQuartileUrls = new List<string>();
            public List<string> midpointUrls = new List<string>();
            public List<string> thirdQuartileUrls = new List<string>();
            public List<string> completeUrls = new List<string>();
            public List<string> skipUrls = new List<string>();
            public List<string> clickTrackingUrls = new List<string>();
            public int skipOffset = -1; // -1 means not skippable, otherwise seconds
            public int duration = 0; // Video duration in seconds
        }

        /// <summary>
        /// Parse VAST XML string
        /// </summary>
        /// <param name="vastXml">VAST XML string</param>
        /// <returns>Parsed VAST data or null if parsing fails</returns>
        public static VASTData Parse(string vastXml)
        {
            return Parse(vastXml, 0);
        }

        /// <summary>
        /// Parse VAST XML string with recursion depth tracking (for Wrapper VAST)
        /// </summary>
        /// <param name="vastXml">VAST XML string</param>
        /// <param name="depth">Recursion depth (to prevent infinite loops)</param>
        /// <returns>Parsed VAST data or null if parsing fails</returns>
        private static VASTData Parse(string vastXml, int depth)
        {
            if (string.IsNullOrEmpty(vastXml))
            {
                Logger.InfoError("[VASTParser] Empty VAST XML");
                return null;
            }

            // Prevent infinite recursion (max 5 wrapper levels)
            if (depth > 5)
            {
                Logger.InfoError("[VASTParser] Maximum wrapper depth reached, stopping to prevent infinite loop");
                return null;
            }

            try
            {
                // Clean the XML: remove BOM, leading whitespace, and handle XML declaration
                vastXml = CleanVASTXml(vastXml);

                // Create XmlDocument with namespace manager to handle namespaces
                var xmlDoc = new XmlDocument();

                // Create namespace manager for handling xmlns attributes
                var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
                namespaceManager.AddNamespace("vast", "http://www.iab.com/VAST");
                namespaceManager.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");

                // Get the default namespace from the root element if present
                string defaultNamespace = null;
                if (xmlDoc.DocumentElement != null && !string.IsNullOrEmpty(xmlDoc.DocumentElement.NamespaceURI))
                {
                    defaultNamespace = xmlDoc.DocumentElement.NamespaceURI;
                    namespaceManager.AddNamespace("def", defaultNamespace);
                }

                // Try to load XML with error handling
                try
                {
                    xmlDoc.LoadXml(vastXml);
                }
                catch (XmlException xmlEx)
                {
                    // If XML parsing fails, try to fix common issues and retry
                    Logger.Info($"[VASTParser] Initial XML parse failed: {xmlEx.Message}, attempting to fix...");

                    // Try to fix unescaped quotes in attributes
                    // This is a best-effort fix - we'll try common patterns
                    string fixedXml = vastXml;

                    // Fix escaped backslashes that might be causing issues
                    // If we see patterns like attribute=\"value\\something\", fix them
                    // But be careful not to break valid escapes

                    // Try using XmlReader with more lenient settings
                    try
                    {
                        using (var reader = new System.IO.StringReader(fixedXml))
                        {
                            var xmlReader = XmlReader.Create(reader, new XmlReaderSettings
                            {
                                CheckCharacters = false,  // Allow invalid characters
                                IgnoreWhitespace = true,
                                IgnoreComments = true
                            });
                            xmlDoc.Load(xmlReader);
                        }
                    }
                    catch
                    {
                        // If that also fails, try one more time with regex fixes
                        // Remove any backslashes that aren't part of valid escape sequences
                        fixedXml = System.Text.RegularExpressions.Regex.Replace(fixedXml, @"\\(?![""\\/bfnrtu])", "");

                        try
                        {
                            xmlDoc.LoadXml(fixedXml);
                        }
                        catch
                        {
                            // Last resort: log the error and return null
                            Logger.InfoError($"[VASTParser] Could not parse XML even after fixes. Error: {xmlEx.Message}");
                            Logger.InfoError($"[VASTParser] XML preview (first 500 chars): {vastXml.Substring(0, Mathf.Min(500, vastXml.Length))}");
                            return null;
                        }
                    }
                }

                var vastData = new VASTData();

                Logger.Info($"[VASTParser] Parsing VAST XML (depth: {depth}, {vastXml.Length} chars)");
                Logger.Info($"[VASTParser] VAST XML preview: {vastXml.Substring(0, Mathf.Min(500, vastXml.Length))}...");

                // Find the first InLine or Wrapper ad
                // Try with namespace first, then without
                // Also try with default namespace prefix if present
                XmlNode inlineNode = null;
                XmlNode wrapperNode = null;

                // Try with default namespace prefix
                if (defaultNamespace != null)
                {
                    inlineNode = xmlDoc.SelectSingleNode("//def:VAST/def:Ad/def:InLine", namespaceManager);
                    wrapperNode = xmlDoc.SelectSingleNode("//def:VAST/def:Ad/def:Wrapper", namespaceManager);
                }

                // Try with vast: prefix
                if (inlineNode == null)
                {
                    inlineNode = xmlDoc.SelectSingleNode("//vast:VAST/vast:Ad/vast:InLine", namespaceManager);
                }
                if (wrapperNode == null)
                {
                    wrapperNode = xmlDoc.SelectSingleNode("//vast:VAST/vast:Ad/vast:Wrapper", namespaceManager);
                }

                // Try without namespace (for backwards compatibility)
                if (inlineNode == null)
                {
                    inlineNode = xmlDoc.SelectSingleNode("//VAST/Ad/InLine") ?? xmlDoc.SelectSingleNode("//InLine");
                }
                if (wrapperNode == null)
                {
                    wrapperNode = xmlDoc.SelectSingleNode("//VAST/Ad/Wrapper") ?? xmlDoc.SelectSingleNode("//Wrapper");
                }

                // Check if this is a Wrapper VAST - Parse should only handle InLine VASTs
                // Wrapper VASTs should be handled by VideoAdView before calling Parse
                if (wrapperNode != null && inlineNode == null)
                {
                    Logger.Info("[VASTParser] Wrapper VAST detected in Parse method - this should be handled by VideoAdView before parsing. Returning null.");
                    return null;
                }

                // This is an InLine VAST (or we're processing the nested VAST from a wrapper)
                var adNode = inlineNode ?? wrapperNode;
                if (adNode == null)
                {
                    Logger.InfoError("[VASTParser] No InLine or Wrapper found in VAST");
                    return null;
                }

                Logger.Info("[VASTParser] Processing InLine VAST");

                // Parse MediaFiles for video URL - try multiple XPath patterns
                XmlNodeList mediaFiles = null;

                // Try with default namespace prefix first
                if (defaultNamespace != null)
                {
                    mediaFiles = adNode.SelectNodes(".//def:MediaFile", namespaceManager);
                    Logger.Info($"[VASTParser] Found {mediaFiles?.Count ?? 0} MediaFile nodes via .//def:MediaFile (default namespace)");
                }

                // Try with vast: prefix
                if (mediaFiles == null || mediaFiles.Count == 0)
                {
                    mediaFiles = adNode.SelectNodes(".//vast:MediaFile", namespaceManager);
                    Logger.Info($"[VASTParser] Found {mediaFiles?.Count ?? 0} MediaFile nodes via .//vast:MediaFile");
                }

                // Try standard path (without namespace)
                if (mediaFiles == null || mediaFiles.Count == 0)
                {
                    mediaFiles = adNode.SelectNodes(".//MediaFile");
                    Logger.Info($"[VASTParser] Found {mediaFiles?.Count ?? 0} MediaFile nodes via .//MediaFile");
                }

                // If not found, try without namespace from root
                if (mediaFiles == null || mediaFiles.Count == 0)
                {
                    if (defaultNamespace != null)
                    {
                        mediaFiles = xmlDoc.SelectNodes("//def:MediaFile", namespaceManager);
                        Logger.Info($"[VASTParser] Found {mediaFiles?.Count ?? 0} MediaFile nodes via //def:MediaFile (default namespace)");
                    }
                    if (mediaFiles == null || mediaFiles.Count == 0)
                    {
                        mediaFiles = xmlDoc.SelectNodes("//vast:MediaFile", namespaceManager);
                        Logger.Info($"[VASTParser] Found {mediaFiles?.Count ?? 0} MediaFile nodes via //vast:MediaFile");
                    }
                    if (mediaFiles == null || mediaFiles.Count == 0)
                    {
                        mediaFiles = xmlDoc.SelectNodes("//MediaFile");
                        Logger.Info($"[VASTParser] Found {mediaFiles?.Count ?? 0} MediaFile nodes via //MediaFile");
                    }
                }

                // Try Linear/MediaFiles path with namespace
                if (mediaFiles == null || mediaFiles.Count == 0)
                {
                    XmlNode linearNode = null;
                    if (defaultNamespace != null)
                    {
                        linearNode = adNode.SelectSingleNode(".//def:Linear", namespaceManager);
                    }
                    if (linearNode == null)
                    {
                        linearNode = adNode.SelectSingleNode(".//vast:Linear", namespaceManager);
                    }
                    if (linearNode == null)
                    {
                        linearNode = adNode.SelectSingleNode(".//Linear");
                    }

                    if (linearNode != null)
                    {
                        if (defaultNamespace != null)
                        {
                            mediaFiles = linearNode.SelectNodes(".//def:MediaFile", namespaceManager);
                        }
                        if (mediaFiles == null || mediaFiles.Count == 0)
                        {
                            mediaFiles = linearNode.SelectNodes(".//vast:MediaFile", namespaceManager);
                        }
                        if (mediaFiles == null || mediaFiles.Count == 0)
                        {
                            mediaFiles = linearNode.SelectNodes(".//MediaFile");
                        }
                        Logger.Info($"[VASTParser] Found {mediaFiles?.Count ?? 0} MediaFile nodes via Linear/.//MediaFile");
                    }
                }

                // Try MediaFiles directly with namespace
                if (mediaFiles == null || mediaFiles.Count == 0)
                {
                    XmlNode mediaFilesNode = null;
                    if (defaultNamespace != null)
                    {
                        mediaFilesNode = adNode.SelectSingleNode(".//def:MediaFiles", namespaceManager);
                    }
                    if (mediaFilesNode == null)
                    {
                        mediaFilesNode = adNode.SelectSingleNode(".//vast:MediaFiles", namespaceManager);
                    }
                    if (mediaFilesNode == null)
                    {
                        mediaFilesNode = adNode.SelectSingleNode(".//MediaFiles");
                    }

                    if (mediaFilesNode != null)
                    {
                        if (defaultNamespace != null)
                        {
                            mediaFiles = mediaFilesNode.SelectNodes(".//def:MediaFile", namespaceManager);
                        }
                        if (mediaFiles == null || mediaFiles.Count == 0)
                        {
                            mediaFiles = mediaFilesNode.SelectNodes(".//vast:MediaFile", namespaceManager);
                        }
                        if (mediaFiles == null || mediaFiles.Count == 0)
                        {
                            mediaFiles = mediaFilesNode.SelectNodes(".//MediaFile");
                        }
                        Logger.Info($"[VASTParser] Found {mediaFiles?.Count ?? 0} MediaFile nodes via MediaFiles/.//MediaFile");
                    }
                }

                if (mediaFiles != null && mediaFiles.Count > 0)
                {
                    // Prefer mp4, then webm, then any other
                    XmlNode selectedMediaFile = null;
                    foreach (XmlNode mediaFile in mediaFiles)
                    {
                        var type = mediaFile.Attributes?["type"]?.Value?.ToLower() ?? "";
                        var url = ExtractMediaFileUrl(mediaFile);

                        Logger.Info($"[VASTParser] MediaFile - Type: {type}, URL: {url?.Substring(0, Mathf.Min(100, url?.Length ?? 0))}...");

                        // Skip if no URL found
                        if (string.IsNullOrEmpty(url))
                        {
                            Logger.Info("[VASTParser] MediaFile has no URL, skipping");
                            continue;
                        }

                        if (type.Contains("mp4") || url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                        {
                            selectedMediaFile = mediaFile;
                            Logger.Info("[VASTParser] Selected MP4 MediaFile");
                            break;
                        }
                        if (selectedMediaFile == null && (type.Contains("webm") || url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)))
                        {
                            selectedMediaFile = mediaFile;
                            Logger.Info("[VASTParser] Selected WebM MediaFile (backup)");
                        }
                        if (selectedMediaFile == null)
                        {
                            selectedMediaFile = mediaFile;
                            Logger.Info($"[VASTParser] Selected MediaFile with type: {type}");
                        }
                    }

                    if (selectedMediaFile != null)
                    {
                        vastData.videoUrl = ExtractMediaFileUrl(selectedMediaFile);

                        var durationAttr = selectedMediaFile.Attributes?["duration"]?.Value;
                        if (!string.IsNullOrEmpty(durationAttr))
                        {
                            vastData.duration = ParseDuration(durationAttr);
                        }
                        Logger.Info($"[VASTParser] Extracted video URL: {vastData.videoUrl}");
                    }
                    else
                    {
                        Logger.Info("[VASTParser] MediaFiles found but none selected (all had empty URLs)");
                    }
                }
                else
                {
                    Logger.Info("[VASTParser] No MediaFile nodes found in any path");
                }

                // Parse duration from Creative/Duration if not found
                if (vastData.duration == 0)
                {
                    var durationNode = adNode.SelectSingleNode(".//Duration");
                    if (durationNode != null)
                    {
                        vastData.duration = ParseDuration(durationNode.InnerText);
                    }
                }

                // Parse ClickThrough
                var clickThroughNode = adNode.SelectSingleNode(".//ClickThrough");
                if (clickThroughNode != null)
                {
                    vastData.clickThroughUrl = clickThroughNode.InnerText?.Trim();
                }

                // Parse ClickTracking
                var clickTrackingNodes = adNode.SelectNodes(".//ClickTracking");
                if (clickTrackingNodes != null)
                {
                    foreach (XmlNode node in clickTrackingNodes)
                    {
                        var url = node.InnerText?.Trim();
                        if (!string.IsNullOrEmpty(url))
                        {
                            vastData.clickTrackingUrls.Add(url);
                        }
                    }
                }

                // Parse Tracking events
                var trackingNodes = adNode.SelectNodes(".//Tracking");
                if (trackingNodes != null)
                {
                    foreach (XmlNode trackingNode in trackingNodes)
                    {
                        var eventType = trackingNode.Attributes?["event"]?.Value;
                        var url = trackingNode.InnerText?.Trim();

                        if (string.IsNullOrEmpty(url)) continue;

                        switch (eventType?.ToLower())
                        {
                            case "impression":
                                vastData.impressionUrls.Add(url);
                                break;
                            case "start":
                            case "creativeView":
                                vastData.startUrls.Add(url);
                                break;
                            case "firstQuartile":
                                vastData.firstQuartileUrls.Add(url);
                                break;
                            case "midpoint":
                                vastData.midpointUrls.Add(url);
                                break;
                            case "thirdQuartile":
                                vastData.thirdQuartileUrls.Add(url);
                                break;
                            case "complete":
                                vastData.completeUrls.Add(url);
                                break;
                            case "skip":
                                vastData.skipUrls.Add(url);
                                break;
                        }
                    }
                }

                // Parse skipoffset
                var skipOffsetNode = adNode.SelectSingleNode(".//Skipoffset");
                if (skipOffsetNode != null)
                {
                    var skipOffsetValue = skipOffsetNode.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(skipOffsetValue))
                    {
                        vastData.skipOffset = ParseSkipOffset(skipOffsetValue);
                    }
                }

                // Also try to find MediaFile directly under Creative/Linear
                if (string.IsNullOrEmpty(vastData.videoUrl))
                {
                    Logger.Info("[VASTParser] Trying to find MediaFile in Creative/Linear path...");
                    var creativeNode = adNode.SelectSingleNode(".//Creative");
                    if (creativeNode != null)
                    {
                        var linearNode = creativeNode.SelectSingleNode(".//Linear");
                        if (linearNode != null)
                        {
                            var creativeMediaFiles = linearNode.SelectNodes(".//MediaFile");
                            if (creativeMediaFiles != null && creativeMediaFiles.Count > 0)
                            {
                                Logger.Info($"[VASTParser] Found {creativeMediaFiles.Count} MediaFile nodes in Creative/Linear");
                                foreach (XmlNode mediaFile in creativeMediaFiles)
                                {
                                    var url = ExtractMediaFileUrl(mediaFile);
                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        vastData.videoUrl = url;
                                        Logger.Info($"[VASTParser] Found video URL in Creative/Linear/MediaFile: {url}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // If no video URL found, try alternative parsing methods
                if (string.IsNullOrEmpty(vastData.videoUrl))
                {
                    Logger.Info("[VASTParser] Trying alternative parsing methods...");
                    // Try to find MediaFile nodes with different XPath (already tried above, but try again with more patterns)
                    var altMediaFiles = xmlDoc.SelectNodes("//MediaFile");
                    Logger.Info($"[VASTParser] Found {altMediaFiles?.Count ?? 0} MediaFile nodes via //MediaFile XPath");
                    if (altMediaFiles != null && altMediaFiles.Count > 0)
                    {
                        foreach (XmlNode mediaFile in altMediaFiles)
                        {
                            var url = ExtractMediaFileUrl(mediaFile);

                            Logger.Info($"[VASTParser] Checking MediaFile URL: {url?.Substring(0, Mathf.Min(100, url?.Length ?? 0))}...");
                            if (!string.IsNullOrEmpty(url) && (url.StartsWith("http://") || url.StartsWith("https://")))
                            {
                                vastData.videoUrl = url;
                                Logger.Info($"[VASTParser] Found video URL via alternative XPath: {url}");
                                break;
                            }
                        }
                    }

                    // Try to find video URL in any element that looks like a video URL
                    if (string.IsNullOrEmpty(vastData.videoUrl))
                    {
                        Logger.Info("[VASTParser] Trying to find video URL in any element...");
                        var allNodes = xmlDoc.SelectNodes("//*");
                        if (allNodes != null)
                        {
                            foreach (XmlNode node in allNodes)
                            {
                                var text = node.InnerText?.Trim();
                                if (!string.IsNullOrEmpty(text) &&
                                    (text.StartsWith("http://") || text.StartsWith("https://")) &&
                                    (text.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                                     text.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                                     text.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                                     text.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)))
                                {
                                    vastData.videoUrl = text;
                                    Logger.Info($"[VASTParser] Found video URL in element {node.Name}: {text}");
                                    break;
                                }
                            }
                        }
                    }

                    // Try to find video URL in CDATA sections
                    if (string.IsNullOrEmpty(vastData.videoUrl))
                    {
                        var cdataMatches = Regex.Matches(vastXml, @"<!\[CDATA\[(.*?)\]\]>", RegexOptions.Singleline);
                        foreach (Match cdataMatch in cdataMatches)
                        {
                            var cdataContent = cdataMatch.Groups[1].Value;
                            var urlMatch = Regex.Match(cdataContent, @"(https?://[^\s""'<>]+\.(mp4|webm|mov|avi|m3u8))", RegexOptions.IgnoreCase);
                            if (urlMatch.Success)
                            {
                                vastData.videoUrl = urlMatch.Groups[1].Value;
                                Logger.Info($"[VASTParser] Found video URL in CDATA: {vastData.videoUrl}");
                                break;
                            }
                        }
                    }

                    // Try to find any video URL in the entire XML
                    if (string.IsNullOrEmpty(vastData.videoUrl))
                    {
                        var urlMatches = Regex.Matches(vastXml, @"(https?://[^\s<>""']+\.(mp4|webm|mov|avi|m3u8))", RegexOptions.IgnoreCase);
                        if (urlMatches.Count > 0)
                        {
                            // Prefer mp4, then webm, then others
                            string preferredUrl = null;
                            foreach (Match match in urlMatches)
                            {
                                var url = match.Groups[1].Value;
                                if (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                                {
                                    preferredUrl = url;
                                    break;
                                }
                                if (preferredUrl == null && url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
                                {
                                    preferredUrl = url;
                                }
                                if (preferredUrl == null)
                                {
                                    preferredUrl = url;
                                }
                            }
                            if (preferredUrl != null)
                            {
                                vastData.videoUrl = preferredUrl;
                                Logger.Info($"[VASTParser] Found video URL via regex: {vastData.videoUrl}");
                            }
                        }
                    }

                    // Try to find in Linear/MediaFiles path
                    if (string.IsNullOrEmpty(vastData.videoUrl))
                    {
                        var linearNode = xmlDoc.SelectSingleNode("//Linear");
                        if (linearNode != null)
                        {
                            var linearMediaFiles = linearNode.SelectNodes(".//MediaFile");
                            if (linearMediaFiles != null && linearMediaFiles.Count > 0)
                            {
                                foreach (XmlNode mediaFile in linearMediaFiles)
                                {
                                    var url = ExtractMediaFileUrl(mediaFile);
                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        vastData.videoUrl = url;
                                        Logger.Info($"[VASTParser] Found video URL in Linear/MediaFile: {url}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Final fallback: Use regex to find any video URL in the entire XML string
                if (string.IsNullOrEmpty(vastData.videoUrl))
                {
                    Logger.Info("[VASTParser] XML parsing didn't find video URL, trying regex fallback...");

                    // Look for video URLs in the raw XML string
                    var videoUrlPattern = @"(https?://[^\s<>""']+\.(mp4|webm|mov|avi|m3u8|flv))";
                    var matches = Regex.Matches(vastXml, videoUrlPattern, RegexOptions.IgnoreCase);

                    if (matches.Count > 0)
                    {
                        // Prefer mp4, then webm, then others
                        string preferredUrl = null;
                        foreach (Match match in matches)
                        {
                            var url = match.Groups[1].Value;
                            if (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                            {
                                preferredUrl = url;
                                Logger.Info($"[VASTParser] Found MP4 URL via regex: {url}");
                                break;
                            }
                            if (preferredUrl == null && url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
                            {
                                preferredUrl = url;
                                Logger.Info($"[VASTParser] Found WebM URL via regex: {url}");
                            }
                            if (preferredUrl == null)
                            {
                                preferredUrl = url;
                                Logger.Info($"[VASTParser] Found video URL via regex: {url}");
                            }
                        }

                        if (preferredUrl != null)
                        {
                            vastData.videoUrl = preferredUrl;
                            Logger.Info($"[VASTParser] Using video URL from regex fallback: {vastData.videoUrl}");
                        }
                    }
                }

                if (string.IsNullOrEmpty(vastData.videoUrl))
                {
                    Logger.InfoError("[VASTParser] Could not find video URL in VAST XML after all parsing attempts");
                    Logger.InfoError($"[VASTParser] VAST XML preview (first 2000 chars):\n{vastXml.Substring(0, Mathf.Min(2000, vastXml.Length))}");

                    // Try to dump the XML structure for debugging
                    try
                    {
                        Logger.InfoError($"[VASTParser] XML Document structure:\n{xmlDoc.OuterXml.Substring(0, Mathf.Min(2000, xmlDoc.OuterXml.Length))}");
                    }
                    catch
                    {
                        // Ignore if we can't dump XML
                    }
                }
                else
                {
                    Logger.Info($"[VASTParser] Successfully parsed VAST - Video URL: {vastData.videoUrl}, Duration: {vastData.duration}s, SkipOffset: {vastData.skipOffset}s");
                }

                return vastData;
            }
            catch (Exception e)
            {
                Logger.InfoError($"[VASTParser] Error parsing VAST XML: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        private static int ParseDuration(string durationStr)
        {
            if (string.IsNullOrEmpty(durationStr))
                return 0;

            // Format: HH:MM:SS or MM:SS or SS
            var parts = durationStr.Split(':');
            if (parts.Length == 3)
            {
                // HH:MM:SS
                if (int.TryParse(parts[0], out int hours) &&
                    int.TryParse(parts[1], out int minutes) &&
                    int.TryParse(parts[2], out int seconds))
                {
                    return hours * 3600 + minutes * 60 + seconds;
                }
            }
            else if (parts.Length == 2)
            {
                // MM:SS
                if (int.TryParse(parts[0], out int minutes) &&
                    int.TryParse(parts[1], out int seconds))
                {
                    return minutes * 60 + seconds;
                }
            }
            else if (parts.Length == 1)
            {
                // SS
                if (int.TryParse(parts[0], out int seconds))
                {
                    return seconds;
                }
            }

            // Try to parse as seconds directly
            if (int.TryParse(durationStr, out int secs))
            {
                return secs;
            }

            return 0;
        }

        private static int ParseSkipOffset(string skipOffsetStr)
        {
            if (string.IsNullOrEmpty(skipOffsetStr))
                return -1;

            // Can be in format "00:00:05" or "5" or "5s"
            skipOffsetStr = skipOffsetStr.Trim().ToLower();

            // Remove 's' suffix if present
            if (skipOffsetStr.EndsWith("s"))
            {
                skipOffsetStr = skipOffsetStr.Substring(0, skipOffsetStr.Length - 1);
            }

            // Try parsing as duration format first
            var duration = ParseDuration(skipOffsetStr);
            if (duration > 0)
            {
                return duration;
            }

            // Try parsing as integer seconds
            if (int.TryParse(skipOffsetStr, out int seconds))
            {
                return seconds;
            }

            return -1;
        }

        /// <summary>
        /// Fetch and parse VAST from Wrapper VASTAdTagURI (synchronous - should be called from coroutine)
        /// </summary>
        /// <param name="vastAdTagUri">VAST Ad Tag URI from Wrapper</param>
        /// <returns>Parsed VAST data or null if fetch/parse fails</returns>
        private static VASTData FetchAndParseWrapperVAST(string vastAdTagUri)
        {
            // This method should not be called directly - it needs to be called from a coroutine
            // The actual fetching should be done in VideoAdView
            Logger.InfoError("[VASTParser] FetchAndParseWrapperVAST called directly - this should be handled in VideoAdView coroutine");
            return null;
        }

        /// <summary>
        /// Check if VAST XML is a Wrapper type
        /// </summary>
        /// <param name="vastXml">VAST XML string</param>
        /// <returns>True if Wrapper, false if InLine</returns>
        public static bool IsWrapperVAST(string vastXml)
        {
            if (string.IsNullOrEmpty(vastXml))
                return false;

            return vastXml.Contains("<Wrapper") || vastXml.Contains("<Wrapper>");
        }

        /// <summary>
        /// Extract VASTAdTagURI from Wrapper VAST
        /// </summary>
        /// <param name="vastXml">VAST XML string</param>
        /// <returns>VAST Ad Tag URI or null if not found</returns>
        public static string ExtractVASTAdTagURI(string vastXml)
        {
            if (string.IsNullOrEmpty(vastXml))
                return null;

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(vastXml);

                var vastAdTagUriNode = xmlDoc.SelectSingleNode("//VASTAdTagURI") ?? xmlDoc.SelectSingleNode("//VASTAdTagURI");
                if (vastAdTagUriNode != null)
                {
                    var vastAdTagUri = vastAdTagUriNode.InnerText?.Trim();

                    // Handle CDATA
                    if (vastAdTagUri.StartsWith("<![CDATA[") && vastAdTagUri.EndsWith("]]>"))
                    {
                        vastAdTagUri = vastAdTagUri.Substring(9, vastAdTagUri.Length - 12).Trim();
                    }

                    return vastAdTagUri;
                }
            }
            catch (System.Exception e)
            {
                Logger.InfoError($"[VASTParser] Error extracting VASTAdTagURI: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Clean VAST XML string - remove BOM, leading whitespace, and normalize XML declaration
        /// Also handles escaped characters that might break XML parsing
        /// </summary>
        /// <param name="vastXml">Raw VAST XML string</param>
        /// <returns>Cleaned VAST XML string</returns>
        private static string CleanVASTXml(string vastXml)
        {
            if (string.IsNullOrEmpty(vastXml))
                return vastXml;

            // Remove BOM (Byte Order Mark) if present
            vastXml = vastXml.TrimStart('\uFEFF');

            // Trim leading/trailing whitespace
            vastXml = vastXml.Trim();

            // Fix common JSON escape sequences that might break XML parsing
            // These can occur when XML is embedded in JSON and not properly unescaped
            // Handle multiple levels of escaping (JSON might have double-escaped characters)

            // First, handle double-escaped sequences (from JSON strings)
            // Pattern: \\\" becomes \" which should become "
            vastXml = vastXml
                .Replace("\\\\\"", "\"")  // Double-escaped quotes
                .Replace("\\\\'", "'")    // Double-escaped single quotes
                .Replace("\\\\n", "\n")   // Double-escaped newlines
                .Replace("\\\\r", "\r")   // Double-escaped carriage returns
                .Replace("\\\\t", "\t")   // Double-escaped tabs
                .Replace("\\\\/", "/")    // Double-escaped forward slashes
                .Replace("\\\\\\\\", "\\"); // Double-escaped backslashes

            // Then handle single-escaped sequences
            vastXml = vastXml
                .Replace("\\\"", "\"")   // Unescape quotes
                .Replace("\\'", "'")     // Unescape single quotes
                .Replace("\\n", "\n")    // Unescape newlines
                .Replace("\\r", "\r")    // Unescape carriage returns
                .Replace("\\t", "\t")    // Unescape tabs
                .Replace("\\/", "/");    // Unescape forward slashes

            // Remove any remaining problematic backslashes that aren't part of valid XML
            // This regex removes backslashes that aren't followed by valid escape sequences
            // Valid escapes in XML: &quot; &apos; &amp; &lt; &gt; but backslashes aren't standard XML escapes
            // So we remove standalone backslashes that might break parsing
            vastXml = Regex.Replace(vastXml, @"\\(?![""\\/bfnrtu]|u[0-9a-fA-F]{4})", "");

            // Remove XML declaration if present (Unity's XmlDocument can have issues with it)
            if (vastXml.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                int declarationEnd = vastXml.IndexOf("?>");
                if (declarationEnd > 0)
                {
                    vastXml = vastXml.Substring(declarationEnd + 2).TrimStart();
                }
            }

            // Remove any leading whitespace/newlines that might cause "Data at the root level" error
            vastXml = vastXml.TrimStart();

            // Ensure we have a valid root element
            if (!vastXml.StartsWith("<VAST", StringComparison.OrdinalIgnoreCase))
            {
                // Try to find VAST tag if it's not at the start (might have namespace)
                int vastStart = vastXml.IndexOf("<VAST", StringComparison.OrdinalIgnoreCase);
                if (vastStart > 0)
                {
                    // Remove everything before VAST tag
                    vastXml = vastXml.Substring(vastStart);
                }
                else
                {
                    Logger.Info("[VASTParser] Could not find <VAST> tag in XML");
                }
            }

            // Extract only the VAST XML portion (handle trailing content after </VAST>)
            // Find the closing </VAST> tag and remove everything after it
            int vastEnd = vastXml.LastIndexOf("</VAST>", StringComparison.OrdinalIgnoreCase);
            if (vastEnd > 0)
            {
                vastXml = vastXml.Substring(0, vastEnd + 7); // +7 for "</VAST>"
            }
            else
            {
                // If no closing tag found, try to find it case-insensitively
                vastEnd = vastXml.LastIndexOf("</vast>", StringComparison.OrdinalIgnoreCase);
                if (vastEnd > 0)
                {
                    vastXml = vastXml.Substring(0, vastEnd + 7);
                }
            }

            return vastXml;
        }

        /// <summary>
        /// Extract URL from MediaFile node (handles CDATA, InnerText, and attributes)
        /// </summary>
        /// <param name="mediaFileNode">MediaFile XML node</param>
        /// <returns>Video URL or empty string</returns>
        private static string ExtractMediaFileUrl(XmlNode mediaFileNode)
        {
            if (mediaFileNode == null)
                return string.Empty;

            // Try InnerText first
            var url = mediaFileNode.InnerText?.Trim();

            // Handle CDATA - InnerText should already contain CDATA content, but check for CDATA wrapper
            if (!string.IsNullOrEmpty(url))
            {
                if (url.StartsWith("<![CDATA[") && url.EndsWith("]]>"))
                {
                    url = url.Substring(9, url.Length - 12).Trim();
                }
            }

            // If InnerText is empty, try attribute
            if (string.IsNullOrEmpty(url))
            {
                url = mediaFileNode.Attributes?["url"]?.Value?.Trim();
            }

            // If still empty, try to get from InnerXml (handles CDATA properly)
            if (string.IsNullOrEmpty(url))
            {
                var innerXml = mediaFileNode.InnerXml?.Trim();
                if (!string.IsNullOrEmpty(innerXml))
                {
                    // Extract from CDATA
                    if (innerXml.StartsWith("<![CDATA[") && innerXml.EndsWith("]]>"))
                    {
                        url = innerXml.Substring(9, innerXml.Length - 12).Trim();
                    }
                    else
                    {
                        url = innerXml;
                    }
                }
            }

            return url ?? string.Empty;
        }

        /// <summary>
        /// Fire tracking URL (make HTTP request)
        /// </summary>
        /// <param name="url">Tracking URL</param>
        public static void FireTrackingUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                // Fire tracking URL asynchronously
                UnityEngine.Networking.UnityWebRequest.Get(url).SendWebRequest();
                Logger.Info($"[VASTParser] Fired tracking URL: {url}");
            }
            catch (Exception e)
            {
                Logger.Info($"[VASTParser] Failed to fire tracking URL {url}: {e.Message}");
            }
        }

        /// <summary>
        /// Fire multiple tracking URLs
        /// </summary>
        /// <param name="urls">List of tracking URLs</param>
        public static void FireTrackingUrls(List<string> urls)
        {
            if (urls == null || urls.Count == 0)
                return;

            foreach (var url in urls)
            {
                FireTrackingUrl(url);
            }
        }
    }
}

