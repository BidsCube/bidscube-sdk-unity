using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BidscubeSDK;

namespace BidscubeSDK.Examples
{
    /// <summary>
    /// Helper script to automatically create the Bidscube test scene
    /// Run this in the Unity editor to set up the complete test scene
    /// </summary>
    public class SceneSetupHelper : MonoBehaviour
    {
        [ContextMenu("Create Bidscube Test Scene")]
        public void CreateTestScene()
        {
            // Create main canvas
            var canvas = CreateCanvas();
            
            // Create main controller
            var controller = CreateController(canvas);
            
            // Create UI elements
            CreateUIElements(canvas, controller);
            
            Debug.Log("✅ Bidscube test scene created successfully!");
        }

        [ContextMenu("Setup Existing Canvas")]
        public void SetupExistingCanvas()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas found in scene!");
                return;
            }

            var controller = canvas.GetComponentInChildren<BidscubeExampleScene>();
            if (controller == null)
            {
                Debug.LogError("No BidscubeExampleScene controller found in canvas!");
                return;
            }

            CreateUIElements(canvas.gameObject, controller);
            Debug.Log("✅ Existing canvas setup completed!");
        }

        private GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("BidscubeExampleCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            return canvasObj;
        }

        private BidscubeExampleScene CreateController(GameObject canvas)
        {
            var controllerObj = new GameObject("BidscubeExampleController");
            controllerObj.transform.SetParent(canvas.transform);
            
            var controller = controllerObj.AddComponent<BidscubeExampleScene>();
            return controller;
        }

        public void CreateUIElements(GameObject canvas, BidscubeExampleScene controller)
        {
            // Create main panel
            var mainPanel = CreatePanel("MainPanel", canvas.transform);
            var mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = Vector2.zero;
            mainPanelRect.anchorMax = Vector2.one;
            mainPanelRect.offsetMin = Vector2.zero;
            mainPanelRect.offsetMax = Vector2.zero;
            
            // Create title
            var title = CreateText("Title", mainPanel.transform, "Bidscube Unity SDK Test");
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.fontSize = 24;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            
            // Create status text
            var statusText = CreateText("StatusText", mainPanel.transform, "Status: Ready to initialize SDK");
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.85f);
            statusRect.anchorMax = new Vector2(1, 0.9f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            statusText.fontSize = 14;
            statusText.alignment = TextAlignmentOptions.Center;
            
            // Create initialize button
            var initButton = CreateButton("InitButton", mainPanel.transform, "Initialize SDK");
            var initRect = initButton.GetComponent<RectTransform>();
            initRect.anchorMin = new Vector2(0.4f, 0.8f);
            initRect.anchorMax = new Vector2(0.6f, 0.85f);
            initRect.offsetMin = Vector2.zero;
            initRect.offsetMax = Vector2.zero;
            
            // Create ad buttons panel
            var adPanel = CreatePanel("AdButtonsPanel", mainPanel.transform);
            var adPanelRect = adPanel.GetComponent<RectTransform>();
            adPanelRect.anchorMin = new Vector2(0.1f, 0.65f);
            adPanelRect.anchorMax = new Vector2(0.9f, 0.75f);
            adPanelRect.offsetMin = Vector2.zero;
            adPanelRect.offsetMax = Vector2.zero;
            
            var adTitle = CreateText("AdTitle", adPanel.transform, "Ad Types");
            adTitle.fontSize = 16;
            adTitle.fontStyle = FontStyles.Bold;
            adTitle.alignment = TextAlignmentOptions.Center;
            
            var imageButton = CreateButton("ImageAdButton", adPanel.transform, "Show Image Ad");
            var videoButton = CreateButton("VideoAdButton", adPanel.transform, "Show Video Ad");
            var nativeButton = CreateButton("NativeAdButton", adPanel.transform, "Show Native Ad");
            
            // Position ad buttons
            PositionButtonsHorizontally(new[] { imageButton, videoButton, nativeButton }, adPanel.transform);
            
            // Create banner buttons panel
            var bannerPanel = CreatePanel("BannerButtonsPanel", mainPanel.transform);
            var bannerPanelRect = bannerPanel.GetComponent<RectTransform>();
            bannerPanelRect.anchorMin = new Vector2(0.1f, 0.5f);
            bannerPanelRect.anchorMax = new Vector2(0.9f, 0.6f);
            bannerPanelRect.offsetMin = Vector2.zero;
            bannerPanelRect.offsetMax = Vector2.zero;
            
            var bannerTitle = CreateText("BannerTitle", bannerPanel.transform, "Banners");
            bannerTitle.fontSize = 16;
            bannerTitle.fontStyle = FontStyles.Bold;
            bannerTitle.alignment = TextAlignmentOptions.Center;
            
            var headerButton = CreateButton("HeaderBannerButton", bannerPanel.transform, "Header");
            var footerButton = CreateButton("FooterBannerButton", bannerPanel.transform, "Footer");
            var sidebarButton = CreateButton("SidebarBannerButton", bannerPanel.transform, "Sidebar");
            var customButton = CreateButton("CustomBannerButton", bannerPanel.transform, "Custom");
            
            // Position banner buttons
            PositionButtonsHorizontally(new[] { headerButton, footerButton, sidebarButton, customButton }, bannerPanel.transform);
            
            // Create other buttons panel
            var otherPanel = CreatePanel("OtherButtonsPanel", mainPanel.transform);
            var otherPanelRect = otherPanel.GetComponent<RectTransform>();
            otherPanelRect.anchorMin = new Vector2(0.1f, 0.35f);
            otherPanelRect.anchorMax = new Vector2(0.9f, 0.45f);
            otherPanelRect.offsetMin = Vector2.zero;
            otherPanelRect.offsetMax = Vector2.zero;
            
            var consentButton = CreateButton("ConsentButton", otherPanel.transform, "Consent Form");
            var removeButton = CreateButton("RemoveAllBannersButton", otherPanel.transform, "Remove All Banners");
            
            // Position other buttons
            PositionButtonsHorizontally(new[] { consentButton, removeButton }, otherPanel.transform);
            
            // Create navigation panel
            var navigationPanel = CreatePanel("NavigationPanel", mainPanel.transform);
            var navigationPanelRect = navigationPanel.GetComponent<RectTransform>();
            navigationPanelRect.anchorMin = new Vector2(0.1f, 0.25f);
            navigationPanelRect.anchorMax = new Vector2(0.9f, 0.35f);
            navigationPanelRect.offsetMin = Vector2.zero;
            navigationPanelRect.offsetMax = Vector2.zero;
            
            var navigationTitle = CreateText("NavigationTitle", navigationPanel.transform, "Navigate to Test Scenes");
            navigationTitle.fontSize = 16;
            navigationTitle.fontStyle = FontStyles.Bold;
            navigationTitle.alignment = TextAlignmentOptions.Center;
            
            var sdkTestButton = CreateButton("SDKTestButton", navigationPanel.transform, "SDK Test");
            var consentTestButton = CreateButton("ConsentTestButton", navigationPanel.transform, "Consent Test");
            var windowedAdButton = CreateButton("WindowedAdButton", navigationPanel.transform, "Windowed Ad");
            
            // Position navigation buttons
            PositionButtonsHorizontally(new[] { sdkTestButton, consentTestButton, windowedAdButton }, navigationPanel.transform);
            
            // Create log panel
            var logPanel = CreatePanel("LogPanel", mainPanel.transform);
            var logPanelRect = logPanel.GetComponent<RectTransform>();
            logPanelRect.anchorMin = new Vector2(0.1f, 0.05f);
            logPanelRect.anchorMax = new Vector2(0.9f, 0.3f);
            logPanelRect.offsetMin = Vector2.zero;
            logPanelRect.offsetMax = Vector2.zero;
            
            var logTitle = CreateText("LogTitle", logPanel.transform, "SDK Log");
            logTitle.fontSize = 16;
            logTitle.fontStyle = FontStyles.Bold;
            logTitle.alignment = TextAlignmentOptions.Center;
            
            var logScrollView = CreateScrollView("LogScrollView", logPanel.transform);
            var logText = CreateText("LogText", logScrollView.transform, "");
            logText.fontSize = 12;
            logText.alignment = TextAlignmentOptions.TopLeft;
            
            // Create banner areas
            var bannerAreasPanel = CreatePanel("BannerAreas", canvas.transform);
            var bannerAreasRect = bannerAreasPanel.GetComponent<RectTransform>();
            bannerAreasRect.anchorMin = Vector2.zero;
            bannerAreasRect.anchorMax = Vector2.one;
            bannerAreasRect.offsetMin = Vector2.zero;
            bannerAreasRect.offsetMax = Vector2.zero;
            
            // Header banner area
            var headerBannerArea = CreateBannerArea("HeaderBannerArea", bannerAreasPanel.transform);
            var headerRect = headerBannerArea.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.95f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = new Vector2(0, -25);
            headerRect.offsetMax = new Vector2(0, 0);
            
            // Footer banner area
            var footerBannerArea = CreateBannerArea("FooterBannerArea", bannerAreasPanel.transform);
            var footerRect = footerBannerArea.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0.05f);
            footerRect.offsetMin = new Vector2(0, 0);
            footerRect.offsetMax = new Vector2(0, 25);
            
            // Sidebar banner area
            var sidebarBannerArea = CreateBannerArea("SidebarBannerArea", bannerAreasPanel.transform);
            var sidebarRect = sidebarBannerArea.GetComponent<RectTransform>();
            sidebarRect.anchorMin = new Vector2(0.95f, 0);
            sidebarRect.anchorMax = new Vector2(1, 1);
            sidebarRect.offsetMin = new Vector2(-60, 0);
            sidebarRect.offsetMax = new Vector2(0, 0);
            
            // Ad display area
            var adDisplayArea = CreatePanel("AdDisplayArea", canvas.transform);
            var adDisplayRect = adDisplayArea.GetComponent<RectTransform>();
            adDisplayRect.anchorMin = Vector2.zero;
            adDisplayRect.anchorMax = Vector2.one;
            adDisplayRect.offsetMin = Vector2.zero;
            adDisplayRect.offsetMax = Vector2.zero;
            adDisplayArea.SetActive(false); // Hidden by default
            
            // Set up controller references
            SetControllerReferences(controller, statusText, logScrollView, logText, 
                initButton, imageButton, videoButton, nativeButton,
                headerButton, footerButton, sidebarButton, customButton,
                consentButton, removeButton, headerBannerArea, footerBannerArea, sidebarBannerArea,
                sdkTestButton, consentTestButton, windowedAdButton);
        }

        private GameObject CreatePanel(string name, Transform parent)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);
            
            var rectTransform = panel.AddComponent<RectTransform>();
            var image = panel.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.1f);
            
            return panel;
        }

        private GameObject CreateBannerArea(string name, Transform parent)
        {
            var bannerArea = new GameObject(name);
            bannerArea.transform.SetParent(parent);
            
            var rectTransform = bannerArea.AddComponent<RectTransform>();
            var image = bannerArea.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.1f); // Semi-transparent for visibility
            
            return bannerArea;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string text)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            
            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            return textComponent;
        }

        private Button CreateButton(string name, Transform parent, string text)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent);
            
            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 30);
            
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.6f, 0.9f, 1f);
            
            var button = buttonObj.AddComponent<Button>();
            
            var buttonText = new GameObject("Text");
            buttonText.transform.SetParent(buttonObj.transform);
            
            var textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var textComponent = buttonText.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            return button;
        }

        private GameObject CreateScrollView(string name, Transform parent)
        {
            var scrollView = new GameObject(name);
            scrollView.transform.SetParent(parent);
            
            var rectTransform = scrollView.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0.8f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            var image = scrollView.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.5f);
            
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0.1f);
            
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            scrollRect.viewport = viewportRect;
            
            return scrollView;
        }

        private void PositionButtonsHorizontally(Button[] buttons, Transform parent)
        {
            var buttonWidth = 1f / buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                var rect = buttons[i].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(i * buttonWidth, 0.1f);
                rect.anchorMax = new Vector2((i + 1) * buttonWidth, 0.9f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private void SetControllerReferences(BidscubeExampleScene controller, TextMeshProUGUI statusText, GameObject logScrollView, TextMeshProUGUI logText,
            Button initButton, Button imageButton, Button videoButton, Button nativeButton,
            Button headerButton, Button footerButton, Button sidebarButton, Button customButton,
            Button consentButton, Button removeButton, GameObject headerBannerArea, GameObject footerBannerArea, GameObject sidebarBannerArea,
            Button sdkTestButton, Button consentTestButton, Button windowedAdButton)
        {
            // Use reflection to set private fields
            var controllerType = typeof(BidscubeExampleScene);
            
            SetField(controller, "_statusText", statusText);
            SetField(controller, "_logScrollRect", logScrollView.GetComponent<ScrollRect>());
            SetField(controller, "_logText", logText);
            SetField(controller, "_initButton", initButton);
            SetField(controller, "_imageAdButton", imageButton);
            SetField(controller, "_videoAdButton", videoButton);
            SetField(controller, "_nativeAdButton", nativeButton);
            SetField(controller, "_headerBannerButton", headerButton);
            SetField(controller, "_footerBannerButton", footerButton);
            SetField(controller, "_sidebarBannerButton", sidebarButton);
            SetField(controller, "_customBannerButton", customButton);
            SetField(controller, "_consentButton", consentButton);
            SetField(controller, "_removeAllBannersButton", removeButton);
            SetField(controller, "_headerBannerArea", headerBannerArea.GetComponent<RectTransform>());
            SetField(controller, "_footerBannerArea", footerBannerArea.GetComponent<RectTransform>());
            SetField(controller, "_sidebarBannerArea", sidebarBannerArea.GetComponent<RectTransform>());
            SetField(controller, "_sdkTestButton", sdkTestButton);
            SetField(controller, "_consentTestButton", consentTestButton);
            SetField(controller, "_windowedAdButton", windowedAdButton);
        }

        private void SetField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}



