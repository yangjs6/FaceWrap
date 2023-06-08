
using System;
using System.Collections.Generic;
using System.IO;
using FaceWrap.Runtime;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace FaceWrap.Editor
{
    public class FaceCalibrationMapStage : PreviewSceneStage
    {
        private FaceCalibration m_Face1;
        private FaceCalibration m_Face2;
        internal GameObject m_GameObject1;
        internal GameObject m_GameObject2;
        private Action m_OnStageClosed;

        internal static FaceCalibrationMapStage CreateStage(
            FaceCalibration face1, FaceCalibration face2, Action onStageClosed)
        {
            FaceCalibrationMapStage instance = ScriptableObject.CreateInstance<FaceCalibrationMapStage>();
            instance.Init(face1, face2, onStageClosed);
            return instance;
        }

        private void Init(FaceCalibration face1, FaceCalibration face2, Action onStageClosed)
        {
            this.m_Face1 = face1;
            this.m_Face2 = face2;
            this.m_OnStageClosed = onStageClosed;
        }

        protected override bool OnOpenStage()
        {
            base.OnOpenStage();
            m_GameObject1 = FaceCalibrationEditorUtils.CreateGameObject(m_Face1);
            m_GameObject2 = FaceCalibrationEditorUtils.CreateGameObject(m_Face2);
            if (m_GameObject1 && m_GameObject2)
            {
                SceneManager.MoveGameObjectToScene(m_GameObject1, this.scene);
                SceneManager.MoveGameObjectToScene(m_GameObject2, this.scene);
            }

            return true;
        }

        protected override void OnCloseStage()
        {
            if (m_OnStageClosed != null)
            {
                m_OnStageClosed();
            }
            base.OnCloseStage();
        }

        protected override void OnFirstTimeOpenStageInSceneView(SceneView sceneView)
        {
            Selection.activeObject = null;
            sceneView.FrameSelected(false, true);
            sceneView.sceneViewState.showFlares = false;
            sceneView.sceneViewState.alwaysRefresh = false;
            sceneView.sceneViewState.showFog = false;
            sceneView.sceneViewState.showSkybox = false;
            sceneView.sceneViewState.showImageEffects = false;
            sceneView.sceneViewState.showParticleSystems = false;
            sceneView.sceneLighting = false;
        }
        
        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent(
                "Face Configuration");
        }
    }
}