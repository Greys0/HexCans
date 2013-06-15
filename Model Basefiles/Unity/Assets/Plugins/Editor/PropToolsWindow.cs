using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class PropToolsWindow : EditorWindow
{
    [MenuItem("Window/KSP Prop Tools")]
    public static void ShowWindow()
    {
        window = (PropToolsWindow)EditorWindow.GetWindow(typeof(PropToolsWindow));
        window.title = "Prop Tools";
        window.minSize = new Vector2(300, 500);
        window.maxSize = new Vector2(300, 1500);
    }

    public static PropTools propTools;

    public static PropToolsWindow window;

    private static GUILayoutOption colLabel = GUILayout.Width(100);


    private void OnGUI()
    {
        CheckGUISetup();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Props Directory"))
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select KSP Props folder", propTools.propRootDirectory, "");

            if (folderPath != null && folderPath != "")
            {
                propTools.propRootDirectory = folderPath;
                propTools.CreatePropInfoList(folderPath);
            }
        }
        GUILayout.EndHorizontal();

        if (propTools.props.Count > 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("#Props", colLabel);
            GUILayout.Label(propTools.props.Count + "");
            if (GUILayout.Button("Refresh"))
            {
                propTools.CreatePropInfoList(propTools.propRootDirectory);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            DrawPropSelection();
        }
    }

    private void CheckGUISetup()
    {
        if (window == null)
        {
            window = this;
        }

        if (propTools == null)
        {
            propTools = (PropTools)FindObjectOfType(typeof(PropTools));

            if (propTools == null)
            {
                GameObject newObject = new GameObject();

                newObject.name = "PropTools";
                newObject.transform.position = Vector3.zero;
                newObject.transform.rotation = Quaternion.identity;
                newObject.transform.localScale = Vector3.one;

                propTools = newObject.AddComponent<PropTools>();
            }
        }
    }

    private void DrawPropSelection()
    {
        if (Event.current.type == EventType.Repaint)
            propTools.lastRect = GUILayoutUtility.GetLastRect();

        //Rect scrollRect = new Rect(5, propTools.lastRect.yMax + 2, window.position.width - 5, 200);
        //Rect scrollRect2 = new Rect(scrollRect.xMin, scrollRect.yMin, scrollRect.width - 30, scrollRect.yMin + (propTools.props.Count * (GUIStyle.none.lineHeight + 4)));

        //// Begin the ScrollView
        //propTools.propScrollPosition = GUI.BeginScrollView(scrollRect, propTools.propScrollPosition, scrollRect2, false, true);

        //foreach (PropTools.Prop p in propTools.props)
        //{
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Label(p.propName, GUILayout.Width(220));
        //    if (GUILayout.Button("Spawn", GUILayout.Width(50)))
        //    {
        //        PropObject newProp = PropObject.Create(propTools.transform, p);

        //        Selection.activeGameObject = newProp.gameObject;

        //        Debug.Log(p.propName + " created");
        //    }
        //    GUILayout.EndHorizontal();
        //}

        //// End the ScrollView
        //GUI.EndScrollView();

        foreach (PropTools.Prop p in propTools.props)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(p.propName, GUILayout.Width(220));
            if (GUILayout.Button("Spawn", GUILayout.Width(50)))
            {
                PropObject newProp = PropObject.Create(propTools.transform, p);

                Selection.activeGameObject = newProp.gameObject;

                Debug.Log(p.propName + " created");
            }
            GUILayout.EndHorizontal();
        }
    }
}
