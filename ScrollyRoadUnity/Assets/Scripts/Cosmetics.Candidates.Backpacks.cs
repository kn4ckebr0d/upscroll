using System.Collections.Generic;
using UnityEngine;

// Backpacks are built on the middle of the back: +z points away from the back, the pack's face is at about
// z = 0.25, and the charm hangs from the hook at the lower right (CharmHook).
public static partial class Cosmetics
{
    static Cosmetic NewPack(string id, string name, int rarity, int price, string notes, System.Action<Transform> build) =>
        Cand(CosmeticKind.Backpack, id, name, rarity, price, notes, build: build);

    /// A strap across the chest, shoulder to hip (for bags and swords worn on one strap).
    static void Sash(Transform t, uint hex) => Box(t, 0.07f, 0.72f, 0.03f, hex, 0, 0.0f, -0.44f, 35);

    /// A soft animal-face pack: a body, eyes and a nose on the front.
    static void FacePack(Transform t, uint body, uint nose)
    {
        Box(t, 0.44f, 0.44f, 0.22f, body, 0, 0.02f, 0.12f);
        Both(s => Box(t, 0.05f, 0.05f, 0.02f, 0x222222, s * 0.09f, 0.08f, 0.235f));
        Box(t, 0.05f, 0.04f, 0.02f, nose, 0, 0.02f, 0.235f);
        Both(s => Box(t, 0.07f, 0.04f, 0.02f, 0xF8A5C2, s * 0.15f, 0.0f, 0.233f));
    }

    static void Feather(Transform t, float x, float y, float length, uint hex) => Box(t, length, 0.1f, 0.04f, hex, x, y, 0);

    static readonly List<Cosmetic> BackpackCandidates = new()
    {
        // Common: 100 to 200
        NewPack("hiking", "Hiking pack", 0, 150, "Tall, with a rolled-up sleeping mat on top", t =>
        {
            Box(t, 0.44f, 0.56f, 0.24f, 0x2E7D32, 0, 0.06f, 0.13f);
            Box(t, 0.45f, 0.08f, 0.26f, 0x1B5E20, 0, 0.32f, 0.13f);
            Box(t, 0.52f, 0.13f, 0.13f, 0xE57373, 0, 0.42f, 0.13f);
            Both(s => Box(t, 0.06f, 0.2f, 0.16f, 0x1B5E20, s * 0.25f, -0.05f, 0.13f));
            Box(t, 0.3f, 0.18f, 0.06f, 0x1B5E20, 0, -0.08f, 0.27f);
            Straps(t, 0x3E2723);
        }),
        NewPack("satchel", "Satchel", 0, 120, "A leather messenger bag on one strap", t =>
        {
            Box(t, 0.5f, 0.36f, 0.14f, 0xA0693A, 0, -0.06f, 0.08f);
            Box(t, 0.51f, 0.2f, 0.03f, 0x8D5A2B, 0, 0.02f, 0.165f);
            Box(t, 0.06f, 0.06f, 0.02f, 0xFFC83D, 0, -0.06f, 0.185f);
            Sash(t, 0x8D5A2B);
        }),
        NewPack("drawstring", "Drawstring bag", 0, 100, "A simple gym bag with white cords", t =>
        {
            Box(t, 0.42f, 0.44f, 0.12f, 0x4C8FE8, 0, 0.0f, 0.08f);
            Box(t, 0.3f, 0.05f, 0.1f, 0x3468B8, 0, 0.24f, 0.08f);
            Both(s => Box(t, 0.03f, 0.5f, 0.03f, 0xFFFFFF, s * 0.2f, 0.0f, 0.15f, s * 10));
            Box(t, 0.1f, 0.1f, 0.02f, 0xFFFFFF, 0.1f, -0.08f, 0.145f, 45);
            Straps(t, 0xFFFFFF);
        }),
        NewPack("lunchbox", "Lunch box", 0, 120, "A teal lunch box with a star sticker", t =>
        {
            Box(t, 0.44f, 0.34f, 0.2f, 0x26A69A, 0, 0.0f, 0.11f);
            Box(t, 0.45f, 0.06f, 0.21f, 0x00796B, 0, 0.12f, 0.11f);
            Box(t, 0.2f, 0.04f, 0.04f, 0x00796B, 0, 0.2f, 0.11f);
            Box(t, 0.06f, 0.06f, 0.02f, 0xFFD54F, 0, 0.07f, 0.22f);
            Box(t, 0.08f, 0.08f, 0.02f, 0xFFD54F, -0.1f, -0.06f, 0.22f, 45);
            Straps(t, 0x00796B);
        }),
        NewPack("picnic", "Picnic basket", 0, 160, "A woven basket with a checked cloth", t =>
        {
            Box(t, 0.48f, 0.32f, 0.24f, 0xD7A86E, 0, -0.04f, 0.13f);
            foreach (float y in new[] { -0.12f, -0.04f, 0.04f }) Box(t, 0.49f, 0.03f, 0.25f, 0xB98A4E, 0, y, 0.13f);
            Box(t, 0.5f, 0.06f, 0.26f, 0xE53935, 0, 0.15f, 0.13f);
            for (int i = 0; i < 4; i++) Box(t, 0.1f, 0.065f, 0.1f, 0xFFFFFF, -0.18f + i * 0.12f, 0.15f, 0.13f + (i % 2 == 0 ? -0.06f : 0.06f));
            Both(s => Box(t, 0.04f, 0.16f, 0.04f, 0xB98A4E, s * 0.15f, 0.24f, 0.13f));
            Box(t, 0.34f, 0.04f, 0.04f, 0xB98A4E, 0, 0.32f, 0.13f);
            Straps(t, 0x8D5A2B);
        }),
        NewPack("cooler", "Cooler box", 0, 150, "Blue and white, for a cold drink on hot planets", t =>
        {
            Box(t, 0.46f, 0.38f, 0.24f, 0x2F80ED, 0, 0.0f, 0.13f);
            Box(t, 0.48f, 0.08f, 0.26f, 0xFFFFFF, 0, 0.22f, 0.13f);
            Box(t, 0.3f, 0.04f, 0.04f, 0xFFFFFF, 0, 0.29f, 0.13f);
            Box(t, 0.2f, 0.1f, 0.02f, 0xFFFFFF, 0, 0.0f, 0.255f);
            Straps(t, 0x1E5BB8);
        }),
        NewPack("bunnypack", "Bunny pack", 1, 450, "A soft white bunny", t =>
        {
            FacePack(t, 0xFFFFFF, 0xF28CA0);
            Both(s => { Box(t, 0.1f, 0.26f, 0.08f, 0xFFFFFF, s * 0.12f, 0.36f, 0.12f); Box(t, 0.05f, 0.18f, 0.02f, 0xF8BBD0, s * 0.12f, 0.36f, 0.165f); });
            Straps(t, 0xF8BBD0);
        }),
        NewPack("kittypack", "Kitty pack", 1, 450, "A grey kitty with whiskers", t =>
        {
            FacePack(t, 0x9E9E9E, 0xF28CA0);
            Both(s =>
            {
                Box(t, 0.13f, 0.13f, 0.08f, 0x9E9E9E, s * 0.15f, 0.22f, 0.12f, 45);
                Box(t, 0.1f, 0.012f, 0.01f, 0x444444, s * 0.13f, 0.03f, 0.24f, s * 8);
            });
            Straps(t, 0x616161);
        }),
        NewPack("froggypack", "Froggy pack", 1, 450, "A green frog with big eyes on top", t =>
        {
            Box(t, 0.46f, 0.4f, 0.22f, 0x6CCB55, 0, 0.0f, 0.12f);
            Both(s => { Box(t, 0.12f, 0.1f, 0.12f, 0xFFFFFF, s * 0.13f, 0.25f, 0.12f); Box(t, 0.05f, 0.05f, 0.02f, 0x111111, s * 0.13f, 0.25f, 0.185f); Box(t, 0.07f, 0.04f, 0.02f, 0xF8A5C2, s * 0.15f, 0.04f, 0.233f); });
            Box(t, 0.24f, 0.02f, 0.02f, 0x2E5E22, 0, 0.08f, 0.233f);
            Straps(t, 0x43A047);
        }),
        NewPack("strawberrypack", "Strawberry", 0, 200, "A big juicy strawberry", t =>
        {
            Box(t, 0.44f, 0.4f, 0.22f, 0xE53935, 0, 0.02f, 0.12f);
            Box(t, 0.32f, 0.12f, 0.2f, 0xE53935, 0, -0.22f, 0.12f);
            foreach (var p in new[] { new Vector2(-0.12f, 0.1f), new Vector2(0.04f, 0.12f), new Vector2(0.15f, 0.02f), new Vector2(-0.06f, -0.04f), new Vector2(0.08f, -0.14f), new Vector2(-0.15f, -0.12f) })
                Box(t, 0.03f, 0.04f, 0.02f, 0xFFE082, p.x, p.y, 0.233f);
            Box(t, 0.46f, 0.06f, 0.24f, 0x43A047, 0, 0.24f, 0.12f);
            Box(t, 0.06f, 0.08f, 0.06f, 0x2E7D32, 0, 0.31f, 0.12f);
            Straps(t, 0x43A047);
        }),
        NewPack("watermelon", "Watermelon", 0, 200, "A watermelon slice for summer planets", t =>
        {
            Box(t, 0.46f, 0.44f, 0.22f, 0x2E7D32, 0, 0.0f, 0.12f);
            Box(t, 0.42f, 0.4f, 0.02f, 0xF1F8E9, 0, 0.0f, 0.233f);
            Box(t, 0.38f, 0.36f, 0.02f, 0xEF5350, 0, 0.0f, 0.24f);
            foreach (var p in new[] { new Vector2(-0.1f, 0.08f), new Vector2(0.08f, 0.1f), new Vector2(0, -0.04f), new Vector2(-0.12f, -0.1f), new Vector2(0.12f, -0.08f) })
                Box(t, 0.03f, 0.05f, 0.02f, 0x222222, p.x, p.y, 0.25f);
            Straps(t, 0x1B5E20);
        }),

        // Rare: 250 to 500
        NewPack("guitar", "Guitar", 1, 400, "An acoustic guitar slung on your back", t =>
        {
            var g = Pivot(t, 0, 0, 0.08f, 0, 0, 25);
            Box(g, 0.4f, 0.34f, 0.1f, 0xD2691E, 0, -0.12f, 0);
            Box(g, 0.32f, 0.22f, 0.1f, 0xD2691E, 0, 0.14f, 0);
            Box(g, 0.11f, 0.11f, 0.02f, 0x3E2723, 0, 0.04f, 0.055f);
            Box(g, 0.08f, 0.5f, 0.06f, 0x5D3A1A, 0, 0.5f, 0);
            Box(g, 0.12f, 0.12f, 0.06f, 0x3E2723, 0, 0.8f, 0);
            Box(g, 0.14f, 0.03f, 0.02f, 0x3E2723, 0, -0.18f, 0.055f);
            Sash(t, 0x3E2723);
        }),
        NewPack("skateboard", "Skateboard", 1, 350, "A purple deck with yellow wheels", t =>
        {
            var b = Pivot(t, 0, 0.05f, 0.06f, 0, 0, 8);
            Box(b, 0.3f, 0.8f, 0.04f, 0x7E57C2, 0, 0, 0);
            Box(b, 0.31f, 0.1f, 0.045f, 0xFFD54F, 0, 0.15f, 0);
            Box(b, 0.31f, 0.1f, 0.045f, 0x4FC3F7, 0, -0.15f, 0);
            Both(s =>
            {
                Box(b, 0.2f, 0.03f, 0.03f, 0x9E9E9E, 0, s * 0.3f, 0.035f);
                Both(x => Box(b, 0.06f, 0.06f, 0.06f, 0xFFEB3B, x * 0.11f, s * 0.3f, 0.06f));
            });
            Straps(t, 0x222222);
        }),
        NewPack("surfboard", "Surfboard", 1, 450, "A long blue board with a white stripe", t =>
        {
            Box(t, 0.3f, 1.0f, 0.05f, 0x4FC3F7, 0, 0.15f, 0.06f);
            Box(t, 0.2f, 0.08f, 0.05f, 0x4FC3F7, 0, 0.69f, 0.06f);
            Box(t, 0.2f, 0.06f, 0.05f, 0x4FC3F7, 0, -0.38f, 0.06f);
            Box(t, 0.06f, 1.06f, 0.055f, 0xFFFFFF, 0, 0.15f, 0.06f);
            Box(t, 0.03f, 0.1f, 0.08f, 0x0288D1, 0, -0.26f, 0.12f);
            Straps(t, 0x0288D1);
        }),
        NewPack("snail", "Snail shell", 1, 350, "A swirly snail shell", t =>
        {
            Box(t, 0.46f, 0.46f, 0.22f, 0xD99A5B, 0, 0.04f, 0.12f);
            Box(t, 0.32f, 0.32f, 0.02f, 0xB5763A, 0.02f, 0.05f, 0.235f);
            Box(t, 0.2f, 0.2f, 0.02f, 0xD99A5B, 0.03f, 0.06f, 0.245f);
            Box(t, 0.09f, 0.09f, 0.02f, 0xB5763A, 0.04f, 0.07f, 0.255f);
            Straps(t, 0x8D5A2B);
        }),
        NewPack("ladybugwings", "Ladybug shell", 1, 300, "Red wing cases with black spots", t =>
        {
            Both(s =>
            {
                Box(t, 0.24f, 0.48f, 0.12f, 0xE53935, s * 0.125f, 0.03f, 0.1f, s * 6);
                Box(t, 0.08f, 0.08f, 0.02f, 0x222222, s * 0.13f, 0.14f, 0.165f);
                Box(t, 0.07f, 0.07f, 0.02f, 0x222222, s * 0.11f, -0.1f, 0.165f);
            });
            Box(t, 0.2f, 0.12f, 0.1f, 0x222222, 0, 0.27f, 0.08f);
            Straps(t, 0x222222);
        }),
        NewPack("butterfly", "Butterfly wings", 1, 450, "Orange monarch wings", t =>
        {
            Box(t, 0.08f, 0.4f, 0.08f, 0x3E2723, 0, 0.0f, 0.08f);
            Both(s =>
            {
                var w = Pivot(t, s * 0.04f, 0, 0.08f, 0, s * 15, 0);
                Box(w, 0.34f, 0.32f, 0.03f, 0x222222, s * 0.2f, 0.14f, 0);
                Box(w, 0.3f, 0.28f, 0.035f, 0xFF9800, s * 0.2f, 0.14f, 0);
                Box(w, 0.26f, 0.22f, 0.03f, 0x222222, s * 0.16f, -0.14f, 0);
                Box(w, 0.22f, 0.18f, 0.035f, 0xFFB74D, s * 0.16f, -0.14f, 0);
                Box(w, 0.04f, 0.04f, 0.04f, 0xFFFFFF, s * 0.34f, 0.26f, 0);
            });
        }),
        NewPack("treasure", "Treasure chest", 1, 500, "A wooden chest with gold bands", t =>
        {
            Box(t, 0.46f, 0.3f, 0.24f, 0x8D5A2B, 0, -0.06f, 0.13f);
            Box(t, 0.47f, 0.13f, 0.25f, 0xA0693A, 0, 0.15f, 0.13f);
            Both(s => Box(t, 0.04f, 0.44f, 0.255f, 0xFFC83D, s * 0.15f, 0.0f, 0.13f));
            Box(t, 0.08f, 0.1f, 0.02f, 0xFFC83D, 0, 0.06f, 0.26f);
            Box(t, 0.03f, 0.04f, 0.02f, 0x3E2723, 0, 0.05f, 0.272f);
            Straps(t, 0x5D3A1A);
        }),
        NewPack("icecreampack", "Ice cream", 1, 300, "Two scoops and a cherry", t =>
        {
            Box(t, 0.3f, 0.26f, 0.18f, 0xE0A85A, 0, -0.16f, 0.1f);
            Box(t, 0.16f, 0.14f, 0.14f, 0xE0A85A, 0, -0.34f, 0.1f);
            foreach (float y in new[] { -0.1f, -0.2f }) Box(t, 0.31f, 0.02f, 0.185f, 0xC98A3E, 0, y, 0.1f);
            Box(t, 0.42f, 0.2f, 0.24f, 0xF8BBD0, 0, 0.06f, 0.12f);
            Box(t, 0.36f, 0.18f, 0.22f, 0xFFF3E0, 0, 0.24f, 0.12f);
            Box(t, 0.08f, 0.08f, 0.08f, 0xD32F2F, 0, 0.37f, 0.12f);
            Straps(t, 0xF06292);
        }),
        NewPack("makipack", "Sushi roll", 1, 350, "A maki roll with salmon and cucumber", t =>
        {
            Box(t, 0.46f, 0.42f, 0.24f, 0x1F2A1F, 0, 0.02f, 0.13f);
            Box(t, 0.38f, 0.34f, 0.02f, 0xFFFFFF, 0, 0.02f, 0.255f);
            Box(t, 0.12f, 0.12f, 0.02f, 0xFF7043, -0.04f, 0.04f, 0.265f);
            Box(t, 0.07f, 0.07f, 0.02f, 0x66BB6A, 0.08f, -0.04f, 0.265f);
            Box(t, 0.06f, 0.06f, 0.02f, 0xFFD54F, 0.07f, 0.1f, 0.265f);
            Straps(t, 0x1F2A1F);
        }),

        // Mega: 600 to 1000
        NewPack("parachute", "Parachute pack", 2, 600, "A skydiver's pack with an orange chute peeking out", t =>
        {
            Box(t, 0.44f, 0.5f, 0.22f, 0x546E7A, 0, 0.04f, 0.12f);
            foreach (float y in new[] { -0.1f, 0.15f }) Box(t, 0.45f, 0.04f, 0.23f, 0x37474F, 0, y, 0.12f);
            Box(t, 0.4f, 0.06f, 0.18f, 0xFF7043, 0, 0.3f, 0.12f);
            Box(t, 0.08f, 0.06f, 0.03f, 0xE53935, -0.15f, 0.06f, -0.47f);
            Straps(t, 0x37474F);
        }),
        NewPack("scuba", "Scuba tanks", 2, 700, "Twin yellow tanks with a hose", t =>
        {
            Both(s => { Box(t, 0.18f, 0.58f, 0.18f, 0xFFC107, s * 0.1f, 0.04f, 0.13f); Box(t, 0.12f, 0.06f, 0.12f, 0x9E9E9E, s * 0.1f, 0.36f, 0.13f); });
            Box(t, 0.3f, 0.05f, 0.05f, 0x9E9E9E, 0, 0.38f, 0.13f);
            Box(t, 0.04f, 0.04f, 0.3f, 0x222222, 0.2f, 0.38f, 0.0f);
            Box(t, 0.38f, 0.05f, 0.2f, 0x37474F, 0, -0.1f, 0.13f);
            Straps(t, 0x37474F);
        }),
        NewPack("fairy", "Fairy wings", 2, 900, "Shimmery pastel wings with sparkles", t =>
        {
            Box(t, 0.08f, 0.08f, 0.06f, 0xF48FB1, 0, 0.02f, 0.04f, 45);
            Both(s =>
            {
                var w = Pivot(t, s * 0.06f, 0.04f, 0.08f, 0, -s * 45, s * 10);   // swept back, clear of the arms
                Box(w, 0.3f, 0.36f, 0.02f, 0xE1F5FE, s * 0.18f, 0.18f, 0, s * 25);
                Box(w, 0.2f, 0.22f, 0.02f, 0xF3E5F5, s * 0.14f, -0.12f, 0, -s * 20);
                Bx(w, 0.04f, 0.04f, 0.025f, 0xFFE082, s * 0.24f, 0.24f, 0, shadow: false);
                Bx(w, 0.03f, 0.03f, 0.025f, 0xFFE082, s * 0.12f, -0.14f, 0, shadow: false);
            });
        }),
        NewPack("batwings", "Bat wings", 2, 800, "Spooky purple bat wings", t =>
        {
            Box(t, 0.12f, 0.14f, 0.08f, 0x311B92, 0, 0.04f, 0.05f);
            Both(s =>
            {
                var w = Pivot(t, s * 0.05f, 0.05f, 0.06f, 0, s * 15, s * 12);
                Box(w, 0.4f, 0.03f, 0.035f, 0x311B92, s * 0.2f, 0.12f, 0);
                Box(w, 0.38f, 0.2f, 0.03f, 0x4A148C, s * 0.2f, 0.02f, 0);
                foreach (float x in new[] { 0.08f, 0.2f, 0.32f }) Box(w, 0.09f, 0.07f, 0.03f, 0x4A148C, s * x, -0.1f, 0);
            });
        }),
        NewPack("taiko", "Taiko drum", 2, 700, "A Japanese drum, with sticks", t =>
        {
            Box(t, 0.46f, 0.46f, 0.26f, 0xC62828, 0, 0.04f, 0.14f);
            Box(t, 0.38f, 0.38f, 0.02f, 0xF5E6C8, 0, 0.04f, 0.28f);
            Box(t, 0.12f, 0.12f, 0.02f, 0xC62828, 0, 0.04f, 0.29f, 45);
            for (int i = 0; i < 8; i++)
            {
                var p = Quaternion.Euler(0, 0, i * 45f) * new Vector3(0.21f, 0, 0);
                Box(t, 0.03f, 0.03f, 0.02f, 0xFFC83D, p.x, 0.04f + p.y, 0.28f);
            }
            Both(s => Box(t, 0.03f, 0.4f, 0.03f, 0x8D5A2B, s * 0.15f, 0.15f, 0.31f, s * 30));
            Straps(t, 0x8D1F1F);
        }),
        NewPack("lanternpack", "Paper lantern", 2, 650, "A red chōchin lantern", t =>
        {
            Box(t, 0.4f, 0.44f, 0.26f, 0xE53935, 0, 0.0f, 0.14f);
            foreach (float y in new[] { -0.12f, 0, 0.12f }) Box(t, 0.41f, 0.02f, 0.27f, 0xB71C1C, 0, y, 0.14f);
            Both(s => Box(t, 0.3f, 0.06f, 0.2f, 0x222222, 0, s * 0.25f, 0.14f));
            Box(t, 0.15f, 0.15f, 0.02f, 0xFFFFFF, 0, 0.0f, 0.275f);
            Box(t, 0.03f, 0.1f, 0.02f, 0x222222, 0, 0.0f, 0.285f);
            Straps(t, 0x222222);
        }),
        NewPack("shield", "Knight's shield", 1, 400, "A blue shield with a gold cross, and a sword", t =>
        {
            Box(t, 0.04f, 0.14f, 0.04f, 0x9E9E9E, 0.16f, 0.36f, 0.04f);
            Box(t, 0.14f, 0.03f, 0.05f, 0x5D3A1A, 0.16f, 0.3f, 0.04f);
            Box(t, 0.5f, 0.5f, 0.06f, 0x1565C0, 0, 0.04f, 0.08f);
            Box(t, 0.34f, 0.1f, 0.06f, 0x1565C0, 0, -0.24f, 0.08f);
            Box(t, 0.54f, 0.04f, 0.065f, 0xFFC83D, 0, 0.29f, 0.08f);
            Both(s => Box(t, 0.04f, 0.5f, 0.065f, 0xFFC83D, s * 0.27f, 0.04f, 0.08f));
            Box(t, 0.08f, 0.34f, 0.02f, 0xFFC83D, 0, 0.02f, 0.12f);
            Box(t, 0.26f, 0.08f, 0.02f, 0xFFC83D, 0, 0.08f, 0.12f);
            Straps(t, 0x5D3A1A);
        }),

        // Ultra: 1200 to 1800
        NewPack("angelwings", "Angel wings", 3, 1500, "Soft white feathered wings", t => Both(s =>
        {
            var w = Pivot(t, s * 0.06f, 0.08f, 0.08f, 0, -s * 50, s * 15);   // swept back, clear of the arms
            Feather(w, s * 0.22f, 0.2f, 0.4f, 0xFFFFFF);
            Feather(w, s * 0.2f, 0.1f, 0.36f, 0xF7F7F7);
            Feather(w, s * 0.17f, 0.0f, 0.3f, 0xFFFFFF);
            Feather(w, s * 0.14f, -0.1f, 0.24f, 0xF0F0F0);
            Feather(w, s * 0.11f, -0.2f, 0.18f, 0xFFFFFF);
        })),
        NewPack("dragonwings", "Dragon wings", 3, 1800, "Big red dragon wings", t =>
        {
            Box(t, 0.14f, 0.16f, 0.08f, 0xB71C1C, 0, 0.1f, 0.05f);
            Both(s =>
            {
                var w = Pivot(t, s * 0.06f, 0.14f, 0.06f, 0, s * 20, s * 25);
                Box(w, 0.56f, 0.05f, 0.05f, 0xB71C1C, s * 0.28f, 0.1f, 0);
                Box(w, 0.06f, 0.06f, 0.06f, 0xF5E6C8, s * 0.58f, 0.12f, 0);
                foreach (var (x, h) in new[] { (0.12f, 0.3f), (0.28f, 0.36f), (0.44f, 0.28f) })
                    Box(w, 0.16f, h, 0.02f, 0xEF5350, s * x, 0.1f - h / 2, 0);
            });
        }),
        NewPack("mechpack", "Mech pack", 3, 1400, "A robot backpack with blinking lights and an antenna", t =>
        {
            Box(t, 0.48f, 0.52f, 0.26f, 0x78909C, 0, 0.04f, 0.14f);
            foreach (float y in new[] { -0.08f, -0.02f, 0.04f }) Box(t, 0.3f, 0.03f, 0.02f, 0x37474F, -0.04f, y, 0.275f);
            Bx(t, 0.05f, 0.05f, 0.02f, 0x76FF03, 0.15f, 0.2f, 0.275f, shadow: false);
            Bx(t, 0.05f, 0.05f, 0.02f, 0xFF5252, 0.15f, 0.12f, 0.275f, shadow: false);
            Box(t, 0.03f, 0.3f, 0.03f, 0x37474F, 0.16f, 0.42f, 0.14f);
            Bx(t, 0.06f, 0.06f, 0.06f, 0xFF5252, 0.16f, 0.58f, 0.14f, shadow: false);
            Straps(t, 0x37474F);
        }),
        NewPack("helipack", "Heli pack", 3, 1300, "A spinning rotor on a mast", t =>
        {
            Box(t, 0.42f, 0.4f, 0.22f, 0xFFC107, 0, 0.0f, 0.12f);
            Box(t, 0.3f, 0.06f, 0.02f, 0x222222, 0, 0.05f, 0.235f);
            Box(t, 0.05f, 0.5f, 0.05f, 0x616161, 0, 0.42f, 0.12f);
            var rotor = Pivot(t, 0, 0.68f, 0.12f);
            Box(rotor, 1.0f, 0.03f, 0.08f, 0x424242, 0, 0, 0);
            Box(rotor, 0.08f, 0.06f, 0.08f, 0x616161, 0, 0.02f, 0);
            rotor.gameObject.AddComponent<Spinner>().DegreesPerSecond = 900;
            Straps(t, 0x616161);
        }),
        NewPack("katana", "Twin katana", 3, 1200, "Two sheathed swords, crossed", t =>
        {
            Both(s =>
            {
                var sword = Pivot(t, 0, 0.05f, 0.08f + s * 0.02f, 0, 0, s * 35);
                Box(sword, 0.07f, 0.8f, 0.06f, 0x222222, 0, 0, 0);
                Box(sword, 0.16f, 0.03f, 0.1f, 0xFFC83D, 0, 0.42f, 0);
                Box(sword, 0.06f, 0.22f, 0.06f, 0xF5F5F5, 0, 0.55f, 0);
                foreach (float y in new[] { 0.48f, 0.55f, 0.62f }) Box(sword, 0.065f, 0.025f, 0.065f, 0x222222, 0, y, 0);
            });
            Sash(t, 0x8E1F1F);
        }),

        // Legendary: 2000 to 2500
        NewPack("turbojet", "Turbo jet", 4, 2500, "Twin rocket boosters with blue flames", t =>
        {
            Both(s =>
            {
                Box(t, 0.2f, 0.56f, 0.22f, 0xECEFF1, s * 0.13f, 0.04f, 0.13f);
                Box(t, 0.2f, 0.1f, 0.22f, 0xD32F2F, s * 0.13f, 0.34f, 0.13f);
                Box(t, 0.16f, 0.08f, 0.18f, 0x424242, s * 0.13f, -0.27f, 0.13f);
                Bx(t, 0.12f, 0.2f, 0.12f, 0x4FC3F7, s * 0.13f, -0.42f, 0.13f, shadow: false);
                Bx(t, 0.06f, 0.12f, 0.06f, 0xFFFFFF, s * 0.13f, -0.38f, 0.13f, shadow: false);
                Box(t, 0.2f, 0.04f, 0.12f, 0xD32F2F, s * 0.3f, -0.05f, 0.13f, -s * 15);
            });
            Straps(t, 0x424242);
        }),
        NewPack("phoenixwings", "Phoenix wings", 4, 2500, "Wings of fire", t => Both(s =>
        {
            var w = Pivot(t, s * 0.06f, 0.1f, 0.06f, 0, s * 15, s * 30);
            Feather(w, s * 0.26f, 0.2f, 0.5f, 0xFF4A1A);
            Feather(w, s * 0.23f, 0.1f, 0.44f, 0xFF6A1A);
            Feather(w, s * 0.19f, 0.0f, 0.36f, 0xFF8A1F);
            Feather(w, s * 0.15f, -0.1f, 0.28f, 0xFFB13D);
            Feather(w, s * 0.11f, -0.2f, 0.2f, 0xFFD23D);
        })),
        NewPack("galaxycape", "Galaxy cape", 4, 2200, "A cape made of the night sky", t =>
        {
            Box(t, 0.62f, 0.76f, 0.03f, 0x2A2466, 0, -0.12f, 0.03f);
            Box(t, 0.7f, 0.18f, 0.03f, 0x2A2466, 0, -0.5f, 0.05f, 0);
            Box(t, 0.64f, 0.06f, 0.06f, 0x4A3F95, 0, 0.27f, 0.02f);
            foreach (var (x, y, c) in new[] { (-0.18f, 0.1f, 0xFFFFFFu), (0.12f, 0.0f, 0xFFE08Au), (-0.05f, -0.22f, 0xB39DFFu), (0.2f, -0.35f, 0xFFFFFFu), (-0.22f, -0.42f, 0xFFE08Au), (0.05f, 0.18f, 0xB39DFFu) })
                Bx(t, 0.04f, 0.04f, 0.035f, c, x, y, 0.04f, shadow: false);
            Both(s => Box(t, 0.08f, 0.08f, 0.03f, 0xFFD54F, s * 0.14f, 0.22f, -0.43f));
        }),
        NewPack("miniplanet", "Mini planet", 4, 2000, "Your own little world, floating behind you", t =>
        {
            var planet = Pivot(t, 0, 0.1f, 0.42f, 10, 0, 20);
            Box(planet, 0.4f, 0.4f, 0.4f, 0x4FC3F7, 0, 0, 0);
            Box(planet, 0.22f, 0.16f, 0.02f, 0x66BB6A, -0.04f, 0.06f, 0.2f);
            Box(planet, 0.02f, 0.14f, 0.2f, 0x66BB6A, 0.2f, -0.06f, 0);
            Box(planet, 0.16f, 0.02f, 0.14f, 0xFFFFFF, 0, 0.2f, 0);
            Ring(planet, 0.34f, 0, 0xFFE082, 16, 0.03f, false);
            planet.gameObject.AddComponent<Spinner>().DegreesPerSecond = 40;
        }),
    };
}
