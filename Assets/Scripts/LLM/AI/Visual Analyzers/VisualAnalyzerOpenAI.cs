using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// VisualAnalyzerOpenAI implements ILLMVisualAnalyzer to perform visual analysis
/// using OpenAI's GPT-4 Vision (GPT-4o) model.
/// 
/// OpenAI's GPT-4o is a multimodal model that excels at understanding both text and images.
/// This implementation sends screenshots to the OpenAI Chat Completions API with
/// image attachments and receives detailed text descriptions.
/// 
/// API Features Used:
/// - Chat Completions endpoint (v1/chat/completions)
/// - Image input via base64 data URLs
/// - gpt-4o model for vision capabilities
/// 
/// Authentication:
/// Uses Bearer token authentication with the Authorization header.
/// 
/// Request Format:
/// Uses the standard Chat Completions format with the content field containing
/// an array of content objects (text and image_url types).
/// 
/// Image Format:
/// Images are passed as data URLs (data:image/jpeg;base64,...)
/// embedded in image_url content objects.
/// 
/// Token Limits:
/// The max_tokens parameter limits response length. Default is 300 tokens,
/// which is sufficient for a detailed paragraph description.
/// 
/// Usage:
/// 1. Attach to a GameObject
/// 2. Configure API key in Inspector
/// 3. Assign as the visual analyzer in MaterialAnalyzer
/// 4. The system will automatically use this for visual analysis
/// </summary>
public class VisualAnalyzerOpenAI : MonoBehaviour, ILLMVisualAnalyzer
{
    /// <summary>
    /// OpenAI API key for authentication.
    /// 
    /// Security Warning: API keys in Inspector fields are convenient for development
    /// but insecure for production builds. Consider using:
    /// - Environment variables
    /// - Secure key storage
    /// - Server-side proxy to hide keys from client builds
    /// </summary>
    [Header("OpenAI Settings")]
    [Tooltip("OpenAI API key. Keep secure - don't commit to source control.")]
    public string apiKey;

    /// <summary>
    /// The OpenAI Chat Completions API endpoint.
    /// 
    /// Default: https://api.openai.com/v1/chat/completions
    /// This is the standard endpoint for all chat-based models including GPT-4o.
    /// </summary>
    [Tooltip("OpenAI API endpoint. Default is the chat completions endpoint.")]
    public string endpoint = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// System prompt that instructs GPT-4o on how to analyze the image.
    /// 
    /// This prompt should guide the model to:
    /// - Understand the context (retail scene analysis)
    /// - Focus on the centered/target object
    /// - Provide useful descriptions for product identification
    /// - Format the response appropriately
    /// </summary>
    [TextArea(5, 10)]
    [Tooltip("Instructions for how GPT-4o should analyze the screenshot.")]
    public string systemPrompt = "You will be shown a screenshot from a 3D retail scene...\n\n1. Briefly describe the overall environment...\n2. Then, carefully analyze the object that is directly centered...";

    /// <summary>
    /// The currently targeted LookableObject for screenshot capture.
    /// Set via SetTargetObject() before calling AnalyzeScene().
    /// </summary>
    [Header("Capture Settings")]
    [Tooltip("The current target object for analysis. Set programmatically.")]
    public LookableObject targetObject;

    /// <summary>
    /// Configuration settings for the screenshot capture process.
    /// Controls image dimensions, quality, camera FOV, and background color.
    /// </summary>
    [Tooltip("Configuration for screenshot capture.")]
    public ScreenshotHelper.CaptureSettings captureSettings = new ScreenshotHelper.CaptureSettings();

    /// <summary>
    /// Returns the name of this visual analysis service for logging.
    /// </summary>
    public string ServiceName => "OpenAI";

    /// <summary>
    /// Sets the target LookableObject for the next screenshot capture.
    /// </summary>
    /// <param name="lookable">The object to capture and analyze</param>
    public void SetTargetObject(LookableObject lookable)
    {
        targetObject = lookable;
    }

    /// <summary>
    /// The viewing direction for positioning the screenshot camera.
    /// Determines the angle from which the object is captured.
    /// </summary>
    private Vector3 viewDirection = Vector3.zero;

    /// <summary>
    /// Sets the viewing direction for the next screenshot capture.
    /// The camera will be positioned to match this viewing angle.
    /// </summary>
    /// <param name="direction">Normalized direction from user's viewpoint</param>
    public void SetViewDirection(Vector3 direction)
    {
        viewDirection = direction.normalized;
    }

    /// <summary>
    /// Captures a screenshot of the target object and sends it to OpenAI for analysis.
    /// 
    /// This coroutine performs the following steps:
    /// 1. Validates that a target object is set
    /// 2. Waits for end of frame to ensure rendering is complete
    /// 3. Captures the screenshot using ScreenshotHelper
    /// 4. Constructs the OpenAI Chat Completions request with image
    /// 5. Sends the request and waits for response
    /// 6. Parses the response and invokes the callback with the result
    /// 
    /// The request uses the user role with multi-part content containing
    /// both the text prompt and the base64-encoded image.
    /// </summary>
    /// <param name="onComplete">
    /// Callback invoked when analysis completes.
    /// Receives either the model's description or an error message.
    /// </param>
    /// <returns>IEnumerator for coroutine execution</returns>
    public IEnumerator AnalyzeScene(Action<string> onComplete)
    {
        // Validate target object is assigned
        if (targetObject == null)
        {
            Debug.LogError("Target object not assigned.");
            onComplete?.Invoke("Target object not assigned.");
            yield break;
        }

        // Wait for end of frame to ensure rendering is complete
        yield return new WaitForEndOfFrame();

        // Capture screenshot and encode to base64
        string base64Image = ScreenshotHelper.CaptureObjectScreenshot(targetObject, captureSettings, viewDirection);

        // Validate screenshot capture succeeded
        if (string.IsNullOrEmpty(base64Image))
        {
            onComplete?.Invoke("Failed to capture screenshot.");
            yield break;
        }

        // Construct the data URL format required by OpenAI
        // Format: data:image/jpeg;base64,{base64_data}
        string imageUrl = $"data:image/jpeg;base64,{base64Image}";

        // Build the content array with text and image parts.
        // OpenAI expects an array of content objects with "type" discriminator.
        var contentList = new List<object>
        {
            // Text content: The analysis prompt
            new Dictionary<string, object> { { "type", "text" }, { "text", $"{systemPrompt}" } },
            // Image content: The captured screenshot as a data URL
            new Dictionary<string, object> { { "type", "image_url" }, { "image_url", new Dictionary<string, string> { { "url", imageUrl } } } }
        };

        // Build the message with user role (including both text and image)
        var message = new Dictionary<string, object> { { "role", "user" }, { "content", contentList } };

        // Construct the complete API request
        var requestData = new Dictionary<string, object>
        {
            { "model", "gpt-4o" },  // GPT-4o has vision capabilities
            { "messages", new List<object> { message } },
            { "max_tokens", 300 }  // Limit response length
        };

        // Serialize to JSON using Newtonsoft.Json
        string jsonBody = JsonConvert.SerializeObject(requestData);

        // Create and configure the HTTP request
        UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Set required headers for OpenAI API
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // Send request and wait for response
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                // Parse the OpenAI Chat Completions response format
                var response = JsonConvert.DeserializeObject<ChatResponse>(request.downloadHandler.text);
                
                // Extract the assistant's message content
                string reply = response?.choices?[0]?.message?.content?.Trim();
                
                onComplete?.Invoke(reply ?? "No description returned.");
            }
            catch (Exception ex)
            {
                Debug.LogError("JSON parse error: " + ex.Message);
                onComplete?.Invoke("Error parsing response.");
            }
        }
        else
        {
            Debug.LogError("OpenAI API error: " + request.error);
            onComplete?.Invoke("Request failed.");
        }
    }

    /// <summary>
    /// Response wrapper for OpenAI Chat Completions API responses.
    /// </summary>
    [Serializable]
    public class ChatResponse
    {
        /// <summary>Array of completion choices (typically one)</summary>
        public List<Choice> choices;
    }

    /// <summary>
    /// A single completion choice from the API response.
    /// </summary>
    [Serializable]
    public class Choice
    {
        /// <summary>The assistant's message in this choice</summary>
        public Message message;
    }

    /// <summary>
    /// A message from the chat completions response.
    /// </summary>
    [Serializable]
    public class Message
    {
        /// <summary>The role of this message (typically "assistant" for responses)</summary>
        public string role;
        /// <summary>The text content of the message</summary>
        public string content;
    }
}
