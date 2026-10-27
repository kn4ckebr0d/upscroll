using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// The title screen and the full-screen cards: settings, "keep climbing?", results and the stamp book.
/// Full-screen cards sit on a soft patterned backdrop with drifting clouds, like Animal Crossing's menus.
public partial class ScrollyGame
{
    GameObject titleGroup, settingsPanel, continuePanel, gameOverPanel;
    RectTransform continueCard, resultsCard, settingsCard;
    HudButton titleBoardButton, runRankButton;
    Text continueHeight, gameOverScore, gameOverDelta, gameOverCoins;
    RectTransform gameOverBest, stampRow, stampBadge, lifePriceTag;
    Text stampBadgeText, lifePriceText;
    GameObject stampsLabel, noStampsLabel;
    RectTransform gameOverCoinPill, nextUnlockFill;
    HudButton nextUnlockRow;
    RawImage nextUnlockThumb;
    Text nextUnlockName, nextUnlockCount;
    Cosmetic nextUnlock;
    HudButton settingsButton, stampsButton, lifeButton, continueButton, declineButton, playAgainButton;
    HudButton soundToggle, hapticsToggle, shakeToggle, bossTestToggle, settingsDone;
    static Texture2D patternTexture;
    static Sprite cloudSprite;

    /// The full-screen card that's open (it takes all touches), or null.
    RectTransform OpenModal
    {
        get
        {
            if (boardPanel != null && boardPanel.activeSelf) return (RectTransform)boardPanel.transform;
            if (settingsPanel != null && settingsPanel.activeSelf) return (RectTransform)settingsPanel.transform;
            if (bookPanel != null && bookPanel.activeSelf) return (RectTransform)bookPanel.transform;
            if (closetPanel != null && closetPanel.activeSelf) return (RectTransform)closetPanel.transform;
            if (giftPanel != null && giftPanel.activeSelf) return (RectTransform)giftPanel.transform;
            if (continuePanel != null && continuePanel.activeSelf) return (RectTransform)continuePanel.transform;
            if (gameOverPanel != null && gameOverPanel.activeSelf) return (RectTransform)gameOverPanel.transform;
            return null;
        }
    }

    void BuildModals()
    {
        BuildContinueOffer();
        BuildResults();
        BuildStampBook();
        BuildCloset();
        BuildGift();
        BuildBoard();
        BuildSettings();
    }

    // MARK: - Title

    void BuildTitle()
    {
        titleGroup = Ui.Rect(safeRect, "Title", Center, Vector2.zero, Vector2.zero).gameObject;
        var t = (RectTransform)titleGroup.transform;
        Ui.Stretch(t);

        var card = Ui.Box(t, Center, new Vector2(0, 150), new Vector2(320, 116), Ui.Cream, Ui.CreamShade, 30, 6);
        var title = Ui.Label(card.transform, "Scrolly Road", 48, Ui.Brown);
        title.rectTransform.anchoredPosition = new Vector2(0, 4);
        var ribbon = Ui.Pill(t, Center, new Vector2(118, 208), new Vector2(76, 28), Ui.Blue, Ui.BlueShade, 3);
        Ui.Label(ribbon.transform, "alpha", 15, Color.white);
        ribbon.transform.parent.localRotation = Quaternion.Euler(0, 0, -8);
        // Your best, which is also the way into the leaderboard.
        titleBoardButton = MakeButton(t, Center, new Vector2(0, 84), new Vector2(210, 44), Ui.Blue, Ui.BlueShade, Color.white,
            () => ShowBoard(true), icon: Ui.Icon("trophy"));
        titleBoardButton.Label.fontSize = 17;
        titleBoardButton.Label.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.25f);

        // Settings live behind a gear, top left: secondary, out of the way.
        settingsButton = MakeRoundButton(t, TopLeft, new Vector2(34, -34), 48, Ui.Cream, Ui.CreamShade, Ui.Icon("gear"), Ui.BrownSoft,
            () => ShowSettings(true));
        settingsButton.Label.gameObject.SetActive(false);

        // Stamps and extra lives as round buttons on the sides, clear of the climber in the middle.
        stampsButton = MakeRoundButton(t, BottomLeft, new Vector2(66, 196), 72, Ui.Cream, Ui.CreamShade, Ui.Icon("stamp"), Ui.Brown,
            () => ShowStampBook(true));
        var badge = Ui.Circle(stampsButton.Rect, Center, new Vector2(26, 26), 28, Ui.Gold);
        stampBadge = badge.rectTransform;
        stampBadgeText = Ui.Label(stampBadge, "", 15, Ui.OnGold, TextAnchor.MiddleCenter);

        lifeButton = MakeRoundButton(t, BottomRight, new Vector2(-66, 196), 72, Ui.Cream, Ui.CreamShade, Ui.Icon("heart"), Color.white, BuyExtraLife);
        var tag = Ui.Pill(lifeButton.Rect, Center, new Vector2(0, -34), new Vector2(70, 26), Ui.Gold, Ui.GoldShade, 2);
        lifePriceTag = (RectTransform)tag.transform.parent;
        lifePriceText = Ui.Label(tag.transform, "", 15, Ui.OnGold, TextAnchor.MiddleRight);
        lifePriceText.rectTransform.offsetMax = new Vector2(-10, 0);
        Ui.Coin(lifePriceTag, new Vector2(0, 0.5f), new Vector2(12, 0), 22);
        lifeButton.Label.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -36 - 30);

        // The closet and the daily gift, a row above.
        closetButton = MakeRoundButton(t, BottomLeft, new Vector2(66, 318), 64, Ui.Cream, Ui.CreamShade, Ui.Icon("shirt"), Ui.Brown,
            () => ShowCloset(true));
        closetButton.Label.text = "Closet";
        giftButton = MakeRoundButton(t, BottomRight, new Vector2(-66, 318), 64, Ui.Cream, Ui.CreamShade, Ui.Icon("gift"), Voxel.Hex(0xE85A4F),
            () => ShowGift(true));
        giftButton.Label.text = "Gift";
        var giftDot = Ui.Circle(giftButton.Rect, Center, new Vector2(23, 23), 26, Ui.Gold);
        giftBadge = giftDot.rectTransform;
        Ui.Label(giftBadge, "!", 17, Ui.OnGold, TextAnchor.MiddleCenter);

        // The one main action, in the thumb zone.
        var prompt = Ui.Pill(t, Bottom, new Vector2(0, 62), new Vector2(250, 52), Ui.Gold, Ui.GoldShade, 4);
        dragPrompt = (RectTransform)prompt.transform.parent;
        Ui.Label(prompt.transform, "Drag down to climb", 20, Ui.OnGold, TextAnchor.MiddleCenter);
    }

    void ShowTitle(bool visible) => titleGroup.SetActive(visible);

    void UpdateScreens()
    {
        titleGroup.SetActive(phase == Phase.Title && OpenModal == null);
        // Full-screen menus cover the world, so don't draw it (and its shadows) behind them.
        bool covered = closetPanel.activeSelf || giftPanel.activeSelf || boardPanel.activeSelf || bookPanel.activeSelf || settingsPanel.activeSelf;
        int mask = covered ? 0 : worldCullingMask;
        if (cam.cullingMask != mask) cam.cullingMask = mask;
        coinPill.gameObject.SetActive(OpenModal == null);
        coinIcon.gameObject.SetActive(OpenModal == null);
    }

    /// Labels on the title screen and in the settings that depend on saved progress.
    void RefreshMenus()
    {
        int lives = Progress.ExtraLives;
        bool full = lives >= Tuning.MaxExtraLives;
        lifeButton.Label.text = $"Lives {lives}/{Tuning.MaxExtraLives}";
        lifePriceText.text = full ? "FULL" : Tuning.ExtraLifePrice.ToString();
        stampsButton.Label.text = $"Stamps {StampsCollected}/{StampTotal}";
        int unseen = UnseenStamps;
        stampBadge.gameObject.SetActive(unseen > 0);
        stampBadgeText.text = unseen.ToString();
        giftBadge.gameObject.SetActive(GiftReady);
        titleBoardButton.Label.text = Progress.Best > 0 ? $"BEST {Progress.Best} m" : "Leaderboard";
        SetToggle(soundToggle, Synth.Instance.Enabled);
        SetToggle(hapticsToggle, Haptics.Enabled);
        SetToggle(shakeToggle, Progress.ShakeOn);
        SetToggle(bossTestToggle, Progress.BossFirst);
    }

    // MARK: - Backdrop

    /// A full-screen panel with the patterned backdrop; returns the panel.
    RectTransform MakeModal(string name)
    {
        var panel = Ui.Rect(canvasRect, name, Center, Vector2.zero, Vector2.zero);
        Ui.Stretch(panel);
        patternTexture ??= MakePattern();
        cloudSprite ??= Ui.MakeShape(160, (x, y) =>
        {
            // Three puffs on a flat bottom.
            bool puff = (x + 0.4f) * (x + 0.4f) + (y + 0.05f) * (y + 0.05f) < 0.2f
                     || x * x + (y - 0.15f) * (y - 0.15f) < 0.3f
                     || (x - 0.45f) * (x - 0.45f) + (y + 0.08f) * (y + 0.08f) < 0.16f;
            return puff && y > -0.42f ? 1 : 0;
        });
        var pattern = Ui.Rect(panel, "Pattern", Center, Vector2.zero, Vector2.zero);
        Ui.Stretch(pattern);
        var raw = pattern.gameObject.AddComponent<RawImage>();
        raw.texture = patternTexture;
        raw.color = new Color(1, 1, 1, 0.94f);
        raw.raycastTarget = false;
        var backdrop = panel.gameObject.AddComponent<Backdrop>();
        backdrop.Pattern = raw;
        for (int i = 0; i < 3; i++)
        {
            var cloud = Ui.Picture(panel, Center, new Vector2(-150 + i * 150, 300 - i * 260), new Vector2(150, 84), cloudSprite, new Color(1, 1, 1, 0.55f));
            backdrop.Clouds.Add(cloud.rectTransform);
        }
        panel.gameObject.SetActive(false);
        return panel;
    }

    /// Soft blue with a lattice of lighter diamonds, tiled across the backdrop.
    static Texture2D MakePattern()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var baseColor = Voxel.Hex(0x9ED3EE);
        var diamond = Voxel.Hex(0xB9E2F5);
        var pixels = new Color[size * size];
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float a = 0;
                for (int s = 0; s < 4; s++)
                {
                    float x = (px + (s % 2 + 0.5f) / 2f) / size, y = (py + (s / 2 + 0.5f) / 2f) / size;
                    // Diamonds at the center and the corners, so the tile repeats seamlessly.
                    float d = Mathf.Min(Mathf.Abs(x - 0.5f) + Mathf.Abs(y - 0.5f),
                        Mathf.Min(Mathf.Min(x, 1 - x) + Mathf.Min(y, 1 - y), 1));
                    a += d < 0.2f ? 1 : 0;
                }
                pixels[py * size + px] = Color.Lerp(baseColor, diamond, a / 4f);
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // MARK: - Settings

    void BuildSettings()
    {
        var panel = MakeModal("Settings");
        settingsPanel = panel.gameObject;
        var card = Ui.Box(panel, Center, new Vector2(0, 60), new Vector2(320, 346), Ui.Cream, Ui.CreamShade, 32, 6);
        settingsCard = (RectTransform)card.transform.parent;
        var t = card.transform;
        var title = Ui.Label(t, "Settings", 30, Ui.Brown);
        title.rectTransform.anchoredPosition = new Vector2(0, 133);
        soundToggle = MakeToggle(t, 71, "Sound", Ui.Icon("sound_on"), ToggleSound);
        hapticsToggle = MakeToggle(t, 9, "Vibration", Ui.Icon("haptics"), ToggleHaptics);
        shakeToggle = MakeToggle(t, -53, "Screen shake", Ui.Icon("shake"), ToggleShake);
        var howTo = MakeButton(t, Center, new Vector2(0, -120), new Vector2(280, 52), Ui.Gold, Ui.GoldShade, Ui.OnGold, ReplayTutorial, icon: Ui.Icon("hand"));
        howTo.Label.text = "How to play";
        howTo.Icon.color = Ui.OnGold;

        // Alpha only: reach a boss planet quickly to try it.
        bossTestToggle = MakeToggle(panel, 0, "Alpha: boss next", StampArt.Icon(5, 1), ToggleBossTest);
        bossTestToggle.Rect.anchorMin = bossTestToggle.Rect.anchorMax = Bottom;
        bossTestToggle.Rect.anchoredPosition = new Vector2(0, 150);
        bossTestToggle.Rect.sizeDelta = new Vector2(300, 48);
        // Alpha only: a fresh daily gift right away, to try the gift again.
        var giftTest = MakeButton(panel, Bottom, new Vector2(0, 206), new Vector2(300, 48), Ui.CreamShade, Ui.GreyShade, Ui.Brown, ResetGiftForTest, icon: Ui.Icon("gift"));
        giftTest.Face.color = Voxel.Hex(0xF3E9CC);
        giftTest.Label.text = "Alpha: new daily gift";
        settingsDone = MakeButton(panel, Bottom, new Vector2(0, 72), new Vector2(240, 58), Ui.Blue, Ui.BlueShade, Color.white,
            () => ShowSettings(false), icon: Ui.Icon("close"));
        settingsDone.Label.text = "Done";
        settingsDone.Label.fontSize = 22;
    }

    HudButton MakeToggle(Transform parent, float y, string label, Sprite icon, System.Action action)
    {
        var button = MakeButton(parent, Center, new Vector2(0, y), new Vector2(280, 52), Ui.CreamShade, Ui.GreyShade, Ui.Brown, action, icon: icon);
        button.Face.color = Voxel.Hex(0xF3E9CC);
        button.Label.text = label;
        // The on/off state as a pill on the right.
        var pill = Ui.Pill(button.Face.transform, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(62, 32), Ui.Blue, Ui.BlueShade, 2);
        button.Price = Ui.Label(pill.transform, "", 15, Color.white, TextAnchor.MiddleCenter, shadow: true);
        return button;
    }

    static void SetToggle(HudButton toggle, bool on)
    {
        if (toggle == null) return;
        toggle.Price.text = on ? "ON" : "OFF";
        var pill = toggle.Price.transform.parent.GetComponent<Image>();
        pill.color = on ? Ui.Blue : Ui.Grey;
        pill.transform.parent.GetChild(0).GetComponent<Image>().color = on ? Ui.BlueShade : Ui.GreyShade;
    }

    void ShowSettings(bool open)
    {
        Synth.Instance.Click();
        settingsPanel.SetActive(open);
        if (open)
        {
            RefreshMenus();
            StartCoroutine(Pop(settingsCard, 0.85f));
        }
    }

    void ToggleHaptics()
    {
        Haptics.Enabled = Progress.HapticsOn = !Progress.HapticsOn;
        Progress.Save();
        Haptics.Tick(1f);
        Synth.Instance.Click();
        RefreshMenus();
    }

    void ToggleBossTest()
    {
        Progress.BossFirst = !Progress.BossFirst;
        Progress.Save();
        Synth.Instance.Click();
        RefreshMenus();
        ResetPlanetEffects();   // applies to the run you start next
    }

    /// Alpha only: forget today's gift (and its rolls) and open a new one.
    void ResetGiftForTest()
    {
        Progress.GiftDay = "";
        Progress.ResetGiftSeed();
        Progress.Save();
        settingsPanel.SetActive(false);
        ShowGift(true);
    }

    void ToggleShake()
    {
        Progress.ShakeOn = !Progress.ShakeOn;
        Progress.Save();
        Synth.Instance.Click();
        if (Progress.ShakeOn) ShakeCamera(0.15f);
        RefreshMenus();
    }

    // MARK: - Keep climbing?

    /// Right after a fall: one clear question, keep going with the rocket or not.
    void BuildContinueOffer()
    {
        var panel = MakeModal("ContinueOffer");
        continuePanel = panel.gameObject;
        var card = Ui.Box(panel, Center, new Vector2(0, 60), new Vector2(320, 250), Ui.Cream, Ui.CreamShade, 32, 6);
        continueCard = (RectTransform)card.transform.parent;
        var t = card.transform;
        var title = Ui.Label(t, "Keep climbing?", 30, Ui.Brown);
        title.rectTransform.anchoredPosition = new Vector2(0, 84);
        var rocket = Ui.Picture(t, Center, new Vector2(-92, 4), new Vector2(72, 72), StampArt.Face(0), Ui.Blue);
        Ui.Picture(rocket.rectTransform, Center, Vector2.zero, new Vector2(44, 44), StampArt.Icon(0, 3), Color.white);
        var hint = Ui.Label(t, "The rocket flies\nyou back up to", 16, Ui.BrownSoft, TextAnchor.MiddleLeft);
        hint.rectTransform.offsetMin = new Vector2(118, 0);
        hint.rectTransform.anchoredPosition += new Vector2(0, 26);
        hint.lineSpacing = 1.05f;
        continueHeight = Ui.Label(t, "", 40, Ui.Brown, TextAnchor.MiddleLeft);
        continueHeight.rectTransform.offsetMin = new Vector2(118, 0);
        continueHeight.rectTransform.anchoredPosition += new Vector2(0, -22);

        // The answers in the thumb zone: a big yes, a quiet no.
        continueButton = MakeButton(panel, Bottom, new Vector2(0, 136), new Vector2(300, 62), Ui.Blue, Ui.BlueShade, Color.white,
            ContinueFromCheckpoint, withPrice: true, icon: StampArt.Icon(0, 3));
        continueButton.Label.text = "Continue";
        continueButton.Label.fontSize = 22;
        declineButton = MakeButton(panel, Bottom, new Vector2(0, 60), new Vector2(170, 48), Ui.Cream, Ui.CreamShade, Ui.BrownSoft, DeclineContinue);
        declineButton.Label.text = "No thanks";
    }

    void ShowContinueOffer(bool visible)
    {
        continuePanel.SetActive(visible);
        if (!visible) return;
        HideMessages();
        continueHeight.text = $"{Mathf.FloorToInt(anchorY)}<size=22><color=#A48F6C> m</color></size>";
        continueButton.Price.text = ContinuePrice.ToString();
        RefreshShop();
        StartCoroutine(Pop(continueCard, 0.85f));
    }

    // MARK: - Results

    void BuildResults()
    {
        var panel = MakeModal("Results");
        gameOverPanel = panel.gameObject;
        var card = Ui.Box(panel, Center, new Vector2(0, 82), new Vector2(330, 424), Ui.Cream, Ui.CreamShade, 32, 6);
        resultsCard = (RectTransform)card.transform.parent;
        var t = card.transform;
        var title = Ui.Label(t, "Ouch!", 30, Ui.Brown);
        title.rectTransform.anchoredPosition = new Vector2(0, 173);
        gameOverScore = Ui.Label(t, "0", 76, Ui.Brown);
        gameOverScore.rectTransform.anchoredPosition = new Vector2(0, 111);
        gameOverDelta = Ui.Label(t, "", 16, Ui.Brown);
        gameOverDelta.rectTransform.anchoredPosition = new Vector2(0, 57);

        var best = Ui.Pill(t, Center, new Vector2(0, 57), new Vector2(160, 34), Ui.Gold, Ui.GoldShade, 3);
        gameOverBest = (RectTransform)best.transform.parent;
        Ui.Label(best.transform, "NEW BEST!", 18, Ui.OnGold, TextAnchor.MiddleCenter);
        gameOverBest.localRotation = Quaternion.Euler(0, 0, -4);

        var coins = Ui.Pill(t, Center, new Vector2(0, 11), new Vector2(124, 36), Ui.Blue, Ui.BlueShade, 3);
        gameOverCoinPill = (RectTransform)coins.transform.parent;
        gameOverCoins = Ui.Label(coins.transform, "+0", 20, Color.white, TextAnchor.MiddleRight, shadow: true);
        gameOverCoins.rectTransform.offsetMax = new Vector2(-18, 0);
        Ui.Coin(coins.transform.parent, new Vector2(0, 0.5f), new Vector2(18, 0), 32);

        var stampsTitle = Ui.Label(t, "STAMPS THIS RUN", 14, Ui.BrownSoft);
        stampsTitle.rectTransform.anchoredPosition = new Vector2(0, -33);
        stampsLabel = stampsTitle.gameObject;
        stampRow = Ui.Rect(t, "StampRow", Center, new Vector2(0, -75), new Vector2(300, 52));
        var noStamps = Ui.Label(t, "Reach a new zone\nto earn a stamp", 16, Ui.Brown);
        noStamps.rectTransform.anchoredPosition = new Vector2(0, -59);
        noStamps.lineSpacing = 1.05f;
        noStampsLabel = noStamps.gameObject;

        BuildNextUnlock(t);

        // Where this run puts you this week (once the leaderboard answers), and the way to see it.
        runRankButton = MakeButton(panel, Bottom, new Vector2(0, 152), new Vector2(240, 44), Ui.Cream, Ui.CreamShade, Ui.Brown,
            () => ShowBoard(true), icon: Ui.Icon("trophy"));
        runRankButton.Icon.color = Ui.Gold;
        runRankButton.Rect.gameObject.SetActive(false);

        // Play again where the thumb is.
        playAgainButton = MakeButton(panel, Bottom, new Vector2(0, 72), new Vector2(300, 62), Ui.Blue, Ui.BlueShade, Color.white,
            PlayAgain, icon: Ui.Icon("replay"));
        playAgainButton.Label.text = "Play again";
        playAgainButton.Label.fontSize = 22;
    }

    // MARK: - The next unlock

    /// A strip at the bottom of the results card: the cheapest thing you don't own yet, and how close your
    /// coins are to it. Tap it to see it in the closet.
    void BuildNextUnlock(Transform card)
    {
        var face = Ui.Box(card, Center, new Vector2(0, -164), new Vector2(296, 58), Voxel.Hex(0xF3E9CC), Ui.CreamShade, 20, 3);
        var rect = (RectTransform)face.transform.parent;
        nextUnlockRow = new HudButton { Rect = rect, Face = face, Lip = rect.GetChild(0).GetComponent<Image>(), Action = OpenNextUnlock };
        buttons.Add(nextUnlockRow);
        nextUnlockThumb = Ui.Rect(face.transform, "Thumb", new Vector2(0, 0.5f), new Vector2(32, 2), new Vector2(50, 50)).gameObject.AddComponent<RawImage>();
        nextUnlockThumb.raycastTarget = false;
        nextUnlockName = Ui.Label(Ui.Rect(face.transform, "Name", new Vector2(0, 0.5f), new Vector2(150, 11), new Vector2(200, 22)), "", 15, Ui.Brown, TextAnchor.MiddleLeft);
        nextUnlockCount = Ui.Label(Ui.Rect(face.transform, "Count", new Vector2(1, 0.5f), new Vector2(-46, 11), new Vector2(80, 22)), "", 14, Ui.Brown, TextAnchor.MiddleRight);
        var track = Ui.Rect(face.transform, "Track", new Vector2(0, 0.5f), new Vector2(166, -11), new Vector2(208, 12));
        Ui.Rounded(track, Ui.CreamShade, 6);
        nextUnlockFill = Ui.Rect(track, "Fill", Center, Vector2.zero, Vector2.zero);
        nextUnlockFill.anchorMin = Vector2.zero;
        nextUnlockFill.anchorMax = new Vector2(0.5f, 1);
        nextUnlockFill.offsetMin = nextUnlockFill.offsetMax = Vector2.zero;
        Ui.Rounded(nextUnlockFill, Ui.Gold, 6);
        rect.gameObject.SetActive(false);
    }

    /// The cheapest item for coins you don't own yet, or null when you have them all.
    Cosmetic NextUnlock() => AllItems.Where(i => i.Price > 0 && !Owns(i)).OrderBy(i => i.Price).FirstOrDefault();

    void ShowNextUnlock()
    {
        nextUnlock = NextUnlock();
        nextUnlockRow.Rect.gameObject.SetActive(nextUnlock != null);
        if (nextUnlock == null) return;
        int coins = Progress.Coins, price = nextUnlock.Price;
        bool ready = coins >= price;
        nextUnlockThumb.texture = Thumbnail(nextUnlock);
        nextUnlockName.text = ready ? $"Get the {nextUnlock.Name}!" : $"Next: {nextUnlock.Name}";
        nextUnlockCount.text = ready ? "Closet ›" : $"{coins}/{price}";
        nextUnlockFill.anchorMax = new Vector2(Mathf.Clamp01(coins / (float)price), 1);
        StartCoroutine(Pop(nextUnlockRow.Rect, 0.8f));
    }

    void OpenNextUnlock()
    {
        if (nextUnlock == null) return;
        var item = nextUnlock;
        ShowCloset(true);
        ShowClosetTab(item.Kind);
        SelectClosetItem(item);
    }

    /// The results of the run, revealed one by one.
    void ShowGameOver(bool visible)
    {
        gameOverPanel.SetActive(visible);
        runRankButton.Rect.gameObject.SetActive(false);
        if (!visible) return;
        HideMessages();
        RefreshShop();
        StartCoroutine(RevealResults());
    }

    IEnumerator RevealResults()
    {
        resultsRevealing = true;
        nextUnlockRow.Rect.gameObject.SetActive(false);
        gameOverScore.text = "0";
        gameOverDelta.text = "";
        gameOverBest.gameObject.SetActive(false);
        gameOverCoins.text = "+0";
        bool anyStamps = runStamps.Count > 0;
        stampsLabel.SetActive(anyStamps);
        stampRow.gameObject.SetActive(anyStamps);
        noStampsLabel.SetActive(!anyStamps);
        foreach (Transform child in stampRow) Destroy(child.gameObject);
        yield return Pop(resultsCard, 0.85f);

        // The height counts up with rising ticks.
        int meters = lastRunMeters;
        float duration = Mathf.Clamp(meters / 200f, 0.35f, 0.9f);
        int lastStep = -1;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float u = 1 - (1 - t / duration) * (1 - t / duration);
            gameOverScore.text = $"{Mathf.RoundToInt(meters * u)}<size=34><color=#A48F6C> m</color></size>";
            int step = (int)(u * 10);
            if (step != lastStep)
            {
                lastStep = step;
                Synth.Instance.ClimbBlip(step, 12);
            }
            yield return null;
        }
        gameOverScore.text = $"{meters}<size=34><color=#A48F6C> m</color></size>";
        yield return Bump(gameOverScore.rectTransform, 0.12f);

        // How it compares to your best: a slammed-in badge, or how close you got.
        if (lastRunNewBest)
        {
            gameOverBest.gameObject.SetActive(true);
            Synth.Instance.NewBest();
            Haptics.Success();
            yield return Pop(gameOverBest, 2f);
        }
        else if (bestAtStart > 0)
        {
            int missing = bestAtStart - meters;
            gameOverDelta.text = missing <= 0 ? "You matched your best!" : $"{missing} m short of your best";
        }

        // Coins tick in (no "+0" pill on a run without coins), then what they bring you closer to.
        gameOverCoinPill.gameObject.SetActive(runCoins > 0);
        for (int c = 1; c <= runCoins; c++)
        {
            gameOverCoins.text = $"+{c}";
            if (c % Mathf.Max(1, runCoins / 8) == 0) Synth.Instance.Coin();
            yield return new WaitForSeconds(Mathf.Min(0.05f, 0.5f / Mathf.Max(1, runCoins)));
        }

        ShowNextUnlock();

        // Stamps pop in one by one.
        const int slots = 5;
        int first = Mathf.Max(0, runStamps.Count - slots);
        for (int i = first; i < runStamps.Count; i++)
        {
            int zone = runStamps[i];
            var position = new Vector2(-120 + (i - first) * 60, 0);
            var stamp = MakeStamp(stampRow, position, 52, PageOf(zone), zone % ZonesPerPlanet, Voxel.Hex(ThemeFor(zone).Stamp));
            stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, (i % 3 - 1) * 8);
            if (runNewStamps.Contains(i))
            {
                var tag = Ui.Pill(stampRow, Center, position + new Vector2(0, -31), new Vector2(46, 20), Ui.Gold, Ui.GoldShade, 2);
                Ui.Label(tag.transform, "NEW", 13, Ui.OnGold, TextAnchor.MiddleCenter);
            }
            Synth.Instance.Click();
            Haptics.Tick(0.6f);
            yield return Pop(stamp.rectTransform, 1.8f);
            yield return new WaitForSeconds(0.06f);
        }

        // Last, where this run puts you on this week's leaderboard (if it has answered by now).
        resultsRevealing = false;
        ShowRunRank();
    }
}

/// Drifts the patterned backdrop and its clouds.
public class Backdrop : MonoBehaviour
{
    public RawImage Pattern;
    public readonly System.Collections.Generic.List<RectTransform> Clouds = new();

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        var rect = (RectTransform)transform;
        var r = Pattern.uvRect;
        r.width = rect.rect.width / 96f;
        r.height = rect.rect.height / 96f;
        r.x = Mathf.Repeat(r.x + dt * 0.04f, 1f);
        r.y = Mathf.Repeat(r.y + dt * 0.04f, 1f);
        Pattern.uvRect = r;
        float half = rect.rect.width / 2 + 100;
        for (int i = 0; i < Clouds.Count; i++)
        {
            var p = Clouds[i].anchoredPosition;
            p.x += dt * (10 + i * 4);
            if (p.x > half) p.x = -half;
            Clouds[i].anchoredPosition = p;
        }
    }
}
