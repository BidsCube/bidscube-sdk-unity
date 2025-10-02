# Bidscube Unity SDK - Test Scene Setup Guide

## 🎯 Scene Hierarchy Structure

The **BidscubeExampleScene** should have the following hierarchy to demonstrate all SDK functionality:

```
BidscubeExampleScene
├── Main Camera
│   ├── Audio Listener
│   └── Camera
└── BidscubeExampleCanvas (Canvas)
    ├── EventSystem
    ├── BidscubeExampleController (GameObject with BidscubeExampleScene script)
    ├── UI Panel (Panel)
    │   ├── Title (Text) - "Bidscube Unity SDK Test"
    │   ├── StatusText (Text) - "Status: Ready to initialize SDK"
    │   ├── InitButton (Button) - "Initialize SDK"
    │   ├── AdButtonsPanel (Panel)
    │   │   ├── ImageAdButton (Button) - "Show Image Ad"
    │   │   ├── VideoAdButton (Button) - "Show Video Ad"
    │   │   └── NativeAdButton (Button) - "Show Native Ad"
    │   ├── BannerButtonsPanel (Panel)
    │   │   ├── HeaderBannerButton (Button) - "Show Header Banner"
    │   │   ├── FooterBannerButton (Button) - "Show Footer Banner"
    │   │   ├── SidebarBannerButton (Button) - "Show Sidebar Banner"
    │   │   └── CustomBannerButton (Button) - "Show Custom Banner"
    │   ├── OtherButtonsPanel (Panel)
    │   │   ├── ConsentButton (Button) - "Show Consent Form"
    │   │   └── RemoveAllBannersButton (Button) - "Remove All Banners"
    │   └── LogPanel (Panel)
    │       ├── LogTitle (Text) - "SDK Log"
    │       └── LogScrollView (ScrollRect)
    │           ├── Viewport
    │           │   └── LogText (Text)
    │           └── Scrollbar
    ├── BannerAreas (Panel)
    │   ├── HeaderBannerArea (RectTransform) - Top banner area
    │   ├── FooterBannerArea (RectTransform) - Bottom banner area
    │   └── SidebarBannerArea (RectTransform) - Right sidebar area
    └── AdDisplayArea (Panel) - Full-screen ad display area
```

## 🎨 UI Layout Specifications

### **Main Canvas Settings:**

- **Render Mode:** Screen Space - Overlay
- **UI Scale Mode:** Scale With Screen Size
- **Reference Resolution:** 1920 x 1080
- **Screen Match Mode:** Match Width Or Height
- **Match:** 0.5

### **Button Layout:**

```
┌─────────────────────────────────────────────────────────┐
│                    Bidscube Unity SDK Test              │
│                                                         │
│ Status: Ready to initialize SDK                        │
│                                                         │
│ [Initialize SDK]                                       │
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │                Ad Types                             │ │
│ │ [Show Image Ad] [Show Video Ad] [Show Native Ad]    │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │                Banners                              │ │
│ │ [Header] [Footer] [Sidebar] [Custom]                │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │                Other Actions                         │ │
│ │ [Consent Form] [Remove All Banners]                 │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │                SDK Log                              │ │
│ │ [12:34:56] Initializing Bidscube SDK...            │ │
│ │ [12:34:57] ✅ SDK initialized successfully         │ │
│ │ [12:34:58] 🖼️ Showing Image Ad...                   │ │
│ │ ...                                                 │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

## 🔧 Component Configuration

### **BidscubeExampleScene Script Settings:**

```csharp
[Header("SDK Configuration")]
_placementId = "test_placement_123"
_baseURL = Constants.baseURL
_enableDebugMode = true
_enableLogging = true

[Header("UI References")]
_initButton = [Initialize SDK Button]
_imageAdButton = [Show Image Ad Button]
_videoAdButton = [Show Video Ad Button]
_nativeAdButton = [Show Native Ad Button]
_headerBannerButton = [Header Banner Button]
_footerBannerButton = [Footer Banner Button]
_sidebarBannerButton = [Sidebar Banner Button]
_customBannerButton = [Custom Banner Button]
_consentButton = [Consent Form Button]
_removeAllBannersButton = [Remove All Banners Button]

[Header("Status Display")]
_statusText = [Status Text Component]
_logScrollRect = [Log ScrollRect Component]
_logText = [Log Text Component]

[Header("Banner Display Areas")]
_headerBannerArea = [Header Banner Area RectTransform]
_footerBannerArea = [Footer Banner Area RectTransform]
_sidebarBannerArea = [Sidebar Banner Area RectTransform]
```

## 🎯 Banner Area Positioning

### **Header Banner Area:**

- **Anchor:** Top stretch
- **Position:** Y = -25, Height = 50
- **Purpose:** Display header banners

### **Footer Banner Area:**

- **Anchor:** Bottom stretch
- **Position:** Y = 25, Height = 50
- **Purpose:** Display footer banners

### **Sidebar Banner Area:**

- **Anchor:** Right stretch
- **Position:** X = -60, Width = 120
- **Purpose:** Display sidebar banners

## 🚀 Testing Workflow

### **1. Initialize SDK**

- Click "Initialize SDK" button
- Check log for successful initialization
- Status should show "SDK Initialized Successfully"

### **2. Test Ad Types**

- **Image Ad:** Click "Show Image Ad" → Full-screen image ad appears
- **Video Ad:** Click "Show Video Ad" → Full-screen video ad appears
- **Native Ad:** Click "Show Native Ad" → Full-screen native ad appears

### **3. Test Banners**

- **Header Banner:** Click "Show Header Banner" → Banner appears at top
- **Footer Banner:** Click "Show Footer Banner" → Banner appears at bottom
- **Sidebar Banner:** Click "Show Sidebar Banner" → Banner appears on right
- **Custom Banner:** Click "Show Custom Banner" → Custom sized banner appears

### **4. Test Other Features**

- **Consent Form:** Click "Show Consent Form" → Consent dialog appears
- **Remove All Banners:** Click "Remove All Banners" → All banners disappear

## 📱 Expected Behavior

### **Ad Callbacks:**

- All ad interactions should trigger appropriate callbacks
- Log should show: Loading → Loaded → Displayed → Clicked/Closed/Failed
- Status text should update with current ad state

### **Banner Behavior:**

- Banners should attach to designated areas
- Banners should be clickable and trigger callbacks
- Multiple banners can be displayed simultaneously
- "Remove All Banners" should clear all active banners

### **Error Handling:**

- Network errors should be logged
- Invalid placement IDs should show error messages
- SDK not initialized should show warning

## 🎨 Visual Design

### **Color Scheme:**

- **Background:** Dark blue (#1A1A2E)
- **Panels:** Semi-transparent white (0.9 alpha)
- **Buttons:** Blue (#4A90E2) with white text
- **Success:** Green (#4CAF50)
- **Error:** Red (#F44336)
- **Warning:** Orange (#FF9800)

### **Typography:**

- **Title:** Bold, 24px
- **Buttons:** Regular, 16px
- **Status:** Regular, 14px
- **Log:** Monospace, 12px

This test scene provides a comprehensive testing environment for all Bidscube Unity SDK functionality, making it easy to verify that all features work correctly.

