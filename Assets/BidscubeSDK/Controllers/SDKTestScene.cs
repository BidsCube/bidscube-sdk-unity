using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BidscubeSDK;

namespace BidscubeSDK.Controllers
{
    /// <summary>
    /// SDK Test Scene - Basic SDK functionality testing
    /// Based on iOS SDKTestView
    /// </summary>
    public class SDKTestScene : MonoBehaviour, IAdCallback
    {
        [Header("SDK Configuration")]
        [SerializeField] private string _placementId = "19481";
        private string _baseURL = Constants.BaseURL;
        [SerializeField] private bool _enableDebugMode = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _bannerCornerRadius = 6.0f;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _sdkStatusText;
        [SerializeField] private TMP_InputField _placementIdInput;
        [SerializeField] private TextMeshProUGUI _currentAdPositionText;
        [SerializeField] private TextMeshProUGUI _activeBannersText;
        [SerializeField] private Button _initializeButton;
        [SerializeField] private Button _cleanupButton;
        [SerializeField] private Button _imageAdButton;
        [SerializeField] private Button _videoAdButton;
        [SerializeField] private Button _nativeAdButton;
        [SerializeField] private Button _testLoggingButton;
        [SerializeField] private Toggle _useManualPositionToggle;
        [SerializeField] private TMP_Dropdown _positionDropdown;
        [SerializeField] private ScrollRect _logScrollRect;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private RectTransform _adDisplayArea;

        [Header("Back to Main Button")]
        [SerializeField] private Button _backToMainButton;

        [Header("SDK Content")]
        [SerializeField] private GameObject _sdkContent;

        private bool _isSDKInitialized = false;
        private string _logContent = "";
        private AdPosition _selectedPosition = AdPosition.Unknown;
        private AdType? _lastDisplayedAdType;

        private void Start()
        {
            SetupUI();
            UpdateStatus("SDK Status: Not Initialized");
            UpdateActiveBannersCount();

            // Hide SDK content initially
            if (_sdkContent != null)
            {
                _sdkContent.SetActive(false);
            }
        }

        private void SetupUI()
        {
            // Initialize SDK button
            if (_initializeButton != null)
                _initializeButton.onClick.AddListener(InitializeSDK);

            // Cleanup SDK button
            if (_cleanupButton != null)
                _cleanupButton.onClick.AddListener(CleanupSDK);

            // Ad type buttons
            if (_imageAdButton != null)
                _imageAdButton.onClick.AddListener(() => ShowAd(AdType.Image));
            if (_videoAdButton != null)
                _videoAdButton.onClick.AddListener(() => ShowAd(AdType.Video));
            if (_nativeAdButton != null)
                _nativeAdButton.onClick.AddListener(() => ShowAd(AdType.Native));

            if (_testLoggingButton != null)
                _testLoggingButton.onClick.AddListener(TestLogging);

            // Position override toggle
            if (_useManualPositionToggle != null)
            {
                _useManualPositionToggle.isOn = false; // Default to OFF (use server response)
                _useManualPositionToggle.onValueChanged.AddListener(OnManualPositionToggleChanged);
            }

            // Position dropdown
            if (_positionDropdown != null)
            {
                _positionDropdown.onValueChanged.AddListener(OnPositionDropdownChanged);
                SetupPositionDropdown();
                _positionDropdown.value = 0; // Default to UNKNOWN position
                _selectedPosition = AdPosition.Unknown;
            }

            // Navigation buttons
            if (_backToMainButton != null)
                _backToMainButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadMainScene());

            // Placement ID input
            if (_placementIdInput != null)
            {
                _placementIdInput.text = _placementId;
                _placementIdInput.onEndEdit.AddListener(OnPlacementIdChanged);
            }
        }

        private void SetupPositionDropdown()
        {
            if (_positionDropdown == null) return;

            _positionDropdown.ClearOptions();
            _positionDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "UNKNOWN",
                "ABOVE_THE_FOLD",
                "DEPEND_ON_SCREEN_SIZE",
                "BELOW_THE_FOLD",
                "HEADER",
                "FOOTER",
                "SIDEBAR",
                "FULL_SCREEN"
            });
        }

        private void InitializeSDK()
        {
            LogMessage("Initializing Bidscube SDK...");

            var config = new SDKConfig.Builder()
                .EnableLogging(_enableLogging)
                .EnableDebugMode(_enableDebugMode)
                .BaseURL(_baseURL)
                .DefaultAdTimeout(30000)
                .DefaultAdPosition(AdPosition.Unknown)
                .Build();

            BidscubeSDK.Initialize(config);

            if (BidscubeSDK.IsInitialized())
            {
                _isSDKInitialized = true;
                UpdateStatus("SDK Status: Initialized");
                LogMessage("SDK initialized successfully");
                UpdateActiveBannersCount();

                // Show SDK content when initialized
                if (_sdkContent != null)
                {
                    _sdkContent.SetActive(true);
                }
            }
            else
            {
                UpdateStatus("SDK Status: Initialization Failed");
                LogMessage("SDK initialization failed");
            }
        }

        private void CleanupSDK()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("SDK not initialized");
                return;
            }

            LogMessage("Cleaning up SDK and removing all banners...");

            // Remove all active banners
            BidscubeSDK.RemoveAllBanners();

            // Cleanup SDK
            BidscubeSDK.Cleanup();

            _isSDKInitialized = false;
            UpdateStatus("SDK Status: Cleaned Up");
            LogMessage("SDK cleaned up successfully");
            UpdateActiveBannersCount();

            // Hide SDK content when cleaned up
            if (_sdkContent != null)
            {
                _sdkContent.SetActive(false);
            }
        }

        private void ShowAd(AdType adType)
        {
            if (!_isSDKInitialized)
            {
                LogMessage("SDK not initialized. Please initialize first.");
                return;
            }

            string placementId = _placementIdInput != null ? _placementIdInput.text : _placementId;
            if (string.IsNullOrEmpty(placementId))
            {
                LogMessage("Placement ID is required");
                return;
            }

            LogMessage($"Showing {adType} Ad...");

            // Handle manual position override
            if (_useManualPositionToggle != null && _useManualPositionToggle.isOn)
            {
                BidscubeSDK.SetAdPosition(_selectedPosition);
                LogMessage($"🔧 Using MANUAL position override: {_selectedPosition}");
            }
            else
            {
                BidscubeSDK.SetAdPosition(AdPosition.Unknown);
                LogMessage("🌐 Using SERVER RESPONSE position (default behavior)");
            }

            // Show the ad based on type
            switch (adType)
            {
                case AdType.Image:
                    ShowImageAdWithWebView(placementId);
                    break;
                case AdType.Video:
                    BidscubeSDK.ShowVideoAd(placementId, this);
                    break;
                case AdType.Native:
                    BidscubeSDK.ShowNativeAd(placementId, this);
                    break;
            }

            _lastDisplayedAdType = adType;
        }

        /// <summary>
        /// Show image ad using NewWebViewController with proper positioning
        /// </summary>
        private void ShowImageAdWithWebView(string placementId)
        {
            LogMessage($"Creating image ad with WebView for placement: {placementId}");

            // Find Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                LogMessage("No Canvas found in scene!");
                return;
            }

            // Create GameObject for image ad
            var imageAdGO = new GameObject($"ImageAd_{placementId}");
            imageAdGO.transform.SetParent(canvas.transform, false);

            // Add RectTransform for positioning
            var rectTransform = imageAdGO.AddComponent<RectTransform>();

            // Set position and size based on ad position
            var effectivePosition = GetEffectiveAdPosition();
            SetAdPositionAndSize(rectTransform, effectivePosition);

            // Add NewWebViewController
            var webViewController = imageAdGO.AddComponent<NewWebViewController>();

            // Set margins based on position
            SetWebViewMargins(webViewController, effectivePosition);

            // Add WebViewObject component
            var webViewObject = imageAdGO.AddComponent<WebViewObject>();

            // Assign canvas to WebViewObject
            webViewObject.canvas = canvas.gameObject;

            // Assign WebViewObject to NewWebViewController
            SetWebViewObjectField(webViewController, webViewObject);

            // Fetch ad content and set HTML
            StartCoroutine(FetchAdContentAndSetHTML(placementId, webViewController));

            LogMessage($"Image ad created with WebView at position: {effectivePosition}");
        }

        /// <summary>
        /// Get effective ad position (manual override or server response)
        /// </summary>
        private AdPosition GetEffectiveAdPosition()
        {
            if (_useManualPositionToggle != null && _useManualPositionToggle.isOn)
            {
                return _selectedPosition;
            }
            return AdPosition.Unknown; // Will use server response
        }

        /// <summary>
        /// Set ad position and size based on AdPosition
        /// </summary>
        private void SetAdPositionAndSize(RectTransform rectTransform, AdPosition position)
        {
            switch (position)
            {
                case AdPosition.FullScreen:
                    // Full screen
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;

                case AdPosition.Header:
                    // Top of screen
                    rectTransform.anchorMin = new Vector2(0, 0.8f);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;

                case AdPosition.Footer:
                    // Bottom of screen
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0.2f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;

                case AdPosition.Sidebar:
                    // Side of screen
                    rectTransform.anchorMin = new Vector2(0, 0.2f);
                    rectTransform.anchorMax = new Vector2(0.3f, 0.8f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;

                default:
                    // Default center position
                    rectTransform.anchorMin = new Vector2(0.1f, 0.1f);
                    rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;
            }
        }

        /// <summary>
        /// Set WebView margins based on position
        /// </summary>
        private void SetWebViewMargins(NewWebViewController webViewController, AdPosition position)
        {
            switch (position)
            {
                case AdPosition.FullScreen:
                    webViewController.LeftMargin = 0;
                    webViewController.RightMargin = 0;
                    webViewController.TopMargin = 0;
                    webViewController.BottomMargin = 0;
                    break;

                case AdPosition.Header:
                    webViewController.LeftMargin = 0;
                    webViewController.RightMargin = 0;
                    webViewController.TopMargin = 0;
                    webViewController.BottomMargin = 0;
                    break;

                case AdPosition.Footer:
                    webViewController.LeftMargin = 0;
                    webViewController.RightMargin = 0;
                    webViewController.TopMargin = 0;
                    webViewController.BottomMargin = 0;
                    break;

                default:
                    webViewController.LeftMargin = 0;
                    webViewController.RightMargin = 0;
                    webViewController.TopMargin = 0;
                    webViewController.BottomMargin = 0;
                    break;
            }
        }

        /// <summary>
        /// Set WebViewObject field using reflection
        /// </summary>
        private void SetWebViewObjectField(NewWebViewController webViewController, WebViewObject webViewObject)
        {
            var field = typeof(NewWebViewController).GetField("webViewObject",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(webViewController, webViewObject);
            }
        }

        /// <summary>
        /// Fetch ad content and set HTML content to WebViewController
        /// </summary>
        private System.Collections.IEnumerator FetchAdContentAndSetHTML(string placementId, NewWebViewController webViewController)
        {
            LogMessage($"Fetching ad content for placement: {placementId}");

            // Build ad request URL
            var adUrl = BuildAdRequestUrl(placementId);
            LogMessage($"🔗 Ad request URL: {adUrl}");

            // Make HTTP request to get ad response
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(adUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    LogMessage($"Ad response received: {request.downloadHandler.text.Length} characters");

                    try
                    {
                        // Parse JSON response
                        var adResponse = JsonUtility.FromJson<AdResponse>(request.downloadHandler.text);

                        if (adResponse != null && !string.IsNullOrEmpty(adResponse.adm))
                        {
                            LogMessage($"Extracted adm content: {adResponse.adm.Length} characters");
                            LogMessage($"Adm preview: {adResponse.adm.Substring(0, Mathf.Min(200, adResponse.adm.Length))}...");

                            // Check if adm content has proper HTML structure and wrap if needed
                            var wrappedHtml = WrapAdmContent(adResponse.adm);

                            // Set HTML content to WebViewController
                            LogMessage($"Setting HTML content to WebViewController: {wrappedHtml.Length} characters");
                            webViewController.HTMLad = wrappedHtml;
                            LogMessage("HTML content set to WebViewController");

                            // Force visibility to ensure content is shown
                            webViewController.SetVisibility(true);
                            LogMessage("WebView visibility set to true");

                            // Force reload to ensure content is displayed
                            webViewController.ReloadContent();
                            LogMessage("WebView content reloaded");
                        }
                        else
                        {
                            LogMessage("No adm content found in ad response");
                            webViewController.HTMLad = "<p>No ad content available</p>";
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogMessage($"Error parsing ad response: {e.Message}");
                        webViewController.HTMLad = "<p>Error loading ad content</p>";
                    }
                }
                else
                {
                    LogMessage($"Failed to fetch ad content: {request.error}");
                    webViewController.HTMLad = "<p>Failed to load ad content</p>";
                }
            }
        }

        /// <summary>
        /// Build ad request URL
        /// </summary>
        private string BuildAdRequestUrl(string placementId)
        {
            // Get effective position
            var effectivePosition = GetEffectiveAdPosition();

            // Use the URLBuilder to create the ad request URL with all required parameters
            return URLBuilder.BuildAdRequestURL(
                baseURL: Constants.BaseURL,
                placementId: placementId,
                adType: AdType.Image,
                position: effectivePosition,
                timeoutMs: 30000,
                debug: true,
                ctaText: "Learn More"
            );
        }

        /// <summary>
        /// Wrap adm content in proper HTML structure if needed
        /// </summary>
        private string WrapAdmContent(string admContent)
        {
            if (string.IsNullOrEmpty(admContent))
            {
                return "<p>No content available</p>";
            }

            // Check if content already has proper HTML structure
            var trimmedContent = admContent.Trim();
            if (trimmedContent.ToLower().StartsWith("<!doctype") ||
                trimmedContent.ToLower().StartsWith("<html"))
            {
                LogMessage("Adm content already has proper HTML structure");
                return admContent;
            }

            LogMessage("Wrapping adm content in proper HTML structure");

            // Wrap content in proper HTML structure
            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Ad Content</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: Arial, sans-serif;
            background-color: transparent;
        }}
        /* Ensure images are responsive */
        img {{
            max-width: 100%;
            height: auto;
        }}
        /* Ensure links work properly */
        a {{
            text-decoration: none;
            color: inherit;
        }}
        /* Ensure divs are properly displayed */
        div {{
            box-sizing: border-box;
        }}
    </style>
</head>
<body>
    {admContent}
</body>
</html>";
        }

        /// <summary>
        /// Ad response data structure
        /// </summary>
        [System.Serializable]
        private class AdResponse
        {
            public string adm;
            public string clickUrl;
            public string impressionUrl;
            public string title;
            public string description;
        }

        private void EnableLogging(bool enabled)
        {
            // This would need to be implemented in the SDK
            LogMessage($"Logging {(enabled ? "enabled" : "disabled")}");
        }

        private void TestLogging()
        {
            LogMessage("=== LOGGING TEST ===");
            LogMessage("This is a test log message");
            LogMessage("Testing different log levels...");
            LogMessage("=== LOGGING TEST COMPLETED ===");
        }

        private void OnManualPositionToggleChanged(bool isOn)
        {
            if (isOn)
            {
                LogMessage("🔧 Manual position override ENABLED - will use dropdown selection");
            }
            else
            {
                LogMessage("🌐 Manual position override DISABLED - will use server response position");
            }

            if (_lastDisplayedAdType.HasValue)
            {
                LogMessage("Refreshing current ad with new position mode...");
                ShowAd(_lastDisplayedAdType.Value);
            }
        }

        private void OnPositionDropdownChanged(int index)
        {
            _selectedPosition = (AdPosition)index;
            if (_useManualPositionToggle != null && _useManualPositionToggle.isOn && _lastDisplayedAdType.HasValue)
            {
                LogMessage("Refreshing current ad with new position...");
                ShowAd(_lastDisplayedAdType.Value);
            }
        }

        private void OnPlacementIdChanged(string newPlacementId)
        {
            _placementId = newPlacementId;
            LogMessage($"Placement ID updated to: {_placementId}");
        }

        private void UpdateStatus(string status)
        {
            if (_sdkStatusText != null)
                _sdkStatusText.text = status;
        }

        private void UpdateActiveBannersCount()
        {
            if (_activeBannersText != null && _isSDKInitialized)
            {
                int count = BidscubeSDK.GetActiveBannerCount();
                _activeBannersText.text = $"Active Banners: {count}";
            }
        }

        private void LogMessage(string message)
        {
            _logContent += $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";

            if (_logText != null)
            {
                _logText.text = _logContent;

                if (_logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    _logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        // IAdCallback implementation
        public void OnAdLoading(string placementId)
        {
            LogMessage($"Ad loading: {placementId}");
        }

        public void OnAdLoaded(string placementId)
        {
            LogMessage($"Ad loaded: {placementId}");
        }

        public void OnAdDisplayed(string placementId)
        {
            LogMessage($"Ad displayed: {placementId}");
        }

        public void OnAdClicked(string placementId)
        {
            LogMessage($"Ad clicked: {placementId}");
        }

        public void OnAdClosed(string placementId)
        {
            LogMessage($"Ad closed: {placementId}");
        }

        public void OnAdFailed(string placementId, int errorCode, string errorMessage)
        {
            LogMessage($"Ad failed: {placementId} - {errorMessage}");
        }

        public void OnVideoAdStarted(string placementId)
        {
            LogMessage($"Video ad started: {placementId}");
        }

        public void OnVideoAdCompleted(string placementId)
        {
            LogMessage($"Video ad completed: {placementId}");
        }

        public void OnVideoAdSkipped(string placementId)
        {
            LogMessage($"Video ad skipped: {placementId}");
        }

        public void OnVideoAdSkippable(string placementId)
        {
            LogMessage($"Video ad skippable: {placementId}");
        }

        public void OnInstallButtonClicked(string placementId, string buttonText)
        {
            LogMessage($"Install button clicked: {placementId} - {buttonText}");
        }
    }
}
