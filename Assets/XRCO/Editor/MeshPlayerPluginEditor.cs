/* Unity Editor for Mesh Player Plugin.
*  All Rights Reserved. XR Company 2020.
*/
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace prometheus
{

    [CustomEditor(typeof(MeshPlayerPlugin))]
    public class MeshPlayerPluginEditor : Editor
    {
        //bool showPath = false;

        // register an event handler when the class is initialized
        static MeshPlayerPluginEditor()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        public override void OnInspectorGUI()
        {
            MeshPlayerPlugin mTarget = (MeshPlayerPlugin)target;

            Undo.RecordObject(mTarget, "Inspector");

            BuildFilesInspector(mTarget);

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }



        private void BuildFilesInspector(MeshPlayerPlugin mTarget)
        {
            GUILayout.Space(10);
            mTarget.SourceType = (SOURCE_TYPE)EditorGUILayout.EnumPopup("Source Type", mTarget.SourceType);

            Rect rect = EditorGUILayout.BeginVertical(); 
            if (mTarget.SourceType == SOURCE_TYPE.PLAYBACK)
            {
                //rect to allow drag'n'drop
                mTarget.SourceUrl = EditorGUILayout.TextField("Source Path", mTarget.SourceUrl);
                EditorGUILayout.EndVertical();

                mTarget.DataInStreamingAssets = EditorGUILayout.Toggle("In Streaming Assets", mTarget.DataInStreamingAssets);

                //Debug.Log("[MeshPlayerPluginEditor] SourceUrl = " + mTarget.SourceUrl);

                if ((Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    if (mTarget.SourceUrl.StartsWith("rtmp"))
                    {
                        mTarget.SourceType = SOURCE_TYPE.RTMP;
                    }
                    else if (mTarget.OpenCurrentSource(false))
                    {
                        mTarget.PreviewSec = 0;
                        mTarget.CurrentSec = 0;
                    }
                }
            }
            else //RTMP
            {
                //mTarget.ConnexionHost = EditorGUILayout.TextField("Host", mTarget.ConnexionHost);
                //mTarget.ConnexionPort = EditorGUILayout.IntField("Port", mTarget.ConnexionPort);
                mTarget.SourceUrl = EditorGUILayout.TextField("URL", mTarget.SourceUrl);
                EditorGUILayout.EndVertical();
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    if (!mTarget.SourceUrl.StartsWith("rtmp"))
                        mTarget.SourceType = SOURCE_TYPE.PLAYBACK;
                    //else if (mTarget.OpenCurrentSource(false))
                    //    mTarget.Preview();
                }
            }

            GUILayout.Space(10);

            mTarget.AutoPlay = EditorGUILayout.Toggle("Play On Start", mTarget.AutoPlay);
            mTarget.Loop = EditorGUILayout.Toggle("Playback Loop", mTarget.Loop);


            GUILayout.Space(10);

            //if (!mTarget.IsOpened && mTarget.OpenCurrentSource(false))
            //{
            //    mTarget.PreviewSec = 0;
            //    mTarget.CurrentSec = 0;
            //    mTarget.Preview();

            //    if (!EditorApplication.isPlaying)
            //    {
            //        mTarget.Uninitialize();
            //    }
            //}

            if (mTarget.SourceDurationSec > 0)
            {
                GUIContent previewframe = new GUIContent("Preview Frame (Seconds)");
                Color color = GUI.color;
                //if ((mTarget.LastActiveFrame != -1) && (mTarget.PreviewFrame < (int)mTarget.FirstActiveFrame || mTarget.PreviewFrame > (int)mTarget.LastActiveFrame))
                //    GUI.color = new Color(1, 0.6f, 0.6f);

                float selectSec = EditorGUILayout.Slider(previewframe, mTarget.PreviewSec, 0, mTarget.SourceDurationSec);

                if (!EditorApplication.isPlaying && mTarget.PreviewSec != selectSec)
                {
                    if (mTarget.OpenCurrentSource(false))
                    {
                        mTarget.PreviewSec = (float)selectSec;
                        Debug.Log("mTarget.PreviewSec = " + mTarget.PreviewSec);

                        mTarget.Preview();
                        mTarget.Uninitialize();
                    }
                }

                GUILayout.Label("SourceDurationSec Scene:" + mTarget.SourceDurationSec);
                GUILayout.Space(10);
                //if (frameVal != mTarget.PreviewFrame)
                //{
                //    mTarget.PreviewFrame = (int)frameVal;
                //    mTarget.Preview();
                //    mTarget.last_preview_time = System.DateTime.Now;
                //}
                //else
                //    mTarget.ConvertPreviewTexture();
            }

            //GUI.color = color;

            //GUIContent activerange = new GUIContent("Active Range");
            //float rangeMax = mTarget.LastActiveFrame == -1 ? mTarget.SequenceNbOfFrames - 1 : mTarget.LastActiveFrame;
            //if (mTarget.LastActiveFrame == -1)
            //    GUI.color = new Color(0.5f, 0.7f, 2.0f);
            //float firstActiveFrame = mTarget.FirstActiveFrame;
            //EditorGUILayout.MinMaxSlider(activerange, ref firstActiveFrame, ref rangeMax, 0.0f, mTarget.SequenceNbOfFrames - 1);
            //mTarget.FirstActiveFrame = (int)firstActiveFrame;
            //if (rangeMax == mTarget.SequenceNbOfFrames - 1 && mTarget.FirstActiveFrame == 0)
            //    mTarget.LastActiveFrame = -1;
            //else
            //    mTarget.LastActiveFrame = (int)rangeMax;

            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.Space();

            //if (mTarget.LastActiveFrame == -1)
            //{
            //    EditorGUILayout.Space();
            //    EditorGUILayout.Space();
            //    EditorGUILayout.LabelField("Full Range", GUILayout.Width(80));
            //    GUI.color = color;
            //    EditorGUILayout.Space();
            //    mTarget.LastActiveFrame = -1;
            //}
            //else
            //{
            //    mTarget.FirstActiveFrame = EditorGUILayout.IntField((int)mTarget.FirstActiveFrame, GUILayout.Width(50));
            //    EditorGUILayout.Space();
            //    mTarget.LastActiveFrame = EditorGUILayout.IntField((int)mTarget.LastActiveFrame, GUILayout.Width(50));
            //}


            //EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            //mTarget.OutOfRangeMode = (OUT_RANGE_MODE)EditorGUILayout.EnumPopup("Out of Range Mode", mTarget.OutOfRangeMode);

            mTarget.SpeedRatio = EditorGUILayout.FloatField("Speed Ratio", mTarget.SpeedRatio);

            mTarget.DebugInfo = EditorGUILayout.Toggle("Debug Info", mTarget.DebugInfo);


            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (rect.Contains(evt.mousePosition))
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    else
                    {
                        //EditorGUILayout.EndVertical();
                        return;
                    }

                    if (evt.type == EventType.DragPerform)
                    {
                        foreach (string path in DragAndDrop.paths)
                        {
                            string sourceUrl = path;

                            if (path.Contains("StreamingAssets"))
                            {
                                sourceUrl = path.Substring(path.LastIndexOf("/") + 1);
                                mTarget.DataInStreamingAssets = true;
                            }
                            else
                            {
                                if (path.Contains("Assets"))
                                {
                                    string message = "The sequence should be in \"Streaming Assets\" for a good application deployment";
                                    EditorUtility.DisplayDialog("Warning", message, "Close");
                                }
                                mTarget.DataInStreamingAssets = false;
                            }

                            mTarget.SourceUrl = sourceUrl;
                            mTarget.SourceType = SOURCE_TYPE.PLAYBACK;
                            mTarget.CurrentSec = 0;
                            //mTarget.FirstActiveFrame = 0;
                            //mTarget.LastActiveFrame = -1;

                            EditorUtility.SetDirty(target);

                            if (mTarget.OpenCurrentSource(false))
                            {
                                mTarget.Preview();
                                mTarget.Uninitialize();
                            }
                            //mTarget.Close();
                            //mTarget.Preview();
                        }
                    }
                    break;
            }
        }

        private static void LogPlayModeState(PlayModeStateChange state)
        {
            Debug.Log(state);
        }
    }

}