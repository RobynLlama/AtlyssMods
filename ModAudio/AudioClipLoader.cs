using NAudio.Wave;
using UnityEngine;
using UnityEngine.Networking;

namespace Marioalexsan.ModAudio;

public static class AudioClipLoader
{
    public static readonly string[] SupportedLoadExtensions = [
        ".aiff",
        ".aif",
        ".mp3",
        ".ogg",
        ".wav",
        ".aac",
        ".alac"
    ];

    public static readonly string[] SupportedStreamExtensions = [
        ".wav",
        ".ogg",
        ".mp3"
    ];

    public static readonly string[] SupportedExtensions = [.. SupportedStreamExtensions, .. SupportedLoadExtensions];

    /// <summary>
    /// Creates an empty clip with the given name and duration.
    /// </summary>
    public static AudioClip GenerateEmptyClip(string name, int samples)
    {
        var clip = AudioClip.Create(name, samples, 1, 44100, false);
        clip.SetData(new float[samples], 0);
        return clip;
    }

    /// <summary>
    /// Loads an audio clip in its entirety from the disk.
    /// </summary>
    public static AudioClip LoadFromFile(string clipName, string path, float volumeModifier)
    {
        using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri($"{path}"), AudioType.UNKNOWN);
        request.SendWebRequest();

        while (!request.isDone)
        {
            Thread.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
            throw new Exception($"Request for audio clip {path} failed.");

        DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)request.downloadHandler;

        var clip = dlHandler.audioClip;
        clip.name = clipName;

        // Adjust volume

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        OptimizedMethods.MultiplyFloatArray(samples, volumeModifier);

        clip.SetData(samples, 0);

        dlHandler.Dispose();
        return clip;
    }

    /// <summary>
    /// Streams an audio clip from the disk.
    /// </summary>
    public static AudioClip StreamFromFile(string clipName, string path, float volumeModifier, out IAudioStream openedStream)
    {
        IAudioStream? stream = null;

        if (path.EndsWith(".ogg"))
        {
            stream = new OggStream(File.OpenRead(path)) { VolumeModifier = volumeModifier };
        }

        if (path.EndsWith(".mp3"))
        {
            stream = new Mp3Stream(File.OpenRead(path)) { VolumeModifier = volumeModifier };
        }

        if (path.EndsWith(".wav"))
        {
            stream = new WavStream(File.OpenRead(path)) { VolumeModifier = volumeModifier };
        }

        if (stream == null)
            throw new NotImplementedException("The given file format isn't supported for streaming.");

        openedStream = stream;
        return AudioClip.Create(clipName, stream.TotalFrames, stream.ChannelsPerFrame, stream.Frequency, true, stream.OnAudioRead, stream.OnAudioSetPosition);
    }

    public interface IAudioStream : IDisposable
    {
        int TotalFrames { get; }
        int ChannelsPerFrame { get; }
        int Frequency { get; }

        void OnAudioRead(float[] samples); // Unity seems to be calling this with float[4096]
        void OnAudioSetPosition(int newPosition);
    }

    private class OggStream(Stream stream) : IAudioStream
    {
        private readonly NVorbis.VorbisReader _reader = new NVorbis.VorbisReader(stream);

        public float VolumeModifier = 1f;

        public int TotalFrames => (int)_reader.TotalSamples;
        public int ChannelsPerFrame => _reader.Channels;
        public int Frequency => _reader.SampleRate;

        public void OnAudioRead(float[] samples)
        {
            _reader.ReadSamples(samples);
            OptimizedMethods.MultiplyFloatArray(samples, VolumeModifier);
        }

        public void OnAudioSetPosition(int newPosition)
        {
            _reader.SamplePosition = newPosition;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }

    private class WavStream : IAudioStream
    {
        public WavStream(Stream stream)
        {
            _reader = new WaveFileReader(stream);
            _provider = _reader.ToSampleProvider();
        }
        private readonly WaveFileReader _reader;
        private readonly ISampleProvider _provider;

        public float VolumeModifier = 1f;

        public int TotalFrames => (int)_reader.SampleCount;
        public int ChannelsPerFrame => _reader.WaveFormat.Channels;
        public int Frequency => _reader.WaveFormat.SampleRate;

        public void OnAudioRead(float[] samples)
        {
            _provider.Read(samples, 0, samples.Length);
            OptimizedMethods.MultiplyFloatArray(samples, VolumeModifier);
        }

        public void OnAudioSetPosition(int newPosition)
        {
            _reader.Position = newPosition * _reader.BlockAlign;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }

    private class Mp3Stream : IAudioStream
    {
        public Mp3Stream(Stream stream)
        {
            _reader = new Mp3FileReader(stream);
            _provider = _reader.ToSampleProvider();
        }
        private readonly Mp3FileReader _reader;
        private readonly ISampleProvider _provider;

        public float VolumeModifier = 1f;

        public int TotalFrames => (int)(_reader.Length * 8 / ChannelsPerFrame / _reader.WaveFormat.BitsPerSample);
        public int ChannelsPerFrame => _reader.WaveFormat.Channels;
        public int Frequency => _reader.WaveFormat.SampleRate;

        public void OnAudioRead(float[] samples)
        {
            _provider.Read(samples, 0, samples.Length);
            OptimizedMethods.MultiplyFloatArray(samples, VolumeModifier);
        }

        public void OnAudioSetPosition(int newPosition)
        {
            _reader.Position = newPosition * _reader.BlockAlign;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
