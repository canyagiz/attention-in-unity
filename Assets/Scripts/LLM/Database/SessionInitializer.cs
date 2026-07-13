#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// SessionInitializer is an Editor-only utility that automatically generates
/// a new session ID whenever Play Mode is entered in the Unity Editor.
/// 
/// This ensures that each time a developer tests the application, a new
/// session is created for logging purposes. Without this, consecutive
/// play sessions would share the same session ID, making it difficult
/// to differentiate between test runs.
/// 
/// How It Works:
/// - Uses [InitializeOnLoad] attribute to run static constructor at load time
/// - Static constructor subscribes to playModeStateChanged event
/// - When Play Mode is entered, a new session ID is generated
/// 
/// Editor-Only:
/// This class is wrapped in #if UNITY_EDITOR because:
/// - It uses UnityEditor namespace (not available in builds)
/// - Session initialization for builds is handled differently
/// - The InitializeOnLoad attribute only works in the editor
/// 
/// Build Behavior:
/// In production builds, session management should be handled by:
/// - Runtime initialization in a MonoBehaviour's Awake()
/// - Platform-specific startup hooks
/// - The SessionManager's own initialization logic
/// 
/// Placement:
/// As a static class with [InitializeOnLoad], this works regardless of
/// what scenes are loaded. It initializes when Unity loads the assemblies.
/// </summary>
[InitializeOnLoad]
public static class SessionInitializer
{
    /// <summary>
    /// Static constructor that runs when Unity loads this script.
    /// 
    /// Subscribes to the playModeStateChanged event to detect when
    /// the developer enters or exits Play Mode.
    /// 
    /// The [InitializeOnLoad] attribute ensures this constructor
    /// runs whenever:
    /// - Unity Editor starts
    /// - Scripts are recompiled
    /// - Domain is reloaded
    /// </summary>
    static SessionInitializer()
    {
        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    /// <summary>
    /// Callback invoked when Play Mode state changes.
    /// 
    /// Play Mode has four states:
    /// - EnteredEditMode: Returned to edit mode after playing
    /// - ExitingEditMode: About to enter play mode
    /// - EnteredPlayMode: Just started playing
    /// - ExitingPlayMode: About to stop playing
    /// 
    /// We only act on EnteredPlayMode to generate a new session
    /// at the start of each play session.
    /// </summary>
    /// <param name="state">The new Play Mode state</param>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Only act when entering Play Mode
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Generate a new session ID for this play session
            // This increments the session counter and persists it to disk
            SessionManager.GenerateNewSessionIfNeeded();
            
            Debug.Log("[SessionInitializer] New session started.");
        }
    }
}
#endif
