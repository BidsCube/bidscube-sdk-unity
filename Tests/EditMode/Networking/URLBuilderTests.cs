using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.Tests
{
    public class URLBuilderTests
    {
        static readonly DeviceSnapshot Fixture = new DeviceSnapshot(
            bundleId: "com.bidscube.test",
            appName: "Test App",
            appStoreURL: "https://play.google.com/store",
            language: "English",
            deviceWidth: 1080,
            deviceHeight: 1920,
            userAgent: "TestUA",
            advertisingIdentifier: "ifa-1",
            doNotTrack: 0,
            gdpr: "0",
            gdprConsent: "0",
            usPrivacy: "1---",
            ccpa: "0",
            coppa: "0");

        [Test]
        public void Image_UsesBannerContentTypeAndJsResource()
        {
            var url = URLBuilder.BuildAdRequestURL(
                "https://ssp.example.com/", "20214", AdType.Image,
                AdPosition.Header, 30000, true, Fixture);

            StringAssert.Contains("placementId=20214", url);
            StringAssert.Contains("&c=b", url);
            StringAssert.Contains("&res=js", url);
            StringAssert.StartsWith("https://ssp.example.com?", url);
        }

        [Test]
        public void Video_UsesVideoContentTypeAndJsonResource()
        {
            var url = URLBuilder.BuildAdRequestURL(
                "https://ssp.example.com", "v1", AdType.Video,
                AdPosition.FullScreen, 30000, false, Fixture);

            StringAssert.Contains("&c=v", url);
            StringAssert.Contains("&res=json", url);
        }

        [Test]
        public void Native_UsesNativeContentTypeAndJsonResource()
        {
            var url = URLBuilder.BuildAdRequestURL(
                "https://ssp.example.com", "n1", AdType.Native,
                AdPosition.Unknown, 30000, false, Fixture);

            StringAssert.Contains("&c=n", url);
            StringAssert.Contains("&res=json", url);
        }

        [Test]
        public void UserId_IsUrlEncoded()
        {
            var url = URLBuilder.BuildAdRequestURL(
                "https://ssp.example.com", "p1", AdType.Image,
                AdPosition.Unknown, 30000, false, Fixture, userId: "user a/b");

            StringAssert.Contains("&user_id=user%20a%2Fb", url);
        }

        [Test]
        public void FixtureSnapshot_IsEmbedded()
        {
            var url = URLBuilder.BuildAdRequestURL(
                "https://ssp.example.com", "p1", AdType.Image,
                AdPosition.Unknown, 30000, false, Fixture);

            StringAssert.Contains("bundle=com.bidscube.test", url);
            StringAssert.Contains("name=Test%20App", url);
            StringAssert.Contains("deviceWidth=1080", url);
            StringAssert.Contains("deviceHeight=1920", url);
            StringAssert.Contains("ifa=ifa-1", url);
            StringAssert.Contains("dnt=0", url);
        }

        [Test]
        public void ContentTypeHelpers_MatchAdType()
        {
            Assert.AreEqual("b", URLBuilder.GetContentType(AdType.Image));
            Assert.AreEqual("v", URLBuilder.GetContentType(AdType.Video));
            Assert.AreEqual("n", URLBuilder.GetContentType(AdType.Native));
            Assert.AreEqual("js", URLBuilder.GetResponseResource(AdType.Image));
            Assert.AreEqual("json", URLBuilder.GetResponseResource(AdType.Video));
            Assert.AreEqual("json", URLBuilder.GetResponseResource(AdType.Native));
        }
    }
}
