using System;
using System.IO;
using UnityEngine;

/// <summary>
/// SessionManager provides static methods for managing session IDs across
/// application runs.
/// 
/// A "session" represents a single continuous play session or application run.
/// Session IDs are used to group related log entries in the database,
/// enabling analysis of user behavior within and across sessions.
/// 
/// Persistence:
/// The session counter is stored in a simple text file (session_counter.txt)
/// in Application.persistentDataPath. This ensures:
/// - Session IDs persist across application runs
/// - Each new session gets a unique, incrementing ID
/// - The state survives application updates
/// 
/// Thread Safety:
/// This implementation is NOT thread-safe. All calls should be made from
/// the main thread. For multi-threaded scenarios, add locking.
/// 
/// Typical Workflow:
/// 1. Application starts
/// 2. SessionInitializer (in Editor) or startup code calls GenerateNewSessionIfNeeded()
/// 3. Session ID is incremented and saved
/// 4. Throughout the session, GetSessionId() returns the cached ID
/// 5. All log entries use this ID to group together
/// 
/// Usage:
/// // Get current session ID (generates if needed)
/// int id = SessionManager.GetSessionId();
/// 
/// // Force new session (typically at app start)
/// SessionManager.GenerateNewSessionIfNeeded();
/// </summary>
public static class SessionManager
{
    /// <summary>
    /// Path to the session counter file.
    /// 
    /// Uses Application.persistentDataPath to ensure:
    /// - Writable on all platforms
    /// - Persists between application runs
    /// - Not deleted by cache cleaning
    /// 
    /// This is an expression-bodied property (=>) that recalculates
    /// the path each time, ensuring it adapts to runtime changes
    /// (though persistentDataPath rarely changes during runtime).
    /// </summary>
    private static string counterPath => Path.Combine(Application.persistentDataPath, "session_counter.txt");

    /// <summary>
    /// Cached current session ID.
    /// 
    /// -1 indicates the session ID has not been loaded yet.
    /// Once loaded or generated, this holds the current session's ID.
    /// 
    /// Caching prevents file I/O on every GetSessionId() call,
    /// which could be called many times per frame in some scenarios.
    /// </summary>
    private static int currentSessionId = -1;

    /// <summary>
    /// Gets the current session ID.
    /// 
    /// If the session ID hasn't been loaded yet, this method
    /// loads it from disk (or generates a new one if the file doesn't exist).
    /// 
    /// This method is idempotent for reads - calling it multiple times
    /// returns the same cached value.
    /// </summary>
    /// <returns>The current session ID (positive integer)</returns>
    public static int GetSessionId()
    {
        // Lazy initialization: load/generate ID on first access
        if (currentSessionId == -1)
        {
            LoadOrGenerateSessionId();
        }
        return currentSessionId;
    }

    /// <summary>
    /// Generates a new session ID if appropriate.
    /// 
    /// This should be called at application/play session start to ensure
    /// a fresh session ID is used. It:
    /// 1. Loads the current counter from disk
    /// 2. Increments it
    /// 3. Saves the new value to disk
    /// 4. Caches the new value for GetSessionId() calls
    /// 
    /// Safety Check:
    /// In the Unity Editor, this method prevents accidental session
    /// generation when not in Play Mode. This guards against the
    /// session being incremented during edit-time script execution.
    /// </summary>
    public static void GenerateNewSessionIfNeeded()
    {
        // Safety: Don't generate sessions outside of Play Mode in Editor
        // This prevents accidental counter increments during edit-time
        // script execution or domain reloads
        if (Application.isEditor && !Application.isPlaying)
            return;

        // Generate a new session ID (forceNew = true)
        LoadOrGenerateSessionId(true);
        
        Debug.Log($"[SessionManager] New session started: {currentSessionId}");
    }

    /// <summary>
    /// Loads the session counter from disk, optionally generating a new session.
    /// 
    /// File Format:
    /// The counter file contains a single integer as plain text.
    /// If the file doesn't exist or can't be parsed, starts at session 1.
    /// 
    /// Behavior:
    /// - If forceNew is false: Just loads and caches the current counter
    /// - If forceNew is true: Loads, increments, saves, and caches
    /// 
    /// Error Handling:
    /// If the file contains invalid data, the session counter resets to 1.
    /// This provides resilience against file corruption.
    /// </summary>
    /// <param name="forceNew">
    /// If true, increment the counter and save to disk (new session).
    /// If false, just load the existing counter.
    /// </param>
    private static void LoadOrGenerateSessionId(bool forceNew = false)
    {
        // Check if the counter file exists
        if (!File.Exists(counterPath))
        {
            // No file exists - this is the first session ever
            currentSessionId = 1;
        }
        else
        {
            // File exists - read and parse the counter
            string text = File.ReadAllText(counterPath);
            
            // Try to parse the text as an integer
            if (!int.TryParse(text, out currentSessionId))
            {
                // Parse failed (corrupted file?) - reset to 1
                currentSessionId = 1;
            }
        }

        // If forcing a new session, increment and save
        if (forceNew)
        {
            currentSessionId++;
            
            // Write the new counter to disk
            // This ensures the next application run will continue counting
            File.WriteAllText(counterPath, currentSessionId.ToString());
        }
    }
}
