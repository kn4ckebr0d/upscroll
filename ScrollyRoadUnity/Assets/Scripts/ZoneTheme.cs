using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Colors per zone. Earth's zones repeat on most planets (recolored); hot and icy planets have their own.
public class ZoneTheme
{
    public string Name;
    public uint Sky;
    public uint[] Rocks;
    public uint Edge;
    public uint[] Accents;
    public float AccentChance;
    public uint Cap;      // grass/snow on exposed tops
    public uint Ledge;
    public uint Stamp;    // the zone's stamp color
    public uint[] Boulder;   // rock colors (two tones and the cracks), when the planet default wouldn't stand out
    public WeatherKind Weather;

    public static readonly ZoneTheme[] All =
    {
        new ZoneTheme { Name = "MEADOW CLIFFS", Sky = 0x8ED6F5, Rocks = new uint[] { 0x9C7B5B, 0x8A6B4E, 0xA88866 },
            Edge = 0x6E5238, Accents = new uint[] { 0x6CBF4B, 0x58A83A }, AccentChance = 0.1f, Cap = 0x6CBF4B, Ledge = 0x6B4423, Stamp = 0x5DAE4B },
        new ZoneTheme { Name = "SNOWY PEAKS", Sky = 0xC9E6F7, Rocks = new uint[] { 0xA7B0B8, 0x98A2AB, 0xB7BFC6 },
            Edge = 0x7D8790, Accents = new uint[] { 0xF4F8FB }, AccentChance = 0.15f, Cap = 0xFFFFFF, Ledge = 0x5D4037, Stamp = 0x5AA9E6 },
        new ZoneTheme { Name = "CLOUD STALK", Sky = 0x5B8FE0, Rocks = new uint[] { 0x4E9E3A, 0x5BAE45, 0x458C33 },
            Edge = 0x33702A, Accents = new uint[] { 0x7BD35A }, AccentChance = 0.1f, Cap = 0x9BE07A, Ledge = 0x2E5E22, Stamp = 0xF0A23B },
        new ZoneTheme { Name = "OUTER SPACE", Sky = 0x12122A, Rocks = new uint[] { 0x6B5A7E, 0x5D4E70, 0x7A6A8C },
            Edge = 0x43375A, Accents = new uint[] { 0x9CF2FF }, AccentChance = 0.06f, Cap = 0x9CF2FF, Ledge = 0x8E9AA6, Stamp = 0x7E62D9 },
    };

    /// Hot planets: red sand, desert, then lava up to the volcano.
    public static readonly ZoneTheme[] Hot =
    {
        new ZoneTheme { Name = "RED DUNES", Sky = 0xF7C890, Rocks = new uint[] { 0xD9744A, 0xCB6640, 0xE2835A },
            Edge = 0xA94C2C, Accents = new uint[] { 0xF2A766, 0xB8583A }, AccentChance = 0.14f, Cap = 0xF3B57E, Ledge = 0x7A3B22, Stamp = 0xE0703A },
        new ZoneTheme { Name = "DESERT MESA", Sky = 0xFFDCA3, Rocks = new uint[] { 0xE3A262, 0xD69352, 0xEBB377 },
            Edge = 0xB0703A, Accents = new uint[] { 0xC4643C, 0xF2C88A }, AccentChance = 0.1f, Cap = 0xF6D59C, Ledge = 0x6B4423, Stamp = 0xE5A43A },
        new ZoneTheme { Name = "LAVA FIELDS", Sky = 0x6B2E28, Rocks = new uint[] { 0x3C302C, 0x312724, 0x47382F },
            Edge = 0x221A18, Accents = new uint[] { 0xFF6A1F, 0xFFB12E }, AccentChance = 0.13f, Cap = 0xFF7A22, Ledge = 0x2A201C, Stamp = 0xE5482F,
            Boulder = new uint[] { 0x968A82, 0x82766F, 0xFF8A1F }, Weather = WeatherKind.Embers },
        new ZoneTheme { Name = "VOLCANO PEAK", Sky = 0x2C1424, Rocks = new uint[] { 0x4A3A36, 0x3E302D, 0x56443F },
            Edge = 0x2A201D, Accents = new uint[] { 0xFF5A1F, 0xFFC43D }, AccentChance = 0.08f, Cap = 0x6E5D57, Ledge = 0x8E9AA6, Stamp = 0x9C3B5A,
            Boulder = new uint[] { 0x968A82, 0x82766F, 0xFF8A1F }, Weather = WeatherKind.Embers },
    };

    /// Icy planets: snow, a blizzard, blue glacier ice and a frozen peak.
    public static readonly ZoneTheme[] Ice =
    {
        new ZoneTheme { Name = "FROST VALLEY", Sky = 0xCDEEFF, Rocks = new uint[] { 0xDCE8F0, 0xC9D9E5, 0xEAF2F8 },
            Edge = 0xA3BDD0, Accents = new uint[] { 0x8FD0F0, 0xFFFFFF }, AccentChance = 0.12f, Cap = 0xFFFFFF, Ledge = 0x5D4037, Stamp = 0x5AA9E6 },
        new ZoneTheme { Name = "BLIZZARD", Sky = 0xA9BCCB, Rocks = new uint[] { 0xB8C6D2, 0xA8B7C4, 0xC9D5DE },
            Edge = 0x8296A6, Accents = new uint[] { 0xFFFFFF }, AccentChance = 0.18f, Cap = 0xFFFFFF, Ledge = 0x4E3B30, Stamp = 0x7E9CB8,
            Weather = WeatherKind.Snow },
        new ZoneTheme { Name = "GLACIER", Sky = 0xA2DEF6, Rocks = new uint[] { 0x7FC8EC, 0x6BB8E0, 0x93D5F2 },
            Edge = 0x4A9CC8, Accents = new uint[] { 0xD8F4FF, 0xB8E8FF }, AccentChance = 0.15f, Cap = 0xF2FBFF, Ledge = 0x5D4037, Stamp = 0x3E8ED0 },
        new ZoneTheme { Name = "FROZEN PEAK", Sky = 0x16223F, Rocks = new uint[] { 0x5E7590, 0x52677F, 0x6B839E },
            Edge = 0x3A4A60, Accents = new uint[] { 0x8CF5C8, 0xBFEFFF }, AccentChance = 0.07f, Cap = 0xEAF4FF, Ledge = 0x8E9AA6, Stamp = 0x5B6FD6,
            Boulder = new uint[] { 0x2C3646, 0x232B38, 0x151B24 } },
    };

    /// Boss planets: a gate zone, then one big storm (three zones tall, one name).
    static readonly ZoneTheme MeteorStorm = new()
    {
        Name = "METEOR STORM", Sky = 0x2A1838, Rocks = new uint[] { 0x4B3F5E, 0x403552, 0x56496C },
        // Only cool purples on the wall, so the red meteor marks are the one warm thing to look at.
        Edge = 0x2C2440, Accents = new uint[] { 0x5E4F78, 0x6B5A86 }, AccentChance = 0.12f, Cap = 0x8C7BA6, Ledge = 0x6E5A4A, Stamp = 0xC2453A,
        Boulder = new uint[] { 0x9C8F86, 0x887C74, 0xFF8A1F }, Weather = WeatherKind.Embers,
    };

    public static readonly ZoneTheme[] Boss =
    {
        new ZoneTheme { Name = "METEOR GATE", Sky = 0x4A3A66, Rocks = new uint[] { 0x6A5C80, 0x5E5174, 0x75688C },
            Edge = 0x40355A, Accents = new uint[] { 0x8A7BAE, 0x9C8CC0 }, AccentChance = 0.08f, Cap = 0xA394C4, Ledge = 0x6E5A4A, Stamp = 0x8C5BC4 },
        MeteorStorm, MeteorStorm, MeteorStorm,
    };

    public static int ZonesPerPlanet => All.Length;

    static readonly Dictionary<int, ZoneTheme> cache = new();

    public static ZoneTheme ForZone(int zone)
    {
        zone = Mathf.Max(0, zone);
        if (cache.TryGetValue(zone, out var theme)) return theme;
        var baseTheme = All[zone % All.Length];
        int planet = zone / All.Length;
        theme = planet == 0 ? baseTheme : baseTheme.Shifted(planet * Tuning.PlanetHueShift);
        cache[zone] = theme;
        return theme;
    }

    public static string Label(int zone) => $"PLANET {zone / All.Length + 1} · ZONE {zone % All.Length + 1}";

    ZoneTheme Shifted(float delta) => new ZoneTheme
    {
        Name = Name,
        Sky = Shift(Sky, delta),
        Rocks = Rocks.Select(c => Shift(c, delta)).ToArray(),
        Edge = Shift(Edge, delta),
        Accents = Accents.Select(c => Shift(c, delta)).ToArray(),
        AccentChance = AccentChance,
        Cap = Shift(Cap, delta),
        Ledge = Shift(Ledge, delta),
        Stamp = Shift(Stamp, delta),
        Weather = Weather,
    };

    /// The same zone recolored toward a planet's look (icy blue, desert orange, moon grey…).
    public ZoneTheme Tinted(uint target, float amount, uint sky, float skyAmount) => new ZoneTheme
    {
        Name = Name,
        Sky = Mix(Sky, sky, skyAmount),
        Rocks = Rocks.Select(c => Mix(c, target, amount)).ToArray(),
        Edge = Mix(Edge, target, amount * 0.8f),
        Accents = Accents.Select(c => Mix(c, target, amount)).ToArray(),
        AccentChance = AccentChance,
        Cap = Mix(Cap, target, amount),
        Ledge = Ledge,
        Stamp = Mix(Stamp, target, amount * 0.5f),
        Weather = Weather,
    };

    static uint Mix(uint a, uint b, float t) => ToHex(Color.Lerp(Voxel.Hex(a), Voxel.Hex(b), t));

    static uint Shift(uint hex, float delta)
    {
        Color.RGBToHSV(Voxel.Hex(hex), out float h, out float s, out float v);
        return ToHex(Color.HSVToRGB(Mathf.Repeat(h + delta, 1f), s, v));
    }

    static uint ToHex(Color c) =>
        ((uint)Mathf.RoundToInt(c.r * 255) << 16) | ((uint)Mathf.RoundToInt(c.g * 255) << 8) | (uint)Mathf.RoundToInt(c.b * 255);
}
