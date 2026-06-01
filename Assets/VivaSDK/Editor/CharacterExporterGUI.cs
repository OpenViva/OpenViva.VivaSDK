using UnityEditor;
using UnityEngine;

public static class CharacterExporterGUI
{
    public static void DrawExportWindow(CharacterExporter exporter)
    {
        GUILayout.Label("Character Exporter", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        exporter.prefabToExport = (GameObject)EditorGUILayout.ObjectField(
            "Character Prefab",
            exporter.prefabToExport, typeof(GameObject), true);

        EditorGUILayout.Space();

        // Bundle Name
        exporter.bundleName = EditorGUILayout.TextField("Name", exporter.bundleName);

        EditorGUILayout.Space();

        // Export Button
        GUI.enabled = exporter.prefabToExport != null && !string.IsNullOrEmpty(exporter.bundleName);
        if (GUILayout.Button("Export"))
        {
            exporter.OnExportClicked();
        }
        GUI.enabled = true;
    }
}
