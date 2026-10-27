using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// The leaderboard, full screen: your league (about 40 players, this week), everyone this week, and all time. Every row is a banner in that player's tag, with
/// their character and hat, their name and their best height. Your own banner sits on top, where you can
/// change your name. After a run, the results screen shows where you landed this week.
public partial class ScrollyGame
{
    const float RowHeight = 68, RowStep = 76;

    GameObject boardPanel;
    RectTransform boardList, boardMine;
    ScrollArea boardScroll;
    Text boardStatus, boardNameHint;
    HudButton boardRetry;
    readonly List<HudButton> boardTabs = new();
    readonly List<(RawImage pattern, float tile)> boardPatterns = new();
    readonly List<(Text label, float fittedWidth)> boardNames = new();
    readonly Dictionary<string, Texture2D> portraits = new();
    string boardShown = Online.Weekly;
    int boardRequest;              // answers to older requests are ignored
    TouchScreenKeyboard nameKeyboard;
    List<Online.Row> boardDemo;    // -scrollyBoardDemo: made-up players, to test without the service
    Online.RunResult runResult;    // where the last run put you, null until the leaderboard answers
    static readonly string[] BoardTabs = { Online.League, Online.Weekly, Online.AllTime };

    // MARK: - Building

    void BuildBoard()
    {
        var panel = MakeModal("Leaderboard");
        boardPanel = panel.gameObject;
        var safe = Ui.SafeArea(panel);

        var close = MakeRoundButton(safe, TopLeft, new Vector2(34, -32), 46, Ui.Cream, Ui.CreamShade, Ui.Icon("close"), Ui.Brown, () => ShowBoard(false));
        close.Label.gameObject.SetActive(false);
        Ui.Label(Ui.Rect(safe, "Title", Top, new Vector2(0, -32), new Vector2(220, 40)), "Leaderboard", 30, Ui.Brown);

        string[] labels = { "League", "This week", "All time" };
        for (int i = 0; i < BoardTabs.Length; i++)
        {
            string board = BoardTabs[i];
            var tab = MakeButton(safe, Top, Vector2.zero, new Vector2(108, 44), Ui.Blue, Ui.BlueShade, Color.white, () => ShowBoardTab(board));
            tab.Label.text = labels[i];
            tab.Label.fontSize = 16;
            boardTabs.Add(tab);
        }
        LayoutBoardTabs();

        // Your banner, and changing your name.
        boardMine = Ui.Rect(safe, "Mine", Top, new Vector2(0, -182), Vector2.zero);
        boardMine.anchorMin = new Vector2(0, 1);
        boardMine.anchorMax = new Vector2(1, 1);
        boardMine.sizeDelta = new Vector2(-28, RowHeight + 8);
        var rename = MakeButton(safe, Top, new Vector2(-82, -258), new Vector2(156, 40), Ui.Cream, Ui.CreamShade, Ui.Brown, StartRename, icon: Ui.Icon("pencil"));
        rename.Label.text = "Change name";
        rename.Label.fontSize = 16;
        var dice = MakeButton(safe, Top, new Vector2(82, -258), new Vector2(156, 40), Ui.Cream, Ui.CreamShade, Ui.Brown, RandomName, icon: Ui.Icon("dice"));
        dice.Label.text = "Random name";
        dice.Label.fontSize = 16;
        boardNameHint = Ui.Label(Ui.Rect(safe, "Name hint", Top, new Vector2(0, -290), new Vector2(340, 20)), "", 14, Voxel.Hex(0xC0392B));

        // The list: a cream sheet with a scrolling column of banners.
        var sheet = Ui.Box(safe, Bottom, Vector2.zero, Vector2.zero, Ui.Cream, Ui.CreamShade, 30, 0);
        var sheetRect = (RectTransform)sheet.transform.parent;
        sheetRect.anchorMin = Vector2.zero;
        sheetRect.anchorMax = Vector2.one;
        sheetRect.offsetMin = new Vector2(8, -30);
        sheetRect.offsetMax = new Vector2(-8, -306);
        var viewport = Ui.Rect(sheet.transform, "Rows", Center, Vector2.zero, Vector2.zero);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(0, 38);
        viewport.offsetMax = new Vector2(0, -6);
        viewport.gameObject.AddComponent<RectMask2D>();
        boardList = Ui.Rect(viewport, "List", Top, Vector2.zero, Vector2.zero);
        boardList.anchorMin = new Vector2(0, 1);
        boardList.anchorMax = new Vector2(1, 1);
        boardList.pivot = new Vector2(0.5f, 1);
        boardScroll = new ScrollArea(viewport, boardList);
        boardStatus = Ui.Label(Ui.Rect(sheet.transform, "Status", Center, new Vector2(0, 40), new Vector2(320, 80)), "", 18, Ui.BrownSoft);
        boardStatus.lineSpacing = 1.1f;
        boardRetry = MakeButton(sheet.transform, Center, new Vector2(0, -30), new Vector2(160, 46), Ui.Blue, Ui.BlueShade, Color.white, RefreshBoard);
        boardRetry.Label.text = "Try again";
        boardRetry.Rect.gameObject.SetActive(false);
    }

    void ShowBoard(bool open)
    {
        Synth.Instance.Click();
        boardPanel.SetActive(open);
        nameKeyboard = null;
        if (!open) return;
        boardNameHint.text = "";
        LayoutBoardTabs();
        ShowBoardTab(Online.HasBoard(Online.League) ? Online.League : Online.Weekly);
        StartCoroutine(Pop((RectTransform)boardMine.parent, 0.94f));
    }

    /// The tabs in a row; the league's only shows when the game's cloud project has a league board.
    void LayoutBoardTabs()
    {
        var shown = new List<int>();
        for (int i = 0; i < BoardTabs.Length; i++)
        {
            bool has = Online.HasBoard(BoardTabs[i]);
            boardTabs[i].Rect.gameObject.SetActive(has);
            if (has) shown.Add(i);
        }
        for (int k = 0; k < shown.Count; k++)
            boardTabs[shown[k]].Rect.anchoredPosition = new Vector2((k - (shown.Count - 1) / 2f) * 116, -94);
    }

    void ShowBoardTab(string board)
    {
        boardShown = board;
        for (int i = 0; i < boardTabs.Count; i++)
        {
            bool on = BoardTabs[i] == board;
            boardTabs[i].Face.color = on ? Ui.Blue : Ui.CreamShade;
            boardTabs[i].Lip.color = on ? Ui.BlueShade : Ui.GreyShade;
            boardTabs[i].Label.color = on ? Color.white : Ui.Brown;
        }
        RefreshBoard();
    }

    // MARK: - Loading

    async void RefreshBoard()
    {
        int request = ++boardRequest;
        string board = boardShown;
        ClearBoardRows();
        boardRetry.Rect.gameObject.SetActive(false);
        SetBoardStatus("Loading…");
        FillBanner(boardMine, MyRow(null), mine: true);

        List<Online.Row> rows;
        Online.Row mine;
        if (boardDemo != null)
        {
            rows = boardDemo;
            mine = boardDemo.Find(r => r.PlayerId == "me");
        }
        else
        {
            rows = await Online.Top(board);
            mine = rows == null ? null : await Online.Mine(board);
        }
        if (request != boardRequest || !boardPanel.activeSelf) return;   // a newer request, or closed meanwhile

        if (rows == null && !Online.HasBoard(board))
        {
            // No league board in this project (yet): carry on without the tab.
            LayoutBoardTabs();
            ShowBoardTab(Online.Weekly);
            return;
        }
        if (rows == null)
        {
            SetBoardStatus(Online.Status == Online.State.NotSetUp
                ? "The leaderboard isn't switched on yet."
                : "Can't reach the leaderboard.\nCheck your internet and try again.");
            boardRetry.Rect.gameObject.SetActive(Online.Status != Online.State.NotSetUp);
            return;
        }
        SetBoardStatus(rows.Count > 0 ? "" : board switch
        {
            Online.League => "Climb once this week to join\na league of 40 climbers!",
            Online.Weekly => "Nobody has climbed this week yet.\nBe the first!",
            _ => "Nobody has climbed yet.\nBe the first!",
        });
        string myId = boardDemo != null ? "me" : Online.PlayerId;
        for (int i = 0; i < rows.Count; i++)
        {
            var rect = Ui.Rect(boardList, "Row", Top, new Vector2(0, -8 - i * RowStep), Vector2.zero);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(-24, RowHeight);
            bool isMe = rows[i].PlayerId == myId;
            FillBanner(rect, isMe ? MyRow(rows[i]) : rows[i], mine: isMe);
        }
        boardList.sizeDelta = new Vector2(0, rows.Count * RowStep + 16);
        boardScroll.Reset();
        FillBanner(boardMine, MyRow(mine), mine: true);
    }

    /// Your row: your name and look as they are on this phone right now, with the board's rank and height.
    static Online.Row MyRow(Online.Row fromBoard) => new()
    {
        Rank = fromBoard?.Rank ?? 0,
        Meters = fromBoard?.Meters ?? 0,
        Name = Online.MyName,
        Look = Online.Look.Mine(),
        PlayerId = fromBoard?.PlayerId,
    };

    void SetBoardStatus(string text)
    {
        boardStatus.text = text;
        boardStatus.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    void ClearBoardRows()
    {
        for (int i = boardList.childCount - 1; i >= 0; i--) Destroy(boardList.GetChild(i).gameObject);
        boardPatterns.RemoveAll(p => p.pattern == null || p.pattern.transform.IsChildOf(boardList));
        boardNames.RemoveAll(n => n.label == null || n.label.transform.IsChildOf(boardList));
        boardList.sizeDelta = Vector2.zero;
    }

    // MARK: - A banner

    static readonly uint[] MedalColors = { 0xFFC83D, 0xCFD8DC, 0xE0A060 };

    /// Fills a rect with one player's banner: their tag behind, a rank medal, their portrait, name and height.
    void FillBanner(RectTransform rect, Online.Row row, bool mine)
    {
        for (int i = rect.childCount - 1; i >= 0; i--) Destroy(rect.GetChild(i).gameObject);
        boardPatterns.RemoveAll(p => p.pattern == null || p.pattern.transform.IsChildOf(rect));
        boardNames.RemoveAll(n => n.label == null || n.label.transform.IsChildOf(rect));

        if (mine)
        {
            var frame = Ui.Rect(rect, "Frame", Center, Vector2.zero, Vector2.zero);
            Ui.Stretch(frame);
            frame.offsetMin = new Vector2(-4, -4);
            frame.offsetMax = new Vector2(4, 4);
            Ui.Rounded(frame, Ui.Blue, 24);
        }
        var style = Cosmetics.Tag(row.Look?.t ?? "").Tag;
        var banner = Ui.Rect(rect, "Banner", Center, Vector2.zero, Vector2.zero);
        Ui.Stretch(banner);
        Ui.Rounded(banner, Color.white, 20);
        banner.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var gradient = Ui.Rect(banner, "Gradient", Center, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
        Ui.Stretch(gradient.rectTransform);
        gradient.texture = TagArt.Gradient(style.Colors);
        gradient.raycastTarget = false;
        var ink = Voxel.Hex(style.Ink);
        if (style.Rays)
            Ui.Picture(banner, new Vector2(0, 0.5f), new Vector2(92, 0), new Vector2(420, 420), RaysSprite, new Color(ink.r, ink.g, ink.b, style.InkAlpha));
        var patternTexture = TagArt.Pattern(style.Pattern);
        if (patternTexture != null)
        {
            var pattern = Ui.Rect(banner, "Pattern", Center, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            Ui.Stretch(pattern.rectTransform);
            pattern.texture = patternTexture;
            pattern.color = new Color(ink.r, ink.g, ink.b, style.InkAlpha);
            pattern.raycastTarget = false;
            boardPatterns.Add((pattern, style.Tile));
        }

        // Rank: a medal for the top three.
        bool medal = row.Rank is >= 1 and <= 3;
        var badge = Ui.Circle(rect, new Vector2(0, 0.5f), new Vector2(32, 0), 40, medal ? Voxel.Hex(MedalColors[row.Rank - 1]) : Ui.Cream);
        Ui.Label(badge.transform, row.Rank > 0 ? row.Rank.ToString() : "–", row.Rank >= 100 ? 13 : 17, Ui.Brown);

        var portrait = Ui.Rect(rect, "Portrait", new Vector2(0, 0.5f), new Vector2(90, 0), new Vector2(64, 64)).gameObject.AddComponent<RawImage>();
        portrait.texture = Portrait(row.Look);
        portrait.raycastTarget = false;

        var nameRect = Ui.Rect(rect, "Name", Center, Vector2.zero, Vector2.zero);
        Ui.Stretch(nameRect);
        nameRect.offsetMin = new Vector2(126, 0);
        nameRect.offsetMax = new Vector2(-112, 0);
        // White with a dark outline, readable on light and dark tags; shrunk to fit on one line in UpdateBoard.
        var name = Ui.Label(nameRect, row.Name, 19, Color.white, TextAnchor.MiddleLeft, shadow: true);
        var outline = name.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.18f, 0.1f, 0.75f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
        boardNames.Add((name, -1));
        if (mine)
        {
            var you = Ui.Pill(rect, new Vector2(0, 1), new Vector2(150, -2), new Vector2(46, 20), Ui.Blue, Ui.BlueShade, 2);
            Ui.Label(you.transform, "YOU", 12, Color.white, TextAnchor.MiddleCenter, shadow: true);
        }

        var height = Ui.Pill(rect, new Vector2(1, 0.5f), new Vector2(-56, 0), new Vector2(96, 34), Ui.Cream, Ui.CreamShade, 3);
        Ui.Label(height.transform, row.Meters > 0 ? $"{row.Meters:N0} m" : "–", 17, Ui.Brown);
    }

    /// A head-and-shoulders picture of a character with their hat, cached per look.
    Texture2D Portrait(Online.Look look)
    {
        var character = Cosmetics.Character(look?.c ?? "");
        var hat = Cosmetics.Hat(look?.h ?? "");
        string key = character.Id + "|" + hat.Id;
        if (portraits.TryGetValue(key, out var cached)) return cached;
        PoseClimber(character, hat, null, null);
        var root = previewClimber.transform;
        root.localRotation = Quaternion.Euler(0, 20, 0);
        var head = new Bounds(root.TransformPoint(0, 0.88f, 0), new Vector3(0.8f, 0.7f, 0.5f));
        FitPreview(root.Find("rig/hat"), 0.5f, head);
        return portraits[key] = RenderThumb();
    }

    // MARK: - Every frame

    void UpdateBoard()
    {
        if (boardPanel == null || !boardPanel.activeSelf) return;
        boardScroll.Update(Time.unscaledDeltaTime);
        // Patterns tile at their own size, whatever the banner's size.
        foreach (var (pattern, tile) in boardPatterns)
        {
            if (pattern == null) continue;
            var size = pattern.rectTransform.rect.size / tile;
            pattern.uvRect = new Rect(0, 0, size.x, size.y);
        }
        // Long names shrink to fit on one line (once per name, or when the width changes).
        for (int i = 0; i < boardNames.Count; i++)
        {
            var (label, fitted) = boardNames[i];
            if (label == null) continue;
            float width = label.rectTransform.rect.width;
            if (width <= 0 || Mathf.Approximately(width, fitted)) continue;
            label.fontSize = 19;
            float needed = label.preferredWidth;
            if (needed > width) label.fontSize = Mathf.Max(11, Mathf.FloorToInt(19 * width / needed));
            boardNames[i] = (label, width);
        }

        if (nameKeyboard == null) return;
        switch (nameKeyboard.status)
        {
            case TouchScreenKeyboard.Status.Done:
                string typed = nameKeyboard.text;
                nameKeyboard = null;
                ApplyName(typed);
                break;
            case TouchScreenKeyboard.Status.Canceled:
            case TouchScreenKeyboard.Status.LostFocus:
                nameKeyboard = null;
                break;
        }
    }

    // MARK: - Your name

    void StartRename()
    {
        boardNameHint.text = "";
        if (!TouchScreenKeyboard.isSupported)
        {
            RandomName();   // no on-screen keyboard (the Mac test build): pick a random one instead
            return;
        }
        nameKeyboard = TouchScreenKeyboard.Open(Online.MyName, TouchScreenKeyboardType.Default, false, false, false, false, "Your name", Names.MaxLength);
    }

    void RandomName()
    {
        Synth.Instance.Click();
        ApplyName(Names.Random());
    }

    async void ApplyName(string typed)
    {
        string name = Names.Clean(typed);
        string problem = Names.Problem(name);
        if (problem != null)
        {
            boardNameHint.text = problem;
            Synth.Instance.Nope();
            Haptics.Slip();
            return;
        }
        boardNameHint.text = "";
        if (name == Online.MyName) return;
        Synth.Instance.Swoosh();
        var renaming = Online.Rename(name);
        FillBanner(boardMine, MyRow(null), mine: true);
        StartCoroutine(Bump(boardMine, 0.06f));
        await renaming;
        if (boardPanel.activeSelf) RefreshBoard();   // so the list shows the new name too
    }

    // MARK: - After a run

    /// Posts the run's height, then shows this week's rank on the results screen.
    async void SubmitRun(int meters)
    {
        runResult = null;
        var result = await Online.Submit(meters);
        if (result == null) return;
        runResult = result;
        if (gameOverPanel.activeSelf && !resultsRevealing) ShowRunRank();
        FetchRivals();   // the ones to beat have changed
    }

    bool resultsRevealing;

    void ShowRunRank()
    {
        if (runResult == null || runResult.WeeklyRank <= 0 && runResult.LeagueRank <= 0) return;
        runRankButton.Label.text = runResult.LeagueRank > 0 ? $"#{runResult.LeagueRank} in your league" : $"#{runResult.WeeklyRank:N0} this week";
        if (runRankButton.Rect.gameObject.activeSelf) return;
        runRankButton.Rect.gameObject.SetActive(true);
        StartCoroutine(Pop(runRankButton.Rect, 0.6f));
    }

    // MARK: - Test data

    List<Online.Row> DemoRows()
    {
        var rows = new List<Online.Row>();
        var characters = Cosmetics.Characters;
        var hats = Cosmetics.Hats;
        var tags = Cosmetics.Tags;
        int meters = 2480;
        for (int i = 0; i < 24; i++)
        {
            bool me = i == 6;
            rows.Add(new Online.Row
            {
                Rank = i + 1,
                Name = me ? Online.MyName : Names.Random(),
                PlayerId = me ? "me" : "p" + i,
                Meters = meters,
                Look = me ? Online.Look.Mine() : new Online.Look
                {
                    c = characters[Random.Range(0, characters.Count)].Id,
                    h = hats[Random.Range(0, hats.Count)].Id,
                    t = tags[Random.Range(0, tags.Count)].Id,
                },
            });
            meters -= Random.Range(20, 140);
        }
        return rows;
    }
}
