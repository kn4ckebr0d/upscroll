using System;
using System.Collections.Generic;
using UnityEngine;

/// Candidate items to pick from: models that exist but aren't in the item sheet yet. Picked ones get a row in
/// Items.csv (with this id as their `base`); the rest can be deleted. -scrollyCatalog renders them all.
public static partial class Cosmetics
{
    static IEnumerable<Cosmetic> CandidatesOf(CosmeticKind kind) => kind switch
    {
        CosmeticKind.Character => CharacterCandidates,
        CosmeticKind.Hat => HatCandidates,
        CosmeticKind.Backpack => BackpackCandidates,
        CosmeticKind.Charm => CharmCandidates,
        _ => TagCandidates,
    };

    // MARK: - Helpers

    static Cosmetic Cand(CosmeticKind kind, string id, string name, int rarity, int price, string notes,
                         CharacterLook look = null, Action<Transform> build = null, TagStyle tag = null) =>
        new() { Id = id, Name = name, Kind = kind, Rarity = (Rarity)rarity, Price = price, Notes = notes, Look = look, Build = build, Tag = tag };

    static Cosmetic Chr(string id, string name, int rarity, int price, string notes, CharacterLook look) =>
        Cand(CosmeticKind.Character, id, name, rarity, price, notes, look: look);

    /// A box turned any way (Box only tilts sideways).
    static GameObject Bx(Transform t, float w, float h, float d, uint hex, float x, float y, float z,
                         float rx = 0, float ry = 0, float rz = 0, bool shadow = true)
    {
        var box = Voxel.Box(t, new Vector3(w, h, d), hex, new Vector3(x, y, z), shadow);
        box.transform.localRotation = Quaternion.Euler(rx, ry, rz);
        return box;
    }

    /// An empty turned point to build on, for parts at an angle (wings, tails).
    static Transform Pivot(Transform t, float x, float y, float z, float rx = 0, float ry = 0, float rz = 0)
    {
        var pivot = new GameObject("pivot").transform;
        pivot.SetParent(t, false);
        pivot.localPosition = new Vector3(x, y, z);
        pivot.localRotation = Quaternion.Euler(rx, ry, rz);
        return pivot;
    }

    /// Runs once for the left side (-1) and once for the right (+1).
    static void Both(Action<float> side)
    {
        side(-1);
        side(1);
    }

    // Faces sit on the front of the head (z = -0.26); eyes at y = 1.0, x = ±0.12.
    static void Blush(Transform t) => Both(s => Box(t, 0.08f, 0.05f, 0.02f, 0xF8A5C2, s * 0.19f, 0.9f, -0.262f));
    static void Nose(Transform t, uint hex, float y = 0.9f, float w = 0.07f) => Box(t, w, 0.05f, 0.02f, hex, 0, y, -0.275f);
    static void Muzzle(Transform t, uint hex, float w = 0.24f, float y = 0.86f) => Box(t, w, 0.13f, 0.02f, hex, 0, y, -0.262f);
    static void Belly(Transform t, uint hex, float w = 0.36f, float h = 0.3f) => Box(t, w, h, 0.02f, hex, 0, 0.5f, -0.21f);
    static void Eyes(Transform t, uint hex, float w = 0.08f, float h = 0.1f, float x = 0.12f, float y = 1.0f) =>
        Both(s => Box(t, w, h, 0.02f, hex, s * x, y, -0.275f));
    static void Shine(Transform t, float x = 0.12f, float y = 1.0f) => Both(s => Box(t, 0.03f, 0.03f, 0.01f, 0xFFFFFF, s * x - 0.015f, y + 0.02f, -0.288f));
    /// Round ears on top of the head (on the Top anchor).
    static void RoundEars(Transform t, uint outer, uint inner, float x = 0.2f) => Both(s =>
    {
        Box(t, 0.14f, 0.13f, 0.1f, outer, s * x, 0.05f, 0);
        Box(t, 0.07f, 0.06f, 0.02f, inner, s * x, 0.05f, -0.055f);
    });
    static void PointyEars(Transform t, uint outer, uint inner, float x = 0.18f) => Both(s =>
    {
        Box(t, 0.14f, 0.17f, 0.08f, outer, s * x, 0.08f, 0);
        Box(t, 0.07f, 0.1f, 0.02f, inner, s * x, 0.07f, -0.045f);
    });
    static void Antennae(Transform t, uint stalk, uint tip) => Both(s =>
    {
        Box(t, 0.03f, 0.18f, 0.03f, stalk, s * 0.11f, 0.09f, 0, -s * 15);
        Box(t, 0.08f, 0.08f, 0.08f, tip, s * 0.14f, 0.2f, 0);
    });
    /// A tail sticking out the back, pointing up and away.
    static void Tail(Transform t, uint hex, float w = 0.1f, float length = 0.4f, float x = 0.14f, float y = 0.3f) =>
        Bx(t, w, length, w, hex, x, y, 0.3f, rx: -35, rz: -30);
    static void Scarf(Transform t, uint hex) { Box(t, 0.62f, 0.08f, 0.44f, hex, 0, 0.73f, 0); Box(t, 0.1f, 0.22f, 0.03f, hex, 0.14f, 0.6f, -0.225f); }
    static void Band(Transform t, uint hex, float y, float h = 0.07f) => Box(t, 0.62f, h, 0.42f, hex, 0, y, 0);
    static void HeadBand(Transform t, uint hex, float y, float h = 0.04f, float tilt = 0) => Box(t, 0.57f, h, 0.52f, hex, 0, y, 0, tilt);

    // MARK: - Characters: 48, from 200 to 6000 coins

    static readonly List<Cosmetic> CharacterCandidates = new()
    {
        // Common
        Chr("dog", "Puppy", 0, 200, "Floppy ears, a patch over one eye, a wagging tail", new CharacterLook
        {
            Legs = 0xB07A4A, Body = 0xC68B59, Arms = 0xC68B59, Hands = 0xE8C9A0, Head = 0xC68B59,
            Details = (t, l) =>
            {
                Box(t, 0.14f, 0.16f, 0.02f, 0x8D5A34, 0.12f, 1.0f, -0.258f);
                Muzzle(t, 0xF1DEC0, 0.26f);
                Nose(t, 0x3E2723, 0.91f, 0.1f);
                Box(t, 0.06f, 0.05f, 0.02f, 0xF28CA0, 0, 0.81f, -0.272f);
                Both(s => Box(t, 0.1f, 0.3f, 0.24f, 0x8D5A34, s * 0.32f, 0.96f, 0));
                Bx(t, 0.08f, 0.25f, 0.08f, l.Body, 0, 0.62f, 0.27f, rx: -30);
            },
        }),
        Chr("bunny", "Bunny", 0, 200, "Tall ears and a cotton tail", new CharacterLook
        {
            Legs = 0xEDEDED, Body = 0xF7F7F7, Arms = 0xF7F7F7, Hands = 0xFFFFFF, Head = 0xFAFAFA,
            Details = (t, l) => { Nose(t, 0xF28CA0); Blush(t); Box(t, 0.14f, 0.14f, 0.1f, 0xFFFFFF, 0, 0.38f, 0.24f); },
            Top = (t, l) => Both(s =>
            {
                Box(t, 0.12f, 0.38f, 0.08f, l.Head, s * 0.12f, 0.19f, 0, -s * 5);
                Box(t, 0.06f, 0.28f, 0.02f, 0xF8BBD0, s * 0.12f, 0.19f, -0.045f, -s * 5);
            }),
        }),
        Chr("bearcub", "Bear", 0, 250, "A brown bear cub with a light belly", new CharacterLook
        {
            Legs = 0x7A4E33, Body = 0x8B5A3C, Arms = 0x8B5A3C, Hands = 0xA0714F, Head = 0x8B5A3C,
            Details = (t, l) => { Muzzle(t, 0xD9B08C, 0.22f); Nose(t, 0x2B1B14, 0.91f, 0.08f); Belly(t, 0xA6764F); },
            Top = (t, l) => RoundEars(t, l.Head, 0xD9B08C),
        }),
        Chr("pig", "Piggy", 0, 250, "Pink, with a big snout and a curly tail", new CharacterLook
        {
            Legs = 0xE892A6, Body = 0xF4A6B8, Arms = 0xF4A6B8, Hands = 0xE892A6, Head = 0xF7B3C2,
            Details = (t, l) =>
            {
                Box(t, 0.18f, 0.12f, 0.06f, 0xF28CA0, 0, 0.87f, -0.27f);
                Both(s => Box(t, 0.03f, 0.05f, 0.02f, 0x9C4A5E, s * 0.04f, 0.87f, -0.305f));
                Blush(t);
                Box(t, 0.06f, 0.06f, 0.12f, 0xF28CA0, 0, 0.45f, 0.25f);
                Box(t, 0.06f, 0.1f, 0.06f, 0xF28CA0, 0.03f, 0.51f, 0.3f);
            },
            Top = (t, l) => Both(s => Box(t, 0.13f, 0.1f, 0.1f, l.Head, s * 0.18f, 0.04f, -0.05f, -s * 20)),
        }),
        Chr("mouse", "Mouse", 0, 250, "Big round ears, whiskers and a long tail", new CharacterLook
        {
            Legs = 0x8E8E99, Body = 0xA9A9B3, Arms = 0xA9A9B3, Hands = 0xF4C2CC, Head = 0xB5B5BF,
            Details = (t, l) =>
            {
                Nose(t, 0xF28CA0, 0.9f, 0.06f);
                Both(s => { Box(t, 0.12f, 0.015f, 0.01f, 0x555555, s * 0.2f, 0.91f, -0.27f, s * 8); Box(t, 0.12f, 0.015f, 0.01f, 0x555555, s * 0.2f, 0.88f, -0.27f, -s * 8); });
                Box(t, 0.04f, 0.04f, 0.4f, 0xF4C2CC, 0, 0.33f, 0.38f);
                Box(t, 0.04f, 0.16f, 0.04f, 0xF4C2CC, 0, 0.4f, 0.58f);
            },
            Top = (t, l) => Both(s =>
            {
                Box(t, 0.24f, 0.22f, 0.06f, l.Head, s * 0.22f, 0.1f, 0);
                Box(t, 0.15f, 0.14f, 0.02f, 0xF4C2CC, s * 0.22f, 0.1f, -0.035f);
            }),
        }),
        Chr("chick", "Chick", 0, 300, "A fluffy yellow chick with a little tuft", new CharacterLook
        {
            Legs = 0xF29C38, Body = 0xFFD84D, Arms = 0xFFCF33, Hands = 0xFFCF33, Head = 0xFFE066,
            Details = (t, l) => { Box(t, 0.12f, 0.07f, 0.08f, 0xF29C38, 0, 0.89f, -0.29f); Blush(t); },
            Top = (t, l) => { Box(t, 0.05f, 0.12f, 0.05f, 0xFFCF33, 0, 0.06f, 0); Box(t, 0.05f, 0.09f, 0.05f, 0xFFCF33, 0.05f, 0.05f, 0, -30); },
        }),
        Chr("koala", "Koala", 0, 300, "Fluffy ears and a big dark nose", new CharacterLook
        {
            Legs = 0x8A8F94, Body = 0x9EA3A8, Arms = 0x9EA3A8, Hands = 0x7B8085, Head = 0xA9AEB3,
            Details = (t, l) => { Box(t, 0.12f, 0.15f, 0.04f, 0x3A3A40, 0, 0.88f, -0.27f); Belly(t, 0xD9DCDF, 0.32f, 0.28f); },
            Top = (t, l) => Both(s => { Box(t, 0.2f, 0.2f, 0.1f, l.Head, s * 0.28f, 0f, 0); Box(t, 0.12f, 0.12f, 0.02f, 0xE3E5E8, s * 0.28f, 0f, -0.055f); }),
        }),
        Chr("panda", "Panda", 0, 350, "Black and white, with eye patches", new CharacterLook
        {
            Legs = 0x222222, Body = 0xF5F5F5, Arms = 0x222222, Hands = 0x222222, Head = 0xFAFAFA, OwnEyes = true,
            Details = (t, l) =>
            {
                Both(s => Box(t, 0.14f, 0.17f, 0.02f, 0x222222, s * 0.12f, 0.99f, -0.262f, s * 15));
                Eyes(t, 0xFFFFFF, 0.04f, 0.05f, 0.12f, 1.01f);
                Nose(t, 0x222222, 0.88f);
                Band(t, 0x222222, 0.7f, 0.08f);
            },
            Top = (t, l) => RoundEars(t, 0x222222, 0x222222),
        }),
        Chr("fox", "Fox", 0, 350, "Pointy ears and a big bushy tail", new CharacterLook
        {
            Legs = 0x3A2A22, Body = 0xF07A2A, Arms = 0xF07A2A, Hands = 0x3A2A22, Head = 0xF5852F,
            Details = (t, l) =>
            {
                Box(t, 0.36f, 0.14f, 0.02f, 0xFFF3E6, 0, 0.84f, -0.262f);
                Nose(t, 0x222222, 0.88f, 0.06f);
                Box(t, 0.24f, 0.2f, 0.02f, 0xFFF3E6, 0, 0.62f, -0.21f);
                var tail = Pivot(t, 0.1f, 0.3f, 0.3f, -40, 0, -20);
                Box(tail, 0.18f, 0.36f, 0.18f, l.Body, 0, 0.18f, 0);
                Box(tail, 0.18f, 0.1f, 0.18f, 0xFFF3E6, 0, 0.41f, 0);
            },
            Top = (t, l) =>
            {
                PointyEars(t, l.Head, 0xFFF3E6);
                Both(s => Box(t, 0.142f, 0.05f, 0.082f, 0x3A2A22, s * 0.18f, 0.15f, 0));
            },
        }),
        Chr("sheep", "Sheep", 0, 300, "Woolly, with a dark face", new CharacterLook
        {
            Legs = 0x3A3A3A, Body = 0xFAF7F0, Arms = 0xFAF7F0, Hands = 0x3A3A3A, Head = 0x4A4A4A, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0xFFFFFF, 0.08f, 0.1f);
                Both(s => Box(t, 0.04f, 0.05f, 0.02f, 0x111111, s * 0.12f, 0.99f, -0.285f));
                Box(t, 0.66f, 0.42f, 0.46f, 0xF5F1E6, 0, 0.53f, 0);
                foreach (var p in new[] { new Vector2(-0.2f, 0.66f), new Vector2(0.05f, 0.7f), new Vector2(0.24f, 0.62f), new Vector2(-0.1f, 0.4f), new Vector2(0.18f, 0.38f) })
                    Box(t, 0.14f, 0.12f, 0.06f, 0xFFFFFF, p.x, p.y, -0.24f);
                Both(s => Box(t, 0.16f, 0.08f, 0.1f, 0x3A3A3A, s * 0.34f, 0.98f, 0, -s * 20));
            },
            Top = (t, l) => { Box(t, 0.4f, 0.12f, 0.36f, 0xFAF7F0, 0, 0.05f, 0.02f); Box(t, 0.2f, 0.08f, 0.2f, 0xFFFFFF, 0, 0.13f, 0); },
        }),
        Chr("cow", "Cow", 0, 350, "Black spots, a pink nose and little horns", new CharacterLook
        {
            Legs = 0xFAFAFA, Body = 0xFAFAFA, Arms = 0xFAFAFA, Hands = 0x3A3A3A, Head = 0xFAFAFA,
            Details = (t, l) =>
            {
                Box(t, 0.18f, 0.14f, 0.02f, 0x2B2B2B, -0.12f, 0.6f, -0.21f);
                Box(t, 0.12f, 0.12f, 0.02f, 0x2B2B2B, 0.16f, 0.42f, -0.21f);
                Box(t, 0.02f, 0.16f, 0.16f, 0x2B2B2B, 0.31f, 0.55f, 0.05f);
                Box(t, 0.14f, 0.12f, 0.02f, 0x2B2B2B, 0.17f, 1.1f, -0.258f);
                Box(t, 0.32f, 0.14f, 0.04f, 0xF4B6C2, 0, 0.84f, -0.27f);
                Both(s => Box(t, 0.04f, 0.04f, 0.02f, 0x9C4A5E, s * 0.07f, 0.84f, -0.295f));
                Both(s => Box(t, 0.14f, 0.07f, 0.08f, 0xFAFAFA, s * 0.34f, 1.05f, 0));
            },
            Top = (t, l) => Both(s => Box(t, 0.06f, 0.12f, 0.06f, 0xF1E3C6, s * 0.17f, 0.06f, 0)),
        }),
        Chr("hamster", "Hamster", 0, 300, "Chubby cheeks and a white belly", new CharacterLook
        {
            Legs = 0xE0A56A, Body = 0xE8B27A, Arms = 0xE8B27A, Hands = 0xFFE2C6, Head = 0xEDB985,
            Details = (t, l) =>
            {
                Both(s => Box(t, 0.18f, 0.12f, 0.04f, 0xFFF5EA, s * 0.15f, 0.85f, -0.265f));
                Nose(t, 0xF28CA0, 0.92f, 0.05f);
                Belly(t, 0xFFF5EA, 0.34f, 0.32f);
            },
            Top = (t, l) => Both(s => { Box(t, 0.1f, 0.09f, 0.06f, l.Head, s * 0.19f, 0.04f, 0); Box(t, 0.05f, 0.05f, 0.02f, 0xF4B6C2, s * 0.19f, 0.04f, -0.035f); }),
        }),
        Chr("duck", "Duck", 0, 250, "A mallard: green head, white collar, yellow bill", new CharacterLook
        {
            Legs = 0xF29C38, Body = 0xB89B7A, Arms = 0x8F7558, Hands = 0x8F7558, Head = 0x2E7D4F,
            Details = (t, l) =>
            {
                Box(t, 0.22f, 0.07f, 0.12f, 0xF6C63B, 0, 0.88f, -0.3f);
                Box(t, 0.57f, 0.05f, 0.52f, 0xFFFFFF, 0, 0.765f, 0);
                Box(t, 0.2f, 0.1f, 0.1f, 0x5A4632, 0, 0.4f, 0.26f);
            },
        }),
        Chr("owl", "Owl", 0, 400, "Big round eyes and ear tufts", new CharacterLook
        {
            Legs = 0xF2B33D, Body = 0x8D6E4F, Arms = 0x7A5C40, Hands = 0x7A5C40, Head = 0x9A7A58, OwnEyes = true,
            Details = (t, l) =>
            {
                Both(s => Box(t, 0.19f, 0.19f, 0.02f, 0xFFF3D6, s * 0.12f, 1.0f, -0.262f));
                Eyes(t, 0x222222, 0.08f, 0.08f);
                Shine(t);
                Box(t, 0.06f, 0.08f, 0.04f, 0xF2B33D, 0, 0.87f, -0.28f);
                Belly(t, 0xE8D3B0, 0.34f, 0.32f);
                foreach (var p in new[] { new Vector2(-0.08f, 0.58f), new Vector2(0.08f, 0.58f), new Vector2(0, 0.48f), new Vector2(-0.08f, 0.4f), new Vector2(0.08f, 0.4f) })
                    Box(t, 0.05f, 0.03f, 0.02f, 0x9A7A58, p.x, p.y, -0.225f);
            },
            Top = (t, l) => Both(s => Box(t, 0.08f, 0.13f, 0.06f, l.Head, s * 0.2f, 0.06f, 0, -s * 15)),
        }),

        // Rare
        Chr("tiger", "Tiger", 1, 600, "Orange with black stripes", new CharacterLook
        {
            Legs = 0xE57E1A, Body = 0xF08A24, Arms = 0xF08A24, Hands = 0xFFF3E6, Head = 0xF59A35,
            Details = (t, l) =>
            {
                foreach (float x in new[] { -0.07f, 0, 0.07f }) Box(t, 0.04f, 0.1f, 0.02f, 0x2B1B14, x, 1.13f, -0.258f);
                Both(s => { Box(t, 0.1f, 0.03f, 0.02f, 0x2B1B14, s * 0.22f, 0.93f, -0.258f); Box(t, 0.1f, 0.03f, 0.02f, 0x2B1B14, s * 0.22f, 0.87f, -0.258f); });
                Muzzle(t, 0xFFF3E6, 0.24f, 0.85f);
                Nose(t, 0xF28CA0, 0.89f);
                Both(s => { Box(t, 0.12f, 0.04f, 0.02f, 0x2B1B14, s * 0.22f, 0.64f, -0.21f); Box(t, 0.12f, 0.04f, 0.02f, 0x2B1B14, s * 0.22f, 0.48f, -0.21f); });
                Both(s => Box(t, 0.02f, 0.04f, 0.3f, 0x2B1B14, s * 0.31f, 0.56f, 0));
                Tail(t, l.Body, 0.08f, 0.42f);
            },
            Top = (t, l) => RoundEars(t, l.Head, 0x2B1B14, 0.19f),
        }),
        Chr("lion", "Lion", 1, 700, "A big fluffy mane", new CharacterLook
        {
            Legs = 0xDDA23D, Body = 0xE8B04A, Arms = 0xE8B04A, Hands = 0xF5D08A, Head = 0xF2C063, HatLift = 0.08f,
            Details = (t, l) =>
            {
                Box(t, 0.72f, 0.62f, 0.4f, 0xB5651D, 0, 0.98f, 0.06f);
                Muzzle(t, 0xFFF0D0, 0.24f, 0.86f);
                Nose(t, 0x5D3A1A, 0.9f, 0.08f);
                Tail(t, l.Body, 0.06f, 0.36f);
                Box(t, 0.12f, 0.12f, 0.12f, 0xB5651D, 0.27f, 0.47f, 0.47f);
            },
            Top = (t, l) => RoundEars(t, l.Head, 0xB5651D, 0.2f),
        }),
        Chr("raccoon", "Raccoon", 1, 500, "A bandit mask and a ringed tail", new CharacterLook
        {
            Legs = 0x5A5A5E, Body = 0x8E8E92, Arms = 0x8E8E92, Hands = 0x3A3A3E, Head = 0x9A9A9E, OwnEyes = true,
            Details = (t, l) =>
            {
                Box(t, 0.48f, 0.13f, 0.02f, 0x2E2E32, 0, 1.0f, -0.262f);
                Eyes(t, 0xFFFFFF, 0.06f, 0.07f);
                Both(s => Box(t, 0.03f, 0.04f, 0.02f, 0x111111, s * 0.12f, 0.995f, -0.285f));
                Muzzle(t, 0xF2F2F2, 0.22f, 0.85f);
                Nose(t, 0x222222, 0.89f, 0.06f);
                var tail = Pivot(t, 0.1f, 0.3f, 0.26f, -45, 0, -20);
                for (int i = 0; i < 5; i++) Box(tail, 0.15f, 0.08f, 0.15f, i % 2 == 0 ? 0x8E8E92 : 0x2E2E32u, 0, 0.04f + i * 0.08f, 0);
            },
            Top = (t, l) => RoundEars(t, l.Head, 0x2E2E32, 0.19f),
        }),
        Chr("monkey", "Monkey", 1, 500, "A peachy face, round ears and a curly tail", new CharacterLook
        {
            Legs = 0x7A4E33, Body = 0x8B5A3C, Arms = 0x8B5A3C, Hands = 0xE8C4A0, Head = 0x8B5A3C,
            Details = (t, l) =>
            {
                Box(t, 0.42f, 0.32f, 0.02f, 0xF0CDA8, 0, 0.93f, -0.258f);
                Both(s => Box(t, 0.03f, 0.03f, 0.02f, 0x5D3A1A, s * 0.03f, 0.9f, -0.272f));
                Box(t, 0.14f, 0.03f, 0.02f, 0x5D3A1A, 0, 0.83f, -0.272f);
                Both(s => { Box(t, 0.1f, 0.16f, 0.1f, l.Head, s * 0.32f, 0.97f, 0); Box(t, 0.02f, 0.1f, 0.06f, 0xF0CDA8, s * 0.37f, 0.97f, 0); });
                Box(t, 0.06f, 0.3f, 0.06f, l.Body, 0.12f, 0.4f, 0.3f, -20);
                Box(t, 0.14f, 0.06f, 0.06f, l.Body, 0.2f, 0.56f, 0.3f);
            },
            Top = (t, l) => Box(t, 0.14f, 0.06f, 0.12f, 0x6E4429, 0, 0.03f, -0.08f),
        }),
        Chr("shiba", "Shiba", 1, 600, "A happy shiba with a curly tail", new CharacterLook
        {
            Legs = 0xFFF3E6, Body = 0xE39A4C, Arms = 0xE39A4C, Hands = 0xFFF3E6, Head = 0xE8A458,
            Details = (t, l) =>
            {
                Box(t, 0.42f, 0.16f, 0.02f, 0xFFF3E6, 0, 0.84f, -0.262f);
                Nose(t, 0x222222, 0.89f, 0.07f);
                Both(s => Box(t, 0.05f, 0.03f, 0.02f, 0xFFF3E6, s * 0.12f, 1.1f, -0.262f));
                Box(t, 0.26f, 0.22f, 0.02f, 0xFFF3E6, 0, 0.6f, -0.21f);
                Box(t, 0.16f, 0.16f, 0.14f, l.Body, 0, 0.68f, 0.27f);
                Box(t, 0.08f, 0.08f, 0.04f, 0xFFF3E6, 0, 0.68f, 0.35f);
            },
            Top = (t, l) => PointyEars(t, l.Head, 0xFFF3E6),
        }),
        Chr("polarbear", "Polar bear", 1, 700, "Snowy white, with a blue scarf for the ice planets", new CharacterLook
        {
            Legs = 0xE8EDF2, Body = 0xF4F7FA, Arms = 0xF4F7FA, Hands = 0xE8EDF2, Head = 0xFAFCFF,
            Details = (t, l) => { Box(t, 0.22f, 0.13f, 0.04f, 0xFFFFFF, 0, 0.86f, -0.27f); Box(t, 0.08f, 0.06f, 0.02f, 0x222222, 0, 0.9f, -0.295f); Scarf(t, 0x4C8FE8); },
            Top = (t, l) => RoundEars(t, l.Head, 0xE3E9F0),
        }),
        Chr("axolotl", "Axolotl", 1, 800, "Pink with frilly gills and a smile", new CharacterLook
        {
            Legs = 0xF29AB8, Body = 0xF7A8C4, Arms = 0xF7A8C4, Hands = 0xF29AB8, Head = 0xFAB6CD, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0x2B1B22, 0.06f, 0.06f, 0.15f);
                Box(t, 0.16f, 0.025f, 0.02f, 0xC2577E, 0, 0.9f, -0.27f);
                Blush(t);
                Both(s =>   // the gills poke out behind the raised arms
                {
                    Box(t, 0.32f, 0.05f, 0.05f, 0xE0507F, s * 0.42f, 1.12f, 0.12f, s * 25);
                    Box(t, 0.32f, 0.05f, 0.05f, 0xE0507F, s * 0.44f, 1.0f, 0.12f);
                    Box(t, 0.32f, 0.05f, 0.05f, 0xE0507F, s * 0.42f, 0.88f, 0.12f, -s * 25);
                });
                Box(t, 0.04f, 0.22f, 0.36f, 0xF29AB8, 0, 0.3f, 0.36f);
            },
        }),
        Chr("bee", "Bee", 1, 600, "Striped, with wings and antennae", new CharacterLook
        {
            Legs = 0x2B2B2B, Body = 0xFFC93C, Arms = 0x2B2B2B, Hands = 0x2B2B2B, Head = 0xFFD54F,
            Details = (t, l) => { Band(t, 0x2B2B2B, 0.42f); Band(t, 0x2B2B2B, 0.6f); Blush(t); },
            Back = (t, l) => Both(s => Box(t, 0.28f, 0.24f, 0.03f, 0xDDF1FF, s * 0.2f, 0.72f, 0.24f, s * 25)),
            Top = (t, l) => Antennae(t, 0x2B2B2B, 0x2B2B2B),
        }),
        Chr("ladybug", "Ladybug", 1, 600, "A spotted red shell on the back", new CharacterLook
        {
            Legs = 0x222222, Body = 0xE53935, Arms = 0x222222, Hands = 0x222222, Head = 0x2B2B2B, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0xFFFFFF, 0.1f, 0.1f);
                Both(s => Box(t, 0.05f, 0.05f, 0.02f, 0x111111, s * 0.12f, 0.99f, -0.285f));
                Box(t, 0.12f, 0.03f, 0.02f, 0xFFFFFF, 0, 0.86f, -0.27f);
                foreach (var p in new[] { new Vector2(-0.15f, 0.6f), new Vector2(0.15f, 0.62f), new Vector2(0, 0.42f) })
                    Box(t, 0.08f, 0.08f, 0.02f, 0x222222, p.x, p.y, -0.21f);
            },
            Back = (t, l) =>
            {
                Box(t, 0.62f, 0.5f, 0.1f, 0xE53935, 0, 0.52f, 0.24f);
                Box(t, 0.03f, 0.5f, 0.02f, 0x222222, 0, 0.52f, 0.3f);
                Both(s => { Box(t, 0.1f, 0.1f, 0.02f, 0x222222, s * 0.15f, 0.64f, 0.3f); Box(t, 0.1f, 0.1f, 0.02f, 0x222222, s * 0.17f, 0.4f, 0.3f); });
            },
            Top = (t, l) => Antennae(t, 0x222222, 0x222222),
        }),
        Chr("snowman", "Snowman", 1, 800, "Carrot nose, coal buttons and a red scarf", new CharacterLook
        {
            Legs = 0xF2F5F8, Body = 0xFFFFFF, Arms = 0x6D4C2F, Hands = 0x6D4C2F, Head = 0xFFFFFF, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0x222222, 0.07f, 0.07f, 0.11f, 1.02f);
                Box(t, 0.06f, 0.06f, 0.18f, 0xF57C1F, 0, 0.94f, -0.33f);
                foreach (var p in new[] { -0.09f, -0.03f, 0.03f, 0.09f }) Box(t, 0.04f, 0.04f, 0.02f, 0x222222, p, Mathf.Abs(p) > 0.05f ? 0.85f : 0.83f, -0.27f);
                foreach (float y in new[] { 0.62f, 0.45f }) Box(t, 0.06f, 0.06f, 0.02f, 0x222222, 0, y, -0.21f);
                Scarf(t, 0xE53935);
            },
            Top = (t, l) => { Box(t, 0.03f, 0.12f, 0.03f, 0x6D4C2F, 0.1f, 0.05f, 0, -20); Box(t, 0.03f, 0.06f, 0.03f, 0x6D4C2F, 0.14f, 0.1f, 0, 30); },
        }),
        Chr("chef", "Chef", 1, 500, "A tall white hat and a fancy mustache", new CharacterLook
        {
            Legs = 0x3A3A3A, Body = 0xFFFFFF, Arms = 0xFFFFFF, Hands = Skin, Head = Skin,
            Details = (t, l) =>
            {
                Both(s => { Box(t, 0.05f, 0.05f, 0.02f, 0x222222, s * 0.1f, 0.6f, -0.21f); Box(t, 0.05f, 0.05f, 0.02f, 0x222222, s * 0.1f, 0.46f, -0.21f); });
                Box(t, 0.2f, 0.08f, 0.02f, 0xE53935, 0, 0.71f, -0.21f);
                Box(t, 0.22f, 0.05f, 0.02f, 0x5D3A1A, 0, 0.88f, -0.272f);
                Both(s => Box(t, 0.07f, 0.04f, 0.02f, 0x5D3A1A, s * 0.13f, 0.86f, -0.272f, -s * 20));
            },
            Top = (t, l) => { Box(t, 0.5f, 0.1f, 0.46f, 0xFFFFFF, 0, 0.05f, 0); Box(t, 0.58f, 0.26f, 0.52f, 0xFAFAFA, 0, 0.22f, 0); },
        }),
        Chr("firefighter", "Firefighter", 1, 900, "A red helmet and a reflective jacket", new CharacterLook
        {
            Legs = 0x2E3A59, Body = 0x2E3A59, Arms = 0x2E3A59, Hands = 0xF2C94C, Head = Skin,
            Details = (t, l) => { Band(t, 0xF2E94C, 0.42f, 0.05f); Box(t, 0.08f, 0.08f, 0.02f, 0xFFC83D, -0.15f, 0.62f, -0.21f); },
            Top = (t, l) =>
            {
                Box(t, 0.6f, 0.16f, 0.56f, 0xD32F2F, 0, 0.08f, 0);
                Box(t, 0.6f, 0.04f, 0.2f, 0xD32F2F, 0, 0.01f, 0.35f);
                Box(t, 0.14f, 0.12f, 0.02f, 0xFFC83D, 0, 0.1f, -0.29f);
            },
        }),

        // Mega
        Chr("ghost", "Ghost", 2, 1200, "A friendly little ghost", new CharacterLook
        {
            Legs = 0xEEF1F8, Body = 0xF4F6FB, Arms = 0xF4F6FB, Hands = 0xF4F6FB, Head = 0xF7F9FD, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0x222222, 0.08f, 0.12f, 0.11f);
                Box(t, 0.06f, 0.08f, 0.02f, 0x222222, 0, 0.86f, -0.275f);
                Blush(t);
                Box(t, 0.64f, 0.1f, 0.44f, 0xF4F6FB, 0, 0.3f, 0);
                foreach (float x in new[] { -0.22f, 0, 0.22f }) Box(t, 0.12f, 0.08f, 0.12f, 0xF4F6FB, x, 0.22f, -0.16f);
            },
            Top = (t, l) => Box(t, 0.08f, 0.1f, 0.08f, 0xF4F6FB, 0, 0.04f, 0.05f, -20),
        }),
        Chr("pumpkinhead", "Pumpkin head", 2, 1300, "A carved pumpkin for a head", new CharacterLook
        {
            Legs = 0x2B2B2B, Body = 0x4A3366, Arms = 0x4A3366, Hands = 0x3D7A2E, Head = 0xF57C1F, OwnEyes = true,
            Details = (t, l) =>
            {
                Both(s => Box(t, 0.04f, 0.45f, 0.02f, 0xE06A10, s * 0.24f, 0.97f, -0.255f));
                Both(s => { Box(t, 0.11f, 0.09f, 0.02f, 0x2B1B0A, s * 0.12f, 1.02f, -0.26f, s * 12); Box(t, 0.05f, 0.04f, 0.02f, 0xFFD54F, s * 0.12f, 1.01f, -0.265f); });
                Box(t, 0.3f, 0.07f, 0.02f, 0x2B1B0A, 0, 0.86f, -0.26f);
                Both(s => Box(t, 0.05f, 0.03f, 0.02f, 0xF57C1F, s * 0.07f, 0.885f, -0.265f));
            },
            Top = (t, l) => { Box(t, 0.08f, 0.12f, 0.08f, 0x5D7A2E, 0, 0.06f, 0); Box(t, 0.14f, 0.03f, 0.08f, 0x5DAE4B, 0.09f, 0.03f, 0, -15); },
        }),
        Chr("mummy", "Mummy", 2, 1400, "All wrapped up, with glowing eyes", new CharacterLook
        {
            Legs = 0xE3D7B8, Body = 0xEDE3C8, Arms = 0xEDE3C8, Hands = 0xE3D7B8, Head = 0xEDE3C8, OwnEyes = true,
            Details = (t, l) =>
            {
                foreach (var (y, tilt) in new[] { (0.36f, 4f), (0.5f, -5f), (0.64f, 3f) }) Box(t, 0.62f, 0.03f, 0.42f, 0xCDBF9C, 0, y, 0, tilt);
                foreach (var (y, tilt) in new[] { (0.84f, -4f), (1.13f, 5f) }) HeadBand(t, 0xCDBF9C, y, 0.03f, tilt);
                Box(t, 0.42f, 0.1f, 0.02f, 0x2B2B2B, 0, 1.0f, -0.258f);
                Eyes(t, 0xFFE14D, 0.06f, 0.05f, 0.1f);
                Box(t, 0.06f, 0.24f, 0.02f, 0xEDE3C8, 0.3f, 0.6f, -0.12f, 20);
            },
        }),
        Chr("pirate", "Pirate", 2, 1500, "An eye patch, a bandana and a striped shirt", new CharacterLook
        {
            Legs = 0x3B5BA5, Body = 0xF5F5F5, Arms = 0xF5F5F5, Hands = Skin, Head = Skin, OwnEyes = true,
            Details = (t, l) =>
            {
                Box(t, 0.08f, 0.1f, 0.02f, 0x111111, -0.12f, 1.0f, -0.275f);
                Box(t, 0.13f, 0.12f, 0.02f, 0x111111, 0.12f, 1.0f, -0.275f);
                HeadBand(t, 0x111111, 1.06f, 0.03f, 15);
                foreach (float y in new[] { 0.42f, 0.56f, 0.7f }) Band(t, 0xD32F2F, y, 0.05f);
                Band(t, 0x5D3A1A, 0.33f, 0.06f);
                Box(t, 0.3f, 0.06f, 0.02f, 0x6D4C2F, 0, 0.82f, -0.265f);
            },
            Top = (t, l) => { Box(t, 0.58f, 0.12f, 0.53f, 0xD32F2F, 0, 0.04f, 0); Box(t, 0.08f, 0.12f, 0.04f, 0xD32F2F, 0.15f, -0.05f, 0.28f); },
        }),
        Chr("knight", "Knight", 2, 1800, "Shining armor and a feather plume", new CharacterLook
        {
            Legs = 0x8A96A2, Body = 0xA9B4BF, Arms = 0xA9B4BF, Hands = 0x7D8894, Head = 0xBAC4CE, OwnEyes = true,
            Details = (t, l) =>
            {
                Box(t, 0.42f, 0.05f, 0.02f, 0x2B2B2B, 0, 1.0f, -0.26f);
                foreach (float x in new[] { -0.08f, -0.03f, 0.03f, 0.08f }) Box(t, 0.03f, 0.08f, 0.02f, 0x5A6470, x, 0.87f, -0.26f);
                Box(t, 0.3f, 0.42f, 0.02f, 0x2F5FC0, 0, 0.5f, -0.21f);
                Box(t, 0.06f, 0.22f, 0.02f, 0xFFD54F, 0, 0.52f, -0.222f);
                Box(t, 0.16f, 0.05f, 0.02f, 0xFFD54F, 0, 0.56f, -0.222f);
            },
            Top = (t, l) => { Box(t, 0.08f, 0.14f, 0.26f, 0xE53935, 0, 0.07f, 0.05f); Box(t, 0.08f, 0.1f, 0.14f, 0xE53935, 0, 0.13f, 0.15f); },
        }),
        Chr("wizard", "Wizard", 2, 1600, "A long beard, a starry robe and a pointy hat", new CharacterLook
        {
            Legs = 0x4A3388, Body = 0x5B3FA0, Arms = 0x5B3FA0, Hands = Skin, Head = Skin,
            Details = (t, l) =>
            {
                Box(t, 0.38f, 0.3f, 0.06f, 0xF2F2F2, 0, 0.83f, -0.27f);
                Box(t, 0.2f, 0.16f, 0.05f, 0xF2F2F2, 0, 0.62f, -0.24f);
                Both(s => Box(t, 0.1f, 0.03f, 0.02f, 0xF2F2F2, s * 0.12f, 1.08f, -0.27f));
                Box(t, 0.06f, 0.06f, 0.02f, 0xFFD54F, 0.18f, 0.4f, -0.21f, 45);
                Box(t, 0.05f, 0.05f, 0.02f, 0xFFD54F, -0.2f, 0.6f, -0.21f, 45);
            },
            Top = (t, l) =>
            {
                Box(t, 0.66f, 0.05f, 0.6f, 0x4A3388, 0, 0.02f, 0);
                Box(t, 0.44f, 0.14f, 0.4f, 0x4A3388, 0.02f, 0.11f, 0);
                Box(t, 0.3f, 0.14f, 0.28f, 0x4A3388, 0.05f, 0.25f, 0);
                Box(t, 0.16f, 0.14f, 0.16f, 0x4A3388, 0.12f, 0.37f, 0, -25);
                Box(t, 0.06f, 0.06f, 0.02f, 0xFFD54F, 0.04f, 0.14f, -0.205f, 45);
            },
        }),
        Chr("samurai", "Samurai", 2, 2000, "Red lacquer armor and a golden kabuto crest", new CharacterLook
        {
            Legs = 0x2B2B2B, Body = 0x8E1F1F, Arms = 0x8E1F1F, Hands = 0x2B2B2B, Head = Skin,
            Details = (t, l) =>
            {
                foreach (float y in new[] { 0.38f, 0.5f, 0.62f }) Band(t, 0xD4A23A, y, 0.03f);
                Box(t, 0.2f, 0.04f, 0.02f, 0x222222, 0, 0.87f, -0.27f);
                Both(s => Box(t, 0.2f, 0.12f, 0.46f, 0x8E1F1F, s * 0.34f, 0.72f, 0));
            },
            Top = (t, l) =>
            {
                Box(t, 0.6f, 0.14f, 0.56f, 0x2B2B2B, 0, 0.07f, 0);
                Both(s => Box(t, 0.08f, 0.16f, 0.4f, 0x2B2B2B, s * 0.32f, -0.04f, 0.05f));
                Both(s => Box(t, 0.04f, 0.24f, 0.04f, 0xD4A23A, s * 0.09f, 0.24f, -0.27f, -s * 22));
                Box(t, 0.1f, 0.07f, 0.04f, 0xD4A23A, 0, 0.12f, -0.29f);
            },
        }),
        Chr("diver", "Deep-sea diver", 2, 1800, "A brass helmet with a round window", new CharacterLook
        {
            Legs = 0x6E5230, Body = 0x8E6B3E, Arms = 0x8E6B3E, Hands = 0x3A3A3A, Head = 0xC98A2B, OwnEyes = true,
            Details = (t, l) =>
            {
                Box(t, 0.3f, 0.26f, 0.02f, 0x2F4A5C, 0, 0.98f, -0.26f);
                Box(t, 0.36f, 0.04f, 0.02f, 0xE0A84A, 0, 1.13f, -0.265f);
                Box(t, 0.36f, 0.04f, 0.02f, 0xE0A84A, 0, 0.83f, -0.265f);
                Both(s => Box(t, 0.04f, 0.3f, 0.02f, 0xE0A84A, s * 0.17f, 0.98f, -0.265f));
                Eyes(t, 0xFFFFFF, 0.05f, 0.06f, 0.07f, 0.99f);
                Both(s => Box(t, 0.02f, 0.14f, 0.14f, 0x2F4A5C, s * 0.28f, 0.98f, 0));
                Box(t, 0.66f, 0.08f, 0.48f, 0xE0A84A, 0, 0.75f, 0);
            },
            Back = (t, l) => Box(t, 0.3f, 0.32f, 0.12f, 0xC98A2B, 0, 0.55f, 0.26f),
            Top = (t, l) => Box(t, 0.12f, 0.06f, 0.12f, 0xE0A84A, 0, 0.03f, 0),
        }),
        Chr("alien", "Alien", 2, 1500, "Big black eyes and bobbly antennae", new CharacterLook
        {
            Legs = 0x9AA5B1, Body = 0xB8C2CC, Arms = 0xB8C2CC, Hands = 0x6CC746, Head = 0x8BE563, OwnEyes = true,
            Details = (t, l) =>
            {
                Both(s => Box(t, 0.16f, 0.12f, 0.02f, 0x111111, s * 0.13f, 1.0f, -0.275f, -s * 15));
                Both(s => Box(t, 0.04f, 0.04f, 0.01f, 0xFFFFFF, s * 0.1f, 1.03f, -0.287f));
                Box(t, 0.08f, 0.02f, 0.02f, 0x2E7D32, 0, 0.86f, -0.27f);
                Box(t, 0.12f, 0.12f, 0.02f, 0x8BE563, 0, 0.58f, -0.21f, 45);
            },
            Top = (t, l) => Antennae(t, 0x6CC746, 0xFF66C4),
        }),
        Chr("dino", "Dino", 2, 1600, "A little T. rex with spikes down its back", new CharacterLook
        {
            Legs = 0x4CA352, Body = 0x5DBB63, Arms = 0x5DBB63, Hands = 0x4CA352, Head = 0x67C76D,
            Details = (t, l) =>
            {
                Belly(t, 0xF5E6A8, 0.36f, 0.34f);
                Box(t, 0.42f, 0.14f, 0.08f, 0x67C76D, 0, 0.83f, -0.28f);
                for (int i = 0; i < 4; i++) Box(t, 0.04f, 0.04f, 0.02f, 0xFFFFFF, -0.12f + i * 0.08f, 0.77f, -0.32f);
                Both(s => Box(t, 0.03f, 0.03f, 0.02f, 0x2E5E22, s * 0.06f, 0.87f, -0.325f));
                Box(t, 0.18f, 0.18f, 0.3f, l.Body, 0, 0.25f, 0.33f);
                Box(t, 0.1f, 0.1f, 0.2f, l.Body, 0, 0.18f, 0.55f);
            },
            Back = (t, l) => { foreach (var (y, z) in new[] { (1.12f, 0.27f), (0.95f, 0.27f), (0.66f, 0.22f), (0.46f, 0.22f) }) Bx(t, 0.06f, 0.1f, 0.1f, 0xF2994A, 0, y, z, rx: 45); },
        }),

        // Ultra
        Chr("dragon", "Dragon", 3, 3500, "Horns, wings and a spade-tipped tail", new CharacterLook
        {
            Legs = 0xB83333, Body = 0xD84343, Arms = 0xD84343, Hands = 0xB83333, Head = 0xE04B4B,
            Details = (t, l) =>
            {
                Belly(t, 0xF5C26B, 0.34f, 0.34f);
                foreach (float y in new[] { 0.42f, 0.52f, 0.62f }) Box(t, 0.34f, 0.02f, 0.02f, 0xE0A04A, 0, y, -0.222f);
                Box(t, 0.3f, 0.12f, 0.06f, 0xE04B4B, 0, 0.85f, -0.28f);
                Both(s => Box(t, 0.03f, 0.03f, 0.02f, 0x5A1A14, s * 0.06f, 0.88f, -0.315f));
                var tail = Pivot(t, 0, 0.25f, 0.2f, 30, 0, 0);
                Box(tail, 0.16f, 0.16f, 0.36f, l.Body, 0, 0, 0.18f);
                Box(tail, 0.2f, 0.04f, 0.14f, 0xF5C26B, 0, 0, 0.42f);
            },
            Back = (t, l) => Both(s =>
            {
                var wing = Pivot(t, s * 0.22f, 0.78f, 0.26f, 0, -s * 20, s * 25);
                Box(wing, 0.42f, 0.05f, 0.05f, 0xB83333, s * 0.2f, 0.12f, 0);
                Box(wing, 0.36f, 0.24f, 0.02f, 0xF08A5A, s * 0.2f, -0.02f, 0);
            }),
            Top = (t, l) => Both(s => Box(t, 0.06f, 0.16f, 0.06f, 0xF5E6C8, s * 0.16f, 0.08f, 0.05f, -s * 20)),
        }),
        Chr("unicorn", "Unicorn", 3, 3000, "A golden horn and a rainbow mane", new CharacterLook
        {
            Legs = 0xF5F0FF, Body = 0xFFFFFF, Arms = 0xFFFFFF, Hands = 0xE6D9FF, Head = 0xFFFFFF,
            Details = (t, l) =>
            {
                Box(t, 0.24f, 0.12f, 0.04f, 0xFCE4EC, 0, 0.85f, -0.27f);
                Blush(t);
                uint[] rainbow = { 0xFF8A8A, 0xFFC56B, 0xFFEE80, 0x8EE08E, 0x7FC2FF, 0xBC94FF };
                for (int i = 0; i < rainbow.Length; i++) Box(t, 0.14f, 0.08f, 0.1f, rainbow[i], 0, 1.17f - i * 0.08f, 0.28f);
                for (int i = 0; i < 3; i++) Bx(t, 0.1f, 0.12f, 0.1f, rainbow[i * 2], 0, 0.5f - i * 0.1f, 0.26f + i * 0.04f, rx: -20);
            },
            Top = (t, l) =>
            {
                Box(t, 0.1f, 0.12f, 0.1f, 0xFFD54F, 0, 0.06f, -0.12f);
                Box(t, 0.07f, 0.1f, 0.07f, 0xFFD54F, 0, 0.16f, -0.12f);
                Box(t, 0.04f, 0.08f, 0.04f, 0xFFE082, 0, 0.25f, -0.12f);
                Both(s => Box(t, 0.08f, 0.1f, 0.06f, 0xFFFFFF, s * 0.18f, 0.05f, 0.05f));
            },
        }),
        Chr("yeti", "Yeti", 3, 2800, "Shaggy white fur and a blue face", new CharacterLook
        {
            Legs = 0xE2E8EE, Body = 0xF0F4F8, Arms = 0xF0F4F8, Hands = 0x8FB8E0, Head = 0xF5F8FB, OwnEyes = true,
            Details = (t, l) =>
            {
                Box(t, 0.38f, 0.28f, 0.02f, 0x8FB8E0, 0, 0.92f, -0.258f);
                Eyes(t, 0x111111, 0.06f, 0.08f, 0.09f);
                Box(t, 0.2f, 0.04f, 0.02f, 0x2B3A55, 0, 0.84f, -0.272f);
                Both(s => Box(t, 0.03f, 0.04f, 0.02f, 0xFFFFFF, s * 0.06f, 0.82f, -0.282f));
                Both(s => { Box(t, 0.1f, 0.1f, 0.1f, 0xF5F8FB, s * 0.31f, 1.05f, 0); Box(t, 0.1f, 0.1f, 0.1f, 0xF5F8FB, s * 0.31f, 0.86f, 0.05f); Box(t, 0.1f, 0.1f, 0.1f, 0xF0F4F8, s * 0.33f, 0.45f, 0); });
            },
            Top = (t, l) => { Box(t, 0.12f, 0.12f, 0.12f, 0xF5F8FB, -0.08f, 0.05f, 0); Box(t, 0.12f, 0.16f, 0.12f, 0xF5F8FB, 0.06f, 0.07f, 0.04f, -15); },
        }),
        Chr("lavagolem", "Lava golem", 3, 3200, "Dark rock with glowing cracks, for hot planets", new CharacterLook
        {
            Legs = 0x2E2424, Body = 0x3A2E2E, Arms = 0x3A2E2E, Hands = 0x2E2424, Head = 0x463838, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0xFF8A1F, 0.1f, 0.06f);
                Box(t, 0.18f, 0.04f, 0.02f, 0xFF8A1F, 0, 0.86f, -0.27f);
                Box(t, 0.04f, 0.22f, 0.02f, 0xFF6A1A, -0.12f, 0.55f, -0.21f, 25);
                Box(t, 0.16f, 0.03f, 0.02f, 0xFFA43A, 0.1f, 0.45f, -0.21f);
                Box(t, 0.03f, 0.14f, 0.02f, 0xFF6A1A, 0.18f, 1.1f, -0.26f, -20);
                Both(s => Box(t, 0.2f, 0.14f, 0.2f, 0x463838, s * 0.3f, 0.76f, 0));
            },
            Top = (t, l) =>
            {
                foreach (var (x, h) in new[] { (-0.12f, 0.14f), (0f, 0.2f), (0.12f, 0.12f) })
                {
                    Bx(t, 0.1f, h, 0.1f, 0xFF8A1F, x, h / 2, 0, shadow: false);
                    Bx(t, 0.06f, h * 0.6f, 0.11f, 0xFFD23D, x, h * 0.3f, 0, shadow: false);
                }
            },
        }),
        Chr("frostgolem", "Frost golem", 3, 3200, "Made of ice, with crystal spikes, for ice planets", new CharacterLook
        {
            Legs = 0x6FBFE8, Body = 0x8FD3F4, Arms = 0x8FD3F4, Hands = 0xBFE9FA, Head = 0xA8DFF7, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0x1E4E8C, 0.08f, 0.08f);
                Box(t, 0.14f, 0.03f, 0.02f, 0x1E4E8C, 0, 0.86f, -0.27f);
                Both(s => Box(t, 0.1f, 0.24f, 0.1f, 0xE0F7FF, s * 0.27f, 0.85f, 0, -s * 25));
                Box(t, 0.12f, 0.12f, 0.02f, 0xE0F7FF, -0.12f, 0.58f, -0.21f, 45);
            },
            Top = (t, l) =>
            {
                Both(s => Box(t, 0.08f, 0.2f, 0.08f, 0xE0F7FF, s * 0.13f, 0.09f, 0, -s * 15));
                Box(t, 0.1f, 0.28f, 0.1f, 0xE0F7FF, 0, 0.14f, 0);
            },
        }),
        Chr("kitsune", "Kitsune", 3, 3800, "A white fox spirit with three tails", new CharacterLook
        {
            Legs = 0xE53935, Body = 0xF5F0E8, Arms = 0xF5F0E8, Hands = 0xFFFFFF, Head = 0xFFFFFF, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0x2B1B14, 0.1f, 0.03f);
                Both(s => { Box(t, 0.04f, 0.1f, 0.02f, 0xE53935, s * 0.12f, 1.09f, -0.262f); Box(t, 0.08f, 0.03f, 0.02f, 0xE53935, s * 0.2f, 0.92f, -0.262f); });
                Nose(t, 0x222222, 0.9f, 0.05f);
                Band(t, 0xE53935, 0.4f, 0.08f);
                Box(t, 0.04f, 0.4f, 0.02f, 0xE53935, 0, 0.55f, -0.215f, 30);
            },
            Back = (t, l) =>
            {
                foreach (float angle in new[] { -35f, 0f, 35f })
                {
                    var tail = Pivot(t, 0, 0.42f, 0.24f, -30, 0, angle);
                    Box(tail, 0.15f, 0.42f, 0.13f, 0xFFFFFF, 0, 0.21f, 0);
                    Box(tail, 0.155f, 0.1f, 0.135f, 0xE53935, 0, 0.4f, 0);
                }
            },
            Top = (t, l) => PointyEars(t, 0xFFFFFF, 0xE53935),
        }),
        Chr("oni", "Oni", 3, 3000, "A red ogre with horns and tiger-striped shorts", new CharacterLook
        {
            Legs = 0xF2B33D, Body = 0xE85D4A, Arms = 0xE85D4A, Hands = 0xE85D4A, Head = 0xEF6A55, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0xFFFFFF, 0.1f, 0.08f, 0.12f, 1.01f);
                Both(s => Box(t, 0.04f, 0.05f, 0.02f, 0x111111, s * 0.12f, 1.0f, -0.285f));
                Both(s => Box(t, 0.12f, 0.04f, 0.02f, 0x2B1B14, s * 0.12f, 1.09f, -0.27f, s * 15));
                Box(t, 0.22f, 0.04f, 0.02f, 0x5A1A14, 0, 0.86f, -0.27f);
                Both(s => Box(t, 0.04f, 0.05f, 0.02f, 0xFFFFFF, s * 0.07f, 0.88f, -0.275f));
                Box(t, 0.62f, 0.16f, 0.42f, 0xF2B33D, 0, 0.36f, 0);
                foreach (float x in new[] { -0.2f, 0, 0.2f }) Box(t, 0.04f, 0.16f, 0.02f, 0x2B1B14, x, 0.36f, -0.215f);
            },
            Top = (t, l) =>
            {
                Box(t, 0.56f, 0.08f, 0.5f, 0x2B1B14, 0, 0.03f, 0.02f);
                Both(s => { Box(t, 0.08f, 0.16f, 0.08f, 0xF5E6C8, s * 0.15f, 0.1f, 0); Box(t, 0.05f, 0.08f, 0.05f, 0xF5E6C8, s * 0.17f, 0.2f, 0); });
            },
        }),

        // Legendary
        Chr("phoenix", "Phoenix", 4, 5500, "A firebird with flaming wings", new CharacterLook
        {
            Legs = 0xFFC83D, Body = 0xFF6A1A, Arms = 0xFF8A1F, Hands = 0xFFC83D, Head = 0xFF7A2A,
            Details = (t, l) =>
            {
                Box(t, 0.44f, 0.15f, 0.02f, 0xFFC83D, 0, 1.0f, -0.258f);
                Box(t, 0.1f, 0.08f, 0.08f, 0xFFD23D, 0, 0.89f, -0.29f);
                Belly(t, 0xFFD23D, 0.34f, 0.3f);
                foreach (var (x, c) in new[] { (-0.1f, 0xFF4A1Au), (0f, 0xFFD23Du), (0.1f, 0xFF8A1Fu) })
                    Bx(t, 0.09f, 0.36f, 0.04f, c, x, 0.22f, 0.3f, rx: -30, rz: x * -100);
            },
            Back = (t, l) => Both(s =>
            {
                var wing = Pivot(t, s * 0.24f, 0.8f, 0.24f, 0, -s * 15, s * 30);
                Box(wing, 0.4f, 0.12f, 0.04f, 0xFF4A1A, s * 0.2f, 0.06f, 0);
                Box(wing, 0.32f, 0.1f, 0.04f, 0xFF8A1F, s * 0.17f, -0.05f, 0);
                Box(wing, 0.24f, 0.1f, 0.04f, 0xFFD23D, s * 0.13f, -0.15f, 0);
            }),
            Top = (t, l) =>
            {
                Box(t, 0.08f, 0.2f, 0.08f, 0xFFD23D, 0, 0.1f, 0);
                Both(s => Box(t, 0.07f, 0.14f, 0.07f, 0xFF8A1F, s * 0.08f, 0.06f, 0.04f, -s * 20));
            },
        }),
        Chr("starchild", "Star child", 4, 5000, "Made of night sky, with star eyes", new CharacterLook
        {
            Legs = 0x1E1A4D, Body = 0x2A2466, Arms = 0x2A2466, Hands = 0xFFE08A, Head = 0x332C7A, OwnEyes = true,
            Details = (t, l) =>
            {
                Both(s => Box(t, 0.09f, 0.09f, 0.02f, 0xFFE08A, s * 0.12f, 1.0f, -0.275f, 45));
                Box(t, 0.12f, 0.03f, 0.02f, 0xFFE08A, 0, 0.87f, -0.272f);
                foreach (var (x, y, c) in new[] { (-0.18f, 0.62f, 0xFFFFFFu), (0.12f, 0.5f, 0xFFE08Au), (-0.05f, 0.38f, 0xB39DFFu), (0.2f, 0.66f, 0xFFFFFFu), (0.18f, 1.12f, 0xFFFFFFu), (-0.2f, 0.84f, 0xB39DFFu) })
                    Box(t, 0.04f, 0.04f, 0.02f, c, x, y, y > 0.75f ? -0.258f : -0.21f);
            },
            Top = (t, l) =>
            {
                var star = Pivot(t, 0, 0.3f, 0);
                Box(star, 0.1f, 0.1f, 0.06f, 0xFFE08A, 0, 0, 0);
                foreach (var p in new[] { new Vector2(0, 0.08f), new Vector2(0, -0.08f), new Vector2(0.08f, 0), new Vector2(-0.08f, 0) })
                    Box(star, 0.06f, 0.06f, 0.06f, 0xFFE08A, p.x, p.y, 0);
                star.gameObject.AddComponent<Spinner>().DegreesPerSecond = 90;
            },
        }),
        Chr("goldmech", "Golden mech", 4, 5500, "A shiny golden robot with a glowing core", new CharacterLook
        {
            Legs = 0xC9952B, Body = 0xE0B03A, Arms = 0xE0B03A, Hands = 0x8A6A20, Head = 0xF0C24E, OwnEyes = true,
            Details = (t, l) =>
            {
                Box(t, 0.45f, 0.13f, 0.02f, 0x1E2A44, 0, 1.0f, -0.26f);
                Eyes(t, 0x7FE7FF, 0.1f, 0.05f, 0.1f);
                Box(t, 0.18f, 0.18f, 0.02f, 0x8A6A20, 0, 0.58f, -0.21f);
                Box(t, 0.11f, 0.11f, 0.02f, 0x7FE7FF, 0, 0.58f, -0.222f);
                Both(s => Box(t, 0.2f, 0.12f, 0.46f, 0xF0C24E, s * 0.34f, 0.74f, 0));
            },
            Back = (t, l) => Both(s => { Box(t, 0.14f, 0.3f, 0.14f, 0x8A6A20, s * 0.12f, 0.55f, 0.27f); Bx(t, 0.1f, 0.08f, 0.1f, 0x7FE7FF, s * 0.12f, 0.37f, 0.27f, shadow: false); }),
            Top = (t, l) => Both(s => Box(t, 0.04f, 0.18f, 0.14f, 0xF0C24E, s * 0.22f, 0.06f, 0, -s * 15)),
        }),
        Chr("moonrabbit", "Moon rabbit", 4, 5000, "The rabbit in the moon, in an indigo kimono", new CharacterLook
        {
            Legs = 0x2E2B6E, Body = 0x3D3A8C, Arms = 0x3D3A8C, Hands = 0xFFFFFF, Head = 0xFFFFFF, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0xD32F2F, 0.08f, 0.1f);
                Shine(t);
                Nose(t, 0xF28CA0, 0.9f, 0.06f);
                Blush(t);
                Box(t, 0.12f, 0.12f, 0.02f, 0xFFE08A, 0.12f, 0.6f, -0.21f);
                Box(t, 0.09f, 0.09f, 0.02f, 0x3D3A8C, 0.15f, 0.62f, -0.215f);
                Band(t, 0xFFD54F, 0.4f, 0.07f);
                Box(t, 0.04f, 0.42f, 0.02f, 0xFFFFFF, 0, 0.55f, -0.215f, -30);
            },
            Back = (t, l) => { Box(t, 0.06f, 0.42f, 0.06f, 0x8D5A2B, 0.08f, 0.55f, 0.25f, 30); Box(t, 0.24f, 0.13f, 0.13f, 0xA0693A, 0.2f, 0.76f, 0.25f, 30); },
            Top = (t, l) => Both(s =>
            {
                Box(t, 0.12f, 0.38f, 0.08f, 0xFFFFFF, s * 0.12f, 0.19f, 0, -s * 8);
                Box(t, 0.06f, 0.28f, 0.02f, 0xF8BBD0, s * 0.12f, 0.19f, -0.045f, -s * 8);
            }),
        }),
        Chr("meteor", "Meteor", 4, 6000, "A walking meteor, trailing fire: the boss planets' own", new CharacterLook
        {
            Legs = 0x3A2C26, Body = 0x4A3A33, Arms = 0x4A3A33, Hands = 0x3A2C26, Head = 0x5A463D, OwnEyes = true,
            Details = (t, l) =>
            {
                Eyes(t, 0xFFD23D, 0.1f, 0.08f);
                Box(t, 0.1f, 0.1f, 0.02f, 0x3A2C26, 0.17f, 1.1f, -0.258f);
                Box(t, 0.08f, 0.08f, 0.02f, 0x3A2C26, -0.18f, 0.84f, -0.258f);
                Box(t, 0.16f, 0.16f, 0.02f, 0xFF8A1F, 0, 0.55f, -0.21f);
                Box(t, 0.08f, 0.08f, 0.02f, 0xFFD23D, 0, 0.55f, -0.222f);
                Box(t, 0.1f, 0.1f, 0.02f, 0x3A2C26, -0.18f, 0.4f, -0.21f);
            },
            Back = (t, l) => { Bx(t, 0.36f, 0.36f, 0.2f, 0xFF8A1F, 0, 0.85f, 0.32f, shadow: false); Bx(t, 0.24f, 0.24f, 0.22f, 0xFFD23D, 0, 0.9f, 0.48f, shadow: false); },
            Top = (t, l) =>
            {
                Bx(t, 0.42f, 0.12f, 0.32f, 0xFF8A1F, 0, 0.05f, 0.06f, shadow: false);
                Bx(t, 0.3f, 0.12f, 0.3f, 0xFFB13D, 0, 0.15f, 0.14f, shadow: false);
                Bx(t, 0.16f, 0.12f, 0.2f, 0xFFE14D, 0, 0.25f, 0.22f, shadow: false);
            },
        }),
    };
}
