using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

namespace BidscubeSDK
{
    /// <summary>
    /// Banner ad view component - Identical to iOS BannerAdView
    /// </summary>
    public class BannerAdView : MonoBehaviour
    {
        // Helper class for JSON deserialization
        [System.Serializable]
        private class AdResponse
        {
            public string adm;
            public int position; // Changed from int? to int - Unity JsonUtility doesn't handle nullable well
        }

        /// <summary>
        /// Some SSP responses nest adm as JSON: {"adm":"..."} inside the adm string (e.g. after document.write).
        /// Peel those layers so the WebView renders markup, not raw {"adm": ...} text.
        /// </summary>
        private static string UnwrapNestedAdmJson(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            var s = raw.Trim();
            for (var i = 0; i < 8; i++)
            {
                if (s.Length < 2 || s[0] != '{') break;
                if (s.IndexOf("\"adm\"", StringComparison.OrdinalIgnoreCase) < 0) break;

                string next = null;
                var parsed = JsonUtility.FromJson<AdResponse>(s);
                if (parsed != null && !string.IsNullOrEmpty(parsed.adm))
                    next = parsed.adm.Trim();
                if (string.IsNullOrEmpty(next))
                    next = TryExtractAdmQuotedString(s)?.Trim();

                if (string.IsNullOrEmpty(next) || string.Equals(next, s, StringComparison.Ordinal))
                    break;
                s = next;
            }

            return s;
        }

        /// <summary>
        /// Some creatives return HTML whose body still contains a literal <c>{"adm":"&lt;div...&lt;/div&gt;"}</c> inside a span.
        /// Peel those layers so the WebView shows markup, not JSON text.
        /// </summary>
        private static string StripEmbeddedAdmJsonInsideHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            var s = html;
            for (var iter = 0; iter < 8; iter++)
            {
                var trimmed = s.TrimStart();
                if (trimmed.Length > 0 && trimmed[0] == '{')
                {
                    var ju = UnwrapNestedAdmJson(s);
                    if (!string.IsNullOrEmpty(ju) && !string.Equals(ju, s, StringComparison.Ordinal))
                    {
                        s = ju;
                        continue;
                    }
                }

                if (s.IndexOf("\"adm\"", StringComparison.OrdinalIgnoreCase) < 0)
                    break;

                var extracted = TryExtractAdmQuotedString(s);
                if (string.IsNullOrEmpty(extracted))
                    break;
                extracted = extracted.Trim();
                if (string.Equals(extracted, s, StringComparison.Ordinal))
                    break;

                if (LooksLikeRenderableAdmPayload(extracted))
                    return extracted;
                s = extracted;
            }

            return s;
        }

        private static bool LooksLikeRenderableAdmPayload(string t)
        {
            if (string.IsNullOrEmpty(t)) return false;
            t = t.TrimStart();
            if (t.StartsWith("<", StringComparison.Ordinal)) return true;
            if (t.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("document.write", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void LogLongResponse(string label, string text, int previewChars = 280)
        {
            if (text == null)
            {
                Logger.Info($"{label}: (null)");
                return;
            }

            Logger.Info($"{label}: {text.Length} chars");
            if (text.Length == 0) return;
            var preview = text.Length <= previewChars ? text : text.Substring(0, previewChars) + "…";
            Logger.DebugLog($"{label} preview: {preview}");
        }

        /// <summary>Decode JSON string escapes inside an "adm" value captured from regex.</summary>
        private static string DecodeJsonStringEscapes(string escaped)
        {
            if (string.IsNullOrEmpty(escaped)) return escaped;
            var withUnicode = Regex.Replace(escaped, @"\\u([0-9a-fA-F]{4})", m =>
            {
                var code = int.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
                return char.ConvertFromUtf32(code);
            });
            return withUnicode
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\/", "/")
                .Replace("\\n", "\n")
                .Replace("\\r", "")
                .Replace("\\t", "\t");
        }

        /// <summary>When JsonUtility fails, pull the first quoted "adm" string value.</summary>
        private static string TryExtractAdmQuotedString(string json)
        {
            var m = Regex.Match(json, @"""adm""\s*:\s*""((?:\\.|[^""\\])*)""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!m.Success) return null;
            return DecodeJsonStringEscapes(m.Groups[1].Value);
        }

        // These fields are kept for backward compatibility but are not used anymore
        // WebViewController handles all rendering and click handling
        [HideInInspector]
        [SerializeField] private RawImage _webView;
        [HideInInspector]
        [SerializeField] private Button _clickButton;
        [HideInInspector]
        [SerializeField] private Canvas _canvas;

        private string _placementId;
        private IAdCallback _callback;
        private AdPosition _bannerPosition = AdPosition.Header;
        private float _cornerRadius = 0f;
        private float _bannerHeight = 40f;
        private float _bannerWidth = 320f;
        // If true, do not accept dimensions extracted from ADM/HTML; configured size applied.
        private bool _useConfiguredSize = false;

        /// <summary>
        /// Apply AdSizeSettings defaults to this banner view. This enforces configured sizes
        /// instead of server-provided sizes when the SDK desires centralized sizing.
        /// </summary>
        public void ApplyAdSizeSettings(AdSizeSettings settings)
        {
            if (settings == null) return;
            var cfg = settings.GetDefaultSize(AdType.Image);
            if (cfg.x > 0f) _bannerWidth = cfg.x;
            if (cfg.y > 0f) _bannerHeight = cfg.y;
            _useConfiguredSize = true;
            Logger.Info($"[BannerAdView] Applied AdSizeSettings: {_bannerWidth}x{_bannerHeight}");
            // Notify parent controller (if any) that dimensions changed
            NotifyDimensionsUpdated();
        }

        private string _clickURL;
        private WebViewObject _webViewObject;

        // Add WebViewController reference for HTML rendering, similar to iOS implementation
        private WebViewController _webViewController;

        // Attachment state like iOS
        private bool _isAttachedToScreen = false;
        private GameObject _parentView;

        private void Awake()
        {
            SetupBannerView();
        }

        private void SetupBannerView()
        {
            // Ensure this GameObject has a RectTransform (required for UI components)
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // The BannerAdView itself doesn't set its own size anymore.
            // It will be placed inside an AdViewController which controls the size and position.
            var image = gameObject.AddComponent<Image>();
            if (image != null)
            {
                image.color = Color.clear; // Transparent background
            }

            SetupWebView();
        }

        private void SetupWebView()
        {
            // Ensure this GameObject has a RectTransform (required for UI components)
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // NOTE: Do not create WebViewController here anymore. Creation is deferred until
            // the SDK decides to render the ad (after calling IAdRenderOverride). This avoids
            // initializing platform WebView components when the app provides a custom renderer.

            // RawImage is not needed since we already have Image component from SetupBannerView
            // and WebViewController handles the actual rendering when created lazily.
            // _webView is kept as null - it's not used for WebView rendering anymore
        }

        private void SetupConstraints()
        {
            // The WebView should fill the entire BannerAdView, which in turn
            // fills the AdViewController's RectTransform.
            var webViewRect = _webViewController?.GetComponent<RectTransform>();
            if (webViewRect != null)
            {
                webViewRect.anchorMin = Vector2.zero;
                webViewRect.anchorMax = Vector2.one;
                webViewRect.offsetMin = Vector2.zero;
                webViewRect.offsetMax = Vector2.zero;
            }
        }

        /// <summary>
        /// Set placement info - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public void SetPlacementInfo(string placementId, IAdCallback callback)
        {
            _placementId = placementId;
            _callback = callback;
        }

        /// <summary>
        /// Load ad from URL - Identical to iOS loadAdFromURL
        /// </summary>
        /// <param name="url">Ad URL</param>
        public void LoadAdFromURL(string url)
        {
            Logger.Info($" BannerAdView: Making HTTP request to: {url}");
            StartCoroutine(LoadAdCoroutine(url));
        }

        private IEnumerator LoadAdCoroutine(string url)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                BidscubeSDK.ApplyConfiguredTimeoutTo(request);
                yield return request.SendWebRequest();

                // #region agent log
                var dh = request.downloadHandler;
                var bodyLen = dh != null && dh.text != null ? dh.text.Length : 0;
                AgentNdjsonDebugLog.Write(
                    "BannerAdView.LoadAdCoroutine",
                    request.result == UnityEngine.Networking.UnityWebRequest.Result.Success ? "http_ok" : "http_fail",
                    "H2",
                    "{\"placementId\":\"" + _placementId + "\",\"result\":\"" + request.result + "\",\"responseCode\":" + request.responseCode + ",\"bodyLen\":" + bodyLen + "}");
                // #endregion

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var responseText = request.downloadHandler.text;
                    LogLongResponse("BannerAdView: Received HTTP response body", responseText);

                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        // #region agent log
                        AgentNdjsonDebugLog.Write("BannerAdView.LoadAdCoroutine", "empty_body", "H3", "{\"placementId\":\"" + _placementId + "\"}");
                        // #endregion
                        Logger.InfoError("[BannerAdView] Error: Empty Response");
                        _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.NetworkError, "Empty response from server.");
                        yield break;
                    }

                    // Check if the response is JSON or raw JS/HTML
                    var trimmedResponse = responseText.Trim();
                    if (trimmedResponse.StartsWith("{") && trimmedResponse.EndsWith("}"))
                    {
                        try
                        {
                            var json = JsonUtility.FromJson<AdResponse>(trimmedResponse);
                            if (json != null && !string.IsNullOrEmpty(json.adm))
                            {
                                Logger.Info("[BidscubeSDK] BannerAdView: Parsed JSON response. Processing 'adm' field.");
                                Logger.Info($"[BidscubeSDK] BannerAdView: Raw JSON position value: {json.position}");

                                // If an app implemented IAdRenderOverride, give it the raw adm (as received from JSON) first
                                string rawAdm = json.adm;
                                int respPosRaw = json.position != 0 ? json.position : (int)BidscubeSDK.GetResponseAdPosition();
                                if (!string.IsNullOrEmpty(rawAdm) && _callback is IAdRenderOverride adRenderOverride)
                                {
                                    Logger.Info("[BidscubeSDK] BannerAdView: IAdRenderOverride detected, invoking override with raw adm.");
                                    if (adRenderOverride.OnAdRenderOverride(_placementId, rawAdm, AdType.Image, respPosRaw))
                                    {
                                        Logger.Info("[BidscubeSDK] BannerAdView: Render override handled by caller, skipping SDK rendering.");
                                        yield break; // Caller handled rendering
                                    }
                                    else
                                    {
                                        Logger.Info("[BidscubeSDK] BannerAdView: Render override returned false, proceeding with default rendering.");
                                    }
                                }

                                // Continue with existing processing using json.adm
                                // Check if position is set (0 is Unknown, so any non-zero value is valid)
                                // Unity JsonUtility will set it to 0 if not present in JSON
                                if (json.position != 0)
                                {
                                    var position = (AdPosition)json.position;
                                    Logger.Info($"[BidscubeSDK] BannerAdView: Received position from server: {json.position} (enum: {position}) - {GetDisplayName(position)}");
                                    BidscubeSDK.SetResponseAdPosition(position);
                                    Logger.Info($"[BidscubeSDK] BannerAdView: SetResponseAdPosition called with: {position}");
                                }
                                else
                                {
                                    Logger.Info("[BidscubeSDK] BannerAdView: No position in response (position=0/Unknown)");
                                }

                                LoadAdContent(UnwrapNestedAdmJson(json.adm));
                            }
                            else
                            {
                                Logger.Info("[BidscubeSDK] BannerAdView: Response looked like JSON but 'adm' was missing. Treating as raw content.");
                                LoadAdContent(UnwrapNestedAdmJson(trimmedResponse));
                            }
                        }
                        catch (System.Exception e)
                        {
                            Logger.Info($"[BidscubeSDK] BannerAdView: JSON parsing failed ('{e.Message}'). Treating as raw content.");
                            LoadAdContent(UnwrapNestedAdmJson(trimmedResponse));
                        }
                    }
                    else
                    {
                        Logger.Info("[BidscubeSDK] BannerAdView: Response is not JSON. Treating as raw content.");
                        LoadAdContent(UnwrapNestedAdmJson(trimmedResponse));
                    }
                }
                else
                {
                    Logger.InfoError($"[BannerAdView] Error: {request.error}");
                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.NetworkError, request.error);
                }
            }
        }

        /// <summary>
        /// Load ad content - Identical to iOS loadAdContent
        /// </summary>
        /// <param name="content">HTML content or JS adm (document.write(...))</param>
        public void LoadAdContent(string content)
        {
            ExtractClickURLFromHTML(content);
            ExtractDimensionsFromHTML(content);

            var trimmedContent = content.Trim();
            string bodyHtml;

            // If content starts with document.write( ... ); we want to extract the "..." part
            if (trimmedContent.StartsWith("document.write(", System.StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info("[BidscubeSDK] BannerAdView: Detected 'document.write' wrapper. Extracting inner HTML.");

                string inner = trimmedContent;

                // Strip leading document.write(
                int openParen = inner.IndexOf('(');
                if (openParen != -1)
                {
                    inner = inner.Substring(openParen + 1).TrimStart();
                }

                // Strip trailing ); if present
                if (inner.EndsWith(");"))
                {
                    inner = inner.Substring(0, inner.Length - 2).TrimEnd();
                }
                else if (inner.EndsWith(")"))
                {
                    inner = inner.Substring(0, inner.Length - 1).TrimEnd();
                }

                // If wrapped in single or double quotes, remove them
                if ((inner.StartsWith("\"") && inner.EndsWith("\"")) ||
                    (inner.StartsWith("'") && inner.EndsWith("'")))
                {
                    inner = inner.Substring(1, inner.Length - 2);
                }

                // Unescape common JS string escapes for HTML
                inner = inner
                    .Replace("\\\"", "\"")  // escaped double quote
                    .Replace("\\'", "'")     // escaped single quote
                    .Replace("\\n", "")      // newlines
                    .Replace("\\r", "")      // carriage returns
                    .Replace("\\t", "")      // tabs
                    .Replace("\\/", "/");    // escaped slash

                bodyHtml = inner;
            }
            else
            {
                Logger.Info("[BidscubeSDK] BannerAdView: Treating content as raw HTML body.");
                bodyHtml = trimmedContent;
            }

            bodyHtml = UnwrapNestedAdmJson(bodyHtml);
            bodyHtml = StripEmbeddedAdmJsonInsideHtml(bodyHtml);

            var slotPos = BidscubeSDK.GetEffectiveAdPosition();
            if (slotPos == AdPosition.Unknown)
                slotPos = BidscubeSDK.GetResponseAdPosition();
            string flexJustify;
            if (BidscubeSDK.AdViewsParentUsesLayoutSlotSizing())
                flexJustify = "flex-start";
            else if (slotPos == AdPosition.Footer)
                flexJustify = "flex-end";
            else
                flexJustify = "flex-start";

            // Native WebView is full-screen; HTML pins the creative along the main axis (overlay).
            // Transparent body uses pointer-events:none so touches pass through except on the ad root.
            string fullHTML = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover'>
    <style>
        html, body {{
            margin: 0;
            padding: 0;
            width: 100%;
            height: 100%;
            min-height: 100%;
            overflow: hidden;
            background-color: transparent;
            display: flex;
            flex-direction: column;
            align-items: {(BidscubeSDK.AdViewsParentUsesLayoutSlotSizing() ? "flex-start" : "center")};
            justify-content: {flexJustify};
            box-sizing: border-box;
            pointer-events: none;
        }}
        .bcc-ad-root {{
            pointer-events: auto;
            flex-shrink: 0;
            max-width: 100%;
            box-sizing: border-box;
        }}
        .bcc-ad-root img, .bcc-ad-root iframe {{
            max-width: 100%;
            max-height: 100%;
            width: auto;
            height: auto;
            display: block;
            object-fit: contain;
        }}
        .bcc-ad-root div, .bcc-ad-root span {{
            max-width: 100%;
            box-sizing: border-box;
        }}
        * {{ box-sizing: border-box; }}
    </style>
</head>
<body>
    <div class=""bcc-ad-root"">
    {bodyHtml}
    </div>
</body>
</html>";

            LogLongResponse("BannerAdView: Final HTML document for WebView", fullHTML, 400);

            // Before loading HTML, check if callback implements IAdRenderOverride
            if (_callback is IAdRenderOverride adRenderOverride)
            {
                Logger.Info("[BidscubeSDK] BannerAdView: IAdRenderOverride detected, calling to check render override.");
                int respPos = (int)BidscubeSDK.GetResponseAdPosition();
                string admForCallback = bodyHtml; // pass the processed HTML/ad content
                // Call the override method, if it returns true, skip the default rendering
                if (adRenderOverride.OnAdRenderOverride(_placementId, admForCallback, AdType.Image, respPos))
                {
                    Logger.Info("[BidscubeSDK] BannerAdView: Render override successful, skipping default rendering.");
                    return; // Skip SDK default rendering
                }
                else
                {
                    Logger.Info("[BidscubeSDK] BannerAdView: Render override returned false, proceeding with default rendering.");
                }
            }

            // At this point we will perform SDK default rendering, create WebViewController lazily
            if (_webViewController == null)
            {
                Logger.Info("[BidscubeSDK] BannerAdView: WebViewController is null, creating on the fly.");
                // Add WebViewController directly to this GameObject (no WebViewHost wrapper)
                _webViewController = gameObject.AddComponent<WebViewController>();
                // Do NOT use full-screen native WebView (margins 0) here: the creative is often a small
                // strip; users see a “blank” screen. Clip the OS WebView to this RectTransform (same
                // idea as NativeAdView). Embedded dock slots also require rect-based margins.
                _webViewController.SetMatchNativeFullscreenMargins(false);
                // Initialize with callbacks - handle clicks via message callback
                _webViewController.Initialize(
                    onHtmlLoaded: null,
                    onError: null,
                    onMessage: OnWebViewMessage
                );
            }

            _webViewController.LoadHTML(fullHTML, "");

            // Refresh WebView margins after loading to ensure it's sized correctly
            if (_webViewController != null)
            {
                _webViewController.RefreshMargins();
            }

            // Update size after loading if dimensions were extracted
            UpdateBannerSize();

            // Refresh WebView margins after size update to ensure correct positioning
            if (_webViewController != null)
            {
                StartCoroutine(DelayedWebViewMarginRefresh());
            }

            // Notify parent AdViewController if it exists
            var adViewController = GetComponentInParent<AdViewController>();
            if (adViewController != null)
            {
                adViewController.MarkAdAsLoaded();
            }

            _callback?.OnAdLoaded(_placementId);
            _callback?.OnAdDisplayed(_placementId);
        }

        private void ExtractClickURLFromHTML(string htmlContent)
        {
            var patterns = new[]
            {
                @"https?://[^""'\\s]+",
                @"curl=([^&""'\\s]+)"
            };

            foreach (var pattern in patterns)
            {
                var regex = new Regex(pattern);
                var match = regex.Match(htmlContent);
                if (match.Success)
                {
                    _clickURL = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    Logger.Info($" BannerAdView: Extracted click URL from HTML: {_clickURL}");
                    return;
                }
            }

            Logger.Info(" BannerAdView: Could not extract click URL from HTML content");
        }

        private void ExtractDimensionsFromHTML(string htmlContent)
        {
            // Priority order: div style > iframe > img (but ignore 1x1 tracking pixels)
            
            // First, try to extract from div style attributes (most reliable for banner ads)
            var divStyleWidthPattern = new Regex(@"<div[^>]*style\s*=\s*[""'][^""']*width\s*:\s*(\d+)px", RegexOptions.IgnoreCase);
            var divStyleHeightPattern = new Regex(@"<div[^>]*style\s*=\s*[""'][^""']*height\s*:\s*(\d+)px", RegexOptions.IgnoreCase);
            
            // Also try style attributes in general
            var styleWidthPattern = new Regex(@"width\s*:\s*(\d+)px", RegexOptions.IgnoreCase);
            var styleHeightPattern = new Regex(@"height\s*:\s*(\d+)px", RegexOptions.IgnoreCase);

            // Try to extract from iframe tags
            var iframeWidthPattern = new Regex(@"<iframe[^>]*width\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);
            var iframeHeightPattern = new Regex(@"<iframe[^>]*height\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);

            // Try to extract width and height from img tags (but ignore 1x1 tracking pixels)
            var imgWidthPattern = new Regex(@"<img[^>]*width\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);
            var imgHeightPattern = new Regex(@"<img[^>]*height\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);

            float extractedWidth = 0f;
            float extractedHeight = 0f;

            // Try div style first (most reliable)
            var divStyleWidthMatch = divStyleWidthPattern.Match(htmlContent);
            var divStyleHeightMatch = divStyleHeightPattern.Match(htmlContent);
            if (!_useConfiguredSize)
            {
                if (divStyleWidthMatch.Success && float.TryParse(divStyleWidthMatch.Groups[1].Value, out extractedWidth))
                {
                    _bannerWidth = extractedWidth;
                }
                if (divStyleHeightMatch.Success && float.TryParse(divStyleHeightMatch.Groups[1].Value, out extractedHeight))
                {
                    _bannerHeight = extractedHeight;
                }
            }

            // If div style didn't work, try general style attributes
            if (extractedWidth == 0f || extractedHeight == 0f)
            {
                var styleWidthMatch = styleWidthPattern.Match(htmlContent);
                var styleHeightMatch = styleHeightPattern.Match(htmlContent);
                if (styleWidthMatch.Success && float.TryParse(styleWidthMatch.Groups[1].Value, out float styleWidth) && styleWidth > 1f)
                {
                    _bannerWidth = styleWidth;
                    extractedWidth = styleWidth;
                }
                if (styleHeightMatch.Success && float.TryParse(styleHeightMatch.Groups[1].Value, out float styleHeight) && styleHeight > 1f)
                {
                    _bannerHeight = styleHeight;
                    extractedHeight = styleHeight;
                }
            }

            // If still not found, try iframe
            if (extractedWidth == 0f || extractedHeight == 0f)
            {
                var iframeWidthMatch = iframeWidthPattern.Match(htmlContent);
                var iframeHeightMatch = iframeHeightPattern.Match(htmlContent);
                if (iframeWidthMatch.Success && float.TryParse(iframeWidthMatch.Groups[1].Value, out extractedWidth))
                {
                    _bannerWidth = extractedWidth;
                }
                if (iframeHeightMatch.Success && float.TryParse(iframeHeightMatch.Groups[1].Value, out extractedHeight))
                {
                    _bannerHeight = extractedHeight;
                }
            }

            // Last resort: try img tags, but ignore 1x1 tracking pixels
            if (extractedWidth == 0f || extractedHeight == 0f)
            {
                var imgWidthMatch = imgWidthPattern.Match(htmlContent);
                var imgHeightMatch = imgHeightPattern.Match(htmlContent);
                if (imgWidthMatch.Success && float.TryParse(imgWidthMatch.Groups[1].Value, out float imgWidth) && imgWidth > 1f)
                {
                    _bannerWidth = imgWidth;
                    extractedWidth = imgWidth;
                }
                if (imgHeightMatch.Success && float.TryParse(imgHeightMatch.Groups[1].Value, out float imgHeight) && imgHeight > 1f)
                {
                    _bannerHeight = imgHeight;
                    extractedHeight = imgHeight;
                }
            }
            
            // Always notify parent if dimensions were set (even if defaults)
            NotifyDimensionsUpdated();
        }

        private string GetDisplayName(AdPosition position)
        {
            switch (position)
            {
                case AdPosition.Unknown: return "UNKNOWN";
                case AdPosition.AboveTheFold: return "ABOVE_THE_FOLD";
                case AdPosition.DependOnScreenSize: return "DEPEND_ON_SCREEN_SIZE";
                case AdPosition.BelowTheFold: return "BELOW_THE_FOLD";
                case AdPosition.Header: return "HEADER";
                case AdPosition.Footer: return "FOOTER";
                case AdPosition.Sidebar: return "SIDEBAR";
                case AdPosition.FullScreen: return "FULL_SCREEN";
                default: return "UNKNOWN";
            }
        }

        /// <summary>
        /// Handle messages from WebViewController (including click events)
        /// </summary>
        private void OnWebViewMessage(string message)
        {
            // WebViewController sends "click:URL" messages when links are clicked
            // The WebViewController already opens the URL in external browser,
            // but we should also notify the callback
            if (message != null && message.StartsWith("click:"))
            {
                OnAdClicked();
            }
        }

        private void OnAdClicked()
        {
            Logger.Info(" BannerAdView: Tap gesture detected");

            _callback?.OnAdClicked(_placementId);

            // Note: WebViewController already opens the URL in external browser
            // via JavaScript interception, so we don't need to do it here again
            // But we keep the clickURL extraction for backward compatibility
            if (!string.IsNullOrEmpty(_clickURL))
            {
                Logger.Info($" BannerAdView: Click URL was: {_clickURL}");
            }
        }

        /// <summary>
        /// Re-run WebView margins after the host layout changes.
        /// </summary>
        public void ReapplyLayout()
        {
            if (_webViewController != null)
                _webViewController.RefreshMargins();
        }

        /// <summary>
        /// Attach banner to screen - Identical to iOS attachToScreen
        /// </summary>
        /// <param name="parentView">Parent view to attach to</param>
        public void AttachToScreen(GameObject parentView)
        {
            if (_isAttachedToScreen) return;

            _parentView = parentView;
            _isAttachedToScreen = true;

            transform.SetParent(parentView.transform, false);

            SetupBannerConstraints();

            // Fade in animation like iOS
            var canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0;
            StartCoroutine(FadeInAnimation());

            Logger.Info($" BannerAdView: Attached to screen at {_bannerPosition}");
        }

        /// <summary>
        /// Detach banner from screen - Identical to iOS detachFromScreen
        /// </summary>
        public void DetachFromScreen()
        {
            // This view is now managed by AdViewController, so detaching is just destroying.
            Destroy(gameObject);
            Logger.Info(" BannerAdView: Detached from screen");
        }

        private IEnumerator FadeInAnimation()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            var duration = 0.3f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1;
        }

        private IEnumerator HideLoadingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            // Loading label removed - no UI elements spawned
        }

        private IEnumerator FadeOutAndDetach()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            var duration = 0.3f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
                yield return null;
            }

            transform.SetParent(null);
            _isAttachedToScreen = false;
            _parentView = null;

            // Untrack banner like iOS
            BidscubeSDK.UntrackBanner(this);
        }

        private void SetupBannerConstraints()
        {
            // This is now handled by AdViewController's ApplyPositioning method.
            // The BannerAdView just fills its parent.
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
        }

        /// <summary>
        /// Set banner dimensions - Identical to iOS setBannerDimensions
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void SetBannerDimensions(float width, float height)
        {
            _bannerWidth = width;
            _bannerHeight = height;
        }

        /// <summary>
        /// Get banner dimensions
        /// </summary>
        /// <returns>Banner dimensions as Vector2 (width, height)</returns>
        public Vector2 GetBannerDimensions()
        {
            // Banner width is always screen width
            float width = Screen.width;
            float height = _bannerHeight > 0 ? _bannerHeight : 100f;
            return new Vector2(width, height);
        }

        /// <summary>
        /// Notify parent AdViewController to update positioning with current dimensions
        /// </summary>
        public void NotifyDimensionsUpdated()
        {
            // Update banner size based on position
            UpdateBannerSize();

            var adViewController = GetComponentInParent<AdViewController>();
            if (adViewController != null)
            {
                // Get effective position (manual/dropdown override takes priority)
                var effectivePosition = BidscubeSDK.GetEffectiveAdPosition();
                if (effectivePosition != AdPosition.Unknown)
                {
                    adViewController.UpdatePosition(effectivePosition);
                }
                else
                {
                    adViewController.MarkAdAsLoaded();
                }
            }
        }

        /// <summary>
        /// Update banner size based on position - if not FullScreen, set explicit size
        /// </summary>
        private void UpdateBannerSize()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            var adViewController = GetComponentInParent<AdViewController>();
            if (adViewController != null && transform != adViewController.transform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                Logger.Info("[BannerAdView] Filling parent ad slot (native WebView is full-screen overlay; slot size from AdViewController)");
                return;
            }

            // Get effective position (manual/dropdown override takes priority)
            var effectivePosition = BidscubeSDK.GetEffectiveAdPosition();
            if (effectivePosition == AdPosition.Unknown)
            {
                effectivePosition = BidscubeSDK.GetResponseAdPosition();
            }

            // If not FullScreen, set explicit size
            if (effectivePosition != AdPosition.FullScreen && effectivePosition != AdPosition.Unknown)
            {
                // Banner width should be screen width
                float width = Screen.width;
                // Use actual banner height if available, otherwise default
                float height = _bannerHeight > 0 ? _bannerHeight : 100f;

                // For Header and Footer positions, clamp height to max 50 Unity units
                if (effectivePosition == AdPosition.Header || effectivePosition == AdPosition.Footer)
                {
                    height = Mathf.Min(height, 50f);
                    Logger.Info($"[BidscubeSDK] BannerAdView: Clamped height to {height} for {effectivePosition} position (original: {_bannerHeight})");
                }

                // Set anchors to center
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(width, height);
                rectTransform.anchoredPosition = Vector2.zero;

                Logger.Info($"[BidscubeSDK] BannerAdView: Set size to {width}x{height} (screen width) for position {effectivePosition}");
            }
            else if (effectivePosition == AdPosition.FullScreen)
            {
                // Full screen - fill parent
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                Logger.Info("[BidscubeSDK] BannerAdView: Set to full screen");
            }
        }

        /// <summary>
        /// Set banner position - Identical to iOS setBannerPosition
        /// </summary>
        /// <param name="position">Ad position</param>
        public void SetBannerPosition(AdPosition position)
        {
            // Deprecated: AdViewController now controls position.
        }

        /// <summary>
        /// Set corner radius - Identical to iOS setCornerRadius
        /// </summary>
        /// <param name="radius">Corner radius</param>
        public void SetCornerRadius(float radius)
        {
            _cornerRadius = radius;
            // Note: Unity UI doesn't have built-in corner radius support
            // This would need to be implemented using a custom shader or third-party solution
        }

        /// <summary>
        /// Show banner
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide banner
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Destroy banner ad view
        /// </summary>
        public void Destroy()
        {
            if (_webViewController != null)
            {
                _webViewController.Destroy();
            }
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Delayed WebView margin refresh to ensure layout is complete
        /// </summary>
        private IEnumerator DelayedWebViewMarginRefresh()
        {
            yield return new WaitForEndOfFrame(); // Wait for frame to complete
            yield return new WaitForEndOfFrame(); // Wait another frame for layout
            Canvas.ForceUpdateCanvases(); // Force canvas update
            if (_webViewController != null)
            {
                _webViewController.RefreshMargins();
            }
        }
    }
}

