using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System;
using System.IO;

namespace BidscubeSDK
{
    /// <summary>
    /// Video ad view component with VAST support
    /// </summary>
    public class VideoAdView : MonoBehaviour, IVideoPlayerEventListener
    {
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _clickButton; // Full screen clickable area
        [SerializeField] private Text _skipText;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private RawImage _videoTexture;
        private Image _videoBackdrop;

        private string _placementId;
        private IAdCallback _callback;
        private VideoAdFormat _videoAdFormat = VideoAdFormat.Interstitial;
        private bool _isLoaded = false;
        private bool _isSkippable = false;

        private bool _hasLoading;
        private bool _hasLoaded;
        private bool _hasDisplayed;
        private bool _hasStarted;
        private bool _hasCompleted;
        private bool _hasSkipped;
        private bool _hasClosed;
        private bool _hasRewarded;
        private bool _isDestroying;
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

        private bool _videoHadError = false;
        private string _videoError = null;

        private bool _cacheDownloadStarted = false;
        private bool _cacheReady = false;
        private string _cacheLocalUrl = null;

        private void Awake()
        {
            // Defer creating UI/video player until we know the SDK will render the ad.
            // SetupUI() will be called lazily in LoadVideoAdCoroutine after render-override checks.
        }

        private void SetupUI()
        {
            // IMA event bridge is not wired to VideoAdView lifecycle yet; use custom VAST/VideoPlayer path.
            _useIMA = false;

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

            if (_videoBackdrop == null)
            {
                var backdropGo = new GameObject("VideoBackdrop");
                backdropGo.transform.SetParent(transform, false);
                var bdRt = backdropGo.AddComponent<RectTransform>();
                bdRt.anchorMin = Vector2.zero;
                bdRt.anchorMax = Vector2.one;
                bdRt.offsetMin = Vector2.zero;
                bdRt.offsetMax = Vector2.zero;
                _videoBackdrop = backdropGo.AddComponent<Image>();
                _videoBackdrop.color = Color.black;
                _videoBackdrop.raycastTarget = false;
            }

            // Video area: RawImage + Button (tap-through for click tracking). Avoid a full-screen transparent Image on top of the video — on Android it often composites as opaque black over RenderTexture playback.
            if (_videoTexture == null)
            {
                var textureObj = new GameObject("VideoTexture");
                textureObj.transform.SetParent(transform, false);
                _videoTexture = textureObj.AddComponent<RawImage>();
                _videoTexture.color = Color.white;
                _videoTexture.raycastTarget = true;
                var vRect = _videoTexture.GetComponent<RectTransform>();
                vRect.anchorMin = Vector2.zero;
                vRect.anchorMax = Vector2.one;
                vRect.offsetMin = Vector2.zero;
                vRect.offsetMax = Vector2.zero;

                var tapBtn = textureObj.AddComponent<Button>();
                tapBtn.targetGraphic = _videoTexture;
                tapBtn.transition = Selectable.Transition.None;
                tapBtn.onClick.AddListener(OnVideoClicked);
                _clickButton = tapBtn;
            }

            // Create video player
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();
                _videoPlayer.playOnAwake = false;
                _videoPlayer.isLooping = false;
                _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                _videoPlayer.waitForFirstFrame = true;
                _videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
                _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

                int rw = Mathf.Max(16, Screen.width);
                int rh = Mathf.Max(16, Screen.height);
                var renderTexture = new RenderTexture(rw, rh, 0, RenderTextureFormat.ARGB32);
                renderTexture.Create();
                _videoPlayer.targetTexture = renderTexture;
                _videoTexture.texture = renderTexture;

                _videoPlayer.prepareCompleted += OnVideoPrepared;
                _videoPlayer.started += OnVideoStarted;
                _videoPlayer.loopPointReached += OnVideoCompleted;
                _videoPlayer.errorReceived += OnVideoError;
            }

            // Create skip button (top-left when skippable)
            if (_skipButton == null)
            {
                var skipObj = new GameObject("SkipButton", typeof(RectTransform), typeof(Image));
                skipObj.transform.SetParent(transform, false);
                var skipRt = skipObj.GetComponent<RectTransform>();
                skipRt.anchorMin = new Vector2(0f, 1f);
                skipRt.anchorMax = new Vector2(0f, 1f);
                skipRt.pivot = new Vector2(0f, 1f);
                skipRt.sizeDelta = new Vector2(168f, 52f);
                skipRt.anchoredPosition = new Vector2(12f, -12f);
                var skipImg = skipObj.GetComponent<Image>();
                skipImg.color = new Color(0f, 0f, 0f, 0.55f);
                _skipButton = skipObj.AddComponent<Button>();
                _skipButton.targetGraphic = skipImg;
                _skipButton.onClick.AddListener(OnSkipClicked);
                _skipButton.gameObject.SetActive(false);
            }

            // Close — top-right, always visible
            if (_closeButton == null)
            {
                var closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image));
                closeObj.transform.SetParent(transform, false);
                var closeRt = closeObj.GetComponent<RectTransform>();
                closeRt.anchorMin = new Vector2(1f, 1f);
                closeRt.anchorMax = new Vector2(1f, 1f);
                closeRt.pivot = new Vector2(1f, 1f);
                closeRt.sizeDelta = new Vector2(72f, 72f);
                closeRt.anchoredPosition = new Vector2(-10f, -10f);
                var closeImg = closeObj.GetComponent<Image>();
                closeImg.color = new Color(0.12f, 0.12f, 0.14f, 0.94f);
                _closeButton = closeObj.AddComponent<Button>();
                _closeButton.targetGraphic = closeImg;
                _closeButton.onClick.AddListener(OnCloseClicked);

                var closeLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                closeLabelGo.transform.SetParent(closeObj.transform, false);
                var closeLblRt = closeLabelGo.GetComponent<RectTransform>();
                closeLblRt.anchorMin = Vector2.zero;
                closeLblRt.anchorMax = Vector2.one;
                closeLblRt.offsetMin = Vector2.zero;
                closeLblRt.offsetMax = Vector2.zero;
                var closeTxt = closeLabelGo.GetComponent<Text>();
                closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                closeTxt.fontSize = 28;
                closeTxt.color = Color.white;
                closeTxt.alignment = TextAnchor.MiddleCenter;
                closeTxt.text = "\u2715";
                closeTxt.raycastTarget = false;
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
                var sliderObj = new GameObject("ProgressSlider", typeof(RectTransform));
                sliderObj.transform.SetParent(transform, false);
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

            if (_videoBackdrop != null)
                _videoBackdrop.transform.SetAsFirstSibling();
            if (_videoTexture != null)
                _videoTexture.transform.SetSiblingIndex(1);
            if (_skipButton != null)
                _skipButton.transform.SetAsLastSibling();
            if (_closeButton != null)
                _closeButton.transform.SetAsLastSibling();
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
        public void SetPlacementInfo(string placementId, IAdCallback callback)
        {
            SetPlacementInfo(placementId, callback, VideoAdFormat.Interstitial);
        }

        /// <summary>
        /// Set placement info and video format (interstitial vs rewarded).
        /// </summary>
        public void SetPlacementInfo(string placementId, IAdCallback callback, VideoAdFormat videoAdFormat)
        {
            _placementId = placementId;
            _callback = callback;
            _videoAdFormat = videoAdFormat;

            if (_useIMA && _imaPlayer != null)
            {
                _imaPlayer.Initialize(placementId, callback);
            }
        }

        private void ResetCallbackState()
        {
            _hasLoading = false;
            _hasLoaded = false;
            _hasDisplayed = false;
            _hasStarted = false;
            _hasCompleted = false;
            _hasSkipped = false;
            _hasClosed = false;
            _hasRewarded = false;
            _isDestroying = false;
        }

        private void NotifyLoading()
        {
            if (_hasLoading) return;
            _hasLoading = true;
            _callback?.OnAdLoading(_placementId);
        }

        private void NotifyLoaded()
        {
            if (_hasLoaded) return;
            _hasLoaded = true;
            _isLoaded = true;
            _callback?.OnAdLoaded(_placementId);
        }

        private void NotifyDisplayed()
        {
            if (_hasDisplayed) return;
            _hasDisplayed = true;
            _callback?.OnAdDisplayed(_placementId);
        }

        private void NotifyStarted()
        {
            if (_hasStarted) return;
            _hasStarted = true;
            _callback?.OnVideoAdStarted(_placementId);
        }

        private void NotifyCompleted()
        {
            if (_hasCompleted) return;
            _hasCompleted = true;
            _callback?.OnVideoAdCompleted(_placementId);
        }

        private void NotifySkipped()
        {
            if (_hasSkipped) return;
            _hasSkipped = true;
            _callback?.OnVideoAdSkipped(_placementId);
        }

        private void NotifyClosed()
        {
            if (_hasClosed) return;
            _hasClosed = true;
            _callback?.OnAdClosed(_placementId);
        }

        private void NotifyFailed(int errorCode, string errorMessage)
        {
            if (_isDestroying) return;
            _callback?.OnAdFailed(_placementId, errorCode, errorMessage);
        }

        private void NotifyRewardedIfNeeded()
        {
            if (_videoAdFormat != VideoAdFormat.Rewarded || _hasRewarded)
                return;
            _hasRewarded = true;
            if (_callback is IRewardedAdCallback rewardedCallback)
                rewardedCallback.OnUserRewarded(_placementId);
        }

        void IVideoPlayerEventListener.OnVideoLoaded() => NotifyLoaded();
        void IVideoPlayerEventListener.OnVideoStarted()
        {
            NotifyDisplayed();
            NotifyStarted();
        }
        void IVideoPlayerEventListener.OnVideoClicked() => OnVideoClicked();
        void IVideoPlayerEventListener.OnVideoCompleted() => HandleVideoCompleted();
        void IVideoPlayerEventListener.OnVideoSkipped() => HandleUserDismissBeforeComplete();
        void IVideoPlayerEventListener.OnVideoFailed(int errorCode, string message) => NotifyFailed(errorCode, message);

        private void HandleVideoCompleted()
        {
            if (_vastData != null && !_hasFiredComplete)
            {
                VASTParser.FireTrackingUrls(_vastData.completeUrls);
                _hasFiredComplete = true;
            }
            NotifyCompleted();
            if (_videoAdFormat == VideoAdFormat.Rewarded)
                NotifyRewardedIfNeeded();
        }

        private void HandleUserDismissBeforeComplete()
        {
            if (!_hasCompleted && !_hasSkipped)
                NotifySkipped();
        }

        /// <summary>
        /// Load video ad from URL (supports VAST XML or direct video URL)
        /// Uses IMA SDK if available, otherwise uses custom VAST parser
        /// </summary>
        /// <param name="url">VAST XML URL or direct video URL</param>
        public void LoadVideoAdFromURL(string url)
        {
            ResetCallbackState();
            NotifyLoading();

            if (_useIMA && _imaPlayer != null)
            {
                Logger.Info("[VideoAdView] Loading video ad with IMA SDK");
                _imaPlayer.RequestAd(url);
            }
            else
            {
                StartCoroutine(LoadVideoAdCoroutine(url));
            }
        }

        private IEnumerator LoadVideoAdCoroutine(string url)
        {
            // VideoPlayer / canvas / RawImage must exist before any branch assigns _videoPlayer.url or reads _videoPlayer.url.
            SetupUI();

            // First, fetch the content from URL
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                BidscubeSDK.ApplyConfiguredTimeoutTo(request);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // #region agent log
                    AgentNdjsonDebugLog.Write(
                        "VideoAdView.LoadVideoAdCoroutine",
                        "http_fail",
                        "H2",
                        "{\"placementId\":\"" + _placementId + "\",\"error\":\"" + AgentNdjsonDebugLog.EscapeForData(request.error ?? "") + "\"}");
                    // #endregion
                    Logger.InfoError($"[VideoAdView] Failed to load ad from URL: {request.error}");
                    NotifyFailed(Constants.ErrorCodes.NetworkError, request.error);
                    yield break;
                }

                var responseText = request.downloadHandler.text;
                Logger.Info($"[VideoAdView] Received response ({responseText.Length} chars)");
                Logger.DebugLog($"[VideoAdView] Response preview: {(responseText.Length <= 400 ? responseText : responseText.Substring(0, 400) + "…")}");

                // Check if response is VAST XML
                if (responseText.TrimStart().StartsWith("<VAST") || responseText.Contains("<VAST"))
                {
                    Logger.Info("[VideoAdView] Detected VAST XML, parsing...");
                    _vastData = VASTParser.Parse(responseText);

                    if (_vastData == null || string.IsNullOrEmpty(_vastData.videoUrl))
                    {
                        // #region agent log
                        AgentNdjsonDebugLog.Write("VideoAdView.LoadVideoAdCoroutine", "vast_parse_fail", "H3", "{\"placementId\":\"" + _placementId + "\"}");
                        // #endregion
                        Logger.InfoError("[VideoAdView] Failed to parse VAST or no video URL found");
                        NotifyFailed(Constants.ErrorCodes.InvalidResponse, "Failed to parse VAST XML");
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

                    // JSON: flat adm, nested adm, or OpenRTB seatbid[].bid[].adm (same as Android SDK envelope)
                    if (responseText.TrimStart().StartsWith("{"))
                    {
                        string admValue = null;
                        AdResponse jsonEnvelope = null;
                        if (AdMarkupExtractor.TryExtractMarkup(responseText, out var extracted, out _, out _))
                        {
                            admValue = extracted;
                            Logger.Info("[VideoAdView] Extracted adm from JSON (flat or OpenRTB seatbid)");
                        }
                        else
                        {
                            try
                            {
                                jsonEnvelope = JsonUtility.FromJson<AdResponse>(responseText);
                                if (jsonEnvelope != null)
                                    admValue = jsonEnvelope.GetAdmString();
                            }
                            catch (System.Exception e)
                            {
                                Logger.InfoError($"[VideoAdView] JSON parsing failed: {e.Message}");
                                NotifyFailed(Constants.ErrorCodes.InvalidResponse, $"JSON parsing failed: {e.Message}");
                                yield break;
                            }

                            if (!string.IsNullOrEmpty(admValue))
                                Logger.Info("[VideoAdView] Extracted adm from JSON envelope (JsonUtility)");
                        }

                        // If callback implements IAdRenderOverride, let it inspect the raw adm JSON first
                        if (!string.IsNullOrEmpty(admValue) && _callback is IAdRenderOverride rawOverride)
                        {
                            int rawPos = jsonEnvelope != null
                                ? jsonEnvelope.GetPosition()
                                : (int)BidscubeSDK.GetResponseAdPosition();
                            Logger.Info("[VideoAdView] IAdRenderOverride detected, invoking override with raw adm JSON...");
                            bool handledRaw = false;
                            try
                            {
                                handledRaw = rawOverride.OnAdRenderOverride(_placementId, admValue, AdType.Video, rawPos);
                            }
                            catch (System.Exception e)
                            {
                                Logger.InfoError($"[VideoAdView] OnAdRenderOverride threw: {e.Message}");
                            }

                            if (handledRaw)
                            {
                                Logger.Info("[VideoAdView] Ad render overridden by app (raw adm); skipping SDK processing.");
                                yield break;
                            }
                            else
                            {
                                Logger.Info("[VideoAdView] OnAdRenderOverride returned false for raw adm; continuing SDK processing.");
                            }
                        }

                        if (!string.IsNullOrEmpty(admValue))
                        {

                            // Log the raw adm field as received
                            Logger.Info($"[VideoAdView] Raw adm field: {admValue.Length} chars");
                            Logger.DebugLog($"[VideoAdView] Raw adm preview:\n{(admValue.Length <= 500 ? admValue : admValue.Substring(0, 500) + "…")}");

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

                            // Unescape Unicode sequences (e.g., \u003c -> <)
                            // Unity's JsonUtility should handle this, but handle it explicitly to be safe
                            admContent = System.Text.RegularExpressions.Regex.Replace(
                                admContent,
                                @"\\u([0-9A-Fa-f]{4})",
                                m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString()
                            );

                            Logger.Info($"[VideoAdView] Processed adm: {admContent.Length} chars");
                            Logger.DebugLog($"[VideoAdView] Processed adm preview:\n{(admContent.Length <= 500 ? admContent : admContent.Substring(0, 500) + "…")}");

                            // AD RENDER OVERRIDE HOOK: if the app implements IAdRenderOverride, give it the admContent
                            // Note: render-override was already invoked with the raw adm JSON earlier; do not call it again here.

                            // Check if adm is VAST XML
                            if (admContent.TrimStart().StartsWith("<VAST") || admContent.Contains("<VAST"))
                            {
                                Logger.Info("[VideoAdView] Detected VAST XML in adm field, parsing...");

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
                                            NotifyFailed(Constants.ErrorCodes.NetworkError, "Failed to fetch or parse nested VAST");
                                            yield break;
                                        }
                                    }
                                    else
                                    {
                                        Logger.InfoError("[VideoAdView] Wrapper VAST found but no VASTAdTagURI");
                                        NotifyFailed(Constants.ErrorCodes.InvalidResponse, "Wrapper VAST has no VASTAdTagURI");
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
                                    NotifyFailed(Constants.ErrorCodes.InvalidResponse, "VAST parsing failed");
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
                                    NotifyFailed(Constants.ErrorCodes.InvalidResponse, "VAST parsed but no video URL found");
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
                if (_videoPlayer == null || string.IsNullOrEmpty(_videoPlayer.url))
                {
                    Logger.InfoError("[VideoAdView] No video URL available to play");
                    NotifyFailed(Constants.ErrorCodes.InvalidResponse, "No video URL found in response");
                    yield break;
                }

                // Validate that the media URL is actually reachable (some demo VASTs include URLs that return 403).
                if (_videoPlayer.url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    using (var head = UnityWebRequest.Head(_videoPlayer.url))
                    {
                        head.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                        BidscubeSDK.ApplyConfiguredTimeoutTo(head);
                        yield return head.SendWebRequest();

                        // UnityWebRequest.Head can report Success even for some error codes; check responseCode.
                        if (head.result != UnityWebRequest.Result.Success || head.responseCode >= 400)
                        {
                            var msg = $"Media URL not reachable (HTTP {head.responseCode}): {head.error}";
                            Logger.InfoError($"[VideoAdView] {msg}");
                            NotifyFailed(Constants.ErrorCodes.NetworkError, msg);
                            yield break;
                        }
                    }
                }

                // Android: VideoPlayer often fails on HTTPS streams (NuCachedSource2 error -1) without errorReceived.
                // Start local cache download early so we can fall back to a file:// URL.
                ResetCacheState();
                if (Application.platform == RuntimePlatform.Android)
                {
                    TryStartLocalCacheFallback(_videoPlayer.url);
                }

                Logger.Info($"[VideoAdView] Preparing video player with URL: {_videoPlayer.url}");

                // Prepare video
                _videoHadError = false;
                _videoError = null;
                _videoPlayer.Prepare();

                float timeout = Mathf.Max(1f, BidscubeSDK.GetConfiguredAdTimeoutMs() / 1000f);
                float elapsed = 0f;

                while (!_videoPlayer.isPrepared && !_videoHadError && elapsed < timeout)
                {
                    // If the stream is not becoming ready but the cache is, switch to the local file and prepare again.
                    if (_cacheReady && !string.IsNullOrEmpty(_cacheLocalUrl) &&
                        _videoPlayer != null && !string.Equals(_videoPlayer.url, _cacheLocalUrl, StringComparison.Ordinal))
                    {
                        Logger.Info($"[VideoAdView] Switching to cached file during prepare loop: {_cacheLocalUrl}");
                        _videoHadError = false;
                        _videoError = null;
                        _videoPlayer.url = _cacheLocalUrl;
                        _videoPlayer.Prepare();
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (_videoHadError || !_videoPlayer.isPrepared)
                {
                    // Android VideoPlayer frequently fails for some HTTPS streams (e.g. NuCachedSource2 error -1).
                    // Fallback: download MP4 and replay from a local cached file.
                    if (TryStartLocalCacheFallback(_videoPlayer.url))
                    {
                        elapsed = 0f;
                        while (!_videoPlayer.isPrepared && !_videoHadError && elapsed < timeout)
                        {
                            elapsed += Time.deltaTime;
                            yield return null;
                        }
                    }

                    if (!_videoPlayer.isPrepared)
                    {
                        var msg = _videoHadError ? (_videoError ?? "Video error") : "Video preparation timeout";
                        Logger.InfoError($"[VideoAdView] Video failed: {msg}");
                        NotifyFailed(
                            _videoHadError ? Constants.ErrorCodes.NetworkError : Constants.ErrorCodes.Timeout,
                            msg);
                        yield break;
                    }
                }

                Logger.Info("[VideoAdView] Video prepared successfully");
                NotifyLoaded();
                Logger.Info("[VideoAdView] Starting video playback...");
                _videoPlayer.Play();
            }
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            NotifyLoaded();
            if (_videoPlayer != null && !_videoPlayer.isPlaying)
            {
                Logger.Info("[VideoAdView] Video prepared, starting playback...");
                _videoPlayer.Play();
            }
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            _videoHadError = true;
            _videoError = message;
            Logger.InfoError($"[VideoAdView] VideoPlayer errorReceived: {message}");
        }

        private void ResetCacheState()
        {
            _cacheDownloadStarted = false;
            _cacheReady = false;
            _cacheLocalUrl = null;
        }

        private bool TryStartLocalCacheFallback(string remoteUrl)
        {
            if (string.IsNullOrEmpty(remoteUrl))
                return false;
            if (!remoteUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return false;
            if (_cacheDownloadStarted)
                return true;
            _cacheDownloadStarted = true;
            StartCoroutine(DownloadToCacheThenReplay(remoteUrl));
            return true;
        }

        private IEnumerator DownloadToCacheThenReplay(string remoteUrl)
        {
            string cacheDir = Path.Combine(Application.persistentDataPath, "bidscube-cache");
            string cachePath = Path.Combine(cacheDir, "video.mp4");

            try
            {
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);
            }
            catch (Exception e)
            {
                Logger.InfoError($"[VideoAdView] Failed to create cache dir: {e.Message}");
                yield break;
            }

            using (var req = UnityWebRequest.Get(remoteUrl))
            {
                req.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                BidscubeSDK.ApplyConfiguredTimeoutTo(req);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Logger.InfoError($"[VideoAdView] Cache download failed: {req.error}");
                    yield break;
                }

                try
                {
                    File.WriteAllBytes(cachePath, req.downloadHandler.data);
                }
                catch (Exception e)
                {
                    Logger.InfoError($"[VideoAdView] Failed to write cache file: {e.Message}");
                    yield break;
                }
            }

            if (_videoPlayer == null)
                yield break;

            // Switch to local file and re-prepare (only if still trying to play the remote URL)
            _cacheLocalUrl = "file://" + cachePath;
            _cacheReady = true;
            Logger.Info($"[VideoAdView] Video cache ready: {_cacheLocalUrl}");

            if (string.Equals(_videoPlayer.url, remoteUrl, StringComparison.Ordinal))
            {
                Logger.Info($"[VideoAdView] Switching VideoPlayer to cached file: {_cacheLocalUrl}");
                _videoHadError = false;
                _videoError = null;
                _videoPlayer.url = _cacheLocalUrl;
                _videoPlayer.Prepare();
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

            NotifyDisplayed();
            NotifyStarted();
            StartCoroutine(UpdateProgress());
            StartCoroutine(EnableSkipButton());
            StartCoroutine(TrackVASTQuartiles());
        }

        private void OnVideoCompleted(VideoPlayer source)
        {
            HandleVideoCompleted();
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
                nestedRequest.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
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
            if (!_isSkippable || _isDestroying)
                return;

            if (_vastData != null)
                VASTParser.FireTrackingUrls(_vastData.skipUrls);

            NotifySkipped();
            NotifyClosed();
            DismissVideoAdHierarchy();
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
            if (_isDestroying)
                return;

            if (!_hasCompleted && !_hasSkipped)
                NotifySkipped();

            NotifyClosed();
            DismissVideoAdHierarchy();
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
        /// Destroy video ad view (removes the hosting <see cref="AdViewController"/> so the fullscreen canvas is cleared).
        /// </summary>
        public void Destroy()
        {
            DismissVideoAdHierarchy();
        }

        void DismissVideoAdHierarchy()
        {
            if (_isDestroying)
                return;
            _isDestroying = true;
            ReleaseVideoPlayerResources();

            var controller = GetComponentInParent<AdViewController>();
            if (controller != null)
                Destroy(controller.gameObject);
            else if (gameObject != null)
                Destroy(gameObject);
        }

        private void ReleaseVideoPlayerResources()
        {
            if (_videoPlayer == null)
                return;

            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            _videoPlayer.started -= OnVideoStarted;
            _videoPlayer.loopPointReached -= OnVideoCompleted;
            _videoPlayer.errorReceived -= OnVideoError;
            _videoPlayer.Stop();
            var rt = _videoPlayer.targetTexture;
            _videoPlayer.targetTexture = null;
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }
        }

        private void OnDestroy()
        {
            if (_isDestroying)
                return;
            _isDestroying = true;
            ReleaseVideoPlayerResources();

            if (_hasStarted && !_hasCompleted && !_hasSkipped)
                NotifySkipped();
            if (!_hasClosed)
                NotifyClosed();
        }
    }
}

