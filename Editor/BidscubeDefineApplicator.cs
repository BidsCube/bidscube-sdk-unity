using System;
using System.Collections.Generic;
using System.Linq;
using BidscubeSDK.Android;
using UnityEditor;
#if UNITY_2022_1_OR_NEWER
using UnityEditor.Build;
#endif

namespace BidscubeSDK.Editor
{
    public static class BidscubeDefineApplicator
    {
        /// <summary>
        /// Android Lite scripting define. Must stay identical to <c>BidscubeSDK.Android.AndroidBuildDefines.LiteNoVideoSymbol</c> in the runtime assembly.
        /// </summary>
        const string LiteNoVideoAndroidDefine = "BIDSCUBE_ANDROID_LITE_NO_VIDEO";

        const string LegacyEnableVideo = "BIDSCUBE_ENABLE_VIDEO";

        public static void ApplyFromEffectiveSettings()
        {
            Apply(BidscubeAndroidExportSettingsResolver.GetEffectiveFeatureSet());
        }

        public static void ApplyFromStoredFeatureSet()
        {
            Apply(BidscubeAndroidFeatureSetStore.Load());
        }

        public static void Apply(BidscubeAndroidFeatureSet featureSet)
        {
            var liteAndroid = featureSet == BidscubeAndroidFeatureSet.LiteNoVideo;
            ApplyLiteNoVideoDefineForAndroid(liteAndroid);
        }

        /// <summary>
        /// Android-only: adds the LiteNoVideo scripting define for LiteNoVideo; removes for FullWithVideo.
        /// Also strips legacy <c>BIDSCUBE_ENABLE_VIDEO</c> from all groups.
        /// </summary>
        static void ApplyLiteNoVideoDefineForAndroid(bool liteNoVideo)
        {
            var liteSym = LiteNoVideoAndroidDefine;
            try
            {
                ApplySymbolForGroup(BuildTargetGroup.Android, liteSym, liteNoVideo);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Bidscube Android] Could not set Android scripting defines: {e.Message}");
            }

            StripLegacyEnableVideoFromAllGroups();
        }

        static void StripLegacyEnableVideoFromAllGroups()
        {
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown)
                    continue;
                try
                {
                    RemoveSymbolFromGroup(group, LegacyEnableVideo);
                }
                catch
                {
                    // ignored
                }
            }
        }

        static bool DefineSymbolSetsEqual(string a, string b)
        {
            return string.Equals(CanonicalDefineKey(a), CanonicalDefineKey(b), StringComparison.Ordinal);
        }

        static string CanonicalDefineKey(string defines)
        {
            if (string.IsNullOrEmpty(defines))
                return "";
            var parts = defines
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal);
            return string.Join(";", parts);
        }

        static void ApplySymbolForGroup(BuildTargetGroup group, string symbol, bool add)
        {
            var defines = GetScriptingDefineSymbols(group);
            var list = new List<string>();
            foreach (var p in defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (p != symbol)
                    list.Add(p);
            }

            if (add)
                list.Add(symbol);
            var next = string.Join(";", list);
            if (DefineSymbolSetsEqual(next, defines))
                return;
            SetScriptingDefineSymbols(group, next);
        }

        static void RemoveSymbolFromGroup(BuildTargetGroup group, string symbol)
        {
            var defines = GetScriptingDefineSymbols(group);
            var tokens = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (Array.IndexOf(tokens, symbol) < 0)
                return;
            var list = new List<string>();
            foreach (var p in defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (p != symbol)
                    list.Add(p);
            }

            var next = string.Join(";", list);
            if (DefineSymbolSetsEqual(next, defines))
                return;
            SetScriptingDefineSymbols(group, next);
        }

        public static string GetScriptingDefineSymbols(BuildTargetGroup group)
        {
#if UNITY_2022_1_OR_NEWER
            return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
        }

        internal static void SetScriptingDefineSymbols(BuildTargetGroup group, string defines)
        {
#if UNITY_2022_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
#endif
        }
    }
}
