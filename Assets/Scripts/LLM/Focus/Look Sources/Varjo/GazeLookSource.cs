using UnityEngine;
using Varjo.XR;

/// <summary>
/// GazeLookSource implements ILookSource to provide eye-tracking based look rays
/// using Varjo's eye tracking system (VR-3, XR-3, Aero headsets).
/// 
/// This is the most accurate look source available, as it tracks the user's
/// actual eye direction rather than relying on head orientation or controller pointing.
/// 
/// Varjo Eye Tracking Features:
/// - Sub-degree accuracy eye tracking
/// - 200Hz eye tracking rate (depending on headset)
/// - Combined gaze ray (averaged from both eyes)
/// - Status information for tracking quality
/// 
/// The gaze data from Varjo is provided in head-local coordinates, so this
/// component transforms it to world space using the headTransform reference.
/// 
/// Validation:
/// - Checks that VarjoGazeMonitor instance exists
/// - Validates that both eyes have valid tracking status
/// - Returns false if tracking is lost or unavailable
/// 
/// Usage:
/// - Requires Varjo XR Plugin and iMotions.VarjoIntegration package
/// - Attach to any GameObject in the scene
/// - Assign the head/camera transform for coordinate transformation
/// - Add to LookTracker with highest priority (most accurate source)
/// 
/// Priority Recommendation:
/// In multi-source setups, eye tracking should typically be highest priority:
/// [GazeLookSource, ControllerLookSource, HeadGazeLookSource]
/// </summary>
public class GazeLookSource : MonoBehaviour, ILookSource
{
    /// <summary>
    /// Transform representing the user's head position and orientation.
    /// Typically the VR camera or camera rig root.
    /// 
    /// Required for transforming Varjo's local gaze vectors to world space.
    /// Varjo provides gaze data relative to the headset's local coordinate system,
    /// so we must use this transform to convert to world coordinates.
    /// 
    /// Should be assigned to:
    /// - The XR Camera transform, or
    /// - The Camera Offset/Tracking Origin transform in XR Rig
    /// </summary>
    [Header("Transform Reference")]
    [Tooltip("Head transform (camera root) to convert Varjo local gaze vectors to world space.")]
    public Transform headTransform;

    /// <summary>
    /// Attempts to retrieve the current eye gaze ray from Varjo's eye tracking system.
    /// 
    /// This method performs several validation steps before returning a ray:
    /// 1. Validates that headTransform is assigned
    /// 2. Checks that VarjoGazeMonitor instance exists
    /// 3. Retrieves current gaze data from the monitor
    /// 4. Validates that both eyes have valid tracking status
    /// 5. Transforms the gaze ray from head-local to world coordinates
    /// 
    /// The "combined gaze" is used, which averages the gaze vectors from
    /// both eyes to produce a single, stable gaze direction.
    /// </summary>
    /// <param name="ray">
    /// Output: The eye gaze ray in world coordinates.
    /// Origin: The cyclopean eye position (average of both eyes) in world space
    /// Direction: The combined gaze direction in world space (normalized)
    /// </param>
    /// <returns>
    /// True if valid eye tracking data is available.
    /// False if:
    /// - headTransform is not assigned
    /// - VarjoGazeMonitor is not initialized
    /// - Either eye has invalid tracking status (eyes closed, out of range, etc.)
    /// </returns>
    public bool TryGetLookRay(out Ray ray)
    {
        // Initialize output ray to default (invalid) value
        ray = default;

        // Validation Step 1: Ensure headTransform is assigned.
        // Without this, we cannot transform local gaze data to world space.
        if (headTransform == null)
        {
            Debug.LogWarning("[GazeLookSource] Head transform is not assigned.");
            return false;
        }

        // Validation Step 2: Ensure VarjoGazeMonitor singleton instance exists.
        // This component manages the connection to Varjo's eye tracking system.
        if (Three.VarjoIntegration.VarjoGazeMonitor.Instance == null)
        {
            Debug.LogWarning("[GazeLookSource] VarjoGazeMonitor instance not found.");
            return false;
        }

        // Step 3: Retrieve the latest gaze data from Varjo's eye tracker.
        // The GazeData struct contains information for both eyes plus the combined gaze.
        var gazeData = Three.VarjoIntegration.VarjoGazeMonitor.Instance.GetCurrentGaze();

        // Validation Step 4: Check that both eyes have valid tracking status.
        // Invalid status can occur when:
        // - User's eyes are closed
        // - User's eyes are outside the tracking range
        // - Calibration is needed
        // - Hardware issue with the eye tracker
        if (gazeData.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid ||
            gazeData.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid)
        {
            // One or both eyes not tracked - cannot provide valid gaze ray
            return false;
        }

        // Step 5: Transform the gaze ray from head-local space to world space.
        // Varjo provides gaze data relative to the headset's coordinate system:
        // - Origin: Position relative to head center
        // - Forward: Direction relative to head orientation
        // 
        // We use TransformPoint for the origin (position transformation)
        // and TransformDirection for the forward vector (direction transformation).
        // 
        // TransformDirection automatically normalizes the result, but we
        // explicitly normalize anyway for consistency and safety.
        Vector3 origin = headTransform.TransformPoint(gazeData.gaze.origin);
        Vector3 direction = headTransform.TransformDirection(gazeData.gaze.forward).normalized;

        // Construct the world-space gaze ray
        ray = new Ray(origin, direction);

        // Debug logging (commented out for performance)
        // Uncomment for debugging gaze tracking issues:
        // Debug.Log($"[GazeLookSource] Gaze ray created: Origin = {origin}, Direction = {direction}");

        return true;
    }
}
