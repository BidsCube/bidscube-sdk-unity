using UnityEngine;
using UnityEngine.UI;
using BidscubeSDK;

namespace BidscubeSDK.Controllers
{
    /// <summary>
    /// Comprehensive test scene for Bidscube Unity SDK
    /// Demonstrates all SDK functionality with proper UI hierarchy
    /// </summary>
    public class BidscubeExampleScene : MonoBehaviour, IAdCallback, IConsentCallback
    {
        [Header("SDK Configuration")]
        [SerializeField] private string _placementId = "test_placement_123";
        [SerializeField] private string _baseURL = Constants.BaseURL;
        [SerializeField] private bool _enableDebugMode = true;
        [SerializeField] private bool _enableLogging = true;

        [Header("UI References")]
        [SerializeField] private Button _initButton;
        [SerializeField] private Button _imageAdButton;
        [SerializeField] private Button _videoAdButton;
        [SerializeField] private Button _nativeAdButton;
        [SerializeField] private Button _headerBannerButton;
        [SerializeField] private Button _footerBannerButton;
        [SerializeField] private Button _sidebarBannerButton;
        [SerializeField] private Button _customBannerButton;
        [SerializeField] private Button _consentButton;
        [SerializeField] private Button _removeAllBannersButton;

        [Header("Status Display")]
        [SerializeField] private Text _statusText;
        [SerializeField] private ScrollRect _logScrollRect;
        [SerializeField] private Text _logText;

        [Header("Banner Display Areas")]
        [SerializeField] private RectTransform _headerBannerArea;
        [SerializeField] private RectTransform _footerBannerArea;
        [SerializeField] private RectTransform _sidebarBannerArea;

        [Header("Navigation")]
        [SerializeField] private Button _sdkTestButton;
        [SerializeField] private Button _consentTestButton;
        [SerializeField] private Button _windowedAdButton;

        [Header("Integration (Bidscube / LevelPlay)")]
        [SerializeField] private bool _showIntegrationModeBar = true;

        private string _logContent = "";
        private Text _integrationHintText;

        private void Start()
        {
            SetupUI();
            BuildIntegrationModeBar();
            ApplyLevelPlayBridgeVisibilityFromMode();
            UpdateStatus(SdkIntegrationContext.GetCurrentShortStatus() + " — ready to initialize SDK");
            if (_integrationHintText != null)
                _integrationHintText.text = SdkIntegrationContext.GetCurrentDescription();
        }

        private void SetupUI()
        {
            // Initialize SDK button
            if (_initButton != null)
                _initButton.onClick.AddListener(InitializeSDK);

            // Ad type buttons
            if (_imageAdButton != null)
                _imageAdButton.onClick.AddListener(ShowImageAd);

            if (_videoAdButton != null)
                _videoAdButton.onClick.AddListener(ShowVideoAd);

            if (_nativeAdButton != null)
                _nativeAdButton.onClick.AddListener(ShowNativeAd);

            // Banner buttons
            if (_headerBannerButton != null)
                _headerBannerButton.onClick.AddListener(ShowHeaderBanner);

            if (_footerBannerButton != null)
                _footerBannerButton.onClick.AddListener(ShowFooterBanner);

            if (_sidebarBannerButton != null)
                _sidebarBannerButton.onClick.AddListener(ShowSidebarBanner);

            if (_customBannerButton != null)
                _customBannerButton.onClick.AddListener(ShowCustomBanner);

            // Other buttons
            if (_consentButton != null)
                _consentButton.onClick.AddListener(ShowConsentForm);

            if (_removeAllBannersButton != null)
                _removeAllBannersButton.onClick.AddListener(RemoveAllBanners);

            // Navigation buttons
            if (_sdkTestButton != null)
                _sdkTestButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadSDKTestScene());

            if (_consentTestButton != null)
                _consentTestButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadConsentTestScene());

            if (_windowedAdButton != null)
                _windowedAdButton.onClick.AddListener(() => GetComponent<SceneManager>()?.LoadWindowedAdScene());
        }

        private void BuildIntegrationModeBar()
        {
            if (!_showIntegrationModeBar)
                return;
            if (GameObject.Find("BccIntegrationModeBar") != null)
                return;

            var canvas = ResolveHostCanvas();
            if (canvas == null)
                return;

            var root = new GameObject("BccIntegrationModeBar");
            var rootRt = root.AddComponent<RectTransform>();
            root.transform.SetParent(canvas.transform, false);
            rootRt.SetAsFirstSibling();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = new Vector2(0f, 118f);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.11f, 0.14f, 0.98f);

            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.spacing = 6;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            CreateBarLabel(root.transform, "Integration check", 13, FontStyle.Bold);

            var row = new GameObject("ModeRow");
            row.transform.SetParent(root.transform, false);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 40f;
            rowLe.flexibleWidth = 1f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childForceExpandWidth = true;
            hlg.childControlWidth = true;

            CreateModeButton(row.transform, "Only Bidscube", SdkIntegrationMode.BidscubeDirect);
            CreateModeButton(row.transform, "Bidscube + LevelPlay adapter", SdkIntegrationMode.BidscubeWithLevelPlayAdapter);
            CreateModeButton(row.transform, "Level Play mediation", SdkIntegrationMode.LevelPlayMediation);

            _integrationHintText = CreateBarLabel(root.transform, SdkIntegrationContext.GetCurrentDescription(), 11, FontStyle.Normal);
        }

        private Canvas ResolveHostCanvas()
        {
            var t = transform;
            while (t != null)
            {
                var c = t.GetComponent<Canvas>();
                if (c != null)
                    return c;
                t = t.parent;
            }
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<Canvas>();
#else
            return FindObjectOfType<Canvas>();
#endif
        }

        private static Font BuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Text CreateBarLabel(Transform parent, string text, int fontSize, FontStyle style)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = BuiltinFont();
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 8;
            le.flexibleWidth = 1f;
            return txt;
        }

        private void CreateModeButton(Transform parent, string label, SdkIntegrationMode mode)
        {
            var go = new GameObject("Btn_" + mode);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.42f, 0.72f, 1f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.55f, 0.85f);
            colors.pressedColor = new Color(0.15f, 0.3f, 0.55f);
            btn.colors = colors;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 38f;
            le.flexibleWidth = 1f;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(4f, 2f);
            trt.offsetMax = new Vector2(-4f, -2f);
            var txt = textGo.AddComponent<Text>();
            txt.text = label;
            txt.font = BuiltinFont();
            txt.fontSize = 11;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;

            btn.onClick.AddListener(() => OnIntegrationModeSelected(mode));
        }

        private void OnIntegrationModeSelected(SdkIntegrationMode mode)
        {
            SdkIntegrationContext.SetMode(mode);
            ApplyLevelPlayBridgeVisibilityFromMode();
            if (_integrationHintText != null)
                _integrationHintText.text = SdkIntegrationContext.GetCurrentDescription();
            UpdateStatus(SdkIntegrationContext.GetCurrentShortStatus());
            LogMessage("[Integration] " + SdkIntegrationContext.GetCurrentDescription());
            if (!SdkIntegrationContext.SuppressLevelPlayBridge && GameObject.Find("BidscubeLevelPlayBridge") == null)
                LogMessage("[Integration] Restart the app once so BidscubeLevelPlayBridge can initialize.");
        }

        private static void ApplyLevelPlayBridgeVisibilityFromMode()
        {
            if (!SdkIntegrationContext.SuppressLevelPlayBridge)
                return;
            var go = GameObject.Find("BidscubeLevelPlayBridge");
            if (go != null)
                Object.Destroy(go);
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
                UpdateStatus("SDK Initialized Successfully");
                LogMessage(" SDK initialized with config:");
                LogMessage($"   - Base URL: {_baseURL}");
                LogMessage($"   - Debug Mode: {_enableDebugMode}");
                LogMessage($"   - Logging: {_enableLogging}");
            }
            else
            {
                UpdateStatus("SDK Initialization Failed");
                LogMessage(" SDK initialization failed");
            }
        }

        private void ShowImageAd()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage(" Showing Image Ad...");
            var adViewControllerObj = new GameObject("AdViewController");
            var adViewController = adViewControllerObj.AddComponent<AdViewController>();
            adViewController.Initialize(_placementId, AdType.Image, this);
        }

        private void ShowVideoAd()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage("🎥 Showing Video Ad...");
            BidscubeSDK.ShowVideoAd(_placementId, this);
        }

        private void ShowNativeAd()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage("📱 Showing Native Ad...");
            BidscubeSDK.ShowNativeAd(_placementId, this);
        }

        private void ShowHeaderBanner()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage("📊 Showing Header Banner...");
            BidscubeSDK.ShowHeaderBanner(_placementId, this);
        }

        private void ShowFooterBanner()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage("📊 Showing Footer Banner...");
            BidscubeSDK.ShowFooterBanner(_placementId, this);
        }

        private void ShowSidebarBanner()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage("📊 Showing Sidebar Banner...");
            BidscubeSDK.ShowSidebarBanner(_placementId, this);
        }

        private void ShowCustomBanner()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage("📊 Showing Custom Banner (320x50)...");
            BidscubeSDK.ShowCustomBanner(_placementId, AdPosition.Header, 320, 50, this);
        }

        private void ShowConsentForm()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage("🔒 Showing Consent Form...");
            BidscubeSDK.ShowConsentForm(this);
        }

        private void RemoveAllBanners()
        {
            if (!BidscubeSDK.IsInitialized())
            {
                LogMessage(" SDK not initialized. Please initialize first.");
                return;
            }

            LogMessage(" Removing all banners...");
            BidscubeSDK.RemoveAllBanners();
        }

        private void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = $"Status: {status}";
            }
        }

        private void LogMessage(string message)
        {
            _logContent += $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";

            if (_logText != null)
            {
                _logText.text = _logContent;

                // Auto-scroll to bottom
                if (_logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    _logScrollRect.verticalNormalizedPosition = 0f;
                }
            }

            Logger.Info($"[BidscubeExample] {message}");
        }

        #region IAdCallback Implementation

        public void OnAdLoading(string placementId)
        {
            LogMessage($"⏳ Ad loading: {placementId}");
            UpdateStatus($"Loading ad: {placementId}");
        }

        public void OnAdLoaded(string placementId)
        {
            LogMessage($" Ad loaded: {placementId}");
            UpdateStatus($"Ad loaded: {placementId}");
        }

        public void OnAdDisplayed(string placementId)
        {
            LogMessage($" Ad displayed: {placementId}");
            UpdateStatus($"Ad displayed: {placementId}");
        }

        public void OnAdClicked(string placementId)
        {
            LogMessage($"👆 Ad clicked: {placementId}");
            UpdateStatus($"Ad clicked: {placementId}");
        }

        public void OnAdClosed(string placementId)
        {
            LogMessage($" Ad closed: {placementId}");
            UpdateStatus($"Ad closed: {placementId}");
        }

        public void OnAdFailed(string placementId, int errorCode, string errorMessage)
        {
            LogMessage($" Ad failed: {placementId} (Code: {errorCode}, Message: {errorMessage})");
            UpdateStatus($"Ad failed: {placementId}");
        }

        public void OnVideoAdStarted(string placementId)
        {
            LogMessage($" Video ad started: {placementId}");
        }

        public void OnVideoAdCompleted(string placementId)
        {
            LogMessage($"🏁 Video ad completed: {placementId}");
        }

        public void OnVideoAdSkipped(string placementId)
        {
            LogMessage($" Video ad skipped: {placementId}");
        }

        public void OnVideoAdSkippable(string placementId)
        {
            LogMessage($" Video ad skippable: {placementId}");
        }

        public void OnInstallButtonClicked(string placementId, string buttonText)
        {
            LogMessage($"📱 Install button clicked: {placementId} ({buttonText})");
        }

        // Added missing OnAdRenderOverride (IAdCallback)
        public bool OnAdRenderOverride(string adm, int position)
        {
            int admLen = adm != null ? adm.Length : 0;
            LogMessage($"OnAdRenderOverride called: position={position}, admLength={admLen}");
            return false; // Let SDK render by default
        }

        #endregion

        #region IConsentCallback Implementation

        public void OnConsentInfoUpdated()
        {
            LogMessage("🔒 Consent info updated");
        }

        public void OnConsentInfoUpdateFailed(System.Exception error)
        {
            LogMessage($" Consent info update failed: {error.Message}");
        }

        public void OnConsentFormShown()
        {
            LogMessage(" Consent form shown");
        }

        public void OnConsentFormError(System.Exception error)
        {
            LogMessage($" Consent form error: {error.Message}");
        }

        public void OnConsentGranted()
        {
            LogMessage(" Consent granted");
            UpdateStatus("Consent granted");
        }

        public void OnConsentDenied()
        {
            LogMessage(" Consent denied");
            UpdateStatus("Consent denied");
        }

        public void OnConsentNotRequired()
        {
            LogMessage(" Consent not required");
            UpdateStatus("Consent not required");
        }

        public void OnConsentStatusChanged(bool hasConsent)
        {
            LogMessage($" Consent status changed: {hasConsent}");
            UpdateStatus($"Consent: {hasConsent}");
        }

        #endregion
    }
}
