using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LookTracker is the central management component for the gaze/look tracking system.
/// It coordinates multiple look sources (eye tracking, controller pointing, crosshair, etc.)
/// using a priority-based system and tracks what objects the user is currently looking at.
/// 
/// This component acts as the bridge between input sources and the visual analysis system,
/// determining what the user is looking at and triggering analysis when appropriate.
/// 
/// Key Features:
/// - Supports multiple look sources with configurable priority ordering
/// - Priority-based input selection (first source that provides a valid ray wins)
/// - Integration with visual analysis system for object identification
/// - Runtime toggle for enabling/disabling look tracking
/// - Automatic focus reset when looking away from objects
/// 
/// Priority System:
/// The lookSourceBehaviours list determines priority. Sources are checked from top to bottom
/// (Element 0 = highest priority). The first source that returns a valid ray is used.
/// This allows configurations like: "Use eye tracking if available, else use controller pointing"
/// 
/// Usage:
/// - Attach to a manager GameObject in your scene
/// - Add look sources to the list in priority order (most preferred first)
/// - Assign a visual analyzer component for object analysis
/// - Configure the tracking range and toggle key as needed
/// </summary>
public class LookTracker : MonoBehaviour
{
    /// <summary>
    /// List of MonoBehaviour components that implement the ILookSource interface.
    /// These are checked in order from top to bottom (Element 0 = highest priority).
    /// 
    /// The priority system allows flexible configurations:
    /// - VR with eye tracking: [EyeTracker, ControllerPointer, HeadGaze]
    /// - VR controller only: [RightController, LeftController]
    /// - Desktop: [CrosshairLookSource]
    /// 
    /// Note: Unity's Inspector cannot display interface types directly, so we use
    /// MonoBehaviour as the serialized type and cast to ILookSource at runtime.
    /// Components that don't implement ILookSource will log a warning and be skipped.
    /// </summary>
    [Header("Look Sources (Priority Order: Top to Bottom)")]
    [Tooltip("Order matters! The topmost element (Element 0) has the highest priority. First valid ray wins.")]
    public List<MonoBehaviour> lookSourceBehaviours = new List<MonoBehaviour>();

    /// <summary>
    /// Internal list of validated ILookSource interfaces.
    /// Populated during Start() by casting from lookSourceBehaviours.
    /// This avoids repeated casting during Update() for better performance.
    /// </summary>
    private List<ILookSource> lookSources = new List<ILookSource>();

    /// <summary>
    /// Reference to the visual analyzer component for analyzing looked-at objects.
    /// This should implement ILLMVisualAnalyzer for LLM-based visual analysis.
    /// The analyzer is notified when the user starts looking at a new object.
    /// 
    /// Note: Uses MonoBehaviour for Inspector serialization, cast to interface at runtime.
    /// </summary>
    [Header("Analyzer")]
    [Tooltip("Component implementing ILLMVisualAnalyzer for processing visual analysis of looked-at objects.")]
    public MonoBehaviour visualAnalyzerBehaviour;

    /// <summary>
    /// Cached reference to the visual analyzer interface.
    /// Populated during Start() by casting visualAnalyzerBehaviour.
    /// </summary>
    private ILLMVisualAnalyzer analyzer;

    /// <summary>
    /// Reference to the object currently being looked at.
    /// Null if the user is not looking at any LookableObject.
    /// Used to track state changes and trigger appropriate callbacks.
    /// </summary>
    private LookableObject currentLookedObject;

    /// <summary>
    /// Flag to enable/disable the entire look tracking system at runtime.
    /// When disabled, no raycasts are performed and any current focus is reset.
    /// Can be toggled using the toggleKey keyboard shortcut.
    /// </summary>
    private bool isTrackingEnabled = true;

    /// <summary>
    /// Maximum distance for look raycasts.
    /// Objects beyond this distance will not be detected even if in the line of sight.
    /// 
    /// Default: 5 meters, suitable for typical retail/indoor VR experiences.
    /// Increase for large environments, decrease for focused close-range interactions.
    /// </summary>
    [Header("Range of track")]
    [Tooltip("Maximum raycast distance for detecting LookableObjects. Objects beyond this distance are ignored.")]
    public float rangeOfTrack = 5f;

    /// <summary>
    /// Keyboard key to toggle look tracking on/off during runtime.
    /// Useful for debugging or allowing users to temporarily disable the system.
    /// 
    /// Default: L key (easy to remember as "Look" toggle)
    /// </summary>
    [Header("Tracking Toggle Key")]
    [Tooltip("Press this key to toggle look tracking on/off at runtime.")]
    public KeyCode toggleKey = KeyCode.L;

    /// <summary>
    /// Unity Start callback - initializes the component by validating and caching references.
    /// 
    /// Performs the following setup:
    /// 1. Converts the MonoBehaviour list to ILookSource interfaces (with validation)
    /// 2. Casts the visual analyzer to its interface type
    /// 3. Logs errors for any configuration issues
    /// </summary>
    void Start()
    {
        // Step 1: Convert the Inspector-assigned MonoBehaviour list to ILookSource interfaces.
        // We iterate through each assigned component and validate that it implements ILookSource.
        // Valid sources are added to the internal list; invalid ones trigger a warning.
        foreach (var behaviour in lookSourceBehaviours)
        {
            // Use pattern matching to safely cast and check in one operation
            if (behaviour is ILookSource source)
            {
                lookSources.Add(source);
            }
            else if (behaviour != null)
            {
                // Component exists but doesn't implement the required interface
                // This is a configuration error that should be fixed by the developer
                Debug.LogWarning($"[LookTracker] '{behaviour.name}' assigned to list but does not implement ILookSource!");
            }
        }

        // Validate that at least one valid look source was found
        if (lookSources.Count == 0)
            Debug.LogError("[LookTracker] No valid ILookSource assigned!");

        // Step 2: Initialize the visual analyzer by casting to the interface type.
        // This allows the analyzer to be any component implementing ILLMVisualAnalyzer.
        analyzer = visualAnalyzerBehaviour as ILLMVisualAnalyzer;
        if (analyzer == null)
            Debug.LogError("Assigned Analyzer does not implement ILLMVisualAnalyzer!");
    }

    /// <summary>
    /// Unity Update callback - performs look tracking every frame.
    /// 
    /// This method:
    /// 1. Handles the toggle key for enabling/disabling tracking
    /// 2. Iterates through look sources by priority to find an active ray
    /// 3. Performs raycast to detect LookableObjects
    /// 4. Manages focus state (entering, continuing, leaving)
    /// </summary>
    void Update()
    {
        // Handle runtime toggle - check if the toggle key was pressed this frame.
        // This allows users or developers to quickly enable/disable look tracking.
        if (Input.GetKeyDown(toggleKey))
        {
            isTrackingEnabled = !isTrackingEnabled;
            Debug.Log("Look tracking is now " + (isTrackingEnabled ? "ENABLED" : "DISABLED"));
        }

        // Early exit if tracking is disabled or no look sources are configured.
        // Also reset any current focus to ensure clean state when re-enabled.
        if (!isTrackingEnabled || lookSources.Count == 0)
        {
            ResetCurrent();
            return;
        }

        // PRIORITY LOGIC: Iterate through look sources in priority order.
        // The first source that provides a valid ray is used; subsequent sources are ignored.
        // This enables configurations like "use eye tracking, fall back to controller if unavailable"
        Ray activeRay = default;
        bool isRayFound = false;

        foreach (var source in lookSources)
        {
            // TryGetLookRay returns true if this source has an active, valid ray.
            // For example, a controller source might only return true when trigger is pressed.
            if (source.TryGetLookRay(out Ray ray))
            {
                activeRay = ray;
                isRayFound = true;
                // Exit loop immediately - we found our priority ray, no need to check others
                break;
            }
        }

        // If a valid ray was found from any source, perform the raycast logic
        if (isRayFound)
        {
            HandleRaycast(activeRay);
        }
        else
        {
            // No look source is currently providing a ray.
            // This can happen when: hands are down, no trigger pressed, eyes closed, etc.
            // Reset the current focus to handle the "looking away" state.
            ResetCurrent();
        }
    }

    /// <summary>
    /// Performs raycast with the given ray and handles LookableObject detection.
    /// 
    /// This method encapsulates the raycast logic to avoid code duplication and improve readability.
    /// It handles three scenarios:
    /// 1. Raycast hits a new LookableObject (switch focus)
    /// 2. Raycast hits the same LookableObject (continue looking)
    /// 3. Raycast hits nothing or non-LookableObject (reset focus)
    /// </summary>
    /// <param name="ray">The look ray from the active look source</param>
    void HandleRaycast(Ray ray)
    {
        // Perform physics raycast within the configured tracking range
        if (Physics.Raycast(ray, out RaycastHit hit, rangeOfTrack))
        {
            // Check if the hit object has a LookableObject component
            LookableObject lookable = hit.collider.GetComponent<LookableObject>();
            if (lookable != null)
            {
                // Object is lookable - check if it's a new object or the same one
                if (lookable != currentLookedObject)
                {
                    // NEW OBJECT: User started looking at a different object
                    // Reset the previous object's focus state before switching
                    ResetCurrent();
                    currentLookedObject = lookable;

                    // Notify the analyzer about the new target object.
                    // The analyzer can then prepare to analyze this object if the user
                    // continues looking at it long enough.
                    analyzer.SetTargetObject(lookable);

                    // If the analyzer supports view direction (for positioned screenshots),
                    // provide the current look direction for accurate visual capture.
                    if (analyzer is ILLMVisualAnalyzer visualWithView)
                    {
                        visualWithView.SetViewDirection(ray.direction);
                    }
                }
                // CONTINUING TO LOOK: User is still looking at the same object
                // Call OnLook every frame to accumulate look time
                // This drives the "dwell time" mechanic in LookableObject
                currentLookedObject.OnLook(Time.deltaTime);
            }
            else
            {
                // Hit something that isn't a LookableObject (wall, floor, etc.)
                // Treat this as "not looking at anything interesting"
                ResetCurrent();
            }
        }
        else
        {
            // Raycast didn't hit anything at all (looking at empty space/sky)
            ResetCurrent();
        }
    }

    /// <summary>
    /// Resets the current focus state when the user stops looking at an object.
    /// 
    /// This method:
    /// 1. Notifies the previously-looked-at object that it's no longer being viewed
    /// 2. Clears the current object reference
    /// 3. Notifies the analyzer to clear its target
    /// 
    /// Safe to call multiple times or when currentLookedObject is already null.
    /// </summary>
    void ResetCurrent()
    {
        if (currentLookedObject != null)
        {
            // Notify the object that it's no longer being looked at.
            // This resets the object's internal look timer and state.
            currentLookedObject.ResetFocus();
            currentLookedObject = null;

            // Clear the analyzer's target using null-conditional operator
            // (safe even if analyzer is null)
            analyzer?.SetTargetObject(null);
        }
    }
}