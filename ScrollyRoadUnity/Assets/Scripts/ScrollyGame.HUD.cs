using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// The play HUD in the Animal Crossing-inspired style (see UIStyle.cs). It stays slim because danger
/// comes from the top of the screen: one height card with the next zone on its gold tab, coins and
/// hearts on the right, this run's stamps and the planet chip on the left.
/// Messages live in ScrollyGame.Messages.cs, the menus and screens in ScrollyGame.Screens.cs.
public partial class ScrollyGame
{
    class HudButton
    {
        public RectTransform Rect;
        public Image Face;
        public Image Lip;
        public Image Icon;
        public Text Label;
        public Text Price;
        public System.Action Action;
        public RectTransform Clip;   // in a scrolling list: only tappable where the list shows it
        public bool Visible => Rect.gameObject.activeInHierarchy;
    }

    RectTransform canvasRect, safeRect;
    RectTransform heightCard, nextBubble, bestPill, coinPill, coinIcon, stampChip;
    Text heightText, nextText, bestText, coinText, stampChipText;
    Image stampChipFace, stampChipIcon;
    readonly List<Image> heartIcons = new();
    HudButton planetChip;
    RectTransform staminaBar;
    Image staminaFill;
    readonly List<HudButton> buttons = new();
    readonly List<int> runStamps = new();
    int shownCoins, coinsInFlight;
    float bestPillUntil;

    static readonly Vector2 Top = new(0.5f, 1), TopLeft = new(0, 1), TopRight = new(1, 1);
    static readonly Vector2 Center = new(0.5f, 0.5f), Bottom = new(0.5f, 0);
    static readonly Vector2 BottomLeft = new(0, 0), BottomRight = new(1, 0);

    void BuildHUD()
    {
        Ui.Init(uiFont);
        var canvasObject = new GameObject("HUD");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(393, 852);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = (RectTransform)canvasObject.transform;

        BuildStaminaBar();
        BuildBestTag();

        safeRect = Ui.SafeArea(canvasRect, "SafeArea");

        BuildTopBar();
        BuildMessages();
        BuildHints();
        BuildTutorial();
        BuildTitle();
        BuildInterruptCard();
        BuildModals();
    }

    /// Shown while the game is frozen after an interruption (a call, Control Center, a notification).
    void BuildInterruptCard()
    {
        var card = Ui.Box(canvasRect, Center, new Vector2(0, 40), new Vector2(310, 110), Ui.Cream, Ui.CreamShade, 30, 6);
        interruptCard = card.transform.parent.gameObject;
        var title = Ui.Label(card.transform, "Paused", 30, Ui.Brown);
        title.rectTransform.anchoredPosition = new Vector2(0, 18);
        var hint = Ui.Label(card.transform, "Touch the screen to grab on again", 16, Ui.Brown);
        hint.rectTransform.anchoredPosition = new Vector2(0, -24);
        interruptCard.SetActive(false);
    }

    // MARK: - Top bar

    void BuildTopBar()
    {
        // Height: a cream card with a big brown number, the next zone on a gold tab under it.
        var card = Ui.Box(safeRect, Top, new Vector2(0, -38), new Vector2(150, 58), Ui.Cream, Ui.CreamShade, 20, 5);
        heightCard = (RectTransform)card.transform.parent;
        heightText = Ui.Label(card.transform, "0", 38, Ui.Brown);
        var bubble = Ui.Pill(safeRect, Top, new Vector2(0, -70), new Vector2(214, 26), Ui.Gold, Ui.GoldShade, 3);
        nextBubble = (RectTransform)bubble.transform.parent;
        nextText = Ui.Label(bubble.transform, "", 14, Color.white, TextAnchor.MiddleCenter, shadow: true);

        // The best height only shows up when you beat it (the flag on the wall shows it before that).
        var best = Ui.Pill(safeRect, Top, new Vector2(0, -100), new Vector2(170, 28), Ui.Gold, Ui.GoldShade, 3);
        bestPill = (RectTransform)best.transform.parent;
        bestText = Ui.Label(best.transform, "", 15, Color.white, TextAnchor.MiddleCenter, shadow: true);
        bestPill.gameObject.SetActive(false);

        // Coins: a blue pill with the coin as a tab on its left edge; hearts for extra lives under it.
        var coins = Ui.Pill(safeRect, TopRight, new Vector2(-50, -24), new Vector2(84, 34), Ui.Blue, Ui.BlueShade, 3);
        coinPill = (RectTransform)coins.transform.parent;
        coinText = Ui.Label(coins.transform, "0", 19, Color.white, TextAnchor.MiddleRight, shadow: true);
        coinText.rectTransform.offsetMax = new Vector2(-14, 0);
        coinIcon = Ui.Coin(safeRect, TopRight, new Vector2(-90, -22), 32).rectTransform;
        for (int i = 0; i < Tuning.MaxExtraLives; i++)
            heartIcons.Add(Ui.Heart(safeRect, TopRight, new Vector2(-22 - i * 24, -60), 24));

        // This run's stamps: a small stamp with a count, where each zone stamp flies to.
        var chip = Ui.Pill(safeRect, TopLeft, new Vector2(46, -24), new Vector2(72, 34), Ui.Cream, Ui.CreamShade, 3);
        stampChip = (RectTransform)chip.transform.parent;
        stampChipFace = Ui.Picture(stampChip, new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(30, 30), StampArt.Face(0), Ui.Blue);
        stampChipIcon = Ui.Picture(stampChipFace.rectTransform, Center, Vector2.zero, new Vector2(18, 18), StampArt.Icon(0, 0), Color.white);
        stampChipText = Ui.Label(chip.transform, "0", 18, Ui.Brown, TextAnchor.MiddleRight);
        stampChipText.rectTransform.offsetMax = new Vector2(-14, 0);

        // The planet's effect as a round chip with its icon; tap it to read the tip again.
        planetChip = MakeRoundButton(safeRect, TopLeft, new Vector2(30, -66), 40, Ui.Blue, Ui.BlueShade, null, Color.white,
            () => ShowTip(EffectTip(CurrentEffect), 4f));
        planetChip.Label.gameObject.SetActive(false);
    }

    void BuildStaminaBar()
    {
        staminaBar = Ui.Rect(canvasRect, "Stamina", Center, Vector2.zero, new Vector2(54, 14));
        var back = Ui.Pill(staminaBar, Center, Vector2.zero, new Vector2(54, 14), Ui.Cream, Ui.CreamShade, 2);
        var fill = Ui.Pill(back.transform, new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(48, 8), Color.green, Color.clear, 0);
        var fillBox = (RectTransform)fill.transform.parent;
        fillBox.pivot = new Vector2(0, 0.5f);
        fillBox.anchoredPosition = new Vector2(3, 0);
        staminaFill = fill;
        staminaBar.gameObject.SetActive(false);
    }

    // MARK: - Buttons

    HudButton MakeButton(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color face, Color shade, Color text,
                         System.Action action, bool withPrice = false, Sprite icon = null)
    {
        var faceImage = Ui.Pill(parent, anchor, position, size, face, shade, 4);
        var rect = (RectTransform)faceImage.transform.parent;
        var label = Ui.Label(faceImage.transform, "", 18, text);
        var button = new HudButton
        {
            Rect = rect,
            Face = faceImage,
            Lip = rect.GetChild(0).GetComponent<Image>(),
            Label = label,
            Action = action,
        };
        if (icon != null)
        {
            // An icon on the left, the label next to it.
            float s = size.y * 0.5f;
            button.Icon = Ui.Picture(faceImage.transform, new Vector2(0, 0.5f), new Vector2(size.y * 0.55f, 0), new Vector2(s, s), icon, text);
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(size.y * 0.55f + s * 0.75f, 0);
        }
        if (withPrice)
        {
            // A small gold price tag with a coin on the right side of the button.
            var tag = Ui.Pill(faceImage.transform, new Vector2(1, 0.5f), new Vector2(-48, 0), new Vector2(80, size.y - 16), Ui.Gold, Ui.GoldShade, 2);
            button.Price = Ui.Label(tag.transform, "", 17, Color.white, TextAnchor.MiddleRight, shadow: true);
            button.Price.rectTransform.offsetMax = new Vector2(-10, 0);
            Ui.Coin(tag.transform.parent, new Vector2(0, 0.5f), new Vector2(14, 0), 24);
        }
        buttons.Add(button);
        return button;
    }

    /// A round button with an icon and an optional caption underneath.
    HudButton MakeRoundButton(Transform parent, Vector2 anchor, Vector2 position, float size, Color face, Color shade,
                              Sprite icon, Color iconColor, System.Action action)
    {
        var rect = Ui.Rect(parent, "RoundButton", anchor, position, new Vector2(size, size));
        var lip = Ui.Picture(rect, Center, new Vector2(0, -4), new Vector2(size, size), Ui.CircleSprite, shade);
        var faceImage = Ui.Picture(rect, Center, Vector2.zero, new Vector2(size, size), Ui.CircleSprite, face);
        var iconImage = Ui.Picture(faceImage.rectTransform, Center, Vector2.zero, new Vector2(size, size) * 0.56f, icon, iconColor);
        var captionRect = Ui.Rect(rect, "Caption", Center, new Vector2(0, -size / 2 - 16), new Vector2(160, 22));
        var label = Ui.Label(captionRect, "", 15, Ui.Brown);
        var button = new HudButton { Rect = rect, Face = faceImage, Lip = lip, Icon = iconImage, Label = label, Action = action };
        buttons.Add(button);
        return button;
    }

    /// The button under a finger, or null. An open screen covers everything behind it, and a button in a
    /// scrolling list only counts where the list shows it.
    HudButton ButtonAt(Vector2 screenPosition)
    {
        foreach (var button in buttons)
            if (CanTap(button, screenPosition)) return button;
        return null;
    }

    bool CanTap(HudButton button, Vector2 screenPosition)
    {
        if (!button.Visible) return false;
        var modal = OpenModal;
        if (modal != null && !button.Rect.IsChildOf(modal)) return false;
        if (button.Clip != null && !RectTransformUtility.RectangleContainsScreenPoint(button.Clip, screenPosition, null)) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(button.Rect, screenPosition, null);
    }

    /// Touch-down on a button only presses it visually; it fires when that finger lifts still on it.
    void PressButton(HudButton button) => StartCoroutine(Bump(button.Rect, -0.08f));

    void FireButton(HudButton button)
    {
        Haptics.Tick(0.4f);
        button.Action();
    }

    // MARK: - Updating

    int shownMeters = -1, shownLeft = -1, shownNextZone = -1;   // what the HUD texts say now

    void UpdateHUD()
    {
        float y = phase switch
        {
            Phase.Launching => launchRocket != null ? launchRocket.transform.position.y : ClimberY,
            Phase.Reviving => taxiRocket != null ? taxiRocket.transform.position.y : ClimberY,
            _ => ClimberY,
        };
        int meters = Mathf.Max(0, Mathf.FloorToInt(y));
        if (meters != shownMeters)
        {
            shownMeters = meters;
            heightText.text = $"{meters}<size=21><color=#A48F6C> m</color></size>";
        }

        bool climbing = phase is Phase.Playing or Phase.Launching or Phase.Reviving or Phase.Falling;
        heightCard.gameObject.SetActive(climbing);
        var next = NextCheckpoint();
        bool showNext = next != null && phase == Phase.Playing;
        nextBubble.gameObject.SetActive(showNext);
        if (showNext)
        {
            int left = Mathf.CeilToInt(next.Y - y);
            if (left != shownLeft || next.Zone != shownNextZone)
            {
                shownLeft = left;
                shownNextZone = next.Zone;
                string target = next.IsLaunchPad ? "the ROCKET" : ThemeFor(next.Zone).Name;
                nextText.text = $"{left} m to {target}";
            }
        }
        bestPill.gameObject.SetActive(climbing && Time.time < bestPillUntil);

        stampChip.gameObject.SetActive(climbing && OpenModal == null);
        var effect = CurrentEffect;
        bool showPlanet = effect != PlanetEffect.None && phase is Phase.Playing or Phase.Falling;
        planetChip.Rect.gameObject.SetActive(showPlanet);
        if (showPlanet)
        {
            planetChip.Face.color = Voxel.Hex(ThemeFor(ZoneIndex(anchorY + 0.01f)).Stamp);
            planetChip.Face.sprite = StampArt.Face(EffectPage(effect));
            planetChip.Lip.sprite = planetChip.Face.sprite;
            planetChip.Icon.sprite = EffectIcon(effect);
            planetChip.Icon.enabled = planetChip.Icon.sprite != null;
        }

        int lives = Progress.ExtraLives;
        for (int i = 0; i < heartIcons.Count; i++) heartIcons[i].gameObject.SetActive(i < lives && OpenModal == null);

        UpdateTutorial();
        UpdateCloset();
        UpdateBoard();
        CheckAchievements();
        UpdateBestFlag();
        UpdateScreens();
        UpdateHints();
        UpdateStaminaBar();
    }

    /// The stamp book page (and stamp shape) of a planet type.
    static int EffectPage(PlanetEffect effect) => Mathf.Max(0, System.Array.IndexOf(BookPages, effect));

    /// An icon for a planet effect, borrowed from its stamps: flame, snowflake, feather, anvil.
    static Sprite EffectIcon(PlanetEffect effect) => effect switch
    {
        PlanetEffect.Hot => StampArt.Icon(1, 2),
        PlanetEffect.Ice => StampArt.Icon(2, 0),
        PlanetEffect.LowGravity => StampArt.Icon(3, 1),
        PlanetEffect.HeavyGravity => StampArt.Icon(4, 0),
        PlanetEffect.Boss => StampArt.Icon(5, 1),
        _ => null,
    };

    void RefreshShop()
    {
        shownCoins = Progress.Coins - coinsInFlight;
        coinText.text = shownCoins.ToString("N0");
        RefreshMenus();
    }

    void UpdateStaminaBar()
    {
        bool visible = phase == Phase.Playing && (stamina < 0.995f || isExhausted);
        staminaBar.gameObject.SetActive(visible);
        if (!visible) return;

        var screen = cam.WorldToScreenPoint(climber.transform.position + Vector3.up * 1.7f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
        staminaBar.anchoredPosition = local;
        ((RectTransform)staminaFill.transform.parent).localScale = new Vector3(Mathf.Max(0.001f, stamina), 1, 1);
        staminaFill.color = stamina > 0.5f ? Voxel.Hex(0x6CD94F) : stamina > 0.25f ? Voxel.Hex(0xFFC83D) : Voxel.Hex(0xFF4D4D);
        bool blink = (isExhausted || stamina < 0.25f) && Mathf.Sin(Time.time * 18f) < 0;
        staminaFill.enabled = !blink;
        OnStaminaBarShown();
    }

    /// Where a world position shows up on the canvas.
    Vector2 CanvasPoint(Vector3 world)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, cam.WorldToScreenPoint(world), null, out var local);
        return local;
    }

    Vector2 CanvasPointOf(RectTransform rect)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(null, rect.position), null, out var local);
        return local;
    }

    // MARK: - Coins fly to the counter

    void FlyCoin(Vector3 worldPosition)
    {
        coinsInFlight++;
        StartCoroutine(CoinFlight(CanvasPoint(worldPosition)));
    }

    IEnumerator CoinFlight(Vector2 from)
    {
        var coin = Ui.Coin(canvasRect, Center, from, 28).rectTransform;
        Vector2 to = CanvasPointOf(coinIcon);
        // An arc: up and over toward the counter.
        Vector2 control = Vector2.Lerp(from, to, 0.5f) + new Vector2(-40, 60);
        const float duration = 0.38f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float u = t / duration;
            u = u * u * (3 - 2 * u);
            coin.anchoredPosition = Vector2.Lerp(Vector2.Lerp(from, control, u), Vector2.Lerp(control, to, u), u);
            coin.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, u);
            yield return null;
        }
        Destroy(coin.gameObject);
        coinsInFlight = Mathf.Max(0, coinsInFlight - 1);
        shownCoins++;
        coinText.text = shownCoins.ToString("N0");
        StartCoroutine(Bump(coinPill, 0.12f));
    }

    // MARK: - Little animations

    IEnumerator Bump(RectTransform rect, float amount)
    {
        for (float t = 0; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            if (rect == null) yield break;   // destroyed meanwhile (a closet grid rebuilt, a page turned)
            float k = t < 0.06f ? t / 0.06f : 1 - (t - 0.06f) / 0.12f;
            rect.localScale = Vector3.one * (1 + amount * k);
            yield return null;
        }
        if (rect != null) rect.localScale = Vector3.one;
    }

    /// Springy entrance: grows from `from` to full size with a small overshoot.
    IEnumerator Pop(RectTransform rect, float from)
    {
        for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
        {
            if (rect == null) yield break;
            float u = t / 0.22f;
            float overshoot = 1 + Mathf.Sin(u * Mathf.PI) * 0.08f;
            rect.localScale = Vector3.one * Mathf.Lerp(from, 1f, u) * overshoot;
            yield return null;
        }
        if (rect != null) rect.localScale = Vector3.one;
    }

    /// A quick side-to-side wiggle, for things that are new.
    IEnumerator Wiggle(RectTransform rect, float delay = 0)
    {
        if (delay > 0) yield return new WaitForSecondsRealtime(delay);
        if (rect == null) yield break;   // gone already, e.g. the stamp book turned the page
        var start = rect.localRotation;
        for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            if (rect == null) yield break;
            rect.localRotation = start * Quaternion.Euler(0, 0, Mathf.Sin(t * 40f) * 12f * (1 - t / 0.5f));
            yield return null;
        }
        if (rect != null) rect.localRotation = start;
    }
}
