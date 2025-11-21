namespace BidscubeSDK
{
    /// <summary>
    /// Common ad response structure for all ad types (Banner, Video, Native)
    /// </summary>
    [System.Serializable]
    public class AdResponse
    {
        /// <summary>
        /// Ad markup - can be HTML (Banner/Video) or native JSON string (Native)
        /// For Native ads, this may be nested: {"adm":{"adm":"{...native JSON...}"}}
        /// </summary>
        public string adm;

        /// <summary>
        /// Nested adm structure for Native ads (optional)
        /// </summary>
        public AdResponseInner admNested;

        /// <summary>
        /// Ad position (0 = Unknown, 1 = AboveTheFold, 2 = DependOnScreenSize, 3 = BelowTheFold, 4 = Header, 5 = Footer, 6 = Sidebar, 7 = FullScreen)
        /// </summary>
        public int position;

        /// <summary>
        /// Get the actual adm string, handling both flat and nested structures
        /// </summary>
        /// <returns>The adm string, or null if not found</returns>
        public string GetAdmString()
        {
            // Check nested structure first (for Native ads)
            if (admNested != null && !string.IsNullOrEmpty(admNested.adm))
            {
                return admNested.adm;
            }
            // Fall back to flat structure (for Banner/Video ads)
            return adm;
        }

        /// <summary>
        /// Get the position, checking nested structure first, then root
        /// </summary>
        /// <returns>The position value, or 0 (Unknown) if not found</returns>
        public int GetPosition()
        {
            // Check nested structure first (for Native ads)
            if (admNested != null && admNested.position != 0)
            {
                return admNested.position;
            }
            // Fall back to root position
            return position;
        }

        /// <summary>
        /// Check if adm is available (either flat or nested)
        /// </summary>
        /// <returns>True if adm is available</returns>
        public bool HasAdm()
        {
            return !string.IsNullOrEmpty(adm) || (admNested != null && !string.IsNullOrEmpty(admNested.adm));
        }
    }

    /// <summary>
    /// Nested adm structure for Native ads
    /// </summary>
    [System.Serializable]
    public class AdResponseInner
    {
        /// <summary>
        /// The actual native JSON string
        /// </summary>
        public string adm;

        /// <summary>
        /// Position in nested structure (optional)
        /// </summary>
        public int position;
    }
}

