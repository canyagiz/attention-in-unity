using SQLite4Unity3d;
using System.IO;
using UnityEngine;

/// <summary>
/// LLMDatabase provides a static interface for logging LLM interactions to a local SQLite database.
/// 
/// This class manages database initialization and logging for research and analytics purposes.
/// Each interaction with the LLM system (object viewed, visual insight, response) is recorded
/// for later analysis.
/// 
/// Database Location:
/// The database file (llm_logs.db) is stored in Application.persistentDataPath,
/// which is a platform-specific writable location:
/// - Windows: C:/Users/{username}/AppData/LocalLow/{company}/{product}/
/// - Android: /data/data/{package}/files/
/// - iOS: /var/mobile/Containers/Data/Application/{guid}/Documents/
/// 
/// Thread Safety:
/// This implementation is NOT thread-safe. All calls should be made from the main thread.
/// For production use with async operations, consider adding locking.
/// 
/// Table Schema:
/// The LLMLogEntry table is automatically created/updated based on the class definition.
/// SQLite4Unity3d handles schema migration for simple cases.
/// 
/// Usage:
/// LLMDatabase.Initialize(); // Called automatically on first insert
/// LLMDatabase.InsertLog(logEntry);
/// </summary>
public static class LLMDatabase
{
    /// <summary>
    /// Static connection to the SQLite database.
    /// Initialized lazily on first use via Initialize() or InsertLog().
    /// Remains open for the lifetime of the application.
    /// </summary>
    private static SQLiteConnection db;

    /// <summary>
    /// Initializes the database connection and creates tables if needed.
    /// 
    /// This method is idempotent - calling it multiple times has no effect
    /// after the first successful initialization.
    /// 
    /// The database file is created in Application.persistentDataPath,
    /// which ensures it persists between application runs and is writable
    /// on all target platforms.
    /// 
    /// CreateTable is also idempotent - it will create the table if it
    /// doesn't exist, or do nothing if it already exists.
    /// </summary>
    public static void Initialize()
    {
        // Skip if already initialized
        if (db != null) return;

        // Construct the full path to the database file
        string dbPath = Path.Combine(Application.persistentDataPath, "llm_logs.db");
        
        // Open or create the database connection
        db = new SQLiteConnection(dbPath);

        // Create the log entries table if it doesn't exist
        // The table schema is derived from the LLMLogEntry class properties
        db.CreateTable<LLMLogEntry>();
        
        Debug.Log($"[LLMDatabase] Initialized at {dbPath}");
    }

    /// <summary>
    /// Inserts a new log entry into the database.
    /// 
    /// Automatically initializes the database if not already done.
    /// Each log entry records a complete LLM interaction including:
    /// - Session ID (for grouping related interactions)
    /// - Object name (what was looked at)
    /// - Physical properties (area, volume)
    /// - Visual insight (from vision model)
    /// - LLM reply (final response shown to user)
    /// - Timestamp
    /// 
    /// The entry's ID is automatically assigned by SQLite (auto-increment).
    /// </summary>
    /// <param name="entry">
    /// The log entry to insert. All fields except id should be populated.
    /// The id will be set by SQLite after insertion.
    /// </param>
    public static void InsertLog(LLMLogEntry entry)
    {
        // Ensure database is initialized before inserting
        if (db == null) Initialize();

        // Insert the entry into the table
        db.Insert(entry);
        
        Debug.Log($"[LLMDatabase] Inserted log for object: {entry.objectName}");
    }
}
