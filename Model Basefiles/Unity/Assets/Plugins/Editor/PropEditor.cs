using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(PropObject))]
public class PropEditor : Editor
{
    public PropObject Target { get { return (PropObject)target; } }

    private static GUILayoutOption colLabel = GUILayout.Width(100);

    public override void OnInspectorGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Prop", colLabel);
        GUILayout.Label(Target.prop.propName);
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Snap Down"))
        {
            Snap();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Orient Down"))
        {
            Orient();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Proxy"))
        {
            Target.RefreshProxy();
        }
        GUILayout.EndHorizontal();

        if (EditorGUIUtility.systemCopyBuffer != null && EditorGUIUtility.systemCopyBuffer != "")
        {
            if (GUILayout.Button("Paste Transform"))
            {
                PasteTransform();
            }
        }
    }

    private void Snap()
    {
        Vector3 direction = -Target.transform.up;

        RaycastHit[] hits = Physics.RaycastAll(new Ray(Target.transform.position - (direction * 0.01f), direction), float.MaxValue);
        if (hits != null)
        {
            List<RaycastHit> hitList = new List<RaycastHit>(hits);
            hitList.Sort(delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
            foreach (RaycastHit h in hitList)
            {
                if (h.collider.gameObject == Target.gameObject)
                    continue;

                Target.transform.position = h.point;
                Debug.Log("Snapped to point " + h.point + " on collider " + h.collider.gameObject.name);
                break;
            }
        }
        else
        {
            Debug.Log("Unable to snap: Suitable collider not found");
        }
    }

    private void Orient()
    {
        Vector3 direction = -Target.transform.up;

        RaycastHit[] hits = Physics.RaycastAll(new Ray(Target.transform.position - (direction * 0.01f), direction), float.MaxValue);
        if (hits != null)
        {
            List<RaycastHit> hitList = new List<RaycastHit>(hits);
            hitList.Sort(delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
            foreach (RaycastHit h in hitList)
            {
                if (h.collider.gameObject == Target.gameObject)
                    continue;

                Target.transform.up = h.normal;
                Debug.Log("Orientated to normal " + h.normal + " on collider " + h.collider.gameObject.name);
                break;
            }
        }
        else
        {
            Debug.Log("Unable to snap: Suitable collider not found");
        }
    }

    private void PasteTransform()
    {
        if (EditorGUIUtility.systemCopyBuffer != null && EditorGUIUtility.systemCopyBuffer != "")
        {
            string[] lines = EditorGUIUtility.systemCopyBuffer.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 4)
                return;

            if (lines[0] != Target.prop.propName)
                return;

            string[] posLine = lines[1].Split(new char[] { ' ', '=' }, System.StringSplitOptions.RemoveEmptyEntries);
            string[] rotLine = lines[2].Split(new char[] { ' ', '=' }, System.StringSplitOptions.RemoveEmptyEntries);
            string[] scaLine = lines[3].Split(new char[] { ' ', '=' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (posLine.Length != 4 || rotLine.Length != 5 || scaLine.Length != 4)
                return;

            Vector3 pos = new Vector3(float.Parse(posLine[1]), float.Parse(posLine[2]), float.Parse(posLine[3]));
            Quaternion rot = new Quaternion(float.Parse(rotLine[1]), float.Parse(rotLine[2]), float.Parse(rotLine[3]), float.Parse(rotLine[4]));
            Vector3 sca = new Vector3(float.Parse(scaLine[1]), float.Parse(scaLine[2]), float.Parse(scaLine[3]));

            Target.transform.localPosition = pos;
            Target.transform.localRotation = rot;
            Target.transform.localScale = sca;
        }
    }
}