using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ILLMVisualAnalyzer is an interface for components that perform visual analysis
/// of game objects using vision-capable Large Language Models (LLMs).
/// 
/// This interface enables polymorphism for different visual analysis backends:
/// - VisualAnalyzerGemini (Google Gemini Vision)
/// - VisualAnalyzerOpenAI (OpenAI GPT-4 Vision)
/// - Future implementations (Claude, local models, etc.)
/// 
/// The interface abstracts the specific API details, allowing the MaterialAnalyzer
/// to work with any visual analysis service through a common contract.
/// 
/// Analysis Flow:
/// 1. SetTargetObject() is called when user starts looking at an object
/// 2. SetViewDirection() provides the viewing angle for screenshot positioning
/// 3. AnalyzeScene() captures a screenshot and sends it to the vision LLM
/// 4. The callback receives the text description of what the LLM sees
/// 
/// Implementation Requirements:
/// - Must capture a screenshot of the target object
/// - Must send the image to a vision-capable LLM API
/// - Must return a text description via the callback
/// - Should handle API errors gracefully with descriptive error messages
/// 
/// Usage:
/// ILLMVisualAnalyzer analyzer = GetComponent&lt;VisualAnalyzerGemini&gt;();
/// analyzer.SetTargetObject(lookableObject);
/// analyzer.SetViewDirection(gazeDirection);
/// StartCoroutine(analyzer.AnalyzeScene((description) => Debug.Log(description)));
/// </summary>
public interface ILLMVisualAnalyzer
{
    /// <summary>
    /// Gets the name of the visual analysis service.
    /// Used for logging and debugging to identify which service is being used.
    /// 
    /// Examples: "Gemini", "OpenAI", "Claude"
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Captures a screenshot of the current target object and analyzes it
    /// using a vision-capable LLM.
    /// 
    /// This is an asynchronous operation that:
    /// 1. Waits for end of frame (to ensure rendering is complete)
    /// 2. Captures a screenshot focused on the target object
    /// 3. Encodes the image and sends it to the LLM API
    /// 4. Parses the response and invokes the callback with the result
    /// 
    /// The callback receives a text description of what the LLM observes
    /// in the image, focusing on the centered/target object.
    /// </summary>
    /// <param name="onComplete">
    /// Callback invoked when analysis completes (or fails).
    /// The string parameter contains either:
    /// - Success: A description of what the LLM sees
    /// - Failure: An error message explaining what went wrong
    /// </param>
    /// <returns>
    /// An IEnumerator for use with StartCoroutine.
    /// The coroutine handles the asynchronous API call.
    /// </returns>
    IEnumerator AnalyzeScene(Action<string> onComplete);

    /// <summary>
    /// Sets the target LookableObject for the next analysis.
    /// 
    /// This should be called before AnalyzeScene() to specify which
    /// object the screenshot should focus on. The screenshot camera
    /// will be positioned to capture this object from an appropriate angle.
    /// 
    /// Passing null clears the target and may cause AnalyzeScene to fail
    /// or return an error message.
    /// </summary>
    /// <param name="lookable">
    /// The LookableObject to analyze, or null to clear the target.
    /// </param>
    void SetTargetObject(LookableObject lookable);

    /// <summary>
    /// Sets the viewing direction for screenshot capture.
    /// 
    /// This direction determines the camera angle for the screenshot.
    /// The screenshot camera will be positioned to capture the object
    /// from a direction that matches or approximates this view direction.
    /// 
    /// This ensures the screenshot captures what the user is actually looking at,
    /// rather than an arbitrary angle that might show a different side of the object.
    /// </summary>
    /// <param name="direction">
    /// The normalized direction vector from which the user is viewing the object.
    /// Typically this is the gaze ray direction or camera forward direction.
    /// </param>
    void SetViewDirection(Vector3 direction);
}