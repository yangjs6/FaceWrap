using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using FaceWrap.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Macaron.UVViewer.Editor;
using Macaron.UVViewer.Editor.Internal;
using Macaron.UVViewer.Editor.Internal.ExtensionMethods;
using PlasticGui.Configuration.CloudEdition.Welcome;
using Unity.VisualScripting;
using Mesh = UnityEngine.Mesh;

namespace FaceWrap.Editor
{

    [Serializable]
    public partial class FaceCalibrationUVEditor : FaceCalibrationSubEditor
    {
        protected FaceCalibration m_Face;
        protected GameObject m_GameObject;
        SerializedObject m_FaceObject;
        

        public FaceLandmarkComponent GetLandmarkComponent(string landmarkName)
        {
            if (m_GameObject != null)
            {
                FaceLandmarkComponent[] landmarkComponents = m_GameObject.GetComponentsInChildren<FaceLandmarkComponent>();
                foreach (var landmarkComponent in landmarkComponents)
                {
                    if (landmarkComponent.gameObject.name == landmarkName)
                    {
                        return landmarkComponent;
                    }
                }
            }
            return null;
        }

        private Mesh _refPointMesh = null;
        private Mesh _refLineMesh = null;
        [SerializeField] private Mesh _faceCurveMesh;
        [SerializeField] private Mesh _faceLandmakrMesh;
        [SerializeField] private Mesh _faceRegionMesh;
        [SerializeField] private Mesh _selectingVertexMesh;
        [SerializeField] private Mesh _selectingLineMesh;
        [SerializeField] private Mesh _selectingRegionMesh;

        public void SetFace(FaceCalibration face, GameObject gameObject)
        {
            this.m_Face = face;
            this.m_GameObject = gameObject;
        }
        
        public override void Enable(UnityEditor.Editor inspector)
        {
            base.Enable(inspector);

	        if (m_Inspector == null || m_Face == null)
	        {
		        return;
	        }
            m_FaceObject = new SerializedObject(m_Face);

            if (m_GameObject)
            {
                m_GameObject.transform.localPosition = m_Face.localPosition;
                m_GameObject.transform.localRotation = m_Face.localRotation;
                m_GameObject.transform.localScale = m_Face.localScale;
            }
            
            _meshSettings.SubMeshIndex = m_Face.subMeshIndex;
            _meshSettings.UVIndex = m_Face.uvIndex;
            SetCustomMesh(m_Face.customMesh);
            SetCustomTexture(m_Face.customTexture);
            
            UpdateFaceMesh();
            
            Selection.selectionChanged += OnSelectionChanged;
        }

        public override void Disable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            
            m_Face.customMesh = _meshSettings.CustomMesh;
            m_Face.subMeshIndex = _meshSettings.SubMeshIndex;
            m_Face.uvIndex = _meshSettings.UVIndex;
            
            m_Face.customTexture = _textureSettings.CustomTexture;

            if (m_GameObject)
            {
                m_Face.localPosition = m_GameObject.transform.localPosition;
                m_Face.localRotation = m_GameObject.transform.localRotation;
                m_Face.localScale = m_GameObject.transform.localScale;
            }
            
        }
        
        public override void OnInspectorGUI()
        {
            OnGUI();
        }

        private bool _foldoutSceneSettings = true;
        private UnityEditor.Editor _gameObjectEditor;
        private void DrawSceneSettings()
        {
            var settingsFoldOutRect = EditorGUILayout.GetControlRect();
            settingsFoldOutRect.xMin -= 12.0f;

            _foldoutSceneSettings = EditorGUI.Foldout(settingsFoldOutRect, _foldoutSceneSettings, "SceneSettings");

            if (_foldoutSceneSettings)
            {
                //_sceneSettingsObject.Update();

                EditorGUI.BeginChangeCheck();
                using (new EditorGUIIndentLevelScope())
                {
                    GameObject go = m_GameObject;
                    if (go)
                    {
                        if (_gameObjectEditor == null)
                        {
                            _gameObjectEditor = UnityEditor.Editor.CreateEditor(go.transform);
                        }
                        if (_gameObjectEditor.target != go.transform)
                        {
                            if (_gameObjectEditor != null) DestroyImmediate(_gameObjectEditor);
                            _gameObjectEditor = UnityEditor.Editor.CreateEditor(go.transform);
                        }

                        _gameObjectEditor.OnInspectorGUI();
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.SetCurrentGroupName("Settings Change");
                }

                //_sceneSettingsObject.ApplyModifiedProperties();
            }
        }
        
        Mesh GetUVVertexMesh(int uvIndex)
        {
            switch (uvIndex)
            {
                case 0: return _uvVertexMesh;
                case 1: return _uv2VertexMesh;
                case 2: return _uv3VertexMesh;
                case 3: return _uv4VertexMesh;
            }
            return null;
        }
        
        Mesh GetUVLineMesh(int uvIndex)
        {
            switch (uvIndex)
            {
                case 0: return _uvLineMesh;
                case 1: return _uv2LineMesh;
                case 2: return _uv3LineMesh;
                case 3: return _uv4LineMesh;
            }
            return null;
        }

        #region selecting vertex and line

        
        public struct VertexData
        {
            public int _vertexIndex;

            public VertexData(int index = -1)
            {
                _vertexIndex = index;
            }
            
            public bool IsValid()
            {
                return _vertexIndex >= 0;
            }

            public static int[] ToArray(List<VertexData> set)
            {
                int[] list = new int[set.Count];
                int i = 0;
                foreach (var v in set)
                {
                    list[i++] = v._vertexIndex;
                }
                return list;
            }
        }
        public struct LineData
        {
            public int _vertexIndex1;
            public int _vertexIndex2;

            public LineData(int vertexIndex1 = -1, int vertexIndex2 = -1)
            {
                _vertexIndex1 = vertexIndex1;
                _vertexIndex2 = vertexIndex2;
            }

            public bool IsValid()
            {
                return _vertexIndex1 >= 0 && _vertexIndex2 >= 0;
            }
            
            public bool IsSame(LineData other)
            {
                if (_vertexIndex1 == other._vertexIndex1 && _vertexIndex2 == other._vertexIndex2)
                {
                    return true;
                }
                if (_vertexIndex1 == other._vertexIndex2 && _vertexIndex2 == other._vertexIndex1)
                {
                    return true;
                }

                return false;
            }

            public bool IsEndWithHead(LineData other)
            {
                return _vertexIndex2 == other._vertexIndex1;
            }
            
            public bool IsEndWithEnd(LineData other)
            {
                return _vertexIndex2 == other._vertexIndex2;
            }
            
            public bool IsHeadWithEnd(LineData other)
            {
                return _vertexIndex1 == other._vertexIndex2;
            }
            
            public bool IsHeadWithHead(LineData other)
            {
                return _vertexIndex1 == other._vertexIndex1;
            }
            
            public LineData Reverse()
            {
                int temp = _vertexIndex1;
                _vertexIndex1 = _vertexIndex2;
                _vertexIndex2 = temp;
                return this;
            }

            public static int[] ToArray(List<LineData> set)
            {
                int[] list = new int[set.Count * 2];
                int i = 0;
                foreach (var v in set)
                {
                    list[i*2+0] = v._vertexIndex1;
                    list[i*2+1] = v._vertexIndex2;
                    i++;
                }
                return list;
            }

        }

        bool _selectingMesh = false;
        Vector2 _selectingPositionStart;
        float _selectingRadius = 20.0f;
        List<VertexData> _selectingVertexSet = new List<VertexData>();
        List<LineData> _selectingLineSet = new List<LineData>();
        HashSet<VertexData> _selectingRegionSet = new HashSet<VertexData>();

        private void HitMeshVertex(Vector3 point, Mesh pointMesh, int subMeshIndex, ref float minDistance, ref VertexData hitIndex)
        {
            Vector3[] vertices = pointMesh.vertices;
            int[] indices = pointMesh.GetIndices(subMeshIndex);

            foreach (var i in indices)
            {
                float dis = Vector3.Distance(point, vertices[i]);
                if (dis < minDistance)
                {
                    minDistance = dis;
                    hitIndex._vertexIndex = i;
                }
            }
        }
        
        private VertexData HitMeshVertex(Vector3 uvPos, float minDistance)
        {
            VertexData hitIndex = new VertexData
            {
                _vertexIndex = -1
            };
            Mesh pointMesh = GetUVVertexMesh(_uvIndex);
            if (pointMesh == null)
            {
                return hitIndex;
            }

            int subMeshCount = _meshSettings.MeshInfo.SubMeshCount;
            
            if (_subMeshIndex == -1)
            {
                for (int i = 0; i < subMeshCount; ++i)
                {
                    HitMeshVertex(uvPos, pointMesh, i, ref minDistance, ref hitIndex);
                }
            }
            else if (_subMeshIndex >= 0 && _subMeshIndex < subMeshCount)
            {
                HitMeshVertex(uvPos, pointMesh, _subMeshIndex, ref minDistance, ref hitIndex);
            }
            return hitIndex;
        }

        private float GetDistanceOnLine(Vector3 point, Vector3 p1, Vector3 p2)
        {
            Vector3 p1p2 = p2 - p1;
            Vector3 p1p = point - p1;
            float t = Vector3.Dot(p1p, p1p2) / Vector3.Dot(p1p2, p1p2);
            if (t < 0)
            {
                return Vector3.Distance(point, p1);
            }
            else if (t > 1)
            {
                return Vector3.Distance(point, p2);
            }
            else
            {
                Vector3 p = p1 + t * p1p2;
                return Vector3.Distance(point, p);
            }
        }
        
        private void HitMeshLine(Vector3 point, Mesh lineMesh, int subMeshIndex, ref float minDistance, ref LineData hitIndex)
        {
            Vector3[] vertices = lineMesh.vertices;
            int[] indices = lineMesh.GetIndices(subMeshIndex);

            for (int i = 0; i < indices.Length; i+=2)
            {
                Vector3 p1 = vertices[indices[i]];
                Vector3 p2 = vertices[indices[i + 1]];
                float dis = GetDistanceOnLine(point, p1, p2);
                if (dis < minDistance)
                {
                    minDistance = dis;
                    hitIndex._vertexIndex1 = indices[i];
                    hitIndex._vertexIndex2 = indices[i + 1];
                }
            }
        }
        
        private LineData HitMeshLine(Vector3 uvPos, float minDistance)
        {
            LineData hitIndex = new LineData();
            
            Mesh lineMesh = GetUVLineMesh(_uvIndex);
            if (lineMesh == null)
            {
                return hitIndex;
            }

            int subMeshCount = _meshSettings.MeshInfo.SubMeshCount;
            
            if (_subMeshIndex == -1)
            {
                for (int i = 0; i < subMeshCount; ++i)
                {
                    HitMeshLine(uvPos, lineMesh, i, ref minDistance, ref hitIndex);
                }
            }
            else if (_subMeshIndex >= 0 && _subMeshIndex < subMeshCount)
            {
                HitMeshLine(uvPos, lineMesh, _subMeshIndex, ref minDistance, ref hitIndex);
            }
            return hitIndex;
        }

        private void HitMeshRegion(Vector3 point, Mesh pointMesh, int subMeshIndex, ref float minDistance, ref HashSet<VertexData> hitIndex)
        {
            Vector3[] vertices = pointMesh.vertices;
            int[] indices = pointMesh.GetIndices(subMeshIndex);

            foreach (var i in indices)
            {
                float dis = Vector3.Distance(point, vertices[i]);
                if (dis < minDistance)
                {
                    hitIndex.Add(new VertexData(i));
                }
            }
        }
        
        private HashSet<VertexData> HitMeshRegion(Vector3 uvPos, float minDistance)
        {
            HashSet<VertexData> hitIndex = new HashSet<VertexData>();
            
            Mesh vertexMesh = GetUVVertexMesh(_uvIndex);
            if (vertexMesh == null)
            {
                return hitIndex;
            }

            int subMeshCount = _meshSettings.MeshInfo.SubMeshCount;
            
            if (_subMeshIndex == -1)
            {
                for (int i = 0; i < subMeshCount; ++i)
                {
                    HitMeshRegion(uvPos, vertexMesh, i, ref minDistance, ref hitIndex);
                }
            }
            else if (_subMeshIndex >= 0 && _subMeshIndex < subMeshCount)
            {
                HitMeshRegion(uvPos, vertexMesh, _subMeshIndex, ref minDistance, ref hitIndex);
            }
            return hitIndex;
        }

        enum SelectType
        {
            None,
            Landmark,
            Curve,
            Region
        }
        enum SelectMode
        {
            None,
            Add,
            Remove
        }
        private void SelectMesh(Vector2 position, Rect viewRect, SelectType selectType, SelectMode selectMode)
        {
            // Matrix4x4 mat =
            //     Matrix4x4Ext.Translate((viewRect.size * 0.5f) - viewRect.position) *
            //     Matrix4x4.Scale(new Vector3((float)_viewScale, -(float)_viewScale, 1.0f)) *
            //     Matrix4x4Ext.Translate(-(Vector2)_viewPivot);

            Vector2 uvPos = position;
            uvPos -= (viewRect.size * 0.5f);
            uvPos /= new Vector2((float)_viewScale, -(float)_viewScale);
            uvPos += new Vector2((float)_viewPivot.x, (float)_viewPivot.y);

            float minDistance = _selectingRadius / (float)_viewScale;
            
            if (selectType == SelectType.Landmark)
            {
                VertexData hitIndex = HitMeshVertex(uvPos, minDistance);
                if (hitIndex.IsValid())
                {
                    _selectingVertexSet.Clear();
                    _selectingVertexSet.Add(hitIndex);
                    UpdateSelectingMesh();
                }
            }
            else if (selectType == SelectType.Curve)
            {
                LineData hitIndex = HitMeshLine(uvPos, minDistance);
                if (hitIndex.IsValid())
                {
                    if (_selectingLineSet.Count <= 0)
                    {
                        _selectingLineSet.Add(hitIndex);
                        UpdateSelectingMesh();
                    }else
                    {
                        // 处理头尾相接的情况
                        LineData lastLine = _selectingLineSet.Last();
                        if (lastLine.IsSame(hitIndex))
                        {
                            _selectingLineSet.RemoveAt(_selectingLineSet.Count - 1);
                            UpdateSelectingMesh();
                        }else if (lastLine.IsEndWithHead(hitIndex))
                        {
                            _selectingLineSet.Add(hitIndex);
                            UpdateSelectingMesh();
                        }else if (lastLine.IsEndWithEnd(hitIndex))
                        {
                            _selectingLineSet.Add(hitIndex.Reverse());
                            UpdateSelectingMesh();
                        }
                        else
                        {
                            LineData firstLine = _selectingLineSet.First();
                            if (firstLine.IsSame(hitIndex))
                            {
                                _selectingLineSet.RemoveAt(0);
                                UpdateSelectingMesh();
                            }else if (firstLine.IsHeadWithEnd(hitIndex))
                            {
                                _selectingLineSet.Insert(0, hitIndex);
                                UpdateSelectingMesh();
                            }else if (firstLine.IsHeadWithHead(hitIndex))
                            {
                                _selectingLineSet.Insert(0, hitIndex.Reverse());
                                UpdateSelectingMesh();
                            }
                        }
                        
                    }
                    
                }
            }else if (selectType == SelectType.Region)
            {
                
                HashSet<VertexData> hitIndex = HitMeshRegion(uvPos, minDistance);
                if (hitIndex.Count > 0)
                {
                    if (selectMode == SelectMode.Add)
                    {
                        _selectingRegionSet.AddRange(hitIndex);
                        UpdateSelectingMesh();
                    }else if (selectMode == SelectMode.Remove)
                    {
                        _selectingRegionSet.RemoveWhere((data) => hitIndex.Contains(data));
                        UpdateSelectingMesh();
                    }
                }
            }
        }
        
        private void ProcessSelecting(Rect viewRect)
        {
            Event evt = Event.current;


            if (!evt.control && !evt.shift)
            {
                return;
            }

            if (activeSelectType == SelectType.None || (activeLandmarkType == FaceLandmarkType.FaceLandmarkNone &&
                activeCurveType == FaceCurveType.FaceCurveNone) && activeRegionType == FaceRegionType.FaceRegionNone)
            {
                return;
            }

            SelectMode selectMode = SelectMode.None;
            if (evt.control)
            {
                selectMode = SelectMode.Add;
            }else if (evt.shift)
            {
                selectMode = SelectMode.Remove;
            }
            //if (EditorGUI.actionKey)
            {
                if (evt.rawType == EventType.ScrollWheel)
                {
                    if (viewRect.Contains(evt.mousePosition))
                    {
                        if (evt.delta.y > 0)
                        {
                            _selectingRadius *= 1.1f;
                        }
                        else if (evt.delta.y < 0)
                        {
                            _selectingRadius *= 0.9f;
                        }
                        
                        _selectingRadius = Mathf.Clamp(_selectingRadius, 1.0f, 100.0f);
                        evt.Use();
                    }
                }
                
                if (_selectingMesh && evt.rawType == EventType.MouseUp )
                {
                    if (viewRect.Contains(evt.mousePosition))
                    {
                        if (evt.button == 0)
                        {
                            SelectMesh(evt.mousePosition, viewRect, activeSelectType, selectMode);
                        }else if (evt.button == 1)
                        {
                            _selectingVertexSet.Clear();
                            _selectingLineSet.Clear();
                            _selectingRegionSet.Clear();
                            UpdateSelectingMesh();
                        }
                    }

                    _selectingMesh = false;
                    evt.Use();
                }
            
                if (evt.type == EventType.MouseDown && viewRect.Contains(evt.mousePosition))
                {
                    _selectingPositionStart = evt.mousePosition;
                    _selectingMesh = true;
                    evt.Use();
                }

                if (_selectingMesh && evt.delta != Vector2.zero && evt.button == 0 && activeSelectType == SelectType.Region)
                {
                    if (viewRect.Contains(evt.mousePosition))
                    {
                        SelectMesh(evt.mousePosition, viewRect, activeSelectType, selectMode);
                        evt.Use();
                    }
                }
            }
        }

        #endregion

        SelectType activeSelectType = SelectType.None;
        FaceLandmarkType activeLandmarkType = FaceLandmarkType.FaceLandmarkNone;
        FaceCurveType activeCurveType = FaceCurveType.FaceCurveNone;
        FaceRegionType activeRegionType = FaceRegionType.FaceRegionNone;

        private void UpdateSelectingMesh(bool needUpdateLandmark = true)
        {
            if (needUpdateLandmark)
            {
                if (activeLandmarkType != FaceLandmarkType.FaceLandmarkNone)
                {
                    int vID = _selectingVertexSet.Count > 0 ? _selectingVertexSet.ToArray().Last()._vertexIndex : -1;
                    m_Face.UpdateLandmark(activeLandmarkType, vID);

                    if (_viewSettings.MirrorX)
                    {
                        FaceLandmarkType mirrorType = FaceMirror.GetMirror(activeLandmarkType);
                        if (mirrorType != activeLandmarkType)
                        {
                            Vector3 pos = _faceLandmakrMesh.vertices[vID];
                            pos.x = _viewSettings.MirrorXCenter * 2 - pos.x;
                            
                            VertexData mirrorVertex = HitMeshVertex(pos, 0.1f);
                            m_Face.UpdateLandmark(mirrorType, mirrorVertex._vertexIndex);
                        }
                    }
                    
                    EditorUtility.SetDirty(m_Face);
                }

                if (activeCurveType != FaceCurveType.FaceCurveNone)
                {
                    int[] vIDs = _selectingLineSet.Count > 0 ? new int[_selectingLineSet.Count + 1] : new []{-1,-1};
                
                    for (int i = 0; i < _selectingLineSet.Count; i++)
                    {
                        if (i == 0)
                        {
                            vIDs[0] = _selectingLineSet[i]._vertexIndex1;
                        }
                        vIDs[i + 1] = _selectingLineSet[i]._vertexIndex2;
                    }
                    m_Face.UpdateCurve(activeCurveType, vIDs);
                    EditorUtility.SetDirty(m_Face);
                }
                
                if (activeRegionType != FaceRegionType.FaceRegionNone)
                {
                    HashSet<int> vIDs = new HashSet<int>();
                    foreach (var vertex in _selectingRegionSet)
                    {
                        vIDs.Add(vertex._vertexIndex);
                    }

                    m_Face.UpdateRegion(activeRegionType, vIDs);
                    EditorUtility.SetDirty(m_Face);
                }
            }
            
            UpdateFaceMesh();

        }

        private void UpdateSelectionToFace()
        {
        }
        private void OnSelectionChanged()
        {
            _selectingVertexSet.Clear();
            _selectingLineSet.Clear();
            _selectingRegionSet.Clear();

            activeSelectType = SelectType.None;
            activeLandmarkType = FaceLandmarkType.FaceLandmarkNone;
            activeCurveType = FaceCurveType.FaceCurveNone;
            activeRegionType = FaceRegionType.FaceRegionNone;
            
            GameObject go = Selection.activeGameObject;
            if (go)
            {
                FaceLandmarkComponent landmark = go.GetComponent<FaceLandmarkComponent>();
                if (landmark)
                {
                    activeLandmarkType = landmark.landmark.type;
                    activeSelectType = SelectType.Landmark;
                }
                
                FaceCurveComponent curve = go.GetComponent<FaceCurveComponent>();
                if (curve)
                {
                    activeCurveType = curve.curve.type;
                    activeSelectType = SelectType.Curve;
                }
                
                FaceRegionComponent region = go.GetComponent<FaceRegionComponent>();
                if (region)
                {
                    activeRegionType = region.region.type;
                    activeSelectType = SelectType.Region;
                }
                
            }

            UpdateSelectingMesh(false);

            foreach (var landmark in m_Face.landmarks)
            {
                if (landmark.type == activeLandmarkType)
                {
                    if (landmark.vID >= 0)
                    {
                        _selectingVertexSet.Add(new VertexData(landmark.vID));
                    }
                }
            }
            
            foreach (var curve in m_Face.curves)
            {
                if (curve.type == activeCurveType)
                {
                    for (int i = 0; i < curve.vIDs.Length - 1; i++)
                    {
                        if (curve.vIDs[i] >= 0 && curve.vIDs[i + 1] >= 0)
                        {
                            _selectingLineSet.Add(new LineData(curve.vIDs[i], curve.vIDs[i+1]));
                        }
                    }
                }
            }
            
            foreach (var region in m_Face.regions)
            {
                if (region.type == activeRegionType)
                {
                    foreach (var vID in region.vIDs)
                    {
                        if (vID >= 0)
                        {
                            _selectingRegionSet.Add(new VertexData(vID));
                        }
                    }
                }
            }
        }

        private void ProcessViewInputEx(Rect viewRect)
        {
            
            m_FaceObject.Update();

            EditorGUI.BeginChangeCheck();
            ProcessSelecting(viewRect);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.SetCurrentGroupName("Settings Change");
            }

            m_FaceObject.ApplyModifiedProperties();
        }


        private void DrawViewMeshEx(Rect viewRect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            using (new DrawMeshScope(viewRect, EnableGeometryShader ? EditorGUIUtility.pixelsPerPoint : 1.0f))
            {
                DrawFaceCalibration(viewRect);
                DrawSelecting(viewRect);
            }
            
            DrawFaceLabel(viewRect);
        }

        private void UpdateFaceMesh()
        {
            Mesh pointMesh = GetUVVertexMesh(_uvIndex);
            Mesh lineMesh = GetUVLineMesh(_uvIndex);
            if (_refPointMesh != pointMesh ||  _refLineMesh != lineMesh)
            {
                _refPointMesh = pointMesh;
                _refLineMesh = lineMesh;

                _faceLandmakrMesh = new Mesh();
                _faceLandmakrMesh.vertices = pointMesh.vertices;
                _faceCurveMesh = new Mesh();
                _faceCurveMesh.vertices = lineMesh.vertices;
                _faceRegionMesh = new Mesh();
                _faceRegionMesh.vertices = pointMesh.vertices;

                _selectingVertexMesh = new Mesh();
                _selectingVertexMesh.vertices = pointMesh.vertices;
                _selectingLineMesh = new Mesh();
                _selectingLineMesh.vertices = lineMesh.vertices;
                _selectingRegionMesh = new Mesh();
                _selectingRegionMesh.vertices = pointMesh.vertices;
            }

            if (pointMesh)
            {
                List<int> landmarkSet = new List<int>();
                List<int> selectedSet = new List<int>();
                
                FaceLandmarkComponent[] landmarkComponents = m_GameObject ? m_GameObject.GetComponentsInChildren<FaceLandmarkComponent>() : null;

                foreach (var landmark in m_Face.landmarks)
                {
                    Vector3 landmarkPos = Vector3.zero;
                    float size = 0;
                    if (landmark.vID >= 0)
                    {
                        if (CustomMesh)
                        {
                            landmarkPos = CustomMesh.vertices[landmark.vID];
                            size = CustomMesh.bounds.size.x * 0.001f;
                        }
                        
                        landmarkSet.Add(landmark.vID);
                        if (landmark.type == activeLandmarkType)
                        {
                            selectedSet.Add(landmark.vID);
                        }
                    }

                    if (landmarkComponents != null)
                    {
                        foreach (var landmarkComponent in landmarkComponents)
                        {
                            if (landmarkComponent.landmark.type == landmark.type)
                            {
                                landmarkComponent.UpdateLandmark(landmark.vID, landmarkPos, size);
                                break;
                            }
                        }
                    }
                }
                
                _faceLandmakrMesh.SetIndices(landmarkSet, MeshTopology.Points, 0);
                _selectingVertexMesh.SetIndices(selectedSet, MeshTopology.Points, 0);
            }

            if (lineMesh)
            {
                List<int> lineSet = new List<int>();
                List<int> selectedSet = new List<int>();
                foreach (var curve in m_Face.curves)
                {
                    for (int i = 0; i < curve.vIDs.Length-1; i++)
                    {
                        int v1 = curve.vIDs[i];
                        int v2 = curve.vIDs[i+1];
                        if (v1 >= 0 && v2 >= 0)
                        {
                            lineSet.Add(v1);
                            lineSet.Add(v2);
                            
                            if (curve.type == activeCurveType)
                            {
                                selectedSet.Add(v1);
                                selectedSet.Add(v2);
                            }
                        }
                    }
                }
                
                _faceCurveMesh.SetIndices(lineSet, MeshTopology.Lines, 0);
                _selectingLineMesh.SetIndices(selectedSet, MeshTopology.Lines, 0);
            }

            if (pointMesh)
            {
                _faceRegionMesh.subMeshCount = m_Face.regions.Length;
                FaceRegionComponent[] regionComponents = m_GameObject ? m_GameObject.GetComponentsInChildren<FaceRegionComponent>() : null;

                List<int> selectedSet = new List<int>();
                for (int i = 0; i < m_Face.regions.Length; i++)
                {
                    FaceRegion region = m_Face.regions[i];
                    List<int> vertices =  region.vIDs.ToList();
                    if (region.type == activeRegionType)
                    {
                        selectedSet = vertices;
                    }
                    
                    // _faceLandmakrMesh.SetIndices(vertices, MeshTopology.Points, i);
                }
                _selectingRegionMesh.SetIndices(selectedSet, MeshTopology.Points, 0);
            }
        }
        
        private void DrawSelecting(Rect viewRect)
        {
            if (_selectingLineMesh)
            {
                Color outlineColor = _viewSettings.OutlineColor;
                _lineMaterial.SetColor("_Color", outlineColor);
                _lineMaterial.SetFloat("_Thickness", _viewSettings.LineThickness * 4);
                _lineMaterial.SetPass(0);
                
                Mesh lineMesh = _selectingLineMesh;
                Graphics.DrawMeshNow(lineMesh, _viewMat, 0);
            }

            if (_selectingVertexMesh)
            {
                _vertexMaterial.SetColor("_Color", _viewSettings.VertexOutlineColor);
                _vertexMaterial.SetFloat("_VertexColorRatio", 0.0f);
                _vertexMaterial.SetFloat("_Radius", (_viewSettings.VertexSize * 4));
                _vertexMaterial.SetPass(0);

                Mesh pointMesh = _selectingVertexMesh;
                Graphics.DrawMeshNow(pointMesh, _viewMat, 0);
            }

            if (_selectingRegionMesh)
            {
                _vertexMaterial.SetColor("_Color", _viewSettings.VertexOutlineColor);
                _vertexMaterial.SetFloat("_VertexColorRatio", 0.0f);
                _vertexMaterial.SetFloat("_Radius", (_viewSettings.VertexSize));
                _vertexMaterial.SetPass(0);

                Mesh pointMesh = _selectingRegionMesh;
                Graphics.DrawMeshNow(pointMesh, _viewMat, 0);
            }
        }

        Color GetColorByRegionType(FaceRegionType type)
        {
            switch (type)
            {
                case FaceRegionType.Face:
                    return Color.gray;
                case FaceRegionType.EyebrowLeft:
                    return Color.green;
                case FaceRegionType.EyebrowRight:
                    return Color.green;
                case FaceRegionType.EyeLeft:
                    return Color.magenta;
                case FaceRegionType.EyeRight:
                    return Color.magenta;
                case FaceRegionType.MouthUpper:
                    return Color.cyan;
                case FaceRegionType.MouthLower:
                    return Color.red;
                case FaceRegionType.NoseUpper:
                    return Color.yellow;
                case FaceRegionType.NoseLower:
                    return Color.yellow;
                case FaceRegionType.Jaw:
                    return Color.blue;
                default:
                    return Color.white;
            }
        }

        private void DrawFaceCalibration(Rect viewRect)
        {
            if (_faceCurveMesh)
            {
                _lineMaterial.SetColor("_Color", Color.red);
                _lineMaterial.SetFloat("_Thickness", _viewSettings.LineThickness * 2);
                _lineMaterial.SetPass(0);
                
                Mesh lineMesh = _faceCurveMesh;
                Graphics.DrawMeshNow(lineMesh, _viewMat, 0);
            }

            if (_faceLandmakrMesh)
            {
                _vertexMaterial.SetColor("_Color", Color.green);
                _vertexMaterial.SetFloat("_VertexColorRatio", 0.0f);
                _vertexMaterial.SetFloat("_Radius", (_viewSettings.VertexSize * 2));
                _vertexMaterial.SetPass(0);

                Mesh pointMesh = _faceLandmakrMesh;
                Graphics.DrawMeshNow(pointMesh, _viewMat, 0);
            }

            if (_faceRegionMesh)
            {

                Mesh pointMesh = _faceRegionMesh;
                for (int i = 0; i < _faceRegionMesh.subMeshCount; i++)
                {
                    Color color = GetColorByRegionType((FaceRegionType)i);
                    
                    _vertexMaterial.SetColor("_Color", color);
                    _vertexMaterial.SetFloat("_VertexColorRatio", 0.0f);
                    _vertexMaterial.SetFloat("_Radius", (_viewSettings.VertexSize * 2));
                    _vertexMaterial.SetPass(0);
                    Graphics.DrawMeshNow(pointMesh, _viewMat, i);
                }
            }
        }
        
        
        private void DrawFaceLabel(Rect viewRect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (!_viewSettings.DrawGrid)
            {
                return;
            }

            Matrix4x4 mat =
                Matrix4x4Ext.Translate((viewRect.size * 0.5f) - viewRect.position) *
                Matrix4x4.Scale(new Vector3((float)_viewScale, -(float)_viewScale, 1.0f)) *
                Matrix4x4Ext.Translate(-(Vector2)_viewPivot);

            using (new GUI.ClipScope(viewRect))
            using (new HandlesGUIScope())
            using (new HandlesDrawingScope(Handles.matrix * mat))
            {
                var style = new GUIStyle
                {
                    normal = new GUIStyleState { textColor = Color.white },
                    alignment = TextAnchor.UpperRight
                };
                for (int i = 0; i < m_Face.landmarks.Length; i++)
                {
                    int vID = m_Face.landmarks[i].vID;
                    if (vID >= 0)
                    {
                        string text = m_Face.landmarks[i].Name;
                        Vector3 Position = _faceLandmakrMesh.vertices[vID];
                        Handles.Label(Position, text, style);
                    }
                }

            }
        }

    }
}