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
            Debug.Log("[VivaSDK] Character Exported!");
        }
    }
}
