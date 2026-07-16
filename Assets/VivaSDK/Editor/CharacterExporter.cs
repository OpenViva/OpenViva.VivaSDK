using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static VivaFormat;

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

        // Prepare export folders
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
        //CollectComponentFileReferences(prefabToExport, allDependencies, exportFolder);

        AssetBundleBuild build = new()
        {
            assetBundleName = bundleName,
            assetNames = allDependencies.ToArray(),
        };

        BuildPipeline.BuildAssetBundles(tempBuildPath, new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression, 
            EditorUserBuildSettings.activeBuildTarget);

        Debug.Log($"--- Build target: {EditorUserBuildSettings.activeBuildTarget}");

        string builtFilePath = Path.Combine(tempBuildPath, bundleName);
        string finalBundlePath = Path.Combine(exportFolder, bundleName + ".bundle");

        if (File.Exists(builtFilePath))
        {
            File.Copy(builtFilePath, finalBundlePath, true);
        }
        else
        {
            Debug.LogError("[Viva Exporter] Export failed: .bundle file not found.");
            return;
        }

        // Collect character data
        VivaCharacterData characterData = new()
        {
            Version = VivaFormat.CurrentVersion,
            CharacterName = bundleName,
            PrefabName = prefabToExport.name,
            ScriptCount = 0,
        };

        CollectCharacterData(prefabToExport, characterData);

        Debug.Log($"------------------ {characterData}");

        string json = JsonUtility.ToJson(characterData, true);
        byte[] characterDataBytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Create .viva file
        string vivaFilePath = Path.Combine(exportFolder, bundleName + ".viva");
        CreateVivaFile(vivaFilePath, finalBundlePath, characterDataBytes, characterData.ScriptCount);

        // Cleanup
        if (isSceneObject && File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        if (Directory.Exists(tempBuildPath))
        {
            Directory.Delete(tempBuildPath, true);
        }

        EditorUtility.RevealInFinder(exportFolder);
        Debug.Log($"[Character Exporter] Exported to: {Path.GetFullPath(vivaFilePath)}");
    }

    #region Data Collection
    private void CollectCharacterData(GameObject root, VivaCharacterData data)
    {
        if (root.TryGetComponent<VivaDescriptor>(out var descriptor))
        {
            data.Info = new CharacterInfo
            {
                Name = descriptor.Name,
                AuthorName = descriptor.AuthorName,
                Version = descriptor.Version,
                PersonalityType = descriptor.PersonalityType,
                VoicePack = descriptor.VoicePack,
                Description = descriptor.Description,
                Tags = descriptor.Tags,
                CustomColor = descriptor.CustomColor,
                Aux1 = descriptor.Aux1,
                Aux2 = descriptor.Aux2,
                Aux3 = descriptor.Aux3,
                AuxString1 = descriptor.AuxString1,
                AuxString2 = descriptor.AuxString2,
                AuxString3 = descriptor.AuxString3,
            };

            data.ScriptCount++;
        }
        else
        {
            Debug.LogWarning("[Character Exporter] No VivaDescriptor found on character!");
        }

        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component is PhysicsBone pb)
            {
                PhysicsBoneData boneData = new()
                {
                    GameObjectPath = GenerateGameObjectPath(component.gameObject, root),
                    BonePath = PhysicsBone.GetTransformPath(pb.boneTransform),
                    BoneName = pb.boneName,
                    Gravity = pb.gravity,
                    Damping = pb.damping,
                    DistanceCompression = pb.distanceCompression,
                    StiffnessValue = pb.stiffnessValue,
                    UseStiffnessCurve = pb.useStiffnessCurve,
                    StiffnessCurveStart = pb.stiffnessCurveStart,
                    StiffnessCurveEnd = pb.stiffnessCurveEnd,
                    VelocityAttenuation = pb.velocityAttenuation,
                    UseLimit = pb.useLimit,
                    SpeedLimit = pb.speedLimit,

                    // TODO: Add more variables
                };

                data.PhysicsBones.Add(boneData);
                data.ScriptCount++;
            }

            // TODO: Add more components here
        }
    }

    private string GenerateGameObjectPath(GameObject obj, GameObject root)
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
    #endregion

    private void CreateVivaFile(string vivaFilePath, string bundlePath, byte[] characterDataBytes, int scriptCount)
    {
        byte[] bundleData = File.ReadAllBytes(bundlePath);

        using var stream = new FileStream(vivaFilePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        var header = new VivaHeader
        {
            VivaKey = VivaFormat.VivaBytes,
            Version = VivaFormat.CurrentVersion,
            BundleSize = bundleData.Length,
            CharacterDataSize = characterDataBytes.Length,
            ScriptCount = scriptCount,
            Checksum = VivaFormat.CalculateChecksum(bundleData)
        };

        VivaFormat.WriteHeader(writer, header);

        // Write character data
        writer.Write(characterDataBytes);

        // Write bundle data
        writer.Write(bundleData);
    }
}