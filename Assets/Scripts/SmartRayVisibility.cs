using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// SmartRayVisibility is a component that intelligently controls the visibility
/// of XR interaction ray visuals based on controller input state.
/// 
/// This class enhances the user experience by hiding the ray/pointer visualization
/// when it's not actively being used, reducing visual clutter in VR environments.
/// The ray becomes visible only when the user is actively engaging with the input
/// (e.g., pressing a trigger or moving a thumbstick beyond a threshold).
/// 
/// Key Features:
/// - Supports both analog inputs (triggers, grips) and 2D axis inputs (thumbsticks)
/// - Automatically detects the input type and reads values appropriately
/// - Configurable activation threshold for fine-tuning sensitivity
/// - Also hides the associated reticle when the ray is hidden
/// 
/// Usage:
/// - Attach this script to your XR controller GameObject (the one with XRController)
/// - Assign the input action that should control ray visibility (e.g., trigger press, thumbstick)
/// - Assign the XRInteractorLineVisual component whose visibility should be controlled
/// - Adjust the threshold value to tune the activation sensitivity
/// 
/// Common Configurations:
/// - Trigger-activated ray: Input = Trigger action, Threshold = 0.1
/// - Thumbstick-activated teleport ray: Input = Thumbstick action, Threshold = 0.1
/// </summary>
public class SmartRayVisibility : MonoBehaviour
{
    /// <summary>
    /// The input action that determines when the ray should be visible.
    /// This action's value is compared against the threshold to show/hide the ray.
    /// 
    /// The component automatically detects whether this is a:
    /// - Float action (0-1): Used for triggers, grips, and buttons
    /// - Vector2 action (x,y): Used for thumbsticks and touchpads
    /// 
    /// For Vector2 inputs, the magnitude of the vector is used for comparison.
    /// </summary>
    [Header("Settings")]
    [Tooltip("Input action controlling ray visibility. Supports both float (trigger) and Vector2 (thumbstick) inputs.")]
    public InputActionProperty inputAction;

    /// <summary>
    /// Reference to the XRInteractorLineVisual component whose visibility is being controlled.
    /// This is the component responsible for rendering the ray visual in the scene.
    /// 
    /// The LineVisual's enabled property is toggled based on the input state.
    /// When hidden, the associated reticle (if any) is also deactivated.
    /// </summary>
    [Tooltip("The XR Interactor Line Visual component to show/hide based on input.")]
    public XRInteractorLineVisual lineVisual;

    /// <summary>
    /// The activation threshold for showing the ray.
    /// Input values above this threshold will make the ray visible.
    /// Input values at or below this threshold will hide the ray.
    /// 
    /// Default: 0.1 (10%)
    /// - Lower values (0.01): More sensitive, ray appears with slight input
    /// - Higher values (0.5): Less sensitive, requires more deliberate input
    /// 
    /// The threshold helps filter out:
    /// - Controller drift (small unwanted input values)
    /// - Accidental light touches
    /// - Resting position variations
    /// </summary>
    [Tooltip("Input threshold for ray activation. Values above this show the ray. Range: 0.01 to 0.9")]
    [Range(0.01f, 0.9f)]
    public float threshold = 0.1f;

    /// <summary>
    /// Unity Update callback - checks input state every frame and updates ray visibility.
    /// 
    /// This method performs the following steps each frame:
    /// 1. Validates that required references are assigned (null safety check)
    /// 2. Detects the input type (float vs Vector2) and reads the appropriate value
    /// 3. Compares the input magnitude against the activation threshold
    /// 4. Shows or hides the ray visual based on the comparison result
    /// 5. Also hides the reticle when the ray is hidden
    /// 
    /// Performance Note: This runs every frame but uses minimal resources
    /// since it only does simple comparisons and property sets when needed.
    /// </summary>
    void Update()
    {
        // Safety check: Ensure both required references are assigned.
        // If either is missing, we cannot control visibility, so early exit.
        // This prevents NullReferenceException and allows the component
        // to be placed on objects before full configuration.
        if (inputAction.action == null || lineVisual == null) return;

        // Variable to store the normalized input magnitude (0 to 1 range)
        float inputMagnitude = 0f;

        // --- INPUT TYPE DETECTION AND VALUE READING ---
        // Unity Input System actions can have different control types.
        // We need to detect whether this action outputs a float (trigger/button)
        // or a Vector2 (thumbstick/touchpad) and read the value accordingly.
        
        // Check the expected control type declared for this action
        if (inputAction.action.expectedControlType == "Vector2")
        {
            // THUMBSTICK/TOUCHPAD INPUT:
            // For 2D inputs like joysticks, we read the Vector2 value
            // and calculate its magnitude (distance from center).
            // This gives us a single 0-1 value representing how far
            // the stick is pushed from the neutral position.
            // 
            // Example: Thumbstick at (0.5, 0.5) would have magnitude ~0.7
            // This is perfect for teleport activation (push stick = show teleport ray)
            inputMagnitude = inputAction.action.ReadValue<Vector2>().magnitude;
        }
        else
        {
            // TRIGGER/BUTTON INPUT:
            // For analog triggers and grips, read the float value directly.
            // These inputs naturally output 0 (not pressed) to 1 (fully pressed).
            // 
            // This handles:
            // - Analog triggers (variable 0-1 based on press depth)
            // - Digital buttons (0 or 1)
            // - Grip buttons (analog on most modern controllers)
            inputMagnitude = inputAction.action.ReadValue<float>();
        }
        // ---------------------------

        // Determine if the ray should be visible based on threshold comparison.
        // The ray appears when input exceeds the threshold (user is actively engaging)
        // and hides when input drops below the threshold (user released the input)
        bool shouldBeVisible = inputMagnitude > threshold;

        // Only update the visual state if it needs to change.
        // This prevents unnecessary property sets every frame,
        // which is slightly more efficient and cleaner for debugging.
        if (lineVisual.enabled != shouldBeVisible)
        {
            // Toggle the line visual's enabled state to show/hide the ray
            lineVisual.enabled = shouldBeVisible;

            // When hiding the ray, also hide the reticle if one exists.
            // The reticle typically appears at the end of the ray showing
            // where it hits geometry. It should disappear with the ray.
            // 
            // Note: We only hide the reticle when turning OFF the ray.
            // The XRInteractorLineVisual will automatically manage showing
            // the reticle when the ray is enabled and hits something.
            if (!shouldBeVisible && lineVisual.reticle != null)
            {
                lineVisual.reticle.SetActive(false);
            }
        }
    }
}