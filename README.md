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
