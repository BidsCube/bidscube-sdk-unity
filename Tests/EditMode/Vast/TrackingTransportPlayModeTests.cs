using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BidscubeSDK.Tests
{
    public class TrackingTransportPlayModeTests
    {
        [UnityTest]
        public IEnumerator FireTrackingUrl_ReachesLocalCollector_And204IsNotAnError()
        {
            var port = 18770;
            var received = new List<string>();
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
            listener.Start();
            var accept = Task.Run(() =>
            {
                var context = listener.GetContext();
                received.Add(context.Request.RawUrl);
                context.Response.StatusCode = 204;
                context.Response.Close();
            });

            try
            {
                VASTParser.FireTrackingUrl(
                    "http://127.0.0.1:" + port + "/imp?cb=[CACHEBUSTING]",
                    "renderer=test session=playmode event=impression");
                var elapsed = 0f;
                while (received.Count == 0 && elapsed < 5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            finally
            {
                listener.Stop();
            }

            Assert.AreEqual(1, received.Count);
            StringAssert.StartsWith("/imp?cb=", received[0]);
            Assert.IsFalse(received[0].Contains("CACHEBUSTING"));
            accept.Wait(1000);
        }

        [UnityTest]
        public IEnumerator LoadVast_DoesNotTrackBeforePlaybackStarts()
        {
            var port = 18771;
            var hits = new List<string>();
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
            listener.Start();
            var accept = Task.Run(() =>
            {
                while (listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = listener.GetContext();
                    }
                    catch
                    {
                        break;
                    }
                    hits.Add(context.Request.RawUrl);
                    context.Response.StatusCode = 204;
                    context.Response.Close();
                }
            });

            var host = new GameObject("TrackingTransportTestAd");
            var view = host.AddComponent<VideoAdView>();
            var vast = "<VAST version=\"3.0\"><Ad id=\"only\"><InLine>"
                + "<Impression><![CDATA[http://127.0.0.1:" + port + "/imp]]></Impression>"
                + "<Creatives><Creative><Linear><MediaFiles>"
                + "<MediaFile><![CDATA[http://127.0.0.1:" + port + "/missing.mp4]]></MediaFile>"
                + "</MediaFiles></Linear></Creative></Creatives></InLine></Ad></VAST>";
            view.LoadVideoAdFromVastXml(vast);
            yield return new WaitForSecondsRealtime(1.5f);
            listener.Stop();
            Object.Destroy(host);

            foreach (var hit in hits)
                Assert.IsFalse(hit.Contains("/imp"), "impression fired before playback: " + hit);
            accept.Wait(500);
        }
    }
}
