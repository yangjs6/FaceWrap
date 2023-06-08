using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace FaceWrap.Runtime
{
    [ScriptedImporter(1, new[] { "vert" })]
    public class VertFileImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var root = ObjectFactory.CreateInstance<FaceVertAnimAsset>();
            ctx.AddObjectToAsset("vertRoot", root);
            ctx.SetMainObject(root);
            root.ReadVertAnimFile(ctx.assetPath);
        }
    }
    
    
    [ScriptedImporter(1, new[] { "npy" })]
    public class NpyFileImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var root = ObjectFactory.CreateInstance<FaceVertAnimAsset>();
            ctx.AddObjectToAsset("vertRoot", root);
            ctx.SetMainObject(root);
            root.ReadNpyAnimFile(ctx.assetPath);
        }
    }
    
    public class FaceVertAnimAsset : ScriptableObject
    {
        [Serializable]
        public struct FaceVertData2
        {
            public int vert_index;
            public Vector3 vert_offset;
        }

        [Serializable]
        public struct FaceVertFrameData2
        {
            public float frame_time;
            public List<FaceVertData2> vert_data;
        }

        public string asset_path;
        public float time_length;
        public float frame_length;
        public int frame_second_per;
        public List<FaceVertFrameData2> frame_list;
        // public Dictionary<int, List<int>> mapping_list;
        public List<float> oral_bs_weight;


        public bool ReadAnimFile(string animFile)
        {
            if (animFile.EndsWith(".vert"))
            {
                return ReadVertAnimFile(animFile);
            }else if (animFile.EndsWith(".npy"))
            {
                return ReadNpyAnimFile(animFile);
            }

            return false;
        }
        public bool ReadVertAnimFile(string animFile)
        {
            asset_path = animFile;
            bool bReadOk = false;
            if (!File.Exists(animFile))
            {
                return false;
            }
            
            using (FileStream fs = new FileStream(animFile, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader fin = new BinaryReader(fs))
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    
                    char[] version = new char[200];
                    fin.Read(version, 0, 7);
                    int target_size = 0;

                    // mapping_list = new Dictionary<int, List<int>>();
                    target_size = fin.ReadInt32();
                    for (int i = 0; i < target_size; i++)
                    {
                        int target_index = 0;
                        target_index = fin.ReadInt32();
                        // if (target_index != -1)
                        // {
                        //     if (!mapping_list.ContainsKey(target_index))
                        //     {
                        //         mapping_list[target_index] = new List<int>();
                        //     }
                        //     mapping_list[target_index].Add(i);
                        // }
                    }
                    
                    int size_num = 0;
                    size_num = fin.ReadInt32();
                    frame_second_per = 0;
                    frame_second_per = fin.ReadInt32();
                    Console.WriteLine("size_num: " + size_num + " " + frame_second_per);
                    frame_length = size_num;
                    time_length = size_num * 1.0f / frame_second_per;
                    
                    frame_list = new List<FaceVertFrameData2>();
                    oral_bs_weight = new List<float>();
                    for (int i = 0; i < size_num; i++)
                    {
                        FaceVertFrameData2 vfd;
                        vfd.frame_time = i * 1.0f / frame_second_per;
                        int vertex_num = 0;
                        vertex_num = fin.ReadInt32();

                        vfd.vert_data = new List<FaceVertData2>();
                        
                        for (int j = 0; j < vertex_num; j++)
                        {
                            FaceVertData2 v;
                            v.vert_index = (int)fin.ReadSingle();
                            v.vert_offset.x = fin.ReadSingle();
                            v.vert_offset.y = fin.ReadSingle();
                            v.vert_offset.z = fin.ReadSingle();
                            vfd.vert_data.Add(v);
                        }
                        frame_list.Add(vfd);
                        float weight = 0.0f;
                        weight = fin.ReadSingle();
                        oral_bs_weight.Add(weight);
                    }

                    bReadOk = true;
                    fin.Close();
                    fs.Close();
                }
            }

            return bReadOk;
        }

        public bool ReadNpyAnimFile(string animFile)
        {
            try
            {
                asset_path = animFile;
                List<int> shape;
                bool fortran_order;
                float[] data;
                NpyLoader.LoadArrayFromNumpy(animFile, out shape, out fortran_order, out data);

                frame_list = new List<FaceVertFrameData2>();
                frame_length = shape[0];
                frame_second_per = 30;
                time_length = frame_length * 1.0f / frame_second_per;
                
                for(int i = 0; i < shape[0]; i++)
                {
                    FaceVertFrameData2 vfd;
                    vfd.vert_data = new List<FaceVertData2>();
                    int v_index = 0;
                    for(int j = 0; j < shape[1]; j += 3)
                    {
                        FaceVertData2 v;
                        v.vert_index = v_index;
                        v.vert_offset.x = data[i*shape[1]+j] - data[0*shape[1]+j];
                        v.vert_offset.y = data[i*shape[1]+j+1] - data[0*shape[1]+j+1];
                        v.vert_offset.z = data[i*shape[1]+j+2] - data[0*shape[1]+j+2];
                        
                        vfd.vert_data.Add(v);
                        v_index++;
                    }
                    vfd.frame_time= i * 1.0f / frame_second_per;
                    frame_list.Add(vfd);
                }
                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
    
    

}