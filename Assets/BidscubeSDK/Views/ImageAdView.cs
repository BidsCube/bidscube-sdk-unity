using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

namespace BidscubeSDK
{
    /// <summary>
    /// Image ad view component - Identical to iOS ImageAdView
    /// </summary>
    public class ImageAdView : MonoBehaviour
    {
        [SerializeField] private RawImage _webView;
        [SerializeField] private Text _loadingLabel;
        [SerializeField] private Button _clickButton;
        
        private string _placementId;
        private IAdCallback _callback;
        private bool _isLoaded = false;
        private string _clickURL;
        private Texture2D _adTexture;

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            // Setup background
            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray like iOS
            
            // Add corner radius effect
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(320, 250);
            
            // Create web view (RawImage for texture display)
            var webViewObj = new GameObject("WebView");
            webViewObj.transform.SetParent(transform);
            _webView = webViewObj.AddComponent<RawImage>();
            _webView.color = Color.clear;
            
            // Create loading label
            var loadingObj = new GameObject("LoadingLabel");
            loadingObj.transform.SetParent(transform);
            _loadingLabel = loadingObj.AddComponent<Text>();
            _loadingLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _loadingLabel.fontSize = 14;
            _loadingLabel.color = Color.white;
            _loadingLabel.alignment = TextAnchor.MiddleCenter;
            _loadingLabel.text = "Loading Ad...";
            
            // Create click button (invisible overlay)
            _clickButton = gameObject.AddComponent<Button>();
            _clickButton.onClick.AddListener(OnAdClicked);
            
            // Setup constraints
            SetupConstraints();
        }

        private void SetupConstraints()
        {
            var rectTransform = GetComponent<RectTransform>();
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
            loadingRect.sizeDelta = new Vector2(120, 30);
            loadingRect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Set placement info
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
            _loadingLabel.text = "Loading Ad...";
            
            Debug.Log($"🔍 ImageAdView: Making HTTP request to: {url}");
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
                    Debug.Log($"🔍 ImageAdView: Received response: {htmlContent}");
                    
                    try
                    {
                        // Try to parse JSON response like iOS
                        var json = JsonUtility.FromJson<AdResponse>(htmlContent);
                        if (json != null && !string.IsNullOrEmpty(json.adm))
                        {
                            Debug.Log($"🔍 Adm: {json.adm}");
                            
                            if (json.position != null)
                            {
                                var position = (AdPosition)json.position;
                                Debug.Log($"🔍 ImageAdView: Received position from server: {json.position} - {GetDisplayName(position)}");
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
        body {{ margin: 0; padding: 0; overflow: hidden; }}
        * {{ box-sizing: border-box; }}
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
            var texture = new Texture2D(320, 250);
            var colors = new Color[320 * 250];
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
                    Debug.Log($"🔍 ImageAdView: Extracted click URL from HTML: {_clickURL}");
                    return;
                }
            }
            
            Debug.Log("⚠️ ImageAdView: Could not extract click URL from HTML content");
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
            Debug.Log("🔍 ImageAdView: Tap gesture detected");
            
            _callback?.OnAdClicked(_placementId);
            
            if (!string.IsNullOrEmpty(_clickURL))
            {
                Debug.Log($"🔍 ImageAdView: Opening extracted click URL: {_clickURL}");
                Application.OpenURL(_clickURL);
            }
            else
            {
                Debug.Log("⚠️ ImageAdView: No click URL available to open");
            }
        }

        /// <summary>
        /// Show ad
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide ad
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Destroy ad view
        /// </summary>
        public void Destroy()
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Ad response structure - Identical to iOS
    /// </summary>
    [System.Serializable]
    public class AdResponse
    {
        public string adm;
        public int? position;
    }
}