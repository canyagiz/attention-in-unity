using UnityEngine;

/// <summary>
/// LLMResponseSpawner manages the creation and lifecycle of LLM response panels.
/// 
/// This component is responsible for:
/// - Instantiating the response panel prefab when needed
/// - Positioning panels appropriately between the object and camera
/// - Orienting panels to face the user
/// - Preventing duplicate panels (singleton pattern for active panel)
/// - Tracking the current panel for cleanup purposes
/// 
/// Panel Positioning:
/// The panel is positioned along a line between the analyzed object and the camera.
/// The 'distance' parameter controls where along this line the panel appears:
/// - 0.0: At the object's center
/// - 0.5: Halfway between object and camera
/// - 1.0: At the camera position
/// 
/// Default is 0.5, placing the panel midway for comfortable viewing.
/// 
/// Panel Orientation:
/// The panel is rotated to face the camera using LookAt(), then inverted
/// so the front faces the camera (LookAt points Z-forward at the target,
/// but UI canvases typically face -Z toward the viewer).
/// 
/// Singleton Active Panel:
/// Only one panel can be active at a time. Attempts to spawn a second
/// panel while one exists are ignored. The panel must be destroyed
/// (manually or by timeout) before a new one can spawn.
/// 
/// Usage:
/// LLMResponseSpawner spawner = FindObjectOfType&lt;LLMResponseSpawner&gt;();
/// spawner.ShowLLMResponse("Panel text here", targetRenderer);
/// </summary>
public class LLMResponseSpawner : MonoBehaviour
{
    /// <summary>
    /// The prefab to instantiate for response panels.
    /// 
    /// Requirements for the prefab:
    /// - Should have LLMResponsePanelController component
    /// - Should have a Canvas (World Space for VR)
    /// - Should have TextMeshPro for text display
    /// - Should be appropriately sized for VR reading
    /// </summary>
    [Header("Panel Configuration")]
    [Tooltip("Prefab with LLMResponsePanelController for displaying responses.")]
    public GameObject llmPanelPrefab;

    /// <summary>
    /// Controls positioning of the panel along the object-to-camera line.
    /// 
    /// Range: 0.0 to 1.0
    /// - 0.0: Panel spawns at the object's center (may overlap with object)
    /// - 0.5: Panel spawns halfway between object and camera (good default)
    /// - 1.0: Panel spawns at the camera position (too close for VR)
    /// 
    /// Default: 0.5 provides comfortable viewing distance in most VR scenarios.
    /// Adjust based on your typical object-to-camera distances.
    /// </summary>
    [Header("Positioning")]
    [Tooltip("Panel position along object-to-camera line. 0=at object, 1=at camera. Default 0.5")]
    [Range(0.0f, 1.0f)]
    public float distance = 0.5f;

    /// <summary>
    /// Reference to the currently active panel instance.
    /// 
    /// Null if no panel is currently displayed.
    /// Used to:
    /// - Prevent spawning duplicate panels
    /// - Track the active panel for cleanup
    /// </summary>
    private GameObject currentPanel;

    /// <summary>
    /// Displays an LLM response in a floating panel near the target object.
    /// 
    /// This method:
    /// 1. Checks if a panel already exists (prevents duplicates)
    /// 2. Calculates panel position based on object and camera positions
    /// 3. Instantiates the panel prefab at the calculated position
    /// 4. Rotates the panel to face the camera
    /// 5. Sets the response text on the panel controller
    /// </summary>
    /// <param name="message">The LLM response text to display</param>
    /// <param name="renderer">
    /// The renderer of the analyzed object.
    /// Used to calculate the panel's spawn position based on the object's center.
    /// </param>
    public void ShowLLMResponse(string message, Renderer renderer)
    {
        // Prevent duplicate panels - only one at a time
        if (currentPanel != null)
        {
            Debug.Log("[LLMResponseSpawner] Panel already exists. Skipping instantiation.");
            return;
        }

        // Get the center of the analyzed object's bounding box
        Vector3 center = renderer.bounds.center;

        // Calculate spawn position using linear interpolation (lerp).
        // Lerp(a, b, t) returns a point t% of the way from a to b.
        // With distance=0.5, the panel appears halfway between object and camera.
        Vector3 spawnPos = Vector3.Lerp(center, Camera.main.transform.position, distance);

        // Instantiate the panel prefab at the calculated position
        currentPanel = Instantiate(llmPanelPrefab, spawnPos, Quaternion.identity);

        // Calculate the direction from panel to camera for orientation.
        // We want the panel's front face to be visible to the camera.
        Vector3 lookDir = (Camera.main.transform.position - currentPanel.transform.position).normalized;
        
        // Rotate the panel to face the camera.
        // Quaternion.LookRotation creates a rotation where Z-forward points at the target.
        // We negate lookDir because we want the panel's front (-Z in most UI setups) facing the camera.
        currentPanel.transform.rotation = Quaternion.LookRotation(-lookDir);

        // Get the panel controller and set the response text
        var controller = currentPanel.GetComponent<LLMResponsePanelController>();
        if (controller != null)
        {
            controller.SetText(message);
        }
    }

    /// <summary>
    /// Checks if there is currently an active response panel.
    /// 
    /// Used by MaterialAnalyzer to skip LLM calls when a panel is already showing.
    /// This prevents overlapping analysis requests while the user is reading.
    /// </summary>
    /// <returns>True if a panel is currently displayed, false otherwise</returns>
    public bool HasActivePanel()
    {
        return currentPanel != null;
    }

    /// <summary>
    /// Clears the reference to the current panel.
    /// 
    /// Called by LLMResponsePanelController when the panel is destroyed
    /// (either by timeout or manual close). This notification system ensures:
    /// - HasActivePanel() returns false after panel destruction
    /// - New panels can be spawned after the old one is destroyed
    /// 
    /// Without this callback, the spawner would retain a reference to a
    /// destroyed object, causing HasActivePanel() to incorrectly return true.
    /// </summary>
    public void ClearPanelReference()
    {
        currentPanel = null;
    }
}
