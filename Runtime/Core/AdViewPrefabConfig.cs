using UnityEngine;
using UnityEngine.UI;

namespace BidscubeSDK
{
    /// <summary>
    /// Configuration for ad view prefabs and predefined UI elements
    /// This allows you to predefine GameObjects and RectTransforms for runtime-spawned ad views
    /// </summary>
    [CreateAssetMenu(fileName = "AdViewPrefabConfig", menuName = "BidscubeSDK/Ad View Prefab Config")]
    public class AdViewPrefabConfig : ScriptableObject
    {
        [Header("Video Ad View Prefabs")]
        [Tooltip("Prefab for VideoAdView. If set, will be instantiated instead of creating from scratch.")]
        public GameObject videoAdViewPrefab;
        
        [Tooltip("Prefab for skip button in video ads.")]
        public GameObject videoSkipButtonPrefab;
        
        [Tooltip("Prefab for close button in video ads.")]
        public GameObject videoCloseButtonPrefab;
        
        [Tooltip("Prefab for click button in video ads.")]
        public GameObject videoClickButtonPrefab;
        
        [Header("Banner Ad View Prefabs")]
        [Tooltip("Prefab for BannerAdView. If set, will be instantiated instead of creating from scratch.")]
        public GameObject bannerAdViewPrefab;
        
        [Tooltip("Prefab for click button in banner ads.")]
        public GameObject bannerClickButtonPrefab;
        
        [Header("Native Ad View Prefabs")]
        [Tooltip("Prefab for NativeAdView. If set, will be instantiated instead of creating from scratch.")]
        public GameObject nativeAdViewPrefab;
        
        [Tooltip("Prefab for install button in native ads.")]
        public GameObject nativeInstallButtonPrefab;
        
        [Header("Common UI Elements")]
        [Tooltip("Prefab for creating custom UI elements (buttons, images, etc.).")]
        public GameObject[] customUIElementPrefabs;
        
        private static AdViewPrefabConfig _instance;
        
        /// <summary>
        /// Get the current prefab configuration instance
        /// </summary>
        public static AdViewPrefabConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find in resources
                    _instance = Resources.Load<AdViewPrefabConfig>("AdViewPrefabConfig");
                    
                    // If not found, create a default one
                    if (_instance == null)
                    {
                        Logger.Info("[AdViewPrefabConfig] No prefab config found in Resources, using defaults");
                    }
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        
        /// <summary>
        /// Create a UI element from prefab or create a new GameObject with RectTransform
        /// </summary>
        public static GameObject CreateUIElement(string name, Transform parent, GameObject prefab = null)
        {
            GameObject element;
            
            if (prefab != null)
            {
                element = Instantiate(prefab, parent);
                element.name = name;
                Logger.Info($"[AdViewPrefabConfig] Created UI element '{name}' from prefab");
            }
            else
            {
                element = new GameObject(name);
                element.transform.SetParent(parent, false);
                
                // Add RectTransform for UI elements
                var rectTransform = element.AddComponent<RectTransform>();
                rectTransform.localScale = Vector3.one;
                
                Logger.Info($"[AdViewPrefabConfig] Created UI element '{name}' dynamically");
            }
            
            return element;
        }
        
        /// <summary>
        /// Create a button with RectTransform
        /// </summary>
        public static Button CreateButton(string name, Transform parent, GameObject prefab = null)
        {
            var buttonObj = CreateUIElement(name, parent, prefab);
            var button = buttonObj.GetComponent<Button>();
            
            if (button == null)
            {
                button = buttonObj.AddComponent<Button>();
            }
            
            // Ensure RectTransform is set up
            var rectTransform = buttonObj.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = buttonObj.AddComponent<RectTransform>();
            }
            
            return button;
        }
        
        /// <summary>
        /// Create an image with RectTransform
        /// </summary>
        public static Image CreateImage(string name, Transform parent, GameObject prefab = null)
        {
            var imageObj = CreateUIElement(name, parent, prefab);
            var image = imageObj.GetComponent<Image>();
            
            if (image == null)
            {
                image = imageObj.AddComponent<Image>();
            }
            
            return image;
        }
        
        /// <summary>
        /// Create a text element with RectTransform
        /// </summary>
        public static Text CreateText(string name, Transform parent, GameObject prefab = null)
        {
            var textObj = CreateUIElement(name, parent, prefab);
            var text = textObj.GetComponent<Text>();
            
            if (text == null)
            {
                text = textObj.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            
            return text;
        }
    }
}

