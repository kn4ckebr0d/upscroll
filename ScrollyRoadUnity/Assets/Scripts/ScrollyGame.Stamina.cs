using UnityEngine;

/// Stamina drains while you hang between checkpoints (faster when the band is tight), refills while
/// resting on a checkpoint or ledge, and the more tired you are, the heavier climbing gets.
/// When it runs out your hands slip.
public partial class ScrollyGame
{
    float stamina = 1f;
    bool isExhausted;
    bool shownSlipTip;
    float nextWarningBeep;

    void ResetStamina()
    {
        stamina = 1f;
        isExhausted = false;
    }

    void UpdateStamina(float dt)
    {
        if (TutorialActive)
        {
            stamina = 1;   // no getting tired while learning
            return;
        }
        bool hanging = stretch > 0.03f && !isSnapping && !perched;   // standing on a ledge counts as resting
        if (hanging)
        {
            float drain = (Tuning.StaminaBaseDrain + Tuning.StaminaTensionDrain * Tension) * StaminaDrainFactor;
            if (inWaterfall) drain += Tuning.WaterfallDrain;
            stamina -= drain * dt;
            if (stamina <= 0)
            {
                stamina = 0;
                Slip();
            }
            else if (stamina < 0.25f && Time.time >= nextWarningBeep)
            {
                Synth.Instance.StaminaWarning();
                StartCoroutine(Heartbeat());
                nextWarningBeep = Time.time + 0.6f;
            }
        }
        else
        {
            stamina = Mathf.Min(1, stamina + Tuning.StaminaRegen * dt);
            if (isExhausted && stamina >= Tuning.StaminaRecoverAt) isExhausted = false;
        }
    }

    /// Out of stamina: you snap back and can't grab until you've rested a bit.
    void Slip()
    {
        isExhausted = true;
        Synth.Instance.Slip();
        Haptics.Slip();
        if (!shownSlipTip)
        {
            shownSlipTip = true;
            ShowTip("OUT OF STAMINA!\nRest on a checkpoint to recover.");
        }
        Release(slipped: true);
    }

    /// Double thump, like a heartbeat, for low stamina.
    System.Collections.IEnumerator Heartbeat()
    {
        Haptics.Snap();
        yield return new WaitForSeconds(0.14f);
        Haptics.Snap();
    }
}
