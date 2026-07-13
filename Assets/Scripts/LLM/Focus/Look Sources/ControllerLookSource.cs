using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// ControllerLookSource implements ILookSource to provide look/pointing direction
/// based on a VR controller's position and orientation.
/// 
/// This component provides trigger-activated pointing, meaning the look ray
/// is only considered "active" when the user presses the controller's trigger.
/// This is useful for:
/// - Intentional selection (user must explicitly activate pointing)
/// - Preventing accidental look interactions from idle controller position
/// - Combining with other look sources in a priority system
/// 
/// The ray is cast from the controller's position in the controller's forward direction,
/// matching the typical "laser pointer" visual commonly used in VR applications.
/// 
/// Usage:
/// - Attach to the controller GameObject or a manager object
/// - Assign the controller transform (defaults to this object's transform)
/// - Configure the trigger input action from Unity's Input System
/// - Add to LookTracker's look source list for integration
/// 
/// Platform Support:
/// - Meta Quest 2/3/Pro
/// - Valve Index
/// - Any controller using Unity XR Interaction Toolkit
/// </summary>
public class ControllerLookSource : MonoBehaviour, ILookSource
{
    /// <summary>
    /// The transform representing the controller's position and orientation.
    /// The look ray is cast from this transform's position in its forward direction.
    /// 
    /// If not assigned in the Inspector, defaults to this GameObject's transform.
    /// For accurate pointing, this should be the controller's "Aim" or "Pointer" transform
    /// (often a child of the main controller object oriented for pointing).
    /// </summary>
    [Header("Controller Reference")]
    [Tooltip("Transform representing the controller's position and forward direction. Defaults to this object's transform.")]
    public Transform controllerTransform;

    /// <summary>
    /// Input action for detecting trigger press.
    /// Should be configured to read the controller's trigger value as a float (0-1 range).
    /// 
    /// Using InputActionProperty allows either:
    /// - Direct action reference from an Input Action Asset
    /// - Embedded action defined inline in the Inspector
    /// 
    /// The trigger is considered "pressed" when the value exceeds 0.1 (10% threshold),
    /// which filters out accidental light touches and controller drift.
    /// </summary>
    [Header("Trigger Input (Input System)")]
    [Tooltip("Input action for trigger detection. Should output float 0-1. Threshold: 0.1 (10%)")]
    public InputActionProperty triggerAction;

    /// <summary>
    /// Unity Awake callback - performs early initialization.
    /// 
    /// Sets the controller transform to this object's transform if not explicitly assigned.
    /// Using Awake (instead of Start) ensures this runs before other components' Start methods.
    /// </summary>
    void Awake()
    {
        // Default to this object's transform if no controller transform is assigned.
        // This allows the component to work in simple setups where it's attached
        // directly to the controller object.
        if (controllerTransform == null)
            controllerTransform = this.transform;
    }

    /// <summary>
    /// Attempts to get the current look ray from this controller.
    /// 
    /// This implements a trigger-activated pointing model:
    /// - Returns true (with valid ray) only when trigger is pressed beyond threshold
    /// - Returns false when trigger is not pressed (controller "inactive")
    /// 
    /// This behavior enables the priority system in LookTracker:
    /// - If this source returns false, LookTracker checks the next priority source
    /// - If this source returns true, it's used and lower-priority sources are ignored
    /// 
    /// Use cases for trigger activation:
    /// - "Point and click" interaction model
    /// - Distinguishing intentional pointing from idle controller position
    /// - Allowing other sources (like eye tracking) to take over when trigger is released
    /// </summary>
    /// <param name="ray">
    /// Output: The look ray when trigger is pressed.
    /// Origin: Controller's world position
    /// Direction: Controller's forward vector (where it's pointing)
    /// </param>
    /// <returns>
    /// True if trigger is pressed (>0.1), indicating this source is active.
    /// False if trigger is not pressed, allowing other sources to be checked.
    /// </returns>
    public bool TryGetLookRay(out Ray ray)
    {
        // Initialize to invalid ray - will be overwritten if trigger is pressed
        ray = default;

        // Check if the trigger action is configured and the trigger is pressed.
        // The 0.1 threshold (10% press) filters out:
        // - Controller drift (small spurious values)
        // - Accidental light touches
        // - Resting finger on trigger without pressing
        if (triggerAction != null && triggerAction.action != null && triggerAction.action.ReadValue<float>() > 0.1f)
        {
            // Create a ray from the controller's position pointing forward
            // This matches the typical "laser pointer" visual in VR
            ray = new Ray(controllerTransform.position, controllerTransform.forward);
            return true; // Trigger is pressed - this source is active
        }

        // Trigger not pressed - this source is not providing a ray
        // LookTracker will check the next priority source
        return false;
    }
}
