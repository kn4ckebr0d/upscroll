using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Teaching by showing: a ghost hand demonstrates a control the first time it matters, one at a time,
/// instead of text up front. (Climbing itself is taught hands-on by the tutorial, ScrollyGame.Tutorial.cs.)
/// Early in a run: swipe sideways. First long let-go: the hand-off.
/// First let-go high up: time slows down and the hand shows you to grab again.
public partial class ScrollyGame
{
    enum Hint { None, Swipe, HandOff, Stamina, Grab }

    Hint activeHint;
    Coroutine hintRoutine;
    RectTransform handA, handB, ripple, hintCaption, dragPrompt;
    Image handAImage, handBImage, rippleImage;
    Text hintCaptionText;
    bool swipedDuringHint;

    void BuildHints()
    {
        handA = MakeHand(out handAImage);
        handB = MakeHand(out handBImage);
        rippleImage = Ui.Picture(canvasRect, Center, Vector2.zero, new Vector2(44, 44), Ui.RingSprite, Color.white);
        ripple = rippleImage.rectTransform;
        ripple.gameObject.SetActive(false);
        var caption = Ui.Box(canvasRect, Center, Vector2.zero, new Vector2(320, 50), Ui.Cream, Ui.CreamShade, 22, 4);
        hintCaption = (RectTransform)caption.transform.parent;
        hintCaptionText = Ui.Label(caption.transform, "", 16, Ui.Brown);
        hintCaptionText.lineSpacing = 1.05f;
        hintCaption.gameObject.SetActive(false);
    }

    RectTransform MakeHand(out Image image)
    {
        image = Ui.Picture(canvasRect, Center, Vector2.zero, new Vector2(84, 84), Ui.Icon("hand"), Color.white);
        var rect = image.rectTransform;
        rect.pivot = new Vector2(0.2f, 0.79f);   // the fingertip (of the tilted glove) is the touch point
        rect.gameObject.SetActive(false);
        return rect;
    }

    void StartHint(Hint kind, IEnumerator routine)
    {
        StopHint();
        activeHint = kind;
        hintRoutine = StartCoroutine(routine);
    }

    void StopHint()
    {
        if (hintRoutine != null) StopCoroutine(hintRoutine);
        hintRoutine = null;
        activeHint = Hint.None;
        handA.gameObject.SetActive(false);
        handB.gameObject.SetActive(false);
        ripple.gameObject.SetActive(false);
        hintCaption.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    void ClearHints() => StopHint();

    void UpdateHints()
    {
        bool titleShowing = phase == Phase.Title && OpenModal == null;
        if (activeHint != Hint.None && phase != Phase.Playing) StopHint();   // the run ended

        // The swipe, a few meters into a first run, before the first mobs.
        if (activeHint == Hint.None && phase == Phase.Playing && !isSnapping && !TutorialActive && ClimberY - anchorY > 3f
            && Progress.HintCount("swipe") == 0)
            StartHint(Hint.Swipe, SwipeHint());
    }

    Vector2 ClimberPoint => CanvasPoint(climber.transform.position + Vector3.up * 0.8f);

    Vector2 CaptionAtBottom => new(0, -canvasRect.rect.height / 2 + 130);

    void ShowCaption(string text, Vector2 at)
    {
        hintCaptionText.text = text;
        hintCaption.sizeDelta = new Vector2(Mathf.Max(200, hintCaptionText.preferredWidth + 40), hintCaptionText.preferredHeight + 26);
        // Keep it on screen.
        float half = canvasRect.rect.width / 2 - hintCaption.sizeDelta.x / 2 - 12;
        hintCaption.anchoredPosition = new Vector2(Mathf.Clamp(at.x, -half, half), at.y);
        if (!hintCaption.gameObject.activeSelf)
        {
            hintCaption.gameObject.SetActive(true);
            StartCoroutine(Pop(hintCaption, 0.8f));
        }
    }

    static void SetAlpha(Image image, float alpha) => image.color = new Color(1, 1, 1, alpha);

    IEnumerator Ripple(Vector2 at)
    {
        ripple.gameObject.SetActive(true);
        for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            ripple.anchoredPosition = at;
            ripple.localScale = Vector3.one * (0.4f + t * 2.5f);
            rippleImage.color = new Color(1, 1, 1, 0.8f * (1 - t / 0.4f));
            yield return null;
        }
        ripple.gameObject.SetActive(false);
    }

    /// A hand moving from one offset (relative to the climber) to another, with a touch ripple at the start.
    IEnumerator Drag(RectTransform hand, Image image, Vector2 from, Vector2 to, float duration)
    {
        hand.gameObject.SetActive(true);
        for (float t = 0; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            hand.anchoredPosition = ClimberPoint + from;
            SetAlpha(image, t / 0.18f);
            yield return null;
        }
        StartCoroutine(Ripple(ClimberPoint + from));
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float u = t / duration;
            u = u * u * (3 - 2 * u);
            hand.anchoredPosition = ClimberPoint + Vector2.Lerp(from, to, u);
            SetAlpha(image, 1);
            yield return null;
        }
        for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            SetAlpha(image, 1 - t / 0.2f);
            yield return null;
        }
        hand.gameObject.SetActive(false);
    }

    // MARK: - The hints

    /// The climbing rhythm, one step at a time: finger 1 taps and slides down, finger 2 taps,
    /// finger 1 lets go, finger 2 slides down.
    IEnumerator HandOffDemo()
    {
        var oneTop = new Vector2(-46, 120);
        var oneBottom = new Vector2(-46, -10);
        var twoTop = new Vector2(46, 120);
        var twoBottom = new Vector2(46, -10);
        yield return Appear(handA, handAImage, oneTop);
        yield return Tap(handA, oneTop);
        yield return Move(handA, oneTop, oneBottom, 0.7f);
        yield return Appear(handB, handBImage, twoTop);
        yield return Tap(handB, twoTop);
        yield return Lift(handA, handAImage, oneBottom);
        yield return Move(handB, twoTop, twoBottom, 0.7f);
        yield return Lift(handB, handBImage, twoBottom);
    }

    IEnumerator Appear(RectTransform hand, Image image, Vector2 at)
    {
        hand.gameObject.SetActive(true);
        hand.localScale = Vector3.one;
        for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            hand.anchoredPosition = ClimberPoint + at + new Vector2(0, 16 * (1 - t / 0.2f));
            SetAlpha(image, t / 0.2f);
            yield return null;
        }
        hand.anchoredPosition = ClimberPoint + at;
        SetAlpha(image, 1);
    }

    /// The finger presses down: it shrinks a little and a ripple spreads from the fingertip.
    IEnumerator Tap(RectTransform hand, Vector2 at)
    {
        StartCoroutine(Ripple(ClimberPoint + at));
        for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
        {
            hand.anchoredPosition = ClimberPoint + at;
            hand.localScale = Vector3.one * (1 - 0.12f * Mathf.Min(1, t / 0.1f));
            yield return null;
        }
        hand.localScale = Vector3.one * 0.88f;
        yield return new WaitForSecondsRealtime(0.12f);
    }

    IEnumerator Move(RectTransform hand, Vector2 from, Vector2 to, float duration)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float u = t / duration;
            u = u * u * (3 - 2 * u);
            hand.anchoredPosition = ClimberPoint + Vector2.Lerp(from, to, u);
            yield return null;
        }
        hand.anchoredPosition = ClimberPoint + to;
    }

    /// The finger lets go: it pops back up to full size, rises a little and fades.
    IEnumerator Lift(RectTransform hand, Image image, Vector2 at)
    {
        for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            float u = t / 0.3f;
            hand.localScale = Vector3.one * Mathf.Lerp(0.88f, 1.05f, Mathf.Min(1, u * 2));
            hand.anchoredPosition = ClimberPoint + at + new Vector2(0, 18 * u);
            SetAlpha(image, 1 - u);
            yield return null;
        }
        hand.gameObject.SetActive(false);
        hand.localScale = Vector3.one;
    }

    void OnSwiped() => swipedDuringHint = true;

    IEnumerator SwipeHint()
    {
        swipedDuringHint = false;
        ShowCaption("Swipe sideways to change column", CaptionAtBottom);
        float shownAt = Time.unscaledTime;
        while (!swipedDuringHint && Time.unscaledTime - shownAt < 7f && phase == Phase.Playing)
        {
            yield return Drag(handA, handAImage, new Vector2(-70, 10), new Vector2(70, 10), 0.45f);
            if (swipedDuringHint) break;
            yield return new WaitForSecondsRealtime(0.3f);
        }
        if (swipedDuringHint)
        {
            Progress.CountHint("swipe");
            Synth.Instance.Click();
        }
        StopHint();
    }

    /// Called when you let go; teaches the hand-off, or fall damage when you let go high up.
    void OnLetGo(float drop, bool highUp)
    {
        if (highUp && Progress.HintCount("grab") == 0)
        {
            StartHint(Hint.Grab, GrabHint());
            return;
        }
        if (drop > 1.5f && Progress.HintCount("handoff") < 2 && activeHint is Hint.None or Hint.Swipe)
            StartHint(Hint.HandOff, HandOffHint());
    }

    IEnumerator HandOffHint()
    {
        Progress.CountHint("handoff");
        ShowCaption("Put your other finger down\nbefore you lift the first", CaptionAtBottom);
        for (int i = 0; i < 2; i++)
        {
            yield return HandOffDemo();
            yield return new WaitForSecondsRealtime(0.3f);
        }
        StopHint();
    }

    /// First let-go high up: time slows down until you grab again.
    IEnumerator GrabHint()
    {
        Progress.CountHint("grab");
        Time.timeScale = 0.3f;
        ShowCaption("Falling fast! Grab again to slow down", CaptionAtBottom);
        float start = Time.unscaledTime;
        handA.gameObject.SetActive(true);
        while (isSnapping && lastTouchCount == 0 && Time.unscaledTime - start < 2.2f && phase == Phase.Playing)
        {
            // A tapping finger on the climber.
            float k = Mathf.PingPong((Time.unscaledTime - start) * 2.4f, 1f);
            handA.anchoredPosition = ClimberPoint + new Vector2(30, 40 - k * 24);
            SetAlpha(handAImage, 1);
            if (k > 0.95f && !ripple.gameObject.activeSelf) StartCoroutine(Ripple(ClimberPoint + new Vector2(30, 16)));
            yield return null;
        }
        if (!interrupted) Time.timeScale = 1;
        yield return new WaitForSecondsRealtime(0.6f);
        StopHint();
    }

    void OnStaminaBarShown()
    {
        if (activeHint != Hint.None || Progress.HintCount("stamina") > 0) return;
        StartHint(Hint.Stamina, StaminaHint());
    }

    IEnumerator StaminaHint()
    {
        Progress.CountHint("stamina");
        for (float t = 0; t < 3.5f && phase == Phase.Playing; t += Time.unscaledDeltaTime)
        {
            ShowCaption("Stamina: rest on checkpoints to refill", staminaBar.anchoredPosition + new Vector2(0, 46));
            yield return null;
        }
        StopHint();
    }
}
