using System.Collections.Generic;
using UnityEngine;

// Tag candidates: backgrounds behind your character, and your banner on the leaderboard (patterns in TagArt).
public static partial class Cosmetics
{
    static Cosmetic NewTag(string id, string name, int rarity, int price, string notes, TagStyle style) =>
        Cand(CosmeticKind.Tag, id, name, rarity, price, notes, tag: style);

    static readonly List<Cosmetic> TagCandidates = new()
    {
        // Common: 60 to 120
        NewTag("bubblegum", "Bubblegum", 0, 60, "Pink with rising bubbles", new TagStyle { Colors = new uint[] { 0xFF8FC7, 0xFFC9E5 }, Pattern = "bubbles", InkAlpha = 0.55f, Tile = 90, Drift = new Vector2(0, -0.04f) }),
        NewTag("limeplaid", "Lime plaid", 0, 80, "A fresh green picnic plaid", new TagStyle { Colors = new uint[] { 0x9CCC65, 0xC5E1A5 }, Pattern = "plaid", InkAlpha = 0.35f, Tile = 80 }),
        NewTag("peach", "Peach", 0, 60, "Soft peach with tiny hearts", new TagStyle { Colors = new uint[] { 0xFFB38A, 0xFFE3D1 }, Pattern = "hearts", InkAlpha = 0.45f, Tile = 52 }),
        NewTag("paws", "Paw prints", 0, 80, "Little paw prints on beige", new TagStyle { Colors = new uint[] { 0xE8D5B5, 0xF5EBDA }, Pattern = "paws", Ink = 0xC9A27A, InkAlpha = 0.6f, Tile = 80 }),
        NewTag("denim", "Denim", 0, 100, "Blue denim with golden stitches", new TagStyle { Colors = new uint[] { 0x3F6FB5, 0x5B8BD0 }, Pattern = "stitches", Ink = 0xF2C94C, InkAlpha = 0.75f, Tile = 70 }),
        NewTag("meadow", "Meadow", 0, 100, "Leaves drifting on a spring breeze", new TagStyle { Colors = new uint[] { 0x7CCB6B, 0xC8EBB0 }, Pattern = "leaves", Ink = 0x4E9A3A, InkAlpha = 0.45f, Tile = 80, Drift = new Vector2(0.03f, 0.01f) }),
        NewTag("mintchoc", "Mint choc chip", 0, 120, "Mint ice cream with chocolate chips", new TagStyle { Colors = new uint[] { 0x9EE6CF, 0xD2F5EA }, Pattern = "chips", Ink = 0x5D3A1A, InkAlpha = 0.75f, Tile = 70 }),

        // Rare: 150 to 300
        NewTag("seigaiha", "Seigaiha", 1, 250, "The classic Japanese wave pattern, in indigo", new TagStyle { Colors = new uint[] { 0x1F3A6E, 0x2F5AA0 }, Pattern = "seigaiha", InkAlpha = 0.4f, Tile = 90 }),
        NewTag("snowday", "Snow day", 1, 200, "Snowflakes falling, for the ice planets", new TagStyle { Colors = new uint[] { 0x8EC9F0, 0xE3F3FC }, Pattern = "snowflakes", InkAlpha = 0.85f, Tile = 90, Drift = new Vector2(0.01f, 0.05f) }),
        NewTag("argyle", "Argyle", 1, 180, "Purple diamonds, cozy like a sweater", new TagStyle { Colors = new uint[] { 0x7E57C2, 0xB39DDB }, Pattern = "diamonds", InkAlpha = 0.3f, Tile = 70 }),
        NewTag("party", "Party", 1, 200, "Golden confetti on purple", new TagStyle { Colors = new uint[] { 0x6A4FC6, 0x9B7BE8 }, Pattern = "chips", Ink = 0xFFE14D, InkAlpha = 0.85f, Tile = 80, Drift = new Vector2(0.02f, 0.04f) }),
        NewTag("deepsea", "Deep sea", 1, 220, "Bubbles rising from the deep", new TagStyle { Colors = new uint[] { 0x0B4F8A, 0x1E88C8 }, Pattern = "bubbles", Ink = 0xB3E5FC, InkAlpha = 0.5f, Tile = 100, Drift = new Vector2(0, -0.05f) }),
        NewTag("sunset", "Sunset", 1, 300, "Orange to purple, with soft rays", new TagStyle { Colors = new uint[] { 0xFF7E5F, 0xFEB47B, 0x8E6CC9 }, Ink = 0xFFE0B2, InkAlpha = 0.3f, Rays = true }),

        // Mega: 350 to 600
        NewTag("lava", "Lava", 2, 500, "Flames licking up, for the hot planets", new TagStyle { Colors = new uint[] { 0x3B0A0A, 0x8A1C0A }, Pattern = "flames", Ink = 0xFF7A1A, InkAlpha = 0.85f, Tile = 70, Drift = new Vector2(0, -0.06f) }),
        NewTag("glacier", "Glacier", 2, 500, "Sparkling ice diamonds", new TagStyle { Colors = new uint[] { 0x6EC6F0, 0xDFF6FF }, Pattern = "diamonds", InkAlpha = 0.45f, Tile = 50 }),
        NewTag("matcha", "Matcha", 2, 400, "Seigaiha waves in tea green", new TagStyle { Colors = new uint[] { 0x6B8E3A, 0xA4C46A }, Pattern = "seigaiha", Ink = 0xF5F0DC, InkAlpha = 0.45f, Tile = 80 }),
        NewTag("aurora", "Aurora", 2, 600, "Northern lights over a starry sky", new TagStyle { Colors = new uint[] { 0x0B1E3F, 0x1A6E6A, 0x6A3F9E }, Pattern = "stars", Ink = 0xC8FFF0, InkAlpha = 0.7f, Tile = 110, Drift = new Vector2(0.02f, 0) }),
        NewTag("racing", "Racing flag", 2, 350, "A checkered flag, waving past", new TagStyle { Colors = new uint[] { 0x222222, 0x3A3A3A }, Pattern = "checker", InkAlpha = 0.9f, Tile = 48, Drift = new Vector2(0.08f, 0) }),

        // Ultra: 700 to 1000
        NewTag("goldrush", "Gold rush", 3, 900, "Coins raining on gold, with rays", new TagStyle { Colors = new uint[] { 0xE8A82B, 0xFFD86B }, Pattern = "coins", Ink = 0xFFF1B0, InkAlpha = 0.7f, Tile = 80, Drift = new Vector2(0, 0.04f), Rays = true }),
        NewTag("sakurastorm", "Sakura storm", 3, 800, "Cherry blossoms blowing through the dusk", new TagStyle { Colors = new uint[] { 0x2E1A47, 0xB04A7E }, Pattern = "flowers", Ink = 0xFFB8D0, InkAlpha = 0.8f, Tile = 70, Drift = new Vector2(0.06f, 0.08f) }),
        NewTag("nebula", "Nebula", 3, 1000, "A pink space cloud full of stars", new TagStyle { Colors = new uint[] { 0x2A0E4A, 0x8E2C8A, 0xF06292 }, Pattern = "stars", InkAlpha = 0.7f, Tile = 120, Drift = new Vector2(0.015f, 0), Rays = true }),
        NewTag("thunder", "Thunder", 3, 750, "Lightning bolts on a stormy night", new TagStyle { Colors = new uint[] { 0x1B1F3B, 0x37406B }, Pattern = "bolts", Ink = 0xFFE14D, InkAlpha = 0.9f, Tile = 90 }),

        // Legendary: 1200 to 1500
        NewTag("galaxy", "Galaxy", 4, 1500, "Deep space with drifting stars and light", new TagStyle { Colors = new uint[] { 0x0A0A23, 0x1E1452, 0x4A2A8A }, Pattern = "stars", InkAlpha = 0.85f, Tile = 100, Drift = new Vector2(0.02f, 0.01f), Rays = true }),
        NewTag("holo", "Holographic", 4, 1300, "Shimmering pastel rainbow diamonds", new TagStyle { Colors = new uint[] { 0xFFC4E1, 0xC4E4FF, 0xD9FFC4, 0xFFF1C4 }, Pattern = "diamonds", InkAlpha = 0.45f, Tile = 60, Drift = new Vector2(0.03f, 0.03f), Rays = true }),
    };
}
