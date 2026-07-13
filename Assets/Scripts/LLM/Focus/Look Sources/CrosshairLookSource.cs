using UnityEngine;

/// <summary>
/// CrosshairLookSource implements ILookSource to provide a simple camera-based
/// look ray for desktop (non-VR) applications.
/// 
/// This is the simplest look source implementation - it creates a ray from
/// the camera's position pointing directly forward (where the camera is facing).
/// 
/// Use cases:
/// - Desktop/keyboard-mouse testing of look-based interactions
/// - Non-VR versions of the application
/// - Simple "look at center of screen" mechanics
/// - First-person games where the player looks where the camera points
/// 
/// Unlike controller or eye-tracking sources, this source is ALWAYS active
/// (TryGetLookRay always returns true). This makes it ideal as a fallback
/// source at the bottom of the priority list.
/// 
/// The "crosshair" name comes from the typical UI pattern of displaying a
/// crosshair or reticle at the center of the screen to indicate where the
/// player is looking.
/// 
/// Usage:
/// - Attach to any GameObject in the scene
/// - Assign the main camera reference
/// - Add to LookTracker's source list (typically as lowest priority fallback)
/// </summary>
public class CrosshairLookSource : MonoBehaviour, ILookSource
{
    /// <summary>
    /// Reference to the main camera used for generating the look ray.
    /// The ray originates from this camera's position and points in its forward direction.
    /// 
    /// Typically this should be assigned to the scene's main camera.
    /// Can be left unassigned if you want to fall back to Camera.main (not implemented here).
    /// 
    /// Warning: If null, this component will throw a NullReferenceException.
    /// Ensure the camera is assigned before use.
    /// </summary>
    [Header("Camera Reference")]
    [Tooltip("The main camera to use for generating the look ray. Ray points in camera's forward direction.")]
    public Camera mainCam;

    /// <summary>
    /// Always returns a valid look ray from the camera's position pointing forward.
    /// 
    /// Unlike other look sources that may require trigger presses or valid tracking,
    /// this source is always "active" - the player is always looking wherever
    /// the camera is pointed.
    /// 
    /// This makes CrosshairLookSource ideal as:
    /// - A fallback when other sources aren't available
    /// - The primary source for desktop/non-VR configurations
    /// - A simple testing mechanism for VR look interactions on desktop
    /// 
    /// Warning: Assumes mainCam is not null. Will throw if camera is not assigned.
    /// </summary>
    /// <param name="ray">
    /// Output: A ray from the camera's world position pointing in its forward direction.
    /// This represents the center of the screen in world space.
    /// </param>
    /// <returns>
    /// Always returns true - this look source is always considered "active".
    /// The player is always looking at something (the center of their screen).
    /// </returns>
    public bool TryGetLookRay(out Ray ray)
    {
        // Create a ray from the camera's position pointing in its forward direction.
        // This ray represents "looking at the center of the screen" in world space.
        // 
        // Note: transform.forward is already normalized, so no need to normalize the direction.
        ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        // Always return true - crosshair gaze is always "active" since the player
        // is always looking at the center of their screen (where the crosshair is).
        return true;
    }
}
