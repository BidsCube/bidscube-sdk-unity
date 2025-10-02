# Scene Reference System Guide

## 🎯 Overview

The SceneManager now uses **SceneReference** objects instead of string names for type-safe scene loading. This provides better compile-time safety and avoids string-based scene loading issues.

## 🏗️ New Architecture

### **SceneReference Class**

```csharp
[System.Serializable]
public class SceneReference
{
    [SerializeField] private string sceneName;
    [SerializeField] private int sceneIndex = -1;

    public string SceneName => sceneName;
    public int SceneIndex => sceneIndex;
    public bool IsValid => !string.IsNullOrEmpty(sceneName) || sceneIndex >= 0;

    public void LoadScene() { /* Implementation */ }
}
```

### **SceneType Enum**

```csharp
public enum SceneType
{
    Main,
    SDKTest,
    ConsentTest,
    WindowedAd
}
```

### **Updated SceneManager**

```csharp
public class SceneManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SceneReference mainScene;
    [SerializeField] private SceneReference sdkTestScene;
    [SerializeField] private SceneReference consentTestScene;
    [SerializeField] private SceneReference windowedAdScene;

    // Load methods now use SceneReference objects
    public void LoadMainScene() { LoadScene(mainScene); }
    public void LoadSDKTestScene() { LoadScene(sdkTestScene); }
    public void LoadConsentTestScene() { LoadScene(consentTestScene); }
    public void LoadWindowedAdScene() { LoadScene(windowedAdScene); }
}
```

## 🛠️ Setup Instructions

### **Step 1: Configure Scene References**

1. **Select the GameObject** with the SceneManager script
2. **In the Inspector**, you'll see the Scene References section
3. **For each scene reference**, you can set either:
   - **Scene Name**: Enter the scene name (e.g., "SDK Test Scene")
   - **Scene Index**: Enter the build index (e.g., 1, 2, 3)

### **Step 2: Scene Reference Configuration**

#### **Option 1: Using Scene Names (Recommended)**

```
Main Scene:
- Scene Name: "BidscubeExampleScene"

SDK Test Scene:
- Scene Name: "SDK Test Scene"

Consent Test Scene:
- Scene Name: "Consent Test Scene"

Windowed Ad Scene:
- Scene Name: "Windowed Ad Scene"
```

#### **Option 2: Using Scene Indices**

```
Main Scene:
- Scene Index: 0

SDK Test Scene:
- Scene Index: 1

Consent Test Scene:
- Scene Index: 2

Windowed Ad Scene:
- Scene Index: 3
```

### **Step 3: Build Settings Setup**

1. **Open Build Settings** (File → Build Settings)
2. **Add all scenes** in the correct order:
   - Index 0: BidscubeExampleScene
   - Index 1: SDK Test Scene
   - Index 2: Consent Test Scene
   - Index 3: Windowed Ad Scene

## 🎮 Usage Examples

### **Basic Scene Loading**

```csharp
// Get the SceneManager component
var sceneManager = GetComponent<SceneManager>();

// Load scenes using the new system
sceneManager.LoadMainScene();
sceneManager.LoadSDKTestScene();
sceneManager.LoadConsentTestScene();
sceneManager.LoadWindowedAdScene();
```

### **Advanced Usage**

```csharp
// Check if scene reference is valid
if (sceneManager.IsSceneReferenceValid(sceneManager.GetSceneReference(SceneType.Main)))
{
    sceneManager.LoadMainScene();
}

// Get scene reference by type
var mainSceneRef = sceneManager.GetSceneReference(SceneType.Main);
if (mainSceneRef != null && mainSceneRef.IsValid)
{
    mainSceneRef.LoadScene();
}
```

### **Button Click Handlers**

```csharp
// In your UI button click handlers
public void OnSDKTestButtonClicked()
{
    GetComponent<SceneManager>()?.LoadSDKTestScene();
}

public void OnConsentTestButtonClicked()
{
    GetComponent<SceneManager>()?.LoadConsentTestScene();
}

public void OnWindowedAdButtonClicked()
{
    GetComponent<SceneManager>()?.LoadWindowedAdScene();
}
```

## 🔧 Benefits of Scene References

### **1. Type Safety**

- **Compile-time checking** instead of runtime errors
- **IntelliSense support** for scene names
- **Refactoring safety** when renaming scenes

### **2. Better Error Handling**

- **Null reference checking** before loading
- **Validation** of scene references
- **Clear error messages** when scenes fail to load

### **3. Flexibility**

- **Support for both** scene names and indices
- **Easy configuration** in the Inspector
- **Runtime validation** of scene references

### **4. Maintainability**

- **Centralized scene management**
- **Easy to update** scene references
- **Consistent loading behavior**

## 🐛 Troubleshooting

### **Common Issues:**

1. **"Scene reference is null" error:**

   - Check that SceneManager script is attached
   - Verify scene references are assigned in Inspector

2. **"Scene reference is not valid" error:**

   - Ensure scene name is correct
   - Check that scene is in Build Settings
   - Verify scene index is valid (if using indices)

3. **Scene doesn't load:**
   - Check Build Settings has all scenes
   - Verify scene names match exactly
   - Ensure no compilation errors

### **Debug Tips:**

```csharp
// Debug scene reference validity
var sceneRef = GetComponent<SceneManager>().GetSceneReference(SceneType.Main);
Debug.Log($"Scene Name: {sceneRef?.SceneName}");
Debug.Log($"Scene Index: {sceneRef?.SceneIndex}");
Debug.Log($"Is Valid: {sceneRef?.IsValid}");
```

## 📝 Migration from String-Based System

### **Old System (String-Based):**

```csharp
[SerializeField] private string mainSceneName = "BidscubeExampleScene";
// ...
UnityEngine.SceneManagement.SceneManager.LoadScene(mainSceneName);
```

### **New System (Scene Reference-Based):**

```csharp
[SerializeField] private SceneReference mainScene;
// ...
mainScene.LoadScene();
```

### **Migration Steps:**

1. **Replace string fields** with SceneReference fields
2. **Update load methods** to use SceneReference objects
3. **Configure scene references** in the Inspector
4. **Test scene loading** to ensure everything works

## ✅ Best Practices

### **1. Use Scene Names for Flexibility**

- Scene names are more readable
- Easier to maintain when scenes are reordered
- Less prone to errors when build order changes

### **2. Validate Scene References**

- Always check if scene references are valid before loading
- Provide fallback behavior for invalid references
- Log errors for debugging

### **3. Centralize Scene Management**

- Use a single SceneManager per scene
- Keep scene references organized
- Use consistent naming conventions

### **4. Error Handling**

- Implement proper error handling for scene loading
- Provide user feedback for failed scene loads
- Log detailed error information for debugging

This new scene reference system provides a more robust and maintainable approach to scene management in Unity!

