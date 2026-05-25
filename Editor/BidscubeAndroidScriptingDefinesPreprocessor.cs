using UnityEditor;

namespace BidscubeSDK.Editor
{
    /// <summary>Keeps Android scripting defines aligned when assets load (Editor session).</summary>
    [InitializeOnLoad]
    internal static class BidscubeAndroidScriptingDefinesPreprocessor
    {
        static BidscubeAndroidScriptingDefinesPreprocessor()
        {
            EditorApplication.delayCall += () => BidscubeDefineApplicator.ApplyFromEffectiveSettings();
        }
    }
}
