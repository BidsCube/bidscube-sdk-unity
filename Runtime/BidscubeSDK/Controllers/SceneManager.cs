using UnityEngine;
using UnityEngine.SceneManagement;

namespace BidscubeSDK.Controllers
{
    /// <summary>
    /// Scene type enumeration
    /// </summary>
    public enum SceneType
    {
        Main,
        SDKTest,
        ConsentTest,
        WindowedAd
    }

    /// <summary>
    /// Scene reference for type-safe scene loading
    /// </summary>
    [System.Serializable]
    public class SceneReference
    {
        [SerializeField] private string sceneName;
        [SerializeField] private int sceneIndex = -1;

        public string SceneName => sceneName;
        public int SceneIndex => sceneIndex;

        public bool IsValid => !string.IsNullOrEmpty(sceneName) || sceneIndex >= 0;

        public void LoadScene()
        {
            if (!IsValid)
            {
                Logger.InfoError("SceneReference is not valid!");
                return;
            }

            if (sceneIndex >= 0)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
            }
            else if (!string.IsNullOrEmpty(sceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }
    }
    /// <summary>
    /// Scene manager for navigating between different test scenes
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private SceneReference mainScene;
        [SerializeField] private SceneReference sdkTestScene;
        [SerializeField] private SceneReference consentTestScene;
        [SerializeField] private SceneReference windowedAdScene;

        /// <summary>
        /// Load the main Bidscube Example Scene
        /// </summary>
        public void LoadMainScene()
        {
            LoadScene(mainScene);
        }

        /// <summary>
        /// Load the SDK Test Scene
        /// </summary>
        public void LoadSDKTestScene()
        {
            LoadScene(sdkTestScene);
        }

        /// <summary>
        /// Load the Consent Test Scene
        /// </summary>
        public void LoadConsentTestScene()
        {
            LoadScene(consentTestScene);
        }

        /// <summary>
        /// Load the Windowed Ad Test Scene
        /// </summary>
        public void LoadWindowedAdScene()
        {
            LoadScene(windowedAdScene);
        }

        private void LoadScene(SceneReference sceneRef)
        {
            if (sceneRef == null)
            {
                Logger.InfoError(" Scene reference is null!");
                return;
            }

            if (!sceneRef.IsValid)
            {
                Logger.InfoError(" Scene reference is not valid!");
                return;
            }

            try
            {
                sceneRef.LoadScene();
                Logger.Info($" Loaded scene: {sceneRef.SceneName}");
            }
            catch (System.Exception e)
            {
                Logger.InfoError($" Failed to load scene {sceneRef.SceneName}: {e.Message}");
            }
        }

        /// <summary>
        /// Get the current scene name
        /// </summary>
        public string GetCurrentSceneName()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Check if a scene reference is valid
        /// </summary>
        public bool IsSceneReferenceValid(SceneReference sceneRef)
        {
            return sceneRef != null && sceneRef.IsValid;
        }

        /// <summary>
        /// Get scene reference by type
        /// </summary>
        public SceneReference GetSceneReference(SceneType sceneType)
        {
            switch (sceneType)
            {
                case SceneType.Main:
                    return mainScene;
                case SceneType.SDKTest:
                    return sdkTestScene;
                case SceneType.ConsentTest:
                    return consentTestScene;
                case SceneType.WindowedAd:
                    return windowedAdScene;
                default:
                    return null;
            }
        }
    }
}
