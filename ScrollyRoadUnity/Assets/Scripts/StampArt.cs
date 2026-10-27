using System.Collections.Generic;
using UnityEngine;

/// The look of every stamp: each planet type has its own stamp shape, and each zone its own
/// icon (an SVG drawing). Pages: 0 Earth, 1 hot, 2 icy, 3 low gravity, 4 heavy gravity, 5 boss.
public static class StampArt
{
    static readonly Dictionary<int, Sprite> faces = new(), rings = new(), icons = new();

    /// Whether (x, y) in -1..1 lies inside this page's stamp shape.
    static bool Inside(int page, float x, float y)
    {
        float r = Mathf.Sqrt(x * x + y * y);
        float angle = Mathf.Atan2(y, x);
        return page switch
        {
            1 => r <= 0.9f + 0.1f * Mathf.Cos(angle * 12),                                          // sunburst
            2 => Mathf.Abs(x) <= 0.866f && Mathf.Abs(y) + 0.5774f * Mathf.Abs(x) <= 1f,             // hexagon, like an ice crystal
            3 => r <= 0.84f + 0.16f * Mathf.Abs(Mathf.Cos(angle * 3)),                            // flower
            4 => x * x * x * x + y * y * y * y <= 0.92f,                                         // chunky rounded square
            5 => InShield(x, y),                                                                 // a crest, for bosses
            _ => r <= 1f,                                                                        // circle
        };
    }

    /// A shield: rounded top corners, straight sides, coming to a point at the bottom.
    static bool InShield(float x, float y)
    {
        float ax = Mathf.Abs(x);
        if (y > 0.9f || y < -0.98f || ax > 0.84f) return false;
        if (ax > 0.6f && y > 0.66f) return (ax - 0.6f) * (ax - 0.6f) + (y - 0.66f) * (y - 0.66f) <= 0.24f * 0.24f;
        if (y < -0.1f) return ax <= 0.84f * (y + 0.98f) / 0.88f;
        return true;
    }

    public static Sprite Face(int page) => faces.TryGetValue(page, out var sprite) ? sprite : faces[page] = MakeFace(page);

    static Sprite MakeFace(int page) => Ui.MakeShape(128, (x, y) => Inside(page, x, y) ? 1 : 0);

    /// A thin outline a little inside the shape's edge, like the rim of a rubber stamp.
    public static Sprite Ring(int page) => rings.TryGetValue(page, out var sprite) ? sprite : rings[page] = MakeRing(page);

    static Sprite MakeRing(int page) =>
        Ui.MakeShape(128, (x, y) => Inside(page, x / 0.88f, y / 0.88f) && !Inside(page, x / 0.77f, y / 0.77f) ? 1 : 0);

    /// The zone's icon: a white SVG drawing from Resources/Stamps, named "<page>_<zone>".
    public static Sprite Icon(int page, int index)
    {
        int key = page * 16 + index;
        if (!icons.TryGetValue(key, out var sprite))
            icons[key] = sprite = Resources.Load<Sprite>($"Stamps/{page}_{index}");
        return sprite;
    }
}
