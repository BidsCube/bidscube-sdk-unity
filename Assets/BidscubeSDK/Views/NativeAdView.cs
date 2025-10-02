using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BidscubeSDK
{
    /// <summary>
    /// Native ad view component
    /// </summary>
    public class NativeAdView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Image _mainImage;
        [SerializeField] private Button _installButton;
        [SerializeField] private Text _installButtonText;
        [SerializeField] private Text _sponsoredText;
        
        private string _placementId;
        private IAdCallback _callback;
        private bool _isLoaded = false;
        private NativeAdData _adData;

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            // Create icon image
            if (_iconImage == null)
            {
                var iconObj = new GameObject("IconImage");
                iconObj.transform.SetParent(transform);
                _iconImage = iconObj.AddComponent<Image>();
            }

            // Create title text
            if (_titleText == null)
            {
                var titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(transform);
                _titleText = titleObj.AddComponent<Text>();
                _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _titleText.fontSize = 16;
                _titleText.fontStyle = FontStyle.Bold;
                _titleText.color = Color.black;
            }

            // Create description text
            if (_descriptionText == null)
            {
                var descObj = new GameObject("DescriptionText");
                descObj.transform.SetParent(transform);
                _descriptionText = descObj.AddComponent<Text>();
                _descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _descriptionText.fontSize = 12;
                _descriptionText.color = Color.gray;
            }

            // Create main image
            if (_mainImage == null)
            {
                var mainObj = new GameObject("MainImage");
                mainObj.transform.SetParent(transform);
                _mainImage = mainObj.AddComponent<Image>();
            }

            // Create install button
            if (_installButton == null)
            {
                var buttonObj = new GameObject("InstallButton");
                buttonObj.transform.SetParent(transform);
                _installButton = buttonObj.AddComponent<Button>();
                _installButton.onClick.AddListener(OnInstallButtonClicked);
            }

            // Create install button text
            if (_installButtonText == null)
            {
                var buttonTextObj = new GameObject("InstallButtonText");
                buttonTextObj.transform.SetParent(_installButton.transform);
                _installButtonText = buttonTextObj.AddComponent<Text>();
                _installButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _installButtonText.fontSize = 14;
                _installButtonText.color = Color.white;
                _installButtonText.alignment = TextAnchor.MiddleCenter;
                _installButtonText.text = "Install";
            }

            // Create sponsored text
            if (_sponsoredText == null)
            {
                var sponsoredObj = new GameObject("SponsoredText");
                sponsoredObj.transform.SetParent(transform);
                _sponsoredText = sponsoredObj.AddComponent<Text>();
                _sponsoredText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _sponsoredText.fontSize = 10;
                _sponsoredText.color = Color.gray;
                _sponsoredText.text = "Sponsored";
            }
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
        /// Load native ad from URL
        /// </summary>
        /// <param name="url">Ad URL</param>
        public void LoadNativeAdFromURL(string url)
        {
            StartCoroutine(LoadNativeAdCoroutine(url));
        }

        private IEnumerator LoadNativeAdCoroutine(string url)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    try
                    {
                        _adData = JsonUtility.FromJson<NativeAdData>(request.downloadHandler.text);
                        PopulateAdData();
                        _isLoaded = true;
                        
                        _callback?.OnAdLoaded(_placementId);
                        _callback?.OnAdDisplayed(_placementId);
                    }
                    catch (System.Exception e)
                    {
                        _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.InvalidResponse, e.Message);
                    }
                }
                else
                {
                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.NetworkError, request.error);
                }
            }
        }

        private void PopulateAdData()
        {
            if (_adData == null) return;

            // Set title
            if (_titleText != null && !string.IsNullOrEmpty(_adData.title))
            {
                _titleText.text = _adData.title;
            }

            // Set description
            if (_descriptionText != null && !string.IsNullOrEmpty(_adData.description))
            {
                _descriptionText.text = _adData.description;
            }

            // Set install button text
            if (_installButtonText != null && !string.IsNullOrEmpty(_adData.installButtonText))
            {
                _installButtonText.text = _adData.installButtonText;
            }

            // Load images
            if (!string.IsNullOrEmpty(_adData.iconUrl))
            {
                StartCoroutine(LoadImage(_adData.iconUrl, _iconImage));
            }

            if (!string.IsNullOrEmpty(_adData.mainImageUrl))
            {
                StartCoroutine(LoadImage(_adData.mainImageUrl, _mainImage));
            }
        }

        private IEnumerator LoadImage(string url, Image targetImage)
        {
            using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    targetImage.sprite = sprite;
                }
            }
        }

        private void OnInstallButtonClicked()
        {
            if (_isLoaded && _adData != null)
            {
                _callback?.OnInstallButtonClicked(_placementId, _adData.installButtonText);
                
                // Open app store URL
                if (!string.IsNullOrEmpty(_adData.storeUrl))
                {
                    Application.OpenURL(_adData.storeUrl);
                }
            }
        }

        /// <summary>
        /// Set native ad size
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void SetNativeAdSize(float width, float height)
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }

        /// <summary>
        /// Show native ad
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide native ad
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Destroy native ad view
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
    /// Native ad data structure
    /// </summary>
    [System.Serializable]
    public class NativeAdData
    {
        public string title;
        public string description;
        public string iconUrl;
        public string mainImageUrl;
        public string installButtonText;
        public string storeUrl;
        public string advertiser;
        public string rating;
    }
}