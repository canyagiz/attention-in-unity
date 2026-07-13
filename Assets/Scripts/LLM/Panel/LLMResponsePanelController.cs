using TMPro;
using UnityEngine;

/// <summary>
/// LLMResponsePanelController manages a floating UI panel that displays
/// LLM-generated descriptions of looked-at objects.
/// 
/// This component handles:
/// - Displaying text content from LLM responses
/// - Automatic cleanup after a configurable lifetime
/// - Manual closing via public method (used by LLMPanelCloser)
/// - Proper cleanup notification to the spawner
/// 
/// Lifecycle:
/// 1. Spawned by LLMResponseSpawner when an LLM response is ready
/// 2. Positioned near the target object, facing the user
/// 3. Displays the response text via SetText()
/// 4. Auto-destroys after lifetime expires or user closes manually
/// 5. Notifies spawner to clear its reference (allows new panels)
/// 
/// UI Setup Requirements:
/// - Should have a Canvas component (World Space mode for VR)
/// - Should have a child TextMeshPro - UI component
/// - The prefab should be appropriately sized for VR reading distance
/// 
/// Usage:
/// Typically instantiated by LLMResponseSpawner, not manually.
/// var controller = panel.GetComponent&lt;LLMResponsePanelController&gt;();
/// controller.SetText("This appears to be a cereal box...");
/// </summary>
public class LLMResponsePanelController : MonoBehaviour
{
    /// <summary>
    /// Reference to the TextMeshPro component for displaying response text.
    /// 
    /// If not assigned in Inspector, auto-finds in children during Awake.
    /// Should be a TextMeshProUGUI component (Canvas-based TMP).
    /// </summary>
    [Header("UI References")]
    [Tooltip("TextMeshPro component for displaying the response. Auto-finds if not assigned.")]
    public TMP_Text responseText;

    /// <summary>
    /// How long (in seconds) before the panel auto-destroys.
    /// 
    /// Default: 20 seconds - gives users enough time to read
    /// a typical LLM response (150-200 words).
    /// 
    /// Set to a very high value (9999) to effectively disable auto-close.
    /// Users can still manually close using the grip button.
    /// </summary>
    [Header("Behavior Settings")]
    [Tooltip("Seconds before panel auto-closes. Default 20s for reading comfort.")]
    public float lifetime = 20f;

    /// <summary>
    /// Internal timer tracking how long the panel has existed.
    /// Incremented each frame in Update().
    /// </summary>
    private float timer;

    /// <summary>
    /// Sets the text content to display on the panel.
    /// 
    /// Called by LLMResponseSpawner after instantiating the panel.
    /// The text should be the final LLM response, already formatted.
    /// </summary>
    /// <param name="message">The text to display (LLM response content)</param>
    public void SetText(string message)
    {
        responseText.text = message;
    }

    /// <summary>
    /// Unity Awake callback - initializes the component.
    /// 
    /// Auto-finds the TextMeshPro component if not explicitly assigned.
    /// Uses GetComponentInChildren to search this object and all descendants.
    /// 
    /// Awake runs before Start, ensuring text is ready to be set
    /// immediately after instantiation.
    /// </summary>
    void Awake()
    {
        // Auto-find text component if not assigned in Inspector
        if (responseText == null)
        {
            responseText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Unity Update callback - handles lifetime timer and auto-close.
    /// 
    /// Increments the timer each frame and destroys the panel
    /// when the lifetime is exceeded.
    /// 
    /// This provides automatic cleanup to prevent panels from
    /// lingering indefinitely if users don't manually close them.
    /// </summary>
    private void Update()
    {
        // Accumulate time since panel creation
        timer += Time.deltaTime;
        
        // Check if lifetime has expired
        if (timer > lifetime)
        {
            DestroySelf();
        }
    }

    /// <summary>
    /// Public method to close the panel immediately.
    /// 
    /// Called by external systems like LLMPanelCloser when
    /// the user presses the grip button.
    /// 
    /// This allows manual closing before the auto-close timer expires.
    /// </summary>
    public void ClosePanel() 
    {
        DestroySelf();
    }

    /// <summary>
    /// Internal cleanup method that properly destroys the panel.
    /// 
    /// Before destroying, notifies the LLMResponseSpawner to clear
    /// its reference to this panel. This is critical because:
    /// - Spawner tracks if a panel exists to prevent duplicates
    /// - If we don't clear the reference, spawner thinks panel still exists
    /// - New panels wouldn't be spawned even after this one is destroyed
    /// 
    /// Uses FindObjectOfType to locate the spawner dynamically,
    /// which is acceptable since this only runs once per panel destruction.
    /// </summary>
    private void DestroySelf()
    {
        // Find the spawner to notify it that this panel is being destroyed
        var spawner = FindObjectOfType<LLMResponseSpawner>();
        
        if (spawner != null)
        {
            // Clear the spawner's reference so it allows new panels
            spawner.ClearPanelReference();
        }

        // Destroy this panel GameObject
        Destroy(gameObject);
    }
}
