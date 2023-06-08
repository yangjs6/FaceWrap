using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace FaceWrap.Runtime
{
    [ScriptedImporter(1, new[] { "dat" })]
    public class DatFileImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var root = ObjectFactory.CreateInstance<FaceObjectVertAsset>();
            ctx.AddObjectToAsset("vertRoot", root);
            ctx.SetMainObject(root);
            root.ReadDatFile(ctx.assetPath);
        }
    }

    public class FaceObjectVertAsset : ScriptableObject
    {
        public List<Vector3> vertices;
        
        public void ReadDatFile(string assetPath)
        {
            FileInfo theSourceFile = new FileInfo(assetPath);
            StreamReader reader = theSourceFile.OpenText();
            string text;
            vertices = new List<Vector3>();
            do
            {
                text = reader.ReadLine();
                if (text == null)
                    break;
                string[] value2 = System.Text.RegularExpressions.Regex.Split(text, @"\s{1,}");
                if (value2[0] == "v")
                {
                    float x = float.Parse(value2[1]);
                    float y = float.Parse(value2[2]);
                    float z = float.Parse(value2[3]);
                    vertices.Add(new Vector3(-x, y, z));
                }

            } while (text != null);
            reader.Close();
        }
    }
}