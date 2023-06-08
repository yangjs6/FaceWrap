using System;
using FaceWrap.Runtime;
using UnityEditor;
using UnityEngine;

namespace FaceWrap.Editor
{
    public class FaceLandmarkComponent : MonoBehaviour
    {
        public FaceLandmark landmark;
        public Vector3 landmarkPosition;
        public float landmarkSize = 0;

        
        public void UpdateLandmark(int vID, Vector3 position, float size)
        {
            landmark.vID = vID;
            landmarkPosition = position;
            landmarkSize = size;
        }
        
        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (landmark.vID >= 0)
            {
                Vector3 pos = this.transform.TransformPoint(landmarkPosition);
                
                bool isSelected = this.gameObject == Selection.activeGameObject;
                Gizmos.color = isSelected ? Color.magenta : Color.green;
                Gizmos.DrawSphere(pos, 0.001f);
                
                string label = landmark.Name;
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label( pos, label );
            }
#endif
        }
    }
}