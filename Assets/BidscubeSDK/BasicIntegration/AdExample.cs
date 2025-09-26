using UnityEngine;
using UnityEngine.UI;
using BidscubeSDK;

namespace BidscubeSDK.Samples
{
    /// <summary>
    /// Basic integration example for Bidscube SDK
    /// </summary>
    public class AdExample : MonoBehaviour, IAdCallback, IConsentCallback
    {
        [Header("UI Elements")]
        [SerializeField] private Button _initializeButton;
        [SerializeField] private Button _imageAdButton;
        [SerializeField] private Button _videoAdButton;
        [SerializeField] private Button _nativeAdButton;
        [SerializeField] private Button _bannerAdButton;
        [SerializeField] private Button _consentButton;
        [SerializeField] private Text _statusText;

        [Header("Ad Settings")]
        [SerializeField] private string _placementId = "test_placement";
        [SerializeField] private AdPosition _bannerPosition = AdPosition.Header;

        private void Start()
        {
            SetupUI();
            UpdateStatus("SDK not initialized");
        }

        private void SetupUI()
        {
            if (_initializeButton != null)
                _initializeButton.onClick.AddListener(InitializeSDK);

            if (_imageAdButton != null)
                _imageAdButton.onClick.AddListener(ShowImageAd);

            if (_videoAdButton != null)
                _videoAdButton.onClick.AddListener(ShowVideoAd);

            if (_nativeAdButton != null)
                _nativeAdButton.onClick.AddListener(ShowNativeAd);

            if (_bannerAdButton != null)
                _bannerAdButton.onClick.AddListener(ShowBannerAd);

            if (_consentButton != null)
                _consentButton.onClick.AddListener(ShowConsentForm);
        }

        private void InitializeSDK()
        {
            // Initialize with custom configuration
            var config = new SDKConfig.Builder()
                .EnableLogging(true)
                .EnableDebugMode(true)
                .DefaultAdTimeout(30000)
                .DefaultAdPosition(AdPosition.Unknown)
                .BaseURL("https://api.bidscube.com")
                .Build();

            BidscubeSDK.Initialize(config);
            UpdateStatus("SDK initialized");
        }

        private void ShowImageAd()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                UpdateStatus("SDK not initialized. Please initialize first.");
                return;
            }

            BidscubeSDK.ShowImageAd(_placementId, this);
            UpdateStatus("Loading image ad...");
        }

        private void ShowVideoAd()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                UpdateStatus("SDK not initialized. Please initialize first.");
                return;
            }

            BidscubeSDK.ShowVideoAd(_placementId, this);
            UpdateStatus("Loading video ad...");
        }

        private void ShowNativeAd()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                UpdateStatus("SDK not initialized. Please initialize first.");
                return;
            }

            BidscubeSDK.ShowNativeAd(_placementId, this);
            UpdateStatus("Loading native ad...");
        }

        private void ShowBannerAd()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                UpdateStatus("SDK not initialized. Please initialize first.");
                return;
            }

            BidscubeSDK.ShowHeaderBanner(_placementId, this);
            UpdateStatus("Loading banner ad...");
        }

        private void ShowConsentForm()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                UpdateStatus("SDK not initialized. Please initialize first.");
                return;
            }

            BidscubeSDK.ShowConsentForm(this);
            UpdateStatus("Showing consent form...");
        }

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
            Debug.Log($"[AdExample] {message}");
        }

        #region IAdCallback Implementation

        public void OnAdLoading(string placementId)
        {
            UpdateStatus($"Ad loading: {placementId}");
        }

        public void OnAdLoaded(string placementId)
        {
            UpdateStatus($"Ad loaded: {placementId}");
        }

        public void OnAdDisplayed(string placementId)
        {
            UpdateStatus($"Ad displayed: {placementId}");
        }

        public void OnAdClicked(string placementId)
        {
            UpdateStatus($"Ad clicked: {placementId}");
        }

        public void OnAdClosed(string placementId)
        {
            UpdateStatus($"Ad closed: {placementId}");
        }

        public void OnAdFailed(string placementId, int errorCode, string errorMessage)
        {
            UpdateStatus($"Ad failed: {placementId}, Error: {errorMessage}");
        }

        public void OnVideoAdStarted(string placementId)
        {
            UpdateStatus($"Video ad started: {placementId}");
        }

        public void OnVideoAdCompleted(string placementId)
        {
            UpdateStatus($"Video ad completed: {placementId}");
        }

        public void OnVideoAdSkipped(string placementId)
        {
            UpdateStatus($"Video ad skipped: {placementId}");
        }

        public void OnVideoAdSkippable(string placementId)
        {
            UpdateStatus($"Video ad skippable: {placementId}");
        }

        public void OnInstallButtonClicked(string placementId, string buttonText)
        {
            UpdateStatus($"Install button clicked: {placementId}, Text: {buttonText}");
        }

        #endregion

        #region IConsentCallback Implementation

        public void OnConsentInfoUpdated()
        {
            UpdateStatus("Consent info updated");
        }

        public void OnConsentInfoUpdateFailed(System.Exception error)
        {
            UpdateStatus($"Consent info update failed: {error.Message}");
        }

        public void OnConsentFormShown()
        {
            UpdateStatus("Consent form shown");
        }

        public void OnConsentFormError(System.Exception error)
        {
            UpdateStatus($"Consent form error: {error.Message}");
        }

        public void OnConsentGranted()
        {
            UpdateStatus("Consent granted");
        }

        public void OnConsentDenied()
        {
            UpdateStatus("Consent denied");
        }

        public void OnConsentNotRequired()
        {
            UpdateStatus("Consent not required");
        }

        public void OnConsentStatusChanged(bool hasConsent)
        {
            UpdateStatus($"Consent status changed: {hasConsent}");
        }

        #endregion
    }
}
