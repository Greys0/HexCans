using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PropObject : MonoBehaviour
{
    public PropTools.Prop prop;

    public static PropObject Create(Transform parent, PropTools.Prop prop)
    {
        GameObject newPropGO = new GameObject();
        newPropGO.name = prop.propName;
        newPropGO.transform.parent = parent;
        newPropGO.transform.localPosition = Vector3.zero;
        newPropGO.transform.localRotation = Quaternion.identity;

        PropObject newProp = newPropGO.AddComponent<PropObject>();
        newProp.prop = prop;

        CreateProxies(newPropGO, prop);

        return newProp;
    }

    public static void CreateProxies(GameObject go, PropTools.Prop prop)
    {
        List<Mesh> proxyMeshes = new List<Mesh>();
        List<Material> proxyMaterials = new List<Material>();

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        MeshFilter cubeMF = cube.GetComponent<MeshFilter>();
        Mesh cubeMesh = cubeMF.sharedMesh;

        foreach (PropTools.Proxy proxy in prop.proxies)
        {
            Vector3[] verts = cubeMesh.vertices;

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i].Scale(proxy.size);
                verts[i] += proxy.center;
            }

            Mesh proxyMesh = new Mesh();
            proxyMesh.vertices = verts;
            proxyMesh.triangles = cubeMesh.triangles;
            proxyMesh.uv = cubeMesh.uv;
            proxyMesh.normals = cubeMesh.normals;


            Material proxyMat = new Material(Shader.Find("Diffuse"));
            proxyMat.color = proxy.color;
            proxyMat.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            proxyMeshes.Add(proxyMesh);
            proxyMaterials.Add(proxyMat);

        }


        CombineInstance[] cI = new CombineInstance[proxyMeshes.Count];
        for (int i = 0; i < proxyMeshes.Count; i++)
        {
            cI[i].mesh = proxyMeshes[i];
        }


        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(cI, false, false);
        combinedMesh.RecalculateBounds();

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
        mf.sharedMesh = combinedMesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
        mr.sharedMaterials = proxyMaterials.ToArray();

        BoxCollider collider = go.AddComponent<BoxCollider>();
        collider.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
        collider.center = combinedMesh.bounds.center;
        collider.size = combinedMesh.bounds.size;

        DestroyImmediate(cube);
        for (int i = 0; i < proxyMeshes.Count; i++)
        {
            DestroyImmediate(proxyMeshes[i]);
        }
    }

    private void RecreateProxies(PropTools.Prop newProp)
    {
        this.prop = newProp;

        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            DestroyImmediate(mr);
        }

        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        if (mf != null)
        {
            DestroyImmediate(mf);
        }

        BoxCollider bc = gameObject.GetComponent<BoxCollider>();
        if (bc != null)
        {
            DestroyImmediate(bc);
        }

        CreateProxies(gameObject, prop);
    }



    public void RefreshProxy()
    {
        PropTools pt = (PropTools)GetComponentInParent("PropTools", gameObject);

        if (!Directory.Exists(System.IO.Path.Combine(pt.propRootDirectory, System.IO.Path.Combine(prop.directory, "prop.cfg"))))
        {
            string[] dirSplit = prop.directory.Split('\\');
            prop.directory = dirSplit[dirSplit.Length - 1];
        }

        FileInfo propFile = null;
        if (prop.configName == null || prop.configName == "")
        {
            string propPath = System.IO.Path.Combine(pt.propRootDirectory, System.IO.Path.Combine(prop.directory, "prop.cfg"));
            propFile = new FileInfo(propPath);
            if (propFile == null)
            {
                Debug.LogError("Cannot find prop file at '" + propPath + "'");
                return;
            }
        }
        else
        {
            string propPath = System.IO.Path.Combine(pt.propRootDirectory, System.IO.Path.Combine(prop.directory, prop.configName));
            propFile = new FileInfo(propPath);
            if (propFile == null)
            {
                Debug.LogError("Cannot find prop file at '" + propPath + "'");
                return;
            }
        }

        PropTools.Prop propInfo = pt.CreatePropInfo(propFile);
        if (propInfo == null)
        {
            Debug.LogError("PropInfo is null");
            return;
        }

        RecreateProxies(propInfo);
    }

    public static Component GetComponentInParent(string type, GameObject obj)
    {
        if (obj.transform.parent != null)
        {
            Component c = obj.transform.parent.GetComponent(type);

            if (c != null)
            {
                return c;
            }
            else
            {
                return GetComponentInParent(type, obj.transform.parent.gameObject);
            }
        }
        else
        {
            return null;
        }
    }
}