using Newtonsoft.Json;
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

        bool hasAnimator = selectedObject.GetComponent<Animator>() != null;

        if (hasAnimator)
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
        ExportAssetBundle();
    }
    #endregion

    private void ExportAssetBundle()
    {
        if (prefabToExport == null)
        {
            Debug.LogError("[VivaSDK] No prefab selected!");
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
            Debug.LogError("[VivaSDK] AssetBundle build failed. Check console for details.");
            return;
        }

        // --- Json Building Below ---
        List<PhysicsBoneData> physicsBones = CollectPhysicsBoneData(prefabToExport);
        // TODO: Add Collider reference data

        // TODO: Create Thumbnail for export

        string configJson = CreateConfigurationJson(bundleName, physicsBones);

        // Where to export and the custom file format
        string packagePath = Path.Combine(exportFolder, bundleName + ".viva");

        using (FileStream fs = new(packagePath, FileMode.Create))
        using (BinaryWriter writer = new(fs))
        {
            // Write "VIVA" in HEX first
            writer.Write(0x56); // V
            writer.Write(0x49); // I
            writer.Write(0x56); // V
            writer.Write(0x41); // A

            // Write bundle name
            writer.Write(bundleName);

            // Write JSON config
            byte[] configBytes = System.Text.Encoding.UTF8.GetBytes(configJson);
            writer.Write(configBytes.Length); // Write length so it can be read back inside the game
            writer.Write(configBytes);

            // TODO: Write Thumbnail

            // Write bundle data
            string bundleFilePath = Path.Combine(tempExportFolder, bundleName);
            if (File.Exists(bundleFilePath))
            {
                byte[] bundleBytes = File.ReadAllBytes(bundleFilePath);
                writer.Write(bundleBytes.Length);
                writer.Write(bundleBytes);
            }
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

        Debug.Log($"[VivaSDK] Character exported to: {Path.GetFullPath(packagePath)}");
        EditorUtility.RevealInFinder(exportFolder);
    }

    #region Data Collection
    private List<PhysicsBoneData> CollectPhysicsBoneData(GameObject root)
    {
        var data = new List<PhysicsBoneData>();

        foreach (var bone in root.GetComponentsInChildren<PhysicsBone>(true))
        {
            if (bone == null) continue;

            PhysicsBoneData boneData = new()
            {
                boneName = bone.boneName,
                preset = bone.preset.ToString(),
                gravity = bone.gravity,
                damping = bone.damping,
                distanceCompression = bone.distanceCompression,
                stiffnessValue = bone.stiffnessValue,
                useStiffnessCurve = bone.useStiffnessCurve,
                stiffnessCurveStart = bone.stiffnessCurveStart,
                stiffnessCurveEnd = bone.stiffnessCurveEnd,
                velocityAttenuation = bone.velocityAttenuation,
                useLimit = bone.useLimit,
                speedLimit = bone.speedLimit
            };

            data.Add(boneData);
        }

        return data;
    }
    #endregion

    #region Json Creation
    private string CreateConfigurationJson(string bundleName, List<PhysicsBoneData> boneData)
    {
        var config = new
        {
            bundleName = bundleName,
            boneData = boneData,
            // TODO: Add more data
        };

        return JsonConvert.SerializeObject(config, Formatting.Indented);
    }
    #endregion

    #region Helper Methods
    private Bounds GetModelBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds();

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
    #endregion
}
