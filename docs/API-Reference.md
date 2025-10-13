# Bidscube Unity SDK

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Integration Guide](#integration-guide)
- [Code Examples](#code-examples)
- [Consent Management](#consent-management)
- [Banner Ad Management](#banner-ad-management)
- [Error Handling](#error-handling)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)
- [Requirements](#requirements)
- [Support](#support)

## Overview

The Bidscube Unity SDK provides a comprehensive advertising solution for Unity games, supporting image ads, video ads, native ads, and banner ads with GDPR/CCPA compliance.

### Features

- **Image Ads**: Display static image advertisements
- **Video Ads**: Show video advertisements with skip functionality
- **Native Ads**: Integrate native-looking advertisements
- **Banner Ads**: Display banner advertisements in various positions
- **Consent Management**: GDPR and CCPA compliance support
- **Device Information**: Automatic device and app information collection
- **Network Management**: Robust HTTP request handling
- **Cross-Platform**: Support for iOS, Android, and WebGL

## Installation

### Unity Package Manager (Recommended)

The Bidscube Unity SDK can be installed directly through Unity's Package Manager using a Git URL.

#### Step 1: Open Unity Package Manager

1. Open your Unity project
2. Go to **Window** → **Package Manager**
3. In the Package Manager window, click the **+** button in the top-left corner
4. Select **Add package from git URL...**

#### Step 2: Add Package URL

1. In the URL field, enter: `https://github.com/Bidscube/bidscube-sdk-unity.git`
2. Click **Add**
3. Unity will download and install the package

#### Step 3: Verify Installation

1. In the Package Manager, switch to **In Project** view
2. Look for **Bidscube SDK** in the list
3. The package should show as installed with the latest version

#### Alternative: Using manifest.json

You can also add the package directly to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.bidscube.sdk": "https://github.com/Bidscube/bidscube-sdk-unity.git"
  }
}
```

### Manual Installation

If you prefer manual installation:

1. Download the latest release from GitHub
2. Extract the package to your Unity project's `Packages` folder
3. In Unity, go to **Assets** → **Import Package** → **Custom Package**
4. Select the extracted package file

### Post-Installation Setup

After installation, you may need to:

1. **Configure Build Settings**: Ensure your target platform is set correctly
2. **Import Required Assets**: Some assets may need to be imported manually
3. **Set Up Scenes**: Use the provided test scenes to verify installation

### Dependencies

The SDK requires the following Unity packages:

- **Unity WebGL Build Support** (for WebGL builds)
- **Unity iOS Build Support** (for iOS builds)
- **Unity Android Build Support** (for Android builds)

These are usually installed automatically with Unity, but you can verify in **Window** → **Package Manager** → **Unity Registry**.

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
        Debug.Log($"Ad loading: {placementId}");
    }

    public void OnAdLoaded(string placementId)
    {
        Debug.Log($"Ad loaded: {placementId}");
    }

    public void OnAdDisplayed(string placementId)
    {
        Debug.Log($"Ad displayed: {placementId}");
    }

    public void OnAdClicked(string placementId)
    {
        Debug.Log($"Ad clicked: {placementId}");
    }

    public void OnAdFailed(string placementId, int errorCode, string errorMessage)
    {
        Debug.Log($"Ad failed: {placementId}, Error: {errorMessage}");
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

### Main Methods

The main SDK class providing static methods for ad management.

#### Methods

##### Initialize

```csharp
public static void Initialize(SDKConfig config)
public static void Initialize()
```

Initializes the SDK with configuration or default settings.

**Parameters:**

- `config` (SDKConfig): SDK configuration object

**Example:**

```csharp
// Initialize with default configuration
BidscubeSDK.BidscubeSDK.Initialize();

// Initialize with custom configuration
var config = new SDKConfig.Builder()
    .EnableLogging(true)
    .EnableDebugMode(false)
    .DefaultAdTimeout(30000)
    .DefaultAdPosition(AdPosition.Header)
    .Build();

BidscubeSDK.BidscubeSDK.Initialize(config);
```

##### IsInitialized

```csharp
public static bool IsInitialized()
```

Checks if the SDK is initialized.

**Returns:**

- `bool`: True if SDK is initialized, false otherwise

##### Cleanup

```csharp
public static void Cleanup()
```

Cleans up SDK resources and removes all active banners.

#### Ad Display Methods

##### ShowImageAd

```csharp
public static void ShowImageAd(string placementId, IAdCallback callback)
```

Displays an image advertisement.

**Parameters:**

- `placementId` (string): Unique placement identifier
- `callback` (IAdCallback): Callback interface for ad events

##### ShowVideoAd

```csharp
public static void ShowVideoAd(string placementId, IAdCallback callback)
```

Displays a video advertisement.

**Parameters:**

- `placementId` (string): Unique placement identifier
- `callback` (IAdCallback): Callback interface for ad events

##### ShowSkippableVideoAd

```csharp
public static void ShowSkippableVideoAd(string placementId, string skipButtonText, IAdCallback callback)
```

Displays a skippable video advertisement.

**Parameters:**

- `placementId` (string): Unique placement identifier
- `skipButtonText` (string): Text for the skip button
- `callback` (IAdCallback): Callback interface for ad events

##### ShowNativeAd

```csharp
public static void ShowNativeAd(string placementId, IAdCallback callback)
```

Displays a native advertisement.

**Parameters:**

- `placementId` (string): Unique placement identifier
- `callback` (IAdCallback): Callback interface for ad events

#### Banner Ad Methods

##### ShowHeaderBanner

```csharp
public static void ShowHeaderBanner(string placementId, IAdCallback callback)
```

Displays a header banner advertisement.

##### ShowFooterBanner

```csharp
public static void ShowFooterBanner(string placementId, IAdCallback callback)
```

Displays a footer banner advertisement.

##### ShowCustomBanner

```csharp
public static void ShowCustomBanner(string placementId, AdPosition position, int width, int height, IAdCallback callback)
```

Displays a custom-sized banner advertisement.

**Parameters:**

- `placementId` (string): Unique placement identifier
- `position` (AdPosition): Ad position
- `width` (int): Banner width in pixels
- `height` (int): Banner height in pixels
- `callback` (IAdCallback): Callback interface for ad events

##### RemoveAllBanners

```csharp
public static void RemoveAllBanners()
```

Removes all active banner advertisements.

#### Ad View Methods

##### GetImageAdView

```csharp
public static GameObject GetImageAdView(string placementId, IAdCallback callback)
```

Gets an image ad view for embedding in UI.

**Returns:**

- `GameObject`: Image ad view GameObject

##### GetVideoAdView

```csharp
public static GameObject GetVideoAdView(string placementId, IAdCallback callback)
```

Gets a video ad view for embedding in UI.

**Returns:**

- `GameObject`: Video ad view GameObject

##### GetNativeAdView

```csharp
public static GameObject GetNativeAdView(string placementId, IAdCallback callback)
```

Gets a native ad view for embedding in UI.

**Returns:**

- `GameObject`: Native ad view GameObject

#### Consent Management Methods

##### RequestConsentInfoUpdate

```csharp
public static void RequestConsentInfoUpdate(IConsentCallback callback)
```

Requests consent information update.

**Parameters:**

- `callback` (IConsentCallback): Consent callback interface

##### ShowConsentForm

```csharp
public static void ShowConsentForm(IConsentCallback callback)
```

Shows the consent form.

**Parameters:**

- `callback` (IConsentCallback): Consent callback interface

##### IsConsentRequired

```csharp
public static bool IsConsentRequired()
```

Checks if consent is required.

**Returns:**

- `bool`: True if consent is required

##### HasAdsConsent

```csharp
public static bool HasAdsConsent()
```

Checks if user has given consent for ads.

**Returns:**

- `bool`: True if user has given ads consent

##### HasAnalyticsConsent

```csharp
public static bool HasAnalyticsConsent()
```

Checks if user has given consent for analytics.

**Returns:**

- `bool`: True if user has given analytics consent

##### GetConsentStatusSummary

```csharp
public static string GetConsentStatusSummary()
```

Gets a summary of consent status.

**Returns:**

- `string`: Consent status summary

#### Ad Position Methods

##### SetAdPosition

```csharp
public static void SetAdPosition(AdPosition position)
```

Sets the manual ad position.

**Parameters:**

- `position` (AdPosition): Ad position to set

##### GetEffectiveAdPosition

```csharp
public static AdPosition GetEffectiveAdPosition()
```

Gets the effective ad position (manual override or response-based).

**Returns:**

- `AdPosition`: Effective ad position

## Configuration

### SDKConfig

Configuration class for SDK settings.

#### Properties

- `EnableLogging` (bool): Enable/disable logging
- `EnableDebugMode` (bool): Enable/disable debug mode
- `DefaultAdTimeoutMs` (int): Default ad timeout in milliseconds
- `DefaultAdPosition` (AdPosition): Default ad position

#### Builder Methods

##### EnableLogging

```csharp
public Builder EnableLogging(bool value)
```

Enables or disables logging.

**Parameters:**

- `value` (bool): True to enable logging

**Returns:**

- `Builder`: Builder instance for method chaining

##### EnableDebugMode

```csharp
public Builder EnableDebugMode(bool value)
```

Enables or disables debug mode.

**Parameters:**

- `value` (bool): True to enable debug mode

**Returns:**

- `Builder`: Builder instance for method chaining

##### DefaultAdTimeout

```csharp
public Builder DefaultAdTimeout(int millis)
```

Sets the default ad timeout.

**Parameters:**

- `millis` (int): Timeout in milliseconds

**Returns:**

- `Builder`: Builder instance for method chaining

##### DefaultAdPosition

```csharp
public Builder DefaultAdPosition(AdPosition position)
```

Sets the default ad position.

**Parameters:**

- `position` (AdPosition): Default ad position

**Returns:**

- `Builder`: Builder instance for method chaining

Sets the base URL for ad requests.

**Parameters:**

- `url` (string): Base URL

**Returns:**

- `Builder`: Builder instance for method chaining

##### Build

```csharp
public SDKConfig Build()
```

Builds the SDK configuration.

**Returns:**

- `SDKConfig`: Configured SDK configuration object

## Ad Types

### AdType

Enumeration of supported ad types.

```csharp
public enum AdType
{
    Image,      // Static image advertisements
    Video,      // Video advertisements
    Native      // Native-looking advertisements
}
```

## Ad Positions

### AdPosition

Enumeration of supported ad positions.

```csharp
public enum AdPosition
{
    Unknown,           // Unknown position
    AboveTheFold,      // Above the fold
    BelowTheFold,      // Below the fold
    Header,           // Header position
    Footer,           // Footer position
    Sidebar,          // Sidebar position
    FullScreen        // Full screen
}
```

## Callbacks

### IAdCallback

Interface for ad event callbacks.

#### Methods

##### OnAdLoading

```csharp
void OnAdLoading(string placementId)
```

Called when ad starts loading.

**Parameters:**

- `placementId` (string): Placement identifier

##### OnAdLoaded

```csharp
void OnAdLoaded(string placementId)
```

Called when ad is loaded.

**Parameters:**

- `placementId` (string): Placement identifier

##### OnAdDisplayed

```csharp
void OnAdDisplayed(string placementId)
```

Called when ad is displayed.

**Parameters:**

- `placementId` (string): Placement identifier

##### OnAdClicked

```csharp
void OnAdClicked(string placementId)
```

Called when ad is clicked.

**Parameters:**

- `placementId` (string): Placement identifier

##### OnAdFailed

```csharp
void OnAdFailed(string placementId, int errorCode, string errorMessage)
```

Called when ad fails.

**Parameters:**

- `placementId` (string): Placement identifier
- `errorCode` (int): Error code
- `errorMessage` (string): Error message

##### OnVideoAdStarted

```csharp
void OnVideoAdStarted(string placementId)
```

Called when video ad starts.

**Parameters:**

- `placementId` (string): Placement identifier

##### OnVideoAdCompleted

```csharp
void OnVideoAdCompleted(string placementId)
```

Called when video ad completes.

**Parameters:**

- `placementId` (string): Placement identifier

##### OnVideoAdSkipped

```csharp
void OnVideoAdSkipped(string placementId)
```

Called when video ad is skipped.

**Parameters:**

- `placementId` (string): Placement identifier

##### OnInstallButtonClicked

```csharp
void OnInstallButtonClicked(string placementId, string buttonText)
```

Called when install button is clicked.

**Parameters:**

- `placementId` (string): Placement identifier
- `buttonText` (string): Button text

### IConsentCallback

Interface for consent management callbacks.

#### Methods

##### OnConsentInfoUpdated

```csharp
void OnConsentInfoUpdated()
```

Called when consent info is updated.

##### OnConsentFormShown

```csharp
void OnConsentFormShown()
```

Called when consent form is shown.

##### OnConsentGranted

```csharp
void OnConsentGranted()
```

Called when consent is granted.

##### OnConsentDenied

```csharp
void OnConsentDenied()
```

Called when consent is denied.

##### OnConsentStatusChanged

```csharp
void OnConsentStatusChanged(bool hasConsent)
```

Called when consent status changes.

**Parameters:**

- `hasConsent` (bool): True if user has given consent

## Error Handling

### Error Codes

The SDK provides specific error codes for different failure scenarios.

#### Constants.ErrorCodes

```csharp
public static class ErrorCodes
{
    public const int InvalidURL = 1001;      // Invalid URL
    public const int InvalidResponse = 1002;  // Invalid response
    public const int NetworkError = 1003;     // Network error
    public const int TimeoutError = 1004;    // Request timeout
    public const int UnknownError = 1005;     // Unknown error
}
```

#### Error Messages

```csharp
public static class ErrorMessages
{
    public const string FailedToBuildURL = "Failed to build request URL";
    public const string InvalidResponse = "Invalid response from server";
    public const string NetworkError = "Network error occurred";
    public const string TimeoutError = "Request timeout";
    public const string UnknownError = "Unknown error occurred";
}
```

### Error Handling Example

```csharp
public void OnAdFailed(string placementId, int errorCode, string errorMessage)
{
    switch (errorCode)
    {
        case Constants.ErrorCodes.InvalidURL:
            Debug.LogError("Invalid URL: " + errorMessage);
            break;
        case Constants.ErrorCodes.NetworkError:
            Debug.LogError("Network error: " + errorMessage);
            break;
        case Constants.ErrorCodes.TimeoutError:
            Debug.LogError("Request timeout: " + errorMessage);
            break;
        default:
            Debug.LogError($"Unknown error ({errorCode}): {errorMessage}");
            break;
    }
}
```

## Constants

### SDK Constants

```csharp
public static class Constants
{
    public const int DefaultTimeoutMs = 30000;                    // Default timeout
    public const AdPosition DefaultAdPosition = AdPosition.Unknown; // Default position
    public const string UserAgentPrefix = "BidscubeSDK";          // User agent prefix
    public const string SdkVersion = "1.0.0";                    // SDK version
}
```

## Integration Guide

### Basic Integration

1. **Initialize SDK Early**: Initialize the SDK as early as possible in your app lifecycle, preferably in a singleton or main game manager.

```csharp
public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Initialize SDK first
        BidscubeSDK.BidscubeSDK.Initialize();

        // Your other initialization code
    }
}
```

2. **Implement Callbacks**: Create a dedicated ad manager class to handle all ad-related callbacks.

```csharp
public class AdManager : MonoBehaviour, IAdCallback
{
    public static AdManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Implement all IAdCallback methods
}
```

3. **Handle Ad Lifecycle**: Always implement proper ad lifecycle management.

```csharp
public void ShowAdWithLifecycle(string placementId)
{
    if (!BidscubeSDK.BidscubeSDK.IsInitialized())
    {
        Debug.LogError("SDK not initialized!");
        return;
    }

    // Show loading indicator
    ShowLoadingIndicator();

    // Request ad
    BidscubeSDK.BidscubeSDK.ShowImageAd(placementId, this);
}

public void OnAdLoaded(string placementId)
{
    HideLoadingIndicator();
    Debug.Log($"Ad loaded successfully: {placementId}");
}

public void OnAdFailed(string placementId, int errorCode, string errorMessage)
{
    HideLoadingIndicator();
    Debug.LogError($"Ad failed: {errorMessage}");
    // Handle failure (retry, show fallback, etc.)
}
```

## Code Examples

### Basic Ad Manager

```csharp
public class AdManager : MonoBehaviour, IAdCallback
{
    public string placementId = "your_placement_id";

    void Start()
    {
        BidscubeSDK.BidscubeSDK.Initialize();
    }

    public void ShowAd()
    {
        BidscubeSDK.BidscubeSDK.ShowImageAd(placementId, this);
    }

    // Essential callbacks only
    public void OnAdLoaded(string placementId)
    {
        Debug.Log("Ad loaded successfully");
    }

    public void OnAdFailed(string placementId, int errorCode, string errorMessage)
    {
        Debug.LogError($"Ad failed: {errorMessage}");
    }

    // Implement other required IAdCallback methods as needed
    public void OnAdLoading(string placementId) { }
    public void OnAdDisplayed(string placementId) { }
    public void OnAdClicked(string placementId) { }
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

## Consent Management

### Basic Consent Implementation

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

### Basic Banner Implementation

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

For support and questions:

- Email: support@bidscube.com
