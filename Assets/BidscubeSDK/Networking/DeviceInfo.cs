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
        /// Get device width
        /// </summary>
        public static int DeviceWidth => Screen.width;

        /// <summary>
        /// Get device height
        /// </summary>
        public static int DeviceHeight => Screen.height;

        /// <summary>
        /// Get language
        /// </summary>
        public static string Language => Application.systemLanguage.ToString();

        /// <summary>
        /// Get user agent
        /// </summary>
        public static string UserAgent
        {
            get
            {
                var appName = AppName;
                var appVersion = AppVersion;
                var unityVersion = Application.unityVersion;
                var os = SystemInfo.operatingSystem;
                
                return $"{appName}/{appVersion} (Unity {unityVersion}; {os})";
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