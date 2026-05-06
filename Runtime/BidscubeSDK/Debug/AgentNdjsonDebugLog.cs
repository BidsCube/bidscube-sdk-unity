using System;
using System.IO;
using System.Text;

namespace BidscubeSDK
{
    /// <summary>Debug-mode NDJSON append for Cursor agent session (Unity Editor on Mac).</summary>
    public static class AgentNdjsonDebugLog
    {
        private const string LogPath = "/Users/catchman/prj/bidcube/unity/test/.cursor/debug-c210ce.log";
        private const string SessionId = "c210ce";

        public static void Write(string location, string message, string hypothesisId, string dataJsonObject)
        {
#if UNITY_EDITOR
            try
            {
                var ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                var sb = new StringBuilder(256);
                sb.Append("{\"sessionId\":\"").Append(SessionId).Append("\",\"timestamp\":").Append(ts);
                sb.Append(",\"location\":\"").Append(EscapeJson(location)).Append('\"');
                sb.Append(",\"message\":\"").Append(EscapeJson(message)).Append('\"');
                sb.Append(",\"hypothesisId\":\"").Append(EscapeJson(hypothesisId)).Append('\"');
                sb.Append(",\"data\":").Append(string.IsNullOrEmpty(dataJsonObject) ? "{}" : dataJsonObject);
                sb.Append("}\n");
                File.AppendAllText(LogPath, sb.ToString());
            }
            catch
            {
                // ignore
            }
#endif
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        public static string EscapeForData(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
