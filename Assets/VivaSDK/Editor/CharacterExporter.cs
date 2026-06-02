using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CharacterExporter : EditorWindow
{
    public GameObject prefabToExport;
    public string bundleName = "char";

    // List of scripts to include in export
    private static readonly List<string> scriptsToInclude = new()
    {
        "PhysicsBone",
        "ColliderReference",
        "CharacterInfo"
    };

    [MenuItem("VivaSDK/Export Character")]
    public static void ShowWindow()
    {
        GetWindow<CharacterExporter>("VivaSDK Exporter");
    }

    private void OnEnable()
    {
        AutoSelectSceneObject();
    }

    private void AutoSelectSceneObject()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            return;
        }

        bool hasAnimator = selectedObject.GetComponent<Animator>() != null;

        if (hasAnimator)
        {
            prefabToExport = selectedObject;
            bundleName = selectedObject.name;
        }
    }

    private void OnGUI()
    {
        CharacterExporterGUI.DrawExportWindow(this);
    }

    public void OnExportClicked()
    {
        ExportAssetBundle();
    }

    private void ExportAssetBundle()
    {
        if (prefabToExport == null)
        {
            Debug.LogError("[VivaSDK] No prefab selected!");
        }
        else
        {
            Debug.Log("[VivaSDK] Character Exported!"); // TODO: Check if file is exported properly instead
        }

        string exportFolder = "Assets/CharacterExports";
        if (!Directory.Exists(exportFolder))
        {
            Directory.CreateDirectory(exportFolder);
        }

        string tempExportFolder = "TempBundles";
        if (!Directory.Exists(tempExportFolder))
        {
            Directory.CreateDirectory(tempExportFolder);
        }

        string assetPath = AssetDatabase.GetAssetPath(prefabToExport);
        bool isSceneObject = string.IsNullOrEmpty(assetPath);

        if (isSceneObject)
        {
            string tempPrefabPath = "Assets/__TempExport.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefabToExport, tempPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            assetPath = tempPrefabPath;
        }

        // Get all dependencies
        var allDependencies = new HashSet<string>(
            AssetDatabase.GetDependencies(assetPath, true)
            .Where(path => !string.IsNullOrEmpty(path) && !path.EndsWith(".cs"))
        );

        // Include only specific scripts
        CollectSpecificScriptDependencies(prefabToExport, allDependencies);

        // Create the build configuration
        AssetBundleBuild build = new()
        {
            assetBundleName = bundleName,
            assetNames = allDependencies.ToArray()
        };

        // Build the AssetBundle
        BuildAssetBundlesParameters buildParams = new()
        {
            outputPath = tempExportFolder,
            options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            bundleDefinitions = new[] { build }
        };

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildParams);

        if (manifest == null)
        {
            Debug.LogError("[Exporter] AssetBundle build failed. Check console for details.");
            return;
        }

        // Move the bundle to the export folder
        string builtFilePath = Path.Combine(tempExportFolder, bundleName);
        string finalBundlePath = Path.Combine(exportFolder, bundleName + ".viva");

        if (File.Exists(builtFilePath))
        {
            File.Copy(builtFilePath, finalBundlePath, true);
            Debug.Log($"[VivaSDK] Exported to: {Path.GetFullPath(finalBundlePath)}");
        }
        else
        {
            Debug.LogError("[VivaSDK] Export failed: bundle not found.");
        }

        // Clean temp files
        if (isSceneObject && File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        if (Directory.Exists(tempExportFolder))
        {
            Directory.Delete(tempExportFolder, true);
        }

        EditorUtility.RevealInFinder(exportFolder);
    }

    private void CollectSpecificScriptDependencies(GameObject root, HashSet<string> deps)
    {
        foreach (var comp in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null) continue;

            string componentTypeName = comp.GetType().Name;

            if (scriptsToInclude.Contains(componentTypeName))
            {
                string scriptPath = FindScriptAssetPath(componentTypeName);

                if (!string.IsNullOrEmpty(scriptPath) && scriptPath.StartsWith("Assets"))
                {
                    deps.Add(scriptPath);
                    Debug.Log($"[Exporter] Included script: {componentTypeName} at {scriptPath}");
                }
            }
        }
    }

    private string FindScriptAssetPath(string typeName)
    {
        // Look for the script file in project
        string[] scriptFiles = AssetDatabase.FindAssets($"t:Script {typeName}");

        if (scriptFiles.Length > 0)
        {
            // Get the first match
            string assetPath = AssetDatabase.GUIDToAssetPath(scriptFiles[0]);
            return assetPath;
        }

        return null;
    }
}
