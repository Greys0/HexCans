using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PartToolsLib
{
    public enum FileType : int
    {
        ModelBinary = 76543
    }

    /// <summary>
    /// Type of entry in the file format
    /// </summary>
    public enum EntryType : int
    {
        ChildTransformStart,
        ChildTransformEnd,

        Animation,

        MeshCollider,
        SphereCollider,
        CapsuleCollider,
        BoxCollider,
        
        MeshFilter,
        MeshRenderer,
        SkinnedMeshRenderer,

        Materials,
        Material,
        Textures,

        MeshStart,
        MeshVerts,
        MeshUV,
        MeshUV2,
        MeshNormals,
        MeshTangents,
        MeshTriangles,
        MeshBoneWeights,
        MeshBindPoses,
        MeshEnd,

        Light,
        
        TagAndLayer,

        MeshCollider2,
        SphereCollider2,
        CapsuleCollider2,
        BoxCollider2,
        WheelCollider,

        Camera
    }

    /// <summary>
    /// Supported shaders
    /// </summary>
    public enum ShaderType : int
    {
        Custom,
        Diffuse,
        Specular,
        Bumped,
        BumpedSpecular,
        Emissive,
        EmissiveSpecular,
        EmissiveBumpedSpecular,
        AlphaCutout,
        AlphaCutoutBumped,
        Alpha,
        AlphaSpecular,
        AlphaUnlit,
        Unlit
    }

    /// <summary>
    /// Supported animation types
    /// </summary>
    public enum AnimationType : int
    {
        Transform,
        Material,
        Light,
        AudioSource
    }

    public enum TextureType : int
    {
        Texture,
        NormalMap
    }
}

/*

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PartLib
{
    /// <summary>
    /// Contains the methods to write mu files
    /// </summary>
    public static class ObjectWriter
    {
        /// <summary>
        /// Writes a gameobject heirarchy to a mu file
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <param name="partName">Name of eventual part name</param>
        /// <param name="rootObject">Parent representing world origin</param>
        public static void Write(string filePath, string filename, Transform rootObject)
        {
            Write(filePath, filename, rootObject, false, false);
        }

        static bool copyTexturesToTargetDir;
        static bool renameTextures;
        static string filePath;
        static string filename;

        /// <summary>
        /// Writes a gameobject heirarchy to a mu file and can copy/rename textures
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <param name="target">Target gameobject to write</param>
        /// <param name="copyTexturesToTargetDir">Copy textures to target directory</param>
        /// <param name="renameTextures">Rename textures with respect to target name</param>
        public static void Write(string filePath, string filename, Transform target, bool copyTexturesToTargetDir, bool renameTextures)
        {
            ObjectWriter.copyTexturesToTargetDir = copyTexturesToTargetDir;
            ObjectWriter.renameTextures = renameTextures;
            ObjectWriter.filePath = filePath;
            ObjectWriter.filename = filename;

            materials = new List<Material>();
            textures = new List<Texture>();

            System.IO.FileInfo file = new System.IO.FileInfo(filePath);
            file.Directory.Create();

            BinaryWriter bw = new BinaryWriter(File.Open(filePath + filename, FileMode.Create));

            try
            {
                WriteChild(bw, target);

                // add on materials

                if (materials.Count > 0)
                {
                    bw.Write((int)MuEntryType.Materials);
                    bw.Write(materials.Count);
                    foreach (Material mat in materials)
                    {
                        WriteMaterial(bw, mat);
                        bw.Flush();
                    }

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

            bw.Close();

            materials = null;
            textures = null;
        }

        #region Write methods

        // materials array is used to cache all the shared materials before writing
        static List<Material> materials;

        static List<Texture> textures;


        static void WriteChild(BinaryWriter bw, Transform t)
        {
            WriteTransform(bw, t);

            WriteCollider(bw, t);
            WriteMeshFiler(bw, t);
            WriteMeshRenderer(bw, t);
            WriteSkinnedMeshRenderer(bw, t);
            WriteAnimation(bw, t);

            foreach (Transform child in t)
            {
                bw.Write((int)MuEntryType.ChildTransformStart);
                WriteChild(bw, child);
                bw.Write((int)MuEntryType.ChildTransformEnd);
            }

            bw.Flush();
        }

        static void WriteMeshFiler(BinaryWriter bw, Transform t)
        {
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf != null)
            {
                bw.Write((int)MuEntryType.MeshFilter);
                WriteMesh(bw, mf.sharedMesh);
            }
        }

        static void WriteMeshRenderer(BinaryWriter bw, Transform t)
        {
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                bw.Write((int)MuEntryType.MeshRenderer);

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
                bw.Write((int)MuEntryType.SkinnedMeshRenderer);

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

            switch (mat.shader.name)
            {
                case "Specular":
                    bw.Write((int)MuShaderType.Specular);

                    AddTexture(bw, mat, "_MainTex");
                    WriteColor(bw, mat.GetColor("_Color"));
                    WriteColor(bw, mat.GetColor("_SpecColor"));
                    bw.Write(mat.GetFloat("_Shininess"));

                    break;

                case "Bumped Diffuse":
                    bw.Write((int)MuShaderType.BumpedDiffuse);

                    AddTexture(bw, mat, "_MainTex");
                    AddTexture(bw, mat, "_BumpMap");
                    WriteColor(bw, mat.GetColor("_Color"));

                    break;

                case "Bumped Specular":
                    bw.Write((int)MuShaderType.BumpedSpecular);

                    AddTexture(bw, mat, "_MainTex");
                    AddTexture(bw, mat, "_BumpMap");
                    WriteColor(bw, mat.GetColor("_Color"));
                    WriteColor(bw, mat.GetColor("_SpecColor"));
                    bw.Write(mat.GetFloat("_Shininess"));

                    break;

                case "Diffuse":
                default:
                    bw.Write((int)MuShaderType.Diffuse);

                    AddTexture(bw, mat, "_MainTex");
                    WriteColor(bw, mat.GetColor("_Color"));

                    break;
            }
        }

        static void AddTexture(BinaryWriter bw, Material mat, string textureName)
        {
            Texture tex = mat.GetTexture(textureName);

            if (tex != null)
            {
                if (!textures.Contains(tex))
                    textures.Add(tex);

                bw.Write(textures.IndexOf(tex));
            }
            else
            {
                bw.Write(-1);
            }
        }

        static void WriteTextures(BinaryWriter bw)
        {
            bw.Write((int)MuEntryType.Textures);
            bw.Write(textures.Count);

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i] == null)
                {
                    Debug.LogError(i);

                    bw.Write(" ");
                }
                else
                {
                    string name = textures[i].name;

                    if (copyTexturesToTargetDir)
                    {
                        string path = AssetDatabase.GetAssetPath(textures[i]);

                        if (renameTextures)
                            name = filename + i.ToString("D3");

                        try
                        {
                            AssetDatabase.CopyAsset(path, filePath + name + ".png");
                        }
                        catch
                        { }
                    }

                    bw.Write(name);
                }
            }
        }



        static void WriteCollider(BinaryWriter bw, Transform t)
        {
            MeshCollider mc = t.GetComponent<MeshCollider>();
            if (mc != null)
            {
                bw.Write((int)MuEntryType.MeshCollider);
                WriteMesh(bw, mc.sharedMesh);
                return;
            }

            BoxCollider bc = t.GetComponent<BoxCollider>();
            if (bc != null)
            {
                bw.Write((int)MuEntryType.BoxCollider);
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
                bw.Write((int)MuEntryType.CapsuleCollider);
                bw.Write(cc.radius);
                bw.Write(cc.direction);
                bw.Write(cc.center.x);
                bw.Write(cc.center.y);
                bw.Write(cc.center.z);
                return;
            }

            SphereCollider sc = t.GetComponent<SphereCollider>();
            if (sc != null)
            {
                bw.Write((int)MuEntryType.SphereCollider);
                bw.Write(sc.radius);
                bw.Write(sc.center.x);
                bw.Write(sc.center.y);
                bw.Write(sc.center.z);
                return;
            }
        }

        static void WriteMesh(BinaryWriter bw, Mesh mesh)
        {
            int vCount = mesh.vertexCount;
            int smCount = mesh.subMeshCount;


            bw.Write((int)MuEntryType.MeshStart);
            bw.Write(vCount);
            bw.Write(smCount);


            Vector3[] verts = mesh.vertices;
            bw.Write((int)MuEntryType.MeshVerts);
            foreach (Vector3 v in verts)
            {
                bw.Write(v.x);
                bw.Write(v.y);
                bw.Write(v.z);
            }

            Vector2[] uvs = mesh.uv;
            if (uvs != null && uvs.Length == vCount)
            {
                bw.Write((int)MuEntryType.MeshUV);
                foreach (Vector2 uv in uvs)
                {
                    bw.Write(uv.x);
                    bw.Write(uv.y);
                }
            }

            Vector2[] uv2s = mesh.uv2;
            if (uv2s != null && uv2s.Length == vCount)
            {
                bw.Write((int)MuEntryType.MeshUV2);
                foreach (Vector2 uv in uv2s)
                {
                    bw.Write(uv.x);
                    bw.Write(uv.y);
                }
            }

            Vector3[] normals = mesh.normals;
            if (normals != null && normals.Length == vCount)
            {
                bw.Write((int)MuEntryType.MeshNormals);
                foreach (Vector3 n in normals)
                {
                    bw.Write(n.x);
                    bw.Write(n.y);
                    bw.Write(n.y);
                }
            }

            Vector4[] tangents = mesh.tangents;
            if (tangents != null && tangents.Length == vCount)
            {
                bw.Write((int)MuEntryType.MeshTangents);
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
                bw.Write((int)MuEntryType.MeshBoneWeights);
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
                bw.Write((int)MuEntryType.MeshBindPoses);
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

                bw.Write((int)MuEntryType.MeshTriangles);
                bw.Write(tri.Length);

                foreach (int tr in tri)
                {
                    bw.Write(tr);
                }
            }

            bw.Write((int)MuEntryType.MeshEnd);
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

        static void WriteAnimation(BinaryWriter bw, Transform t)
        {
            Animation anim = t.GetComponent<Animation>();
            if (anim != null)
            {
                int clipCount = anim.GetClipCount();
                if (clipCount == 0) // if no clips then can return
                    return;

                bw.Write((int)MuEntryType.Animation);



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
                            bw.Write((int)MuAnimationType.Transform);
                        }
                        else if (curveDatas[i].type == typeof(Material))
                        {
                            bw.Write((int)MuAnimationType.Material);
                        }
                        else if (curveDatas[i].type == typeof(Light))
                        {
                            bw.Write((int)MuAnimationType.Light);
                        }
                        else if (curveDatas[i].type == typeof(AudioSource))
                        {
                            bw.Write((int)MuAnimationType.AudioSource);
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

        static void WriteColor(BinaryWriter bw, Color c)
        {
            bw.Write(c.r);
            bw.Write(c.g);
            bw.Write(c.b);
            bw.Write(c.a);
        }

        #endregion
    }
}*/