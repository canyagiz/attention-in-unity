using UnityEngine;

/// <summary>
/// LookableObject is a component that marks GameObjects as "lookable" targets
/// for the gaze/attention tracking system. It handles dwell-time detection and
/// triggers visual analysis when the user looks at an object long enough.
/// 
/// This component implements a "dwell time" interaction model where looking at
/// an object for a configured duration triggers an action (in this case, LLM analysis).
/// 
/// Key Features:
/// - Configurable dwell time threshold for activation
/// - Integration with MaterialAnalyzer for LLM-based object identification
/// - Automatic reset when user looks away
/// - One-shot activation (won't re-trigger until focus is reset)
/// 
/// How it works:
/// 1. LookTracker detects this object via raycast
/// 2. LookTracker calls OnLook() every frame while user is looking
/// 3. Internal timer accumulates look duration
/// 4. When threshold is exceeded, triggers material analysis
/// 5. When user looks away, LookTracker calls ResetFocus() to reset state
/// 
/// Usage:
/// - Attach this component to any GameObject you want to be "lookable"
/// - The object MUST have a Renderer component (enforced by RequireComponent)
/// - Configure the threshold time as needed (default: 3 seconds)
/// - Ensure a MaterialAnalyzer exists in the scene for analysis to work
/// </summary>
[RequireComponent(typeof(Renderer))]
public class LookableObject : MonoBehaviour
{
    /// <summary>
    /// Accumulated time the user has been continuously looking at this object.
    /// Measured in seconds, incremented by deltaTime each frame during OnLook().
    /// Resets to zero when ResetFocus() is called (user looks away).
    /// </summary>
    private float lookTimer = 0f;

    /// <summary>
    /// The minimum duration (in seconds) the user must look at this object
    /// before triggering the analysis action.
    /// 
    /// Default: 3 seconds, which provides a balance between:
    /// - Long enough to prevent accidental triggers from glancing
    /// - Short enough to feel responsive to intentional focus
    /// 
    /// Adjust based on your UX requirements:
    /// - Shorter (1-2s): More responsive but more accidental triggers
    /// - Longer (4-5s): More intentional but can feel sluggish
    /// </summary>
    [Tooltip("Duration in seconds user must look at this object to trigger analysis. Default: 3 seconds.")]
    public float threshold = 3f;

    /// <summary>
    /// Flag to ensure the analysis action only triggers once per focus session.
    /// Set to true after analysis is triggered, reset to false in ResetFocus().
    /// 
    /// This prevents the same object from being analyzed multiple times
    /// in a single continuous look - user must look away and back to re-trigger.
    /// </summary>
    private bool hasPrinted = false;

    /// <summary>
    /// Cached reference to the MaterialAnalyzer component in the scene.
    /// The analyzer performs LLM-based visual analysis of the looked-at object.
    /// Found automatically during Start() using FindObjectOfType.
    /// </summary>
    private MaterialAnalyzer analyzer;

    /// <summary>
    /// Unity Start callback - initializes the component by finding required references.
    /// 
    /// Automatically locates the MaterialAnalyzer in the scene.
    /// Logs a warning if not found, as analysis won't work without it.
    /// 
    /// Note: For larger scenes, consider assigning the analyzer directly
    /// via Inspector to avoid FindObjectOfType performance cost.
    /// </summary>
    void Start()
    {
        // Attempt to find the MaterialAnalyzer in the scene.
        // This component is responsible for capturing screenshots and
        // sending them to LLM services for visual analysis.
        analyzer = Object.FindObjectOfType<MaterialAnalyzer>();
        if (analyzer == null)
        {
            Debug.LogWarning("MaterialAnalyzer not found in the scene.");
        }
    }

    /// <summary>
    /// Called every frame by LookTracker while the user is looking at this object.
    /// Accumulates look time and triggers analysis when the threshold is reached.
    /// 
    /// The dwell-time pattern implemented here is a common UX technique for
    /// gaze-based interfaces, as it:
    /// - Filters out brief, unintentional glances
    /// - Provides implicit confirmation of user intent
    /// - Works well with both eye tracking and head gaze
    /// </summary>
    /// <param name="deltaTime">
    /// Time elapsed since last frame (typically Time.deltaTime).
    /// Passed from LookTracker to ensure consistent timing.
    /// </param>
    public void OnLook(float deltaTime)
    {
        // Early exit if already triggered or analyzer is missing.
        // hasPrinted prevents multiple triggers during the same look session.
        if (hasPrinted || analyzer == null) return;

        // Accumulate the time spent looking at this object
        lookTimer += deltaTime;

        // Check if the accumulated look time has exceeded the threshold
        if (lookTimer >= threshold)
        {
            // Log the look detection for debugging purposes
            Debug.Log($"[Look Detected] Object: {gameObject.name}");

            // Gather material/visual data from this object's renderer.
            // MaterialData extracts information like name, surface area, and volume
            // to provide context for the LLM analysis.
            Renderer rend = GetComponent<Renderer>();
            MaterialData matData = MaterialData.FromRenderer(rend);

            // Trigger the visual analysis pipeline.
            // This will:
            // 1. Capture a screenshot of this object
            // 2. Send it to a visual LLM (Gemini, OpenAI, etc.)
            // 3. Get a description of what the object appears to be
            // 4. Display the response in a UI panel
            analyzer.AnalyzeMaterialWithVisual(matData, rend);

            // Mark as triggered to prevent re-triggering during this focus session
            hasPrinted = true;
        }
    }

    /// <summary>
    /// Called by LookTracker when the user stops looking at this object.
    /// Resets all internal state to prepare for the next look interaction.
    /// 
    /// This enables the "look away and back" interaction pattern:
    /// - User looks at object → triggers analysis after threshold
    /// - User looks away → this method resets the object
    /// - User looks back → can trigger analysis again
    /// 
    /// Without this reset, each object could only be analyzed once per session.
    /// </summary>
    public void ResetFocus()
    {
        // Reset the look timer to zero - any previous look time is discarded
        lookTimer = 0f;

        // Reset the trigger flag to allow re-triggering on next look
        hasPrinted = false;
    }
}
