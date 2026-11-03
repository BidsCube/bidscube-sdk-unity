namespace BidscubeSDK
{
    /// <summary>
    /// Snapshot of device/app fields used to build ad request URLs.
    /// Construct via <see cref="FromDeviceInfo"/> at runtime, or a fixture in tests.
    /// </summary>
    public readonly struct DeviceSnapshot
    {
        public readonly string BundleId;
        public readonly string AppName;
        public readonly string AppStoreURL;
        public readonly string Language;
        public readonly int DeviceWidth;
        public readonly int DeviceHeight;
        public readonly string UserAgent;
        public readonly string AdvertisingIdentifier;
        public readonly int DoNotTrack;
        public readonly string GDPR;
        public readonly string GDPRConsent;
        public readonly string USPrivacy;
        public readonly string CCPA;
        public readonly string COPPA;

        public DeviceSnapshot(
            string bundleId,
            string appName,
            string appStoreURL,
            string language,
            int deviceWidth,
            int deviceHeight,
            string userAgent,
            string advertisingIdentifier,
            int doNotTrack,
            string gdpr,
            string gdprConsent,
            string usPrivacy,
            string ccpa,
            string coppa)
        {
            BundleId = bundleId ?? "";
            AppName = appName ?? "";
            AppStoreURL = appStoreURL ?? "";
            Language = language ?? "";
            DeviceWidth = deviceWidth;
            DeviceHeight = deviceHeight;
            UserAgent = userAgent ?? "";
            AdvertisingIdentifier = advertisingIdentifier ?? "";
            DoNotTrack = doNotTrack;
            GDPR = gdpr ?? "";
            GDPRConsent = gdprConsent ?? "";
            USPrivacy = usPrivacy ?? "";
            CCPA = ccpa ?? "0";
            COPPA = coppa ?? "0";
        }

        public static DeviceSnapshot FromDeviceInfo()
        {
            return new DeviceSnapshot(
                DeviceInfo.BundleId,
                DeviceInfo.AppName,
                DeviceInfo.AppStoreURL,
                DeviceInfo.Language,
                DeviceInfo.DeviceWidth,
                DeviceInfo.DeviceHeight,
                DeviceInfo.UserAgent,
                DeviceInfo.AdvertisingIdentifier,
                DeviceInfo.DoNotTrack,
                DeviceInfo.GDPR,
                DeviceInfo.GDPRConsent,
                DeviceInfo.USPrivacy,
                DeviceInfo.CCPA,
                DeviceInfo.COPPA);
        }
    }
}
