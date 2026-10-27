using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Rival flags: the three players just above you on the leaderboard (your league, or everyone this week)
/// plant flags on the wall at their heights, in their tag's colors, with their name. Pass one and you get a
/// little cheer. Fetched in the background; without a connection there are simply no flags.
public partial class ScrollyGame
{
    const int RivalCount = 3;

    class RivalFlag
    {
        public Online.Row Row;
        public GameObject Flag;
        public RectTransform Tag;
        public bool Passed;
    }

    readonly List<RivalFlag> rivalFlags = new();
    List<Online.Row> rivals;   // the latest rivals from the leaderboard
    bool rivalsPlaced;         // flags are on this world's wall

    /// Asks the leaderboard who's just above you; the flags go up as soon as it answers.
    async void FetchRivals()
    {
        string board = Online.HasBoard(Online.League) ? Online.League : Online.Weekly;
        var found = await Online.Rivals(board, RivalCount);
        if (found == null && board == Online.League && !Online.HasBoard(Online.League))
            found = await Online.Rivals(Online.Weekly, RivalCount);
        if (found == null) return;
        rivals = found;
        if (phase is Phase.Title or Phase.Playing && !rivalsPlaced) PlaceRivalFlags();
    }

    /// Plants this world's rival flags (once per world; ResetWorld clears them).
    void PlaceRivalFlags()
    {
        ClearRivalFlags();
        rivalsPlaced = true;
        if (rivals == null) return;
        foreach (var row in rivals)
        {
            if (row.Meters <= 0) continue;
            var style = Cosmetics.Tag(row.Look?.t ?? "").Tag;
            var flag = new GameObject("Rival flag");
            flag.transform.SetParent(world, false);
            float y = row.Meters;
            Voxel.Box(flag.transform, new Vector3(0.08f, 1.5f, 0.08f), 0xF2E6CF, new Vector3(3.2f, y + 0.75f, -1.1f));
            // The cloth in the rival's tag colors: their gradient's top color with a stripe of their ink.
            Voxel.Box(flag.transform, new Vector3(0.7f, 0.45f, 0.05f), style.Colors[0], new Vector3(2.82f, y + 1.25f, -1.1f));
            Voxel.Box(flag.transform, new Vector3(0.7f, 0.1f, 0.06f), style.Ink, new Vector3(2.82f, y + 1.25f, -1.1f));
            for (float x = 0.6f; x <= 6.6f; x += 0.8f)
                Voxel.Box(flag.transform, new Vector3(0.3f, 0.06f, 0.04f), style.Colors[0], new Vector3(x, y, -0.08f), castShadows: false);

            var pill = Ui.Pill(canvasRect, Center, Vector2.zero, new Vector2(150, 26), Ui.Cream, Ui.CreamShade, 2);
            var tag = (RectTransform)pill.transform.parent;
            var label = Ui.Label(pill.transform, $"{row.Name} · {row.Meters} m", 13, Ui.Brown);
            tag.sizeDelta = new Vector2(Mathf.Min(220, label.preferredWidth + 22), 26);
            tag.gameObject.SetActive(false);
            // Already above them (the answer came in mid-run): no cheer for that one.
            rivalFlags.Add(new RivalFlag { Row = row, Flag = flag, Tag = tag, Passed = runBest > y });
        }
    }

    void ClearRivalFlags()
    {
        foreach (var rival in rivalFlags)
        {
            if (rival.Flag != null) Destroy(rival.Flag);
            if (rival.Tag != null) Destroy(rival.Tag.gameObject);
        }
        rivalFlags.Clear();
        rivalsPlaced = false;
    }

    /// Every frame: name tags follow their flags, and passing a rival is a small celebration.
    void UpdateRivalFlags()
    {
        if (!rivalsPlaced && rivals != null && phase is Phase.Title or Phase.Playing) PlaceRivalFlags();
        bool climbing = phase is Phase.Playing or Phase.Falling && OpenModal == null;
        foreach (var rival in rivalFlags)
        {
            if (rival.Tag == null) continue;
            float y = rival.Row.Meters;
            bool show = climbing && !rival.Passed;
            if (show)
            {
                var point = CanvasPoint(new Vector3(1.9f, y + 0.45f, -0.5f));
                show = Mathf.Abs(point.y) < canvasRect.rect.height / 2 - 60;
                rival.Tag.anchoredPosition = point;
            }
            rival.Tag.gameObject.SetActive(show);

            if (!rival.Passed && phase == Phase.Playing && runBest > y)
            {
                rival.Passed = true;
                Synth.Instance.NewBest();
                Haptics.Success();
                Debris.Burst(new Vector3(2.8f, y + 1.2f, -1.2f), 0xFFD54F, 14, 3.5f);
                ShowBanner("PASSED!", rival.Row.Name.ToUpperInvariant());
            }
        }
    }
}
