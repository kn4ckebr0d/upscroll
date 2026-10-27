using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// The closet, full screen: a big live preview you can spin and zoom (or inspect full screen), standing
/// on your tag, and characters, hats, backpacks, charms and tags in tabs with a scrolling grid. Plus achievements that unlock exclusive
/// items, and the free daily gift. Everything for sale has a fixed coin price you see before buying;
/// the only surprise is the free gift. A small camera renders the preview climber, far away from the
/// world on a layer of its own.
public partial class ScrollyGame
{
    const int PreviewLayer = 31;

    class ClosetCard
    {
        public Cosmetic Item;
        public HudButton Button;
        public RawImage Thumb;
        public Image Coin, Lock;
    }

    enum Framing { Character, Hat, Backpack, Charm, Inspect }

    static readonly CosmeticKind[] ClosetTabs = { CosmeticKind.Character, CosmeticKind.Hat, CosmeticKind.Backpack, CosmeticKind.Charm, CosmeticKind.Tag };
    static readonly string[] ClosetTabIcons = { "tab_character", "tab_hat", "tab_backpack", "tab_charm", "tab_tag" };
    const float CardSize = 82, CardStep = 90;

    GameObject closetPanel, giftPanel;
    RectTransform closetCard, giftCard, closetViewport, closetContent;
    Camera previewCam;
    RenderTexture previewTexture, thumbTexture;
    Transform previewRoot, previewItem;
    Climber previewClimber;
    RawImage previewImage;
    Text closetItemName, closetCoinText, closetHint;
    HudButton closetButton, giftButton, closetAction, closetClose, giftAction, closetInspectButton;
    readonly List<HudButton> closetTabs = new();
    readonly List<ClosetCard> closetCards = new();
    readonly Dictionary<string, Texture2D> thumbs = new();
    RectTransform giftBadge;
    CosmeticKind closetTab;
    Cosmetic closetSelected;
    float achievementCheckAt;

    ScrollArea closetScroll;   // the grid, with a rubber band at the ends

    // Spinning and zooming the preview.
    float inspectYaw, inspectYawVelocity, inspectZoom = 1, pinchStartDistance, pinchStartZoom;

    // Inspecting: the item sheet slides away and the character gets the whole screen. 0..1 while it moves.
    bool closetInspecting;
    float closetInspectT;

    // The preview texture is 2:3; the stage shows its middle square, inspecting shows all of it.
    // previewWindow: the part of its height on screen; previewSpan: meters of character shown in that part.
    float previewWindow = 2 / 3f, previewSpan = 2.3f;

    // The tag behind the character: a gradient, a tiled pattern and maybe rays, inside a rounded mask.
    RectTransform closetTag, closetTagFrame, closetHeader, closetNamePill;
    RawImage closetTagGradient, closetTagPattern;
    Image closetTagRays;
    TagStyle closetTagStyle;

    Cosmetic EquippedCharacter => Cosmetics.Character(Progress.Character);
    Cosmetic EquippedHat => Cosmetics.Hat(Progress.Hat);
    Cosmetic EquippedBackpack => Cosmetics.Backpack(Progress.Backpack);
    Cosmetic EquippedCharm => Cosmetics.Charm(Progress.Charm);
    Cosmetic EquippedTag => Cosmetics.Tag(Progress.Tag);

    IEnumerable<Cosmetic> AllItems => ClosetTabs.SelectMany(Cosmetics.OfKind);

    void DressClimber() => climber.Build(EquippedCharacter, EquippedHat, EquippedBackpack, EquippedCharm);

    // MARK: - Owning things

    int BestEver => Mathf.Max(Progress.Best, Mathf.FloorToInt(runBest));

    bool AchievementDone(string id) => id switch
    {
        "stamps10" => StampsCollected >= 10,
        "height500" => BestEver >= 500,
        "height1000" => BestEver >= 1000,
        "page" => BookPages.Any(e => Enumerable.Range(0, PageSize(e)).All(i => Progress.StampCount(StampId(e, i)) > 0)),
        "boss" => Progress.BossesBeaten > 0,
        _ => false,
    };

    bool Owns(Cosmetic item) =>
        item.Free || Progress.Owns(item.Id) || (item.Achievement != null && AchievementDone(item.Achievement));

    bool Wearing(Cosmetic item) => item.Kind switch
    {
        CosmeticKind.Character => EquippedCharacter.Id == item.Id,
        CosmeticKind.Hat => EquippedHat.Id == item.Id,
        CosmeticKind.Backpack => EquippedBackpack.Id == item.Id,
        CosmeticKind.Charm => EquippedCharm.Id == item.Id,
        _ => EquippedTag.Id == item.Id,
    };

    static readonly uint[] RarityColors = { 0x6FA35A, 0x4C8FE8, 0x9B59D0, 0xF28C28, 0xF06292 };

    /// A newly earned achievement item gets an "UNLOCKED!" banner (once).
    void CheckAchievements()
    {
        if (Time.time < achievementCheckAt) return;
        achievementCheckAt = Time.time + 0.5f;
        foreach (var item in AllItems)
        {
            if (item.Achievement == null || !AchievementDone(item.Achievement)) continue;
            if (Progress.HintCount("ach_" + item.Achievement) > 0) continue;
            Progress.CountHint("ach_" + item.Achievement);
            Progress.Save();
            if (phase is Phase.Playing or Phase.Launching)
            {
                ShowBanner("UNLOCKED!", item.Name.ToUpper());
                Synth.Instance.NewBest();
                Debris.Burst(climber.transform.position + Vector3.up * 1.4f, 0xFFD54F, 18, 3.5f);
            }
        }
    }

    // MARK: - Preview and thumbnails

    void BuildPreview()
    {
        cam.cullingMask &= ~(1 << PreviewLayer);
        previewRoot = new GameObject("Closet preview").transform;
        previewRoot.position = new Vector3(500, 500, 0);
        previewClimber = new GameObject("Preview climber").AddComponent<Climber>();
        previewClimber.transform.SetParent(previewRoot, false);
        previewItem = new GameObject("Preview item").transform;
        previewItem.SetParent(previewRoot, false);

        previewCam = new GameObject("Preview camera").AddComponent<Camera>();
        previewCam.orthographic = true;
        previewCam.clearFlags = CameraClearFlags.SolidColor;
        previewCam.backgroundColor = Color.clear;   // transparent: models sit right on the cards
        previewCam.cullingMask = 1 << PreviewLayer;
        previewCam.nearClipPlane = 0.1f;
        previewCam.farClipPlane = 30f;
        previewCam.enabled = false;
        previewTexture = new RenderTexture(720, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        thumbTexture = new RenderTexture(192, 192, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
    }

    /// Points the preview camera at a whole character, or at a single item on its own.
    void AimPreview(Framing framing)
    {
        var (center, size) = framing switch
        {
            Framing.Hat => (new Vector3(0, 0.2f, 0), 0.5f),
            Framing.Backpack => (new Vector3(0, 0.05f, 0.14f), 0.44f),
            Framing.Charm => (new Vector3(0, -0.15f, 0), 0.2f),
            Framing.Inspect => (new Vector3(0, 0.78f - (inspectZoom - 1) * 0.12f, 0), previewSpan / 2 / previewWindow / inspectZoom),
            _ => (new Vector3(0, 0.88f, 0), 1.05f),
        };
        var rotation = Quaternion.Euler(12, 0, 0);
        previewCam.transform.SetPositionAndRotation(previewRoot.position + center - rotation * Vector3.forward * 10, rotation);
        previewCam.orthographicSize = size;
    }

    static Framing FramingFor(CosmeticKind kind) => kind switch
    {
        CosmeticKind.Hat => Framing.Hat,
        CosmeticKind.Backpack => Framing.Backpack,
        CosmeticKind.Charm => Framing.Charm,
        _ => Framing.Character,
    };

    /// Dresses the preview climber.
    void PoseClimber(Cosmetic character, Cosmetic hat, Cosmetic backpack, Cosmetic charm)
    {
        if (giftModel != null) giftModel.gameObject.SetActive(false);
        previewItem.gameObject.SetActive(false);
        previewClimber.gameObject.SetActive(true);
        previewClimber.Build(character, hat, backpack, charm);
        SetPreviewLayer();
    }

    /// Shows one hat, backpack or charm on its own (for its thumbnail).
    void PoseItem(Cosmetic item)
    {
        if (giftModel != null) giftModel.gameObject.SetActive(false);
        previewClimber.gameObject.SetActive(false);
        for (int i = previewItem.childCount - 1; i >= 0; i--) DestroyImmediate(previewItem.GetChild(i).gameObject);   // gone before the next render
        previewItem.gameObject.SetActive(true);
        if (item.Build != null)
        {
            var holder = new GameObject("item").transform;
            holder.SetParent(previewItem, false);
            item.Build(holder);
        }
        SetPreviewLayer();
    }

    void SetPreviewLayer()
    {
        foreach (var t in previewRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer;
    }

    Texture2D Thumbnail(Cosmetic item)
    {
        string key = item.Kind + ":" + item.Id;
        if (thumbs.TryGetValue(key, out var cached)) return cached;
        if (item.Kind == CosmeticKind.Tag) return thumbs[key] = TagArt.Thumbnail(item.Tag, 192, 66);
        var none = Cosmetics.Hat("none");
        if (item.Kind == CosmeticKind.Character) PoseClimber(item, none, null, null);
        else PoseItem(item);
        // Backpacks and charms face away from the wearer, so turn them around to show their front.
        float yaw = item.Kind is CosmeticKind.Backpack or CosmeticKind.Charm ? 204 : 24;
        previewClimber.transform.localRotation = previewItem.localRotation = Quaternion.Euler(0, yaw, 0);
        AimPreview(FramingFor(item.Kind));
        if (item.Kind != CosmeticKind.Character) FitPreview(previewItem, previewCam.orthographicSize);   // big hats and wings: zoom out to fit
        previewCam.targetTexture = thumbTexture;
        previewCam.aspect = 1;
        previewCam.Render();
        var texture = new Texture2D(thumbTexture.width, thumbTexture.height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        RenderTexture.active = thumbTexture;
        texture.ReadPixels(new Rect(0, 0, thumbTexture.width, thumbTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;
        thumbs[key] = texture;
        return texture;
    }

    // MARK: - Building the screens

    void BuildCloset()
    {
        var panel = MakeModal("Closet");
        closetPanel = panel.gameObject;
        // Everything sits in the safe area: the header on top, a big preview stage, and the item sheet below.
        var safe = Ui.SafeArea(panel);
        closetCard = safe;

        // The stage: your tag in a cream frame, behind everything else. Inspecting, it fills the screen.
        closetTagFrame = Ui.Rect(safe, "Tag frame", Center, Vector2.zero, Vector2.zero);
        Ui.Rounded(closetTagFrame, Ui.Cream, 34);
        closetTag = Ui.Rect(safe, "Tag", Center, Vector2.zero, Vector2.zero);
        Ui.Rounded(closetTag, Color.white, 28);
        closetTag.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        foreach (var rect in new[] { closetTagFrame, closetTag }) Ui.Stretch(rect);
        closetTagGradient = Ui.Rect(closetTag, "Gradient", Center, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
        closetTagRays = Ui.Picture(closetTag, Center, Vector2.zero, new Vector2(800, 800), RaysSprite, Color.white);
        closetTagPattern = Ui.Rect(closetTag, "Pattern", Center, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
        foreach (var layer in new[] { closetTagGradient, closetTagPattern })
        {
            Ui.Stretch(layer.rectTransform);
            layer.raycastTarget = false;
        }

        closetHeader = Ui.Rect(safe, "Header", Center, Vector2.zero, Vector2.zero);
        Ui.Stretch(closetHeader);
        closetHeader.gameObject.AddComponent<CanvasGroup>();
        closetClose = MakeRoundButton(closetHeader, TopLeft, new Vector2(34, -32), 46, Ui.Cream, Ui.CreamShade, Ui.Icon("close"), Ui.Brown, () => ShowCloset(false));
        closetClose.Label.gameObject.SetActive(false);
        Ui.Label(Ui.Rect(closetHeader, "Title", Top, new Vector2(0, -32), new Vector2(200, 40)), "Closet", 30, Ui.Brown);
        var coins = Ui.Pill(closetHeader, TopRight, new Vector2(-54, -32), new Vector2(92, 36), Ui.Blue, Ui.BlueShade, 3);
        closetCoinText = Ui.Label(coins.transform, "0", 18, Color.white, TextAnchor.MiddleRight, shadow: true);
        closetCoinText.rectTransform.offsetMax = new Vector2(-12, 0);
        Ui.Coin(coins.transform.parent, new Vector2(0, 0.5f), new Vector2(14, 0), 30);

        // The item sheet: a big cream panel along the bottom, with tabs, a scrolling grid and the button.
        // Its height and the stage above it are fitted to the screen in LayoutCloset.
        var sheet = Ui.Box(safe, Bottom, Vector2.zero, new Vector2(0, 460), Ui.Cream, Ui.CreamShade, 34, 0);
        closetSheet = (RectTransform)sheet.transform.parent;
        closetSheet.anchorMin = new Vector2(0, 0);
        closetSheet.anchorMax = new Vector2(1, 0);
        var t = sheet.transform;
        for (int i = 0; i < ClosetTabs.Length; i++)
        {
            var kind = ClosetTabs[i];
            var tab = MakeButton(t, Top, new Vector2((i - (ClosetTabs.Length - 1) / 2f) * 68, -36), new Vector2(60, 46), Ui.Blue, Ui.BlueShade, Color.white, () => ShowClosetTab(kind));
            tab.Icon = Ui.Picture(tab.Face.transform, Center, Vector2.zero, new Vector2(28, 28), Ui.Icon(ClosetTabIcons[i]), Color.white);
            closetTabs.Add(tab);
        }
        closetViewport = Ui.Rect(t, "Grid", Top, Vector2.zero, new Vector2(GridWidth, 260));
        closetViewport.pivot = new Vector2(0.5f, 1);
        closetViewport.gameObject.AddComponent<RectMask2D>();
        closetContent = Ui.Rect(closetViewport, "Items", Top, Vector2.zero, new Vector2(GridWidth, 0));
        closetContent.pivot = new Vector2(0.5f, 1);
        closetScroll = new ScrollArea(closetViewport, closetContent);
        closetHint = Ui.Label(Ui.Rect(t, "Hint", Bottom, new Vector2(0, 112), new Vector2(340, 22)), "", 14, Ui.BrownSoft);
        closetAction = MakeButton(safe, Bottom, new Vector2(0, 50), new Vector2(300, 60), Ui.Blue, Ui.BlueShade, Color.white, ClosetAction, withPrice: true);
        closetAction.Label.fontSize = 22;

        // On the stage: a soft shadow, the live 3D preview you can spin and zoom, and the item's name.
        closetShadow = Ui.Picture(safe, Top, Vector2.zero, new Vector2(170, 34), Ui.CircleSprite, new Color(0.25f, 0.35f, 0.55f, 0.22f)).rectTransform;
        previewImage = Ui.Rect(safe, "Preview", Top, Vector2.zero, new Vector2(300, 300)).gameObject.AddComponent<RawImage>();
        previewImage.texture = previewTexture;
        previewImage.raycastTarget = false;
        closetSpinHint = Ui.Rect(safe, "Spin", Top, Vector2.zero, new Vector2(300, 20));
        Ui.Label(closetSpinHint, "Drag to spin · pinch to zoom", 13, Color.white, shadow: true);
        var namePill = Ui.Pill(safe, Top, Vector2.zero, new Vector2(200, 34), Ui.Cream, Ui.CreamShade, 3);
        closetNamePill = (RectTransform)namePill.transform.parent;
        closetNamePill.gameObject.AddComponent<CanvasGroup>();
        closetItemName = Ui.Label(namePill.transform, "", 20, Ui.Brown);

        // A round button to see your character up close, full screen.
        closetInspectButton = MakeRoundButton(safe, Top, Vector2.zero, 46, Ui.Cream, Ui.CreamShade, Ui.Icon("inspect"), Ui.Brown, ToggleInspect);
        closetInspectButton.Label.gameObject.SetActive(false);
    }

    RectTransform closetSheet, closetShadow, closetSpinHint;

    /// Fits the closet to the screen: the item sheet takes about half the height, the stage fills the
    /// space between the header and the sheet, and the preview shrinks on shorter screens. Inspecting,
    /// the sheet slides off the bottom and the tag and the preview grow to fill the screen.
    void LayoutCloset()
    {
        closetInspectT = Mathf.MoveTowards(closetInspectT, closetInspecting ? 1 : 0, Time.unscaledDeltaTime / 0.35f);
        float k = Mathf.SmoothStep(0, 1, closetInspectT);
        float width = closetCard.rect.width, height = closetCard.rect.height;
        const float header = 64, bottomRoom = 130, tabsRoom = 70;
        float sheet = Mathf.Clamp(height * 0.5f, 340, 440);
        float slide = k * (sheet + 90);
        closetSheet.sizeDelta = new Vector2(-16, sheet + 30);
        closetSheet.anchoredPosition = new Vector2(0, (sheet + 30) / 2 - 30 - slide);
        closetAction.Rect.anchoredPosition = new Vector2(0, 50 - slide);
        float grid = sheet + 30 - tabsRoom - bottomRoom;
        closetViewport.sizeDelta = new Vector2(GridWidth, grid);
        closetViewport.anchoredPosition = new Vector2(0, -tabsRoom);
        var headerGroup = closetHeader.GetComponent<CanvasGroup>();
        headerGroup.alpha = 1 - k;
        closetHeader.gameObject.SetActive(k < 0.98f);
        closetNamePill.GetComponent<CanvasGroup>().alpha = 1 - k;

        // The stage: the preview (its middle square), then the spin hint and the item's name, above the sheet.
        float room = height - sheet - header;
        float square = Mathf.Clamp(room - 62, 150, 300);
        float top = header + Mathf.Max(0, (room - square - 62) / 2);
        // Inspecting: all of the 2:3 preview, as big as fits, with room for the hint at the bottom.
        float tall = Mathf.Min(width, (height - 110) / 1.5f);
        var size = Vector2.Lerp(new Vector2(square, square), new Vector2(tall, tall * 1.5f), k);
        float center = Mathf.Lerp(top + square / 2, 16 + (height - 110) / 2, k);
        previewImage.rectTransform.sizeDelta = size;
        previewImage.rectTransform.anchoredPosition = new Vector2(0, -center);
        previewWindow = 2 / 3f * size.y / size.x;
        previewImage.uvRect = new Rect(0, (1 - previewWindow) / 2, 1, previewWindow);
        previewSpan = Mathf.Lerp(2.3f, 2.6f, k);

        // The shadow under the feet (about 0.8 m below the middle of the view), fading as you zoom in.
        float metersToPoints = size.y / previewSpan;
        closetShadow.sizeDelta = new Vector2(1.27f, 0.25f) * metersToPoints;
        closetShadow.anchoredPosition = new Vector2(0, -(center + 0.8f * metersToPoints));
        closetShadow.GetComponent<Image>().color = new Color(0.25f, 0.35f, 0.55f, 0.22f * Mathf.Clamp01(1 - (inspectZoom - 1) * 2));
        closetSpinHint.anchoredPosition = new Vector2(0, -Mathf.Lerp(top + square + 10, height - 34, k));
        closetNamePill.anchoredPosition = new Vector2(0, -(top + square + 38));
        closetInspectButton.Rect.anchoredPosition = new Vector2(width / 2 - Mathf.Lerp(42, 40, k), -Mathf.Lerp(header + 30, 36, k));

        // The tag: a framed card from the header down under the sheet; inspecting, past every edge of the screen.
        var full = ((RectTransform)closetPanel.transform).rect.size;
        var below = Vector2.Scale(closetCard.anchorMin, full) + new Vector2(40, 40);
        var above = Vector2.Scale(Vector2.one - closetCard.anchorMax, full) + new Vector2(40, 40);
        var offsetMin = Vector2.Lerp(new Vector2(12, sheet - 40), -below, k);
        var offsetMax = Vector2.Lerp(new Vector2(-12, -(header - 4)), above, k);
        closetTag.offsetMin = offsetMin;
        closetTag.offsetMax = offsetMax;
        closetTagFrame.offsetMin = offsetMin - new Vector2(6, 6);
        closetTagFrame.offsetMax = offsetMax + new Vector2(6, 6);
    }

    /// The tag behind the character: its pattern drifts along and its rays turn.
    void ShowClosetTag(TagStyle tag)
    {
        closetTagStyle = tag;
        closetTagGradient.texture = TagArt.Gradient(tag.Colors);
        closetTagPattern.texture = TagArt.Pattern(tag.Pattern);
        closetTagPattern.enabled = closetTagPattern.texture != null;
        var ink = Voxel.Hex(tag.Ink);
        closetTagPattern.color = new Color(ink.r, ink.g, ink.b, tag.InkAlpha);
        closetTagRays.enabled = tag.Rays;
        closetTagRays.color = new Color(ink.r, ink.g, ink.b, tag.InkAlpha);
    }

    void UpdateClosetTag()
    {
        var tag = closetTagStyle;
        if (tag == null) return;
        var size = closetTag.rect.size;
        // Tiles counted out from the middle, so growing the tag doesn't slide the pattern around.
        var tiles = size / tag.Tile;
        var drift = tag.Drift * Time.unscaledTime;
        closetTagPattern.uvRect = new Rect(-tiles.x / 2 - drift.x, -tiles.y / 2 - drift.y, tiles.x, tiles.y);
        if (!tag.Rays) return;
        closetTagRays.rectTransform.position = previewImage.rectTransform.position;
        closetTagRays.rectTransform.sizeDelta = Vector2.one * Mathf.Max(size.x, size.y) * 1.5f;
        closetTagRays.rectTransform.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 12f);
    }

    void ToggleInspect()
    {
        closetInspecting = !closetInspecting;
        Synth.Instance.Swoosh();
        closetInspectButton.Icon.sprite = Ui.Icon(closetInspecting ? "close" : "inspect");
        if (!closetInspecting) previewSpunAt = -10;   // turn back and zoom out
    }

    const float GridWidth = 360;
    const int GridColumns = 4;

    void BuildGift()
    {
        var panel = MakeModal("Gift");
        giftPanel = panel.gameObject;
        // Full screen, right on the backdrop, inside the safe area. What changes per state goes in giftCard.
        var stage = Ui.SafeArea(panel, "Stage");
        giftCard = Ui.Rect(stage, "Content", Center, Vector2.zero, Vector2.zero);
        Ui.Stretch(giftCard);
        // A big invisible button over the gift, for tapping it open.
        var tapArea = Ui.Rect(stage, "Tap the gift", Center, new Vector2(0, 40), new Vector2(360, 360));
        giftTap = new HudButton { Rect = tapArea, Action = TapGift };
        buttons.Add(giftTap);
        giftAction = MakeButton(stage, Bottom, new Vector2(0, 70), new Vector2(260, 60), Ui.Gold, Ui.GoldShade, Color.white, GiftAction);
        giftAction.Label.fontSize = 22;
    }

    // MARK: - Closet

    void ShowCloset(bool open)
    {
        Synth.Instance.Click();
        closetPanel.SetActive(open);
        previewCam.enabled = open;
        if (!open)
        {
            previewCam.targetTexture = null;
            return;
        }
        previewSpunAt = -10;
        inspectZoom = 1;
        closetInspecting = false;
        closetInspectT = 0;
        closetInspectButton.Icon.sprite = Ui.Icon("inspect");
        LayoutCloset();
        ShowClosetTab(CosmeticKind.Character);
        StartCoroutine(Pop(closetCard, 0.94f));
    }

    void ShowClosetTab(CosmeticKind tab)
    {
        closetTab = tab;
        for (int i = 0; i < closetTabs.Count; i++)
        {
            bool on = ClosetTabs[i] == tab;
            closetTabs[i].Face.color = on ? Ui.Blue : Ui.CreamShade;
            closetTabs[i].Lip.color = on ? Ui.BlueShade : Ui.GreyShade;
            closetTabs[i].Icon.color = on ? Color.white : Ui.Brown;
        }

        // Rebuild the grid for this tab (thumbnails first: they use the preview camera).
        foreach (var old in closetCards)
        {
            buttons.Remove(old.Button);
            Destroy(old.Button.Rect.gameObject);
        }
        closetCards.Clear();
        var items = Cosmetics.OfKind(tab);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var position = new Vector2((i % GridColumns - (GridColumns - 1) / 2f) * CardStep, -CardSize / 2 - 4 - (i / GridColumns) * CardStep);
            var face = Ui.Box(closetContent, Top, position, new Vector2(CardSize, CardSize), Voxel.Hex(0xF3E9CC), Ui.CreamShade, 20, 4);
            var rect = (RectTransform)face.transform.parent;
            var thumb = Ui.Rect(face.transform, "Thumb", Center, new Vector2(0, 6), new Vector2(66, 66)).gameObject.AddComponent<RawImage>();
            thumb.raycastTarget = false;
            thumb.texture = Thumbnail(item);
            var check = Ui.Picture(face.transform, new Vector2(1, 0), new Vector2(-16, 16), new Vector2(22, 22), Ui.Icon("check"), Ui.Blue);
            var labelRect = Ui.Rect(face.transform, "Price", new Vector2(0.5f, 0), new Vector2(8, 11), new Vector2(80, 20));
            var label = Ui.Label(labelRect, "", 15, Ui.Brown);
            var coin = Ui.Coin(face.transform, new Vector2(0.5f, 0), new Vector2(-17, 11), 17);
            var lockIcon = Ui.Picture(face.transform, Center, new Vector2(0, 6), new Vector2(30, 30), Ui.Icon("lock"), new Color(1, 1, 1, 0.9f));
            var card = new ClosetCard { Item = item, Thumb = thumb, Coin = coin, Lock = lockIcon };
            card.Button = new HudButton { Rect = rect, Face = face, Lip = rect.GetChild(0).GetComponent<Image>(), Icon = check, Label = label,
                Action = () => SelectClosetItem(card.Item) };
            buttons.Add(card.Button);
            closetCards.Add(card);
        }
        closetContent.sizeDelta = new Vector2(GridWidth, Mathf.CeilToInt(items.Count / (float)GridColumns) * CardStep + 8);
        closetScroll.Reset();

        closetSelected = items.Find(Wearing) ?? items[0];
        RefreshCloset();
    }

    void SelectClosetItem(Cosmetic item)
    {
        closetSelected = item;
        Synth.Instance.Click();
        RefreshCloset();
        StartCoroutine(Bump(previewImage.rectTransform, 0.05f));
    }

    /// The outfit the preview shows: what you wear, with the selected item swapped in. A charm needs
    /// a backpack to hang from, so charms are shown on one even if you don't wear one yet.
    (Cosmetic character, Cosmetic hat, Cosmetic backpack, Cosmetic charm) PreviewOutfit()
    {
        var character = closetTab == CosmeticKind.Character ? closetSelected : EquippedCharacter;
        var hat = closetTab == CosmeticKind.Hat ? closetSelected : EquippedHat;
        var backpack = closetTab == CosmeticKind.Backpack ? closetSelected : EquippedBackpack;
        var charm = closetTab == CosmeticKind.Charm ? closetSelected : EquippedCharm;
        if (closetTab == CosmeticKind.Charm && backpack.Build == null) backpack = Cosmetics.Backpacks.Find(b => b.Build != null) ?? backpack;
        return (character, hat, backpack, charm);
    }

    void RefreshCloset()
    {
        foreach (var card in closetCards)
        {
            var item = card.Item;
            bool owned = Owns(item);
            bool forSale = !owned && item.Price > 0;
            bool locked = !owned && item.Price <= 0;
            card.Button.Face.color = item == closetSelected ? Voxel.Hex(0xFFE9A8) : Voxel.Hex(0xF3E9CC);
            card.Button.Icon.enabled = Wearing(item);
            card.Button.Label.text = forSale ? item.Price.ToString() : IsNothing(item) ? "None" : "";
            card.Coin.enabled = forSale;
            card.Lock.enabled = locked;
            card.Thumb.color = locked ? new Color(1, 1, 1, 0.4f) : Color.white;
        }

        var outfit = PreviewOutfit();
        PoseClimber(outfit.character, outfit.hat, outfit.backpack, outfit.charm);
        ShowClosetTag((closetTab == CosmeticKind.Tag ? closetSelected : EquippedTag).Tag);
        closetItemName.text = $"{closetSelected.Name}  <size=13><color=#{RarityColors[(int)closetSelected.Rarity]:X6}>{closetSelected.Rarity.ToString().ToUpper()}</color></size>";
        closetNamePill.sizeDelta = new Vector2(closetItemName.preferredWidth + 36, closetNamePill.sizeDelta.y);
        closetCoinText.text = Progress.Coins.ToString("N0");

        // What the big button does: wear it, buy it, or tell you how to earn it.
        bool ownedSelected = Owns(closetSelected);
        closetHint.text = closetTab == CosmeticKind.Charm && EquippedBackpack.Build == null && closetSelected.Build != null
            ? "Charms hang on a backpack: wear one to show it"
            : closetTab == CosmeticKind.Tag ? "Your background, and your banner on the leaderboard" : "";
        closetAction.Price.transform.parent.parent.gameObject.SetActive(false);
        bool nothing = IsNothing(closetSelected);
        if (ownedSelected && Wearing(closetSelected))
            SetClosetAction("Wearing it!", Ui.Grey, Ui.GreyShade);
        else if (ownedSelected)
            SetClosetAction(nothing ? "Take off" : "Wear", Ui.Blue, Ui.BlueShade);
        else if (closetSelected.Price > 0)
        {
            bool affordable = Progress.Coins >= closetSelected.Price;
            SetClosetAction("Buy", affordable ? Ui.Gold : Ui.Grey, affordable ? Ui.GoldShade : Ui.GreyShade);
            closetAction.Price.transform.parent.parent.gameObject.SetActive(true);
            closetAction.Price.text = closetSelected.Price.ToString();
            if (!affordable) closetHint.text = $"Collect {closetSelected.Price - Progress.Coins} more coins";
        }
        else if (closetSelected.Achievement != null && Cosmetics.AchievementText.TryGetValue(closetSelected.Achievement, out var goal))
        {
            SetClosetAction(goal, Ui.Grey, Ui.GreyShade);
            closetHint.text = "Earn it to unlock";
        }
        else
        {
            SetClosetAction("Daily gift only", Ui.Grey, Ui.GreyShade);
            closetHint.text = "Look out for it in a rare gift";
        }
    }

    static bool IsNothing(Cosmetic item) => item.Build == null && item.Look == null && item.Tag == null;

    void SetClosetAction(string label, Color face, Color lip)
    {
        closetAction.Label.text = label;
        closetAction.Face.color = face;
        closetAction.Lip.color = lip;
    }

    void ClosetAction()
    {
        var item = closetSelected;
        if (Owns(item))
        {
            if (Wearing(item)) return;
            Wear(item);
            return;
        }
        if (item.Price <= 0 || !Progress.Spend(item.Price))
        {
            Synth.Instance.Nope();
            Haptics.Slip();
            return;
        }
        Progress.AddOwned(item.Id);
        Progress.Save();
        Synth.Instance.Purchase();
        Haptics.Success();
        RefreshShop();
        Wear(item);
        StartCoroutine(Pop(previewImage.rectTransform, 1.25f));
    }

    void Wear(Cosmetic item)
    {
        switch (item.Kind)
        {
            case CosmeticKind.Character: Progress.Character = item.Id; break;
            case CosmeticKind.Hat: Progress.Hat = item.Id; break;
            case CosmeticKind.Backpack: Progress.Backpack = item.Id; break;
            case CosmeticKind.Charm: Progress.Charm = item.Id; break;
            default: Progress.Tag = item.Id; break;
        }
        Progress.Save();
        DressClimber();
        Synth.Instance.Swoosh();
        RefreshCloset();
    }

    // MARK: - Spinning and zooming the preview

    int previewTouch = -1;

    /// A finger that starts on the stage spins the climber (with momentum); two fingers pinch to zoom.
    void PreviewInput()
    {
        var active = new List<Touch>();
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase is UnityEngine.InputSystem.TouchPhase.Ended or UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                if (touch.touchId == previewTouch) previewTouch = -1;
                continue;
            }
            // Inspecting, a finger anywhere spins; otherwise it has to start on the preview.
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && !buttonTouches.Contains(touch.touchId)
                && (closetInspecting || RectTransformUtility.RectangleContainsScreenPoint(previewImage.rectTransform, touch.screenPosition, null)))
                previewTouch = touch.touchId;
            if (previewTouch != -1) active.Add(touch);
        }
        if (previewTouch == -1 || active.Count == 0)
        {
            pinchStartDistance = 0;
            return;
        }
        previewSpunAt = Time.unscaledTime;
        if (active.Count == 1)
        {
            float dx = active[0].delta.x / canvasRect.localScale.x;
            inspectYaw -= dx * 0.6f;
            inspectYawVelocity = -dx * 0.6f / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            pinchStartDistance = 0;
        }
        else
        {
            float distance = Vector2.Distance(active[0].screenPosition, active[1].screenPosition);
            if (pinchStartDistance <= 0)
            {
                pinchStartDistance = distance;
                pinchStartZoom = inspectZoom;
            }
            inspectZoom = Mathf.Clamp(pinchStartZoom * distance / Mathf.Max(1, pinchStartDistance), 1f, 2.8f);
            inspectYawVelocity = 0;
        }
    }

    float previewSpunAt = -10;

    /// The preview turns gently on its own (showing the back for backpacks and charms) until you spin it.
    void UpdateCloset()
    {
        UpdateGift();
        if (closetPanel == null || !closetPanel.activeSelf) return;
        float dt = Time.unscaledDeltaTime;
        LayoutCloset();   // every frame: cheap, and right even while the screen size is still settling
        closetScroll.Update(dt);
        bool back = closetTab is CosmeticKind.Backpack or CosmeticKind.Charm;
        if (Time.unscaledTime - previewSpunAt > 4f)
        {
            // Drift back to showing off the item, and zoom back out (inspecting, the zoom stays).
            float rest = (back ? 200 : 15) + 20 * Mathf.Sin(Time.unscaledTime * 0.8f);
            inspectYaw = Mathf.LerpAngle(inspectYaw, rest, 1 - Mathf.Exp(-2 * dt));
            if (!closetInspecting) inspectZoom = Mathf.Lerp(inspectZoom, 1, 1 - Mathf.Exp(-2 * dt));
        }
        else if (previewTouch == -1)
        {
            inspectYaw += inspectYawVelocity * dt;
            inspectYawVelocity *= Mathf.Exp(-3 * dt);
        }
        UpdateClosetTag();
        previewClimber.transform.localRotation = Quaternion.Euler(0, inspectYaw, 0);
        AimPreview(Framing.Inspect);
        previewCam.targetTexture = previewTexture;
        previewCam.aspect = previewTexture.width / (float)previewTexture.height;
    }

    // MARK: - Daily gift

    // Tap the gift three times to open it. Each tap may upgrade it a tier. It's free, so the surprise is fine.
    // The box is a voxel model, rendered live by the preview camera; the party on opening is drawn in the UI.
    static readonly string[] GiftTiers = { "GIFT", "RARE!", "MEGA!", "ULTRA!", "LEGENDARY!" };
    static readonly uint[] GiftColors = { 0xE85A4F, 0x4C8FE8, 0x9B59D0, 0xF28C28, 0xF06292 };
    const int GiftTaps = 3;

    static string Today => System.DateTime.Now.ToString("yyyy-MM-dd");
    bool GiftReady => Progress.GiftDay != Today;

    enum GiftState { Wrapped, Opening, Opened, Waiting }
    GiftState giftState;
    int giftTier, giftTapsDone;
    HudButton giftTap;
    Transform giftModel, giftBodyGroup, giftLid;
    RawImage giftImage;
    RectTransform giftTierPill, giftRays;
    Image giftRaysImage;
    Text giftTierText;
    readonly List<Image> giftDots = new();
    static Sprite raysSprite;
    float giftSquash;   // 1 right after a tap, easing back to 0

    void ShowGift(bool open)
    {
        Synth.Instance.Click();
        giftPanel.SetActive(open);
        previewCam.enabled = open;
        if (!open)
        {
            if (giftModel != null) giftModel.gameObject.SetActive(false);
            previewCam.targetTexture = null;
            RefreshMenus();
            return;
        }
        giftTier = giftTapsDone = 0;
        giftState = GiftReady ? GiftState.Wrapped : GiftState.Waiting;
        // Today's upgrades and prize come from a seed fixed for the day: leaving and coming back
        // (or restarting the app) replays the same gift, so rerolling gets you nothing.
        giftRolls = new System.Random(Progress.GiftSeed(Today));
        nextSuspenseAt = Time.unscaledTime + 0.5f;
        FillGiftCard(null, null);
        StartCoroutine(Pop(giftCard, 0.85f));
    }

    System.Random giftRolls;
    float nextSuspenseAt;

    // MARK: The 3D box

    void BuildGiftModel()
    {
        if (giftModel != null) DestroyImmediate(giftModel.gameObject);
        giftModel = new GameObject("Gift").transform;
        giftModel.SetParent(previewRoot, false);
        giftBodyGroup = new GameObject("Body").transform;
        giftBodyGroup.SetParent(giftModel, false);
        giftLid = new GameObject("Lid").transform;
        giftLid.SetParent(giftModel, false);
        giftLid.localPosition = new Vector3(0, 0.7f, 0);
        ColorGift(GiftColors[0]);
    }

    /// (Re)builds the box in a tier's color: body, lid, a golden ribbon both ways and a bow on top.
    void ColorGift(uint color)
    {
        foreach (var group in new[] { giftBodyGroup, giftLid })
            for (int c = group.childCount - 1; c >= 0; c--) DestroyImmediate(group.GetChild(c).gameObject);
        const uint ribbon = 0xFFD54F, ribbonShade = 0xE6A817;
        Voxel.Box(giftBodyGroup, new Vector3(1f, 0.7f, 1f), color, new Vector3(0, 0.35f, 0));
        Voxel.Box(giftBodyGroup, new Vector3(0.2f, 0.71f, 1.02f), ribbon, new Vector3(0, 0.35f, 0));
        Voxel.Box(giftBodyGroup, new Vector3(1.02f, 0.71f, 0.2f), ribbon, new Vector3(0, 0.35f, 0));
        Voxel.Box(giftLid, new Vector3(1.12f, 0.24f, 1.12f), color, new Vector3(0, 0.12f, 0));
        Voxel.Box(giftLid, new Vector3(0.2f, 0.25f, 1.14f), ribbon, new Vector3(0, 0.12f, 0));
        Voxel.Box(giftLid, new Vector3(1.14f, 0.25f, 0.2f), ribbon, new Vector3(0, 0.12f, 0));
        foreach (float side in new[] { -1f, 1f })
        {
            var loop = Voxel.Box(giftLid, new Vector3(0.34f, 0.22f, 0.16f), ribbon, new Vector3(side * 0.2f, 0.36f, 0));
            loop.transform.localRotation = Quaternion.Euler(0, 0, side * 28f);
        }
        Voxel.Box(giftLid, new Vector3(0.16f, 0.16f, 0.18f), ribbonShade, new Vector3(0, 0.32f, 0));
        foreach (var t in giftModel.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer;
    }

    void AimAtGift()
    {
        previewClimber.gameObject.SetActive(false);
        previewItem.gameObject.SetActive(false);
        giftModel.gameObject.SetActive(true);
        var target = previewRoot.position + new Vector3(0, 0.5f, 0);
        var rotation = Quaternion.Euler(22, 0, 0);
        previewCam.transform.SetPositionAndRotation(target - rotation * Vector3.forward * 10, rotation);
        previewCam.orthographicSize = 1.5f;   // the gift card shows the middle square of the 2:3 texture
        previewCam.targetTexture = previewTexture;
        previewCam.aspect = previewTexture.width / (float)previewTexture.height;
    }

    // MARK: The screen

    RectTransform GiftFace => giftCard;

    void ClearGiftCard()
    {
        var face = GiftFace;
        for (int i = face.childCount - 1; i >= 0; i--) Destroy(face.GetChild(i).gameObject);
        giftDots.Clear();
    }

    /// Light rays fanning out from the middle, fading toward the edges.
    static Sprite RaysSprite => raysSprite ??= Ui.MakeShape(256, (x, y) =>
    {
        float r = Mathf.Sqrt(x * x + y * y);
        float ray = Mathf.Sin(Mathf.Atan2(y, x) * 10f) > 0.25f ? 1 : 0;
        return ray * Mathf.Clamp01(1 - r) * 0.9f;
    });

    /// A line of text on a cream pill, readable on the patterned backdrop.
    Text TextPill(Transform parent, Vector2 position, string text, int size, float height = 44)
    {
        var pill = Ui.Pill(parent, Center, position, new Vector2(200, height), Ui.Cream, Ui.CreamShade, 4);
        var label = Ui.Label(pill.transform, text, size, Ui.Brown);
        label.lineSpacing = 1.1f;
        ((RectTransform)pill.transform.parent).sizeDelta = new Vector2(label.preferredWidth + 48, height);
        return label;
    }

    /// The whole screen: the wrapped gift to tap, what was inside, or when the next one comes.
    void FillGiftCard(Texture reward, string rewardText, Sprite rewardIcon = null)
    {
        ClearGiftCard();
        var face = GiftFace;
        var banner = Ui.Pill(face, Top, new Vector2(0, -56), new Vector2(240, 56), Ui.Gold, Ui.GoldShade, 5);
        Ui.Label(banner.transform, "DAILY GIFT", 28, Color.white, TextAnchor.MiddleCenter, shadow: true);
        giftTap.Rect.gameObject.SetActive(giftState == GiftState.Wrapped);
        // No way out while it's wrapped: open it first.
        giftAction.Rect.gameObject.SetActive(giftState is GiftState.Opened or GiftState.Waiting);

        switch (giftState)
        {
            case GiftState.Wrapped:
                BuildGiftModel();
                AimAtGift();
                // Shining rays behind the box once it upgrades.
                giftRaysImage = Ui.Picture(face, Center, new Vector2(0, 40), new Vector2(560, 560), RaysSprite, Color.clear);
                giftRays = giftRaysImage.rectTransform;
                giftImage = Ui.Rect(face, "Gift", Center, new Vector2(0, 40), new Vector2(360, 360)).gameObject.AddComponent<RawImage>();
                giftImage.texture = previewTexture;
                giftImage.uvRect = new Rect(0, 1 / 6f, 1, 2 / 3f);
                giftImage.raycastTarget = false;
                var pill = Ui.Pill(face, Center, new Vector2(0, 250), new Vector2(200, 44), Ui.Gold, Ui.GoldShade, 4);
                giftTierPill = (RectTransform)pill.transform.parent;
                giftTierText = Ui.Label(pill.transform, "", 24, Color.white, TextAnchor.MiddleCenter, shadow: true);
                giftTierPill.gameObject.SetActive(false);
                TextPill(face, new Vector2(0, -175), "Tap it 3 times!", 22);
                for (int d = 0; d < GiftTaps; d++)
                    giftDots.Add(Ui.Circle(face, Center, new Vector2((d - 1) * 40, -228), 24, Ui.Cream));
                break;

            case GiftState.Opened:
                // Always a party: big golden rays turning behind the prize, and confetti everywhere.
                giftRaysImage = Ui.Picture(face, Center, new Vector2(0, 50), new Vector2(720, 720), RaysSprite,
                    Color.Lerp(Voxel.Hex(GiftColors[giftTier]), Voxel.Hex(0xFFD54F), 0.5f) * new Color(1, 1, 1, 0.8f));
                giftRays = giftRaysImage.rectTransform;
                if (giftTier > 0)
                {
                    var tierPill = Ui.Pill(face, Center, new Vector2(0, 250), new Vector2(230, 44), Voxel.Hex(GiftColors[giftTier]), Ui.GoldShade, 4);
                    Ui.Label(tierPill.transform, GiftTiers[giftTier].TrimEnd('!') + " GIFT", 22, Color.white, TextAnchor.MiddleCenter, shadow: true);
                }
                if (reward != null)
                {
                    var picture = Ui.Rect(face, "Reward", Center, new Vector2(0, 50), new Vector2(240, 240)).gameObject.AddComponent<RawImage>();
                    picture.texture = reward;
                    StartCoroutine(Pop(picture.rectTransform, 0.3f));
                }
                else if (rewardIcon != null)
                    StartCoroutine(Pop(Ui.Picture(face, Center, new Vector2(0, 50), new Vector2(170, 170), rewardIcon, Color.white).rectTransform, 0.3f));
                else StartCoroutine(Pop(Ui.Coin(face, Center, new Vector2(0, 50), 170).rectTransform, 0.3f));
                TextPill(face, new Vector2(0, -160), rewardText, 28, 54);
                SetGiftButton("Collect", Ui.Gold, Ui.GoldShade, Color.white);
                Confetti(110 + giftTier * 35);
                break;

            case GiftState.Waiting:
                Ui.Picture(face, Center, new Vector2(0, 60), new Vector2(180, 180), Ui.Icon("gift_box"), new Color(1, 1, 1, 0.6f));
                var wait = System.DateTime.Today.AddDays(1) - System.DateTime.Now;
                TextPill(face, new Vector2(0, -110), $"Come back tomorrow!\nNext gift in {wait.Hours} h {wait.Minutes} m", 20, 76);
                SetGiftButton("OK", Ui.Blue, Ui.BlueShade, Color.white);
                break;
        }
    }

    void SetGiftButton(string label, Color face, Color lip, Color text)
    {
        giftAction.Label.text = label;
        giftAction.Label.color = text;
        giftAction.Face.color = face;
        giftAction.Lip.color = lip;
    }

    void GiftAction()
    {
        if (giftState is GiftState.Opened or GiftState.Waiting) ShowGift(false);
    }

    // MARK: Tapping and opening

    /// One tap on the wrapped gift: a squash and shake, a dot, and maybe an upgrade.
    void TapGift()
    {
        if (giftState != GiftState.Wrapped) return;
        giftTapsDone++;
        giftDots[giftTapsDone - 1].color = Voxel.Hex(GiftColors[giftTier]);
        giftSquash = 1;
        Haptics.Tick(0.4f + 0.2f * giftTapsDone);

        // Each tap has a chance to upgrade the gift a tier, now and then two (rolled from today's seed).
        double roll = giftRolls.NextDouble();
        int steps = roll < 0.05 ? 2 : roll < 0.33 ? 1 : 0;
        if (steps > 0 && giftTier < GiftTiers.Length - 1)
        {
            giftTier = Mathf.Min(GiftTiers.Length - 1, giftTier + steps);
            ColorGift(GiftColors[giftTier]);
            giftDots[giftTapsDone - 1].color = Voxel.Hex(GiftColors[giftTier]);
            giftTierPill.gameObject.SetActive(true);
            giftTierPill.GetChild(1).GetComponent<Image>().color = Voxel.Hex(GiftColors[giftTier]);
            giftTierText.text = GiftTiers[giftTier];
            StartCoroutine(Pop(giftTierPill, 1.8f));
            Synth.Instance.GiftUpgrade(giftTier);
            Haptics.Success();
            Confetti(18 + giftTier * 8, small: true);
        }
        else Synth.Instance.GiftTap(giftTapsDone, giftTier);
        nextSuspenseAt = Mathf.Min(nextSuspenseAt, Time.unscaledTime + 0.25f);
        if (giftTapsDone >= GiftTaps) StartCoroutine(OpenGift());
    }

    /// After the third tap: a drumroll and a rising whine, the box shakes harder, the lid pops off, party.
    IEnumerator OpenGift()
    {
        giftState = GiftState.Opening;
        giftTap.Rect.gameObject.SetActive(false);
        Progress.GiftDay = Today;
        yield return new WaitForSecondsRealtime(0.35f);
        Synth.Instance.GiftRiser(1.1f);
        for (float t = 0; t < 1.1f; t += Time.unscaledDeltaTime)
        {
            float k = t / 1.1f;
            giftModel.localRotation = Quaternion.Euler(0, 25, Mathf.Sin(t * (40f + 40f * k)) * (4f + 10f * k));
            giftModel.localScale = Vector3.one * (1 + 0.08f * k * k);
            yield return null;
        }
        Synth.Instance.GiftOpen();
        Haptics.Hit();
        for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float u = t / 0.35f;
            giftLid.localPosition = new Vector3(0, 0.7f + u * 1.6f, 0);
            giftLid.localRotation = Quaternion.Euler(-u * 40f, 0, u * 25f);
            yield return null;
        }

        giftModel.gameObject.SetActive(false);   // out of the way before a prize hat gets its picture taken

        // A common gift holds a heart or coins; a better gift holds an item one rarity below it
        // (rare gift: a common item, ... legendary gift: an ultra or legendary item).
        Texture reward = null;
        Sprite icon = null;
        string text;
        var prize = giftTier > 0 ? PickGiftItem(giftTier) : null;
        if (prize != null)
        {
            Progress.AddOwned(prize.Id);
            reward = Thumbnail(prize);
            text = $"{prize.Name}!";
        }
        else if (giftTier == 0 && Progress.ExtraLives < Tuning.MaxExtraLives && giftRolls.NextDouble() < 0.5)
        {
            Progress.ExtraLives++;
            icon = Ui.Icon("heart");
            text = "+1 heart";
        }
        else
        {
            // Nothing left to find at this rarity (or a common gift): coins, more for better gifts.
            int[] coinsMin = { 20, 120, 200, 300, 500 }, coinsMax = { 40, 160, 260, 380, 600 };
            int coins = giftRolls.Next(coinsMin[giftTier], coinsMax[giftTier] + 1) / 5 * 5;
            Progress.Coins += coins;
            text = $"+{coins} coins";
        }
        Progress.Save();
        Synth.Instance.ZoneFanfare();
        Haptics.Success();
        giftState = GiftState.Opened;
        FillGiftCard(reward, text, icon);
        RefreshShop();
    }

    /// An item for a gift tier that you don't have yet: one rarity below the gift (a legendary gift can also
    /// hold a legendary item), falling back to lower rarities. Null when there's nothing left to find.
    Cosmetic PickGiftItem(int tier)
    {
        var pool = AllItems.Where(i => i.InGift && !Owns(i)).ToList();
        for (int rarity = tier - 1; rarity >= 0; rarity--)
        {
            var options = pool.Where(i => (int)i.Rarity == rarity || (tier == 4 && rarity == 3 && i.Rarity == Rarity.Legendary)).ToList();
            if (options.Count > 0) return options[giftRolls.Next(options.Count)];
        }
        return null;
    }

    /// Every frame while the gift is open: the box bobs and turns, trembles more with every tap, squashes
    /// after a tap, and the rays spin (in rainbow colors for a legendary gift). A drumroll speeds up as you tap.
    void UpdateGift()
    {
        if (giftPanel == null || !giftPanel.activeSelf) return;
        float time = Time.unscaledTime;
        if (giftState is GiftState.Wrapped or GiftState.Opening && time >= nextSuspenseAt)
        {
            float[] gaps = { 0.6f, 0.36f, 0.2f };
            bool opening = giftState == GiftState.Opening;
            Synth.Instance.SuspenseTick(opening ? 1 : giftTapsDone / 3f, giftTier);
            nextSuspenseAt = time + (opening ? 0.07f : gaps[Mathf.Min(giftTapsDone, 2)]);
        }
        if (giftState == GiftState.Wrapped && giftModel != null)
        {
            giftSquash = Mathf.MoveTowards(giftSquash, 0, Time.unscaledDeltaTime * 5f);
            float squash = Mathf.Sin(giftSquash * Mathf.PI) * 0.18f;
            giftModel.localScale = new Vector3(1 + squash, 1 - squash, 1 + squash);
            float tremble = Mathf.Sin(time * 47f) * giftTapsDone * 1.4f;
            giftModel.localRotation = Quaternion.Euler(0, 25 + 15 * Mathf.Sin(time * 0.9f), Mathf.Sin(time * 30f) * 6f * giftSquash + tremble);
            giftModel.localPosition = new Vector3(0, 0.05f * Mathf.Sin(time * 2.2f), 0);
            if (giftTier > 0)
            {
                var color = giftTier == GiftTiers.Length - 1
                    ? Color.HSVToRGB(Mathf.Repeat(time * 0.35f, 1f), 0.5f, 1f)
                    : Voxel.Hex(GiftColors[giftTier]);
                giftRaysImage.color = new Color(color.r, color.g, color.b, 0.45f + 0.1f * giftTier);
            }
        }
        if (giftRays != null) giftRays.localRotation = Quaternion.Euler(0, 0, time * 25f);
    }

    // MARK: Confetti

    /// A burst of confetti from the middle of the gift, falling over the whole screen.
    void Confetti(int count, bool small = false)
    {
        uint[] colors = { 0xF06292, 0xFFD54F, 0x4C8FE8, 0x6CD94F, 0x9B59D0, 0xFF8A3D, 0xFFFFFF };
        for (int i = 0; i < count; i++)
        {
            var piece = Ui.Picture(giftPanel.transform, Center, new Vector2(0, 80), new Vector2(Random.Range(8f, 13f), Random.Range(12f, 20f)),
                null, Voxel.Hex(colors[Random.Range(0, colors.Length)]));
            piece.enabled = true;   // no sprite: a plain colored rectangle
            var mover = piece.gameObject.AddComponent<ConfettiPiece>();
            float angle = Random.Range(0f, Mathf.PI * 2);
            float speed = Random.Range(small ? 150f : 300f, small ? 350f : 900f);
            mover.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.8f + 0.6f) * speed;
            mover.Spin = Random.Range(-720f, 720f);
        }
    }
}

/// One piece of confetti: flies out, tumbles, falls, fades and goes away.
public class ConfettiPiece : MonoBehaviour
{
    public Vector2 Velocity;
    public float Spin;
    float age;
    Image image;

    void Start() => image = GetComponent<Image>();

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        age += dt;
        var rect = (RectTransform)transform;
        Velocity += new Vector2(0, -900f) * dt;
        Velocity *= 1 - 1.2f * dt;   // air drag
        rect.anchoredPosition += Velocity * dt;
        rect.localRotation = Quaternion.Euler(0, Mathf.Sin(age * 9f) * 70f, Spin * age);
        if (age > 1.8f && image != null) image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Max(0, 1 - (age - 1.8f)));
        if (age > 2.8f) Destroy(gameObject);
    }
}
