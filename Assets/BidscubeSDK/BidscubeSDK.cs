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
        private static AdPosition _manualAdPosition;
        private static AdPosition _responseAdPosition = AdPosition.Unknown;
        private static bool _consentRequired = false;
        private static bool _hasAdsConsentFlag = false;
        private static bool _hasAnalyticsConsentFlag = false;
        private static string _consentDebugDeviceId;
        
        private static List<BannerAdView> _activeBanners = new List<BannerAdView>();

        /// <summary>
        /// Initialize the SDK with configuration
        /// </summary>
        /// <param name="config">SDK configuration</param>
        public static void Initialize(SDKConfig config)
        {
            _configuration = config;
            Logger.Configure(config);
            Logger.Info("BidsCube SDK initialized with configuration");
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
        /// Cleanup SDK resources
        /// </summary>
        public static void Cleanup()
        {
            RemoveAllBanners();
            
            _configuration = null;
            _manualAdPosition = AdPosition.Unknown;
            _responseAdPosition = AdPosition.Unknown;
            _consentRequired = false;
            _hasAdsConsentFlag = false;
            _hasAnalyticsConsentFlag = false;
            _consentDebugDeviceId = null;
        }

        /// <summary>
        /// Set manual ad position
        /// </summary>
        /// <param name="position">Ad position</param>
        public static void SetAdPosition(AdPosition position)
        {
            _manualAdPosition = position;
        }

        /// <summary>
        /// Get current manual ad position
        /// </summary>
        /// <returns>Current ad position</returns>
        public static AdPosition GetCurrentAdPosition()
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
            _responseAdPosition = position;
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
            StartCoroutine(DelayedConsentUpdate(callback));
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
            StartCoroutine(DelayedConsentForm(callback));
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
        public static string BuildRequestURL(string placementId, AdType adType, string ctaText = null)
        {
            if (_configuration == null)
            {
                Logger.Error("SDK not initialized");
                return null;
            }

            var timeout = _configuration.DefaultAdTimeoutMs;
            var debug = _configuration.EnableDebugMode;
            var position = GetEffectiveAdPosition();

            return URLBuilder.BuildAdRequestURL(
                _configuration.BaseURL,
                placementId,
                adType,
                position,
                timeout,
                debug,
                ctaText
            );
        }

        /// <summary>
        /// Show image ad - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public static void ShowImageAd(string placementId, IAdCallback callback = null)
        {
            if (!IsInitialized())
            {
                Logger.Error("SDK not initialized. Call Initialize() first.");
                return;
            }

            Logger.Info($"ShowImageAd called for placement: {placementId}");

            var effectivePosition = GetEffectiveAdPosition();
            
            // Create AdViewController like iOS
            var adViewControllerObj = new GameObject("AdViewController");
            var adViewController = adViewControllerObj.AddComponent<AdViewController>();
            adViewController.Initialize(placementId, AdType.Image, callback, effectivePosition);

            // Load ad from URL
            var url = URLBuilder.BuildAdRequestURL(_configuration.BaseURL, placementId, AdType.Image, effectivePosition, _configuration.DefaultAdTimeoutMs, _configuration.EnableDebugMode);
            Logger.Info($"Image ad request URL: {url}");

            // Get the ImageAdView from the controller
            var imageAdView = adViewControllerObj.GetComponentInChildren<ImageAdView>();
            if (imageAdView != null)
            {
                imageAdView.LoadAdFromURL(url);
            }
        }

        private static IEnumerator LoadImageAd(string placementId, string url, IAdCallback callback)
        {
            using (var request = UnityWebRequest.Get(url))
            {
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
        /// Get image ad view
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        /// <returns>Image ad view</returns>
        public static GameObject GetImageAdView(string placementId, IAdCallback callback = null)
        {
            Logger.Info($"GetImageAdView called for placement: {placementId}");

            var effectivePosition = GetEffectiveAdPosition();
            GameObject view;

            if (effectivePosition == AdPosition.Header || effectivePosition == AdPosition.Footer || effectivePosition == AdPosition.Sidebar)
            {
                var bannerView = CreateBannerAdView(effectivePosition);
                view = bannerView.gameObject;
            }
            else
            {
                view = CreateImageAdView();
            }

            callback?.OnAdLoading(placementId);

            var url = BuildRequestURL(placementId, AdType.Image);
            if (string.IsNullOrEmpty(url))
            {
                Logger.Error("Failed to build request URL for image ad");
                callback?.OnAdFailed(placementId, Constants.ErrorCodes.InvalidURL, Constants.ErrorMessages.FailedToBuildURL);
                return view;
            }

            var imageAdView = view.GetComponent<ImageAdView>();
            if (imageAdView != null)
            {
                imageAdView.SetPlacementInfo(placementId, callback);
                imageAdView.LoadAdFromURL(url);
            }
            else
            {
                var bannerAdView = view.GetComponent<BannerAdView>();
                if (bannerAdView != null)
                {
                    bannerAdView.SetPlacementInfo(placementId, callback);
                    bannerAdView.LoadAdFromURL(url);
                }
            }

            StartCoroutine(DelayedAdLoaded(placementId, callback));
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

            var effectivePosition = GetEffectiveAdPosition();
            
            // Create AdViewController like iOS
            var adViewControllerObj = new GameObject("AdViewController");
            var adViewController = adViewControllerObj.AddComponent<AdViewController>();
            adViewController.Initialize(placementId, AdType.Video, callback);

            // Load ad from URL
            var url = URLBuilder.BuildAdRequestURL(_configuration.BaseURL, placementId, AdType.Video, effectivePosition, _configuration.DefaultAdTimeoutMs, _configuration.EnableDebugMode);
            Logger.Info($"Video ad request URL: {url}");

            // Get the VideoAdView from the controller
            var videoAdView = adViewControllerObj.GetComponentInChildren<VideoAdView>();
            if (videoAdView != null)
            {
                videoAdView.LoadVideoAdFromURL(url);
            }
        }

        private static IEnumerator LoadVideoAd(string placementId, string url, IAdCallback callback)
        {
            using (var request = UnityWebRequest.Get(url))
            {
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

            StartCoroutine(DelayedVideoAdLoaded(placementId, callback));
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

            Logger.Info($"ShowNativeAd called for placement: {placementId}");

            var effectivePosition = GetEffectiveAdPosition();
            
            // Create AdViewController like iOS
            var adViewControllerObj = new GameObject("AdViewController");
            var adViewController = adViewControllerObj.AddComponent<AdViewController>();
            adViewController.Initialize(placementId, AdType.Native, callback);

            // Load ad from URL
            var url = URLBuilder.BuildAdRequestURL(_configuration.BaseURL, placementId, AdType.Native, effectivePosition, _configuration.DefaultAdTimeoutMs, _configuration.EnableDebugMode);
            Logger.Info($"Native ad request URL: {url}");

            // Get the NativeAdView from the controller
            var nativeAdView = adViewControllerObj.GetComponentInChildren<NativeAdView>();
            if (nativeAdView != null)
            {
                nativeAdView.LoadNativeAdFromURL(url);
            }
        }

        private static IEnumerator LoadNativeAd(string placementId, string url, IAdCallback callback)
        {
            using (var request = UnityWebRequest.Get(url))
            {
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
            Logger.Info($"GetNativeAdView called for placement: {placementId}");

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

            StartCoroutine(DelayedAdLoaded(placementId, callback));
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

            StartCoroutine(DelayedBannerAdLoaded(placementId, position, callback));
            return bannerView;
        }

        /// <summary>
        /// Show header banner - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public static void ShowHeaderBanner(string placementId, IAdCallback callback = null)
        {
            if (!IsInitialized())
            {
                Logger.Error("SDK not initialized. Call Initialize() first.");
                return;
            }

            Logger.Info($"ShowHeaderBanner called for placement: {placementId}");
            
            // Create banner view like iOS
            var bannerObj = new GameObject("BannerAdView");
            var bannerView = bannerObj.AddComponent<BannerAdView>();
            bannerView.SetPlacementInfo(placementId, callback);
            bannerView.SetBannerPosition(AdPosition.Header);
            
            // Track banner
            TrackBanner(bannerView);
            
            // Attach to screen
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                bannerView.AttachToScreen(canvas.gameObject);
            }
            
            // Load ad from URL
            var url = URLBuilder.BuildAdRequestURL(_configuration.BaseURL, placementId, AdType.Image, AdPosition.Header, _configuration.DefaultAdTimeoutMs, _configuration.EnableDebugMode);
            bannerView.LoadAdFromURL(url);
        }

        /// <summary>
        /// Show footer banner - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public static void ShowFooterBanner(string placementId, IAdCallback callback = null)
        {
            if (!IsInitialized())
            {
                Logger.Error("SDK not initialized. Call Initialize() first.");
                return;
            }

            Logger.Info($"ShowFooterBanner called for placement: {placementId}");
            
            // Create banner view like iOS
            var bannerObj = new GameObject("BannerAdView");
            var bannerView = bannerObj.AddComponent<BannerAdView>();
            bannerView.SetPlacementInfo(placementId, callback);
            bannerView.SetBannerPosition(AdPosition.Footer);
            
            // Track banner
            TrackBanner(bannerView);
            
            // Attach to screen
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                bannerView.AttachToScreen(canvas.gameObject);
            }
            
            // Load ad from URL
            var url = URLBuilder.BuildAdRequestURL(_configuration.BaseURL, placementId, AdType.Image, AdPosition.Footer, _configuration.DefaultAdTimeoutMs, _configuration.EnableDebugMode);
            bannerView.LoadAdFromURL(url);
        }

        /// <summary>
        /// Show sidebar banner - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public static void ShowSidebarBanner(string placementId, IAdCallback callback = null)
        {
            if (!IsInitialized())
            {
                Logger.Error("SDK not initialized. Call Initialize() first.");
                return;
            }

            Logger.Info($"ShowSidebarBanner called for placement: {placementId}");
            
            // Create banner view like iOS
            var bannerObj = new GameObject("BannerAdView");
            var bannerView = bannerObj.AddComponent<BannerAdView>();
            bannerView.SetPlacementInfo(placementId, callback);
            bannerView.SetBannerPosition(AdPosition.Sidebar);
            
            // Track banner
            TrackBanner(bannerView);
            
            // Attach to screen
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                bannerView.AttachToScreen(canvas.gameObject);
            }
            
            // Load ad from URL
            var url = URLBuilder.BuildAdRequestURL(_configuration.BaseURL, placementId, AdType.Image, AdPosition.Sidebar, _configuration.DefaultAdTimeoutMs, _configuration.EnableDebugMode);
            bannerView.LoadAdFromURL(url);
        }

        /// <summary>
        /// Show custom banner - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="position">Ad position</param>
        /// <param name="width">Banner width</param>
        /// <param name="height">Banner height</param>
        /// <param name="callback">Ad callback</param>
        public static void ShowCustomBanner(string placementId, AdPosition position, float width, float height, IAdCallback callback = null)
        {
            if (!IsInitialized())
            {
                Logger.Error("SDK not initialized. Call Initialize() first.");
                return;
            }

            Logger.Info($"ShowCustomBanner called for placement: {placementId}, position: {position}, size: {width}x{height}");
            
            // Create banner view like iOS
            var bannerObj = new GameObject("BannerAdView");
            var bannerView = bannerObj.AddComponent<BannerAdView>();
            bannerView.SetPlacementInfo(placementId, callback);
            bannerView.SetBannerPosition(position);
            bannerView.SetBannerDimensions(width, height);
            
            // Track banner
            TrackBanner(bannerView);
            
            // Attach to screen
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                bannerView.AttachToScreen(canvas.gameObject);
            }
            
            // Load ad from URL
            var url = URLBuilder.BuildAdRequestURL(_configuration.BaseURL, placementId, AdType.Image, position, _configuration.DefaultAdTimeoutMs, _configuration.EnableDebugMode);
            bannerView.LoadAdFromURL(url);
        }

        // Helper methods
        private static GameObject CreateImageAdView()
        {
            var go = new GameObject("ImageAdView");
            go.AddComponent<ImageAdView>();
            return go;
        }

        private static GameObject CreateVideoAdView()
        {
            var go = new GameObject("VideoAdView");
            go.AddComponent<VideoAdView>();
            return go;
        }

        private static GameObject CreateNativeAdView()
        {
            var go = new GameObject("NativeAdView");
            go.AddComponent<NativeAdView>();
            return go;
        }

        private static BannerAdView CreateBannerAdView(AdPosition position)
        {
            var go = new GameObject("BannerAdView");
            var bannerView = go.AddComponent<BannerAdView>();
            bannerView.SetBannerPosition(position);
            return bannerView;
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
        /// Remove all banners
        /// </summary>
        public static void RemoveAllBanners()
        {
            Logger.DebugLog($"Removing all active banners. Count: {_activeBanners.Count}");
            
            foreach (var banner in _activeBanners)
            {
                banner.DetachFromScreen();
            }
            
            _activeBanners.Clear();
            Logger.DebugLog("All banners removed");
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

        private static void StartCoroutine(IEnumerator coroutine)
        {
            if (_instance != null)
            {
                ((MonoBehaviour)_instance).StartCoroutine(coroutine);
            }
        }

        // Helper class for JSON deserialization
        [System.Serializable]
        private class AdResponse
        {
            public int position;
        }
    }
}

