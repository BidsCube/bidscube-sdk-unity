using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BidscubeSDK;

namespace BidscubeSDK.Examples
{
    /// <summary>
    /// Setup script for SDK Test Scene - automatically creates UI elements
    /// Based on iOS SDKTestView structure
    /// </summary>
    public class SDKTestSceneSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createNavigationButtons = true;

        [ContextMenu("Setup SDK Test Scene")]
        public void SetupSDKTestScene()
        {
            Debug.Log("Setting up SDK Test Scene...");

            // Create main canvas
            var canvas = CreateCanvas();
            
            // Create main controller
            var controller = CreateController(canvas);
            
            // Create UI elements
            CreateUIElements(canvas, controller);
            
            Debug.Log("✅ SDK Test Scene setup completed!");
        }

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupSDKTestScene();
            }
        }

        private GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("SDKTestCanvas");
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

        private SDKTestScene CreateController(GameObject canvas)
        {
            var controllerObj = new GameObject("SDKTestController");
            controllerObj.transform.SetParent(canvas.transform);
            
            var controller = controllerObj.AddComponent<SDKTestScene>();
            return controller;
        }

        private void CreateUIElements(GameObject canvas, SDKTestScene controller)
        {
            // Create main panel
            var mainPanel = CreatePanel("MainPanel", canvas.transform);
            var mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = Vector2.zero;
            mainPanelRect.anchorMax = Vector2.one;
            mainPanelRect.offsetMin = Vector2.zero;
            mainPanelRect.offsetMax = Vector2.zero;
            
            // Create title
            var title = CreateText("Title", mainPanel.transform, "SDK Test Scene");
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
            
            // Create placement ID input
            var placementInput = CreateInputField("PlacementIdInput", mainPanel.transform, "19481");
            var placementRect = placementInput.GetComponent<RectTransform>();
            placementRect.anchorMin = new Vector2(0.2f, 0.8f);
            placementRect.anchorMax = new Vector2(0.8f, 0.85f);
            placementRect.offsetMin = Vector2.zero;
            placementRect.offsetMax = Vector2.zero;
            
            var placementLabel = CreateText("PlacementLabel", mainPanel.transform, "Placement ID:");
            var labelRect = placementLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.1f, 0.8f);
            labelRect.anchorMax = new Vector2(0.2f, 0.85f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            placementLabel.fontSize = 14;
            placementLabel.alignment = TextAlignmentOptions.Left;
            
            // Create initialize button
            var initButton = CreateButton("InitializeButton", mainPanel.transform, "Initialize SDK");
            var initRect = initButton.GetComponent<RectTransform>();
            initRect.anchorMin = new Vector2(0.4f, 0.75f);
            initRect.anchorMax = new Vector2(0.6f, 0.8f);
            initRect.offsetMin = Vector2.zero;
            initRect.offsetMax = Vector2.zero;
            
            // Create cleanup button
            var cleanupButton = CreateButton("CleanupButton", mainPanel.transform, "Cleanup SDK");
            var cleanupRect = cleanupButton.GetComponent<RectTransform>();
            cleanupRect.anchorMin = new Vector2(0.7f, 0.75f);
            cleanupRect.anchorMax = new Vector2(0.9f, 0.8f);
            cleanupRect.offsetMin = Vector2.zero;
            cleanupRect.offsetMax = Vector2.zero;
            
            // Create ad type buttons panel
            var adPanel = CreatePanel("AdButtonsPanel", mainPanel.transform);
            var adPanelRect = adPanel.GetComponent<RectTransform>();
            adPanelRect.anchorMin = new Vector2(0.1f, 0.6f);
            adPanelRect.anchorMax = new Vector2(0.9f, 0.7f);
            adPanelRect.offsetMin = Vector2.zero;
            adPanelRect.offsetMax = Vector2.zero;
            
            var adTitle = CreateText("AdTitle", adPanel.transform, "Ad Types");
            adTitle.fontSize = 18;
            adTitle.fontStyle = FontStyles.Bold;
            adTitle.alignment = TextAlignmentOptions.Center;
            
            var imageButton = CreateButton("ImageAdButton", adPanel.transform, "Banner/Image Ads");
            var videoButton = CreateButton("VideoAdButton", adPanel.transform, "Video Ads");
            var nativeButton = CreateButton("NativeAdButton", adPanel.transform, "Native Ads");
            
            // Position ad buttons
            PositionButtonsHorizontally(new[] { imageButton, videoButton, nativeButton }, adPanel.transform);
            
            // Create logging control panel
            var loggingPanel = CreatePanel("LoggingPanel", mainPanel.transform);
            var loggingPanelRect = loggingPanel.GetComponent<RectTransform>();
            loggingPanelRect.anchorMin = new Vector2(0.1f, 0.45f);
            loggingPanelRect.anchorMax = new Vector2(0.9f, 0.55f);
            loggingPanelRect.offsetMin = Vector2.zero;
            loggingPanelRect.offsetMax = Vector2.zero;
            
            var loggingTitle = CreateText("LoggingTitle", loggingPanel.transform, "Logging Control");
            loggingTitle.fontSize = 18;
            loggingTitle.fontStyle = FontStyles.Bold;
            loggingTitle.alignment = TextAlignmentOptions.Center;
            
            var enableLoggingButton = CreateButton("EnableLoggingButton", loggingPanel.transform, "Enable Logging");
            var disableLoggingButton = CreateButton("DisableLoggingButton", loggingPanel.transform, "Disable Logging");
            var testLoggingButton = CreateButton("TestLoggingButton", loggingPanel.transform, "Test Logging");
            
            // Position logging buttons
            PositionButtonsHorizontally(new[] { enableLoggingButton, disableLoggingButton, testLoggingButton }, loggingPanel.transform);
            
            // Create position control panel
            var positionPanel = CreatePanel("PositionPanel", mainPanel.transform);
            var positionPanelRect = positionPanel.GetComponent<RectTransform>();
            positionPanelRect.anchorMin = new Vector2(0.1f, 0.3f);
            positionPanelRect.anchorMax = new Vector2(0.9f, 0.4f);
            positionPanelRect.offsetMin = Vector2.zero;
            positionPanelRect.offsetMax = Vector2.zero;
            
            var positionTitle = CreateText("PositionTitle", positionPanel.transform, "Position Override (Testing Only)");
            positionTitle.fontSize = 16;
            positionTitle.fontStyle = FontStyles.Bold;
            positionTitle.alignment = TextAlignmentOptions.Center;
            
            var manualToggle = CreateToggle("UseManualPositionToggle", positionPanel.transform, "Override Ad Position for Testing");
            var positionDropdown = CreateDropdown("PositionDropdown", positionPanel.transform);
            
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
            adDisplayRect.anchorMin = new Vector2(0.1f, 0.35f);
            adDisplayRect.anchorMax = new Vector2(0.9f, 0.45f);
            adDisplayRect.offsetMin = Vector2.zero;
            adDisplayRect.offsetMax = Vector2.zero;
            adDisplayArea.SetActive(false); // Hidden by default
            
            // Create active banners text
            var activeBannersText = CreateText("ActiveBannersText", mainPanel.transform, "Active Banners: 0");
            var activeBannersRect = activeBannersText.GetComponent<RectTransform>();
            activeBannersRect.anchorMin = new Vector2(0.1f, 0.7f);
            activeBannersRect.anchorMax = new Vector2(0.9f, 0.75f);
            activeBannersRect.offsetMin = Vector2.zero;
            activeBannersRect.offsetMax = Vector2.zero;
            activeBannersText.fontSize = 14;
            activeBannersText.alignment = TextAlignmentOptions.Center;
            activeBannersText.color = Color.blue;
            
            // Create navigation buttons if requested
            if (createNavigationButtons)
            {
                CreateNavigationButtons(mainPanel.transform);
            }
            
            // Set up controller references
            SetControllerReferences(controller, statusText, placementInput, activeBannersText, logScrollView, logText,
                initButton, cleanupButton, imageButton, videoButton, nativeButton,
                enableLoggingButton, disableLoggingButton, testLoggingButton,
                manualToggle, positionDropdown, adDisplayArea);
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
            var consentButton = CreateButton("ConsentTestButton", navPanel.transform, "Consent Test");
            var windowedButton = CreateButton("WindowedAdButton", navPanel.transform, "Windowed Ad");
            
            PositionButtonsHorizontally(new[] { backButton, consentButton, windowedButton }, navPanel.transform);
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

        private TMP_InputField CreateInputField(string name, Transform parent, string placeholder)
        {
            var inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent);
            
            var rectTransform = inputObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 30);
            
            var image = inputObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.text = placeholder;
            
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = placeholder;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;
            
            inputField.textComponent = textComponent;
            
            return inputField;
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

        private Dropdown CreateDropdown(string name, Transform parent)
        {
            var dropdownObj = new GameObject(name);
            dropdownObj.transform.SetParent(parent);
            
            var rectTransform = dropdownObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 30);
            
            var image = dropdownObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var dropdown = dropdownObj.AddComponent<Dropdown>();
            
            // Add options
            dropdown.options.Add(new Dropdown.OptionData("UNKNOWN"));
            dropdown.options.Add(new Dropdown.OptionData("ABOVE_THE_FOLD"));
            dropdown.options.Add(new Dropdown.OptionData("DEPEND_ON_SCREEN_SIZE"));
            dropdown.options.Add(new Dropdown.OptionData("BELOW_THE_FOLD"));
            dropdown.options.Add(new Dropdown.OptionData("HEADER"));
            dropdown.options.Add(new Dropdown.OptionData("FOOTER"));
            dropdown.options.Add(new Dropdown.OptionData("SIDEBAR"));
            dropdown.options.Add(new Dropdown.OptionData("FULL_SCREEN"));
            
            return dropdown;
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

        private void SetControllerReferences(SDKTestScene controller, TextMeshProUGUI statusText, TMP_InputField placementInput, TextMeshProUGUI activeBannersText, GameObject logScrollView, TextMeshProUGUI logText,
            Button initButton, Button cleanupButton, Button imageButton, Button videoButton, Button nativeButton,
            Button enableLoggingButton, Button disableLoggingButton, Button testLoggingButton,
            Toggle manualToggle, Dropdown positionDropdown, GameObject adDisplayArea)
        {
            // Use reflection to set private fields
            var controllerType = typeof(SDKTestScene);
            
            SetField(controller, "_sdkStatusText", statusText);
            SetField(controller, "_placementIdInput", placementInput);
            SetField(controller, "_activeBannersText", activeBannersText);
            SetField(controller, "_initializeButton", initButton);
            SetField(controller, "_cleanupButton", cleanupButton);
            SetField(controller, "_imageAdButton", imageButton);
            SetField(controller, "_videoAdButton", videoButton);
            SetField(controller, "_nativeAdButton", nativeButton);
            SetField(controller, "_enableLoggingButton", enableLoggingButton);
            SetField(controller, "_disableLoggingButton", disableLoggingButton);
            SetField(controller, "_testLoggingButton", testLoggingButton);
            SetField(controller, "_useManualPositionToggle", manualToggle);
            SetField(controller, "_positionDropdown", positionDropdown);
            SetField(controller, "_logScrollRect", logScrollView.GetComponent<ScrollRect>());
            SetField(controller, "_logText", logText);
            SetField(controller, "_adDisplayArea", adDisplayArea.GetComponent<RectTransform>());
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
