using System.Collections.Generic;
using FaceWrap.Runtime;
using UnityEditor;
using UnityEngine;

namespace FaceWrap.Editor
{
    public class FaceVertAnimPlayerGUI
    {
        private static bool play = false;        
        private static float time, prev, current = 0f;
        public static bool AnimFoldOut { get; private set; } = true;     
        private static bool doneInitFace = false; 
        

        private static double updateTime = 0f;
        private static double deltaTime = 0f;
        private static double frameTime = 1f;
        private static bool forceUpdate = false;
        private static bool isShown = false;
        private static bool isAnimationMode = false;
        
        // load from flame obj file
        public static bool cachedDataChanged = false;
        public static Dictionary<int, Vector3> cachedOffset;
        public static float cachedBlendShapeWeight;
  
        public static FaceVertAnimAsset animClip { get ; set; }
        public static AudioClip audioClip { get ; set; }
        
        public static void DrawPlayer()
        {            
            GUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            AnimFoldOut = EditorGUILayout.Foldout(AnimFoldOut, "Animation Playback", EditorStyles.foldout);
            if (EditorGUI.EndChangeCheck())
            {
                //if (foldOut && FacialMorphIMGUI.FoldOut)
                //    FacialMorphIMGUI.FoldOut = false;
            }
            if (AnimFoldOut)
            {
                EditorGUI.BeginChangeCheck();
                animClip = (FaceVertAnimAsset)EditorGUILayout.ObjectField(new GUIContent("Animation", "Animation to play and manipulate"), animClip, typeof(FaceVertAnimAsset), false);
                audioClip = (AudioClip)EditorGUILayout.ObjectField(new GUIContent("Audio", "Audio to play and manipulate"), audioClip, typeof(AudioClip), false);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateAnimatorClip();
                }

                GUI.enabled = animClip;

                EditorGUI.BeginDisabledGroup(!isAnimationMode);

                if (animClip != null)
                {
                    float startTime = 0.0f;
                    float stopTime = animClip.time_length;
                    EditorGUI.BeginChangeCheck();
                    time = EditorGUILayout.Slider(time, startTime, stopTime);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ResetFace();
                    }
                }
                else
                {
                    float value = 0f;
                    value = EditorGUILayout.Slider(value, 0f, 1f); //disabled dummy entry
                }

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                // "Animation.FirstKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.FirstKey").image, "First Frame"), EditorStyles.toolbarButton))
                {
                    play = false;
                    time = 0f;
                    ResetFace();
                }
                // "Animation.PrevKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.PrevKey").image, "Previous Frame"), EditorStyles.toolbarButton))
                {
                    play = false;
                    time -= 1.0f / animClip.frame_second_per;
                    ResetFace();
                }
                // "Animation.Play"
                EditorGUI.BeginChangeCheck();
                play = GUILayout.Toggle(play, new GUIContent(EditorGUIUtility.IconContent("Animation.Play").image, "Play (Toggle)"), EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {                    
                    ResetFace();
                }
                // "PauseButton"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("PauseButton").image, "Pause"), EditorStyles.toolbarButton))
                {
                    play = false;
                    ResetFace();
                }
                // "Animation.NextKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.NextKey").image, "Next Frame"), EditorStyles.toolbarButton))
                {
                    play = false;
                    time +=  1.0f / animClip.frame_second_per;
                    ResetFace();
                }
                // "Animation.LastKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.LastKey").image, "Last Frame"), EditorStyles.toolbarButton))
                {
                    play = false;
                    time = animClip.time_length;
                    ResetFace();
                }

                if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) play = false;                

                GUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();

                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }

        public static void ResetFace(bool update = true, bool full = false)
        {
            forceUpdate = update;

            if (audioClip)
            {
                if (play)
                {
                    audioSource.Play();
                }
                else
                {
                    audioSource.Pause();
                }

                audioSource.time = time;
            }
        }

        public static bool IsPlayerShown()
        {          
            return isShown;
        }

        private static GameObject audioPlayer;
        private static AudioSource audioSource;
        public static void OpenPlayer()
        {
            audioPlayer = new GameObject("audioPlayer");
            audioSource = audioPlayer.AddComponent<AudioSource>();
            
            isAnimationMode = true;
            if (!isShown)
            {
                isShown = true;     
                SceneView.RepaintAll();
            }
        }

        public static void ClosePlayer()  
        {
            if (isShown)
            {
                doneInitFace = false;
                isShown = false;
                //Common
                play = false;
                time = 0f;
                animClip = null;
                SceneView.RepaintAll();
            }
            isAnimationMode = false;

            audioClip = null;
            
            GameObject.DestroyImmediate(audioPlayer);
        }

        static public void UpdateAnimatorClip()
        {
            if (doneInitFace) ResetFace(true, true);
            
            time = 0f;
            play = false;            

            // intitialise the face refs if needed
            if (!doneInitFace) doneInitFace = true;

            // finally, apply the face
            //ApplyFace();

            if (animClip)
            {
                // also restarts animation mode
                SampleOnce();
            }

            audioSource.clip = audioClip;
        }
        
        static Vector3 toV2(Vector3 v)
        {
            return new Vector3(-v.x, v.y, v.z) / 1.2f;
        }
        
        static void SetFrameWeight(ref Dictionary<int, Vector3> offsetData, int frame, float frameWeight)
        {
            FaceVertAnimAsset.FaceVertFrameData2 frameData = animClip.frame_list[frame];
            for (int i = 0; i < frameData.vert_data.Count; i++)
            {
                int vert_index = frameData.vert_data[i].vert_index;
                Vector3 vert_offset = frameData.vert_data[i].vert_offset;
                vert_offset = toV2(vert_offset);
                vert_offset *= frameWeight;
                if (offsetData.ContainsKey(vert_index))
                {
                    offsetData[vert_index] += vert_offset;
                }
                else
                {
                    offsetData.Add(vert_index, vert_offset);
                }
            }
        }
        
        public static void SampleOnce()
        {
            if (animClip)
            {
                
                int frame =  (int)(time * animClip.frame_second_per);
                if (frame >= animClip.frame_list.Count )
                {
                    return;
                }

                float frameWeight = 1 - (time * animClip.frame_second_per - frame);
                int frame2 = (frame + 1) % animClip.frame_list.Count;

                // 计算每个顶点的偏移量
                cachedOffset = new Dictionary<int, Vector3>();
                SetFrameWeight(ref cachedOffset, frame, frameWeight);
                SetFrameWeight(ref cachedOffset, frame2, 1 - frameWeight);
                //cachedBlendShapeWeight = animClip.oral_bs_weight[frame] * frameWeight + animClip.oral_bs_weight[frame2] * (1 - frameWeight);
                cachedDataChanged = true;

                // // 设置 blendshape
                // if (blendShapeIndex >= 0)
                // {
                //     float oral_bs_weight = animData.oral_bs_weight[frame] * frameWeight + animData.oral_bs_weight[frame2] * (1 - frameWeight);
                //     skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, 100 * oral_bs_weight * blendShapeWeight);
                // }
            }
        }

        public static void UpdatePlayer()
        {
            if (updateTime == 0f) updateTime = EditorApplication.timeSinceStartup;
            deltaTime = EditorApplication.timeSinceStartup - updateTime;
            updateTime = EditorApplication.timeSinceStartup;

            if (!EditorApplication.isPlaying && isAnimationMode)
            {
                if (animClip)
                {
                    if (play)
                    {
                        double frameDuration = 1.0f / animClip.frame_second_per;

                        time += (float)deltaTime;
                        frameTime += deltaTime;
                        if (time >= animClip.time_length)
                        {
                            time = 0f;
                            if (audioClip)
                            {
                                audioSource.time = 0f;
                            }
                        }

                        if (frameTime < frameDuration) return;
                        frameTime = 0f;
                    }
                    else
                        frameTime = 1f;

                    if (current != time || forceUpdate)
                    {
                        SampleOnce();
                        SceneView.RepaintAll();
                        
                        current = time;
                        forceUpdate = false;
                    }
                }
            }
        }
    }
}