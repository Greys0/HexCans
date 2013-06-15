using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[AddComponentMenu("KSP/Part Tools Library")]
public class PartToolsLibrary : MonoBehaviour
{
    public List<PartTools> partPrefabs;

    public string loaderLevelName;

    public bool forceTextureFormat;

    public PartTools.TextureFormat textureFormat = PartTools.TextureFormat.TGA_Smallest;

    void Awake()
    {
        if (loaderLevelName != "")
        {
            DontDestroyOnLoad(gameObject);

            Application.LoadLevel(loaderLevelName);
        }
    }
}