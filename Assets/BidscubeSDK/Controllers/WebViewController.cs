using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BidscubeSDK
{
    /// <summary>
    /// WebView controller for rendering HTML content
    /// </summary>
    public class WebViewController : MonoBehaviour
    {
        [Header("WebView Settings")]
        [SerializeField] private WebViewObject _webViewObject;
        [SerializeField] private string _html;

        [Header("Callbacks")]
        [SerializeField] private System.Action<string> _onHtmlLoaded;
        [SerializeField] private System.Action<string> _onError;
        [SerializeField] private System.Action<string> _onMessage;

        /// <summary>
        /// Initialize WebView controller for HTML rendering
        /// </summary>
        /// <param name="onHtmlLoaded">Callback when HTML is loaded</param>
        /// <param name="onError">Callback when error occurs</param>
        /// <param name="onMessage">Callback for JavaScript messages</param>
        public void Initialize(System.Action<string> onHtmlLoaded = null, System.Action<string> onError = null, System.Action<string> onMessage = null)
        {
            _onHtmlLoaded = onHtmlLoaded;
            _onError = onError;
            _onMessage = onMessage;

            Logger.Info($"WebViewController: Initializing for HTML rendering");
            CreateWebView();
        }

        /// <summary>
        /// Load HTML content into WebView
        /// </summary>
        /// <param name="htmlContent">HTML content to load</param>
        /// <param name="baseUrl">Base URL for relative links</param>
        public void LoadHTML(string htmlContent, string baseUrl = "")
        {
            if (_webViewObject == null)
            {
                Logger.InfoError("WebViewController: WebViewObject is null, cannot load HTML");
                return;
            }

            // Store HTML content
            _html = htmlContent;

            Logger.Info($"WebViewController: Loading HTML content");
            Logger.Info($"WebViewController: HTML content length: {htmlContent.Length}");
            Logger.Info($"WebViewController: HTML preview: {htmlContent.Substring(0, Mathf.Min(200, htmlContent.Length))}...");

            // Wrap HTML content in proper HTML structure if needed
            string wrappedHtml = WrapHtmlContent(htmlContent);

            // Ensure WebView is visible before loading
            _webViewObject.SetVisibility(true);

            // Load HTML content
            _webViewObject.LoadHTML(wrappedHtml, baseUrl);

            Logger.Info($"WebViewController: HTML content loaded into WebView");

            // Refresh margins after a short delay to ensure layout is complete
            StartCoroutine(DelayedMarginRefresh());
        }

        /// <summary>
        /// Delayed margin refresh to ensure layout is complete
        /// </summary>
        private IEnumerator DelayedMarginRefresh()
        {
            yield return null; // Wait one frame
            yield return null; // Wait another frame for layout to settle
            UpdateWebViewMargins();
            // Ensure visibility is still true
            if (_webViewObject != null)
            {
                _webViewObject.SetVisibility(true);
            }
        }

        /// <summary>
        /// Test HTML rendering with a simple test page
        /// </summary>
        public void LoadTestHTML()
        {
            var testHtml = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Test Ad</title>
    <style>
        body { 
            margin: 0; 
            background: #ff0000; 
            display: flex; 
            justify-content: center; 
            align-items: center; 
            height: 100vh; 
            color: white;
            font-family: Arial, sans-serif;
        }
        .test-content {
            text-align: center;
            font-size: 24px;
        }
    </style>
</head>
<body>
    <div class='test-content'>
        <h1>Test Ad Loaded!</h1>
        <p>If you can see this, the WebView is working!</p>
    </div>
</body>
</html>";

            LoadHTML(testHtml, "");
        }

        /// <summary>
        /// Load your specific HTML content
        /// </summary>
        [ContextMenu("Load Your HTML")]
        public void LoadYourHTML()
        {
            var yourHtml = @"<h3>Welcome to the real-time HTML editor!</h3> <p>Type HTML in the textarea above, and it will magically appear in the frame below.</p>";
            LoadHTML(yourHtml, "");
        }


        /// <summary>
        /// Set WebView margins
        /// </summary>
        /// <param name="left">Left margin</param>
        /// <param name="top">Top margin</param>
        /// <param name="right">Right margin</param>
        /// <param name="bottom">Bottom margin</param>
        public void SetMargins(int left, int top, int right, int bottom)
        {
            if (_webViewObject != null)
            {
                _webViewObject.SetMargins(left, top, right, bottom);
            }
        }

        /// <summary>
        /// Evaluate JavaScript in WebView
        /// </summary>
        /// <param name="js">JavaScript code to execute</param>
        public void EvaluateJS(string js)
        {
            if (_webViewObject != null)
            {
                _webViewObject.EvaluateJS(js);
            }
        }

        /// <summary>
        /// Get current HTML content
        /// </summary>
        /// <returns>Current HTML content</returns>
        public string GetCurrentHtml()
        {
            return _html;
        }

        /// <summary>
        /// Wrap HTML content in proper HTML structure if needed
        /// </summary>
        /// <param name="htmlContent">Raw HTML content</param>
        /// <returns>Properly wrapped HTML</returns>
        private string WrapHtmlContent(string htmlContent)
        {
            // Check if content already has proper HTML structure
            if (htmlContent.Trim().ToLower().StartsWith("<!doctype") ||
                htmlContent.Trim().ToLower().StartsWith("<html"))
            {
                return htmlContent;
            }

            // Wrap content in proper HTML structure
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>WebView Content</title>
    <style>
        body {{
            margin: 0;
            padding: 20px;
            font-family: Arial, sans-serif;
            background-color: #f0f0f0;
        }}
        h1, h2, h3, h4, h5, h6 {{
            color: #333;
            margin-top: 0;
        }}
        p {{
            line-height: 1.6;
            color: #666;
        }}
    </style>
</head>
<body>
    {htmlContent}
</body>
</html>";
        }



        /// <summary>
        /// Destroy WebView
        /// </summary>
        public void Destroy()
        {
            if (_webViewObject == null)
                return;

            _webViewObject.SetVisibility(false);
            // Use Destroy instead of DestroyImmediate to avoid multiple destroy issues
            Destroy(_webViewObject.gameObject);
            _webViewObject = null;
        }

        /// <summary>
        /// Create WebView object for HTML rendering
        /// </summary>
        private void CreateWebView()
        {
            try
            {
                // Create WebView GameObject
                var webViewGO = new GameObject("WebViewObject");
                webViewGO.transform.SetParent(transform, false);

                // Add WebViewObject component
                _webViewObject = webViewGO.AddComponent<WebViewObject>();

                // Get Canvas for WebView positioning
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    _webViewObject.canvas = canvas.gameObject;
                }

                // Initialize WebView with callbacks for HTML rendering
                _webViewObject.Init(
                    cb: OnWebViewMessage,
                    err: OnWebViewError,
                    httpErr: OnWebViewHttpError,
                    ld: OnWebViewLoaded,
                    started: OnWebViewStarted,
                    hooked: OnWebViewHooked,
                    transparent: false,
                    zoom: true,
                    ua: "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
                );

                // Set initial visibility and settings
                _webViewObject.SetVisibility(true);
                _webViewObject.SetInteractionEnabled(true);
                _webViewObject.SetScrollbarsVisibility(true);

                // Calculate margins based on parent RectTransform's screen position
                // This ensures WebView renders within the ad container, not full screen
                UpdateWebViewMargins();

                // Set refresh cycle for better performance
                _webViewObject.bitmapRefreshCycle = 1;
                _webViewObject.devicePixelRatio = 1;

                Logger.Info("WebViewController: WebView created successfully for HTML rendering");
                Logger.Info($"WebViewController: WebView visibility: {_webViewObject.GetVisibility()}");
                Logger.Info($"WebViewController: WebView initialized: {_webViewObject.IsInitialized()}");
            }
            catch (System.Exception e)
            {
                Logger.InfoError($"WebViewController: Failed to create WebView: {e.Message}");
                _onError?.Invoke(e.Message);
            }
        }

        #region WebViewObject Callbacks

        /// <summary>
        /// WebView message callback
        /// </summary>
        /// <param name="message">Message from WebView</param>
        private void OnWebViewMessage(string message)
        {
            Logger.Info($"WebViewController: WebView message: {message}");
            _onMessage?.Invoke(message);
        }

        /// <summary>
        /// WebView started callback
        /// </summary>
        /// <param name="url">Starting URL</param>
        private void OnWebViewStarted(string url)
        {
            Logger.Info($"WebViewController: WebView started loading HTML");
        }

        /// <summary>
        /// WebView error callback
        /// </summary>
        /// <param name="error">Error message</param>
        private void OnWebViewError(string error)
        {
            Logger.InfoError($"WebViewController: WebView error: {error}");
            _onError?.Invoke(error);
        }

        /// <summary>
        /// WebView HTTP error callback
        /// </summary>
        /// <param name="error">HTTP error message</param>
        private void OnWebViewHttpError(string error)
        {
            Logger.InfoError($"WebViewController: WebView HTTP error: {error}");
            _onError?.Invoke(error);
        }

        /// <summary>
        /// WebView loaded callback
        /// </summary>
        /// <param name="url">Loaded URL</param>
        private void OnWebViewLoaded(string url)
        {
            Logger.Info($"WebViewController: HTML content loaded successfully");
            Logger.Info($"WebViewController: Loaded URL: {url}");
            Logger.Info($"WebViewController: WebView visibility: {_webViewObject.GetVisibility()}");
            Logger.Info($"WebViewController: WebView initialized: {_webViewObject.IsInitialized()}");

            // Ensure WebView is visible after loading
            if (_webViewObject != null)
            {
                _webViewObject.SetVisibility(true);
            }

            // Refresh margins after HTML loads to ensure correct positioning
            StartCoroutine(DelayedMarginRefresh());

            _onHtmlLoaded?.Invoke(url);
        }

        /// <summary>
        /// WebView hooked callback
        /// </summary>
        /// <param name="url">Hooked URL</param>
        private void OnWebViewHooked(string url)
        {
            Logger.Info($"WebViewController: WebView hooked: {url}");
        }

        #endregion

        /// <summary>
        /// Unity OnDestroy
        /// </summary>
        private void OnDestroy()
        {
            Destroy();
        }

        /// <summary>
        /// Update WebView margins based on parent RectTransform's screen position
        /// This ensures WebView renders within the ad container, not full screen
        /// </summary>
        private void UpdateWebViewMargins()
        {
            if (_webViewObject == null) return;

            // Force canvas update to ensure layout is complete
            Canvas.ForceUpdateCanvases();

            // Get parent RectTransform (WebViewHost - this GameObject)
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                // Fallback to full screen if no RectTransform
                _webViewObject.SetMargins(0, 0, 0, 0);
                Logger.InfoError("[WebViewController] No RectTransform found, using full screen margins");
                return;
            }

            // Get Canvas to convert RectTransform to screen coordinates
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                // Fallback to full screen if no Canvas
                _webViewObject.SetMargins(0, 0, 0, 0);
                Logger.InfoError("[WebViewController] No Canvas found, using full screen margins");
                return;
            }

            // Log RectTransform details for debugging
            Logger.Info($"[WebViewController] RectTransform details: rect={rectTransform.rect}, anchoredPosition={rectTransform.anchoredPosition}, sizeDelta={rectTransform.sizeDelta}, anchors=({rectTransform.anchorMin}, {rectTransform.anchorMax})");

            // Convert RectTransform bounds to screen coordinates
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // Log world corners for debugging
            Logger.Info($"[WebViewController] World corners: bottomLeft={corners[0]}, topLeft={corners[1]}, topRight={corners[2]}, bottomRight={corners[3]}");

            // Get screen coordinates
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera ?? Camera.main;
            Logger.Info($"[WebViewController] Canvas renderMode={canvas.renderMode}, Camera={(cam != null ? cam.name : "null")}, Screen size={Screen.width}x{Screen.height}");

            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

            Logger.Info($"[WebViewController] Screen coordinates before clamping: min={min}, max={max}");

            // Ensure coordinates are valid (not negative or out of bounds)
            min.x = Mathf.Max(0, Mathf.Min(min.x, Screen.width));
            min.y = Mathf.Max(0, Mathf.Min(min.y, Screen.height));
            max.x = Mathf.Max(0, Mathf.Min(max.x, Screen.width));
            max.y = Mathf.Max(0, Mathf.Min(max.y, Screen.height));

            Logger.Info($"[WebViewController] Screen coordinates after clamping: min={min}, max={max}");

            // Calculate margins (left, top, right, bottom in screen pixels)
            // Note: Unity's screen coordinates have (0,0) at bottom-left
            int left = (int)min.x;
            int bottom = (int)min.y;
            int right = Screen.width - (int)max.x;
            int top = Screen.height - (int)max.y;

            // Clamp margins to valid range
            left = Mathf.Max(0, left);
            top = Mathf.Max(0, top);
            right = Mathf.Max(0, right);
            bottom = Mathf.Max(0, bottom);

            // Check if calculated size is reasonable (not too small or invalid)
            float calculatedWidth = max.x - min.x;
            float calculatedHeight = max.y - min.y;

            Logger.Info($"[WebViewController] Calculated margins: left={left}, top={top}, right={right}, bottom={bottom}, calculated size={calculatedWidth}x{calculatedHeight}, rectTransform size={rectTransform.rect.width}x{rectTransform.rect.height}");

            // If calculated size is too small or invalid, use full screen as fallback
            if (calculatedWidth < 10 || calculatedHeight < 10 ||
                rectTransform.rect.width < 10 || rectTransform.rect.height < 10)
            {
                Logger.InfoError($"[WebViewController] Calculated size too small ({calculatedWidth}x{calculatedHeight}) or rectTransform size invalid ({rectTransform.rect.width}x{rectTransform.rect.height}), using full screen fallback");
                _webViewObject.SetMargins(0, 0, 0, 0);
            }
            else
            {
                _webViewObject.SetMargins(left, top, right, bottom);
                Logger.Info($"[WebViewController] Successfully set margins: left={left}, top={top}, right={right}, bottom={bottom}, WebView size={calculatedWidth}x{calculatedHeight}");
            }

            // Always ensure WebView is visible after setting margins
            if (_webViewObject != null)
            {
                _webViewObject.SetVisibility(true);
            }
        }

        /// <summary>
        /// Call this when the parent RectTransform size/position changes
        /// </summary>
        public void RefreshMargins()
        {
            UpdateWebViewMargins();
        }
    }
}
