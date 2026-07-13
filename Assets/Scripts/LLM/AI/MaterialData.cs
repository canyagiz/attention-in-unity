using UnityEngine;

/// <summary>
/// MaterialData is a serializable data class that holds physical properties
/// of a 3D object for use in LLM-based analysis.
/// 
/// This class extracts and stores:
/// - Object name (from GameObject name)
/// - Approximate surface area (calculated from bounding box)
/// - Approximate volume (calculated from bounding box)
/// 
/// These physical properties provide context for the LLM when analyzing
/// what an object might be. For example:
/// - A small box with 0.01 cubic meters might be a phone
/// - A medium box with 0.5 cubic meters might be a TV
/// - A large box with 2 cubic meters might be a refrigerator
/// 
/// Calculation Method:
/// Both surface area and volume are approximated using the object axis-aligned
/// bounding box (AABB). This provides rough estimates rather than precise values,
/// but is computationally efficient and works for any mesh.
/// 
/// Usage:
/// MaterialData data = MaterialData.FromRenderer(myRenderer);
/// string prompt = data.ToString();
/// </summary>
[System.Serializable]
public class MaterialData
{
    /// <summary>
    /// The name of the GameObject this data was extracted from.
    /// Used as an identifier and potential hint for the LLM analysis.
    /// </summary>
    public string objectName;

    /// <summary>
    /// Approximate surface area of the object in square meters.
    /// Calculated from the bounding box as the sum of all face areas.
    /// Formula: 2 * (width*height + width*depth + height*depth)
    /// </summary>
    public float surfaceArea;

    /// <summary>
    /// Approximate volume of the object in cubic meters.
    /// Calculated as the volume of the axis-aligned bounding box.
    /// Formula: width * height * depth
    /// </summary>
    public float volume;

    /// <summary>
    /// Factory method to create a MaterialData instance from a Renderer component.
    /// 
    /// Extracts the GameObject name and calculates physical properties
    /// from the renderer bounds (axis-aligned bounding box).
    /// </summary>
    /// <param name="renderer">
    /// The Renderer component to extract data from.
    /// Can be null, in which case placeholder values are used.
    /// </param>
    /// <returns>
    /// A new MaterialData instance populated with object properties,
    /// or placeholder values if the renderer is null.
    /// </returns>
    public static MaterialData FromRenderer(Renderer renderer)
    {
        var data = new MaterialData();

        if (renderer != null)
        {
            // Use the GameObject name as the object identifier
            data.objectName = renderer.gameObject.name;

            // Get the world-space bounding box from the renderer
            Bounds bounds = renderer.bounds;
            Vector3 size = bounds.size;

            // Calculate approximate surface area using the bounding box
            // This sums the areas of all six faces of a rectangular box
            data.surfaceArea = 2 * (size.x * size.y + size.x * size.z + size.y * size.z);

            // Calculate approximate volume as the bounding box volume
            data.volume = size.x * size.y * size.z;
        }
        else
        {
            // Handle the case where no renderer is provided
            data.objectName = "Unknown";
            data.surfaceArea = 0f;
            data.volume = 0f;
        }

        return data;
    }

    /// <summary>
    /// Converts the material data to a formatted string suitable for LLM prompts.
    /// 
    /// The output is formatted as a bulleted list for easy LLM parsing:
    /// - Object Name: [name]
    /// - Surface Area: [area] m squared
    /// - Volume: [volume] m cubed
    /// </summary>
    /// <returns>A formatted string representation of the material data.</returns>
    public override string ToString()
    {
        return
            $"- Object Name: {objectName}\n" +
            $"- Surface Area: {surfaceArea:F2} m squared\n" +
            $"- Volume: {volume:F3} m cubed";
    }
}
