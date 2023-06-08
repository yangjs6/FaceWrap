using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace FaceWrap.Runtime
{
    [ScriptedImporter(1, new[] { "bone" })]
    public class BoneFileImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var root = ObjectFactory.CreateInstance<FaceBoneWeightsAsset>();
            ctx.AddObjectToAsset("mapRoot", root);
            ctx.SetMainObject(root);
            root.ReadBoneFile(ctx.assetPath);
        }
    }

    public class FaceBoneWeightsAsset : ScriptableObject
    {
        public string asset_path;
        public int numSubMesh;
        public int[] boneIndex;
        public BoneWeight[] boneWeights;
        public bool ReadBoneFile(string boneFile)
        {
            
            asset_path = boneFile;
            
            bool bReadOk = false;
            if (!File.Exists(boneFile))
            {
                return false;
            }

            using (FileStream fs = new FileStream(boneFile, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader fin = new BinaryReader(fs))
                {
                    numSubMesh = fin.ReadInt32();
                    
                    int numBone = fin.ReadInt32();
                    boneIndex = new int[numBone];
                    
                    for (int i = 0; i < numBone; i++)
                    {
                        boneIndex[i] = fin.ReadInt32();
                    }

                    int numVertex = fin.ReadInt32();
                    boneWeights = new BoneWeight[numVertex];
                    
                    for (int i = 0; i < numVertex; i++)
                    {
                        boneWeights[i].boneIndex0 = fin.ReadInt32();
                        boneWeights[i].weight0 = fin.ReadSingle();
                        boneWeights[i].boneIndex1 = fin.ReadInt32();
                        boneWeights[i].weight1 = fin.ReadSingle();
                        boneWeights[i].boneIndex2 = fin.ReadInt32();
                        boneWeights[i].weight2 = fin.ReadSingle();
                        boneWeights[i].boneIndex3 = fin.ReadInt32();
                        boneWeights[i].weight3 = fin.ReadSingle();
                    }
                    
                    
                    fin.Close();
                    fs.Close();
                    bReadOk = true;
                }
            }

            return bReadOk;
        }

        public bool SaveBoneFile(string boneFile)
        {
            
            bool bSaveOk = false;
            
            using (FileStream fs = new FileStream(boneFile, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter fin = new BinaryWriter(fs))
                {
                    fin.Write(numSubMesh);
                    
                    fin.Write(boneIndex.Length);
                    for (int i = 0; i < boneIndex.Length; i++)
                    {
                        fin.Write(boneIndex[i]);
                    }
                    
                    fin.Write(boneWeights.Length);
                    for (int i = 0; i < boneWeights.Length; i++)
                    {
                        fin.Write(boneWeights[i].boneIndex0);
                        fin.Write(boneWeights[i].weight0);
                        fin.Write(boneWeights[i].boneIndex1);
                        fin.Write(boneWeights[i].weight1);
                        fin.Write(boneWeights[i].boneIndex2);
                        fin.Write(boneWeights[i].weight2);
                        fin.Write(boneWeights[i].boneIndex3);
                        fin.Write(boneWeights[i].weight3);
                    }
                    
                    fin.Close();
                    fs.Close();
                    bSaveOk = true;
                }
            }

            return bSaveOk;
        }
    }
}