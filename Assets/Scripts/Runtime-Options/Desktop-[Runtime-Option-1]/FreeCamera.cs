using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// FreeCamera provides a Unity editor-style free-flying camera controller
/// for desktop debugging and development.
/// 
/// This component enables WASD movement, mouse look, and zoom controls,
/// similar to the Scene View camera in the Unity Editor. It's useful for:
/// - Testing the application without a VR headset
/// - Debugging object placement and scene setup
/// - Creating screenshots and recordings
/// - Rapid iteration during development
/// 
/// Control Scheme:
/// 
/// Movement (relative to camera orientation):
/// - W/Up Arrow: Move forward
/// - S/Down Arrow: Move backward
/// - A/Left Arrow: Strafe left
/// - D/Right Arrow: Strafe right
/// - Q: Move up (relative to camera)
/// - E: Move down (relative to camera)
/// - R/PageUp: Move up (world space - always straight up)
/// - F/PageDown: Move down (world space - always straight down)
/// - Hold Shift: Fast movement mode
/// 
/// Camera Rotation:
/// - Right Mouse Button + Mouse: Look around (pitch and yaw)
/// - Mouse released: Stops rotation, unlocks cursor
/// 
/// Zoom:
/// - Mouse Scroll Wheel: Adjusts camera FOV (zoom in/out)
/// 
/// Usage:
/// - Attach to a Camera GameObject
/// - Use right-click + mouse to look around
/// - Use WASD to fly around the scene
/// - Hold Shift for faster movement
/// 
/// Note: This is for desktop testing only. In VR builds, this script
/// should be disabled or the GameObject deactivated.
/// </summary>
public class FreeCamera : MonoBehaviour
{
    /// <summary>
    /// Normal movement speed in units per second.
    /// Default: 10 units/second for comfortable scene navigation.
    /// </summary>
    [Header("Movement Settings")]
    [Tooltip("Normal movement speed in units per second.")]
    public float movementSpeed = 10f;

    /// <summary>
    /// Movement speed when holding Shift key.
    /// Default: 100 units/second for rapid scene traversal.
    /// </summary>
    [Tooltip("Movement speed when holding Shift. For rapid traversal.")]
    public float fastMovementSpeed = 100f;

    /// <summary>
    /// Mouse look sensitivity for rotation.
    /// Higher values = faster rotation for the same mouse movement.
    /// Default: 3 provides smooth, controllable rotation.
    /// </summary>
    [Header("Look Settings")]
    [Tooltip("Mouse sensitivity for rotation. Higher = faster rotation.")]
    public float freeLookSensitivity = 3f;

    /// <summary>
    /// Sensitivity for scroll wheel zoom.
    /// Controls how much FOV changes per scroll wheel tick.
    /// Default: 10 degrees per tick.
    /// </summary>
    [Header("Zoom Settings")]
    [Tooltip("Normal zoom sensitivity (FOV change per scroll tick).")]
    public float zoomSensitivity = 10f;

    /// <summary>
    /// Zoom sensitivity when holding Shift.
    /// Allows rapid FOV changes for quick zooming.
    /// Default: 50 degrees per tick.
    /// </summary>
    [Tooltip("Fast zoom sensitivity when holding Shift.")]
    public float fastZoomSensitivity = 50f;

    /// <summary>
    /// Tracks whether the camera is currently in "looking" mode.
    /// True when right mouse button is held down.
    /// When looking, mouse movement rotates the camera and cursor is locked.
    /// </summary>
    private bool looking = false;

    /// <summary>
    /// Unity Update callback - processes input and updates camera every frame.
    /// 
    /// Handles all input types:
    /// - Movement keys (WASD, arrows, QERF)
    /// - Mouse look (when right button held)
    /// - Scroll wheel zoom
    /// - Right mouse button press/release for look mode toggle
    /// </summary>
    void Update()
    {
        // Check if fast mode is active (either Shift key)
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

        // Strafe left: A key or Left Arrow
        // Moves camera along its local right axis (negative direction)
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
        }

        // Strafe right: D key or Right Arrow
        // Moves camera along its local right axis (positive direction)
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
        }

        // Move forward: W key or Up Arrow
        // Moves camera along its local forward axis
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.position = transform.position + (transform.forward * movementSpeed * Time.deltaTime);
        }

        // Move backward: S key or Down Arrow
        // Moves camera along its local forward axis (negative direction)
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            transform.position = transform.position + (-transform.forward * movementSpeed * Time.deltaTime);
        }

        // Move up (camera-relative): Q key
        // Moves camera along its local up axis
        // Different from R key which uses world up
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position = transform.position + (transform.up * movementSpeed * Time.deltaTime);
        }

        // Move down (camera-relative): E key
        // Moves camera along its local up axis (negative direction)
        if (Input.GetKey(KeyCode.E))
        {
            transform.position = transform.position + (-transform.up * movementSpeed * Time.deltaTime);
        }

        // Move up (world space): R key or PageUp
        // Always moves straight up regardless of camera orientation
        // Useful for quickly gaining altitude
        if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
        {
            transform.position = transform.position + (Vector3.up * movementSpeed * Time.deltaTime);
        }

        // Move down (world space): F key or PageDown
        // Always moves straight down regardless of camera orientation
        if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
        {
            transform.position = transform.position + (-Vector3.up * movementSpeed * Time.deltaTime);
        }

        // Mouse look rotation (when right mouse button is held)
        // Rotates camera based on mouse movement for free look
        if (looking)
        {
            // Calculate new rotation angles based on mouse delta
            // X mouse movement = Y rotation (yaw/horizontal look)
            // Y mouse movement = X rotation (pitch/vertical look)
            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
            
            // Apply the new rotation (Z rotation stays at 0 to prevent roll)
            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        // Scroll wheel zoom (adjusts camera FOV)
        // Narrower FOV = zoomed in, wider FOV = zoomed out
        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis > 0)
        {
            // Scroll up = zoom in = decrease FOV
            GetComponent<Camera>().fieldOfView--;
        }
        else if (axis < 0)
        {
            // Scroll down = zoom out = increase FOV
            GetComponent<Camera>().fieldOfView++;
        }

        // Right mouse button pressed = start looking
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartLooking();
        }
        // Right mouse button released = stop looking
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            StopLooking();
        }
    }

    /// <summary>
    /// Unity OnDisable callback - ensures proper cleanup when component is disabled.
    /// 
    /// Stops looking mode to restore cursor state, preventing the cursor
    /// from remaining locked/hidden if the component is disabled while looking.
    /// </summary>
    void OnDisable()
    {
        StopLooking();
    }

    /// <summary>
    /// Enters looking mode - cursor is hidden and locked for mouse look.
    /// 
    /// Called when right mouse button is pressed.
    /// Enables mouse movement to rotate the camera.
    /// </summary>
    public void StartLooking()
    {
        looking = true;
        
        // Hide the cursor for immersive look experience
        Cursor.visible = false;
        
        // Lock cursor to center of screen - mouse movement becomes delta values
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Exits looking mode - cursor is restored for normal UI interaction.
    /// 
    /// Called when right mouse button is released or component is disabled.
    /// Mouse movement no longer rotates the camera.
    /// </summary>
    public void StopLooking()
    {
        looking = false;
        
        // Show the cursor again
        Cursor.visible = true;
        
        // Unlock cursor for normal mouse interaction
        Cursor.lockState = CursorLockMode.None;
    }
}