using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// The player's saved progress. Stored on the device for now; one place to move to the cloud later.
public static class Progress
{
    public static int Coins
    {
        get => PlayerPrefs.GetInt("coins", 0);
        set => PlayerPrefs.SetInt("coins", Mathf.Max(0, value));
    }

    public static int ExtraLives
    {
        get => PlayerPrefs.GetInt("lives", 0);
        set => PlayerPrefs.SetInt("lives", Mathf.Max(0, value));
    }

    public static int Best
    {
        get => PlayerPrefs.GetInt("best", 0);
        set => PlayerPrefs.SetInt("best", value);
    }

    // Cosmetics: what you wear, and what you've bought or been given.
    public static string Character
    {
        get => PlayerPrefs.GetString("character", "climber");
        set => PlayerPrefs.SetString("character", value);
    }

    public static string Hat
    {
        get => PlayerPrefs.GetString("hat", "");
        set => PlayerPrefs.SetString("hat", value);
    }

    public static string Backpack
    {
        get => PlayerPrefs.GetString("backpack", "none");
        set => PlayerPrefs.SetString("backpack", value);
    }

    public static string Charm
    {
        get => PlayerPrefs.GetString("charm", "none");
        set => PlayerPrefs.SetString("charm", value);
    }

    /// Your name on the leaderboard ("" until it's first needed; then a random cute one).
    public static string PlayerName
    {
        get => PlayerPrefs.GetString("playerName", "");
        set => PlayerPrefs.SetString("playerName", value);
    }

    public static string Tag
    {
        get => PlayerPrefs.GetString("tag", "sky");
        set => PlayerPrefs.SetString("tag", value);
    }

    static HashSet<string> owned;

    static HashSet<string> Owned =>
        owned ??= new HashSet<string>(PlayerPrefs.GetString("owned", "").Split(',').Where(s => s.Length > 0));

    public static bool Owns(string id) => Owned.Contains(id);

    public static void AddOwned(string id)
    {
        Owned.Add(id);
        PlayerPrefs.SetString("owned", string.Join(",", Owned));
    }

    public static int BossesBeaten
    {
        get => PlayerPrefs.GetInt("bossesBeaten", 0);
        set => PlayerPrefs.SetInt("bossesBeaten", value);
    }

    /// The day the daily gift was last opened ("yyyy-MM-dd", local time).
    public static string GiftDay
    {
        get => PlayerPrefs.GetString("giftDay", "");
        set => PlayerPrefs.SetString("giftDay", value);
    }

    /// How many daily gifts you've opened, and how many since the last Ultra or better (for the pity rule).
    public static int GiftsOpened
    {
        get => PlayerPrefs.GetInt("giftsOpened", 0);
        set => PlayerPrefs.SetInt("giftsOpened", value);
    }

    public static int GiftsSinceUltra
    {
        get => PlayerPrefs.GetInt("giftsSinceUltra", 0);
        set => PlayerPrefs.SetInt("giftsSinceUltra", value);
    }

    /// A seed for one day's gift, the same all day (so leaving and coming back can't reroll it).
    public static int GiftSeed(string day)
    {
        if (PlayerPrefs.GetString("giftSeedDay", "") != day)
        {
            PlayerPrefs.SetString("giftSeedDay", day);
            PlayerPrefs.SetInt("giftSeed", Random.Range(1, int.MaxValue));
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetInt("giftSeed", 1);
    }

    /// Alpha testing: the next gift rolls fresh.
    public static void ResetGiftSeed() => PlayerPrefs.DeleteKey("giftSeedDay");

    // Settings
    public static bool HapticsOn
    {
        get => PlayerPrefs.GetInt("haptics", 1) == 1;
        set => PlayerPrefs.SetInt("haptics", value ? 1 : 0);
    }

    /// Alpha testing: make the planet after Earth a boss planet.
    public static bool BossFirst
    {
        get => PlayerPrefs.GetInt("bossFirst", 0) == 1;
        set => PlayerPrefs.SetInt("bossFirst", value ? 1 : 0);
    }

    public static bool ShakeOn
    {
        get => PlayerPrefs.GetInt("shake", 1) == 1;
        set => PlayerPrefs.SetInt("shake", value ? 1 : 0);
    }

    /// How many times the climbing lesson was finished: the full one on first launch, a short warm-up after.
    /// (Older saves only knew "done", which counts as once.)
    public static int TutorialRuns
    {
        get => PlayerPrefs.GetInt("tutorialRuns", PlayerPrefs.GetInt("tutorialDone", 0));
        set => PlayerPrefs.SetInt("tutorialRuns", value);
    }

    // First-time hints, each shown until the player has done (or seen) it.
    public static int HintCount(string hint) => PlayerPrefs.GetInt("hint_" + hint, 0);
    public static void CountHint(string hint) => PlayerPrefs.SetInt("hint_" + hint, HintCount(hint) + 1);

    // Stamps already looked at in the stamp book (for the "new" badge), saved as "Hot:2,None:0".
    static HashSet<string> seenStamps;

    static HashSet<string> SeenStamps =>
        seenStamps ??= new HashSet<string>(PlayerPrefs.GetString("stampsSeen", "").Split(',').Where(s => s.Length > 0));

    public static bool StampSeen(string id) => SeenStamps.Contains(id);

    public static void MarkStampsSeen(IEnumerable<string> ids)
    {
        foreach (var id in ids) SeenStamps.Add(id);
        PlayerPrefs.SetString("stampsSeen", string.Join(",", SeenStamps));
    }

    // Stamp book: how often each zone stamp was collected, saved as "Hot:2=3,None:0=5".
    static Dictionary<string, int> stamps;

    static Dictionary<string, int> Stamps
    {
        get
        {
            if (stamps != null) return stamps;
            stamps = new Dictionary<string, int>();
            foreach (var entry in PlayerPrefs.GetString("stamps", "").Split(','))
            {
                var parts = entry.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[1], out int count)) stamps[parts[0]] = count;
            }
            return stamps;
        }
    }

    public static int StampCount(string id) => Stamps.TryGetValue(id, out int count) ? count : 0;

    /// Adds a stamp to the book. Returns true the first time this stamp is collected.
    public static bool CollectStamp(string id)
    {
        int count = StampCount(id);
        Stamps[id] = count + 1;
        PlayerPrefs.SetString("stamps", string.Join(",", Stamps.Select(kv => $"{kv.Key}={kv.Value}")));
        return count == 0;
    }

    // Purchases and ads.

    /// Bought Remove Ads (comes with the Supporter tag).
    public static bool RemoveAds
    {
        get => PlayerPrefs.GetInt("removeAds", 0) == 1;
        set => PlayerPrefs.SetInt("removeAds", value ? 1 : 0);
    }

    /// The Supporter boost: the day it last started, and when it runs out.
    public static string BoostDay
    {
        get => PlayerPrefs.GetString("boostDay", "");
        set => PlayerPrefs.SetString("boostDay", value);
    }

    public static System.DateTime BoostUntil
    {
        get => long.TryParse(PlayerPrefs.GetString("boostUntil", ""), out long ticks) ? new System.DateTime(ticks, System.DateTimeKind.Utc) : System.DateTime.MinValue;
        set => PlayerPrefs.SetString("boostUntil", value.Ticks.ToString());
    }

    /// Alpha: the thumb tuning switch (see Tuning.ThumbTuning).
    public static bool ThumbTuning
    {
        get => PlayerPrefs.GetInt("thumbTuning", 1) == 1;
        set => PlayerPrefs.SetInt("thumbTuning", value ? 1 : 0);
    }

    /// Alpha: whether strokes go into the stroke log (off by default, so the designer's own strokes stay out).
    public static bool StrokeLogOn
    {
        get => PlayerPrefs.GetInt("strokeLogOn", 0) == 1;
        set => PlayerPrefs.SetInt("strokeLogOn", value ? 1 : 0);
    }

    /// Runs played in total (ads wait until you've had a few).
    public static int RunsPlayed
    {
        get => PlayerPrefs.GetInt("runsPlayed", 0);
        set => PlayerPrefs.SetInt("runsPlayed", value);
    }

    static HashSet<string> handledOrders;

    /// Remembers a coin purchase as paid out. Returns false if it already was (the store can deliver one twice).
    public static bool MarkOrderHandled(string transaction)
    {
        if (string.IsNullOrEmpty(transaction)) return true;
        handledOrders ??= new HashSet<string>(PlayerPrefs.GetString("orders", "").Split(',').Where(s => s.Length > 0));
        if (!handledOrders.Add(transaction)) return false;
        PlayerPrefs.SetString("orders", string.Join(",", handledOrders));
        return true;
    }

    /// Spends coins if there are enough. Returns whether it worked.
    public static bool Spend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        Save();
        return true;
    }

    public static void Save() => PlayerPrefs.Save();
}
