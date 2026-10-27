using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CosmeticKind { Character, Hat, Backpack, Charm, Tag }

public enum Rarity { Common, Rare, Mega, Ultra, Legendary }

/// A character or hat: what it looks like and how you get it. Everything is either bought for coins
/// at a fixed price you see up front, earned with an achievement, found in the free daily gift, or free.
/// Nothing random is for sale. Which items exist (and their names, prices, rarity and colors) comes
/// from the item sheet, Resources/Items.csv; the 3D models are built in code below.
public class Cosmetic
{
    public string Id, Name;
    public CosmeticKind Kind;
    public Rarity Rarity;
    public bool InGift;               // can turn up in the daily gift
    public int Price;                 // coins; 0 = not for sale
    public string Achievement;        // the achievement that unlocks it, or null
    public bool Free;                 // owned from the start
    public CharacterLook Look;        // characters
    public Action<Transform> Build;   // hats: on the hat anchor (y = 0 is the top of the head); backpacks: on the
                                      // middle of the back (+z points away from the back); charms: hanging down from the hook
    public TagStyle Tag;              // tags
    public string Notes;              // candidates: a line about it, for the overview to pick from
}

/// A tag: the background behind your character in the closet, and your banner on the leaderboard.
/// A soft gradient with a pattern tiled over it in one ink color (drawn by TagArt).
public class TagStyle
{
    public uint[] Colors;          // the gradient, top to bottom
    public string Pattern;         // dots, stripes, checker, waves, zigzag, stars, hearts, flowers, clouds, or null
    public uint Ink = 0xFFFFFF;    // the pattern's color
    public float InkAlpha = 0.4f;
    public float Tile = 64;        // the pattern's size in UI points
    public Vector2 Drift;          // the pattern slowly slides along (tiles per second)
    public bool Rays;              // light rays turning behind the character

    /// The same tag in other colors (from the item sheet).
    public TagStyle Recolored(uint? top, uint? bottom, uint? ink) => new()
    {
        Colors = top == null && bottom == null ? Colors : new[] { top ?? Colors[0], bottom ?? Colors[^1] },
        Pattern = Pattern, Ink = ink ?? Ink, InkAlpha = InkAlpha, Tile = Tile, Drift = Drift, Rays = Rays,
    };
}

/// How a character is colored, plus its own details (faces, ears, helmets).
public class CharacterLook
{
    public uint Legs, Body, Arms, Hands, Head;
    public bool OwnEyes;                            // draws its own eyes instead of the default dots
    public float HatLift;                           // raise hats above tall heads
    public Action<Transform, CharacterLook> Details;   // on the rig: faces, tails, backpacks
    public Action<Transform, CharacterLook> Top;       // on top of the head (caps, ears): hidden under a hat
    public Action<Transform, CharacterLook> Back;      // on the back (a built-in pack): hidden under a backpack

    /// The same character in other colors (from the item sheet); details that follow the colors follow along.
    public CharacterLook Recolored(uint? legs, uint? body, uint? arms, uint? hands, uint? head) => new()
    {
        Legs = legs ?? Legs, Body = body ?? Body, Arms = arms ?? Arms, Hands = hands ?? Hands, Head = head ?? Head,
        OwnEyes = OwnEyes, HatLift = HatLift, Details = Details, Top = Top, Back = Back,
    };
}

public static partial class Cosmetics
{
    const uint Skin = 0xF5C99B;

    static GameObject Box(Transform t, float w, float h, float d, uint hex, float x, float y, float z, float tilt = 0)
    {
        var box = Voxel.Box(t, new Vector3(w, h, d), hex, new Vector3(x, y, z));
        if (tilt != 0) box.transform.localRotation = Quaternion.Euler(0, 0, tilt);
        return box;
    }

    // MARK: - Characters (the models; the item sheet picks from these by "base")

    static readonly List<Cosmetic> CharacterModels = new()
    {
        new Cosmetic { Id = "climber", Name = "Climber", Kind = CosmeticKind.Character, Free = true, Look = new CharacterLook
        {
            Legs = 0x3B5BA5, Body = 0xE8553E, Arms = 0xE8553E, Hands = Skin, Head = Skin,
            Top = (t, l) => { Box(t, 0.6f, 0.17f, 0.55f, 0x2F80ED, 0, 0.08f, 0); Box(t, 0.6f, 0.06f, 0.25f, 0x2569C9, 0, 0.01f, -0.36f); },
        } },
        new Cosmetic { Id = "cat", Name = "Cat", Kind = CosmeticKind.Character, Price = 200, Look = new CharacterLook
        {
            Legs = 0xE08A2A, Body = 0xF2A03D, Arms = 0xF2A03D, Hands = 0xFFE2B8, Head = 0xF2A03D,
            Details = (t, l) =>
            {
                Box(t, 0.26f, 0.14f, 0.02f, 0xFFF3E0, 0, 0.88f, -0.26f);    // muzzle
                Box(t, 0.08f, 0.06f, 0.02f, 0xF28CA0, 0, 0.93f, -0.275f);   // nose
                Box(t, 0.1f, 0.4f, 0.1f, l.Body, 0.16f, 0.2f, 0.3f, -40);       // tail, low enough to show under a backpack
            },
            Top = (t, l) =>
            {
                foreach (float x in new[] { -0.17f, 0.17f })
                {
                    Box(t, 0.15f, 0.16f, 0.12f, l.Head, x, 0.08f, 0);
                    Box(t, 0.08f, 0.09f, 0.02f, 0xF28CA0, x, 0.07f, -0.07f);
                }
            },
        } },
        new Cosmetic { Id = "frog", Name = "Frog", Kind = CosmeticKind.Character, Price = 200, Look = new CharacterLook
        {
            Legs = 0x4A9A3A, Body = 0x5DBB4A, Arms = 0x5DBB4A, Hands = 0x7BD35A, Head = 0x6CCB55, OwnEyes = true, HatLift = 0.12f,
            Details = (t, l) =>
            {
                Box(t, 0.4f, 0.3f, 0.02f, 0xC9EFA0, 0, 0.5f, -0.21f);       // belly
                Box(t, 0.3f, 0.03f, 0.02f, 0x2E5E22, 0, 0.88f, -0.26f);     // smile
                foreach (float x in new[] { -0.15f, 0.15f })                 // big eyes on top of the head
                {
                    Box(t, 0.18f, 0.16f, 0.18f, 0xFFFFFF, x, 1.25f, -0.08f);
                    Box(t, 0.08f, 0.08f, 0.02f, 0x111111, x, 1.26f, -0.18f);
                }
            },
        } },
        new Cosmetic { Id = "penguin", Name = "Penguin", Kind = CosmeticKind.Character, Price = 250, Look = new CharacterLook
        {
            Legs = 0xF29C38, Body = 0x2B2F3A, Arms = 0x2B2F3A, Hands = 0x2B2F3A, Head = 0x2B2F3A,
            Details = (t, l) =>
            {
                Box(t, 0.4f, 0.36f, 0.02f, 0xFFFFFF, 0, 0.52f, -0.21f);     // belly
                Box(t, 0.42f, 0.26f, 0.02f, 0xFFFFFF, 0, 0.95f, -0.26f);    // face
                Box(t, 0.14f, 0.08f, 0.1f, 0xF29C38, 0, 0.9f, -0.3f);       // beak
            },
        } },
        new Cosmetic { Id = "robot", Name = "Robot", Kind = CosmeticKind.Character, Achievement = "stamps10", Look = new CharacterLook
        {
            Legs = 0x7D8894, Body = 0xA9B4BF, Arms = 0x98A3AE, Hands = 0x6E7A86, Head = 0xC9D2DA, OwnEyes = true,
            Details = (t, l) =>
            {
                Box(t, 0.45f, 0.14f, 0.02f, 0x2C3E50, 0, 1.0f, -0.26f);     // visor
                foreach (float x in new[] { -0.1f, 0.1f }) Box(t, 0.08f, 0.06f, 0.02f, 0x7FE7FF, x, 1.0f, -0.275f);
                Box(t, 0.1f, 0.1f, 0.02f, 0xFF5252, 0, 0.58f, -0.21f);       // chest light
            },
            Top = (t, l) => { Box(t, 0.04f, 0.22f, 0.04f, 0x6E7A86, 0, 0.11f, 0); Box(t, 0.1f, 0.1f, 0.1f, 0xFF5252, 0, 0.26f, 0); },
        } },
        new Cosmetic { Id = "astronaut", Name = "Astronaut", Kind = CosmeticKind.Character, Achievement = "height500", Look = new CharacterLook
        {
            Legs = 0xE1E5EA, Body = 0xF2F4F7, Arms = 0xF2F4F7, Hands = 0x9AA5B1, Head = 0xF2F4F7, OwnEyes = true,
            Details = (t, l) =>
            {
                Box(t, 0.42f, 0.3f, 0.02f, 0x1E2A44, 0, 0.97f, -0.26f);     // visor
                Box(t, 0.1f, 0.06f, 0.02f, 0x9CC7FF, -0.12f, 1.06f, -0.275f);
                Box(t, 0.12f, 0.1f, 0.02f, 0x2F80ED, 0.15f, 0.62f, -0.21f); // badge
            },
            Back = (t, l) => Box(t, 0.45f, 0.4f, 0.15f, 0xD0D6DE, 0, 0.55f, 0.27f),   // air tank
        } },
        new Cosmetic { Id = "ninja", Name = "Ninja", Kind = CosmeticKind.Character, Achievement = "page", Look = new CharacterLook
        {
            Legs = 0x222831, Body = 0x222831, Arms = 0x222831, Hands = 0x222831, Head = 0x222831,
            Details = (t, l) =>
            {
                Box(t, 0.45f, 0.12f, 0.02f, Skin, 0, 1.0f, -0.26f);          // eye slit
                Box(t, 0.57f, 0.07f, 0.52f, 0xE53935, 0, 1.1f, 0);           // headband
                Box(t, 0.06f, 0.2f, 0.04f, 0xE53935, 0.08f, 1.0f, 0.28f, 25);
                Box(t, 0.06f, 0.16f, 0.04f, 0xE53935, -0.04f, 0.99f, 0.28f, -20);
                Box(t, 0.62f, 0.06f, 0.42f, 0xE53935, 0, 0.36f, 0);          // belt
            },
        } },
    };

    // MARK: - Hats

    static readonly List<Cosmetic> HatModels = new()
    {
        new Cosmetic { Id = "none", Name = "No hat", Kind = CosmeticKind.Hat, Free = true },
        new Cosmetic { Id = "party", Name = "Party hat", Kind = CosmeticKind.Hat, Price = 80, Build = t =>
        {
            float[] sizes = { 0.36f, 0.27f, 0.18f, 0.09f };
            for (int i = 0; i < sizes.Length; i++)
            {
                Box(t, sizes[i], 0.12f, sizes[i], 0xF06292, 0, 0.06f + i * 0.12f, 0);
                if (i < 3) Box(t, 0.06f, 0.06f, 0.02f, 0xFFE14D, (i - 1) * 0.04f, 0.06f + i * 0.12f, -sizes[i] / 2 - 0.01f);
            }
            Box(t, 0.11f, 0.11f, 0.11f, 0xFFE14D, 0, 0.52f, 0);
        } },
        new Cosmetic { Id = "beanie", Name = "Beanie", Kind = CosmeticKind.Hat, Price = 80, Build = t =>
        {
            Box(t, 0.6f, 0.2f, 0.55f, 0xE53935, 0, 0.1f, 0);
            Box(t, 0.62f, 0.08f, 0.57f, 0xC62828, 0, 0.03f, 0);
            Box(t, 0.16f, 0.14f, 0.16f, 0xFFFFFF, 0, 0.27f, 0);
        } },
        new Cosmetic { Id = "flower", Name = "Flower", Kind = CosmeticKind.Hat, Price = 60, Build = t =>
        {
            const float x = 0.17f, y = 0.09f, z = -0.13f;
            Box(t, 0.1f, 0.04f, 0.06f, 0x5DAE4B, x - 0.1f, 0.03f, z);
            foreach (var p in new[] { new Vector2(0.08f, 0), new Vector2(-0.08f, 0), new Vector2(0, 0.08f), new Vector2(0, -0.08f) })
                Box(t, 0.08f, 0.08f, 0.06f, 0xF48FB1, x + p.x, y + p.y, z);
            Box(t, 0.08f, 0.08f, 0.08f, 0xFFD54F, x, y, z - 0.01f);
        } },
        new Cosmetic { Id = "cowboy", Name = "Cowboy hat", Kind = CosmeticKind.Hat, Price = 120, Build = t =>
        {
            Box(t, 0.96f, 0.05f, 0.84f, 0x8D5A2B, 0, 0.03f, 0);
            Box(t, 0.5f, 0.26f, 0.46f, 0xA0693A, 0, 0.18f, 0);
            Box(t, 0.52f, 0.06f, 0.48f, 0x4E342E, 0, 0.08f, 0);
        } },
        new Cosmetic { Id = "propeller", Name = "Propeller cap", Kind = CosmeticKind.Hat, Price = 120, Build = t =>
        {
            uint[] colors = { 0xE53935, 0xFFD54F, 0x2F80ED, 0x43A047 };
            for (int i = 0; i < 4; i++)
                Box(t, 0.3f, 0.16f, 0.28f, colors[i], i % 2 == 0 ? -0.15f : 0.15f, 0.08f, i < 2 ? -0.14f : 0.14f);
            Box(t, 0.6f, 0.05f, 0.22f, 0x2F80ED, 0, 0.01f, -0.36f);
            Box(t, 0.04f, 0.14f, 0.04f, 0x555555, 0, 0.23f, 0);
            var prop = new GameObject("propeller").transform;
            prop.SetParent(t, false);
            prop.localPosition = new Vector3(0, 0.31f, 0);
            Box(prop, 0.5f, 0.03f, 0.08f, 0xE53935, 0, 0, 0);
            prop.gameObject.AddComponent<Spinner>().DegreesPerSecond = 720;
        } },
        new Cosmetic { Id = "tophat", Name = "Top hat", Kind = CosmeticKind.Hat, Price = 150, Build = t =>
        {
            Box(t, 0.8f, 0.05f, 0.72f, 0x222222, 0, 0.03f, 0);
            Box(t, 0.48f, 0.46f, 0.44f, 0x2B2B2B, 0, 0.28f, 0);
            Box(t, 0.5f, 0.07f, 0.46f, 0xC62828, 0, 0.09f, 0);
        } },
        new Cosmetic { Id = "viking", Name = "Viking helmet", Kind = CosmeticKind.Hat, Achievement = "boss", Build = t =>
        {
            Box(t, 0.62f, 0.2f, 0.58f, 0x9EA7B0, 0, 0.1f, 0);
            Box(t, 0.64f, 0.06f, 0.6f, 0x8D6E63, 0, 0.02f, 0);
            foreach (float s in new[] { -1f, 1f })
            {
                Box(t, 0.12f, 0.16f, 0.12f, 0xEEE6D3, s * 0.36f, 0.12f, 0);
                Box(t, 0.09f, 0.16f, 0.09f, 0xEEE6D3, s * 0.42f, 0.26f, 0, -s * 20);
            }
        } },
        new Cosmetic { Id = "crown", Name = "Crown", Kind = CosmeticKind.Hat, Achievement = "height1000", Build = t =>
        {
            Box(t, 0.6f, 0.14f, 0.55f, 0xFFC83D, 0, 0.07f, 0);
            foreach (var p in new[] { new Vector2(-0.25f, -0.22f), new Vector2(0.25f, -0.22f), new Vector2(-0.25f, 0.22f), new Vector2(0.25f, 0.22f), new Vector2(0, -0.22f), new Vector2(0, 0.22f) })
                Box(t, 0.1f, 0.14f, 0.1f, 0xFFC83D, p.x, 0.2f, p.y);
            Box(t, 0.08f, 0.08f, 0.02f, 0xE53935, 0, 0.08f, -0.285f);
            Box(t, 0.06f, 0.06f, 0.02f, 0x2F80ED, -0.16f, 0.08f, -0.285f);
            Box(t, 0.06f, 0.06f, 0.02f, 0x2F80ED, 0.16f, 0.08f, -0.285f);
        } },
    };

    // MARK: - Backpacks (built on the middle of the back; the charm hook is at the lower right)

    /// Shoulder straps, seen on the chest from the front.
    static void Straps(Transform t, uint color)
    {
        foreach (float x in new[] { -0.15f, 0.15f })
        {
            // A little in front of the chest (and of bellies, belts and badges on it), so nothing shows through.
            Box(t, 0.07f, 0.44f, 0.03f, color, x, 0.02f, -0.44f);
            Box(t, 0.07f, 0.04f, 0.47f, color, x, 0.25f, -0.2f);
        }
    }

    public static readonly Vector3 CharmHook = new(0.17f, -0.17f, 0.29f);

    static readonly List<Cosmetic> BackpackModels = new()
    {
        new Cosmetic { Id = "none", Name = "No backpack", Kind = CosmeticKind.Backpack, Free = true },
        new Cosmetic { Id = "classic", Name = "Backpack", Kind = CosmeticKind.Backpack, Build = t =>
        {
            Box(t, 0.46f, 0.5f, 0.24f, 0xE85A4F, 0, 0.04f, 0.13f);
            Box(t, 0.47f, 0.08f, 0.26f, 0xC94A40, 0, 0.27f, 0.13f);
            Box(t, 0.32f, 0.2f, 0.06f, 0xC94A40, 0, -0.08f, 0.27f);
            Box(t, 0.1f, 0.03f, 0.02f, 0xFFD54F, 0, 0.0f, 0.305f);
            Straps(t, 0x2E3A59);
        } },
        new Cosmetic { Id = "randoseru", Name = "School bag", Kind = CosmeticKind.Backpack, Build = t =>
        {
            Box(t, 0.46f, 0.5f, 0.22f, 0xD9363E, 0, 0.04f, 0.12f);
            Box(t, 0.47f, 0.36f, 0.04f, 0xB52A31, 0, 0.1f, 0.245f);    // the flap
            Box(t, 0.37f, 0.06f, 0.04f, 0xB52A31, 0, -0.11f, 0.245f);
            Box(t, 0.08f, 0.06f, 0.02f, 0xFFC83D, 0, -0.1f, 0.27f);    // golden clasp
            Box(t, 0.04f, 0.3f, 0.02f, 0xFFE14D, -0.2f, 0.02f, 0.235f);   // reflective stripe
            Straps(t, 0xB52A31);
        } },
        new Cosmetic { Id = "bear", Name = "Teddy pack", Kind = CosmeticKind.Backpack, Build = t =>
        {
            Box(t, 0.46f, 0.48f, 0.24f, 0xA0693A, 0, 0.03f, 0.13f);
            foreach (float x in new[] { -0.17f, 0.17f })
            {
                Box(t, 0.13f, 0.13f, 0.1f, 0xA0693A, x, 0.31f, 0.13f);
                Box(t, 0.06f, 0.06f, 0.02f, 0xE8C9A0, x, 0.31f, 0.185f);
                Box(t, 0.05f, 0.05f, 0.02f, 0x3E2723, x * 0.55f, 0.09f, 0.255f);   // eyes
            }
            Box(t, 0.17f, 0.12f, 0.04f, 0xE8C9A0, 0, -0.03f, 0.27f);   // snout
            Box(t, 0.06f, 0.04f, 0.02f, 0x3E2723, 0, 0.0f, 0.295f);
            Straps(t, 0x6D4C2F);
        } },
        new Cosmetic { Id = "turtle", Name = "Turtle shell", Kind = CosmeticKind.Backpack, Build = t =>
        {
            Box(t, 0.5f, 0.48f, 0.2f, 0x4E9A3A, 0, 0.03f, 0.11f);
            Box(t, 0.38f, 0.36f, 0.08f, 0x6CBF4B, 0, 0.03f, 0.24f);
            Box(t, 0.12f, 0.12f, 0.02f, 0x3E7D2E, 0, 0.03f, 0.285f);
            foreach (var p in new[] { new Vector2(-0.11f, 0.12f), new Vector2(0.11f, 0.12f), new Vector2(-0.11f, -0.06f), new Vector2(0.11f, -0.06f) })
                Box(t, 0.09f, 0.09f, 0.02f, 0x3E7D2E, p.x, p.y, 0.282f);
            Box(t, 0.54f, 0.06f, 0.22f, 0xC9A66B, 0, -0.21f, 0.11f);
            Straps(t, 0x3E7D2E);
        } },
        new Cosmetic { Id = "jetpack", Name = "Jetpack", Kind = CosmeticKind.Backpack, Build = t =>
        {
            foreach (float x in new[] { -0.11f, 0.11f })
            {
                Box(t, 0.18f, 0.48f, 0.2f, 0x9AA5B1, x, 0.04f, 0.13f);
                Box(t, 0.19f, 0.06f, 0.21f, 0xE53935, x, 0.3f, 0.13f);
                Box(t, 0.12f, 0.08f, 0.12f, 0x555555, x, -0.24f, 0.13f);
                Voxel.Box(t, new Vector3(0.08f, 0.14f, 0.08f), 0xFF8A1F, new Vector3(x, -0.36f, 0.13f), castShadows: false);
                Voxel.Box(t, new Vector3(0.04f, 0.08f, 0.04f), 0xFFE14D, new Vector3(x, -0.33f, 0.13f), castShadows: false);
            }
            Straps(t, 0x555555);
        } },
    };

    // MARK: - Charms (hanging from the hook: a short chain, then the charm; +z faces away from the back)

    static void Chain(Transform t)
    {
        Box(t, 0.025f, 0.04f, 0.025f, 0xC0C6CC, 0, -0.025f, 0);
        Box(t, 0.025f, 0.04f, 0.025f, 0xC0C6CC, 0, -0.065f, 0);
    }

    static readonly List<Cosmetic> CharmModels = new()
    {
        new Cosmetic { Id = "none", Name = "No charm", Kind = CosmeticKind.Charm, Free = true },
        new Cosmetic { Id = "star", Name = "Star", Kind = CosmeticKind.Charm, Build = t =>
        {
            Chain(t);
            Box(t, 0.08f, 0.08f, 0.03f, 0xFFD54F, 0, -0.15f, 0);
            foreach (var p in new[] { new Vector2(0, 0.065f), new Vector2(0, -0.065f), new Vector2(0.065f, 0), new Vector2(-0.065f, 0) })
                Box(t, 0.05f, 0.05f, 0.03f, 0xFFD54F, p.x, -0.15f + p.y, 0);
        } },
        new Cosmetic { Id = "heart", Name = "Heart", Kind = CosmeticKind.Charm, Build = t =>
        {
            Chain(t);
            Box(t, 0.06f, 0.06f, 0.04f, 0xF06292, -0.03f, -0.11f, 0);
            Box(t, 0.06f, 0.06f, 0.04f, 0xF06292, 0.03f, -0.11f, 0);
            Box(t, 0.11f, 0.06f, 0.04f, 0xF06292, 0, -0.15f, 0);
            Box(t, 0.05f, 0.05f, 0.04f, 0xF06292, 0, -0.19f, 0);
        } },
        new Cosmetic { Id = "onigiri", Name = "Onigiri", Kind = CosmeticKind.Charm, Build = t =>
        {
            Chain(t);
            Box(t, 0.05f, 0.04f, 0.05f, 0xFFFFFF, 0, -0.1f, 0);
            Box(t, 0.1f, 0.04f, 0.05f, 0xFFFFFF, 0, -0.14f, 0);
            Box(t, 0.14f, 0.05f, 0.05f, 0xFFFFFF, 0, -0.185f, 0);
            Box(t, 0.08f, 0.06f, 0.055f, 0x1F2A1F, 0, -0.19f, 0);   // nori
        } },
        new Cosmetic { Id = "dango", Name = "Dango", Kind = CosmeticKind.Charm, Build = t =>
        {
            Chain(t);
            Box(t, 0.015f, 0.26f, 0.015f, 0xA0693A, 0, -0.2f, 0);
            Box(t, 0.07f, 0.07f, 0.07f, 0xF8BBD0, 0, -0.12f, 0);
            Box(t, 0.07f, 0.07f, 0.07f, 0xFFFFFF, 0, -0.19f, 0);
            Box(t, 0.07f, 0.07f, 0.07f, 0xA5D6A7, 0, -0.26f, 0);
        } },
        new Cosmetic { Id = "bell", Name = "Bell", Kind = CosmeticKind.Charm, Build = t =>
        {
            Chain(t);
            Box(t, 0.08f, 0.03f, 0.03f, 0xE53935, 0, -0.095f, 0);
            Box(t, 0.1f, 0.1f, 0.1f, 0xFFC83D, 0, -0.15f, 0);
            Box(t, 0.12f, 0.03f, 0.12f, 0xE6A817, 0, -0.2f, 0);
            Box(t, 0.03f, 0.03f, 0.03f, 0x5D4037, 0, -0.225f, 0);
        } },
        new Cosmetic { Id = "daruma", Name = "Daruma", Kind = CosmeticKind.Charm, Build = t =>
        {
            Chain(t);
            Box(t, 0.12f, 0.13f, 0.1f, 0xD32F2F, 0, -0.15f, 0);
            Box(t, 0.08f, 0.06f, 0.02f, 0xFFF3E0, 0, -0.13f, 0.055f);
            Box(t, 0.02f, 0.02f, 0.01f, 0x111111, -0.02f, -0.125f, 0.066f);
            Box(t, 0.02f, 0.02f, 0.01f, 0x111111, 0.02f, -0.125f, 0.066f);
            Box(t, 0.06f, 0.02f, 0.02f, 0xFFC83D, 0, -0.19f, 0.052f);
        } },
        new Cosmetic { Id = "luckycat", Name = "Lucky cat", Kind = CosmeticKind.Charm, Build = t =>
        {
            Chain(t);
            Box(t, 0.12f, 0.1f, 0.1f, 0xFFFFFF, 0, -0.125f, 0);       // head
            Box(t, 0.03f, 0.03f, 0.03f, 0xFFFFFF, -0.04f, -0.065f, 0);
            Box(t, 0.03f, 0.03f, 0.03f, 0xFFFFFF, 0.04f, -0.065f, 0);
            Box(t, 0.02f, 0.02f, 0.01f, 0x111111, -0.025f, -0.12f, 0.055f);
            Box(t, 0.02f, 0.02f, 0.01f, 0x111111, 0.025f, -0.12f, 0.055f);
            Box(t, 0.12f, 0.025f, 0.1f, 0xE53935, 0, -0.18f, 0);       // collar
            Box(t, 0.025f, 0.025f, 0.02f, 0xFFC83D, 0, -0.18f, 0.055f);
            Box(t, 0.11f, 0.08f, 0.09f, 0xFFFFFF, 0, -0.23f, 0);       // body
            Box(t, 0.035f, 0.07f, 0.035f, 0xFFFFFF, 0.07f, -0.17f, 0.01f);   // the raised paw
        } },
        new Cosmetic { Id = "minirocket", Name = "Mini rocket", Kind = CosmeticKind.Charm, Build = t =>
        {
            Chain(t);
            Box(t, 0.05f, 0.05f, 0.05f, 0xE53935, 0, -0.105f, 0);
            Box(t, 0.065f, 0.14f, 0.065f, 0xF4F4F4, 0, -0.195f, 0);
            Box(t, 0.03f, 0.03f, 0.01f, 0x4FC3F7, 0, -0.17f, 0.036f);
            Box(t, 0.12f, 0.04f, 0.02f, 0xE53935, 0, -0.26f, 0);
            Voxel.Box(t, new Vector3(0.04f, 0.05f, 0.04f), 0xFF8A1F, new Vector3(0, -0.3f, 0), castShadows: false);
        } },
    };

    // MARK: - Tags

    static readonly List<Cosmetic> TagModels = new()
    {
        Tag("sky", "Sky", new TagStyle { Colors = new uint[] { 0x8FD3F4, 0xDDF3FC }, Pattern = "clouds", InkAlpha = 0.6f, Tile = 120, Drift = new Vector2(0.02f, 0) }),
        Tag("polka", "Polka dots", new TagStyle { Colors = new uint[] { 0xFF9EB8, 0xFFD6E1 }, Pattern = "dots", InkAlpha = 0.5f, Tile = 56 }),
        Tag("lemonade", "Lemonade", new TagStyle { Colors = new uint[] { 0xFFD04F, 0xFFEBA3 }, Pattern = "stripes", InkAlpha = 0.45f, Tile = 72, Drift = new Vector2(0.06f, 0) }),
        Tag("picnic", "Picnic", new TagStyle { Colors = new uint[] { 0xE8645A, 0xF29286 }, Pattern = "checker", InkAlpha = 0.32f, Tile = 64 }),
        Tag("ocean", "Ocean", new TagStyle { Colors = new uint[] { 0x2F7FE0, 0x86CCF7 }, Pattern = "waves", InkAlpha = 0.35f, Tile = 80, Drift = new Vector2(0.05f, 0) }),
        Tag("mint", "Mint zigzag", new TagStyle { Colors = new uint[] { 0x4FC4A0, 0xB0EBD6 }, Pattern = "zigzag", InkAlpha = 0.45f, Tile = 64 }),
        Tag("sakura", "Sakura", new TagStyle { Colors = new uint[] { 0xFFC2D4, 0xFFF1F5 }, Pattern = "flowers", Ink = 0xF27DA6, InkAlpha = 0.6f, Tile = 90, Drift = new Vector2(0.015f, 0.03f) }),
        Tag("sweetheart", "Sweetheart", new TagStyle { Colors = new uint[] { 0xEE5A8A, 0xF9A8C4 }, Pattern = "hearts", InkAlpha = 0.45f, Tile = 70, Drift = new Vector2(0, -0.03f) }),
        Tag("starry", "Starry night", new TagStyle { Colors = new uint[] { 0x1A1F4D, 0x4A3F95 }, Pattern = "stars", Ink = 0xFFE08A, InkAlpha = 0.85f, Tile = 96, Drift = new Vector2(0.012f, 0) }),
        Tag("sunburst", "Sunburst", new TagStyle { Colors = new uint[] { 0xFF8F3D, 0xFFD96B }, Ink = 0xFFF3C4, InkAlpha = 0.45f, Rays = true }),
        Tag("rainbow", "Rainbow", new TagStyle { Colors = new uint[] { 0xFF8A8A, 0xFFC56B, 0xFFEE80, 0x8EE08E, 0x7FC2FF, 0xBC94FF }, Pattern = "stars", InkAlpha = 0.5f, Tile = 96, Drift = new Vector2(0.02f, 0), Rays = true }),
    };

    static Cosmetic Tag(string id, string name, TagStyle style) => new() { Id = id, Name = name, Kind = CosmeticKind.Tag, Tag = style };

    /// What each achievement asks for, as shown on locked items.
    public static readonly Dictionary<string, string> AchievementText = new()
    {
        ["stamps10"] = "Collect 10 stamps",
        ["height500"] = "Reach 500 m",
        ["page"] = "Fill a stamp book page",
        ["boss"] = "Beat a boss planet",
        ["height1000"] = "Reach 1000 m",
    };

    // MARK: - The item sheet

    static List<Cosmetic> characters, hats, backpacks, charms, tags;

    /// Every character, in sheet order (the first is the free default).
    public static List<Cosmetic> Characters { get { Load(); return characters; } }

    /// Every hat, in sheet order ("No hat" first).
    public static List<Cosmetic> Hats { get { Load(); return hats; } }

    public static List<Cosmetic> Backpacks { get { Load(); return backpacks; } }
    public static List<Cosmetic> Charms { get { Load(); return charms; } }
    public static List<Cosmetic> Tags { get { Load(); return tags; } }

    public static List<Cosmetic> OfKind(CosmeticKind kind) => kind switch
    {
        CosmeticKind.Character => Characters,
        CosmeticKind.Hat => Hats,
        CosmeticKind.Backpack => Backpacks,
        CosmeticKind.Charm => Charms,
        _ => Tags,
    };

    static Dictionary<CosmeticKind, List<Cosmetic>> allModels;

    /// Every model of a kind: the ones in the game, then the candidates (see Cosmetics.Candidates*.cs).
    public static List<Cosmetic> ModelsOf(CosmeticKind kind)
    {
        allModels ??= new Dictionary<CosmeticKind, List<Cosmetic>>();
        if (allModels.TryGetValue(kind, out var list)) return list;
        var models = kind switch
        {
            CosmeticKind.Character => CharacterModels,
            CosmeticKind.Hat => HatModels,
            CosmeticKind.Backpack => BackpackModels,
            CosmeticKind.Charm => CharmModels,
            _ => TagModels,
        };
        return allModels[kind] = models.Concat(CandidatesOf(kind)).ToList();
    }

    public static Cosmetic Character(string id) => Characters.Find(c => c.Id == id) ?? Characters[0];
    public static Cosmetic Hat(string id) => Hats.Find(h => h.Id == (id == "" ? "none" : id)) ?? Hats[0];
    public static Cosmetic Backpack(string id) => Backpacks.Find(b => b.Id == id) ?? Backpacks[0];
    public static Cosmetic Charm(string id) => Charms.Find(c => c.Id == id) ?? Charms[0];
    public static Cosmetic Tag(string id) => Tags.Find(t => t.Id == id) ?? Tags[0];

    /// Reads Resources/Items.csv. Columns: id, type, name, rarity, unlock, price, achievement, in_gift, base,
    /// legs, body, arms, hands, head, notes (for tags, legs/body/arms are the top, bottom and pattern colors). Without the sheet, the built-in models are used as they are.
    static void Load()
    {
        if (characters != null) return;
        characters = new List<Cosmetic>();
        hats = new List<Cosmetic>();
        backpacks = new List<Cosmetic>();
        charms = new List<Cosmetic>();
        tags = new List<Cosmetic>();
        var sheet = Resources.Load<TextAsset>("Items");
        if (sheet == null)
        {
            foreach (CosmeticKind kind in Enum.GetValues(typeof(CosmeticKind))) OfKind(kind).AddRange(ModelsOf(kind));
            return;
        }
        var lines = sheet.text.Replace("\r", "").Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var c = SplitCsv(lines[i]);
            if (c.Count < 9 || c[0].Trim().Length == 0) continue;
            string Col(int index) => index < c.Count ? c[index].Trim() : "";
            if (!Enum.TryParse(Col(1), true, out CosmeticKind kind))
            {
                Debug.LogWarning($"Items.csv: unknown type \"{Col(1)}\" for {Col(0)}");
                continue;
            }
            string baseId = Col(8).Length > 0 ? Col(8) : Col(0);
            var model = ModelsOf(kind).Find(m => m.Id == baseId);
            if (model == null)
            {
                Debug.LogWarning($"Items.csv: no model called \"{baseId}\" for {Col(0)}");
                continue;
            }
            string unlock = Col(4).ToLower();
            var item = new Cosmetic
            {
                Id = Col(0), Name = Col(2), Kind = kind,
                Rarity = Enum.TryParse(Col(3), true, out Rarity rarity) ? rarity : Rarity.Common,
                Free = unlock == "free",
                Price = unlock == "coins" && int.TryParse(Col(5), out int price) ? price : 0,
                Achievement = unlock == "achievement" && Col(6).Length > 0 ? Col(6) : null,
                InGift = Col(7).ToLower() is "yes" or "y" or "true" or "1",
                Build = model.Build,
                Look = model.Look?.Recolored(Hex(Col(9)), Hex(Col(10)), Hex(Col(11)), Hex(Col(12)), Hex(Col(13))),
                Tag = model.Tag?.Recolored(Hex(Col(9)), Hex(Col(10)), Hex(Col(11))),   // tags: top, bottom and pattern color
            };
            OfKind(kind).Add(item);
        }
        foreach (CosmeticKind kind in Enum.GetValues(typeof(CosmeticKind)))
            if (OfKind(kind).Count == 0) OfKind(kind).AddRange(ModelsOf(kind));
    }

    static uint? Hex(string value)
    {
        value = value.TrimStart('#');
        return value.Length == 6 && uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out uint hex) ? hex : null;
    }

    /// Splits one CSV line, allowing "quoted, text" in a field.
    static List<string> SplitCsv(string line)
    {
        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        bool quoted = false;
        foreach (char ch in line)
        {
            if (ch == '"') quoted = !quoted;
            else if (ch == ',' && !quoted)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else field.Append(ch);
        }
        fields.Add(field.ToString());
        return fields;
    }
}
