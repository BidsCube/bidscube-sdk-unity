namespace BidscubeSDK
{
    /// <summary>
    /// Unified video lifecycle events for custom VideoPlayer and IMA bridges.
    /// </summary>
    public interface IVideoPlayerEventListener
    {
        void OnVideoLoaded();
        void OnVideoStarted();
        void OnVideoClicked();
        void OnVideoCompleted();
        void OnVideoSkipped();
        void OnVideoFailed(int errorCode, string message);
    }
}
