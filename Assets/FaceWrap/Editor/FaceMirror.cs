using FaceWrap.Runtime;

namespace FaceWrap.Editor
{
    public class FaceMirror
    {
        static FaceLandmarkType GetMirror(FaceLandmarkType inLandmarkType, FaceLandmarkType min, FaceLandmarkType max)
        {
            if (inLandmarkType >= min && inLandmarkType <= max )
            {
                return (FaceLandmarkType)(max + (int)min - inLandmarkType);
            }

            return inLandmarkType;
        }
        
        static FaceLandmarkType GetMirror2(FaceLandmarkType inLandmarkType, FaceLandmarkType min, FaceLandmarkType max)
        {
            if (inLandmarkType >= min && inLandmarkType <= max )
            {
                int center = ((int)max + (int)min + 1) / 2;
                if ((int)inLandmarkType > center)
                {
                    return (FaceLandmarkType)(min + (int)inLandmarkType - center);
                }
                else
                {
                    return (FaceLandmarkType)(center + (int)inLandmarkType - min);
                }
            }

            return inLandmarkType;
        }
        
        public static FaceLandmarkType GetMirror(FaceLandmarkType inLandmarkType)
        {
            inLandmarkType = GetMirror(inLandmarkType, FaceLandmarkType.F0, FaceLandmarkType.F16);
            inLandmarkType = GetMirror(inLandmarkType, FaceLandmarkType.F17, FaceLandmarkType.F32);
            inLandmarkType = GetMirror(inLandmarkType, FaceLandmarkType.F33, FaceLandmarkType.F50);
            inLandmarkType = GetMirror(inLandmarkType, FaceLandmarkType.F55, FaceLandmarkType.F59);
            inLandmarkType = GetMirror2(inLandmarkType, FaceLandmarkType.F60, FaceLandmarkType.F75);
            inLandmarkType = GetMirror(inLandmarkType, FaceLandmarkType.F76, FaceLandmarkType.F82);
            inLandmarkType = GetMirror(inLandmarkType, FaceLandmarkType.F83, FaceLandmarkType.F87);
            inLandmarkType = GetMirror(inLandmarkType, FaceLandmarkType.F88, FaceLandmarkType.F92);
            inLandmarkType = GetMirror(inLandmarkType, FaceLandmarkType.F93, FaceLandmarkType.F95);
            return inLandmarkType;
        }
    }
}