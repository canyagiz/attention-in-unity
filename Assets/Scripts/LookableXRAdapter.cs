using UnityEngine;

// Conditionally include XR Interaction Toolkit namespace.
// This allows the project to compile on non-VR platforms (like standalone Windows)
// where XR packages may not be installed, while still supporting VR builds.
#if UNITY_EDITOR || UNITY_ANDROID
using UnityEngine.XR.Interaction.Toolkit;
#endif

/// <summary>
/// LookableXRAdapter is a utility component that automatically configures LookableObject
/// instances in the scene to be compatible with XR Interaction Toolkit's interaction system.
/// 
/// This adapter bridges the gap between the custom LookableObject system and Unity's
/// XR Interaction Toolkit, enabling objects that can be "looked at" to also be
/// interactable with VR controllers and hand tracking.
/// 
/// The adapter performs the following setup operations at runtime:
/// 1. Finds all LookableObject instances in the scene
/// 2. Adds XRSimpleInteractable components to make them VR-interactive
/// 3. Adds appropriate Collider components if missing (required for raycast detection)
/// 
/// This approach allows scene designers to focus on placing LookableObjects
/// without worrying about XR-specific component configuration.
/// 
/// Usage:
/// - Attach this script to any GameObject in your scene (typically an XR manager object)
/// - Enable/disable Quest interaction via the Inspector checkbox
/// - All LookableObjects will be automatically configured when the scene starts
/// 
/// Platform Support:
/// - Meta Quest 2/3/Pro (Android builds)
/// - Unity Editor (for testing)
/// - Can be disabled for non-VR builds via the Inspector checkbox
/// </summary>
public class LookableXRAdapter : MonoBehaviour
{
    /// <summary>
    /// Controls whether XR interaction components should be added to LookableObjects.
    /// 
    /// When ENABLED (true):
    /// - XRSimpleInteractable components are added to all LookableObjects
    /// - Colliders are automatically added if missing
    /// - Objects become fully interactive with VR controllers
    /// 
    /// When DISABLED (false):
    /// - No XR components are added
    /// - LookableObjects remain as-is
    /// - Useful for non-VR builds or specific gameplay sections
    /// 
    /// This checkbox provides a convenient way to toggle VR interaction support
    /// without removing the component or modifying code.
    /// </summary>
    [Header("Platform Configuration")]
    [Tooltip("Enable this for Meta Quest 3 builds. If unchecked, XR components will NOT be added.")]
    public bool enableQuestInteraction = true;

    /// <summary>
    /// Unity Start callback - initializes XR interaction components on all LookableObjects.
    /// 
    /// This method runs once when the scene starts and performs automatic setup
    /// to make all LookableObject instances compatible with XR interaction.
    /// 
    /// The setup process:
    /// 1. Checks if Quest interaction is enabled via the Inspector checkbox
    /// 2. Finds all LookableObject instances in the scene using FindObjectsOfType
    /// 3. For each object, adds XRSimpleInteractable if not already present
    /// 4. Ensures each object has a Collider for raycast detection
    /// 
    /// Note: This uses FindObjectsOfType which can be slow in large scenes.
    /// For performance-critical applications, consider caching or manual setup.
    /// </summary>
    void Start()
    {
        // Step 0: Check the enable/disable checkbox first
        // If Quest interaction is disabled via the Inspector, skip all setup.
        // This allows easy toggling for different build targets or gameplay modes.
        if (!enableQuestInteraction)
        {
            Debug.Log("[XR Adapter] Quest interaction is disabled via Inspector. Skipping setup.");
            return;
        }

        // Step 1: Find all LookableObject instances currently in the scene.
        // Note: This only finds active objects. Inactive objects will need
        // to be configured manually or activated before this runs.
        LookableObject[] allLookables = FindObjectsOfType<LookableObject>();

        // Log the discovery results for debugging purposes
        Debug.Log($"[XR Adapter] Found {allLookables.Length} Lookable objects, making them VR compatible...");

        // Step 2: Iterate through all discovered LookableObjects and configure each one
        foreach (var obj in allLookables)
        {
            // Step 3: Check if an XRSimpleInteractable component already exists.
            // This prevents adding duplicate components which could cause
            // conflicts or unexpected behavior in the interaction system.
            if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>() == null)
            {
                // Step 4: Add the XRSimpleInteractable component dynamically.
                // XRSimpleInteractable is a lightweight interactable that supports
                // hover, select, and activate events without complex grab mechanics.
                // This is ideal for objects that should respond to gaze or pointing.
                var interactable = obj.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();

                // Step 5: Ensure a Collider exists for raycast detection.
                // XR Interaction Toolkit relies on Physics raycasts to detect
                // interactable objects. Without a Collider, the object cannot
                // be detected by XRRayInteractor or other interactors.
                if (obj.GetComponent<Collider>() == null)
                {
                    // Automatic Collider selection based on the object's components:
                    // - If the object has a MeshFilter, add a MeshCollider for precise hit detection
                    // - Otherwise, add a BoxCollider as a simpler fallback
                    
                    if (obj.GetComponent<MeshFilter>() != null)
                    {
                        // MeshCollider provides accurate collision matching the visual mesh.
                        // Best for complex shapes where precise hit detection matters.
                        // Note: MeshCollider can be more expensive for physics calculations.
                        obj.gameObject.AddComponent<MeshCollider>();
                    }
                    else
                    {
                        // BoxCollider is a simple axis-aligned bounding box.
                        // More performant than MeshCollider but less precise.
                        // Suitable for objects without complex geometry or UI elements.
                        obj.gameObject.AddComponent<BoxCollider>();
                    }
                }
            }
        }
    }
}