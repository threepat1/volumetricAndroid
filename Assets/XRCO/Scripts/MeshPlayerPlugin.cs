/* Mesh Player Plugin for Unity.
*  All Rights Reserved. XR Company 2020.
*/
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEditor;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace prometheus
{
    public enum SOURCE_TYPE
    {
        PLAYBACK = 0,
        RTMP = 2,
    }

    public enum MODE_TYPE
    {
        WITHOUT_SOUND = 0,
        WITH_SOUND = 1,
    }


    public class MeshPlayerPlugin : MonoBehaviour
    {
        #region Properties
        //-----------------------------//
        //-  PROPERTIES               -//
        //-----------------------------//
        public float CurrentSec { get { return mCurrentPts; } set { GotoSecond(value); } }
        public int CurrentFrame { get { return mCurrentFrameIdx; } set { GotoFrameIdx((int)value); } }
        public float SourceDurationSec { get { return mSourceDurationSec; } }
        public float FPS { get { return mSourceFPS; } }
        public int SourceNbFrames { get { return mSourceNbFrames; } }

        public bool IsInitialized { get { return mIsInitialized; } }
        public bool IsOpened { get { return mIsOpened; } }
        public bool AutoPlay { get { return mAutoPlay; } set { mAutoPlay = value; } }
        public bool IsPlaying { get { return mIsPlaying; } set { mIsPlaying = value; } }
        public bool Loop { get { return mIsLoop; } set { mIsLoop = value; } }

        public SOURCE_TYPE SourceType { get { return mSourceType; } set { mSourceType = value; } }
        public string SourceUrl { get { return mSourceUrl; } set { mSourceUrl = value; } }
        public bool DataInStreamingAssets { get { return mDataInStreamingAssets; } set { mDataInStreamingAssets = value; } }
        //public string SequenceDataPath { get { return _mainDataPath; } set { _mainDataPath = value; } }
       
        public float SpeedRatio { get { return mSpeedRatio; } set { mSpeedRatio = value; } }
        public float PreviewSec { get { return mPreviewSec; } set { mPreviewSec = value; } }

        public bool DebugInfo { get { return mDebugInfo; } set { mDebugInfo = value; } }

        public BoxCollider boxCollider;
        #endregion

        #region Variables
        //-----------------------------//
        //-  Variables                -//
        //-----------------------------//
        public MeshReader mReader = null;
        public AudioReader mAudioReader = null;

        [SerializeField] private string mSourceUrl;
        [SerializeField] private SOURCE_TYPE mSourceType = SOURCE_TYPE.PLAYBACK;
        [SerializeField] private bool mDataInStreamingAssets = false;

        [SerializeField] private float mSourceDurationSec = 0;
        [SerializeField] private float mSourceFPS = 0;
        [SerializeField] private int mSourceNbFrames = 0;

        [SerializeField] private float mCurrentPts = -1;
        [SerializeField] private int mCurrentFrameIdx = 0;
        [SerializeField] private float mStartSecond = 0;
        [SerializeField] private float mEndSecond = -1;
        [SerializeField] private float mSpeedRatio = 1.0f;
        [SerializeField] private float mPreviewSec = -1.0f;

        private bool mIsInitialized = false;
        private bool mIsOpened = false;
        private int mApiKey = -1;
        [HideInInspector]
        public bool mIsPlaying = false;
        [SerializeField] private bool mAutoPlay = true;
        [SerializeField] private bool mIsLoop = true;

        [SerializeField] private bool mDebugInfo = true;

        // gui handler
        private MeshFilter mMeshComponent;
        public Renderer mRendererComponent;
        private MeshCollider mMeshCollider;

        // mesh buffer
        private Mesh[] mMeshes = null;
        private Texture2D[] mTextures = null;
        private int mBufferSize = 2;
        private int mBufferIdx = 0;

        private bool mIsMeshDataUpdated = false;
        private bool mIsFirstMeshDataArrived = false;

        // short max
        private const int MAX_SHORT = 65535;

        AudioClip mAudioClip;
        AudioSource mAudioSource;
        
        //control audioSource play speed(mfPitch) and volume(mfVolume)
        private float mfPitch = 1.0f;
        private float mfVolume = 1.0f;
        private bool mfLoop = false;
        private bool mIsFirstFrameReady = false;
        private bool mIsStartAudioTimeRecorded = false;
        private bool mIsMeshAligned = false;
        

        //control mode have sound or not
        private MODE_TYPE mModeType = MODE_TYPE.WITH_SOUND;
        private bool mIsModeChanged = false;

        //audio params
        private UInt16 mAudioChannels = 0;
        private int mAudioSampleRate = 0;

        //audio data store
        private List<float[]> mListAudio;
        private List<double> mListAudioPts;
        private List<float> mListAudioPtsTime;

        private int mAudioCount = 0;
        //create 60 sec space to store audio data
        private float ratio = 60.0f;
        private bool mLoopTag = false;
        private int mAudioLengthSamples = 0;
        private int mAudioLoopCountInRtmp = 0;
        private int mLastAudioPtsNum = 0;
        //first audio time of mListAudioPtsTime,
        private float mStartAudioTime = 0.0f;
        //start time of timeline
        public float mStartTimeUseTimeLine = 0.0f;
        
        //m_realtime,if audio data is not enough ,then pause
        private float mLatestAudioTime = 0.0f;
        private float mAudioMidwayPlayTime = 0.0f;
        private bool mIsAudioPaused = false;
        //max audio and mesh diff is 5 sec
        private float mMaxDiffAudioAndMesh = 5.0f;

        //mAudioSourcePlayTime>0, equal to mAudioSource.time
        //mAudioSourcePlayTime < 0. mean the video is going to play back
        private float mAudioSourcePlayTime = 0.0f;

        //delete first audio frame 
        private int mNumOfDelStartAudio = 3;
        
        private int mAudioStartPts = 0;

        //if false, means audio is not allowed, though audio data existed,default true
        private bool mAudioMainSwitch = true;

        //time gap return the gap between audio pts and mesh pts, if gap is large, stop playing audio temporary
        private float mAudioMeshLastTimeGap = -1.0f;
        private float mAudioMeshCurTimeGap = -1.0f;
        private float mAudioMeshThreshold = 0.5f;
        #endregion
       
        #region Events
        //-----------------------------//
        //-  EVENTS                   -//
        //-----------------------------//
        public delegate void EventFDV();
        public event EventFDV OnNewModel;
        public event EventFDV OnModelNotFound;
        public event EventFDV OnLastFrame;
        #endregion

        #region Functions
        //-----------------------------//
        //- Functions                 -//
        //-----------------------------//
        public void SetAudioSpeedRatio(float speedRatio)
        {
            mfPitch = speedRatio;
        }

        public void GotoSecond(float sec)
        {
            mCurrentPts = sec;
            if (mReader == null)
                return;

            Debug.Log("[MeshPlayerPlugin] GotoSecond(): " + sec);
            mReader.StartFromSecond(sec);
        }

        public bool GotoFrameIdx(int frmIdx)
        {
            if (mReader != null)
                return mReader.StartFromFrameIdx(frmIdx);

            return false;
        }

        public void Initialize()
        {
           
            if (mIsInitialized && mReader != null)
                return;

            Debug.Log("[MeshPlayerPlugin] Initialize()");
           
            mDebugInfo = false;
            // disable if cannot compile -->
            if (mDebugInfo)
            {
                DebugDelegate callback_delegate = new DebugDelegate(CallbackDebug);
                System.IntPtr intptr_delegate = Marshal.GetFunctionPointerForDelegate(callback_delegate);
                ReaderAPI.SetDebugFunction(intptr_delegate);
            }
            // <-- disable if cannot compile

            // create reader
            if (mReader == null)
            {
                Debug.Log("[MeshPlayerPlugin] Create Reader Instance");
               
                mReader = MeshReader.CreateMeshReader(ref mApiKey);
                if (mReader == null)
                {
                    Debug.Log("[MeshPlayerPlugin][WARNING] Create Reader Instance Failed");
                    
                    OnModelNotFound?.Invoke();
                    return;
                }
                Debug.Log("[MeshPlayerPlugin] Create Reader Success");
               
            }

            //initial audio reader
            if (mAudioReader == null)
            {
                Debug.Log("[MeshPlayerPlugin] Create Audio Reader Instance");
                mAudioReader = AudioReader.CreateAudioReader(mReader.getMeshApiKey());
                if (mAudioReader == null)
                {
                    Debug.Log("[MeshPlayerPlugin][WARNING] Create Reader Instance Failed");
                    OnModelNotFound?.Invoke();
                    return;
                }
                Debug.Log("[MeshPlayerPlugin] Create Reader Success");
            }

            mMeshComponent = GetComponent<MeshFilter>();
            mRendererComponent = GetComponent<Renderer>();
            mMeshCollider = GetComponent<MeshCollider>();

            mIsFirstMeshDataArrived = false;
            //audio bool params
            mIsFirstFrameReady = false;
            mIsStartAudioTimeRecorded = false;
            mIsModeChanged = false;

            mIsInitialized = true;
           
        }

        public void Uninitialize()
        {
            if (!mIsInitialized)
                return;

            ReaderAPI.UnityPluginUnload();
            mReader = null;

            mAudioReader = null;
            mIsOpened = false;

            //_isSequenceTriggerON = false;
            mIsInitialized = false;

#if UNITY_EDITOR
            //EditorApplication.pauseStateChanged -= HandlePauseState;
#endif
        }

        public void OpenSourceAsync(string str)
        {
            mSourceUrl = str;
            OpenCurrentSourceAsync(allowAutoPlay: true);
        }

        public void OpenSource(string str)
        {
            mSourceUrl = str;
            OpenCurrentSource(allowAutoPlay: true);
        }

        public void EnableAudio(bool bo)
        {
            mAudioReader.setAudioMainSwitch(bo);
        }

        public bool OpenCurrentSource(bool allowAutoPlay = true)
        {
            Initialize();


            if (mSourceUrl == "")
                return false;

            //initial audio player
            //mAudioSource = gameObject.GetComponent<AudioSource>();

            Debug.Log("[MeshPlayerPlugin] Open " + mSourceUrl);
            mIsOpened = mReader.OpenMeshStream(mSourceUrl, mDataInStreamingAssets);

            if (mIsOpened)
            {
                //get audio stream info
                mAudioReader.audioStreamInfo();
                mAudioChannels = mAudioReader.mChannels;
                mAudioSampleRate = mAudioReader.mSampleRate;
                
                //check have audio or not
                if (mAudioChannels > 0 && mAudioSampleRate > 0)
                    mModeType = MODE_TYPE.WITH_SOUND;
                else
                    mModeType = MODE_TYPE.WITHOUT_SOUND;
                
                mAudioReader.setAudioMainSwitch(mAudioMainSwitch);
                if (!mAudioMainSwitch)
                {
                    mModeType = MODE_TYPE.WITHOUT_SOUND;
                }
                    
                if (mSourceType == SOURCE_TYPE.PLAYBACK)
                {
                    ratio = (int)mReader.SourceDurationSec + 5;
                }else if(mSourceType == SOURCE_TYPE.RTMP)
                {
                    ratio = 600;
                }
                mAudioLengthSamples = getAudioLengthSample(mAudioSampleRate * (int)ratio, 1024);
                
                mSourceDurationSec = mReader.SourceDurationSec;
                mSourceFPS = mReader.SourceFPS;
                mSourceNbFrames = mReader.SourceNbFrames;
                AllocMeshBuffers();

                mReader.SetSpeedRatio(mSpeedRatio);
                mCurrentPts = -1;
                mReader.StartFromSecond(mStartSecond);
                
                //initial mAudioList
                mListAudio = new List<float[]>();
                mListAudioPts = new List<double>();
                mListAudioPtsTime = new List<float>();


                if (allowAutoPlay && mAutoPlay)
                {
                    Debug.Log("[MeshPlayerPlugin] Auto Play");
                    Play();
                }
                else
                    Preview();
            }
            else
            {
                Debug.Log("[MeshPlayerPlugin] Open Failed!");
            }

            return mIsOpened;
        }

        public async void OpenCurrentSourceAsync(bool allowAutoPlay = true)
        {

            Initialize();

            if (mSourceUrl == "")
                return;

            Debug.Log("[MeshPlayerPlugin] Open " + mSourceUrl);
            mIsOpened = await Task.Run(() => mReader.OpenMeshStream(mSourceUrl, mDataInStreamingAssets));

            if (mIsOpened)
            {
                mSourceDurationSec = mReader.SourceDurationSec;
                mSourceFPS = mReader.SourceFPS;
                mSourceNbFrames = mReader.SourceNbFrames;
                AllocMeshBuffers();

                mReader.SetSpeedRatio(mSpeedRatio);
                mCurrentPts = -1;
                mReader.StartFromSecond(mStartSecond);
                if (allowAutoPlay && mAutoPlay)
                {
                    Debug.Log("[MeshPlayerPlugin] Auto Play");
                    Play();
                }
                else
                    Preview();
            }
            else
            {
                Debug.Log("[MeshPlayerPlugin] Open Failed!");
            }
        }

        private void AllocMeshBuffers()
        {
            //Allocates objects buffers for double buffering
            mMeshes = new Mesh[mBufferSize];
            mTextures = new Texture2D[mBufferSize];

            for (int i = 0; i < mBufferSize; i++)
            {
                //Mesh
                Mesh mesh = new Mesh();

                Bounds newBounds = mesh.bounds;
                newBounds.extents = new Vector3(2, 2, 2);
                mesh.bounds = newBounds;
                mMeshes[i] = mesh;
            }

            for (int i = 0; i < mBufferSize; i++)
            {
                //Texture
                Texture2D texture = new Texture2D(mReader.TextureWidth, mReader.TextureHeight, mReader.TextFormat, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                texture.Apply(); //upload to GPU
                mTextures[i] = texture;
            }

            mBufferIdx = 0;
        }

        public void CleanBuffer()
        {
            if (mMeshes != null)
            {
                for (int i = 0; i < mMeshes.Length; i++)
                    DestroyImmediate(mMeshes[i]);
                mMeshes = null;
            }
            if (mTextures != null)
            {
                for (int i = 0; i < mTextures.Length; i++)
                    DestroyImmediate(mTextures[i]);
                mTextures = null;
            }
            mReader.MeshData.ClearMeshBuffer();
            mReader.MeshData.ClearTextureBuffer();
            mIsInitialized = false;
        }

        public void Play()
        {
            if (mReader == null)
                return;

            if (mAudioReader == null)
                return;
            mIsPlaying = false;
            if (!mIsPlaying)
            {
                StartCoroutine("SequenceTrigger");
                
                if (mModeType == MODE_TYPE.WITH_SOUND)
                {
                    StartCoroutine("SequenceAudioTrigger");
                }
                Debug.Log("[MeshPlayerPlugin] Play()");
                mReader.Play();
                mIsPlaying = true;

                InitBoxCollider();
            }
        }

        public void Pause()
        {
            if (mReader == null)
                return;

            if (mIsPlaying)
            {
                StopCoroutine("SequenceTrigger");
                
                if (mModeType == MODE_TYPE.WITH_SOUND)
                {
                    StopCoroutine("SequenceAudioTrigger");
                }

                Debug.Log("[MeshPlayerPlugin] Pause()");
                mReader.Pause();
                mIsPlaying = false;
            }
        }

        public void Preview()
        {
            if (mReader == null)
                return;
            if (mModeType == MODE_TYPE.WITH_SOUND)
            {
                mModeType = MODE_TYPE.WITHOUT_SOUND;
                mIsModeChanged = true;
            }
                
            Debug.Log("[MeshPlayerPlugin] Preview()");
            Pause();
            GotoSecond(mPreviewSec);
            mReader.ForwardOneFrame();

            mIsPlaying = true;
            UpdateMesh();
            Update();
            mIsPlaying = false;

            if (mIsModeChanged)
            {
                mModeType = MODE_TYPE.WITH_SOUND;
                mIsModeChanged = false;
            }
            //if preview finished ,set mIsMeshDataUpdated = false;
            if (mIsMeshDataUpdated)
                mIsMeshDataUpdated = false;
        }

        public void Destroy()
        {
            Pause();
            Uninitialize();
            mPreviewSec = -1.0f;

            if (mMeshes != null)
            {
                for (int i = 0; i < mMeshes.Length; i++)
                    DestroyImmediate(mMeshes[i]);
                mMeshes = null;
            }
            if (mTextures != null)
            {
                for (int i = 0; i < mTextures.Length; i++)
                    DestroyImmediate(mTextures[i]);
                mTextures = null;
            }
            
            if (mListAudio != null)
                mListAudio.Clear();

            if (mListAudioPts != null)
                mListAudioPts.Clear();

            if (mListAudioPtsTime != null)
                mListAudioPtsTime.Clear();

        }

        #endregion

        #region routine
        // callback for debug
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugDelegate(string str);

        [AOT.MonoPInvokeCallback(typeof(DebugDelegate))]
        static void CallbackDebug(string str)
        {
            Debug.Log(str);
        }

        void InitPlugin()
        {
            Debug.Log("[MeshPlayerPlugin] InitPlugin()");
#if UNITY_WEBGL && !UNITY_EDITOR
		    RegisterPlugin();
#endif
            Uninitialize();

            mIsInitialized = false;
            mIsOpened = false;
            mIsPlaying = false;

            mApiKey = ReaderAPI.CreateApiInstance();
            Initialize();

        }

        void Start()
        {
            StartMeshPlayer();

        }

        public void StartMeshPlayer()
        {
            mDebugInfo = false;
            // disable if cannot compile -->
            if (mDebugInfo)
            {
                DebugDelegate callback_delegate = new DebugDelegate(CallbackDebug);
                System.IntPtr intptr_delegate = Marshal.GetFunctionPointerForDelegate(callback_delegate);
                ReaderAPI.SetDebugFunction(intptr_delegate);
            }
            // <-- disable if cannot compile

            if (mReader == null)
                return;
            if (mSourceUrl != null)
                OpenCurrentSource();
        }

        void Update()
        {
            if (mDebugInfo)
                Debug.Log("[MeshPlayerPlugin] Update()");
            if (!mIsInitialized)
                Initialize();

            if (!mIsOpened)
                return;

            if (!checkIsReadyToPlay())
                return;
            AW
            //if (!mIsMeshAligned)
            //{
            //    mIsMeshAligned = makeAudioAndMeshAligned();

            //}
            if (mModeType == MODE_TYPE.WITH_SOUND)
            {
                if (mIsPlaying == false)
                {
                    if (mAudioSource != null)
                    {
                        mAudioSource.Pause();
                    }
                    return;
                }
                else
                {
                    if (mAudioSource != null && mIsFirstFrameReady == true)
                    {
                        //if in live ,pts time is not started at 0
                        if (!mIsStartAudioTimeRecorded)
                        {
                            if (mListAudioPtsTime.Count > 0)
                            {
                                mStartAudioTime = mListAudioPtsTime[0];
                                Debug.Log("start_time: " + mStartAudioTime);
                                mIsStartAudioTimeRecorded = true;
                            }
                        }

                        if (mAudioSource.isPlaying == false)
                        {
                            mAudioSource.Play();
                        }
                        

                        if (mSourceType == SOURCE_TYPE.RTMP && mAudioSource != null && mAudioMeshCurTimeGap > mAudioMeshThreshold)
                        {
                            if(mAudioMeshLastTimeGap > mAudioMeshThreshold && mAudioMeshCurTimeGap > mAudioMeshLastTimeGap)
                            {
                                if (mAudioSource.time - mAudioMeshCurTimeGap - 3 > 0)
                                    mAudioSource.time = mAudioSource.time - mAudioMeshCurTimeGap - 3;
                                else
                                    mAudioSource.time = 0;
                            }
                            mAudioMeshLastTimeGap = mAudioMeshCurTimeGap;
                            
                        }
                        //use mLoopTag check if mAudioSource is play back
                        //if (mAudioSource.time >= ratio/2)
                        //{
                        //    mLoopTag = true;
                        //}
                        //if(mAudioSource.time < 0.1 &&mLoopTag)
                        //{
                        //    mAudioLoopCount++;
                        //    mLoopTag = false;
                        //}
                        //if playback
                        //mAudioSourcePlayTime<0 ,play back
                        updateAudioSourcePlayTime();
                             
                    }
                }

                //if realtime, to solve problem with audio data not enough
                //update latest audio pts_sec
                //if (mSourceType == SOURCE_TYPE.RTMP && mListAudioPtsTime.Count > 0 && mStartAudioTime > 0)
                //{
                //    int counts = mListAudioPtsTime.Count;
                //    float time_ = (mListAudioPtsTime[counts - 1] - mStartAudioTime) % ratio;
                //    if (mLatestAudioTime < time_)
                //        mLatestAudioTime = time_;
                //}
                
                //if (mSourceType == SOURCE_TYPE.RTMP && mAudioSource != null && mAudioSource.isPlaying == true)
                //{
                //    //如果间隔太短（<2s），则暂停播放
                //    if (mAudioSource.time > mLatestAudioTime - 2)
                //    {
                //        mAudioMidwayPlayTime = mAudioSource.time % ratio;
                //        mAudioSource.Pause();
                //        mIsAudioPaused = true;
                //    }
                //}

                //if (mSourceType == SOURCE_TYPE.RTMP && mAudioSource != null && mIsAudioPaused == true)
                //{
                //    Debug.Log("~~~~~~mIsAudioPaused~~~~~~~ ");
                //    //如果间隔足够（>5s），则重新播放
                //    if (mAudioMidwayPlayTime < mLatestAudioTime - 5)
                //    {
                //        mAudioSource.time = mAudioMidwayPlayTime;
                //        mAudioSource.Play();
                //        mIsAudioPaused = false;
                //    }
                //}

                // create audiosource object and audio clip
                if (mAudioSource == null)
                {
                    mAudioSource = gameObject.AddComponent<AudioSource>();
                    mAudioSource.pitch = mfPitch;
                    mAudioSource.volume = mfVolume;
                    mAudioSource.loop = mfLoop;
                }
                if (mAudioClip == null && mAudioSource != null)
                {
                    
                    //if (mSourceType == SOURCE_TYPE.PLAYBACK)
                    //    ratio = (int)mSourceDurationSec + 5;
                    //else if (mSourceType == SOURCE_TYPE.RTMP)
                    //    ratio = 600;
                    //mAudioLengthSamples = getAudioLengthSample(mAudioSampleRate * (int)ratio, 1024);
                    mAudioClip = AudioClip.Create("videoAudio", mAudioLengthSamples, mAudioChannels, mAudioSampleRate, false);
                    mAudioSource.clip = mAudioClip;
                }
            }

           
            //add sound data to mAudioClip
            if (mModeType == MODE_TYPE.WITH_SOUND)
            {
                for (int i = 0; i < mListAudio.Count; i++)
                {

                    if (mAudioSource != null)
                    {
                        if (mListAudioPts.Count > i && mListAudioPts[i] >= 0)
                        {
                            mAudioClip.SetData(mListAudio[i], (int)((mListAudioPts[i] - mAudioStartPts) % mAudioLengthSamples));
                        }
                    }
                }

                if (mAudioSource != null && mAudioSource.isPlaying)
                {
                    mListAudio.Clear();
                    mListAudioPts.Clear();
                    mListAudioPtsTime.Clear();
                }
            }
            //update mesh 
            //mesh display controlled by audioSource.time (if audio existed)
            updateAndDiaplayMesh();
           
            //else
            //{
            //    if (mModeType == MODE_TYPE.WITH_SOUND)
            //    { 
            //        if (mIsMeshDataUpdated && !mIsAudioPaused)
            //        {
            //            //float pts_sec = mReader.MeshData.ptsSec;
            //            float pts_sec = pts_next_mesh;
            //            Debug.Log("mesh pts_sec: " + pts_sec);
            //            //Debug.Log("....Before mLastFrameTime" + mLastFrameTime + " mCurrentSeekTime" + mCurrentSeekTime + " pts_next_mesh" + pts_next_mesh + " mAudioSource.time" + mAudioSource.time);
            //            updateAndDiaplayMesh();
            //            //Debug.Log("....After mLastFrameTime" + mLastFrameTime + " mCurrentSeekTime" + mCurrentSeekTime + "pts_next_mesh" + pts_next_mesh + " mAudioSource.time" + mAudioSource.time);

            //            if (mLastFrameTime == 0)
            //            {
            //                if (pts_sec < 0)
            //                {
            //                    mLastFrameTime = 0;
            //                }
            //                else
            //                {
            //                    mCurrentSeekTime = pts_sec;
            //                    mLastFrameTime = pts_sec;
            //                }
            //                if (mAudioSource != null)
            //                    mAudioSource.time = mLastFrameTime % ratio;
            //            }
            //            else
            //            {
            //                if (pts_sec <= 0)
            //                    mLastFrameTime = mCurrentSeekTime - 0.05f;
            //                else
            //                    mLastFrameTime = pts_sec;
            //            }
            //        }

            //        //update audio data
            //        if (mListAudio.Count > 0)
            //        {
            //            //if (mAudioSource == null /*&& (int)((float)pAudioCodecContext->sample_rate * ((float)listAudioPtsTime[i] + ((float)Call_GetDuration() / 1000.0f))) > 0*/)
            //            //{
            //            //    mAudioSource = gameObject.AddComponent<AudioSource>();
            //            //}
            //            //if (mAudioClip == null && mAudioClip != null)
            //            //{
            //            //    mAudioClip = AudioClip.Create("videoAudio", mAudioSampleRate * (int)ratio, mAudioChannels, mAudioSampleRate, false);
            //            //    mAudioSource.clip = mAudioClip;
            //            //}
            //            for (int i = 0; i < mListAudio.Count; i++)
            //            {
            //                if (mListAudioPts.Count > i && mListAudioPts[i] >= 0)
            //                {
            //                    mAudioClip.SetData(mListAudio[i], (int)((mListAudioPts[i] - mAudioStartPts) % (mAudioSampleRate * ratio)));
            //                }
            //            }
            //        }
            //    }else
            //    {
            //        updateAndDiaplayMesh();
            //    }

            //}

        }

        //if (false && mIsPlaying && mIsAudioDataUpdated)


        //            if (mIsPlaying && mIsMeshDataUpdated)
        //            {
        //                Mesh mesh = mMeshes[mBufferIdx];
        //                mesh.MarkDynamic();

        //                if (mMeshList[0].vertices.Length > MAX_SHORT)
        //                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        //                mesh.vertices = mMeshList[0].vertices;
        //                mesh.normals = mMeshList[0].normals;
        //                mesh.uv = mMeshList[0].uv;
        //                mesh.triangles = mMeshList[0].triangles;
        //                mesh.UploadMeshData(false);

        //                Texture2D texture = mTextures[mBufferIdx];
        //                //texture.LoadRawTextureData(mReader.MeshData.colors);
        //                texture.SetPixels32(mMeshList[0].colors);
        //                texture.Apply();

        //                mMeshList.RemoveAt(0);

        //                //if (mReader.MeshData.vertices.Length > MAX_SHORT)
        //                //    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        //                //mesh.vertices = mReader.MeshData.vertices;
        //                //mesh.normals = mReader.MeshData.normals;
        //                //mesh.uv = mReader.MeshData.uv;
        //                //mesh.triangles = mReader.MeshData.triangles;
        //                //mesh.UploadMeshData(false);

        //                //Texture2D texture = mTextures[mBufferIdx];
        //                ////texture.LoadRawTextureData(mReader.MeshData.colors);
        //                //texture.SetPixels32(mReader.MeshData.colors);
        //                //texture.Apply();

        //                // display
        //                mMeshComponent.sharedMesh = mesh;
        //#if UNITY_EDITOR
        //                            //如果场景中需要多个meshPlayerPlugin实例，将sharedMaterial替换成Material
        //                            mRendererComponent.sharedMaterial.mainTexture = texture;
        //#else
        //                mRendererComponent.material.mainTexture = texture;
        //#endif

        //                //                if (mRendererComponent.sharedMaterial.HasProperty("_BaseMap"))
        //                //                    mRendererComponent.sharedMaterial.SetTexture("_BaseMap", texture);
        //                //                else if (mRendererComponent.sharedMaterial.HasProperty("_BaseColorMap"))
        //                //                    mRendererComponent.sharedMaterial.SetTexture("_BaseColorMap", texture);
        //                //                else if (mRendererComponent.sharedMaterial.HasProperty("_UnlitColorMap"))
        //                //                    mRendererComponent.sharedMaterial.SetTexture("_UnlitColorMap", texture);
        //                //                else
        //                //                {
        //                //#if UNITY_EDITOR
        //                //                    var tempMaterial = new Material(mRendererComponent.sharedMaterial);
        //                //                    tempMaterial.mainTexture = texture;
        //                //                    mRendererComponent.sharedMaterial = tempMaterial;
        //                //#else
        //                //                    mRendererComponent.material.mainTexture = texture;
        //                //#endif
        //                //                }

        //                // done with buffer
        //                mBufferIdx = (mBufferIdx + 1) % mBufferSize;

        //                // event
        //                OnNewModel?.Invoke();
        //                mIsMeshDataUpdated = false;

        //                if (mMeshCollider && mMeshCollider.enabled)
        //                    mMeshCollider.sharedMesh = mesh;
        //            }


        void updateAndDiaplayMesh()
        {
            // display
			Mesh mesh = mMeshes[mBufferIdx];
			mesh.MarkDynamic();
			if (mReader.MeshData.vertices.Length > MAX_SHORT)
				mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

			mesh.vertices = mReader.MeshData.vertices;
			mesh.normals = mReader.MeshData.normals;
			mesh.uv = mReader.MeshData.uv;
			mesh.triangles = mReader.MeshData.triangles;
			mesh.UploadMeshData(false);
			mesh.RecalculateNormals();
			Texture2D texture = mTextures[mBufferIdx];
			//texture.LoadRawTextureData(mReader.MeshData.colors);
			texture.SetPixels32(mReader.MeshData.colors);
			texture.Apply();

            mMeshComponent.sharedMesh = mesh;
            mMeshComponent.mesh = mesh;

#if UNITY_EDITOR
                mRendererComponent.sharedMaterial.mainTexture = texture;
#else
            mRendererComponent.material.mainTexture = texture;
#endif


            //                if (mRendererComponent.sharedMaterial.HasProperty("_BaseMap"))
            //                    mRendererComponent.sharedMaterial.SetTexture("_BaseMap", texture);
            //                else if (mRendererComponent.sharedMaterial.HasProperty("_BaseColorMap"))
            //                    mRendererComponent.sharedMaterial.SetTexture("_BaseColorMap", texture);
            //                else if (mRendererComponent.sharedMaterial.HasProperty("_UnlitColorMap"))
            //                    mRendererComponent.sharedMaterial.SetTexture("_UnlitColorMap", texture);
            //                else
            //                {
            //#if UNITY_EDITOR
            //                    var tempMaterial = new Material(mRendererComponent.sharedMaterial);
            //                    tempMaterial.mainTexture = texture;
            //                    mRendererComponent.sharedMaterial = tempMaterial;
            //#else
            //                    mRendererComponent.material.mainTexture = texture;
            //#endif
            //                }

            // done with buffer
            mBufferIdx = (mBufferIdx + 1) % mBufferSize;

            // event
            OnNewModel?.Invoke();
            mIsMeshDataUpdated = false;

            if (mMeshCollider && mMeshCollider.enabled)
                mMeshCollider.sharedMesh = mesh;
        }

        public void InitBoxCollider()
        {

            if (boxCollider == null)
            {
                //Debug.Log("InitBoxCollider!!");
                boxCollider = gameObject.AddComponent<BoxCollider>();
                //Debug.LogError(-boxCollider.bounds.min.y);
                //gameObject.transform.localPosition += new Vector3(0, -boxCollider.bounds.min.y, 0);
            }
            else
            {

            }
        }

        bool checkIsReadyToPlay()
        {
            if (mModeType == MODE_TYPE.WITH_SOUND)
            {
                if (mIsFirstFrameReady && mIsFirstMeshDataArrived)
                    return true;
            }else
            {
                if (mIsFirstMeshDataArrived)
                    return true;
            }
            return false;
           
        }

        void updateAudioSourcePlayTime()
        {
            if(mSourceType == SOURCE_TYPE.PLAYBACK)
            {
                if (mAudioSourcePlayTime < 0)
                {
                    mAudioSource.time = 0;
                    mAudioSourcePlayTime = 0;
                }
                else
                    mAudioSourcePlayTime = mAudioSource.time + mStartAudioTime - mStartTimeUseTimeLine;
            }
            else 
            {
                //if AudioClip have filled with audio, then set data from begining
                float sec = mAudioLengthSamples / (float)mAudioSampleRate;
                if (mAudioSource.time > sec/2)
                {
                    mLoopTag = true;
                }

                if (mAudioSource.time < 1.0f && mLoopTag)
                {
                    mAudioLoopCountInRtmp++;
                    mLoopTag = false;
                }

                //Debug.Log("mAudioSource.time: " + mAudioSource.time);
                mAudioSourcePlayTime =  mAudioSource.time + mStartAudioTime + mAudioLoopCountInRtmp * sec;
                //Debug.Log("[mAudioSourcePlayTime] " + mAudioSourcePlayTime);
            }
        }

        bool makeAudioAndMeshAligned()
        {
            if (mListAudioPtsTime.Count == 0)
                return false;

            float mesh_sec = mReader.MeshData.ptsSec;
            float diff = mListAudioPtsTime[0] - mesh_sec;
            //if audio cache is larger than mesh cache 
            if (diff<0)
            {
                float start_sec = mesh_sec - mMaxDiffAudioAndMesh;
                int start_idx = 0;
                for(int i=0;i<mListAudioPtsTime.Count;i++)
                {
                    if (mListAudioPtsTime[i] > mesh_sec - mMaxDiffAudioAndMesh)
                    {
                        start_idx = i;
                        break;
                    }    
                }
                
                //del audio data
                if (start_idx > 0)
                {
                    lock (mListAudio)
                    {
                        lock(mListAudioPts)
                        {
                            lock(mListAudioPtsTime)
                            {
                                mListAudio.RemoveRange(0, start_idx);
                                mListAudioPts.RemoveRange(0, start_idx);
                                mListAudioPtsTime.RemoveRange(0, start_idx);
                            }
                        }
                    }
                }
                mAudioStartPts = (int)mListAudioPts[0];

            }
           
            if (mListAudioPtsTime.Count > 0)
                return true;
            else
                return false; 
        }

        void UpdateMesh()
        {
            //if (mDebugInfo)
            //    Debug.Log("[MeshPlayerPlugin] UpdateMesh()");
            float ptsSec = -1;
            if (mModeType == MODE_TYPE.WITH_SOUND)
            {
                //mesh display controlled by audioSource.time(if audio existed)
                //Debug.Log("[mAudioSourcePlayTime] " + mAudioSourcePlayTime);
                if (mIsPlaying && mReader.ReadNextFrame(ref ptsSec, ref mAudioSourcePlayTime, ref mAudioMeshCurTimeGap))
                {
                    if (mAudioMeshCurTimeGap < mAudioMeshThreshold)
                    {
                        mAudioMeshLastTimeGap = -1.0f;
                        mAudioMeshCurTimeGap = -1.0f;
                    }
                    
                    //Debug.Log("------mesh pts_sec" + ptsSec);
                    //video loop mode, mAudioSourcePlayTime return -1,mean to update mAudioSource.time 
                    mIsFirstMeshDataArrived = true;
                    mIsMeshDataUpdated = true;
                   
                }
            }
            else
            {
                if (mIsPlaying && mReader.ReadNextFrame(ref ptsSec))
                //if (mIsPlaying && !mIsMeshDataUpdated &&mReader.ReadNextFrame(ref ptsSec))
                {
                    mIsFirstMeshDataArrived = true;
                    mIsMeshDataUpdated = true;
                   
                }
            }
        }

        private IEnumerator SequenceTrigger()
        {
            float triggerRate = 0.1f;
            float gap = (triggerRate / (float)mReader.SourceFPS);
            Debug.Log("[SequenceTrigger] Trigger Gap = " + gap);

            //infinite loop to keep executing this coroutine
            while (true)
            {
                UpdateMesh();
                //yield return new WaitForSeconds(gap);
                yield return 0;
            }
        }

        void UpdateAudio()
        {
            //if (mDebugInfo)
            //    Debug.Log("[MeshPlayerPlugin] UpdateAudio()");
            float ptsSec = -1;
            while (mIsPlaying && mAudioReader.GetAudioClipData(ref ptsSec))
            {
                //important if realtime, must delete audio frames until first mesh is found
                if (mSourceType == SOURCE_TYPE.RTMP && !mIsFirstMeshDataArrived)
                {
                    continue;
                }

                if (mSourceType == SOURCE_TYPE.RTMP && ptsSec < mReader.FirstPtsSecInRealTime)
                {
                    continue;
                }

                //must delete first several audio frames
                if (mNumOfDelStartAudio > 0)
                {
                    mNumOfDelStartAudio--;
                    continue;
                }

                float pts_sec = mAudioReader.AudioData.ptsSec;

                //Debug.Log("~~~~~~~~~~~~pts_sec : "  + pts_sec);
                if (pts_sec > mStartTimeUseTimeLine)
                {
                    float[] data = mAudioReader.AudioData.audio_data;

                    if (mDebugInfo)
                    {
                        int pts_n = (int)Math.Round(pts_sec * mAudioSampleRate / 1024);
                        if (pts_n > mLastAudioPtsNum + 1)
                        {
                            //to detect if there is audio packet lost
                            Debug.Log("pts " + pts_n + " mLastAudioPtsNum " + mLastAudioPtsNum);
                        }
                        mLastAudioPtsNum = pts_n;
                    }

                    bool isFrameReady = false;
                    if (mListAudio.Count > 30)
                        isFrameReady = true;
                    if (mSourceType == SOURCE_TYPE.PLAYBACK && mSourceDurationSec <1.0 && mListAudio.Count > 1)
                    {
                        isFrameReady = true;
                    }

                    if (isFrameReady)
                    {
                        if (!mIsFirstFrameReady)
                        {
                            mIsFirstFrameReady = true;
                        }
                    }

                    //if playback and play loop, don't record audio data to audioclip again 
                    if (mSourceType == SOURCE_TYPE.PLAYBACK && mAudioCount * 1024 / (float)mAudioSampleRate > mSourceDurationSec)
                        continue;
                    //if realtime and audioclip data have
                    if(mSourceType == SOURCE_TYPE.RTMP && mAudioCount*1024 >= mAudioLengthSamples)
                    {
                        mAudioCount = 0;
                        continue;
                    }
                        
                    lock (mListAudio)
                    {
                        lock (mListAudioPts)
                        {
                            lock (mListAudioPtsTime)
                            {
                                mListAudio.Add(data);
                                mListAudioPts.Add(mAudioCount++ * 1024);
                                mListAudioPtsTime.Add(pts_sec);
                            }
                        }
                    }

                }
            }
        }

        int getAudioLengthSample(int num, int divider)
        {
            return (num / divider + 1) * divider;
        }

        private IEnumerator SequenceAudioTrigger()
        {
            float triggerRate = 0.6f;
            float gap = (triggerRate / (float)mReader.SourceFPS);
            Debug.Log("[SequenceAudioTrigger] Trigger Gap = " + gap);
            while (true)
            {
                UpdateAudio();
                //yield return new WaitForSeconds(gap);
                yield return 0;
            }
        }

        private void OnDestroy()
        {
            if (mDebugInfo)
                Debug.Log("[MeshPlayerPlugin] OnDestroy()");
            Destroy();
        }

        void Awake()
        {
            Debug.Log("[MeshPlayerPlugin] Awake()");
            Uninitialize();
            Initialize();
         

            //Hide preview mesh
            if (mMeshComponent != null)
                mMeshComponent.mesh = null;

//#if UNITY_EDITOR
//            EditorApplication.pauseStateChanged += HandlePauseState;
//#endif
        }
        #endregion
    }


}
