/* Audio Reader Plugin for Unity.
*  All Rights rRserved. XR Company 2020.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Runtime.InteropServices;

namespace prometheus
{
    public class AudioData
    {
        //audio
        public bool is_video;
        public byte[] audio_bytes;
        public bool ready;
        public int buffer_size = 0;
        public float ptsSec;
        public float[] audio_data;

        public GCHandle gcHandlerAudioBytes;

        public void AllocAudioBuffers(int count)
        {
            ClearAudioBuffer();
            //audio
            audio_bytes = new byte[count];
            gcHandlerAudioBytes = GCHandle.Alloc(audio_bytes, GCHandleType.Pinned);
        }

        public void ClearAudioBuffer()
        {
            //if (buffer_size == 0)
            //    return;
            if (gcHandlerAudioBytes.IsAllocated) gcHandlerAudioBytes.Free();
            audio_bytes = null;
            //buffer_size = 0;
        }

    }

    public class AudioReader
    {
        public int ApiKey = -1;
        public UInt16 mChannels;
        public int mSampleRate;
        public UInt16 mBitDepth;

        public AudioData AudioData = null;

        // create api instance
        static public AudioReader CreateAudioReader(int apiKey)
        {

            AudioReader instance = new AudioReader(apiKey);
            if (instance.ApiKey == -1)
                return null;

            return instance;
        }

        public void audioStreamInfo()
        {
            ReaderAPI.GetAudioStreamInfo(ApiKey, ref mChannels, ref mSampleRate, ref mBitDepth);
        }
       
        private AudioReader(int apiKey)
        {
            if (apiKey == -1)
                ApiKey = ReaderAPI.CreateApiInstance();
            else
                ApiKey = apiKey;

            AudioData = new AudioData();
            
        }


        ~AudioReader()
        {
            Release();
        }

        public void Release()
        {
            AudioData.ClearAudioBuffer();
        }

        public void setAudioMainSwitch(bool value)
        {
            ReaderAPI.SetAudioMainSwitch(ApiKey,value);
        }

        public bool GetAudioClipData(ref float ptsSec)
        {
            if (!ReaderAPI.ReadAudioFrame(ApiKey, ref ptsSec))
                return false;
            //Debug.Log("[GetAudioClipData] audio ptsSec ........" + ptsSec);
            AudioData.ptsSec = ptsSec;
            int byte_count = ReaderAPI.GetAudioByteCount(ApiKey);
            if(byte_count <1)
                Debug.Log("byte_count " + byte_count);
            AudioData.AllocAudioBuffers(byte_count);
            ReaderAPI.SetAudioByte(ApiKey, AudioData.gcHandlerAudioBytes.AddrOfPinnedObject());
            AudioData.audio_data = ToAudioFloatData(AudioData.audio_bytes);

            AudioData.ptsSec = ptsSec;
            return true;

        }


        // unity play pcm format audio 
        public float[] ToAudioFloatData(byte[] fileBytes)
        {
            UInt16 channels = mChannels;
            int sampleRate = mSampleRate;
            UInt16 bitDepth = mBitDepth;
            //UInt16 channels = 1;
            //int sampleRate = 48000;
            //UInt16 bitDepth = 32;
            int headerOffset = 0;
            int subchunk2 = 0;

            if (channels == 0 && sampleRate == 0)
                throw new Exception(bitDepth + " Failed to get decode audio params.");
            //Debug.AssertFormat(channels == 0 && sampleRate == 0 && bitDepth ==0, "Failed to get decode audio params: {0} from data bytes: {1} at offset: {2}", channels, sampleRate, bitDepth);

            float[] data;
            switch (bitDepth)
            {
                case 8:
                    data = Convert8BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                    break;
                case 16:
                    data = Convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                    break;
                case 24:
                    data = Convert24BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                    break;
                case 32:
                    data = Convert32BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                    break;
                default:
                    throw new Exception(bitDepth + " bit depth is not supported.");
            }

            return data;
        }


        public AudioClip ToAudioClip(byte[] fileBytes, int offsetSamples = 0, string name = "wav")
        {
            //string riff = Encoding.ASCII.GetString (fileBytes, 0, 4);
            //string wave = Encoding.ASCII.GetString (fileBytes, 8, 4);

            //int subchunk1 = BitConverter.ToInt32(fileBytes, 16);
            //UInt16 audioFormat = BitConverter.ToUInt16(fileBytes, 20);

            //// NB: Only uncompressed PCM wav files are supported.
            //string formatCode = FormatCode(audioFormat);
            //Debug.AssertFormat(audioFormat == 1 || audioFormat == 65534, "Detected format code '{0}' {1}, but only PCM and WaveFormatExtensable uncompressed formats are currently supported.", audioFormat, formatCode);

            //UInt16 channels = BitConverter.ToUInt16(fileBytes, 22);
            //int sampleRate = BitConverter.ToInt32(fileBytes, 24);
            ////int byteRate = BitConverter.ToInt32 (fileBytes, 28);
            ////UInt16 blockAlign = BitConverter.ToUInt16 (fileBytes, 32);
            //UInt16 bitDepth = BitConverter.ToUInt16(fileBytes, 34);

            //int headerOffset = 16 + 4 + subchunk1 + 4;
            //int subchunk2 = BitConverter.ToInt32(fileBytes, headerOffset);
            //Debug.LogFormat ("riff={0} wave={1} subchunk1={2} format={3} channels={4} sampleRate={5} byteRate={6} blockAlign={7} bitDepth={8} headerOffset={9} subchunk2={10} filesize={11}", riff, wave, subchunk1, formatCode, channels, sampleRate, byteRate, blockAlign, bitDepth, headerOffset, subchunk2, fileBytes.Length);

            //if deliver raw audio data ,need fill params by hand
            UInt16 channels = mChannels;
            int sampleRate = mSampleRate;
            UInt16 bitDepth = mBitDepth;
            //UInt16 channels = 1;
            //int sampleRate = 48000;
            //UInt16 bitDepth = 32;
            int headerOffset = 0;
            int subchunk2 = 0;

            if (channels == 0 && sampleRate == 0)
                throw new Exception(bitDepth + " Failed to get decode audio params.");
            //Debug.AssertFormat(channels == 0 && sampleRate == 0 && bitDepth ==0, "Failed to get decode audio params: {0} from data bytes: {1} at offset: {2}", channels, sampleRate, bitDepth);

            float[] data;
            switch (bitDepth)
            {
                case 8:
                    data = Convert8BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                    break;
                case 16:
                    data = Convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                    break;
                case 24:
                    data = Convert24BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                    break;
                case 32:
                    data = Convert32BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                    break;
                default:
                    throw new Exception(bitDepth + " bit depth is not supported.");
            }

            AudioClip audioClip = AudioClip.Create(name, data.Length, (int)channels, sampleRate, false);
            audioClip.SetData(data, 0);
            return audioClip;
        }

        #region wav file bytes to Unity AudioClip conversion methods

        private static float[] Convert8BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            int wavSize = BitConverter.ToInt32(source, headerOffset);
            headerOffset += sizeof(int);
            Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 8-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

            float[] data = new float[wavSize];

            sbyte maxValue = sbyte.MaxValue;

            int i = 0;
            while (i < wavSize)
            {
                data[i] = (float)source[i] / maxValue;
                ++i;
            }

            return data;
        }

        private static float[] Convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            int wavSize = BitConverter.ToInt32(source, headerOffset);
            headerOffset += sizeof(int);
            Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 16-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

            int x = sizeof(Int16); // block size = 2
            int convertedSize = wavSize / x;

            float[] data = new float[convertedSize];

            Int16 maxValue = Int16.MaxValue;

            int offset = 0;
            int i = 0;
            while (i < convertedSize)
            {
                offset = i * x + headerOffset;
                data[i] = (float)BitConverter.ToInt16(source, offset) / maxValue;
                ++i;
            }

            Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

            return data;
        }

        private static float[] Convert24BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            int wavSize = BitConverter.ToInt32(source, headerOffset);
            headerOffset += sizeof(int);
            Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 24-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

            int x = 3; // block size = 3
            int convertedSize = wavSize / x;

            int maxValue = Int32.MaxValue;

            float[] data = new float[convertedSize];

            byte[] block = new byte[sizeof(int)]; // using a 4 byte block for copying 3 bytes, then copy bytes with 1 offset

            int offset = 0;
            int i = 0;
            while (i < convertedSize)
            {
                offset = i * x + headerOffset;
                Buffer.BlockCopy(source, offset, block, 1, x);
                data[i] = (float)BitConverter.ToInt32(block, 0) / maxValue;
                ++i;
            }

            Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

            return data;
        }

        private static float[] Convert32BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            //int wavSize = BitConverter.ToInt32(source, headerOffset);
            //headerOffset += sizeof(int);
            //Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 32-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);


            int x = sizeof(float); //  block size = 4
                                   //int convertedSize = wavSize / x;
            int convertedSize = source.Length / x;

            //Int32 maxValue = Int32.MaxValue;

            float[] data = new float[convertedSize];

            int offset = 0;
            int i = 0;
            while (i < convertedSize)
            {
                offset = i * x + headerOffset;
                //data[i] = (float)BitConverter.ToInt32(source, offset) / maxValue;
                data[i] = (float)BitConverter.ToSingle(source, offset);
                ++i;
            }

            Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

            return data;
        }

        public static UInt16 BitDepth(AudioClip audioClip)
        {
            UInt16 bitDepth = Convert.ToUInt16(audioClip.samples * audioClip.channels * audioClip.length / audioClip.frequency);
            Debug.AssertFormat(bitDepth == 8 || bitDepth == 16 || bitDepth == 32, "Unexpected AudioClip bit depth: {0}. Expected 8 or 16 or 32 bit.", bitDepth);
            return bitDepth;
        }

        private static int BytesPerSample(UInt16 bitDepth)
        {
            return bitDepth / 8;
        }

        private static int BlockSize(UInt16 bitDepth)
        {
            switch (bitDepth)
            {
                case 32:
                    return sizeof(Int32); // 32-bit -> 4 bytes (Int32)
                case 16:
                    return sizeof(Int16); // 16-bit -> 2 bytes (Int16)
                case 8:
                    return sizeof(sbyte); // 8-bit -> 1 byte (sbyte)
                default:
                    throw new Exception(bitDepth + " bit depth is not supported.");
            }
        }

        private static string FormatCode(UInt16 code)
        {
            switch (code)
            {
                case 1:
                    return "PCM";
                case 2:
                    return "ADPCM";
                case 3:
                    return "IEEE";
                case 7:
                    return "μ-law";
                case 65534:
                    return "WaveFormatExtensable";
                default:
                    Debug.LogWarning("Unknown wav code format:" + code);
                    return "";
            }
        }

    }
}
#endregion