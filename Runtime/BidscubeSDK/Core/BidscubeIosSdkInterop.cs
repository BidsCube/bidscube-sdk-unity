#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace BidscubeSDK
{
    /// <summary>
    /// Forwards Unity <see cref="SDKConfig"/> / <see cref="BidscubeSDK.SetUserId"/> to native iOS <c>BidscubeSDK</c> (CocoaPods).
    /// </summary>
    internal static class BidscubeIosSdkInterop
    {
        [DllImport("__Internal")]
        private static extern void BidscubeUnityNativeSyncInitialize(
            string baseUrl,
            bool enableLogging,
            bool enableDebugMode,
            int defaultAdTimeoutMs,
            string userId);

        [DllImport("__Internal")]
        private static extern void BidscubeUnityNativeSetUserId(string userId);

        internal static void SyncInitializeFromUnityConfig(SDKConfig cfg, string userId)
        {
            if (cfg == null)
                return;

            try
            {
                BidscubeUnityNativeSyncInitialize(
                    cfg.BaseURL ?? Constants.BaseURL,
                    cfg.EnableLogging,
                    cfg.EnableDebugMode,
                    cfg.DefaultAdTimeoutMs,
                    userId);
                Logger.Info("Init (iOS native): BidscubeSDK initialize / user_id sync invoked.");
            }
            catch (Exception e)
            {
                Logger.Warning($"Init (iOS native): {e.GetType().Name}: {e.Message}");
            }
        }

        internal static void SyncSetUserId(string userId)
        {
            try
            {
                BidscubeUnityNativeSetUserId(userId);
            }
            catch (Exception e)
            {
                Logger.Warning($"SetUserId (iOS native): {e.GetType().Name}: {e.Message}");
            }
        }
    }
}
#endif
