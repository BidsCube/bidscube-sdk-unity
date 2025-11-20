using UnityEngine;

namespace BidscubeSDK
{
    /// <summary>
    /// Logger class for SDK logging
    /// </summary>
    public static class Logger
    {
        private static bool _isEnabled = true;
        private static bool _isDebugMode = false;

        /// <summary>
        /// Configure logger from SDK config
        /// </summary>
        /// <param name="config">SDK configuration</param>
        public static void Configure(SDKConfig config)
        {
            _isEnabled = config.EnableLogging;
            _isDebugMode = config.EnableDebugMode;
        }

        /// <summary>
        /// Log info message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void Info(string message)
        {
            if (_isEnabled)
            {
                UnityEngine.Debug.Log($"[BidscubeSDK] {message}");
            }
        }

        /// <summary>
        /// Log error message (alias for InfoError)
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void InfoError(string message)
        {
            if (_isEnabled)
            {
                UnityEngine.Debug.LogError($"[BidscubeSDK] {message}");
            }
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void DebugLog(string message)
        {
            if (_isEnabled && _isDebugMode)
            {
                UnityEngine.Debug.Log($"[BidscubeSDK] DEBUG: {message}");
            }
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void Warning(string message)
        {
            if (_isEnabled)
            {
                UnityEngine.Debug.LogWarning($"[BidscubeSDK] WARNING: {message}");
            }
        }

        /// <summary>
        /// Log error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void Error(string message)
        {
            if (_isEnabled)
            {
                InfoError($"[BidscubeSDK] ERROR: {message}");
            }
        }
    }
}