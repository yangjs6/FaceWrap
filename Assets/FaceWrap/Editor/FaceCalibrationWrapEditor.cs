using System.Collections.Generic;
using System.Linq;
using FaceWrap.Runtime;
using KdTree;
using KdTree.Math;
using log4net.Core;
using UnityEditor;
using UnityEngine;

namespace FaceWrap.Editor
{

    public class FaceVertAnimator
    {
        private FaceCalibration mFace;
        private GameObject mGameObject;
        private Renderer mRenderer;
        private Mesh originalMesh;
        private Mesh clonedMesh;
        private Vector3[] originalVertices;

        public void Init(FaceCalibration face, GameObject gameObject)
        {
            mFace = face;
            mGameObject = gameObject;
            mRenderer = null;

            if (mGameObject)
            {
                Renderer[] renderers = mGameObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer is SkinnedMeshRenderer)
                    {
                        originalMesh = (renderer as SkinnedMeshRenderer).sharedMesh;
                    }else if (renderer is MeshRenderer)
                    {
                        originalMesh = (renderer as MeshRenderer).GetComponent<MeshFilter>().sharedMesh;
                    }

                    if (originalMesh == mFace.customMesh)
                    {
                        mRenderer = renderer;
                        clonedMesh = UnityEngine.Object.Instantiate(originalMesh);
                        originalVertices = clonedMesh.vertices.Clone() as Vector3[];
                        if (mRenderer is SkinnedMeshRenderer)
                        {
                            (mRenderer as SkinnedMeshRenderer).sharedMesh = clonedMesh;
                        }else if (mRenderer is MeshRenderer)
                        {
                            (mRenderer as MeshRenderer).GetComponent<MeshFilter>().sharedMesh = clonedMesh;
                        }
                        break;
                    }
                }
            }
        }

        public void Uninit()
        {
            if (mRenderer)
            {
                if (mRenderer is SkinnedMeshRenderer)
                {
                    (mRenderer as SkinnedMeshRenderer).sharedMesh = originalMesh;
                }else if (mRenderer is MeshRenderer)
                {
                    (mRenderer as MeshRenderer).GetComponent<MeshFilter>().sharedMesh = originalMesh;
                }
            }
        }

        public void UpdateFace(Dictionary<int, Vector3> cachedOffset, FaceMeshMapAsset faceMeshMapAsset)
        {
            if (faceMeshMapAsset == null || originalVertices == null || clonedMesh == null ||
                originalVertices.Length != faceMeshMapAsset.indexMap.Length)
            {
                return;
            }

            Vector3[] clonedVertices = originalVertices.Clone() as Vector3[];
            for (int i = 0; i < faceMeshMapAsset.indexMap.Length; i++)
            {
                int mapIndex = faceMeshMapAsset.indexMap[i];
                if (mapIndex < 0)
                {
                    continue;
                }

                if (cachedOffset.ContainsKey(mapIndex))
                {
                    Vector3 offset = cachedOffset[mapIndex];
                    clonedVertices[i] += offset;
                }
            }
            clonedMesh.vertices = clonedVertices;

        }

        

        public void UpdateFace(Dictionary<int, Vector3> cachedOffset, FaceBoneWeightsAsset faceBoneWeightsAsset)
        {
            if (faceBoneWeightsAsset == null || originalVertices == null || clonedMesh == null ||
                originalVertices.Length != faceBoneWeightsAsset.boneWeights.Length)
            {
                return;
            }


            HashSet<int> MouthUpper = new HashSet<int>();
            if (mFace)
            {
                MouthUpper = mFace.regions[(int)FaceRegionType.MouthUpper].vIDs.ToHashSet();
            }

            Vector3[] clonedVertices = originalVertices.Clone() as Vector3[];
            for (int i = 0; i < faceBoneWeightsAsset.boneWeights.Length; i++)
            {
                float faceWeight = 1;
                if (MouthUpper.Contains(i))
                {
                    faceWeight = 0.5f;
                }
                int boneIndex = faceBoneWeightsAsset.boneWeights[i].boneIndex0;
                if (boneIndex >= 0)
                {
                    int mapIndex = faceBoneWeightsAsset.boneIndex[boneIndex];
                    if (mapIndex >= 0)
                    {
                        float weight = faceBoneWeightsAsset.boneWeights[i].weight0 * faceWeight;
                        
                        if (cachedOffset.ContainsKey(mapIndex))
                        {
                            Vector3 offset = cachedOffset[mapIndex];
                            clonedVertices[i] += offset * weight;
                        }
                    }
                }
                
                boneIndex = faceBoneWeightsAsset.boneWeights[i].boneIndex1;
                if (boneIndex >= 0)
                {
                    int mapIndex = faceBoneWeightsAsset.boneIndex[boneIndex];
                    if (mapIndex >= 0)
                    {
                        float weight = faceBoneWeightsAsset.boneWeights[i].weight1 * faceWeight;
                        
                        if (cachedOffset.ContainsKey(mapIndex))
                        {
                            Vector3 offset = cachedOffset[mapIndex];
                            clonedVertices[i] += offset * weight;
                        }
                    }
                }
                
                boneIndex = faceBoneWeightsAsset.boneWeights[i].boneIndex2;
                if (boneIndex >= 0)
                {
                    int mapIndex = faceBoneWeightsAsset.boneIndex[boneIndex];
                    if (mapIndex >= 0)
                    {
                        float weight = faceBoneWeightsAsset.boneWeights[i].weight2 * faceWeight;
                        
                        if (cachedOffset.ContainsKey(mapIndex))
                        {
                            Vector3 offset = cachedOffset[mapIndex];
                            clonedVertices[i] += offset * weight;
                        }
                    }
                }
                
                boneIndex = faceBoneWeightsAsset.boneWeights[i].boneIndex3;
                if (boneIndex >= 0)
                {
                    int mapIndex = faceBoneWeightsAsset.boneIndex[boneIndex];
                    if (mapIndex >= 0)
                    {
                        float weight = faceBoneWeightsAsset.boneWeights[i].weight3 * faceWeight;
                        
                        if (cachedOffset.ContainsKey(mapIndex))
                        {
                            Vector3 offset = cachedOffset[mapIndex];
                            clonedVertices[i] += offset * weight;
                        }
                    }
                }

            }
            clonedMesh.vertices = clonedVertices;
            
            
            if (mFace)
            {
                int vID1 = mFace.landmarks[(int)FaceLandmarkType.F90].vID;
                int vID2 = mFace.landmarks[(int)FaceLandmarkType.F94].vID;

                if (vID2 >= 0)
                {
                    float dy = originalMesh.vertices[vID2].y - clonedMesh.vertices[vID2].y;
                    float weight = dy / 0.015f;
                    UpdateFaceBS(weight);
                }
            }
        }

        public void UpdateFaceBS(float cachedBlendShapeWeight)
        {
            
            if (mRenderer is SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = (mRenderer as SkinnedMeshRenderer);
                int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("oral.oral_cavity");
                // 设置 blendshape
                if (blendShapeIndex >= 0)
                {
                    skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, 100 * cachedBlendShapeWeight);
                }
            }
        }
    }
    
    public class FaceCalibrationWrapEditor : FaceCalibrationSubEditor
    {
        private FaceObjectVertAsset objectVertAsset;
        private FaceMeshMapAsset faceMeshMapAsset1;
        private FaceMeshMapAsset faceMeshMapAsset2;
        private FaceBoneWeightsAsset faceBoneWeightsAsset2;
        

        private FaceCalibration mFace1;
        private FaceCalibration mFace2;
        private GameObject mGameObject1;
        private GameObject mGameObject2;

        private FaceVertAnimator mAnimator1;
        private FaceVertAnimator mAnimator2;

        
        public override void Enable(UnityEditor.Editor inspector)
        {
            mAnimator1 = new FaceVertAnimator();
            mAnimator2 = new FaceVertAnimator();
            mAnimator1.Init(mFace1, mGameObject1);
            mAnimator2.Init(mFace2, mGameObject2);
            
            base.Enable(inspector);
            FaceVertAnimPlayerGUI.OpenPlayer();
            EditorApplication.update += UpdateDelegate;
        }

        public override void Disable()
        {
            EditorApplication.update -= UpdateDelegate;
            FaceVertAnimPlayerGUI.ClosePlayer();
            base.Disable();
            
            mAnimator1.Uninit();
            mAnimator2.Uninit();
            mAnimator1 = null;
            mAnimator2 = null;
        }

        void UpdateDelegate()
        {
            FaceVertAnimPlayerGUI.UpdatePlayer();
            if (FaceVertAnimPlayerGUI.cachedDataChanged)
            {
                mAnimator1.UpdateFace(FaceVertAnimPlayerGUI.cachedOffset, faceMeshMapAsset1);
                if (faceMeshMapAsset2)
                {
                    mAnimator2.UpdateFace(FaceVertAnimPlayerGUI.cachedOffset, faceMeshMapAsset2);
                    //mAnimator2.UpdateFaceBS(FaceVertAnimPlayerGUI.cachedBlendShapeWeight);
                }else if (faceBoneWeightsAsset2)
                {
                    mAnimator2.UpdateFace(FaceVertAnimPlayerGUI.cachedOffset, faceBoneWeightsAsset2);
                    //mAnimator2.UpdateFaceBS(FaceVertAnimPlayerGUI.cachedBlendShapeWeight);
                }
                FaceVertAnimPlayerGUI.cachedDataChanged = false;
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            objectVertAsset = (FaceObjectVertAsset)EditorGUILayout.ObjectField(new GUIContent("FLAME_Object", "flame object file"), objectVertAsset, typeof(FaceObjectVertAsset), false);
            faceMeshMapAsset1 = (FaceMeshMapAsset)EditorGUILayout.ObjectField(new GUIContent("face1Map", "FLAME to face1 MapAsset"), faceMeshMapAsset1, typeof(FaceMeshMapAsset), false);
            faceMeshMapAsset2 = (FaceMeshMapAsset)EditorGUILayout.ObjectField(new GUIContent("face2Map", "FLAME to face2 MapAsset"), faceMeshMapAsset2, typeof(FaceMeshMapAsset), false);
            faceBoneWeightsAsset2 = (FaceBoneWeightsAsset)EditorGUILayout.ObjectField(new GUIContent("face2Bone", "face2 bone weight"), faceBoneWeightsAsset2, typeof(FaceBoneWeightsAsset), false);
            
            if (EditorGUI.EndChangeCheck())
            {
                
            }
            
            FaceVertAnimPlayerGUI.DrawPlayer();
            base.OnInspectorGUI();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("BuildMapData")))
            {
                string mapFile = EditorUtility.SaveFilePanelInProject("Save Map File", "map", "map", "Save Map File");
                if (mapFile.Length > 0)
                {
                    FaceMeshMapAsset mapAsset = BuildMapData();
                    mapAsset.SaveMapFile(mapFile);
                }
            }
            if (GUILayout.Button(new GUIContent("BuildBoneData")))
            {
                string boneFile = EditorUtility.SaveFilePanelInProject("Save Bone File", "bone", "bone", "Save Bone File");
                if (boneFile.Length > 0)
                {
                    FaceBoneWeightsAsset boneAsset = BuildBoneData();
                    boneAsset.SaveBoneFile(boneFile);
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button(new GUIContent("LoggingData")))
            {
                LoggingData();
            }
            
            GUILayout.EndHorizontal();
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();
        }

        public void SetFaces(FaceCalibration face1, FaceCalibration face2, GameObject gameObject1, GameObject gameObject2)
        {
            mFace1 = face1;
            mFace2 = face2;
            mGameObject1 = gameObject1;
            mGameObject2 = gameObject2;
        }

        int[] BuildMapData(Vector3[] meshVertices, Vector3[] objVertices)
        {
            int[] mapData = new int[meshVertices.Length];

            var vertices_kdTree = new KdTree<float, int>(3, new FloatMath());
            for (int i = 0; i < objVertices.Length; i++)
            {
                vertices_kdTree.Add(new[] {objVertices[i].x, objVertices[i].y, objVertices[i].z}, i);
            }
            
            for (int i = 0; i < meshVertices.Length; i++)
            {
                var nearestNodes = vertices_kdTree.GetNearestNeighbours(new[] { meshVertices[i].x, meshVertices[i].y, meshVertices[i].z }, 1);
                mapData[i] = -1;
                foreach (var node in nearestNodes)
                {
                    mapData[i] = node.Value;
                }
            }
            return mapData;
        }
        
        struct FaceLandmarkData
        {
            public int vID;
            public Vector3 position;
            public Vector3 offset;
            
            public Vector2 uv;
            public Vector2 uvoffset;
        }

        struct FaceVertexToLandmarkData
        {
            public int landmarkId;
            public float distance;
            public float weight;
        }
        
        struct FaceVertexData
        {
            public int vID;
            public Vector3 position;
            public Vector3 offset;
            
            
            public Vector2 uv;
            public Vector2 uvoffset;
            
            public List<FaceVertexToLandmarkData> nearbyLandmarks;
        }
        
        public FaceMeshMapAsset BuildMapData()
        {
            if (mFace1 == null || mFace2 == null)
            {
                return null;
            }
            
            Vector3[] objVertices = objectVertAsset.vertices.ToArray();
            Vector3[] vertices_face1 = mFace1.customMesh.vertices;
            Vector3[] vertices_face2 = mFace2.customMesh.vertices;
            Vector2[] uv_face1 = mFace1.customMesh.uv;
            Vector2[] uv_face2 = mFace2.customMesh.uv;

            // 要求 face1 为 FLAME 的mesh，与 obj 进行映射
            // TODO: 此处可以预计算好 map 文件，不需要每次都计算
            int[] mapData = BuildMapData(mFace1.customMesh.vertices, objVertices);
            
            // 将 mesh 的 uv 信息映射到 obj 上，注 obj 可能一个点有多个 uv，直接取最后一个覆盖
            Vector2[] objUV = new Vector2[objVertices.Length];
            for (int i = 0; i < mapData.Length; i++)
            {
                int objIndex = mapData[i];
                objUV[objIndex] = uv_face1[i];
            }
            
            int landmarkCount = mFace1.landmarks.Length;
            var landmark_kdTree = new KdTree<float, int>(5, new FloatMath());
            Dictionary<int, int> vertexToLandmark_face1 = new Dictionary<int, int>();
            
            // 先计算 landmark 的数据，作为控制点，为后续传输使用
            FaceLandmarkData[] landmark_face1 = new FaceLandmarkData[landmarkCount];
            for (int i = 0; i < landmarkCount; i++)
            {
                int vID1 = mFace1.landmarks[i].vID;
                int vID2 = mFace2.landmarks[i].vID;
                if (vID1 == -1 || vID2 == -1)
                {
                    landmark_face1[i].vID = -1;
                    continue;
                }
                int obj_vID = mapData[vID1];
                
                if (obj_vID >= 0)
                {
                    landmark_face1[i].vID = obj_vID;
                    landmark_face1[i].position = objVertices[obj_vID];
                    landmark_face1[i].offset = vertices_face2[vID2] - objVertices[obj_vID];
                    
                    landmark_face1[i].uv = uv_face1[vID1];
                    landmark_face1[i].uvoffset = uv_face2[vID2] - uv_face1[vID1];
                    
                    landmark_kdTree.Add(new[]
                    {
                        landmark_face1[i].position.x, landmark_face1[i].position.y, landmark_face1[i].position.z,
                        landmark_face1[i].uv.x, landmark_face1[i].uv.y
                    }, i);
                    vertexToLandmark_face1.Add(obj_vID, i);
                }
            }

            int[] mapData2 = new int[vertices_face2.Length];
            for (int i = 0; i < mapData2.Length; i++)
            {
                mapData2[i] = -1;
            }


            // 仅计算 region 范围内的顶点映射
            int regionCount = mFace1.regions.Length;
            for (int r = 0; r < regionCount; r++)
            {
                FaceRegion faceRegion1 = mFace1.regions[r];
                FaceRegion faceRegion2 = mFace2.regions[r];
                if (faceRegion1.vIDs.Length <= 0 || faceRegion2.vIDs.Length <= 0)
                {
                    continue;
                }

                HashSet<int> obj_vIDs = new HashSet<int>();
                foreach (var mesh_vID in faceRegion1.vIDs)
                {
                    if (mesh_vID == -1)
                    {
                        continue;
                    }
                    int obj_vID = mapData[mesh_vID];
                    if (obj_vID == -1)
                    {
                        continue;
                    }
                    obj_vIDs.Add(obj_vID);
                }

                int[] vIDs = obj_vIDs.ToArray();
                int vertexCount = vIDs.Length;

                // 计算 obj 中顶点与 landmark 之间的关系，以便后续通过 landmark 进行传输位置和uv 
                var vertices_kdTree = new KdTree<float, int>(5, new FloatMath());
                FaceVertexData[] vertex_face1 = new FaceVertexData[vertexCount];
                const int n = 5;
                for (int i = 0; i < vertexCount; i++)
                {
                    int vID = vIDs[i];
                    if (vID == -1)
                    {
                        continue;
                    }
                    
                    vertex_face1[i].vID = vID;
                    vertex_face1[i].position = objVertices[vID];
                    vertex_face1[i].nearbyLandmarks = new List<FaceVertexToLandmarkData>();

                    vertex_face1[i].uv = objUV[vID];
                    
                    // 计算每个点与 landmark 之间的距离，作为权重参考
                    float allWeight = 0;
                    if (vertexToLandmark_face1.ContainsKey(vID))
                    {
                        // 该点是landmark
                        int landmarkId = vertexToLandmark_face1[vID];
                        vertex_face1[i].nearbyLandmarks.Add(new FaceVertexToLandmarkData()
                        {
                            landmarkId = landmarkId,
                            distance = 0,
                            weight = 1.0f
                        });
                        allWeight = 1.0f;
                    }
                    else
                    {
                        // 取最临近的n个点
                        var nearestNodes = landmark_kdTree.GetNearestNeighbours(new[]
                        {
                            vertex_face1[i].position.x, vertex_face1[i].position.y, vertex_face1[i].position.z,
                            vertex_face1[i].uv.x, vertex_face1[i].uv.y
                        }, n);

                        foreach (var node in nearestNodes)
                        {
                            FaceLandmarkData landmark = landmark_face1[node.Value];
                            float distance = (landmark.position - vertex_face1[i].position).magnitude + (landmark.uv - vertex_face1[i].uv).magnitude;
                            float weight = 1.0f / distance;
                            allWeight += weight;
                        
                            vertex_face1[i].nearbyLandmarks.Add(new FaceVertexToLandmarkData()
                            {
                                landmarkId = node.Value,
                                distance = distance,
                                weight = weight
                            });
                        }
                    }


                    // 通过周围的控制点 landmark 计算出该点的偏移量
                    Vector3 offset = Vector3.zero;
                    Vector2 uvoffset = Vector2.zero;
                    for (int j = 0; j < vertex_face1[i].nearbyLandmarks.Count; j++)
                    {
                        FaceVertexToLandmarkData data = vertex_face1[i].nearbyLandmarks[j];
                        data.weight /= allWeight;
                        vertex_face1[i].nearbyLandmarks[j] = data;

                        offset += landmark_face1[data.landmarkId].offset * data.weight;
                        uvoffset += landmark_face1[data.landmarkId].uvoffset * data.weight;
                    }
                    vertex_face1[i].offset = offset;
                    vertex_face1[i].uvoffset = uvoffset;
                    
                    // 将顶点数据加入到 kdTree 中，以便后续查找
                    Vector3 pos = vertex_face1[i].position + vertex_face1[i].offset;
                    Vector2 uv = vertex_face1[i].uv + vertex_face1[i].uvoffset;
                    vertices_kdTree.Add(new[]
                    {
                        pos.x, pos.y, pos.z,
                        uv.x, uv.y
                    }, i);
                }

                // 此时 vertices_kdTree 中已经是 Face1 的顶点通过控制点传输到目标 Face2 附近了
                // 只需要将 Face2 的顶点通过查找最近的 Face1 的点，即可建立 Face2 的顶点与 Face1 的顶点的映射关系表
                int[] vIDs2 =  faceRegion2.vIDs.ToArray();
                
                for (int i = 0; i < vIDs2.Length; i++)
                {
                    int vID2 = vIDs2[i];
                    if (vID2 == -1)
                    {
                        continue;
                    }
                    Vector3 pos = vertices_face2[vID2];
                    Vector2 uv = uv_face2[vID2];
                    
                    // 取最临近的n个点
                    var nearestNodes = vertices_kdTree.GetNearestNeighbours(new[]
                    {
                        pos.x, pos.y, pos.z,
                        uv.x, uv.y
                    }, 1);

                    const float minDis = 0.1f;
                    const float minUvDis = 0.1f;
                    foreach (var node in nearestNodes)
                    {
                        int v = node.Value;
                        
                        Vector3 pos2 = vertex_face1[v].position + vertex_face1[v].offset;
                        float dis = (pos2 - pos).magnitude;

                        Vector2 uv2 = vertex_face1[v].uv + vertex_face1[v].uvoffset;
                        float uvdis = (uv2 - uv).magnitude;
                        if (dis < minDis && uvdis < minUvDis)
                        {
                            // 建立映射关系
                            mapData2[vID2] = vertex_face1[v].vID;
                        }
                    }
                }

            }
            
            
            FaceMeshMapAsset mapAsset = ScriptableObject.CreateInstance<FaceMeshMapAsset>();
            mapAsset.numSubMesh = 1;
            mapAsset.indexMap = mapData2;
            return mapAsset;
        }
        
        
        private FaceBoneWeightsAsset BuildBoneData()
        {
            if (mFace1 == null || mFace2 == null)
            {
                return null;
            }
            
            Vector3[] objVertices = objectVertAsset.vertices.ToArray();
            Vector3[] vertices_face1 = mFace1.customMesh.vertices;
            Vector3[] vertices_face2 = mFace2.customMesh.vertices;
            Vector2[] uv_face1 = mFace1.customMesh.uv;
            Vector2[] uv_face2 = mFace2.customMesh.uv;

            // 要求 face1 为 FLAME 的mesh，与 obj 进行映射
            // TODO: 此处可以预计算好 map 文件，不需要每次都计算
            int[] mapData = BuildMapData(mFace1.customMesh.vertices, objVertices);
            
            // 将 mesh 的 uv 信息映射到 obj 上，注 obj 可能一个点有多个 uv，直接取最后一个覆盖
            Vector2[] objUV = new Vector2[objVertices.Length];
            for (int i = 0; i < mapData.Length; i++)
            {
                int objIndex = mapData[i];
                objUV[objIndex] = uv_face1[i];
            }
            
            int landmarkCount = mFace2.landmarks.Length;
            Dictionary<int, int> vertexToLandmark_face2 = new Dictionary<int, int>();
            
            int[] mapData2 = new int[landmarkCount];
            
            // 先计算 landmark 的数据，作为控制点，为后续传输使用
            FaceLandmarkData[] landmark_face2 = new FaceLandmarkData[landmarkCount];
            for (int i = 0; i < landmarkCount; i++)
            {
                mapData2[i] = -1;
                int vID1 = mFace1.landmarks[i].vID;
                int vID2 = mFace2.landmarks[i].vID;
                if (vID1 == -1 || vID2 == -1)
                {
                    landmark_face2[i].vID = -1;
                    continue;
                }
                int obj_vID = mapData[vID1];
                
                if (obj_vID >= 0)
                {
                    mapData2[i] = obj_vID;
                    landmark_face2[i].vID = vID2;
                    landmark_face2[i].position = vertices_face2[vID2];
                    //landmark_face1[i].offset = vertices_face2[vID2] - objVertices[obj_vID];
                    
                    landmark_face2[i].uv = uv_face2[vID2];
                    //landmark_face1[i].uvoffset = uv_face2[vID2] - uv_face1[vID1];
                    
                    vertexToLandmark_face2.Add(vID2, i);
                }
            }


            BoneWeight defaultBoneWeight = new BoneWeight();
            defaultBoneWeight.boneIndex0 = -1;
            defaultBoneWeight.weight0 = 0;
            defaultBoneWeight.boneIndex1 = -1;
            defaultBoneWeight.weight1 = 0;
            defaultBoneWeight.boneIndex2 = -1;
            defaultBoneWeight.weight2 = 0;
            defaultBoneWeight.boneIndex3 = -1;
            defaultBoneWeight.weight3 = 0;
            
            BoneWeight[] boneWeights_face2 = new BoneWeight[vertices_face2.Length];
            for (int i = 0; i < boneWeights_face2.Length; i++)
            {
                boneWeights_face2[i] = defaultBoneWeight;
            }

            // 仅计算 region 范围内的顶点映射
            int regionCount = mFace1.regions.Length;
            for (int r = 0; r < regionCount; r++)
            {
                FaceRegion faceRegion2 = mFace2.regions[r];
                if (faceRegion2.vIDs.Length <= 0)
                {
                    continue;
                }

                
                int[] vIDs = faceRegion2.vIDs;
                int vertexCount = vIDs.Length;

                // 计算 obj 中顶点与 landmark 之间的关系，以便后续通过 landmark 进行传输位置和uv 

                var landmark_kdTree = new KdTree<float, int>(5, new FloatMath());
                for (int i = 0; i < vertexCount; i++)
                {
                    int vID = vIDs[i];
                    if (vertexToLandmark_face2.ContainsKey(vID))
                    {
                        int lID = vertexToLandmark_face2[vID];
                        landmark_kdTree.Add(new[]
                        {
                            landmark_face2[lID].position.x, landmark_face2[lID].position.y, landmark_face2[lID].position.z,
                            landmark_face2[lID].uv.x, landmark_face2[lID].uv.y
                        }, lID);
                    }
                }
                
                FaceVertexData[] vertex_face2 = new FaceVertexData[vertexCount];
                const int n = 4;
                for (int i = 0; i < vertexCount; i++)
                {
                    int vID = vIDs[i];
                    if (vID == -1)
                    {
                        continue;
                    }
                    
                    vertex_face2[i].vID = vID;
                    vertex_face2[i].position = vertices_face2[vID];
                    vertex_face2[i].nearbyLandmarks = new List<FaceVertexToLandmarkData>();

                    vertex_face2[i].uv = uv_face2[vID];
                    
                    // 计算每个点与 landmark 之间的距离，作为权重参考
                    float allWeight = 0;
                    if (vertexToLandmark_face2.ContainsKey(vID))
                    {
                        // 该点是landmark
                        int landmarkId = vertexToLandmark_face2[vID];
                        vertex_face2[i].nearbyLandmarks.Add(new FaceVertexToLandmarkData()
                        {
                            landmarkId = landmarkId,
                            distance = 0,
                            weight = 1.0f
                        });
                        allWeight = 1.0f;
                    }
                    else
                    {
                        // 取最临近的n个点
                        var nearestNodes = landmark_kdTree.GetNearestNeighbours(new[]
                        {
                            vertex_face2[i].position.x, vertex_face2[i].position.y, vertex_face2[i].position.z,
                            vertex_face2[i].uv.x, vertex_face2[i].uv.y
                        }, n);

                        foreach (var node in nearestNodes)
                        {
                            FaceLandmarkData landmark = landmark_face2[node.Value];
                            float distance = (landmark.position - vertex_face2[i].position).magnitude + (landmark.uv - vertex_face2[i].uv).magnitude;
                            float weight = 1.0f / distance;
                            allWeight += weight;
                        
                            vertex_face2[i].nearbyLandmarks.Add(new FaceVertexToLandmarkData()
                            {
                                landmarkId = node.Value,
                                distance = distance,
                                weight = weight
                            });
                        }
                    }


                    // 通过周围的控制点 landmark 计算出该点的偏移量
                    //Vector3 offset = Vector3.zero;
                    //Vector2 uvoffset = Vector2.zero;
                    
                    BoneWeight boneWeight = new BoneWeight();
                    int boneCount = vertex_face2[i].nearbyLandmarks.Count;
                    for (int j = 0; j < 4; j++)
                    {
                        int boneIndex = -1;
                        float weight = 0;

                        if (j < boneCount)
                        {
                            FaceVertexToLandmarkData data = vertex_face2[i].nearbyLandmarks[j];
                            data.weight /= allWeight;
                            vertex_face2[i].nearbyLandmarks[j] = data;
                            
                            boneIndex = data.landmarkId;
                            weight = data.weight;
                        }
                        

                        switch (j)
                        {
                            case 0:
                                boneWeight.boneIndex0 = boneIndex;
                                boneWeight.weight0 = weight;
                                break;
                            case 1:
                                boneWeight.boneIndex1 = boneIndex;
                                boneWeight.weight1 = weight;
                                break;
                            case 2:
                                boneWeight.boneIndex2 = boneIndex;
                                boneWeight.weight2 = weight;
                                break;
                            case 3:
                                boneWeight.boneIndex3 = boneIndex;
                                boneWeight.weight3 = weight;
                                break;
                            
                        }
                    }
                    
                    boneWeights_face2[vID] = boneWeight;
                }

            }
            
            FaceBoneWeightsAsset boneAsset = ScriptableObject.CreateInstance<FaceBoneWeightsAsset>();
            boneAsset.numSubMesh = 1;
            boneAsset.boneIndex = mapData2;
            boneAsset.boneWeights = boneWeights_face2;
            return boneAsset;
        }

        private void LoggingData()
        {
            if (mFace1 == null)
            {
                return;
            }
            
            Vector3[] objVertices = objectVertAsset.vertices.ToArray();
            
            // 要求 face1 为 FLAME 的mesh，与 obj 进行映射
            // TODO: 此处可以预计算好 map 文件，不需要每次都计算
            int[] mapData = BuildMapData(mFace1.customMesh.vertices, objVertices);
            
            int landmarkCount = mFace1.landmarks.Length;
            int[] vIDs = new int[landmarkCount];
            
            string log = "";
            for (int i = 0; i < landmarkCount; i++)
            {
                vIDs[i] = mapData[mFace1.landmarks[i].vID];
                
                log += vIDs[i] + ",";
            }
            // log landmark
            Debug.Log(log);

        }

    }
}