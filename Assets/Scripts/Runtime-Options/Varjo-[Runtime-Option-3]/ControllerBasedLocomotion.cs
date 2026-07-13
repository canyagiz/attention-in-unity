using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// ControllerBasedLocomotion provides smooth joystick-based movement for VR
/// using a VR controller's thumbstick input.
/// 
/// This component enables the user to move through the virtual environment
/// by pushing the controller's thumbstick, similar to FPS game controls.
/// The movement direction is relative to the player's head orientation,
/// providing intuitive "go where you're looking" navigation.
/// 
/// Movement Characteristics:
/// - Forward/Back: Thumbstick Y-axis, relative to camera forward
/// - Left/Right (Strafe): Thumbstick X-axis, relative to camera right
/// - No vertical movement: Y-axis locked to prevent flying/clipping
/// - Smooth, continuous motion: No teleportation, constant velocity
/// 
/// Camera-Relative Movement:
/// The movement direction is calculated based on where the camera (player's head)
/// is facing, not where the controller is pointing. This is the most intuitive
/// scheme for most users - push forward on the stick to go where you're looking.
/// 
/// Usage:
/// - Attach to any persistent GameObject (XR Rig recommended)
/// - Assign the XR Rig's root transform as rigRoot
/// - Configure which controller to use (left or right hand)
/// - Set movement speed as appropriate for your environment
/// 
/// Platform Support:
/// - Meta Quest 2/3/Pro
/// - Valve Index
/// - Any controller with thumbstick/touchpad
/// </summary>
public class ControllerBasedLocomotion : MonoBehaviour
{
    /// <summary>
    /// Reference to the XR Rig's root transform that will be moved.
    /// 
    /// This should be the topmost parent object of the XR camera hierarchy.
    /// Moving this transform moves the entire VR rig (camera, controllers, etc.)
    /// through the virtual world.
    /// 
    /// Typical hierarchy:
    /// - XR Rig (rigRoot) <- This should be assigned
    ///   - Camera Offset
    ///     - Main Camera
    ///     - Left Controller
    ///     - Right Controller
    /// </summary>
    [Header("XR Rig Configuration")]
    [Tooltip("The XR Rig root transform to move. This should be the topmost parent of the camera hierarchy.")]
    public Transform rigRoot;

    /// <summary>
    /// Specifies which VR controller's thumbstick to use for movement input.
    /// 
    /// Common configurations:
    /// - XRNode.LeftHand: Movement on left stick, right stick for rotation (common in games)
    /// - XRNode.RightHand: Movement on right stick (less common)
    /// 
    /// The choice depends on your control scheme and what other features
    /// use the thumbsticks (e.g., snap turn, teleport selection).
    /// </summary>
    [Tooltip("Which controller's thumbstick to use for movement input.")]
    public XRNode inputSource = XRNode.LeftHand;

    /// <summary>
    /// Movement speed in meters per second when the thumbstick is fully pushed.
    /// 
    /// The actual movement speed scales linearly with thumbstick displacement:
    /// - Thumbstick at 0%: No movement
    /// - Thumbstick at 50%: 50% of moveSpeed
    /// - Thumbstick at 100%: Full moveSpeed
    /// 
    /// Default: 1.5 m/s, which is a comfortable walking pace in VR.
    /// Higher speeds may cause motion sickness in some users.
    /// 
    /// VR Comfort Guidelines:
    /// - 1.0-2.0 m/s: Comfortable for most users
    /// - 2.0-4.0 m/s: May cause discomfort, provide options
    /// - 4.0+ m/s: High discomfort potential, use sparingly
    /// </summary>
    [Header("Movement Settings")]
    [Tooltip("Movement speed in meters per second. Default 1.5 is comfortable walking pace.")]
    public float moveSpeed = 1.5f;

    /// <summary>
    /// Unity Update callback - processes thumbstick input and moves the rig.
    /// 
    /// This method performs camera-relative movement:
    /// 1. Reads the thumbstick position from the configured controller
    /// 2. Gets the camera's forward and right directions (flattened to XZ plane)
    /// 3. Combines stick input with camera directions to get movement vector
    /// 4. Moves the rig by the calculated amount times speed and deltaTime
    /// 
    /// The Y-axis of directions is zeroed to prevent unintended vertical movement
    /// when looking up or down.
    /// </summary>
    void Update()
    {
        // Variable to store the thumbstick's current position
        Vector2 inputAxis;

        // Get the input device for the specified controller
        var device = InputDevices.GetDeviceAtXRNode(inputSource);

        // Attempt to read the thumbstick (primary2DAxis) value.
        // TryGetFeatureValue returns false if the device or feature is unavailable.
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out inputAxis))
        {
            // Get the camera's world-space directions for camera-relative movement.
            // These vectors tell us which way is "forward" and "right" from the player's perspective.
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;

            // Flatten the direction vectors to the horizontal plane (XZ).
            // This prevents vertical movement when the player looks up or down.
            // Without this, looking down while pushing forward would move you into the ground.
            camForward.y = 0;
            camRight.y = 0;

            // Normalize the flattened vectors to ensure consistent speed regardless of head tilt.
            // Without normalization, movement would be slower when looking straight ahead
            // vs. looking at an angle.
            camForward.Normalize();
            camRight.Normalize();

            // Calculate the movement direction by combining the flattened camera
            // directions with the thumbstick input:
            // - inputAxis.y (forward/back on stick) = movement along camForward
            // - inputAxis.x (left/right on stick) = movement along camRight
            Vector3 direction = camForward * inputAxis.y + camRight * inputAxis.x;

            // Apply movement to the rig.
            // Multiply by moveSpeed and deltaTime for consistent, frame-rate-independent movement.
            rigRoot.position += direction * moveSpeed * Time.deltaTime;
        }
    }
}
