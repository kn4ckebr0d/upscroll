using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.UI;

/// The stamp book: one page per planet type, one stamp per zone. You get a stamp the first time
/// you reach a zone (on that kind of planet), and the book counts how often you've been back.
/// Stamps you haven't looked at yet get a badge on the title screen and a wiggle in the book.
/// Swipe to turn pages; past the first and last page, the page stretches like a rubber band.
public partial class ScrollyGame
{
    static readonly PlanetEffect[] BookPages =
        { PlanetEffect.None, PlanetEffect.Hot, PlanetEffect.Ice, PlanetEffect.LowGravity, PlanetEffect.HeavyGravity, PlanetEffect.Boss };

    /// Stamps on a page: one per zone, but a boss planet has just two (the gate and the storm).
    static int PageSize(PlanetEffect effect) => effect == PlanetEffect.Boss ? 2 : ZonesPerPlanet;

    static int StampTotal => BookPages.Sum(PageSize);

    GameObject bookPanel;
    RectTransform bookCard, bookPage;
    Text bookCount;
    readonly List<HudButton> bookTabs = new();
    HudButton bookClose;
    int bookPageIndex;
    int bookDragTouch = -1;
    bool bookTurning;
    const float BookPageWidth = 340;
    readonly HashSet<int> runNewStamps = new();   // positions in runStamps that were new to the book

    string StampId(int zone) => StampId(EffectFor(zone / ZonesPerPlanet), zone % ZonesPerPlanet);

    static string StampId(PlanetEffect effect, int index) => $"{effect}:{index}";

    /// Which book page (and stamp style) a zone's planet type belongs to.
    int PageOf(int zone) => Mathf.Max(0, System.Array.IndexOf(BookPages, EffectFor(zone / ZonesPerPlanet)));

    static string PageTitle(PlanetEffect effect) => effect == PlanetEffect.None ? "EARTH" : EffectTitle(effect);

    static IEnumerable<string> AllStampIds =>
        BookPages.SelectMany(effect => Enumerable.Range(0, PageSize(effect)).Select(i => StampId(effect, i)));

    static int StampsCollected => AllStampIds.Count(id => Progress.StampCount(id) > 0);

    static int UnseenStamps => AllStampIds.Count(id => Progress.StampCount(id) > 0 && !Progress.StampSeen(id));

    /// What a planet type does, each with an icon, shown on its page.
    static (Sprite icon, string text)[] PlanetFacts(PlanetEffect effect) => effect switch
    {
        PlanetEffect.Hot => new[] { (EffectIcon(effect), "Stamina drains fast"), (Ui.Icon("drop"), "Water drops cool you down") },
        PlanetEffect.Ice => new[] { (EffectIcon(effect), "You slowly slip while holding on"), (Ui.Icon("cloud"), "It snows in the Blizzard") },
        PlanetEffect.LowGravity => new[] { (EffectIcon(effect), "Pulling is easy"), (Ui.Icon("cloud"), "Falls are slow and floaty") },
        PlanetEffect.HeavyGravity => new[] { (EffectIcon(effect), "Every pull is heavy"), (Ui.Icon("flag"), "A rest ledge halfway up each zone") },
        PlanetEffect.Boss => new[] { (EffectIcon(effect), "Meteors! Dodge the red marks"), (Ui.Icon("flag"), "One long climb with two rest ledges") },
        _ => new[] { (StampArt.Icon(0, 0), "A calm start, no effects"), (Ui.Icon("coin"), "Coins buy lives and continues") },
    };

    static bool PageVisited(PlanetEffect effect) =>
        Enumerable.Range(0, PageSize(effect)).Any(i => Progress.StampCount(StampId(effect, i)) > 0);

    /// A stamp in its planet type's shape, with the zone's icon.
    Image MakeStamp(Transform parent, Vector2 position, float size, int page, int index, Color color)
    {
        var face = Ui.Picture(parent, Center, position, new Vector2(size, size), StampArt.Face(page), color);
        Ui.Picture(face.rectTransform, Center, Vector2.zero, new Vector2(size, size), StampArt.Ring(page), new Color(1, 1, 1, 0.85f));
        Ui.Picture(face.rectTransform, Center, Vector2.zero, new Vector2(size, size) * 0.6f, StampArt.Icon(page, index), Color.white);
        return face;
    }

    void BuildStampBook()
    {
        var panel = MakeModal("StampBook");
        bookPanel = panel.gameObject;
        var card = Ui.Box(panel, Center, new Vector2(0, 50), new Vector2(350, 610), Ui.Cream, Ui.CreamShade, 32, 6);
        bookCard = (RectTransform)card.transform.parent;
        var t = card.transform;
        var title = Ui.Label(t, "Stamp Book", 32, Ui.Brown);
        title.rectTransform.anchoredPosition = new Vector2(0, 268);
        var countPill = Ui.Pill(t, Center, new Vector2(0, 230), new Vector2(110, 28), Ui.Blue, Ui.BlueShade, 3);
        bookCount = Ui.Label(countPill.transform, "", 16, Color.white, TextAnchor.MiddleCenter, shadow: true);

        // One tab per planet type, in that type's stamp shape.
        for (int i = 0; i < BookPages.Length; i++)
        {
            int page = i;
            var tab = MakeRoundButton(t, Center, new Vector2((i - (BookPages.Length - 1) / 2f) * 54, 176), 44, Ui.CreamShade, Ui.GreyShade, null, Color.white,
                () => TurnBookPage(page));
            tab.Face.sprite = tab.Lip.sprite = StampArt.Face(i);
            tab.Label.gameObject.SetActive(false);
            bookTabs.Add(tab);
        }
        // The page sits in a masked window so it can slide in and out.
        var window = Ui.Rect(t, "PageWindow", Center, new Vector2(0, -66), new Vector2(340, 450));
        window.gameObject.AddComponent<RectMask2D>();
        bookPage = Ui.Rect(window, "Page", Center, Vector2.zero, new Vector2(330, 450));

        bookClose = MakeButton(panel, Bottom, new Vector2(0, 72), new Vector2(240, 58), Ui.Blue, Ui.BlueShade, Color.white,
            () => ShowStampBook(false), icon: Ui.Icon("close"));
        bookClose.Label.text = "Close";
        bookClose.Label.fontSize = 22;
    }

    void ShowStampBook(bool open)
    {
        Synth.Instance.Click();
        bookPanel.SetActive(open);
        if (open)
        {
            // Open where something new is waiting.
            int page = System.Array.FindIndex(BookPages, effect =>
                Enumerable.Range(0, PageSize(effect)).Any(i => Progress.StampCount(StampId(effect, i)) > 0 && !Progress.StampSeen(StampId(effect, i))));
            bookDragTouch = -1;
            bookTurning = false;
            bookPage.anchoredPosition = Vector2.zero;
            ShowBookPage(Mathf.Max(0, page));
            StartCoroutine(Pop(bookCard, 0.85f));
        }
        else
        {
            Progress.MarkStampsSeen(AllStampIds.Where(id => Progress.StampCount(id) > 0));
            Progress.Save();
            RefreshMenus();
        }
    }

    // MARK: - Turning pages

    float BookDragDistance(Touch touch) => (touch.screenPosition.x - touch.startScreenPosition.x) / canvasRect.localScale.x;

    void DragBookPage(Touch touch)
    {
        if (bookTurning) return;
        if (bookDragTouch == -1) bookDragTouch = touch.touchId;
        if (touch.touchId != bookDragTouch) return;
        float dx = BookDragDistance(touch);
        // No page that way: the page stretches like a rubber band instead.
        bool atEdge = dx > 0 ? bookPageIndex == 0 : bookPageIndex == BookPages.Length - 1;
        if (atEdge) dx = Mathf.Sign(dx) * (1 - 1 / (Mathf.Abs(dx) * 0.55f / 120 + 1)) * 120;
        bookPage.anchoredPosition = new Vector2(dx, 0);
    }

    void EndBookDrag(Touch touch)
    {
        bookDragTouch = -1;
        if (bookTurning) return;
        float dx = BookDragDistance(touch);
        float seconds = (float)(Time.realtimeSinceStartupAsDouble - touch.startTime);
        bool flick = Mathf.Abs(dx) > 30 && Mathf.Abs(dx) / Mathf.Max(seconds, 0.01f) > 500;
        int direction = Mathf.Abs(dx) > BookPageWidth * 0.25f || flick ? (dx < 0 ? 1 : -1) : 0;
        int target = Mathf.Clamp(bookPageIndex + direction, 0, BookPages.Length - 1);
        if (target == bookPageIndex) StartCoroutine(SlidePage(bookPage.anchoredPosition.x, 0, 0.2f));
        else TurnBookPage(target);
    }

    void TurnBookPage(int page)
    {
        if (bookTurning || page == bookPageIndex) return;
        StartCoroutine(TurnPage(page));
    }

    IEnumerator TurnPage(int page)
    {
        bookTurning = true;
        int direction = page > bookPageIndex ? 1 : -1;
        Synth.Instance.Swoosh();
        Haptics.Tick(0.4f);
        yield return SlidePage(bookPage.anchoredPosition.x, -direction * BookPageWidth, 0.14f);
        ShowBookPage(page);
        yield return SlidePage(direction * BookPageWidth, 0, 0.22f);
        bookTurning = false;
    }

    IEnumerator SlidePage(float from, float to, float duration)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float u = 1 - (1 - t / duration) * (1 - t / duration);   // ease out
            bookPage.anchoredPosition = new Vector2(Mathf.Lerp(from, to, u), 0);
            yield return null;
        }
        bookPage.anchoredPosition = new Vector2(to, 0);
    }

    void ShowBookPage(int page)
    {
        bookPageIndex = page;
        foreach (Transform child in bookPage) Destroy(child.gameObject);
        bookCount.text = $"{StampsCollected} / {StampTotal}";

        // Tabs: colored once you've been to that planet type, the open one a size bigger.
        for (int i = 0; i < bookTabs.Count; i++)
        {
            var effect = BookPages[i];
            bool visited = PageVisited(effect);
            var tab = bookTabs[i];
            tab.Face.color = visited ? Voxel.Hex(PlanetTheme(effect, 0).Stamp) : Ui.CreamShade;
            tab.Icon.sprite = visited ? (effect == PlanetEffect.None ? StampArt.Icon(0, 0) : EffectIcon(effect)) : null;
            tab.Icon.enabled = tab.Icon.sprite != null;
            tab.Rect.localScale = Vector3.one * (i == page ? 1.18f : 0.92f);
        }

        var pageEffect = BookPages[page];
        bool pageVisited = PageVisited(pageEffect);
        var heading = Ui.Rect(bookPage, "Heading", Center, new Vector2(0, 198), new Vector2(330, 28));
        Ui.Label(heading, pageVisited ? PageTitle(pageEffect) : "? ? ?", 22, Ui.Brown);
        int stamps = PageSize(pageEffect);
        int found = Enumerable.Range(0, stamps).Count(i => Progress.StampCount(StampId(pageEffect, i)) > 0);
        var pagePill = Ui.Pill(bookPage, Center, new Vector2(0, 168), new Vector2(64, 24), Ui.Gold, Ui.GoldShade, 2);
        Ui.Label(pagePill.transform, $"{found}/{stamps}", 15, Color.white, TextAnchor.MiddleCenter, shadow: true);

        // What this planet does, each with a small icon (a mystery until you've been there).
        var facts = pageVisited ? PlanetFacts(pageEffect) : new (Sprite icon, string text)[] { (null, "Visit this planet to find out more") };
        var factColor = pageVisited ? Voxel.Hex(PlanetTheme(pageEffect, 0).Stamp) : Ui.CreamShade;
        for (int f = 0; f < facts.Length; f++)
        {
            float y = 130 - f * 32;
            var dot = Ui.Circle(bookPage, Center, new Vector2(-132, y), 28, factColor);
            if (facts[f].icon != null) Ui.Picture(dot.rectTransform, Center, Vector2.zero, new Vector2(18, 18), facts[f].icon, Color.white);
            else Ui.Label(dot.rectTransform, "?", 16, Color.white);
            var line = Ui.Rect(bookPage, "Fact", Center, new Vector2(26, y), new Vector2(284, 28));
            Ui.Label(line, facts[f].text, 15, Ui.Brown, TextAnchor.MiddleLeft);
        }

        int wiggles = 0;
        for (int i = 0; i < stamps; i++)
        {
            var position = new Vector2(i % 2 == 0 ? -78 : 78, i < 2 ? 8 : -122);
            string id = StampId(pageEffect, i);
            int count = Progress.StampCount(id);
            var theme = PlanetTheme(pageEffect, i);
            if (count > 0)
            {
                var stamp = MakeStamp(bookPage, position, 88, page, i, Voxel.Hex(theme.Stamp));
                stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, (Voxel.Hash(page, i, 4) - 0.5f) * 16);
                if (count > 1)
                {
                    // How often you've been here.
                    var tag = Ui.Pill(bookPage, Center, position + new Vector2(32, -32), new Vector2(44, 24), Ui.Cream, Ui.CreamShade, 2);
                    Ui.Label(tag.transform, $"x{count}", 14, Ui.Brown);
                }
                if (!Progress.StampSeen(id))
                {
                    var tag = Ui.Pill(bookPage, Center, position + new Vector2(-32, 34), new Vector2(50, 22), Ui.Gold, Ui.GoldShade, 2);
                    Ui.Label(tag.transform, "NEW", 13, Color.white, TextAnchor.MiddleCenter, shadow: true);
                    StartCoroutine(Wiggle(stamp.rectTransform, 0.25f + wiggles++ * 0.15f));
                }
            }
            else
            {
                // An empty spot in the stamp's shape, waiting for its stamp.
                Ui.Picture(bookPage, Center, position, new Vector2(88, 88), StampArt.Face(page), Ui.CreamShade);
                Ui.Picture(bookPage, Center, position, new Vector2(76, 76), StampArt.Face(page), Ui.Cream);
                var mark = Ui.Rect(bookPage, "Unknown", Center, position, new Vector2(88, 88));
                Ui.Label(mark, "?", 30, Ui.CreamShade);
            }
            var name = Ui.Rect(bookPage, "Name", Center, position + new Vector2(0, -60), new Vector2(150, 34));
            var label = Ui.Label(name, count > 0 ? theme.Name : "???", 15, count > 0 ? Ui.Brown : Ui.BrownSoft);
            label.lineSpacing = 0.9f;
        }
    }
}
