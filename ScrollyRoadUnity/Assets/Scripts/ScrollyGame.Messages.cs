using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Announcements during a run: zone stamps, banners and tips. They play one at a time from a queue,
/// so the screen never stacks them; a zone stamp cuts in line and cuts a running tip short.
/// Also the best-height flag on the wall.
public partial class ScrollyGame
{
    RectTransform stampRect, bannerCard, tipBubble, countdownBadge;
    GameObject stampNewRibbon;
    Image stampFace, stampRing, stampIcon;
    Text stampTop, stampName, stampHeight, bannerTop, bannerName, tipText, countdownText;
    Coroutine countdownRoutine, messageRunner;
    readonly List<IEnumerator> messageQueue = new();
    readonly List<string> queuedTips = new();
    bool tipShowing, cutTip;

    void BuildMessages()
    {
        // The zone stamp: a big stamp that thumps down when you reach a zone, then flies to the stamp chip.
        stampRect = Ui.Rect(safeRect, "Stamp", Top, new Vector2(0, -230), new Vector2(170, 170));
        stampFace = Ui.Picture(stampRect, Center, Vector2.zero, new Vector2(170, 170), StampArt.Face(0), Ui.Blue);
        stampRing = Ui.Picture(stampRect, Center, Vector2.zero, new Vector2(170, 170), StampArt.Ring(0), new Color(1, 1, 1, 0.85f));
        stampIcon = Ui.Picture(stampRect, Center, Vector2.zero, new Vector2(112, 112), StampArt.Icon(0, 0), new Color(1, 1, 1, 0.22f));
        stampTop = Ui.Label(stampRect, "", 14, Color.white);
        stampTop.rectTransform.anchoredPosition = new Vector2(0, 38);
        stampName = Ui.Label(stampRect, "", 24, Color.white, TextAnchor.MiddleCenter, shadow: true);
        stampName.lineSpacing = 0.85f;
        stampHeight = Ui.Label(stampRect, "", 15, Color.white);
        stampHeight.rectTransform.anchoredPosition = new Vector2(0, -42);
        var ribbon = Ui.Pill(stampRect, Center, new Vector2(0, -80), new Vector2(124, 30), Ui.Gold, Ui.GoldShade, 3);
        Ui.Label(ribbon.transform, "NEW STAMP!", 15, Ui.OnGold, TextAnchor.MiddleCenter);
        stampNewRibbon = ribbon.transform.parent.gameObject;
        stampNewRibbon.transform.localRotation = Quaternion.Euler(0, 0, -5);
        stampRect.gameObject.SetActive(false);

        // Other announcements: a cream card.
        var banner = Ui.Box(safeRect, Top, new Vector2(0, -200), new Vector2(280, 76), Ui.Cream, Ui.CreamShade, 22, 5);
        bannerCard = (RectTransform)banner.transform.parent;
        bannerTop = Ui.Label(banner.transform, "", 14, Ui.BrownSoft);
        bannerTop.rectTransform.anchoredPosition = new Vector2(0, 17);
        bannerName = Ui.Label(banner.transform, "", 26, Ui.Brown);
        bannerName.rectTransform.anchoredPosition = new Vector2(0, -8);
        bannerCard.gameObject.SetActive(false);

        // Tips: a cream bubble under the top bar, away from the climber.
        var tip = Ui.Box(safeRect, Top, new Vector2(0, -150), new Vector2(330, 62), Ui.Cream, Ui.CreamShade, 22, 4);
        tipBubble = (RectTransform)tip.transform.parent;
        tipText = Ui.Label(tip.transform, "", 15, Ui.Brown);
        tipText.lineSpacing = 1.1f;
        tipBubble.gameObject.SetActive(false);

        // Countdown before liftoff: a big number on a cream badge.
        var badge = Ui.Circle(safeRect, Center, new Vector2(0, 70), 130, Ui.Cream);
        countdownBadge = badge.rectTransform;
        countdownText = Ui.Label(countdownBadge, "", 64, Ui.Brown);
        countdownBadge.gameObject.SetActive(false);
    }

    // MARK: - The queue

    void Enqueue(IEnumerator message, bool first = false)
    {
        if (first) messageQueue.Insert(0, message);
        else messageQueue.Add(message);
        messageRunner ??= StartCoroutine(RunMessages());
    }

    IEnumerator RunMessages()
    {
        while (messageQueue.Count > 0)
        {
            var message = messageQueue[0];
            messageQueue.RemoveAt(0);
            yield return message;
        }
        messageRunner = null;
    }

    /// Banners, stamps, tips and the countdown are animated by coroutines; a reset stops those midway.
    void ClearTransientHud()
    {
        messageQueue.Clear();
        queuedTips.Clear();
        messageRunner = null;
        tipShowing = cutTip = false;
        stampRect.gameObject.SetActive(false);
        bannerCard.gameObject.SetActive(false);
        tipBubble.gameObject.SetActive(false);
        countdownBadge.gameObject.SetActive(false);
        foreach (var button in buttons) button.Rect.localScale = Vector3.one;
        coinPill.localScale = Vector3.one;
        coinsInFlight = 0;
        runStamps.Clear();
        runNewStamps.Clear();
        stampChipText.text = "0";
        stampChipFace.color = Ui.CreamShade;   // no stamps yet this run
        stampChipIcon.enabled = false;
        bestPillUntil = 0;
        ClearHints();
    }

    /// A full-screen card opened: close the tip and banner rather than show them through it.
    void HideMessages()
    {
        if (tipShowing) cutTip = true;
        tipBubble.gameObject.SetActive(false);
        bannerCard.gameObject.SetActive(false);
    }

    // MARK: - Tips

    void ShowTip(string text, float seconds = 3f)
    {
        // Don't line up the same tip twice, and drop the oldest when too many wait.
        if (queuedTips.Contains(text)) return;
        if (queuedTips.Count >= 2) return;
        queuedTips.Add(text);
        Enqueue(Tip(text, seconds));
    }

    IEnumerator Tip(string text, float seconds)
    {
        queuedTips.Remove(text);
        tipText.text = text;
        tipBubble.gameObject.SetActive(true);
        tipShowing = true;
        cutTip = false;
        yield return Pop(tipBubble, 0.8f);
        for (float t = 0; t < seconds && !cutTip; t += Time.deltaTime) yield return null;
        tipBubble.gameObject.SetActive(false);
        tipShowing = false;
    }

    // MARK: - Banners

    void ShowBanner(string top, string name) => Enqueue(Banner(top, name));

    IEnumerator Banner(string top, string name)
    {
        bannerTop.text = top;
        bannerName.text = name;
        bannerCard.gameObject.SetActive(true);
        yield return Pop(bannerCard, 0.6f);
        yield return new WaitForSeconds(1.5f);
        bannerCard.gameObject.SetActive(false);
    }

    // MARK: - Zone stamps

    /// Reached a zone: a stamp thumps down, and it's added to this run's stamps and the stamp book.
    void ShowZoneStamp(int zone, bool collect)
    {
        bool isNew = Progress.CollectStamp(StampId(zone));
        if (collect)
        {
            if (isNew) runNewStamps.Add(runStamps.Count);
            runStamps.Add(zone);
        }
        if (isNew)
        {
            Synth.Instance.Purchase();
            RefreshShop();
        }
        // The stamp goes first, and cuts a tip that's on screen short.
        if (tipShowing) cutTip = true;
        Enqueue(Stamp(zone, collect, isNew), first: true);
    }

    IEnumerator Stamp(int zone, bool collect, bool isNew)
    {
        var theme = ThemeFor(zone);
        int page = PageOf(zone);
        stampFace.color = Voxel.Hex(theme.Stamp);
        stampFace.sprite = StampArt.Face(page);
        stampRing.sprite = StampArt.Ring(page);
        stampIcon.sprite = StampArt.Icon(page, zone % ZonesPerPlanet);
        stampIcon.enabled = stampIcon.sprite != null;
        int planet = zone / ZonesPerPlanet;
        stampTop.text = planet == 0 ? $"ZONE {zone % ZonesPerPlanet + 1}" : $"PLANET {planet + 1} · {zone % ZonesPerPlanet + 1}";
        stampName.text = theme.Name.Replace(' ', '\n');
        stampHeight.text = $"{Mathf.FloorToInt(anchorY)} m";
        stampNewRibbon.SetActive(isNew);

        // Thump down with a tilt; a UI bounce instead of shaking the camera.
        var home = new Vector2(0, -230);
        stampRect.anchoredPosition = home;
        stampRect.gameObject.SetActive(true);
        var tilt = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
        for (float t = 0; t < 0.14f; t += Time.deltaTime)
        {
            float u = t / 0.14f;
            stampRect.localScale = Vector3.one * Mathf.Lerp(1.9f, 1f, u * u);
            stampRect.localRotation = Quaternion.Slerp(Quaternion.Euler(0, 0, -25), tilt, u);
            yield return null;
        }
        stampRect.localRotation = tilt;
        yield return Bump(stampRect, -0.06f);
        yield return new WaitForSeconds(isNew ? 0.9f : 0.5f);

        if (!collect)
        {
            // The starting zone isn't collected again: the stamp just shrinks away.
            for (float t = 0; t < 0.2f; t += Time.deltaTime)
            {
                stampRect.localScale = Vector3.one * (1 - t / 0.2f);
                yield return null;
            }
            stampRect.gameObject.SetActive(false);
            yield break;
        }

        // Fly into the stamp chip at the top left.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRect,
            RectTransformUtility.WorldToScreenPoint(null, stampChipFace.rectTransform.position), null, out var target);
        // anchoredPosition is measured from the top-center anchor.
        target -= new Vector2(0, safeRect.rect.height / 2);
        var from = stampRect.anchoredPosition;
        for (float t = 0; t < 0.32f; t += Time.deltaTime)
        {
            float u = t / 0.32f;
            u = u * u;
            stampRect.anchoredPosition = Vector2.Lerp(from, target, u);
            stampRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.18f, u);
            yield return null;
        }
        stampRect.gameObject.SetActive(false);
        stampRect.anchoredPosition = home;
        stampChipFace.color = Voxel.Hex(theme.Stamp);
        stampChipFace.sprite = StampArt.Face(page);
        stampChipIcon.sprite = StampArt.Icon(page, zone % ZonesPerPlanet);
        stampChipIcon.enabled = stampChipIcon.sprite != null;
        stampChipText.text = (runStamps.IndexOf(zone) + 1).ToString();
        Haptics.Tick(0.7f);
        yield return Bump(stampChip, 0.25f);
    }

    // MARK: - Countdown

    void ShowCountdown(string text, bool final)
    {
        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(Countdown(text));
        Synth.Instance.CountdownBeep(final);
        Haptics.Rumble(final ? 1f : 0.5f);
    }

    IEnumerator Countdown(string text)
    {
        countdownText.text = text;
        countdownText.fontSize = text.Length > 1 ? 40 : 64;
        countdownBadge.gameObject.SetActive(true);
        yield return Pop(countdownBadge, 1.6f);
        yield return new WaitForSeconds(0.35f);
        countdownBadge.gameObject.SetActive(false);
    }

    // MARK: - Best-height flag

    GameObject bestFlag;
    RectTransform bestTag;
    Text bestTagText;
    bool passedBest;

    void BuildBestTag()
    {
        var tag = Ui.Pill(canvasRect, Center, Vector2.zero, new Vector2(116, 26), Ui.Blue, Ui.BlueShade, 2);
        bestTag = (RectTransform)tag.transform.parent;
        bestTagText = Ui.Label(tag.transform, "", 14, Color.white, TextAnchor.MiddleCenter, shadow: true);
        bestTag.gameObject.SetActive(false);
    }

    /// A flag and a dashed line across the wall at your best height, so you see the record coming.
    void PlaceBestFlag()
    {
        passedBest = false;
        if (bestAtStart <= 0) return;
        bestFlag = new GameObject("Best flag");
        bestFlag.transform.SetParent(world, false);
        float y = bestAtStart;
        for (float x = -6.6f; x <= 6.6f; x += 0.8f)
            Voxel.Box(bestFlag.transform, new Vector3(0.45f, 0.09f, 0.04f), 0xFFFFFF, new Vector3(x, y, -0.08f), castShadows: false);
        Voxel.Box(bestFlag.transform, new Vector3(0.08f, 1.5f, 0.08f), 0xF2E6CF, new Vector3(-3.2f, y + 0.75f, -1.1f));
        Voxel.Box(bestFlag.transform, new Vector3(0.7f, 0.45f, 0.05f), 0x4C8FE8, new Vector3(-2.82f, y + 1.25f, -1.1f));
        bestTagText.text = $"BEST {bestAtStart} m";
    }

    void UpdateBestFlag()
    {
        bool show = bestFlag != null && phase is Phase.Playing or Phase.Falling && !passedBest;
        if (show)
        {
            var point = CanvasPoint(new Vector3(-1.6f, bestAtStart + 0.45f, -0.5f));
            show = Mathf.Abs(point.y) < canvasRect.rect.height / 2 - 60;
            bestTag.anchoredPosition = point;
        }
        bestTag.gameObject.SetActive(show);

        // Passing the record: a little celebration at the flag.
        if (!passedBest && bestAtStart > 0 && runBest > bestAtStart && phase == Phase.Playing)
        {
            passedBest = true;
            Synth.Instance.NewBest();
            Haptics.Success();
            if (bestFlag != null) Debris.Burst(new Vector3(-2.8f, bestAtStart + 1.2f, -1.2f), 0xFFD54F, 14, 3.5f);
            bestText.text = "NEW BEST!";
            bestPillUntil = Time.time + 2.5f;
            StartCoroutine(Pop(bestPill, 1.6f));
        }
    }
}
