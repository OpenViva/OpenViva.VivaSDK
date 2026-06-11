using UnityEditor;
using UnityEngine;

public class CharacterExporterGUI : MonoBehaviour
{
    public static void DrawExportWindow(CharacterExporter exporter)
    {
        GUILayout.Label("Export Character (.viva + .bundle)", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Viva Format Usage\n" +
            "Exported characters will be turned into a pair of .viva and .bundle files containing script metadata and models.\n" +
            "Only whitelisted scripts will be exported!",
            MessageType.Info
        );

        EditorGUILayout.Space();

        exporter.prefabToExport = (GameObject)EditorGUILayout.ObjectField("Your Chracter", exporter.prefabToExport, typeof(GameObject), true);
        exporter.bundleName = EditorGUILayout.TextField("Character File Name", exporter.bundleName);

        GUI.enabled = exporter.prefabToExport != null && !string.IsNullOrEmpty(exporter.bundleName);
        if (GUILayout.Button("Export"))
        {
            exporter.OnExportClicked();
        }
        GUI.enabled = true;
    }
}
