using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BidscubeSDK;

namespace BidscubeSDK.Examples
{
    /// <summary>
    /// Setup script for Consent Test Scene - automatically creates UI elements
    /// Based on iOS ConsentTestView structure
    /// </summary>
    public class ConsentTestSceneSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createNavigationButtons = true;

        [ContextMenu("Setup Consent Test Scene")]
        public void SetupConsentTestScene()
        {
            Debug.Log("Setting up Consent Test Scene...");

            // Create main canvas
            var canvas = CreateCanvas();
            
            // Create main controller
            var controller = CreateController(canvas);
            
            // Create UI elements
            CreateUIElements(canvas, controller);
            
            Debug.Log("✅ Consent Test Scene setup completed!");
        }

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupConsentTestScene();
            }
        }

        private GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("ConsentTestCanvas");
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

        private ConsentTestScene CreateController(GameObject canvas)
        {
            var controllerObj = new GameObject("ConsentTestController");
            controllerObj.transform.SetParent(canvas.transform);
            
            var controller = controllerObj.AddComponent<ConsentTestScene>();
            return controller;
        }

        private void CreateUIElements(GameObject canvas, ConsentTestScene controller)
        {
            // Create main panel
            var mainPanel = CreatePanel("MainPanel", canvas.transform);
            var mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = Vector2.zero;
            mainPanelRect.anchorMax = Vector2.one;
            mainPanelRect.offsetMin = Vector2.zero;
            mainPanelRect.offsetMax = Vector2.zero;
            
            // Create title
            var title = CreateText("Title", mainPanel.transform, "Consent Test Scene");
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
            
            // Create consent management panel
            var consentPanel = CreatePanel("ConsentPanel", mainPanel.transform);
            var consentPanelRect = consentPanel.GetComponent<RectTransform>();
            consentPanelRect.anchorMin = new Vector2(0.1f, 0.55f);
            consentPanelRect.anchorMax = new Vector2(0.9f, 0.7f);
            consentPanelRect.offsetMin = Vector2.zero;
            consentPanelRect.offsetMax = Vector2.zero;
            
            var consentTitle = CreateText("ConsentTitle", consentPanel.transform, "Consent Management");
            consentTitle.fontSize = 18;
            consentTitle.fontStyle = FontStyles.Bold;
            consentTitle.alignment = TextAlignmentOptions.Center;
            
            var requestConsentButton = CreateButton("RequestConsentInfoButton", consentPanel.transform, "Request Consent Info Update");
            var showConsentButton = CreateButton("ShowConsentFormButton", consentPanel.transform, "Show Consent Form");
            var checkRequiredButton = CreateButton("CheckConsentRequiredButton", consentPanel.transform, "Check if Consent Required");
            var checkAdsButton = CreateButton("CheckAdsConsentButton", consentPanel.transform, "Check Ads Consent");
            var checkAnalyticsButton = CreateButton("CheckAnalyticsConsentButton", consentPanel.transform, "Check Analytics Consent");
            var getSummaryButton = CreateButton("GetConsentSummaryButton", consentPanel.transform, "Get Consent Summary");
            var enableDebugButton = CreateButton("EnableDebugModeButton", consentPanel.transform, "Enable Debug Mode");
            var resetConsentButton = CreateButton("ResetConsentButton", consentPanel.transform, "Reset Consent");
            
            // Position consent buttons in grid
            PositionButtonsInGrid(new[] { requestConsentButton, showConsentButton, checkRequiredButton, checkAdsButton, 
                checkAnalyticsButton, getSummaryButton, enableDebugButton, resetConsentButton }, consentPanel.transform, 4);
            
            // Create ad testing panel
            var adTestingPanel = CreatePanel("AdTestingPanel", mainPanel.transform);
            var adTestingPanelRect = adTestingPanel.GetComponent<RectTransform>();
            adTestingPanelRect.anchorMin = new Vector2(0.1f, 0.4f);
            adTestingPanelRect.anchorMax = new Vector2(0.9f, 0.5f);
            adTestingPanelRect.offsetMin = Vector2.zero;
            adTestingPanelRect.offsetMax = Vector2.zero;
            
            var adTestingTitle = CreateText("AdTestingTitle", adTestingPanel.transform, "Ad Testing (requires consent and placementId)");
            adTestingTitle.fontSize = 16;
            adTestingTitle.fontStyle = FontStyles.Bold;
            adTestingTitle.alignment = TextAlignmentOptions.Center;
            
            var showImageButton = CreateButton("ShowImageAdButton", adTestingPanel.transform, "Show Image Ad (if consent)");
            var showVideoButton = CreateButton("ShowVideoAdButton", adTestingPanel.transform, "Show Video Ad (if consent)");
            var showNativeButton = CreateButton("ShowNativeAdButton", adTestingPanel.transform, "Show Native Ad (if consent)");
            
            // Position ad testing buttons
            PositionButtonsHorizontally(new[] { showImageButton, showVideoButton, showNativeButton }, adTestingPanel.transform);
            
            // Create log panel
            var logPanel = CreatePanel("LogPanel", mainPanel.transform);
            var logPanelRect = logPanel.GetComponent<RectTransform>();
            logPanelRect.anchorMin = new Vector2(0.1f, 0.05f);
            logPanelRect.anchorMax = new Vector2(0.9f, 0.35f);
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
            adDisplayRect.anchorMax = new Vector2(0.9f, 0.4f);
            adDisplayRect.offsetMin = Vector2.zero;
            adDisplayRect.offsetMax = Vector2.zero;
            adDisplayArea.SetActive(false); // Hidden by default
            
            // Create navigation buttons if requested
            if (createNavigationButtons)
            {
                CreateNavigationButtons(mainPanel.transform);
            }
            
            // Set up controller references
            SetControllerReferences(controller, statusText, placementInput, logScrollView, logText,
                initButton, cleanupButton, requestConsentButton, showConsentButton, checkRequiredButton,
                checkAdsButton, checkAnalyticsButton, getSummaryButton, enableDebugButton, resetConsentButton,
                showImageButton, showVideoButton, showNativeButton, adDisplayArea);
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
            var windowedButton = CreateButton("WindowedAdButton", navPanel.transform, "Windowed Ad");
            
            PositionButtonsHorizontally(new[] { backButton, sdkButton, windowedButton }, navPanel.transform);
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

        private void SetControllerReferences(ConsentTestScene controller, TextMeshProUGUI statusText, TMP_InputField placementInput, GameObject logScrollView, TextMeshProUGUI logText,
            Button initButton, Button cleanupButton, Button requestConsentButton, Button showConsentButton, Button checkRequiredButton,
            Button checkAdsButton, Button checkAnalyticsButton, Button getSummaryButton, Button enableDebugButton, Button resetConsentButton,
            Button showImageButton, Button showVideoButton, Button showNativeButton, GameObject adDisplayArea)
        {
            // Use reflection to set private fields
            var controllerType = typeof(ConsentTestScene);
            
            SetField(controller, "_sdkStatusText", statusText);
            SetField(controller, "_placementIdInput", placementInput);
            SetField(controller, "_initializeButton", initButton);
            SetField(controller, "_cleanupButton", cleanupButton);
            SetField(controller, "_requestConsentInfoButton", requestConsentButton);
            SetField(controller, "_showConsentFormButton", showConsentButton);
            SetField(controller, "_checkConsentRequiredButton", checkRequiredButton);
            SetField(controller, "_checkAdsConsentButton", checkAdsButton);
            SetField(controller, "_checkAnalyticsConsentButton", checkAnalyticsButton);
            SetField(controller, "_getConsentSummaryButton", getSummaryButton);
            SetField(controller, "_enableDebugModeButton", enableDebugButton);
            SetField(controller, "_resetConsentButton", resetConsentButton);
            SetField(controller, "_showImageAdButton", showImageButton);
            SetField(controller, "_showVideoAdButton", showVideoButton);
            SetField(controller, "_showNativeAdButton", showNativeButton);
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
