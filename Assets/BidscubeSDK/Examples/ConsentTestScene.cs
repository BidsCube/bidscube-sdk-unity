using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BidscubeSDK;

namespace BidscubeSDK.Examples
{
    /// <summary>
    /// Consent Test Scene - Consent form and consent management testing
    /// Based on iOS ConsentTestView
    /// </summary>
    public class ConsentTestScene : MonoBehaviour, IAdCallback, IConsentCallback
    {
        [Header("SDK Configuration")]
        [SerializeField] private string _placementId = "19481";
        [SerializeField] private string _baseURL = Constants.BaseURL;
        [SerializeField] private bool _enableDebugMode = true;
        [SerializeField] private bool _enableLogging = true;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _sdkStatusText;
        [SerializeField] private TMP_InputField _placementIdInput;
        [SerializeField] private Button _initializeButton;
        [SerializeField] private Button _cleanupButton;
        [SerializeField] private Button _requestConsentInfoButton;
        [SerializeField] private Button _showConsentFormButton;
        [SerializeField] private Button _checkConsentRequiredButton;
        [SerializeField] private Button _checkAdsConsentButton;
        [SerializeField] private Button _checkAnalyticsConsentButton;
        [SerializeField] private Button _getConsentSummaryButton;
        [SerializeField] private Button _enableDebugModeButton;
        [SerializeField] private Button _resetConsentButton;
        [SerializeField] private Button _showImageAdButton;
        [SerializeField] private Button _showVideoAdButton;
        [SerializeField] private Button _showNativeAdButton;
        [SerializeField] private ScrollRect _logScrollRect;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private RectTransform _adDisplayArea;

        [Header("Navigation")]
        [SerializeField] private Button _backToMainButton;
        [SerializeField] private Button _sdkTestButton;
        [SerializeField] private Button _windowedAdButton;

        private bool _isSDKInitialized = false;
        private string _logContent = "";

        private void Start()
        {
            SetupUI();
            UpdateStatus("SDK Status: Not Initialized");
        }

        private void SetupUI()
        {
            // Initialize SDK button
            if (_initializeButton != null)
                _initializeButton.onClick.AddListener(InitializeSDK);

            // Cleanup SDK button
            if (_cleanupButton != null)
                _cleanupButton.onClick.AddListener(CleanupSDK);

            // Consent management buttons
            if (_requestConsentInfoButton != null)
                _requestConsentInfoButton.onClick.AddListener(RequestConsentInfoUpdate);
            if (_showConsentFormButton != null)
                _showConsentFormButton.onClick.AddListener(ShowConsentForm);
            if (_checkConsentRequiredButton != null)
                _checkConsentRequiredButton.onClick.AddListener(CheckConsentRequired);
            if (_checkAdsConsentButton != null)
                _checkAdsConsentButton.onClick.AddListener(CheckAdsConsent);
            if (_checkAnalyticsConsentButton != null)
                _checkAnalyticsConsentButton.onClick.AddListener(CheckAnalyticsConsent);
            if (_getConsentSummaryButton != null)
                _getConsentSummaryButton.onClick.AddListener(GetConsentSummary);
            if (_enableDebugModeButton != null)
                _enableDebugModeButton.onClick.AddListener(EnableDebugMode);
            if (_resetConsentButton != null)
                _resetConsentButton.onClick.AddListener(ResetConsent);

            // Ad testing buttons
            if (_showImageAdButton != null)
                _showImageAdButton.onClick.AddListener(() => ShowAdIfConsent(AdType.Image));
            if (_showVideoAdButton != null)
                _showVideoAdButton.onClick.AddListener(() => ShowAdIfConsent(AdType.Video));
            if (_showNativeAdButton != null)
                _showNativeAdButton.onClick.AddListener(() => ShowAdIfConsent(AdType.Native));

            // Navigation buttons
            if (_backToMainButton != null)
                _backToMainButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadMainScene());
            if (_sdkTestButton != null)
                _sdkTestButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadSDKTestScene());
            if (_windowedAdButton != null)
                _windowedAdButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadWindowedAdScene());

            // Placement ID input
            if (_placementIdInput != null)
            {
                _placementIdInput.text = _placementId;
                _placementIdInput.onEndEdit.AddListener(OnPlacementIdChanged);
            }
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
                UpdateConsentStatus();
                LogMessage("✅ SDK initialized successfully");
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

        private void RequestConsentInfoUpdate()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            LogMessage("Requesting consent info update...");
            BidscubeSDK.RequestConsentInfoUpdate(this);
        }

        private void ShowConsentForm()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            LogMessage("Showing consent form...");
            BidscubeSDK.ShowConsentForm(this);
        }

        private void CheckConsentRequired()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            bool isRequired = BidscubeSDK.IsConsentRequired();
            LogMessage($"Consent required: {isRequired}");
        }

        private void CheckAdsConsent()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            bool hasConsent = BidscubeSDK.HasAdsConsent();
            LogMessage($"Ads consent: {hasConsent}");
        }

        private void CheckAnalyticsConsent()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            bool hasConsent = BidscubeSDK.HasAnalyticsConsent();
            LogMessage($"Analytics consent: {hasConsent}");
        }

        private void GetConsentSummary()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            string summary = BidscubeSDK.GetConsentStatusSummary();
            LogMessage($"Consent Summary: {summary}");
        }

        private void EnableDebugMode()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            BidscubeSDK.EnableConsentDebugMode("test_device_123");
            LogMessage("Debug mode enabled for test device");
        }

        private void ResetConsent()
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            BidscubeSDK.ResetConsent();
            LogMessage("Consent information reset");
            UpdateConsentStatus();
        }

        private void ShowAdIfConsent(AdType adType)
        {
            if (!_isSDKInitialized)
            {
                LogMessage("❌ SDK not initialized");
                return;
            }

            if (!BidscubeSDK.HasAdsConsent())
            {
                LogMessage("❌ No ads consent. Request consent first.");
                return;
            }

            string placementId = _placementIdInput != null ? _placementIdInput.text : _placementId;
            if (string.IsNullOrEmpty(placementId))
            {
                LogMessage("❌ Placement ID is required");
                return;
            }

            LogMessage($"Showing {adType} ad (with consent)...");

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

        private void UpdateConsentStatus()
        {
            if (!_isSDKInitialized)
            {
                UpdateStatus("SDK Status: Not Initialized");
                return;
            }

            string status = "SDK Status: Initialized\n";
            status += $"Consent Required: {BidscubeSDK.IsConsentRequired()}\n";
            status += $"Ads Consent: {BidscubeSDK.HasAdsConsent()}\n";
            status += $"Analytics Consent: {BidscubeSDK.HasAnalyticsConsent()}";
            
            UpdateStatus(status);
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
            UpdateConsentStatus();
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
            UpdateConsentStatus();
        }

        public void OnConsentDenied()
        {
            LogMessage("❌ Consent denied");
            UpdateConsentStatus();
        }

        public void OnConsentNotRequired()
        {
            LogMessage("ℹ️ Consent not required");
            UpdateConsentStatus();
        }

        public void OnConsentStatusChanged(bool hasConsent)
        {
            LogMessage($"Consent status changed: {hasConsent}");
            UpdateConsentStatus();
        }
    }
}
