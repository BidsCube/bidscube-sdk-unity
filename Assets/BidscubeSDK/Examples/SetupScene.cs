using UnityEngine;
using BidscubeSDK.Examples;

namespace BidscubeSDK.Examples
{
    /// <summary>
    /// Simple script to set up the Bidscube test scene
    /// Add this to any GameObject in the scene and run the context menu
    /// </summary>
    public class SetupScene : MonoBehaviour
    {
        [ContextMenu("Setup Bidscube Scene")]
        public void SetupBidscubeScene()
        {
            // Find the existing canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas found in scene! Please make sure you have a Canvas in your scene.");
                return;
            }

            // Find the existing controller
            var controller = canvas.GetComponentInChildren<BidscubeExampleScene>();
            if (controller == null)
            {
                Debug.LogError("No BidscubeExampleScene controller found in canvas! Please make sure you have the BidscubeExampleController with BidscubeExampleScene script attached.");
                return;
            }

            Debug.Log("Setting up Bidscube test scene...");

            // Create a temporary helper to set up the UI
            var helper = new GameObject("TempSceneSetupHelper");
            var sceneHelper = helper.AddComponent<SceneSetupHelper>();
            
            try
            {
                // Set up the UI elements
                sceneHelper.CreateUIElements(canvas.gameObject, controller);
                Debug.Log("✅ Bidscube test scene setup completed successfully!");
            }
            finally
            {
                // Clean up the temporary helper
                DestroyImmediate(helper);
            }
        }
    }
}

