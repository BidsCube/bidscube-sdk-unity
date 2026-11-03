using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System;
using System.IO;
using BidscubeSDK.OpenRTB;

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
        [SerializeField] private GameObject _endCardRoot;
        [SerializeField] private RawImage _endCardPreview;
        [SerializeField] private Button _endCardPreviewButton;
        [SerializeField] private Button _endCardCtaButton;
        [SerializeField] private Text _endCardCtaText;
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
        private bool _endCardShown;
        private bool _companionViewTracked;
        private bool _awaitingManualClose;
        private WebViewObject _companionWebView;
        private float _skipTime = 15.0f; // Default skip delay when VAST has no skipoffset

        /// <summary>
        /// Playback facts the MAX adapter needs. Public so IL2CPP keeps them without reflection.
        /// </summary>
        public struct VideoAdSessionState
        {
            public bool Rewarded;
            public bool Presented;
            public bool Completed;
            public bool EndCardShown;
            public bool AwaitingManualClose;
            public bool Closed;
        }

        public VideoAdSessionState SessionState
        {
            get
            {
                return new VideoAdSessionState
                {
                    Rewarded = _videoAdFormat == VideoAdFormat.Rewarded,
                    Presented = _hasDisplayed || _hasStarted,
                    Completed = _hasCompleted,
                    EndCardShown = _endCardShown,
                    AwaitingManualClose = _awaitingManualClose,
                    Closed = _hasClosed
                };
            }
        }

        // VAST data
        private VASTParser.VASTData _vastData;
        private bool _hasFiredImpression = false;
        private string _trackingSessionId = "";
        private bool _hasFiredStart = false;
        private bool _hasFiredFirstQuartile = false;
        private bool _hasFiredMidpoint = false;
        private bool _hasFiredThirdQuartile = false;
        private bool _hasFiredComplete = false;

        private VideoPlaybackPlan _playbackPlan;
        private int _currentPlaybackSlotIndex = -1;
        private bool _isPlayingPod;
        private Text _podCounterText;
        private Coroutine _skipButtonCoroutine;

        // IMA player (when available)
        private IMAVideoPlayer _imaPlayer;
        private bool _useIMA = false;

        private bool _videoHadError = false;
        private string _videoError = null;

        private bool _cacheDownloadStarted = false;
        private bool _cacheReady = false;
        private string _cacheLocalUrl = null;
        private int _endCardRequestId;
        private Texture2D _endCardOwnedTexture;
        private readonly List<AudioSource> _pausedHostAudio = new List<AudioSource>();
        private bool _hostAudioPaused;
        private bool _explicitAdMuteCaptured;
        private bool _explicitAdMute;
        private bool _pausedForBackground;

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
                BindAdAudio();

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
                _skipButton.interactable = false;
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

            SetupEndCard();

            if (_videoBackdrop != null)
                _videoBackdrop.transform.SetAsFirstSibling();
            if (_videoTexture != null)
                _videoTexture.transform.SetSiblingIndex(1);
            if (_skipButton != null)
                _skipButton.transform.SetAsLastSibling();
            if (_closeButton != null)
                _closeButton.transform.SetAsLastSibling();
        }

        private void SetupEndCard()
        {
            if (_endCardRoot != null)
                return;

            var endCardObj = new GameObject("EndCardRoot", typeof(RectTransform), typeof(Image));
            endCardObj.transform.SetParent(transform, false);
            var endCardRect = endCardObj.GetComponent<RectTransform>();
            endCardRect.anchorMin = Vector2.zero;
            endCardRect.anchorMax = Vector2.one;
            endCardRect.offsetMin = Vector2.zero;
            endCardRect.offsetMax = Vector2.zero;
            var endCardBg = endCardObj.GetComponent<Image>();
            endCardBg.color = new Color(0f, 0f, 0f, 0.65f);

            var previewButtonObj = new GameObject("EndCardPreviewButton", typeof(RectTransform), typeof(Image), typeof(Button));
            previewButtonObj.transform.SetParent(endCardObj.transform, false);
            var previewRect = previewButtonObj.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 0.5f);
            previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.sizeDelta = new Vector2(720f, 405f);
            previewRect.anchoredPosition = new Vector2(0f, 30f);
            var previewImage = previewButtonObj.GetComponent<Image>();
            previewImage.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            _endCardPreviewButton = previewButtonObj.GetComponent<Button>();
            _endCardPreviewButton.targetGraphic = previewImage;
            _endCardPreviewButton.onClick.AddListener(OnEndCardClicked);

            var previewContentObj = new GameObject("EndCardPreview", typeof(RectTransform), typeof(RawImage));
            previewContentObj.transform.SetParent(previewButtonObj.transform, false);
            var previewContentRect = previewContentObj.GetComponent<RectTransform>();
            previewContentRect.anchorMin = Vector2.zero;
            previewContentRect.anchorMax = Vector2.one;
            previewContentRect.offsetMin = Vector2.zero;
            previewContentRect.offsetMax = Vector2.zero;
            _endCardPreview = previewContentObj.GetComponent<RawImage>();
            _endCardPreview.color = new Color(1f, 1f, 1f, 0f);
            _endCardPreview.raycastTarget = false;

            var ctaObj = new GameObject("EndCardCTA", typeof(RectTransform), typeof(Image), typeof(Button));
            ctaObj.transform.SetParent(endCardObj.transform, false);
            var ctaRect = ctaObj.GetComponent<RectTransform>();
            ctaRect.anchorMin = new Vector2(0.5f, 0.5f);
            ctaRect.anchorMax = new Vector2(0.5f, 0.5f);
            ctaRect.pivot = new Vector2(0.5f, 0.5f);
            ctaRect.sizeDelta = new Vector2(240f, 60f);
            ctaRect.anchoredPosition = new Vector2(0f, -220f);
            var ctaImage = ctaObj.GetComponent<Image>();
            ctaImage.color = new Color(0.0f, 0.48f, 1.0f, 0.96f);
            _endCardCtaButton = ctaObj.GetComponent<Button>();
            _endCardCtaButton.targetGraphic = ctaImage;
            _endCardCtaButton.onClick.AddListener(OnEndCardClicked);

            var ctaTextObj = new GameObject("EndCardCTAText", typeof(RectTransform), typeof(Text));
            ctaTextObj.transform.SetParent(ctaObj.transform, false);
            var ctaTextRect = ctaTextObj.GetComponent<RectTransform>();
            ctaTextRect.anchorMin = Vector2.zero;
            ctaTextRect.anchorMax = Vector2.one;
            ctaTextRect.offsetMin = Vector2.zero;
            ctaTextRect.offsetMax = Vector2.zero;
            _endCardCtaText = ctaTextObj.GetComponent<Text>();
            _endCardCtaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _endCardCtaText.fontSize = 22;
            _endCardCtaText.alignment = TextAnchor.MiddleCenter;
            _endCardCtaText.color = Color.white;
            _endCardCtaText.text = "Learn More";
            _endCardCtaText.raycastTarget = false;

            _endCardRoot = endCardObj;
            _endCardRoot.SetActive(false);
            _endCardRoot.transform.SetAsLastSibling();
        }

        private void SetupFullScreenCanvas()
        {
            var canvas = FindOrCreateSdkCanvas();
            if (transform.parent != canvas.transform)
                transform.SetParent(canvas.transform, false);

            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = gameObject.AddComponent<RectTransform>();

            Logger.Info("[VideoAdView] Setting up full screen display");
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static Canvas FindOrCreateSdkCanvas()
        {
            var canvases = FindObjectsOfType<Canvas>();
            for (var i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].name == "BidscubeSdkCanvas")
                    return canvases[i];
            }

            var canvasObj = new GameObject("BidscubeSdkCanvas");
            DontDestroyOnLoad(canvasObj);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvas;
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
            _endCardShown = false;
            _companionViewTracked = false;
            _awaitingManualClose = false;
            _isSkippable = false;
            _hasFiredImpression = false;
            _trackingSessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            _hasFiredStart = false;
            _hasFiredFirstQuartile = false;
            _hasFiredMidpoint = false;
            _hasFiredThirdQuartile = false;
            _hasFiredComplete = false;
            _playbackPlan = null;
            _currentPlaybackSlotIndex = -1;
            _isPlayingPod = false;
        }

        private void ResetSlotTrackingState()
        {
            _hasFiredImpression = false;
            _trackingSessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            _hasFiredStart = false;
            _hasFiredFirstQuartile = false;
            _hasFiredMidpoint = false;
            _hasFiredThirdQuartile = false;
            _hasFiredComplete = false;
            _isSkippable = false;
            _skipTime = 15.0f;

            if (_skipButtonCoroutine != null)
            {
                StopCoroutine(_skipButtonCoroutine);
                _skipButtonCoroutine = null;
            }

            if (_skipButton != null)
            {
                _skipButton.interactable = false;
                _skipButton.gameObject.SetActive(true);
            }

            if (_skipText != null)
                _skipText.text = "Skip";

            if (_progressSlider != null)
                _progressSlider.value = 0;
        }

        private void ResetEndCardState()
        {
            _endCardRequestId++;
            _skipTime = 15.0f;
            _endCardShown = false;
            if (_endCardRoot != null)
                _endCardRoot.SetActive(false);
            if (_endCardPreviewButton != null)
                _endCardPreviewButton.interactable = false;
            if (_endCardCtaButton != null)
                _endCardCtaButton.gameObject.SetActive(false);
            if (_endCardPreview != null)
            {
                _endCardPreview.texture = null;
                _endCardPreview.color = new Color(1f, 1f, 1f, 0f);
            }
            ReleaseEndCardTexture();
            if (_endCardCtaText != null)
                _endCardCtaText.text = "Learn More";
            if (_skipButton != null)
            {
                _skipButton.interactable = false;
                _skipButton.gameObject.SetActive(false);
            }
            if (_skipText != null)
                _skipText.text = "Skip";
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

            if (_isPlayingPod && _playbackPlan != null && TryAdvanceToNextPlaybackSlot())
                return;

            NotifyCompleted();
            if (VideoSessionEndPolicy.ShouldGrantReward(_videoAdFormat, completedNaturally: true, wasSkipped: false, alreadyRewarded: _hasRewarded))
                NotifyRewardedIfNeeded();

            PresentPostVideoUi(fromSkip: false);
        }

        private void HandleUserDismissBeforeComplete()
        {
            if (_hasCompleted || _hasSkipped || _isDestroying)
                return;

            _isPlayingPod = false;
            NotifySkipped();
            PresentPostVideoUi(fromSkip: true);
        }

        private bool IsAutoCloseEnabled()
        {
            return BidscubeSDK.GetConfiguration()?.AutoClose == true;
        }

        /// <summary>
        /// After linear video complete/skip: auto-close or show companion / last frame.
        /// Does not call OnAdClosed when autoClose=false.
        /// </summary>
        private void PresentPostVideoUi(bool fromSkip)
        {
            if (_isDestroying || _hasClosed)
                return;

            var action = VideoSessionEndPolicy.Resolve(IsAutoCloseEnabled(), _vastData != null && _vastData.HasCompanion);
            Logger.Info($"[VideoAdView] Post-video action={action}, fromSkip={fromSkip}, autoClose={IsAutoCloseEnabled()}");

            switch (action)
            {
                case VideoSessionEndAction.AutoClose:
                    CloseAdSession(treatIncompleteAsSkip: false);
                    break;
                case VideoSessionEndAction.ShowCompanionEndCard:
                    ShowCompanionEndCard();
                    break;
                default:
                    KeepLastFrameOrPostVideoContent();
                    break;
            }
        }

        /// <summary>
        /// Centralized dismiss path: release player, clear overlays/end card, close fullscreen, fire OnAdClosed once.
        /// </summary>
        private void CloseAdSession(bool treatIncompleteAsSkip)
        {
            if (_hasClosed || _isDestroying)
                return;

            if (treatIncompleteAsSkip && !_hasCompleted && !_hasSkipped)
                NotifySkipped();

            _awaitingManualClose = false;
            _pausedForBackground = false;
            SilenceAdAudio();
            DestroyCompanionWebView();
            CancelEndCardLoad();
            RestoreHostAudio();
            NotifyClosed();
            DismissVideoAdHierarchy();
        }

        private void KeepLastFrameOrPostVideoContent()
        {
            // Leave VideoPlayer (last frame / mini-game / post-video surface) until manual close.
            _awaitingManualClose = true;
            SetupUI();

            if (_videoPlayer != null && _videoPlayer.isPlaying)
                _videoPlayer.Pause();

            if (_skipButton != null)
                _skipButton.gameObject.SetActive(false);

            if (_endCardRoot != null)
                _endCardRoot.SetActive(false);

            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(true);
                _closeButton.transform.SetAsLastSibling();
            }
        }

        private void ShowCompanionEndCard()
        {
            if (_endCardShown || _isDestroying)
                return;

            SetupUI();
            _endCardShown = true;
            _awaitingManualClose = true;

            // Companion replaces the linear player session — release VideoPlayer resources.
            if (_videoPlayer != null)
            {
                if (_videoPlayer.isPlaying)
                    _videoPlayer.Pause();
                ReleaseVideoPlayerResources();
            }

            if (_skipButton != null)
                _skipButton.gameObject.SetActive(false);

            FireCompanionViewTrackingOnce();

            var type = _vastData?.companionResourceType ?? VASTParser.VastCompanionResourceType.None;
            if (VideoEndCardResolver.ShouldRenderInWebView(type))
            {
                ShowCompanionWebView(_vastData.companionResource, type == VASTParser.VastCompanionResourceType.Html);
            }
            else
            {
                ShowStaticCompanionEndCard();
            }

            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(true);
                _closeButton.transform.SetAsLastSibling();
            }
        }

        private void ShowStaticCompanionEndCard()
        {
            if (_endCardRoot == null)
                return;

            _endCardRoot.SetActive(true);
            _endCardRoot.transform.SetAsLastSibling();

            var clickUrl = VideoEndCardResolver.ResolveClickUrl(_vastData);

            if (_endCardCtaButton != null)
                _endCardCtaButton.gameObject.SetActive(!string.IsNullOrEmpty(clickUrl));
            if (_endCardPreviewButton != null)
                _endCardPreviewButton.interactable = !string.IsNullOrEmpty(clickUrl);
            if (_endCardCtaText != null)
                _endCardCtaText.text = VideoEndCardResolver.ResolveCtaLabel(clickUrl);

            var imageUrl = VideoEndCardResolver.ResolveStaticPreviewImageUrl(_vastData);

            if (!string.IsNullOrEmpty(imageUrl))
                StartCoroutine(LoadEndCardPreview(imageUrl));
            else if (_endCardPreview != null)
            {
                _endCardPreview.texture = _videoTexture != null ? _videoTexture.texture : null;
                _endCardPreview.color = _endCardPreview.texture != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }

        private void ShowCompanionWebView(string resource, bool isHtml)
        {
            if (_endCardRoot != null)
                _endCardRoot.SetActive(false);

            DestroyCompanionWebView();

            var host = new GameObject("CompanionWebView");
            host.transform.SetParent(transform, false);
            _companionWebView = host.AddComponent<WebViewObject>();
            _companionWebView.Init(
                cb: null,
                err: msg => Logger.InfoError($"[VideoAdView] Companion WebView error: {msg}"),
                httpErr: msg => Logger.InfoError($"[VideoAdView] Companion WebView HTTP error: {msg}"),
                ld: null,
                started: null,
                hooked: null,
                cookies: null,
                transparent: false);
            _companionWebView.SetMargins(0, 0, 0, 0);
            _companionWebView.SetVisibility(true);

            if (isHtml)
            {
                var html = resource.TrimStart().StartsWith("<", StringComparison.Ordinal)
                    ? resource
                    : $"<html><body style=\"margin:0\">{resource}</body></html>";
                if (!html.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                    && !html.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                {
                    html = $"<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/></head><body style=\"margin:0\">{html}</body></html>";
                }
                _companionWebView.LoadHTML(html, "");
            }
            else
            {
                _companionWebView.LoadURL(resource);
            }
        }

        private void DestroyCompanionWebView()
        {
            if (_companionWebView == null)
                return;

            try
            {
                _companionWebView.SetVisibility(false);
            }
            catch
            {
                // ignore teardown errors
            }

            if (_companionWebView.gameObject != null)
                Destroy(_companionWebView.gameObject);
            _companionWebView = null;
        }

        private void FireCompanionViewTrackingOnce()
        {
            if (_companionViewTracked || _vastData == null)
                return;
            _companionViewTracked = true;
            VASTParser.FireTrackingUrls(_vastData.companionViewTrackingUrls);
        }

        private bool TryAdvanceToNextPlaybackSlot()
        {
            if (_playbackPlan == null || _hasSkipped || _hasClosed || _isDestroying)
                return false;

            int nextIndex = _currentPlaybackSlotIndex + 1;
            if (nextIndex >= _playbackPlan.Slots.Count)
                return false;

            _currentPlaybackSlotIndex = nextIndex;
            ResetSlotTrackingState();
            StartCoroutine(LoadPlaybackSlotCoroutine(_playbackPlan.Slots[nextIndex]));
            return true;
        }

        private IEnumerator LoadPlaybackPlanCoroutine(VideoPlaybackPlan plan)
        {
            _playbackPlan = plan;
            _isPlayingPod = plan != null && plan.Slots.Count > 1;
            _currentPlaybackSlotIndex = 0;
            yield return LoadPlaybackSlotCoroutine(plan.Slots[0]);
        }

        private IEnumerator LoadPlaybackSlotCoroutine(VideoPlaybackSlot slot)
        {
            if (slot == null)
            {
                yield return HandleSlotFailure("Playback slot is null");
                yield break;
            }

            UpdatePodCounterDisplay();
            _vastData = null;

            if (!string.IsNullOrWhiteSpace(slot.VastXml))
            {
                yield return LoadInlineVastXmlCoroutine(slot.VastXml.Trim());
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(slot.VastAdTagUrl))
            {
                yield return FetchAndLoadVastAdTagUrlCoroutine(slot.VastAdTagUrl.Trim());
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(slot.DirectVideoUrl))
            {
                _videoPlayer.url = slot.DirectVideoUrl.Trim();
                yield return PrepareAndPlayVideoCoroutine(notifyLoadedOnPrepare: _currentPlaybackSlotIndex == 0);
                yield break;
            }

            var admContent = slot.Adm?.Trim();
            if (string.IsNullOrWhiteSpace(admContent))
            {
                yield return HandleSlotFailure("Playback slot has no adm content");
                yield break;
            }

            if (VastAdSequenceParser.ContentLikelyContainsVast(admContent))
            {
                yield return LoadInlineVastXmlCoroutine(admContent);
                yield break;
            }

            if (OpenRtbVideoUrlHelper.IsHttpUrl(admContent))
            {
                if (OpenRtbVideoUrlHelper.IsLikelyDirectVideoUrl(admContent))
                {
                    _videoPlayer.url = admContent;
                    yield return PrepareAndPlayVideoCoroutine(notifyLoadedOnPrepare: _currentPlaybackSlotIndex == 0);
                }
                else
                {
                    yield return FetchAndLoadVastAdTagUrlCoroutine(admContent);
                }
                yield break;
            }

            yield return HandleSlotFailure("Unsupported slot adm format");
        }

        private IEnumerator LoadInlineVastXmlCoroutine(string vastXml)
        {
            if (VASTParser.IsWrapperVAST(vastXml))
            {
                var vastAdTagUri = VASTParser.ExtractVASTAdTagURI(vastXml);
                if (string.IsNullOrEmpty(vastAdTagUri))
                {
                    yield return HandleSlotFailure("Wrapper VAST has no VASTAdTagURI");
                    yield break;
                }

                yield return StartCoroutine(FetchNestedVASTRecursive(vastAdTagUri, vastXml));
            }
            else
            {
                _vastData = VASTParser.Parse(vastXml);
            }

            yield return PlayCurrentVastDataCoroutine();
        }

        private IEnumerator FetchAndLoadVastAdTagUrlCoroutine(string vastAdTagUrl, int depth = 0)
        {
            if (OpenRtbVideoUrlHelper.IsVastAdTagUrlRedirectDepthExceeded(depth))
            {
                yield return HandleSlotFailure("Maximum VAST ad tag URL redirect depth reached");
                yield break;
            }

            Logger.Info($"[VideoAdView] Fetching VAST ad tag URL (depth: {depth}): {vastAdTagUrl}");

            using (var request = UnityWebRequest.Get(vastAdTagUrl))
            {
                request.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                BidscubeSDK.ApplyConfiguredTimeoutTo(request);
                yield return request.SendWebRequest();

                if (request.responseCode == 204)
                {
                    yield return HandleSlotFailure("No fill (HTTP 204)");
                    yield break;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    yield return HandleSlotFailure($"Failed to fetch VAST ad tag URL: {request.error}");
                    yield break;
                }

                var responseText = request.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    yield return HandleSlotFailure("VAST ad tag URL returned empty body");
                    yield break;
                }

                if (VastAdSequenceParser.ContentLikelyContainsVast(responseText))
                {
                    yield return LoadInlineVastXmlCoroutine(responseText.Trim());
                    yield break;
                }

                if (responseText.TrimStart().StartsWith("{"))
                {
                    var sdkConfig = BidscubeSDK.GetConfiguration() ?? new SDKConfig.Builder().Build();
                    var resolved = VideoAdPayloadResolver.Resolve(responseText, sdkConfig);
                    var loadMode = VastAdTagJsonPlanLoader.GetNestedPlanLoadMode(resolved);
                    if (loadMode == VastAdTagJsonPlanLoader.NestedPlanLoadMode.FullPlan)
                    {
                        Logger.Info($"[VideoAdView] VAST ad tag URL returned nested playback plan with {resolved.PlaybackPlan.Slots.Count} slots.");
                        yield return LoadPlaybackPlanCoroutine(resolved.PlaybackPlan);
                        yield break;
                    }

                    if (loadMode == VastAdTagJsonPlanLoader.NestedPlanLoadMode.SingleSlot)
                    {
                        yield return LoadPlaybackSlotCoroutine(resolved.PlaybackPlan.Slots[0]);
                        yield break;
                    }

                    yield return HandleSlotFailure("Failed to resolve JSON from VAST ad tag URL");
                    yield break;
                }

                var trimmed = responseText.Trim();
                if (OpenRtbVideoUrlHelper.IsLikelyDirectVideoUrl(trimmed))
                {
                    _videoPlayer.url = trimmed;
                    yield return PrepareAndPlayVideoCoroutine(notifyLoadedOnPrepare: _currentPlaybackSlotIndex == 0);
                    yield break;
                }

                if (OpenRtbVideoUrlHelper.IsHttpUrl(trimmed))
                {
                    yield return FetchAndLoadVastAdTagUrlCoroutine(trimmed, depth + 1);
                    yield break;
                }

                yield return HandleSlotFailure("VAST ad tag URL response is not VAST, JSON, or video");
            }
        }

        private IEnumerator PlayCurrentVastDataCoroutine()
        {
            if (_vastData == null || string.IsNullOrEmpty(_vastData.videoUrl))
            {
                yield return HandleSlotFailure("Failed to parse VAST or no video URL in slot");
                yield break;
            }

            if (VideoSkipPolicy.IsSkippable(_vastData.skipOffset))
                _skipTime = _vastData.skipOffset;

            _videoPlayer.url = _vastData.videoUrl;
            yield return PrepareAndPlayVideoCoroutine(notifyLoadedOnPrepare: _currentPlaybackSlotIndex == 0);
        }

        private IEnumerator HandleSlotFailure(string message)
        {
            Logger.InfoError($"[VideoAdView] {message}");
            var config = BidscubeSDK.GetConfiguration() ?? new SDKConfig.Builder().Build();

            if (_isPlayingPod)
            {
                if (config.VideoPodSkipPolicy == OpenRtbPodSkipPolicy.FailEntirePod)
                {
                    NotifyFailed(Constants.ErrorCodes.InvalidResponse, message);
                    yield break;
                }

                if (config.VideoPodContinueOnSlotError && TryAdvanceToNextPlaybackSlot())
                    yield break;
            }

            NotifyFailed(Constants.ErrorCodes.InvalidResponse, message);
        }

        private IEnumerator PrepareAndPlayVideoCoroutine(bool notifyLoadedOnPrepare)
        {
            if (_videoPlayer == null || string.IsNullOrEmpty(_videoPlayer.url))
            {
                yield return HandleSlotFailure("No video URL available to play");
                yield break;
            }

            if (_videoPlayer.url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                using (var head = UnityWebRequest.Head(_videoPlayer.url))
                {
                    head.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                    BidscubeSDK.ApplyConfiguredTimeoutTo(head);
                    yield return head.SendWebRequest();

                    if (head.result != UnityWebRequest.Result.Success || head.responseCode >= 400)
                    {
                        var msg = $"Media URL not reachable (HTTP {head.responseCode}): {head.error}";
                        Logger.InfoError($"[VideoAdView] {msg}");
                        yield return HandleSlotFailure(msg);
                        yield break;
                    }
                }
            }

            ResetCacheState();
            if (Application.platform == RuntimePlatform.Android)
                TryStartLocalCacheFallback(_videoPlayer.url);

            Logger.Info($"[VideoAdView] Preparing video player with URL: {_videoPlayer.url}");

            _videoHadError = false;
            _videoError = null;
            BindAdAudio();
            _videoPlayer.Prepare();

            float timeout = Mathf.Max(1f, BidscubeSDK.GetConfiguredAdTimeoutMs() / 1000f);
            float elapsed = 0f;

            while (!_videoPlayer.isPrepared && !_videoHadError && elapsed < timeout)
            {
                if (_cacheReady && !string.IsNullOrEmpty(_cacheLocalUrl) &&
                    _videoPlayer != null && !string.Equals(_videoPlayer.url, _cacheLocalUrl, StringComparison.Ordinal))
                {
                    Logger.Info($"[VideoAdView] Switching to cached file during prepare loop: {_cacheLocalUrl}");
                    _videoHadError = false;
                    _videoError = null;
                    _videoPlayer.url = _cacheLocalUrl;
                    BindAdAudio();
                    _videoPlayer.Prepare();
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_videoHadError || !_videoPlayer.isPrepared)
            {
                if (TryStartLocalCacheFallback(_videoPlayer.url))
                {
                    elapsed = 0f;
                    while (!_videoPlayer.isPrepared && !_videoHadError && elapsed < timeout)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                if (!_videoPlayer.isPrepared)
                {
                    var msg = _videoHadError ? (_videoError ?? "Video error") : "Video preparation timeout";
                    Logger.InfoError($"[VideoAdView] Video failed: {msg}");
                    yield return HandleSlotFailure(msg);
                    yield break;
                }
            }

            Logger.Info("[VideoAdView] Video prepared successfully");
            if (notifyLoadedOnPrepare)
                NotifyLoaded();

            Logger.Info("[VideoAdView] Starting video playback...");
            _videoPlayer.Play();
        }

        private void UpdatePodCounterDisplay()
        {
            var config = BidscubeSDK.GetConfiguration();
            if (config == null || !config.VideoPodShowCounter || !_isPlayingPod || _playbackPlan == null)
            {
                if (_podCounterText != null)
                    _podCounterText.gameObject.SetActive(false);
                return;
            }

            if (_podCounterText == null)
            {
                var counterObj = new GameObject("PodCounter", typeof(RectTransform), typeof(Text));
                counterObj.transform.SetParent(transform, false);
                var rt = counterObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(200f, 36f);
                rt.anchoredPosition = new Vector2(0f, 24f);
                _podCounterText = counterObj.GetComponent<Text>();
                _podCounterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _podCounterText.fontSize = 16;
                _podCounterText.alignment = TextAnchor.MiddleCenter;
                _podCounterText.color = Color.white;
            }

            _podCounterText.gameObject.SetActive(true);
            _podCounterText.text = $"{_currentPlaybackSlotIndex + 1}/{_playbackPlan.Slots.Count}";
            _podCounterText.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Load video ad from URL (supports VAST XML or direct video URL)
        /// Uses IMA SDK if available, otherwise uses custom VAST parser
        /// </summary>
        /// <param name="url">VAST XML URL or direct video URL</param>
        public void LoadVideoAdFromURL(string url)
        {
            ResetCallbackState();
            ResetEndCardState();
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

        public void LoadVideoAdFromVastXml(string vastXml)
        {
            ResetCallbackState();
            ResetEndCardState();
            NotifyLoading();
            SetupUI();

            if (string.IsNullOrEmpty(vastXml))
            {
                NotifyFailed(Constants.ErrorCodes.InvalidResponse, "VAST XML is empty");
                return;
            }

            var sdkConfig = BidscubeSDK.GetConfiguration() ?? new SDKConfig.Builder().Build();
            var resolved = VideoAdPayloadResolver.Resolve(vastXml, sdkConfig);
            if (resolved?.PlaybackPlan != null && resolved.PlaybackPlan.IsPlayable)
            {
                StartCoroutine(LoadPlaybackPlanCoroutine(resolved.PlaybackPlan));
                return;
            }

            _vastData = VASTParser.Parse(vastXml);
            if (_vastData == null || string.IsNullOrEmpty(_vastData.videoUrl))
            {
                Logger.InfoError("[VideoAdView] Failed to parse local VAST or no video URL found");
                NotifyFailed(Constants.ErrorCodes.InvalidResponse, "Failed to parse local VAST XML");
                return;
            }

            if (VideoSkipPolicy.IsSkippable(_vastData.skipOffset))
                _skipTime = _vastData.skipOffset;

            _videoHadError = false;
            _videoError = null;
            _videoPlayer.url = _vastData.videoUrl;
            BindAdAudio();
            _videoPlayer.Prepare();
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

                if (request.responseCode == 204)
                {
                    yield return HandleSlotFailure("No fill (HTTP 204)");
                    yield break;
                }

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

                var sdkConfig = BidscubeSDK.GetConfiguration() ?? new SDKConfig.Builder().Build();

                if (responseText.TrimStart().StartsWith("{"))
                {
                    string admValue = null;
                    AdResponse jsonEnvelope = null;
                    if (AdMarkupExtractor.TryExtractMarkup(responseText, out var extracted, out _, out _))
                        admValue = extracted;
                    else
                    {
                        try
                        {
                            jsonEnvelope = JsonUtility.FromJson<AdResponse>(responseText);
                            if (jsonEnvelope != null)
                                admValue = jsonEnvelope.GetAdmString();
                        }
                        catch (Exception e)
                        {
                            Logger.InfoError($"[VideoAdView] JSON parsing failed: {e.Message}");
                        }
                    }

                    if (!string.IsNullOrEmpty(admValue) && _callback is IAdRenderOverride rawOverride)
                    {
                        int rawPos = jsonEnvelope != null
                            ? jsonEnvelope.GetPosition()
                            : (int)BidscubeSDK.GetResponseAdPosition();
                        bool handledRaw = false;
                        try
                        {
                            handledRaw = rawOverride.OnAdRenderOverride(_placementId, admValue, AdType.Video, rawPos);
                        }
                        catch (Exception e)
                        {
                            Logger.InfoError($"[VideoAdView] OnAdRenderOverride threw: {e.Message}");
                        }

                        if (handledRaw)
                        {
                            Logger.Info("[VideoAdView] Ad render overridden by app (raw adm); skipping SDK processing.");
                            yield break;
                        }
                    }
                }

                var resolved = VideoAdPayloadResolver.Resolve(responseText, sdkConfig);
                if (resolved != null && resolved.Position != AdPosition.Unknown)
                    BidscubeSDK.SetResponseAdPosition(resolved.Position);

                if (resolved?.PlaybackPlan != null && resolved.PlaybackPlan.IsPlayable)
                {
                    Logger.Info($"[VideoAdView] Resolved playback plan with {resolved.PlaybackPlan.Slots.Count} slot(s)");
                    yield return LoadPlaybackPlanCoroutine(resolved.PlaybackPlan);
                    yield break;
                }

                Logger.InfoError("[VideoAdView] Failed to resolve playable video ad payload");
                NotifyFailed(Constants.ErrorCodes.InvalidResponse, "Failed to resolve playable video ad payload");
            }
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            BindAdAudio(beforePrepare: false);
            NotifyLoaded();
            if (_videoPlayer != null && !_videoPlayer.isPlaying)
            {
                Logger.Info("[VideoAdView] Video prepared, starting playback...");
                _videoPlayer.Play();
            }
        }

        private void BindAdAudio()
        {
            BindAdAudio(beforePrepare: true);
        }

        private void BindAdAudio(bool beforePrepare)
        {
            if (_videoPlayer == null)
                return;

            CaptureExplicitAdMute();
            // Direct sends the video track to the platform output. This is the
            // route under test. It is not a measured result that AudioSource
            // cannot play the same file.
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            if (beforePrepare)
                _videoPlayer.controlledAudioTrackCount = 1;
            _videoPlayer.EnableAudioTrack(0, true);
            ApplyConfiguredAdGain();

            Logger.Info("[VideoAdView] renderer=VideoAdView"
                + " configuredRoute=Direct"
                + " configuredTrack=0"
                + " beforePrepare=" + beforePrepare
                + " readbackAudioTrackCount=" + _videoPlayer.audioTrackCount
                + " readbackDirectMute=" + _videoPlayer.GetDirectAudioMute(0)
                + " readbackDirectVolume=" + _videoPlayer.GetDirectAudioVolume(0)
                + " readbackListenerPause=" + AudioListener.pause
                + " readbackListenerVolume=" + AudioListener.volume
                + " evidence=videoplayer-property-readback not microphone");
        }

        private void CaptureExplicitAdMute()
        {
            if (_explicitAdMuteCaptured || _videoPlayer == null)
                return;
            _explicitAdMuteCaptured = true;
            if (_videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                _explicitAdMute = _videoPlayer.GetDirectAudioMute(0);
        }

        private void ApplyConfiguredAdGain()
        {
            if (_videoPlayer == null)
                return;
            var mute = _explicitAdMute;
            var gain = mute ? 0f : 1f;
            _videoPlayer.SetDirectAudioMute(0, mute);
            _videoPlayer.SetDirectAudioVolume(0, gain);
        }

        private void SilenceAdAudio()
        {
            if (_videoPlayer == null)
                return;
            _pausedForBackground = false;
            try
            {
                if (_videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                    _videoPlayer.SetDirectAudioMute(0, true);
                if (_videoPlayer.isPlaying)
                    _videoPlayer.Pause();
            }
            catch (Exception)
            {
                // The player may already be stopping.
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (_videoPlayer == null || _hasClosed || _isDestroying)
                return;
            if (paused)
            {
                _pausedForBackground = _videoPlayer.isPlaying;
                try
                {
                    if (_videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                        _videoPlayer.SetDirectAudioMute(0, true);
                    if (_videoPlayer.isPlaying)
                        _videoPlayer.Pause();
                }
                catch (Exception)
                {
                    _pausedForBackground = false;
                }
                return;
            }

            if (!_pausedForBackground)
                return;
            _pausedForBackground = false;
            ApplyConfiguredAdGain();
            try
            {
                _videoPlayer.Play();
            }
            catch (Exception)
            {
                // A closed player can reject Play. Leave it stopped.
            }
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            _videoHadError = true;
            _videoError = message;
            Logger.InfoError($"[VideoAdView] VideoPlayer errorReceived: {message}");
            _pausedForBackground = false;
            SilenceAdAudio();
            RestoreHostAudio();
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
                BindAdAudio();
                _videoPlayer.Prepare();
            }
        }

        private string TrackingContext(string eventName)
        {
            if (string.IsNullOrEmpty(_trackingSessionId))
                _trackingSessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            return "renderer=VideoAdView session=" + _trackingSessionId
                + " event=" + eventName
                + " placement=" + (_placementId ?? "");
        }

        private void OnVideoStarted(VideoPlayer source)
        {
            if (!_hasFiredImpression && !_hasClosed && isActiveAndEnabled
                && _vastData != null && _vastData.impressionUrls != null && _vastData.impressionUrls.Count > 0)
            {
                VASTParser.FireTrackingUrls(_vastData.impressionUrls, TrackingContext("impression"));
                _hasFiredImpression = true;
            }

            // Fire VAST start tracking URLs
            if (!_hasFiredStart && _vastData != null)
            {
                VASTParser.FireTrackingUrls(_vastData.startUrls, TrackingContext("start"));
                _hasFiredStart = true;
            }

            if (!_hasStarted)
            {
                NotifyDisplayed();
                NotifyStarted();
            }

            PauseHostAudio();

            StartCoroutine(UpdateProgress());
            _skipButtonCoroutine = StartCoroutine(EnableSkipButton());
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
                BidscubeSDK.ApplyConfiguredTimeoutTo(nestedRequest);
                yield return nestedRequest.SendWebRequest();

                if (nestedRequest.responseCode == 204)
                {
                    Logger.InfoError("[VideoAdView] Nested VAST no fill (HTTP 204)");
                    yield break;
                }

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
                                var wrapperImpressionUrls = ExtractWrapperImpressionUrls(nestedVastXml, nextVastAdTagUri);
                                if (wrapperImpressionUrls.Count > 0 && _vastData != null)
                                    MergeImpressionUrls(_vastData.impressionUrls, wrapperImpressionUrls);
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
                            var wrapperImpressionUrls = ExtractWrapperImpressionUrls(wrapperVastXml, vastAdTagUri);
                            MergeImpressionUrls(_vastData.impressionUrls, wrapperImpressionUrls);
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
        private List<string> ExtractWrapperImpressionUrls(string wrapperVastXml, string vastAdTagUri)
        {
            var impressionUrls = new List<string>();

            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(wrapperVastXml);
                var matched = FindWrapperAd(xmlDoc.DocumentElement, vastAdTagUri);
                CollectImpressionNodes(matched ?? xmlDoc.DocumentElement, impressionUrls);
            }
            catch (System.Exception e)
            {
                Logger.Info($"[VideoAdView] Failed to extract wrapper impression URLs: {e.Message}");
            }

            return impressionUrls;
        }

        private static System.Xml.XmlNode FindWrapperAd(System.Xml.XmlNode root, string vastAdTagUri)
        {
            if (root == null)
                return null;
            System.Xml.XmlNode firstAd = null;
            var ads = new List<System.Xml.XmlNode>();
            CollectElements(root, "Ad", ads);
            for (var i = 0; i < ads.Count; i++)
            {
                if (firstAd == null)
                    firstAd = ads[i];
                if (string.IsNullOrEmpty(vastAdTagUri))
                    continue;
                var tags = new List<System.Xml.XmlNode>();
                CollectElements(ads[i], "VASTAdTagURI", tags);
                for (var t = 0; t < tags.Count; t++)
                {
                    var text = tags[t].InnerText?.Trim();
                    if (!string.IsNullOrEmpty(text) && string.Equals(text, vastAdTagUri.Trim(), StringComparison.Ordinal))
                        return ads[i];
                }
            }
            return firstAd;
        }

        private static void CollectElements(System.Xml.XmlNode node, string localName, List<System.Xml.XmlNode> found)
        {
            if (node == null)
                return;
            if (node.NodeType == System.Xml.XmlNodeType.Element
                && string.Equals(node.LocalName, localName, StringComparison.OrdinalIgnoreCase))
            {
                found.Add(node);
                if (string.Equals(localName, "Ad", StringComparison.OrdinalIgnoreCase))
                    return;
            }
            foreach (System.Xml.XmlNode child in node.ChildNodes)
                CollectElements(child, localName, found);
        }

        private static void MergeImpressionUrls(List<string> target, List<string> extra)
        {
            if (target == null || extra == null)
                return;
            for (var i = 0; i < extra.Count; i++)
            {
                if (!string.IsNullOrEmpty(extra[i]) && !target.Contains(extra[i]))
                    target.Add(extra[i]);
            }
        }

        private static void CollectImpressionNodes(System.Xml.XmlNode node, List<string> urls)
        {
            if (node == null)
                return;
            if (node.NodeType == System.Xml.XmlNodeType.Element
                && string.Equals(node.LocalName, "Impression", StringComparison.OrdinalIgnoreCase))
            {
                var impUrl = node.InnerText?.Trim();
                if (!string.IsNullOrEmpty(impUrl) && !urls.Contains(impUrl))
                    urls.Add(impUrl);
            }

            foreach (System.Xml.XmlNode child in node.ChildNodes)
                CollectImpressionNodes(child, urls);
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
            if (_skipButton != null)
                _skipButton.gameObject.SetActive(true);
            if (_skipButton != null)
                _skipButton.interactable = false;

            float remaining = _skipTime;
            while (remaining > 0f)
            {
                if (_skipText != null)
                    _skipText.text = VideoSkipPolicy.FormatCountdownLabel(Mathf.CeilToInt(remaining));
                yield return new WaitForSeconds(1f);
                remaining -= 1f;
            }

            _isSkippable = true;
            if (_skipButton != null)
                _skipButton.interactable = true;
            if (_skipText != null)
                _skipText.text = VideoSkipPolicy.FormatCountdownLabel(0);
            _callback?.OnVideoAdSkippable(_placementId);
        }

        private void OnSkipClicked()
        {
            if (!_isSkippable || _isDestroying || _hasClosed)
                return;

            if (_vastData != null)
                VASTParser.FireTrackingUrls(_vastData.skipUrls);

            _isPlayingPod = false;
            NotifySkipped();
            PresentPostVideoUi(fromSkip: true);
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

        private void OnEndCardClicked()
        {
            if (_vastData == null)
                return;

            var clickUrl = VideoEndCardResolver.ResolveClickUrl(_vastData);

            if (string.IsNullOrEmpty(clickUrl))
                return;

            if (_vastData.companionClickTrackingUrls != null && _vastData.companionClickTrackingUrls.Count > 0)
                VASTParser.FireTrackingUrls(_vastData.companionClickTrackingUrls);

            Logger.Info($"[VideoAdView] Opening end-card click-through URL: {clickUrl}");
            Application.OpenURL(clickUrl);
            _callback?.OnAdClicked(_placementId);
        }

        private void OnCloseClicked()
        {
            if (_isDestroying || _hasClosed)
                return;

            _isPlayingPod = false;
            CloseAdSession(treatIncompleteAsSkip: true);
        }

        private void ShowEndCard()
        {
            // Legacy entry point — route through post-video policy.
            PresentPostVideoUi(fromSkip: _hasSkipped && !_hasCompleted);
        }

        private IEnumerator LoadEndCardPreview(string previewUrl)
        {
            if (_endCardPreview == null || string.IsNullOrEmpty(previewUrl))
                yield break;

            var requestId = ++_endCardRequestId;
            using (var request = UnityWebRequestTexture.GetTexture(previewUrl))
            {
                request.SetRequestHeader("User-Agent", DeviceInfo.UserAgent);
                yield return request.SendWebRequest();

                var downloaded = request.result == UnityWebRequest.Result.Success
                    ? DownloadHandlerTexture.GetContent(request)
                    : null;
                if (requestId != _endCardRequestId || this == null || _hasClosed || _endCardPreview == null)
                {
                    if (downloaded != null)
                        Destroy(downloaded);
                    yield break;
                }

                if (downloaded != null)
                {
                    ReleaseEndCardTexture();
                    _endCardOwnedTexture = downloaded;
                    _endCardPreview.texture = _endCardOwnedTexture;
                    _endCardPreview.color = Color.white;
                }
                else
                {
                    Logger.InfoError($"[VideoAdView] Failed to load preview image, using fallback: {request.error}");
                    ApplyEndCardFallbackFrame();
                }
            }
        }

        private void CancelEndCardLoad()
        {
            _endCardRequestId++;
            if (_endCardPreview != null)
            {
                _endCardPreview.texture = null;
                _endCardPreview.color = new Color(1f, 1f, 1f, 0f);
            }
            ReleaseEndCardTexture();
        }

        private void ApplyEndCardFallbackFrame()
        {
            if (_endCardPreview == null)
                return;
            var frame = _videoTexture != null ? _videoTexture.texture : null;
            _endCardPreview.texture = frame;
            _endCardPreview.color = frame != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        private void ReleaseEndCardTexture()
        {
            if (_endCardOwnedTexture == null)
                return;
            if (_endCardPreview != null && _endCardPreview.texture == _endCardOwnedTexture)
            {
                _endCardPreview.texture = null;
                _endCardPreview.color = new Color(1f, 1f, 1f, 0f);
            }
            Destroy(_endCardOwnedTexture);
            _endCardOwnedTexture = null;
        }

        private void PauseHostAudio()
        {
            if (_hostAudioPaused)
                return;
            _hostAudioPaused = true;
            var sources = FindObjectsOfType<AudioSource>();
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (source == null || source.gameObject == gameObject || !source.isPlaying)
                    continue;
                source.Pause();
                _pausedHostAudio.Add(source);
            }
        }

        private void RestoreHostAudio()
        {
            if (!_hostAudioPaused)
                return;
            _hostAudioPaused = false;
            for (var i = 0; i < _pausedHostAudio.Count; i++)
            {
                var source = _pausedHostAudio[i];
                if (source != null)
                    source.UnPause();
            }
            _pausedHostAudio.Clear();
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
            CloseAdSession(treatIncompleteAsSkip: !_hasCompleted && !_hasSkipped);
        }

        void DismissVideoAdHierarchy()
        {
            if (_isDestroying)
                return;
            _isDestroying = true;
            DestroyCompanionWebView();
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
            _pausedForBackground = false;
            SilenceAdAudio();
            try
            {
                _videoPlayer.Stop();
            }
            catch
            {
                // ignore
            }
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
            if (_companionWebView != null)
            {
                DestroyCompanionWebView();
            }

            if (_isDestroying)
            {
                if (!_hasClosed)
                    NotifyClosed();
                return;
            }

            _isDestroying = true;
            CancelEndCardLoad();
            RestoreHostAudio();
            ReleaseVideoPlayerResources();

            if (_hasStarted && !_hasCompleted && !_hasSkipped)
                NotifySkipped();
            if (!_hasClosed)
                NotifyClosed();
        }
    }
}

