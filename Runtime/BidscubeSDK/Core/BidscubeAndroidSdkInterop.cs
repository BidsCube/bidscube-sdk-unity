#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace BidscubeSDK
{
    /// <summary>
    /// Mirrors native Android <c>com.bidscube.sdk.BidscubeSDK</c> so AppLovin MAX mediation shares the same Java instance after <see cref="BidscubeSDK.Initialize(SDKConfig)"/>.
    /// </summary>
    internal static class BidscubeAndroidSdkInterop
    {
        private const string SdkClass = "com.bidscube.sdk.BidscubeSDK";
        private const string BuilderClass = "com.bidscube.sdk.config.SDKConfig$Builder";
        private const string UnityPlayerClass = "com.unity3d.player.UnityPlayer";

        internal static void SyncInitializeFromUnityConfig(SDKConfig cfg, string userId)
        {
            if (cfg == null)
                return;

            try
            {
                using (var unityPlayer = new AndroidJavaClass(UnityPlayerClass))
                {
                    var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    if (activity == null)
                    {
                        Logger.Warning("Init (Android Java): skipped — Unity currentActivity is null (too early?). Retry Initialize after Activity is ready.");
                        return;
                    }

                    var app = activity.Call<AndroidJavaObject>("getApplicationContext");
                    using (var sdk = new AndroidJavaClass(SdkClass))
                    {
                        if (sdk.CallStatic<bool>("isInitialized"))
                        {
                            sdk.CallStatic("setActivity", activity);
                            SyncSetUserId(userId);
                            Logger.Info("Init (Android Java): native SDK already initialized; setActivity + user_id sync applied.");
                            return;
                        }
                    }

                    Logger.Info("Init (Android Java): invoking com.bidscube.sdk.BidscubeSDK.initialize …");

                    using (var builder = new AndroidJavaObject(BuilderClass, app))
                    {
                        builder.Call<AndroidJavaObject>("enableLogging", cfg.EnableLogging);
                        builder.Call<AndroidJavaObject>("enableDebugMode", cfg.EnableDebugMode);
                        builder.Call<AndroidJavaObject>("defaultAdTimeout", cfg.DefaultAdTimeoutMs);
                        builder.Call<AndroidJavaObject>("defaultAdPosition", MapAdPositionToJavaEnumName(cfg.DefaultAdPosition));
                        TryApplyOptionalBuilderMethods(builder, cfg, userId);

                        using (var javaConfig = builder.Call<AndroidJavaObject>("build"))
                        using (var sdk = new AndroidJavaClass(SdkClass))
                        {
                            sdk.CallStatic("initialize", app, javaConfig);
                            sdk.CallStatic("setActivity", activity);
                        }
                    }

                    using (var sdk = new AndroidJavaClass(SdkClass))
                    {
                        if (sdk.CallStatic<bool>("isInitialized"))
                            Logger.Info("Init (Android Java): SUCCESS — native BidscubeSDK.isInitialized()==true.");
                        else
                            Logger.InfoError("Init (Android Java): initialize returned but isInitialized()==false.");
                    }
                }
            }
            catch (Exception e)
            {
                var msg = e.Message ?? string.Empty;
                if (e is AndroidJavaException && msg.Contains("ClassNotFoundException", StringComparison.Ordinal) &&
                    msg.Contains("com.bidscube.sdk", StringComparison.Ordinal))
                {
                    Logger.Warning(
                        "Init (Android Java): com.bidscube.sdk.BidscubeSDK not in APK (ClassNotFoundException). " +
                        "Unity C# ads still work; embed native Bidscube SDK AAR for MAX / user_id on native requests.");
                }
                else
                {
                    Logger.InfoError($"Init (Android Java): FAILED — {e.GetType().Name}: {e.Message}");
                }
            }
        }

        internal static void SyncSetUserId(string userId)
        {
            try
            {
                using (var sdk = new AndroidJavaClass(SdkClass))
                {
                    if (!sdk.CallStatic<bool>("isInitialized"))
                        return;

                    sdk.CallStatic("setUserId", userId);
                }
            }
            catch (Exception e)
            {
                Logger.Warning($"SetUserId (Android Java): {e.GetType().Name}: {e.Message}");
            }
        }

        private static void TryApplyOptionalBuilderMethods(AndroidJavaObject builder, SDKConfig cfg, string userId)
        {
            var baseUrl = cfg.BaseURL?.Trim();
            if (!string.IsNullOrEmpty(baseUrl) &&
                !TryInvokeBuilderReturnsBuilder(builder, new[] { "baseURL", "setBaseUrl", "setBaseURL" }, baseUrl))
            {
                Logger.Warning("[BidscubeAndroidSdkInterop] SDKConfig.Builder has no baseURL setter; C# BaseURL not applied on this native SDK version.");
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                if (!TryInvokeBuilderReturnsBuilder(builder, new[] { "userId", "setUserId" }, userId.Trim()))
                {
                    Logger.Warning("[BidscubeAndroidSdkInterop] SDKConfig.Builder has no userId setter; upgrade native SDK to 1.2.11+ for user_id postbacks.");
                }
            }
        }

        private static bool TryInvokeBuilderReturnsBuilder(AndroidJavaObject builder, string[] methodNames, string arg)
        {
            foreach (var name in methodNames)
            {
                try
                {
                    builder.Call(name, arg);
                    return true;
                }
                catch (Exception)
                {
                    // try next alias
                }
            }

            return false;
        }

        private static string MapAdPositionToJavaEnumName(AdPosition p)
        {
            switch (p)
            {
                case AdPosition.AboveTheFold: return "ABOVE_THE_FOLD";
                case AdPosition.DependOnScreenSize: return "MAYBE_DEPENDING_ON_SCREEN_SIZE";
                case AdPosition.BelowTheFold: return "BELOW_THE_FOLD";
                case AdPosition.Header: return "HEADER";
                case AdPosition.Footer: return "FOOTER";
                case AdPosition.Sidebar: return "SIDEBAR";
                case AdPosition.FullScreen: return "FULL_SCREEN";
                default: return "UNKNOWN";
            }
        }
    }
}
#endif
