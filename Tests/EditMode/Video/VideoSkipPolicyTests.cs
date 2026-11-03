using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.Tests
{
    public class VideoSkipPolicyTests
    {
        [Test]
        public void IsSkippable_OnlyPositiveOffset()
        {
            Assert.IsFalse(VideoSkipPolicy.IsSkippable(-1));
            Assert.IsFalse(VideoSkipPolicy.IsSkippable(0));
            Assert.IsTrue(VideoSkipPolicy.IsSkippable(5));
        }

        [Test]
        public void FormatCountdownLabel_CountdownThenSkip()
        {
            Assert.AreEqual("Skip in 5", VideoSkipPolicy.FormatCountdownLabel(5));
            Assert.AreEqual("Skip in 1", VideoSkipPolicy.FormatCountdownLabel(1));
            Assert.AreEqual("Skip", VideoSkipPolicy.FormatCountdownLabel(0));
            Assert.AreEqual("Skip", VideoSkipPolicy.FormatCountdownLabel(-1));
        }
    }
}
