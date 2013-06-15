using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

[CustomEditor(typeof(PartToolsLibrary))]
public class PartToolsLibraryEditor : Editor
{
    public PartToolsLibrary Target { get { return (PartToolsLibrary)target; } }

    public override void OnInspectorGUI()
    {
        DrawWriterGUI();

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        DrawDefaultInspector();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawWriterGUI()
    {
        if (GUILayout.Button("Compile List"))
        {
            CompileList();
        }

        Target.forceTextureFormat = GUILayout.Toggle(Target.forceTextureFormat, "Force Texture Format");
        if (Target.forceTextureFormat)
        {
            Target.textureFormat = (PartTools.TextureFormat)EditorGUILayout.EnumPopup("Texture Format", (System.Enum)Target.textureFormat);
        }

        if (GUILayout.Button("Write All"))
        {
            float startTime = Time.time;
            foreach (PartTools p in Target.partPrefabs)
            {
                Debug.Log("Writing to: " + p.filePath);
                PartToolsEditor.PartWriter.Write(p.modelName,
                p.filePath, p.filename, p.fileExt,
                p.transform,
                p.copyTexturesToOutputDirectory, p.convertTextures, p.autoRenameTextures, 
                Target.forceTextureFormat ? Target.textureFormat : p.textureFormat);
            }
            Debug.Log("Finished " + (Time.time - startTime) + "s");
        }
    }

    private void CompileList()
    {
        Target.partPrefabs.Clear();

        Object prefabObject = PrefabUtility.GetPrefabObject(target);
        string path = AssetDatabase.GetAssetPath(prefabObject);
        string dirPath = path.Substring(7, path.LastIndexOf('/') - 7);

        RecurseDirectory(dirPath);
    }

    private void RecurseDirectory(string unityPath)
    {
        DirectoryInfo dir = new DirectoryInfo(System.IO.Path.Combine(Application.dataPath, unityPath));

        Debug.Log("Compiling: " + unityPath);

        foreach (FileInfo f in dir.GetFiles("*.prefab"))
        {
            string prefabName = "Assets/" + unityPath + "/" + f.Name;

            GameObject o = (GameObject)AssetDatabase.LoadAssetAtPath(prefabName, typeof(GameObject));
            if (o == null)
                continue;

            PartTools pt = o.GetComponent<PartTools>();
            if (pt == null)
                continue;

            if (pt.production)
            {
                Debug.Log("Compiling: " + prefabName);
                Target.partPrefabs.Add(pt);
            }
            else
            {
                Debug.Log("Skipping: " + prefabName);
            }
        }

        foreach (DirectoryInfo d in dir.GetDirectories())
        {
            RecurseDirectory(unityPath + "/" + d.Name);
        }
    }
}