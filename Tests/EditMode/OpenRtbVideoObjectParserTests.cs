using System.Collections.Generic;
using NUnit.Framework;

namespace BidscubeSDK.OpenRTB.Tests
{
    public class OpenRtbVideoObjectParserTests
    {
        [Test]
        public void FindVideoObject_OpenRtbLowerCase()
        {
            var root = new Dictionary<string, object>
            {
                ["openrtb"] = new Dictionary<string, object>
                {
                    ["video"] = new Dictionary<string, object> { ["podid"] = "pod-1" }
                }
            };

            var video = OpenRtbVideoObjectParser.FindVideoObject(root);
            Assert.NotNull(video);
            Assert.AreEqual("pod-1", OpenRtbVideoObjectParser.StringValue(video["podid"]));
        }

        [Test]
        public void FindVideoObject_OpenRtbCamelCase()
        {
            var root = new Dictionary<string, object>
            {
                ["openRtb"] = new Dictionary<string, object>
                {
                    ["video"] = new Dictionary<string, object> { ["poddur"] = 60 }
                }
            };

            var video = OpenRtbVideoObjectParser.FindVideoObject(root);
            Assert.NotNull(video);
            Assert.AreEqual(60, OpenRtbVideoObjectParser.IntValue(video["poddur"]));
        }

        [Test]
        public void IntValue_RejectsNaNAndInfinity()
        {
            Assert.IsNull(OpenRtbVideoObjectParser.IntValue(double.NaN));
            Assert.IsNull(OpenRtbVideoObjectParser.IntValue(double.PositiveInfinity));
            Assert.AreEqual(5, OpenRtbVideoObjectParser.IntValue("5"));
        }

        [Test]
        public void IntArrayValue_ParsesMixedTypes()
        {
            var list = new List<object> { 15, 30.0, "45", "bad", double.NaN };
            var result = OpenRtbVideoObjectParser.IntArrayValue(list);
            CollectionAssert.AreEqual(new[] { 15, 30, 45 }, result);
        }
    }
}
