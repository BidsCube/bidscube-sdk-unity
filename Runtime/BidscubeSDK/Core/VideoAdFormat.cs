namespace BidscubeSDK
{
    /// <summary>
    /// Video ad presentation format (SDK contract; not mediation network format).
    /// </summary>
    public enum VideoAdFormat
    {
        /// <summary>Full-screen video without user reward callback.</summary>
        Interstitial,

        /// <summary>Rewarded video; <see cref="IRewardedAdCallback.OnUserRewarded"/> fires only after playback completes.</summary>
        Rewarded
    }
}
