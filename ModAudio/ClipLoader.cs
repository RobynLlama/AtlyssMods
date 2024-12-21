using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Marioalexsan.ModAudio;

public static class ClipLoader
{
    public static AudioClip LoadFromFile(string clipName, string path)
    {
        if (path.EndsWith(".ogg"))
        {
            using var stream = File.OpenRead(path);
            return LoadOgg(clipName, stream);
        }

        if (path.EndsWith(".mp3"))
        {
            using var stream = File.OpenRead(path);
            return LoadMp3(clipName, stream);
        }

        if (path.EndsWith(".wav"))
        {
            using var stream = File.OpenRead(path);
            return LoadWav(clipName, stream);
        }

        return null;
    }

    private static AudioClip LoadOgg(string clipName, Stream stream)
    {
        using var reader = new NVorbis.VorbisReader(stream);

        var clip = AudioClip.Create(clipName, (int)reader.TotalSamples, reader.Channels, reader.SampleRate, false);

        var samples = new float[reader.TotalSamples * reader.Channels];
        reader.ReadSamples(samples);
        clip.SetData(samples, 0);

        return clip;
    }

    private static AudioClip LoadWav(string clipName, Stream stream)
    {
        using var reader = new WaveFileReader(stream);

        var clip = AudioClip.Create(clipName, (int)reader.SampleCount, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, false);

        var provider = reader.ToSampleProvider();

        var samples = new float[(int)reader.SampleCount * reader.WaveFormat.Channels];

        provider.Read(samples, 0, samples.Length);
        clip.SetData(samples, 0);

        return clip;
    }

    private static AudioClip LoadMp3(string clipName, Stream stream)
    {
        using var reader = new Mp3FileReader(stream);

        var totalSamples = (int)(reader.Length * 8 / reader.WaveFormat.BitsPerSample);

        var clip = AudioClip.Create(clipName, totalSamples, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, false);

        var provider = reader.ToSampleProvider();

        var samples = new float[totalSamples * reader.WaveFormat.Channels];

        provider.Read(samples, 0, samples.Length);
        clip.SetData(samples, 0);

        return clip;
    }
}
