using System.Collections.Generic;
using UnityEngine;

// Hats sit on the hat anchor: y = 0 is the top of the head, which is 0.55 wide and 0.5 deep (front at z = -0.25).
public static partial class Cosmetics
{
    static Cosmetic NewHat(string id, string name, int rarity, int price, string notes, System.Action<Transform> build) =>
        Cand(CosmeticKind.Hat, id, name, rarity, price, notes, build: build);

    /// A ring of boxes, flat, around the y axis (halos, planet rings).
    static void Ring(Transform t, float radius, float y, uint hex, int pieces = 10, float thickness = 0.04f, bool shadow = true)
    {
        for (int i = 0; i < pieces; i++)
        {
            float a = i * 360f / pieces;
            var p = Quaternion.Euler(0, a, 0) * new Vector3(radius, 0, 0);
            Bx(t, thickness * 1.4f, thickness, 2 * Mathf.PI * radius / pieces + 0.01f, hex, p.x, y, p.z, ry: a, shadow: shadow);
        }
    }

    /// A five-point star made of boxes, facing -z (or +z for charms; it's flat).
    static void StarShape(Transform t, float size, uint hex, float x, float y, float z, float depth = 0.03f)
    {
        Box(t, size, size, depth, hex, x, y, z);
        float arm = size * 0.62f, off = size * 0.78f;
        Box(t, arm, arm, depth, hex, x, y + off, z);
        Bx(t, arm, arm, depth, hex, x - off * 0.95f, y + off * 0.25f, z, rz: 18);
        Bx(t, arm, arm, depth, hex, x + off * 0.95f, y + off * 0.25f, z, rz: -18);
        Bx(t, arm, arm, depth, hex, x - off * 0.6f, y - off * 0.8f, z, rz: 36);
        Bx(t, arm, arm, depth, hex, x + off * 0.6f, y - off * 0.8f, z, rz: -36);
    }

    static readonly List<Cosmetic> HatCandidates = new()
    {
        // Common: 60 to 120
        NewHat("cap", "Baseball cap", 0, 60, "Red cap with a white logo", t =>
        {
            Box(t, 0.58f, 0.16f, 0.53f, 0xE53935, 0, 0.08f, 0);
            Box(t, 0.5f, 0.04f, 0.24f, 0xC62828, 0, 0.01f, -0.36f);
            Box(t, 0.08f, 0.04f, 0.08f, 0xC62828, 0, 0.18f, 0);
            Box(t, 0.1f, 0.08f, 0.02f, 0xFFFFFF, 0, 0.09f, -0.272f);
        }),
        NewHat("bucket", "Bucket hat", 0, 70, "A sandy bucket hat for the desert planets", t =>
        {
            Box(t, 0.52f, 0.16f, 0.48f, 0xF2E3B3, 0, 0.1f, 0);
            Box(t, 0.76f, 0.04f, 0.7f, 0xE8D49A, 0, 0.02f, 0);
            Box(t, 0.53f, 0.04f, 0.49f, 0x8D6E4F, 0, 0.05f, 0);
        }),
        NewHat("bow", "Big bow", 0, 60, "A big pink ribbon bow", t =>
        {
            Box(t, 0.08f, 0.08f, 0.08f, 0xE05584, 0.14f, 0.04f, -0.08f);
            Both(s => Box(t, 0.15f, 0.13f, 0.06f, 0xF06292, 0.14f + s * 0.1f, 0.05f, -0.08f, s * 20));
        }),
        NewHat("sweatband", "Sweatband", 0, 60, "Sporty, with a red stripe", t =>
        {
            Box(t, 0.58f, 0.08f, 0.53f, 0xFFFFFF, 0, -0.08f, 0);
            Box(t, 0.585f, 0.025f, 0.535f, 0xE53935, 0, -0.08f, 0);
        }),
        NewHat("beret", "Beret", 0, 80, "A tilted red beret", t =>
        {
            Box(t, 0.6f, 0.08f, 0.56f, 0xC62828, 0.04f, 0.04f, 0, -8);
            Box(t, 0.04f, 0.06f, 0.04f, 0xC62828, 0.04f, 0.11f, 0);
        }),
        NewHat("bunnyears", "Bunny ears", 0, 90, "A headband with bunny ears", t =>
        {
            Box(t, 0.6f, 0.04f, 0.08f, 0x222222, 0, 0.02f, 0);
            Both(s =>
            {
                Box(t, 0.1f, 0.32f, 0.06f, 0xFFFFFF, s * 0.13f, 0.18f, 0, -s * 8);
                Box(t, 0.05f, 0.24f, 0.02f, 0xF8BBD0, s * 0.13f, 0.17f, -0.035f, -s * 8);
            });
        }),
        NewHat("catears", "Cat ears", 0, 90, "A headband with black cat ears", t =>
        {
            Box(t, 0.6f, 0.04f, 0.08f, 0x2B2B2B, 0, 0.02f, 0);
            Both(s =>
            {
                Box(t, 0.13f, 0.13f, 0.06f, 0x2B2B2B, s * 0.17f, 0.06f, 0, 45);
                Box(t, 0.06f, 0.06f, 0.02f, 0xF48FB1, s * 0.17f, 0.07f, -0.035f, 45);
            });
        }),
        NewHat("sprout", "Leaf sprout", 0, 60, "A little sprout growing out of your head", t =>
        {
            Box(t, 0.03f, 0.12f, 0.03f, 0x5DAE4B, 0, 0.06f, 0);
            Both(s => Box(t, 0.12f, 0.03f, 0.07f, 0x6CCB55, s * 0.06f, 0.13f, 0, s * 20));
        }),
        NewHat("bandana", "Bandana", 0, 80, "Blue with white dots", t =>
        {
            Box(t, 0.58f, 0.08f, 0.53f, 0x2F80ED, 0, 0.03f, 0);
            foreach (float x in new[] { -0.15f, 0, 0.15f }) Box(t, 0.04f, 0.04f, 0.02f, 0xFFFFFF, x, 0.03f, -0.27f);
            Box(t, 0.1f, 0.12f, 0.04f, 0x2F80ED, 0.1f, -0.06f, 0.28f);
        }),
        NewHat("earmuffs", "Earmuffs", 0, 100, "Fluffy pink earmuffs for the ice planets", t =>
        {
            Box(t, 0.6f, 0.04f, 0.06f, 0x8E8E99, 0, 0.03f, 0);
            Both(s => { Box(t, 0.04f, 0.18f, 0.06f, 0x8E8E99, s * 0.3f, -0.06f, 0); Box(t, 0.1f, 0.2f, 0.22f, 0xF8BBD0, s * 0.32f, -0.2f, 0); });
        }),
        NewHat("strawhat", "Straw hat", 0, 100, "A wide straw hat with a red band", t =>
        {
            Box(t, 0.92f, 0.04f, 0.86f, 0xF2D27A, 0, 0.02f, 0);
            Box(t, 0.5f, 0.16f, 0.46f, 0xEBC65E, 0, 0.1f, 0);
            Box(t, 0.51f, 0.05f, 0.47f, 0xD32F2F, 0, 0.05f, 0);
        }),
        NewHat("visor", "Sun visor", 0, 70, "Sporty, for sunny planets", t =>
        {
            Box(t, 0.58f, 0.06f, 0.53f, 0x4FC3F7, 0, -0.03f, 0);
            Box(t, 0.5f, 0.03f, 0.22f, 0x29A0D8, 0, -0.03f, -0.36f);
        }),
        NewHat("hardhat", "Hard hat", 0, 120, "Safety first on the climb", t =>
        {
            Box(t, 0.6f, 0.18f, 0.56f, 0xFFC107, 0, 0.09f, 0);
            Box(t, 0.08f, 0.06f, 0.5f, 0xFFB300, 0, 0.2f, 0);
            Box(t, 0.66f, 0.03f, 0.66f, 0xFFC107, 0, 0.01f, -0.02f);
        }),
        NewHat("snorkel", "Snorkel mask", 0, 120, "Pushed up on the forehead", t =>
        {
            Box(t, 0.58f, 0.04f, 0.53f, 0x222222, 0, -0.07f, 0);
            Box(t, 0.46f, 0.13f, 0.04f, 0x4FC3F7, 0, -0.07f, -0.28f);
            Box(t, 0.4f, 0.09f, 0.02f, 0xBDEBFF, 0, -0.07f, -0.3f);
            Box(t, 0.05f, 0.32f, 0.05f, 0xFF8A1F, 0.31f, 0.0f, 0);
        }),

        // Rare: 150 to 300
        NewHat("chefhat", "Chef hat", 1, 150, "A puffy white toque", t =>
        {
            Box(t, 0.56f, 0.1f, 0.51f, 0xFFFFFF, 0, 0.05f, 0);
            Box(t, 0.64f, 0.24f, 0.58f, 0xFAFAFA, 0, 0.22f, 0);
            Box(t, 0.4f, 0.08f, 0.4f, 0xFAFAFA, 0, 0.36f, 0);
        }),
        NewHat("tricorn", "Pirate hat", 1, 250, "A tricorn with a skull and gold trim", t =>
        {
            Box(t, 0.82f, 0.06f, 0.5f, 0x222222, 0, 0.04f, 0);
            Box(t, 0.52f, 0.2f, 0.4f, 0x2B2B2B, 0, 0.14f, 0);
            Box(t, 0.82f, 0.16f, 0.04f, 0x222222, 0, 0.1f, -0.24f);
            Box(t, 0.82f, 0.02f, 0.045f, 0xFFC83D, 0, 0.18f, -0.24f);
            Box(t, 0.1f, 0.08f, 0.02f, 0xFFFFFF, 0, 0.11f, -0.265f);
        }),
        NewHat("wizardhat", "Wizard hat", 1, 250, "Tall, blue and starry, with a floppy tip", t =>
        {
            Box(t, 0.74f, 0.04f, 0.68f, 0x3949AB, 0, 0.02f, 0);
            Box(t, 0.44f, 0.14f, 0.4f, 0x3949AB, 0.02f, 0.09f, 0);
            Box(t, 0.32f, 0.14f, 0.3f, 0x3949AB, 0.05f, 0.23f, 0);
            Box(t, 0.2f, 0.14f, 0.2f, 0x3949AB, 0.1f, 0.36f, 0, -15);
            Box(t, 0.1f, 0.12f, 0.1f, 0x3949AB, 0.18f, 0.46f, 0, -40);
            Box(t, 0.06f, 0.06f, 0.02f, 0xFFD54F, 0.06f, 0.13f, -0.205f, 45);
            Box(t, 0.04f, 0.04f, 0.02f, 0xFFD54F, -0.04f, 0.26f, -0.155f, 45);
        }),
        NewHat("witchhat", "Witch hat", 1, 250, "Black with a purple band and a gold buckle", t =>
        {
            Box(t, 0.8f, 0.04f, 0.74f, 0x2B2B2B, 0, 0.02f, 0);
            Box(t, 0.44f, 0.14f, 0.4f, 0x2B2B2B, 0, 0.09f, 0);
            Box(t, 0.45f, 0.05f, 0.41f, 0x8E44AD, 0, 0.06f, 0);
            Box(t, 0.08f, 0.06f, 0.02f, 0xFFC83D, 0, 0.06f, -0.21f);
            Box(t, 0.3f, 0.16f, 0.28f, 0x2B2B2B, -0.03f, 0.23f, 0, 8);
            Box(t, 0.16f, 0.16f, 0.16f, 0x2B2B2B, -0.08f, 0.37f, 0, 20);
        }),
        NewHat("graduation", "Graduation cap", 1, 180, "With a golden tassel", t =>
        {
            Box(t, 0.5f, 0.1f, 0.46f, 0x222222, 0, 0.05f, 0);
            Bx(t, 0.68f, 0.03f, 0.68f, 0x2B2B2B, 0, 0.12f, 0, ry: 45);
            Box(t, 0.06f, 0.03f, 0.06f, 0xFFC83D, 0, 0.145f, 0);
            Box(t, 0.03f, 0.03f, 0.3f, 0xFFC83D, 0, 0.14f, -0.15f);
            Box(t, 0.03f, 0.18f, 0.03f, 0xFFC83D, 0, 0.05f, -0.3f);
        }),
        NewHat("santa", "Santa hat", 1, 200, "Red with a fluffy white trim and bobble", t =>
        {
            Box(t, 0.6f, 0.1f, 0.55f, 0xFFFFFF, 0, 0.05f, 0);
            Box(t, 0.5f, 0.14f, 0.46f, 0xD32F2F, 0, 0.17f, 0.02f);
            Box(t, 0.36f, 0.12f, 0.32f, 0xD32F2F, 0.04f, 0.29f, 0.06f);
            Box(t, 0.2f, 0.1f, 0.18f, 0xD32F2F, 0.12f, 0.36f, 0.12f, -25);
            Box(t, 0.12f, 0.12f, 0.12f, 0xFFFFFF, 0.22f, 0.36f, 0.14f);
        }),
        NewHat("headphones", "Headphones", 1, 200, "Big pink headphones", t =>
        {
            Box(t, 0.62f, 0.05f, 0.08f, 0x2B2B2B, 0, 0.04f, 0);
            Both(s => { Box(t, 0.04f, 0.2f, 0.06f, 0x2B2B2B, s * 0.31f, -0.06f, 0); Box(t, 0.1f, 0.22f, 0.22f, 0xFF5C8A, s * 0.32f, -0.2f, 0); Box(t, 0.02f, 0.14f, 0.14f, 0xFFFFFF, s * 0.375f, -0.2f, 0); });
        }),
        NewHat("sombrero", "Sombrero", 1, 280, "A huge brim with a colorful band", t =>
        {
            Box(t, 1.1f, 0.04f, 1.0f, 0xE8C25A, 0, 0.02f, 0);
            Both(s => { Box(t, 1.1f, 0.06f, 0.05f, 0xC9A23A, 0, 0.05f, s * 0.5f); Box(t, 0.05f, 0.06f, 1.0f, 0xC9A23A, s * 0.55f, 0.05f, 0); });
            Box(t, 0.46f, 0.3f, 0.42f, 0xE8C25A, 0, 0.18f, 0);
            Box(t, 0.47f, 0.06f, 0.43f, 0xD32F2F, 0, 0.08f, 0);
            Box(t, 0.47f, 0.03f, 0.43f, 0x43A047, 0, 0.125f, 0);
        }),
        NewHat("aviator", "Aviator cap", 1, 220, "Leather cap with goggles", t =>
        {
            Box(t, 0.6f, 0.14f, 0.55f, 0x8D5A2B, 0, 0.04f, 0);
            Both(s => Box(t, 0.06f, 0.24f, 0.2f, 0x8D5A2B, s * 0.3f, -0.12f, 0));
            Box(t, 0.6f, 0.04f, 0.555f, 0x5D3A1A, 0, 0.0f, 0);
            Both(s => { Box(t, 0.14f, 0.1f, 0.04f, 0xC9952B, s * 0.11f, 0.02f, -0.28f); Box(t, 0.1f, 0.07f, 0.02f, 0x9EE6FF, s * 0.11f, 0.02f, -0.3f); });
        }),
        NewHat("fez", "Fez", 1, 150, "Red, with a swinging tassel", t =>
        {
            Box(t, 0.36f, 0.24f, 0.34f, 0xC62828, 0, 0.12f, 0);
            Box(t, 0.3f, 0.02f, 0.28f, 0xB71C1C, 0, 0.245f, 0);
            Box(t, 0.03f, 0.04f, 0.03f, 0x222222, 0, 0.27f, 0);
            Box(t, 0.12f, 0.03f, 0.03f, 0x222222, 0.08f, 0.28f, 0);
            Box(t, 0.03f, 0.16f, 0.03f, 0x222222, 0.15f, 0.2f, 0);
        }),
        NewHat("bikehelmet", "Bike helmet", 1, 180, "Teal with vents and a visor", t =>
        {
            Box(t, 0.62f, 0.16f, 0.6f, 0x26C6DA, 0, 0.08f, 0.02f);
            Both(s => Box(t, 0.06f, 0.17f, 0.5f, 0x00838F, s * 0.12f, 0.09f, 0.02f));
            Box(t, 0.4f, 0.04f, 0.12f, 0x00838F, 0, 0.03f, -0.33f);
        }),
        NewHat("antlers", "Antlers", 1, 220, "A headband with reindeer antlers", t =>
        {
            Box(t, 0.6f, 0.04f, 0.08f, 0x8D5A2B, 0, 0.02f, 0);
            Both(s =>
            {
                var antler = Pivot(t, s * 0.18f, 0.02f, 0, 0, 0, -s * 15);
                Box(antler, 0.05f, 0.26f, 0.05f, 0xA0693A, 0, 0.13f, 0);
                Box(antler, 0.12f, 0.05f, 0.05f, 0xA0693A, s * 0.06f, 0.2f, 0);
                Box(antler, 0.05f, 0.1f, 0.05f, 0xA0693A, s * 0.1f, 0.25f, 0);
                Box(antler, 0.1f, 0.05f, 0.05f, 0xA0693A, -s * 0.05f, 0.12f, 0);
            });
        }),

        // Mega: 350 to 600
        NewHat("halo", "Halo", 2, 500, "A floating golden ring", t =>
        {
            var halo = Pivot(t, 0, 0.18f, 0);
            Ring(halo, 0.22f, 0, 0xFFD54F, 12, 0.04f, false);
            halo.gameObject.AddComponent<Spinner>().DegreesPerSecond = 60;
        }),
        NewHat("horns", "Little horns", 2, 400, "Two little red devil horns", t => Both(s =>
        {
            Box(t, 0.09f, 0.1f, 0.09f, 0xD32F2F, s * 0.16f, 0.05f, -0.05f);
            Box(t, 0.06f, 0.08f, 0.06f, 0xD32F2F, s * 0.19f, 0.12f, -0.05f, -s * 20);
            Box(t, 0.04f, 0.05f, 0.04f, 0xE57373, s * 0.22f, 0.17f, -0.05f, -s * 35);
        })),
        NewHat("mushroomhat", "Mushroom cap", 2, 400, "A red toadstool with white spots", t =>
        {
            Box(t, 0.72f, 0.14f, 0.66f, 0xE53935, 0, 0.07f, 0);
            Box(t, 0.5f, 0.1f, 0.48f, 0xE53935, 0, 0.18f, 0);
            foreach (var p in new[] { new Vector2(-0.12f, -0.1f), new Vector2(0.14f, 0.08f), new Vector2(-0.06f, 0.14f) }) Box(t, 0.1f, 0.02f, 0.1f, 0xFFFFFF, p.x, 0.235f, p.y);
            Both(s => Box(t, 0.08f, 0.08f, 0.02f, 0xFFFFFF, s * 0.2f, 0.07f, -0.335f));
            Box(t, 0.1f, 0.06f, 0.02f, 0xFFFFFF, 0, 0.09f, -0.335f);
        }),
        NewHat("sushihat", "Sushi hat", 2, 450, "A salmon nigiri, sitting on your head", t =>
        {
            Box(t, 0.42f, 0.12f, 0.26f, 0xFFFFFF, 0, 0.06f, 0);
            Box(t, 0.46f, 0.06f, 0.3f, 0xFF8A65, 0, 0.15f, 0);
            foreach (float x in new[] { -0.12f, 0, 0.12f }) Box(t, 0.025f, 0.065f, 0.3f, 0xFFCCBC, x, 0.15f, 0, 20);
        }),
        NewHat("froghat", "Frog hat", 2, 450, "A frog face hat, with big eyes", t =>
        {
            Box(t, 0.6f, 0.16f, 0.55f, 0x6CCB55, 0, 0.08f, 0);
            Both(s =>
            {
                Box(t, 0.15f, 0.13f, 0.15f, 0xFFFFFF, s * 0.14f, 0.2f, -0.1f);
                Box(t, 0.06f, 0.06f, 0.02f, 0x111111, s * 0.14f, 0.2f, -0.18f);
                Box(t, 0.07f, 0.04f, 0.02f, 0xF8A5C2, s * 0.2f, 0.06f, -0.28f);
            });
            Box(t, 0.36f, 0.02f, 0.02f, 0x2E5E22, 0, 0.05f, -0.28f);
        }),
        NewHat("pumpkinhat", "Pumpkin hat", 2, 400, "A grinning jack-o'-lantern", t =>
        {
            Box(t, 0.5f, 0.3f, 0.46f, 0xF57C1F, 0, 0.15f, 0);
            Both(s => Box(t, 0.04f, 0.3f, 0.47f, 0xE06A10, s * 0.12f, 0.15f, 0));
            Box(t, 0.06f, 0.1f, 0.06f, 0x5D7A2E, 0, 0.35f, 0);
            Both(s => Box(t, 0.08f, 0.06f, 0.02f, 0x2B1B0A, s * 0.1f, 0.19f, -0.235f, s * 12));
            Box(t, 0.22f, 0.04f, 0.02f, 0x2B1B0A, 0, 0.09f, -0.235f);
        }),
        NewHat("cake", "Birthday cake", 2, 500, "With frosting, sprinkles and a lit candle", t =>
        {
            Box(t, 0.5f, 0.12f, 0.46f, 0xF8BBD0, 0, 0.06f, 0);
            Box(t, 0.52f, 0.04f, 0.48f, 0xFFFFFF, 0, 0.14f, 0);
            Box(t, 0.36f, 0.1f, 0.34f, 0xFFF3E0, 0, 0.21f, 0);
            Box(t, 0.38f, 0.03f, 0.36f, 0xF06292, 0, 0.265f, 0);
            Box(t, 0.04f, 0.14f, 0.04f, 0x4FC3F7, 0, 0.35f, 0);
            Bx(t, 0.05f, 0.07f, 0.05f, 0xFFB13D, 0, 0.45f, 0, shadow: false);
            foreach (var (x, c) in new[] { (-0.16f, 0xFFD54Fu), (-0.05f, 0x4FC3F7u), (0.07f, 0x81C784u), (0.17f, 0xFFD54Fu) }) Box(t, 0.03f, 0.03f, 0.02f, c, x, 0.06f, -0.235f);
        }),
        NewHat("ufohat", "Mini UFO", 2, 600, "A tiny flying saucer hovering over you", t =>
        {
            var ufo = Pivot(t, 0, 0.24f, 0);
            Box(ufo, 0.56f, 0.06f, 0.56f, 0xB0BEC5, 0, 0, 0);
            Box(ufo, 0.4f, 0.05f, 0.4f, 0x90A4AE, 0, -0.05f, 0);
            Box(ufo, 0.24f, 0.12f, 0.24f, 0x9EE6FF, 0, 0.09f, 0);
            foreach (var p in new[] { new Vector2(0.24f, 0), new Vector2(-0.24f, 0), new Vector2(0, 0.24f), new Vector2(0, -0.24f) })
                Bx(ufo, 0.05f, 0.04f, 0.05f, 0xFFE14D, p.x, 0, p.y, shadow: false);
            ufo.gameObject.AddComponent<Spinner>().DegreesPerSecond = 80;
        }),
        NewHat("raincloud", "Rain cloud", 2, 550, "Your own little cloud, raining on you", t =>
        {
            var cloud = Pivot(t, 0, 0.3f, 0);
            Box(cloud, 0.24f, 0.16f, 0.24f, 0xCFD8DC, -0.13f, 0, 0);
            Box(cloud, 0.3f, 0.22f, 0.28f, 0xECEFF1, 0.03f, 0.04f, 0);
            Box(cloud, 0.2f, 0.14f, 0.2f, 0xCFD8DC, 0.19f, -0.01f, 0);
            foreach (var p in new[] { new Vector2(-0.12f, -0.14f), new Vector2(0.02f, -0.2f), new Vector2(0.16f, -0.15f) })
                Bx(cloud, 0.03f, 0.06f, 0.03f, 0x4FC3F7, p.x, p.y, -0.05f, shadow: false);
        }),
        NewHat("kabuto", "Kabuto", 2, 600, "A samurai helmet with a golden crest", t =>
        {
            Box(t, 0.62f, 0.16f, 0.58f, 0x2B2B2B, 0, 0.08f, 0);
            Box(t, 0.74f, 0.05f, 0.62f, 0x3A3A3A, 0, 0.0f, 0.04f);
            Both(s => Box(t, 0.06f, 0.18f, 0.4f, 0x2B2B2B, s * 0.37f, -0.06f, 0.05f, s * 20));
            Both(s => Box(t, 0.05f, 0.3f, 0.04f, 0xD4A23A, s * 0.1f, 0.25f, -0.3f, -s * 25));
            Box(t, 0.1f, 0.08f, 0.04f, 0xD4A23A, 0, 0.1f, -0.3f);
            Box(t, 0.63f, 0.03f, 0.59f, 0xC62828, 0, 0.03f, 0);
        }),

        // Ultra: 700 to 1000
        NewHat("birdnest", "Bird's nest", 3, 750, "A nest with a chick and two blue eggs", t =>
        {
            Box(t, 0.46f, 0.1f, 0.42f, 0x8D6E4F, 0, 0.05f, 0);
            foreach (var (x, z, a) in new[] { (-0.2f, -0.1f, 30f), (0.18f, 0.12f, -20f), (0.05f, -0.2f, 70f) }) Box(t, 0.2f, 0.03f, 0.03f, 0x6D4C2F, x, 0.1f, z, a);
            Box(t, 0.16f, 0.14f, 0.16f, 0xFFD54F, 0, 0.17f, 0);
            Box(t, 0.05f, 0.04f, 0.05f, 0xF57C1F, 0, 0.18f, -0.1f);
            Both(s => Box(t, 0.02f, 0.03f, 0.01f, 0x111111, s * 0.04f, 0.21f, -0.085f));
            Both(s => Box(t, 0.08f, 0.1f, 0.08f, 0xBFE9FA, s * 0.15f, 0.12f, 0.08f));
        }),
        NewHat("jester", "Jester hat", 3, 700, "Two floppy points with bells", t =>
        {
            Box(t, 0.58f, 0.1f, 0.53f, 0x8E44AD, 0, 0.05f, 0);
            Box(t, 0.6f, 0.05f, 0.55f, 0xFFD54F, 0, 0.0f, 0);
            Both(s =>
            {
                var point = Pivot(t, s * 0.12f, 0.08f, 0, 0, 0, -s * 50);
                Box(point, 0.16f, 0.36f, 0.16f, s < 0 ? 0x8E44AD : 0xFFD54Fu, 0, 0.16f, 0);
                Box(point, 0.1f, 0.14f, 0.1f, s < 0 ? 0xFFD54F : 0x8E44ADu, 0, 0.4f, 0);
                Box(point, 0.09f, 0.09f, 0.09f, 0xFFC83D, 0, 0.5f, 0);
            });
        }),
        NewHat("pharaoh", "Pharaoh headdress", 3, 900, "Blue and gold stripes, with a cobra", t =>
        {
            Box(t, 0.6f, 0.08f, 0.55f, 0x2F5FC0, 0, 0.03f, 0);
            Box(t, 0.61f, 0.025f, 0.56f, 0xFFC83D, 0, 0.0f, 0);
            Both(s =>
            {
                Box(t, 0.12f, 0.42f, 0.16f, 0x2F5FC0, s * 0.33f, -0.22f, -0.05f);
                foreach (float y in new[] { -0.08f, -0.2f, -0.32f }) Box(t, 0.125f, 0.03f, 0.165f, 0xFFC83D, s * 0.33f, y, -0.05f);
            });
            Box(t, 0.06f, 0.12f, 0.04f, 0xFFC83D, 0, 0.08f, -0.28f);
            Box(t, 0.08f, 0.05f, 0.04f, 0xFFC83D, 0, 0.13f, -0.28f);
        }),
        NewHat("knighthelm", "Knight helmet", 3, 900, "A full steel helmet with a red plume", t =>
        {
            Box(t, 0.62f, 0.5f, 0.6f, 0xB0BEC5, 0, -0.2f, 0);
            Box(t, 0.42f, 0.05f, 0.02f, 0x2B2B2B, 0, -0.18f, -0.305f);
            Box(t, 0.04f, 0.4f, 0.02f, 0x90A4AE, 0, -0.22f, -0.305f);
            Box(t, 0.08f, 0.2f, 0.3f, 0xE53935, 0, 0.13f, 0.05f);
            Box(t, 0.08f, 0.12f, 0.16f, 0xE53935, 0, 0.21f, 0.17f);
        }),
        NewHat("dragonhood", "Dragon hood", 3, 1000, "A green dragon hood with horns and spikes", t =>
        {
            Box(t, 0.62f, 0.16f, 0.57f, 0x43A047, 0, 0.06f, 0.02f);
            Both(s => { Box(t, 0.07f, 0.18f, 0.07f, 0xF5E6C8, s * 0.17f, 0.2f, 0.05f, -s * 25); Box(t, 0.08f, 0.06f, 0.04f, 0xFFD54F, s * 0.12f, 0.1f, -0.28f); Box(t, 0.04f, 0.04f, 0.02f, 0x111111, s * 0.12f, 0.1f, -0.3f); });
            foreach (var (y, z) in new[] { (0.16f, 0.1f), (0.12f, 0.24f), (0.02f, 0.32f) }) Bx(t, 0.06f, 0.1f, 0.08f, 0xF2994A, 0, y, z, rx: 30);
        }),
        NewHat("octopus", "Octopus", 3, 850, "A pink octopus giving you a hug", t =>
        {
            Box(t, 0.42f, 0.26f, 0.4f, 0xF06292, 0, 0.13f, 0);
            Both(s => { Box(t, 0.08f, 0.08f, 0.02f, 0xFFFFFF, s * 0.09f, 0.15f, -0.205f); Box(t, 0.04f, 0.04f, 0.01f, 0x111111, s * 0.09f, 0.14f, -0.215f); });
            for (int i = 0; i < 6; i++)
            {
                if (i == 1) continue;   // not in front of the face
                float a = (i + 0.5f) * 60f;
                var p = Quaternion.Euler(0, a, 0) * new Vector3(0.26f, 0, 0);
                Box(t, 0.07f, 0.24f, 0.07f, 0xF06292, p.x, -0.08f, p.z);
                Box(t, 0.07f, 0.07f, 0.12f, 0xF06292, p.x * 1.2f, -0.18f, p.z * 1.2f);
            }
        }),
        NewHat("volcano", "Volcano", 3, 1000, "A smoking volcano, for the hot planets", t =>
        {
            Box(t, 0.6f, 0.12f, 0.56f, 0x5D4037, 0, 0.06f, 0);
            Box(t, 0.44f, 0.12f, 0.4f, 0x6D4C41, 0, 0.18f, 0);
            Box(t, 0.3f, 0.1f, 0.28f, 0x795548, 0, 0.29f, 0);
            Bx(t, 0.2f, 0.03f, 0.18f, 0xFF6A1A, 0, 0.345f, 0, shadow: false);
            Bx(t, 0.05f, 0.14f, 0.02f, 0xFF8A1F, -0.06f, 0.27f, -0.145f, shadow: false);
            Bx(t, 0.05f, 0.18f, 0.02f, 0xFF8A1F, 0.1f, 0.17f, -0.205f, shadow: false);
            Box(t, 0.1f, 0.1f, 0.1f, 0xB0BEC5, 0.02f, 0.44f, 0);
            Box(t, 0.08f, 0.08f, 0.08f, 0xCFD8DC, -0.05f, 0.54f, 0);
        }),

        // Legendary: 1200 to 1500
        NewHat("saturn", "Ringed planet", 4, 1500, "A little planet with a ring, orbiting above you", t =>
        {
            var planet = Pivot(t, 0, 0.32f, 0, 15, 0, 15);
            Box(planet, 0.28f, 0.28f, 0.28f, 0xF5B860, 0, 0, 0);
            Box(planet, 0.29f, 0.05f, 0.29f, 0xE0974A, 0, 0.04f, 0);
            Ring(planet, 0.3f, 0, 0xFFE0A3, 14, 0.03f, false);
            planet.gameObject.AddComponent<Spinner>().DegreesPerSecond = 40;
        }),
        NewHat("startiara", "Star tiara", 4, 1400, "A golden tiara with three stars", t =>
        {
            Box(t, 0.58f, 0.06f, 0.53f, 0xFFD54F, 0, 0.03f, 0);
            StarShape(t, 0.08f, 0xFFE14D, 0, 0.15f, -0.25f);
            Both(s => StarShape(t, 0.05f, 0xFFE14D, s * 0.17f, 0.11f, -0.25f));
            Box(t, 0.04f, 0.04f, 0.02f, 0xF06292, 0, 0.15f, -0.27f);
        }),
        NewHat("flamecrown", "Flame crown", 4, 1300, "A crown of living fire", t =>
        {
            Bx(t, 0.58f, 0.06f, 0.53f, 0xFF6A1A, 0, 0.03f, 0, shadow: false);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f;
                var p = Quaternion.Euler(0, a, 0) * new Vector3(0.23f, 0, 0);
                float h = i % 2 == 0 ? 0.22f : 0.14f;
                Bx(t, 0.1f, h, 0.1f, i % 3 == 0 ? 0xFFE14D : i % 3 == 1 ? 0xFF8A1F : 0xFF6A1Au, p.x, h / 2 + 0.04f, p.z, ry: a, shadow: false);
            }
        }),
        NewHat("moons", "Orbiting moons", 4, 1500, "Three tiny moons circling your head", t =>
        {
            var orbit = Pivot(t, 0, 0.12f, 0, 10, 0, 0);
            foreach (var (a, c) in new[] { (0f, 0xE0E0E0u), (120f, 0xFFE08Au), (240f, 0x9EC9FFu) })
            {
                var p = Quaternion.Euler(0, a, 0) * new Vector3(0.42f, 0, 0);
                Box(orbit, 0.12f, 0.12f, 0.12f, c, p.x, 0, p.z);
            }
            Ring(orbit, 0.42f, 0, 0xFFFFFF, 24, 0.01f, false);
            orbit.gameObject.AddComponent<Spinner>().DegreesPerSecond = 70;
        }),
        NewHat("rockethat", "Rocket", 4, 1200, "A rocket ready for launch, right on your head", t =>
        {
            Box(t, 0.16f, 0.3f, 0.16f, 0xF4F4F4, 0, 0.19f, 0);
            Box(t, 0.12f, 0.08f, 0.12f, 0xE53935, 0, 0.38f, 0);
            Box(t, 0.06f, 0.06f, 0.06f, 0xE53935, 0, 0.45f, 0);
            Both(s => Box(t, 0.06f, 0.12f, 0.04f, 0xE53935, s * 0.1f, 0.08f, 0));
            Box(t, 0.04f, 0.12f, 0.06f, 0xE53935, 0, 0.08f, 0.1f);
            Box(t, 0.07f, 0.07f, 0.02f, 0x4FC3F7, 0, 0.24f, -0.09f);
            Both(s => Box(t, 0.1f, 0.05f, 0.1f, 0xECEFF1, s * 0.12f, 0.02f, -0.04f));
        }),
    };
}
