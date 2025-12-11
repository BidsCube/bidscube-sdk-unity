using System;

namespace BidscubeSDK
{
    /// <summary>
    /// Ad callback interface
    /// </summary>
    public interface IAdCallback
    {
        /// <summary>
        /// Called when ad starts loading
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnAdLoading(string placementId);

        /// <summary>
        /// Called when ad is loaded
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnAdLoaded(string placementId);

        /// <summary>
        /// Called when ad is displayed
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnAdDisplayed(string placementId);

        /// <summary>
        /// Called when ad is clicked
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnAdClicked(string placementId);

        /// <summary>
        /// Called when ad is closed
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnAdClosed(string placementId);

        /// <summary>
        /// Called when ad fails to load
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="errorCode">Error code</param>
        /// <param name="errorMessage">Error message</param>
        void OnAdFailed(string placementId, int errorCode, string errorMessage);

        /// <summary>
        /// Called when video ad starts
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnVideoAdStarted(string placementId);

        /// <summary>
        /// Called when video ad completes
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnVideoAdCompleted(string placementId);

        /// <summary>
        /// Called when video ad is skipped
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnVideoAdSkipped(string placementId);

        /// <summary>
        /// Called when video ad becomes skippable
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        void OnVideoAdSkippable(string placementId);

        /// <summary>
        /// Called when install button is clicked
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="buttonText">Button text</param>
        void OnInstallButtonClicked(string placementId, string buttonText);
    }

    /// <summary>
    /// Base ad callback implementation with default empty methods
    /// </summary>
    public abstract class AdCallback : IAdCallback
    {
        public virtual void OnAdLoading(string placementId) { }
        public virtual void OnAdLoaded(string placementId) { }
        public virtual void OnAdDisplayed(string placementId) { }
        public virtual void OnAdClicked(string placementId) { }
        public virtual void OnAdClosed(string placementId) { }
        public virtual void OnAdFailed(string placementId, int errorCode, string errorMessage) { }
        public virtual void OnVideoAdStarted(string placementId) { }
        public virtual void OnVideoAdCompleted(string placementId) { }
        public virtual void OnVideoAdSkipped(string placementId) { }
        public virtual void OnVideoAdSkippable(string placementId) { }
        public virtual void OnInstallButtonClicked(string placementId, string buttonText) { }
    }

    /// <summary>
    /// Optional interface: implement this if you want to override SDK rendering of 'adm' content.
    /// Return true to indicate you handled rendering and the SDK should skip default rendering.
    /// </summary>
    public interface IAdRenderOverride
    {
        bool OnAdRenderOverride(string placementId, string adm, AdType adType, int position);
    }

    /// <summary>
    /// Consent callback interface
    /// </summary>
    public interface IConsentCallback
    {
        /// <summary>
        /// Called when consent info is updated
        /// </summary>
        void OnConsentInfoUpdated();

        /// <summary>
        /// Called when consent info update fails
        /// </summary>
        /// <param name="error">Error that occurred</param>
        void OnConsentInfoUpdateFailed(Exception error);

        /// <summary>
        /// Called when consent form is shown
        /// </summary>
        void OnConsentFormShown();

        /// <summary>
        /// Called when consent form has an error
        /// </summary>
        /// <param name="error">Error that occurred</param>
        void OnConsentFormError(Exception error);

        /// <summary>
        /// Called when consent is granted
        /// </summary>
        void OnConsentGranted();

        /// <summary>
        /// Called when consent is denied
        /// </summary>
        void OnConsentDenied();

        /// <summary>
        /// Called when consent is not required
        /// </summary>
        void OnConsentNotRequired();

        /// <summary>
        /// Called when consent status changes
        /// </summary>
        /// <param name="hasConsent">Whether consent is granted</param>
        void OnConsentStatusChanged(bool hasConsent);
    }

    /// <summary>
    /// Base consent callback implementation with default empty methods
    /// </summary>
    public abstract class ConsentCallback : IConsentCallback
    {
        public virtual void OnConsentInfoUpdated() { }
        public virtual void OnConsentInfoUpdateFailed(Exception error) { }
        public virtual void OnConsentFormShown() { }
        public virtual void OnConsentFormError(Exception error) { }
        public virtual void OnConsentGranted() { }
        public virtual void OnConsentDenied() { }
        public virtual void OnConsentNotRequired() { }
        public virtual void OnConsentStatusChanged(bool hasConsent) { }
    }
}