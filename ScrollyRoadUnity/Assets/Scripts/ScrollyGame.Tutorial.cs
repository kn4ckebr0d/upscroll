using UnityEngine;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// The first-launch tutorial, learned by doing. Two soft tracks each have a circle at the top.
/// One lights up "HOLD"; hold it, swipe down and keep holding, and the climber really climbs.
/// Then the other track, three strokes in all, which is the hand-off. Then: keep holding with one
/// finger and swipe sideways with the other. Letting go of the screen slides you back down and
/// starts the climbing lesson over, so you learn why the hand-off matters. No hits or crashes, though.
public partial class ScrollyGame
{
    enum TutorialStep { Off, Hold, Swipe, Side }

    const int TutorialStrokes = 3;

    TutorialStep tutorialStep;
    int tutorialTrack;                          // the lit track: 0 left, 1 right
    int tutorialStrokesDone;
    int tutorialStrokesNeeded = TutorialStrokes;   // fewer when redoing it after a fall in the sideways step
    readonly int[] trackTouch = { -1, -1 };     // the finger on each track
    float trackTouchStartY;                     // where the lit track's finger touched down
    GameObject tutorialGroup;
    readonly RectTransform[] trackRects = new RectTransform[2];
    readonly RectTransform[] trackCircles = new RectTransform[2];
    readonly Image[] trackCircleFaces = new Image[2];
    readonly Text[] trackCircleLabels = new Text[2];
    RectTransform sideCircle, holdTag, swipeArrow;
    float trackTopY, trackBottomY, circleTopY;
    float tutorialOopsUntil;   // show "keep a finger down" until then

    bool TutorialActive => tutorialStep != TutorialStep.Off;

    void BuildTutorial()
    {
        var group = Ui.Rect(canvasRect, "Tutorial", Center, Vector2.zero, Vector2.zero);
        Ui.Stretch(group);
        tutorialGroup = group.gameObject;
        for (int i = 0; i < 2; i++)
        {
            // A stadium shape: the corner radius is half the width, so a half circle at each end.
            var track = Ui.Box(group, Center, Vector2.zero, new Vector2(96, 400), new Color(1, 1, 1, 0.2f), Color.clear, 48, 0);
            trackRects[i] = (RectTransform)track.transform.parent;
            var circle = Ui.Circle(group, Center, Vector2.zero, 84, Ui.Gold);
            trackCircleFaces[i] = circle;
            trackCircles[i] = circle.rectTransform;
            Ui.Ring(trackCircles[i], Center, Vector2.zero, 84, new Color(1, 1, 1, 0.9f));
            trackCircleLabels[i] = Ui.Label(trackCircles[i], "HOLD", 18, Ui.OnGold, TextAnchor.MiddleCenter);
        }

        // While holding: an arrow under the circle, bobbing down, so the swipe needs no reading.
        swipeArrow = Ui.Picture(group, Center, Vector2.zero, new Vector2(52, 52), Ui.Icon("down"), new Color(1, 1, 1, 0.9f)).rectTransform;
        swipeArrow.gameObject.SetActive(false);

        // For the sideways step: a circle with arrows for the second finger, and "HOLD" over the first.
        var side = Ui.Circle(group, Center, Vector2.zero, 84, Ui.Blue);
        sideCircle = side.rectTransform;
        Ui.Ring(sideCircle, Center, Vector2.zero, 84, new Color(1, 1, 1, 0.9f));
        Ui.Picture(sideCircle, Center, new Vector2(-17, 0), new Vector2(26, 26), Ui.Icon("left"), Color.white);
        Ui.Picture(sideCircle, Center, new Vector2(17, 0), new Vector2(26, 26), Ui.Icon("right"), Color.white);
        var tag = Ui.Pill(group, Center, Vector2.zero, new Vector2(80, 30), Ui.Gold, Ui.GoldShade, 3);
        holdTag = (RectTransform)tag.transform.parent;
        Ui.Label(tag.transform, "HOLD", 16, Ui.OnGold, TextAnchor.MiddleCenter);
        tutorialGroup.SetActive(false);
    }

    bool tutorialWarmUp;   // the short version: one stroke each side, then sideways

    void StartTutorial(bool warmUp = false)
    {
        tutorialWarmUp = warmUp;
        phase = Phase.Playing;
        ShowTitle(false);
        StopHint();
        tutorialStep = TutorialStep.Hold;
        tutorialTrack = 0;
        tutorialStrokesDone = 0;
        tutorialStrokesNeeded = warmUp ? 2 : TutorialStrokes;
        trackTouch[0] = trackTouch[1] = -1;
        trackCircleLabels[0].text = trackCircleLabels[1].text = "HOLD";
        LayoutTracks();
        for (int i = 0; i < 2; i++) trackCircles[i].anchoredPosition = new Vector2(trackRects[i].anchoredPosition.x, circleTopY);
        tutorialGroup.SetActive(true);
        StartCoroutine(Pop((RectTransform)tutorialGroup.transform, 0.9f));
    }

    /// Tracks on either side of the climber, from above it down to the thumb zone.
    void LayoutTracks()
    {
        float h = canvasRect.rect.height;
        trackTopY = h * 0.2f;
        trackBottomY = -h * 0.32f;
        circleTopY = trackTopY - 48;
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -104 : 104;
            trackRects[i].anchoredPosition = new Vector2(x, (trackTopY + trackBottomY) / 2);
            trackRects[i].sizeDelta = new Vector2(96, trackTopY - trackBottomY);
        }
    }

    void FinishTutorial()
    {
        tutorialStep = TutorialStep.Off;
        tutorialGroup.SetActive(false);
        hintCaption.gameObject.SetActive(false);
        Progress.TutorialRuns++;
        // The hints for the same things aren't needed any more.
        if (Progress.HintCount("swipe") == 0) Progress.CountHint("swipe");
        while (Progress.HintCount("handoff") < 2) Progress.CountHint("handoff");
        Progress.Save();
        Synth.Instance.ZoneFanfare();
        Haptics.Success();
        Debris.Burst(climber.transform.position + Vector3.up * 1.2f, 0xFFD54F, 16, 3.5f);
        if (tutorialWarmUp) ShowBanner("WARMED UP!", "LET'S CLIMB");
        else if (Progress.HintCount("welcome") == 0)
        {
            // A welcome gift, so there's something to buy in the closet right away.
            Progress.CountHint("welcome");
            Progress.Coins += Tuning.WelcomeCoins;
            Progress.Save();
            RefreshShop();
            ShowBanner("YOU'RE READY!", $"+{Tuning.WelcomeCoins} COINS TO SPEND");
        }
        else ShowBanner("NICE CLIMBING!", "YOU'RE READY");
        ShowZoneStamp(ZoneIndex(anchorY + 0.01f), collect: false);
    }

    /// All fingers left the screen: back to the first step of the climbing lesson.
    void TutorialLetGo()
    {
        tutorialOopsUntil = Time.unscaledTime + 2.5f;
        Synth.Instance.Nope();
        // Fell during the sideways step: climb back up with two strokes (once left, once right), then try again.
        if (tutorialStep == TutorialStep.Side)
        {
            tutorialStrokesNeeded = 2;
            trackCircleLabels[0].text = "HOLD";
        }
        tutorialStrokesDone = 0;
        tutorialTrack = 0;
        tutorialStep = TutorialStep.Hold;
        trackTouch[0] = trackTouch[1] = -1;
        var circle = trackCircles[0];
        circle.anchoredPosition = new Vector2(trackRects[0].anchoredPosition.x, circleTopY);
        circle.gameObject.SetActive(true);
        StartCoroutine(Pop(circle, 0.2f));
    }

    /// Replay from the settings.
    void ReplayTutorial()
    {
        ShowSettings(false);
        if (phase == Phase.Title) StartTutorial();
    }

    Vector2 ToCanvas(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
        return local;
    }

    bool InTrack(int track, Vector2 point)
    {
        var rect = trackRects[track];
        var p = rect.anchoredPosition;
        var size = rect.sizeDelta;
        return Mathf.Abs(point.x - p.x) < size.x / 2 + 40 && Mathf.Abs(point.y - p.y) < size.y / 2 + 40;
    }

    bool FingerPosition(int touchId, out Vector2 point)
    {
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.touchId != touchId || touch.phase is TouchPhase.Ended or TouchPhase.Canceled) continue;
            point = ToCanvas(touch.screenPosition);
            return true;
        }
        point = default;
        return false;
    }

    // MARK: - Input

    void TutorialTouch(Touch touch)
    {
        if (tutorialStep != TutorialStep.Hold || touch.phase != TouchPhase.Began) return;
        var point = ToCanvas(touch.screenPosition);
        if (!InTrack(tutorialTrack, point)) return;
        trackTouch[tutorialTrack] = touch.touchId;
        trackTouchStartY = point.y;
        tutorialStep = TutorialStep.Swipe;
        Haptics.Tick(0.7f);
        Synth.Instance.Click();
    }

    void TutorialTouchEnded(Touch touch)
    {
        for (int i = 0; i < 2; i++)
        {
            if (trackTouch[i] != touch.touchId) continue;
            trackTouch[i] = -1;
            // Let go before the swipe was done: try that track again.
            if (tutorialStep == TutorialStep.Swipe && i == tutorialTrack) tutorialStep = TutorialStep.Hold;
        }
    }

    // MARK: - Every frame

    void UpdateTutorial()
    {
        if (!TutorialActive) return;
        if (phase != Phase.Playing)
        {
            tutorialStep = TutorialStep.Off;
            tutorialGroup.SetActive(false);
            return;
        }
        LayoutTracks();
        float pulse = 1 + 0.07f * Mathf.Sin(Time.unscaledTime * 7f);
        bool side = tutorialStep == TutorialStep.Side;
        sideCircle.gameObject.SetActive(false);
        holdTag.gameObject.SetActive(false);
        swipeArrow.gameObject.SetActive(false);

        for (int i = 0; i < 2; i++)
        {
            trackRects[i].gameObject.SetActive(!side);
            var circle = trackCircles[i];
            bool lit = i == tutorialTrack;
            bool held = FingerPosition(trackTouch[i], out var finger);
            float x = trackRects[i].anchoredPosition.x;
            if (side)
            {
                circle.gameObject.SetActive(false);
                continue;
            }
            circle.gameObject.SetActive(lit || held);
            if (held)
            {
                // The circle stays under the finger as it slides down the track.
                float y = Mathf.Clamp(finger.y, trackBottomY + 44, circleTopY);
                circle.anchoredPosition = new Vector2(x, y);
                circle.localScale = Vector3.one;
                trackCircleFaces[i].color = Ui.Blue;
                trackCircleLabels[i].color = Color.white;
            }
            else
            {
                circle.anchoredPosition = Vector2.Lerp(circle.anchoredPosition, new Vector2(x, circleTopY), Time.unscaledDeltaTime * 12);
                circle.localScale = Vector3.one * pulse;
                trackCircleFaces[i].color = Ui.Gold;
                trackCircleLabels[i].color = Ui.OnGold;
            }
        }

        switch (tutorialStep)
        {
            case TutorialStep.Hold:
                if (Time.unscaledTime < tutorialOopsUntil)
                    ShowCaption("Oops! Keep a finger on the screen,\nor you slide back down", new Vector2(0, trackTopY + 46));
                else
                    ShowCaption(tutorialStrokesDone > 0 ? "Now hold the other circle"
                        : tutorialWarmUp ? "Quick warm-up! Hold the circle" : "Hold the circle", new Vector2(0, trackTopY + 46));
                break;

            case TutorialStep.Swipe:
                ShowCaption("Swipe down and hold", new Vector2(0, trackTopY + 46));
                {
                    // The arrow bobs well below the held circle (clear of the thumb), resting at the
                    // bottom of the track, and goes away once the thumb gets close to it.
                    var held = trackCircles[tutorialTrack].anchoredPosition;
                    float bob = 14 * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
                    float arrowY = Mathf.Max(held.y - 170, trackBottomY + 50) - bob;
                    swipeArrow.gameObject.SetActive(held.y - arrowY > 120);
                    swipeArrow.anchoredPosition = new Vector2(held.x, arrowY);
                }
                // Once this finger starts swiping down, the other circle goes away (it comes back when this one is down).
                if (FingerPosition(trackTouch[tutorialTrack], out var swiping) && trackTouchStartY - swiping.y > 14)
                    trackTouch[1 - tutorialTrack] = -1;
                if (FingerPosition(trackTouch[tutorialTrack], out var point)
                    && trackTouchStartY - point.y > (trackTopY - trackBottomY) * 0.5f)
                {
                    // A full stroke: on to the other hand.
                    tutorialStrokesDone++;
                    Haptics.Success();
                    Synth.Instance.ClimbBlip(4 + tutorialStrokesDone * 3, 12);
                    if (tutorialStrokesDone >= tutorialStrokesNeeded)
                    {
                        tutorialStep = TutorialStep.Side;
                        swipedDuringHint = false;
                    }
                    else
                    {
                        // The other circle pops back in at the top of its track, ready for the next hand.
                        tutorialTrack = 1 - tutorialTrack;
                        tutorialStep = TutorialStep.Hold;
                        trackTouch[tutorialTrack] = -1;
                        var circle = trackCircles[tutorialTrack];
                        circle.anchoredPosition = new Vector2(trackRects[tutorialTrack].anchoredPosition.x, circleTopY);
                        circle.gameObject.SetActive(true);
                        StartCoroutine(Pop(circle, 0.2f));
                    }
                }
                break;

            case TutorialStep.Side:
                UpdateSideStep(pulse);
                break;
        }
    }

    /// Hold with one finger, swipe sideways with the other.
    void UpdateSideStep(float pulse)
    {
        if (swipedDuringHint)
        {
            FinishTutorial();
            return;
        }
        // The first finger that's down is the holding one.
        Vector2? holding = null;
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled) continue;
            holding = ToCanvas(touch.screenPosition);
            break;
        }
        var circle = trackCircles[0];
        circle.gameObject.SetActive(true);
        trackCircleLabels[0].text = "";   // "HOLD" goes above the finger instead
        holdTag.gameObject.SetActive(true);
        if (holding is Vector2 at)
        {
            circle.anchoredPosition = at;
            circle.localScale = Vector3.one;
            trackCircleFaces[0].color = Ui.Blue;
            trackCircleLabels[0].color = Color.white;
            holdTag.anchoredPosition = at + new Vector2(0, 66);
            // The second circle with arrows on the other side and higher up, for the other finger.
            sideCircle.gameObject.SetActive(true);
            sideCircle.anchoredPosition = new Vector2(at.x > 0 ? -104 : 104, Mathf.Min(at.y + 190, trackTopY - 50));
            sideCircle.localScale = Vector3.one * pulse;
            ShowCaption("Swipe left or right\nwith your other finger", new Vector2(0, trackTopY + 46));
        }
        else
        {
            var waiting = new Vector2(-104, (trackTopY + trackBottomY) / 2);
            circle.anchoredPosition = waiting;
            circle.localScale = Vector3.one * pulse;
            trackCircleFaces[0].color = Ui.Gold;
            trackCircleLabels[0].color = Ui.OnGold;
            holdTag.anchoredPosition = waiting + new Vector2(0, 66);
            ShowCaption("Hold with one finger", new Vector2(0, trackTopY + 46));
        }
    }
}
