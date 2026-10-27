using System.Collections.Generic;
using UnityEngine;

/// Draws tags. A tag is a gradient with a pattern tiled over it in one color, and maybe light rays.
/// Big tags (the closet stage, later the leaderboard banner) are built from layers: the gradient
/// stretched, the pattern tiled and drifting. Thumbnails are drawn into one small picture.
public static class TagArt
{
    const int TileSize = 128;
    static readonly Dictionary<string, Texture2D> patterns = new();
    static readonly Dictionary<string, Texture2D> gradients = new();

    /// The gradient as a tall thin texture, to stretch over any size.
    public static Texture2D Gradient(uint[] colors)
    {
        string key = string.Join(",", colors);
        if (gradients.TryGetValue(key, out var cached)) return cached;
        const int height = 64;
        var texture = new Texture2D(2, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < height; y++)
        {
            var color = GradientAt(colors, 1 - (y + 0.5f) / height);
            texture.SetPixel(0, y, color);
            texture.SetPixel(1, y, color);
        }
        texture.Apply();
        return gradients[key] = texture;
    }

    /// The color at t (0 = top, 1 = bottom).
    static Color GradientAt(uint[] colors, float t)
    {
        if (colors.Length == 1) return Voxel.Hex(colors[0]);
        float f = Mathf.Clamp01(t) * (colors.Length - 1);
        int i = Mathf.Min((int)f, colors.Length - 2);
        return Color.Lerp(Voxel.Hex(colors[i]), Voxel.Hex(colors[i + 1]), f - i);
    }

    /// One tile of a pattern: white with the shapes in alpha, made to repeat. Null for no pattern.
    public static Texture2D Pattern(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (patterns.TryGetValue(name, out var cached)) return cached;
        var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
        var pixels = new Color[TileSize * TileSize];
        for (int py = 0; py < TileSize; py++)
            for (int px = 0; px < TileSize; px++)
            {
                // 3×3 supersampling for smooth edges.
                float a = 0;
                for (int s = 0; s < 9; s++)
                    a += Inside(name, (px + (s % 3 + 0.5f) / 3f) / TileSize, (py + (s / 3 + 0.5f) / 3f) / TileSize) ? 1 : 0;
                pixels[py * TileSize + px] = new Color(1, 1, 1, a / 9f);
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return patterns[name] = texture;
    }

    // MARK: - The patterns (u, v in 0..1 across one tile, v up)

    static bool Inside(string pattern, float u, float v) => pattern switch
    {
        "dots" => Near(u, v, 0.25f, 0.25f) < 0.12f || Near(u, v, 0.75f, 0.75f) < 0.12f,
        "stripes" => Frac((u + v) * 2) < 0.42f,
        "checker" => ((int)(u * 2) + (int)(v * 2)) % 2 == 0,
        "waves" => Mathf.Abs(Frac(v * 2) - 0.5f - 0.14f * Mathf.Sin(u * Mathf.PI * 2)) < 0.09f,
        "zigzag" => Mathf.Abs(Frac(v * 2) - 0.3f - 0.4f * Mathf.Abs(Frac(u * 2) - 0.5f) * 2) < 0.1f,
        "stars" => Star(u, v, 0.27f, 0.3f, 0.15f) || Star(u, v, 0.76f, 0.74f, 0.09f)
                   || Near(u, v, 0.72f, 0.2f) < 0.025f || Near(u, v, 0.2f, 0.8f) < 0.02f || Near(u, v, 0.5f, 0.55f) < 0.015f,
        "hearts" => Heart(u, v, 0.27f, 0.3f, 0.14f) || Heart(u, v, 0.76f, 0.76f, 0.1f),
        "flowers" => Flower(u, v, 0.28f, 0.3f, 0.16f) || Flower(u, v, 0.76f, 0.72f, 0.1f) || Petal(u, v, 0.72f, 0.2f) || Petal(u, v, 0.18f, 0.82f),
        "clouds" => Cloud(u, v, 0.32f, 0.62f, 1f) || Cloud(u, v, 0.78f, 0.2f, 0.65f),
        "bubbles" => Mathf.Abs(Near(u, v, 0.27f, 0.3f) - 0.13f) < 0.025f || Mathf.Abs(Near(u, v, 0.74f, 0.72f) - 0.08f) < 0.02f
                     || Near(u, v, 0.76f, 0.2f) < 0.04f || Near(u, v, 0.2f, 0.8f) < 0.025f,
        "plaid" => Frac(u) < 0.18f || Frac(v) < 0.18f || Mathf.Abs(Frac(u) - 0.6f) < 0.018f || Mathf.Abs(Frac(v) - 0.6f) < 0.018f,
        "paws" => Paw(u, v, 0.28f, 0.3f, 1f) || Paw(u, v, 0.74f, 0.74f, 0.75f),
        "stitches" => (Mathf.Abs(Frac(v) - 0.5f) < 0.022f && Frac(u * 5) < 0.55f) || (Mathf.Abs(Frac(u) - 0.5f) < 0.022f && Frac(v * 5) < 0.55f),
        "leaves" => Leaf(u, v, 0.28f, 0.3f, 0.34f, 40) || Leaf(u, v, 0.75f, 0.74f, 0.26f, -30) || Leaf(u, v, 0.78f, 0.22f, 0.2f, 110),
        "chips" => Chip(u, v, 0.15f, 0.2f, 20) || Chip(u, v, 0.55f, 0.12f, -40) || Chip(u, v, 0.85f, 0.45f, 70)
                   || Chip(u, v, 0.35f, 0.6f, -10) || Chip(u, v, 0.7f, 0.85f, 35) || Chip(u, v, 0.1f, 0.88f, 80),
        "seigaiha" => Seigaiha(u, v),
        "snowflakes" => Snowflake(u, v, 0.28f, 0.3f, 0.15f) || Snowflake(u, v, 0.75f, 0.74f, 0.1f),
        "diamonds" => Mathf.Abs(Frac(u) - 0.5f) + Mathf.Abs(Frac(v) - 0.5f) < 0.36f
                      || Mathf.Abs(Frac(u + v) - 0.5f) < 0.012f || Mathf.Abs(Frac(u - v) - 0.5f) < 0.012f,
        "flames" => Flame(u, v, 0.27f, 0.22f, 0.22f) || Flame(u, v, 0.76f, 0.66f, 0.16f),
        "coins" => Coin(u, v, 0.27f, 0.3f, 0.13f) || Coin(u, v, 0.75f, 0.74f, 0.09f),
        "bolts" => Bolt(u, v, 0.3f, 0.3f, 1f) || Bolt(u, v, 0.76f, 0.78f, 0.6f),
        _ => false,
    };

    static float Frac(float x) => x - Mathf.Floor(x);

    /// The offset to (cx, cy), the short way round the repeating tile.
    static Vector2 Offset(float u, float v, float cx, float cy)
    {
        float dx = u - cx, dy = v - cy;
        return new Vector2(dx - Mathf.Round(dx), dy - Mathf.Round(dy));
    }

    static float Near(float u, float v, float cx, float cy) => Offset(u, v, cx, cy).magnitude;

    /// A soft five-pointed star, one point up.
    static bool Star(float u, float v, float cx, float cy, float radius)
    {
        var d = Offset(u, v, cx, cy);
        float angle = Mathf.Atan2(d.y, d.x) - Mathf.PI / 2;
        float edge = radius * (0.48f + 0.52f * Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 2.5f)), 2.2f));
        return d.magnitude < edge;
    }

    static bool Heart(float u, float v, float cx, float cy, float size)
    {
        var d = Offset(u, v, cx, cy) / size;
        float x = d.x * 1.15f, y = d.y * 1.15f + 0.2f;
        float a = x * x + y * y - 1;
        return a * a * a - x * x * y * y * y <= 0;
    }

    /// A cherry blossom: five round petals, an open middle.
    static bool Flower(float u, float v, float cx, float cy, float radius)
    {
        var d = Offset(u, v, cx, cy);
        float r = d.magnitude;
        float angle = Mathf.Atan2(d.y, d.x) + 0.3f;
        float edge = radius * (0.55f + 0.45f * Mathf.Abs(Mathf.Cos(angle * 2.5f)));
        return r < edge && r > radius * 0.2f;
    }

    static bool Petal(float u, float v, float cx, float cy)
    {
        var d = Offset(u, v, cx, cy);
        var p = new Vector2(d.x * 0.8f + d.y * 0.6f, -d.x * 0.6f + d.y * 0.8f);   // tilted
        return p.x * p.x / (0.045f * 0.045f) + p.y * p.y / (0.028f * 0.028f) < 1;
    }

    /// A puffy cloud: three bumps on a flat bottom.
    static bool Cloud(float u, float v, float cx, float cy, float scale)
    {
        var d = Offset(u, v, cx, cy) / scale;
        if (d.y < -0.06f) return false;
        return (d - new Vector2(-0.11f, 0)).magnitude < 0.08f || (d - new Vector2(0, 0.04f)).magnitude < 0.11f
            || (d - new Vector2(0.11f, 0)).magnitude < 0.08f || (Mathf.Abs(d.x) < 0.11f && d.y < 0.02f);
    }

    static bool Paw(float u, float v, float cx, float cy, float scale)
    {
        var d = Offset(u, v, cx, cy) / scale;
        if (new Vector2(d.x / 1.2f, d.y + 0.03f).magnitude < 0.075f) return true;
        foreach (var toe in new[] { new Vector2(-0.09f, 0.06f), new Vector2(-0.035f, 0.11f), new Vector2(0.035f, 0.11f), new Vector2(0.09f, 0.06f) })
            if ((d - toe).magnitude < 0.033f) return true;
        return false;
    }

    static bool Leaf(float u, float v, float cx, float cy, float length, float angle)
    {
        var d = Offset(u, v, cx, cy);
        float a = angle * Mathf.Deg2Rad;
        var p = new Vector2(d.x * Mathf.Cos(a) + d.y * Mathf.Sin(a), -d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a)) / length;
        return p.x * p.x + p.y * p.y * 9 < 0.25f && Mathf.Abs(p.y) > 0.025f;
    }

    static bool Chip(float u, float v, float cx, float cy, float angle)
    {
        var d = Offset(u, v, cx, cy);
        float a = angle * Mathf.Deg2Rad;
        var p = new Vector2(d.x * Mathf.Cos(a) + d.y * Mathf.Sin(a), -d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a));
        return Mathf.Abs(p.x) < 0.06f && Mathf.Abs(p.y) < 0.025f;
    }

    /// Overlapping fans of rings, the lower row on top: the Japanese wave pattern.
    static bool Seigaiha(float u, float v)
    {
        const float radius = 0.25f, step = radius / 2;
        int first = Mathf.FloorToInt((v - radius) / step), last = Mathf.CeilToInt((v + radius) / step);
        for (int k = first; k <= last; k++)
        {
            float cy = k * step;
            float offset = ((k % 2) + 2) % 2 * radius;
            float cx = Mathf.Round((u - offset) / (2 * radius)) * 2 * radius + offset;
            float dist = new Vector2(u - cx, v - cy).magnitude;
            if (dist < radius && v >= cy - 0.001f) return Frac(dist / (radius / 4)) < 0.42f;
        }
        return false;
    }

    static bool Snowflake(float u, float v, float cx, float cy, float radius)
    {
        var d = Offset(u, v, cx, cy);
        if (d.magnitude > radius) return false;
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI / 3;
            if (Mathf.Abs(d.x * Mathf.Sin(a) - d.y * Mathf.Cos(a)) < radius * 0.09f) return true;
        }
        float r = d.magnitude, angle = Mathf.Repeat(Mathf.Atan2(d.y, d.x), Mathf.PI / 3) - Mathf.PI / 6;
        return r > radius * 0.55f && r < radius * 0.75f && Mathf.Abs(angle) < 0.35f && Mathf.Abs(angle) > 0.12f;
    }

    /// A teardrop flame, pointing up.
    static bool Flame(float u, float v, float cx, float cy, float size)
    {
        var d = Offset(u, v, cx, cy) / size;
        if (d.magnitude < 0.5f) return true;
        if (d.y < 0 || d.y > 1.1f) return false;
        float half = 0.5f * Mathf.Pow(1 - d.y / 1.1f, 0.8f);
        return Mathf.Abs(d.x - 0.12f * Mathf.Sin(d.y * 4)) < half;
    }

    static bool Coin(float u, float v, float cx, float cy, float radius)
    {
        float r = Near(u, v, cx, cy);
        return r < radius && Mathf.Abs(r - radius * 0.72f) > radius * 0.08f;
    }

    static bool Bolt(float u, float v, float cx, float cy, float scale)
    {
        var d = Offset(u, v, cx, cy) / scale;
        var points = new[] { new Vector2(0.05f, 0.2f), new Vector2(-0.05f, 0.0f), new Vector2(0.05f, 0.0f), new Vector2(-0.05f, -0.2f) };
        for (int i = 0; i < points.Length - 1; i++)
        {
            var a = points[i];
            var b = points[i + 1];
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(d - a, ab) / ab.sqrMagnitude);
            if ((d - (a + ab * t)).magnitude < 0.03f) return true;
        }
        return false;
    }

    // MARK: - Thumbnails

    /// The whole tag drawn into one square picture with rounded corners, as if shown at `points` UI points.
    public static Texture2D Thumbnail(TagStyle tag, int size, float points)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pattern = Pattern(tag.Pattern);
        var ink = Voxel.Hex(tag.Ink);
        float tiles = points / tag.Tile;
        float radius = size * 0.2f;
        var pixels = new Color[size * size];
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float u = (px + 0.5f) / size, v = (py + 0.5f) / size;
                var color = GradientAt(tag.Colors, 1 - v);
                if (tag.Rays)
                {
                    float x = u * 2 - 1, y = v * 2 - 1;
                    float ray = Mathf.Sin(Mathf.Atan2(y, x) * 10f) > 0.25f ? 1 : 0;
                    color = Color.Lerp(color, ink, ray * Mathf.Clamp01(1.3f - Mathf.Sqrt(x * x + y * y) * 0.6f) * tag.InkAlpha);
                }
                if (pattern != null)
                    color = Color.Lerp(color, ink, pattern.GetPixelBilinear((u - 0.5f) * tiles, (v - 0.5f) * tiles).a * tag.InkAlpha);

                // Rounded corners.
                float qx = Mathf.Abs(px + 0.5f - size / 2f) - (size / 2f - radius);
                float qy = Mathf.Abs(py + 0.5f - size / 2f) - (size / 2f - radius);
                float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude - radius;
                color.a = Mathf.Clamp01(0.5f - outside);
                pixels[py * size + px] = color;
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
