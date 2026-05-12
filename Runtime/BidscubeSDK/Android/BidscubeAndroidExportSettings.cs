using UnityEngine;

namespace BidscubeSDK.Android
{
    /// <summary>
    /// Optional project asset to pin Android export behaviour (commit for CI / teams parity).
    /// </summary>
    [CreateAssetMenu(fileName = "BidscubeAndroidExportSettings", menuName = "Bidscube/Android Export Settings", order = 10)]
    public sealed class BidscubeAndroidExportSettings : ScriptableObject
    {
        [Tooltip(
            "LiteNoVideo — bundled sdk-lite-no-video AAR, no video, no Media3/IMA, no injected core library desugaring. " +
            "WebViewVideoNoDesugar — bundled sdk-webview-video AAR, HTML5 video via Android WebView, no Media3/IMA/desugaring. " +
            "LegacyMediaVideoNoDesugar — bundled sdk-legacy-media-video AAR, VideoView/MediaPlayer only, no Media3/IMA/desugaring. " +
            "FullWithVideo — bundled sdk-full-video AAR or Maven sdk-full-video + Media3/IMA + desugar_jdk_libs.")]
        public BidscubeAndroidFeatureSet featureSet = BidscubeAndroidFeatureSet.LiteNoVideo;

        public BidscubeAndroidCoreDependencyMode coreDependencyMode = BidscubeAndroidCoreDependencyMode.BundledUnityLibraryLibsAar;

        [TextArea(2, 8)]
        [Tooltip("Used when coreDependencyMode == CustomGradleLines (newline-separated implementation lines).")]
        public string customCoreImplementationGradleLines = "";

        public bool forceCompileSdk;
        public int forceCompileSdkValue = 34;

        public bool forceMinSdk;
        public int forceMinSdkValue = 26;

        [Tooltip(
            "Legacy field kept for assets created before 1.2.8. Gradle desugaring injection is driven by the feature set: " +
            "LiteNoVideo/WebViewVideoNoDesugar/LegacyMediaVideoNoDesugar strip launcher desugaring lines; FullWithVideo ensures desugar_jdk_libs in the launcher.")]
        public bool enableDesugaring = true;
    }
}
