using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(PropTools))]
public class PropToolsEditor : Editor
{
    public PropTools Target { get { return (PropTools)target; } }

    private static GUILayoutOption colLabel = GUILayout.Width(100);

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Prop Tools"))
        {
            PropToolsWindow.ShowWindow();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("File Path", colLabel);
        Target.filePath = GUILayout.TextField(Target.filePath, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Set"))
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select path", Target.filePath, "");

            if (folderPath != null && folderPath != "")
            {
                Target.filePath = folderPath;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("File Name", colLabel);
        Target.filename = GUILayout.TextField(Target.filename);
        GUILayout.EndHorizontal();


        if (GUILayout.Button("Write Config"))
        {
            WritePropConfig();
        }

        if (GUILayout.Button("Refresh"))
        {
            RefreshAll();
        }

        // DrawDefaultInspector();
    }

    private void WritePropConfig()
    {
        if (Target.filePath == null || Target.filePath == "")
        {
            Debug.Log("Cannot write prop config: Target directory is invalid");
            return;
        }

        DirectoryInfo dirInfo = new DirectoryInfo(Target.filePath);
        if (dirInfo == null)
        {
            Debug.Log("Cannot write prop config: Target directory is invalid");
            return;
        }

        if (Target.filename == null || Target.filename == "")
        {
            Debug.Log("Cannot write prop config: Output filename is invalid");
            return;
        }

        PropObject[] props = Target.gameObject.GetComponentsInChildren<PropObject>();
        if (props.Length == 0)
        {
            Debug.Log("Cannot write prop config: No props found");
            return;
        }

        List<string> fileLines = new List<string>();
        fileLines.Add("// Sample prop config, not to be used on its own. Copy this into the Part config.");
        fileLines.Add("");
        fileLines.Add("INTERNAL");
        fileLines.Add("{");
        fileLines.Add("\tname = INSERT_INTERNAL_NAME");
        foreach(PropObject p in props)
        {
            fileLines.Add("\tPROP");
            fileLines.Add("\t{");
            fileLines.Add("\t\tname = " + p.prop.propName);
            fileLines.Add("\t\tposition = " + p.transform.localPosition.x + ", " + p.transform.localPosition.y + ", " + p.transform.localPosition.z);
            fileLines.Add("\t\trotation = " + p.transform.localRotation.x + ", " + p.transform.localRotation.y + ", " + p.transform.localRotation.z + ", " + p.transform.localRotation.w);
            fileLines.Add("\t\tscale = " + p.transform.localScale.x + ", " + p.transform.localScale.y + ", " + p.transform.localScale.z);
            fileLines.Add("\t}");
        }
        fileLines.Add("}");

        File.WriteAllLines(System.IO.Path.Combine(Target.filePath, Target.filename), fileLines.ToArray());
        Debug.Log("Prop config written");
    }

    private void RefreshAll()
    {
        PropObject[] objs = Target.GetComponentsInChildren<PropObject>();
        foreach (PropObject obj in objs)
        {
            obj.RefreshProxy();
        }
    }
}