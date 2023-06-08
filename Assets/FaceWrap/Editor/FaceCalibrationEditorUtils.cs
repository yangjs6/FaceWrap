
using FaceWrap.Runtime;
using Unity.VisualScripting;
using UnityEngine;
namespace FaceWrap.Editor
{
    public class FaceCalibrationEditorUtils
    {
        public static Renderer GetMeshRenderer(GameObject rootGameObject, Mesh mesh)
        {
            if (mesh == null)
            {
                return null;
            }
            
            Mesh originalMesh = null;
            Renderer[] renderers = rootGameObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer)
                {
                    originalMesh = (renderer as SkinnedMeshRenderer).sharedMesh;
                }else if (renderer is MeshRenderer)
                {
                    originalMesh = (renderer as MeshRenderer).GetComponent<MeshFilter>().sharedMesh;
                }

                if (originalMesh == mesh)
                {
                    return renderer;
                }
            }

            return null;
        }
        public static GameObject CreateGameObject(FaceCalibration face)
        {
            if (face == null)
                return null;
            
            GameObject gameObject = new GameObject(face.name);
            GameObject original = face.faceMesh;
            GameObject parent = gameObject;
            if (original != null)
            {
                GameObject meshGameObject = UnityEngine.Object.Instantiate(original, gameObject.transform, true);

                Renderer renderer = GetMeshRenderer(meshGameObject, face.customMesh);
                if (renderer)
                {
                    parent = renderer.gameObject;
                }
            }

            GameObject landmarks = new GameObject("FaceLandmarks");
            landmarks.transform.SetParent(parent.transform, false);
            GameObject curves = new GameObject("FaceCurves");
            curves.transform.SetParent(parent.transform, false);
            GameObject regions = new GameObject("FaceRegions");
            regions.transform.SetParent(parent.transform, false);

            if (face.landmarks.Length <=0 && face.curves.Length <=0)
            {
                face.Reset();
            }
        
            for (int i = 0; i < face.landmarks.Length; i++)
            {
                FaceLandmark landmark = face.landmarks[i];
                string name = landmark.Name;
                GameObject go = new GameObject(name);
                FaceLandmarkComponent component = go.GetOrAddComponent<FaceLandmarkComponent>();
                component.landmark = landmark;
                go.transform.SetParent(landmarks.transform, false);
            }
        
            for (int i = 0; i < face.curves.Length; i++)
            {
                FaceCurve curve = face.curves[i];
                string name = curve.type.ToString();
                GameObject go = new GameObject(name);
                FaceCurveComponent component = go.GetOrAddComponent<FaceCurveComponent>();
                component.curve = curve;
                go.transform.SetParent(curves.transform, false);
            }

            for (int i = 0; i < face.regions.Length; i++)
            {
                FaceRegion region = face.regions[i];
                string name = region.type.ToString();
                GameObject go = new GameObject(name);
                FaceRegionComponent component = go.GetOrAddComponent<FaceRegionComponent>();
                component.region = region;
                go.transform.SetParent(regions.transform, false);
            }

            return gameObject;
        }

    }
}