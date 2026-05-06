using BidscubeSDK.Android;
using UnityEditor;

namespace BidscubeSDK.Editor
{
    /// <summary>Editor preference fallback when no <see cref="BidscubeAndroidExportSettings"/> asset exists.</summary>
    public static class BidscubeAndroidFeatureSetStore
    {
        internal const string PrefKey = "Bidscube.Android.BidscubeAndroidFeatureSet";
        const string InitKey = "Bidscube.Android.BidscubeAndroidFeatureSetInitialized";

        public static BidscubeAndroidFeatureSet Load()
        {
            return (BidscubeAndroidFeatureSet)EditorPrefs.GetInt(PrefKey, (int)BidscubeAndroidFeatureSet.LiteNoVideo);
        }

        public static void Save(BidscubeAndroidFeatureSet value)
        {
            EditorPrefs.SetInt(PrefKey, (int)value);
        }

        public static bool HasInitializedDefaults()
        {
            return EditorPrefs.GetBool(InitKey, false);
        }

        public static void MarkInitializedDefaults()
        {
            EditorPrefs.SetBool(InitKey, true);
        }
    }
}
