using NUnit.Framework;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class OpenRtbPodSkipPolicyTests
    {
        [Test]
        public void VideoPodSkipPolicy_Default_IsSkipCurrentAndContinue()
        {
            var config = new SDKConfig.Builder().Build();
            Assert.AreEqual(OpenRtbPodSkipPolicy.SkipCurrentAndContinue, config.VideoPodSkipPolicy);
        }

        [Test]
        public void VideoPodSkipPolicy_Builder_SetsFailEntirePod()
        {
            var config = new SDKConfig.Builder()
                .VideoPodSkipPolicy(OpenRtbPodSkipPolicy.FailEntirePod)
                .Build();
            Assert.AreEqual(OpenRtbPodSkipPolicy.FailEntirePod, config.VideoPodSkipPolicy);
        }
    }
}
