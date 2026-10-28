using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Ads, kept friendly: mostly videos you choose to watch for a reward (a free continue, double coins), and now
/// and then one between runs (never in your first runs, at most every few runs and minutes, and never after you
/// bought Remove Ads). Nothing random is ever behind an ad. Until an ad network is set up, a stand-in "test ad"
/// shows where the video would play.
public partial class ScrollyGame
{
    GameObject adPanel;
    Text adTitle, adNote, adCountdown;
    HudButton adClose;
    RectTransform adCard;
    Action<bool> adDone;
    bool adRewarded, adFinished;

    float lastAdAt;          // realtime of the last ad of any kind (none in the first minutes)
    int runsSinceAd;         // runs ended since the last ad
    bool adContinueUsed;     // one free continue per run
    bool coinsDoubled;       // the results' double coins, once per run

    /// The ad network has a video ready (the test ad always is).
    bool RewardedAdReady => true;

    /// Between runs, now and then: never after Remove Ads, not in your first runs, at most every few runs and minutes.
    bool InterstitialDue =>
        !Progress.RemoveAds &&
        Progress.RunsPlayed >= Tuning.AdFreeRuns &&
        runsSinceAd >= Tuning.AdEveryRuns &&
        Time.realtimeSinceStartup - lastAdAt >= Tuning.AdMinSeconds;

    /// A run ended (counts toward the next ad between runs).
    void CountRunForAds()
    {
        runsSinceAd++;
        adContinueUsed = false;
        coinsDoubled = false;
    }

    /// A video the player chose to watch; `done(true)` when they watched it to the end and earned the reward.
    void ShowRewardedAd(Action<bool> done)
    {
        if (!RewardedAdReady)
        {
            ShowTip("No video right now. Try again in a bit.");
            done(false);
            return;
        }
        ShowAd(true, done);
    }

    /// Plays the ad between runs if one is due, then carries on.
    void AfterRunAd(Action then)
    {
        if (!InterstitialDue)
        {
            then();
            return;
        }
        ShowAd(false, _ => then());
    }

    void ShowAd(bool rewarded, Action<bool> done)
    {
        lastAdAt = Time.realtimeSinceStartup;
        runsSinceAd = 0;
        adDone = done;
        adRewarded = rewarded;
        adFinished = false;
        adTitle.text = "Test ad";
        adNote.text = rewarded
            ? "A short video plays here once ads are set up.\nWatch it to the end for your reward."
            : "An ad between runs plays here once ads are set up.\nRemove Ads makes these go away.";
        adPanel.SetActive(true);
        adPanel.transform.SetAsLastSibling();
        Synth.Instance.Click();
        StartCoroutine(Pop(adCard, 0.85f));
        StartCoroutine(RunTestAd());
    }

    IEnumerator RunTestAd()
    {
        adClose.Rect.gameObject.SetActive(false);
        for (float left = Tuning.TestAdSeconds; left > 0; left -= Time.unscaledDeltaTime)
        {
            adCountdown.text = adRewarded ? $"Reward in {Mathf.CeilToInt(left)}" : $"Closes in {Mathf.CeilToInt(left)}";
            yield return null;
        }
        adFinished = true;
        adCountdown.text = adRewarded ? "Reward earned!" : "";
        adClose.Rect.gameObject.SetActive(true);
        StartCoroutine(Pop(adClose.Rect, 0.7f));
    }

    void CloseAd()
    {
        if (!adFinished) return;
        adPanel.SetActive(false);
        var done = adDone;
        adDone = null;
        done?.Invoke(adRewarded);
    }

    void BuildTestAd()
    {
        var panel = Ui.Rect(canvasRect, "TestAd", Center, Vector2.zero, Vector2.zero);
        Ui.Stretch(panel);
        var dim = panel.gameObject.AddComponent<Image>();
        dim.color = new Color(0.07f, 0.07f, 0.1f, 0.96f);
        adPanel = panel.gameObject;

        var card = Ui.Box(panel, Center, new Vector2(0, 20), new Vector2(310, 250), Ui.Cream, Ui.CreamShade, 32, 6);
        adCard = (RectTransform)card.transform.parent;
        var t = card.transform;
        var play = Ui.Picture(t, Center, new Vector2(0, 66), new Vector2(64, 64), Ui.CircleSprite, Ui.Blue);
        Ui.Picture(play.rectTransform, Center, new Vector2(3, 0), new Vector2(34, 34), Ui.Icon("play"), Color.white);
        adTitle = Ui.Label(t, "", 28, Ui.Brown);
        adTitle.rectTransform.anchoredPosition = new Vector2(0, 6);
        adNote = Ui.Label(t, "", 14, Ui.BrownSoft);
        adNote.rectTransform.anchoredPosition = new Vector2(0, -40);
        adNote.lineSpacing = 1.1f;
        adCountdown = Ui.Label(t, "", 18, Ui.Brown);
        adCountdown.rectTransform.anchoredPosition = new Vector2(0, -92);

        adClose = MakeRoundButton(panel, TopRight, new Vector2(-40, -60), 46, Ui.Cream, Ui.CreamShade, Ui.Icon("close"), Ui.Brown, CloseAd);
        adClose.Label.gameObject.SetActive(false);
        panel.gameObject.SetActive(false);
    }

    // MARK: - The rewards

    /// Continue offer: watch a video and the rocket flies you back up for free (once per run).
    bool CanContinueWithAd => CanContinue && !adContinueUsed && RewardedAdReady;

    void ContinueWithAd()
    {
        if (!CanContinueWithAd) return;
        ShowRewardedAd(watched =>
        {
            if (!watched) return;
            adContinueUsed = true;
            Continue();
        });
    }

    /// Results: watch a video to double this run's coins.
    bool CanDoubleCoins => runCoins >= Tuning.AdDoubleCoinsMin && !coinsDoubled && RewardedAdReady;

    void DoubleCoinsWithAd()
    {
        if (!CanDoubleCoins) return;
        ShowRewardedAd(watched =>
        {
            if (!watched) return;
            coinsDoubled = true;
            Progress.Coins += runCoins;
            Progress.Save();
            RefreshShop();
            StartCoroutine(CountDoubledCoins());
        });
    }

    IEnumerator CountDoubledCoins()
    {
        doubleCoinsButton.Rect.gameObject.SetActive(false);
        gameOverCoinPill.anchoredPosition = new Vector2(0, gameOverCoinPill.anchoredPosition.y);
        for (int c = runCoins + 1; c <= runCoins * 2; c++)
        {
            gameOverCoins.text = $"+{c}";
            if (c % Mathf.Max(1, runCoins / 8) == 0) Synth.Instance.Coin();
            yield return new WaitForSeconds(Mathf.Min(0.04f, 0.5f / Mathf.Max(1, runCoins)));
        }
        Haptics.Success();
        yield return Bump(gameOverCoinPill, 0.15f);
        ShowNextUnlock();
    }
}
