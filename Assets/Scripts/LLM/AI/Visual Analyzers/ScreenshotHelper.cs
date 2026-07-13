using System;
using System.IO;
using UnityEngine;

/// <summary>
/// ScreenshotHelper is a static utility class that captures isolated screenshots
/// of specific game objects for use in LLM visual analysis.
/// 
/// Key Features:
/// - Captures a single object in isolation (no background clutter)
/// - Automatically frames the object based on its bounding box
/// - Captures from the user's viewing direction for accurate representation
/// - Uses a dedicated layer to isolate the target object
/// - Saves screenshots to disk for debugging/verification
/// - Returns base64-encoded JPEG for direct API upload
/// 
/// Isolation Technique:
/// The helper uses Unity's layer system to capture only the target object:
/// 1. Temporarily moves the target to a "ScreenshotOnly" layer
/// 2. Creates a camera that only renders that layer
/// 3. Captures the screenshot
/// 4. Restores the object to its original layer
/// 
/// Camera Positioning:
/// The camera is automatically positioned based on:
/// - The object's bounding box size (determines distance)
/// - The incoming ray direction (determines angle)
/// - The configured field of view (determines framing)
/// 
/// Requirements:
/// - A layer named "ScreenshotOnly" must exist in Project Settings
/// - Target objects must have a Renderer component
/// 
/// Usage:
/// string base64 = ScreenshotHelper.CaptureObjectScreenshot(
///     targetObject, 
///     captureSettings, 
///     gazeDirection
/// );
/// </summary>
public static class ScreenshotHelper
{
    /// <summary>
    /// Configuration settings for screenshot capture.
    /// Allows customization of image dimensions, quality, and camera parameters.
    /// </summary>
    [Serializable]
    public class CaptureSettings
    {
        /// <summary>
        /// Width of the captured screenshot in pixels.
        /// Higher values provide more detail but increase API payload size.
        /// Default: 512px (good balance for most vision LLMs)
        /// </summary>
        [Tooltip("Screenshot width in pixels. Higher = more detail but larger file.")]
        public int width = 512;

        /// <summary>
        /// Height of the captured screenshot in pixels.
        /// Typically matches width for a square image.
        /// Default: 512px
        /// </summary>
        [Tooltip("Screenshot height in pixels. Higher = more detail but larger file.")]
        public int height = 512;

        /// <summary>
        /// JPEG compression quality (0-100).
        /// Higher values = better quality but larger file size.
        /// Default: 100 (maximum quality) for accurate visual analysis.
        /// 
        /// Recommendations:
        /// - 100: Best quality, ~50-150KB typical file size
        /// - 75: Good quality, ~20-40KB typical file size
        /// - 50: Acceptable quality, ~10-20KB typical file size
        /// </summary>
        [Tooltip("JPEG quality 0-100. Higher = better quality but larger file.")]
        [Range(0, 100)] 
        public int resolution = 100;

        /// <summary>
        /// Camera field of view in degrees.
        /// Controls how much of the object fills the frame.
        /// 
        /// Default: 40 degrees (moderate telephoto effect)
        /// - Lower values (20-30): Tighter framing, less distortion
        /// - Higher values (60-90): Wider view, more distortion
        /// </summary>
        [Tooltip("Camera FOV in degrees. Lower = tighter framing of object.")]
        public float fieldOfView = 40f;

        /// <summary>
        /// Background color for the screenshot.
        /// Should provide contrast with most objects for clear visibility.
        /// 
        /// Default: Medium gray (0.4, 0.4, 0.4)
        /// This neutral color:
        /// - Provides contrast with both light and dark objects
        /// - Doesn't distract from the object being analyzed
        /// - Works well with most vision LLMs
        /// </summary>
        [Tooltip("Background color. Should contrast with most objects.")]
        public Color backgroundColor = new Color(0.4f, 0.4f, 0.4f);
    }

    /// <summary>
    /// Captures an isolated screenshot of a LookableObject from a specified viewing angle.
    /// 
    /// This method performs the complete capture workflow:
    /// 1. Validates inputs (target object, renderer, layer)
    /// 2. Calculates optimal camera position based on object bounds and view direction
    /// 3. Creates a temporary camera with isolation settings
    /// 4. Temporarily changes object layer for isolated rendering
    /// 5. Renders the object to a texture
    /// 6. Encodes to JPEG and saves to disk for debugging
    /// 7. Returns base64-encoded image string for API upload
    /// 8. Cleans up temporary objects and restores original layer
    /// </summary>
    /// <param name="targetObject">
    /// The LookableObject to capture. Must have a Renderer component.
    /// </param>
    /// <param name="settings">
    /// Configuration for image dimensions, quality, and camera settings.
    /// </param>
    /// <param name="incomingRayDirection">
    /// The direction from which the user is viewing the object.
    /// The camera will be positioned opposite to this direction
    /// to capture what the user is actually seeing.
    /// </param>
    /// <returns>
    /// Base64-encoded JPEG string suitable for API upload,
    /// or null if capture failed (check debug logs for details).
    /// </returns>
    public static string CaptureObjectScreenshot(
        LookableObject targetObject,
        CaptureSettings settings,
        Vector3 incomingRayDirection
    )
    {
        // Validation: Ensure target object is assigned
        if (targetObject == null)
        {
            Debug.LogError("Target LookableObject not assigned.");
            return null;
        }

        // Validation: Ensure target has a renderer for bounds calculation
        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("Target LookableObject does not have a Renderer.");
            return null;
        }

        // Calculate the object's world-space bounding box
        // This is used to determine camera distance and position
        Bounds bounds = renderer.bounds;
        Vector3 center = bounds.center;
        float boundingRadius = bounds.extents.magnitude;

        // Calculate optimal camera distance based on object size and FOV.
        // The formula ensures the object fills the frame appropriately.
        // Distance = (object radius) / tan(FOV/2)
        // This positions the camera so the object subtends the expected angle.
        float fovRad = settings.fieldOfView * Mathf.Deg2Rad;
        float distance = boundingRadius / Mathf.Tan(fovRad / 2f);
        distance *= 1.1f; // Add 10% padding for comfortable framing

        // Calculate camera position.
        // The camera is positioned opposite to the incoming ray direction
        // (negated) so it captures what the user is actually looking at.
        Vector3 cameraDirection = -incomingRayDirection.normalized;
        Vector3 cameraPosition = center + cameraDirection * distance;

        // Create temporary camera for isolated rendering
        GameObject camObj = new GameObject("TempCaptureCamera");
        Camera tempCam = camObj.AddComponent<Camera>();
        tempCam.enabled = false; // We'll render manually
        tempCam.clearFlags = CameraClearFlags.Color;
        tempCam.backgroundColor = settings.backgroundColor;
        tempCam.fieldOfView = settings.fieldOfView;

        // Layer isolation setup.
        // We temporarily move the object to a dedicated layer that only
        // by the screenshot camera will be rendered.
        int originalLayer = targetObject.gameObject.layer;
        int screenshotLayer = LayerMask.NameToLayer("ScreenshotOnly");
        
        if (screenshotLayer == -1)
        {
            Debug.LogError("Layer 'ScreenshotOnly' does not exist. Please define it in Unity Tags and Layers.");
            UnityEngine.Object.Destroy(camObj);
            return null;
        }

        // Apply the screenshot layer to the object and all children
        ApplyLayerRecursively(targetObject.gameObject, screenshotLayer);
        
        // Configure camera to only see the screenshot layer
        tempCam.cullingMask = 1 << screenshotLayer;

        // Position and orient the camera to look at the object center
        camObj.transform.position = cameraPosition;
        camObj.transform.LookAt(center);

        // Create render texture and destination texture
        RenderTexture rt = new RenderTexture(settings.width, settings.height, 24);
        Texture2D tex = new Texture2D(settings.width, settings.height, TextureFormat.RGB24, false);

        // Render the scene to the texture
        tempCam.targetTexture = rt;
        tempCam.Render();

        // Read the rendered pixels into the Texture2D
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, settings.width, settings.height), 0, 0);
        tex.Apply();

        // Cleanup render state
        tempCam.targetTexture = null;
        RenderTexture.active = null;

        // Cleanup temporary objects
        UnityEngine.Object.Destroy(rt);
        UnityEngine.Object.Destroy(camObj);
        
        // Restore the object's original layer
        ApplyLayerRecursively(targetObject.gameObject, originalLayer);

        // Encode the texture to JPEG bytes
        byte[] jpgBytes = tex.EncodeToJPG(settings.resolution);

        // Additional cleanup (some redundant calls for safety)
        rt.Release();
        UnityEngine.Object.Destroy(rt);
        UnityEngine.Object.Destroy(camObj);
        ApplyLayerRecursively(targetObject.gameObject, originalLayer);
        UnityEngine.Object.Destroy(tex);

        // Save to disk for debugging and verification.
        // This allows manual inspection of what was sent to the LLM.
        string fileName = $"screenshot_{targetObject.name}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(filePath, jpgBytes);
        Debug.Log($"Screenshot saved for {targetObject.name} at {filePath}");
        
        // Return base64-encoded string for API upload
        return Convert.ToBase64String(jpgBytes);
    }

    /// <summary>
    /// Recursively applies a layer to a GameObject and all its descendants.
    /// 
    /// This ensures the entire object hierarchy (including child meshes,
    /// particle systems, etc.) is captured or excluded from the screenshot.
    /// </summary>
    /// <param name="obj">The root GameObject to start from</param>
    /// <param name="layer">The layer index to apply</param>
    private static void ApplyLayerRecursively(GameObject obj, int layer)
    {
        // Set the layer on this object
        obj.layer = layer;
        
        // Recursively apply to all children
        foreach (Transform child in obj.transform)
        {
            ApplyLayerRecursively(child.gameObject, layer);
        }
    }
}
