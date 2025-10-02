using UnityEngine;
using BidscubeSDK.Examples;

namespace BidscubeSDK.Examples
{
    /// <summary>
    /// Script to automatically set up the Bidscube test scene
    /// This can be run from the Unity editor to create the complete UI hierarchy
    /// </summary>
    public class SceneAutoSetup : MonoBehaviour
    {
        [ContextMenu("Setup Bidscube Test Scene")]
        public void SetupTestScene()
        {
            // Find existing canvas or create new one
            var existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null && existingCanvas.name == "BidscubeExampleCanvas")
            {
                Debug.Log("Found existing BidscubeExampleCanvas, setting up UI elements...");
                SetupExistingCanvas(existingCanvas);
            }
            else
            {
                Debug.Log("Creating new Bidscube test scene...");
                var helper = new GameObject("SceneSetupHelper").AddComponent<SceneSetupHelper>();
                helper.CreateTestScene();
                DestroyImmediate(helper.gameObject);
            }
            
            Debug.Log("✅ Bidscube test scene setup completed!");
        }

        private void SetupExistingCanvas(Canvas canvas)
        {
            // Find the existing controller
            var controller = canvas.GetComponentInChildren<BidscubeExampleScene>();
            if (controller == null)
            {
                Debug.LogError("No BidscubeExampleScene controller found in canvas!");
                return;
            }

            // Create UI elements using the helper
            var helper = new GameObject("TempHelper").AddComponent<SceneSetupHelper>();
            helper.CreateUIElements(canvas.gameObject, controller);
            DestroyImmediate(helper.gameObject);
        }
    }
}

