namespace BidscubeSDK
{
    /// <summary>
    /// Post-linear-video UI decision for fullscreen video ads.
    /// </summary>
    public enum VideoSessionEndAction
    {
        /// <summary>Dismiss fullscreen immediately (autoClose=true).</summary>
        AutoClose,
        /// <summary>Show VAST Companion end card (HTML / IFrame / Static).</summary>
        ShowCompanionEndCard,
        /// <summary>Keep VideoPlayer last frame (or post-video / mini-game surface) until manual close.</summary>
        KeepLastFrameOrPostVideoContent
    }

    /// <summary>
    /// Pure policy helper for <see cref="SDKConfig.AutoClose"/> + Companion availability.
    /// </summary>
    public static class VideoSessionEndPolicy
    {
        public static VideoSessionEndAction Resolve(bool autoClose, bool hasCompanion)
        {
            if (autoClose)
                return VideoSessionEndAction.AutoClose;

            if (hasCompanion)
                return VideoSessionEndAction.ShowCompanionEndCard;

            return VideoSessionEndAction.KeepLastFrameOrPostVideoContent;
        }

        public static bool ShouldGrantReward(
            VideoAdFormat format,
            bool completedNaturally,
            bool wasSkipped,
            bool alreadyRewarded)
        {
            if (alreadyRewarded)
                return false;
            if (format != VideoAdFormat.Rewarded)
                return false;
            if (!completedNaturally || wasSkipped)
                return false;
            return true;
        }
    }
}
