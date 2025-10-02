# Scene Setup Scripts Guide

## 🎯 Overview

I've created separate setup scripts for each test scene that automatically create the complete UI hierarchy, matching the iOS SDK test structure. Each script creates the appropriate UI elements and connects them to the scene controllers.

## 🏗️ Available Setup Scripts

### **1. SDKTestSceneSetup.cs**

- **Purpose**: Sets up SDK Test Scene with basic SDK functionality
- **Features**:
  - Initialize SDK button
  - 5 main buttons: Banner/Image Ads, Video Ads, Native Ads, Test Logging, Cleanup SDK
  - Placement ID input
  - Logging control buttons
  - Manual position override for testing
  - Active banner count display
  - Navigation buttons

### **2. ConsentTestSceneSetup.cs**

- **Purpose**: Sets up Consent Test Scene for consent management
- **Features**:
  - Initialize SDK button
  - Consent management buttons (8 buttons in grid)
  - Ad testing with consent validation
  - Placement ID input
  - Log panel with scrollable text
  - Navigation buttons

### **3. WindowedAdTestSceneSetup.cs**

- **Purpose**: Sets up Windowed Ad Test Scene for ad positioning
- **Features**:
  - Initialize SDK button
  - Ad creation buttons (Image, Video, Native, Validate Layout)
  - Position testing buttons (8 buttons in grid)
  - Positioning panel toggle
  - Content simulation area
  - Log panel
  - Navigation buttons

## 🚀 How to Use

### **Method 1: Automatic Setup (Recommended)**

1. **Add the setup script** to any GameObject in your scene:

   - For SDK Test Scene: Add `SDKTestSceneSetup` component
   - For Consent Test Scene: Add `ConsentTestSceneSetup` component
   - For Windowed Ad Test Scene: Add `WindowedAdTestSceneSetup` component

2. **Enable Auto Setup**:

   - Check `Auto Setup On Start` in the Inspector
   - The scene will be set up automatically when you play it

3. **Run the scene** - everything will be created automatically!

### **Method 2: Manual Setup**

1. **Add the setup script** to any GameObject in your scene
2. **Right-click the component** in the Inspector
3. **Select the setup method** from the context menu:
   - "Setup SDK Test Scene"
   - "Setup Consent Test Scene"
   - "Setup Windowed Ad Test Scene"

### **Method 3: Programmatic Setup**

```csharp
// Get the setup script component
var setupScript = GetComponent<SDKTestSceneSetup>();

// Run the setup
setupScript.SetupSDKTestScene();
```

## 📋 Scene-Specific Features

### **SDK Test Scene Features:**

- **Initialize SDK Button**: Initializes the Bidscube SDK
- **5 Main Buttons**:
  - Banner/Image Ads
  - Video Ads
  - Native Ads
  - Test Logging
  - Cleanup SDK
- **Placement ID Input**: Enter custom placement ID
- **Logging Control**: Enable/Disable/Test logging
- **Position Override**: Manual position testing
- **Active Banner Count**: Shows number of active banners
- **Navigation**: Back to Main, Consent Test, Windowed Ad

### **Consent Test Scene Features:**

- **Initialize SDK Button**: Initializes the Bidscube SDK
- **8 Consent Management Buttons** (in grid layout):
  - Request Consent Info Update
  - Show Consent Form
  - Check if Consent Required
  - Check Ads Consent
  - Check Analytics Consent
  - Get Consent Summary
  - Enable Debug Mode
  - Reset Consent
- **3 Ad Testing Buttons**:
  - Show Image Ad (if consent)
  - Show Video Ad (if consent)
  - Show Native Ad (if consent)
- **Navigation**: Back to Main, SDK Test, Windowed Ad

### **Windowed Ad Test Scene Features:**

- **Initialize SDK Button**: Initializes the Bidscube SDK
- **4 Ad Creation Buttons**:
  - Create Image Ad
  - Create Video Ad
  - Create Native Ad
  - Validate Layout
- **8 Position Testing Buttons** (in grid layout):
  - UNKNOWN, ABOVE_THE_FOLD, DEPEND_ON_SCREEN_SIZE
  - BELOW_THE_FOLD, HEADER, FOOTER, SIDEBAR, FULL_SCREEN
- **Positioning Panel Toggle**: Show/hide positioning controls
- **Content Simulation**: Sample content for testing
- **Navigation**: Back to Main, SDK Test, Consent Test

## 🔧 Configuration Options

### **Setup Script Settings:**

```csharp
[Header("Auto Setup")]
[SerializeField] private bool autoSetupOnStart = true;
[SerializeField] private bool createNavigationButtons = true;
```

- **`autoSetupOnStart`**: Automatically set up the scene when it starts
- **`createNavigationButtons`**: Create navigation buttons to other scenes

### **Canvas Settings:**

All setup scripts create Canvas with these settings:

- **Render Mode**: Screen Space - Overlay
- **UI Scale Mode**: Scale With Screen Size
- **Reference Resolution**: 1920 x 1080
- **Screen Match Mode**: Match Width Or Height
- **Match**: 0.5

## 🎮 Usage Examples

### **For SDK Test Scene:**

1. **Add `SDKTestSceneSetup`** to any GameObject
2. **Enable `Auto Setup On Start`**
3. **Play the scene** - UI will be created automatically
4. **Test the 5 main buttons**:
   - Click "Initialize SDK"
   - Click "Banner/Image Ads", "Video Ads", "Native Ads"
   - Click "Test Logging"
   - Click "Cleanup SDK"

### **For Consent Test Scene:**

1. **Add `ConsentTestSceneSetup`** to any GameObject
2. **Enable `Auto Setup On Start`**
3. **Play the scene** - UI will be created automatically
4. **Test consent management**:
   - Click "Initialize SDK"
   - Click "Request Consent Info Update"
   - Click "Show Consent Form"
   - Test other consent buttons

### **For Windowed Ad Test Scene:**

1. **Add `WindowedAdTestSceneSetup`** to any GameObject
2. **Enable `Auto Setup On Start`**
3. **Play the scene** - UI will be created automatically
4. **Test ad positioning**:
   - Click "Initialize SDK"
   - Click "Create Image Ad"
   - Test different position buttons
   - Toggle positioning panel

## 🐛 Troubleshooting

### **Common Issues:**

1. **"Setup script not found" error:**

   - Make sure the setup script is attached to a GameObject
   - Check that the script is enabled

2. **"Controller script not found" error:**

   - Ensure the corresponding scene controller script exists
   - Check that the controller script is properly named

3. **"UI elements not created" error:**
   - Check for compilation errors
   - Verify the setup script is running
   - Check the console for error messages

### **Debug Tips:**

```csharp
// Check if setup script is working
var setupScript = GetComponent<SDKTestSceneSetup>();
if (setupScript != null)
{
    Debug.Log("Setup script found");
    setupScript.SetupSDKTestScene();
}
```

## 📝 Best Practices

### **1. Use Auto Setup:**

- Enable `autoSetupOnStart` for automatic setup
- This ensures the scene is always properly set up

### **2. Enable Navigation:**

- Keep `createNavigationButtons` enabled
- This provides easy navigation between scenes

### **3. Test Each Scene:**

- Test all buttons and functionality
- Verify UI elements are properly connected
- Check that navigation works correctly

### **4. Customize as Needed:**

- Modify button text or colors
- Add additional UI elements
- Adjust positioning and sizing

## ✅ Verification Checklist

After running any setup script, verify:

- [ ] All UI elements are created
- [ ] Buttons are clickable and functional
- [ ] Controller script references are connected
- [ ] Navigation buttons work
- [ ] Canvas scaling is correct
- [ ] No compilation errors
- [ ] Scene loads without issues

## 🎯 iOS SDK Parity

These setup scripts provide the same functionality as the iOS SDK test scenes:

- **SDKTestSceneSetup** ↔ **SDKTestView.swift**
- **ConsentTestSceneSetup** ↔ **ConsentTestView.swift**
- **WindowedAdTestSceneSetup** ↔ **WindowedAdTestView.swift**

Each Unity scene now has the same structure and functionality as its iOS counterpart, making it easy to test and develop across both platforms.

The setup scripts ensure consistent UI creation and proper script reference connections, eliminating the need for manual UI setup in each scene!

