using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

[CustomEditor(typeof(PartTools))]
public class PartToolsEditor : Editor
{
    public PartTools Target { get { return (PartTools)target; } }

    private static GUILayoutOption colLabel = GUILayout.Width(100);

    public override void OnInspectorGUI()
    {
        DrawWriterGUI();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawWriterGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Production", colLabel);
        Target.production = GUILayout.Toggle(Target.production, "");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Part Name", colLabel);
        Target.modelName = GUILayout.TextField(Target.modelName);
        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();

        GUILayout.BeginHorizontal();
        GUILayout.Label("File Path", colLabel);
        Target.filePath = GUILayout.TextField(Target.filePath);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("File Name", colLabel);
        Target.filename = GUILayout.TextField(Target.filename);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("File Ext", colLabel);
        Target.fileExt = GUILayout.TextField(Target.fileExt);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Copy Textures", colLabel);
        Target.copyTexturesToOutputDirectory = GUILayout.Toggle(Target.copyTexturesToOutputDirectory, "");
        GUILayout.EndHorizontal();

        if (Target.copyTexturesToOutputDirectory)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Texture Format", colLabel);
            Target.textureFormat = (PartTools.TextureFormat)EditorGUILayout.EnumPopup(Target.textureFormat);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Convert Textures", colLabel);
            Target.convertTextures = GUILayout.Toggle(Target.convertTextures, "");
            GUILayout.EndHorizontal();

            if (!Target.convertTextures)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Rename Textures", colLabel);
                Target.autoRenameTextures = GUILayout.Toggle(Target.autoRenameTextures, "");
                GUILayout.EndHorizontal();
            }
        }

        if (GUILayout.Button("Write"))
        {
            PartWriter.Write(Target.modelName,
                Target.filePath, Target.filename, Target.fileExt,
                Target.transform,
                Target.copyTexturesToOutputDirectory, Target.convertTextures, Target.autoRenameTextures, Target.textureFormat);
        }
    }

    /// <summary>
    /// Contains the methods to write mu files
    /// </summary>
    public static class PartWriter
    {
        public static int fileVersion = 2;

        /// <summary>
        /// Writes a gameobject heirarchy to a mu file and can copy/rename textures
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <param name="target">Target gameobject to write</param>
        /// <param name="copyTexturesToTargetDir">Copy textures to target directory</param>
        /// <param name="renameTextures">Rename textures with respect to target name</param>
        public static void Write(string modelName, string filePath, string filename, string fileExtension, Transform target, bool copyTexturesToTargetDir, bool convertTextures, bool renameTextures, PartTools.TextureFormat textureFormat)
        {
            PartWriter.copyTexturesToTargetDir = copyTexturesToTargetDir;
            PartWriter.convertTextures = convertTextures;
            PartWriter.renameTextures = renameTextures;
            PartWriter.textureFormat = textureFormat;

            PartWriter.filePath = filePath;
            PartWriter.filename = filename;

            materials = new List<Material>();
            textures = new TextureDummyList();

            System.IO.FileInfo file = new System.IO.FileInfo(filePath);
            file.Directory.Create();

            BinaryWriter bw = new BinaryWriter(File.Open(Path.Combine(filePath, filename + fileExtension), FileMode.Create));

            // write header
            bw.Write((int)PartToolsLib.FileType.ModelBinary);
            bw.Write(fileVersion);
            bw.Write(modelName);

            try
            {
                WriteChild(bw, target);

                // write shared materials
                if (materials.Count > 0)
                {
                    bw.Write((int)PartToolsLib.EntryType.Materials);
                    bw.Write(materials.Count);
                    foreach (Material mat in materials)
                    {
                        WriteMaterial(bw, mat);
                        bw.Flush();
                    }

                    // write textures
                    if (textures.Count > 0)
                    {
                        WriteTextures(bw);
                        bw.Flush();
                    }
                }

            }
            catch (System.Exception ex)
            {
                Debug.LogError("File error: " + ex.Message + "\n" + ex.StackTrace + "\n");
            }

            Debug.Log(filePath + filename + fileExtension + " written.");
            bw.Close();

            materials = null;
            textures = null;
        }

        #region Write methods

        const string textureFileExtension = "mbm";

        static bool copyTexturesToTargetDir;
        static bool convertTextures;
        static bool renameTextures;
        static PartTools.TextureFormat textureFormat;

        static string filePath;
        static string filename;

        // materials array is used to cache all the shared materials before writing
        static List<Material> materials;

        public class TextureDummy
        {
            public Texture texture;

            public PartToolsLib.TextureType type;

            public TextureDummy(Texture texture, PartToolsLib.TextureType type)
            {
                this.texture = texture;
                this.type = type;
            }
        }

        public class TextureDummyList : List<TextureDummy>
        {
            public bool Contains(Texture tex)
            {
                foreach (TextureDummy dummy in textures)
                {
                    if (dummy.texture == tex)
                        return true;
                }
                return false;
            }

            public void Add(Texture tex, PartToolsLib.TextureType type)
            {
                if (!Contains(tex))
                {
                    Add(new TextureDummy(tex, type));
                }
            }

            public int IndexOf(Texture tex)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].texture == tex)
                        return i;
                }
                return -1;
            }
        }

        static TextureDummyList textures;


        static void WriteChild(BinaryWriter bw, Transform t)
        {
            WriteTransform(bw, t);

            WriteTagAndLayer(bw, t);
            WriteCollider(bw, t);
            WriteMeshFiler(bw, t);
            WriteMeshRenderer(bw, t);
            WriteSkinnedMeshRenderer(bw, t);
            WriteAnimation(bw, t);
            WriteLight(bw, t);
            WriteCamera(bw, t);

            foreach (Transform child in t)
            {
                if (child.GetComponent<PropCollider>() != null || child.GetComponent<PropObject>() != null)
                    continue;

                bw.Write((int)PartToolsLib.EntryType.ChildTransformStart);
                WriteChild(bw, child);
                bw.Write((int)PartToolsLib.EntryType.ChildTransformEnd);
            }

            bw.Flush();
        }


        static void WriteMeshFiler(BinaryWriter bw, Transform t)
        {
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf != null)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshFilter);
                WriteMesh(bw, mf.sharedMesh);
            }
        }

        static void WriteMeshRenderer(BinaryWriter bw, Transform t)
        {
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshRenderer);

                bw.Write(mr.castShadows);
                bw.Write(mr.receiveShadows);

                bw.Write(mr.sharedMaterials.Length);
                for (int i = 0; i < mr.sharedMaterials.Length; i++)
                {
                    if (!materials.Contains(mr.sharedMaterials[i]))
                        materials.Add(mr.sharedMaterials[i]);
                    bw.Write(materials.IndexOf(mr.sharedMaterials[i]));
                }
            }
        }

        static void WriteSkinnedMeshRenderer(BinaryWriter bw, Transform t)
        {
            SkinnedMeshRenderer smr = t.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                bw.Write((int)PartToolsLib.EntryType.SkinnedMeshRenderer);

                bw.Write(smr.sharedMaterials.Length);
                for (int i = 0; i < smr.sharedMaterials.Length; i++)
                {
                    if (!materials.Contains(smr.sharedMaterials[i]))
                        materials.Add(smr.sharedMaterials[i]);
                    bw.Write(materials.IndexOf(smr.sharedMaterials[i]));
                }

                bw.Write(smr.localBounds.center.x);
                bw.Write(smr.localBounds.center.y);
                bw.Write(smr.localBounds.center.z);
                bw.Write(smr.localBounds.size.x);
                bw.Write(smr.localBounds.size.y);
                bw.Write(smr.localBounds.size.z);

                bw.Write((int)smr.quality);

                bw.Write(smr.updateWhenOffscreen);

                int nBones = smr.bones.Length;
                bw.Write(nBones);
                for (int i = 0; i < nBones; i++)
                {
                    bw.Write(smr.bones[i].gameObject.name);
                }

                WriteMesh(bw, smr.sharedMesh);
            }
        }


        static void WriteMaterial(BinaryWriter bw, Material mat)
        {
            bw.Write(mat.name);
            Debug.Log(mat.shader.name);
            switch (mat.shader.name)
            {
                case "KSP/Specular":

                    bw.Write((int)PartToolsLib.ShaderType.Specular);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);

                    WriteColor(bw, mat.GetColor("_SpecColor"));
                    bw.Write(mat.GetFloat("_Shininess"));

                    break;

                case "KSP/Bumped":

                    bw.Write((int)PartToolsLib.ShaderType.Bumped);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);
                    WriteMaterialTexture(bw, mat, "_BumpMap", PartToolsLib.TextureType.NormalMap);

                    break;

                case "KSP/Bumped Specular":
                    bw.Write((int)PartToolsLib.ShaderType.BumpedSpecular);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);
                    WriteMaterialTexture(bw, mat, "_BumpMap", PartToolsLib.TextureType.NormalMap);

                    WriteColor(bw, mat.GetColor("_SpecColor"));
                    bw.Write(mat.GetFloat("_Shininess"));

                    break;

                case "KSP/Emissive/Diffuse":
                    bw.Write((int)PartToolsLib.ShaderType.Emissive);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);

                    WriteMaterialTexture(bw, mat, "_Emissive", PartToolsLib.TextureType.Texture);
                    WriteColor(bw, mat.GetColor("_EmissiveColor"));

                    break;

                case "KSP/Emissive/Specular":
                    bw.Write((int)PartToolsLib.ShaderType.EmissiveSpecular);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);

                    WriteColor(bw, mat.GetColor("_SpecColor"));
                    bw.Write(mat.GetFloat("_Shininess"));

                    WriteMaterialTexture(bw, mat, "_Emissive", PartToolsLib.TextureType.Texture);
                    WriteColor(bw, mat.GetColor("_EmissiveColor"));

                    break;

                case "KSP/Emissive/Bumped Specular":
                    bw.Write((int)PartToolsLib.ShaderType.EmissiveBumpedSpecular);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);
                    WriteMaterialTexture(bw, mat, "_BumpMap", PartToolsLib.TextureType.NormalMap);

                    WriteColor(bw, mat.GetColor("_SpecColor"));
                    bw.Write(mat.GetFloat("_Shininess"));

                    WriteMaterialTexture(bw, mat, "_Emissive", PartToolsLib.TextureType.Texture);
                    WriteColor(bw, mat.GetColor("_EmissiveColor"));

                    break;

                case "KSP/Alpha/Cutoff":
                    bw.Write((int)PartToolsLib.ShaderType.AlphaCutout);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);

                    bw.Write(mat.GetFloat("_Cutoff"));

                    break;

                case "KSP/Alpha/Cutoff Bumped":
                    bw.Write((int)PartToolsLib.ShaderType.AlphaCutoutBumped);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);
                    WriteMaterialTexture(bw, mat, "_BumpMap", PartToolsLib.TextureType.NormalMap);

                    bw.Write(mat.GetFloat("_Cutoff"));

                    break;

                case "KSP/Alpha/Translucent":
                    bw.Write((int)PartToolsLib.ShaderType.Alpha);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);

                    break;

                case "KSP/Alpha/Translucent Specular":
                    bw.Write((int)PartToolsLib.ShaderType.AlphaSpecular);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);

                    bw.Write(mat.GetFloat("_Gloss"));
                    WriteColor(bw, mat.GetColor("_SpecColor"));
                    bw.Write(mat.GetFloat("_Shininess"));

                    break;

                case "KSP/Alpha/Unlit Transparent":
                    bw.Write((int)PartToolsLib.ShaderType.AlphaUnlit);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);
                    WriteColor(bw, mat.GetColor("_Color"));

                    break;

                case "KSP/Unlit":
                    bw.Write((int)PartToolsLib.ShaderType.Unlit);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);
                    WriteColor(bw, mat.GetColor("_Color"));

                    break;

                case "KSP/Diffuse":
                default:
                    bw.Write((int)PartToolsLib.ShaderType.Diffuse);

                    WriteMaterialTexture(bw, mat, "_MainTex", PartToolsLib.TextureType.Texture);

                    break;
            }
        }

        static void WriteMaterialTexture(BinaryWriter bw, Material mat, string textureName, PartToolsLib.TextureType type)
        {
            AddTextureInstance(bw, mat, textureName, type);

            Vector2 tempV2 = mat.GetTextureScale(textureName);
            bw.Write(tempV2.x);
            bw.Write(tempV2.y);

            tempV2 = mat.GetTextureOffset(textureName);
            bw.Write(tempV2.x);
            bw.Write(tempV2.y);
        }

        static void AddTextureInstance(BinaryWriter bw, Material mat, string textureName, PartToolsLib.TextureType type)
        {
            Texture tex = mat.GetTexture(textureName);

            if (tex != null)
            {
                if (!textures.Contains(tex))
                    textures.Add(tex, type);

                bw.Write(textures.IndexOf(tex));
            }
            else
            {
                bw.Write(-1);
            }
        }

        static void WriteTextures(BinaryWriter bw)
        {
            bw.Write((int)PartToolsLib.EntryType.Textures);
            bw.Write(textures.Count);

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i] == null)
                {
                    Debug.LogError(i);

                    bw.Write(" ");
                    bw.Write((int)PartToolsLib.TextureType.Texture);
                }
                else
                {
                    string name = textures[i].texture.name;

                    if (copyTexturesToTargetDir)
                    {

                        string path = AssetDatabase.GetAssetPath(textures[i].texture);
                        string texExt = (path.Substring(path.LastIndexOf('.') + 1)).ToLower();

                        if (convertTextures)
                        {
                            name = WriteTexture(textures[i].texture, path, filePath, i);
                            //name = filename + i.ToString("D3") + "." + textureFileExtension;

                            //Debug.Log("Texture: '" + path + "' >> '" + name + "'");
                            //BitmapWriter.Write2D(textures[i].texture, filePath + name, textures[i].type);
                        }
                        else
                        {
                            if (renameTextures)
                                name = filename + i.ToString("D3") + "." + texExt;
                            else
                                name = name + "." + texExt;

                            Debug.Log("Texture: '" + path + "' >> '" + name + "'");
                            AssetDatabase.CopyAsset(path, filePath + name);

                            if (textures[i].type == PartToolsLib.TextureType.NormalMap)
                            {
                                // check if we need to create a normal map from the texture
                                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                                if (textureImporter.convertToNormalmap)
                                {
                                    Debug.Log("Converting '" + (filePath + name) + "' to a normal map");
                                    // we do. make it so
                                    ConvertNormalTexture(filePath + name, textureImporter.heightmapScale);
                                }
                            }
                        }
                    }

                    bw.Write(name);
                    bw.Write((int)textures[i].type);
                }
            }
        }

        static string WriteTexture(Texture texture, string path, string filePath, int index)
        {
            if (textureFormat == PartTools.TextureFormat.MBM)
            {
                string mbmName = filename + index.ToString("D3") + "." + "mbm";
                Debug.Log("Texture: '" + path + "' >> '" + mbmName + "'");
                BitmapWriter.Write2D(textures[index].texture, filePath + mbmName, textures[index].type);
                return mbmName;
            }
            else if (textureFormat == PartTools.TextureFormat.TGA_Compressed)
            {
                string tgaCompressedName = filename + index.ToString("D3") + "." + "tga";
                Debug.Log("Texture: '" + path + "' >> '" + tgaCompressedName + "'");
                TGAWriter.WriteImage(textures[index].texture, filePath + tgaCompressedName, textures[index].type, true);
                return tgaCompressedName;
            }
            else if (textureFormat == PartTools.TextureFormat.TGA_Uncompressed)
            {
                string tgaUncompressedName = filename + index.ToString("D3") + "." + "tga";
                Debug.Log("Texture: '" + path + "' >> '" + tgaUncompressedName + "'");
                TGAWriter.WriteImage(textures[index].texture, filePath + tgaUncompressedName, textures[index].type, false);
                return tgaUncompressedName;
            }
            else if (textureFormat == PartTools.TextureFormat.TGA_Smallest)
            {
                string tgaNameTemp00 = filename + index.ToString("D3") + "." + "tga_temp0";
                TGAWriter.WriteImage(textures[index].texture, filePath + tgaNameTemp00, textures[index].type, false);
                string tgaNameTemp01 = filename + index.ToString("D3") + "." + "tga_temp1";
                TGAWriter.WriteImage(textures[index].texture, filePath + tgaNameTemp01, textures[index].type, true);

                long tgaUncompressedLength0 = new FileInfo(filePath + tgaNameTemp00).Length;
                long tgaCompressedLength0 = new FileInfo(filePath + tgaNameTemp01).Length;

                string newName = "";

                if (tgaUncompressedLength0 <= tgaCompressedLength0)
                {
                    File.Delete(filePath + tgaNameTemp01);
                    newName = filename + index.ToString("D3") + "." + "tga";
                    if (File.Exists(filePath + newName))
                        File.Delete(filePath + newName);
                    File.Move(filePath + tgaNameTemp00, filePath + newName);
                    Debug.Log("Texture: '" + path + "' >> '" + newName + "'");
                }
                else
                {
                    File.Delete(filePath + tgaNameTemp00);
                    newName = filename + index.ToString("D3") + "." + "tga";
                    if (File.Exists(filePath + newName))
                        File.Delete(filePath + newName);
                    File.Move(filePath + tgaNameTemp01, filePath + newName);
                    Debug.Log("Texture: '" + path + "' >> '" + newName + "'");
                }
                return newName;
            }
            else if (textureFormat == PartTools.TextureFormat.PNG)
            {
                string pngName = filename + index.ToString("D3") + "." + "png";
                Debug.Log("Texture: '" + path + "' >> '" + pngName + "'");

                PNGWriter.WriteImage(texture, filePath + pngName, textures[index].type);
                return pngName;
            }
            else if (textureFormat == PartTools.TextureFormat.Smallest)
            {
                string mbmNameTemp = filename + index.ToString("D3") + "." + "mbm";
                BitmapWriter.Write2D(textures[index].texture, filePath + mbmNameTemp, textures[index].type);
                string tgaNameTemp0 = filename + index.ToString("D3") + "." + "tga_temp0";
                TGAWriter.WriteImage(textures[index].texture, filePath + tgaNameTemp0, textures[index].type, false);
                string tgaNameTemp1 = filename + index.ToString("D3") + "." + "tga_temp1";
                TGAWriter.WriteImage(textures[index].texture, filePath + tgaNameTemp1, textures[index].type, true);

                string pngName = filename + index.ToString("D3") + "." + "png";
                PNGWriter.WriteImage(texture, filePath + pngName, textures[index].type);

                long mbmLength = new FileInfo(filePath + mbmNameTemp).Length;
                long tgaUncompressedLength1 = new FileInfo(filePath + tgaNameTemp0).Length;
                long tgaCompressedLength1 = new FileInfo(filePath + tgaNameTemp1).Length;
                long pngLength = new FileInfo(filePath + pngName).Length;

                string newName0 = "";
                if (mbmLength <= tgaUncompressedLength1 && mbmLength <= tgaCompressedLength1 && mbmLength <= pngLength)
                {
                    File.Delete(filePath + tgaNameTemp0);
                    File.Delete(filePath + tgaNameTemp1);

                    File.Delete(filePath + pngName);

                    Debug.Log("Texture: '" + path + "' >> '" + mbmNameTemp + "'");
                    newName0 = mbmNameTemp;
                }
                else if (tgaUncompressedLength1 <= mbmLength && tgaUncompressedLength1 <= tgaCompressedLength1 && tgaUncompressedLength1 <= pngLength)
                {
                    File.Delete(filePath + mbmNameTemp);
                    File.Delete(filePath + tgaNameTemp1);
                    newName0 = filename + index.ToString("D3") + "." + "tga";
                    if (File.Exists(filePath + newName0))
                        File.Delete(filePath + newName0);
                    File.Move(filePath + tgaNameTemp0, filePath + newName0);

                    File.Delete(filePath + pngName);

                    Debug.Log("Texture: '" + path + "' >> '" + newName0 + "'");
                }
                else if (tgaCompressedLength1 <= mbmLength && tgaCompressedLength1 <= tgaUncompressedLength1 && tgaCompressedLength1 <= pngLength)
                {
                    File.Delete(filePath + mbmNameTemp);
                    File.Delete(filePath + tgaNameTemp0);
                    newName0 = filename + index.ToString("D3") + "." + "tga";
                    if (File.Exists(filePath + newName0))
                        File.Delete(filePath + newName0);
                    File.Move(filePath + tgaNameTemp1, filePath + newName0);

                    File.Delete(filePath + pngName);

                    Debug.Log("Texture: '" + path + "' >> '" + newName0 + "'");
                }
                else
                {
                    File.Delete(filePath + mbmNameTemp);
                    File.Delete(filePath + tgaNameTemp0);
                    File.Delete(filePath + tgaNameTemp1);

                    newName0 = pngName;
                    Debug.Log("Texture: '" + path + "' >> '" + pngName + "'");
                }
                return newName0;
            }

            return " ";
        }

        static void WriteCollider(BinaryWriter bw, Transform t)
        {
            MeshCollider mc = t.GetComponent<MeshCollider>();
            if (mc != null)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshCollider2);
                bw.Write(mc.isTrigger);
                bw.Write(mc.convex);
                WriteMesh(bw, mc.sharedMesh);
                return;
            }

            BoxCollider bc = t.GetComponent<BoxCollider>();
            if (bc != null)
            {
                bw.Write((int)PartToolsLib.EntryType.BoxCollider2);
                bw.Write(bc.isTrigger);
                bw.Write(bc.size.x);
                bw.Write(bc.size.y);
                bw.Write(bc.size.z);
                bw.Write(bc.center.x);
                bw.Write(bc.center.y);
                bw.Write(bc.center.z);
                return;
            }

            CapsuleCollider cc = t.GetComponent<CapsuleCollider>();
            if (cc != null)
            {
                bw.Write((int)PartToolsLib.EntryType.CapsuleCollider2);
                bw.Write(cc.isTrigger);
                bw.Write(cc.radius);
                bw.Write(cc.height);
                bw.Write(cc.direction);
                bw.Write(cc.center.x);
                bw.Write(cc.center.y);
                bw.Write(cc.center.z);
                return;
            }

            SphereCollider sc = t.GetComponent<SphereCollider>();
            if (sc != null)
            {
                bw.Write((int)PartToolsLib.EntryType.SphereCollider2);
                bw.Write(sc.isTrigger);
                bw.Write(sc.radius);
                bw.Write(sc.center.x);
                bw.Write(sc.center.y);
                bw.Write(sc.center.z);
                return;
            }

            WheelCollider wc = t.GetComponent<WheelCollider>();
            if (wc != null)
            {
                bw.Write((int)PartToolsLib.EntryType.WheelCollider);

                bw.Write(wc.mass);
                bw.Write(wc.radius);
                bw.Write(wc.suspensionDistance);

                bw.Write(wc.center.x);
                bw.Write(wc.center.y);
                bw.Write(wc.center.z);

                bw.Write(wc.suspensionSpring.spring);
                bw.Write(wc.suspensionSpring.damper);
                bw.Write(wc.suspensionSpring.targetPosition);

                bw.Write(wc.forwardFriction.extremumSlip);
                bw.Write(wc.forwardFriction.extremumValue);
                bw.Write(wc.forwardFriction.asymptoteSlip);
                bw.Write(wc.forwardFriction.asymptoteValue);
                bw.Write(wc.forwardFriction.stiffness);

                bw.Write(wc.sidewaysFriction.extremumSlip);
                bw.Write(wc.sidewaysFriction.extremumValue);
                bw.Write(wc.sidewaysFriction.asymptoteSlip);
                bw.Write(wc.sidewaysFriction.asymptoteValue);
                bw.Write(wc.sidewaysFriction.stiffness);
                return;
            }
        }

        static void WriteMesh(BinaryWriter bw, Mesh mesh)
        {
            int vCount = mesh.vertexCount;
            int smCount = mesh.subMeshCount;


            bw.Write((int)PartToolsLib.EntryType.MeshStart);
            bw.Write(vCount);
            bw.Write(smCount);


            Vector3[] verts = mesh.vertices;
            bw.Write((int)PartToolsLib.EntryType.MeshVerts);
            foreach (Vector3 v in verts)
            {
                bw.Write(v.x);
                bw.Write(v.y);
                bw.Write(v.z);
            }

            Vector2[] uvs = mesh.uv;
            if (uvs != null && uvs.Length == vCount)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshUV);
                foreach (Vector2 uv in uvs)
                {
                    bw.Write(uv.x);
                    bw.Write(uv.y);
                }
            }

            Vector2[] uv2s = mesh.uv2;
            if (uv2s != null && uv2s.Length == vCount)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshUV2);
                foreach (Vector2 uv in uv2s)
                {
                    bw.Write(uv.x);
                    bw.Write(uv.y);
                }
            }

            Vector3[] normals = mesh.normals;
            if (normals != null && normals.Length == vCount)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshNormals);
                foreach (Vector3 n in normals)
                {
                    bw.Write(n.x);
                    bw.Write(n.y);
                    bw.Write(n.z);
                }
            }

            Vector4[] tangents = mesh.tangents;
            if (tangents != null && tangents.Length == vCount)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshTangents);
                foreach (Vector4 ta in tangents)
                {
                    bw.Write(ta.x);
                    bw.Write(ta.y);
                    bw.Write(ta.z);
                    bw.Write(ta.w);
                }
            }

            BoneWeight[] weights = mesh.boneWeights;
            if (weights != null && weights.Length == vCount)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshBoneWeights);
                foreach (BoneWeight w in weights)
                {
                    bw.Write(w.boneIndex0);
                    bw.Write(w.weight0);
                    bw.Write(w.boneIndex1);
                    bw.Write(w.weight1);
                    bw.Write(w.boneIndex2);
                    bw.Write(w.weight2);
                    bw.Write(w.boneIndex3);
                    bw.Write(w.weight3);
                }
            }

            Matrix4x4[] bindposes = mesh.bindposes;
            if (bindposes != null && bindposes.Length > 0)
            {
                bw.Write((int)PartToolsLib.EntryType.MeshBindPoses);
                bw.Write(bindposes.Length);
                foreach (Matrix4x4 m in bindposes)
                {
                    bw.Write(m.m00);
                    bw.Write(m.m01);
                    bw.Write(m.m02);
                    bw.Write(m.m03);

                    bw.Write(m.m10);
                    bw.Write(m.m11);
                    bw.Write(m.m12);
                    bw.Write(m.m13);

                    bw.Write(m.m20);
                    bw.Write(m.m21);
                    bw.Write(m.m22);
                    bw.Write(m.m23);

                    bw.Write(m.m30);
                    bw.Write(m.m31);
                    bw.Write(m.m32);
                    bw.Write(m.m33);
                }
            }

            int[] tri;
            for (int i = 0; i < smCount; i++)
            {
                tri = mesh.GetTriangles(i);

                bw.Write((int)PartToolsLib.EntryType.MeshTriangles);
                bw.Write(tri.Length);

                foreach (int tr in tri)
                {
                    bw.Write(tr);
                }
            }

            bw.Write((int)PartToolsLib.EntryType.MeshEnd);
        }

        static void WriteTransform(BinaryWriter bw, Transform t)
        {
            bw.Write(t.gameObject.name);

            bw.Write(t.localPosition.x);
            bw.Write(t.localPosition.y);
            bw.Write(t.localPosition.z);
            bw.Write(t.localRotation.x);
            bw.Write(t.localRotation.y);
            bw.Write(t.localRotation.z);
            bw.Write(t.localRotation.w);
            bw.Write(t.localScale.x);
            bw.Write(t.localScale.y);
            bw.Write(t.localScale.z);
            bw.Write(t.localScale.x);
        }

        static void WriteTagAndLayer(BinaryWriter bw, Transform t)
        {
            bw.Write((int)PartToolsLib.EntryType.TagAndLayer);
            bw.Write(t.tag);
            bw.Write(t.gameObject.layer);
        }

        static void WriteCamera(BinaryWriter bw, Transform t)
        {
            Camera c = t.GetComponent<Camera>();

            if (c != null)
            {
                bw.Write((int)PartToolsLib.EntryType.Camera);

                bw.Write((int)c.clearFlags);
                WriteColor(bw, c.backgroundColor);
                bw.Write(c.cullingMask);
                bw.Write(c.orthographic);
                bw.Write(c.fov);
                bw.Write(c.near);
                bw.Write(c.far);
                bw.Write(c.depth);
            }
        }

        static void WriteAnimation(BinaryWriter bw, Transform t)
        {
            Animation anim = t.GetComponent<Animation>();
            if (anim != null)
            {
                int clipCount = anim.GetClipCount();
                if (clipCount == 0) // if no clips then can return
                    return;

                bw.Write((int)PartToolsLib.EntryType.Animation);



                // sadly need to use animationutility to get clip/curve data (hence writer must be an editor script)
                AnimationClip[] clips = AnimationUtility.GetAnimationClips(anim);
                bw.Write(clipCount);
                foreach (AnimationClip clip in clips)
                {
                    bw.Write(clip.name);

                    bw.Write(clip.localBounds.center.x);
                    bw.Write(clip.localBounds.center.y);
                    bw.Write(clip.localBounds.center.z);
                    bw.Write(clip.localBounds.size.x);
                    bw.Write(clip.localBounds.size.y);
                    bw.Write(clip.localBounds.size.z);

                    bw.Write((int)clip.wrapMode);


                    AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves(clip, true);
                    int curvesN = curveDatas.Length;

                    bw.Write((int)curvesN);
                    for (int i = 0; i < curvesN; i++)
                    {
                        bw.Write(curveDatas[i].path);
                        bw.Write(curveDatas[i].propertyName);

                        // work out type and write key
                        if (curveDatas[i].type == typeof(Transform))
                        {
                            bw.Write((int)PartToolsLib.AnimationType.Transform);
                        }
                        else if (curveDatas[i].type == typeof(Material))
                        {
                            bw.Write((int)PartToolsLib.AnimationType.Material);
                        }
                        else if (curveDatas[i].type == typeof(Light))
                        {
                            bw.Write((int)PartToolsLib.AnimationType.Light);
                        }
                        else if (curveDatas[i].type == typeof(AudioSource))
                        {
                            bw.Write((int)PartToolsLib.AnimationType.AudioSource);
                        }

                        bw.Write((int)curveDatas[i].curve.preWrapMode);
                        bw.Write((int)curveDatas[i].curve.postWrapMode);

                        // curve keys
                        int keysN = curveDatas[i].curve.keys.Length;
                        bw.Write(keysN);
                        for (int j = 0; j < keysN; j++)
                        {
                            bw.Write(curveDatas[i].curve.keys[j].time);
                            bw.Write(curveDatas[i].curve.keys[j].value);
                            bw.Write(curveDatas[i].curve.keys[j].inTangent);
                            bw.Write(curveDatas[i].curve.keys[j].outTangent);
                            bw.Write(curveDatas[i].curve.keys[j].tangentMode);
                        }
                    }
                }

                if (anim.clip != null)
                    bw.Write(anim.clip.name);
                else
                    bw.Write((string)"");

                bw.Write(anim.playAutomatically);
            }
        }

        static void WriteLight(BinaryWriter bw, Transform t)
        {
            Light l = t.GetComponent<Light>();

            if (l != null)
            {
                bw.Write((int)PartToolsLib.EntryType.Light);

                bw.Write((int)l.type);
                bw.Write(l.intensity);
                bw.Write(l.range);
                WriteColor(bw, l.color);
                bw.Write(l.cullingMask);
                bw.Write(l.spotAngle);
            }
        }

        static void WriteColor(BinaryWriter bw, Color c)
        {
            bw.Write(c.r);
            bw.Write(c.g);
            bw.Write(c.b);
            bw.Write(c.a);
        }

        #endregion

        #region Convert texture to normal map

        static void ConvertNormalTexture(string fileFullPath, float normalStrength)
        {
            byte[] imageData = File.ReadAllBytes(fileFullPath);
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(imageData);

            int width = tex.width;
            int height = tex.height;

            Texture2D newTex = new Texture2D(width, height, TextureFormat.RGB24, false);
            newTex.wrapMode = TextureWrapMode.Repeat;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float tl = GetTextureOffset(tex, x, y, -1, -1);// top left
                    float l = GetTextureOffset(tex, x, y, -1, 0);  // left
                    float bl = GetTextureOffset(tex, x, y, -1, 1); // bottom left
                    float t = GetTextureOffset(tex, x, y, 0, -1);  // top
                    float b = GetTextureOffset(tex, x, y, 0, 1);   // bottom
                    float tr = GetTextureOffset(tex, x, y, 1, -1); // top right
                    float r = GetTextureOffset(tex, x, y, 1, 0);   // right
                    float br = GetTextureOffset(tex, x, y, 1, 1);  // bottom right

                    // Compute dx using Sobel:
                    //           -1 0 1 
                    //           -2 0 2
                    //           -1 0 1

                    float dX = tr + 2 * r + br - tl - 2 * l - bl;

                    // Compute dy using Sobel:
                    //           -1 -2 -1 
                    //            0  0  0
                    //            1  2  1
                    float dY = bl + 2 * b + br - tl - 2 * t - tr;

                    Vector3 normal = new Vector3(dX, normalStrength, dY).normalized;

                    normal = (normal * 0.5f) + new Vector3(0.5f, 0.5f, 0.5f);

                    newTex.SetPixel(x, y, new Color(1.0f - normal.x, 1.0f - normal.z, 1.0f - normal.y));
                }
            }

            newTex.Apply();
            imageData = newTex.EncodeToPNG();
            File.WriteAllBytes(fileFullPath, imageData);
        }

        static float GetTextureOffset(Texture2D tex, int x, int y, int offSetX, int offsetY)
        {
            return tex.GetPixel(x + offSetX, y + offsetY).grayscale;
        }

        #endregion
    }

    public static class BitmapWriter
    {
        public static bool Write2D(Texture texture, string newPath)
        {
            return Write2D(texture, newPath, PartToolsLib.TextureType.Texture);
        }

        public static bool Write2D(Texture texture, string newPath, PartToolsLib.TextureType texType)
        {
            string oldPath = AssetDatabase.GetAssetPath(texture);

            TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(oldPath);

            TextureImporterSettings texSettingsBackup = new TextureImporterSettings();
            texImporter.ReadTextureSettings(texSettingsBackup);


            if (texImporter == null)
                return false;

            texImporter.isReadable = true;
            texImporter.mipmapEnabled = false;

            bool wasConverted = texImporter.convertToNormalmap;
            texImporter.convertToNormalmap = false;

            // force the update of the above settings
            AssetDatabase.ImportAsset(oldPath, ImportAssetOptions.ForceUpdate);


            if (texType == PartToolsLib.TextureType.NormalMap && wasConverted)
            {
                WriteTexture2D(NormalMapTools.GreyscaleToNormalMap((Texture2D)texture, texImporter.heightmapScale), texType, newPath);
            }
            else
                WriteTexture2D((Texture2D)texture, texType, newPath);


            // reapply the old settings
            texImporter.SetTextureSettings(texSettingsBackup);
            AssetDatabase.ImportAsset(oldPath, ImportAssetOptions.ForceUpdate);

            return true;
        }


        private static void WriteTexture2D(Texture2D texture, PartToolsLib.TextureType texType, string newPath)
        {
            BinaryWriter bw = new BinaryWriter(File.Open(newPath, FileMode.Create));

            // write header
            bw.Write("KSP");
            bw.Write(texture.width);
            bw.Write(texture.height);
            bw.Write((int)texType);


            Color32[] colors = texture.GetPixels32(0);
            int nPixels = colors.Length;
            Color32 color;

            if (texType == PartToolsLib.TextureType.Texture)
            {
                bool writeAlpha = false;
                switch (texture.format)
                {
                    case TextureFormat.ARGB32:
                    case TextureFormat.RGBA32:
                    case TextureFormat.DXT5:

                        writeAlpha = true;
                        bw.Write(32);

                        break;
                    case TextureFormat.DXT1:
                    case TextureFormat.RGB24:

                        writeAlpha = false;
                        bw.Write(24);

                        break;
                }

                for (int i = 0; i < nPixels; i++)
                {
                    color = colors[i];

                    bw.Write(color.r);
                    bw.Write(color.g);
                    bw.Write(color.b);

                    if (writeAlpha)
                        bw.Write(color.a);

                    if (i % texture.height == 0)
                        bw.Flush();
                }
            }
            else if (texType == PartToolsLib.TextureType.NormalMap)
            {
                bw.Write(32);

                for (int i = 0; i < nPixels; i++)
                {
                    color = colors[i];

                    bw.Write(color.r);
                    bw.Write(color.g);
                    bw.Write(color.b);
                    bw.Write(color.a);

                    if (i % texture.height == 0)
                        bw.Flush();
                }
            }

            bw.Close();
        }
    }

    public static class NormalMapTools
    {
        public static Texture2D GreyscaleToNormalMap(Texture2D tex, float normalStrength)
        {
            int width = tex.width;
            int height = tex.height;

            Texture2D newTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            newTex.wrapMode = TextureWrapMode.Repeat;

            Color color;

            float nStrength = normalStrength * 0.001f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float tl = GetTextureOffset(tex, x, y, -1, -1);// top left
                    float l = GetTextureOffset(tex, x, y, -1, 0);  // left
                    float bl = GetTextureOffset(tex, x, y, -1, 1); // bottom left
                    float t = GetTextureOffset(tex, x, y, 0, -1);  // top
                    float b = GetTextureOffset(tex, x, y, 0, 1);   // bottom
                    float tr = GetTextureOffset(tex, x, y, 1, -1); // top right
                    float r = GetTextureOffset(tex, x, y, 1, 0);   // right
                    float br = GetTextureOffset(tex, x, y, 1, 1);  // bottom right

                    // Compute dx using Sobel:
                    //           -1 0 1 
                    //           -2 0 2
                    //           -1 0 1

                    float dX = tr + 2 * r + br - tl - 2 * l - bl;

                    // Compute dy using Sobel:
                    //           -1 -2 -1 
                    //            0  0  0
                    //            1  2  1
                    float dY = bl + 2 * b + br - tl - 2 * t - tr;


                    Vector3 normal = new Vector3(dX, nStrength, dY).normalized;

                    normal = (normal * 0.5f) + new Vector3(0.5f, 0.5f, 0.5f);

                    color.r = 1.0f - normal.y;
                    color.g = 1.0f - normal.z;
                    color.b = 1.0f;
                    color.a = 1.0f - normal.x;

                    newTex.SetPixel(x, y, color);
                }
            }

            newTex.Apply();

            return newTex;
        }

        private static float GetTextureOffset(Texture2D tex, int x, int y, int offSetX, int offsetY)
        {
            return tex.GetPixel(x + offSetX, y + offsetY).grayscale;
        }

        public static Texture2D UnityNormalMapToNormalMap(Texture2D tex)
        {
            int width = tex.width;
            int height = tex.height;
            Texture2D newTex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            newTex.wrapMode = TextureWrapMode.Repeat;

            Color inColor;
            Color outColor = new Color(1f, 1f, 1f, 1f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    inColor = tex.GetPixel(x, y);

                    outColor.r = inColor.a;
                    outColor.g = inColor.r;
                    outColor.b = 1f;
                    //outColor.a = inColor.b;

                    newTex.SetPixel(x, y, outColor);
                }
            }

            newTex.Apply(false, false);

            return newTex;
        }
    }

    public static class TGAWriter
    {
        public static bool WriteImage(Texture texture, string newPath, PartToolsLib.TextureType texType, bool compress)
        {
            string oldPath = AssetDatabase.GetAssetPath(texture);

            TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(oldPath);

            TextureImporterSettings texSettingsBackup = new TextureImporterSettings();
            texImporter.ReadTextureSettings(texSettingsBackup);


            if (texImporter == null)
                return false;

            texImporter.isReadable = true;
            texImporter.mipmapEnabled = false;

            bool wasConverted = texImporter.convertToNormalmap;
            texImporter.convertToNormalmap = false;

            // force the update of the above settings
            AssetDatabase.ImportAsset(oldPath, ImportAssetOptions.ForceUpdate);
            Texture2D tex2d = (Texture2D)texture;

            if (texType == PartToolsLib.TextureType.NormalMap)
            {
                if (wasConverted)
                {
                    WriteImage(newPath, NormalMapTools.UnityNormalMapToNormalMap(NormalMapTools.GreyscaleToNormalMap(tex2d, texImporter.heightmapScale)), false, compress);
                }
                else
                {
                    WriteImage(newPath, NormalMapTools.UnityNormalMapToNormalMap(tex2d), false, compress);
                }
            }
            else
            {
                WriteImage(newPath, tex2d,
                    (tex2d.format == TextureFormat.ARGB32 || tex2d.format == TextureFormat.RGBA32 || tex2d.format == TextureFormat.DXT1 || tex2d.format == TextureFormat.DXT5)
                    , compress);
            }

            // reapply the old settings
            texImporter.SetTextureSettings(texSettingsBackup);
            AssetDatabase.ImportAsset(oldPath, ImportAssetOptions.ForceUpdate);

            return true;
        }

        public static bool WriteImage(string filePath, Texture2D texture, bool hasAlpha, bool compressRTE)
        {
            TGAHeader header = new TGAHeader();

            #region Setup header

            header.width = (ushort)texture.width;
            header.height = (ushort)texture.height;

            if (compressRTE)
            {
                header.imageType = TGAImageType.RTE_TrueColor;
            }
            else
            {
                header.imageType = TGAImageType.Uncompressed_TrueColor;
            }

            if (hasAlpha)
            {
                header.pixelDepth = 32;
            }
            else
            {
                header.pixelDepth = 24;
            }

            #endregion

            byte[] headerData = header.GetData();
            byte[] colorData = null;

            Color32[] texData = texture.GetPixels32();

            if (compressRTE)
            {
                colorData = CreateRTEFromTexture(header, texData, hasAlpha);
            }
            else
            {
                colorData = CreateFromTexture(texData, hasAlpha);
            }

            if (colorData == null)
            {
                Debug.Log("Color data is null");
                return false;
            }

            byte[] allData = new byte[18 + colorData.Length];

            headerData.CopyTo(allData, 0);
            colorData.CopyTo(allData, 18);

            File.WriteAllBytes(filePath, allData);

            return true;
        }


        private static byte[] CreateRTEFromTexture(TGAHeader header, Color32[] colorData, bool hasAlpha)
        {
            List<byte> data = new List<byte>(header.width * header.height * 2 * 4);

            int lineIndex = 0;

            while (lineIndex < header.height)
            {
                int rowStartIndex = lineIndex * header.width;
                int rowEndIndex = rowStartIndex + header.width;

                int rowIndex = rowStartIndex;

                while (rowIndex < rowEndIndex)
                {
                    if (rowIndex == rowEndIndex - 1)
                    {
                        // we're in last row so its definatly raw encoded
                        AddRawPixels(rowIndex, 1, colorData, data, hasAlpha);
                        rowIndex++;

                        //  Debug.Log("END " + lineIndex + " " + (rowIndex - rowStartIndex) + " " + 1 + " " + ((rowIndex - rowStartIndex) + 1));
                    }
                    else
                    {
                        int repetitionCount = CalculateColorRepetition(rowIndex, rowEndIndex, colorData);

                        if (repetitionCount == 0)
                        {
                            // no repetition, should be raw encoded
                            repetitionCount = CalculateColorNoRepetition(rowIndex, rowEndIndex, colorData);

                            // Debug.Log("RAW " + lineIndex + " " + (rowIndex - rowStartIndex) + " " + (repetitionCount + 1) + " " + ((rowIndex - rowStartIndex) + (repetitionCount + 1)));

                            if (repetitionCount == 0)
                                Debug.Log(lineIndex + " " + rowIndex + " " + repetitionCount);

                            AddRawPixels(rowIndex, repetitionCount, colorData, data, hasAlpha);
                            rowIndex += repetitionCount;
                        }
                        else
                        {
                            // Debug.Log("RTE " + lineIndex + " " + (rowIndex - rowStartIndex) + " " + (repetitionCount + 1) + " " + ((rowIndex - rowStartIndex) + (repetitionCount + 1)));

                            // has at least 1 repetition so should be rte encoded
                            AddRTEPixels(rowIndex, repetitionCount, colorData, data, hasAlpha);
                            rowIndex += repetitionCount;
                        }
                    }
                }

                lineIndex++;
            }

            return data.ToArray();
        }

        private static void AddRTEPixels(int startIndex, int numPixels, Color32[] colorData, List<byte> data, bool hasAlpha)
        {
            data.Add((byte)(128 + (numPixels - 1)));

            Color32 color = colorData[startIndex];

            data.Add(color.b);
            data.Add(color.g);
            data.Add(color.r);

            if (hasAlpha)
                data.Add(color.a);
        }

        private static void AddRawPixels(int startIndex, int numPixels, Color32[] colorData, List<byte> data, bool hasAlpha)
        {
            data.Add((byte)(numPixels - 1));

            Color32 color;
            int endIndex = startIndex + numPixels;

            for (int i = startIndex; i < endIndex; i++)
            {
                color = colorData[i];

                data.Add(color.b);
                data.Add(color.g);
                data.Add(color.r);

                if (hasAlpha)
                    data.Add(color.a);
            }
        }

        private static int CalculateColorRepetition(int startColorIndex, int lineEndIndex, Color32[] colorData)
        {
            int repetitionCount = 0;
            int repetitionIndex = startColorIndex + 1;

            Color32 startColor = colorData[startColorIndex];

            if (!ColorsEqual(startColor, colorData[repetitionIndex]))
            {
                return 0;
            }

            while (repetitionIndex < lineEndIndex && repetitionCount < 128 && ColorsEqual(startColor, colorData[repetitionIndex]))
            {
                repetitionIndex++;
                repetitionCount++;
            }

            return repetitionCount;
        }

        private static int CalculateColorNoRepetition(int startColorIndex, int lineEndIndex, Color32[] colorData)
        {
            int repetitionCount = 0;
            int repetitionIndex = startColorIndex;

            while (repetitionIndex < (lineEndIndex - 1) && repetitionCount < 128 && !ColorsEqual(colorData[repetitionIndex], colorData[repetitionIndex + 1]))
            {
                repetitionIndex++;
                repetitionCount++;
            }

            return repetitionCount;
        }


        private static bool ColorsEqual(Color32 a, Color32 b)
        {
            return (a.r == b.r && a.b == b.b && a.g == b.g && a.a == b.a);
        }

        private static byte[] CreateFromTexture(Color32[] colorData, bool hasAlpha)
        {
            int bpp = 3;
            if (hasAlpha)
                bpp = 4;

            byte[] data = new byte[colorData.Length * bpp];
            int dataIndex = 0;

            for (int i = 0; i < colorData.Length; i++)
            {
                data[dataIndex++] = colorData[i].b;
                data[dataIndex++] = colorData[i].g;
                data[dataIndex++] = colorData[i].r;

                if (hasAlpha)
                    data[dataIndex++] = colorData[i].a;
            }

            return data;
        }
    }

    public static class PNGWriter
    {
        public static bool WriteImage(Texture texture, string newPath, PartToolsLib.TextureType texType)
        {
            string oldPath = AssetDatabase.GetAssetPath(texture);

            TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(oldPath);

            if (texImporter == null)
                return false;


            TextureImporterSettings texSettingsBackup = new TextureImporterSettings();
            texImporter.ReadTextureSettings(texSettingsBackup);

            TextureImporterSettings texSettings = new TextureImporterSettings();
            texImporter.ReadTextureSettings(texSettings);

            texSettings.readable = true;
            texSettings.mipmapEnabled = false;

            bool wasConverted = texSettings.convertToNormalMap;
            texSettings.convertToNormalMap = false;

            if (texSettings.textureFormat == TextureImporterFormat.AutomaticCompressed)
                texSettings.textureFormat = TextureImporterFormat.AutomaticTruecolor;
            if (texSettings.textureFormat == TextureImporterFormat.DXT1)
                texSettings.textureFormat = TextureImporterFormat.RGB24;
            else if (texSettings.textureFormat == TextureImporterFormat.DXT5)
                texSettings.textureFormat = TextureImporterFormat.ARGB32;

            // force the update of the above settings
            texImporter.SetTextureSettings(texSettings);
            AssetDatabase.ImportAsset(oldPath, ImportAssetOptions.ForceUpdate);

            Texture tex = (Texture)AssetDatabase.LoadAssetAtPath(oldPath, typeof(Texture));
            Texture2D tex2d = (Texture2D)tex;

            if (tex2d.format == TextureFormat.DXT1 || tex2d.format == TextureFormat.DXT5)
            {
                tex2d = CreateCopy(tex2d);
            }

            if (texType == PartToolsLib.TextureType.NormalMap)
            {
                if (wasConverted)
                {
                    WriteImage(newPath, NormalMapTools.UnityNormalMapToNormalMap(NormalMapTools.GreyscaleToNormalMap(tex2d, texImporter.heightmapScale)));
                }
                else
                {
                    WriteImage(newPath, NormalMapTools.UnityNormalMapToNormalMap(tex2d));
                }
            }
            else
            {
                WriteImage(newPath, tex2d);
            }

            // reapply the old settings
            texImporter.SetTextureSettings(texSettingsBackup);
            AssetDatabase.ImportAsset(oldPath, ImportAssetOptions.ForceUpdate);

            return true;
        }

        private static void WriteImage(string path, Texture2D texture)
        {
            byte[] byteArray = texture.EncodeToPNG();
            File.WriteAllBytes(path, byteArray);
        }

        private static Texture2D CreateCopy(Texture2D orig)
        {
            Texture2D newTex = new Texture2D(orig.width, orig.height, TextureFormat.ARGB32, false);
            Color32[] colors = orig.GetPixels32();
            newTex.SetPixels32(colors);
            newTex.Apply(false, false);
            return newTex;
        }
    }
}