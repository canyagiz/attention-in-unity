using UnityEngine;

/// <summary>
/// HeadGazeLookSource is a component that implements the ILookSource interface
/// to provide head-based gaze tracking functionality for VR applications.
/// 
/// This class uses the main camera's transform (representing the player's head in VR)
/// to determine where the user is looking. Unlike controller-based look sources,
/// head gaze is always active - wherever the player looks with their head, 
/// that becomes the look direction.
/// 
/// Additionally, this component manages an optional visual reticle that shows
/// the user exactly where they are looking in 3D space. The reticle:
/// - Snaps to surfaces when the gaze ray hits geometry
/// - Floats at a fixed distance when looking at empty space
/// - Orients itself appropriately (billboard effect or surface normal alignment)
/// 
/// Usage:
/// - Attach this script to a GameObject in your scene (typically the XR Rig or a dedicated manager)
/// - Optionally assign the main camera transform (will auto-detect if not assigned)
/// - Configure the reticle prefab and settings as desired
/// - Any system needing gaze direction can call TryGetLookRay()
/// </summary>
public class HeadGazeLookSource : MonoBehaviour, ILookSource
{
    /// <summary>
    /// Reference to the main camera's transform, representing the player's head position and orientation.
    /// This transform is used as the origin and direction for the gaze ray.
    /// If not manually assigned in the Inspector, this will be automatically set to Camera.main.transform
    /// during the Start() method.
    /// 
    /// Note: In VR, this should be the XR Camera that moves with the headset.
    /// </summary>
    [Header("Configuration")]
    [Tooltip("Reference to the main camera transform (player's head). Auto-detects Camera.main if not assigned.")]
    public Transform cameraTransform;

    /// <summary>
    /// The maximum distance for raycast operations when determining what the player is looking at.
    /// This value limits how far the gaze detection will reach.
    /// Objects beyond this distance will not be detected by the raycast.
    /// 
    /// Default: 20 meters, suitable for most indoor VR environments.
    /// Increase for large outdoor scenes, decrease for focused close-range interactions.
    /// </summary>
    [Tooltip("Maximum raycast distance for gaze detection. Objects beyond this distance won't be detected.")]
    public float maxDistance = 20f;

    /// <summary>
    /// Controls whether the visual reticle indicator is displayed.
    /// When enabled, a visual indicator appears at the point where the player is looking.
    /// This can be toggled at runtime through the Inspector or via script.
    /// 
    /// Use cases for disabling:
    /// - When you want gaze tracking without visual feedback
    /// - During cutscenes or specific gameplay moments
    /// - When another UI element should have focus
    /// </summary>
    [Header("Reticle Settings")]
    [Tooltip("Toggle visibility of the gaze reticle. Can be changed at runtime.")]
    public bool showReticle = true;

    /// <summary>
    /// The prefab to instantiate as the visual gaze reticle.
    /// This should be a simple visual indicator (e.g., a ring, dot, or crosshair)
    /// that shows the player where their gaze is landing.
    /// 
    /// Requirements for the prefab:
    /// - Should be a relatively flat object (ring or disc works well)
    /// - Should have a shader that renders on top of other geometry (optional but recommended)
    /// - Should be appropriately scaled for VR (usually small, 0.02-0.05 units)
    /// - Should NOT have a collider to avoid interfering with raycasts
    /// </summary>
    [Tooltip("Prefab for the visual reticle indicator. Should be a simple ring, dot, or crosshair object.")]
    public GameObject reticlePrefab;

    /// <summary>
    /// The distance at which the reticle floats when the gaze ray doesn't hit any geometry.
    /// When the player looks at empty space (like the sky), the reticle will appear
    /// at this distance from the camera to maintain visibility.
    /// 
    /// Default: 2 meters, which keeps the reticle comfortably visible without being too close.
    /// Adjust based on your VR experience's typical interaction distances.
    /// </summary>
    [Tooltip("Distance to position the reticle when looking at empty space (no geometry hit).")]
    public float defaultDistance = 2.0f;

    /// <summary>
    /// Runtime instance of the reticle prefab.
    /// This is created during Start() and managed throughout the component's lifetime.
    /// The instance is repositioned every frame based on where the player is looking.
    /// </summary>
    private GameObject currentReticleInstance;

    /// <summary>
    /// Stores the original scale of the reticle prefab.
    /// Preserved for potential future use (e.g., distance-based scaling).
    /// Currently captured but not actively used for scaling calculations.
    /// </summary>
    private Vector3 originalScale;

    /// <summary>
    /// Unity Start callback - initializes the component when the scene loads.
    /// 
    /// Performs two main setup operations:
    /// 1. Auto-detects the main camera if not manually assigned
    /// 2. Instantiates the reticle prefab if one is provided
    /// 
    /// The reticle's initial visibility is set based on the showReticle flag.
    /// </summary>
    void Start()
    {
        // Auto-detect camera if not manually assigned in the Inspector.
        // This provides a sensible default while still allowing explicit camera assignment
        // for more complex setups (e.g., multiple cameras, non-standard XR rigs)
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        // Instantiate the reticle if a prefab has been provided.
        // The reticle serves as a visual indicator showing the player exactly
        // where their head gaze is directed in 3D space.
        if (reticlePrefab != null)
        {
            // Create a new instance of the reticle prefab in the scene
            currentReticleInstance = Instantiate(reticlePrefab);

            // Store the original scale for potential future scaling operations
            // (e.g., maintaining consistent screen-space size regardless of distance)
            originalScale = currentReticleInstance.transform.localScale;

            // Set initial visibility based on the Inspector checkbox setting
            // This ensures the reticle respects the showReticle flag from the start
            currentReticleInstance.SetActive(showReticle);
        }
    }

    /// <summary>
    /// Unity Update callback - called every frame to manage reticle visibility and positioning.
    /// 
    /// This method handles:
    /// 1. Runtime toggle checking - allows showReticle to be changed during play
    /// 2. Reticle position updates - moves the reticle to match the current gaze point
    /// 
    /// Performance consideration: If the reticle is hidden, position calculations are skipped
    /// to avoid unnecessary raycast operations.
    /// </summary>
    void Update()
    {
        // Step 1: Handle runtime visibility toggle
        // Check if the visibility setting has changed and update the GameObject accordingly.
        // This allows designers or gameplay systems to show/hide the reticle dynamically.
        if (currentReticleInstance != null)
        {
            // Only update the active state if it differs from the desired state
            // This prevents unnecessary SetActive calls every frame
            if (currentReticleInstance.activeSelf != showReticle)
            {
                currentReticleInstance.SetActive(showReticle);
            }

            // Step 2: Early exit optimization
            // If the reticle is hidden, there's no need to calculate its position.
            // Skip the raycast and positioning logic to save performance.
            if (!showReticle) return;

            // Step 3: Update the reticle's position and rotation based on current gaze
            UpdateReticlePosition();
        }
    }

    /// <summary>
    /// Implementation of the ILookSource interface.
    /// Attempts to retrieve the current gaze ray based on head orientation.
    /// 
    /// Unlike controller-based look sources, head gaze is always "active" -
    /// the player is always looking somewhere with their head. Therefore,
    /// this method will return true as long as the camera reference is valid.
    /// 
    /// The ray originates from the camera's position (eye/head position in VR)
    /// and points in the camera's forward direction (where the player is facing).
    /// </summary>
    /// <param name="ray">
    /// Output parameter: A ray representing the player's current head gaze direction.
    /// Origin is at the camera position, direction is the camera's forward vector.
    /// </param>
    /// <returns>
    /// Returns true if the camera reference is valid and a ray was generated.
    /// Returns false if the camera transform is null (configuration error).
    /// </returns>
    public bool TryGetLookRay(out Ray ray)
    {
        // Validate that we have a valid camera reference
        // If the camera transform is missing, we cannot generate a look ray
        if (cameraTransform == null)
        {
            ray = default;
            return false;
        }

        // Head gaze is always active - the player is always looking somewhere.
        // Create a ray from the camera position pointing in the head's forward direction.
        // In VR, this accurately represents where the player's eyes are directed.
        ray = new Ray(cameraTransform.position, cameraTransform.forward);
        return true;
    }

    /// <summary>
    /// Updates the reticle's world position and rotation based on the current gaze ray.
    /// 
    /// This method implements two different behaviors:
    /// 
    /// 1. Surface Hit Mode (when gaze ray hits geometry):
    ///    - Positions the reticle at the exact hit point
    ///    - Rotates the reticle to align with the surface normal (appears "stuck" to the surface)
    ///    - Applies a small offset along the normal to prevent z-fighting/flickering
    /// 
    /// 2. Floating Mode (when gaze ray hits nothing):
    ///    - Positions the reticle at a fixed distance in front of the camera
    ///    - Rotates the reticle to face the camera (billboard effect)
    ///    - Provides visual feedback even when looking at empty space
    /// </summary>
    private void UpdateReticlePosition()
    {
        // Create a ray from the camera position pointing forward
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        // Perform a raycast to check if the gaze intersects with any geometry
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            // SURFACE HIT MODE:
            // The gaze ray has hit a surface - position the reticle at the impact point

            // Set the reticle position to the exact point where the ray hit the surface
            currentReticleInstance.transform.position = hit.point;

            // Rotate the reticle to align with the surface normal.
            // This creates a "sticker" effect where the reticle appears flat against the surface.
            // The rotation is calculated by determining how to rotate the forward vector (Z-axis)
            // to align with the surface's outward-facing normal vector.
            currentReticleInstance.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);

            // Apply a small offset along the surface normal to prevent Z-fighting.
            // Z-fighting causes visual flickering when two surfaces occupy the same space.
            // This 2cm offset ensures the reticle renders cleanly above the surface.
            currentReticleInstance.transform.position += hit.normal * 0.02f;
        }
        else
        {
            // FLOATING MODE:
            // The gaze ray didn't hit anything - the player is looking at empty space (e.g., sky)

            // Position the reticle at a fixed distance ahead of the camera.
            // This ensures the player always sees a reticle, even when looking at empty areas.
            // The distance is controlled by the defaultDistance parameter.
            currentReticleInstance.transform.position = ray.origin + (ray.direction * defaultDistance);

            // Apply a billboard effect - rotate the reticle to face the camera.
            // This ensures the reticle always appears oriented toward the player,
            // maintaining optimal visibility regardless of the viewing angle.
            currentReticleInstance.transform.LookAt(cameraTransform);
        }
    }

    /// <summary>
    /// Unity OnDisable callback - called when the component is disabled or destroyed.
    /// 
    /// This cleanup method ensures the reticle is hidden when:
    /// - The component is disabled
    /// - The scene is being unloaded
    /// - The GameObject is being destroyed
    /// 
    /// This prevents orphaned reticle objects from remaining visible
    /// after scene transitions or component deactivation.
    /// </summary>
    void OnDisable()
    {
        // Hide the reticle instance when this component is disabled.
        // We use SetActive(false) instead of Destroy() to allow the reticle
        // to be shown again if the component is re-enabled later.
        if (currentReticleInstance != null)
            currentReticleInstance.SetActive(false);
    }
}