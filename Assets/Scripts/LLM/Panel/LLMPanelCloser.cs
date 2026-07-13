using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// LLMPanelCloser is a VR controller input handler that allows users to close
/// the LLM response panel using the grip button on their VR controller.
/// 
/// This provides a natural, physical interaction for dismissing information panels
/// in VR - similar to "putting something away" or "dropping" in physical space.
/// 
/// How it works:
/// 1. Monitors the specified controller's grip button state each frame
/// 2. When grip is pressed and an active panel exists, closes it
/// 3. The panel is searched for dynamically each frame (supports spawned panels)
/// 
/// Controller Configuration:
/// - Default: Right hand (XRNode.RightHand)
/// - Can be configured for left hand (XRNode.LeftHand)
/// - Uses Unity's XR Input System for cross-platform compatibility
/// 
/// Usage:
/// - Attach to any persistent GameObject (XR Rig, manager object, etc.)
/// - Configure which controller to use (left or right hand)
/// - Panels with LLMResponsePanelController will be closable by grip
/// 
/// Platform Support:
/// - Meta Quest 2/3/Pro
/// - Valve Index
/// - HP Reverb
/// - Any HMD supporting Unity XR Input
/// </summary>
public class LLMPanelCloser : MonoBehaviour
{
    /// <summary>
    /// Specifies which VR controller to monitor for grip input.
    /// 
    /// XRNode.RightHand: Uses the right controller's grip button (default)
    /// XRNode.LeftHand: Uses the left controller's grip button
    /// 
    /// The grip button is typically located on the side of VR controllers
    /// and is pressed by squeezing the controller.
    /// </summary>
    [Header("Controller Configuration")]
    [Tooltip("Which controller to use for grip detection. RightHand or LeftHand.")]
    public XRNode controllerNode = XRNode.RightHand;

    /// <summary>
    /// Cached reference to the XR input device for the specified controller.
    /// Obtained once during Start() and reused for efficient input reading.
    /// </summary>
    private InputDevice device;

    /// <summary>
    /// Unity Start callback - initializes the input device reference.
    /// 
    /// Uses InputDevices.GetDeviceAtXRNode to get the device corresponding
    /// to the configured controller (left or right hand).
    /// 
    /// Note: If the device is not connected at Start(), subsequent calls
    /// to TryGetFeatureValue may fail. Consider re-querying the device
    /// if input stops working.
    /// </summary>
    void Start()
    {
        // Get the XR input device for the specified controller node.
        // This caches the device reference for efficient polling in Update().
        device = InputDevices.GetDeviceAtXRNode(controllerNode);
    }

    /// <summary>
    /// Unity Update callback - checks for grip button press and closes panel if active.
    /// 
    /// This method runs every frame and:
    /// 1. Polls the grip button state from the cached XR device
    /// 2. If grip is pressed and a panel exists in the scene, closes it
    /// 
    /// The panel search uses FindObjectOfType which has performance implications,
    /// but is acceptable here because:
    /// - Only one panel typically exists at a time
    /// - Grip presses are relatively rare events
    /// - The search only happens when grip is pressed
    /// 
    /// Performance optimization: If multiple panels are common, consider
    /// caching the panel reference or using a singleton pattern.
    /// </summary>
    void Update()
    {
        // Storage for the grip button state
        bool gripPressed;

        // Query the grip button state from the XR device.
        // CommonUsages.gripButton is a standard input that works across different controllers.
        // TryGetFeatureValue returns false if the feature isn't available or device is disconnected.
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out gripPressed) && gripPressed)
        {
            // Grip is pressed - check if there's an active panel to close.
            // FindObjectOfType searches the entire scene for the component.
            var panel = FindObjectOfType<LLMResponsePanelController>();
            
            if (panel != null)
            {
                // Close the panel using its public method.
                // This triggers proper cleanup including:
                // - Notifying the spawner to clear its reference
                // - Destroying the panel GameObject
                panel.ClosePanel();
            }
        }
    }
}
