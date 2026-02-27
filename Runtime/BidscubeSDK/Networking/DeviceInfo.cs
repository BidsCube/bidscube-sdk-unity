using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BidscubeSDK
{
    /// <summary>
    /// Device information class
    /// </summary>
    public static class DeviceInfo
    {
        /// <summary>
        /// Get bundle ID
        /// </summary>
        public static string BundleId => Application.identifier;

        /// <summary>
        /// Get app name
        /// </summary>
        public static string AppName => Application.productName;

        /// <summary>
        /// Get app version
        /// </summary>
        public static string AppVersion => Application.version;

        /// <summary>
        /// Get app store URL
        /// </summary>
        public static string AppStoreURL => "https://play.google.com/store"; // Default for Unity

        /// <summary>
        /// Get device width (horizontal viewport dimension).
        /// Corrects for orientation: in portrait width &lt; height, in landscape width &gt; height.
        /// </summary>
        public static int DeviceWidth => GetOrientationCorrectedDimensions().width;

        /// <summary>
        /// Get device height (vertical viewport dimension).
        /// Corrects for orientation: in portrait width &lt; height, in landscape width &gt; height.
        /// </summary>
        public static int DeviceHeight => GetOrientationCorrectedDimensions().height;

        /// <summary>
        /// Returns width/height corrected for screen orientation.
        /// On some devices Screen.width/height can be swapped at startup; this fixes it.
        /// </summary>
        private static (int width, int height) GetOrientationCorrectedDimensions()
        {
            var w = Screen.width;
            var h = Screen.height;
            var orientation = Screen.orientation;

            // Portrait: width (horizontal) should be less than height (vertical)
            if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
            {
                if (w > h) return (h, w); // swapped, correct it
            }
            // Landscape: width should be greater than height
            else if (orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight)
            {
                if (w < h) return (h, w); // swapped, correct it
            }
            // Unknown/auto: trust Screen values
            return (w, h);
        }

        /// <summary>
        /// Get language
        /// </summary>
        public static string Language => Application.systemLanguage.ToString();

        /// <summary>
        /// Get user agent (browser-style, matches web SDK for consistent ad serving)
        /// </summary>
        public static string UserAgent
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                var version = SystemInfo.operatingSystem.Replace("Android ", "").Split(' ')[0];
                var model = SystemInfo.deviceModel ?? "Mobile";
                return $"Mozilla/5.0 (Linux; Android {version}; {model}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";
#elif UNITY_IOS && !UNITY_EDITOR
                var raw = SystemInfo.operatingSystem.Replace("iPhone OS ", "").Split(';')[0].Trim();
                var osVer = raw.Replace(".", "_");
                return $"Mozilla/5.0 (iPhone; CPU iPhone OS {osVer} like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
#else
                return "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
#endif
            }
        }

        /// <summary>
        /// Check if tracking is enabled
        /// </summary>
        public static bool IsTrackingEnabled => true; // Unity doesn't have built-in tracking restrictions

        /// <summary>
        /// Get advertising identifier (placeholder for Unity)
        /// </summary>
        public static string AdvertisingIdentifier => SystemInfo.deviceUniqueIdentifier;

        /// <summary>
        /// Get do not track flag
        /// </summary>
        public static int DoNotTrack => IsTrackingEnabled ? 0 : 1;

        /// <summary>
        /// Get GDPR flag
        /// </summary>
        public static string GDPR
        {
            get
            {
                var euCountries = new[] { "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE" };
                var currentCountry = GetCurrentCountryCode();
                return euCountries.Contains(currentCountry) ? "1" : "0";
            }
        }

        /// <summary>
        /// Get GDPR consent
        /// </summary>
        public static string GDPRConsent => "0";

        /// <summary>
        /// Get US privacy string
        /// </summary>
        public static string USPrivacy => "1---";

        /// <summary>
        /// Get CCPA flag
        /// </summary>
        public static string CCPA => "0";

        /// <summary>
        /// Get COPPA flag
        /// </summary>
        public static string COPPA => "0";

        /// <summary>
        /// Get network type
        /// </summary>
        public static string NetworkType => Application.internetReachability.ToString().ToLower();

        /// <summary>
        /// Get debug info dictionary
        /// </summary>
        public static Dictionary<string, object> GetDeviceInfo()
        {
            return new Dictionary<string, object>
            {
                { "bundleId", BundleId },
                { "appName", AppName },
                { "appVersion", AppVersion },
                { "deviceWidth", DeviceWidth },
                { "deviceHeight", DeviceHeight },
                { "language", Language },
                { "userAgent", UserAgent },
                { "trackingEnabled", IsTrackingEnabled },
                { "advertisingId", AdvertisingIdentifier },
                { "doNotTrack", DoNotTrack },
                { "unityVersion", Application.unityVersion },
                { "deviceModel", SystemInfo.deviceModel },
                { "operatingSystem", SystemInfo.operatingSystem },
                { "deviceType", SystemInfo.deviceType.ToString() },
                { "processorType", SystemInfo.processorType },
                { "processorCount", SystemInfo.processorCount },
                { "systemMemorySize", SystemInfo.systemMemorySize },
                { "graphicsDeviceName", SystemInfo.graphicsDeviceName },
                { "graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion },
                { "graphicsMemorySize", SystemInfo.graphicsMemorySize },
                { "graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString() },
                { "graphicsShaderLevel", SystemInfo.graphicsShaderLevel },
                { "graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded },
                { "supportsAccelerometer", SystemInfo.supportsAccelerometer },
                { "supportsGyroscope", SystemInfo.supportsGyroscope },
                { "supportsLocationService", SystemInfo.supportsLocationService },
                { "supportsVibration", SystemInfo.supportsVibration },
                { "batteryLevel", SystemInfo.batteryLevel },
                { "batteryStatus", SystemInfo.batteryStatus.ToString() },
                { "internetReachability", Application.internetReachability.ToString() }
            };
        }

        private static string GetCurrentCountryCode()
        {
            // Unity doesn't have built-in country detection
            // This would need to be implemented using a third-party service or plugin
            return "US"; // Default fallback
        }
    }
}