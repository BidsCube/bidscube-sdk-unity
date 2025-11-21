/*
 * Copyright (C) 2012 GREE, Inc.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using BidscubeSDK;

public class NewWebViewController : MonoBehaviour
{
    [SerializeField]
    private string _htmlAd;
    public string HTMLad
    {
        get => _htmlAd;
        set
        {
            _htmlAd = value;
            if (webViewObject != null && !string.IsNullOrEmpty(_htmlAd))
            {
                BidscubeSDK.Logger.Info($"Reloading WebView with new HTML content: {_htmlAd.Length} characters");
                webViewObject.LoadHTML(_htmlAd, "");
            }
        }
    }
    public int LeftMargin, RightMargin, TopMargin, BottomMargin;

    [SerializeField]
    private WebViewObject webViewObject;

    private Coroutine _loadCoroutine;

    private void Start()
    {
        _loadCoroutine = StartCoroutine(LoadWebView());
        SetVisibility(true);
    }

    private void OnDisable()
    {
        if (_loadCoroutine != null)
        {
            StopCoroutine(_loadCoroutine);
        }
    }

    public void SetVisibility(bool visibility)
    {
        webViewObject.SetVisibility(visibility);
    }

    public bool GetVisibility()
    {
        return webViewObject.GetVisibility();
    }

    /// <summary>
    /// Manually reload the WebView with current HTML content
    /// </summary>
    public void ReloadContent()
    {
        if (webViewObject != null && !string.IsNullOrEmpty(HTMLad))
        {
            BidscubeSDK.Logger.Info($"Manually reloading WebView content: {HTMLad.Length} characters");
            webViewObject.LoadHTML(HTMLad, "");
            SetVisibility(true);
        }
        else
        {
            BidscubeSDK.Logger.Info("Cannot reload: WebViewObject is null or HTMLad is empty");
        }
    }

    // Note: Load web view loads the page but wont make it visible.
    // to do this, you must run SetVisibility(true);
    private IEnumerator LoadWebView()
    {
        webViewObject.Init(
            cb: (msg) =>
            {
                BidscubeSDK.Logger.Info($"CallFromJS[{msg}]");
            },
            err: (msg) =>
            {
                BidscubeSDK.Logger.InfoError($"CallOnError[{msg}]");
            },
            httpErr: (msg) =>
            {
                BidscubeSDK.Logger.InfoError($"CallOnHttpError[{msg}]");
            },
            started: (msg) =>
            {
                BidscubeSDK.Logger.Info($"CallOnStarted[{msg}]");
            },
            hooked: (msg) =>
            {
                BidscubeSDK.Logger.Info($"CallOnHooked[{msg}]");
            },
            cookies: (msg) =>
            {
                BidscubeSDK.Logger.Info($"CallOnCookies[{msg}]");
            },
            ld: (msg) =>
            {
                BidscubeSDK.Logger.Info($"CallOnLoaded[{msg}]");

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
            var js = @"
                if (!(window.webkit && window.webkit.messageHandlers)) {
                    window.Unity = {
                        call: function(msg) {
                            window.location = 'unity:' + msg;
                        }
                    };
                }
            ";
#else
                var js = "";
#endif
                webViewObject.EvaluateJS(js + @"Unity.call('ua=' + navigator.userAgent)");
            }
        );

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    webViewObject.bitmapRefreshCycle = 1;
    webViewObject.devicePixelRatio = 1;
#endif

        webViewObject.SetMargins(LeftMargin, TopMargin, RightMargin, BottomMargin);
        webViewObject.SetTextZoom(100);
        webViewObject.SetVisibility(true);

        // Load HTML content if available
        if (!string.IsNullOrEmpty(HTMLad))
        {
            BidscubeSDK.Logger.Info($"Loading initial HTML content: {HTMLad.Length} characters");
            webViewObject.LoadHTML(HTMLad, "");
        }
        else
        {
            BidscubeSDK.Logger.Info("No HTML content available for initial load");
        }

        yield break;
    }
}