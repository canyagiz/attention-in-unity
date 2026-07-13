using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// VisualAnalyzerGemini implements ILLMVisualAnalyzer to perform visual analysis
/// using Google's Gemini multimodal AI model.
/// 
/// Gemini is Google's flagship multimodal model that can understand both text and images.
/// This implementation sends screenshots to the Gemini API and receives text descriptions
/// of what the model sees in the image.
/// 
/// API Features Used:
/// - generateContent endpoint for multimodal requests
/// - Inline image data (base64 encoded JPEG)
/// - Text prompt for guiding the analysis
/// 
/// Authentication:
/// Uses the x-goog-api-key header for authentication (API key based).
/// 
/// Request Format:
/// The request includes both text (system prompt) and image data (base64 JPEG)
/// in the content parts array, allowing the model to analyze the image
/// according to the prompt instructions.
/// 
/// Usage:
/// 1. Attach to a GameObject
/// 2. Configure API key and endpoint in Inspector
/// 3. Assign as the visual analyzer in MaterialAnalyzer
/// 4. The system will automatically use this for visual analysis
/// 
/// Supported Models:
/// - gemini-1.5-pro (recommended for best accuracy)
/// - gemini-1.5-flash (faster, good accuracy)
/// - gemini-pro-vision (legacy, being deprecated)
/// </summary>
public class VisualAnalyzerGemini : MonoBehaviour, ILLMVisualAnalyzer
{
    /// <summary>
    /// API key for Google Gemini API authentication.
    /// 
    /// Security Warning: API keys in Inspector fields are convenient for development
    /// but insecure for production builds. Consider using:
    /// - Environment variables
    /// - Secure key storage services
    /// - Server-side proxy
    /// </summary>
    [Header("Gemini API Settings")]
    [Tooltip("Google API key for Gemini. Keep secure - don't commit to source control.")]
    public string apiKey;

    /// <summary>
    /// The Gemini API endpoint URL.
    /// 
    /// Format: https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent
    /// 
    /// Different models can be specified by changing the URL.
    /// Note: The API key is typically passed in the request header, not the URL.
    /// </summary>
    [Tooltip("Gemini API endpoint URL. Include model name in the URL.")]
    public string endpoint = "...";

    /// <summary>
    /// System prompt that instructs Gemini on how to analyze the image.
    /// 
    /// Should provide context about:
    /// - What type of scene/environment is being analyzed
    /// - What aspect of the image to focus on
    /// - What format the response should take
    /// - Any specific details to include or exclude
    /// </summary>
    [TextArea(5, 10)]
    [Tooltip("Instructions for how Gemini should analyze the screenshot.")]
    public string systemPrompt = "...";

    /// <summary>
    /// The currently targeted LookableObject for screenshot capture.
    /// Set via SetTargetObject() before calling AnalyzeScene().
    /// </summary>
    private LookableObject targetObject;

    /// <summary>
    /// Configuration settings for the screenshot capture process.
    /// Includes image dimensions, quality, FOV, and background color.
    /// </summary>
    [Header("Screenshot Settings")]
    [Tooltip("Configuration for screenshot capture (dimensions, quality, etc.)")]
    public ScreenshotHelper.CaptureSettings captureSettings = new ScreenshotHelper.CaptureSettings();

    /// <summary>
    /// Returns the name of this visual analysis service for logging purposes.
    /// </summary>
    public string ServiceName => "Gemini";

    /// <summary>
    /// The viewing direction for screenshot positioning.
    /// Determines the angle from which the object screenshot is taken.
    /// </summary>
    private Vector3 viewDirection = Vector3.zero;

    /// <summary>
    /// Sets the viewing direction for the next screenshot capture.
    /// The screenshot camera will be positioned to match this viewing angle.
    /// </summary>
    /// <param name="direction">Normalized direction vector from user's viewpoint</param>
    public void SetViewDirection(Vector3 direction)
    {
        viewDirection = direction.normalized;
    }

    /// <summary>
    /// Captures a screenshot of the target object and sends it to Gemini for analysis.
    /// 
    /// This coroutine performs the following steps:
    /// 1. Validates that a target object is set
    /// 2. Waits for end of frame to ensure rendering is complete
    /// 3. Captures the screenshot using ScreenshotHelper
    /// 4. Constructs the Gemini API request with image and prompt
    /// 5. Sends the request and waits for response
    /// 6. Parses the response and invokes the callback with the result
    /// </summary>
    /// <param name="onComplete">
    /// Callback invoked when analysis completes.
    /// Receives either the vision model's description or an error message.
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

        // Wait for end of frame to ensure all rendering is complete
        // This prevents capturing incomplete or outdated frames
        yield return new WaitForEndOfFrame();

        // Capture the screenshot and get base64-encoded image
        string base64Image = ScreenshotHelper.CaptureObjectScreenshot(
            targetObject, captureSettings, viewDirection
        );

        // Validate screenshot was captured successfully
        if (string.IsNullOrEmpty(base64Image))
        {
            onComplete?.Invoke("Failed to capture screenshot.");
            yield break;
        }

        // Construct the Gemini API request.
        // The request format uses "contents" array with "parts" containing
        // both text (the prompt) and inline image data.
        var requestData = new
        {
            contents = new[] {
                new {
                    parts = new object[] {
                        // Text part: The instruction/prompt for analysis
                        new { text = $"{systemPrompt}" },
                        // Image part: Base64-encoded JPEG with MIME type
                        new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                    }
                }
            }
        };

        // Serialize request to JSON using Newtonsoft.Json
        // (Unity's JsonUtility doesn't handle anonymous types well)
        string jsonBody = JsonConvert.SerializeObject(requestData);

        // Clean up the endpoint URL (remove any accidental whitespace)
        endpoint = endpoint.Trim();

        // Create and configure the HTTP request
        UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Set required headers
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-goog-api-key", apiKey);

        // Send request and wait for response
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                // Parse the Gemini response format
                var parsed = JsonConvert.DeserializeObject<GeminiResponse>(request.downloadHandler.text);
                
                // Extract the text content from the response structure
                // Response format: { candidates: [{ content: { parts: [{ text: "..." }] } }] }
                string reply = parsed?.candidates?[0]?.content?.parts?[0]?.text?.Trim();
                
                onComplete?.Invoke(reply ?? "No response.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse response: " + ex.Message);
                onComplete?.Invoke("Error parsing response.");
            }
        }
        else
        {
            Debug.LogError("Gemini API error: " + request.error);
            onComplete?.Invoke("Request failed.");
        }
    }

    /// <summary>
    /// Sets the target LookableObject for the next screenshot capture.
    /// </summary>
    /// <param name="lookable">The object to capture and analyze</param>
    public void SetTargetObject(LookableObject lookable)
    {
        targetObject = lookable;
    }

    /// <summary>
    /// Response wrapper for Gemini API responses.
    /// Contains an array of candidates (possible completions).
    /// </summary>
    [Serializable]
    public class GeminiResponse
    {
        /// <summary>Array of candidate responses (typically just one)</summary>
        public List<Candidate> candidates;
    }

    /// <summary>
    /// A single candidate response from Gemini.
    /// Contains the content (message) from the model.
    /// </summary>
    [Serializable]
    public class Candidate
    {
        /// <summary>The content of this candidate response</summary>
        public Content content;
    }

    /// <summary>
    /// Content wrapper containing the parts of a response.
    /// </summary>
    [Serializable]
    public class Content
    {
        /// <summary>Array of content parts (text, images, etc.)</summary>
        public List<Part> parts;
    }

    /// <summary>
    /// A single part of content (typically text for responses).
    /// </summary>
    [Serializable]
    public class Part
    {
        /// <summary>The text content of this part</summary>
        public string text;
    }
}
