using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

namespace BidscubeSDK
{
    /// <summary>
    /// Image ad view component - Identical to iOS ImageAdView
    /// </summary>
    public class ImageAdView : MonoBehaviour
    {
        [SerializeField] private RawImage _webView;
        [SerializeField] private Text _loadingLabel;
        [SerializeField] private Button _clickButton;

        private string _placementId;
        private IAdCallback _callback;
        private bool _isLoaded = false;
        private string _clickURL;
        private Texture2D _adTexture;
        private WebViewObject _webViewObject;

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            // Setup background
            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray like iOS

            // Add corner radius effect
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1020, 1250);

            // Create real web view container
            var webViewObj = new GameObject("WebView");
            webViewObj.transform.SetParent(transform);

            // Add Canvas for web view
            var webViewCanvas = webViewObj.AddComponent<Canvas>();
            webViewCanvas.overrideSorting = true;
            webViewCanvas.sortingOrder = 1;

            // Add GraphicRaycaster for click handling
            webViewObj.AddComponent<GraphicRaycaster>();

            // Create web view content area
            var webViewContent = new GameObject("WebViewContent");
            webViewContent.transform.SetParent(webViewObj.transform);

            // Add RawImage for texture display
            _webView = webViewContent.AddComponent<RawImage>();
            _webView.color = Color.clear;

            // Create click button (invisible overlay)
            _clickButton = gameObject.AddComponent<Button>();
            _clickButton.onClick.AddListener(OnAdClicked);

            // Setup constraints
            SetupConstraints();
        }

        private void SetupConstraints()
        {
            var rectTransform = GetComponent<RectTransform>();
            var webViewRect = _webView.GetComponent<RectTransform>();
            var loadingRect = _loadingLabel.GetComponent<RectTransform>();

            // WebView fills the entire area
            webViewRect.anchorMin = Vector2.zero;
            webViewRect.anchorMax = Vector2.one;
            webViewRect.offsetMin = Vector2.zero;
            webViewRect.offsetMax = Vector2.zero;

            // Loading label centered
            loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.sizeDelta = new Vector2(120, 30);
            loadingRect.anchoredPosition = Vector2.zero;
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
        /// Load ad from URL - Identical to iOS loadAdFromURL
        /// </summary>
        /// <param name="url">Ad URL</param>
        public void LoadAdFromURL(string url)
        {
            _loadingLabel.gameObject.SetActive(true);
            _loadingLabel.text = "Loading Ad...";

            Debug.Log($" ImageAdView: Making HTTP request to: {url}");
            StartCoroutine(LoadAdCoroutine(url));
        }

        private IEnumerator LoadAdCoroutine(string url)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var htmlContent = request.downloadHandler.text;
                    Debug.Log($" ImageAdView: Received response: {htmlContent}");

                    try
                    {
                        // Try to parse JSON response like iOS
                        var json = JsonUtility.FromJson<AdResponse>(htmlContent);
                        if (json != null && !string.IsNullOrEmpty(json.adm))
                        {
                            Debug.Log($" Adm: {json.adm}");

                            if (json.position != null)
                            {
                                var position = (AdPosition)json.position;
                                Debug.Log($" ImageAdView: Received position from server: {json.position} - {GetDisplayName(position)}");
                                BidscubeSDK.SetResponseAdPosition(position);
                            }

                            LoadAdContent(json.adm);
                        }
                        else
                        {
                            // Handle HTML response with document.write
                            Debug.Log(" ImageAdView: Received HTML content, extracting image URL...");
                            var imageUrl = ExtractImageUrlFromHtml(htmlContent);
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                Debug.Log($" ImageAdView: Extracted image URL: {imageUrl}");
                                LoadImageFromUrl(imageUrl);
                                yield break;
                            }
                            else
                            {
                                Debug.LogError(" ImageAdView: Could not extract image URL from HTML content");
                                OnAdFailed("Failed to extract image URL from HTML response");
                                yield break;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($" ImageAdView: Error parsing response: {e.Message}");
                        OnAdFailed($"Error parsing response: {e.Message}");
                        yield break;
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
        /// Extract image URL from HTML content
        /// </summary>
        /// <param name="htmlContent">HTML content</param>
        /// <returns>Image URL or null if not found</returns>
        private string ExtractImageUrlFromHtml(string htmlContent)
        {
            try
            {
                // Look for iframe src attribute in the HTML content
                var iframePattern = @"<iframe[^>]+src=""([^""]+)""[^>]*>";
                var match = System.Text.RegularExpressions.Regex.Match(htmlContent, iframePattern);
                if (match.Success)
                {
                    var iframeUrl = match.Groups[1].Value;
                    Debug.Log($" ImageAdView: Found iframe URL: {iframeUrl}");
                    return iframeUrl;
                }

                // Look for img src attribute as fallback
                var imgPattern = @"<img[^>]+src=""([^""]+)""[^>]*>";
                var imgMatch = System.Text.RegularExpressions.Regex.Match(htmlContent, imgPattern);
                if (imgMatch.Success)
                {
                    var imgUrl = imgMatch.Groups[1].Value;
                    Debug.Log($" ImageAdView: Found img URL: {imgUrl}");
                    return imgUrl;
                }

                Debug.LogWarning(" ImageAdView: No image URL found in HTML content");
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($" ImageAdView: Error extracting image URL: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load image from URL
        /// </summary>
        /// <param name="imageUrl">Image URL</param>
        private void LoadImageFromUrl(string imageUrl)
        {
            StartCoroutine(LoadImageCoroutine(imageUrl));
        }

        /// <summary>
        /// Load image coroutine
        /// </summary>
        /// <param name="imageUrl">Image URL</param>
        /// <returns>Coroutine</returns>
        private IEnumerator LoadImageCoroutine(string imageUrl)
        {
            using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    _webView.texture = texture;
                    _loadingLabel.gameObject.SetActive(false);
                    _webView.gameObject.SetActive(true);

                    Debug.Log(" ImageAdView: Image loaded successfully");
                    _callback?.OnAdLoaded(_placementId);
                }
                else
                {
                    Debug.LogError($" ImageAdView: Failed to load image: {request.error}");
                    OnAdFailed($"Failed to load image: {request.error}");
                }
            }
        }

        /// <summary>
        /// Load ad content - Identical to iOS loadAdContent
        /// </summary>
        /// <param name="htmlContent">HTML content</param>
        public void LoadAdContent(string htmlContent)
        {
            _loadingLabel.gameObject.SetActive(true);

            // Extract click URL from HTML like iOS
            ExtractClickURLFromHTML(htmlContent);

            // Clean HTML like iOS
            var cleanHTML = htmlContent
                .Replace("document.write(", "")
                .Replace(");", "")
                .Trim();

            Debug.Log($" ImageAdView: Loading HTML content: {cleanHTML}");

            // Create real web view experience
            CreateWebViewContent(cleanHTML);
            StartCoroutine(HideLoadingAfterDelay(2.0f));
        }

        /// <summary>
        /// Create web view content with HTML rendering and click handling
        /// </summary>
        /// <param name="htmlContent">HTML content to render</param>
        private void CreateWebViewContent(string htmlContent)
        {
            Debug.Log($" ImageAdView: Creating web view with HTML content");

            // Create full HTML document
            var fullHTML = CreateFullHTMLDocument(htmlContent);

            // Initialize WebViewObject
            InitializeWebView(fullHTML);
        }

        /// <summary>
        /// Create full HTML document with proper structure
        /// </summary>
        /// <param name="htmlContent">HTML content</param>
        /// <returns>Full HTML document</returns>
        private string CreateFullHTMLDocument(string htmlContent)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ 
            margin: 0; 
            padding: 0; 
            overflow: hidden; 
            font-family: Arial, sans-serif;
        }}
        * {{ box-sizing: border-box; }}
        img {{ max-width: 100%; height: auto; }}
    </style>
</head>
<body>
    {htmlContent}
</body>
</html>";
        }

        private void OnWebViewLoaded(string msg)   // added string parameter
        {
            Debug.Log(" ImageAdView: WebView loaded successfully");
            _loadingLabel.gameObject.SetActive(false);
            _webView.gameObject.SetActive(true);
            _callback?.OnAdLoaded(_placementId);
        }

        private void OnWebViewStarted(string msg)  // added string parameter
        {
            Debug.Log(" ImageAdView: WebView started");
        }

        private void OnWebViewHooked(string msg)   // added string parameter
        {
            Debug.Log(" ImageAdView: WebView hooked");
        }


        /// <summary>
        /// Initialize WebViewObject for HTML rendering
        /// </summary>
        /// <param name="htmlContent">HTML content to render</param>
        private void InitializeWebView(string htmlContent)
        {
            try
            {
                // Create WebViewObject
                var webViewGO = new GameObject("WebViewObject");
                _webViewObject = webViewGO.AddComponent<WebViewObject>();

                // Get the Canvas and RectTransform for proper positioning
                var canvas = GetComponentInParent<Canvas>();
                var rectTransform = _webView.GetComponent<RectTransform>();

                if (canvas == null)
                {
                    Debug.LogError(" ImageAdView: No Canvas found for WebView positioning");
                    OnAdFailed("No Canvas found for WebView positioning");
                    return;
                }

                // Set the canvas reference for WebViewObject (required for macOS/OSX rendering)
                _webViewObject.canvas = canvas.gameObject;

                // Calculate RectTransform position and size
                var rectTransformRect = GetRectTransformRect(rectTransform);
                Debug.Log($" ImageAdView: WebView RectTransform rect: {rectTransformRect}");
                Debug.Log($" ImageAdView: Screen size: {Screen.width}x{Screen.height}");
                Debug.Log($" ImageAdView: RectTransform position: {rectTransform.position}");
                Debug.Log($" ImageAdView: RectTransform size: {rectTransform.sizeDelta}");

                // Initialize web view with proper settings
                _webViewObject.Init(
                    cb: OnWebViewMessage,
                    err: OnWebViewError,
                    httpErr: OnWebViewHttpError,
                    ld: _ => OnWebViewLoaded(),
                    started: _ => OnWebViewStarted(),
                    hooked: _ => OnWebViewHooked(),
                    transparent: false,
                    zoom: true
                );

                // Set web view position and size using RectTransform coordinates
                // WebViewObject SetMargins: left, top, right, bottom
                var leftMargin = (int)rectTransformRect.x;
                var topMargin = (int)(Screen.height - rectTransformRect.y - rectTransformRect.height);
                var rightMargin = (int)(Screen.width - rectTransformRect.x - rectTransformRect.width);
                var bottomMargin = (int)rectTransformRect.y;

                Debug.Log($" ImageAdView: WebView margins - Left: {leftMargin}, Top: {topMargin}, Right: {rightMargin}, Bottom: {bottomMargin}");

                _webViewObject.SetMargins(leftMargin, topMargin, rightMargin, bottomMargin);

                // Configure WebViewObject for proper rendering
                _webViewObject.SetVisibility(true);
                _webViewObject.SetInteractionEnabled(true);
                _webViewObject.SetScrollbarsVisibility(true);

                // Set refresh cycle for better performance (macOS/OSX specific)
                _webViewObject.bitmapRefreshCycle = 1;
                _webViewObject.devicePixelRatio = 1;

                // Load HTML content
                _webViewObject.LoadHTML(htmlContent, "");

                // Verify WebViewObject initialization
                if (_webViewObject.IsInitialized())
                {
                    Debug.Log(" ImageAdView: WebView initialized successfully and is ready");
                }
                else
                {
                    Debug.LogWarning(" ImageAdView: WebView initialization may have failed");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($" ImageAdView: Failed to initialize WebView: {e.Message}");
                OnAdFailed($"Failed to initialize WebView: {e.Message}");
            }
        }

        /// <summary>
        /// Get RectTransform rect in screen coordinates for WebViewObject positioning
        /// </summary>
        /// <param name="rectTransform">RectTransform</param>
        /// <returns>RectTransform rect in screen coordinates</returns>
        private Rect GetRectTransformRect(RectTransform rectTransform)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError(" ImageAdView: No Canvas found for RectTransform rect calculation");
                return new Rect(0, 0, 1000, 1250); // Default fallback
            }

            // Get the RectTransform's rect in local coordinates
            var rect = rectTransform.rect;

            // Convert to world position
            var worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            // Handle different Canvas render modes
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // For ScreenSpaceOverlay, use world corners directly
                var min = worldCorners[0];
                var max = worldCorners[2];
                return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            }
            else
            {
                // For ScreenSpaceCamera and WorldSpace, convert to screen space
                var camera = canvas.worldCamera ?? Camera.main;
                if (camera != null)
                {
                    var min = camera.WorldToScreenPoint(worldCorners[0]);
                    var max = camera.WorldToScreenPoint(worldCorners[2]);
                    return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
                }
                else
                {
                    // Fallback to world corners
                    var min = worldCorners[0];
                    var max = worldCorners[2];
                    return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
                }
            }
        }

        /// <summary>
        /// Get screen rect from RectTransform with proper Canvas handling (legacy method)
        /// </summary>
        /// <param name="rectTransform">RectTransform</param>
        /// <returns>Screen rect</returns>
        private Rect GetScreenRect(RectTransform rectTransform)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError(" ImageAdView: No Canvas found for screen rect calculation");
                return new Rect(0, 0, 1000, 1250); // Default fallback
            }

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // Convert world corners to screen space
            var min = corners[0];
            var max = corners[2];

            // Handle different Canvas render modes
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // For ScreenSpaceOverlay, coordinates are already in screen space
                return new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
            }
            else
            {
                // For ScreenSpaceCamera and WorldSpace, convert to screen space
                var camera = canvas.worldCamera ?? Camera.main;
                if (camera != null)
                {
                    min = camera.WorldToScreenPoint(min);
                    max = camera.WorldToScreenPoint(max);
                }
                return new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
            }
        }

        /// <summary>
        /// Extract image URLs from HTML content
        /// </summary>
        /// <param name="htmlContent">HTML content</param>
        /// <returns>List of image URLs</returns>
        private System.Collections.Generic.List<string> ExtractImageUrlsFromHTML(string htmlContent)
        {
            var imageUrls = new System.Collections.Generic.List<string>();

            try
            {
                // Look for img src attributes
                var imgPattern = @"<img[^>]+src=""([^""]+)""[^>]*>";
                var matches = System.Text.RegularExpressions.Regex.Matches(htmlContent, imgPattern);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var url = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(url) && !url.StartsWith("data:"))
                    {
                        imageUrls.Add(url);
                        Debug.Log($" ImageAdView: Found image URL: {url}");
                    }
                }

                // Look for iframe src attributes as fallback
                if (imageUrls.Count == 0)
                {
                    var iframePattern = @"<iframe[^>]+src=""([^""]+)""[^>]*>";
                    var iframeMatches = System.Text.RegularExpressions.Regex.Matches(htmlContent, iframePattern);

                    foreach (System.Text.RegularExpressions.Match match in iframeMatches)
                    {
                        var url = match.Groups[1].Value;
                        if (!string.IsNullOrEmpty(url))
                        {
                            imageUrls.Add(url);
                            Debug.Log($" ImageAdView: Found iframe URL: {url}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($" ImageAdView: Error extracting image URLs: {e.Message}");
            }

            return imageUrls;
        }

        /// <summary>
        /// Extract click URLs from HTML content
        /// </summary>
        /// <param name="htmlContent">HTML content</param>
        /// <returns>List of click URLs</returns>
        private System.Collections.Generic.List<string> ExtractClickUrlsFromHTML(string htmlContent)
        {
            var clickUrls = new System.Collections.Generic.List<string>();

            try
            {
                // Look for click URLs in various formats
                var clickPatterns = new string[]
                {
                    @"https://[^\s""']+", // General HTTPS URLs
                    @"http://[^\s""']+",  // General HTTP URLs
                    @"clck[^""']*",       // Click tracking URLs
                    @"click[^""']*"       // Click URLs
                };

                foreach (var pattern in clickPatterns)
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(htmlContent, pattern);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var url = match.Value;
                        if (!string.IsNullOrEmpty(url) && !url.Contains("img") && !url.Contains("iframe"))
                        {
                            clickUrls.Add(url);
                            Debug.Log($" ImageAdView: Found click URL: {url}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($" ImageAdView: Error extracting click URLs: {e.Message}");
            }

            return clickUrls;
        }

        /// <summary>
        /// Load main ad image and setup click handling
        /// </summary>
        /// <param name="imageUrl">Image URL</param>
        /// <param name="clickUrl">Click URL</param>
        private IEnumerator LoadMainAdImage(string imageUrl, string clickUrl)
        {
            using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    _webView.texture = texture;

                    // Setup click handling if click URL is available
                    if (!string.IsNullOrEmpty(clickUrl))
                    {
                        SetupClickHandler(clickUrl);
                    }

                    _loadingLabel.gameObject.SetActive(false);
                    _webView.gameObject.SetActive(true);

                    Debug.Log(" ImageAdView: Ad image loaded successfully");
                    _callback?.OnAdLoaded(_placementId);
                }
                else
                {
                    Debug.LogError($" ImageAdView: Failed to load ad image: {request.error}");
                    OnAdFailed($"Failed to load ad image: {request.error}");
                }
            }
        }

        /// <summary>
        /// Setup click handler for browser redirect
        /// </summary>
        /// <param name="clickUrl">Click URL</param>
        private void SetupClickHandler(string clickUrl)
        {
            // Create invisible button over the entire web view for click handling
            var clickButton = _webView.gameObject.AddComponent<Button>();
            clickButton.onClick.AddListener(() => OpenUrlInBrowser(clickUrl));

            Debug.Log($" ImageAdView: Click handler setup for URL: {clickUrl}");
        }

        /// <summary>
        /// Open URL in browser
        /// </summary>
        /// <param name="url">URL to open</param>
        private void OpenUrlInBrowser(string url)
        {
            Debug.Log($" ImageAdView: Opening URL in browser: {url}");

            try
            {
                // Use Unity's Application.OpenURL to open in browser
                Application.OpenURL(url);

                // Notify callback of click
                _callback?.OnAdClicked(_placementId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($" ImageAdView: Failed to open URL: {e.Message}");
            }
        }

        #region WebViewObject Callbacks

        /// <summary>
        /// WebView message callback
        /// </summary>
        /// <param name="message">Message from web view</param>
        private void OnWebViewMessage(string message)
        {
            Debug.Log($" ImageAdView: WebView message: {message}");

            // Handle click messages
            if (message.StartsWith("click:"))
            {
                var url = message.Substring(6);
                OpenUrlInBrowser(url);
            }
        }

        /// <summary>
        /// WebView error callback
        /// </summary>
        /// <param name="error">Error message</param>
        private void OnWebViewError(string error)
        {
            Debug.LogError($" ImageAdView: WebView error: {error}");
            OnAdFailed($"WebView error: {error}");
        }

        /// <summary>
        /// WebView HTTP error callback
        /// </summary>
        /// <param name="error">HTTP error</param>
        private void OnWebViewHttpError(string error)
        {
            Debug.LogError($" ImageAdView: WebView HTTP error: {error}");
            OnAdFailed($"WebView HTTP error: {error}");
        }

        /// <summary>
        /// WebView loaded callback
        /// </summary>
        private void OnWebViewLoaded()
        {
            Debug.Log(" ImageAdView: WebView loaded successfully");
            Debug.Log($" ImageAdView: WebView visibility: {_webViewObject.GetVisibility()}");
            Debug.Log($" ImageAdView: WebView initialized: {_webViewObject.IsInitialized()}");
            _loadingLabel.gameObject.SetActive(false);
            _webView.gameObject.SetActive(true);
            _callback?.OnAdLoaded(_placementId);
        }

        /// <summary>
        /// WebView started callback
        /// </summary>
        private void OnWebViewStarted()
        {
            Debug.Log(" ImageAdView: WebView started");
        }

        /// <summary>
        /// WebView hooked callback
        /// </summary>
        private void OnWebViewHooked()
        {
            Debug.Log(" ImageAdView: WebView hooked");
        }

        /// <summary>
        /// WebView load URL callback
        /// </summary>
        /// <param name="url">URL being loaded</param>
        private void OnWebViewLoadURL(string url)
        {
            Debug.Log($" ImageAdView: WebView loading URL: {url}");
        }

        #endregion

        private IEnumerator RenderHTMLToTexture(string htmlContent)
        {
            // In a real implementation, you'd use a WebView plugin
            // For now, we'll create a placeholder texture
            var texture = new Texture2D(1020, 1250);
            var colors = new Color[1020 * 1250];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(0.9f, 0.9f, 0.9f, 1f);
            }
            texture.SetPixels(colors);
            texture.Apply();

            _webView.texture = texture;
            _isLoaded = true;

            _callback?.OnAdLoaded(_placementId);
            _callback?.OnAdDisplayed(_placementId);

            yield return null;
        }

        private IEnumerator HideLoadingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _loadingLabel.gameObject.SetActive(false);
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
                    _clickURL = match.Value;
                    Debug.Log($" ImageAdView: Extracted click URL from HTML: {_clickURL}");
                    return;
                }
            }

            Debug.Log(" ImageAdView: Could not extract click URL from HTML content");
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
            Debug.Log(" ImageAdView: Tap gesture detected");

            _callback?.OnAdClicked(_placementId);

            if (!string.IsNullOrEmpty(_clickURL))
            {
                Debug.Log($" ImageAdView: Opening extracted click URL: {_clickURL}");
                Application.OpenURL(_clickURL);
            }
            else
            {
                Debug.Log(" ImageAdView: No click URL available to open");
            }
        }

        /// <summary>
        /// Show ad
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide ad
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Destroy ad view
        /// </summary>
        public void Destroy()
        {
            // Cleanup WebViewObject
            if (_webViewObject != null)
            {
                _webViewObject.SetVisibility(false);
                _webViewObject = null;
            }

            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Handle ad failure with logging and toast message
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        private void OnAdFailed(string errorMessage)
        {
            Debug.LogError($" ImageAdView: Ad failed - {errorMessage}");

            // Show error message on screen for 3 seconds
            ShowErrorToast(errorMessage);

            // Notify callback
            _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.UnknownError, errorMessage);
        }

        /// <summary>
        /// Show error toast message for 3 seconds
        /// </summary>
        /// <param name="message">Error message</param>
        private void ShowErrorToast(string message)
        {
            // Create toast message GameObject
            var toastObj = new GameObject("ErrorToast");
            toastObj.transform.SetParent(transform);

            // Add Canvas to toast
            var canvas = toastObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1001; // Higher than ad canvas

            // Add RectTransform
            var toastRect = toastObj.GetComponent<RectTransform>();
            if (toastRect == null)
            {
                toastRect = toastObj.AddComponent<RectTransform>();
            }

            // Position toast at center of screen
            toastRect.anchorMin = new Vector2(0.5f, 0.5f);
            toastRect.anchorMax = new Vector2(0.5f, 0.5f);
            toastRect.sizeDelta = new Vector2(400, 100);
            toastRect.anchoredPosition = Vector2.zero;

            // Add background image
            var background = toastObj.AddComponent<Image>();
            background.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // Red background

            // Add text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(toastObj.transform);

            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = $"Ad Error: {message}";

            // Position text
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Auto-destroy after 3 seconds
            StartCoroutine(DestroyToastAfterDelay(toastObj, 3.0f));
        }

        /// <summary>
        /// Destroy toast after delay
        /// </summary>
        /// <param name="toastObj">Toast GameObject</param>
        /// <param name="delay">Delay in seconds</param>
        /// <returns>Coroutine</returns>
        private IEnumerator DestroyToastAfterDelay(GameObject toastObj, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (toastObj != null)
            {
                Destroy(toastObj);
            }
        }
    }

    /// <summary>
    /// Ad response structure - Identical to iOS
    /// </summary>
    [System.Serializable]
    public class AdResponse
    {
        public string adm;
        public int? position;
    }
}