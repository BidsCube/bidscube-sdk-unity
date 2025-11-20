using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BidscubeSDK
{
    /// <summary>
    /// Ad view controller - Identical to iOS AdViewController
    /// </summary>
    public class AdViewController : MonoBehaviour
    {
        [SerializeField] private string _placementId;
        [SerializeField] private AdType _adType;
        [SerializeField] private IAdCallback _callback;
        [SerializeField] private GameObject _adView;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _positionLabel;
        [SerializeField] private AdPosition _currentPosition = AdPosition.Unknown;
        [SerializeField] private bool _hasAdLoaded = false;
        [SerializeField] private bool _isVideoPlaying = false;
        [SerializeField] private BannerAdView _imageAdView;

        private Coroutine _loadingTimeoutCoroutine;
        private Coroutine _swipeGestureCoroutine;
        private Coroutine _doubleTapGestureCoroutine;

        /// <summary>
        /// Initialize ad view controller
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="adType">Ad type</param>
        /// <param name="callback">Ad callback</param>
        /// <param name="position">Ad position (optional)</param>
        public void Initialize(string placementId, AdType adType, IAdCallback callback = null, AdPosition position = AdPosition.Unknown)
        {
            _placementId = placementId;
            _adType = adType;
            _callback = callback;

            // Ensure scale is 1,1,1 before anything else
            transform.localScale = Vector3.one;

            // Video ads are always full screen
            if (adType == AdType.Video)
            {
                _currentPosition = AdPosition.FullScreen;
                Logger.Info("[AdViewController] Video ad detected - forcing full screen position at initialization");
            }
            else
            {
                // Default position priority: Server response first, then parameter, then manual override
                // At initialization, server response might not be available yet, so use parameter
                // The position will be updated in MarkAdAsLoaded() based on server response
                var serverPosition = BidscubeSDK.GetResponseAdPosition();
                if (serverPosition != AdPosition.Unknown)
                {
                    _currentPosition = serverPosition;
                    Logger.Info($"[AdViewController] Initializing with server response position: {serverPosition}");
                }
                else
                {
                    // No server response yet, use parameter or fallback to Unknown
                    _currentPosition = position;
                    Logger.Info($"[AdViewController] Initializing with parameter position: {position}");
                }
            }

            SetupUI();

            // Apply initial positioning (will be updated when ad loads with actual dimensions)
            ApplyPositioning(_currentPosition);

            LoadAd();
        }

        private void SetupUI()
        {
            // Ensure RectTransform exists and scale is 1,1,1
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // Ensure scale is 1,1,1
            transform.localScale = Vector3.one;

            // Check if position is FullScreen to decide canvas mode
            bool isFullScreen = _currentPosition == AdPosition.FullScreen;

            // Check if we're already parented to a Canvas or SDKContent
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Camera mainCamera = Camera.main;

            Canvas canvas = null;

            // Try to use existing canvas if available and not full screen
            if (parentCanvas != null && !isFullScreen)
            {
                // Already parented to a Canvas - use it
                Logger.Info("[AdViewController] Already parented to existing Canvas, using it");
                canvas = parentCanvas;
                // Ensure parent canvas scale is 1,1,1
                if (canvas.transform.localScale != Vector3.one)
                {
                    Logger.Info($"[AdViewController] Fixing parent canvas scale from {canvas.transform.localScale} to 1,1,1");
                    canvas.transform.localScale = Vector3.one;
                }
            }
            else if (transform.parent != null)
            {
                // Check if parent has a Canvas component
                parentCanvas = transform.parent.GetComponent<Canvas>();
                if (parentCanvas != null && !isFullScreen)
                {
                    Logger.Info("[AdViewController] Parent has Canvas component, using it");
                    canvas = parentCanvas;
                    // Ensure parent canvas scale is 1,1,1
                    if (canvas.transform.localScale != Vector3.one)
                    {
                        Logger.Info($"[AdViewController] Fixing parent canvas scale from {canvas.transform.localScale} to 1,1,1");
                        canvas.transform.localScale = Vector3.one;
                    }
                }
            }

            // If no canvas found, create our own
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                if (canvas == null)
                {
                    Logger.InfoError("[AdViewController] Failed to create Canvas component!");
                    return;
                }
                SetupCanvas(canvas, isFullScreen, mainCamera);
            }
            else
            {
                // Using existing canvas - ensure our GameObject scale is 1,1,1
                transform.localScale = Vector3.one;
                // Also ensure the existing canvas scale is 1,1,1
                if (canvas != null && canvas.transform.localScale != Vector3.one)
                {
                    Logger.Info($"[AdViewController] Fixing existing canvas scale from {canvas.transform.localScale} to 1,1,1");
                    canvas.transform.localScale = Vector3.one;
                }
            }

            // Always ensure canvas GameObject scale is 1,1,1 (double-check)
            if (canvas != null)
            {
                if (canvas.transform.localScale != Vector3.one)
                {
                    Logger.Info($"[AdViewController] Final check: Fixing canvas scale from {canvas.transform.localScale} to 1,1,1");
                    canvas.transform.localScale = Vector3.one;
                }
            }

            // Create background
            var background = new GameObject("Background");
            background.transform.SetParent(transform, false);
            background.transform.localScale = Vector3.one; // Ensure scale is 1,1,1
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = Color.black;

            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            // Create back button
            CreateBackButton();

            // Create close button
            CreateCloseButton();

            // Create position label
            CreatePositionLabel();
        }

        private void LateUpdate()
        {
            // Continuously ensure canvas scale is 1,1,1 at runtime
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.transform.localScale != Vector3.one)
            {
                canvas.transform.localScale = Vector3.one;
            }

            // Also ensure our own scale is 1,1,1
            if (transform.localScale != Vector3.one)
            {
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Setup canvas with appropriate render mode
        /// </summary>
        private void SetupCanvas(Canvas canvas, bool isFullScreen, Camera mainCamera)
        {
            if (canvas == null)
            {
                Logger.InfoError("[AdViewController] SetupCanvas called with null canvas!");
                return;
            }

            // Ensure canvas GameObject scale is 1,1,1 (force it)
            if (canvas.transform.localScale != Vector3.one)
            {
                Logger.Info($"[AdViewController] Fixing canvas scale from {canvas.transform.localScale} to 1,1,1");
            }
            canvas.transform.localScale = Vector3.one;

            // Also ensure parent scale is 1,1,1 if it exists
            if (canvas.transform.parent != null && canvas.transform.parent.localScale != Vector3.one)
            {
                Logger.Info($"[AdViewController] Fixing canvas parent scale from {canvas.transform.parent.localScale} to 1,1,1");
                canvas.transform.parent.localScale = Vector3.one;
            }

            if (isFullScreen || mainCamera == null)
            {
                // Full screen or no camera - use overlay mode
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
            }
            else
            {
                // Not full screen and camera exists - use camera mode for proper sizing
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
                canvas.sortingOrder = 1000;

                // Calculate proper plane distance based on camera settings
                // For proper scaling, plane distance should be within camera's clipping planes
                float planeDistance = 100f;
                if (mainCamera != null)
                {
                    // Use a distance that's well within the camera's far clipping plane
                    // and far enough from near plane to avoid clipping
                    planeDistance = Mathf.Clamp(mainCamera.nearClipPlane + 50f, mainCamera.nearClipPlane + 10f, mainCamera.farClipPlane - 10f);
                }
                canvas.planeDistance = planeDistance;

                // For Screen Space Camera, Canvas automatically sizes based on camera viewport
                // The "Some values driven by Canvas" message is normal - Canvas controls the size
                // We just need to ensure the RectTransform is set up correctly
                var canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    // Set anchors to fill (Canvas will drive the size based on camera)
                    canvasRect.anchorMin = Vector2.zero;
                    canvasRect.anchorMax = Vector2.one;
                    canvasRect.sizeDelta = Vector2.zero; // Let Canvas drive the size
                    canvasRect.anchoredPosition = Vector2.zero;
                }

                // Log camera info for debugging
                if (mainCamera != null)
                {
                    Logger.Info($"[AdViewController] Using ScreenSpaceCamera mode - Camera: {mainCamera.name}, Plane Distance: {planeDistance}, Clear Flags: {mainCamera.clearFlags}, Depth: {mainCamera.depth}");
                }
            }

            var canvasScaler = gameObject.AddComponent<CanvasScaler>();
            if (canvasScaler != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    // For Screen Space Camera, use ScaleWithScreenSize to maintain proper scaling
                    canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);
                    canvasScaler.matchWidthOrHeight = 0.5f; // Match both width and height
                    canvasScaler.referencePixelsPerUnit = 100f; // Standard reference
                }
                else
                {
                    // For Overlay mode, use Constant Pixel Size with scale 1
                    canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    canvasScaler.scaleFactor = 1f; // Scale factor must be 1
                    canvasScaler.referencePixelsPerUnit = 100f; // Standard reference
                }
            }

            var graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreateBackButton()
        {
            var backButtonObj = new GameObject("BackButton");
            backButtonObj.transform.SetParent(transform, false);
            backButtonObj.transform.localScale = Vector3.one; // Ensure scale is 1,1,1

            // Ensure RectTransform is available
            var backButtonRect = backButtonObj.GetComponent<RectTransform>();
            if (backButtonRect == null)
            {
                backButtonRect = backButtonObj.AddComponent<RectTransform>();
            }

            _backButton = backButtonObj.AddComponent<Button>();

            // Position and size back button
            backButtonRect.anchorMin = new Vector2(0, 1);
            backButtonRect.anchorMax = new Vector2(0, 1);
            backButtonRect.pivot = new Vector2(0, 1);
            backButtonRect.sizeDelta = new Vector2(20, 20);
            backButtonRect.anchoredPosition = new Vector2(20, -20);

            var backButtonImage = backButtonObj.AddComponent<Image>();
            backButtonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var backButtonText = new GameObject("Text");
            backButtonText.transform.SetParent(backButtonObj.transform, false);
            backButtonText.transform.localScale = Vector3.one; // Ensure scale is 1,1,1
            var text = backButtonText.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Make text 5x bigger for full screen mode
            if (_currentPosition == AdPosition.FullScreen)
            {
                text.fontSize = 80;
            }
            else
            {
                text.fontSize = 80;
            }

            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "Back";

            var textRect = backButtonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _backButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void CreateCloseButton()
        {
            var closeButtonObj = new GameObject("CloseButton");
            closeButtonObj.transform.SetParent(transform, false);
            closeButtonObj.transform.localScale = Vector3.one; // Ensure scale is 1,1,1

            // Ensure RectTransform is available
            var closeButtonRect = closeButtonObj.GetComponent<RectTransform>();
            if (closeButtonRect == null)
            {
                closeButtonRect = closeButtonObj.AddComponent<RectTransform>();
            }

            _closeButton = closeButtonObj.AddComponent<Button>();

            // Position close button
            closeButtonRect.anchorMin = new Vector2(1, 1);
            closeButtonRect.anchorMax = new Vector2(1, 1);
            closeButtonRect.pivot = new Vector2(1, 1);
            closeButtonRect.sizeDelta = new Vector2(50, 50);
            closeButtonRect.anchoredPosition = new Vector2(-20, -20);

            var closeButtonImage = closeButtonObj.AddComponent<Image>();
            closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

            var closeButtonText = new GameObject("Text");
            closeButtonText.transform.SetParent(closeButtonObj.transform, false);
            closeButtonText.transform.localScale = Vector3.one; // Ensure scale is 1,1,1
            var text = closeButtonText.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 80;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "×";

            var textRect = closeButtonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _closeButton.gameObject.SetActive(false);
        }

        private void CreatePositionLabel()
        {
            var positionLabelObj = new GameObject("PositionLabel");
            positionLabelObj.transform.SetParent(transform, false);
            positionLabelObj.transform.localScale = Vector3.one; // Ensure scale is 1,1,1

            // Ensure RectTransform is available
            var positionLabelRect = positionLabelObj.GetComponent<RectTransform>();
            if (positionLabelRect == null)
            {
                positionLabelRect = positionLabelObj.AddComponent<RectTransform>();
            }

            _positionLabel = positionLabelObj.AddComponent<Text>();
            positionLabelRect.anchorMin = new Vector2(0.5f, 1f);
            positionLabelRect.anchorMax = new Vector2(0.5f, 1f);
            positionLabelRect.pivot = new Vector2(0.5f, 1f);
            positionLabelRect.sizeDelta = new Vector2(400, 60);
            positionLabelRect.anchoredPosition = new Vector2(0, -80);

            _positionLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _positionLabel.fontSize = 100;
            _positionLabel.color = Color.white;
            _positionLabel.alignment = TextAnchor.MiddleLeft;
            _positionLabel.text = $"Position: {_currentPosition}";
        }

        private void LoadAd()
        {
            _callback?.OnAdLoading(_placementId);

            // Start loading timeout
            _loadingTimeoutCoroutine = StartCoroutine(LoadingTimeout());

            // Create appropriate ad view based on type
            switch (_adType)
            {
                case AdType.Image:
                    CreateImageAdView();
                    break;
                case AdType.Video:
                    CreateVideoAdView();
                    break;
                case AdType.Native:
                    CreateNativeAdView();
                    break;
            }
        }

        private void CreateImageAdView()
        {
            // AdViewController will now handle its own positioning.
            // The BannerAdView will just fill the AdViewController's rect.
            var bannerAdView = gameObject.AddComponent<BannerAdView>();
            bannerAdView.SetPlacementInfo(_placementId, _callback);

            // Assign to both _adView and _imageAdView
            _adView = bannerAdView.gameObject;
            _imageAdView = bannerAdView;

            var url = BidscubeSDK.BuildRequestURL(_placementId, AdType.Image, _currentPosition);
            if (!string.IsNullOrEmpty(url))
            {
                bannerAdView.LoadAdFromURL(url);
            }

            // Ensure buttons/text render above the ad content
            EnsureButtonsOnTop();
        }

        /// <summary>
        /// Test HTML rendering using WebViewController
        /// </summary>
        [ContextMenu("Test HTML Rendering")]
        public void TestHTMLRendering()
        {
            Logger.Info(" AdViewController: Testing HTML rendering using WebViewController...");

            // Create WebViewController for testing
            var webViewControllerGO = new GameObject("TestWebViewController");
            webViewControllerGO.transform.SetParent(transform);
            var webViewController = webViewControllerGO.AddComponent<WebViewController>();

            // Initialize WebViewController
            webViewController.Initialize(
                onHtmlLoaded: (url) => Logger.Info($" Test WebViewController: HTML loaded: {url}"),
                onError: (error) => Logger.InfoError($" Test WebViewController error: {error}"),
                onMessage: (message) => Logger.Info($" Test WebViewController message: {message}")
            );

            // Load test HTML using WebViewController
            webViewController.LoadTestHTML();

            Logger.Info(" AdViewController: Test HTML loaded using WebViewController");
        }

        /// <summary>
        /// Test with your specific HTML content using WebViewController
        /// </summary>
        [ContextMenu("Test Your HTML")]
        public void TestYourHTML()
        {
            Logger.Info(" AdViewController: Testing your specific HTML content using WebViewController...");

            // Your specific HTML content
            var yourHtml = @"<!DOCTYPE html> <html lang='en'> <head> <meta charset='UTF-8'> <meta name='viewport' content='width=device-width, initial-scale=1.0'> <title>Test Image Ad</title> <style>     body {         margin: 0;         background: black;         display: flex;         justify-content: center;         align-items: center;         height: 100vh;     }     img {         width: 100%;         height: auto;     } </style> </head> <body>     <img src='https://images.ctfassets.net/qclcq9s44sii/Xn8oCGZCVbWFZuDtPMYjE/854403c5605e210a371f958c0b5aa5f2/7_Image_APIs_To_Use_On_Your_Product_In_2025__Updated___2_.png' alt='Ad'> </body> </html>";

            // Create WebViewController for testing
            var webViewControllerGO = new GameObject("YourHTMLWebViewController");
            webViewControllerGO.transform.SetParent(transform);
            var webViewController = webViewControllerGO.AddComponent<WebViewController>();

            // Initialize WebViewController
            webViewController.Initialize(
                onHtmlLoaded: (url) => Logger.Info($" Your HTML WebViewController: HTML loaded: {url}"),
                onError: (error) => Logger.InfoError($" Your HTML WebViewController error: {error}"),
                onMessage: (message) => Logger.Info($" Your HTML WebViewController message: {message}")
            );

            // Load your HTML using WebViewController
            webViewController.LoadHTML(yourHtml, "");

            Logger.Info(" AdViewController: Your HTML loaded using WebViewController");
        }

        private void CreateVideoAdView()
        {
            // Video ads should be full screen
            var videoAdObj = new GameObject("VideoAdView");
            videoAdObj.transform.SetParent(transform, false);
            videoAdObj.transform.localScale = Vector3.one; // Ensure scale is 1,1,1

            // Ensure RectTransform is available
            var videoAdRect = videoAdObj.GetComponent<RectTransform>();
            if (videoAdRect == null)
            {
                videoAdRect = videoAdObj.AddComponent<RectTransform>();
            }

            // Make full screen
            videoAdRect.anchorMin = Vector2.zero;
            videoAdRect.anchorMax = Vector2.one;
            videoAdRect.offsetMin = Vector2.zero;
            videoAdRect.offsetMax = Vector2.zero;

            var videoAdView = videoAdObj.AddComponent<VideoAdView>();
            videoAdView.SetPlacementInfo(_placementId, _callback);
            _adView = videoAdObj;
        }

        private void CreateNativeAdView()
        {
            // AdViewController will handle positioning.
            // The NativeAdView will fill the AdViewController's rect.
            var nativeAdView = gameObject.AddComponent<NativeAdView>();
            nativeAdView.SetPlacementInfo(_placementId, _callback);

            var url = BidscubeSDK.BuildRequestURL(_placementId, AdType.Native, _currentPosition);
            if (!string.IsNullOrEmpty(url))
            {
                nativeAdView.LoadNativeAdFromURL(url);
            }
            _adView = nativeAdView.gameObject;

            // Ensure buttons/text render above the ad content
            EnsureButtonsOnTop();
        }

        private IEnumerator LoadingTimeout()
        {
            yield return new WaitForSeconds(30f); // 30 second timeout like iOS

            if (!_hasAdLoaded)
            {
                _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.Timeout, "Ad loading timeout");
                Destroy(gameObject);
            }
        }

        private void OnBackButtonClicked()
        {
            Logger.Info(" AdViewController: Back button clicked");
            _callback?.OnAdClosed(_placementId);
            BidscubeSDK.UnregisterAdViewController(this);
            Destroy(gameObject);
        }

        private void OnCloseButtonClicked()
        {
            Logger.Info(" AdViewController: Close button clicked");
            _callback?.OnAdClosed(_placementId);
            BidscubeSDK.UnregisterAdViewController(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Show close button - Identical to iOS showCloseButton
        /// </summary>
        public void ShowCloseButton()
        {
            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Show close button on complete - Identical to iOS showCloseButtonOnComplete
        /// </summary>
        public void ShowCloseButtonOnComplete()
        {
            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hide close button - Identical to iOS hideCloseButton
        /// </summary>
        public void HideCloseButton()
        {
            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update position label
        /// </summary>
        /// <param name="position">New position</param>
        public void UpdatePosition(AdPosition position)
        {
            // Video ads are always full screen
            if (_adType == AdType.Video)
            {
                Logger.Info("[AdViewController] Video ad detected - forcing full screen position");
                position = AdPosition.FullScreen;
            }

            // Always update position even if it's the same, to ensure it's correctly positioned
            // (dimensions might have changed after ad loads)
            _currentPosition = position;
            if (_positionLabel != null)
            {
                _positionLabel.text = $"Position: {position}";
            }

            Logger.Info($"[AdViewController] Updating position to: {position}");
            ApplyPositioning(position);

            // Refresh WebView margins after repositioning
            RefreshWebViewMargins();
        }

        private void ApplyPositioning(AdPosition position)
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Video ads are always full screen
            if (_adType == AdType.Video)
            {
                Logger.Info("[AdViewController] Video ad detected - forcing full screen");
                position = AdPosition.FullScreen;
            }

            // Get actual banner dimensions from the ad view
            float bannerHeight = 40f;
            float bannerWidth = 320f;

            // Try to get dimensions from BannerAdView (check both _imageAdView and GetComponent)
            if (_imageAdView != null)
            {
                var dimensions = _imageAdView.GetBannerDimensions();
                if (dimensions.x > 0f && dimensions.y > 0f)
                {
                    bannerWidth = dimensions.x;
                    bannerHeight = dimensions.y;
                    Logger.Info($"[AdViewController] Using actual banner dimensions from _imageAdView: {bannerWidth}x{bannerHeight}");
                }
            }
            else
            {
                var bannerAdView = GetComponent<BannerAdView>();
                if (bannerAdView != null)
                {
                    var dimensions = bannerAdView.GetBannerDimensions();
                    if (dimensions.x > 0f && dimensions.y > 0f)
                    {
                        bannerWidth = dimensions.x;
                        bannerHeight = dimensions.y;
                        Logger.Info($"[AdViewController] Using actual banner dimensions: {bannerWidth}x{bannerHeight}");
                    }
                }
            }

            // Try to get dimensions from NativeAdView
            var nativeAdView = GetComponent<NativeAdView>();
            if (nativeAdView != null)
            {
                var dimensions = nativeAdView.GetNativeAdDimensions();
                if (dimensions.x > 0f && dimensions.y > 0f)
                {
                    bannerWidth = dimensions.x;
                    bannerHeight = dimensions.y;
                    Logger.Info($"[AdViewController] Using actual native ad dimensions: {bannerWidth}x{bannerHeight}");
                }
            }

            // Get canvas or screen dimensions for proper sizing
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // If we have a canvas in Screen Space Camera mode, calculate proper dimensions
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
            {
                // For Screen Space Camera, calculate size based on camera viewport
                Camera cam = canvas.worldCamera;
                float distance = canvas.planeDistance;

                // Calculate world space size at the canvas distance
                float height = 2.0f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float width = height * cam.aspect;

                // Convert to canvas space (pixels) - use screen dimensions as reference
                screenWidth = Screen.width;
                screenHeight = Screen.height;

                Logger.Info($"[AdViewController] Screen Space Camera mode - using screen dimensions: {screenWidth}x{screenHeight}");
            }
            else if (canvas != null)
            {
                // For other canvas modes, try to get canvas rect dimensions
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null && canvasRect.rect.width > 0 && canvasRect.rect.height > 0)
                {
                    screenWidth = canvasRect.rect.width;
                    screenHeight = canvasRect.rect.height;
                    Logger.Info($"[AdViewController] Using canvas rect dimensions: {screenWidth}x{screenHeight}");
                }
            }

            switch (position)
            {
                case AdPosition.FullScreen:
                    // Full screen - fill entire screen
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                    Logger.Info("[AdViewController] Positioned as FullScreen - filling entire screen");
                    break;

                case AdPosition.Header:
                    // Header - full width at top
                    rectTransform.anchorMin = new Vector2(0.5f, 1f);
                    rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                    rectTransform.sizeDelta = new Vector2(screenWidth, bannerHeight);
                    rectTransform.anchoredPosition = Vector2.zero;
                    Logger.Info($"[AdViewController] Positioned at Header with size {screenWidth}x{bannerHeight}");
                    break;

                case AdPosition.Footer:
                    // Footer - full width at bottom
                    rectTransform.anchorMin = new Vector2(0.5f, 0f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    rectTransform.pivot = new Vector2(0.5f, 0f);
                    rectTransform.sizeDelta = new Vector2(screenWidth, bannerHeight);
                    rectTransform.anchoredPosition = Vector2.zero;
                    Logger.Info($"[AdViewController] Positioned at Footer with size {screenWidth}x{bannerHeight}");
                    break;

                case AdPosition.Sidebar:
                    // Sidebar - right side
                    rectTransform.anchorMin = new Vector2(1f, 0.5f);
                    rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    rectTransform.pivot = new Vector2(1f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(bannerWidth > 0 ? bannerWidth : 120, bannerHeight > 0 ? bannerHeight : 250);
                    rectTransform.anchoredPosition = Vector2.zero;
                    Logger.Info($"[AdViewController] Positioned at Sidebar with size {rectTransform.sizeDelta.x}x{rectTransform.sizeDelta.y}");
                    break;

                case AdPosition.AboveTheFold:
                case AdPosition.BelowTheFold:
                case AdPosition.DependOnScreenSize:
                case AdPosition.Unknown:
                default:
                    // Centered with specific dimensions
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(bannerWidth, bannerHeight);
                    rectTransform.anchoredPosition = Vector2.zero;
                    Logger.Info($"[AdViewController] Positioned at center (default) with size {bannerWidth}x{bannerHeight}");
                    break;
            }

            // Force layout update to ensure positioning is applied
            Canvas.ForceUpdateCanvases();
        }

        public void MarkAdAsLoaded()
        {
            _hasAdLoaded = true;
            if (_loadingTimeoutCoroutine != null)
            {
                StopCoroutine(_loadingTimeoutCoroutine);
                _loadingTimeoutCoroutine = null;
            }

            // Video ads are always full screen
            if (_adType == AdType.Video)
            {
                Logger.Info("[AdViewController] Video ad detected - forcing full screen position");
                _currentPosition = AdPosition.FullScreen;
                ApplyPositioning(AdPosition.FullScreen);
                EnsureButtonsOnTop();
                RefreshWebViewMargins();
                return;
            }

            // Ensure buttons/text render above the ad content after ad is loaded
            EnsureButtonsOnTop();

            // Position priority: Server response first, then manual override (if set)
            // 1. First, check if server provided a position (default behavior)
            var serverPosition = BidscubeSDK.GetResponseAdPosition();
            AdPosition positionToUse = serverPosition;

            if (serverPosition != AdPosition.Unknown)
            {
                Logger.Info($"[AdViewController] Server provided position: {serverPosition}");
            }

            // 2. Then, check if manual/dropdown override is set (overrides server response)
            var manualPosition = BidscubeSDK.GetAdPosition();
            if (manualPosition != AdPosition.Unknown)
            {
                positionToUse = manualPosition;
                Logger.Info($"[AdViewController] Manual/dropdown position override: {manualPosition} (overrides server position: {serverPosition})");
            }

            // 3. If we have a position to use, apply it
            if (positionToUse != AdPosition.Unknown)
            {
                _currentPosition = positionToUse;
                UpdatePosition(positionToUse);
            }
            // 4. Otherwise, use current position (may have been set initially)
            else if (_currentPosition != AdPosition.Unknown)
            {
                // Apply positioning with current position (may have dimensions now)
                ApplyPositioning(_currentPosition);

                // Refresh WebView margins after positioning
                RefreshWebViewMargins();
            }
            else
            {
                // Fallback: apply positioning with Unknown (centered)
                ApplyPositioning(_currentPosition);

                // Refresh WebView margins after positioning
                RefreshWebViewMargins();
            }
        }

        /// <summary>
        /// Ensure buttons and text are rendered above ad content by moving them to the end of sibling list
        /// </summary>
        private void EnsureButtonsOnTop()
        {
            // In Unity UI, objects that appear later in the hierarchy render on top
            // Move button GameObjects to the end so they render above ad content
            if (_backButton != null && _backButton.gameObject != null)
            {
                _backButton.transform.SetAsLastSibling();
            }
            if (_closeButton != null && _closeButton.gameObject != null)
            {
                _closeButton.transform.SetAsLastSibling();
            }
            if (_positionLabel != null && _positionLabel.gameObject != null)
            {
                _positionLabel.transform.SetAsLastSibling();
            }

            // Also ensure Background is at the bottom (renders first/behind everything)
            var background = transform.Find("Background");
            if (background != null)
            {
                background.SetAsFirstSibling();
            }
        }

        /// <summary>
        /// Refresh WebView margins for all WebViewControllers in child ad views
        /// </summary>
        private void RefreshWebViewMargins()
        {
            // Find all WebViewControllers in child components and refresh their margins
            var bannerAdView = GetComponent<BannerAdView>();
            if (bannerAdView != null)
            {
                var webViewController = bannerAdView.GetComponentInChildren<WebViewController>();
                if (webViewController != null)
                {
                    webViewController.RefreshMargins();
                }
            }

            var nativeAdView = GetComponent<NativeAdView>();
            if (nativeAdView != null)
            {
                var webViewController = nativeAdView.GetComponentInChildren<WebViewController>();
                if (webViewController != null)
                {
                    webViewController.RefreshMargins();
                }
            }
        }
    }
}