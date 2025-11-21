using UnityEngine;
using System;
using System.Collections;

namespace BidscubeSDK
{
    /// <summary>
    /// IMA Video Player wrapper for Unity
    /// This class provides a unified interface for IMA SDK integration
    /// When IMA SDK is available, it uses native IMA player
    /// Otherwise, falls back to custom VAST parser
    /// </summary>
    public class IMAVideoPlayer : MonoBehaviour
    {
        public enum PlayerType
        {
            IMA,        // Google IMA SDK
            Custom      // Custom VAST parser
        }

        private PlayerType _playerType = PlayerType.Custom;
        private bool _isInitialized = false;
        private string _adTagUrl;
        private IAdCallback _callback;
        private string _placementId;

        // IMA SDK references (when available)
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _imaAdsLoader;
        private AndroidJavaObject _imaAdsRequest;
        private AndroidJavaObject _imaAdDisplayContainer;
        private AndroidJavaObject _imaVideoPlayer;
#endif

#if UNITY_IOS && !UNITY_EDITOR
        // iOS IMA SDK would use native plugins
        // This would require native iOS code integration
#endif

        /// <summary>
        /// Initialize IMA player
        /// </summary>
        public void Initialize(string placementId, IAdCallback callback)
        {
            _placementId = placementId;
            _callback = callback;

            // Try to detect IMA SDK availability
            _playerType = DetectIMAAvailability();

            if (_playerType == PlayerType.IMA)
            {
                InitializeIMA();
            }
            else
            {
                Logger.Info("[IMAVideoPlayer] IMA SDK not detected, using custom VAST parser");
            }

            _isInitialized = true;
        }

        private PlayerType DetectIMAAvailability()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Check if IMA SDK classes are available
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    var classLoader = currentActivity.Call<AndroidJavaObject>("getClassLoader");
                    
                    // Try to load IMA SDK class
                    var imaClass = new AndroidJavaClass("com.google.ads.interactivemedia.v3.api.AdsLoader");
                    if (imaClass != null)
                    {
                        return PlayerType.IMA;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Info($"[IMAVideoPlayer] IMA SDK not found: {e.Message}");
            }
#endif

#if UNITY_IOS && !UNITY_EDITOR
            // Check for iOS IMA SDK
            // This would require checking for native library
            try
            {
                // Check if IMA SDK is linked
                // Implementation would depend on how IMA is integrated
            }
            catch
            {
                // IMA not available
            }
#endif

            return PlayerType.Custom;
        }

        private void InitializeIMA()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    
                    // Initialize IMA AdsLoader
                    var imaAdsLoaderClass = new AndroidJavaClass("com.google.ads.interactivemedia.v3.api.AdsLoader");
                    var imaSettingsClass = new AndroidJavaClass("com.google.ads.interactivemedia.v3.api.ImaSdkSettings");
                    
                    var settings = new AndroidJavaObject("com.google.ads.interactivemedia.v3.api.ImaSdkSettings");
                    _imaAdsLoader = imaAdsLoaderClass.CallStatic<AndroidJavaObject>("create", currentActivity, settings);
                    
                    Logger.Info("[IMAVideoPlayer] IMA SDK initialized for Android");
                }
            }
            catch (Exception e)
            {
                Logger.InfoError($"[IMAVideoPlayer] Failed to initialize IMA SDK: {e.Message}");
                _playerType = PlayerType.Custom;
            }
#endif

#if UNITY_IOS && !UNITY_EDITOR
            // Initialize iOS IMA SDK
            // This would require native iOS code
            Logger.Info("[IMAVideoPlayer] IMA SDK initialization for iOS would go here");
#endif
        }

        /// <summary>
        /// Request ad from VAST tag URL
        /// </summary>
        /// <param name="vastTagUrl">VAST tag URL</param>
        public void RequestAd(string vastTagUrl)
        {
            _adTagUrl = vastTagUrl;

            if (!_isInitialized)
            {
                Logger.InfoError("[IMAVideoPlayer] Player not initialized");
                return;
            }

            if (_playerType == PlayerType.IMA)
            {
                RequestAdWithIMA(vastTagUrl);
            }
            else
            {
                // Fallback to custom VAST parser (handled by VideoAdView)
                Logger.Info("[IMAVideoPlayer] Using custom VAST parser fallback");
            }
        }

        private void RequestAdWithIMA(string vastTagUrl)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Create ads request
                var imaAdsRequestClass = new AndroidJavaClass("com.google.ads.interactivemedia.v3.api.AdsRequest");
                _imaAdsRequest = new AndroidJavaObject("com.google.ads.interactivemedia.v3.api.AdsRequest");
                
                // Set ad tag URL
                _imaAdsRequest.Call("setAdTagUrl", vastTagUrl);
                
                // Request ad
                _imaAdsLoader.Call("requestAds", _imaAdsRequest);
                
                Logger.Info($"[IMAVideoPlayer] Requested ad from IMA: {vastTagUrl}");
            }
            catch (Exception e)
            {
                Logger.InfoError($"[IMAVideoPlayer] Failed to request ad with IMA: {e.Message}");
                _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.NetworkError, e.Message);
            }
#endif

#if UNITY_IOS && !UNITY_EDITOR
            // Request ad using iOS IMA SDK
            Logger.Info("[IMAVideoPlayer] Requesting ad with iOS IMA SDK");
#endif
        }

        /// <summary>
        /// Show ad (full screen)
        /// </summary>
        public void Show()
        {
            if (_playerType == PlayerType.IMA)
            {
                ShowWithIMA();
            }
        }

        private void ShowWithIMA()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Show IMA ad
                // Implementation depends on IMA SDK setup
                Logger.Info("[IMAVideoPlayer] Showing ad with IMA SDK");
            }
            catch (Exception e)
            {
                Logger.InfoError($"[IMAVideoPlayer] Failed to show ad: {e.Message}");
            }
#endif

#if UNITY_IOS && !UNITY_EDITOR
            // Show iOS IMA ad
            Logger.Info("[IMAVideoPlayer] Showing ad with iOS IMA SDK");
#endif
        }

        /// <summary>
        /// Get player type
        /// </summary>
        public PlayerType GetPlayerType()
        {
            return _playerType;
        }

        /// <summary>
        /// Check if IMA SDK is available
        /// </summary>
        public static bool IsIMAAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var imaClass = new AndroidJavaClass("com.google.ads.interactivemedia.v3.api.AdsLoader");
                return imaClass != null;
            }
            catch
            {
                return false;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // Check iOS IMA availability
            return false; // Would need native check
#else
            return false;
#endif
        }
    }
}

