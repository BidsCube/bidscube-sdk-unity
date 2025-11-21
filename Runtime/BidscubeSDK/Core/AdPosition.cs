namespace BidscubeSDK
{
    /// <summary>
    /// Ad position enumeration
    /// </summary>
    public enum AdPosition
    {
        Unknown = 0,
        AboveTheFold = 1,
        DependOnScreenSize = 2, // internal use
        BelowTheFold = 3,
        Header = 4,
        Footer = 5,
        Sidebar = 6,
        FullScreen = 7
    }
}
