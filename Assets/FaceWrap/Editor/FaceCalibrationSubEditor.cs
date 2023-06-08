using System;
using FaceWrap.Runtime;
using UnityEngine;

namespace FaceWrap.Editor
{

    [Serializable]
    public class FaceCalibrationSubEditor : ScriptableObject
    {
        protected UnityEditor.Editor m_Inspector;

        public virtual void Enable(UnityEditor.Editor inspector)
        {
            this.m_Inspector = inspector;
        }

        public virtual void Disable()
        {
        }
        

        public virtual void OnInspectorGUI()
        {
        }

        public virtual void OnSceneGUI()
        {
        }

        public void Repaint()
        {
            m_Inspector.Repaint();
        }
        
    }
}