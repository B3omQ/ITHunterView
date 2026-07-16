using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ITHunterview.Domain.Entities.Cv;

public static class CvDocumentHelper
{
    /// <summary>
    /// Gets a field value from the CvDocument using a path (e.g. "Experience[0].Company" or "Summary")
    /// </summary>
    public static string? GetFieldByPath(CvDocument doc, string path)
    {
        var target = NavigatePath(doc, path, out var property, out var listIndex);
        if (target == null) return null;

        if (listIndex.HasValue)
        {
            if (target is IList list && listIndex.Value >= 0 && listIndex.Value < list.Count)
            {
                return list[listIndex.Value]?.ToString();
            }
            return null;
        }

        if (property == null) return null;
        return property.GetValue(target)?.ToString();
    }

    /// <summary>
    /// Sets a field value in the CvDocument using a path. Note: CvDocument uses init properties,
    /// so this reflection hack works if the backing field or setter allows it, but records with `init` 
    /// properties might require cloning (with expression). Since we are just modifying an object 
    /// in-memory before saving to JSON, we will use reflection to set the underlying property.
    /// In C# 9+ records, reflection CAN set `init` properties.
    /// </summary>
    public static void SetFieldByPath(CvDocument doc, string path, string value)
    {
        var target = NavigatePath(doc, path, out var property, out var listIndex);
        if (target == null) throw new ArgumentException($"Invalid path: {path}");

        if (listIndex.HasValue)
        {
            if (target is IList list && listIndex.Value >= 0 && listIndex.Value < list.Count)
            {
                list[listIndex.Value] = value;
            }
            else
            {
                throw new IndexOutOfRangeException($"Index {listIndex} is out of bounds for path {path}");
            }
        }
        else if (property != null)
        {
            property.SetValue(target, value);
        }
    }

    private static object? NavigatePath(object root, string path, out PropertyInfo? finalProperty, out int? listIndex)
    {
        finalProperty = null;
        listIndex = null;

        // Split path like "Experience[0].Bullets[2]" into ["Experience[0]", "Bullets[2]"]
        var parts = path.Split('.');
        object? current = root;

        var arrayRegex = new Regex(@"(\w+)\[(\d+)\]", RegexOptions.Compiled);

        for (int i = 0; i < parts.Length; i++)
        {
            if (current == null) return null;

            var part = parts[i];
            var match = arrayRegex.Match(part);
            string propName;
            int? index = null;

            if (match.Success)
            {
                propName = match.Groups[1].Value;
                index = int.Parse(match.Groups[2].Value);
            }
            else
            {
                propName = part;
            }

            // Case-insensitive property lookup
            var prop = current.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null && current is not IList) return null;

            if (i == parts.Length - 1)
            {
                // This is the target
                if (index.HasValue)
                {
                    // Target is an element inside a list
                    current = prop!.GetValue(current); // the list
                    finalProperty = null;
                    listIndex = index;
                    return current;
                }
                else
                {
                    finalProperty = prop;
                    listIndex = null;
                    return current;
                }
            }
            else
            {
                // Navigate deeper
                if (prop != null)
                {
                    current = prop.GetValue(current);
                    if (index.HasValue && current is IList list)
                    {
                        if (index.Value >= 0 && index.Value < list.Count)
                        {
                            current = list[index.Value];
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        return null;
    }
}
