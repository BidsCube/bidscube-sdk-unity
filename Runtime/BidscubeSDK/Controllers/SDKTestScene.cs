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
        [SerializeField] private string _placementId = "";
        private string _baseURL = Constants.BaseURL;
        [SerializeField] private bool _enableDebugMode = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _bannerCornerRadius = 6.0f;
        [Tooltip("Optional: Ad size settings asset to control default ad sizes")]
        [SerializeField] private AdSizeSettings _adSizeSettings = null;

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
        [SerializeField] private Button _clearAllAdsButton;
        [SerializeField] private Toggle _useManualPositionToggle;
        [SerializeField] private TMP_Dropdown _positionDropdown;
        [SerializeField] private ScrollRect _logScrollRect;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private RectTransform _adDisplayArea;

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

            // Clear all ads button
            if (_clearAllAdsButton != null)
                _clearAllAdsButton.onClick.AddListener(ClearAllAds);

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

            // Placement ID input – start EMPTY so defaults are used unless user overrides
            if (_placementIdInput != null)
            {
                _placementIdInput.text = string.Empty;
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
                .AdSizeSettings(_adSizeSettings)
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

            // Cleanup SDK (which will clear all ads)
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

        private void ClearAllAds()
        {
            LogMessage("Clearing all ads (banners, images, natives, videos)...");
            BidscubeSDK.ClearAllAds();
            UpdateActiveBannersCount();
            LogMessage("All ads cleared");
        }

        private void ShowAd(AdType adType)
        {
            if (!_isSDKInitialized)
            {
                LogMessage("SDK not initialized. Please initialize first.");
                return;
            }

            string inputPlacement = _placementIdInput != null ? _placementIdInput.text.Trim() : string.Empty;
            string placementId;

            if (!string.IsNullOrEmpty(inputPlacement))
            {
                placementId = inputPlacement;
            }
            else
            {
                switch (adType)
                {
                    case AdType.Image:
                        placementId = "20212"; // default for Banner/Image
                        break;
                    case AdType.Video:
                        placementId = "20213"; // default for Video
                        break;
                    case AdType.Native:
                        placementId = "20214"; // default for Native
                        break;
                    default:
                        placementId = _placementId; // fallback to serialized field
                        break;
                }

                LogMessage($"Using default placement ID {placementId} for {adType} (input empty)");
            }

            LogMessage($"Showing {adType} Ad with placementId={placementId}...");

            // Determine position: manual override from dropdown is highest priority
            AdPosition positionToSend = AdPosition.Unknown;
            if (_useManualPositionToggle != null && _useManualPositionToggle.isOn)
            {
                positionToSend = _selectedPosition;
                LogMessage($"🔧 Using MANUAL position override: {positionToSend}");
            }
            else
            {
                LogMessage("🌐 Using SERVER RESPONSE position (default behavior)");
            }

            // Set the determined position in the SDK so the AdViewController receives it
            BidscubeSDK.SetAdPosition(positionToSend);

            // Show the ad based on type
            switch (adType)
            {
                case AdType.Image:
                    BidscubeSDK.ShowImageAd(placementId, this);
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

            // Assign canvas to WebViewObject (macOS only)
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            webViewObject.canvas = canvas.gameObject;
#endif

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
        /// This controls the GameObject container size, HTML content is always full-screen
        /// </summary>
        private void SetAdPositionAndSize(RectTransform rectTransform, AdPosition position)
        {
            LogMessage($"Setting GameObject container size for position: {position}");

            switch (position)
            {
                case AdPosition.FullScreen:
                    // Full screen GameObject container - covers entire screen
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    LogMessage("GameObject container: Full screen (entire screen)");
                    break;

                case AdPosition.Header:
                    // Header GameObject container - top banner style
                    rectTransform.anchorMin = new Vector2(0, 0.9f);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    LogMessage("GameObject container: Header (top banner - 10% height)");
                    break;

                case AdPosition.Footer:
                    // Footer GameObject container - bottom banner style
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0.1f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    LogMessage("GameObject container: Footer (bottom banner - 10% height)");
                    break;

                case AdPosition.Sidebar:
                    // Sidebar GameObject container - left or right side panel
                    rectTransform.anchorMin = new Vector2(0, 0.1f);
                    rectTransform.anchorMax = new Vector2(0.25f, 0.9f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    LogMessage("GameObject container: Sidebar (left panel - 25% width)");
                    break;

                case AdPosition.AboveTheFold:
                    // Above the fold GameObject container - top half of screen
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    LogMessage("GameObject container: Above the fold (top 50% of screen)");
                    break;

                case AdPosition.BelowTheFold:
                    // Below the fold GameObject container - bottom half of screen
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    LogMessage("GameObject container: Below the fold (bottom 50% of screen)");
                    break;

                case AdPosition.DependOnScreenSize:
                    // Screen size dependent - use responsive sizing
                    float screenAspect = (float)Screen.width / Screen.height;
                    if (screenAspect > 1.5f) // Wide screen
                    {
                        rectTransform.anchorMin = new Vector2(0.2f, 0.2f);
                        rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
                        LogMessage("GameObject container: DependOnScreenSize (wide screen - 60% center)");
                    }
                    else // Tall screen
                    {
                        rectTransform.anchorMin = new Vector2(0.1f, 0.1f);
                        rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
                        LogMessage("GameObject container: DependOnScreenSize (tall screen - 80% center)");
                    }
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;

                default:
                    // Default center position GameObject container
                    rectTransform.anchorMin = new Vector2(0.1f, 0.1f);
                    rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    LogMessage("GameObject container: Default center (80% of screen)");
                    break;
            }

            LogMessage($"GameObject container positioned: anchorMin={rectTransform.anchorMin}, anchorMax={rectTransform.anchorMax}");
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

            // TODO: Get effective position to determine if we need full-screen styling
            // var effectivePosition = GetEffectiveAdPosition();
            // bool isFullScreen = effectivePosition == AdPosition.FullScreen;

            // FOR NOW: Always use full-screen styling
            bool isFullScreen = true;

            // Wrap content in proper HTML structure with responsive full-screen support
            return $@"<!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Ad Content</title>
            <style>
                * {{
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }}

                body {{
                    width: 100vw;
                    height: 100vh;
                    margin: 0;
                    padding: 0;
                    font-family: Arial, sans-serif;
                    background-color: transparent;
                    overflow: hidden;
                }}

                .ad-container {{
                    width: 100vw;
                    height: 100vh;
                    position: absolute;
                    top: 0;
                    left: 0;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                }}

                .ad-content {{
                    width: 100vw;
                    height: 100vh;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                }}

                /* Force all content to be full screen */
                .ad-content > div {{
                    width: 100vw !important;
                    height: 100vh !important;
                    position: relative;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                }}

                /* Force images to be full screen */
                img {{
                    width: 100vw !important;
                    height: 100vh !important;
                    object-fit: cover;
                    display: block;
                }}

                /* Force links to be full screen */
                a {{
                    width: 100vw !important;
                    height: 100vh !important;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    text-decoration: none;
                    color: inherit;
                }}

                /* Override any fixed dimensions */
                div[style*='width:300px'], div[style*='width: 300px'] {{
                    width: 100vw !important;
                    height: 100vh !important;
                }}

                div[style*='height:150px'], div[style*='height: 150px'] {{
                    width: 100vw !important;
                    height: 100vh !important;
                }}

                /* Ensure divs are properly displayed and full screen */
                div {{
                    box-sizing: border-box;
                }}

                /* Full screen specific styles - ALWAYS APPLIED FOR NOW */
                {(isFullScreen ? @"
                .ad-container {
                    width: 100vw;
                    height: 100vh;
                    position: absolute;
                    top: 0;
                    left: 0;
                }

                .ad-content {
                    width: 100vw;
                    height: 100vh;
                }

                .ad-content > div {
                    width: 100vw !important;
                    height: 100vh !important;
                }

                img {
                    width: 100vw !important;
                    height: 100vh !important;
                    object-fit: cover;
                }
                " : "")}
            </style>
        </head>
        <body>
            <div class='ad-container'>
                <div class='ad-content'>
                    {admContent}
                </div>
            </div>
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

        // Added missing OnAdRenderOverride (IAdCallback)
        public bool OnAdRenderOverride(string adm, int position)
        {
            int admLen = adm != null ? adm.Length : 0;
            LogMessage($"OnAdRenderOverride called: position={position}, admLength={admLen}");
            return false; // Let SDK render by default
        }
    }
}
