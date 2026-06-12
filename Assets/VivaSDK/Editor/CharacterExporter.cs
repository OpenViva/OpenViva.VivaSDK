using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class CharacterExporter : EditorWindow
{
    public GameObject prefabToExport;
    public string bundleName = "char";

    [MenuItem("VivaSDK/Character Exporter")]
    public static void ShowWindow()
    {
        GetWindow<CharacterExporter>("VivaSDK Character Exporter");
    }

    #region Auto Selection
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

        bool hasDescriptor = selectedObject.GetComponent<VivaDescriptor>() != null;

        if (hasDescriptor)
        {
            prefabToExport = selectedObject;
            bundleName = selectedObject.name;
        }
    }
    #endregion

    #region GUI
    private void OnGUI()
    {
        CharacterExporterGUI.DrawExportWindow(this);
    }

    public void OnExportClicked()
    {
        ExportVivaCharacter();
    }
    #endregion

    private void ExportVivaCharacter()
    {
        if (prefabToExport == null)
        {
            Debug.LogError("[Viva Exporter] No object assigned.");
            return;
        }

        if (string.IsNullOrEmpty(bundleName))
        {
            Debug.LogError("[Viva Exporter] Model name cannot be empty.");
            return;
        }

        // Sanitize scripts before export
        if (!VivaScriptSanitizer.ValidateAllScripts(prefabToExport))
        {
            Debug.LogError("[Viva Exporter] Script sanitization failed. Export aborted.");
            return;
        }

        string exportFolder = "Assets/Character Exports";
        if (!Directory.Exists(exportFolder))
            Directory.CreateDirectory(exportFolder);

        string tempBuildPath = "TempBundleBuild";
        if (!Directory.Exists(tempBuildPath))
            Directory.CreateDirectory(tempBuildPath);

        // Find if the object is a prefab already
        string assetPath = AssetDatabase.GetAssetPath(prefabToExport);
        bool isSceneObject = string.IsNullOrEmpty(assetPath);

        if (isSceneObject)
        {
            string tempPrefabPath = "Assets/__TempPrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefabToExport, tempPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            assetPath = tempPrefabPath;
        }

        // Collect dependencies
        var allDependencies = new HashSet<string>(AssetDatabase.GetDependencies(assetPath, true)
            .Where(path => !string.IsNullOrEmpty(path) && !path.EndsWith(".cs")));

        // Collect component file references
        CollectComponentFileReferences(prefabToExport, allDependencies, exportFolder);

        AssetBundleBuild build = new()
        {
            assetBundleName = bundleName,
            assetNames = allDependencies.ToArray(),
        };

        BuildPipeline.BuildAssetBundles(tempBuildPath, new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

        string builtFilePath = Path.Combine(tempBuildPath, bundleName);
        string bundlePath = Path.Combine(exportFolder, bundleName + ".bundle");

        if (File.Exists(builtFilePath))
        {
            File.Copy(builtFilePath, bundlePath, true);
        }
        else
        {
            Debug.LogError("[Viva Exporter] Export failed: .bundle file not found.");
            return;
        }

        // Collect script metadata
        var metadata = new VivaMetadata();
        CollectScriptMetadata(prefabToExport, metadata);

        // Create .viva file
        string vivaFilePath = Path.Combine(exportFolder, bundleName + ".viva");
        CreateVivaFile(bundlePath, metadata, vivaFilePath);

        // Cleanup
        if (isSceneObject && File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        if (Directory.Exists(tempBuildPath)) Directory.Delete(tempBuildPath, true);

        EditorUtility.RevealInFinder(exportFolder);
        Debug.Log($"[Character Exporter] Exported to: {Path.GetFullPath(vivaFilePath)}");
    }

    #region Data Collection
    private void CollectComponentFileReferences(GameObject rootObject, HashSet<string> dependencies, string exportFolder)
    {
        foreach (Component component in rootObject.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;

            var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                if (!typeof(Object).IsAssignableFrom(field.FieldType)) continue;

                Object value = field.GetValue(component) as Object;
                if (value == null) continue;

                string fieldPath = AssetDatabase.GetAssetPath(value);
                if (!string.IsNullOrEmpty(fieldPath) && fieldPath.StartsWith("Assets"))
                {
                    dependencies.Add(fieldPath);
                }
            }
        }
    }

    private void CollectScriptMetadata(GameObject rootObject, VivaMetadata metadata)
    {
        foreach (var component in rootObject.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;

            // Skip components not in whitelist
            if (!VivaScriptSanitizer.IsScriptAllowed(component.GetType())) continue;

            var serializedData = SerializeComponent(component);
            if (serializedData != null)
            {
                metadata.AddScriptData(component.GetType().FullName, component.gameObject, serializedData);
            }
        }
    }
    #endregion

    private void CreateVivaFile(string bundlePath, VivaMetadata metadata, string vivaFilePath)
    {
        byte[] bundleData = File.ReadAllBytes(bundlePath);
        byte[] metadataData = metadata.Serialize();

        using var stream = new FileStream(vivaFilePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);
        VivaFormat.Header header = new()
        {
            VivaHeader = VivaFormat.VivaBytes,
            Version = VivaFormat.CurrentVersion,
            BundleSize = bundleData.Length,
            MetadataSize = metadataData.Length,
            ScriptCount = metadata.Scripts.Count,
            Checksum = VivaFormat.CalculateChecksum(bundleData)
        };

        VivaFormat.WriteHeader(writer, header);

        // Write bundle data
        writer.Write(bundleData);

        // Write metadata
        writer.Write(metadataData);
    }

    private byte[] SerializeComponent(Component component)
    {
        try
        {
            string json = JsonUtility.ToJson(component, true);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Viva Exporter] Failed to serialize component: {ex.Message}");
            return null;
        }
    }
}