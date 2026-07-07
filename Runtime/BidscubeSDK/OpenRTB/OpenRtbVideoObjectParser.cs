using System;
using System.Collections.Generic;

namespace BidscubeSDK.OpenRTB
{
    internal static class OpenRtbVideoObjectParser
    {
        internal static Dictionary<string, object> FindVideoObject(Dictionary<string, object> root)
        {
            if (root == null)
                return null;

            var openrtb = ObjectValue(GetIgnoreCase(root, "openrtb"));
            if (openrtb != null)
            {
                var video = ObjectValue(GetIgnoreCase(openrtb, "video"));
                if (video != null)
                    return video;
            }

            var openRtb = ObjectValue(GetIgnoreCase(root, "openRtb"));
            if (openRtb != null)
            {
                var video = ObjectValue(GetIgnoreCase(openRtb, "video"));
                if (video != null)
                    return video;
            }

            return ObjectValue(GetIgnoreCase(root, "video"));
        }

        internal static int? IntValue(object value)
        {
            if (value == null)
                return null;

            try
            {
                switch (value)
                {
                    case int i: return i;
                    case long l when l >= int.MinValue && l <= int.MaxValue: return (int)l;
                    case double d when !double.IsNaN(d) && !double.IsInfinity(d): return (int)d;
                    case float f when !float.IsNaN(f) && !float.IsInfinity(f): return (int)f;
                    case decimal m: return (int)m;
                    case string s when int.TryParse(s, out int parsed): return parsed;
                    case string s2 when double.TryParse(s2, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double pd)
                        && !double.IsNaN(pd) && !double.IsInfinity(pd):
                        return (int)pd;
                    default: return null;
                }
            }
            catch
            {
                return null;
            }
        }

        internal static double? DoubleValue(object value)
        {
            if (value == null)
                return null;

            try
            {
                switch (value)
                {
                    case double d when !double.IsNaN(d) && !double.IsInfinity(d): return d;
                    case float f when !float.IsNaN(f) && !float.IsInfinity(f): return f;
                    case int i: return i;
                    case long l: return l;
                    case decimal m: return (double)m;
                    case string s when double.TryParse(s, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double parsed)
                        && !double.IsNaN(parsed) && !double.IsInfinity(parsed):
                        return parsed;
                    default: return null;
                }
            }
            catch
            {
                return null;
            }
        }

        internal static string StringValue(object value)
        {
            if (value == null)
                return null;
            var s = value as string ?? value.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        internal static List<int> IntArrayValue(object value)
        {
            var result = new List<int>();
            var array = ArrayValue(value);
            if (array == null)
                return result;

            foreach (var item in array)
            {
                var parsed = IntValue(item);
                if (parsed.HasValue)
                    result.Add(parsed.Value);
            }

            return result;
        }

        internal static Dictionary<string, object> ObjectValue(object value)
        {
            return value as Dictionary<string, object>;
        }

        internal static List<object> ArrayValue(object value)
        {
            return value as List<object>;
        }

        internal static object GetIgnoreCase(Dictionary<string, object> dict, string key)
        {
            if (dict == null || string.IsNullOrEmpty(key))
                return null;

            if (dict.TryGetValue(key, out var exact))
                return exact;

            foreach (var kv in dict)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            return null;
        }
    }
}
