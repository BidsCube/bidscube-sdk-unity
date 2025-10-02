# Unity Build Settings Setup Guide

## 🎯 Problem

The error "Scene 'SDKTestScene' couldn't be loaded because it has not been added to the active build profile" occurs because Unity scenes need to be added to Build Settings before they can be loaded programmatically.

## 🛠️ Solution: Add Scenes to Build Settings

### **Step 1: Open Build Settings**

1. In Unity, go to **File → Build Settings...**
2. This will open the Build Settings window

### **Step 2: Add All Test Scenes**

You need to add the following scenes to the Build Settings:

#### **Required Scenes:**

1. **BidscubeExampleScene** (Main Scene)

   - Path: `Assets/Scenes/BidscubeExampleScene.unity`
   - This is your main scene with navigation

2. **SDK Test Scene**

   - Path: `Assets/BidscubeSDK/Scenes/SDK Test Scene.unity`
   - For basic SDK functionality testing

3. **Consent Test Scene**

   - Path: `Assets/BidscubeSDK/Scenes/Consent Test Scene.unity`
   - For consent form testing

4. **Windowed Ad Scene**
   - Path: `Assets/BidscubeSDK/Scenes/Windowed Ad Scene.unity`
   - For ad positioning testing

### **Step 3: Add Scenes to Build Settings**

#### **Method 1: Drag and Drop (Recommended)**

1. In the **Project** window, navigate to the scene files
2. **Drag each scene** from the Project window into the **"Scenes In Build"** list in Build Settings
3. **Order matters**: Put `BidscubeExampleScene` first (index 0) as it's the main scene

#### **Method 2: Add Open Scenes**

1. Open each scene in Unity
2. Click **"Add Open Scenes"** in Build Settings
3. This will add the currently open scene to the build

#### **Method 3: Manual Addition**

1. Click **"Add Open Scenes"** for each scene
2. Or use the **"+"** button to browse and add scenes

### **Step 4: Verify Scene Order**

The scenes should appear in this order in Build Settings:

```
Index 0: BidscubeExampleScene (Main Scene)
Index 1: SDK Test Scene
Index 2: Consent Test Scene
Index 3: Windowed Ad Scene
```

### **Step 5: Update SceneManager Script**

Make sure the SceneManager script has the correct scene names:

```csharp
[Header("Scene Names")]
[SerializeField] private string mainSceneName = "BidscubeExampleScene";
[SerializeField] private string sdkTestSceneName = "SDK Test Scene";
[SerializeField] private string consentTestSceneName = "Consent Test Scene";
[SerializeField] private string windowedAdSceneName = "Windowed Ad Scene";
```

## 🔧 Alternative: Create New Scenes

If the scenes don't exist yet, you can create them:

### **Create SDK Test Scene:**

1. **File → New Scene**
2. Save as: `Assets/BidscubeSDK/Scenes/SDK Test Scene.unity`
3. Add `SDKTestScene` script to a GameObject
4. Add `SceneManager` script to the same GameObject

### **Create Consent Test Scene:**

1. **File → New Scene**
2. Save as: `Assets/BidscubeSDK/Scenes/Consent Test Scene.unity`
3. Add `ConsentTestScene` script to a GameObject
4. Add `SceneManager` script to the same GameObject

### **Create Windowed Ad Scene:**

1. **File → New Scene**
2. Save as: `Assets/BidscubeSDK/Scenes/Windowed Ad Scene.unity`
3. Add `WindowedAdTestScene` script to a GameObject
4. Add `SceneManager` script to the same GameObject

## 🎮 Testing Scene Loading

### **Test Navigation:**

1. **Play the main scene** (BidscubeExampleScene)
2. **Click navigation buttons** to switch between scenes
3. **Verify each scene loads** correctly
4. **Test navigation back** to main scene

### **Debug Scene Loading:**

If scenes still don't load, check:

1. **Scene names match exactly** (case-sensitive)
2. **All scenes are in Build Settings**
3. **SceneManager script is attached** to GameObjects
4. **No compilation errors** in the console

## 📝 Scene Manager Configuration

The SceneManager script should be configured like this:

```csharp
public class SceneManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainSceneName = "BidscubeExampleScene";
    [SerializeField] private string sdkTestSceneName = "SDK Test Scene";
    [SerializeField] private string consentTestSceneName = "Consent Test Scene";
    [SerializeField] private string windowedAdSceneName = "Windowed Ad Scene";

    // ... rest of the implementation
}
```

## 🐛 Troubleshooting

### **Common Issues:**

1. **"Scene not found" error:**

   - Check scene names match exactly
   - Verify scenes are in Build Settings
   - Ensure no typos in scene names

2. **"Scene couldn't be loaded" error:**

   - Add scene to Build Settings
   - Check scene file exists
   - Verify scene is not corrupted

3. **Navigation not working:**
   - Check SceneManager script is attached
   - Verify button references are assigned
   - Check for compilation errors

### **Quick Fix:**

1. **Open Build Settings** (File → Build Settings)
2. **Add all scenes** to "Scenes In Build"
3. **Save the project**
4. **Test navigation** again

## ✅ Verification Checklist

- [ ] All 4 scenes are in Build Settings
- [ ] Scene order is correct (main scene first)
- [ ] SceneManager script is attached to GameObjects
- [ ] Scene names match exactly in SceneManager
- [ ] No compilation errors
- [ ] Navigation buttons are assigned
- [ ] Scenes can be loaded manually in Unity

Once all scenes are properly added to Build Settings, the navigation between scenes should work correctly!

