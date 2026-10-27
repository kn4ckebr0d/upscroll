using System.Collections.Generic;
using UnityEngine;

/// Every planet after Earth gets a random effect (never the same twice in a row), with its own colors.
/// Every 4 to 5 planets comes a boss planet instead: a warm-up zone, then one big meteor storm.
public partial class ScrollyGame
{
    enum PlanetEffect { None, LowGravity, HeavyGravity, Ice, Hot, Boss }

    // None is a calm planet in between, to mix things up (Earth's zones, recolored).
    static readonly PlanetEffect[] EffectPool = { PlanetEffect.None, PlanetEffect.LowGravity, PlanetEffect.HeavyGravity, PlanetEffect.Ice, PlanetEffect.Hot };

    readonly List<PlanetEffect> planetEffects = new();   // per planet, rolled as the run goes
    readonly Dictionary<int, ZoneTheme> themeCache = new();
    PlanetEffect? forcedEffect;                           // test harness: -scrollyEffect Ice
    int nextBossPlanet;                                   // the next boss planet in this run

    void ResetPlanetEffects()
    {
        planetEffects.Clear();
        themeCache.Clear();
        nextBossPlanet = Progress.BossFirst ? 1 : Random.Range(4, 6);
    }

    PlanetEffect EffectFor(int planet)
    {
        while (planetEffects.Count <= planet)
        {
            if (planetEffects.Count == 0)
            {
                planetEffects.Add(PlanetEffect.None);   // Earth is always normal
                continue;
            }
            if (forcedEffect.HasValue)
            {
                planetEffects.Add(forcedEffect.Value);
                continue;
            }
            int upcoming = planetEffects.Count;
            if (upcoming >= nextBossPlanet)
            {
                planetEffects.Add(PlanetEffect.Boss);
                nextBossPlanet = upcoming + Random.Range(4, 6);
                continue;
            }
            planetEffects.Add(PickEffect(planetEffects[^1]));
        }
        return planetEffects[planet];
    }

    /// Weighted random pick, never the same as the previous planet. The hot planet is the
    /// toughest, so it comes up half as often; a calm planet comes up now and then.
    static PlanetEffect PickEffect(PlanetEffect previous)
    {
        float Weight(PlanetEffect e) => e == previous ? 0 : e switch { PlanetEffect.Hot => 0.5f, PlanetEffect.None => 0.6f, _ => 1f };
        float total = 0;
        foreach (var e in EffectPool) total += Weight(e);
        float roll = Random.value * total;
        foreach (var e in EffectPool)
        {
            roll -= Weight(e);
            if (roll <= 0 && Weight(e) > 0) return e;
        }
        return EffectPool[0] == previous ? EffectPool[1] : EffectPool[0];
    }

    PlanetEffect EffectAt(float y) => EffectFor(PlanetIndex(y + 0.01f));

    /// On a boss planet, everything above the first zone is one big meteor storm.
    bool InMeteorStorm(float y) =>
        EffectAt(y) == PlanetEffect.Boss && Mathf.Max(0, y) - PlanetBase(PlanetIndex(y + 0.01f)) >= Tuning.ZoneLength - 0.01f;
    PlanetEffect CurrentEffect => EffectAt(anchorY);

    static string EffectTitle(PlanetEffect effect) => effect switch
    {
        PlanetEffect.LowGravity => "LOW GRAVITY",
        PlanetEffect.HeavyGravity => "HEAVY GRAVITY",
        PlanetEffect.Ice => "ICE PLANET",
        PlanetEffect.Hot => "HOT PLANET",
        PlanetEffect.Boss => "BOSS PLANET",
        PlanetEffect.None => "CALM PLANET",
        _ => "",
    };

    static string EffectTip(PlanetEffect effect) => effect switch
    {
        PlanetEffect.LowGravity => "Low gravity! Pulling is easy,\nbut letting go floats you back slowly.",
        PlanetEffect.HeavyGravity => "Heavy gravity! Every meter is hard.\nRest on the ledge halfway up.",
        PlanetEffect.Ice => "Ice planet! Keep pulling,\nor you slowly slip back down.",
        PlanetEffect.Hot => "Hot planet! Stamina drains fast.\nGrab water drops to cool down.",
        PlanetEffect.Boss => "Boss planet! Meteors are coming.\nSee a red mark? Swipe out of the way!",
        _ => "",
    };

    /// How strong planet effects are: gentle on the first planets after Earth, full strength from the
    /// fourth on, so the challenge grows with the run instead of hitting all at once.
    float EffectStrength => Mathf.Lerp(0.45f, 1f, Mathf.Clamp01((PlanetIndex(anchorY + 0.01f) - 1) / 3f));

    float GravityStretchFactor => CurrentEffect switch
    {
        PlanetEffect.LowGravity => 1.45f,
        PlanetEffect.HeavyGravity => Mathf.Lerp(1f, 0.55f, EffectStrength),   // much heavier, but every zone has a rest ledge halfway
        _ => 1f,
    };

    float StaminaDrainFactor => CurrentEffect switch
    {
        PlanetEffect.Hot => 1 + 0.6f * EffectStrength,
        PlanetEffect.LowGravity => 0.8f,
        _ => 1f,
    };

    float FallGravityFactor => CurrentEffect switch
    {
        PlanetEffect.LowGravity => 0.6f,     // floaty falls: you survive longer ones
        PlanetEffect.HeavyGravity => 1.15f,
        _ => 1f,
    };

    float MobSpeedFactor(float y) => EffectAt(y) == PlanetEffect.LowGravity ? 0.85f : 1f;

    /// Which mob flies in a zone. Hot planets swap birds for vultures in the desert and skeleton birds
    /// over the lava (where planes would fly, too).
    MobKind MobFor(int zone)
    {
        var kind = Mobs.ForZone(zone);
        if (EffectFor(zone / ZonesPerPlanet) != PlanetEffect.Hot || kind == MobKind.Ufo) return kind;
        return ThemeFor(zone).Weather == WeatherKind.Embers ? MobKind.Skeleton : MobKind.Vulture;
    }

    /// Zone colors for this run: Earth as designed, hot and icy planets with their own zones,
    /// the gravity planets tinted to match their effect.
    ZoneTheme ThemeFor(int zone)
    {
        if (themeCache.TryGetValue(zone, out var theme)) return theme;
        var effect = EffectFor(zone / ZonesPerPlanet);
        theme = effect == PlanetEffect.None ? ZoneTheme.ForZone(zone) : PlanetTheme(effect, zone % ZonesPerPlanet);
        themeCache[zone] = theme;
        return theme;
    }

    /// The look of a planet type's zone (Earth's colors for planets without an effect).
    static ZoneTheme PlanetTheme(PlanetEffect effect, int index)
    {
        var baseTheme = ZoneTheme.All[index];
        // Keep space dark: only nudge the sky of each planet's last zone.
        float skyAmount = index == ZonesPerPlanet - 1 ? 0.15f : 0.75f;
        return effect switch
        {
            PlanetEffect.Ice => ZoneTheme.Ice[index],
            PlanetEffect.Hot => ZoneTheme.Hot[index],
            PlanetEffect.Boss => ZoneTheme.Boss[index],
            PlanetEffect.LowGravity => baseTheme.Tinted(0xB4B4C6, 0.6f, 0x3A3D6B, skyAmount),
            PlanetEffect.HeavyGravity => baseTheme.Tinted(0xA4502F, 0.42f, 0xE59C76, skyAmount),
            _ => baseTheme,
        };
    }
}
