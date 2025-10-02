using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BidscubeSDK;

namespace BidscubeSDK.Examples
{
    /// <summary>
    /// Setup script for Windowed Ad Test Scene - automatically creates UI elements
    /// Based on iOS WindowedAdTestView structure
    /// </summary>
    public class WindowedAdTestSceneSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createNavigationButtons = true;

        [ContextMenu("Setup Windowed Ad Test Scene")]
        public void SetupWindowedAdTestScene()
        {
            Debug.Log("Setting up Windowed Ad Test Scene...");

            // Create main canvas
            var canvas = CreateCanvas();
            
            // Create main controller
            var controller = CreateController(canvas);
            
            // Create UI elements
            CreateUIElements(canvas, controller);
            
            Debug.Log("✅ Windowed Ad Test Scene setup completed!");
        }

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupWindowedAdTestScene();
            }
        }

        private GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("WindowedAdTestCanvas");
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

        private WindowedAdTestScene CreateController(GameObject canvas)
        {
            var controllerObj = new GameObject("WindowedAdTestController");
            controllerObj.transform.SetParent(canvas.transform);
            
            var controller = controllerObj.AddComponent<WindowedAdTestScene>();
            return controller;
        }

        private void CreateUIElements(GameObject canvas, WindowedAdTestScene controller)
        {
            // Create main panel
            var mainPanel = CreatePanel("MainPanel", canvas.transform);
            var mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = Vector2.zero;
            mainPanelRect.anchorMax = Vector2.one;
            mainPanelRect.offsetMin = Vector2.zero;
            mainPanelRect.offsetMax = Vector2.zero;
            
            // Create title
            var title = CreateText("Title", mainPanel.transform, "Windowed Ad Test Scene");
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.fontSize = 24;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            
            // Create SDK status text
            var statusText = CreateText("SDKStatusText", mainPanel.transform, "SDK Status: Not Initialized");
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.85f);
            statusRect.anchorMax = new Vector2(1, 0.9f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            statusText.fontSize = 16;
            statusText.alignment = TextAlignmentOptions.Center;
            
            // Create initialize button
            var initButton = CreateButton("InitializeButton", mainPanel.transform, "Initialize SDK");
            var initRect = initButton.GetComponent<RectTransform>();
            initRect.anchorMin = new Vector2(0.4f, 0.8f);
            initRect.anchorMax = new Vector2(0.6f, 0.85f);
            initRect.offsetMin = Vector2.zero;
            initRect.offsetMax = Vector2.zero;
            
            // Create cleanup button
            var cleanupButton = CreateButton("CleanupButton", mainPanel.transform, "Cleanup SDK");
            var cleanupRect = cleanupButton.GetComponent<RectTransform>();
            cleanupRect.anchorMin = new Vector2(0.7f, 0.8f);
            cleanupRect.anchorMax = new Vector2(0.9f, 0.85f);
            cleanupRect.offsetMin = Vector2.zero;
            cleanupRect.offsetMax = Vector2.zero;
            
            // Create ad creation panel
            var adCreationPanel = CreatePanel("AdCreationPanel", mainPanel.transform);
            var adCreationPanelRect = adCreationPanel.GetComponent<RectTransform>();
            adCreationPanelRect.anchorMin = new Vector2(0.1f, 0.7f);
            adCreationPanelRect.anchorMax = new Vector2(0.9f, 0.8f);
            adCreationPanelRect.offsetMin = Vector2.zero;
            adCreationPanelRect.offsetMax = Vector2.zero;
            
            var adCreationTitle = CreateText("AdCreationTitle", adCreationPanel.transform, "Ad Creation");
            adCreationTitle.fontSize = 18;
            adCreationTitle.fontStyle = FontStyles.Bold;
            adCreationTitle.alignment = TextAlignmentOptions.Center;
            
            var createImageButton = CreateButton("CreateImageAdButton", adCreationPanel.transform, "Create Image Ad");
            var createVideoButton = CreateButton("CreateVideoAdButton", adCreationPanel.transform, "Create Video Ad");
            var createNativeButton = CreateButton("CreateNativeAdButton", adCreationPanel.transform, "Create Native Ad");
            var validateLayoutButton = CreateButton("ValidateLayoutButton", adCreationPanel.transform, "Validate Layout");
            
            // Position ad creation buttons
            PositionButtonsHorizontally(new[] { createImageButton, createVideoButton, createNativeButton, validateLayoutButton }, adCreationPanel.transform);
            
            // Create positioning panel
            var positioningPanel = CreatePanel("PositioningPanel", mainPanel.transform);
            var positioningPanelRect = positioningPanel.GetComponent<RectTransform>();
            positioningPanelRect.anchorMin = new Vector2(0.1f, 0.55f);
            positioningPanelRect.anchorMax = new Vector2(0.9f, 0.7f);
            positioningPanelRect.offsetMin = Vector2.zero;
            positioningPanelRect.offsetMax = Vector2.zero;
            
            var positioningTitle = CreateText("PositioningTitle", positioningPanel.transform, "AD POSITIONING");
            positioningTitle.fontSize = 18;
            positioningTitle.fontStyle = FontStyles.Bold;
            positioningTitle.alignment = TextAlignmentOptions.Center;
            
            // Create position buttons
            var unknownButton = CreateButton("UnknownPositionButton", positioningPanel.transform, "UNKNOWN");
            var aboveFoldButton = CreateButton("AboveTheFoldButton", positioningPanel.transform, "ABOVE_THE_FOLD");
            var dependScreenButton = CreateButton("DependOnScreenSizeButton", positioningPanel.transform, "DEPEND_ON_SCREEN_SIZE");
            var belowFoldButton = CreateButton("BelowTheFoldButton", positioningPanel.transform, "BELOW_THE_FOLD");
            var headerButton = CreateButton("HeaderButton", positioningPanel.transform, "HEADER");
            var footerButton = CreateButton("FooterButton", positioningPanel.transform, "FOOTER");
            var sidebarButton = CreateButton("SidebarButton", positioningPanel.transform, "SIDEBAR");
            var fullScreenButton = CreateButton("FullScreenButton", positioningPanel.transform, "FULL_SCREEN");
            
            // Position position buttons in grid
            PositionButtonsInGrid(new[] { unknownButton, aboveFoldButton, dependScreenButton, belowFoldButton,
                headerButton, footerButton, sidebarButton, fullScreenButton }, positioningPanel.transform, 4);
            
            // Create positioning panel toggle
            var positioningToggle = CreateToggle("ShowPositioningPanelToggle", mainPanel.transform, "Show Positioning Panel");
            var toggleRect = positioningToggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.1f, 0.5f);
            toggleRect.anchorMax = new Vector2(0.3f, 0.55f);
            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;
            
            // Create log panel
            var logPanel = CreatePanel("LogPanel", mainPanel.transform);
            var logPanelRect = logPanel.GetComponent<RectTransform>();
            logPanelRect.anchorMin = new Vector2(0.1f, 0.05f);
            logPanelRect.anchorMax = new Vector2(0.9f, 0.25f);
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
            
            // Create ad display area
            var adDisplayArea = CreatePanel("AdDisplayArea", mainPanel.transform);
            var adDisplayRect = adDisplayArea.GetComponent<RectTransform>();
            adDisplayRect.anchorMin = new Vector2(0.1f, 0.3f);
            adDisplayRect.anchorMax = new Vector2(0.9f, 0.5f);
            adDisplayRect.offsetMin = Vector2.zero;
            adDisplayRect.offsetMax = Vector2.zero;
            adDisplayArea.SetActive(false); // Hidden by default
            
            // Create content area for testing
            var contentArea = CreatePanel("ContentArea", mainPanel.transform);
            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.3f);
            contentRect.anchorMax = new Vector2(0.9f, 0.5f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            // Add some sample content
            for (int i = 0; i < 5; i++)
            {
                var contentItem = CreateText($"ContentItem{i}", contentArea.transform, $"Content Section {i + 1}");
                var itemRect = contentItem.GetComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, (4f - i) / 5f);
                itemRect.anchorMax = new Vector2(1, (5f - i) / 5f);
                itemRect.offsetMin = Vector2.zero;
                itemRect.offsetMax = Vector2.zero;
                contentItem.fontSize = 16;
                contentItem.color = Color.gray;
            }
            
            // Create navigation buttons if requested
            if (createNavigationButtons)
            {
                CreateNavigationButtons(mainPanel.transform);
            }
            
            // Set up controller references
            SetControllerReferences(controller, statusText, logScrollView, logText,
                initButton, cleanupButton, createImageButton, createVideoButton, createNativeButton, validateLayoutButton,
                positioningToggle, positioningPanel, adDisplayArea, contentArea,
                unknownButton, aboveFoldButton, dependScreenButton, belowFoldButton,
                headerButton, footerButton, sidebarButton, fullScreenButton);
        }

        private void CreateNavigationButtons(Transform parent)
        {
            var navPanel = CreatePanel("NavigationPanel", parent);
            var navPanelRect = navPanel.GetComponent<RectTransform>();
            navPanelRect.anchorMin = new Vector2(0, 0);
            navPanelRect.anchorMax = new Vector2(1, 0.05f);
            navPanelRect.offsetMin = Vector2.zero;
            navPanelRect.offsetMax = Vector2.zero;
            
            var backButton = CreateButton("BackToMainButton", navPanel.transform, "← Back to Main");
            var sdkButton = CreateButton("SDKTestButton", navPanel.transform, "SDK Test");
            var consentButton = CreateButton("ConsentTestButton", navPanel.transform, "Consent Test");
            
            PositionButtonsHorizontally(new[] { backButton, sdkButton, consentButton }, navPanel.transform);
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
            rectTransform.sizeDelta = new Vector2(150, 40);
            
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
            
            var textComponent = buttonText.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            return button;
        }

        private Toggle CreateToggle(string name, Transform parent, string label)
        {
            var toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(parent);
            
            var rectTransform = toggleObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 20);
            
            var toggle = toggleObj.AddComponent<Toggle>();
            
            var background = new GameObject("Background");
            background.transform.SetParent(toggleObj.transform);
            var bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.sizeDelta = new Vector2(20, 20);
            bgRect.anchoredPosition = new Vector2(10, 0);
            
            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform);
            var checkRect = checkmark.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = Color.green;
            
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(30, 0);
            labelRect.offsetMax = Vector2.zero;
            
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;
            
            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;
            
            return toggle;
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

        private void PositionButtonsInGrid(Button[] buttons, Transform parent, int columns)
        {
            int rows = Mathf.CeilToInt((float)buttons.Length / columns);
            float buttonWidth = 1f / columns;
            float buttonHeight = 1f / rows;
            
            for (int i = 0; i < buttons.Length; i++)
            {
                int row = i / columns;
                int col = i % columns;
                
                var rect = buttons[i].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(col * buttonWidth, 1f - (row + 1) * buttonHeight);
                rect.anchorMax = new Vector2((col + 1) * buttonWidth, 1f - row * buttonHeight);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private void SetControllerReferences(WindowedAdTestScene controller, TextMeshProUGUI statusText, GameObject logScrollView, TextMeshProUGUI logText,
            Button initButton, Button cleanupButton, Button createImageButton, Button createVideoButton, Button createNativeButton, Button validateLayoutButton,
            Toggle positioningToggle, GameObject positioningPanel, GameObject adDisplayArea, GameObject contentArea,
            Button unknownButton, Button aboveFoldButton, Button dependScreenButton, Button belowFoldButton,
            Button headerButton, Button footerButton, Button sidebarButton, Button fullScreenButton)
        {
            // Use reflection to set private fields
            var controllerType = typeof(WindowedAdTestScene);
            
            SetField(controller, "_sdkStatusText", statusText);
            SetField(controller, "_initializeButton", initButton);
            SetField(controller, "_cleanupButton", cleanupButton);
            SetField(controller, "_createImageAdButton", createImageButton);
            SetField(controller, "_createVideoAdButton", createVideoButton);
            SetField(controller, "_createNativeAdButton", createNativeButton);
            SetField(controller, "_validateLayoutButton", validateLayoutButton);
            SetField(controller, "_showPositioningPanelToggle", positioningToggle);
            SetField(controller, "_positioningPanel", positioningPanel.GetComponent<RectTransform>());
            SetField(controller, "_logScrollRect", logScrollView.GetComponent<ScrollRect>());
            SetField(controller, "_logText", logText);
            SetField(controller, "_adDisplayArea", adDisplayArea.GetComponent<RectTransform>());
            SetField(controller, "_contentArea", contentArea.GetComponent<RectTransform>());
            SetField(controller, "_unknownPositionButton", unknownButton);
            SetField(controller, "_aboveTheFoldButton", aboveFoldButton);
            SetField(controller, "_dependOnScreenSizeButton", dependScreenButton);
            SetField(controller, "_belowTheFoldButton", belowFoldButton);
            SetField(controller, "_headerButton", headerButton);
            SetField(controller, "_footerButton", footerButton);
            SetField(controller, "_sidebarButton", sidebarButton);
            SetField(controller, "_fullScreenButton", fullScreenButton);
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
