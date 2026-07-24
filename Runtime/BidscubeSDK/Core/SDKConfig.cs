using System;
using UnityEngine;

namespace BidscubeSDK
{
    public enum OpenRtbPodDurationValidationMode
    {
        Lenient,
        Strict
    }

    public enum OpenRtbPodSkipPolicy
    {
        /// <summary>
        /// Non-user slot failures may continue the pod when <see cref="SDKConfig.VideoPodContinueOnSlotError"/> is true.
        /// User skip/close always stops the entire pod.
        /// </summary>
        SkipCurrentAndContinue,
        /// <summary>
        /// Any non-user slot failure stops the entire pod immediately.
        /// User skip/close always stops the entire pod.
        /// </summary>
        FailEntirePod
    }

    /// <summary>
    /// SDK configuration class
    /// </summary>
    [Serializable]
    public class SDKConfig
    {
        public bool EnableLogging { get; private set; }
        public bool EnableDebugMode { get; private set; }
        public int DefaultAdTimeoutMs { get; private set; }
        public AdPosition DefaultAdPosition { get; private set; }
        public string BaseURL { get; private set; }
        public AdSizeSettings AdSizeSettings { get; private set; }
        public bool DisableInitialization { get; private set; }
        public bool OpenRtbPodMetadataEnabled { get; private set; }
        public OpenRtbPodDurationValidationMode VideoPodDurationValidationMode { get; private set; }
        public OpenRtbPodSkipPolicy VideoPodSkipPolicy { get; private set; }
        public bool VideoPodContinueOnSlotError { get; private set; }
        public bool VideoPodShowCounter { get; private set; }
        /// <summary>
        /// Integrator-provided user id sent on ad requests as <c>user_id</c> for server postbacks.
        /// </summary>
        public string UserId { get; private set; }

        private SDKConfig(
            bool enableLogging,
            bool enableDebugMode,
            int defaultAdTimeoutMs,
            AdPosition defaultAdPosition,
            string baseURL,
            AdSizeSettings adSizeSettings,
            bool disableInitialization,
            bool openRtbPodMetadataEnabled,
            OpenRtbPodDurationValidationMode videoPodDurationValidationMode,
            OpenRtbPodSkipPolicy videoPodSkipPolicy,
            bool videoPodContinueOnSlotError,
            bool videoPodShowCounter,
            string userId)
        {
            EnableLogging = enableLogging;
            EnableDebugMode = enableDebugMode;
            DefaultAdTimeoutMs = defaultAdTimeoutMs;
            DefaultAdPosition = defaultAdPosition;
            BaseURL = baseURL;
            AdSizeSettings = adSizeSettings;
            DisableInitialization = disableInitialization;
            OpenRtbPodMetadataEnabled = openRtbPodMetadataEnabled;
            VideoPodDurationValidationMode = videoPodDurationValidationMode;
            VideoPodSkipPolicy = videoPodSkipPolicy;
            VideoPodContinueOnSlotError = videoPodContinueOnSlotError;
            VideoPodShowCounter = videoPodShowCounter;
            UserId = userId;
        }

        /// <summary>
        /// Builder class for SDK configuration
        /// </summary>
        public class Builder
        {
            private bool _enableLogging = true;
            private bool _enableDebugMode = false;
            private int _defaultAdTimeoutMs = 30000;
            private AdPosition _defaultAdPosition = AdPosition.Unknown;
            private string _baseURL = Constants.BaseURL;
            private AdSizeSettings _adSizeSettings = null;
            private bool _disableInitialization = false;
            private bool _openRtbPodMetadataEnabled = true;
            private OpenRtbPodDurationValidationMode _videoPodDurationValidationMode =
                OpenRtbPodDurationValidationMode.Lenient;
            private OpenRtbPodSkipPolicy _videoPodSkipPolicy = OpenRtbPodSkipPolicy.SkipCurrentAndContinue;
            private bool _videoPodContinueOnSlotError = true;
            private bool _videoPodShowCounter = true;
            private string _userId = null;

            public Builder() { }

            /// <summary>
            /// Enable logging
            /// </summary>
            /// <param name="value">Enable logging flag</param>
            /// <returns>Builder instance</returns>
            public Builder EnableLogging(bool value)
            {
                _enableLogging = value;
                return this;
            }

            /// <summary>
            /// Enable debug mode
            /// </summary>
            /// <param name="value">Enable debug mode flag</param>
            /// <returns>Builder instance</returns>
            public Builder EnableDebugMode(bool value)
            {
                _enableDebugMode = value;
                return this;
            }

            /// <summary>
            /// Set default ad timeout
            /// </summary>
            /// <param name="millis">Timeout in milliseconds</param>
            /// <returns>Builder instance</returns>
            public Builder DefaultAdTimeout(int millis)
            {
                _defaultAdTimeoutMs = millis;
                return this;
            }

            /// <summary>
            /// Set default ad position
            /// </summary>
            /// <param name="position">Default ad position</param>
            /// <returns>Builder instance</returns>
            public Builder DefaultAdPosition(AdPosition position)
            {
                _defaultAdPosition = position;
                return this;
            }

            /// <summary>
            /// Set base URL
            /// </summary>
            /// <param name="url">Base URL</param>
            /// <returns>Builder instance</returns>
            public Builder BaseURL(string url)
            {
                _baseURL = url;
                return this;
            }

            /// <summary>
            /// Set AdSizeSettings asset to provide default ad sizes
            /// </summary>
            public Builder AdSizeSettings(AdSizeSettings settings)
            {
                _adSizeSettings = settings;
                return this;
            }

            /// <summary>
            /// When true, calling <c>BidscubeSDK.Initialize(config)</c> becomes a no-op.
            /// Useful for builds / environments where you want the app to ship without initializing Bidscube.
            /// </summary>
            public Builder DisableInitialization(bool value)
            {
                _disableInitialization = value;
                return this;
            }

            public Builder OpenRtbPodMetadataEnabled(bool value)
            {
                _openRtbPodMetadataEnabled = value;
                return this;
            }

            public Builder VideoPodDurationValidationMode(OpenRtbPodDurationValidationMode value)
            {
                _videoPodDurationValidationMode = value;
                return this;
            }

            public Builder VideoPodSkipPolicy(OpenRtbPodSkipPolicy value)
            {
                _videoPodSkipPolicy = value;
                return this;
            }

            public Builder VideoPodContinueOnSlotError(bool value)
            {
                _videoPodContinueOnSlotError = value;
                return this;
            }

            public Builder VideoPodShowCounter(bool value)
            {
                _videoPodShowCounter = value;
                return this;
            }

            /// <summary>
            /// Set the integrator user id. Sent on every ad request as query param <c>user_id</c>
            /// so the SSP can include it in postbacks.
            /// </summary>
            /// <param name="userId">App user identifier (empty/null omitted from requests)</param>
            public Builder UserId(string userId)
            {
                _userId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
                return this;
            }

            /// <summary>
            /// Build SDK configuration
            /// </summary>
            /// <returns>SDK configuration</returns>
            public SDKConfig Build()
            {
                return new SDKConfig(
                    _enableLogging,
                    _enableDebugMode,
                    _defaultAdTimeoutMs,
                    _defaultAdPosition,
                    _baseURL,
                    _adSizeSettings,
                    _disableInitialization,
                    _openRtbPodMetadataEnabled,
                    _videoPodDurationValidationMode,
                    _videoPodSkipPolicy,
                    _videoPodContinueOnSlotError,
                    _videoPodShowCounter,
                    _userId
                );
            }
        }

        /// <summary>
        /// Get detected app ID
        /// </summary>
        public static string DetectedAppId
        {
            get
            {
                return Application.identifier;
            }
        }

        /// <summary>
        /// Get detected app name
        /// </summary>
        public static string DetectedAppName
        {
            get
            {
                return Application.productName;
            }
        }

        /// <summary>
        /// Get detected app version
        /// </summary>
        public static string DetectedAppVersion
        {
            get
            {
                return Application.version;
            }
        }

        /// <summary>
        /// Get detected language
        /// </summary>
        public static string DetectedLanguage
        {
            get
            {
                return Application.systemLanguage.ToString();
            }
        }

        /// <summary>
        /// Get detected user agent
        /// </summary>
        public static string DetectedUserAgent
        {
            get
            {
                return $"BidscubeSDK-Unity/1.0 (Unity {Application.unityVersion}; {SystemInfo.operatingSystem})";
            }
        }
    }
}
