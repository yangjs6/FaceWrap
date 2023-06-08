
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
    public class FaceCalibrationStage : PreviewSceneStage
    {
        private FaceCalibration m_Face;
        private Action m_OnStageClosed;
        private string m_AssetPath;
        private GameObject m_GameObject;

        public override string assetPath => this.m_AssetPath;

        public GameObject gameObject => this.m_GameObject;
        
        internal static FaceCalibrationStage CreateStage(
            string assetPath,
            FaceCalibration face, Action onStageClosed)
        {
            FaceCalibrationStage instance = ScriptableObject.CreateInstance<FaceCalibrationStage>();
            instance.Init(assetPath, face, onStageClosed);
            return instance;
        }

        private void Init(string modelAssetPath, FaceCalibration face, Action onStageClosed)
        {
            this.m_AssetPath = modelAssetPath;
            this.m_Face = face;
            this.m_OnStageClosed = onStageClosed;
        }

        
        protected override bool OnOpenStage()
        {
            base.OnOpenStage();
            m_GameObject = FaceCalibrationEditorUtils.CreateGameObject(m_Face);
            if (m_GameObject)
            {
                SceneManager.MoveGameObjectToScene(m_GameObject, this.scene);
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
            Selection.activeObject = (UnityEngine.Object) this.m_GameObject;
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