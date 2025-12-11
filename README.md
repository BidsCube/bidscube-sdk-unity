# Bidscube Unity SDK

The Bidscube Unity SDK provides a comprehensive advertising solution for Unity games, supporting image ads, video ads, native ads, and banner ads.

## Features

- **Image Ads**: Display static image advertisements
- **Video Ads**: Show video advertisements with skip functionality
- **Native Ads**: Integrate native-looking advertisements
- **Banner Ads**: Display banner advertisements in various positions
- **Consent Management**: GDPR and CCPA compliance support
- **Device Information**: Automatic device and app information collection
- **Network Management**: Robust HTTP request handling

## Installation

### Unity Package Manager

1. Open Unity Package Manager
2. Click the "+" button and select "Add package from git URL"
3. Enter the package URL: `https://github.com/Bidscube/bidscube-sdk-unity.git`

**Note:** The SDK will automatically install required dependencies (TextMeshPro, UGUI) when imported. No manual setup is required.

### Manual Installation

1. Download the latest release
2. Extract the package to your Unity project's `Packages` folder
3. Import the package in Unity

## Quick Start

### 1. Initialize the SDK

```csharp
using BidscubeSDK;

// Initialize with default configuration
BidscubeSDK.BidscubeSDK.Initialize();

// Or initialize with custom configuration
var config = new SDKConfig.Builder()
    .EnableLogging(true)
    .EnableDebugMode(false)
    .DefaultAdTimeout(30000)
    .DefaultAdPosition(AdPosition.Unknown)
    .BaseURL(Constants.)
    .Build();

BidscubeSDK.BidscubeSDK.Initialize(config);
```

### 2. Show Image Ad

```csharp
public class AdManager : MonoBehaviour, IAdCallback
{
    public void ShowImageAd()
    {
        BidscubeSDK.BidscubeSDK.ShowImageAd("your_placement_id", this);
    }

    public void OnAdLoading(string placementId)
    {
        Logger.Info($"Ad loading: {placementId}");
    }

    public void OnAdLoaded(string placementId)
    {
        Logger.Info($"Ad loaded: {placementId}");
    }

    public void OnAdDisplayed(string placementId)
    {
        Logger.Info($"Ad displayed: {placementId}");
    }

    public void OnAdClicked(string placementId)
    {
        Logger.Info($"Ad clicked: {placementId}");
    }

    public void OnAdFailed(string placementId, int errorCode, string errorMessage)
    {
        Logger.Info($"Ad failed: {placementId}, Error: {errorMessage}");
    }

    // Implement other IAdCallback methods as needed
}
```

### 3. Show Video Ad

```csharp
public void ShowVideoAd()
{
    BidscubeSDK.BidscubeSDK.ShowVideoAd("your_placement_id", this);
}
```

### 4. Show Banner Ad

```csharp
public void ShowBannerAd()
{
    // Show header banner
    BidscubeSDK.BidscubeSDK.ShowHeaderBanner("your_placement_id", this);

    // Or show custom banner
    BidscubeSDK.BidscubeSDK.ShowCustomBanner("your_placement_id", AdPosition.Header, 320, 50, this);
}
```

### 5. Show Native Ad

```csharp
public void ShowNativeAd()
{
    BidscubeSDK.BidscubeSDK.ShowNativeAd("your_placement_id", this);
}
```

## API Reference

### Core Classes

#### BidscubeSDK

Main SDK class providing static methods for ad management.

**Methods:**

- `Initialize(SDKConfig config)` - Initialize SDK with configuration
- `Initialize()` - Initialize SDK with default configuration
- `IsInitialized()` - Check if SDK is initialized
- `Cleanup()` - Cleanup SDK resources

#### SDKConfig

Configuration class for SDK settings.

**Builder Methods:**

- `EnableLogging(bool value)` - Enable/disable logging
- `EnableDebugMode(bool value)` - Enable/disable debug mode
- `DefaultAdTimeout(int millis)` - Set default ad timeout
- `DefaultAdPosition(AdPosition position)` - Set default ad position
- `BaseURL(string url)` - Set base URL for ad requests

### Ad Types

#### AdType

- `Image` - Static image advertisements
- `Video` - Video advertisements
- `Native` - Native-looking advertisements

#### AdPosition

- `Unknown` - Unknown position
- `AboveTheFold` - Above the fold
- `BelowTheFold` - Below the fold
- `Header` - Header position
- `Footer` - Footer position
- `Sidebar` - Sidebar position
- `FullScreen` - Full screen

### Callbacks

#### IAdCallback

Interface for ad event callbacks.

**Methods:**

- `OnAdLoading(string placementId)` - Called when ad starts loading
- `OnAdLoaded(string placementId)` - Called when ad is loaded
- `OnAdDisplayed(string placementId)` - Called when ad is displayed
- `OnAdClicked(string placementId)` - Called when ad is clicked
- `OnAdFailed(string placementId, int errorCode, string errorMessage)` - Called when ad fails
- `OnVideoAdStarted(string placementId)` - Called when video ad starts
- `OnVideoAdCompleted(string placementId)` - Called when video ad completes
- `OnVideoAdSkipped(string placementId)` - Called when video ad is skipped
- `OnInstallButtonClicked(string placementId, string buttonText)` - Called when install button is clicked

#### IConsentCallback

Interface for consent management callbacks.

**Methods:**

- `OnConsentInfoUpdated()` - Called when consent info is updated
- `OnConsentFormShown()` - Called when consent form is shown
- `OnConsentGranted()` - Called when consent is granted
- `OnConsentDenied()` - Called when consent is denied
- `OnConsentStatusChanged(bool hasConsent)` - Called when consent status changes

## Consent Management

The SDK provides built-in consent management for GDPR and CCPA compliance.

```csharp
public class ConsentManager : MonoBehaviour, IConsentCallback
{
    public void RequestConsent()
    {
        BidscubeSDK.BidscubeSDK.RequestConsentInfoUpdate(this);
    }

    public void ShowConsentForm()
    {
        BidscubeSDK.BidscubeSDK.ShowConsentForm(this);
    }

    public void OnConsentInfoUpdated()
    {
        Logger.Info("Consent info updated");
    }

    public void OnConsentGranted()
    {
        Logger.Info("Consent granted");
    }

    public void OnConsentDenied()
    {
        Logger.Info("Consent denied");
    }

    // Implement other IConsentCallback methods as needed
}
```

## Banner Ad Management

The SDK provides comprehensive banner ad management.

```csharp
public class BannerManager : MonoBehaviour
{
    public void ShowHeaderBanner()
    {
        BidscubeSDK.BidscubeSDK.ShowHeaderBanner("placement_id", this);
    }

    public void ShowFooterBanner()
    {
        BidscubeSDK.BidscubeSDK.ShowFooterBanner("placement_id", this);
    }

    public void ShowCustomBanner()
    {
        BidscubeSDK.BidscubeSDK.ShowCustomBanner("placement_id", AdPosition.Header, 320, 50, this);
    }

    public void RemoveAllBanners()
    {
        BidscubeSDK.BidscubeSDK.RemoveAllBanners();
    }
}
```

## Error Handling

The SDK provides comprehensive error handling with specific error codes.

```csharp
public void OnAdFailed(string placementId, int errorCode, string errorMessage)
{
    switch (errorCode)
    {
        case Constants.ErrorCodes.InvalidURL:
            Logger.InfoError("Invalid URL");
            break;
        case Constants.ErrorCodes.NetworkError:
            Logger.InfoError("Network error");
            break;
        case Constants.ErrorCodes.TimeoutError:
            Logger.InfoError("Request timeout");
            break;
        default:
            Logger.InfoError($"Unknown error: {errorMessage}");
            break;
    }
}
```


## Optional callback: IAdRenderOverride (new)

The SDK supports an optional callback interface that lets your app fully override SDK rendering for a specific ad response. If your callback implements `IAdRenderOverride` and you pass that callback object when requesting an ad (for example, via `BidscubeSDK.ShowNativeAd(placementId, callback)` or `GetBannerAdView(placementId, position, callback)`), the SDK will call the override before it attempts its own rendering.

Interface signature

```csharp
public interface IAdRenderOverride
{
    /// <summary>
    /// Called by the SDK before default rendering. Return true if you handled rendering
    /// (SDK will skip default rendering). Return false to let the SDK render normally.
    /// </summary>
    /// <param name="placementId">Placement id provided to SDK</param>
    /// <param name="adm">Raw adm payload (may be OpenRTB JSON, HTML fragment, or full HTML document)</param>
    /// <param name="adType">Ad type (Image, Native, Video)</param>
    /// <param name="position">Numeric ad position (AdPosition enum value)</param>
    /// <returns>true if override handled rendering and SDK should not render; false to let SDK render</returns>
    bool OnAdRenderOverride(string placementId, string adm, AdType adType, int position);
}
```

When it's called

- The SDK will call this method when it detects that a callback passed to the ad request implements `IAdRenderOverride`.
- The `adm` argument contains the raw ad markup or JSON from the server. It may be:
    - A full HTML document (starts with `<!DOCTYPE` or `<html>`), or
    - An HTML fragment (starts with `<`), or
    - A JSON-ad payload (OpenRTB native JSON, or a custom JSON structure).
- For HTML payloads you can load them directly into your own webview instance. For JSON you can parse and render with Unity UI.

Return semantics

- Return `true` to indicate you handled rendering: the SDK will stop and will NOT perform its built-in rendering for that ad (no webview or default UI will be created).
- Return `false` to indicate you did not handle rendering: the SDK will continue with its default rendering flow.

Example usage (simple HTML handler)

```csharp
public class CustomAdRenderController : MonoBehaviour, IAdCallback, IAdRenderOverride
{
    public NewWebViewController webViewPrefab; // reuse existing controller in project

    public bool OnAdRenderOverride(string placementId, string adm, AdType adType, int position)
    {
        if (string.IsNullOrEmpty(adm)) return false;

        var trimmed = adm.TrimStart();
        if (trimmed.StartsWith("<"))
        {
            var go = new GameObject($"AdWebHost_{placementId}");
            var ctrl = go.AddComponent<NewWebViewController>();
            ctrl.HTMLad = adm; // use existing API
            ctrl.SetVisibility(true);
            return true;
        }
        return false;
    }

    // implement IAdCallback as needed
}
```

How to wire it

- When you request an ad, pass your controller object as the `IAdCallback`/override callback parameter. If it implements `IAdRenderOverride`, the SDK will call it automatically.

```csharp
var controller = FindObjectOfType<CustomAdRenderController>();
BidscubeSDK.BidscubeSDK.ShowNativeAd("placementId", controller);
```

---

## `AdSizeSettings` (editor ScriptableObject)

You can define project-wide default sizes for banners, native, and video ads using the `AdSizeSettings` ScriptableObject. This is useful to enforce consistent sizes across your UI and the SDK.

- Create an asset: `Assets -> Create -> BidscubeSDK -> Ad Size Settings`.
- Default fields (recommended defaults in this SDK version):
    - `defaultBannerSize`: Vector2 — default 1080x150 (Header/Footer banners typically use screen width, height used is 150px)
    - `defaultNativeSize`: Vector2 — default 1080x400
    - `defaultVideoSize`: Vector2 — default Vector2.zero (0,0) means full-screen

Apply AdSizeSettings

- Assign the `AdSizeSettings` object to an `AdViewController`/`NativeAdView` via inspector (if you have an exposed field). Or call `ApplyAdSizeSettings()` at runtime on the `NativeAdView` instance:

```csharp
// example: find the NativeAdView component and apply settings loaded from resources
var settings = Resources.Load<BidscubeSDK.AdSizeSettings>("AdSizeSettings");
var nativeView = FindObjectOfType<BidscubeSDK.NativeAdView>();
if (settings != null && nativeView != null)
{
    nativeView.ApplyAdSizeSettings(settings);
}
```

Notes

- If you apply `AdSizeSettings`, the view will prefer those values over sizes reported by the adm/response (unless the SDK code is configured to allow server override). You can toggle this behavior in the view by calling the view's API (see `NativeAdView.ApplyAdSizeSettings`).
- Header/Footer banners are commonly clamped to a smaller height (e.g., 50px) depending on `AdPosition`. The SDK will clamp heights for Header/Footer to a sane maximum when appropriate.


## Dependencies

The SDK requires the following Unity packages (automatically installed when using Unity Package Manager):

- **com.unity.ugui** (1.0.0) - Unity UI system
- **com.unity.textmeshpro** (3.0.6) - TextMeshPro for UI text rendering

These dependencies are declared in `package.json` and will be automatically resolved when the package is imported via Git URL.

## Requirements

- Unity 2020.3 or later
- .NET Standard 2.0 or later
- iOS 12.0 or later (for iOS builds)
- Android API Level 21 or later (for Android builds)

## Support

For support and questions, please contact:

- Email: support@bidscube.com
- Documentation: https://docs.bidscube.com
- GitHub Issues: https://github.com/Bidscube/bidscube-sdk-unity/issues

## License

This SDK is licensed under the MIT License. See LICENSE file for details.