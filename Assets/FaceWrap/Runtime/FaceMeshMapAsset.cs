using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace FaceWrap.Runtime
{

    [ScriptedImporter(1, new[] { "map" })]
    public class MapFileImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var root = ObjectFactory.CreateInstance<FaceMeshMapAsset>();
            ctx.AddObjectToAsset("mapRoot", root);
            ctx.SetMainObject(root);
            root.ReadMapFile(ctx.assetPath);
        }
    }

    
    public class FaceMeshMapAsset : ScriptableObject
    {
        public string asset_path;
        public int numSubMesh;
        public int[] indexMap;

        public bool ReadMapFile(string mapFile)
        {
            asset_path = mapFile;
            
            bool bReadOk = false;
            if (!File.Exists(mapFile))
            {
                return false;
            }

            using (FileStream fs = new FileStream(mapFile, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader fin = new BinaryReader(fs))
                {
                    numSubMesh = fin.ReadInt32();
                    int numVertex = fin.ReadInt32();

                    indexMap = new int[numVertex];
                    
                    for (int i = 0; i < numVertex; i++)
                    {
                        indexMap[i] = fin.ReadInt32();
                    }

                    fin.Close();
                    fs.Close();
                    bReadOk = true;
                }
            }

            return bReadOk;
        }

        public bool SaveMapFile(string mapFile)
        {
            bool bSaveOk = false;
            
            using (FileStream fs = new FileStream(mapFile, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter fin = new BinaryWriter(fs))
                {
                    fin.Write(numSubMesh);
                    fin.Write(indexMap.Length);
                    
                    for (int i = 0; i < indexMap.Length; i++)
                    {
                        fin.Write(indexMap[i]);
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