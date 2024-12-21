using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marioalexsan.ModAudio;

public static class RouteConfig
{
    private static readonly char[] Separator = ['='];

    public static Dictionary<string, string> Read(string path)
    {
        using var stream = new StreamReader(File.OpenRead(path));

        var data = new Dictionary<string, string>();

        string line;
        while ((line = stream.ReadLine()) != null)
        {
            var parts = line.Split(Separator, 3);

            if (parts.Length == 3)
                continue;

            data[parts[0].Trim()] = parts[1].Trim();
        }

        return data;
    }

    public static void Write(string path, Dictionary<string, string> data)
    {
        using var stream = new StreamWriter(File.OpenWrite(path));

        foreach (var kvp in data)
            stream.WriteLine($"{kvp.Key.Trim()} = {kvp.Value.Trim()}");
    }
}
