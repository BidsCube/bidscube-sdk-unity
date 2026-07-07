using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using BidscubeSDK;

namespace BidscubeSDK.OpenRTB
{
    internal static class VastAdSequenceParser
    {
        static readonly Regex VastRootRegex = new Regex(@"<VAST\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex AdSequenceAttrRegex = new Regex(@"<Ad\b[^>]*\bsequence\s*=\s*[""']?(\d+)[""']?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex DurationRegex = new Regex(@"<Duration>\s*(\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?\s*</Duration>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static bool ContentLikelyContainsVast(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;
            return VastRootRegex.IsMatch(content);
        }

        internal static int? FirstAdSequence(string vastXml)
        {
            if (string.IsNullOrWhiteSpace(vastXml))
                return null;
            var m = AdSequenceAttrRegex.Match(vastXml);
            if (!m.Success)
                return null;
            if (int.TryParse(m.Groups[1].Value, out int seq))
                return seq;
            return null;
        }

        internal static int? FirstLinearDurationSeconds(string vastXml)
        {
            if (string.IsNullOrWhiteSpace(vastXml))
                return null;
            var m = DurationRegex.Match(vastXml);
            if (!m.Success)
                return null;

            if (!int.TryParse(m.Groups[1].Value, out int hours))
                return null;
            if (!int.TryParse(m.Groups[2].Value, out int minutes))
                return null;
            if (!int.TryParse(m.Groups[3].Value, out int seconds))
                return null;

            return hours * 3600 + minutes * 60 + seconds;
        }

        internal static List<string> ExtractAdNodes(string vastXml)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(vastXml))
                return result;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(vastXml.Trim());
                var adNodes = doc.GetElementsByTagName("Ad");
                if (adNodes == null || adNodes.Count == 0)
                {
                    adNodes = doc.SelectNodes("//*[local-name()='Ad']") as XmlNodeList;
                }

                if (adNodes == null)
                    return result;

                foreach (XmlNode node in adNodes)
                {
                    if (node != null && !string.IsNullOrWhiteSpace(node.OuterXml))
                        result.Add(node.OuterXml);
                }
            }
            catch (Exception e)
            {
                Logger.Info($"[VastAdSequenceParser] ExtractAdNodes failed: {e.Message}");
            }

            return result;
        }

        internal static List<string> ExtractAdDocuments(string vastXml)
        {
            var documents = new List<string>();
            if (string.IsNullOrWhiteSpace(vastXml))
                return documents;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(vastXml.Trim());
                var root = doc.DocumentElement;
                if (root == null)
                    return documents;

                string version = root.GetAttribute("version");
                if (string.IsNullOrEmpty(version))
                    version = "3.0";

                var adNodes = root.SelectNodes(".//*[local-name()='Ad']");
                if (adNodes == null || adNodes.Count == 0)
                {
                    if (string.Equals(root.LocalName, "Ad", StringComparison.OrdinalIgnoreCase))
                    {
                        documents.Add(WrapAdInVast(root.OuterXml, version));
                    }
                    else if (documents.Count == 0 && ContentLikelyContainsVast(vastXml))
                    {
                        documents.Add(vastXml.Trim());
                    }
                    return documents;
                }

                if (adNodes.Count == 1)
                {
                    documents.Add(vastXml.Trim());
                    return documents;
                }

                foreach (XmlNode adNode in adNodes)
                {
                    if (adNode == null)
                        continue;
                    documents.Add(WrapAdInVast(adNode.OuterXml, version));
                }
            }
            catch (Exception e)
            {
                Logger.Info($"[VastAdSequenceParser] ExtractAdDocuments failed: {e.Message}");
                if (ContentLikelyContainsVast(vastXml))
                    documents.Add(vastXml.Trim());
            }

            return documents;
        }

        static string WrapAdInVast(string adXml, string version)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<VAST version=\"").Append(version).Append("\">");
            sb.Append(adXml);
            sb.Append("</VAST>");
            return sb.ToString();
        }
    }
}
