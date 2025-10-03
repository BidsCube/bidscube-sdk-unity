using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

namespace BidscubeSDK
{
    /// <summary>
    /// Banner ad view component - Identical to iOS BannerAdView
    /// </summary>
    public class BannerAdView : MonoBehaviour
    {
        [SerializeField] private RawImage _webView;
        [SerializeField] private Text _loadingLabel;
        [SerializeField] private Button _clickButton;
        [SerializeField] private Canvas _canvas;
        
        private string _placementId;
        private IAdCallback _callback;
        private AdPosition _bannerPosition = AdPosition.Header;
        private bool _isLoaded = false;
        private float _cornerRadius = 0f;
        private float _bannerHeight = 50f;
        private float _bannerWidth = 320f;
        private string _clickURL;
        private WebViewObject _webViewObject;
        
        // Attachment state like iOS
        private bool _isAttachedToScreen = false;
        private GameObject _parentView;

        private void Awake()
        {
            SetupBannerView();
        }

        private void SetupBannerView()
        {
            // Set banner dimensions based on position like iOS
            switch (_bannerPosition)
            {
                case AdPosition.Header:
                case AdPosition.Footer:
                    _bannerHeight = 50f;
                    _bannerWidth = Screen.width;
                    break;
                case AdPosition.Sidebar:
                    _bannerHeight = 250f;
                    _bannerWidth = 120f;
                    break;
                default:
                    _bannerHeight = 50f;
                    _bannerWidth = 320f;
                    break;
            }
            
            // Setup background like iOS
            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray like iOS
            
            // Add corner radius
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(_bannerWidth, _bannerHeight);
            
            // Add shadow effect like iOS
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.1f);
            shadow.effectDistance = new Vector2(0, 2);
            
            SetupLoadingLabel();
            SetupWebView();
            SetupConstraints();
            
            // Create click button
            _clickButton = gameObject.AddComponent<Button>();
            _clickButton.onClick.AddListener(OnAdClicked);
        }

        private void SetupLoadingLabel()
        {
            var loadingObj = new GameObject("LoadingLabel");
            loadingObj.transform.SetParent(transform);
            _loadingLabel = loadingObj.AddComponent<Text>();
            _loadingLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _loadingLabel.fontSize = 12;
            _loadingLabel.color = Color.white;
            _loadingLabel.alignment = TextAnchor.MiddleCenter;
            _loadingLabel.text = "Loading Banner...";
        }

        private void SetupWebView()
        {
            var webViewObj = new GameObject("WebView");
            webViewObj.transform.SetParent(transform);
            _webView = webViewObj.AddComponent<RawImage>();
            _webView.color = Color.clear;
        }

        private void SetupConstraints()
        {
            var webViewRect = _webView.GetComponent<RectTransform>();
            var loadingRect = _loadingLabel.GetComponent<RectTransform>();
            
            // WebView fills the entire area
            webViewRect.anchorMin = Vector2.zero;
            webViewRect.anchorMax = Vector2.one;
            webViewRect.offsetMin = Vector2.zero;
            webViewRect.offsetMax = Vector2.zero;
            
            // Loading label centered
            loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.sizeDelta = new Vector2(100, 24);
            loadingRect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Set placement info - Identical to iOS
        /// </summary>
        /// <param name="placementId">Placement ID</param>
        /// <param name="callback">Ad callback</param>
        public void SetPlacementInfo(string placementId, IAdCallback callback)
        {
            _placementId = placementId;
            _callback = callback;
        }

        /// <summary>
        /// Load ad from URL - Identical to iOS loadAdFromURL
        /// </summary>
        /// <param name="url">Ad URL</param>
        public void LoadAdFromURL(string url)
        {
            _loadingLabel.gameObject.SetActive(true);
            _loadingLabel.text = "Loading Banner...";
            
            Debug.Log($" BannerAdView: Making HTTP request to: {url}");
            StartCoroutine(LoadAdCoroutine(url));
        }

        private IEnumerator LoadAdCoroutine(string url)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var htmlContent = request.downloadHandler.text;
                    Debug.Log($" BannerAdView: Received response: {htmlContent}");
                    
                    try
                    {
                        // Try to parse JSON response like iOS
                        var json = JsonUtility.FromJson<AdResponse>(htmlContent);
                        if (json != null && !string.IsNullOrEmpty(json.adm))
                        {
                            Debug.Log($" Banner Adm: {json.adm}");
                            
                            if (json.position != null)
                            {
                                var position = (AdPosition)json.position;
                                Debug.Log($" BannerAdView: Received position from server: {json.position} - {GetDisplayName(position)}");
                                BidscubeSDK.SetResponseAdPosition(position);
                            }
                            
                            LoadAdContent(json.adm);
                        }
                        else
                        {
                            LoadAdContent(htmlContent);
                        }
                    }
                    catch
                    {
                        LoadAdContent(htmlContent);
                    }
                }
                else
                {
                    _loadingLabel.text = $"Error: {request.error}";
                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.NetworkError, request.error);
                }
            }
        }

        /// <summary>
        /// Load ad content - Identical to iOS loadAdContent
        /// </summary>
        /// <param name="htmlContent">HTML content</param>
        public void LoadAdContent(string htmlContent)
        {
            _loadingLabel.gameObject.SetActive(true);
            
            // Extract click URL from HTML like iOS
            ExtractClickURLFromHTML(htmlContent);
            
            // Clean HTML like iOS
            var cleanHTML = htmlContent
                .Replace("document.write(", "")
                .Replace(");", "")
                .Trim();
            
            // Create full HTML document like iOS
            var fullHTML = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ 
            margin: 0; 
            padding: 0; 
            overflow: hidden; 
            background: transparent;
        }}
        * {{ 
            box-sizing: border-box; 
        }}
        img {{
            max-width: 100%;
            height: auto;
            display: block;
        }}
    </style>
</head>
<body>
    {cleanHTML}
</body>
</html>";
            
            // For Unity, we'll render this as a texture
            StartCoroutine(RenderHTMLToTexture(fullHTML));
            
            // Hide loading after 2 seconds like iOS
            StartCoroutine(HideLoadingAfterDelay(2.0f));
        }

        private IEnumerator RenderHTMLToTexture(string htmlContent)
        {
            // In a real implementation, you'd use a WebView plugin
            // For now, we'll create a placeholder texture
            var texture = new Texture2D((int)_bannerWidth, (int)_bannerHeight);
            var colors = new Color[(int)(_bannerWidth * _bannerHeight)];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(0.9f, 0.9f, 0.9f, 1f);
            }
            texture.SetPixels(colors);
            texture.Apply();
            
            _webView.texture = texture;
            _isLoaded = true;
            
            _callback?.OnAdLoaded(_placementId);
            _callback?.OnAdDisplayed(_placementId);
            
            yield return null;
        }

        private IEnumerator HideLoadingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _loadingLabel.gameObject.SetActive(false);
        }

        private void ExtractClickURLFromHTML(string htmlContent)
        {
            var patterns = new[]
            {
                @"https?://[^""'\\s]+",
                @"curl=([^&""'\\s]+)"
            };
            
            foreach (var pattern in patterns)
            {
                var regex = new Regex(pattern);
                var match = regex.Match(htmlContent);
                if (match.Success)
                {
                    _clickURL = match.Value;
                    Debug.Log($" BannerAdView: Extracted click URL from HTML: {_clickURL}");
                    return;
                }
            }
            
            Debug.Log(" BannerAdView: Could not extract click URL from HTML content");
        }

        private string GetDisplayName(AdPosition position)
        {
            switch (position)
            {
                case AdPosition.Unknown: return "UNKNOWN";
                case AdPosition.AboveTheFold: return "ABOVE_THE_FOLD";
                case AdPosition.DependOnScreenSize: return "DEPEND_ON_SCREEN_SIZE";
                case AdPosition.BelowTheFold: return "BELOW_THE_FOLD";
                case AdPosition.Header: return "HEADER";
                case AdPosition.Footer: return "FOOTER";
                case AdPosition.Sidebar: return "SIDEBAR";
                case AdPosition.FullScreen: return "FULL_SCREEN";
                default: return "UNKNOWN";
            }
        }

        private void OnAdClicked()
        {
            Debug.Log(" BannerAdView: Tap gesture detected");
            
            _callback?.OnAdClicked(_placementId);
            
            if (!string.IsNullOrEmpty(_clickURL))
            {
                Debug.Log($" BannerAdView: Opening extracted click URL: {_clickURL}");
                Application.OpenURL(_clickURL);
            }
            else
            {
                Debug.Log(" BannerAdView: No click URL available to open");
            }
        }

        /// <summary>
        /// Attach banner to screen - Identical to iOS attachToScreen
        /// </summary>
        /// <param name="parentView">Parent view to attach to</param>
        public void AttachToScreen(GameObject parentView)
        {
            if (_isAttachedToScreen) return;
            
            _parentView = parentView;
            _isAttachedToScreen = true;
            
            transform.SetParent(parentView.transform, false);
            
            SetupBannerConstraints();
            
            // Fade in animation like iOS
            var canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0;
            StartCoroutine(FadeInAnimation());
            
            Debug.Log($" BannerAdView: Attached to screen at {_bannerPosition}");
        }

        /// <summary>
        /// Detach banner from screen - Identical to iOS detachFromScreen
        /// </summary>
        public void DetachFromScreen()
        {
            if (!_isAttachedToScreen) return;
            
            StartCoroutine(FadeOutAndDetach());
            
            Debug.Log(" BannerAdView: Detached from screen");
        }

        private IEnumerator FadeInAnimation()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            var duration = 0.3f;
            var elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
                yield return null;
            }
            
            canvasGroup.alpha = 1;
        }

        private IEnumerator FadeOutAndDetach()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            var duration = 0.3f;
            var elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
                yield return null;
            }
            
            transform.SetParent(null);
            _isAttachedToScreen = false;
            _parentView = null;
            
            // Untrack banner like iOS
            BidscubeSDK.UntrackBanner(this);
        }

        private void SetupBannerConstraints()
        {
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(_bannerWidth, _bannerHeight);
            
            switch (_bannerPosition)
            {
                case AdPosition.Header:
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.offsetMin = new Vector2(0, -_bannerHeight);
                    rectTransform.offsetMax = new Vector2(0, 0);
                    break;
                case AdPosition.Footer:
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.offsetMin = new Vector2(0, 0);
                    rectTransform.offsetMax = new Vector2(0, _bannerHeight);
                    break;
                case AdPosition.Sidebar:
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.offsetMin = new Vector2(-_bannerWidth, 0);
                    rectTransform.offsetMax = new Vector2(0, 0);
                    break;
                default:
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.offsetMin = new Vector2(-_bannerWidth / 2, -_bannerHeight / 2);
                    rectTransform.offsetMax = new Vector2(_bannerWidth / 2, _bannerHeight / 2);
                    break;
            }
        }

        /// <summary>
        /// Set banner dimensions - Identical to iOS setBannerDimensions
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void SetBannerDimensions(float width, float height)
        {
            _bannerWidth = width;
            _bannerHeight = height;
            
            // Reattach if currently attached
            if (_isAttachedToScreen)
            {
                DetachFromScreen();
                if (_parentView != null)
                {
                    AttachToScreen(_parentView);
                }
            }
        }

        /// <summary>
        /// Set banner position - Identical to iOS setBannerPosition
        /// </summary>
        /// <param name="position">Ad position</param>
        public void SetBannerPosition(AdPosition position)
        {
            _bannerPosition = position;
            
            // Reattach if currently attached
            if (_isAttachedToScreen)
            {
                DetachFromScreen();
                if (_parentView != null)
                {
                    AttachToScreen(_parentView);
                }
            }
        }

        /// <summary>
        /// Set corner radius - Identical to iOS setCornerRadius
        /// </summary>
        /// <param name="radius">Corner radius</param>
        public void SetCornerRadius(float radius)
        {
            _cornerRadius = radius;
            // Note: Unity UI doesn't have built-in corner radius support
            // This would need to be implemented using a custom shader or third-party solution
        }

        /// <summary>
        /// Show banner
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide banner
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Destroy banner ad view
        /// </summary>
        public void Destroy()
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }
}