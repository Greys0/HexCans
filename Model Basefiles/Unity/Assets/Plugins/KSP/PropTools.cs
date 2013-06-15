using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[AddComponentMenu("KSP/Prop Tools")]
[ExecuteInEditMode()]
public class PropTools : MonoBehaviour
{
    #region Proxy class, Prop class and other definitions

    [System.Serializable]
    public class Proxy
    {
        public Vector3 center;

        public Vector3 size;

        public Color color;

        public Proxy()
        {
            center = Vector3.zero;
            size = Vector3.zero;
            color = Color.white;
        }
    }

    [System.Serializable]
    public class Prop
    {
        public string directory;

        public string configName;

        public string propName;

        public List<Proxy> proxies;

        public Prop(string directory, string configName, string propName)
        {
            this.directory = directory;
            this.configName = configName;
            this.propName = propName;

            this.proxies = new List<Proxy>();
        }
    }

    private static char[] charTrim = new char[] { ' ', '\t', '\n', '\r' };

    private static char[] charDelimiters = new char[] { ' ', '\t', '=', ',', ';' };

    #endregion

    public string propRootDirectory = "";

    public List<Prop> props = new List<Prop>();

    public Vector2 propScrollPosition = new Vector2();

    public Rect lastRect = new Rect();



    public string filePath = "";

    public string filename = "PropConfig.cfg";


    public bool CreatePropInfoList(string propRoot)
    {
        props = new List<Prop>();

        DirectoryInfo dirInfo = new DirectoryInfo(propRoot);

        if (dirInfo == null)
        {
            Debug.Log("Cannot find prop root directory '" + propRoot + "'");
            return false;
        }

        foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
        {
            foreach (FileInfo file in subDir.GetFiles("*.cfg"))
            {
                Prop newProp = CreatePropInfo(file);

                if (newProp == null)
                    continue;

                props.Add(newProp);
            }
        }

        if (props.Count == 0)
        {
            Debug.Log("No valid props found in directory '" + propRoot + "'");
        }
        else
        {
            Debug.Log(props.Count + " props loaded successfully");
        }

        return true;
    }

    public Prop CreatePropInfo(FileInfo file)
    {
        string[] cfgData = File.ReadAllLines(file.FullName);

        if (cfgData == null || cfgData.Length == 0)
        {
            Debug.Log("Config data for prop '" + file.Directory.Name + "' is null or has zero length");
            return null;
        }

        Prop newProp = new Prop(file.Directory.Name, file.Name, file.Directory.Name);
        bool nameSet = false;

        foreach (string cfgLine in cfgData)
        {
            string cfg = cfgLine.Trim(charTrim).ToLower();

            if (!nameSet && cfg.StartsWith("name"))
            {
                string[] nameSplit = cfgLine.Split(charDelimiters, System.StringSplitOptions.RemoveEmptyEntries);
                if (nameSplit.Length == 2)
                {
                    newProp.propName = nameSplit[1];
                    nameSet = true;
                }
            }

            if (cfg.StartsWith("proxy"))
            {
                Proxy newProxy = CreateProxyInfo(file.Directory.Name, cfg);

                if (newProxy != null)
                    newProp.proxies.Add(newProxy);
            }
        }

        if (newProp.proxies.Count == 0)
        {
            Debug.Log("Config data for prop '" + file.Directory.Name + "' contains no usable proxies and is therefore invalid");
            return null;
        }

        return newProp;
    }

    public Proxy CreateProxyInfo(string propName, string proxyString)
    {
        string[] split = proxyString.Split(charDelimiters, System.StringSplitOptions.RemoveEmptyEntries);

        if (split == null || split.Length == 0 || split.Length < 7)
        {
            ProxyError(propName, proxyString);
            return null;
        }

        float centerX = 0f, centerY = 0f, centerZ = 0f;
        float sizeX = 0f, sizeY = 0f, sizeZ = 0f;
        float colorR = 1f, colorG = 1f, colorB = 1f;

        if (!float.TryParse(split[1], out centerX))
        {
            ProxyError(propName, proxyString);
            return null;
        }
        if (!float.TryParse(split[2], out centerY))
        {
            ProxyError(propName, proxyString);
            return null;
        }
        if (!float.TryParse(split[3], out centerZ))
        {
            ProxyError(propName, proxyString);
            return null;
        }

        if (!float.TryParse(split[4], out sizeX))
        {
            ProxyError(propName, proxyString);
            return null;
        }
        if (!float.TryParse(split[5], out sizeY))
        {
            ProxyError(propName, proxyString);
            return null;
        }
        if (!float.TryParse(split[6], out sizeZ))
        {
            ProxyError(propName, proxyString);
            return null;
        }

        // must have colour information also so try parse that
        if (split.Length == 10)
        {
            if (!float.TryParse(split[7], out colorR))
            {
                ProxyError(propName, proxyString);
                return null;
            }
            if (!float.TryParse(split[8], out colorG))
            {
                ProxyError(propName, proxyString);
                return null;
            }
            if (!float.TryParse(split[9], out colorB))
            {
                ProxyError(propName, proxyString);
                return null;
            }
        }

        Proxy newProxy = new Proxy();

        newProxy.center = new Vector3(centerX, centerY, centerZ);
        newProxy.size = new Vector3(sizeX, sizeY, sizeZ);
        newProxy.color = new Color(colorR, colorG, colorB);

        return newProxy;
    }

    private void ProxyError(string propName, string proxyString)
    {
        Debug.Log("Config data for prop '" + propName + "' contains an invalid proxy '" + proxyString + "'. Data must be in the format 'proxy = centerX, centerY, centerZ, sizeX, sizeY, sizeZ {, colorR, colorG, colorB }'");
    }


    //[ExecuteInEditMode()]
    //private void Update()
    //{
    //    Debug.Log("LOL");

    //    foreach (GameObject go in UnityEditor.Selection.gameObjects)
    //    {
    //        ProxyObject proxyTest = go.GetComponent<ProxyObject>();

    //        if (proxyTest != null)
    //        {
    //            UnityEditor.Selection.activeGameObject = proxyTest.prop.gameObject;
    //            return;
    //        }
    //    }
    //}
}