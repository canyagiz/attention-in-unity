using UnityEngine;

/// <summary>
/// ILookSource is an interface that defines the contract for any component
/// that can provide a "look" or "gaze" direction ray.
/// 
/// This interface enables a polymorphic approach to handling different input
/// modalities for determining where the user is looking or pointing:
/// - Eye tracking (Varjo, Tobii, etc.)
/// - Controller pointing (Quest, Index, etc.)
/// - Head/crosshair gaze (simple camera forward)
/// - Mouse-based desktop crosshair
/// 
/// The interface uses the "Try" pattern (TryGetLookRay) which is ideal for
/// scenarios where a look source may not always be active or available.
/// For example:
/// - Eye tracker might not have valid data momentarily
/// - Controller might only be "active" when trigger is pressed
/// - Head gaze is always available (TryGet always returns true)
/// 
/// Usage:
/// Implement this interface on any MonoBehaviour that should serve as a look source.
/// The LookTracker component will call TryGetLookRay() each frame to poll for rays.
/// 
/// Example implementations:
/// - GazeLookSource (Varjo eye tracking)
/// - ControllerLookSource (VR controller pointing)
/// - CrosshairLookSource (desktop camera-based crosshair)
/// - HeadGazeLookSource (VR head direction)
/// </summary>
public interface ILookSource
{
    /// <summary>
    /// Attempts to retrieve the current look/gaze ray from this source.
    /// 
    /// This method follows the "Try" pattern common in C#:
    /// - Returns true if a valid ray is available, with the ray in the out parameter
    /// - Returns false if no valid ray is available (e.g., trigger not pressed, tracking lost)
    /// 
    /// The caller (typically LookTracker) uses this to determine which source
    /// is currently "active" in a priority-based system. The first source
    /// that returns true is used; others are ignored for that frame.
    /// </summary>
    /// <param name="ray">
    /// Output parameter: The look ray if available.
    /// - Origin: Starting point of the ray (eye position, controller position, camera position)
    /// - Direction: Normalized direction the user is looking/pointing
    /// 
    /// If the method returns false, the ray value should be considered invalid/undefined.
    /// </param>
    /// <returns>
    /// True if a valid look ray is currently available from this source.
    /// False if this source is not currently active (trigger not pressed, tracking lost, etc.)
    /// </returns>
    bool TryGetLookRay(out Ray ray);
}
