using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.Tests
{
    public class VideoEndCardResolverTests
    {
        [Test]
        public void ShouldRenderInWebView_HtmlAndIFrameOnly()
        {
            Assert.IsTrue(VideoEndCardResolver.ShouldRenderInWebView(VASTParser.VastCompanionResourceType.Html));
            Assert.IsTrue(VideoEndCardResolver.ShouldRenderInWebView(VASTParser.VastCompanionResourceType.IFrame));
            Assert.IsFalse(VideoEndCardResolver.ShouldRenderInWebView(VASTParser.VastCompanionResourceType.StaticImage));
            Assert.IsFalse(VideoEndCardResolver.ShouldRenderInWebView(VASTParser.VastCompanionResourceType.None));
        }

        [Test]
        public void ResolveClickUrl_PrefersCompanionThenLinear()
        {
            var data = new VASTParser.VASTData
            {
                previewClickThroughUrl = "https://example.com/companion",
                clickThroughUrl = "https://example.com/linear"
            };
            Assert.AreEqual("https://example.com/companion", VideoEndCardResolver.ResolveClickUrl(data));

            data.previewClickThroughUrl = null;
            Assert.AreEqual("https://example.com/linear", VideoEndCardResolver.ResolveClickUrl(data));
            Assert.IsNull(VideoEndCardResolver.ResolveClickUrl(null));
        }

        [Test]
        public void ResolveStaticPreviewImageUrl_StaticCompanionThenPreview()
        {
            var data = new VASTParser.VASTData
            {
                companionResourceType = VASTParser.VastCompanionResourceType.StaticImage,
                companionResource = "https://example.com/end.jpg",
                previewImageUrl = "https://example.com/fallback.jpg"
            };
            Assert.AreEqual("https://example.com/end.jpg", VideoEndCardResolver.ResolveStaticPreviewImageUrl(data));

            data.companionResourceType = VASTParser.VastCompanionResourceType.Html;
            data.companionResource = "<div>end</div>";
            Assert.AreEqual("https://example.com/fallback.jpg", VideoEndCardResolver.ResolveStaticPreviewImageUrl(data));

            data.previewImageUrl = null;
            Assert.IsNull(VideoEndCardResolver.ResolveStaticPreviewImageUrl(data));
        }

        [Test]
        public void ResolveCtaLabel_DependsOnClickUrl()
        {
            Assert.AreEqual("Learn More", VideoEndCardResolver.ResolveCtaLabel("https://example.com"));
            Assert.AreEqual("Preview", VideoEndCardResolver.ResolveCtaLabel(null));
            Assert.AreEqual("Preview", VideoEndCardResolver.ResolveCtaLabel(""));
        }

        [Test]
        public void QaVastWithPreview_ResolvesStaticImageAndClick()
        {
            var data = VASTParser.Parse(QaVastFixtures.WithPreview);
            Assert.NotNull(data);
            Assert.IsFalse(VideoEndCardResolver.ShouldRenderInWebView(data.companionResourceType));
            Assert.AreEqual("https://www.gstatic.com/webp/gallery/3.jpg", VideoEndCardResolver.ResolveStaticPreviewImageUrl(data));
            Assert.AreEqual("https://www.google.com", VideoEndCardResolver.ResolveClickUrl(data));
        }

        [Test]
        public void QaVastNoPreview_HasNoStaticImage()
        {
            var data = VASTParser.Parse(QaVastFixtures.NoPreview);
            Assert.NotNull(data);
            Assert.IsNull(VideoEndCardResolver.ResolveStaticPreviewImageUrl(data));
            Assert.IsTrue(string.IsNullOrEmpty(VideoEndCardResolver.ResolveClickUrl(data)));
        }
    }
}
