# Bidscube Unity SDK - Multi-Scene Test Setup Guide

## 🎯 Overview

This guide explains how to set up and use the three separate test scenes that mirror the iOS SDK test structure:

1. **BidscubeExampleScene** - Main scene with navigation to other test scenes
2. **SDKTestScene** - Basic SDK functionality testing
3. **ConsentTestScene** - Consent form and consent management testing
4. **WindowedAdTestScene** - Ad positioning and layout testing

## 🏗️ Scene Structure

### **Main Scene: BidscubeExampleScene**

- **Purpose**: Central hub with navigation to all test scenes
- **Features**:
  - All original SDK functionality (ads, banners, consent)
  - Navigation buttons to other test scenes
  - Complete UI setup with SceneSetupHelper

### **SDK Test Scene: SDKTestScene**

- **Purpose**: Basic SDK functionality testing
- **Features**:
  - SDK initialization and cleanup
  - Ad type testing (Image, Video, Native)
  - Logging control and testing
  - Manual position override for testing
  - Active banner count display
  - Navigation back to other scenes

### **Consent Test Scene: ConsentTestScene**

- **Purpose**: Consent form and consent management testing
- **Features**:
  - Consent info update requests
  - Consent form display
  - Consent status checking (required, ads, analytics)
  - Debug mode and consent reset
  - Ad testing with consent validation
  - Navigation back to other scenes

### **Windowed Ad Test Scene: WindowedAdTestScene**

- **Purpose**: Ad positioning and layout testing
- **Features**:
  - Ad positioning testing (all positions)
  - Real ad creation and display
  - Layout validation
  - Content simulation for testing
  - Navigation back to other scenes

## 🚀 Setup Instructions

### **Step 1: Create the Scenes**

1. **Create the main BidscubeExampleScene** (already exists)
2. **Create SDKTestScene**:

   - Create new scene: `File → New Scene`
   - Save as: `Assets/Scenes/SDKTestScene.unity`
   - Add the `SDKTestScene` script to a GameObject
   - Add the `SceneManager` script to the same GameObject

3. **Create ConsentTestScene**:

   - Create new scene: `File → New Scene`
   - Save as: `Assets/Scenes/ConsentTestScene.unity`
   - Add the `ConsentTestScene` script to a GameObject
   - Add the `SceneManager` script to the same GameObject

4. **Create WindowedAdTestScene**:
   - Create new scene: `File → New Scene`
   - Save as: `Assets/Scenes/WindowedAdTestScene.unity`
   - Add the `WindowedAdTestScene` script to a GameObject
   - Add the `SceneManager` script to the same GameObject

### **Step 2: Add Scenes to Build Settings**

1. Open `File → Build Settings`
2. Add all four scenes to the build:
   - `BidscubeExampleScene`
   - `SDKTestScene`
   - `ConsentTestScene`
   - `WindowedAdTestScene`

### **Step 3: Set Up UI for Each Scene**

#### **For BidscubeExampleScene (Main Scene):**

1. Use the existing `SceneSetupHelper` or `SetupScene` script
2. This will create the complete UI with navigation buttons

#### **For SDKTestScene:**

1. Create a Canvas with the following UI elements:
   - SDK Status Text
   - Placement ID Input Field
   - Initialize/Cleanup Buttons
   - Ad Type Buttons (Image, Video, Native)
   - Logging Control Buttons
   - Manual Position Toggle and Dropdown
   - Log Panel with ScrollRect
   - Navigation Buttons
   - Ad Display Area

#### **For ConsentTestScene:**

1. Create a Canvas with the following UI elements:
   - SDK Status Text
   - Placement ID Input Field
   - Initialize/Cleanup Buttons
   - Consent Management Buttons
   - Ad Testing Buttons
   - Log Panel with ScrollRect
   - Navigation Buttons
   - Ad Display Area

#### **For WindowedAdTestScene:**

1. Create a Canvas with the following UI elements:
   - SDK Status Text
   - Initialize/Cleanup Buttons
   - Ad Creation Buttons
   - Position Testing Buttons
   - Layout Validation Button
   - Positioning Panel Toggle
   - Log Panel with ScrollRect
   - Navigation Buttons
   - Ad Display Area
   - Content Area (for testing)

### **Step 4: Configure Script References**

For each scene, you need to assign the UI references in the respective script components:

#### **SDKTestScene References:**

- `_sdkStatusText` → Status Text component
- `_placementIdInput` → Placement ID Input Field
- `_initializeButton` → Initialize Button
- `_cleanupButton` → Cleanup Button
- `_imageAdButton` → Image Ad Button
- `_videoAdButton` → Video Ad Button
- `_nativeAdButton` → Native Ad Button
- `_enableLoggingButton` → Enable Logging Button
- `_disableLoggingButton` → Disable Logging Button
- `_testLoggingButton` → Test Logging Button
- `_useManualPositionToggle` → Manual Position Toggle
- `_positionDropdown` → Position Dropdown
- `_logScrollRect` → Log ScrollRect
- `_logText` → Log Text component
- `_adDisplayArea` → Ad Display Area RectTransform
- Navigation buttons → Back to Main, Consent Test, Windowed Ad

#### **ConsentTestScene References:**

- `_sdkStatusText` → Status Text component
- `_placementIdInput` → Placement ID Input Field
- `_initializeButton` → Initialize Button
- `_cleanupButton` → Cleanup Button
- `_requestConsentInfoButton` → Request Consent Info Button
- `_showConsentFormButton` → Show Consent Form Button
- `_checkConsentRequiredButton` → Check Consent Required Button
- `_checkAdsConsentButton` → Check Ads Consent Button
- `_checkAnalyticsConsentButton` → Check Analytics Consent Button
- `_getConsentSummaryButton` → Get Consent Summary Button
- `_enableDebugModeButton` → Enable Debug Mode Button
- `_resetConsentButton` → Reset Consent Button
- `_showImageAdButton` → Show Image Ad Button
- `_showVideoAdButton` → Show Video Ad Button
- `_showNativeAdButton` → Show Native Ad Button
- `_logScrollRect` → Log ScrollRect
- `_logText` → Log Text component
- `_adDisplayArea` → Ad Display Area RectTransform
- Navigation buttons → Back to Main, SDK Test, Windowed Ad

#### **WindowedAdTestScene References:**

- `_sdkStatusText` → Status Text component
- `_initializeButton` → Initialize Button
- `_cleanupButton` → Cleanup Button
- `_createImageAdButton` → Create Image Ad Button
- `_createVideoAdButton` → Create Video Ad Button
- `_createNativeAdButton` → Create Native Ad Button
- `_validateLayoutButton` → Validate Layout Button
- `_showPositioningPanelToggle` → Positioning Panel Toggle
- `_positioningPanel` → Positioning Panel RectTransform
- `_logScrollRect` → Log ScrollRect
- `_logText` → Log Text component
- `_adDisplayArea` → Ad Display Area RectTransform
- `_contentArea` → Content Area RectTransform
- Position buttons → All position testing buttons
- Navigation buttons → Back to Main, SDK Test, Consent Test

## 🎮 Usage

### **Navigation Flow:**

1. **Start with BidscubeExampleScene** (main scene)
2. **Use navigation buttons** to switch between test scenes
3. **Each scene is independent** with its own SDK state
4. **Return to main scene** using navigation buttons

### **Testing Workflow:**

1. **Main Scene**: Test all basic functionality
2. **SDK Test Scene**: Focus on SDK initialization and ad types
3. **Consent Test Scene**: Test consent management
4. **Windowed Ad Scene**: Test ad positioning and layout

## 🔧 Scene Manager

The `SceneManager` script handles scene transitions:

```csharp
// Load different scenes
GetComponent<SceneManager>()?.LoadMainScene();
GetComponent<SceneManager>()?.LoadSDKTestScene();
GetComponent<SceneManager>()?.LoadConsentTestScene();
GetComponent<SceneManager>()?.LoadWindowedAdScene();
```

## 📱 Scene-Specific Features

### **SDKTestScene Features:**

- Manual position override for testing
- Active banner count display
- Logging control and testing
- Ad type testing with position control

### **ConsentTestScene Features:**

- Consent status checking
- Debug mode for consent testing
- Ad testing with consent validation
- Consent form display

### **WindowedAdTestScene Features:**

- Ad positioning testing
- Layout validation
- Content simulation
- Real ad creation and display

## 🎨 UI Layout Guidelines

### **Common Elements:**

- **Canvas**: Screen Space - Overlay, Scale With Screen Size
- **Reference Resolution**: 1920 x 1080
- **Navigation**: Always at top or bottom
- **Log Panel**: Scrollable text area
- **Status Display**: Real-time SDK status

### **Scene-Specific Layout:**

- **Main Scene**: Full functionality with navigation
- **SDK Test**: Focus on SDK operations
- **Consent Test**: Focus on consent management
- **Windowed Ad**: Focus on ad positioning

## 🐛 Troubleshooting

### **Scene Navigation Issues:**

- Ensure all scenes are in Build Settings
- Check that SceneManager script is attached
- Verify scene names match exactly

### **UI Reference Issues:**

- Check that all UI references are assigned
- Verify component types match expected types
- Ensure UI elements are properly named

### **SDK State Issues:**

- Each scene initializes its own SDK state
- SDK state is not shared between scenes
- Cleanup properly when switching scenes

## 📝 Notes

- Each scene is independent with its own SDK state
- Navigation preserves scene state
- All scenes use the same SDK configuration
- Logging is consistent across all scenes
- UI layout follows the same design principles

This multi-scene structure provides a comprehensive testing environment that mirrors the iOS SDK test structure while maintaining the flexibility of Unity's scene system.

