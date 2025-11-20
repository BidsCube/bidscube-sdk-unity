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
            public int? position;
        }

        [SerializeField] private RawImage _webView;
        [SerializeField] private Text _loadingLabel;
        [SerializeField] private Button _clickButton;
        [SerializeField] private Canvas _canvas;

        private string _placementId;
        private IAdCallback _callback;
        private AdPosition _bannerPosition = AdPosition.Header;
        private bool _isLoaded = false;
        private float _cornerRadius = 0f;
        private float _bannerHeight = 40f;
        private float _bannerWidth = 320f;
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

            SetupLoadingLabel();
            SetupWebView();
        }

        private void SetupLoadingLabel()
        {
            var loadingObj = new GameObject("LoadingLabel");
            loadingObj.transform.SetParent(transform);
            _loadingLabel = loadingObj.AddComponent<Text>();
            _loadingLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _loadingLabel.fontSize = 12;
            _loadingLabel.color = Color.white;
            _loadingLabel.alignment = TextAnchor.MiddleCenter;
            _loadingLabel.text = "Loading Banner...";
        }

        private void SetupWebView()
        {
            // Ensure this GameObject has a RectTransform (required for UI components)
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // Instead of only a RawImage, create a child for WebViewController
            var webViewHost = new GameObject("WebViewHost");
            webViewHost.transform.SetParent(transform, false);
            webViewHost.transform.localScale = Vector3.one; // Ensure scale is 1,1,1

            // Ensure RectTransform is available and configured to fill parent
            var webViewRect = webViewHost.GetComponent<RectTransform>();
            if (webViewRect == null)
            {
                webViewRect = webViewHost.AddComponent<RectTransform>();
            }

            // Configure RectTransform to fill the parent container
            webViewRect.anchorMin = Vector2.zero;
            webViewRect.anchorMax = Vector2.one;
            webViewRect.offsetMin = Vector2.zero;
            webViewRect.offsetMax = Vector2.zero;

            _webViewController = webViewHost.AddComponent<WebViewController>();
            _webViewController.Initialize();

            // RawImage is not needed since we already have Image component from SetupBannerView
            // and WebViewController handles the actual rendering
            // _webView is kept as null - it's not used for WebView rendering anymore
        }

        private void SetupConstraints()
        {
            // The WebView should fill the entire BannerAdView, which in turn
            // fills the AdViewController's RectTransform.
            var webViewRect = _webViewController.GetComponent<RectTransform>();
            var loadingRect = _loadingLabel.GetComponent<RectTransform>();

            if (webViewRect != null)
            {
                webViewRect.anchorMin = Vector2.zero;
                webViewRect.anchorMax = Vector2.one;
                webViewRect.offsetMin = Vector2.zero;
                webViewRect.offsetMax = Vector2.zero;
            }

            if (loadingRect != null)
            {
                loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
                loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
                loadingRect.sizeDelta = new Vector2(100, 24);
                loadingRect.anchoredPosition = Vector2.zero;
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
            _loadingLabel.gameObject.SetActive(true);
            _loadingLabel.text = "Loading Banner...";

            Logger.Info($" BannerAdView: Making HTTP request to: {url}");
            StartCoroutine(LoadAdCoroutine(url));
        }

        private IEnumerator LoadAdCoroutine(string url)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var responseText = request.downloadHandler.text;
                    Logger.Info($"[BidscubeSDK] BannerAdView: Received response ({responseText.Length} chars): {responseText}");

                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        _loadingLabel.text = "Error: Empty Response";
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

                                if (json.position.HasValue)
                                {
                                    var position = (AdPosition)json.position.Value;
                                    Logger.Info($"[BidscubeSDK] BannerAdView: Received position from server: {json.position} - {GetDisplayName(position)}");
                                    BidscubeSDK.SetResponseAdPosition(position);
                                }

                                LoadAdContent(json.adm);
                            }
                            else
                            {
                                Logger.Info("[BidscubeSDK] BannerAdView: Response looked like JSON but 'adm' was missing. Treating as raw content.");
                                LoadAdContent(trimmedResponse);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Logger.Info($"[BidscubeSDK] BannerAdView: JSON parsing failed ('{e.Message}'). Treating as raw content.");
                            LoadAdContent(trimmedResponse);
                        }
                    }
                    else
                    {
                        Logger.Info("[BidscubeSDK] BannerAdView: Response is not JSON. Treating as raw content.");
                        LoadAdContent(trimmedResponse);
                    }
                }
                else
                {
                    _loadingLabel.text = $"Error: {request.error}";
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
            _loadingLabel.gameObject.SetActive(true);
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

            // Banner width should be screen width
            float bannerWidth = Screen.width;
            float bannerHeight = _bannerHeight > 0 ? _bannerHeight : 40f;

            string fullHTML = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width={bannerWidth}, initial-scale=1.0, user-scalable=no'>
    <style>
        html, body {{
            margin: 0;
            padding: 0;
            width: 100%;
            height: 100%;
            overflow: hidden;
            background-color: transparent;
            display: flex;
            justify-content: center;
            align-items: center;
            box-sizing: border-box;
        }}
        img, iframe {{
            max-width: 100%;
            max-height: 100%;
            width: auto;
            height: auto;
            display: block;
            object-fit: contain;
        }}
        div, span {{
            max-width: 100%;
            max-height: 100%;
            box-sizing: border-box;
        }}
        /* Ensure content fits within container, not viewport */
        * {{
            box-sizing: border-box;
        }}
    </style>
</head>
<body>
    {bodyHtml}
</body>
</html>";

            Logger.Info($"[BidscubeSDK] BannerAdView: Final HTML to load: {fullHTML}");

            if (_webViewController == null)
            {
                Logger.Info("[BidscubeSDK] BannerAdView: WebViewController is null, creating on the fly.");
                var webViewHost = new GameObject("WebViewHost");
                webViewHost.transform.SetParent(transform, false);
                webViewHost.transform.localScale = Vector3.one; // Ensure scale is 1,1,1

                // Ensure RectTransform is available
                var webViewRect = webViewHost.GetComponent<RectTransform>();
                if (webViewRect == null)
                {
                    webViewRect = webViewHost.AddComponent<RectTransform>();
                }
                // Setup RectTransform to fill parent
                webViewRect.anchorMin = Vector2.zero;
                webViewRect.anchorMax = Vector2.one;
                webViewRect.offsetMin = Vector2.zero;
                webViewRect.offsetMax = Vector2.zero;

                _webViewController = webViewHost.AddComponent<WebViewController>();
                _webViewController.Initialize();
            }

            _webViewController.LoadHTML(fullHTML, "");

            // Refresh WebView margins after loading to ensure it's sized correctly
            if (_webViewController != null)
            {
                _webViewController.RefreshMargins();
            }

            _isLoaded = true;
            _loadingLabel.gameObject.SetActive(false);

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
            // Try to extract width and height from img tags
            var imgWidthPattern = new Regex(@"<img[^>]*width\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);
            var imgHeightPattern = new Regex(@"<img[^>]*height\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);

            // Try to extract from iframe tags
            var iframeWidthPattern = new Regex(@"<iframe[^>]*width\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);
            var iframeHeightPattern = new Regex(@"<iframe[^>]*height\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);

            // Try to extract from style attributes
            var styleWidthPattern = new Regex(@"width\s*:\s*(\d+)px", RegexOptions.IgnoreCase);
            var styleHeightPattern = new Regex(@"height\s*:\s*(\d+)px", RegexOptions.IgnoreCase);

            float extractedWidth = 0f;
            float extractedHeight = 0f;

            // Try img tag first
            var imgWidthMatch = imgWidthPattern.Match(htmlContent);
            var imgHeightMatch = imgHeightPattern.Match(htmlContent);
            if (imgWidthMatch.Success && float.TryParse(imgWidthMatch.Groups[1].Value, out extractedWidth))
            {
                _bannerWidth = extractedWidth;
            }
            if (imgHeightMatch.Success && float.TryParse(imgHeightMatch.Groups[1].Value, out extractedHeight))
            {
                _bannerHeight = extractedHeight;
            }

            // If img didn't work, try iframe
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

            // If still not found, try style attributes
            if (extractedWidth == 0f || extractedHeight == 0f)
            {
                var styleWidthMatch = styleWidthPattern.Match(htmlContent);
                var styleHeightMatch = styleHeightPattern.Match(htmlContent);
                if (styleWidthMatch.Success && float.TryParse(styleWidthMatch.Groups[1].Value, out extractedWidth))
                {
                    _bannerWidth = extractedWidth;
                }
                if (styleHeightMatch.Success && float.TryParse(styleHeightMatch.Groups[1].Value, out extractedHeight))
                {
                    _bannerHeight = extractedHeight;
                }
            }

            if (_bannerWidth > 0f && _bannerHeight > 0f)
            {
                Logger.Info($"[BidscubeSDK] BannerAdView: Extracted dimensions from HTML: {_bannerWidth}x{_bannerHeight}");
                // Notify parent if dimensions were successfully extracted
                NotifyDimensionsUpdated();
            }
            else
            {
                Logger.Info($"[BidscubeSDK] BannerAdView: Could not extract dimensions from HTML, using defaults: {_bannerWidth}x{_bannerHeight}");
            }
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

        private void OnAdClicked()
        {
            Logger.Info(" BannerAdView: Tap gesture detected");

            _callback?.OnAdClicked(_placementId);

            if (!string.IsNullOrEmpty(_clickURL))
            {
                Logger.Info($" BannerAdView: Opening extracted click URL: {_clickURL}");
                Application.OpenURL(_clickURL);
            }
            else
            {
                Logger.Info(" BannerAdView: No click URL available to open");
            }
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
            _loadingLabel.gameObject.SetActive(false);
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
            float height = _bannerHeight > 0 ? _bannerHeight : 40f;
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
                float height = _bannerHeight > 0 ? _bannerHeight : 40f;

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

