# Bidscube Unity SDK

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Configuration](#configuration)
- [Callbacks](#callbacks)
- [Error Handling](#error-handling)
- [Integration Guide](#integration-guide)
- [Code Examples](#code-examples)
- [Consent Management](#consent-management)
- [Banner Ad Management](#banner-ad-management)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)
- [Requirements](#requirements)
- [Support](#support)

## Overview

The Bidscube Unity SDK provides a comprehensive advertising solution for Unity games, supporting
image ads, video ads, native ads, and banner ads with GDPR/CCPA compliance.

## Features

- Image Ads: Display static image advertisements
- Video Ads: Show video advertisements with skip functionality
- Native Ads: Integrate native-looking advertisements
- Banner Ads: Display banner advertisements in various positions
- Consent Management: GDPR and CCPA compliance support
- Device Information: Automatic device and app information collection
- Network Management: Robust HTTP request handling
- Cross-Platform: Support for iOS, Android, and WebGL

## Installation

### Unity Package Manager (Recommended)

The Bidscube Unity SDK can be installed directly through Unity's Package Manager using a Git URL.

**Step 1: Unity Asset Store (Easiest Method)**  
Install the Bidscube Unity SDK from the Unity Asset Store:  
<https://assetstore.unity.com/packages/slug/345904>

Steps:

1. Open the link
2. Click **Add to My Assets**
3. Click **Open in Unity**
4. In Unity go to **Window → Package Manager**
5. Switch the dropdown to **My Assets**
6. Search for **Bidscube SDK**
7. Click **Download**, then **Import**

**Step 2: Open Unity Package Manager**

1. Open your Unity project
2. Go to **Window → Package Manager**
3. In the Package Manager window, click the **+** button in the top-left corner
4. Select **Add package from git URL...**

**Step 3: Add Package URL**

In the URL field, enter the Git URL (clickable link with access):  
[Bidscube Unity SDK Git repository](https://github.com/BidsCube/bidscube-sdk-unity.git)

> Note: If you use a private-access URL with a token, do **not** paste the token directly in your project or documentation. Use your own credential helper instead.

Then click **Add**. Unity will download and install the package.

**Step 4: Verify Installation**

1. In the Package Manager, switch to **In Project** view
2. Look for **Bidscube SDK** in the list
3. The package should show as installed with the latest version

### Alternative: Using `manifest.json`

You can also add the package directly to your `Packages/manifest.json`.
Use this dependency entry with a masked, clickable Git link:

```jsonc
{
  "dependencies": {
    "com.bidscube.sdk": "https://github.com/BidsCube/bidscube-sdk-unity.git"
  }
}
```

### Manual Installation

If you prefer manual installation:

1. Download the latest release from GitHub:  
   <https://github.com/BidsCube/bidscube-sdk-unity/releases>
2. Extract the package to your Unity project's `Packages` folder
3. In Unity, go to **Assets  Import Package  Custom Package**
4. Select the extracted package file

### Post-Installation Setup

After installation, you may need to:

- **Configure Build Settings**: Ensure your target platform is set correctly
- **Import Required Assets**: Some assets may need to be imported manually
- **Set Up Scenes**: Use the provided test scenes to verify installation

### Dependencies

The SDK requires the following Unity modules/packages:

- **Unity WebGL Build Support** (for WebGL builds)
- **Unity iOS Build Support** (for iOS builds)
- **Unity Android Build Support** (for Android builds)

These are usually installed with Unity, but you can verify in **Window  Package Manager  Unity Registry**.

## Quick Start

### 1. Initialize the SDK

```csharp
using UnityEngine;
using BidscubeSDK;

public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        // Initialize with default configuration
        BidscubeSDK.Initialize();

        // Or initialize with custom configuration
        var config = new SDKConfig.Builder()
            .EnableLogging(true)
            .EnableDebugMode(false)
            .DefaultAdTimeout(30000)
            .DefaultAdPosition(AdPosition.Unknown)
            .Build();

        BidscubeSDK.Initialize(config);
    }
}
```

### 2. Show Image Ad (internally uses banner rendering)

```csharp
using UnityEngine;
using BidscubeSDK;

public class ImageAdExample : MonoBehaviour, IAdCallback
{
    public string placementId = "your_placement_id";

    public void ShowImageAd()
    {
        BidscubeSDK.ShowImageAd(placementId, this);
    }

    public void OnAdLoading(string placementId) { }
    public void OnAdLoaded(string placementId) { }
    public void OnAdDisplayed(string placementId) { }
    public void OnAdClicked(string placementId) { }
    public void OnAdClosed(string placementId) { }
    public void OnAdFailed(string placementId, int errorCode, string errorMessage) { }
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnVideoAdSkippable(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

### 3. Show Video Ad

```csharp
using UnityEngine;
using BidscubeSDK;

public class VideoAdExample : MonoBehaviour, IAdCallback
{
    public string placementId = "your_placement_id";

    public void ShowVideoAd()
    {
        BidscubeSDK.ShowVideoAd(placementId, this);
    }

    public void OnAdLoading(string placementId) { }
    public void OnAdLoaded(string placementId) { }
    public void OnAdDisplayed(string placementId) { }
    public void OnAdClicked(string placementId) { }
    public void OnAdClosed(string placementId) { }
    public void OnAdFailed(string placementId, int errorCode, string errorMessage) { }
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnVideoAdSkippable(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

### 4. Show Banner Ad

```csharp
using UnityEngine;
using BidscubeSDK;

public class BannerAdExample : MonoBehaviour, IAdCallback
{
    public string placementId = "your_placement_id";

    public void ShowHeaderBanner()
    {
        BidscubeSDK.ShowHeaderBanner(placementId, this);
    }

    public void ShowCustomBanner(AdPosition position, int width, int height)
    {
        BidscubeSDK.ShowCustomBanner(placementId, position, width, height, this);
    }

    public void OnAdLoading(string placementId) { }
    public void OnAdLoaded(string placementId) { }
    public void OnAdDisplayed(string placementId) { }
    public void OnAdClicked(string placementId) { }
    public void OnAdClosed(string placementId) { }
    public void OnAdFailed(string placementId, int errorCode, string errorMessage) { }
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnVideoAdSkippable(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

## API Reference

### Initialization

```csharp
// Initialize with default configuration
BidscubeSDK.Initialize();

// Initialize with custom configuration
var config = new SDKConfig.Builder()
    .EnableLogging(true)
    .EnableDebugMode(false)
    .DefaultAdTimeout(30000)
    .DefaultAdPosition(AdPosition.Header)
    .Build();

BidscubeSDK.Initialize(config);

// Check if initialized
bool isInitialized = BidscubeSDK.IsInitialized();

// Cleanup
BidscubeSDK.Cleanup();
```

### Ad Display Methods (signatures)

```csharp
// Image and Banner Ads
public static void ShowImageAd(string placementId, IAdCallback callback = null);
public static void ShowHeaderBanner(string placementId, IAdCallback callback);
public static void ShowFooterBanner(string placementId, IAdCallback callback);
public static void ShowSidebarBanner(string placementId, IAdCallback callback);
public static void ShowCustomBanner(string placementId, AdPosition position, int width, int height, IAdCallback callback);

// Video Ads
public static void ShowVideoAd(string placementId, IAdCallback callback = null);
public static void ShowSkippableVideoAd(string placementId, string skipButtonText, IAdCallback callback);

// Native Ads
public static void ShowNativeAd(string placementId, IAdCallback callback = null);

// Banner Management
public static void RemoveAllBanners();
```

### Ad View Methods (signatures)

```csharp
// Get ad view GameObjects for custom integration
public static GameObject GetImageAdView(string placementId, IAdCallback callback = null);
public static GameObject GetBannerAdView(string placementId, IAdCallback callback = null);
public static GameObject GetVideoAdView(string placementId, IAdCallback callback = null);
public static GameObject GetNativeAdView(string placementId, IAdCallback callback = null);
public static BannerAdView GetBannerAdView(string placementId, AdPosition position, IAdCallback callback = null);
```

## Configuration

### SDKConfig

```csharp
public class SDKConfig
{
    public bool EnableLogging { get; }
    public bool EnableDebugMode { get; }
    public int DefaultAdTimeoutMs { get; }
    public AdPosition DefaultAdPosition { get; }

    public class Builder
    {
        public Builder EnableLogging(bool value);
        public Builder EnableDebugMode(bool value);
        public Builder DefaultAdTimeout(int millis);
        public Builder DefaultAdPosition(AdPosition position);
        public SDKConfig Build();
    }
}
```

### AdType

```csharp
public enum AdType
{
    Image,   // Static image advertisements
    Video,   // Video advertisements
    Native   // Native-looking advertisements
}
```

### AdPosition

```csharp
public enum AdPosition
{
    Unknown = 0,
    AboveTheFold = 1,
    DependOnScreenSize = 2, // Internal use
    BelowTheFold = 3,
    Header = 4,
    Footer = 5,
    Sidebar = 6,
    FullScreen = 7
}
```

## Callbacks

### IAdCallback

```csharp
public interface IAdCallback
{
    void OnAdLoading(string placementId);
    void OnAdLoaded(string placementId);
    void OnAdDisplayed(string placementId);
    void OnAdClicked(string placementId);
    void OnAdClosed(string placementId);
    void OnAdFailed(string placementId, int errorCode, string errorMessage);
    void OnVideoAdStarted(string placementId);
    void OnVideoAdCompleted(string placementId);
    void OnVideoAdSkipped(string placementId);
    void OnVideoAdSkippable(string placementId);
    void OnInstallButtonClicked(string placementId, string buttonText);
}
```

### IConsentCallback

```csharp
public interface IConsentCallback
{
    void OnConsentInfoUpdated();
    void OnConsentInfoUpdateFailed(Exception error);
    void OnConsentFormShown();
    void OnConsentFormError(Exception error);
    void OnConsentGranted();
    void OnConsentDenied();
    void OnConsentNotRequired();
    void OnConsentStatusChanged(bool hasConsent);
}
```

## Error Handling

### Error Codes

```csharp
// Error codes are available via Constants.ErrorCodes or ErrorCodes (alias)
public static class ErrorCodes
{
    public const int InvalidURL      = 1001; // Invalid URL
    public const int InvalidResponse  = 1002; // Invalid response
    public const int NetworkError     = 1003; // Network error
    public const int TimeoutError     = 1004; // Request timeout
    public const int Timeout          = 1004; // Alias for TimeoutError
    public const int UnknownError     = 1005; // Unknown error
}
```

### Error Messages

```csharp
public static class ErrorMessages
{
    public const string FailedToBuildURL = "Failed to build request URL";
    public const string InvalidResponse  = "Invalid response from server";
    public const string NetworkError     = "Network error occurred";
    public const string TimeoutError     = "Request timeout";
    public const string UnknownError     = "Unknown error occurred";
}
```

### Error Handling Example

```csharp
public void OnAdFailed(string placementId, int errorCode, string errorMessage)
{
    switch (errorCode)
    {
        case ErrorCodes.InvalidURL:
            Logger.InfoError("Invalid URL: " + errorMessage);
            break;
        case ErrorCodes.NetworkError:
            Logger.InfoError("Network error: " + errorMessage);
            break;
        case ErrorCodes.TimeoutError:
            Logger.InfoError("Request timeout: " + errorMessage);
            break;
        default:
            Logger.InfoError($"Unknown error ({errorCode}): {errorMessage}");
            break;
    }
}
```

## Integration Guide

### Basic Integration

```csharp
using UnityEngine;
using BidscubeSDK;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        // Initialize SDK first
        BidscubeSDK.Initialize();

        // Your other initialization code
    }
}
```

```csharp
using UnityEngine;
using BidscubeSDK;

public class AdManager : MonoBehaviour, IAdCallback
{
    public static AdManager Instance { get; private set; }

    private void Awake()
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

    public void OnAdLoading(string placementId) { }
    public void OnAdLoaded(string placementId) { }
    public void OnAdDisplayed(string placementId) { }
    public void OnAdClicked(string placementId) { }
    public void OnAdClosed(string placementId) { }
    public void OnAdFailed(string placementId, int errorCode, string errorMessage) { }
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnVideoAdSkippable(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

## Code Examples

### Ad Lifecycle Example

```csharp
using UnityEngine;
using BidscubeSDK;

public class AdLifecycleExample : MonoBehaviour, IAdCallback
{
    public string placementId = "your_placement_id";

    public void ShowAdWithLifecycle()
    {
        if (!BidscubeSDK.IsInitialized())
        {
            Logger.InfoError("SDK not initialized!");
            return;
        }

        // Show loading indicator
        ShowLoadingIndicator();

        // Request ad
        BidscubeSDK.ShowImageAd(placementId, this);
    }

    private void ShowLoadingIndicator() { /* ... */ }
    private void HideLoadingIndicator() { /* ... */ }

    public void OnAdLoading(string placementId) { }

    public void OnAdLoaded(string placementId)
    {
        HideLoadingIndicator();
        Logger.Info($"Ad loaded successfully: {placementId}");
    }

    public void OnAdFailed(string placementId, int errorCode, string errorMessage)
    {
        HideLoadingIndicator();
        Logger.InfoError($"Ad failed: {errorMessage}");
        // Handle failure (retry, show fallback, etc.)
    }

    public void OnAdDisplayed(string placementId) { }
    public void OnAdClicked(string placementId) { }
    public void OnAdClosed(string placementId) { }
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnVideoAdSkippable(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

## Consent Management

### Basic Consent Implementation

```csharp
using UnityEngine;
using BidscubeSDK;

public class ConsentManager : MonoBehaviour, IConsentCallback
{
    public void RequestConsent()
    {
        BidscubeSDK.RequestConsentInfoUpdate(this);
    }

    public void OnConsentInfoUpdated()
    {
        if (BidscubeSDK.IsConsentRequired())
        {
            BidscubeSDK.ShowConsentForm(this);
        }
    }

    public void OnConsentInfoUpdateFailed(Exception error) { }
    public void OnConsentFormShown() { }
    public void OnConsentFormError(Exception error) { }
    public void OnConsentGranted() { Logger.Info("Consent granted - ads can be shown"); }
    public void OnConsentDenied() { Logger.Info("Consent denied - ads should not be shown"); }
    public void OnConsentNotRequired() { }
    public void OnConsentStatusChanged(bool hasConsent) { }
}
```

## Banner Ad Management

### Basic Banner Implementation

```csharp
using UnityEngine;
using BidscubeSDK;

public class BannerManager : MonoBehaviour, IAdCallback
{
    public string bannerPlacementId = "banner_placement_id";

    public void ShowHeaderBanner()
    {
        BidscubeSDK.ShowHeaderBanner(bannerPlacementId, this);
    }

    public void ShowCustomBanner(AdPosition position, int width, int height)
    {
        BidscubeSDK.ShowCustomBanner(bannerPlacementId, position, width, height, this);
    }

    public void OnAdLoading(string placementId) { }
    public void OnAdLoaded(string placementId) { }
    public void OnAdDisplayed(string placementId) { }
    public void OnAdClicked(string placementId) { }
    public void OnAdClosed(string placementId) { }
    public void OnAdFailed(string placementId, int errorCode, string errorMessage) { }
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnVideoAdSkippable(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

## Troubleshooting

```csharp
var config = new SDKConfig.Builder()
    .EnableDebugMode(true)
    .EnableLogging(true)
    .Build();

BidscubeSDK.Initialize(config);
```

## Best Practices

```csharp
// Initialize early in app lifecycle
BidscubeSDK.Initialize();

// Always handle errors and provide fallback content
public void OnAdFailed(string placementId, int errorCode, string errorMessage)
{
    Logger.InfoError($"Ad failed: {errorMessage}");
    // Show fallback content here
}

// Clean up resources when no longer needed
private void OnDestroy()
{
    BidscubeSDK.Cleanup();
}
```

## Requirements

```text
Unity 2020.3 or later
.NET Standard 2.0 or later
iOS 12.0 or later (for iOS builds)
Android API Level 21 or later (for Android builds)
```

## Support

For support and questions:

```text
Email: support@bidscube.com
```
