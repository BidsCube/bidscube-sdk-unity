# Bidscube Unity SDK - Complete Documentation

## Overview

The Bidscube Unity SDK provides advertising solutions for Unity games, supporting image ads, video ads, native ads, and banner ads with GDPR/CCPA compliance.

### Features

- **Image Ads**: Static image advertisements
- **Video Ads**: Video advertisements with skip functionality
- **Native Ads**: Native-looking advertisements
- **Banner Ads**: Banner advertisements in various positions
- **Consent Management**: GDPR and CCPA compliance support
- **Cross-Platform**: Support for iOS, Android, and WebGL

## Installation

### Unity Package Manager (Recommended)

1. Open Unity Package Manager
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/Bidscube/bidscube-sdk-unity.git`
4. Click **Add**

### Alternative: manifest.json

```json
{
  "dependencies": {
    "com.bidscube.sdk": "https://github.com/Bidscube/bidscube-sdk-unity.git"
  }
}
```

## Quick Start

### 1. Initialize SDK

```csharp
using BidscubeSDK;

// Initialize with default configuration
BidscubeSDK.BidscubeSDK.Initialize();

// Or with custom configuration
var config = new SDKConfig.Builder()
    .EnableLogging(true)
    .EnableDebugMode(false)
    .DefaultAdTimeout(30000)
    .DefaultAdPosition(AdPosition.Header)
    .Build();

BidscubeSDK.BidscubeSDK.Initialize(config);
```

### 2. Basic Ad Manager

```csharp
public class AdManager : MonoBehaviour, IAdCallback
{
    public string placementId = "your_placement_id";

    void Start()
    {
        BidscubeSDK.BidscubeSDK.Initialize();
    }

    public void ShowImageAd()
    {
        BidscubeSDK.BidscubeSDK.ShowImageAd(placementId, this);
    }

    public void ShowVideoAd()
    {
        BidscubeSDK.BidscubeSDK.ShowVideoAd(placementId, this);
    }

    public void ShowBannerAd()
    {
        BidscubeSDK.BidscubeSDK.ShowHeaderBanner(placementId, this);
    }

    // Essential callbacks
    public void OnAdLoaded(string placementId)
    {
        Debug.Log("Ad loaded successfully");
    }

    public void OnAdFailed(string placementId, int errorCode, string errorMessage)
    {
        Debug.LogError($"Ad failed: {errorMessage}");
    }

    // Implement other required IAdCallback methods
    public void OnAdLoading(string placementId) { }
    public void OnAdDisplayed(string placementId) { }
    public void OnAdClicked(string placementId) { }
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

## API Reference

### Main Methods

- `Initialize()` - Initialize SDK
- `ShowImageAd(placementId, callback)` - Show image ad
- `ShowVideoAd(placementId, callback)` - Show video ad
- `ShowNativeAd(placementId, callback)` - Show native ad
- `ShowHeaderBanner(placementId, callback)` - Show header banner
- `ShowCustomBanner(placementId, position, width, height, callback)` - Show custom banner
- `RemoveAllBanners()` - Remove all banners

### Configuration

```csharp
var config = new SDKConfig.Builder()
    .EnableLogging(true)
    .EnableDebugMode(false)
    .DefaultAdTimeout(30000)
    .DefaultAdPosition(AdPosition.Header)
    .Build();
```

### Callbacks

**IAdCallback** - Required methods:

- `OnAdLoading(string placementId)`
- `OnAdLoaded(string placementId)`
- `OnAdDisplayed(string placementId)`
- `OnAdClicked(string placementId)`
- `OnAdFailed(string placementId, int errorCode, string errorMessage)`

**IConsentCallback** - Optional methods:

- `OnConsentInfoUpdated()`
- `OnConsentGranted()`
- `OnConsentDenied()`

### Error Codes

- `1001` - Invalid URL
- `1002` - Invalid response
- `1003` - Network error
- `1004` - Timeout error
- `1005` - Unknown error

## Consent Management

### Basic Implementation

```csharp
public class ConsentManager : MonoBehaviour, IConsentCallback
{
    public void RequestConsent()
    {
        BidscubeSDK.BidscubeSDK.RequestConsentInfoUpdate(this);
    }

    public void OnConsentInfoUpdated()
    {
        if (BidscubeSDK.BidscubeSDK.IsConsentRequired())
        {
            BidscubeSDK.BidscubeSDK.ShowConsentForm(this);
        }
    }

    public void OnConsentGranted()
    {
        Debug.Log("Consent granted - ads can be shown");
    }

    public void OnConsentDenied()
    {
        Debug.Log("Consent denied - ads should not be shown");
    }

    // Implement other IConsentCallback methods as needed
    public void OnConsentFormShown() { }
    public void OnConsentStatusChanged(bool hasConsent) { }
}
```

## Banner Ad Management

### Basic Implementation

```csharp
public class BannerManager : MonoBehaviour
{
    public string bannerPlacementId = "banner_placement_id";

    public void ShowBanner()
    {
        BidscubeSDK.BidscubeSDK.ShowHeaderBanner(bannerPlacementId, this);
    }

    public void ShowCustomBanner()
    {
        BidscubeSDK.BidscubeSDK.ShowCustomBanner(bannerPlacementId, AdPosition.Header, 320, 50, this);
    }

    public void RemoveAllBanners()
    {
        BidscubeSDK.BidscubeSDK.RemoveAllBanners();
    }
}
```

## Troubleshooting

### Common Issues

1. **SDK Not Initialized**: Always check `BidscubeSDK.BidscubeSDK.IsInitialized()` before use
2. **Ads Not Loading**: Check network connectivity, placement IDs, and consent status
3. **Video Ads Not Playing**: Verify IMA SDK integration and VAST response format
4. **Build Errors**: Ensure proper deployment targets and framework linking

### Debug Mode

```csharp
var config = new SDKConfig.Builder()
    .EnableDebugMode(true)
    .EnableLogging(true)
    .Build();
```

## Best Practices

1. **Initialize Early**: Initialize SDK as early as possible in app lifecycle
2. **Error Handling**: Always implement proper error handling and fallback content
3. **Consent Management**: Check consent status before showing ads
4. **Resource Cleanup**: Clean up ad resources in `OnDestroy()`
5. **Testing**: Use test placement IDs during development

## Requirements

- Unity 2020.3 or later
- .NET Standard 2.0 or later
- iOS 12.0 or later (for iOS builds)
- Android API Level 21 or later (for Android builds)

## Support

- Email: support@bidscube.com
- Documentation: https://docs.bidscube.com
- GitHub Issues: https://github.com/Bidscube/bidscube-sdk-unity/issues

