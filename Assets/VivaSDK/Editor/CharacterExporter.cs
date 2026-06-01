using System.IO;
using UnityEditor;
using UnityEngine;

public class CharacterExporter : EditorWindow
{
    public GameObject prefabToExport;
    public string bundleName = "char";

    [MenuItem("VivaSDK/Export Character")]
    public static void ShowWindow()
    {
        GetWindow<CharacterExporter>("VivaSDK Exporter");
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
            string tempPrefabPath = "Assets/_TempExport.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefabToExport, tempPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            assetPath = tempPrefabPath;
        }

        // This properly configures the AssetBundle build
        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = bundleName,
        };

        // Use BuildAssetBundlesParameters for proper configuration
        BuildAssetBundlesParameters buildParams = new BuildAssetBundlesParameters
        {
            outputPath = tempExportFolder,
            options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            bundleDefinitions = new[] { build }
        };

        // Build the AssetBundle with proper parameters
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
}
