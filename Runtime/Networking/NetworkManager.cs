using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace BidscubeSDK
{
    /// <summary>
    /// Network manager for handling HTTP requests
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _instance;
        private float _timeout = 30.0f;

        /// <summary>
        /// Get singleton instance
        /// </summary>
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("NetworkManager");
                    _instance = go.AddComponent<NetworkManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initialize network manager
        /// </summary>
        /// <param name="timeout">Request timeout in seconds</param>
        public void Initialize(float timeout = 30.0f)
        {
            _timeout = timeout;
        }

        /// <summary>
        /// Perform GET request
        /// </summary>
        /// <param name="url">Request URL</param>
        /// <param name="callback">Callback for result</param>
        public void Get(string url, Action<NetworkResult> callback)
        {
            StartCoroutine(PerformGetRequest(url, callback));
        }

        /// <summary>
        /// Perform POST request
        /// </summary>
        /// <param name="url">Request URL</param>
        /// <param name="data">Request data</param>
        /// <param name="callback">Callback for result</param>
        public void Post(string url, string data, Action<NetworkResult> callback)
        {
            StartCoroutine(PerformPostRequest(url, data, callback));
        }

        private IEnumerator PerformGetRequest(string url, Action<NetworkResult> callback)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)_timeout;
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("User-Agent", $"{Constants.UserAgentPrefix}/{Constants.SdkVersion}");

                yield return request.SendWebRequest();

                var result = new NetworkResult
                {
                    IsSuccess = request.result == UnityWebRequest.Result.Success,
                    Data = request.downloadHandler.data,
                    Text = request.downloadHandler.text,
                    Error = request.error,
                    ResponseCode = request.responseCode
                };

                callback?.Invoke(result);
            }
        }

        private IEnumerator PerformPostRequest(string url, string data, Action<NetworkResult> callback)
        {
            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.timeout = (int)_timeout;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("User-Agent", $"{Constants.UserAgentPrefix}/{Constants.SdkVersion}");

                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                var result = new NetworkResult
                {
                    IsSuccess = request.result == UnityWebRequest.Result.Success,
                    Data = request.downloadHandler.data,
                    Text = request.downloadHandler.text,
                    Error = request.error,
                    ResponseCode = request.responseCode
                };

                callback?.Invoke(result);
            }
        }
    }

    /// <summary>
    /// Network result class
    /// </summary>
    [Serializable]
    public class NetworkResult
    {
        public bool IsSuccess { get; set; }
        public byte[] Data { get; set; }
        public string Text { get; set; }
        public string Error { get; set; }
        public long ResponseCode { get; set; }
    }

    /// <summary>
    /// Network error enumeration
    /// </summary>
    public enum NetworkError
    {
        InvalidURL,
        NoData,
        InvalidResponse,
        HttpError,
        Timeout,
        NetworkUnavailable,
        Unknown
    }
}