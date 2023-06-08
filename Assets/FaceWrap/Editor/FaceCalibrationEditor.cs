using System;
using System.Collections.Generic;
using System.Reflection;
using FaceWrap.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace FaceWrap.Editor
{

    [CustomEditor(typeof(FaceCalibration))]
    public class FaceCalibrationEditor : UnityEditor.Editor
    {
        private static Styles s_Styles;
        protected int m_TabIndex;
        
        internal FaceCalibrationStage m_Stage;
        internal GameObject m_GameObject;
        private EditMode m_EditMode = EditMode.NotEditing;
        internal bool m_CameFromImportSettings = false;
        
        internal static bool s_EditImmediatelyOnNextOpen;
        internal SerializedObject m_SerializedAssetImporter = (SerializedObject) null;
        
        
        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();
                return s_Styles;
            }
        }

        public SerializedObject serializedAssetImporter
        {
            get
            {
                if (this.m_SerializedAssetImporter == null)
                    this.m_SerializedAssetImporter = CreateSerializedImporterForTarget(this.target);
                return this.m_SerializedAssetImporter;
            }
        }

        internal FaceCalibration face => this.target as FaceCalibration;
        private FaceCalibrationUVEditor m_UVEditor;
        
        protected FaceCalibrationSubEditor editor
        {
            get
            {
                switch (this.m_TabIndex)
                {
                    default:
                        return (FaceCalibrationSubEditor) this.m_UVEditor;
                }
            }
            set
            {
                switch (this.m_TabIndex)
                {
                    default:
                        this.m_UVEditor = value as FaceCalibrationUVEditor;
                        break;
                }
            }
        }
        
        private static SerializedObject CreateSerializedImporterForTarget(UnityEngine.Object target)
        {
            SerializedObject importerForTarget = (SerializedObject) null;
            AssetImporter atPath = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(target));
            if ((UnityEngine.Object) atPath != (UnityEngine.Object) null)
                importerForTarget = new SerializedObject((UnityEngine.Object) atPath);
            return importerForTarget;
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += new Action<SceneView>(this.OnSceneGUI);
            
            if (this.m_EditMode == EditMode.Editing)
            {
                //this.m_ModelBones = AvatarSetupTool.GetModelBones(this.m_GameObject.transform, false, (AvatarSetupTool.BoneWrapper[]) null);
                this.editor.Enable(this);
            }
            else
            {
                if (this.m_EditMode != EditMode.NotEditing)
                    return;
                this.editor = null;
                if (s_EditImmediatelyOnNextOpen)
                {
                    this.m_CameFromImportSettings = true;
                    s_EditImmediatelyOnNextOpen = false;
                }
            }
            
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= new Action<SceneView>(this.OnSceneGUI);
            if (this.m_EditMode == EditMode.Editing)
                this.editor.Disable();
            
            if (this.m_SerializedAssetImporter == null)
                return;
            // this.m_SerializedAssetImporter.Cache(this.GetInstanceID());
            this.m_SerializedAssetImporter = (SerializedObject) null;
        }

        private void OnDestroy()
        {
            DestroyEditor();
        }

        private void SelectAsset() => Selection.activeObject = !this.m_CameFromImportSettings ? this.target : AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(this.target));

        protected void CreateEditor()
        {
            switch (this.m_TabIndex)
            {
                default:
                    FaceCalibrationUVEditor faceCalibrationUvEditor = ScriptableObject.CreateInstance<FaceCalibrationUVEditor>();
                    faceCalibrationUvEditor.SetFace(face, m_GameObject);
                    
                    this.editor = faceCalibrationUvEditor;
                    break;
            }
            this.editor.hideFlags = HideFlags.HideAndDontSave;
            this.editor.Enable(this);
        }

        protected void DestroyEditor()
        {
            if (this.editor)
            {
                this.editor.Disable();
                DestroyImmediate(this.editor);
                this.editor = null;
            }
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = true;
            using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorFullWidthMargins, Array.Empty<GUILayoutOption>()))
            {
                if (this.m_EditMode == EditMode.Editing)
                    this.EditingGUI();
                else if (!this.m_CameFromImportSettings)
                    this.EditButtonGUI();
            }
        }

        private void EditButtonGUI()
        {
            if ((UnityEngine.Object) this.face == (UnityEngine.Object) null )
                return;
            using (new EditorGUILayout.HorizontalScope(Array.Empty<GUILayoutOption>()))
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(styles.editFace, GUILayout.Width(120f)))
                {
                    this.SwitchToEditMode();
                    GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
            }
            base.OnInspectorGUI();
        }
        
        private void EditingGUI()
        {
            using (new EditorGUILayout.HorizontalScope(Array.Empty<GUILayoutOption>()))
            {
                GUILayout.FlexibleSpace();
                int tabIndex = this.m_TabIndex;
                bool enabled = GUI.enabled;
                GUI.enabled = !((UnityEngine.Object) this.face == (UnityEngine.Object) null);
                int num = GUILayout.Toolbar(tabIndex, styles.tabs, (GUIStyle) "LargeButton", GUI.ToolbarButtonSize.FitToContents);
                GUI.enabled = enabled;
                if (num != this.m_TabIndex)
                {
                    this.DestroyEditor();
                    if ((UnityEngine.Object) this.face != (UnityEngine.Object) null)
                        this.m_TabIndex = num;
                    this.CreateEditor();
                }
                GUILayout.FlexibleSpace();
            }
            this.editor.OnInspectorGUI();
        }
        
        public void OnSceneGUI(SceneView view)
        {
            if (this.m_EditMode != EditMode.Editing)
                return;
            this.editor.OnSceneGUI();
        }

        internal void SwitchToEditMode()
        {
            this.ChangeInspectorLock(true);
            m_Stage = FaceCalibrationStage.CreateStage(AssetDatabase.GetAssetPath(face.faceMesh), face, CleanupEditor);
            
            StageUtility.GoToStage((Stage) m_Stage, true);
            this.m_EditMode = EditMode.Starting;
            this.m_GameObject = m_Stage.gameObject;
            this.CreateEditor();
            this.m_EditMode = EditMode.Editing;
        }
        
        internal void SwitchToAssetMode()
        {            
            this.m_EditMode = EditMode.Stopping;
            this.DestroyEditor();

            this.m_Stage = null;
            this.m_GameObject = (GameObject) null;
            StageUtility.GoToMainStage();
            this.ChangeInspectorLock(false);
        }

        internal void CleanupEditor()
        {
            SwitchToAssetMode();
            
            bool selectAvatarAsset = StageUtility.GetCurrentStageHandle() == StageUtility.GetMainStageHandle();

            EditorApplication.CallbackFunction CleanUpOnDestroy = (EditorApplication.CallbackFunction) null;
            CleanUpOnDestroy = (EditorApplication.CallbackFunction) (() =>
            {
                if (selectAvatarAsset)
                    this.SelectAsset();
                if (!this.m_CameFromImportSettings)
                    this.m_EditMode = EditMode.NotEditing;
                EditorApplication.update -= CleanUpOnDestroy;
                Repaint();
            });
            EditorApplication.update += CleanUpOnDestroy;
            //this.m_ModelBones = (Dictionary<Transform, bool>) null;
        }
        
        private bool IsPartOfLockedInspector()
        {
            if (ActiveEditorTracker.sharedTracker == null)
            {
                return false;
            }
            foreach (UnityEngine.Object activeEditor in ActiveEditorTracker.sharedTracker.activeEditors)
            {
                if (activeEditor == (UnityEngine.Object) this && ActiveEditorTracker.sharedTracker.isLocked)
                    return true;
            }
            
            return false;
        }

        private void ChangeInspectorLock(bool locked)
        {
            if (ActiveEditorTracker.sharedTracker == null)
            {
                return;
            }
            
            foreach (UnityEngine.Object activeEditor in ActiveEditorTracker.sharedTracker.activeEditors)
            {
                if (activeEditor == (UnityEngine.Object) this)
                {
                    ActiveEditorTracker.sharedTracker.isLocked = locked;
                }
            }
        }

        private class Styles
        {
            public GUIContent[] tabs = new GUIContent[2]
            {
                EditorGUIUtility.TrTextContent("face UV1"),
                EditorGUIUtility.TrTextContent("face UV2")
            };
            public GUIContent editFace = EditorGUIUtility.TrTextContent("Configure face");
            public GUIContent reset = EditorGUIUtility.TrTextContent("Reset");
        }
        
        private enum EditMode
        {
            NotEditing,
            Starting,
            Editing,
            Stopping,
        }
    }
}