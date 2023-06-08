using UnityEngine;

namespace FaceWrap.Runtime
{
    [CreateAssetMenu(fileName = "FaceCalibrationMap", menuName = "FaceWrap/FaceCalibrationMap", order = 0)]
    public class FaceCalibrationMap : ScriptableObject
    {
        public FaceCalibration source;
        public FaceCalibration target;
    }
}