using UnityEngine;
using UnityEngine.UI;

/// The Animal Crossing-inspired UI kit: cream cards with brown text, blue pills for currency,
/// gold speech bubbles, round stamps, and a rounded heavy font. Shapes are drawn from code.
public static class Ui
{
    public static readonly Color Cream = Voxel.Hex(0xFBF5E1);
    public static readonly Color CreamShade = Voxel.Hex(0xE2D3AC);
    public static readonly Color Brown = Voxel.Hex(0x725D42);
    public static readonly Color BrownSoft = Voxel.Hex(0xA48F6C);
    public static readonly Color Blue = Voxel.Hex(0x4C8FE8);
    public static readonly Color BlueShade = Voxel.Hex(0x3468B8);
    public static readonly Color Gold = Voxel.Hex(0xE2B24C);
    public static readonly Color GoldShade = Voxel.Hex(0xB98A2B);
    public static readonly Color Grey = Voxel.Hex(0xC9C1AE);
    public static readonly Color GreyShade = Voxel.Hex(0xA69D88);

    public static Font Font { get; private set; }
    static Sprite rounded, circle, ring, heart, coin;
    const int RoundedRadius = 32;

    public static void Init(Font font)
    {
        Font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rounded = MakeRounded(96, RoundedRadius);
        circle = MakeShape(128, (x, y) => Disc(x, y, 1f));
        ring = MakeShape(128, (x, y) => Mathf.Min(Disc(x, y, 1f), 1 - Disc(x, y, 0.84f)));
        // The coin and heart are SVG drawings; the shapes drawn in code are a fallback.
        heart = Resources.Load<Sprite>("Icons/heart") ?? MakeShape(96, Heart);
        coin = Resources.Load<Sprite>("Icons/coin") ?? MakeCoin(96);
    }

    // MARK: - Shapes

    public delegate float Coverage(float x, float y);   // x, y in -1..1, returns 0..1

    public static Sprite MakeShape(int size, Coverage coverage, Color? tint = null)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                // 4×4 supersampling for smooth edges.
                float a = 0;
                for (int s = 0; s < 16; s++)
                {
                    float x = (px + (s % 4 + 0.5f) / 4f) / size * 2 - 1;
                    float y = (py + (s / 4 + 0.5f) / 4f) / size * 2 - 1;
                    a += coverage(x, y);
                }
                pixels[py * size + px] = new Color(1, 1, 1, a / 16f);
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    static float Disc(float x, float y, float radius) => x * x + y * y <= radius * radius ? 1 : 0;

    static float Heart(float x, float y)
    {
        x *= 1.25f;
        y = y * 1.25f + 0.15f;
        float a = x * x + y * y - 1;
        return a * a * a - x * x * y * y * y <= 0 ? 1 : 0;
    }

    static Sprite MakeRounded(int size, int radius)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        float half = size / 2f;
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float qx = Mathf.Abs(px + 0.5f - half) - (half - radius);
                float qy = Mathf.Abs(py + 0.5f - half) - (half - radius);
                float distance = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                pixels[py * size + px] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance));
            }
        texture.SetPixels(pixels);
        texture.Apply();
        var border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
    }

    static Sprite MakeCoin(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        var rim = Voxel.Hex(0xD9961F);
        var face = Voxel.Hex(0xFFC83D);
        var shine = Voxel.Hex(0xFFF1B0);
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * 2 - 1;
                float y = (py + 0.5f) / size * 2 - 1;
                float r = Mathf.Sqrt(x * x + y * y);
                float alpha = Mathf.Clamp01((1 - r) * size / 2f);
                var c = r > 0.78f ? rim : face;
                if (Mathf.Abs(x + 0.18f) < 0.1f && Mathf.Abs(y - 0.05f) < 0.38f) c = shine;   // a little shine stripe
                c.a = alpha;
                pixels[py * size + px] = c;
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    // MARK: - Building blocks

    public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool sliced, float radius = RoundedRadius)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sliced)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = RoundedRadius / Mathf.Max(1f, radius);
        }
        return image;
    }

    /// A rounded box with a darker "lip" underneath, the chunky Animal Crossing look.
    /// Returns the face image; its parent rect is the box.
    public static Image Box(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color face, Color shade, float radius, float lip = 4)
    {
        var box = Rect(parent, "Box", anchor, position, size);
        var under = Rect(box, "Lip", new Vector2(0.5f, 0.5f), new Vector2(0, -lip), size);
        Stretch(under);
        under.offsetMin = new Vector2(0, -lip);
        under.offsetMax = new Vector2(0, -lip);
        AddImage(under, rounded, shade, true, radius);
        var top = Rect(box, "Face", new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Stretch(top);
        return AddImage(top, rounded, face, true, radius);
    }

    /// A pill: a box whose corners are fully round.
    public static Image Pill(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color face, Color shade, float lip = 3) =>
        Box(parent, anchor, position, size, face, shade, size.y / 2f, lip);

    public static Image Circle(Transform parent, Vector2 anchor, Vector2 position, float diameter, Color color)
    {
        var rect = Rect(parent, "Circle", anchor, position, new Vector2(diameter, diameter));
        return AddImage(rect, circle, color, false);
    }

    public static Image Ring(Transform parent, Vector2 anchor, Vector2 position, float diameter, Color color)
    {
        var rect = Rect(parent, "Ring", anchor, position, new Vector2(diameter, diameter));
        return AddImage(rect, ring, color, false);
    }

    public static Sprite CircleSprite => circle;

    /// A plain rounded rectangle (no lip), for masks and frames.
    public static Image Rounded(RectTransform rect, Color color, float radius) => AddImage(rect, rounded, color, true, radius);
    public static Sprite RingSprite => ring;

    static readonly System.Collections.Generic.Dictionary<string, Sprite> icons = new();

    /// A white SVG icon from Resources/Icons (tint it with the image color).
    public static Sprite Icon(string name)
    {
        if (!icons.TryGetValue(name, out var sprite)) icons[name] = sprite = Resources.Load<Sprite>("Icons/" + name);
        return sprite;
    }

    /// An image showing a sprite as is (stamp shapes and icons).
    public static Image Picture(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Sprite sprite, Color color)
    {
        var rect = Rect(parent, "Picture", anchor, position, size);
        var image = AddImage(rect, sprite, color, false);
        image.enabled = sprite != null;   // a missing sprite would show as a plain square
        return image;
    }

    public static Image Heart(Transform parent, Vector2 anchor, Vector2 position, float size)
    {
        var rect = Rect(parent, "Heart", anchor, position, new Vector2(size, size));
        return AddImage(rect, heart, Color.white, false);
    }

    public static Image Coin(Transform parent, Vector2 anchor, Vector2 position, float size)
    {
        var rect = Rect(parent, "Coin", anchor, position, new Vector2(size, size));
        return AddImage(rect, coin, Color.white, false);
    }

    public static Text Label(Transform parent, string text, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool shadow = false)
    {
        var rect = Rect(parent, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(rect);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = Font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.text = text;
        label.supportRichText = true;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        if (shadow)
        {
            var s = rect.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.25f);
            s.effectDistance = new Vector2(0, -2);
        }
        return label;
    }
}
