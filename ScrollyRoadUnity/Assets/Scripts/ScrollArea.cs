using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// A list you drag up and down with momentum, and that stretches like a rubber band past its ends
/// (the closet grid, the leaderboard). The content hangs from the top of the viewport (pivot at the top).
public class ScrollArea
{
    public readonly RectTransform Viewport, Content;
    int touchId = -1;
    bool scrolling;
    float startY, contentStartY, velocity, lastY;

    public ScrollArea(RectTransform viewport, RectTransform content)
    {
        Viewport = viewport;
        Content = content;
    }

    float Max => Mathf.Max(0, Content.sizeDelta.y - Viewport.rect.height);

    static float RubberBand(float distance) => (1 - 1 / (distance * 0.55f / 120 + 1)) * 120;

    /// Back to the top, standing still.
    public void Reset()
    {
        Content.anchoredPosition = Vector2.zero;
        velocity = 0;
    }

    /// A finger on the list: once it moves enough it scrolls (and the button it started on isn't tapped).
    /// `scale` is the canvas scale, to turn screen pixels into UI points.
    public void Drag(Touch touch, float scale)
    {
        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(Viewport, touch.screenPosition, null)) return;
            touchId = touch.touchId;
            scrolling = false;
            startY = lastY = touch.screenPosition.y;
            contentStartY = Content.anchoredPosition.y;
            velocity = 0;
            return;
        }
        if (touch.touchId != touchId) return;
        float moved = (touch.screenPosition.y - startY) / scale;
        if (!scrolling && Mathf.Abs(moved) > 12) scrolling = true;
        if (!scrolling) return;
        float max = Max;
        float y = contentStartY + moved;
        if (y < 0) y = -RubberBand(-y);
        else if (y > max) y = max + RubberBand(y - max);
        Content.anchoredPosition = new Vector2(0, y);
        velocity = (touch.screenPosition.y - lastY) / scale / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        lastY = touch.screenPosition.y;
    }

    /// The finger lifted: was it a scroll (then no tap)?
    public bool End(Touch touch)
    {
        if (touch.touchId != touchId) return false;
        touchId = -1;
        bool scrolled = scrolling;
        scrolling = false;
        return scrolled;
    }

    /// Glides on after a flick and springs back inside the ends.
    public void Update(float dt)
    {
        if (touchId != -1 && scrolling) return;
        float y = Content.anchoredPosition.y;
        float max = Max;
        if (y < 0 || y > max)
        {
            velocity = 0;
            y = Mathf.Lerp(y, Mathf.Clamp(y, 0, max), 1 - Mathf.Exp(-14 * dt));
        }
        else
        {
            y += velocity * dt;
            velocity *= Mathf.Exp(-4 * dt);
        }
        Content.anchoredPosition = new Vector2(0, y);
    }
}
