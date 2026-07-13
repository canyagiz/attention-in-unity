using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// MaterialAnalyzer is the central coordinator for the LLM-based object analysis system.
/// It orchestrates the pipeline of capturing visual data, analyzing it with AI services,
/// and presenting results to the user.
/// 
/// Analysis Pipeline:
/// 1. Receives material data and renderer from LookableObject (via AnalyzeMaterialWithVisual)
/// 2. Delegates visual analysis to an ILLMVisualAnalyzer (Gemini, OpenAI, etc.)
/// 3. Constructs a prompt combining physical properties and visual insights
/// 4. Sends the combined prompt to a text LLM (Groq/Llama) for final response
/// 5. Displays the response via LLMResponseSpawner
/// 6. Logs the interaction to the SQLite database
/// 
/// Two-Stage AI Analysis:
/// This system uses a two-stage approach for better results:
/// - Stage 1 (Visual): Specialized vision model describes what the object looks like
/// - Stage 2 (Reasoning): Text model combines visual description with physical data
/// 
/// This separation allows using the best model for each task and provides
/// more structured, informative responses than single-model approaches.
/// 
/// API Configuration:
/// - Uses Groq's API for the text LLM stage (fast inference)
/// - Visual analyzer (ILLMVisualAnalyzer) can be Gemini, OpenAI, etc.
/// - API keys configured via Inspector (should be moved to secure storage for production)
/// 
/// Usage:
/// - Place one instance in the scene
/// - Configure API key and endpoint
/// - Assign a visual analyzer component (VisualAnalyzerGemini or VisualAnalyzerOpenAI)
/// - LookableObjects will automatically find and use this analyzer
/// </summary>
public class MaterialAnalyzer : MonoBehaviour
{
    /// <summary>
    /// API key for the Groq LLM service.
    /// 
    /// Security Warning: Storing API keys in Inspector fields is convenient for development
    /// but insecure for production. Consider using:
    /// - Environment variables
    /// - Secure key management services
    /// - Server-side proxy to hide API keys
    /// </summary>
    [Header("Groq LLM Settings")]
    [Tooltip("API key for Groq service. Keep secure - don't commit to source control.")]
    public string apiKey;

    /// <summary>
    /// The Groq API endpoint URL for chat completions.
    /// 
    /// Default: Groq's OpenAI-compatible chat completions endpoint.
    /// Can be changed to point to different services that use the same API format.
    /// </summary>
    [Tooltip("Groq API endpoint. Default uses OpenAI-compatible chat completions format.")]
    public string endpoint = "https://api.groq.com/openai/v1/chat/completions";

    /// <summary>
    /// The LLM model to use for text generation.
    /// 
    /// Default: llama-3.3-70b-versatile (Llama 3.3 70B parameter model)
    /// - Excellent reasoning capabilities
    /// - Good at combining multiple pieces of information
    /// - Fast inference through Groq's optimized infrastructure
    /// 
    /// Alternatives on Groq: llama-3-8b, mixtral-8x7b, gemma-7b
    /// </summary>
    [Tooltip("Model name for Groq. Default: llama-3.3-70b-versatile")]
    public string model = "llama-3.3-70b-versatile";

    /// <summary>
    /// System prompt that defines the AI assistant's behavior and response format.
    /// 
    /// This prompt instructs the model to:
    /// - Act as a visual assistant for 3D scene understanding
    /// - Focus only on the centered object
    /// - Use physical properties and visual description together
    /// - Avoid technical jargon and environment description
    /// - Keep responses concise (under 150 words)
    /// </summary>
    [TextArea(5, 15)]
    [Tooltip("System prompt defining assistant behavior and response format.")]
    public string systemPrompt = @"You are a visual assistant helping users understand what a specific object in a 3D scene might be.

You will be provided:
- The object's physical properties (name, surface area, volume)
- A visual description based on a screenshot, including appearance and visible details

Your task:
- Focus only on the object that is directly in the center of the image
- Use its physical size, shape, packaging, and any visible text or design to guess what it might be
- Present your explanation as if you're describing it to a general user—not a developer

Do not describe the surrounding environment. Do not mention other products or shelf labels.  
Avoid quoting technical names. Focus only on what the centered object looks like and what it could be.

Limit your response to a single, clear paragraph under 150 words.
";

    /// <summary>
    /// Reference to the visual analyzer component.
    /// Must implement ILLMVisualAnalyzer interface.
    /// 
    /// This component handles Stage 1 of the analysis pipeline:
    /// - Capturing a screenshot of the target object
    /// - Sending it to a vision-capable LLM
    /// - Returning a text description of the visual appearance
    /// </summary>
    [Header("Visual Analyzer")]
    [Tooltip("Component implementing ILLMVisualAnalyzer for visual analysis (Gemini, OpenAI, etc.)")]
    public MonoBehaviour visualAnalyzerComponent;

    /// <summary>
    /// Cached interface reference to the visual analyzer.
    /// Populated during Awake by casting visualAnalyzerComponent.
    /// </summary>
    private ILLMVisualAnalyzer visualAnalyzer;

    /// <summary>
    /// Unity Awake callback - validates and caches the visual analyzer reference.
    /// 
    /// Uses Awake instead of Start to ensure the reference is available
    /// before other components try to use this analyzer.
    /// </summary>
    void Awake()
    {
        // Cast the MonoBehaviour to the interface type
        visualAnalyzer = visualAnalyzerComponent as ILLMVisualAnalyzer;
        
        if (visualAnalyzer == null)
        {
            Debug.LogError("Assigned visualAnalyzerComponent does not implement ILLMVisualAnalyzer");
        }
    }

    /// <summary>
    /// Main entry point for analyzing an object with visual AI.
    /// Called by LookableObject when the user has looked at an object long enough.
    /// 
    /// This method:
    /// 1. Validates that visual analyzer is available
    /// 2. Checks if a panel is already showing (prevents duplicate analysis)
    /// 3. Starts the visual analysis coroutine which chains to text analysis
    /// </summary>
    /// <param name="data">Material data containing object name, surface area, and volume</param>
    /// <param name="renderer">The renderer component for visual capture</param>
    public void AnalyzeMaterialWithVisual(MaterialData data, Renderer renderer)
    {
        // Validate visual analyzer is configured
        if (visualAnalyzer == null)
        {
            Debug.LogWarning("[MaterialAnalyzer] VisualAnalyzer is not assigned or invalid.");
            return;
        }

        // Prevent duplicate panel spawning.
        // If a panel is already showing, don't start another analysis.
        // This prevents overwhelming the user with multiple responses.
        var spawner = Object.FindObjectOfType<LLMResponseSpawner>();
        if (spawner != null && spawner.HasActivePanel())
        {
            Debug.Log("[LLM Analyzer] Panel already exists. Skipping LLM call.");
            return;
        }

        // Start the visual analysis pipeline.
        // The visual analyzer will capture a screenshot and analyze it.
        // When complete, it calls our callback with the visual insight string,
        // which then triggers the text LLM stage.
        StartCoroutine(visualAnalyzer.AnalyzeScene((visualInsight) => 
        {
            // This callback is invoked when visual analysis completes.
            // Now proceed to Stage 2: Text LLM analysis with combined data.
            AnalyzeMaterial(data, renderer, visualInsight);
        }));
    }

    /// <summary>
    /// Stage 2 of the analysis pipeline: Text LLM processing.
    /// Combines physical material data with visual insights and sends to text LLM.
    /// 
    /// Called as a callback from the visual analyzer when Stage 1 completes.
    /// </summary>
    /// <param name="data">Physical properties of the object</param>
    /// <param name="renderer">Renderer for spawning the response panel near the object</param>
    /// <param name="visualInsight">Description from the visual analysis stage</param>
    public void AnalyzeMaterial(MaterialData data, Renderer renderer, string visualInsight)
    {
        // Build the user prompt combining physical data and visual insight
        string userPrompt = BuildPrompt(data, visualInsight);
        Debug.Log("[User Prompt] " + userPrompt);
        
        // Send to the text LLM for final response generation
        StartCoroutine(SendToLLM(systemPrompt, userPrompt, data, visualInsight, renderer));
    }

    /// <summary>
    /// Constructs the user prompt by combining physical object data with visual insights.
    /// 
    /// The prompt format provides structured information to the LLM:
    /// - GameObject Data section with physical properties
    /// - Visual Insight section with the description from visual analysis
    /// </summary>
    /// <param name="data">Object's physical properties (name, area, volume)</param>
    /// <param name="visualInsight">Text description from visual analysis</param>
    /// <returns>Formatted prompt string combining both data sources</returns>
    private string BuildPrompt(MaterialData data, string visualInsight)
    {
        return $"GameObject Data:\n\n{data.ToString()}\n\nVisual Insight from Screenshot:\n{visualInsight}";
    }

    /// <summary>
    /// Sends the analysis request to the Groq LLM API and handles the response.
    /// 
    /// This coroutine:
    /// 1. Constructs the API request with system and user messages
    /// 2. Sends it to the Groq endpoint with proper headers
    /// 3. Parses the JSON response to extract the assistant's reply
    /// 4. Spawns a UI panel to display the response
    /// 5. Logs the interaction to the SQLite database
    /// </summary>
    /// <param name="systemPrompt">System message defining assistant behavior</param>
    /// <param name="userPrompt">User message with object data and visual insight</param>
    /// <param name="data">Original material data for logging</param>
    /// <param name="visualInsight">Visual insight string for logging</param>
    /// <param name="renderer">Renderer for positioning the response panel</param>
    private IEnumerator SendToLLM(string systemPrompt, string userPrompt, MaterialData data, string visualInsight, Renderer renderer = null)
    {
        // Construct the chat completion request in OpenAI-compatible format
        ChatRequest request = new ChatRequest
        {
            model = model,
            messages = new Message[]
            {
                new Message { role = "system", content = systemPrompt },
                new Message { role = "user", content = userPrompt }
            }
        };

        // Serialize the request to JSON
        string json = JsonUtility.ToJson(request);

        // Create and configure the HTTP request
        using (UnityWebRequest req = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();

            // Set required headers for the API
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // Send the request and wait for response
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                // Parse the JSON response
                string rawJson = req.downloadHandler.text;
                ChatResponse parsed = JsonUtility.FromJson<ChatResponse>(rawJson);

                if (parsed.choices != null && parsed.choices.Length > 0)
                {
                    // Extract the assistant's reply from the response
                    string reply = parsed.choices[0].message.content.Trim();

                    // Display the response in a UI panel near the object
                    LLMResponseSpawner spawner = Object.FindObjectOfType<LLMResponseSpawner>();
                    if (spawner != null)
                    {
                        spawner.ShowLLMResponse(reply, renderer);
                    }

                    // Log the interaction to the database for analysis
                    LLMLogEntry log = new LLMLogEntry
                    {
                        sessionId = SessionManager.GetSessionId(),
                        objectName = data.objectName,
                        area = data.surfaceArea,
                        volume = data.volume,
                        visualInsight = visualInsight,
                        llmReply = reply,
                        timestamp = System.DateTime.UtcNow.ToString("o")
                    };

                    LLMDatabase.InsertLog(log);
                    Debug.Log("[LLM Assistant Reply] " + reply);
                }
            }
            else
            {
                Debug.LogError("LLM request failed: " + req.error);
            }
        }
    }

    /// <summary>
    /// Data class for serializing chat completion requests.
    /// Follows the OpenAI/Groq API format.
    /// </summary>
    [System.Serializable]
    private class ChatRequest
    {
        /// <summary>Model name to use for completion</summary>
        public string model;
        /// <summary>Array of messages in the conversation</summary>
        public Message[] messages;
    }

    /// <summary>
    /// Data class representing a single message in the conversation.
    /// </summary>
    [System.Serializable]
    private class Message
    {
        /// <summary>Role of the message sender (system, user, or assistant)</summary>
        public string role;
        /// <summary>Content of the message</summary>
        public string content;
    }

    /// <summary>
    /// Data class for deserializing chat completion responses.
    /// </summary>
    [System.Serializable]
    private class ChatResponse
    {
        /// <summary>Array of completion choices (typically one)</summary>
        public Choice[] choices;
    }

    /// <summary>
    /// Data class representing a single completion choice.
    /// </summary>
    [System.Serializable]
    private class Choice
    {
        /// <summary>The assistant's message in this choice</summary>
        public Message message;
    }
}
