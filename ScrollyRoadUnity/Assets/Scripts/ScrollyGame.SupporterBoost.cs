using System;
using UnityEngine;
using UnityEngine.UI;

/// The Supporter boost: players who bought Remove Ads get double coins for 15 minutes every day, starting with
/// their first run of the day. A gold pill under the coins counts it down.
public partial class ScrollyGame
{
    RectTransform boostPill;
    Text boostText;

    static bool BoostActive => Progress.RemoveAds && DateTime.UtcNow < Progress.BoostUntil;

    /// What one coin is worth right now.
    static int CoinValue => BoostActive ? 2 : 1;

    /// The first run of the day starts the boost (for supporters).
    void StartDailyBoost()
    {
        if (!Progress.RemoveAds) return;
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (Progress.BoostDay == today) return;
        Progress.BoostDay = today;
        Progress.BoostUntil = DateTime.UtcNow.AddMinutes(Tuning.SupporterBoostMinutes);
        Progress.Save();
        ShowBanner("SUPPORTER BOOST", $"2x COINS · {Tuning.SupporterBoostMinutes} MIN");
        Synth.Instance.NewBest();
    }

    void BuildBoostPill()
    {
        var pill = Ui.Pill(safeRect, TopRight, new Vector2(-50, -94), new Vector2(92, 26), Ui.Gold, Ui.GoldShade, 3);
        boostPill = (RectTransform)pill.transform.parent;
        boostText = Ui.Label(pill.transform, "", 14, Ui.OnGold);
        boostPill.gameObject.SetActive(false);
    }

    void UpdateBoostPill()
    {
        bool show = BoostActive && phase is Phase.Playing or Phase.Launching or Phase.Falling && OpenModal == null;
        boostPill.gameObject.SetActive(show);
        if (!show) return;
        var left = Progress.BoostUntil - DateTime.UtcNow;
        boostText.text = $"2x  {(int)left.TotalMinutes}:{left.Seconds:00}";
    }
}
