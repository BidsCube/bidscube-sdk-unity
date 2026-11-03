using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace BidscubeSDK.QA
{
    /// <summary>
    /// Offline WebView location/referrer probe for Banner / Video companion / Native paths.
    /// Uses <see cref="WebViewObject"/> with <c>LoadHTML(html, "")</c> — same baseUrl as SDK.
    /// Auto-runs when marker file exists (see <see cref="TryAutoRunFromMarker"/>).
    /// </summary>
    public sealed class SdkLocationProbeRunner : MonoBehaviour
    {
        public static event Action<string> LogEmitted;

        const string MarkerFileName = "bidscube_run_location_probe";
        const string ReportFileName = "bidscube_location_probe_report.txt";

        WebViewObject _webView;
        Action<string> _pendingProbe;
        StringBuilder _report;
        bool _running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void TryAutoRunFromMarker()
        {
            try
            {
                string[] candidates =
                {
                    Path.Combine(Application.persistentDataPath, MarkerFileName),
#if UNITY_ANDROID && !UNITY_EDITOR
                    "/sdcard/Download/" + MarkerFileName,
                    "/storage/emulated/0/Download/" + MarkerFileName,
#endif
                };
                string found = null;
                foreach (var path in candidates)
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        found = path;
                        break;
                    }
                }
                if (found == null)
                    return;

                try { File.Delete(found); } catch { /* keep running even if delete fails */ }

                var runner = Ensure();
                runner.StartCoroutine(runner.DelayedStart());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SdkLocationProbe] auto-run check failed: " + ex.Message);
            }
        }

        IEnumerator DelayedStart()
        {
            // Wait a frame so WebView native plugin is ready on device.
            yield return null;
            yield return new WaitForSecondsRealtime(1f);
            RunAllAndLog();
        }

        public static SdkLocationProbeRunner Ensure()
        {
#if UNITY_2023_1_OR_NEWER
            var existing = FindFirstObjectByType<SdkLocationProbeRunner>();
#else
            var existing = FindObjectOfType<SdkLocationProbeRunner>();
#endif
            if (existing != null)
                return existing;
            var go = new GameObject("SdkLocationProbeRunner");
            DontDestroyOnLoad(go);
            return go.AddComponent<SdkLocationProbeRunner>();
        }

        public void RunAllAndLog()
        {
            if (_running)
            {
                Emit("PROBE already running");
                return;
            }
            StartCoroutine(RunAllCoroutine());
        }

        IEnumerator RunAllCoroutine()
        {
            _running = true;
            _report = new StringBuilder();
            Emit("═══ SDK PATH LOCATION PROBE START (SDK default baseUrl=\"\") ═══");

            EnsureWebView();

            Emit("——— BANNER ———");
            yield return ProbeHtml("BANNER", WebViewJsProbeFixtures.DspWrapperWithLocationCheckHtml);

            Emit("——— VIDEO COMPANION (HTMLResource path) ———");
            yield return ProbeHtml("VIDEO", SdkLocationProbeFixtures.VideoCompanionHtml);

            Emit("——— NATIVE (BuildNativeHtml-equivalent template in WebView) ———");
            yield return ProbeHtml("NATIVE", SdkLocationProbeFixtures.NativeProbeHtml);

            var report = FormatReport();
            Emit(report);
            Emit("═══ SDK PATH LOCATION PROBE DONE ═══");
            Debug.Log(report);
            TryWriteReportFile(report);
            _running = false;
        }

        string FormatReport()
        {
            return _report.ToString();
        }

        void TryWriteReportFile(string report)
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, ReportFileName);
                File.WriteAllText(path, report);
                Emit("Report written: " + path);
            }
            catch (Exception ex)
            {
                Emit("Report write failed: " + ex.Message);
            }
        }

        void AppendSection(string title, LocationProbeValues values)
        {
            _report.AppendLine(title + ":");
            if (values == null)
            {
                _report.AppendLine("window.location.href = <probe failed>");
                _report.AppendLine("window.location.origin = <probe failed>");
                _report.AppendLine("hostname = <probe failed>");
                _report.AppendLine("document.referrer = <probe failed>");
                _report.AppendLine("localStorage = <probe failed>");
                _report.AppendLine("sessionStorage = <probe failed>");
                _report.AppendLine("cookieWritable = <probe failed>");
                _report.AppendLine();
                return;
            }
            _report.AppendLine("window.location.href = " + values.Href);
            _report.AppendLine("window.location.origin = " + values.Origin);
            _report.AppendLine("hostname = " + values.Hostname);
            _report.AppendLine("document.referrer = " + values.Referrer);
            _report.AppendLine("localStorage = " + values.LocalStorage);
            _report.AppendLine("sessionStorage = " + values.SessionStorage);
            _report.AppendLine("cookieWritable = " + values.CookieWritable);
            _report.AppendLine();
        }

        IEnumerator ProbeHtml(string label, string html)
        {
            LocationProbeValues values = null;
            var done = false;

            _pendingProbe = payload =>
            {
                values = LocationProbeValues.TryParse(payload);
                if (values != null)
                {
                    Emit($"{label} href={values.Href} origin={values.Origin} hostname={values.Hostname} referrer={values.Referrer}");
                    Emit($"{label} localStorage={values.LocalStorage} sessionStorage={values.SessionStorage} cookieWritable={values.CookieWritable}");
                    Emit($"{label} checkPixel={values.CheckPixel}");
                }
                else
                {
                    Emit($"{label} PROBE invalid: {payload}");
                }
                done = true;
            };

            _webView.SetVisibility(true);
            _webView.LoadHTML(html ?? string.Empty, "");

            float timeout = 8f;
            while (!done && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!done)
            {
                Emit($"{label} PROBE TIMEOUT");
                // Fallback EvaluateJS + unity: callback already pending — try inject again
                InjectProbeScript();
                float extra = 3f;
                while (!done && extra > 0f)
                {
                    extra -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            AppendSection(
                label == "BANNER" ? "Баннер" : label == "VIDEO" ? "Video companion" : "Native",
                values);
            _pendingProbe = null;
            yield return new WaitForSecondsRealtime(0.1f);
        }

        void EnsureWebView()
        {
            if (_webView != null)
                return;

            var go = new GameObject("LocationProbeWebView");
            go.transform.SetParent(transform, false);
            _webView = go.AddComponent<WebViewObject>();
            _webView.Init(
                cb: OnWebViewMessage,
                err: msg => Emit("WebView err: " + msg),
                httpErr: msg => Emit("WebView httpErr: " + msg),
                ld: url =>
                {
                    Emit($"CallOnLoaded url={url}");
                    StartCoroutine(InjectAfterLoaded());
                },
                started: url => Emit($"CallOnStarted url={url}"),
                hooked: null,
                cookies: null,
                transparent: false);
            _webView.SetMargins(0, 0, 0, 0);
            _webView.SetVisibility(false);
        }

        IEnumerator InjectAfterLoaded()
        {
            yield return new WaitForSecondsRealtime(0.08f);
            InjectProbeScript();
        }

        void InjectProbeScript()
        {
            if (_webView == null)
                return;
            // unity: protocol → Init cb
            _webView.EvaluateJS(LocationProbeScript.UnityProtocolProbeJs);
        }

        void OnWebViewMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            if (msg.StartsWith("probe:", StringComparison.Ordinal))
            {
                var payload = Uri.UnescapeDataString(msg.Substring("probe:".Length));
                Emit("PROBE raw: " + payload);
                _pendingProbe?.Invoke(payload);
                return;
            }
            Emit("WebView msg: " + msg);
        }

        void Emit(string message)
        {
            var line = "[SdkLocationProbe] " + message;
            Debug.Log(line);
            LogEmitted?.Invoke(line);
        }
    }

    public sealed class LocationProbeValues
    {
        public string Href;
        public string Origin;
        public string Hostname;
        public string Referrer;
        public string Protocol;
        public string LocalStorage;
        public string SessionStorage;
        public string CookieWritable;
        public string CheckPixel;

        public static LocationProbeValues TryParse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            try
            {
                // Minimal JSON field extract (avoid Newtonsoft dependency)
                return new LocationProbeValues
                {
                    Href = Extract(raw, "href"),
                    Origin = Extract(raw, "origin"),
                    Hostname = Extract(raw, "hostname"),
                    Referrer = Extract(raw, "referrer"),
                    Protocol = Extract(raw, "protocol"),
                    LocalStorage = Extract(raw, "localStorage"),
                    SessionStorage = Extract(raw, "sessionStorage"),
                    CookieWritable = Extract(raw, "cookieWritable"),
                    CheckPixel = Extract(raw, "checkPixel")
                };
            }
            catch
            {
                return null;
            }
        }

        static string Extract(string json, string key)
        {
            var token = "\"" + key + "\"";
            var i = json.IndexOf(token, StringComparison.Ordinal);
            if (i < 0) return "";
            var colon = json.IndexOf(':', i + token.Length);
            if (colon < 0) return "";
            // boolean / null without quotes
            var j = colon + 1;
            while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
            if (j < json.Length && json[j] != '"')
            {
                var end = j;
                while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
                return json.Substring(j, end - j).Trim();
            }
            var q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return "";
            var q2 = q1 + 1;
            var sb = new StringBuilder();
            while (q2 < json.Length)
            {
                char c = json[q2];
                if (c == '\\' && q2 + 1 < json.Length)
                {
                    sb.Append(json[q2 + 1]);
                    q2 += 2;
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
                q2++;
            }
            return sb.ToString();
        }
    }

    public static class LocationProbeScript
    {
        /// <summary>
        /// Probes location + storage/cookie availability in the same WebView as SDK LoadHTML(..., "").
        /// </summary>
        public const string UnityProtocolProbeJs =
            "(function(){" +
            "function storageProbe(kind){" +
            "  try{" +
            "    var s=window[kind];" +
            "    if(!s) return 'unavailable';" +
            "    var k='__bc_probe_'+kind;" +
            "    s.setItem(k,'1');" +
            "    var ok=s.getItem(k)==='1';" +
            "    s.removeItem(k);" +
            "    return ok?'writable':'unavailable';" +
            "  }catch(e){ return 'blocked:'+String(e&&e.name?e.name:e); }" +
            "}" +
            "function cookieProbe(){" +
            "  try{" +
            "    var k='__bc_probe_cookie';" +
            "    document.cookie=k+'=1; path=/';" +
            "    var ok=document.cookie.indexOf(k+'=')!==-1;" +
            "    document.cookie=k+'=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';" +
            "    return ok?'true':'false';" +
            "  }catch(e){ return 'blocked:'+String(e&&e.name?e.name:e); }" +
            "}" +
            "var originVal='';" +
            "try{ originVal=String(location.origin); }catch(e){ originVal='error:'+String(e&&e.name?e.name:e); }" +
            "var payload=JSON.stringify({" +
            "href:String(location.href)," +
            "origin:originVal," +
            "hostname:String(location.hostname)," +
            "protocol:String(location.protocol)," +
            "referrer:String(document.referrer)," +
            "localStorage:storageProbe('localStorage')," +
            "sessionStorage:storageProbe('sessionStorage')," +
            "cookieWritable:cookieProbe()," +
            "checkPixel:'https://da-dsp.com/?t=check&u='+encodeURIComponent(location.hostname)" +
            "});" +
            "window.location='unity:probe:'+encodeURIComponent(payload);" +
            "})();";
    }
}
