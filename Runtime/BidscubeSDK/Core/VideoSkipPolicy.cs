namespace BidscubeSDK
{
    /// <summary>
    /// Pure skip-button policy for linear video ads.
    /// </summary>
    public static class VideoSkipPolicy
    {
        public static bool IsSkippable(int skipOffsetSeconds)
        {
            return skipOffsetSeconds > 0;
        }

        /// <summary>
        /// Countdown label while remaining seconds are positive; otherwise "Skip".
        /// </summary>
        public static string FormatCountdownLabel(int remainingSeconds)
        {
            if (remainingSeconds > 0)
                return $"Skip in {remainingSeconds}";
            return "Skip";
        }
    }
}
