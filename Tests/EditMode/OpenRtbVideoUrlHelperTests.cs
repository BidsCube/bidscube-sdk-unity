using NUnit.Framework;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class OpenRtbVideoUrlHelperTests
    {
        [Test]
        public void IsLikelyDirectVideoUrl_Mp4_ReturnsTrue()
        {
            Assert.IsTrue(OpenRtbVideoUrlHelper.IsLikelyDirectVideoUrl("https://cdn.example.com/ad.mp4"));
        }

        [Test]
        public void IsLikelyDirectVideoUrl_VastAdTag_ReturnsFalse()
        {
            Assert.IsFalse(OpenRtbVideoUrlHelper.IsLikelyDirectVideoUrl("https://example.com/vast?type=wrapper"));
        }

        [Test]
        public void AssignHttpAdmFields_ClassifiesCorrectly()
        {
            OpenRtbVideoUrlHelper.AssignHttpAdmFields(
                "https://example.com/vast.xml",
                out var vastAdTagUrl,
                out var directVideoUrl);

            Assert.AreEqual("https://example.com/vast.xml", vastAdTagUrl);
            Assert.IsNull(directVideoUrl);

            OpenRtbVideoUrlHelper.AssignHttpAdmFields(
                "https://example.com/movie.webm",
                out vastAdTagUrl,
                out directVideoUrl);

            Assert.IsNull(vastAdTagUrl);
            Assert.AreEqual("https://example.com/movie.webm", directVideoUrl);
        }
    }
}
