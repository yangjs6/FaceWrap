using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FaceWrap.Runtime
{
    [Serializable]
    public struct FaceLandmark
    {
        public FaceLandmarkType type;
        public int vID;

        public FaceLandmark(FaceLandmarkType t)
        {
            type = t;
            vID = -1;
        }

        public string Name
        {
            get { return type.ToString(); }
        }
    }
    
    [Serializable]
    public struct FaceCurve
    {
        public FaceCurveType type;
        public int[] vIDs;
        
        
        public FaceCurve(FaceCurveType t)
        {
            type = t;
            vIDs = new [] {-1,-1};
        }
    }
    
    
    [Serializable]
    public struct FaceRegion
    {
        public FaceRegionType type;
        public int[] vIDs;
        
        
        public FaceRegion(FaceRegionType t)
        {
            type = t;
            vIDs = new int[]{};
        }
    }
    
    [CreateAssetMenu(fileName = "FaceCalibration", menuName = "FaceWrap/FaceCalibration", order = 0)]
    public class FaceCalibration : ScriptableObject
    {
        public GameObject faceMesh;
        public Mesh customMesh;
        public int subMeshIndex = -1;
        public int uvIndex;
        public Texture2D customTexture;
        
        public FaceLandmark[] landmarks;
        public FaceCurve[] curves;
        public FaceRegion[] regions;
        
        [SerializeField] public Vector3 localPosition = Vector3.zero;
        [SerializeField] public Quaternion localRotation = Quaternion.identity;
        [SerializeField] public Vector3 localScale = Vector3.one;

        public FaceCalibration()
        {
            Reset();
        }

        public void UpdateLandmark(FaceLandmarkType type, int vID)
        {
            landmarks[(int) type].vID = vID;
        }
        
        public void UpdateCurve(FaceCurveType type, int[] vIDs)
        {
            curves[(int) type].vIDs = vIDs;
        }
        
        public void UpdateRegion(FaceRegionType type, HashSet<int> vIDs)
        {
            regions[(int) type].vIDs = vIDs.ToArray();
        }
        
        public void Reset()
        {
            int landmarkCount = (int)FaceLandmarkType.FaceLandmarkCount;
            landmarks = new FaceLandmark[landmarkCount];
            for (int i = 0; i < landmarkCount; i++)
            {
                landmarks[i] = new FaceLandmark((FaceLandmarkType) i);
            }
            
            int curveCount = (int)FaceCurveType.FaceCurveCount;
            curves = new FaceCurve[curveCount];
            for (int i = 0; i < curveCount; i++)
            {
                curves[i] = new FaceCurve((FaceCurveType) i);
            }
            
            int regionCount = (int)FaceRegionType.FaceRegionCount;
            regions = new FaceRegion[regionCount];
            for (int i = 0; i < regionCount; i++)
            {
                regions[i] = new FaceRegion((FaceRegionType) i);
            }
        }
    }
}