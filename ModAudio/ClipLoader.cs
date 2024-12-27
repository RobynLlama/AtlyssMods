using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Marioalexsan.ModAudio;

public static class ClipLoader
{
    public static AudioClip LoadFromFile(string clipName, string path)
    {
        using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri($"{path}"), AudioType.UNKNOWN);
        request.SendWebRequest();

        while (!request.isDone) { }

        if (request.result != UnityWebRequest.Result.Success)
            throw new Exception($"Request for audio clip {path} failed.");

        DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)request.downloadHandler;

        var clip = dlHandler.audioClip;
        clip.name = clipName;
        return clip;
    }
}
