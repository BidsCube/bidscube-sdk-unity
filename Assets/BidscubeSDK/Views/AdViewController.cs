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
        [SerializeField] private ImageAdView _imageAdView;

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
            _currentPosition = position;

            SetupUI();
            LoadAd();
        }

        private void SetupUI()
        {
            // Create canvas
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            var graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

            // Create background
            var background = new GameObject("Background");
            background.transform.SetParent(transform);
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

        private void CreateBackButton()
        {
            var backButtonObj = new GameObject("BackButton");
            backButtonObj.transform.SetParent(transform);

            // Ensure RectTransform is available
            var backButtonRect = backButtonObj.GetComponent<RectTransform>();
            if (backButtonRect == null)
            {
                backButtonRect = backButtonObj.AddComponent<RectTransform>();
            }

            _backButton = backButtonObj.AddComponent<Button>();

            // Position and size back button based on ad position
            if (_currentPosition == AdPosition.FullScreen)
            {
                // Full screen mode - 5x bigger button at top-left
                backButtonRect.anchorMin = new Vector2(0, 1);
                backButtonRect.anchorMax = new Vector2(0, 1);
                backButtonRect.sizeDelta = new Vector2(500, 250); // 5x bigger (100*5, 50*5)
                backButtonRect.anchoredPosition = new Vector2(-660, 1556); // Adjusted position for bigger button
            }
            else
            {
                // Non-full screen mode - smaller button positioned relative to ad display area
                backButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
                backButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
                backButtonRect.sizeDelta = new Vector2(500, 200); // Normal size
                backButtonRect.anchoredPosition = new Vector2(-660, 1556); // Position relative to ad area
            }

            var backButtonImage = backButtonObj.AddComponent<Image>();
            backButtonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var backButtonText = new GameObject("Text");
            backButtonText.transform.SetParent(backButtonObj.transform);
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
            closeButtonObj.transform.SetParent(transform);

            // Ensure RectTransform is available
            var closeButtonRect = closeButtonObj.GetComponent<RectTransform>();
            if (closeButtonRect == null)
            {
                closeButtonRect = closeButtonObj.AddComponent<RectTransform>();
            }

            _closeButton = closeButtonObj.AddComponent<Button>();

            // Position close button based on ad position
            if (_currentPosition == AdPosition.FullScreen)
            {
                // Full screen mode - at top-right corner
                closeButtonRect.anchorMin = new Vector2(1, 1);
                closeButtonRect.anchorMax = new Vector2(1, 1);
                closeButtonRect.sizeDelta = new Vector2(50, 50);
                closeButtonRect.anchoredPosition = new Vector2(-25, -25);
            }
            else
            {
                // Non-full screen mode - positioned relative to ad display area
                closeButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
                closeButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
                closeButtonRect.sizeDelta = new Vector2(40, 40); // Slightly smaller for display area
                closeButtonRect.anchoredPosition = new Vector2(160, 125); // Position relative to ad area
            }

            var closeButtonImage = closeButtonObj.AddComponent<Image>();
            closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

            var closeButtonText = new GameObject("Text");
            closeButtonText.transform.SetParent(closeButtonObj.transform);
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
            positionLabelObj.transform.SetParent(transform);

            // Ensure RectTransform is available
            var positionLabelRect = positionLabelObj.GetComponent<RectTransform>();
            if (positionLabelRect == null)
            {
                positionLabelRect = positionLabelObj.AddComponent<RectTransform>();
            }

            _positionLabel = positionLabelObj.AddComponent<Text>();
            positionLabelRect.anchorMin = new Vector2(0, 1);
            positionLabelRect.anchorMax = new Vector2(0, 1);
            positionLabelRect.sizeDelta = new Vector2(1000, 200);
            positionLabelRect.anchoredPosition = new Vector2(1076, -150);

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
            Debug.Log("🔍 AdViewController: Creating Image AdView with WebView…");

            // Create parent container
            var imageAdObj = new GameObject("ImageAdView");
            imageAdObj.transform.SetParent(transform);

            // ✅ Ensure RectTransform exists
            var rectTransform = imageAdObj.GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = imageAdObj.AddComponent<RectTransform>();

            // Stretch full-screen
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Create WebView
            var webViewGO = new GameObject("WebViewObject");
            webViewGO.transform.SetParent(imageAdObj.transform, false);

            var webViewObject = webViewGO.AddComponent<WebViewObject>();

            // Init WebView
            webViewObject.Init(
                cb: (msg) => Debug.Log($"🔍 WebView msg: {msg}"),
                err: (msg) => Debug.LogError($"🔍 WebView error: {msg}"),
                httpErr: (msg) => Debug.LogError($"🔍 WebView HTTP error: {msg}"),
                started: (url) =>
                {
                    Debug.Log($"🔍 WebView started: {url}");
                    if (url == "about:blank")
                    {
                        // Load your ad HTML or URL here
                        string htmlAd = @"
                  <html><body style='margin:0;background:#000'>
                    <img src='https://via.placeholder.com/1080x1920/00ff00/ffffff?text=Ad'
                         style='width:100%;height:auto;'/>
                  </body></html>";
                        webViewObject.LoadHTML(htmlAd, "");
                    }
                },
                ld: (url) =>
                {
                    Debug.Log($"🔍 WebView loaded: {url}");
                    MarkAdAsLoaded();
                    ShowCloseButton();
                },
                enableWKWebView: true
            );

            webViewObject.SetVisibility(true);
            webViewObject.SetMargins(0, 0, 0, 0);

            _adView = imageAdObj;
        }

        /// <summary>
        /// Test HTML rendering using WebViewController
        /// </summary>
        [ContextMenu("Test HTML Rendering")]
        public void TestHTMLRendering()
        {
            Debug.Log("🔍 AdViewController: Testing HTML rendering using WebViewController...");

            // Create WebViewController for testing
            var webViewControllerGO = new GameObject("TestWebViewController");
            webViewControllerGO.transform.SetParent(transform);
            var webViewController = webViewControllerGO.AddComponent<WebViewController>();

            // Initialize WebViewController
            webViewController.Initialize(
                onHtmlLoaded: (url) => Debug.Log($"🔍 Test WebViewController: HTML loaded: {url}"),
                onError: (error) => Debug.LogError($"🔍 Test WebViewController error: {error}"),
                onMessage: (message) => Debug.Log($"🔍 Test WebViewController message: {message}")
            );

            // Load test HTML using WebViewController
            webViewController.LoadTestHTML();

            Debug.Log("🔍 AdViewController: Test HTML loaded using WebViewController");
        }

        /// <summary>
        /// Test with your specific HTML content using WebViewController
        /// </summary>
        [ContextMenu("Test Your HTML")]
        public void TestYourHTML()
        {
            Debug.Log("🔍 AdViewController: Testing your specific HTML content using WebViewController...");

            // Your specific HTML content
            var yourHtml = @"<!DOCTYPE html> <html lang='en'> <head> <meta charset='UTF-8'> <meta name='viewport' content='width=device-width, initial-scale=1.0'> <title>Test Image Ad</title> <style>     body {         margin: 0;         background: black;         display: flex;         justify-content: center;         align-items: center;         height: 100vh;     }     img {         width: 100%;         height: auto;     } </style> </head> <body>     <img src='https://images.ctfassets.net/qclcq9s44sii/Xn8oCGZCVbWFZuDtPMYjE/854403c5605e210a371f958c0b5aa5f2/7_Image_APIs_To_Use_On_Your_Product_In_2025__Updated___2_.png' alt='Ad'> </body> </html>";

            // Create WebViewController for testing
            var webViewControllerGO = new GameObject("YourHTMLWebViewController");
            webViewControllerGO.transform.SetParent(transform);
            var webViewController = webViewControllerGO.AddComponent<WebViewController>();

            // Initialize WebViewController
            webViewController.Initialize(
                onHtmlLoaded: (url) => Debug.Log($"🔍 Your HTML WebViewController: HTML loaded: {url}"),
                onError: (error) => Debug.LogError($"🔍 Your HTML WebViewController error: {error}"),
                onMessage: (message) => Debug.Log($"🔍 Your HTML WebViewController message: {message}")
            );

            // Load your HTML using WebViewController
            webViewController.LoadHTML(yourHtml, "");

            Debug.Log("🔍 AdViewController: Your HTML loaded using WebViewController");
        }

        private void CreateVideoAdView()
        {
            var videoAdObj = new GameObject("VideoAdView");
            videoAdObj.transform.SetParent(transform);

            // Ensure RectTransform is available
            var videoAdRect = videoAdObj.GetComponent<RectTransform>();
            if (videoAdRect == null)
            {
                videoAdRect = videoAdObj.AddComponent<RectTransform>();
            }

            var videoAdView = videoAdObj.AddComponent<VideoAdView>();
            videoAdRect.anchorMin = new Vector2(0.5f, 0.5f);
            videoAdRect.anchorMax = new Vector2(0.5f, 0.5f);
            videoAdRect.sizeDelta = new Vector2(640, 360);
            videoAdRect.anchoredPosition = Vector2.zero;

            videoAdView.SetPlacementInfo(_placementId, _callback);
            _adView = videoAdObj;
        }

        private void CreateNativeAdView()
        {
            var nativeAdObj = new GameObject("NativeAdView");
            nativeAdObj.transform.SetParent(transform);

            // Ensure RectTransform is available
            var nativeAdRect = nativeAdObj.GetComponent<RectTransform>();
            if (nativeAdRect == null)
            {
                nativeAdRect = nativeAdObj.AddComponent<RectTransform>();
            }

            var nativeAdView = nativeAdObj.AddComponent<NativeAdView>();
            nativeAdRect.anchorMin = new Vector2(0.5f, 0.5f);
            nativeAdRect.anchorMax = new Vector2(0.5f, 0.5f);
            nativeAdRect.sizeDelta = new Vector2(300, 200);
            nativeAdRect.anchoredPosition = Vector2.zero;

            nativeAdView.SetPlacementInfo(_placementId, _callback);
            _adView = nativeAdObj;
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
            Debug.Log("🔍 AdViewController: Back button clicked");
            _callback?.OnAdClosed(_placementId);
            Destroy(gameObject);
        }

        private void OnCloseButtonClicked()
        {
            Debug.Log("🔍 AdViewController: Close button clicked");
            _callback?.OnAdClosed(_placementId);
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
            _currentPosition = position;
            if (_positionLabel != null)
            {
                _positionLabel.text = $"Position: {position}";
            }
        }

        /// <summary>
        /// Mark ad as loaded
        /// </summary>
        public void MarkAdAsLoaded()
        {
            _hasAdLoaded = true;
            if (_loadingTimeoutCoroutine != null)
            {
                StopCoroutine(_loadingTimeoutCoroutine);
                _loadingTimeoutCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (_loadingTimeoutCoroutine != null)
            {
                StopCoroutine(_loadingTimeoutCoroutine);
            }
            if (_swipeGestureCoroutine != null)
            {
                StopCoroutine(_swipeGestureCoroutine);
            }
            if (_doubleTapGestureCoroutine != null)
            {
                StopCoroutine(_doubleTapGestureCoroutine);
            }
        }
    }
}




