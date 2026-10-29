using UnityEngine;
using UnityEngine.UI;

/// Alpha: the stroke log screen (Settings). Logging is off until you switch it on, so hand the phone over, switch
/// it on, and let people play. It shows the strokes made with the game as it is now and where on the screen they
/// start, and copies everything (earlier playtest setups too) to the clipboard to paste somewhere.
public partial class ScrollyGame
{
    GameObject strokeLogPanel;
    RectTransform strokeLogCard;
    HudButton strokeLogToggle;
    Text strokeLogStatus;
    readonly Text[] strokeLogCells = new Text[6];
    readonly Image[] strokeLogBars = new Image[10];

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

        // The table, for the game as it is now.
        for (int r = 0; r < StrokeLogRows.Length; r++)
        {
            float y = 106 - r * 26;
            Cell(t, new Vector2(-64, y), StrokeLogRows[r], 14, Ui.Brown, TextAnchor.MiddleLeft, 180);
            strokeLogCells[r] = Cell(t, new Vector2(100, y), "", 16, Ui.Brown, TextAnchor.MiddleRight, 100);
        }

        // Where strokes start: a phone with a bar for every tenth of the screen.
        Cell(t, new Vector2(-64, -122), "Where strokes\nstart on the\nscreen", 14, Ui.Brown, TextAnchor.MiddleLeft, 180).lineSpacing = 1.05f;
        var phone = Ui.Box(t, Center, new Vector2(100, -122), new Vector2(60, 124), Voxel.Hex(0x3A3740), Voxel.Hex(0x1D1B22), 12, 0);
        for (int b = 0; b < 10; b++)
        {
            var bar = Ui.Rect(phone.transform, "Bar", new Vector2(0, 0), new Vector2(6, 9 + b * 11), new Vector2(48, 9));
            bar.pivot = new Vector2(0, 0.5f);
            strokeLogBars[b] = Ui.Rounded(bar, Ui.Gold, 3);
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
        var s = StrokeLog.For(StrokeLog.CurrentSetup);
        bool any = s.Count > 0;
        strokeLogCells[0].text = s.Count.ToString();
        strokeLogCells[1].text = any ? $"{s.AverageLength * 100:0}%" : "–";
        strokeLogCells[2].text = any ? $"{s.MedianLength * 100:0}%" : "–";
        strokeLogCells[3].text = any ? $"{s.AverageStart * 100:0}% up" : "–";
        strokeLogCells[4].text = any ? $"{s.BottomHalfShare * 100:0}%" : "–";
        strokeLogCells[5].text = any ? $"{s.MetersPerStroke:0.0} m" : "–";
        int most = Mathf.Max(1, Mathf.Max(s.StartBins));
        for (int b = 0; b < 10; b++)
            strokeLogBars[b].rectTransform.localScale = new Vector3(Mathf.Max(0.02f, s.StartBins[b] / (float)most), 1, 1);
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
