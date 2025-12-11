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

        private const float LogicalWidth = 1080f;
        private const float LogicalHeight = 800f;
        private float _nativeAdWidth = LogicalWidth;
        private float _nativeAdHeight = LogicalHeight;
        // When true, do not override dimensions from server/adm; use configured asset size
        private bool _useConfiguredSize = false;
        private AdSizeSettings _configuredAdSizeSettings = null;

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
            public AdResponseInner adm;
            public int? position;
            public int? width;
            public int? height;
        }

        [System.Serializable]
        private class AdResponseInner
        {
            public string adm;
            public int? position;
        }

        [System.Serializable]
        private class OpenRtbLink { public string url; }

        private void Awake()
        {
            var rootRect = GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = gameObject.AddComponent<RectTransform>();
            }

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

            if (_useWebViewRendering)
            {
                var webViewHost = new GameObject("NativeWebViewHost");
                webViewHost.transform.SetParent(transform, false);
                webViewHost.transform.localScale = Vector3.one;

                var r = webViewHost.GetComponent<RectTransform>();
                if (r == null) r = webViewHost.AddComponent<RectTransform>();
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;

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

            float nativeWidth = _nativeAdWidth > 0 ? _nativeAdWidth : LogicalWidth;
            float nativeHeight = _nativeAdHeight > 0 ? _nativeAdHeight : LogicalHeight;

            var effectivePosition = BidscubeSDK.GetEffectiveAdPosition();
            if (effectivePosition == AdPosition.Unknown)
            {
                effectivePosition = BidscubeSDK.GetResponseAdPosition();
            }
            bool isFullScreen = effectivePosition == AdPosition.FullScreen;
            bool isHorizontal = effectivePosition == AdPosition.Header || effectivePosition == AdPosition.Footer;
            bool isVertical = effectivePosition == AdPosition.Sidebar;

            if (isHorizontal)
            {
                nativeHeight = Mathf.Min(nativeHeight, 50f);
            }

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

            float aspectRatio = nativeWidth > 0 && nativeHeight > 0 ? nativeWidth / nativeHeight : 1f;
            bool imageTooWide = aspectRatio > 2f;
            bool imageTooTall = aspectRatio < 0.5f;

            string sizeClass = "size-normal";
            if (nativeWidth < 200 || nativeHeight < 50)
            {
                sizeClass = "size-small";
            }
            else if (nativeWidth > 600 || nativeHeight > 400)
            {
                sizeClass = "size-large";
            }

            // Extra CSS injected conditionally
            string extraCss = string.Empty;
            if (imageTooWide)
            {
                extraCss += ".image { background-size: contain !important; background-position: center !important; }\n";
            }
            if (imageTooTall)
            {
                extraCss += ".image { background-size: cover !important; background-position: center top !important; }\n";
            }

            // Template uses string.Format indices:
            // {0}=nativeWidth, {1}=nativeHeight, {2}=main, {3}=icon, {4}=click, {5}=layoutClass, {6}=sizeClass, {7}=extraCss, {8}=title, {9}=desc, {10}=cta
            string template = @"<!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width={0}, height={1}, initial-scale=1.0, user-scalable=no'>
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

                .card {{ flex-direction: column; }}
                .image {{ flex: 3 1 auto; min-height: 100px; max-height: 70%; display:flex; align-items:center; justify-content:center; overflow:hidden; background-color: transparent; }}
                .image img.main-img {{ width: auto; max-width: 100%; max-height: 100%; object-fit:contain; display:block; }}
                .content {{ flex: 2 1 auto; padding: 8px; display: flex; flex-direction: column; justify-content: flex-start; gap:6px; overflow:auto; }}

                .layout-fullscreen .image {{ flex: 4; min-height: 300px; }}
                .layout-fullscreen .content {{ flex: 3; padding: 16px; }}
                .layout-fullscreen .title {{ font-size: 24px; }}
                .layout-fullscreen .desc {{ font-size: 16px; }}

                .layout-horizontal {{ flex-direction: row; align-items: center; }}
                .layout-horizontal .image {{ flex: 0 0 auto; width: 80px; height: 100%; min-width: 60px; max-width: 120px; min-height: auto; }}
                .layout-horizontal .content {{ flex: 1 1 auto; padding: 4px 8px; min-width: 0; overflow: hidden; }}
                .layout-horizontal .title {{ font-size: 14px; margin-bottom: 2px; -webkit-line-clamp: 1; }}
                .layout-horizontal .desc {{ font-size: 11px; margin-bottom: 4px; -webkit-line-clamp: 1; }}

                .layout-vertical {{ flex-direction: column; }}
                .layout-vertical .image {{ flex: 2; min-height: 120px; }}
                .layout-vertical .content {{ flex: 3; padding: 8px; }}

                .size-small .title {{ font-size: 14px; }}
                .size-small .desc {{ font-size: 12px; }}
                .size-small .icon {{ width: 36px; height: 36px; }}
                .size-small .cta {{ padding: 4px 12px; font-size: 12px; }}
                .size-small .content {{ padding: 6px; }}

                .size-large .title {{ font-size: 22px; }}
                .size-large .desc {{ font-size: 16px; }}
                .size-large .icon {{ width: 56px; height: 56px; }}
                .size-large .cta {{ padding: 8px 20px; font-size: 16px; }}

                /* Image size handling - conditional */
                {7}

                /* Responsive rules for small heights */
                @media (max-height: 200px) {{
                    .card {{ flex-direction: row; align-items: center; }}
                    .image {{ flex: 0 0 auto; width: 80px; height: 100%; min-height: auto; min-width: 60px; max-width: 120px; background-size: cover; }}
                    .content {{ flex: 1 1 auto; padding: 4px 8px; min-width: 0; overflow: hidden; }}
                    .title {{ font-size: 14px; -webkit-line-clamp: 1; margin-bottom: 2px; }}
                    .desc {{ display: none; }}
                    .icon {{ width: 32px; height: 32px; margin-right: 6px; }}
                    .cta {{ padding: 4px 12px; font-size: 11px; }}
                    .footer {{ margin-top: 0; flex-direction: row-reverse; justify-content: flex-end; }}
                    .sponsored {{ top: 4px; right: 4px; font-size: 8px; }}
                }}

                @media (max-height: 50px) {{
                    .image {{ width: 40px; min-width: 40px; max-width: 40px; background-size: contain; }}
                    .content {{ padding: 2px 4px; justify-content: center; }}
                    .title {{ font-size: 10px; margin-bottom: 0; }}
                    .icon {{ display: none; }}
                    .cta {{ display: none; }}
                    .footer {{ display: none; }}
                    .sponsored {{ display: none; }}
                }}

                .title {{ font-size: 18px; font-weight: 700; margin-bottom: 4px; color: #111; line-height: 1.2; word-break: break-word; }}
                .desc {{ font-size: 14px; color: #555; margin-bottom: 8px; line-height: 1.4; word-break: break-word; }}
                .footer {{ display: flex; align-items: center; justify-content: space-between; gap: 8px; }}
                .cta {{ padding: 6px 16px; background: #007aff; color: #fff; border-radius: 9999px; font-size: 14px; text-align: center; white-space: nowrap; flex-shrink: 0; }}
                .icon {{ width: 48px; height: 48px; border-radius: 10px; overflow:hidden; margin-right: 12px; flex-shrink: 0; display:flex; align-items:center; justify-content:center; }}
                .icon img.icon-img {{ width:100%; height:100%; object-fit:cover; display:block; border-radius:10px; }}
                .sponsored {{ position: absolute; top: 8px; right: 12px; font-size: 10px; color: #777; background: rgba(255, 255, 255, 0.8); padding: 2px 6px; border-radius: 4px; z-index: 10; }}
                a {{ text-decoration: none; color: inherit; }}
            </style>
        </head>
        <body>
            <a href='{4}' class='card {5} {6}'>
                <div class='sponsored'>Sponsored</div>
                <div class='image'><img class='main-img' src='{2}' alt='ad-main'/></div>
                <div class='content'>
                    <div class='title'>{8}</div>
                    <div class='desc'>{9}</div>
                    <div class='footer'>
                        <div class='icon'><img class='icon-img' src='{3}' alt='ad-icon'/></div>
                        <div class='cta'>{10}</div>
                    </div>
                </div>
            </a>
        </body>
        </html>";

            return string.Format(template, nativeWidth, nativeHeight, main, icon, click, layoutClass, sizeClass, extraCss, title, desc, cta);
        }


        private void SetupUI()
        {
            RectTransform CreateChild(string name, Transform parent)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return go.GetComponent<RectTransform>();
            }

            var rootRect = GetComponent<RectTransform>();

            if (_iconImage == null)
            {
                var iconRect = CreateChild("IconImage", transform);
                _iconImage = iconRect.gameObject.AddComponent<Image>();
            }

            if (_titleText == null)
            {
                var titleRect = CreateChild("TitleText", transform);
                _titleText = titleRect.gameObject.AddComponent<Text>();
                _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _titleText.fontSize = 18;
                _titleText.fontStyle = FontStyle.Bold;
                _titleText.color = Color.black;
            }

            if (_descriptionText == null)
            {
                var descRect = CreateChild("DescriptionText", transform);
                _descriptionText = descRect.gameObject.AddComponent<Text>();
                _descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _descriptionText.fontSize = 14;
                _descriptionText.color = Color.gray;
            }

            if (_mainImage == null)
            {
                var mainRect = CreateChild("MainImage", transform);
                _mainImage = mainRect.gameObject.AddComponent<Image>();
                _mainImage.color = Color.black;
            }

            if (_installButton == null)
            {
                var buttonRect = CreateChild("InstallButton", transform);
                _installButton = buttonRect.gameObject.AddComponent<Button>();
                _installButton.onClick.AddListener(OnInstallButtonClicked);
            }

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

            SetAnchors(mainRect,
                new Vector2(0f, 0.45f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero);

            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(0f, 0.75f);
                iconRect.anchorMax = new Vector2(0f, 0.95f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = new Vector2(10f, -10f);
                iconRect.sizeDelta = new Vector2(50f, 50f);
            }

            SetAnchors(titleRect,
                new Vector2(0f, 0.35f),
                new Vector2(1f, 0.45f),
                new Vector2(10f, 0f),
                new Vector2(-10f, 0f));

            SetAnchors(descRect,
                new Vector2(0f, 0.20f),
                new Vector2(1f, 0.35f),
                new Vector2(10f, 0f),
                new Vector2(-10f, 0f));

            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0.35f, 0.05f);
                buttonRect.anchorMax = new Vector2(0.65f, 0.15f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
            }

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
                        ExtractPositionAndDimensionsFromResponse(json);

                        string admJson = ExtractAdmJson(json);

                        // If admJson looks like raw HTML (starts with '<'), load it directly into the WebView
                        if (!string.IsNullOrEmpty(admJson))
                        {
                            var trimmedAdm = admJson.TrimStart();
                            if (trimmedAdm.StartsWith("<"))
                            {
                                Logger.Info("[BidscubeSDK] NativeAdView: adm looks like raw HTML — loading directly into WebView.");

                                // Create WebViewController if it's missing
                                if (_webViewController == null && _useWebViewRendering)
                                {
                                    var webViewHost = new GameObject("NativeWebViewHost");
                                    webViewHost.transform.SetParent(transform, false);
                                    webViewHost.transform.localScale = Vector3.one;

                                    var r = webViewHost.GetComponent<RectTransform>();
                                    if (r == null) r = webViewHost.AddComponent<RectTransform>();
                                    r.anchorMin = Vector2.zero;
                                    r.anchorMax = Vector2.one;
                                    r.offsetMin = Vector2.zero;
                                    r.offsetMax = Vector2.zero;

                                    _webViewController = webViewHost.AddComponent<WebViewController>();
                                    _webViewController.Initialize();
                                }

                                if (_webViewController != null)
                                {
                                    // Safety CSS to force images to scale and text to remain visible
                                    string safetyCss = "<style> .bidscube-safe img{max-width:100%!important;height:auto!important;max-height:100%!important;object-fit:contain!important;display:block!important;} .bidscube-safe *{box-sizing:border-box!important;} body{background:transparent!important;color:inherit!important;} </style>";

                                    // Safety JS: on DOM ready, ensure img styles are constrained and visible text nodes are not hidden
                                    string safetyScript = @"<script>(function(){function fixImages(){var imgs=document.querySelectorAll('img');for(var i=0;i<imgs.length;i++){try{imgs[i].style.maxWidth='100%';imgs[i].style.height='auto';imgs[i].style.maxHeight='100%';imgs[i].style.objectFit='contain';imgs[i].style.display='block';}catch(e){} } }
                                        function unhideText(){var els=document.querySelectorAll('*');for(var i=0;i<els.length;i++){var s=window.getComputedStyle(els[i]);if(s && (s.display==='none' || s.visibility==='hidden' || parseFloat(s.opacity||1)===0)){els[i].style.display='block';els[i].style.visibility='visible';els[i].style.opacity='1';}}}
                                        document.addEventListener('DOMContentLoaded', function(){try{fixImages();unhideText();}catch(e){}});
                                        setTimeout(function(){try{fixImages();unhideText();}catch(e){}},250);
                                    })();</script>";

                                    string htmlToLoad;

                                    if (trimmedAdm.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || trimmedAdm.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Attempt to inject safetyCss into existing <head> if possible
                                        if (admJson.IndexOf("</head>", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            htmlToLoad = admJson.Replace("</head>", safetyCss + safetyScript + "</head>");
                                        }
                                        else
                                        {
                                            // No head end tag - wrap entire document body into bidscube-safe container
                                            htmlToLoad = admJson;
                                            // If there's a <body>, inject wrapper after opening body
                                            int bodyOpen = htmlToLoad.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
                                            if (bodyOpen >= 0)
                                            {
                                                int bodyStart = htmlToLoad.IndexOf('>', bodyOpen);
                                                if (bodyStart >= 0)
                                                {
                                                    htmlToLoad = htmlToLoad.Insert(bodyStart + 1, "<div class=\'bidscube-safe\'>");
                                                    // insert closing wrapper before </body>
                                                    if (htmlToLoad.IndexOf("</body>", StringComparison.OrdinalIgnoreCase) >= 0)
                                                    {
                                                        htmlToLoad = htmlToLoad.Replace("</body>", "</div></body>");
                                                    }
                                                    else
                                                    {
                                                        htmlToLoad += "</div>";
                                                    }
                                                }
                                                // ensure CSS present in head
                                                if (htmlToLoad.IndexOf("</head>", StringComparison.OrdinalIgnoreCase) >= 0)
                                                {
                                                    htmlToLoad = htmlToLoad.Replace("</head>", safetyCss + safetyScript + "</head>");
                                                }
                                            }
                                            else
                                            {
                                                // fallback: wrap whole adm inside scaffold
                                                htmlToLoad = $"<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'>{safetyCss}{safetyScript}</head><body><div class='bidscube-safe'>{admJson}</div></body></html>";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // adm is a fragment/inline HTML - wrap it and apply safetyCss
                                        htmlToLoad = $"<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'>{safetyCss}{safetyScript}</head><body><div class='bidscube-safe'>{admJson}</div></body></html>";
                                    }

                                    _webViewController.LoadHTML(htmlToLoad, "");
                                     StartCoroutine(DelayedWebViewMarginRefresh());

                                    // Hide legacy UI and notify controller/callbacks
                                    SetLegacyUiVisible(false);
                                    var parentControllerRaw = GetComponentInParent<AdViewController>();
                                    parentControllerRaw?.MarkAdAsLoaded();
                                    _isLoaded = true;
                                    _callback?.OnAdLoaded(_placementId);
                                    _callback?.OnAdDisplayed(_placementId);

                                    // We've handled the rendering using the raw HTML — done
                                    yield break;
                                }
                            }
                        }

                        var parsed = ParseOpenRtbNative(admJson);
                        if (parsed != null)
                        {
                            _adData = parsed;
                            ExtractDimensionsFromOpenRtb(parsed);
                        }
                        else
                        {
                            _adData = !string.IsNullOrEmpty(admJson)
                                ? JsonUtility.FromJson<NativeAdData>(admJson)
                                : JsonUtility.FromJson<NativeAdData>(json);

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

                        UpdateNativeAdSize();

                        if (_useWebViewRendering && _webViewController != null)
                        {
                            var html = BuildNativeHtml(_adData);
                            _webViewController.LoadHTML(html, "");

                            if (_webViewController != null)
                            {
                                StartCoroutine(DelayedWebViewMarginRefresh());
                            }
                            SetLegacyUiVisible(false);
                        }

                        var parentController = GetComponentInParent<AdViewController>();
                        if (parentController != null)
                        {
                            parentController.MarkAdAsLoaded();
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
                var response = JsonUtility.FromJson<AdResponse>(responseJson);
                if (response != null && response.adm != null && !string.IsNullOrEmpty(response.adm.adm))
                {
                    Logger.Info("[BidscubeSDK] NativeAdView: Found nested adm structure");
                    var adm = response.adm.adm.Trim();

                    adm = adm.Replace("\\\"", "\"")
                             .Replace("\\n", "\n")
                             .Replace("\\r", "\r")
                             .Replace("\\t", "\t")
                             .Replace("\\/", "/");

                    adm = UnescapeUnicode(adm);

                    return adm;
                }
            }
            catch
            {
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

                    adm = UnescapeUnicode(adm);

                    return adm;
                }
            }
            catch
            {
            }

            return responseJson;
        }

        private void ExtractPositionAndDimensionsFromResponse(string responseJson)
        {
            try
            {
                var response = JsonUtility.FromJson<AdResponse>(responseJson);

                // Only extract width/height from response if we don't have configured size
                if (!_useConfiguredSize)
                {
                    if (response.width.HasValue)
                    {
                        _nativeAdWidth = response.width.Value;
                    }
                    if (response.height.HasValue)
                    {
                        _nativeAdHeight = response.height.Value;
                    }
                }

                if (response.position.HasValue)
                {
                    BidscubeSDK.SetResponseAdPosition((AdPosition)response.position.Value);
                }
                else if (response.adm != null && response.adm.position.HasValue)
                {
                    BidscubeSDK.SetResponseAdPosition((AdPosition)response.adm.position.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.InfoError($"[BidscubeSDK] NativeAdView: Failed to extract size/position from response: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply AdSizeSettings defaults to this native view. When applied, the view will
        /// prefer the configured size over sizes provided by the adm/response.
        /// </summary>
        public void ApplyAdSizeSettings(AdSizeSettings settings)
        {
            if (settings == null) return;
            _configuredAdSizeSettings = settings;
            var cfg = settings.GetDefaultSize(AdType.Native);
            if (cfg.x > 0f) _nativeAdWidth = cfg.x;
            if (cfg.y > 0f) _nativeAdHeight = cfg.y;
            _useConfiguredSize = true;
            Logger.Info($"[NativeAdView] Applied AdSizeSettings: {_nativeAdWidth}x{_nativeAdHeight}");
        }

        private NativeAdData ParseOpenRtbNative(string admJson)
        {
            if (string.IsNullOrEmpty(admJson)) return null;

            try
            {
                var root = JsonUtility.FromJson<OpenRtbNativeRoot>(admJson);
                if (root == null || root.native == null || root.native.assets == null)
                {
                    return null;
                }

                var data = new NativeAdData();
                data.storeUrl = root.native.link?.url;

                foreach (var asset in root.native.assets)
                {
                    if (asset.title != null)
                    {
                        data.title = asset.title.text;
                    }
                    if (asset.data != null)
                    {
                        data.description = asset.data.value; // Assuming value is description/text
                    }
                    if (asset.img != null)
                    {
                        if (asset.id == 1) // ID 1 often denotes Icon (Unity's convention may differ)
                        {
                            data.iconUrl = asset.img.url;
                        }
                        else if (asset.id == 2) // ID 2 often denotes Main Image
                        {
                            data.mainImageUrl = asset.img.url;
                        }
                        // Fallback logic if IDs are unreliable or missing
                        if (string.IsNullOrEmpty(data.iconUrl) && (asset.img.w < 100 || asset.img.h < 100))
                        {
                            data.iconUrl = asset.img.url;
                        }
                        else if (string.IsNullOrEmpty(data.mainImageUrl))
                        {
                            data.mainImageUrl = asset.img.url;
                        }
                    }
                }

                // If description is missing, check the advertiser/sponsored field
                if (string.IsNullOrEmpty(data.description))
                {
                    foreach (var asset in root.native.assets)
                    {
                        if (asset.data != null && (asset.id == 3 || asset.id == 4)) // Data fields for sponsored/advertiser
                        {
                            data.advertiser = asset.data.value;
                        }
                        if (asset.data != null && (asset.id == 5)) // CTA/Install button text
                        {
                            data.installButtonText = asset.data.value;
                        }
                    }
                }
                // Final clean-up for Unicode
                if (!string.IsNullOrEmpty(data.title))
                    data.title = UnescapeUnicode(data.title);
                if (!string.IsNullOrEmpty(data.description))
                    data.description = UnescapeUnicode(data.description);
                if (!string.IsNullOrEmpty(data.installButtonText))
                    data.installButtonText = UnescapeUnicode(data.installButtonText);
                if (!string.IsNullOrEmpty(data.advertiser))
                    data.advertiser = UnescapeUnicode(data.advertiser);


                return data;
            }
            catch (Exception ex)
            {
                Logger.InfoError($"[BidscubeSDK] NativeAdView: Failed to parse OpenRTB native: {ex.Message}");
                return null;
            }
        }

        private void ExtractDimensionsFromOpenRtb(NativeAdData data)
        {
            if (data == null || string.IsNullOrEmpty(data.mainImageUrl)) return;

            // This is a placeholder as Unity's JsonUtility does not easily map the image dimensions
            // from the assets array after initial parsing. Dimensions should ideally be available
            // on the main response object, but if they aren't:
            // The ad size must be assumed from the container size or extracted via a separate image request.

            // Since we rely on _nativeAdWidth and _nativeAdHeight from the response, this method
            // serves mainly as a reminder if OpenRTB parsing were to be fully relied upon for dimensions.
        }

        private void PopulateAdData()
        {
            if (_adData == null) return;

            _titleText.text = _adData.title;
            _descriptionText.text = _adData.description;
            _installButtonText.text = string.IsNullOrEmpty(_adData.installButtonText) ? "Install" : _adData.installButtonText;

            // Load images (Async loading required for production)
            // For now, these lines are placeholders for the legacy UI:
            // StartCoroutine(ImageLoader.LoadImage(_adData.iconUrl, _iconImage));
            // StartCoroutine(ImageLoader.LoadImage(_adData.mainImageUrl, _mainImage));

            _sponsoredText.text = string.IsNullOrEmpty(_adData.advertiser) ? "Sponsored" : _adData.advertiser;

            _installButton.onClick.RemoveAllListeners();
            _installButton.onClick.AddListener(OnInstallButtonClicked);
        }

        private void OnInstallButtonClicked()
        {
            if (!_isLoaded || _adData == null || string.IsNullOrEmpty(_adData.storeUrl)) return;

            Application.OpenURL(_adData.storeUrl);
            _callback?.OnAdClicked(_placementId);
        }

        private string UnescapeUnicode(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return Regex.Replace(str, @"\\u([0-9a-fA-F]{4})",
                (m) => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        }

        private IEnumerator DelayedWebViewMarginRefresh()
        {
            // Wait one frame to ensure the webview host RectTransform has settled in Unity layout system
            yield return null;
            if (_webViewController != null)
            {
                _webViewController.RefreshMargins();
            }
        }

        private void UpdateNativeAdSize()
        {
            var adViewController = GetComponentInParent<AdViewController>();
            if (adViewController != null)
            {
                // If AdSizeSettings provided via _configuredAdSizeSettings, prefer those values
                float w = _nativeAdWidth;
                float h = _nativeAdHeight;
                if (_configuredAdSizeSettings != null)
                {
                    var cfg = _configuredAdSizeSettings.GetDefaultSize(AdType.Native);
                    if (cfg.x > 0f) w = cfg.x;
                    if (cfg.y > 0f) h = cfg.y;
                }
                adViewController.SetAdSize(w, h);
            }
        }

        /// <summary>
        /// Return native ad dimensions as width,height Vector2.
        /// Used by AdViewController when calculating positioning.
        /// </summary>
        public Vector2 GetNativeAdDimensions()
        {
            float width = _nativeAdWidth > 0 ? _nativeAdWidth : LogicalWidth;
            float height = _nativeAdHeight > 0 ? _nativeAdHeight : LogicalHeight;

            // If configured settings are present, prefer them
            if (_configuredAdSizeSettings != null)
            {
                var cfg = _configuredAdSizeSettings.GetDefaultSize(AdType.Native);
                if (cfg.x > 0f) width = cfg.x;
                if (cfg.y > 0f) height = cfg.y;
            }

            var effectivePosition = BidscubeSDK.GetEffectiveAdPosition();
            if (effectivePosition == AdPosition.Unknown)
            {
                effectivePosition = BidscubeSDK.GetResponseAdPosition();
            }

            if (effectivePosition == AdPosition.Header || effectivePosition == AdPosition.Footer)
            {
                height = Mathf.Min(height, 50f);
            }

            return new Vector2(width, height);
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
