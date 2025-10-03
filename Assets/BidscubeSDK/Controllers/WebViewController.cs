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

            Debug.Log($"WebViewController: Initializing for HTML rendering");
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
                Debug.LogError("WebViewController: WebViewObject is null, cannot load HTML");
                return;
            }

            // Store HTML content
            _html = htmlContent;

            Debug.Log($"WebViewController: Loading HTML content");
            Debug.Log($"WebViewController: HTML content length: {htmlContent.Length}");
            Debug.Log($"WebViewController: HTML preview: {htmlContent.Substring(0, Mathf.Min(200, htmlContent.Length))}...");

            // Wrap HTML content in proper HTML structure if needed
            string wrappedHtml = WrapHtmlContent(htmlContent);

            // Load HTML content
            _webViewObject.LoadHTML(wrappedHtml, baseUrl);

            Debug.Log($"WebViewController: HTML content loaded into WebView");
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

                // Set margins to cover full screen
                _webViewObject.SetMargins(0, 0, 0, 0);

                // Set refresh cycle for better performance
                _webViewObject.bitmapRefreshCycle = 1;
                _webViewObject.devicePixelRatio = 1;

                Debug.Log("WebViewController: WebView created successfully for HTML rendering");
                Debug.Log($"WebViewController: WebView visibility: {_webViewObject.GetVisibility()}");
                Debug.Log($"WebViewController: WebView initialized: {_webViewObject.IsInitialized()}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"WebViewController: Failed to create WebView: {e.Message}");
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
            Debug.Log($"WebViewController: WebView message: {message}");
            _onMessage?.Invoke(message);
        }

        /// <summary>
        /// WebView started callback
        /// </summary>
        /// <param name="url">Starting URL</param>
        private void OnWebViewStarted(string url)
        {
            Debug.Log($"WebViewController: WebView started loading HTML");
        }

        /// <summary>
        /// WebView error callback
        /// </summary>
        /// <param name="error">Error message</param>
        private void OnWebViewError(string error)
        {
            Debug.LogError($"WebViewController: WebView error: {error}");
            _onError?.Invoke(error);
        }

        /// <summary>
        /// WebView HTTP error callback
        /// </summary>
        /// <param name="error">HTTP error message</param>
        private void OnWebViewHttpError(string error)
        {
            Debug.LogError($"WebViewController: WebView HTTP error: {error}");
            _onError?.Invoke(error);
        }

        /// <summary>
        /// WebView loaded callback
        /// </summary>
        /// <param name="url">Loaded URL</param>
        private void OnWebViewLoaded(string url)
        {
            Debug.Log($"WebViewController: HTML content loaded successfully");
            Debug.Log($"WebViewController: Loaded URL: {url}");
            Debug.Log($"WebViewController: WebView visibility: {_webViewObject.GetVisibility()}");
            Debug.Log($"WebViewController: WebView initialized: {_webViewObject.IsInitialized()}");
            _onHtmlLoaded?.Invoke(url);
        }

        /// <summary>
        /// WebView hooked callback
        /// </summary>
        /// <param name="url">Hooked URL</param>
        private void OnWebViewHooked(string url)
        {
            Debug.Log($"WebViewController: WebView hooked: {url}");
        }

        #endregion

        /// <summary>
        /// Unity OnDestroy
        /// </summary>
        private void OnDestroy()
        {
            Destroy();
        }
    }
}
