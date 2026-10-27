using System.Collections.Generic;
using UnityEngine;

// Charms hang from the hook: a short chain (down to y = -0.085), then the charm, about 0.15 big, centered
// near y = -0.16. +z is the side you see (it faces away from the back). Japanese style: kawaii to ninja.
public static partial class Cosmetics
{
    static Cosmetic NewCharm(string id, string name, int rarity, int price, string notes, System.Action<Transform> build) =>
        Cand(CosmeticKind.Charm, id, name, rarity, price, notes, build: t => { Chain(t); build(t); });

    /// A tiny kawaii face: dot eyes and pink cheeks, on the front at depth z.
    static void Face(Transform t, float y, float z, float spread = 0.025f, bool blush = true)
    {
        Both(s => Box(t, 0.016f, 0.02f, 0.01f, 0x222222, s * spread, y, z));
        if (blush) Both(s => Box(t, 0.022f, 0.012f, 0.01f, 0xF8A5C2, s * (spread + 0.024f), y - 0.02f, z));
    }

    /// A four-bladed throwing star, flat, facing +z.
    static void Shuriken(Transform t, float size, uint blade, uint core)
    {
        var star = Pivot(t, 0, -0.165f, 0);
        Bx(star, size * 0.38f, size * 0.38f, 0.02f, core, 0, 0, 0, rz: 45);
        for (int i = 0; i < 4; i++)
        {
            var dir = Quaternion.Euler(0, 0, i * 90f) * Vector3.up;
            Bx(star, size * 0.22f, size * 0.55f, 0.014f, blade, dir.x * size * 0.3f, dir.y * size * 0.3f, 0, rz: i * 90f + 20);
        }
        Box(star, size * 0.12f, size * 0.12f, 0.024f, 0x263238, 0, 0, 0);
    }

    static void LuckyCat(Transform t, uint body, uint accent)
    {
        Box(t, 0.12f, 0.1f, 0.1f, body, 0, -0.125f, 0);
        Both(s => Box(t, 0.03f, 0.03f, 0.03f, body, s * 0.04f, -0.065f, 0));
        Face(t, -0.12f, 0.055f, 0.025f, false);
        Box(t, 0.12f, 0.025f, 0.1f, 0xE53935, 0, -0.18f, 0);
        Box(t, 0.03f, 0.03f, 0.02f, accent, 0, -0.18f, 0.055f);
        Box(t, 0.11f, 0.08f, 0.09f, body, 0, -0.23f, 0);
        Box(t, 0.035f, 0.07f, 0.035f, body, 0.07f, -0.17f, 0.01f);
    }

    static readonly List<Cosmetic> CharmCandidates = new()
    {
        // Common: 40 to 80, kawaii food and good luck
        NewCharm("sakuramochi", "Sakura mochi", 0, 40, "Pink mochi in a leaf, with a smile", t =>
        {
            Box(t, 0.11f, 0.08f, 0.08f, 0xF8BBD0, 0, -0.14f, 0);
            Box(t, 0.12f, 0.045f, 0.085f, 0x7CB342, 0, -0.185f, 0);
            Face(t, -0.13f, 0.045f);
        }),
        NewCharm("taiyaki", "Taiyaki", 0, 50, "A fish-shaped cake, golden brown", t =>
        {
            Box(t, 0.13f, 0.09f, 0.04f, 0xD99A3E, -0.01f, -0.15f, 0);
            Box(t, 0.045f, 0.09f, 0.035f, 0xD99A3E, 0.075f, -0.15f, 0, 20);
            Box(t, 0.02f, 0.02f, 0.01f, 0x3E2723, -0.045f, -0.135f, 0.024f);
            foreach (float x in new[] { -0.005f, 0.025f }) Box(t, 0.012f, 0.05f, 0.01f, 0xB5763A, x, -0.15f, 0.024f);
        }),
        NewCharm("ebifry", "Ebi fry", 0, 50, "A crispy fried shrimp", t =>
        {
            Box(t, 0.06f, 0.14f, 0.05f, 0xE0A040, 0, -0.165f, 0);
            foreach (float y in new[] { -0.12f, -0.16f, -0.2f }) Box(t, 0.065f, 0.02f, 0.055f, 0xC98A2B, 0, y, 0);
            Box(t, 0.05f, 0.03f, 0.03f, 0xE53935, 0, -0.25f, 0);
            Both(s => Box(t, 0.03f, 0.04f, 0.03f, 0xE53935, s * 0.022f, -0.275f, 0, s * 25));
        }),
        NewCharm("tamago", "Tamago sushi", 0, 40, "Sweet egg on rice, with a nori belt", t =>
        {
            Box(t, 0.12f, 0.05f, 0.07f, 0xFFFFFF, 0, -0.185f, 0);
            Box(t, 0.13f, 0.05f, 0.08f, 0xFFD54F, 0, -0.14f, 0);
            Box(t, 0.03f, 0.11f, 0.085f, 0x1F2A1F, 0.03f, -0.16f, 0);
            Face(t, -0.135f, 0.045f, 0.02f);
        }),
        NewCharm("salmon", "Salmon nigiri", 0, 40, "A happy salmon sushi", t =>
        {
            Box(t, 0.12f, 0.05f, 0.07f, 0xFFFFFF, 0, -0.18f, 0);
            Box(t, 0.13f, 0.04f, 0.08f, 0xFF8A65, 0, -0.14f, 0);
            foreach (float x in new[] { -0.03f, 0.01f, 0.05f }) Box(t, 0.012f, 0.041f, 0.081f, 0xFFCCBC, x, -0.14f, 0, 25);
            Face(t, -0.18f, 0.04f, 0.02f);
        }),
        NewCharm("maki", "Maki roll", 0, 40, "A cucumber roll", t =>
        {
            Box(t, 0.11f, 0.11f, 0.06f, 0x1F2A1F, 0, -0.15f, 0);
            Box(t, 0.09f, 0.09f, 0.01f, 0xFFFFFF, 0, -0.15f, 0.03f);
            Box(t, 0.035f, 0.035f, 0.01f, 0x66BB6A, 0, -0.15f, 0.037f);
        }),
        NewCharm("mochibun", "Mochi bunny", 0, 50, "A squishy white mochi with bunny ears", t =>
        {
            Box(t, 0.12f, 0.08f, 0.09f, 0xFFFFFF, 0, -0.17f, 0);
            Box(t, 0.1f, 0.03f, 0.08f, 0xFFFFFF, 0, -0.125f, 0);
            Both(s => { Box(t, 0.025f, 0.05f, 0.02f, 0xFFFFFF, s * 0.03f, -0.095f, 0); Box(t, 0.012f, 0.03f, 0.01f, 0xF8BBD0, s * 0.03f, -0.095f, 0.012f); });
            Face(t, -0.16f, 0.05f);
        }),
        NewCharm("daifuku", "Strawberry daifuku", 0, 50, "Soft mochi with a strawberry peeking out", t =>
        {
            Box(t, 0.11f, 0.1f, 0.09f, 0xFFF4F4, 0, -0.17f, 0);
            Box(t, 0.06f, 0.04f, 0.06f, 0xE53935, 0, -0.11f, 0);
            Box(t, 0.05f, 0.015f, 0.04f, 0x43A047, 0, -0.092f, 0);
            Face(t, -0.17f, 0.05f);
        }),
        NewCharm("melonpan", "Melon pan", 0, 50, "A sweet bun with a cookie crust", t =>
        {
            Box(t, 0.13f, 0.1f, 0.06f, 0xF2C46B, 0, -0.16f, 0);
            Both(s => { Bx(t, 0.14f, 0.008f, 0.008f, 0xD9A54A, 0, -0.145f, 0.032f, rz: s * 35); Bx(t, 0.12f, 0.008f, 0.008f, 0xD9A54A, 0, -0.18f, 0.032f, rz: s * 35); });
        }),
        NewCharm("ramune", "Ramune", 0, 60, "A soda bottle with a marble in the neck", t =>
        {
            Box(t, 0.04f, 0.02f, 0.04f, 0x1E88E5, 0, -0.095f, 0);
            Box(t, 0.035f, 0.035f, 0.035f, 0xB2EBF2, 0, -0.12f, 0);
            Box(t, 0.03f, 0.03f, 0.04f, 0xE0F7FA, 0, -0.125f, 0);
            Box(t, 0.055f, 0.11f, 0.055f, 0x80DEEA, 0, -0.19f, 0);
            Box(t, 0.057f, 0.04f, 0.057f, 0x1E88E5, 0, -0.19f, 0);
            Box(t, 0.02f, 0.02f, 0.01f, 0xFFFFFF, 0, -0.19f, 0.03f, 45);
        }),
        NewCharm("omamori", "Omamori", 0, 60, "A good luck amulet in red and gold", t =>
        {
            Box(t, 0.08f, 0.12f, 0.03f, 0xD32F2F, 0, -0.165f, 0);
            Box(t, 0.08f, 0.02f, 0.03f, 0xD32F2F, 0, -0.1f, 0, 0);
            foreach (float y in new[] { -0.14f, -0.19f }) Box(t, 0.05f, 0.015f, 0.01f, 0xFFC83D, 0, y, 0.018f);
            Box(t, 0.04f, 0.03f, 0.035f, 0xFFC83D, 0, -0.095f, 0);
        }),
        NewCharm("teruteru", "Teru teru bozu", 0, 50, "A little doll that wishes for sunny weather", t =>
        {
            Box(t, 0.09f, 0.08f, 0.08f, 0xFFFFFF, 0, -0.125f, 0);
            Box(t, 0.12f, 0.07f, 0.1f, 0xFFFFFF, 0, -0.195f, 0);
            Box(t, 0.095f, 0.015f, 0.085f, 0xE53935, 0, -0.16f, 0);
            Face(t, -0.12f, 0.045f);
        }),
        NewCharm("kokeshi", "Kokeshi", 0, 70, "A wooden doll with a flower kimono", t =>
        {
            Box(t, 0.08f, 0.07f, 0.08f, 0xFFE0C2, 0, -0.125f, 0);
            Box(t, 0.085f, 0.04f, 0.085f, 0x222222, 0, -0.1f, 0);
            Box(t, 0.085f, 0.07f, 0.02f, 0x222222, 0, -0.12f, -0.035f);
            Face(t, -0.13f, 0.045f, 0.018f);
            Box(t, 0.07f, 0.1f, 0.07f, 0xE53935, 0, -0.205f, 0);
            Box(t, 0.03f, 0.03f, 0.01f, 0xFFE082, 0, -0.2f, 0.038f, 45);
        }),
        NewCharm("tanuki", "Tanuki", 0, 70, "A raccoon dog with a leaf on its head", t =>
        {
            Box(t, 0.11f, 0.1f, 0.08f, 0x8D6E4F, 0, -0.16f, 0);
            Both(s => { Box(t, 0.03f, 0.03f, 0.03f, 0x4E342E, s * 0.04f, -0.105f, 0); Box(t, 0.035f, 0.025f, 0.01f, 0x4E342E, s * 0.025f, -0.15f, 0.043f); });
            Both(s => Box(t, 0.01f, 0.01f, 0.01f, 0xFFFFFF, s * 0.025f, -0.148f, 0.049f));
            Box(t, 0.06f, 0.05f, 0.01f, 0xE8D3B0, 0, -0.19f, 0.043f);
            Box(t, 0.045f, 0.015f, 0.03f, 0x43A047, 0.015f, -0.1f, 0, 20);
        }),
        NewCharm("vpet", "Virtual pet", 0, 80, "A pocket egg with a pixel pet inside", t =>
        {
            Box(t, 0.1f, 0.12f, 0.03f, 0xF06292, 0, -0.16f, 0);
            Box(t, 0.08f, 0.02f, 0.03f, 0xF06292, 0, -0.095f, 0);
            Box(t, 0.06f, 0.05f, 0.01f, 0xC5E1A5, 0, -0.145f, 0.018f);
            Box(t, 0.02f, 0.02f, 0.01f, 0x33691E, 0, -0.145f, 0.024f);
            foreach (float x in new[] { -0.025f, 0, 0.025f }) Box(t, 0.015f, 0.015f, 0.01f, 0xFFEB3B, x, -0.195f, 0.018f);
        }),
        NewCharm("koi", "Koi", 0, 60, "A white and orange koi fish", t =>
        {
            Box(t, 0.07f, 0.15f, 0.05f, 0xFFFFFF, 0, -0.16f, 0);
            Box(t, 0.04f, 0.05f, 0.01f, 0xFF7043, -0.01f, -0.13f, 0.027f);
            Box(t, 0.03f, 0.04f, 0.01f, 0xFF7043, 0.015f, -0.19f, 0.027f);
            Both(s => Box(t, 0.04f, 0.05f, 0.03f, 0xFF7043, s * 0.02f, -0.255f, 0, s * 25));
            Both(s => Box(t, 0.012f, 0.012f, 0.01f, 0x222222, s * 0.02f, -0.1f, 0.027f));
        }),
        NewCharm("uchiwa", "Uchiwa fan", 0, 60, "A round summer fan with a red sun", t =>
        {
            Box(t, 0.02f, 0.05f, 0.015f, 0x8D5A2B, 0, -0.11f, 0);
            Box(t, 0.12f, 0.11f, 0.015f, 0xFFFFFF, 0, -0.185f, 0);
            Box(t, 0.1f, 0.13f, 0.015f, 0xFFFFFF, 0, -0.185f, 0);
            Box(t, 0.05f, 0.05f, 0.01f, 0xE53935, 0, -0.185f, 0.012f);
        }),
        NewCharm("dorayaki", "Dorayaki", 0, 50, "Two pancakes with sweet bean paste", t =>
        {
            Box(t, 0.12f, 0.035f, 0.08f, 0xC98A3E, 0, -0.13f, 0);
            Box(t, 0.11f, 0.02f, 0.075f, 0x6D2F2F, 0, -0.155f, 0);
            Box(t, 0.12f, 0.035f, 0.08f, 0xC98A3E, 0, -0.18f, 0);
            Face(t, -0.18f, 0.045f, 0.022f);
        }),

        // Rare: 100 to 180, ninja gear and Japan
        NewCharm("shuriken", "Shuriken", 1, 120, "A steel ninja star", t => Shuriken(t, 0.16f, 0x90A4AE, 0x78909C)),
        NewCharm("kunai", "Kunai", 1, 120, "A ninja throwing knife with a ring", t =>
        {
            Both(s => Box(t, 0.01f, 0.03f, 0.01f, 0x78909C, s * 0.015f, -0.1f, 0));
            Box(t, 0.04f, 0.01f, 0.01f, 0x78909C, 0, -0.115f, 0);
            Box(t, 0.022f, 0.06f, 0.022f, 0xC62828, 0, -0.15f, 0);
            foreach (float y in new[] { -0.135f, -0.165f }) Box(t, 0.024f, 0.008f, 0.024f, 0x222222, 0, y, 0);
            Box(t, 0.045f, 0.09f, 0.012f, 0x90A4AE, 0, -0.22f, 0);
            Box(t, 0.032f, 0.032f, 0.012f, 0x90A4AE, 0, -0.265f, 0, 45);
        }),
        NewCharm("minininja", "Mini ninja", 1, 160, "A tiny ninja with a red headband", t =>
        {
            Box(t, 0.09f, 0.09f, 0.08f, 0x222831, 0, -0.13f, 0);
            Box(t, 0.075f, 0.025f, 0.01f, 0xF5C99B, 0, -0.125f, 0.042f);
            Both(s => Box(t, 0.012f, 0.015f, 0.01f, 0x111111, s * 0.018f, -0.125f, 0.048f));
            Box(t, 0.095f, 0.015f, 0.085f, 0xE53935, 0, -0.1f, 0);
            Both(s => Box(t, 0.012f, 0.04f, 0.01f, 0xE53935, 0.03f + s * 0.01f, -0.11f, -0.045f, s * 25));
            Box(t, 0.07f, 0.06f, 0.06f, 0x222831, 0, -0.2f, 0);
            Box(t, 0.072f, 0.012f, 0.062f, 0xE53935, 0, -0.195f, 0);
        }),
        NewCharm("matcha", "Matcha cup", 1, 100, "Green tea in a ceramic cup", t =>
        {
            Box(t, 0.09f, 0.07f, 0.08f, 0x5D4037, 0, -0.18f, 0);
            Box(t, 0.092f, 0.02f, 0.082f, 0x8D6E63, 0, -0.155f, 0);
            Box(t, 0.08f, 0.01f, 0.07f, 0x8BC34A, 0, -0.142f, 0);
            Face(t, -0.185f, 0.042f, 0.02f);
        }),
        NewCharm("ramen", "Ramen", 1, 150, "A bowl with an egg, naruto and chopsticks", t =>
        {
            Box(t, 0.13f, 0.06f, 0.1f, 0xE53935, 0, -0.18f, 0);
            Box(t, 0.09f, 0.03f, 0.08f, 0xE53935, 0, -0.22f, 0);
            Box(t, 0.131f, 0.012f, 0.101f, 0xFFFFFF, 0, -0.17f, 0);
            Box(t, 0.12f, 0.01f, 0.09f, 0xF5C26B, 0, -0.147f, 0);
            Box(t, 0.04f, 0.015f, 0.04f, 0xFFFFFF, -0.03f, -0.14f, 0.01f);
            Box(t, 0.02f, 0.016f, 0.02f, 0xFFB300, -0.03f, -0.139f, 0.01f);
            Box(t, 0.03f, 0.015f, 0.03f, 0xF8BBD0, 0.03f, -0.14f, 0.0f);
            Both(s => Box(t, 0.01f, 0.14f, 0.01f, 0x8D5A2B, 0.03f + s * 0.01f, -0.11f, 0, -20));
        }),
        NewCharm("bento", "Bento", 1, 140, "A lunch box with rice, egg and broccoli", t =>
        {
            Box(t, 0.13f, 0.1f, 0.03f, 0xC62828, 0, -0.155f, 0);
            Box(t, 0.11f, 0.08f, 0.01f, 0x222222, 0, -0.155f, 0.012f);
            Box(t, 0.05f, 0.06f, 0.01f, 0xFFFFFF, -0.025f, -0.155f, 0.018f);
            Box(t, 0.015f, 0.015f, 0.01f, 0xD32F2F, -0.025f, -0.155f, 0.023f);
            Box(t, 0.04f, 0.025f, 0.01f, 0xFFD54F, 0.03f, -0.14f, 0.018f);
            Box(t, 0.03f, 0.03f, 0.01f, 0x43A047, 0.03f, -0.175f, 0.018f);
        }),
        NewCharm("pandacharm", "Panda", 1, 120, "A round panda face", t =>
        {
            Box(t, 0.11f, 0.1f, 0.08f, 0xFFFFFF, 0, -0.15f, 0);
            Both(s => { Box(t, 0.035f, 0.035f, 0.04f, 0x222222, s * 0.045f, -0.095f, 0); Box(t, 0.03f, 0.035f, 0.01f, 0x222222, s * 0.025f, -0.15f, 0.043f, s * 15); Box(t, 0.01f, 0.01f, 0.01f, 0xFFFFFF, s * 0.025f, -0.147f, 0.049f); });
            Box(t, 0.02f, 0.012f, 0.01f, 0x222222, 0, -0.17f, 0.043f);
        }),
        NewCharm("shibacharm", "Shiba", 1, 150, "A smiling shiba face", t =>
        {
            Box(t, 0.11f, 0.09f, 0.08f, 0xE39A4C, 0, -0.15f, 0);
            Box(t, 0.09f, 0.035f, 0.01f, 0xFFF3E6, 0, -0.175f, 0.042f);
            Both(s => { Box(t, 0.035f, 0.035f, 0.03f, 0xE39A4C, s * 0.035f, -0.105f, 0, 45); Box(t, 0.015f, 0.01f, 0.01f, 0xFFF3E6, s * 0.022f, -0.128f, 0.042f); });
            Face(t, -0.145f, 0.046f, 0.022f, false);
            Box(t, 0.02f, 0.012f, 0.01f, 0x222222, 0, -0.165f, 0.048f);
        }),
        NewCharm("nekocharm", "Neko", 1, 120, "A calico cat face with whiskers", t =>
        {
            Box(t, 0.11f, 0.09f, 0.08f, 0xFFFFFF, 0, -0.15f, 0);
            Box(t, 0.04f, 0.04f, 0.01f, 0xFFB74D, -0.03f, -0.125f, 0.042f);
            Box(t, 0.035f, 0.03f, 0.01f, 0x5D4037, 0.035f, -0.13f, 0.042f);
            Both(s => Box(t, 0.035f, 0.035f, 0.03f, s < 0 ? 0xFFB74D : 0x5D4037u, s * 0.035f, -0.105f, 0, 45));
            Face(t, -0.15f, 0.048f, 0.022f);
            Both(s => Box(t, 0.03f, 0.006f, 0.006f, 0x555555, s * 0.06f, -0.16f, 0.044f));
        }),
        NewCharm("sakurablossom", "Sakura", 1, 100, "A cherry blossom flower", t =>
        {
            for (int i = 0; i < 5; i++)
            {
                var p = Quaternion.Euler(0, 0, i * 72f) * new Vector3(0, 0.04f, 0);
                Bx(t, 0.045f, 0.05f, 0.025f, 0xF8BBD0, p.x, -0.16f + p.y, 0, rz: i * 72f);
            }
            Box(t, 0.03f, 0.03f, 0.03f, 0xF06292, 0, -0.16f, 0);
        }),
        NewCharm("fuji", "Mount Fuji", 1, 150, "Snowy Fuji with a red sun behind", t =>
        {
            Box(t, 0.06f, 0.06f, 0.02f, 0xE53935, 0.04f, -0.125f, -0.02f);
            Box(t, 0.15f, 0.04f, 0.05f, 0x5C6BC0, 0, -0.215f, 0);
            Box(t, 0.11f, 0.04f, 0.05f, 0x5C6BC0, 0, -0.175f, 0);
            Box(t, 0.07f, 0.04f, 0.05f, 0xFFFFFF, 0, -0.135f, 0);
            Box(t, 0.035f, 0.02f, 0.05f, 0xFFFFFF, 0, -0.108f, 0);
        }),
        NewCharm("torii", "Torii gate", 1, 160, "A red shrine gate", t =>
        {
            Both(s => Box(t, 0.02f, 0.13f, 0.02f, 0xE53935, s * 0.045f, -0.175f, 0));
            Box(t, 0.15f, 0.022f, 0.03f, 0x222222, 0, -0.1f, 0);
            Box(t, 0.13f, 0.02f, 0.025f, 0xE53935, 0, -0.12f, 0);
            Box(t, 0.11f, 0.015f, 0.02f, 0xE53935, 0, -0.145f, 0);
        }),
        NewCharm("kendama", "Kendama", 1, 140, "The cup-and-ball toy", t =>
        {
            Box(t, 0.06f, 0.06f, 0.06f, 0xE53935, 0, -0.115f, 0);
            Box(t, 0.015f, 0.015f, 0.01f, 0xFFFFFF, -0.012f, -0.105f, 0.032f);
            Box(t, 0.08f, 0.03f, 0.04f, 0xD7A86E, 0, -0.165f, 0);
            Box(t, 0.025f, 0.1f, 0.025f, 0xD7A86E, 0, -0.225f, 0);
        }),
        NewCharm("crane", "Paper crane", 1, 130, "A folded origami crane", t =>
        {
            Box(t, 0.06f, 0.06f, 0.03f, 0xF06292, 0, -0.16f, 0, 45);
            Both(s => Box(t, 0.1f, 0.02f, 0.06f, 0xF48FB1, s * 0.06f, -0.14f, 0, s * 25));
            Box(t, 0.015f, 0.07f, 0.015f, 0xF06292, -0.045f, -0.13f, 0, 30);
            Box(t, 0.03f, 0.015f, 0.015f, 0xF06292, -0.07f, -0.1f, 0, -20);
            Box(t, 0.015f, 0.06f, 0.015f, 0xF06292, 0.045f, -0.14f, 0, -40);
        }),
        NewCharm("kitsunemask", "Kitsune mask", 1, 180, "A white fox festival mask", t =>
        {
            Box(t, 0.12f, 0.11f, 0.03f, 0xFFFFFF, 0, -0.16f, 0);
            Both(s =>
            {
                Box(t, 0.035f, 0.05f, 0.03f, 0xFFFFFF, s * 0.04f, -0.09f, 0, -s * 10);
                Box(t, 0.012f, 0.04f, 0.01f, 0xE53935, s * 0.03f, -0.125f, 0.02f, s * 20);
                Box(t, 0.03f, 0.01f, 0.01f, 0x222222, s * 0.03f, -0.155f, 0.02f, -s * 15);
            });
            Box(t, 0.045f, 0.035f, 0.03f, 0xFFFFFF, 0, -0.195f, 0.025f);
            Box(t, 0.015f, 0.012f, 0.01f, 0x222222, 0, -0.185f, 0.042f);
        }),

        // Mega: 200 to 350, masks, warriors and Japanese icons
        NewCharm("tengu", "Tengu mask", 2, 300, "A red mask with a long nose", t =>
        {
            Box(t, 0.1f, 0.11f, 0.03f, 0xD32F2F, 0, -0.16f, 0);
            Box(t, 0.025f, 0.025f, 0.09f, 0xD32F2F, 0, -0.165f, 0.055f);
            Both(s => { Box(t, 0.035f, 0.012f, 0.01f, 0xFFFFFF, s * 0.025f, -0.13f, 0.018f, s * 20); Box(t, 0.018f, 0.015f, 0.01f, 0xFFC83D, s * 0.025f, -0.145f, 0.018f); });
            Box(t, 0.05f, 0.012f, 0.01f, 0x222222, 0, -0.2f, 0.018f);
        }),
        NewCharm("onimask", "Oni mask", 2, 300, "A blue ogre mask with horns and fangs", t =>
        {
            Box(t, 0.11f, 0.11f, 0.03f, 0x4C6FD0, 0, -0.16f, 0);
            Both(s =>
            {
                Box(t, 0.02f, 0.04f, 0.02f, 0xFFF3C4, s * 0.035f, -0.095f, 0, -s * 15);
                Box(t, 0.025f, 0.018f, 0.01f, 0xFFD54F, s * 0.025f, -0.145f, 0.018f);
                Box(t, 0.012f, 0.02f, 0.01f, 0xFFFFFF, s * 0.02f, -0.19f, 0.018f);
            });
            Box(t, 0.06f, 0.015f, 0.01f, 0x1A237E, 0, -0.18f, 0.018f);
        }),
        NewCharm("kabutocharm", "Kabuto", 2, 280, "A tiny samurai helmet", t =>
        {
            Box(t, 0.11f, 0.06f, 0.09f, 0x2B2B2B, 0, -0.15f, 0);
            Box(t, 0.14f, 0.03f, 0.1f, 0x2B2B2B, 0, -0.185f, 0);
            Box(t, 0.112f, 0.012f, 0.092f, 0xC62828, 0, -0.17f, 0);
            Both(s => Box(t, 0.012f, 0.06f, 0.012f, 0xD4A23A, s * 0.02f, -0.1f, 0.04f, -s * 25));
            Box(t, 0.025f, 0.02f, 0.012f, 0xD4A23A, 0, -0.13f, 0.048f);
        }),
        NewCharm("minikatana", "Mini katana", 2, 250, "A little sword in a black and gold style", t =>
        {
            Box(t, 0.022f, 0.05f, 0.022f, 0xF5F5F5, 0, -0.11f, 0);
            foreach (float y in new[] { -0.1f, -0.12f }) Box(t, 0.024f, 0.008f, 0.024f, 0x222222, 0, y, 0);
            Box(t, 0.05f, 0.012f, 0.035f, 0xFFC83D, 0, -0.14f, 0);
            Box(t, 0.02f, 0.13f, 0.01f, 0xCFD8DC, 0, -0.21f, 0);
            Box(t, 0.015f, 0.015f, 0.01f, 0xCFD8DC, 0.003f, -0.275f, 0, 45);
        }),
        NewCharm("koinobori", "Koinobori", 2, 260, "A carp streamer for Children's Day", t =>
        {
            Box(t, 0.06f, 0.16f, 0.03f, 0xE53935, 0, -0.17f, 0);
            foreach (float y in new[] { -0.15f, -0.19f }) Box(t, 0.062f, 0.015f, 0.032f, 0xFFFFFF, 0, y, 0);
            Box(t, 0.03f, 0.03f, 0.01f, 0xFFFFFF, 0, -0.115f, 0.018f);
            Box(t, 0.015f, 0.015f, 0.01f, 0x222222, 0, -0.115f, 0.024f);
            Both(s => Box(t, 0.03f, 0.04f, 0.03f, 0xE53935, s * 0.018f, -0.265f, 0, s * 25));
        }),
        NewCharm("chochin", "Chōchin lantern", 2, 220, "A glowing red paper lantern", t =>
        {
            Box(t, 0.08f, 0.1f, 0.07f, 0xE53935, 0, -0.16f, 0);
            foreach (float y in new[] { -0.135f, -0.185f }) Box(t, 0.082f, 0.006f, 0.072f, 0xB71C1C, 0, y, 0);
            Both(s => Box(t, 0.06f, 0.015f, 0.05f, 0x222222, 0, -0.16f + s * 0.055f, 0));
            Box(t, 0.015f, 0.04f, 0.015f, 0xFFC83D, 0, -0.245f, 0);
        }),
        NewCharm("obake", "Obake", 2, 220, "A kawaii little ghost", t =>
        {
            Box(t, 0.1f, 0.12f, 0.07f, 0xF4F6FB, 0, -0.15f, 0);
            foreach (float x in new[] { -0.035f, 0, 0.035f }) Box(t, 0.03f, 0.03f, 0.07f, 0xF4F6FB, x, -0.22f + (x == 0 ? 0.005f : 0), 0);
            Face(t, -0.14f, 0.04f);
            Box(t, 0.015f, 0.02f, 0.01f, 0xF06292, 0, -0.165f, 0.04f);
        }),
        NewCharm("shinkansen", "Shinkansen", 2, 320, "A bullet train with a blue stripe", t =>
        {
            Box(t, 0.14f, 0.05f, 0.05f, 0xFFFFFF, 0.01f, -0.15f, 0);
            Box(t, 0.05f, 0.04f, 0.045f, 0xFFFFFF, -0.08f, -0.155f, 0, -15);
            Box(t, 0.18f, 0.012f, 0.055f, 0x1E88E5, -0.01f, -0.16f, 0);
            foreach (float x in new[] { -0.02f, 0.02f, 0.06f }) Box(t, 0.025f, 0.015f, 0.01f, 0x263238, x, -0.14f, 0.026f);
            Box(t, 0.025f, 0.015f, 0.01f, 0x263238, -0.075f, -0.14f, 0.024f, -15);
        }),
        NewCharm("gacha", "Gacha capsule", 2, 250, "A capsule toy, with a star inside", t =>
        {
            Box(t, 0.1f, 0.05f, 0.1f, 0xF06292, 0, -0.185f, 0);
            Box(t, 0.1f, 0.05f, 0.1f, 0xB3E5FC, 0, -0.135f, 0);
            Box(t, 0.105f, 0.01f, 0.105f, 0xFFFFFF, 0, -0.16f, 0);
            StarShape(t, 0.025f, 0xFFD54F, 0, -0.135f, 0.051f, 0.01f);
        }),
        NewCharm("sumo", "Sumo", 2, 300, "A round little sumo wrestler", t =>
        {
            Box(t, 0.13f, 0.11f, 0.09f, 0xF5C99B, 0, -0.19f, 0);
            Box(t, 0.07f, 0.06f, 0.06f, 0xF5C99B, 0, -0.115f, 0);
            Box(t, 0.072f, 0.02f, 0.062f, 0x222222, 0, -0.092f, 0);
            Face(t, -0.115f, 0.031f, 0.017f);
            Box(t, 0.135f, 0.03f, 0.095f, 0x5E35B1, 0, -0.215f, 0);
        }),
        NewCharm("kappa", "Kappa", 2, 280, "A river spirit with a water dish on its head", t =>
        {
            Box(t, 0.1f, 0.09f, 0.08f, 0x7CB342, 0, -0.15f, 0);
            Box(t, 0.104f, 0.02f, 0.084f, 0x33691E, 0, -0.115f, 0);
            Box(t, 0.06f, 0.012f, 0.06f, 0xB3E5FC, 0, -0.1f, 0);
            Box(t, 0.035f, 0.015f, 0.02f, 0xFFC107, 0, -0.17f, 0.045f);
            Face(t, -0.145f, 0.042f, 0.022f);
        }),
        NewCharm("nekosushi", "Neko sushi", 2, 350, "A cat napping on a sushi", t =>
        {
            Box(t, 0.12f, 0.04f, 0.07f, 0xFFFFFF, 0, -0.2f, 0);
            Box(t, 0.12f, 0.05f, 0.07f, 0xFFB74D, 0, -0.155f, 0);
            Box(t, 0.05f, 0.05f, 0.06f, 0xFFB74D, -0.045f, -0.12f, 0.005f);
            Both(s => Box(t, 0.018f, 0.018f, 0.02f, 0xFFB74D, -0.045f + s * 0.016f, -0.093f, 0.005f, 45));
            Both(s => Box(t, 0.012f, 0.004f, 0.01f, 0x222222, -0.045f + s * 0.012f, -0.12f, 0.038f));
            Box(t, 0.025f, 0.1f, 0.075f, 0x1F2A1F, 0.03f, -0.175f, 0);
        }),

        // Ultra: 400 to 600
        NewCharm("goldshuriken", "Golden shuriken", 3, 500, "A gleaming golden ninja star", t => Shuriken(t, 0.18f, 0xFFC83D, 0xE6A817)),
        NewCharm("ryu", "Ryu dragon", 3, 600, "A green Japanese dragon with golden whiskers", t =>
        {
            var p = new[] { new Vector2(0.03f, -0.15f), new Vector2(-0.02f, -0.18f), new Vector2(0.02f, -0.22f), new Vector2(-0.01f, -0.25f) };
            for (int i = 0; i < p.Length; i++) Box(t, 0.045f - i * 0.006f, 0.045f - i * 0.006f, 0.045f - i * 0.006f, 0x43A047, p[i].x, p[i].y, 0);
            Box(t, 0.07f, 0.06f, 0.06f, 0x43A047, 0, -0.11f, 0);
            Box(t, 0.04f, 0.025f, 0.03f, 0x66BB6A, 0, -0.125f, 0.04f);
            Both(s => { Box(t, 0.012f, 0.035f, 0.012f, 0xFFF3C4, s * 0.025f, -0.08f, 0, -s * 25); Box(t, 0.04f, 0.006f, 0.006f, 0xFFC83D, s * 0.04f, -0.13f, 0.045f, s * 20); Box(t, 0.012f, 0.012f, 0.01f, 0xFFD54F, s * 0.017f, -0.1f, 0.031f); });
        }),
        NewCharm("kitsunecharm", "Kitsune", 3, 550, "A white fox spirit with three tails", t =>
        {
            Box(t, 0.08f, 0.07f, 0.07f, 0xFFFFFF, 0, -0.12f, 0);
            Both(s => { Box(t, 0.025f, 0.035f, 0.02f, 0xFFFFFF, s * 0.025f, -0.075f, 0); Box(t, 0.015f, 0.01f, 0.01f, 0xE53935, s * 0.02f, -0.11f, 0.037f, s * 15); });
            Box(t, 0.012f, 0.01f, 0.01f, 0x222222, 0, -0.13f, 0.037f);
            Box(t, 0.07f, 0.07f, 0.06f, 0xFFFFFF, 0, -0.19f, 0);
            Box(t, 0.072f, 0.012f, 0.062f, 0xE53935, 0, -0.16f, 0);
            foreach (float a in new[] { -40f, 0f, 40f })
            {
                var tail = Pivot(t, 0, -0.2f, -0.03f, 0, 0, a);
                Box(tail, 0.03f, 0.08f, 0.03f, 0xFFFFFF, 0, -0.04f, 0);
                Box(tail, 0.031f, 0.02f, 0.031f, 0xE53935, 0, -0.08f, 0);
            }
        }),
        NewCharm("pagoda", "Pagoda", 3, 500, "A three-story pagoda", t =>
        {
            for (int i = 0; i < 3; i++)
            {
                float y = -0.24f + i * 0.045f;
                Box(t, 0.07f - i * 0.012f, 0.03f, 0.05f, 0xC62828, 0, y, 0);
                Box(t, 0.12f - i * 0.02f, 0.012f, 0.07f, 0x37474F, 0, y + 0.02f, 0);
            }
            Box(t, 0.012f, 0.05f, 0.012f, 0xFFC83D, 0, -0.1f, 0);
        }),
        NewCharm("mecha", "Mecha", 3, 600, "A heroic giant robot, pocket-sized", t =>
        {
            Box(t, 0.07f, 0.06f, 0.06f, 0xFFFFFF, 0, -0.125f, 0);
            Both(s => Box(t, 0.012f, 0.045f, 0.01f, 0xFFC83D, s * 0.022f, -0.1f, 0.03f, -s * 50));
            Box(t, 0.05f, 0.018f, 0.01f, 0x4FC3F7, 0, -0.125f, 0.031f);
            Box(t, 0.02f, 0.015f, 0.01f, 0xE53935, 0, -0.145f, 0.031f);
            Box(t, 0.08f, 0.07f, 0.06f, 0x1E88E5, 0, -0.19f, 0);
            Both(s => { Box(t, 0.03f, 0.04f, 0.06f, 0xE53935, s * 0.055f, -0.17f, 0); Box(t, 0.02f, 0.015f, 0.01f, 0xFFD54F, s * 0.018f, -0.18f, 0.031f); });
            Box(t, 0.06f, 0.03f, 0.05f, 0xFFFFFF, 0, -0.235f, 0);
        }),
        NewCharm("tsukimi", "Moon rabbit", 3, 450, "A rabbit in front of the full moon", t =>
        {
            Box(t, 0.15f, 0.15f, 0.02f, 0x283593, 0, -0.165f, -0.012f);
            Box(t, 0.11f, 0.11f, 0.02f, 0xFFE082, 0, -0.165f, 0);
            Box(t, 0.13f, 0.08f, 0.02f, 0xFFE082, 0, -0.165f, 0);
            Box(t, 0.08f, 0.13f, 0.02f, 0xFFE082, 0, -0.165f, 0);
            Box(t, 0.06f, 0.045f, 0.02f, 0xFFFFFF, 0.01f, -0.195f, 0.015f);
            Box(t, 0.04f, 0.035f, 0.02f, 0xFFFFFF, -0.03f, -0.17f, 0.015f);
            Both(s => { Box(t, 0.014f, 0.05f, 0.02f, 0xFFFFFF, -0.03f + s * 0.01f, -0.13f, 0.015f, s * 10); Box(t, 0.006f, 0.03f, 0.01f, 0xF8BBD0, -0.03f + s * 0.01f, -0.13f, 0.026f, s * 10); });
            Box(t, 0.01f, 0.01f, 0.01f, 0xD32F2F, -0.04f, -0.168f, 0.027f);
        }),
        NewCharm("koban", "Lucky koban", 3, 450, "A golden oval coin for good fortune", t =>
        {
            Box(t, 0.09f, 0.14f, 0.025f, 0xFFC83D, 0, -0.165f, 0);
            Box(t, 0.07f, 0.16f, 0.025f, 0xFFC83D, 0, -0.165f, 0);
            foreach (float y in new[] { -0.12f, -0.21f }) Box(t, 0.06f, 0.01f, 0.01f, 0xE6A817, 0, y, 0.016f);
            Box(t, 0.03f, 0.04f, 0.01f, 0xE6A817, 0, -0.165f, 0.016f);
        }),
        NewCharm("sakurabranch", "Sakura branch", 3, 400, "A cherry branch in full bloom", t =>
        {
            Box(t, 0.015f, 0.05f, 0.015f, 0x5D4037, 0, -0.11f, 0);
            Box(t, 0.15f, 0.015f, 0.015f, 0x5D4037, 0, -0.14f, 0, -15);
            foreach (var p in new[] { new Vector2(-0.05f, -0.12f), new Vector2(0.02f, -0.15f), new Vector2(0.06f, -0.17f), new Vector2(-0.02f, -0.17f) })
            {
                Box(t, 0.035f, 0.035f, 0.03f, 0xF8BBD0, p.x, p.y, 0, 45);
                Box(t, 0.012f, 0.012f, 0.032f, 0xF06292, p.x, p.y, 0);
            }
            Box(t, 0.02f, 0.02f, 0.01f, 0xF8BBD0, 0.03f, -0.23f, 0, 30);
        }),
        NewCharm("goldfan", "Golden fan", 3, 400, "A folding fan in gold, with a red sun", t =>
        {
            var hinge = Pivot(t, 0, -0.23f, 0);
            for (int i = 0; i < 7; i++)
            {
                float a = -60 + i * 20f;
                var dir = Quaternion.Euler(0, 0, a) * Vector3.up;
                Bx(hinge, 0.028f, 0.1f, 0.01f, i % 2 == 0 ? 0xFFC83D : 0xE6A817u, dir.x * 0.07f, dir.y * 0.07f, 0, rz: a);
            }
            Box(hinge, 0.04f, 0.04f, 0.01f, 0xE53935, 0, 0.08f, 0.01f);
            Box(hinge, 0.02f, 0.02f, 0.02f, 0x5D4037, 0, 0, 0);
            Box(t, 0.012f, 0.06f, 0.012f, 0xE53935, 0, -0.12f, 0);
        }),

        // Legendary: 700 to 800
        NewCharm("golddaruma", "Golden daruma", 4, 800, "A gold daruma for a wish come true", t =>
        {
            Box(t, 0.12f, 0.13f, 0.1f, 0xFFC83D, 0, -0.15f, 0);
            Box(t, 0.08f, 0.06f, 0.02f, 0xFFF3E0, 0, -0.13f, 0.055f);
            Both(s => Box(t, 0.02f, 0.02f, 0.01f, 0x111111, s * 0.02f, -0.125f, 0.066f));
            Box(t, 0.06f, 0.02f, 0.02f, 0xE53935, 0, -0.19f, 0.052f);
            Box(t, 0.02f, 0.02f, 0.01f, 0xFFFFFF, -0.04f, -0.1f, 0.051f);
        }),
        NewCharm("hoo", "Hō-ō phoenix", 4, 750, "The Japanese phoenix, with a long rainbow tail", t =>
        {
            Box(t, 0.06f, 0.05f, 0.05f, 0xE53935, 0, -0.12f, 0);
            Box(t, 0.02f, 0.015f, 0.02f, 0xFFC83D, 0, -0.12f, 0.035f);
            Box(t, 0.012f, 0.03f, 0.012f, 0xFFC83D, 0, -0.09f, 0);
            Both(s => Box(t, 0.07f, 0.02f, 0.04f, 0xFF8A1F, s * 0.05f, -0.135f, 0, s * 25));
            foreach (var (x, c) in new[] { (-0.02f, 0x4FC3F7u), (0f, 0x66BB6Au), (0.02f, 0xFFD54Fu) })
                Box(t, 0.018f, 0.13f, 0.012f, c, x, -0.21f, 0, x * 300);
        }),
        NewCharm("goldneko", "Golden maneki-neko", 4, 800, "A golden beckoning cat", t => LuckyCat(t, 0xFFC83D, 0xFFFFFF)),
        NewCharm("crystalshuriken", "Crystal shuriken", 4, 700, "A ninja star made of ice crystal", t => Shuriken(t, 0.18f, 0x80DEEA, 0xE0F7FA)),
        NewCharm("minimeteor", "Mini meteor", 4, 750, "A meteor from the boss planets, still burning", t =>
        {
            Box(t, 0.09f, 0.09f, 0.09f, 0x4A3A33, 0, -0.19f, 0);
            Box(t, 0.04f, 0.04f, 0.02f, 0x3A2C26, -0.02f, -0.17f, 0.046f);
            Bx(t, 0.095f, 0.03f, 0.03f, 0xFF8A1F, 0, -0.205f, 0.035f, shadow: false);
            Bx(t, 0.06f, 0.06f, 0.06f, 0xFF8A1F, 0.03f, -0.13f, -0.01f, shadow: false);
            Bx(t, 0.04f, 0.04f, 0.04f, 0xFFD23D, 0.05f, -0.1f, -0.01f, shadow: false);
        }),
        NewCharm("galaxycapsule", "Galaxy gacha", 4, 800, "A capsule holding a tiny galaxy", t =>
        {
            Box(t, 0.1f, 0.05f, 0.1f, 0x311B92, 0, -0.185f, 0);
            Box(t, 0.1f, 0.05f, 0.1f, 0x7E57C2, 0, -0.135f, 0);
            Box(t, 0.105f, 0.01f, 0.105f, 0xFFD54F, 0, -0.16f, 0);
            foreach (var (x, y) in new[] { (-0.025f, -0.125f), (0.02f, -0.145f), (0.03f, -0.19f), (-0.02f, -0.2f) })
                Box(t, 0.012f, 0.012f, 0.01f, 0xFFFFFF, x, y, 0.051f);
            StarShape(t, 0.02f, 0xFFE082, 0, -0.135f, 0.052f, 0.01f);
        }),
    };
}
