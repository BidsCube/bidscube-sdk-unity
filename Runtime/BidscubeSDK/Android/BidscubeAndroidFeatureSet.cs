namespace BidscubeSDK.Android
{
    /// <summary>
    /// Android Bidscube core variant for Gradle export. Default is <see cref="LiteNoVideo"/> (smaller graph, no IMA/Media3).
    /// </summary>
    public enum BidscubeAndroidFeatureSet
    {
        LiteNoVideo = 0,
        FullWithVideo = 1
    }

    /// <summary>How the Bidscube Android core SDK is linked in <c>unityLibrary/build.gradle</c>.</summary>
    public enum BidscubeAndroidCoreDependencyMode
    {
        BundledUnityLibraryLibsAar = 0,
        MavenBidscubeSdkAar = 1,
        CustomGradleLines = 2,
        SkipInjectionIntegratorOwnsCore = 3
    }
}
