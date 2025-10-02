# Bidscube Unity SDK - Scene Setup Instructions

## 🚀 Quick Setup

You have several options to set up the Bidscube Example Scene:

### Option 1: Automatic Setup (Recommended)

1. **Open the BidscubeExampleScene** in Unity
2. **Add the SetupScene script** to any GameObject in the scene:
   - Right-click in the Hierarchy → Create Empty
   - Name it "SceneSetup"
   - Add the `SetupScene` component to it
3. **Run the setup**:
   - Select the SceneSetup GameObject
   - In the Inspector, click the gear icon (⚙️) next to the SetupScene component
   - Choose "Setup Bidscube Scene"
4. **Done!** All UI elements and script references will be automatically created and connected.

### Option 2: Manual Setup with SceneSetupHelper

1. **Add SceneSetupHelper** to any GameObject in the scene
2. **Right-click the SceneSetupHelper component** in the Inspector
3. **Choose "Setup Existing Canvas"** from the context menu
4. **All UI elements will be created** and connected to the BidscubeExampleScene script

### Option 3: Create New Scene from Scratch

1. **Add SceneSetupHelper** to any GameObject in the scene
2. **Right-click the SceneSetupHelper component** in the Inspector
3. **Choose "Create Bidscube Test Scene"** from the context menu
4. **A complete new scene will be created** with all necessary elements

## 📋 What Gets Created

The setup scripts will automatically create:

### UI Hierarchy

- **Main Canvas** with proper scaling settings
- **UI Panels** for organizing buttons
- **All Required Buttons**:
  - Initialize SDK
  - Show Image Ad, Video Ad, Native Ad
  - Show Header/Footer/Sidebar/Custom Banners
  - Show Consent Form, Remove All Banners
- **Status Display** with real-time updates
- **Log Panel** with scrollable text area
- **Banner Display Areas** for header, footer, and sidebar banners
- **Ad Display Area** for full-screen ads

### Script References

All UI elements are automatically connected to the `BidscubeExampleScene` script:

- Button click events
- Text components for status and logging
- ScrollRect for log display
- RectTransforms for banner areas

## 🎯 Scene Structure After Setup

```
BidscubeExampleScene
├── Main Camera
└── BidscubeExampleCanvas
    ├── EventSystem
    ├── BidscubeExampleController (with BidscubeExampleScene script)
    ├── MainPanel
    │   ├── Title
    │   ├── StatusText
    │   ├── InitButton
    │   ├── AdButtonsPanel
    │   │   ├── ImageAdButton
    │   │   ├── VideoAdButton
    │   │   └── NativeAdButton
    │   ├── BannerButtonsPanel
    │   │   ├── HeaderBannerButton
    │   │   ├── FooterBannerButton
    │   │   ├── SidebarBannerButton
    │   │   └── CustomBannerButton
    │   ├── OtherButtonsPanel
    │   │   ├── ConsentButton
    │   │   └── RemoveAllBannersButton
    │   └── LogPanel
    │       ├── LogTitle
    │       └── LogScrollView
    │           └── LogText
    ├── BannerAreas
    │   ├── HeaderBannerArea
    │   ├── FooterBannerArea
    │   └── SidebarBannerArea
    └── AdDisplayArea
```

## 🔧 Configuration

After setup, you can configure the SDK in the `BidscubeExampleScene` script:

- **Placement ID**: `test_placement_123` (default)
- **Base URL**: `Constants.baseURL` (default)
- **Debug Mode**: Enabled by default
- **Logging**: Enabled by default

## 🧪 Testing

Once setup is complete:

1. **Click "Initialize SDK"** to start
2. **Test different ad types** using the buttons
3. **Test banner ads** in different positions
4. **Check the log panel** for real-time feedback
5. **Use "Remove All Banners"** to clear banner ads

## 🐛 Troubleshooting

### "No Canvas found in scene"

- Make sure you have a Canvas in your scene
- The Canvas should be named "BidscubeExampleCanvas"

### "No BidscubeExampleScene controller found"

- Make sure you have a GameObject with the `BidscubeExampleScene` script
- The script should be attached to a child of the Canvas

### UI elements not working

- Check that all script references are properly set
- Verify that the `BidscubeExampleScene` script has all UI references assigned
- Make sure the Canvas has an EventSystem

### Buttons not responding

- Ensure the Canvas has a GraphicRaycaster component
- Check that the EventSystem is present in the scene
- Verify that UI elements are on the correct layer (UI layer)

## 📝 Notes

- The setup scripts use reflection to automatically connect UI elements to the script
- All UI elements are created with proper positioning and styling
- Banner areas are positioned according to the SCENE_SETUP_GUIDE specifications
- The scene is designed to work with the reference resolution of 1920x1080
- All buttons have proper event handlers connected automatically

## 🎨 Customization

After setup, you can customize:

- Colors and styling of UI elements
- Button text and labels
- Banner area sizes and positions
- Log panel appearance
- Overall layout and spacing

The setup scripts provide a solid foundation that you can build upon for your specific needs.

