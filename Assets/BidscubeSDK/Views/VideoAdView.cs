using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace BidscubeSDK
{
    /// <summary>
    /// Video ad view component with VAST support
    /// </summary>
    public class VideoAdView : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _clickButton; // Full screen clickable area
        [SerializeField] private Text _skipText;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private RawImage _videoTexture;

        private string _placementId;
        private IAdCallback _callback;
        private bool _isLoaded = false;
        private bool _isSkippable = false;
        private float _skipTime = 5.0f; // Skip button appears after 5 seconds

        // VAST data
        private VASTParser.VASTData _vastData;
        private bool _hasFiredImpression = false;
        private bool _hasFiredStart = false;
        private bool _hasFiredFirstQuartile = false;
        private bool _hasFiredMidpoint = false;
        private bool _hasFiredThirdQuartile = false;
        private bool _hasFiredComplete = false;

        // IMA player (when available)
        private IMAVideoPlayer _imaPlayer;
        private bool _useIMA = false;

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            // Check if IMA SDK is available
            _useIMA = IMAVideoPlayer.IsIMAAvailable();

            if (_useIMA)
            {
                Logger.Info("[VideoAdView] IMA SDK detected, will use IMA player for video ads");
                SetupIMA();
            }
            else
            {
                Logger.Info("[VideoAdView] IMA SDK not available, using custom VAST parser");
            }

            // Setup full screen canvas
            SetupFullScreenCanvas();

            // Create video texture for rendering
            if (_videoTexture == null)
            {
                var textureObj = new GameObject("VideoTexture");
                textureObj.transform.SetParent(transform, false);
                _videoTexture = textureObj.AddComponent<RawImage>();
                var rectTransform = _videoTexture.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            // Create video player
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();
                _videoPlayer.playOnAwake = false;
                _videoPlayer.isLooping = false;
                _videoPlayer.renderMode = VideoRenderMode.RenderTexture;

                // Create render texture
                var renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
                _videoPlayer.targetTexture = renderTexture;
                _videoTexture.texture = renderTexture;

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

                var sliderRect = sliderObj.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0, 0);
                sliderRect.anchorMax = new Vector2(1, 0);
                sliderRect.sizeDelta = new Vector2(0, 10);
                sliderRect.anchoredPosition = new Vector2(0, 10);
            }

            // Create full screen click button for click-through
            if (_clickButton == null)
            {
                var clickObj = new GameObject("ClickButton");
                clickObj.transform.SetParent(transform, false);

                // Ensure RectTransform exists (Button requires it)
                var clickRect = clickObj.GetComponent<RectTransform>();
                if (clickRect == null)
                {
                    clickRect = clickObj.AddComponent<RectTransform>();
                }

                _clickButton = clickObj.AddComponent<Button>();
                _clickButton.onClick.AddListener(OnVideoClicked);

                clickRect.anchorMin = Vector2.zero;
                clickRect.anchorMax = Vector2.one;
                clickRect.offsetMin = Vector2.zero;
                clickRect.offsetMax = Vector2.zero;

                // Make button transparent but clickable
                var image = clickObj.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0);
            }
        }

        private void SetupFullScreenCanvas()
        {
            // Ensure we have a canvas for full screen display
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("VideoAdCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999; // Very high to be on top

                var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

                canvasObj.AddComponent<GraphicRaycaster>();

                transform.SetParent(canvasObj.transform, false);
            }

            // Always make video full screen
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            Logger.Info("[VideoAdView] Setting up full screen display");
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void SetupIMA()
        {
            // Create IMA player component
            _imaPlayer = gameObject.AddComponent<IMAVideoPlayer>();
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

            // Initialize IMA player if available
            if (_useIMA && _imaPlayer != null)
            {
                _imaPlayer.Initialize(placementId, callback);
            }
        }

        /// <summary>
        /// Load video ad from URL (supports VAST XML or direct video URL)
        /// Uses IMA SDK if available, otherwise uses custom VAST parser
        /// </summary>
        /// <param name="url">VAST XML URL or direct video URL</param>
        public void LoadVideoAdFromURL(string url)
        {
            if (_useIMA && _imaPlayer != null)
            {
                // Use IMA SDK
                Logger.Info("[VideoAdView] Loading video ad with IMA SDK");
                _imaPlayer.RequestAd(url);
                _callback?.OnAdLoading(_placementId);
            }
            else
            {
                // Use custom VAST parser
                StartCoroutine(LoadVideoAdCoroutine(url));
            }
        }

        private IEnumerator LoadVideoAdCoroutine(string url)
        {
            // First, fetch the content from URL
            using (var request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Logger.InfoError($"[VideoAdView] Failed to load ad from URL: {request.error}");
                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.NetworkError, request.error);
                    yield break;
                }

                var responseText = request.downloadHandler.text;
                Logger.Info($"[VideoAdView] Received response ({responseText.Length} chars)");
                Logger.Info($"[VideoAdView] Full response content:\n{responseText}");

                // Check if response is VAST XML
                if (responseText.TrimStart().StartsWith("<VAST") || responseText.Contains("<VAST"))
                {
                    Logger.Info("[VideoAdView] Detected VAST XML, parsing...");

                    // First test with default VAST to ensure parser is working
                    Logger.Info("[VideoAdView] Testing parser with default VAST XML first...");
                    bool testSuccess = VASTParser.TestDefaultVAST();
                    if (!testSuccess)
                    {
                        Logger.InfoError("[VideoAdView] Parser test failed, but continuing with actual VAST...");
                    }

                    // Now parse the actual VAST from server
                    _vastData = VASTParser.Parse(responseText);

                    if (_vastData == null || string.IsNullOrEmpty(_vastData.videoUrl))
                    {
                        Logger.InfoError("[VideoAdView] Failed to parse VAST or no video URL found");
                        _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.InvalidResponse, "Failed to parse VAST XML");
                        yield break;
                    }

                    // Fire impression tracking URLs
                    VASTParser.FireTrackingUrls(_vastData.impressionUrls);
                    _hasFiredImpression = true;

                    // Set skip time from VAST skipoffset
                    if (_vastData.skipOffset > 0)
                    {
                        _skipTime = _vastData.skipOffset;
                    }

                    // Load the actual video
                    _videoPlayer.url = _vastData.videoUrl;
                }
                else
                {
                    // Direct video URL or JSON response
                    Logger.Info("[VideoAdView] Treating as direct video URL or JSON response");

                    // Try to parse as JSON first (similar to BannerAdView)
                    if (responseText.TrimStart().StartsWith("{"))
                    {
                        AdResponse json = null;
                        try
                        {
                            json = JsonUtility.FromJson<AdResponse>(responseText);
                        }
                        catch (System.Exception e)
                        {
                            Logger.InfoError($"[VideoAdView] JSON parsing failed: {e.Message}");
                            _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.InvalidResponse, $"JSON parsing failed: {e.Message}");
                            yield break;
                        }

                        // Handle nested adm structure: {"adm":{"adm":"<VAST>...","position":7}}
                        string admValue = null;
                        if (json != null && json.adm != null)
                        {
                            admValue = json.adm.adm;

                            Logger.Info("[VideoAdView] Extracted adm from nested structure");
                        }

                        if (!string.IsNullOrEmpty(admValue))
                        {
                            // Log the raw adm field as received
                            Logger.Info($"[VideoAdView] ========== RAW ADM FIELD RECEIVED ==========");
                            Logger.Info($"[VideoAdView] Raw adm length: {admValue.Length} chars");
                            Logger.Info($"[VideoAdView] Raw adm content:\n{admValue}");
                            Logger.Info($"[VideoAdView] ============================================");

                            // Extract and unescape adm field (similar to BannerAdView)
                            string admContent = admValue.Trim();

                            // If wrapped in quotes, remove them
                            if ((admContent.StartsWith("\"") && admContent.EndsWith("\"")) ||
                                (admContent.StartsWith("'") && admContent.EndsWith("'")))
                            {
                                admContent = admContent.Substring(1, admContent.Length - 2);
                            }

                            // Unescape common JSON string escapes
                            admContent = admContent
                                .Replace("\\\"", "\"")
                                .Replace("\\'", "'")
                                .Replace("\\n", "\n")  // Keep newlines for better logging
                                .Replace("\\r", "\r")
                                .Replace("\\t", "\t")
                                .Replace("\\/", "/");

                            Logger.Info($"[VideoAdView] ========== PROCESSED ADM CONTENT ==========");
                            Logger.Info($"[VideoAdView] Processed adm length: {admContent.Length} chars");
                            Logger.Info($"[VideoAdView] Full processed adm content:\n{admContent}");
                            Logger.Info($"[VideoAdView] ============================================");

                            // Check if adm is VAST XML
                            if (admContent.TrimStart().StartsWith("<VAST") || admContent.Contains("<VAST"))
                            {
                                Logger.Info("[VideoAdView] Detected VAST XML in adm field, parsing...");

                                // First test with default VAST to ensure parser is working
                                Logger.Info("[VideoAdView] Testing parser with default VAST XML first...");
                                bool testSuccess = VASTParser.TestDefaultVAST();
                                if (!testSuccess)
                                {
                                    Logger.InfoError("[VideoAdView] Parser test failed, but continuing with actual VAST...");
                                }

                                // Check if it's a Wrapper VAST (needs to fetch nested VAST)
                                if (VASTParser.IsWrapperVAST(admContent))
                                {
                                    Logger.Info("[VideoAdView] Detected Wrapper VAST, fetching nested VAST...");
                                    var vastAdTagUri = VASTParser.ExtractVASTAdTagURI(admContent);

                                    if (!string.IsNullOrEmpty(vastAdTagUri))
                                    {
                                        Logger.Info($"[VideoAdView] Fetching nested VAST from: {vastAdTagUri}");

                                        // Fetch the nested VAST (outside try-catch to allow yield return)
                                        // Handle nested wrappers recursively
                                        yield return StartCoroutine(FetchNestedVASTRecursive(vastAdTagUri, admContent));

                                        if (_vastData == null || string.IsNullOrEmpty(_vastData.videoUrl))
                                        {
                                            Logger.InfoError("[VideoAdView] Failed to fetch or parse nested VAST");
                                            _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.NetworkError, "Failed to fetch or parse nested VAST");
                                            yield break;
                                        }
                                    }
                                    else
                                    {
                                        Logger.InfoError("[VideoAdView] Wrapper VAST found but no VASTAdTagURI");
                                        _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.InvalidResponse, "Wrapper VAST has no VASTAdTagURI");
                                        yield break;
                                    }
                                }
                                else
                                {
                                    // Regular InLine VAST
                                    _vastData = VASTParser.Parse(admContent);
                                }

                                if (_vastData == null)
                                {
                                    Logger.InfoError("[VideoAdView] VASTParser returned null - parsing failed");
                                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.InvalidResponse, "VAST parsing failed");
                                    yield break;
                                }

                                if (_vastData != null && !string.IsNullOrEmpty(_vastData.videoUrl))
                                {
                                    Logger.Info($"[VideoAdView] Successfully parsed VAST, video URL: {_vastData.videoUrl}");
                                    _videoPlayer.url = _vastData.videoUrl;
                                    VASTParser.FireTrackingUrls(_vastData.impressionUrls);
                                    _hasFiredImpression = true;

                                    if (_vastData.skipOffset > 0)
                                    {
                                        _skipTime = _vastData.skipOffset;
                                    }
                                }
                                else
                                {
                                    Logger.InfoError("[VideoAdView] VAST parsed but no video URL found");
                                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.InvalidResponse, "VAST parsed but no video URL found");
                                    yield break;
                                }
                            }
                            else
                            {
                                Logger.Info("[VideoAdView] adm field is not VAST XML, treating as direct video URL");
                                _videoPlayer.url = admContent; // Try adm as direct URL
                            }
                        }
                        else
                        {
                            Logger.Info("[VideoAdView] JSON response has no adm field, treating original URL as direct video URL");
                            _videoPlayer.url = url; // Direct video URL
                        }
                    }
                    else
                    {
                        // Direct video URL
                        Logger.Info("[VideoAdView] Treating response as direct video URL");
                        _videoPlayer.url = url;
                    }
                }

                // Validate video URL before preparing
                if (string.IsNullOrEmpty(_videoPlayer.url))
                {
                    Logger.InfoError("[VideoAdView] No video URL available to play");
                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.InvalidResponse, "No video URL found in response");
                    yield break;
                }

                Logger.Info($"[VideoAdView] Preparing video player with URL: {_videoPlayer.url}");

                // Prepare video
                _videoPlayer.Prepare();

                float timeout = 30f; // 30 second timeout
                float elapsed = 0f;

                while (!_videoPlayer.isPrepared && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!_videoPlayer.isPrepared)
                {
                    Logger.InfoError("[VideoAdView] Video preparation timeout");
                    _callback?.OnAdFailed(_placementId, Constants.ErrorCodes.Timeout, "Video preparation timeout");
                    yield break;
                }

                _isLoaded = true;
                Logger.Info("[VideoAdView] Video prepared successfully");
                _callback?.OnAdLoaded(_placementId);
                _callback?.OnAdDisplayed(_placementId);

                // Automatically play the video after preparation
                Logger.Info("[VideoAdView] Starting video playback...");
                _videoPlayer.Play();
            }
        }

        [System.Serializable]
        private class AdResponse
        {
            public AdResponseInner adm; // Handle nested adm object: {"adm":{"adm":"<VAST>..."}}
            public int? position;
        }

        [System.Serializable]
        private class AdResponseInner
        {
            public string adm; // The actual VAST XML string
            public int? position;
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            _isLoaded = true;
            _callback?.OnAdLoaded(_placementId);
            _callback?.OnAdDisplayed(_placementId);

            // Automatically play the video when prepared
            if (!_videoPlayer.isPlaying)
            {
                Logger.Info("[VideoAdView] Video prepared, starting playback...");
                _videoPlayer.Play();
            }
        }

        private void OnVideoStarted(VideoPlayer source)
        {
            // Fire VAST start tracking URLs
            if (!_hasFiredStart && _vastData != null)
            {
                VASTParser.FireTrackingUrls(_vastData.startUrls);
                _hasFiredStart = true;
            }

            _callback?.OnVideoAdStarted(_placementId);
            StartCoroutine(UpdateProgress());
            StartCoroutine(EnableSkipButton());
            StartCoroutine(TrackVASTQuartiles());
        }

        private void OnVideoCompleted(VideoPlayer source)
        {
            // Fire VAST complete tracking URLs
            if (!_hasFiredComplete && _vastData != null)
            {
                VASTParser.FireTrackingUrls(_vastData.completeUrls);
                _hasFiredComplete = true;
            }

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

        /// <summary>
        /// Recursively fetch nested VAST from wrapper (handles multiple wrapper levels)
        /// </summary>
        private IEnumerator FetchNestedVASTRecursive(string vastAdTagUri, string wrapperVastXml, int depth = 0)
        {
            if (depth > 5)
            {
                Logger.InfoError("[VideoAdView] Maximum wrapper depth reached");
                yield break;
            }

            Logger.Info($"[VideoAdView] Fetching nested VAST (depth: {depth}) from: {vastAdTagUri}");

            using (var nestedRequest = UnityWebRequest.Get(vastAdTagUri))
            {
                yield return nestedRequest.SendWebRequest();

                if (nestedRequest.result == UnityWebRequest.Result.Success)
                {
                    var nestedVastXml = nestedRequest.downloadHandler.text;
                    Logger.Info($"[VideoAdView] Fetched nested VAST (depth: {depth}, {nestedVastXml.Length} chars)");

                    // Check if nested VAST is also a wrapper
                    if (VASTParser.IsWrapperVAST(nestedVastXml))
                    {
                        Logger.Info($"[VideoAdView] Nested VAST is also a wrapper (depth: {depth}), fetching next level...");
                        var nextVastAdTagUri = VASTParser.ExtractVASTAdTagURI(nestedVastXml);

                        if (!string.IsNullOrEmpty(nextVastAdTagUri))
                        {
                            // Recursively fetch the next level
                            yield return StartCoroutine(FetchNestedVASTRecursive(nextVastAdTagUri, nestedVastXml, depth + 1));

                            // After recursive call, _vastData should be set
                            if (_vastData != null)
                            {
                                // Merge impression URLs from this wrapper level
                                var wrapperImpressionUrls = ExtractWrapperImpressionUrls(nestedVastXml);
                                if (wrapperImpressionUrls.Count > 0)
                                {
                                    _vastData.impressionUrls.AddRange(wrapperImpressionUrls);
                                }
                            }
                            yield break;
                        }
                        else
                        {
                            Logger.InfoError("[VideoAdView] Nested wrapper VAST has no VASTAdTagURI");
                            yield break;
                        }
                    }
                    else
                    {
                        // This is an InLine VAST - parse it
                        _vastData = VASTParser.Parse(nestedVastXml);

                        if (_vastData != null)
                        {
                            // Merge wrapper impression URLs from all wrapper levels
                            var wrapperImpressionUrls = ExtractWrapperImpressionUrls(wrapperVastXml);
                            if (wrapperImpressionUrls.Count > 0)
                            {
                                _vastData.impressionUrls.AddRange(wrapperImpressionUrls);
                            }
                        }
                        else
                        {
                            Logger.InfoError("[VideoAdView] Failed to parse nested InLine VAST");
                        }
                    }
                }
                else
                {
                    Logger.InfoError($"[VideoAdView] Failed to fetch nested VAST: {nestedRequest.error}");
                    yield break;
                }
            }
        }

        /// <summary>
        /// Extract impression URLs from wrapper VAST XML
        /// </summary>
        private List<string> ExtractWrapperImpressionUrls(string wrapperVastXml)
        {
            var impressionUrls = new List<string>();

            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(wrapperVastXml);

                var impressionNodes = xmlDoc.SelectNodes("//Impression");
                if (impressionNodes != null)
                {
                    foreach (System.Xml.XmlNode impNode in impressionNodes)
                    {
                        var impUrl = impNode.InnerText?.Trim();
                        if (impUrl.StartsWith("<![CDATA[") && impUrl.EndsWith("]]>"))
                        {
                            impUrl = impUrl.Substring(9, impUrl.Length - 12).Trim();
                        }
                        if (!string.IsNullOrEmpty(impUrl))
                        {
                            impressionUrls.Add(impUrl);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Logger.Info($"[VideoAdView] Failed to extract wrapper impression URLs: {e.Message}");
            }

            return impressionUrls;
        }

        private IEnumerator TrackVASTQuartiles()
        {
            if (_vastData == null || _videoPlayer.frameCount == 0)
                yield break;

            while (_videoPlayer.isPlaying)
            {
                if (_videoPlayer.frameCount > 0)
                {
                    var progress = (float)_videoPlayer.frame / _videoPlayer.frameCount;

                    // First quartile (25%)
                    if (progress >= 0.25f && !_hasFiredFirstQuartile)
                    {
                        VASTParser.FireTrackingUrls(_vastData.firstQuartileUrls);
                        _hasFiredFirstQuartile = true;
                    }

                    // Midpoint (50%)
                    if (progress >= 0.5f && !_hasFiredMidpoint)
                    {
                        VASTParser.FireTrackingUrls(_vastData.midpointUrls);
                        _hasFiredMidpoint = true;
                    }

                    // Third quartile (75%)
                    if (progress >= 0.75f && !_hasFiredThirdQuartile)
                    {
                        VASTParser.FireTrackingUrls(_vastData.thirdQuartileUrls);
                        _hasFiredThirdQuartile = true;
                    }
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
                // Fire VAST skip tracking URLs
                if (_vastData != null)
                {
                    VASTParser.FireTrackingUrls(_vastData.skipUrls);
                }

                _callback?.OnVideoAdSkipped(_placementId);
                _callback?.OnAdClosed(_placementId);
                Destroy();
            }
        }

        private void OnVideoClicked()
        {
            if (_vastData != null && !string.IsNullOrEmpty(_vastData.clickThroughUrl))
            {
                // Fire click tracking URLs
                VASTParser.FireTrackingUrls(_vastData.clickTrackingUrls);

                // Open click-through URL
                Logger.Info($"[VideoAdView] Opening click-through URL: {_vastData.clickThroughUrl}");
                Application.OpenURL(_vastData.clickThroughUrl);
                _callback?.OnAdClicked(_placementId);
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