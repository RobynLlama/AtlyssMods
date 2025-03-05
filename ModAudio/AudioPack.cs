using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Marioalexsan.ModAudio.AudioPackConfig;

namespace Marioalexsan.ModAudio;

public class AudioPack
{
    public string PackPath { get; set; } = "???";
    public bool Enabled { get; set; } = true;

    public AudioPackConfig Config { get; set; } = new();

    public Dictionary<string, AudioClip> LoadedClips { get; } = [];
    public Dictionary<string, List<ClipReplacement>> Replacements { get; } = [];
}
