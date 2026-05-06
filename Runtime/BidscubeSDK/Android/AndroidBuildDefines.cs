namespace BidscubeSDK.Android
{
    /// <summary>
    /// Added to <b>Android</b> Player scripting defines when <see cref="BidscubeAndroidFeatureSet.LiteNoVideo"/> is active.
    /// </summary>
    public static class AndroidBuildDefines
    {
        public const string LiteNoVideoSymbol = "BIDSCUBE_ANDROID_LITE_NO_VIDEO";
    }

    /// <summary>Compile-time: true in Android Lite player builds (check platform when testing).</summary>
    public static class AndroidLiteBuildInfo
    {
#if BIDSCUBE_ANDROID_LITE_NO_VIDEO
        public const bool IsLiteNoVideo = true;
#else
        public const bool IsLiteNoVideo = false;
#endif
    }
}
