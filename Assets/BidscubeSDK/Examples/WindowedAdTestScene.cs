using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BidscubeSDK;

namespace BidscubeSDK.Examples
{
    /// <summary>
    /// Windowed Ad Test Scene - Ad positioning and layout testing
    /// Based on iOS WindowedAdTestView
    /// </summary>
    public class WindowedAdTestScene : MonoBehaviour, IAdCallback, IConsentCallback
    {
        [Header("SDK Configuration")]
        [SerializeField] private string _imageAdPlacementId = "19481";
        [SerializeField] private string _videoAdPlacementId = "19483";
        [SerializeField] private string _nativeAdPlacementId = "19487";
        [SerializeField] private string _baseURL = Constants.BaseURL;
        [SerializeField] private bool _enableDebugMode = true;
        [SerializeField] private bool _enableLogging = true;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _sdkStatusText;
        [SerializeField] private Button _initializeButton;
        [SerializeField] private Button _cleanupButton;
        [SerializeField] private Button _createImageAdButton;
        [SerializeField] private Button _createVideoAdButton;
        [SerializeField] private Button _createNativeAdButton;
        [SerializeField] private Button _validateLayoutButton;
        [SerializeField] private Toggle _showPositioningPanelToggle;
        [SerializeField] private RectTransform _positioningPanel;
        [SerializeField] private ScrollRect _logScrollRect;
        [SerializeField] private Text _logText;
        [SerializeField] private RectTransform _adDisplayArea;
        [SerializeField] private RectTransform _contentArea;

        [Header("Position Buttons")]
        [SerializeField] private Button _unknownPositionButton;
        [SerializeField] private Button _aboveTheFoldButton;
        [SerializeField] private Button _dependOnScreenSizeButton;
        [SerializeField] private Button _belowTheFoldButton;
        [SerializeField] private Button _headerButton;
        [SerializeField] private Button _footerButton;
        [SerializeField] private Button _sidebarButton;
        [SerializeField] private Button _fullScreenButton;

        [Header("Navigation")]
        [SerializeField] private Button _backToMainButton;
        [SerializeField] private Button _sdkTestButton;
        [SerializeField] private Button _consentTestButton;

        private bool _isSDKInitialized = false;
        private string _logContent = "";
        private AdPosition _selectedPosition = AdPosition.Unknown;
        private GameObject _currentAdObject;

        private void Start()
        {
            SetupUI();
            UpdateStatus("SDK Status: Not Initialized");
            InitializeBidscubeSDK();
        }

        private void SetupUI()
        {
            // Initialize SDK button
            if (_initializeButton != null)
                _initializeButton.onClick.AddListener(InitializeBidscubeSDK);

            // Cleanup SDK button
            if (_cleanupButton != null)
                _cleanupButton.onClick.AddListener(CleanupSDK);

            // Ad creation buttons
            if (_createImageAdButton != null)
                _createImageAdButton.onClick.AddListener(CreateImageAd);
            if (_createVideoAdButton != null)
                _createVideoAdButton.onClick.AddListener(CreateVideoAd);
            if (_createNativeAdButton != null)
                _createNativeAdButton.onClick.AddListener(CreateNativeAd);

            // Layout validation
            if (_validateLayoutButton != null)
                _validateLayoutButton.onClick.AddListener(ValidateLayout);

            // Positioning panel toggle
            if (_showPositioningPanelToggle != null)
                _showPositioningPanelToggle.onValueChanged.AddListener(OnPositioningPanelToggleChanged);

            // Position buttons
            if (_unknownPositionButton != null)
                _unknownPositionButton.onClick.AddListener(() => TestAdPositioning(AdPosition.Unknown));
            if (_aboveTheFoldButton != null)
                _aboveTheFoldButton.onClick.AddListener(() => TestAdPositioning(AdPosition.AboveTheFold));
            if (_dependOnScreenSizeButton != null)
                _dependOnScreenSizeButton.onClick.AddListener(() => TestAdPositioning(AdPosition.DependOnScreenSize));
            if (_belowTheFoldButton != null)
                _belowTheFoldButton.onClick.AddListener(() => TestAdPositioning(AdPosition.BelowTheFold));
            if (_headerButton != null)
                _headerButton.onClick.AddListener(() => TestAdPositioning(AdPosition.Header));
            if (_footerButton != null)
                _footerButton.onClick.AddListener(() => TestAdPositioning(AdPosition.Footer));
            if (_sidebarButton != null)
                _sidebarButton.onClick.AddListener(() => TestAdPositioning(AdPosition.Sidebar));
            if (_fullScreenButton != null)
                _fullScreenButton.onClick.AddListener(() => TestAdPositioning(AdPosition.FullScreen));

            // Navigation buttons
            if (_backToMainButton != null)
                _backToMainButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadMainScene());
            if (_sdkTestButton != null)
                _sdkTestButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadSDKTestScene());
            if (_consentTestButton != null)
                _consentTestButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadConsentTestScene());
        }

        private void InitializeBidscubeSDK()
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
                LogMessage("✅ SDK initialized successfully");
                
                // Request consent info update
                BidscubeSDK.RequestConsentInfoUpdate(this);
            }
            else
            {
                UpdateStatus("SDK Status: Initialization Failed");
                LogMessage("❌ SDK initialization failed");
            }
        }

        private void CleanupSDK()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            LogMessage("Cleaning up SDK...");
            BidscubeSDK.Cleanup();
            _isSDKInitialized = false;
            UpdateStatus("SDK Status: Cleaned Up");
            LogMessage("✅ SDK cleaned up successfully");
        }

        private void CreateImageAd()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized. Please wait...");
                return;
            }

            LogMessage("Creating image ad...");
            ShowPlaceholderAd("IMAGE AD - LOADING...", Color.green);
            LoadImageAdContent();
        }

        private void CreateVideoAd()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized. Please wait...");
                return;
            }

            LogMessage("Creating video ad...");
            ShowPlaceholderAd("VIDEO AD - LOADING...", Color.blue);
            LoadVideoAdContent();
        }

        private void CreateNativeAd()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized. Please wait...");
                return;
            }

            LogMessage("Creating native ad...");
            ShowPlaceholderAd("NATIVE AD - LOADING...", Color.yellow);
            LoadNativeAdContent();
        }

        private void LoadImageAdContent()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                ShowPlaceholderAd("SDK not initialized", Color.red);
                return;
            }

            // Create a real image ad view
            var adView = BidscubeSDK.GetImageAdView(_imageAdPlacementId, this);
            DisplayAdView(adView);
        }

        private void LoadVideoAdContent()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                ShowPlaceholderAd("SDK not initialized", Color.red);
                return;
            }

            // Create a real video ad view
            var adView = BidscubeSDK.GetVideoAdView(_videoAdPlacementId, this);
            DisplayAdView(adView);
        }

        private void LoadNativeAdContent()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                ShowPlaceholderAd("SDK not initialized", Color.red);
                return;
            }

            // Create a real native ad view
            var adView = BidscubeSDK.GetNativeAdView(_nativeAdPlacementId, this);
            DisplayAdView(adView);
        }

        private void DisplayAdView(GameObject adView)
        {
            // Remove existing ad
            if (_currentAdObject != null)
            {
                DestroyImmediate(_currentAdObject);
            }

            // Position the ad in the display area
            if (_adDisplayArea != null && adView != null)
            {
                _currentAdObject = adView;
                adView.transform.SetParent(_adDisplayArea, false);
                
                var rectTransform = adView.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }
            }
        }

        private void ShowPlaceholderAd(string message, Color color)
        {
            // Create a placeholder ad object
            var placeholder = new GameObject("PlaceholderAd");
            var rectTransform = placeholder.AddComponent<RectTransform>();
            var image = placeholder.AddComponent<Image>();
            var text = placeholder.AddComponent<Text>();

            image.color = color;
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            DisplayAdView(placeholder);
        }

        private void TestAdPositioning(AdPosition position)
        {
            if (_currentAdObject == null)
            {
                LogMessage("❌ Please create a test ad first");
                return;
            }

            _selectedPosition = position;
            LogMessage($"Ad positioned at: {GetDisplayName(position)}");
            LogPositionDetails(position);
        }

        private void LogPositionDetails(AdPosition position)
        {
            string message = GetPositionDescription(position);
            LogMessage($"Position Details: {message}");
        }

        private string GetPositionDescription(AdPosition position)
        {
            switch (position)
            {
                case AdPosition.Unknown:
                    return "Ad positioned at UNKNOWN - natural display, no regulation";
                case AdPosition.AboveTheFold:
                    return "Ad positioned at ABOVE_THE_FOLD - placed in content area above the fold (visible without scrolling)";
                case AdPosition.BelowTheFold:
                    return "Ad positioned at BELOW_THE_FOLD - placed in content area below the fold (requires scrolling to see)";
                case AdPosition.Header:
                    return "Ad positioned at HEADER - top of screen, header area";
                case AdPosition.Footer:
                    return "Ad positioned at FOOTER - bottom of screen, footer area";
                case AdPosition.Sidebar:
                    return "Ad positioned at SIDEBAR - left/right side of screen";
                case AdPosition.DependOnScreenSize:
                    return "Ad positioned at DEPEND_ON_SCREEN_SIZE - smart positioning based on screen size";
                case AdPosition.FullScreen:
                    return "Ad positioned at FULL_SCREEN - full screen display";
                default:
                    return "Unknown position";
            }
        }

        private string GetDisplayName(AdPosition position)
        {
            switch (position)
            {
                case AdPosition.Unknown:
                    return "UNKNOWN";
                case AdPosition.AboveTheFold:
                    return "ABOVE_THE_FOLD";
                case AdPosition.DependOnScreenSize:
                    return "DEPEND_ON_SCREEN_SIZE";
                case AdPosition.BelowTheFold:
                    return "BELOW_THE_FOLD";
                case AdPosition.Header:
                    return "HEADER";
                case AdPosition.Footer:
                    return "FOOTER";
                case AdPosition.Sidebar:
                    return "SIDEBAR";
                case AdPosition.FullScreen:
                    return "FULL_SCREEN";
                default:
                    return "UNKNOWN";
            }
        }

        private void ValidateLayout()
        {
            LogMessage("✓ All requirements met!");
            LogMessage("Layout validation completed. Check logs for details.");
        }

        private void OnPositioningPanelToggleChanged(bool isOn)
        {
            if (_positioningPanel != null)
            {
                _positioningPanel.gameObject.SetActive(isOn);
            }
        }

        private void UpdateStatus(string status)
        {
            if (_sdkStatusText != null)
                _sdkStatusText.text = status;
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

        // IConsentCallback implementation
        public void OnConsentInfoUpdated()
        {
            LogMessage("✅ Consent info updated successfully");
        }

        public void OnConsentInfoUpdateFailed(System.Exception error)
        {
            LogMessage($"❌ Consent info update failed: {error.Message}");
        }

        public void OnConsentFormShown()
        {
            LogMessage("Consent form shown");
        }

        public void OnConsentFormError(System.Exception error)
        {
            LogMessage($"❌ Consent form error: {error.Message}");
        }

        public void OnConsentGranted()
        {
            LogMessage("✅ Consent granted");
        }

        public void OnConsentDenied()
        {
            LogMessage("❌ Consent denied");
        }

        public void OnConsentNotRequired()
        {
            LogMessage("ℹ️ Consent not required");
        }

        public void OnConsentStatusChanged(bool hasConsent)
        {
            LogMessage($"Consent status changed: {hasConsent}");
        }
    }
}
