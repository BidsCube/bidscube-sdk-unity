using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

namespace BidscubeSDK
{
    /// <summary>
    /// Video ad view component
    /// </summary>
    public class VideoAdView : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _skipText;
        [SerializeField] private Slider _progressSlider;
        
        private string _placementId;
        private IAdCallback _callback;
        private bool _isLoaded = false;
        private bool _isSkippable = false;
        private float _skipTime = 5.0f; // Skip button appears after 5 seconds

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            // Create video player
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();
                _videoPlayer.playOnAwake = false;
                _videoPlayer.isLooping = false;
                _videoPlayer.prepareCompleted += OnVideoPrepared;
                _videoPlayer.started += OnVideoStarted;
                _videoPlayer.loopPointReached += OnVideoCompleted;
            }

            // Create skip button
            if (_skipButton == null)
            {
                var skipObj = new GameObject("SkipButton");
                skipObj.transform.SetParent(transform);
                _skipButton = skipObj.AddComponent<Button>();
                _skipButton.onClick.AddListener(OnSkipClicked);
                _skipButton.gameObject.SetActive(false);
            }

            // Create close button
            if (_closeButton == null)
            {
                var closeObj = new GameObject("CloseButton");
                closeObj.transform.SetParent(transform);
                _closeButton = closeObj.AddComponent<Button>();
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            // Create skip text
            if (_skipText == null)
            {
                var textObj = new GameObject("SkipText");
                textObj.transform.SetParent(_skipButton.transform);
                _skipText = textObj.AddComponent<Text>();
                _skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _skipText.fontSize = 12;
                _skipText.color = Color.white;
                _skipText.alignment = TextAnchor.MiddleCenter;
                _skipText.text = "Skip";
            }

            // Create progress slider
            if (_progressSlider == null)
            {
                var sliderObj = new GameObject("ProgressSlider");
                sliderObj.transform.SetParent(transform);
                _progressSlider = sliderObj.AddComponent<Slider>();
                _progressSlider.minValue = 0;
                _progressSlider.maxValue = 1;
                _progressSlider.value = 0;
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
        /// Load video ad from URL
        /// </summary>
        /// <param name="url">Video URL</param>
        public void LoadVideoAdFromURL(string url)
        {
            StartCoroutine(LoadVideoAdCoroutine(url));
        }

        private IEnumerator LoadVideoAdCoroutine(string url)
        {
            _videoPlayer.url = url;
            _videoPlayer.Prepare();

            while (!_videoPlayer.isPrepared)
            {
                yield return null;
            }

            _isLoaded = true;
            _callback?.OnAdLoaded(_placementId);
            _callback?.OnAdDisplayed(_placementId);
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            _isLoaded = true;
            _callback?.OnAdLoaded(_placementId);
            _callback?.OnAdDisplayed(_placementId);
        }

        private void OnVideoStarted(VideoPlayer source)
        {
            _callback?.OnVideoAdStarted(_placementId);
            StartCoroutine(UpdateProgress());
            StartCoroutine(EnableSkipButton());
        }

        private void OnVideoCompleted(VideoPlayer source)
        {
            _callback?.OnVideoAdCompleted(_placementId);
        }

        private IEnumerator UpdateProgress()
        {
            while (_videoPlayer.isPlaying)
            {
                if (_videoPlayer.frameCount > 0)
                {
                    _progressSlider.value = (float)_videoPlayer.frame / _videoPlayer.frameCount;
                }
                yield return null;
            }
        }

        private IEnumerator EnableSkipButton()
        {
            yield return new WaitForSeconds(_skipTime);
            _isSkippable = true;
            _skipButton.gameObject.SetActive(true);
            _callback?.OnVideoAdSkippable(_placementId);
        }

        private void OnSkipClicked()
        {
            if (_isSkippable)
            {
                _callback?.OnVideoAdSkipped(_placementId);
                _callback?.OnAdClosed(_placementId);
                Destroy();
            }
        }

        private void OnCloseClicked()
        {
            _callback?.OnAdClosed(_placementId);
            Destroy();
        }

        /// <summary>
        /// Play video
        /// </summary>
        public void Play()
        {
            if (_isLoaded && _videoPlayer != null)
            {
                _videoPlayer.Play();
            }
        }

        /// <summary>
        /// Pause video
        /// </summary>
        public void Pause()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Pause();
            }
        }

        /// <summary>
        /// Stop video
        /// </summary>
        public void Stop()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
        }

        /// <summary>
        /// Set video size
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void SetVideoSize(float width, float height)
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }

        /// <summary>
        /// Show video ad
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            Play();
        }

        /// <summary>
        /// Hide video ad
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            Stop();
        }

        /// <summary>
        /// Destroy video ad view
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