using System.Text.RegularExpressions;

namespace BidscubeSDK
{
    /// <summary>
    /// Extracts encoded ad markup (<c>adm</c>) from JSON responses aligned with typical SSP / OpenRTB
    /// mobile SDKs (flat <c>{"adm":"..."}</c> or nested <c>seatbid[].bid[].adm</c>).
    /// </summary>
    public static class AdMarkupExtractor
    {
        static readonly Regex AdmStringField = new Regex(
            "\"adm\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"",
            RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Attempts to read <paramref name="admMarkup"/> and optional logical <paramref name="width"/> /
        /// <paramref name="height"/> from a JSON auction response.
        /// </summary>
        public static bool TryExtractMarkup(
            string responseJson,
            out string admMarkup,
            out int width,
            out int height)
        {
            admMarkup = null;
            width = 0;
            height = 0;

            if (string.IsNullOrWhiteSpace(responseJson))
                return false;

            var s = responseJson.Trim();
            if (s.Length == 0 || s[0] != '{')
                return false;

            // 1) Flat SDK shape (same envelope as other BidsCube mobile SDKs)
            try
            {
                var flat = UnityEngine.JsonUtility.FromJson<AdResponse>(s);
                if (flat != null)
                {
                    admMarkup = flat.GetAdmString();
                    if (string.IsNullOrEmpty(admMarkup) && !string.IsNullOrEmpty(flat.adm))
                        admMarkup = flat.adm;
                    if (flat.width > 0) width = flat.width;
                    if (flat.height > 0) height = flat.height;
                    if (!string.IsNullOrEmpty(admMarkup))
                        return true;
                }
            }
            catch { /* JsonUtility mismatch */ }

            // 2) OpenRTB-ish: seatbid[].bid[].{ adm, w, h }
            try
            {
                var o = UnityEngine.JsonUtility.FromJson<OpenRtbSeatRoot>(s);
                if (o?.seatbid != null)
                {
                    foreach (var seat in o.seatbid)
                    {
                        if (seat?.bid == null)
                            continue;
                        foreach (var b in seat.bid)
                        {
                            if (b == null || string.IsNullOrEmpty(b.adm))
                                continue;
                            admMarkup = b.adm;
                            if (b.w > 0) width = b.w;
                            if (b.h > 0) height = b.h;
                            return true;
                        }
                    }
                }
            }
            catch { /* structure mismatch */ }

            // 3) Regex fallback when JsonUtility cannot map the root (unknown extra fields are OK, but some payloads fail anyway)
            var m = AdmStringField.Match(s);
            if (m.Success && m.Groups.Count > 1)
            {
                admMarkup = UnescapeJsonString(m.Groups[1].Value);
                return !string.IsNullOrEmpty(admMarkup);
            }

            return false;
        }

        static string UnescapeJsonString(string escaped)
        {
            if (string.IsNullOrEmpty(escaped))
                return escaped;
            return escaped
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\/", "/");
        }

        [System.Serializable]
        class OpenRtbSeatRoot
        {
            public OpenRtbSeatBlock[] seatbid;
        }

        [System.Serializable]
        class OpenRtbSeatBlock
        {
            public OpenRtbBidRecord[] bid;
        }

        [System.Serializable]
        class OpenRtbBidRecord
        {
            public string adm;
            public int w;
            public int h;
        }
    }
}
