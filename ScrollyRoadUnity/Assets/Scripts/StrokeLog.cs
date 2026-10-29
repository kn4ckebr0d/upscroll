using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// Alpha playtest tool: remembers the last strokes people made while climbing (where on the screen they started,
/// how long they were, how far they climbed), split by the feel setting, so the settings can be compared.
/// Stored on the device only.
public static class StrokeLog
{
    public struct Stroke
    {
        public float Start;    // where the stroke began, 0 = bottom of the screen, 1 = top
        public float Length;   // how far down it went, in screen heights
        public float Meters;   // how far it climbed
        public Tuning.FeelMode Feel;   // the feel setting at the time
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
    public static readonly string[] FeelNames = { "Original", "Thumb", "Thumb+", "Mix" };

    /// The settings shown side by side on the stroke log screen (Thumb+ is retired; its strokes stay in the export).
    public static readonly Tuning.FeelMode[] Compared = { Tuning.FeelMode.Mix, Tuning.FeelMode.Classic, Tuning.FeelMode.Thumb };

    public static string Export()
    {
        var text = new System.Text.StringBuilder("UPSCROLL stroke log\n");
        foreach (Tuning.FeelMode feel in new[] { Tuning.FeelMode.Mix, Tuning.FeelMode.Classic, Tuning.FeelMode.Thumb, Tuning.FeelMode.ThumbPlus })
        {
            var s = For(feel);
            if (s.Count == 0 && feel == Tuning.FeelMode.ThumbPlus) continue;
            text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Feel {0}: {1} strokes, average {2:0}% of the screen (median {3:0}%), start on average {4:0}% up, {5:0}% start in the bottom half, {6:0.0} m per stroke",
                FeelNames[(int)feel], s.Count, s.AverageLength * 100, s.MedianLength * 100, s.AverageStart * 100, s.BottomHalfShare * 100, s.MetersPerStroke));
        }
        text.AppendLine("start,length,meters,feel (0 original, 1 thumb, 2 thumb+, 3 mix)");
        foreach (var s in All)
            text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:0.###},{1:0.###},{2:0.##},{3}", s.Start, s.Length, s.Meters, (int)s.Feel));
        return text.ToString();
    }

    public static Stats For(Tuning.FeelMode feel)
    {
        var list = All.Where(s => s.Feel == feel).ToList();
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
                Feel = (Tuning.FeelMode)Mathf.Clamp(int.TryParse(f[3], out int feel) ? feel : 0, 0, 3),   // older logs: 1 = thumb, 0 = original
            });
        }
        return list;
    }

    static void Save() => PlayerPrefs.SetString("strokeLog", string.Join(";", All.Select(s =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.###},{1:0.###},{2:0.##},{3}", s.Start, s.Length, s.Meters, (int)s.Feel))));
}
