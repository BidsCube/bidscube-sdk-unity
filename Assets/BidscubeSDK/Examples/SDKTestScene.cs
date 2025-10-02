using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BidscubeSDK;

namespace BidscubeSDK.Examples
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
                LogMessage("✅ SDK initialized successfully");
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

            LogMessage("Cleaning up SDK and removing all banners...");
            
            // Remove all active banners
            BidscubeSDK.RemoveAllBanners();
            
            // Cleanup SDK
            BidscubeSDK.Cleanup();
            
            _isSDKInitialized = false;
            UpdateStatus("SDK Status: Cleaned Up");
            LogMessage("✅ SDK cleaned up successfully");
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
                LogMessage("❌ SDK not initialized. Please initialize first.");
                return;
            }

            string placementId = _placementIdInput != null ? _placementIdInput.text : _placementId;
            if (string.IsNullOrEmpty(placementId))
            {
                LogMessage("❌ Placement ID is required");
                return;
            }

            LogMessage($"🖼️ Showing {adType} Ad...");

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
