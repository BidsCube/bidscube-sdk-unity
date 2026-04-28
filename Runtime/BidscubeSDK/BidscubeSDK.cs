using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace BidscubeSDK
{
    /// <summary>
    /// Main SDK class for Bidscube Unity SDK
    /// </summary>
    public class BidscubeSDK : MonoBehaviour
    {
        private static BidscubeSDK _instance;
        private static SDKConfig _configuration;
        // Optional runtime AdSizeSettings if SDKConfig doesn't include it
        private static AdSizeSettings _runtimeAdSizeSettings;
        private static bool _initializationEnabled = true;

        private static AdPosition _manualAdPosition;
        private static AdPosition _responseAdPosition = AdPosition.Unknown;
        private static bool _consentRequired = false;
        private static bool _hasAdsConsentFlag = false;
        private static bool _hasAnalyticsConsentFlag = false;
        private static string _consentDebugDeviceId;

        private static List<BannerAdView> _activeBanners = new List<BannerAdView>();
        private static List<AdViewController> _activeControllers = new List<AdViewController>();

        /// <summary>Optional UI parent for new <see cref="AdViewController"/> / video roots (e.g. launcher slot below buttons).</summary>
        private static Transform _adViewsParentOverride;

        /// <summary>When true with an override parent, <see cref="AdViewController"/> sizes the slot for VerticalLayoutGroup-style parents.</summary>
        private static bool _adViewsParentUseLayoutSizing;

        private static BidscubeSDK Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("BidscubeSDK");
                    _instance = go.AddComponent<BidscubeSDK>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private static Coroutine StartSDKCoroutine(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }

        /// <summary>
        /// Initialize the SDK with configuration
        /// </summary>
        /// <param name="config">SDK configuration</param>
        public static void Initialize(SDKConfig config)
        {
            if (!_initializationEnabled || (config != null && config.DisableInitialization))
            {
                Debug.Log("[BidscubeSDK] Initialize skipped (disabled).");
                return;
            }
#if BIDSCUBE_DISABLE_INIT
            Debug.Log("[BidscubeSDK] Initialize skipped (BIDSCUBE_DISABLE_INIT).");
            return;
#endif
            _configuration = config;
            Logger.Configure(config);
            Logger.Info("BidsCube SDK initialized with configuration");

            // Consent APIs in this SDK are still stubs (see ShowConsentForm). Integration samples and
            // INTEGRATION.md expect Show*Ad to work immediately after Initialize without a prior CMP flow.
            // ResetConsent() clears these flags for Consent Test Scene / manual testing.
            _hasAdsConsentFlag = true;
            _hasAnalyticsConsentFlag = true;
            _consentRequired = false;
            // #region agent log
            var bu = config.BaseURL;
            AgentNdjsonDebugLog.Write(
                "BidscubeSDK.Initialize",
                "init_ok",
                "H4",
                "{\"defaultAdTimeoutMs\":" + config.DefaultAdTimeoutMs + ",\"baseUrlLen\":" + (bu != null ? bu.Length : 0) + "}");
            // #endregion
        }

        /// <summary>
        /// Enable/disable SDK initialization globally.
        /// When disabled, <see cref="Initialize(SDKConfig)"/> becomes a no-op and the SDK stays uninitialized.
        /// </summary>
        public static void SetInitializationEnabled(bool enabled)
        {
            _initializationEnabled = enabled;
        }

        public static bool IsInitializationEnabled()
        {
            return _initializationEnabled;
        }

        /// <summary>
        /// Initialize the SDK with default configuration
        /// </summary>
        public static void Initialize()
        {
            var config = new SDKConfig.Builder()
                .EnableLogging(true)
                .EnableDebugMode(false)
                .DefaultAdTimeout(Constants.DefaultTimeoutMs)
                .DefaultAdPosition(Constants.DefaultAdPosition)
                .Build();

            Initialize(config);
        }

        /// <summary>
        /// Check if SDK is initialized
        /// </summary>
        /// <returns>True if initialized</returns>
        public static bool IsInitialized()
        {
            return _configuration != null;
        }

        /// <summary>
        /// Ad load timeout from <see cref="SDKConfig.DefaultAdTimeoutMs"/>, or <see cref="Constants.DefaultTimeoutMs"/> if not initialized.
        /// </summary>
        public static int GetConfiguredAdTimeoutMs()
        {
            return _configuration != null ? _configuration.DefaultAdTimeoutMs : Constants.DefaultTimeoutMs;
        }

        /// <summary>
        /// Sets <see cref="UnityWebRequest.timeout"/> (seconds) to match the configured ad timeout.
        /// </summary>
        public static void ApplyConfiguredTimeoutTo(UnityWebRequest request)
        {
            if (request == null)
                return;
            int ms = GetConfiguredAdTimeoutMs();
            int sec = Mathf.Clamp((ms + 999) / 1000, 1, 3600);
            request.timeout = sec;
        }

        /// <summary>
        /// Cleanup SDK resources
        /// </summary>
        public static void Cleanup()
        {
            ClearAllAds();

            _configuration = null;
            _manualAdPosition = AdPosition.Unknown;
            _responseAdPosition = AdPosition.Unknown;
            _consentRequired = false;
            _hasAdsConsentFlag = false;
            _hasAnalyticsConsentFlag = false;
            _consentDebugDeviceId = null;
            _adViewsParentOverride = null;
            _adViewsParentUseLayoutSizing = false;
        }

        /// <summary>
        /// Parent transform for new ad view controllers (image, native, video). Use for in-app preview areas; call <see cref="ClearAdViewsParentTransform"/> when hiding that UI.
        /// </summary>
        /// <param name="parent">Rect transform (e.g. empty slot under buttons). If null, same as Clear.</param>
        /// <param name="layoutSlotSizing">When true, non-video ads stretch horizontally to the parent and use banner height (for VerticalLayoutGroup slots).</param>
        public static void SetAdViewsParentTransform(Transform parent, bool layoutSlotSizing = true)
        {
            if (parent == null)
            {
                ClearAdViewsParentTransform();
                return;
            }

            _adViewsParentOverride = parent;
            _adViewsParentUseLayoutSizing = layoutSlotSizing;
        }

        /// <summary>Clears <see cref="SetAdViewsParentTransform"/> so ads parent under SDKContent again.</summary>
        public static void ClearAdViewsParentTransform()
        {
            _adViewsParentOverride = null;
            _adViewsParentUseLayoutSizing = false;
        }

        internal static Transform GetAdViewsRootTransform()
        {
            // If launcher dock (or any override) was deactivated but ClearAdViewsParentTransform was not called,
            // the Transform reference can still be non-null while inactiveInHierarchy — parenting there hides all ads.
            if (_adViewsParentOverride != null)
            {
                if (_adViewsParentOverride && _adViewsParentOverride.gameObject.activeInHierarchy)
                    return _adViewsParentOverride;
                ClearAdViewsParentTransform();
            }

            return GetOrCreateSDKContent().transform;
        }

        internal static bool AdViewsParentUsesLayoutSlotSizing()
        {
            if (_adViewsParentOverride == null || !_adViewsParentOverride || !_adViewsParentOverride.gameObject.activeInHierarchy)
                return false;
            return _adViewsParentUseLayoutSizing;
        }

        /// <summary>
        /// Set manual ad position
        /// </summary>
        /// <param name="position">Ad position</param>
        public static void SetAdPosition(AdPosition position)
        {
            _manualAdPosition = position;
            _responseAdPosition = AdPosition.Unknown; // Reset server position when manual is set
        }

        /// <summary>
        /// Get current manual ad position
        /// </summary>
        /// <returns>Current ad position</returns>
        public static AdPosition GetAdPosition()
        {
            return _manualAdPosition;
        }

        /// <summary>
        /// Get response ad position
        /// </summary>
        /// <returns>Response ad position</returns>
        public static AdPosition GetResponseAdPosition()
        {
            return _responseAdPosition;
        }

        /// <summary>
        /// Set response ad position
        /// </summary>
        /// <param name="position">Ad position</param>
        public static void SetResponseAdPosition(AdPosition position)
        {
            // This should only be set by the ad views based on server response
            _responseAdPosition = position;
            Logger.Info($"[BidscubeSDK] SetResponseAdPosition called with: {position} (value: {(int)position})");
        }

        /// <summary>
        /// Get effective ad position (manual or response)
        /// </summary>
        /// <returns>Effective ad position</returns>
        public static AdPosition GetEffectiveAdPosition()
        {
            return _manualAdPosition != AdPosition.Unknown ? _manualAdPosition : _responseAdPosition;
        }

        /// <summary>
        /// Request consent info update
        /// </summary>
        /// <param name="callback">Consent callback</param>
        public static void RequestConsentInfoUpdate(IConsentCallback callback)
        {
            StartSDKCoroutine(DelayedConsentUpdate(callback));
        }

        private static IEnumerator DelayedConsentUpdate(IConsentCallback callback)
        {
            yield return new WaitForSeconds(0.1f);
            callback.OnConsentInfoUpdated();
        }

        /// <summary>
        /// Show consent form
        /// </summary>
        /// <param name="callback">Consent callback</param>
        public static void ShowConsentForm(IConsentCallback callback)
        {
            StartSDKCoroutine(DelayedConsentForm(callback));
        }

        private static IEnumerator DelayedConsentForm(IConsentCallback callback)
        {
            yield return new WaitForSeconds(0.1f);
            callback.OnConsentFormShown();
            _hasAdsConsentFlag = true;
            _hasAnalyticsConsentFlag = true;
            callback.OnConsentGranted();
            callback.OnConsentStatusChanged(true);
        }

        /// <summary>
        /// Enable consent debug mode
        /// </summary>
        /// <param name="testDeviceId">Test device ID</param>
        public static void EnableConsentDebugMode(string testDeviceId)
        {
            _consentDebugDeviceId = testDeviceId;
        }

        /// <summary>
        /// Reset consent
        /// </summary>
        public static void ResetConsent()
        {
            _hasAdsConsentFlag = false;
            _hasAnalyticsConsentFlag = false;
            _consentRequired = false;
        }

        /// <summary>
        /// Check if consent is required
        /// </summary>
        /// <returns>True if consent required</returns>
        public static bool IsConsentRequired()
        {
            return _consentRequired;
        }

        /// <summary>
        /// Check if ads consent is granted
        /// </summary>
        /// <returns>True if ads consent granted</returns>
        public static bool HasAdsConsent()
        {
            return _hasAdsConsentFlag;
        }

        /// <summary>
        /// Check if analytics consent is granted
        /// </summary>
        /// <returns>True if analytics consent granted</returns>
        public static bool HasAnalyticsConsent()
        {
            return _hasAnalyticsConsentFlag;
        }

        /// <summary>
        /// Get consent status summary
        /// </summary>
        /// <returns>Consent status string</returns>
        public static string GetConsentStatusSummary()
        {
            return $"required={_consentRequired}, ads={_hasAdsConsentFlag}, analytics={_hasAnalyticsConsentFlag}";
        }

        /// <summary>
        /// Build request URL for ad
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="adType">Ad type</param>
        /// <param name="ctaText">CTA text (optional)</param>
        /// <returns>Request URL</returns>
        public static string BuildRequestURL(string placementId, AdType adType, AdPosition position = AdPosition.Unknown)
        {
            return URLBuilder.BuildAdRequestURL(
                _configuration.BaseURL,
                placementId,
                adType,
                position,
                _configuration.DefaultAdTimeoutMs,
                _configuration.EnableDebugMode
            );
        }

        // Banner / Image helpers -------------------------------------------------

        /// <summary>Initial layout slot for image/native when manual and response positions are still unknown (uses <see cref="SDKConfig.DefaultAdPosition"/>).</summary>
        private static AdPosition ResolveSlotPositionForNonVideoAds()
        {
            if (_manualAdPosition != AdPosition.Unknown)
                return _manualAdPosition;
            if (_responseAdPosition != AdPosition.Unknown)
                return _responseAdPosition;
            if (_configuration != null && _configuration.DefaultAdPosition != AdPosition.Unknown)
                return _configuration.DefaultAdPosition;
            return AdPosition.Unknown;
        }

        /// <summary>
        /// Show image ad (internally uses banner rendering)
        /// </summary>
        public static void ShowImageAd(string placementId, IAdCallback callback = null)
        {
            if (!IsInitialized())
            {
                Logger.Error("SDK not initialized. Please call BidscubeSDK.Initialize() first.");
                return;
            }

            // #region agent log
            AgentNdjsonDebugLog.Write("BidscubeSDK.ShowImageAd", "entry", "H4", "{\"placementId\":\"" + placementId + "\"}");
            // #endregion
            var position = ResolveSlotPositionForNonVideoAds();
            // If SDK configuration contains AdSizeSettings, pass default image size to the controller
            Vector2? configuredSize = null;
            if (_configuration != null && _configuration.AdSizeSettings != null)
            {
                var s = _configuration.AdSizeSettings.GetDefaultSize(AdType.Image);
                if (s.x > 0 || s.y > 0) configuredSize = s;
            }
            CreateAdViewController(placementId, AdType.Image, callback, position, configuredSize);
        }

        /// <summary>
        /// Show header banner
        /// </summary>
        public static void ShowHeaderBanner(string placementId, IAdCallback callback)
        {
            SetAdPosition(AdPosition.Header);
            GetBannerAdView(placementId, callback);
        }

        /// <summary>
        /// Show footer banner
        /// </summary>
        public static void ShowFooterBanner(string placementId, IAdCallback callback)
        {
            SetAdPosition(AdPosition.Footer);
            GetBannerAdView(placementId, callback);
        }

        /// <summary>
        /// Show sidebar banner
        /// </summary>
        public static void ShowSidebarBanner(string placementId, IAdCallback callback)
        {
            SetAdPosition(AdPosition.Sidebar);
            GetBannerAdView(placementId, callback);
        }

        /// <summary>
        /// Show custom banner with explicit position and size
        /// </summary>
        public static void ShowCustomBanner(string placementId, AdPosition position, int width, int height, IAdCallback callback)
        {
            SetAdPosition(position);
            var bannerGO = GetBannerAdView(placementId, callback);
            var rect = bannerGO.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }

        /// <summary>
        /// Remove and destroy all active banners
        /// </summary>
        /// <summary>
        /// Remove all active banners (legacy method)
        /// </summary>
        public static void RemoveAllBanners()
        {
            ClearAllAds();
        }

        /// <summary>
        /// Clear all ads (banners, images, natives, videos)
        /// </summary>
        public static void ClearAllAds()
        {
            Logger.Info($"Clearing all ads. Banners: {_activeBanners.Count}, Controllers: {_activeControllers.Count}");

            // Remove all active banners
            foreach (var banner in _activeBanners)
            {
                if (banner != null)
                {
                    // If BannerAdView has its own detach/cleanup, use it; otherwise destroy the GameObject
                    banner.DetachFromScreen();
                }
            }
            _activeBanners.Clear();

            // Remove all active ad controllers (Image, Native, Video). Snapshot avoids issues if OnDestroy mutates the list.
            var controllersSnapshot = new List<AdViewController>(_activeControllers);
            foreach (var controller in controllersSnapshot)
            {
                if (controller != null)
                    UnityEngine.Object.Destroy(controller.gameObject);
            }
            _activeControllers.Clear();

            Logger.Info("All ads cleared");
        }

        /// <summary>
        /// Re-runs <see cref="AdViewController.ReapplyLayoutAndWebView"/> on all active ad controllers.
        /// Call after the host UI (e.g. embedded ad slot) finishes layout so slot height is non-zero.
        /// </summary>
        public static void ReapplyLayoutForAllActiveAds()
        {
            var snapshot = new List<AdViewController>(_activeControllers);
            foreach (var c in snapshot)
            {
                if (c != null)
                    c.ReapplyLayoutAndWebView();
            }
        }

        /// <summary>
        /// Convenience wrapper for using banner as image ad view
        /// </summary>
        public static GameObject GetImageAdView(string placementId, IAdCallback callback = null)
        {
            return GetBannerAdView(placementId, callback);
        }

        /// <summary>
        /// Get banner ad view
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        /// <returns>Banner ad view</returns>
        public static GameObject GetBannerAdView(string placementId, IAdCallback callback = null)
        {
            Logger.Info($"GetBannerAdView called for placement: {placementId}");

            var effectivePosition = GetEffectiveAdPosition();

            var bannerView = CreateBannerAdView(effectivePosition);
            var view = bannerView.gameObject;

            if (!_activeBanners.Contains(bannerView))
            {
                _activeBanners.Add(bannerView);
            }

            callback?.OnAdLoading(placementId);

            var url = BuildRequestURL(placementId, AdType.Image); // Use AdType.Image for banners
            if (string.IsNullOrEmpty(url))
            {
                Logger.Error("Failed to build request URL for banner ad");
                callback?.OnAdFailed(placementId, Constants.ErrorCodes.InvalidURL, Constants.ErrorMessages.FailedToBuildURL);
                return view;
            }

            bannerView.SetPlacementInfo(placementId, callback);
            bannerView.LoadAdFromURL(url);

            StartSDKCoroutine(DelayedAdLoaded(placementId, callback));
            return view;
        }

        /// <summary>
        /// Show video ad - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public static void ShowVideoAd(string placementId, IAdCallback callback = null)
        {
            if (!IsInitialized())
            {
                Logger.Error("SDK not initialized. Call Initialize() first.");
                return;
            }

            Logger.Info($"ShowVideoAd called for placement: {placementId}");

            // #region agent log
            AgentNdjsonDebugLog.Write("BidscubeSDK.ShowVideoAd", "entry", "H4", "{\"placementId\":\"" + placementId + "\"}");
            // #endregion
            var effectivePosition = GetEffectiveAdPosition();

            // Video must not parent under a small UI slot (e.g. launcher dock); use dedicated SDK root for fullscreen / overlay.
            Transform parentTransform = GetOrCreateSDKContent().transform;

            // Create AdViewController like iOS
            var adViewControllerObj = new GameObject("AdViewController");
            adViewControllerObj.transform.SetParent(parentTransform, false);
            var adViewController = adViewControllerObj.AddComponent<AdViewController>();
            adViewController.Initialize(placementId, AdType.Video, callback);

            // Load ad from URL
            var url = URLBuilder.BuildAdRequestURL(_configuration.BaseURL, placementId, AdType.Video, effectivePosition, _configuration.DefaultAdTimeoutMs, _configuration.EnableDebugMode);
            Logger.Info($"Video ad request URL: {url}");

            // #region agent log
            AgentNdjsonDebugLog.Write(
                "BidscubeSDK.ShowVideoAd",
                "url_ready",
                "H1",
                "{\"urlLen\":" + (url != null ? url.Length : 0) + ",\"placementId\":\"" + placementId + "\"}");
            // #endregion

            // Get the VideoAdView from the controller
            var videoAdView = adViewControllerObj.GetComponentInChildren<VideoAdView>();
            if (videoAdView != null)
            {
                videoAdView.LoadVideoAdFromURL(url);
            }
        }

        /// <summary>
        /// Show skippable video ad (skip button text currently not used but kept for API parity)
        /// </summary>
        public static void ShowSkippableVideoAd(string placementId, string skipButtonText, IAdCallback callback)
        {
            ShowVideoAd(placementId, callback);
        }

        private static IEnumerator LoadVideoAd(string placementId, string url, IAdCallback callback)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var json = JsonUtility.FromJson<AdResponse>(request.downloadHandler.text);
                        if (json != null)
                        {
                            _responseAdPosition = (AdPosition)json.position;
                        }
                    }
                    catch
                    {
                        _responseAdPosition = AdPosition.FullScreen;
                    }

                    callback?.OnAdLoaded(placementId);
                    callback?.OnAdDisplayed(placementId);
                    callback?.OnVideoAdStarted(placementId);
                    callback?.OnVideoAdCompleted(placementId);
                }
                else
                {
                    callback?.OnAdFailed(placementId, Constants.ErrorCodes.NetworkError, request.error);
                }
            }
        }

        /// <summary>
        /// Get video ad view
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        /// <returns>Video ad view</returns>
        public static GameObject GetVideoAdView(string placementId, IAdCallback callback = null)
        {
            var view = CreateVideoAdView();

            callback?.OnAdLoading(placementId);

            var url = BuildRequestURL(placementId, AdType.Video);
            if (string.IsNullOrEmpty(url))
            {
                Logger.Error("Failed to build request URL for video ad");
                callback?.OnAdFailed(placementId, Constants.ErrorCodes.InvalidURL, Constants.ErrorMessages.FailedToBuildURL);
                return view;
            }

            var videoAdView = view.GetComponent<VideoAdView>();
            if (videoAdView != null)
            {
                videoAdView.SetPlacementInfo(placementId, callback);
                videoAdView.LoadVideoAdFromURL(url);
            }

            StartSDKCoroutine(DelayedVideoAdLoaded(placementId, callback));
            return view;
        }

        /// <summary>
        /// Show native ad - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public static void ShowNativeAd(string placementId, IAdCallback callback = null)
        {
            if (!IsInitialized())
            {
                Logger.Error("SDK not initialized. Call Initialize() first.");
                return;
            }

            var position = ResolveSlotPositionForNonVideoAds();
            // Pass configured native default size when available
            Vector2? configuredNativeSize = null;
            if (_configuration != null && _configuration.AdSizeSettings != null)
            {
                var s = _configuration.AdSizeSettings.GetDefaultSize(AdType.Native);
                if (s.x > 0 || s.y > 0) configuredNativeSize = s;
            }
            CreateAdViewController(placementId, AdType.Native, callback, position, configuredNativeSize);
        }

        private static IEnumerator LoadNativeAd(string placementId, string url, IAdCallback callback)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var json = JsonUtility.FromJson<AdResponse>(request.downloadHandler.text);
                        if (json != null)
                        {
                            _responseAdPosition = (AdPosition)json.position;
                        }
                    }
                    catch
                    {
                        _responseAdPosition = AdPosition.Unknown;
                    }

                    callback?.OnAdLoaded(placementId);
                    callback?.OnAdDisplayed(placementId);
                }
                else
                {
                    callback?.OnAdFailed(placementId, Constants.ErrorCodes.NetworkError, request.error);
                }
            }
        }

        /// <summary>
        /// Get native ad view
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        /// <returns>Native ad view</returns>
        public static GameObject GetNativeAdView(string placementId, IAdCallback callback = null)
        {
            var view = CreateNativeAdView();

            callback?.OnAdLoading(placementId);

            var url = BuildRequestURL(placementId, AdType.Native);
            if (string.IsNullOrEmpty(url))
            {
                Logger.Error("Failed to build request URL for native ad");
                callback?.OnAdFailed(placementId, Constants.ErrorCodes.InvalidURL, Constants.ErrorMessages.FailedToBuildURL);
                return view;
            }

            var nativeAdView = view.GetComponent<NativeAdView>();
            if (nativeAdView != null)
            {
                nativeAdView.SetPlacementInfo(placementId, callback);
                nativeAdView.LoadNativeAdFromURL(url);
            }

            StartSDKCoroutine(DelayedAdLoaded(placementId, callback));
            return view;
        }

        /// <summary>
        /// Get banner ad view
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="position">Ad position</param>
        /// <param name="callback">Ad callback</param>
        /// <returns>Banner ad view</returns>
        public static BannerAdView GetBannerAdView(string placementId, AdPosition position, IAdCallback callback = null)
        {
            Logger.Info($"GetBannerAdView called for placement: {placementId}, position: {position}");

            var bannerView = CreateBannerAdView(position);
            var view = bannerView.gameObject;

            callback?.OnAdLoading(placementId);

            var url = BuildRequestURL(placementId, AdType.Image);
            if (string.IsNullOrEmpty(url))
            {
                Logger.Error("Failed to build request URL for banner ad");
                callback?.OnAdFailed(placementId, Constants.ErrorCodes.InvalidURL, Constants.ErrorMessages.FailedToBuildURL);
                return bannerView;
            }

            bannerView.SetPlacementInfo(placementId, callback);
            bannerView.LoadAdFromURL(url);

            StartSDKCoroutine(DelayedAdLoaded(placementId, callback));
            return bannerView;
        }

        // Helper methods
        private static GameObject CreateVideoAdView()
        {
            var go = new GameObject("VideoAdView", typeof(RectTransform));
            // Ensure RectTransform exists for UI parenting operations
            if (go.GetComponent<RectTransform>() == null)
                go.AddComponent<RectTransform>();
            go.AddComponent<VideoAdView>();
            return go;
        }

        private static GameObject CreateNativeAdView()
        {
            var go = new GameObject("NativeAdView", typeof(RectTransform));
            if (go.GetComponent<RectTransform>() == null)
                go.AddComponent<RectTransform>();
            go.AddComponent<NativeAdView>();
            return go;
        }

        private static BannerAdView CreateBannerAdView(AdPosition position)
        {
            var go = new GameObject("BannerAdView", typeof(RectTransform));
            if (go.GetComponent<RectTransform>() == null)
                go.AddComponent<RectTransform>();
            var bannerView = go.AddComponent<BannerAdView>();
            // Determine active AdSizeSettings: first prefer configuration, then runtime setter
            AdSizeSettings activeSettings = _configuration != null && _configuration.AdSizeSettings != null
                ? _configuration.AdSizeSettings
                : _runtimeAdSizeSettings;
            if (activeSettings != null)
            {
                bannerView.ApplyAdSizeSettings(activeSettings);
            }
            bannerView.SetBannerPosition(position);
            return bannerView;
        }

        private static void CreateAdViewController(string placementId, AdType adType, IAdCallback callback, AdPosition position, Vector2? adSize = null)
        {
            Transform parentTransform = adType == AdType.Video
                ? GetOrCreateSDKContent().transform
                : GetAdViewsRootTransform();

            var controllerGO = new GameObject($"AdViewController_{placementId}");
            controllerGO.transform.SetParent(parentTransform, false);

            // Ensure scale is 1,1,1 before adding components
            controllerGO.transform.localScale = Vector3.one;

            var adController = controllerGO.AddComponent<AdViewController>();
            // Inject AdSizeSettings from SDK configuration or runtime setting so controller always uses configured defaults
            AdSizeSettings activeSettings = _configuration != null && _configuration.AdSizeSettings != null
                ? _configuration.AdSizeSettings
                : _runtimeAdSizeSettings;
            if (activeSettings != null)
            {
                adController.SetAdSizeSettings(activeSettings);
            }
            adController.Initialize(placementId, adType, callback, position);

            // If caller provided an explicit size (for example a GameObject's RectTransform), apply it
            if (adSize.HasValue)
            {
                var size = adSize.Value;
                // SetAdSize will re-apply positioning and refresh webview margins
                adController.SetAdSize(size.x, size.y);
                Logger.Info($"[BidscubeSDK] CreateAdViewController: Applied caller-provided ad size {size.x}x{size.y} for placement {placementId}");
            }

            _activeControllers.Add(adController);
        }

        /// <summary>
        /// Get or create SDKContent GameObject to parent all SDK objects
        /// </summary>
        /// <returns>SDKContent GameObject</returns>
        private static GameObject GetOrCreateSDKContent()
        {
            // Dedicated root for all ad UI — do not parent to the first scene Canvas (menu / sample UI),
            // or ads end up nested under unrelated canvases with wrong scale, sorting, or camera mode.
            GameObject sdkContent = GameObject.Find("SDKContent");
            if (sdkContent != null)
            {
                sdkContent.transform.localScale = Vector3.one;
                return sdkContent;
            }

            sdkContent = new GameObject("SDKContent");
            sdkContent.transform.localScale = Vector3.one;
            UnityEngine.Object.DontDestroyOnLoad(sdkContent);
            Logger.Info("[BidscubeSDK] Created SDKContent (DontDestroyOnLoad) as dedicated parent for ad views");

            return sdkContent;
        }

        internal static void UnregisterAdViewController(AdViewController controller)
        {
            _activeControllers.Remove(controller);
        }

        private static IEnumerator DelayedAdLoaded(string placementId, IAdCallback callback)
        {
            yield return new WaitForSeconds(1.0f);
            _responseAdPosition = AdPosition.Unknown;
            callback?.OnAdLoaded(placementId);
            callback?.OnAdDisplayed(placementId);
        }

        private static IEnumerator DelayedVideoAdLoaded(string placementId, IAdCallback callback)
        {
            yield return new WaitForSeconds(1.0f);
            _responseAdPosition = AdPosition.FullScreen;
            callback?.OnAdLoaded(placementId);
            callback?.OnAdDisplayed(placementId);
            callback?.OnVideoAdStarted(placementId);
            callback?.OnVideoAdCompleted(placementId);
        }

        private static IEnumerator DelayedBannerAdLoaded(string placementId, AdPosition position, IAdCallback callback)
        {
            yield return new WaitForSeconds(1.0f);
            _responseAdPosition = position;
            callback?.OnAdLoaded(placementId);
            callback?.OnAdDisplayed(placementId);
        }

        private static void TrackBanner(BannerAdView banner)
        {
            _activeBanners.Add(banner);
            Logger.DebugLog($"Tracking banner ad. Total active banners: {_activeBanners.Count}");
        }

        /// <summary>
        /// Untrack banner
        /// </summary>
        /// <param name="banner">Banner to untrack</param>
        public static void UntrackBanner(BannerAdView banner)
        {
            _activeBanners.Remove(banner);
            Logger.DebugLog($"Untracking banner ad. Total active banners: {_activeBanners.Count}");
        }

        /// <summary>
        /// Get active banner count
        /// </summary>
        /// <returns>Number of active banners</returns>
        public static int GetActiveBannerCount()
        {
            return _activeBanners.Count;
        }

        // Unity MonoBehaviour methods
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }


        // Helper class for JSON deserialization
        [System.Serializable]
        private class AdResponse
        {
            public string adm;
            public int position;
        }
    }
}
