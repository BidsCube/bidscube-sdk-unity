# Bidscube Unity SDK Integration Guide

This guide will help you integrate the Bidscube Unity SDK into your Unity project and start showing ads.

## Table of Contents

1. [Installation](#installation)
2. [Initialization](#initialization)
3. [Configuration](#configuration)
4. [Showing Ads](#showing-ads)
5. [Ad Callbacks](#ad-callbacks)
6. [Ad Positioning](#ad-positioning) a
7. [Consent Management](#consent-management)
8. [Examples](#examples)
9. [SDK Test Scene](#sdk-test-scene)

---

## Installation

The Bidscube Unity SDK can be installed in two ways:

### Method 1: Unity Package Import (Recommended)

1. Download the latest `BidscubeSDK-unity-vX.X.X.unitypackage` from the [GitHub Releases](https://github.com/Bidscube/bidscube-sdk-unity/releases)
2. Open your Unity project
3. In Unity Editor, go to `Assets` → `Import Package` → `Custom Package...`
4. Select the downloaded `.unitypackage` file
5. In the import dialog, ensure all files are selected and click `Import`
6. The SDK will be imported into your project under `Assets/BidscubeSDK/`

### Method 2: Git Repository Import

1. In Unity Editor, open the Package Manager (`Window` → `Package Manager`)
2. Click the `+` button in the top-left corner
3. Select `Add package from git URL...`
4. Enter the repository URL: `https://github.com/Bidscube/bidscube-sdk-unity.git`
5. Optionally, specify a version tag: `https://github.com/Bidscube/bidscube-sdk-unity.git#v1.1.0`
6. Click `Add`
7. The SDK will be added as a package dependency

**Note:** For Git import, you may need to add the package to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.bidscube.sdk": "https://github.com/Bidscube/bidscube-sdk-unity.git#v1.1.0"
  }
}
```

---

## Initialization

Before using the SDK, you must initialize it. The best place to do this is in your game's startup script (e.g., in a `GameManager` or `SDKInitializer` script).

### Basic Initialization

```csharp
using BidscubeSDK;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Initialize with default settings
        BidscubeSDK.BidscubeSDK.Initialize();
    }
}
```

### Advanced Initialization with Configuration

```csharp
using BidscubeSDK;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Create custom configuration
        var config = new SDKConfig.Builder()
            .EnableLogging(true)                    // Enable SDK logging
            .EnableDebugMode(false)                  // Disable debug mode for production
            .DefaultAdTimeout(30000)                  // 30 second timeout
            .DefaultAdPosition(AdPosition.Unknown)    // Default position (centered)
            .BaseURL("https://ssp-bcc-ads.com/sdk")  // Base URL for ad requests
            .Build();

        // Initialize with configuration
        BidscubeSDK.BidscubeSDK.Initialize(config);
    }
}
```

### Check if SDK is Initialized

```csharp
if (BidscubeSDK.BidscubeSDK.IsInitialized())
{
    // SDK is ready to use
}
```

---

## Configuration

### SDK Configuration Options

The `SDKConfig` class allows you to configure various aspects of the SDK:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableLogging` | `bool` | `true` | Enable/disable SDK logging |
| `EnableDebugMode` | `bool` | `false` | Enable/disable debug mode |
| `DefaultAdTimeout` | `int` | `30000` | Default ad loading timeout in milliseconds |
| `DefaultAdPosition` | `AdPosition` | `Unknown` | Default ad position (centered) |
| `BaseURL` | `string` | `https://ssp-bcc-ads.com/sdk` | Base URL for ad requests |

### Configuration Builder Pattern

```csharp
var config = new SDKConfig.Builder()
    .EnableLogging(true)
    .EnableDebugMode(false)
    .DefaultAdTimeout(30000)
    .DefaultAdPosition(AdPosition.Header)
    .BaseURL("https://your-custom-url.com/sdk")
    .Build();

BidscubeSDK.BidscubeSDK.Initialize(config);
```

---

## Showing Ads

The SDK supports three types of ads: **Image/Banner**, **Video**, and **Native**.

### Image/Banner Ads

Image ads are static banner advertisements that can be displayed at various positions on the screen.

#### Basic Image Ad

```csharp
using BidscubeSDK;

// Show image ad with default position (centered)
BidscubeSDK.BidscubeSDK.ShowImageAd("your-placement-id", new MyAdCallback());
```

#### Positioned Banner Ads

```csharp
// Show header banner (top of screen)
BidscubeSDK.BidscubeSDK.ShowHeaderBanner("your-placement-id", new MyAdCallback());

// Show footer banner (bottom of screen)
BidscubeSDK.BidscubeSDK.ShowFooterBanner("your-placement-id", new MyAdCallback());

// Show sidebar banner (right side)
BidscubeSDK.BidscubeSDK.ShowSidebarBanner("your-placement-id", new MyAdCallback());
```

#### Custom Position Banner

```csharp
// Show banner at custom position with specific dimensions
BidscubeSDK.BidscubeSDK.ShowCustomBanner(
    "your-placement-id",
    AdPosition.Header,  // Position
    320,                 // Width in pixels
    50,                  // Height in pixels
    new MyAdCallback()
);
```

### Video Ads

Video ads are full-screen video advertisements that play automatically.

#### Basic Video Ad

```csharp
// Show video ad
BidscubeSDK.BidscubeSDK.ShowVideoAd("your-placement-id", new MyAdCallback());
```

#### Skippable Video Ad

```csharp
// Show skippable video ad with custom skip button text
BidscubeSDK.BidscubeSDK.ShowSkippableVideoAd(
    "your-placement-id",
    "Skip Ad",           // Skip button text
    new MyAdCallback()
);
```

**Note:** Video ads are always displayed in full-screen mode regardless of position settings.

### Native Ads

Native ads are customizable advertisements that match your app's design.

```csharp
// Show native ad
BidscubeSDK.BidscubeSDK.ShowNativeAd("your-placement-id", new MyAdCallback());
```

---

## Ad Callbacks

Implement the `IAdCallback` interface to receive ad events, or extend the `AdCallback` base class for convenience.

### Using IAdCallback Interface

```csharp
using BidscubeSDK;

public class MyAdHandler : MonoBehaviour, IAdCallback
{
    public void OnAdLoading(string placementId)
    {
        Debug.Log($"Ad is loading: {placementId}");
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

    public void OnAdClosed(string placementId)
    {
        Debug.Log($"Ad closed: {placementId}");
    }

    public void OnAdFailed(string placementId, int errorCode, string errorMessage)
    {
        Debug.LogError($"Ad failed: {placementId}, Error: {errorCode} - {errorMessage}");
    }

    // Video-specific callbacks
    public void OnVideoAdStarted(string placementId)
    {
        Debug.Log($"Video ad started: {placementId}");
    }

    public void OnVideoAdCompleted(string placementId)
    {
        Debug.Log($"Video ad completed: {placementId}");
    }

    public void OnVideoAdSkipped(string placementId)
    {
        Debug.Log($"Video ad skipped: {placementId}");
    }

    public void OnVideoAdSkippable(string placementId)
    {
        Debug.Log($"Video ad is now skippable: {placementId}");
    }

    public void OnInstallButtonClicked(string placementId, string buttonText)
    {
        Debug.Log($"Install button clicked: {placementId}, Text: {buttonText}");
    }
}
```

### Using AdCallback Base Class

```csharp
using BidscubeSDK;

public class MyAdHandler : AdCallback
{
    public override void OnAdLoaded(string placementId)
    {
        Debug.Log($"Ad loaded: {placementId}");
        // Only override methods you need
    }

    public override void OnAdFailed(string placementId, int errorCode, string errorMessage)
    {
        Debug.LogError($"Ad failed: {errorMessage}");
    }
}
```

### Using Callbacks

```csharp
// Create callback instance
var callback = new MyAdHandler();

// Show ad with callback
BidscubeSDK.BidscubeSDK.ShowImageAd("your-placement-id", callback);
```

---

## Ad Positioning

Ads can be positioned at various locations on the screen. The SDK supports both manual positioning and server-determined positioning.

### Ad Position Enum

```csharp
public enum AdPosition
{
    Unknown = 0,           // Centered (default)
    AboveTheFold = 1,      // Above the fold
    BelowTheFold = 3,      // Below the fold
    Header = 4,            // Top of screen
    Footer = 5,            // Bottom of screen
    Sidebar = 6,           // Right side
    FullScreen = 7         // Full screen (video ads only)
}
```

### Manual Position Override

You can manually set the ad position, which will override the server response:

```csharp
// Set manual position
BidscubeSDK.BidscubeSDK.SetAdPosition(AdPosition.Header);

// Show ad (will use Header position)
BidscubeSDK.BidscubeSDK.ShowImageAd("your-placement-id", callback);
```

### Get Current Position

```csharp
// Get manual position
AdPosition manualPosition = BidscubeSDK.BidscubeSDK.GetAdPosition();

// Get server response position
AdPosition serverPosition = BidscubeSDK.BidscubeSDK.GetResponseAdPosition();

// Get effective position (manual override takes priority)
AdPosition effectivePosition = BidscubeSDK.BidscubeSDK.GetEffectiveAdPosition();
```

### Position Priority

The SDK uses the following priority order for ad positioning:

1. **Manual Position** (if set via `SetAdPosition()`) - Highest priority
2. **Server Response Position** (from ad response) - Medium priority
3. **Default Position** (Unknown/Centered) - Lowest priority

---

## Consent Management

The SDK includes built-in consent management for GDPR and CCPA compliance.

### Request Consent Info

```csharp
using BidscubeSDK;

public class ConsentManager : MonoBehaviour, IConsentCallback
{
    void Start()
    {
        // Request consent info update
        BidscubeSDK.BidscubeSDK.RequestConsentInfoUpdate(this);
    }

    public void OnConsentInfoUpdated()
    {
        Debug.Log("Consent info updated");
        // Check if consent form is required
        // Show consent form if needed
    }

    public void OnConsentInfoUpdateFailed(Exception error)
    {
        Debug.LogError($"Consent info update failed: {error.Message}");
    }

    public void OnConsentFormShown()
    {
        Debug.Log("Consent form shown");
    }

    public void OnConsentFormError(Exception error)
    {
        Debug.LogError($"Consent form error: {error.Message}");
    }

    public void OnConsentGranted()
    {
        Debug.Log("Consent granted");
        // User granted consent, can now show ads
    }

    public void OnConsentDenied()
    {
        Debug.Log("Consent denied");
        // User denied consent, handle accordingly
    }

    public void OnConsentNotRequired()
    {
        Debug.Log("Consent not required");
        // Consent not required for this user
    }

    public void OnConsentStatusChanged(bool hasConsent)
    {
        Debug.Log($"Consent status changed: {hasConsent}");
    }
}
```

### Show Consent Form

```csharp
// Show consent form
BidscubeSDK.BidscubeSDK.ShowConsentForm(new MyConsentCallback());
```

---

## Examples

### Complete Integration Example

```csharp
using BidscubeSDK;
using UnityEngine;

public class AdManager : MonoBehaviour, IAdCallback
{
    [Header("Ad Configuration")]
    public string placementId = "your-placement-id";
    public AdPosition adPosition = AdPosition.Header;

    void Start()
    {
        // Initialize SDK
        var config = new SDKConfig.Builder()
            .EnableLogging(true)
            .DefaultAdTimeout(30000)
            .Build();
        
        BidscubeSDK.BidscubeSDK.Initialize(config);

        // Set ad position
        BidscubeSDK.BidscubeSDK.SetAdPosition(adPosition);

        // Show ad after a delay
        Invoke(nameof(ShowAd), 2f);
    }

    void ShowAd()
    {
        BidscubeSDK.BidscubeSDK.ShowImageAd(placementId, this);
    }

    // IAdCallback implementation
    public void OnAdLoading(string placementId)
    {
        Debug.Log("Loading ad...");
    }

    public void OnAdLoaded(string placementId)
    {
        Debug.Log("Ad loaded successfully");
    }

    public void OnAdDisplayed(string placementId)
    {
        Debug.Log("Ad displayed");
    }

    public void OnAdClicked(string placementId)
    {
        Debug.Log("Ad clicked");
    }

    public void OnAdClosed(string placementId)
    {
        Debug.Log("Ad closed");
    }

    public void OnAdFailed(string placementId, int errorCode, string errorMessage)
    {
        Debug.LogError($"Ad failed: {errorMessage}");
    }

    // Video callbacks (optional)
    public void OnVideoAdStarted(string placementId) { }
    public void OnVideoAdCompleted(string placementId) { }
    public void OnVideoAdSkipped(string placementId) { }
    public void OnVideoAdSkippable(string placementId) { }
    public void OnInstallButtonClicked(string placementId, string buttonText) { }
}
```

### Show Different Ad Types

```csharp
using BidscubeSDK;

public class AdExamples : MonoBehaviour
{
    public string imagePlacementId = "image-placement-id";
    public string videoPlacementId = "video-placement-id";
    public string nativePlacementId = "native-placement-id";

    void Start()
    {
        BidscubeSDK.BidscubeSDK.Initialize();
    }

    public void ShowImageAd()
    {
        BidscubeSDK.BidscubeSDK.ShowImageAd(imagePlacementId, new MyAdCallback());
    }

    public void ShowVideoAd()
    {
        BidscubeSDK.BidscubeSDK.ShowVideoAd(videoPlacementId, new MyAdCallback());
    }

    public void ShowNativeAd()
    {
        BidscubeSDK.BidscubeSDK.ShowNativeAd(nativePlacementId, new MyAdCallback());
    }
}
```

### Cleanup

When your app closes or you want to clean up SDK resources:

```csharp
void OnDestroy()
{
    BidscubeSDK.BidscubeSDK.Cleanup();
}
```

---

## SDK Test Scene

The SDK includes a comprehensive test scene (`SDKTestScene`) that demonstrates all SDK features and provides a working example of integration.

### Accessing the Test Scene

1. After importing the SDK, navigate to `Assets/BidscubeSDK/Scenes/` (or `Runtime/Scenes/` if using Git import)
2. Open `SDK Test Scene.unity`
3. The scene contains:
   - UI buttons to test different ad types
   - Position selection dropdown
   - Manual position toggle
   - Log output display
   - All callback implementations

### Using the Test Scene

1. Open the test scene in Unity Editor
2. Configure your placement IDs in the `SDKTestScene` component
3. Press Play
4. Initialize SDK with Initialize SDK (Button)
 Then Use the UI buttons to test:
   - Image/Banner ads
   - Video ads
   - Native ads
   - Manual position override
   - Clean Up SDK to destroy adObjects

The test scene serves as both a testing tool and a reference implementation for integrating the SDK into your project.

---

## Additional Resources

- **GitHub Repository**: [https://github.com/Bidscube/bidscube-sdk-unity](https://github.com/Bidscube/bidscube-sdk-unity)
- **Releases**: [https://github.com/Bidscube/bidscube-sdk-unity/releases](https://github.com/Bidscube/bidscube-sdk-unity/releases)
- **Issues**: [https://github.com/Bidscube/bidscube-sdk-unity/issues](https://github.com/Bidscube/bidscube-sdk-unity/issues)

---

## Support

For support, please open an issue on the GitHub repository or contact the Bidscube support team.

---

## License

See the LICENSE file in the repository for license information.

