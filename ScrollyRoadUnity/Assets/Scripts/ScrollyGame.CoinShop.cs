using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// The shop: Remove Ads (to support the game, with perks as a thank-you: double coins for 15 minutes a day and
/// the Supporter tag) and three coin packs, at the store's real prices. Opened by tapping your coins on the title screen or in the closet.
public partial class ScrollyGame
{
    class PackRow
    {
        public Purchases.Pack Pack;
        public HudButton Button;
        public Text Price;
        public Image PricePill;
    }

    GameObject shopPanel;
    RectTransform shopCard;
    readonly List<PackRow> packRows = new();
    Text shopStatus, supportTitle, testStoreNote;
    readonly List<Text> supportPerks = new();
    GameObject supportPrice, supportHeart;
    HudButton restoreButton;

    void BuildShop()
    {
        var panel = MakeModal("Shop");
        shopPanel = panel.gameObject;
        var card = Ui.Box(panel, Center, new Vector2(0, 70), new Vector2(330, 480), Ui.Cream, Ui.CreamShade, 32, 6);
        shopCard = (RectTransform)card.transform.parent;
        var t = card.transform;
        var title = Ui.Label(t, "Shop", 30, Ui.Brown);
        title.rectTransform.anchoredPosition = new Vector2(0, 212);

        // Support the game: Remove Ads, with a heart, the price by the title and the perks underneath.
        var support = MakePackRow(t, Purchases.Packs[0], 122, 128, Voxel.Hex(0xFFE9EE), Voxel.Hex(0xF2C1CC));
        var supportFace = support.Button.Face.transform;
        supportHeart = Ui.Picture(supportFace, new Vector2(0, 0.5f), new Vector2(34, 34), new Vector2(40, 40), Ui.Icon("heart"), Color.white).gameObject;
        supportTitle = RowLabel(supportFace, new Vector2(64, 36), 19, Ui.Brown);
        ((RectTransform)support.PricePill.transform.parent).anchoredPosition = new Vector2(-50, 34);
        supportPrice = support.PricePill.transform.parent.gameObject;
        string[] perks = { "No ads between runs", $"2x coins, {Tuning.SupporterBoostMinutes} min every day", "The Supporter tag" };
        for (int i = 0; i < perks.Length; i++)
        {
            float y = 2 - i * 23;
            Ui.Picture(supportFace, new Vector2(0, 0.5f), new Vector2(28, y), new Vector2(16, 16), Ui.Icon("check"), Voxel.Hex(0xE0607E));
            var perk = RowLabel(supportFace, new Vector2(44, y), 14, Ui.Brown, 230);
            perk.text = perks[i];
            supportPerks.Add(perk);
        }

        // Coins, in packs that match what things cost (no awkward leftovers).
        for (int i = 1; i < Purchases.Packs.Length; i++)
        {
            var pack = Purchases.Packs[i];
            var row = MakePackRow(t, pack, 10 - (i - 1) * 80, 70, Voxel.Hex(0xF3E9CC), Ui.CreamShade);
            var face = row.Button.Face.transform;
            for (int c = 0; c < i; c++)   // one, two or three coins
                Ui.Coin(face, new Vector2(0, 0.5f), new Vector2(28 + c * 9, 4 - c * 5), 30);
            var name = RowLabel(face, new Vector2(82, 11), 16, Ui.Brown);
            name.text = pack.Name;
            var amount = RowLabel(face, new Vector2(82, -13), 18, Ui.Blue);
            amount.text = $"{pack.Coins:N0} coins";
            if (pack.Badge != null)
            {
                var badge = Ui.Pill(row.Button.Rect, new Vector2(1, 1), new Vector2(-52, -2), new Vector2(84, 22), Ui.Gold, Ui.GoldShade, 2);
                Ui.Label(badge.transform, pack.Badge.ToUpper(), 11, Ui.OnGold);
                ((RectTransform)badge.transform.parent).localRotation = Quaternion.Euler(0, 0, -3);
            }
        }

        shopStatus = Ui.Label(t, "", 15, Ui.Brown);
        shopStatus.rectTransform.anchoredPosition = new Vector2(0, -202);
        testStoreNote = Ui.Label(t, "Test store: nothing is charged.", 12, Ui.BrownSoft);
        testStoreNote.rectTransform.anchoredPosition = new Vector2(0, -222);

        restoreButton = MakeButton(panel, Bottom, new Vector2(0, 140), new Vector2(220, 40), Ui.Cream, Ui.CreamShade, Ui.BrownSoft, () => Purchases.Restore());
        restoreButton.Label.text = "Restore purchases";
        restoreButton.Label.fontSize = 15;
        var done = MakeButton(panel, Bottom, new Vector2(0, 72), new Vector2(240, 58), Ui.Blue, Ui.BlueShade, Color.white, () => ShowShop(false), icon: Ui.Icon("close"));
        done.Label.text = "Done";
        done.Label.fontSize = 22;

        Purchases.Changed += RefreshShopScreen;
        Purchases.Bought += OnPackBought;
        Purchases.Failed += message =>
        {
            if (message == null) return;   // cancelled: nothing to say
            shopStatus.text = message;
            Synth.Instance.Nope();
        };
        Purchases.Start();
    }

    PackRow MakePackRow(Transform card, Purchases.Pack pack, float y, float height, Color face, Color shade)
    {
        var faceImage = Ui.Box(card, Center, new Vector2(0, y), new Vector2(296, height), face, shade, 20, 4);
        var rect = (RectTransform)faceImage.transform.parent;
        var row = new PackRow { Pack = pack };
        row.Button = new HudButton { Rect = rect, Face = faceImage, Lip = rect.GetChild(0).GetComponent<Image>(), Action = () => BuyPack(row) };
        buttons.Add(row.Button);
        row.PricePill = Ui.Pill(faceImage.transform, new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(80, 38), Ui.Blue, Ui.BlueShade, 3);
        row.Price = Ui.Label(row.PricePill.transform, "", 16, Color.white, TextAnchor.MiddleCenter, shadow: true);
        packRows.Add(row);
        return row;
    }

    /// A left-aligned line of text in a pack row, starting `position.x` from its left edge.
    static Text RowLabel(Transform face, Vector2 position, int size, Color color, float width = 180)
    {
        var rect = Ui.Rect(face, "Line", new Vector2(0, 0.5f), new Vector2(position.x + width / 2, position.y), new Vector2(width, 24));
        return Ui.Label(rect, "", size, color, TextAnchor.MiddleLeft);
    }

    void ShowShop(bool open)
    {
        Synth.Instance.Click();
        shopPanel.SetActive(open);
        if (!open)
        {
            RefreshShop();
            if (closetPanel.activeSelf) RefreshCloset();
            return;
        }
        shopPanel.transform.SetAsLastSibling();
        shopStatus.text = "";
        RefreshShopScreen();
        StartCoroutine(Pop(shopCard, 0.85f));
    }

    void RefreshShopScreen()
    {
        if (shopPanel == null) return;
        foreach (var row in packRows)
        {
            bool owned = row.Pack.Coins == 0 && Progress.RemoveAds;
            bool canBuy = Purchases.CanBuy(row.Pack);
            row.Price.text = Purchases.Busy ? "…" : Purchases.PriceOf(row.Pack);
            row.PricePill.color = canBuy ? Ui.Blue : Ui.Grey;
            row.PricePill.transform.parent.GetChild(0).GetComponent<Image>().color = canBuy ? Ui.BlueShade : Ui.GreyShade;
            if (row.Pack.Coins == 0) supportPrice.SetActive(!owned);
        }
        bool supporter = Progress.RemoveAds;
        supportTitle.text = supporter ? "Thank you, Supporter!" : "Remove ads";
        testStoreNote.gameObject.SetActive(Purchases.OnTestStore);
        restoreButton.Rect.gameObject.SetActive(!supporter);
        RefreshShop();
    }

    void BuyPack(PackRow row)
    {
        if (row.Pack.Coins == 0 && Progress.RemoveAds) return;
        if (!Purchases.CanBuy(row.Pack))
        {
            Synth.Instance.Nope();
            if (!Purchases.Ready) shopStatus.text = "The store isn't reachable right now.";
            return;
        }
        shopStatus.text = "";
        Purchases.Buy(row.Pack);
    }

    void OnPackBought(Purchases.Pack pack)
    {
        Synth.Instance.Purchase();
        Haptics.Success();
        shopStatus.text = pack.Coins > 0 ? $"+{pack.Coins:N0} coins!" : "Thank you for your support!";
        RefreshShop();
        if (shopPanel.activeSelf) StartCoroutine(Pop(shopCard, 1.05f));
        if (pack.Coins == 0) ShowBanner("THANK YOU!", "SUPPORTER");
    }

    // MARK: - Ways in

    /// Taps on the coin counters (title screen and closet) open the shop; a little "+" says so.
    void BuildShopEntrances(RectTransform titleArea, RectTransform closetArea)
    {
        AddShopEntrance(titleArea, new Vector2(-58, -24), new Vector2(-6, 6));
        AddShopEntrance(closetArea, new Vector2(-62, -32), new Vector2(-10, 4));
    }

    void AddShopEntrance(RectTransform parent, Vector2 center, Vector2 plus)
    {
        var rect = Ui.Rect(parent, "ShopEntrance", TopRight, center, new Vector2(116, 52));
        buttons.Add(new HudButton { Rect = rect, Action = () => ShowShop(true) });
        var badge = Ui.Picture(rect, new Vector2(1, 0), plus, new Vector2(22, 22), Ui.CircleSprite, Ui.Gold);
        Ui.Label(badge.transform, "+", 18, Ui.OnGold);
    }
}
