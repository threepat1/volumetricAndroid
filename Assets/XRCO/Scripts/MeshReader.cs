/* Mesh Player Reader Instance Wrapper.
*  All Rights Reserved. XR Company 2020.
*/
using UnityEngine;
using System.Runtime.InteropServices;

namespace prometheus
{
    public class MeshData
    {
        // mesh buffer
        public Vector3[] vertices = null;
        public Vector3[] normals = null;
        public Vector2[] uv = null;
        public int[] triangles = null;
        public int bufferSize = 0;

        public GCHandle gcHandlerVertices;
        public GCHandle gcHandlerNormals;
        public GCHandle gcHandlerUV;
        public GCHandle gcHandlerTriangles;

        // texture buffer
        public int textWidth = 0, textHeight = 0;
        public Color32[] colors = null;
        public GCHandle gcHandlerColors;

        public float ptsSec;
        public bool realtime = false;

        public void AllocMeshBuffer(int numTris)
        {
            if (bufferSize < numTris * 3)
            {
                ClearMeshBuffer();

                bufferSize = ((int)(numTris * 1.1) / 3 + 1) * 3;
                vertices = new Vector3[bufferSize];
                normals = new Vector3[bufferSize];
                uv = new Vector2[bufferSize];
                triangles = new int[bufferSize];

                // pin memory
                gcHandlerVertices = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                gcHandlerNormals = GCHandle.Alloc(normals, GCHandleType.Pinned);
                gcHandlerUV = GCHandle.Alloc(uv, GCHandleType.Pinned);
                gcHandlerTriangles = GCHandle.Alloc(triangles, GCHandleType.Pinned);
            }
        }

        public void ClearMeshBuffer()
        {
            if (bufferSize == 0)
                return;

            if (gcHandlerVertices.IsAllocated) gcHandlerVertices.Free();
            if (gcHandlerUV.IsAllocated) gcHandlerUV.Free();
            if (gcHandlerTriangles.IsAllocated) gcHandlerTriangles.Free();
            if (gcHandlerNormals.IsAllocated) gcHandlerNormals.Free();
            bufferSize = 0;
            vertices = null;
            normals = null;
            uv = null;
            triangles = null;
        }

        public void AllocTextureBuffer(int width, int height, TextureFormat textFormat)
        {
            if (textWidth != width || textHeight != height)
            {
                ClearTextureBuffer();
                colors = new Color32[width * height];
                gcHandlerColors = GCHandle.Alloc(colors, GCHandleType.Pinned);

                textWidth = width;
                textHeight = height;
            }
        }

        public void ClearTextureBuffer()
        {
            if (textWidth == 0 || textHeight == 0)
                return;

            if (gcHandlerColors.IsAllocated) gcHandlerColors.Free();
            colors = null;
            textWidth = 0;
            textHeight = 0;
        }
    }

    public class MeshReader
    {
        // public
        public int ApiKey = -1;
        public TextureFormat TextFormat;
        public int TextureWidth, TextureHeight;

        public string SourceUrl = "";
        public float SourceDurationSec = 0, SourceFPS = 0;
        public int SourceNbFrames = 0;

        //added by lhy,to record first mesh pts sec
        public float FirstPtsSecInRealTime = -1;
        public bool FirstRecordInRealTime = true;

        public MeshData MeshData = null;

        // create api instance
        static public MeshReader CreateMeshReader(ref int apiKey)
        {
            MeshReader instance = new MeshReader(apiKey);
            if (instance.ApiKey == -1)
                return null;

            return instance;
        }

        // MeshReader
        private MeshReader(int apiKey)
        {
            if (apiKey == -1)
                ApiKey = ReaderAPI.CreateApiInstance();
            else
                ApiKey = apiKey;

            MeshData = new MeshData();

            Debug.Log("[MeshReader] Create API instance " + ApiKey);
        }

        ~MeshReader()
        {
            Release();
        }

        public void Release()
        {
            MeshData.ClearMeshBuffer();
            MeshData.ClearTextureBuffer();
        }

        public int getMeshApiKey()
        {
            return ApiKey;
        }

        // Control
        public bool OpenMeshStream(string sourceUrl, bool mDataInStreamingAssets)
        {
            string url = sourceUrl;
            if (!url.StartsWith("http") && !url.StartsWith("rtmp") && mDataInStreamingAssets)
            {
                url = Application.streamingAssetsPath + "/" + sourceUrl;
                Debug.Log("[MeshReader] Open in StreamingAssets: " + url);

                //ANDROID STREAMING ASSETS => need to copy the data somewhere else on device to acces it, beacause it is currently in jar file
                if (url.StartsWith("jar"))
                {
                    WWW www = new WWW(url);
                    //yield return www; //can't do yield here, not really blocking beacause the data is local
                    while (!www.isDone) ;

                    if (!string.IsNullOrEmpty(www.error))
                    {
                        Debug.LogError("[MeshReader] PATH : " + url);
                        Debug.LogError("[MeshReader] Can't read data in streaming assets: " + www.error);
                    }
                    else
                    {
                        //copy data on device
                        url = Application.persistentDataPath + "/" + sourceUrl;
                        if (!System.IO.File.Exists(url))
                        {
                            Debug.Log("[MeshReader] NEW Roopath: " + url);
                            System.IO.FileStream fs = System.IO.File.Create(url);
                            fs.Write(www.bytes, 0, www.bytesDownloaded);
                            Debug.Log("[MeshReader] data copied");
                            fs.Dispose();
                        }
                    }
                }
            }
            
            if (!ReaderAPI.OpenMeshStream(ApiKey, url))
                return false;

            ReaderAPI.GetResolution(ApiKey, ref TextureWidth, ref TextureHeight);
            ReaderAPI.GetMeshStreamInfo(ApiKey, ref SourceDurationSec, ref SourceFPS, ref SourceNbFrames);
            TextFormat = TextureFormat.ARGB32;

            Debug.Log("[MeshPlayerPlugin] Open Success!");
            Debug.Log("[MeshReader] Stream Duration = " + SourceDurationSec);
            Debug.Log("[MeshReader] Stream FPS = " + SourceFPS);
            Debug.Log("[MeshReader] Stream Number of Frames = " + SourceNbFrames);
            return true;
        }

        public bool StartFromSecond(float sec)
        {
            ReaderAPI.SetReaderStartSecond(ApiKey, sec);
            return false;
        }

        public bool StartFromFrameIdx(int frmIdx)
        {
            return false;
        }

        public void SetSpeedRatio(float speedRatio)
        {
            ReaderAPI.SetSpeedRatio(ApiKey, speedRatio);
            return;
        }

        public void Play()
        {
            ReaderAPI.PlayReader(ApiKey);
        }

        public void Pause()
        {
            ReaderAPI.PauseReader(ApiKey);
        }

        public void ForwardOneFrame()
        {
            ReaderAPI.ForwardOneFrame(ApiKey);
        }

        // Access Data
        public bool ReadNextFrame(ref float ptsSec)
        {
            //Debug.Log("[MeshReader] ReadNextFrame()");

            if (!ReaderAPI.BeginReadFrame(ApiKey, ref ptsSec))
                return false;
            //Debug.Log("~~~~~~~~~~~~~~~~~~~~~[ReadNextFrame] Video ptsSec ........" + ptsSec);
            MeshData.ptsSec = ptsSec;
            int numTris = ReaderAPI.GetVerticesCount(ApiKey);
            MeshData.AllocMeshBuffer(numTris);
            ReaderAPI.SetMeshVertices(ApiKey, MeshData.bufferSize, 
                MeshData.gcHandlerVertices.AddrOfPinnedObject(),
                MeshData.gcHandlerNormals.AddrOfPinnedObject(),
                MeshData.gcHandlerUV.AddrOfPinnedObject(),
                MeshData.gcHandlerTriangles.AddrOfPinnedObject());
            //Debug.Log("[MeshReader] Read Mesh Triangles = " + numTris);

            MeshData.AllocTextureBuffer(TextureWidth, TextureHeight, TextFormat);
            ReaderAPI.SetMeshTextures(ApiKey, TextureWidth, TextureHeight, 4,
                MeshData.gcHandlerColors.AddrOfPinnedObject());
            //Debug.Log("[MeshReader] Read Texture = " + TextureWidth + "x" + TextureHeight);

            ReaderAPI.EndReadFrame(ApiKey);
            return true;
        }

        public bool ReadNextFrame(ref float ptsSec,ref float soundSec, ref float lastTimeGap)
        {
            //Debug.Log("[MeshReader] ReadNextFrame()");

            if (!ReaderAPI.BeginReadFrameWithSoundSec(ApiKey, ref ptsSec, ref soundSec, ref lastTimeGap))
                return false;
            Debug.Log("~~~~~~~~~~~~~~~~~~~~~[ReadNextFrame] Video ptsSec ........" + ptsSec);
            if (FirstRecordInRealTime && ptsSec > 0)
            {
                FirstPtsSecInRealTime = ptsSec;
                FirstRecordInRealTime = false;
            }
            MeshData.ptsSec = ptsSec;
            int numTris = ReaderAPI.GetVerticesCount(ApiKey);
            MeshData.AllocMeshBuffer(numTris);
            ReaderAPI.SetMeshVertices(ApiKey, MeshData.bufferSize,
                MeshData.gcHandlerVertices.AddrOfPinnedObject(),
                MeshData.gcHandlerNormals.AddrOfPinnedObject(),
                MeshData.gcHandlerUV.AddrOfPinnedObject(),
                MeshData.gcHandlerTriangles.AddrOfPinnedObject());
            //Debug.Log("[MeshReader] Read Mesh Triangles = " + numTris);

            MeshData.AllocTextureBuffer(TextureWidth, TextureHeight, TextFormat);
            ReaderAPI.SetMeshTextures(ApiKey, TextureWidth, TextureHeight, 4,
                MeshData.gcHandlerColors.AddrOfPinnedObject());
            //Debug.Log("[MeshReader] Read Texture = " + TextureWidth + "x" + TextureHeight);

            ReaderAPI.EndReadFrame(ApiKey);
            return true;
        }


    }

}
