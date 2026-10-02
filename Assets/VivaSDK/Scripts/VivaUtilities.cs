using System.Collections.Generic;
using UnityEngine;

public class VivaUtilities : MonoBehaviour
{
    /// <summary>
    /// Generate GameObject path from the `root` to the given `object`.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="root"></param>
    /// <returns></returns>
    public static string GenerateGameObjectPath(GameObject obj, GameObject root)
    {
        if (obj == root) return root.name;

        List<string> parts = new();
        Transform current = obj.transform;

        while (current != null)
        {
            parts.Insert(0, current.name);
            if (current.gameObject == root) break;
            current = current.parent;
        }

        return string.Join("/", parts);
    }
}
