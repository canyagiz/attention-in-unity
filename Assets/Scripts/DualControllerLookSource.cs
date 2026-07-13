using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DualControllerLookSource is a component that implements the ILookSource interface
/// to provide gaze/look direction based on VR controller input.
/// 
/// This class manages dual-controller input for determining where the user is pointing/looking.
/// It prioritizes the right controller by default (as most users are right-handed), 
/// but will fall back to the left controller if only the left trigger is pressed.
/// 
/// Usage:
/// - Attach this script to a GameObject in your scene (typically the XR Rig or a dedicated manager object)
/// - Assign the left and right hand transforms (typically the controller aim transforms)
/// - Configure the trigger input actions for both controllers
/// - Any system that needs to know where the user is looking can call TryGetLookRay()
/// </summary>
public class DualControllerLookSource : MonoBehaviour, ILookSource
{
    /// <summary>
    /// The transform representing the left hand controller's position and rotation.
    /// This is used to calculate the ray origin and direction when the left trigger is pressed.
    /// Typically, this should be assigned to the left controller's "Aim" or "Pointer" transform
    /// in the XR Rig hierarchy for accurate pointing direction.
    /// </summary>
    [Header("Left Controller")]
    [Tooltip("Reference to the left hand controller's transform. Used for ray origin and direction.")]
    public Transform leftHandTransform;

    /// <summary>
    /// Input action that reads the left controller's trigger value.
    /// The action should return a float value between 0 (not pressed) and 1 (fully pressed).
    /// This is typically bound to the left trigger or grip button through Unity's Input System.
    /// </summary>
    [Tooltip("Input action for the left controller's trigger. Should output a float value [0-1].")]
    public InputActionProperty leftTriggerAction;

    /// <summary>
    /// The transform representing the right hand controller's position and rotation.
    /// This is used to calculate the ray origin and direction when the right trigger is pressed.
    /// Typically, this should be assigned to the right controller's "Aim" or "Pointer" transform
    /// in the XR Rig hierarchy for accurate pointing direction.
    /// </summary>
    [Header("Right Controller")]
    [Tooltip("Reference to the right hand controller's transform. Used for ray origin and direction.")]
    public Transform rightHandTransform;

    /// <summary>
    /// Input action that reads the right controller's trigger value.
    /// The action should return a float value between 0 (not pressed) and 1 (fully pressed).
    /// This is typically bound to the right trigger or grip button through Unity's Input System.
    /// </summary>
    [Tooltip("Input action for the right controller's trigger. Should output a float value [0-1].")]
    public InputActionProperty rightTriggerAction;

    /// <summary>
    /// Attempts to get the current look ray based on which controller's trigger is being pressed.
    /// This method follows a priority system:
    ///   1. First, check if the RIGHT controller's trigger is pressed (since most users are right-handed)
    ///   2. If the right trigger is NOT pressed, check the LEFT controller's trigger
    ///   3. If neither trigger is pressed, return false (no active look ray available)
    /// 
    /// The ray is constructed using the active controller's position as the origin
    /// and its forward vector as the direction.
    /// </summary>
    /// <param name="ray">
    /// Output parameter: The resulting ray from the active controller.
    /// If no controller is active, this will be set to a default (invalid) ray.
    /// </param>
    /// <returns>
    /// Returns true if a valid look ray was generated (a trigger was pressed),
    /// Returns false if no trigger is currently pressed on either controller.
    /// </returns>
    public bool TryGetLookRay(out Ray ray)
    {
        // Initialize the ray to a default value in case no controller is active
        ray = default;

        // Step 1: Priority check - Examine the right controller first
        // The right hand is typically the dominant hand for most users,
        // so we give it priority when determining the look direction.
        // This ensures that if both triggers are pressed simultaneously,
        // the right controller takes precedence.
        if (IsTriggerPressed(rightTriggerAction))
        {
            // Create a ray starting from the right hand's position,
            // pointing in the direction the controller is facing (forward vector)
            ray = new Ray(rightHandTransform.position, rightHandTransform.forward);
            return true;
        }

        // Step 2: Fallback check - If the right trigger is not pressed, check the left controller
        // This allows left-handed users or situations where only the left hand is available
        // to still have functional pointing/looking capability
        if (IsTriggerPressed(leftTriggerAction))
        {
            // Create a ray starting from the left hand's position,
            // pointing in the direction the controller is facing (forward vector)
            ray = new Ray(leftHandTransform.position, leftHandTransform.forward);
            return true;
        }

        // Step 3: No trigger pressed on either controller
        // Return false to indicate that no valid look ray is available.
        // The calling system should handle this case appropriately
        // (e.g., by not performing any look-based interactions)
        return false;
    }

    /// <summary>
    /// Helper method to determine if a trigger button is currently being pressed.
    /// Performs safety checks to ensure the action and its properties are valid
    /// before attempting to read the input value.
    /// 
    /// A trigger is considered "pressed" if its analog value exceeds 0.1 (10% threshold).
    /// This small threshold helps filter out accidental touches and controller drift
    /// while still allowing responsive input detection.
    /// </summary>
    /// <param name="actionProperty">
    /// The InputActionProperty containing the trigger action to check.
    /// This wraps the Unity Input System's InputAction for easier Inspector serialization.
    /// </param>
    /// <returns>
    /// Returns true if the trigger is pressed beyond the threshold (>0.1),
    /// Returns false if the action is null, not configured, or the trigger value is below threshold.
    /// </returns>
    private bool IsTriggerPressed(InputActionProperty actionProperty)
    {
        // Perform null safety checks to prevent NullReferenceException:
        // 1. Check if the InputActionProperty itself is valid
        // 2. Check if the underlying InputAction is assigned
        // 3. Read the float value and compare against the threshold
        // The threshold of 0.1 prevents false positives from controller drift or light touches
        return actionProperty != null &&
               actionProperty.action != null &&
               actionProperty.action.ReadValue<float>() > 0.1f;
    }
}