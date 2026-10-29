using UnityEngine;
using UnityEngine.UI;

/// Alpha: the stroke log screen (Settings). Logging is off until you switch it on, so hand the phone over, switch
/// it on, and let people play. It compares strokes with thumb feel on and off, shows where on the screen strokes
/// start, and copies everything to the clipboard to paste somewhere.
public partial class ScrollyGame
{
    GameObject strokeLogPanel;
    RectTransform strokeLogCard;
    HudButton strokeLogToggle;
    Text strokeLogStatus;
    readonly Text[,] strokeLogCells = new Text[6, 2];
    readonly Image[,] strokeLogBars = new Image[2, 10];

    static readonly string[] StrokeLogRows = { "Strokes", "Average stroke", "Median stroke", "Start height", "Start in bottom half", "Meters per stroke" };

    void BuildStrokeLog()
    {
        var panel = MakeModal("StrokeLog");
        strokeLogPanel = panel.gameObject;
        var card = Ui.Box(panel, Center, new Vector2(0, 70), new Vector2(340, 500), Ui.Cream, Ui.CreamShade, 32, 6);
        strokeLogCard = (RectTransform)card.transform.parent;
        var t = card.transform;
        var title = Ui.Label(t, "Stroke log", 28, Ui.Brown);
        title.rectTransform.anchoredPosition = new Vector2(0, 212);

        strokeLogToggle = MakeToggle(t, 160, "Logging", Ui.Icon("hand"), ToggleStrokeLogging);

        // The table: one column per setting.
        for (int c = 0; c < 2; c++)
            Cell(t, new Vector2(16 + c * 84, 112), c == 0 ? "Thumbs on" : "Thumbs off", 13, Ui.BrownSoft, TextAnchor.MiddleCenter);
        for (int r = 0; r < StrokeLogRows.Length; r++)
        {
            float y = 88 - r * 24;
            Cell(t, new Vector2(-96, y), StrokeLogRows[r], 13, Ui.Brown, TextAnchor.MiddleLeft, 124);
            for (int c = 0; c < 2; c++) strokeLogCells[r, c] = Cell(t, new Vector2(16 + c * 84, y), "", 15, Ui.Brown, TextAnchor.MiddleCenter);
        }

        // Where strokes start: a phone per setting, with a bar for every tenth of the screen.
        Cell(t, new Vector2(-96, -122), "Where strokes\nstart on the\nscreen", 13, Ui.Brown, TextAnchor.MiddleLeft, 124).lineSpacing = 1.05f;
        for (int c = 0; c < 2; c++)
        {
            var phone = Ui.Box(t, Center, new Vector2(16 + c * 84, -122), new Vector2(62, 124), Voxel.Hex(0x3A3740), Voxel.Hex(0x1D1B22), 12, 0);
            for (int b = 0; b < 10; b++)
            {
                var bar = Ui.Rect(phone.transform, "Bar", new Vector2(0, 0), new Vector2(6, 9 + b * 11), new Vector2(50, 9));
                bar.pivot = new Vector2(0, 0.5f);
                strokeLogBars[c, b] = Ui.Rounded(bar, Ui.Gold, 3);
            }
        }

        strokeLogStatus = Ui.Label(t, "", 14, Ui.Brown);
        strokeLogStatus.rectTransform.anchoredPosition = new Vector2(0, -220);

        var copy = MakeButton(panel, Bottom, new Vector2(-76, 140), new Vector2(146, 44), Ui.Gold, Ui.GoldShade, Ui.OnGold, CopyStrokeLog);
        copy.Label.text = "Copy data";
        copy.Label.fontSize = 16;
        var clear = MakeButton(panel, Bottom, new Vector2(76, 140), new Vector2(146, 44), Ui.Cream, Ui.CreamShade, Ui.BrownSoft, ClearStrokeLog);
        clear.Label.text = "Clear";
        clear.Label.fontSize = 16;
        var done = MakeButton(panel, Bottom, new Vector2(0, 72), new Vector2(240, 58), Ui.Blue, Ui.BlueShade, Color.white, () => ShowStrokeLog(false), icon: Ui.Icon("close"));
        done.Label.text = "Done";
        done.Label.fontSize = 22;
    }

    static Text Cell(Transform card, Vector2 position, string text, int size, Color color, TextAnchor alignment, float width = 84)
    {
        var rect = Ui.Rect(card, "Cell", Center, position, new Vector2(width, 40));
        return Ui.Label(rect, text, size, color, alignment);
    }

    void ShowStrokeLog(bool open)
    {
        Synth.Instance.Click();
        strokeLogPanel.SetActive(open);
        if (!open) return;
        strokeLogPanel.transform.SetAsLastSibling();
        strokeLogStatus.text = "";
        RefreshStrokeLog();
        StartCoroutine(Pop(strokeLogCard, 0.85f));
    }

    void RefreshStrokeLog()
    {
        SetToggle(strokeLogToggle, Progress.StrokeLogOn);
        for (int c = 0; c < 2; c++)
        {
            var s = StrokeLog.For(c == 0);
            bool any = s.Count > 0;
            strokeLogCells[0, c].text = s.Count.ToString();
            strokeLogCells[1, c].text = any ? $"{s.AverageLength * 100:0}%" : "–";
            strokeLogCells[2, c].text = any ? $"{s.MedianLength * 100:0}%" : "–";
            strokeLogCells[3, c].text = any ? $"{s.AverageStart * 100:0}% up" : "–";
            strokeLogCells[4, c].text = any ? $"{s.BottomHalfShare * 100:0}%" : "–";
            strokeLogCells[5, c].text = any ? $"{s.MetersPerStroke:0.0} m" : "–";
            int most = Mathf.Max(1, Mathf.Max(s.StartBins));
            for (int b = 0; b < 10; b++)
                strokeLogBars[c, b].rectTransform.localScale = new Vector3(Mathf.Max(0.02f, s.StartBins[b] / (float)most), 1, 1);
        }
    }

    void ToggleStrokeLogging()
    {
        Progress.StrokeLogOn = !Progress.StrokeLogOn;
        Progress.Save();
        Synth.Instance.Click();
        strokeLogStatus.text = Progress.StrokeLogOn ? "Logging! Hand the phone over." : "Logging is off.";
        RefreshStrokeLog();
    }

    void CopyStrokeLog()
    {
        GUIUtility.systemCopyBuffer = StrokeLog.Export();
        Synth.Instance.Purchase();
        strokeLogStatus.text = "Copied! Paste it in Notes or a message.";
    }

    void ClearStrokeLog()
    {
        StrokeLog.Clear();
        Synth.Instance.Click();
        strokeLogStatus.text = "Log cleared.";
        RefreshStrokeLog();
    }
}
