using System.Collections;
using UnityEngine;

/// Spending coins: extra lives (bought on the title screen) and continues (on the game-over screen),
/// where the parked rocket flies you back up to your last checkpoint.
public partial class ScrollyGame
{
    int continuesUsed;

    /// Grows with how high your checkpoint is (35 at 100 m, 125 at 1000 m) and doubles with each continue.
    int ContinuePrice => Mathf.RoundToInt((Tuning.ContinueBasePrice + anchorY / Tuning.ContinueMetersPerCoin) / 5f) * 5 << continuesUsed;

    void BuyExtraLife()
    {
        if (Progress.ExtraLives >= Tuning.MaxExtraLives)
        {
            Synth.Instance.Nope();
            return;
        }
        if (!Progress.Spend(Tuning.ExtraLifePrice))
        {
            Synth.Instance.Nope();
            ShowTip($"Not enough coins yet!\nCollect {Tuning.ExtraLifePrice - Progress.Coins} more.");
            return;
        }
        Progress.ExtraLives++;
        Progress.Save();
        Synth.Instance.Purchase();
        Haptics.Success();
        RefreshShop();
    }

    void ToggleSound()
    {
        Synth.Instance.Enabled = !Synth.Instance.Enabled;
        Progress.Save();
        Synth.Instance.Click();
        RefreshShop();
    }

    void DeclineContinue()
    {
        Synth.Instance.Click();
        ShowContinueOffer(false);
        ShowGameOver(true);
    }

    void PlayAgain()
    {
        Synth.Instance.Click();
        AfterRunAd(ResetWorld);
    }

    /// A mob hit you but you had an extra life: slide back to the checkpoint instead of falling.
    void UseExtraLife()
    {
        Progress.ExtraLives--;
        Progress.Save();
        RefreshShop();
        Synth.Instance.Hit();
        Haptics.Hit();
        ShakeCamera(0.25f);
        ShowBanner("EXTRA LIFE USED", "SAVED!");
        if (stretch > 0.02f) SnapTo(0, toneStep, safe: true);   // knocked all the way back to the checkpoint
        perched = false;
        awaitingRelease = lastTouchCount > 0;
        MakeInvulnerable();
    }

    void MakeInvulnerable()
    {
        invulnerableUntil = Time.time + Tuning.InvulnerableTime;
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        bool visible = true;
        while (Time.time < invulnerableUntil)
        {
            visible = !visible;
            climber.SetVisible(visible);
            yield return new WaitForSeconds(0.1f);
        }
        climber.SetVisible(true);
    }

    bool CanContinue => anchorY > fallFloorY + 0.01f && continuesUsed < Tuning.MaxContinuesPerRun;

    void ContinueFromCheckpoint()
    {
        if (!CanContinue) return;
        if (!Progress.Spend(ContinuePrice))
        {
            Synth.Instance.Nope();
            ShowTip("Not enough coins to continue.");
            return;
        }
        continuesUsed++;
        Continue();
    }

    /// Paid for (coins or a video): back up to the checkpoint.
    void Continue()
    {
        RefreshShop();
        ShowContinueOffer(false);
        ShowGameOver(false);
        StartCoroutine(ContinueSequence());
    }

    /// Get up, hop into the parked rocket, fly up to the checkpoint, hop out and keep climbing.
    IEnumerator ContinueSequence()
    {
        phase = Phase.Reviving;
        var rocket = groundRocket != null ? groundRocket : PlaceGroundRocket(fallFloorY);
        groundRocket = null;
        taxiRocket = rocket;

        climber.ResetPose();
        climber.transform.position = new Vector3(climber.transform.position.x, fallFloorY, -0.55f);
        Haptics.Success();
        yield return new WaitForSeconds(0.25f);
        yield return Hop(climber.transform, climber.transform.position, rocket.Door, 0.4f);
        climber.SetVisible(false);
        yield return new WaitForSeconds(0.4f);

        float flight = Mathf.Min(2.4f, 0.8f + (anchorY - fallFloorY) * 0.01f);
        rocket.SetThrust(true);
        rocket.SetRumble(true);
        Synth.Instance.Liftoff(flight + 0.6f);
        Haptics.Land();
        Debris.Burst(rocket.transform.position, 0xFFFFFF, 10, 3f);
        yield return MoveY(rocket.transform, anchorY + 3f, flight);
        rocket.SetThrust(true, 0.6f);
        yield return MoveY(rocket.transform, anchorY, 0.6f, easeIn: false);
        rocket.SetThrust(false);
        rocket.SetRumble(false);
        Synth.Instance.Thud();
        Haptics.Land();

        column = Tuning.Columns / 2;
        climberX = ColumnX(column);
        climber.SetVisible(true);
        yield return Hop(climber.transform, rocket.Door, new Vector3(climberX, anchorY, -0.55f), 0.4f);
        StartCoroutine(FlyAway(rocket));
        PlaceGroundRocket(fallFloorY);   // a fresh one waits on the ground for next time
        taxiRocket = null;

        stretch = restBase = 0;
        isSnapping = false;
        ResetStamina();
        awaitingRelease = lastTouchCount > 0;
        toneStep = 0;
        lastBlipY = anchorY;
        phase = Phase.Playing;
        climber.Land();
        MakeInvulnerable();
        ShowBanner("CONTINUE", "BACK IN!");
    }

    IEnumerator FlyAway(Rocket rocket)
    {
        yield return new WaitForSeconds(0.4f);
        rocket.SetThrust(true);
        yield return MoveY(rocket.transform, rocket.transform.position.y + 25f, 1.2f, easeOut: false);
        Destroy(rocket.gameObject);
    }
}
