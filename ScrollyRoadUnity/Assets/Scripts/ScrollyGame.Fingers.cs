using System.Collections.Generic;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// Climbing is hand over hand. A finger that stays put while the other finger makes a whole stroke
/// has to climb next: it may sit out only one stroke. Otherwise you could hold one thumb still forever
/// and keep swiping with the other. Pulling with the wrong finger does nothing (the climber just holds)
/// and gets a "nope" buzz, plus a hint the first few times.
public partial class ScrollyGame
{
    class FingerState
    {
        public float Drove;                // meters this finger has pulled
        public float FirstDroveAt = -1;    // when this finger's stroke began
        public float LastDroveAt = -1;
        public int SatOut;                 // whole strokes by another finger while this one stayed put
        public float BlockedPull;          // pulling while it wasn't this finger's turn
        public bool Warned;
    }

    readonly Dictionary<int, FingerState> fingers = new();

    FingerState Finger(int id)
    {
        if (!fingers.TryGetValue(id, out var state)) fingers[id] = state = new FingerState();
        return state;
    }

    /// Does another finger still owe a stroke? Then this one can't pull.
    bool OtherFingerMustClimb(int id)
    {
        foreach (var pair in fingers)
            if (pair.Key != id && pair.Value.SatOut > 0) return true;
        return false;
    }

    /// The pull this finger may add this frame (0 when it isn't its turn).
    float AllowedPull(Touch touch, float dy)
    {
        var state = Finger(touch.touchId);
        if (dy <= 0 || phase != Phase.Playing || !OtherFingerMustClimb(touch.touchId)) return dy;
        state.BlockedPull += dy;
        if (!state.Warned && state.BlockedPull > 1.2f)
        {
            state.Warned = true;
            Synth.Instance.Nope();
            Haptics.Slip();
            if (Progress.HintCount("turn") < 3)
            {
                Progress.CountHint("turn");
                ShowTip($"Climb with your other {Digit}!\nHand over hand.", 2.5f);
            }
        }
        return 0;
    }

    /// The finger that drove the climb this frame: its stroke is going, so it no longer owes one.
    void FingerDrove(int id, float pull)
    {
        if (pull <= 0.01f) return;
        var state = Finger(id);
        if (state.FirstDroveAt < 0) state.FirstDroveAt = Time.time;
        state.LastDroveAt = Time.time;
        state.Drove += pull;
        state.SatOut = 0;
    }

    /// A finger lifted. If it made a real stroke, every finger that stayed put during it owes the next one.
    void FingerLifted(Touch touch)
    {
        int id = touch.touchId;
        if (!fingers.TryGetValue(id, out var lifted)) return;
        fingers.Remove(id);
        // Alpha: note the stroke for the stroke log (playtests: how do people really hold the phone?).
        if (Progress.StrokeLogOn && lifted.Drove >= 0.3f && phase is Phase.Playing or Phase.Falling)
            StrokeLog.Add(new StrokeLog.Stroke
            {
                Start = touch.startScreenPosition.y / Mathf.Max(1, Screen.height),
                Length = (touch.startScreenPosition.y - touch.screenPosition.y) / Mathf.Max(1, Screen.height),
                Meters = lifted.Drove,
                Feel = Tuning.Feel,
            });
        if (lifted.Drove < 1f) return;
        foreach (var other in fingers.Values)
            if (other.LastDroveAt < lifted.FirstDroveAt) other.SatOut++;
    }
}
