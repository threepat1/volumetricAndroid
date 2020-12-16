/* Mesh Player low level Reader API.
*  All Rights rRserved. XR Company 2020.
*/
using UnityEngine;
using System.Runtime.InteropServices;

namespace prometheus
{
    public class ReaderAPI
    {
#if UNITY_IPHONE && !UNITY_EDITOR
        private const string IMPORT_NAME = "__Internal";  
#else //Android & Desktop
        private const string IMPORT_NAME = "MeshPlayerPlugin";
#endif

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void UnityPluginUnload();

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetDebugFunction(System.IntPtr func);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int CreateApiInstance();

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool OpenMeshStream(int api, string src);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetMeshStreamInfo(int api, ref float duration_sec, ref float fps, ref int nb_frames);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetReaderStartFrame(int api, int frm_idx);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetSpeedRatio(int api, float speed_ratio);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetReaderStartSecond(int api, float start_sec);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void PlayReader(int api);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void PauseReader(int api);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void ForwardOneFrame(int api);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void CloseReader(int api);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetResolution(int api, ref int width, ref int height);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool BeginReadFrame(int api, ref float pts_sec);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool BeginReadFrameWithSoundSec(int api, ref float pts_sec,ref float soundSec, ref float lastTimeGap);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void EndReadFrame(int api);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetVerticesCount(int api);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetMeshVertices(int api, int count, 
            System.IntPtr vertices, System.IntPtr normals, System.IntPtr uvs, System.IntPtr triangles);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetMeshTextures(int api, int width, int height, int channels, System.IntPtr colors);


        //Audio function
        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool ReadAudioFrame(int api, ref float pts_sec);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetAudioByte(int api, System.IntPtr Audios);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetAudioStreamInfo(int api, ref System.UInt16 mChannels, ref int mSampleRate, ref System.UInt16 mBitDepth);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioByteCount(int api);

        [DllImport(IMPORT_NAME, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetAudioMainSwitch(int api, bool value);

    }
    
}