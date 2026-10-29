using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// Alpha playtest tool: remembers the last strokes people made while climbing (where on the screen they started,
/// how long they were, how far they climbed), with the setup they were made with, so setups can be compared.
/// Stored on the device only.
public static class StrokeLog
{
    public struct Stroke
    {
        public float Start;    // where the stroke began, 0 = bottom of the screen, 1 = top
        public float Length;   // how far down it went, in screen heights
        public float Meters;   // how far it climbed
        public int Setup;      // the camera setup at the time (see SetupNames)
    }

    public class Stats
    {
        public int Count;
        public float AverageLength, MedianLength, AverageStart, BottomHalfShare, MetersPerStroke;
        public int[] StartBins = new int[10];   // how many strokes began in each tenth of the screen, bottom first
    }

    const int Keep = 400;
    static List<Stroke> strokes;

    static List<Stroke> All => strokes ??= Load();

    public static void Add(Stroke stroke)
    {
        All.Add(stroke);
        if (All.Count > Keep) All.RemoveRange(0, All.Count - Keep);
        Save();
    }

    public static void Clear()
    {
        All.Clear();
        Save();
    }

    /// Everything as text: a summary per setting, then one line per stroke (to paste into Notes or a message).
    /// The setups strokes were logged with during the playtests; the last one is the game as it is now.
    public static readonly string[] SetupNames =
    {
        "Original (18 m view, climber 30% up)",
        "Thumb (23 m view, climber 42% up)",
        "Thumb+ (24 m view, 1.25x pull, climber 47% up)",
        "Mix (18 m view, climber 36% up)",
        "Now (18 m view, climber 50% up)",
    };

    public const int CurrentSetup = 4;

    public static string Export()
    {
        var text = new System.Text.StringBuilder("UPSCROLL stroke log\n");
        for (int setup = CurrentSetup; setup >= 0; setup--)
        {
            var s = For(setup);
            if (s.Count == 0) continue;
            text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: {1} strokes, average {2:0}% of the screen (median {3:0}%), start on average {4:0}% up, {5:0}% start in the bottom half, {6:0.0} m per stroke",
                SetupNames[setup], s.Count, s.AverageLength * 100, s.MedianLength * 100, s.AverageStart * 100, s.BottomHalfShare * 100, s.MetersPerStroke));
        }
        text.AppendLine("start,length,meters,setup (0 original, 1 thumb, 2 thumb+, 3 mix, 4 now)");
        foreach (var s in All)
            text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:0.###},{1:0.###},{2:0.##},{3}", s.Start, s.Length, s.Meters, s.Setup));
        return text.ToString();
    }

    public static Stats For(int setup)
    {
        var list = All.Where(s => s.Setup == setup).ToList();
        var stats = new Stats { Count = list.Count };
        if (list.Count == 0) return stats;
        var lengths = list.Select(s => s.Length).OrderBy(l => l).ToList();
        stats.AverageLength = lengths.Average();
        stats.MedianLength = lengths[lengths.Count / 2];
        stats.AverageStart = list.Average(s => s.Start);
        stats.BottomHalfShare = list.Count(s => s.Start < 0.5f) / (float)list.Count;
        stats.MetersPerStroke = list.Average(s => s.Meters);
        foreach (var s in list) stats.StartBins[Mathf.Clamp((int)(s.Start * 10), 0, 9)]++;
        return stats;
    }

    static List<Stroke> Load()
    {
        var list = new List<Stroke>();
        foreach (var entry in PlayerPrefs.GetString("strokeLog", "").Split(';'))
        {
            var f = entry.Split(',');
            if (f.Length != 4) continue;
            list.Add(new Stroke
            {
                Start = float.Parse(f[0], CultureInfo.InvariantCulture),
                Length = float.Parse(f[1], CultureInfo.InvariantCulture),
                Meters = float.Parse(f[2], CultureInfo.InvariantCulture),
                Setup = Mathf.Clamp(int.TryParse(f[3], out int setup) ? setup : 0, 0, CurrentSetup),
            });
        }
        return list;
    }

    static void Save() => PlayerPrefs.SetString("strokeLog", string.Join(";", All.Select(s =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.###},{1:0.###},{2:0.##},{3}", s.Start, s.Length, s.Meters, s.Setup))));
}
