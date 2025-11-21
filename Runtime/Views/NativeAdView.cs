using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;
using System;

namespace BidscubeSDK
{
    /// <summary>
    /// Native ad view component
    /// </summary>
    public class NativeAdView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Image _mainImage;
        [SerializeField] private Button _installButton;
        [SerializeField] private Text _installButtonText;
        [SerializeField] private Text _sponsoredText;

        private string _placementId;
        private IAdCallback _callback;
        private bool _isLoaded = false;
        private NativeAdData _adData;

        private const float LogicalWidth = 300f;
        private const float LogicalHeight = 40f;
        private float _nativeAdWidth = LogicalWidth;
        private float _nativeAdHeight = LogicalHeight;

        private WebViewController _webViewController;
        private bool _useWebViewRendering = true; // toggle if needed

        [System.Serializable]
        private class OpenRtbNativeRoot { public OpenRtbNativePayload native; }

        [System.Serializable]
        private class AdmWrapper { public string adm; }

        [System.Serializable]
        private class OpenRtbNativePayload
        {
            public string ver;
            public OpenRtbAsset[] assets;
            public OpenRtbLink link;
        }

        [System.Serializable]
        private class OpenRtbAsset
        {
            public int id;
            public OpenRtbTitle title;
            public OpenRtbData data;
            public OpenRtbImage img;
        }

        [System.Serializable]
        private class OpenRtbTitle { public string text; }

        [System.Serializable]
        private class OpenRtbData { public string value; }

        [System.Serializable]
        private class OpenRtbImage { public string url; public int w; public int h; }

        [System.Serializable]
        private class AdResponse
        {
            public AdResponseInner adm; // Handle nested adm object: {"adm":{"adm":"{...native JSON...}"}}
            public int? position;
            public int? width;
            public int? height;
        }

        [System.Serializable]
        private class AdResponseInner
        {
            public string adm; // The actual native JSON string
            public int? position;
        }

        [System.Serializable]
        private class OpenRtbLink { public string url; }

        private void Awake()
        {
            // Ensure root has RectTransform but do NOT force full-screen anchoring here;
            // let the parent (AdViewController or user) decide the outer size.
            var rootRect = GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = gameObject.AddComponent<RectTransform>();
            }

            // Default: center 300x250 card in parent if parent size is not set explicitly.
            if (rootRect.anchorMin == rootRect.anchorMax && rootRect.sizeDelta == Vector2.zero)
            {
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.sizeDelta = new Vector2(LogicalWidth, LogicalHeight);
                rootRect.anchoredPosition = Vector2.zero;
            }

            SetupUI();
            ApplyLayout(rootRect);

            // Initialize WebView host for HTML-based native rendering
            if (_useWebViewRendering)
            {
                var webViewHost = new GameObject("NativeWebViewHost");
                webViewHost.transform.SetParent(transform, false);
                webViewHost.transform.localScale = Vector3.one; // Ensure scale is 1,1,1

                // Add RectTransform BEFORE initializing WebViewController
                var r = webViewHost.GetComponent<RectTransform>();
                if (r == null) r = webViewHost.AddComponent<RectTransform>();
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;

                // Now initialize WebViewController (it will find the RectTransform)
                _webViewController = webViewHost.AddComponent<WebViewController>();
                _webViewController.Initialize();
            }
        }

        // Build responsive HTML for WebView-based native rendering
        private string BuildNativeHtml(NativeAdData data)
        {
            if (data == null) return string.Empty;

            string Escape(string s) => string.IsNullOrEmpty(s)
                ? string.Empty
                : s
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;");

            string title = Escape(data.title);
            string desc = Escape(data.description);
            string cta = Escape(string.IsNullOrEmpty(data.installButtonText) ? "Learn more" : data.installButtonText);
            string main = data.mainImageUrl ?? string.Empty;
            string icon = data.iconUrl ?? string.Empty;
            string click = data.storeUrl ?? "#";

            // Get actual native ad dimensions for proper sizing
            float nativeWidth = _nativeAdWidth > 0 ? _nativeAdWidth : LogicalWidth;
            float nativeHeight = _nativeAdHeight > 0 ? _nativeAdHeight : LogicalHeight;

            // Get effective position to determine layout
            var effectivePosition = BidscubeSDK.GetEffectiveAdPosition();
            if (effectivePosition == AdPosition.Unknown)
            {
                effectivePosition = BidscubeSDK.GetResponseAdPosition();
            }
            bool isFullScreen = effectivePosition == AdPosition.FullScreen;
            bool isHorizontal = effectivePosition == AdPosition.Header || effectivePosition == AdPosition.Footer;
            bool isVertical = effectivePosition == AdPosition.Sidebar;

            // For Header/Footer, use clamped height (max 50px) for HTML viewport
            if (isHorizontal)
            {
                nativeHeight = Mathf.Min(nativeHeight, 50f);
            }

            // Determine layout based on position and size
            string layoutClass = "layout-default";
            if (isFullScreen)
            {
                layoutClass = "layout-fullscreen";
            }
            else if (isHorizontal)
            {
                layoutClass = "layout-horizontal";
            }
            else if (isVertical)
            {
                layoutClass = "layout-vertical";
            }

            // Determine if image is too big (aspect ratio > 2:1 or < 1:2)
            float aspectRatio = nativeWidth > 0 && nativeHeight > 0 ? nativeWidth / nativeHeight : 1f;
            bool imageTooWide = aspectRatio > 2f;
            bool imageTooTall = aspectRatio < 0.5f;

            // Add size class for responsive adjustments
            string sizeClass = "size-normal";
            if (nativeWidth < 200 || nativeHeight < 50)
            {
                sizeClass = "size-small";
            }
            else if (nativeWidth > 600 || nativeHeight > 400)
            {
                sizeClass = "size-large";
            }

            return $@"<!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width={nativeWidth}, height={nativeHeight}, initial-scale=1.0, user-scalable=no'>
            <style>
                html, body {{
                    margin: 0;
                    padding: 0;
                    width: 100%;
                    height: 100%;
                    background: transparent;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                    box-sizing: border-box;
                }}
                .card {{
                    box-sizing: border-box;
                    width: 100%;
                    height: 100%;
                    display: flex;
                    background: #ffffff;
                    overflow: hidden;
                    position: relative;
                }}
                
                /* Default layout: vertical (image top, content bottom) */
                .card {{
                    flex-direction: column;
                }}
                .image {{
                    flex: 3;
                    min-height: 150px;
                    background-image: url('{main}');
                    background-position: center;
                    background-size: cover;
                    background-repeat: no-repeat;
                }}
                .content {{
                    flex: 2;
                    padding: 8px;
                    display: flex;
                    flex-direction: column;
                    justify-content: space-between;
                }}
                
                /* Fullscreen layout: large image, more content space */
                .layout-fullscreen .image {{
                    flex: 4;
                    min-height: 300px;
                }}
                .layout-fullscreen .content {{
                    flex: 3;
                    padding: 16px;
                }}
                .layout-fullscreen .title {{
                    font-size: 24px;
                }}
                .layout-fullscreen .desc {{
                    font-size: 16px;
                }}
                .layout-fullscreen .icon {{
                    width: 64px;
                    height: 64px;
                }}
                .layout-fullscreen .cta {{
                    padding: 10px 24px;
                    font-size: 16px;
                }}
                
                /* Horizontal layout: image left, content right (for Header/Footer) */
                .layout-horizontal {{
                    flex-direction: row;
                    align-items: center;
                }}
                .layout-horizontal .image {{
                    flex: 0 0 auto;
                    width: 80px;
                    height: 100%;
                    min-width: 60px;
                    max-width: 120px;
                    min-height: auto;
                }}
                .layout-horizontal .content {{
                    flex: 1 1 auto;
                    padding: 4px 8px;
                    min-width: 0;
                    overflow: hidden;
                }}
                .layout-horizontal .title {{
                    font-size: 14px;
                    margin-bottom: 2px;
                    -webkit-line-clamp: 1;
                }}
                .layout-horizontal .desc {{
                    font-size: 11px;
                    margin-bottom: 4px;
                    -webkit-line-clamp: 1;
                }}
                .layout-horizontal .icon {{
                    width: 32px;
                    height: 32px;
                    margin-right: 6px;
                }}
                .layout-horizontal .cta {{
                    padding: 4px 12px;
                    font-size: 11px;
                }}
                .layout-horizontal .footer {{
                    margin-top: 2px;
                }}
                
                /* Vertical layout: image top, content bottom (for Sidebar) */
                .layout-vertical {{
                    flex-direction: column;
                }}
                .layout-vertical .image {{
                    flex: 2;
                    min-height: 120px;
                }}
                .layout-vertical .content {{
                    flex: 3;
                    padding: 8px;
                }}
                
                /* Size adjustments */
                .size-small .title {{
                    font-size: 14px;
                }}
                .size-small .desc {{
                    font-size: 12px;
                }}
                .size-small .icon {{
                    width: 36px;
                    height: 36px;
                }}
                .size-small .cta {{
                    padding: 4px 12px;
                    font-size: 12px;
                }}
                .size-small .content {{
                    padding: 6px;
                }}
                
                .size-large .title {{
                    font-size: 22px;
                }}
                .size-large .desc {{
                    font-size: 16px;
                }}
                .size-large .icon {{
                    width: 56px;
                    height: 56px;
                }}
                .size-large .cta {{
                    padding: 8px 20px;
                    font-size: 16px;
                }}
                
                /* Image size handling - if image is too wide or too tall */
                {(imageTooWide ? @"
                .image {
                    background-size: contain !important;
                    background-position: center !important;
                }" : "")}
                {(imageTooTall ? @"
                .image {
                    background-size: cover !important;
                    background-position: center top !important;
                }" : "")}
                
                .title {{
                    font-size: 18px;
                    font-weight: 700;
                    margin-bottom: 4px;
                    color: #111;
                    line-height: 1.2;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    display: -webkit-box;
                    -webkit-line-clamp: 2;
                    -webkit-box-orient: vertical;
                }}
                .desc {{
                    font-size: 14px;
                    color: #555;
                    margin-bottom: 8px;
                    line-height: 1.4;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    display: -webkit-box;
                    -webkit-line-clamp: 3;
                    -webkit-box-orient: vertical;
                }}
                .footer {{
                    display: flex;
                    align-items: center;
                    justify-content: space-between;
                    gap: 8px;
                }}
                .cta {{
                    padding: 6px 16px;
                    background: #007aff;
                    color: #fff;
                    border-radius: 9999px;
                    font-size: 14px;
                    text-align: center;
                    white-space: nowrap;
                    flex-shrink: 0;
                }}
                .icon {{
                    width: 48px;
                    height: 48px;
                    border-radius: 10px;
                    background-image: url('{icon}');
                    background-position: center;
                    background-size: cover;
                    background-repeat: no-repeat;
                    margin-right: 12px;
                    flex-shrink: 0;
                }}
                .sponsored {{
                    position: absolute;
                    top: 8px;
                    right: 12px;
                    font-size: 10px;
                    color: #777;
                    background: rgba(255, 255, 255, 0.8);
                    padding: 2px 6px;
                    border-radius: 4px;
                    z-index: 10;
                }}
                a {{
                    text-decoration: none;
                    color: inherit;
                }}
            </style>
        </head>
        <body>
            <a href='{click}' class='card {layoutClass} {sizeClass}'>
                <div class='sponsored'>Sponsored</div>
                <div class='image'></div>
                <div class='content'>
                    <div class='title'>{title}</div>
                    <div class='desc'>{desc}</div>
                    <div class='footer'>
                        <div class='icon'></div>
                        <div class='cta'>{cta}</div>
                    </div>
                </div>
            </a>
        </body>
        </html>";
        }


        private void SetupUI()
        {
            // Helper to create a UI child with RectTransform
            RectTransform CreateChild(string name, Transform parent)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return go.GetComponent<RectTransform>();
            }

            var rootRect = GetComponent<RectTransform>();

            // Icon image
            if (_iconImage == null)
            {
                var iconRect = CreateChild("IconImage", transform);
                _iconImage = iconRect.gameObject.AddComponent<Image>();
            }

            // Title text
            if (_titleText == null)
            {
                var titleRect = CreateChild("TitleText", transform);
                _titleText = titleRect.gameObject.AddComponent<Text>();
                _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _titleText.fontSize = 18;
                _titleText.fontStyle = FontStyle.Bold;
                _titleText.color = Color.black;
            }

            // Description text
            if (_descriptionText == null)
            {
                var descRect = CreateChild("DescriptionText", transform);
                _descriptionText = descRect.gameObject.AddComponent<Text>();
                _descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _descriptionText.fontSize = 14;
                _descriptionText.color = Color.gray;
            }

            // Main image
            if (_mainImage == null)
            {
                var mainRect = CreateChild("MainImage", transform);
                _mainImage = mainRect.gameObject.AddComponent<Image>();
                _mainImage.color = Color.black;
            }

            // Install button
            if (_installButton == null)
            {
                var buttonRect = CreateChild("InstallButton", transform);
                _installButton = buttonRect.gameObject.AddComponent<Button>();
                _installButton.onClick.AddListener(OnInstallButtonClicked);
            }

            // Install button text
            if (_installButtonText == null)
            {
                var buttonTextRect = CreateChild("InstallButtonText", _installButton.transform);
                _installButtonText = buttonTextRect.gameObject.AddComponent<Text>();
                _installButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _installButtonText.fontSize = 16;
                _installButtonText.color = Color.white;
                _installButtonText.alignment = TextAnchor.MiddleCenter;
                _installButtonText.text = "Install";
            }

            // Sponsored text
            if (_sponsoredText == null)
            {
                var sponsoredRect = CreateChild("SponsoredText", transform);
                _sponsoredText = sponsoredRect.gameObject.AddComponent<Text>();
                _sponsoredText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _sponsoredText.fontSize = 10;
                _sponsoredText.color = Color.gray;
                _sponsoredText.text = "Sponsored";
            }
        }

        private void ApplyLayout(RectTransform rootRect)
        {
            // Use normalized anchors within the root rect so this card can scale with any parent size.
            RectTransform GetRect(Graphic g) => g != null ? g.GetComponent<RectTransform>() : null;

            var mainRect = GetRect(_mainImage);
            var iconRect = GetRect(_iconImage);
            var titleRect = GetRect(_titleText);
            var descRect = GetRect(_descriptionText);
            var buttonRect = _installButton != null ? _installButton.GetComponent<RectTransform>() : null;
            var sponsoredRect = GetRect(_sponsoredText);

            void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
            {
                if (rt == null) return;
                rt.anchorMin = min;
                rt.anchorMax = max;
                rt.offsetMin = offsetMin;
                rt.offsetMax = offsetMax;
            }

            // Treat the root as a 300x250 card, but use normalized vertical bands so it
            // stays proportional when the parent size changes.
            // Main image: top 55%
            SetAnchors(mainRect,
                new Vector2(0f, 0.45f),  // bottom 45%
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero);

            // Icon: small area in top-left over image
            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(0f, 0.75f);
                iconRect.anchorMax = new Vector2(0f, 0.95f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = new Vector2(10f, -10f);
                iconRect.sizeDelta = new Vector2(50f, 50f);
            }

            // Title: band below image
            SetAnchors(titleRect,
                new Vector2(0f, 0.35f),
                new Vector2(1f, 0.45f),
                new Vector2(10f, 0f),
                new Vector2(-10f, 0f));

            // Description: below title
            SetAnchors(descRect,
                new Vector2(0f, 0.20f),
                new Vector2(1f, 0.35f),
                new Vector2(10f, 0f),
                new Vector2(-10f, 0f));

            // Install button: bottom center band
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0.35f, 0.05f);
                buttonRect.anchorMax = new Vector2(0.65f, 0.15f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
            }

            // Ensure button text fills button
            if (_installButtonText != null)
            {
                var tr = _installButtonText.GetComponent<RectTransform>();
                if (tr != null)
                {
                    tr.anchorMin = Vector2.zero;
                    tr.anchorMax = Vector2.one;
                    tr.offsetMin = Vector2.zero;
                    tr.offsetMax = Vector2.zero;
                }
            }

            // Sponsored label: top-right corner
            if (sponsoredRect != null)
            {
                sponsoredRect.anchorMin = new Vector2(1f, 1f);
                sponsoredRect.anchorMax = new Vector2(1f, 1f);
                sponsoredRect.pivot = new Vector2(1f, 1f);
                sponsoredRect.anchoredPosition = new Vector2(-10f, -10f);
                sponsoredRect.sizeDelta = new Vector2(90f, 20f);
            }
        }

        /// <summary>
        /// Set placement info
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public void SetPlacementInfo(string placementId, IAdCallback callback)
        {
            _placementId = placementId;
            _callback = callback;
        }

        /// <summary>
        /// Load native ad from URL
        /// </summary>
        /// <param name="url">Ad URL</param>
        public void LoadNativeAdFromURL(string url)
        {
            StartCoroutine(LoadNativeAdCoroutine(url));
        }

        private IEnumerator LoadNativeAdCoroutine(string url)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var json = request.downloadHandler.text;
                    Logger.Info($"[BidscubeSDK] NativeAdView: Response length={json.Length}, preview={json.Substring(0, Mathf.Min(200, json.Length))}");

                    try
                    {
                        // Try to extract position and dimensions from response
                        ExtractPositionAndDimensionsFromResponse(json);

                        string admJson = ExtractAdmJson(json);
                        var parsed = ParseOpenRtbNative(admJson);
                        if (parsed != null)
                        {
                            _adData = parsed;
                            // Extract dimensions from OpenRTB image assets
                            ExtractDimensionsFromOpenRtb(parsed);
                        }
                        else
                        {
                            _adData = !string.IsNullOrEmpty(admJson)
                                ? JsonUtility.FromJson<NativeAdData>(admJson)
                                : JsonUtility.FromJson<NativeAdData>(json);

                            // Unescape Unicode sequences in text fields
                            if (_adData != null)
                            {
                                if (!string.IsNullOrEmpty(_adData.title))
                                    _adData.title = UnescapeUnicode(_adData.title);
                                if (!string.IsNullOrEmpty(_adData.description))
                                    _adData.description = UnescapeUnicode(_adData.description);
                                if (!string.IsNullOrEmpty(_adData.installButtonText))
                                    _adData.installButtonText = UnescapeUnicode(_adData.installButtonText);
                                if (!string.IsNullOrEmpty(_adData.advertiser))
                                    _adData.advertiser = UnescapeUnicode(_adData.advertiser);
                            }
                        }

                        if (_adData == null)
                        {
                            throw new System.Exception("Failed to parse native ad data");
                        }

                        Logger.Info($"[BidscubeSDK] NativeAdView: Parsed ad data - title='{_adData.title}', mainImage='{_adData.mainImageUrl}', icon='{_adData.iconUrl}', cta='{_adData.installButtonText}'");

                        PopulateAdData();
                        _isLoaded = true;

                        // Update size after loading if dimensions were extracted
                        UpdateNativeAdSize();

                        if (_useWebViewRendering && _webViewController != null)
                        {
                            var html = BuildNativeHtml(_adData);
                            _webViewController.LoadHTML(html, "");

                            // Refresh WebView margins after loading to ensure it's sized correctly
                            if (_webViewController != null)
                            {
                                StartCoroutine(DelayedWebViewMarginRefresh());
                            }
                            SetLegacyUiVisible(false);
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
                    catch (System.Exception e)
                    {
                        Logger.InfoError($"[BidscubeSDK] NativeAdView: Parse error: {e.Message}\\nJSON={json}");
                        _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.InvalidResponse, e.Message);
                    }
                }
                else
                {
                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.NetworkError, request.error);
                }
            }
        }

        private void SetLegacyUiVisible(bool visible)
        {
            if (_iconImage != null) _iconImage.gameObject.SetActive(visible);
            if (_titleText != null) _titleText.gameObject.SetActive(visible);
            if (_descriptionText != null) _descriptionText.gameObject.SetActive(visible);
            if (_mainImage != null) _mainImage.gameObject.SetActive(visible);
            if (_installButton != null) _installButton.gameObject.SetActive(visible);
            if (_sponsoredText != null) _sponsoredText.gameObject.SetActive(visible);
        }

        private string ExtractAdmJson(string responseJson)
        {
            if (string.IsNullOrEmpty(responseJson)) return responseJson;

            try
            {
                // First try to parse as AdResponse with nested adm structure
                var response = JsonUtility.FromJson<AdResponse>(responseJson);
                if (response != null && response.adm != null && !string.IsNullOrEmpty(response.adm.adm))
                {
                    Logger.Info("[BidscubeSDK] NativeAdView: Found nested adm structure");
                    var adm = response.adm.adm.Trim();

                    // Unescape JSON string escapes
                    adm = adm.Replace("\\\"", "\"")
                             .Replace("\\n", "\n")
                             .Replace("\\r", "\r")
                             .Replace("\\t", "\t")
                             .Replace("\\/", "/");

                    // Unescape Unicode sequences (e.g., \u00E4 -> ä, \u00FC -> ü)
                    adm = UnescapeUnicode(adm);

                    return adm;
                }
            }
            catch
            {
                // Try fallback with AdmWrapper
            }

            try
            {
                var wrapper = JsonUtility.FromJson<AdmWrapper>(responseJson);
                if (wrapper != null && !string.IsNullOrEmpty(wrapper.adm))
                {
                    var adm = wrapper.adm.Trim();

                    if (adm.StartsWith("\"") && adm.EndsWith("\""))
                    {
                        adm = adm.Substring(1, adm.Length - 2);
                    }

                    adm = adm.Replace("\\\"", "\"")
                             .Replace("\\n", "\n")
                             .Replace("\\r", "\r")
                             .Replace("\\t", "\t")
                             .Replace("\\/", "/");

                    // Unescape Unicode sequences
                    adm = UnescapeUnicode(adm);

                    return adm;
                }
            }
            catch
            {
                // ignore and fall back
            }

            return responseJson;
        }

        private NativeAdData ParseOpenRtbNative(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            json = json.Trim();

            if (json.StartsWith("\"") && json.EndsWith("\""))
            {
                json = json.Substring(1, json.Length - 2);
                json = json.Replace("\\\"", "\"");
            }

            if (!json.Contains("\"native\""))
                return null;

            try
            {
                var root = JsonUtility.FromJson<OpenRtbNativeRoot>(json);
                if (root == null || root.native == null || root.native.assets == null)
                    return null;

                var result = new NativeAdData();

                foreach (var asset in root.native.assets)
                {
                    switch (asset.id)
                    {
                        case 2:
                            if (asset.title != null && !string.IsNullOrEmpty(asset.title.text))
                                result.title = UnescapeUnicode(asset.title.text);
                            break;
                        case 6:
                            if (asset.data != null && !string.IsNullOrEmpty(asset.data.value))
                                result.description = UnescapeUnicode(asset.data.value);
                            break;
                        case 1:
                            if (asset.data != null && !string.IsNullOrEmpty(asset.data.value))
                                result.installButtonText = UnescapeUnicode(asset.data.value);
                            break;
                        case 4:
                            if (asset.img != null && !string.IsNullOrEmpty(asset.img.url))
                            {
                                result.mainImageUrl = asset.img.url.Replace("\\u0026", "&");
                                // Extract dimensions from main image
                                if (asset.img.w > 0 && asset.img.h > 0)
                                {
                                    _nativeAdWidth = asset.img.w;
                                    _nativeAdHeight = asset.img.h;
                                    Logger.Info($"[BidscubeSDK] NativeAdView: Extracted dimensions from OpenRTB main image: {_nativeAdWidth}x{_nativeAdHeight}");
                                }
                            }
                            break;
                        case 3:
                            if (asset.img != null && !string.IsNullOrEmpty(asset.img.url))
                                result.iconUrl = asset.img.url.Replace("\\u0026", "&");
                            break;
                    }
                }

                if (root.native.link != null && !string.IsNullOrEmpty(root.native.link.url))
                {
                    result.storeUrl = root.native.link.url.Replace("\\u0026", "&");
                }

                if (string.IsNullOrEmpty(result.installButtonText))
                    result.installButtonText = "Learn more";

                return result;
            }
            catch (System.Exception e)
            {
                Logger.Info($"[BidscubeSDK] NativeAdView: OpenRTB parse failed: {e.Message}\\nJSON={json}");
                return null;
            }
        }

        /// <summary>
        /// Unescape Unicode escape sequences in JSON strings (e.g., \u00E4 -> ä, \u00FC -> ü)
        /// </summary>
        private string UnescapeUnicode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Unescape Unicode sequences (e.g., \u00E4 -> ä)
            return Regex.Replace(
                text,
                @"\\u([0-9A-Fa-f]{4})",
                m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString()
            );
        }

        private void PopulateAdData()
        {
            if (_adData == null) return;

            if (_titleText != null && !string.IsNullOrEmpty(_adData.title))
                _titleText.text = _adData.title; // Already unescaped in ParseOpenRtbNative or LoadNativeAdCoroutine

            if (_descriptionText != null && !string.IsNullOrEmpty(_adData.description))
                _descriptionText.text = _adData.description; // Already unescaped

            if (_installButtonText != null && !string.IsNullOrEmpty(_adData.installButtonText))
                _installButtonText.text = _adData.installButtonText; // Already unescaped

            if (!string.IsNullOrEmpty(_adData.iconUrl))
                StartCoroutine(LoadImage(_adData.iconUrl, _iconImage));

            if (!string.IsNullOrEmpty(_adData.mainImageUrl))
                StartCoroutine(LoadImage(_adData.mainImageUrl, _mainImage));
        }

        private void OnInstallButtonClicked()
        {
            if (_isLoaded && _adData != null)
            {
                _callback?.OnInstallButtonClicked(_placementId, _adData.installButtonText);

                if (!string.IsNullOrEmpty(_adData.storeUrl))
                    Application.OpenURL(_adData.storeUrl);
            }
        }

        private IEnumerator LoadImage(string url, Image image)
        {
            using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                var asyncOp = request.SendWebRequest();

                while (!asyncOp.isDone)
                    yield return null;

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
                    if (texture != null)
                    {
                        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        image.sprite = sprite;
                    }
                }
                else
                {
                    Logger.InfoError($"[BidscubeSDK] NativeAdView: Image load error: {request.error}");
                }
            }
        }

        /// <summary>
        /// Set native ad size
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void SetNativeAdSize(float width, float height)
        {
            _nativeAdWidth = width;
            _nativeAdHeight = height;
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }

        /// <summary>
        /// Get native ad dimensions
        /// </summary>
        /// <returns>Native ad dimensions as Vector2 (width, height)</returns>
        public Vector2 GetNativeAdDimensions()
        {
            float width = _nativeAdWidth;
            float height = _nativeAdHeight;

            // Get effective position to determine if height should be clamped
            var effectivePosition = BidscubeSDK.GetEffectiveAdPosition();
            if (effectivePosition == AdPosition.Unknown)
            {
                effectivePosition = BidscubeSDK.GetResponseAdPosition();
            }

            // For Header and Footer positions, clamp height to max 50 Unity units
            if (effectivePosition == AdPosition.Header || effectivePosition == AdPosition.Footer)
            {
                height = Mathf.Min(height, 50f);
            }

            return new Vector2(width, height);
        }

        private void ExtractPositionAndDimensionsFromResponse(string json)
        {
            try
            {
                var response = JsonUtility.FromJson<AdResponse>(json);
                if (response != null)
                {
                    // Check for position in nested adm object first
                    if (response.adm != null && response.adm.position.HasValue)
                    {
                        var position = (AdPosition)response.adm.position.Value;
                        Logger.Info($"[BidscubeSDK] NativeAdView: Received position from nested adm: {response.adm.position} - {position}");
                        BidscubeSDK.SetResponseAdPosition(position);
                    }
                    // Check for position in root response
                    else if (response.position.HasValue)
                    {
                        var position = (AdPosition)response.position.Value;
                        Logger.Info($"[BidscubeSDK] NativeAdView: Received position from server: {response.position} - {position}");
                        BidscubeSDK.SetResponseAdPosition(position);
                    }

                    if (response.width.HasValue && response.height.HasValue)
                    {
                        _nativeAdWidth = response.width.Value;
                        _nativeAdHeight = response.height.Value;
                        Logger.Info($"[BidscubeSDK] NativeAdView: Extracted dimensions from response: {_nativeAdWidth}x{_nativeAdHeight}");

                        // Update size immediately if not FullScreen
                        UpdateNativeAdSize();
                    }
                }
            }
            catch (System.Exception e)
            {
                Logger.Info($"[BidscubeSDK] NativeAdView: Failed to extract position/dimensions from response: {e.Message}");
            }
        }

        /// <summary>
        /// Update native ad size based on position - if not FullScreen, set explicit size
        /// </summary>
        private void UpdateNativeAdSize()
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
                // Use actual native ad dimensions if available
                float width = _nativeAdWidth > 0 ? _nativeAdWidth : LogicalWidth;
                float height = _nativeAdHeight > 0 ? _nativeAdHeight : LogicalHeight;

                // For Header and Footer positions, clamp height to max 50 Unity units
                if (effectivePosition == AdPosition.Header || effectivePosition == AdPosition.Footer)
                {
                    height = Mathf.Min(height, 50f);
                    Logger.Info($"[BidscubeSDK] NativeAdView: Clamped height to {height} for {effectivePosition} position (original: {_nativeAdHeight})");
                }

                // Set anchors to center
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(width, height);
                rectTransform.anchoredPosition = Vector2.zero;

                Logger.Info($"[BidscubeSDK] NativeAdView: Set size to {width}x{height} for position {effectivePosition}");
            }
            else if (effectivePosition == AdPosition.FullScreen)
            {
                // Full screen - fill parent
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                Logger.Info("[BidscubeSDK] NativeAdView: Set to full screen");
            }
        }

        private void ExtractDimensionsFromOpenRtb(NativeAdData adData)
        {
            // Dimensions are extracted during ParseOpenRtbNative
            // This method is kept for future use if needed
        }

        /// <summary>
        /// Show native ad
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide native ad
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Destroy native ad view
        /// </summary>
        public void Destroy()
        {
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

    /// <summary>
    /// Native ad data structure
    /// </summary>
    [System.Serializable]
    public class NativeAdData
    {
        public string title;
        public string description;
        public string iconUrl;
        public string mainImageUrl;
        public string installButtonText;
        public string storeUrl;
        public string advertiser;
        public string rating;
    }
}
