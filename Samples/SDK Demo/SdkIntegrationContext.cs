using UnityEngine;

namespace BidscubeSDK
{
    /// <summary>How the sample app / host integrates Bidscube with LevelPlay (ironSource).</summary>
    public enum SdkIntegrationMode
    {
        /// <summary>Bidscube SDK only; LevelPlay bridge should not run.</summary>
        BidscubeDirect = 0,
        /// <summary>Bidscube plus LevelPlay custom adapter (bridge receives UnitySendMessage from native).</summary>
        BidscubeWithLevelPlayAdapter = 1,
        /// <summary>Level Play mediation path: same bridge when testing adapter; use mediation UI / placements.</summary>
        LevelPlayMediation = 2,
    }

    /// <summary>Persisted integration mode for sample scenes and optional LevelPlay bridge bootstrap.</summary>
    public static class SdkIntegrationContext
    {
        private const string PlayerPrefsKey = "bcc_sdk_integration_mode";

        public static SdkIntegrationMode Mode { get; private set; } = SdkIntegrationMode.BidscubeDirect;

        /// <summary>When true, auto-created LevelPlay bridge must not exist (Bidscube-only build / test).</summary>
        public static bool SuppressLevelPlayBridge => Mode == SdkIntegrationMode.BidscubeDirect;

        public static void SetMode(SdkIntegrationMode mode)
        {
            Mode = mode;
            PlayerPrefs.SetInt(PlayerPrefsKey, (int)mode);
            PlayerPrefs.Save();
        }

        public static void LoadPersistedMode()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
                Mode = (SdkIntegrationMode)Mathf.Clamp(PlayerPrefs.GetInt(PlayerPrefsKey, 0), 0, 2);
        }

        public static string GetCurrentDescription()
        {
            switch (Mode)
            {
                case SdkIntegrationMode.BidscubeDirect:
                    return "Bidscube only: no LevelPlay bridge. Use Init and ad buttons below.";
                case SdkIntegrationMode.BidscubeWithLevelPlayAdapter:
                    return "Bidscube + ironSource adapter: keep LevelPlay-SDK-Unity; restart app if bridge was off.";
                case SdkIntegrationMode.LevelPlayMediation:
                    return "Level Play: test via ironSource mediation / custom network; bridge on when not \"only Bidscube\".";
                default:
                    return string.Empty;
            }
        }

        public static string GetCurrentShortStatus()
        {
            switch (Mode)
            {
                case SdkIntegrationMode.BidscubeDirect:
                    return "Mode: Bidscube only";
                case SdkIntegrationMode.BidscubeWithLevelPlayAdapter:
                    return "Mode: Bidscube + LevelPlay adapter";
                case SdkIntegrationMode.LevelPlayMediation:
                    return "Mode: Level Play mediation";
                default:
                    return "Mode: unknown";
            }
        }
    }

    internal static class SdkIntegrationContextBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadPersistedIntegrationMode()
        {
            SdkIntegrationContext.LoadPersistedMode();
        }
    }
}
