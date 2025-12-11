using UnityEngine;

namespace BidscubeSDK
{
    /// <summary>
    /// ScriptableObject that holds default ad sizes per AdType.
    /// Users can create an asset via Assets -> Create -> BidscubeSDK -> Ad Size Settings
    /// and configure defaults for Image (banner), Native and Video.
    /// Video size of (0,0) means full-screen.
    /// </summary>
    [CreateAssetMenu(fileName = "AdSizeSettings", menuName = "BidscubeSDK/Ad Size Settings")]
    public class AdSizeSettings : ScriptableObject
    {
        [Tooltip("Default banner size. Width is typically overridden to screen width for Header/Footer but used for Sidebar/centered placements.")]
        public Vector2 defaultBannerSize = new Vector2(1080f, 150f);

        [Tooltip("Default native ad size.")]
        public Vector2 defaultNativeSize = new Vector2(1080f, 400f);

        [Tooltip("Default video size. Set to (0,0) to indicate full-screen.")]
        public Vector2 defaultVideoSize = Vector2.zero;

        [Tooltip("If true, the SDK will prefer the configured defaults in this asset over sizes provided by the ad's adm/html.")]
        public bool preferDefaultsOverAdm = false;

        /// <summary>
        /// Returns default size for given ad type.
        /// </summary>
        public Vector2 GetDefaultSize(AdType adType)
        {
            switch (adType)
            {
                case AdType.Image:
                    return defaultBannerSize;
                case AdType.Native:
                    return defaultNativeSize;
                case AdType.Video:
                    return defaultVideoSize;
                default:
                    return Vector2.zero;
            }
        }
    }
}
