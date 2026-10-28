using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

/// Real-money purchases through the App Store and Google Play (Unity IAP 5): coin packs, and Remove Ads, which
/// also says thank you with the Supporter tag and a daily double-coins boost. Coins are fixed amounts you see up front; nothing random is ever
/// sold. Until the products exist in App Store Connect and the Play Console, Tuning.TestStore runs everything
/// against Unity's fake store, which "buys" instantly.
public static class Purchases
{
    public class Pack
    {
        public string Id;           // the product id, the same in both stores
        public string Name;
        public int Coins;           // 0 for Remove Ads
        public string TestPrice;    // shown while on the test store (the real one comes from the store)
        public string Badge;        // a small note on the pack, or null
    }

    public const string RemoveAdsId = "upscroll.removeads";

    public static readonly Pack[] Packs =
    {
        new Pack { Id = RemoveAdsId, Name = "Remove ads", TestPrice = "€2.99" },
        new Pack { Id = "upscroll.coins.1000", Name = "Pouch of coins", Coins = 1000, TestPrice = "€0.99" },
        new Pack { Id = "upscroll.coins.3000", Name = "Bag of coins", Coins = 3000, TestPrice = "€2.49", Badge = "Popular" },
        new Pack { Id = "upscroll.coins.7500", Name = "Chest of coins", Coins = 7500, TestPrice = "€4.99", Badge = "Best value" },
    };

    public static Pack PackOf(string id) => Packs.FirstOrDefault(p => p.Id == id);

    /// Something changed: products arrived, a purchase went through or failed. The shop redraws.
    public static event Action Changed;
    /// A purchase went through (the pack), for a little celebration.
    public static event Action<Pack> Bought;
    /// A purchase didn't go through, with a short reason to show; null when the player simply cancelled.
    public static event Action<string> Failed;

    public static bool Ready { get; private set; }
    public static bool Busy { get; private set; }   // waiting for the store to answer a purchase or restore
    public static bool OnTestStore => Tuning.TestStore;

    static StoreController store;
    static bool started;

    /// Connects to the store once, in the background. Pending purchases (bought but not yet confirmed, e.g. the
    /// app closed mid-purchase) come in again here and are handed out then.
    public static async void Start()
    {
        if (started) return;
        started = true;
        try
        {
            store = OnTestStore ? UnityIAPServices.StoreController("fake") : UnityIAPServices.StoreController();
            store.OnPurchasePending += OnPending;
            store.OnPurchaseConfirmed += OnConfirmed;
            store.OnPurchaseFailed += order => Finish(order.FailureReason == PurchaseFailureReason.UserCancelled ? null : "The purchase didn't go through.");
            store.OnPurchaseDeferred += _ => Finish("Waiting for approval. It'll arrive once it's approved.");
            store.OnStoreConnected += OnConnected;
            store.OnStoreDisconnected += failure => Debug.LogWarning($"[Purchases] store disconnected: {failure.Message}");
            store.OnProductsFetched += _ => { Ready = true; Changed?.Invoke(); };
            store.OnProductsFetchFailed += failure => Debug.LogWarning($"[Purchases] products not fetched: {failure.FailureReason}");
            store.OnPurchasesFetched += OnFetched;
            store.OnPurchasesFetchFailed += failure => Debug.LogWarning($"[Purchases] purchases not fetched: {failure.Message}");
            await store.Connect();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Purchases] not available: {e.Message}");
        }
    }

    static void OnConnected()
    {
        var definitions = Packs.Select(p => new ProductDefinition(p.Id, p.Coins > 0 ? ProductType.Consumable : ProductType.NonConsumable)).ToList();
        store.FetchProducts(definitions);
        store.FetchPurchases();
    }

    /// The price to show on a pack: the store's, in the player's own currency.
    public static string PriceOf(Pack pack)
    {
        if (OnTestStore) return pack.TestPrice;
        var product = Ready ? store.GetProductById(pack.Id) : null;
        return product != null && product.availableToPurchase ? product.metadata.localizedPriceString : "…";
    }

    public static bool CanBuy(Pack pack) => Ready && !Busy && (pack.Coins > 0 || !Progress.RemoveAds);

    public static void Buy(Pack pack)
    {
        if (!CanBuy(pack)) return;
        Busy = true;
        Changed?.Invoke();
        store.PurchaseProduct(pack.Id);
    }

    /// Brings back Remove Ads on a new phone or after reinstalling (Apple requires a button for this).
    public static void Restore()
    {
        if (store == null || Busy) return;
        Busy = true;
        Changed?.Invoke();
        store.RestoreTransactions((success, error) =>
        {
            Busy = false;
            Changed?.Invoke();
            if (!success) Failed?.Invoke("Couldn't restore right now. Try again later.");
        });
    }

    /// Bought (or bought earlier and not yet confirmed): hand it out and save, and only then confirm with the
    /// store. If the app dies in between, the store delivers it again and the ledger stops a second payout.
    static void OnPending(PendingOrder order)
    {
        var pack = PackOf(order.CartOrdered.Items().FirstOrDefault()?.Product?.definition.id);
        if (pack != null) Grant(pack, order.Info.TransactionID);
        store.ConfirmPurchase(order);
    }

    static void Grant(Pack pack, string transaction)
    {
        if (pack.Coins > 0)
        {
            if (!Progress.MarkOrderHandled(transaction)) return;   // already paid out
            Progress.Coins += pack.Coins;
        }
        else
        {
            Progress.RemoveAds = true;
        }
        Progress.Save();
        Busy = false;
        Bought?.Invoke(pack);
        Changed?.Invoke();
    }

    static void OnConfirmed(Order order)
    {
        if (order is FailedOrder failed) Debug.LogWarning($"[Purchases] confirm failed: {failed.FailureReason} {failed.Details}");
        Busy = false;
        Changed?.Invoke();
    }

    /// Purchases the store already knows about: Remove Ads on this account means no ads here either.
    static void OnFetched(Orders orders)
    {
        foreach (var order in orders.ConfirmedOrders)
            foreach (var item in order.CartOrdered.Items())
                if (item.Product?.definition.id == RemoveAdsId && !Progress.RemoveAds)
                {
                    Progress.RemoveAds = true;
                    Progress.Save();
                    Changed?.Invoke();
                }
    }

    static void Finish(string message)
    {
        Busy = false;
        Changed?.Invoke();
        Failed?.Invoke(message);
    }
}
